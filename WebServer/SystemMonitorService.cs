using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ThGameMode.WebServer
{
    /// <summary>
    /// Coleta métricas do sistema (CPU, RAM, Disco, GPU, Clima) em background
    /// e as fornece para o servidor web embutido.
    /// Conversão do pc-dashboard/app.py (Python/Flask) para C#.
    /// </summary>
    public sealed class SystemMonitorService : IDisposable
    {
        private volatile SystemStats _current = new();
        private readonly System.Threading.Timer _timer;
        private readonly HttpClient _http;

        // Cache de clima
        private SystemStats.WeatherInfo? _weatherCache;
        private DateTime _weatherCacheTime = DateTime.MinValue;
        private const int WeatherCacheMinutes = 10;

        // Cache de frequência de CPU (WMI é lento, consultar de 10 em 10 s)
        private double _cpuFreqGhzCache;
        private DateTime _cpuFreqTime = DateTime.MinValue;

        // PerformanceCounter de CPU (amostragem em 1 s como no psutil)
        private readonly PerformanceCounter _cpuCounter;
        private bool _firstCpuRead = true;

        public SystemMonitorService()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true);
            _cpuCounter.NextValue(); // Primeiro valor sempre 0, descartar

            // Primeira coleta imediata e depois a cada 2 s (igual ao Python)
            _timer = new System.Threading.Timer(Collect, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }

        public SystemStats GetCurrentStats() => _current;

        private async void Collect(object? _)
        {
            try
            {
                var cpu = GetCpu();
                var mem = GetMemory();
                var disk = GetDisk();
                var gpu = GetGpu();
                var uptime = GetUptime();
                var weather = await GetWeatherAsync();
                var ip = GetLocalIp();

                _current = new SystemStats
                {
                    CpuPercent = cpu.Percent,
                    CpuCount = cpu.Count,
                    CpuFreqGhz = cpu.FreqGhz,
                    MemPercent = mem.Percent,
                    MemUsedGb = mem.UsedGb,
                    MemTotalGb = mem.TotalGb,
                    DiskPercent = disk.Percent,
                    DiskUsedGb = disk.UsedGb,
                    DiskFreeGb = disk.FreeGb,
                    DiskTotalGb = disk.TotalGb,
                    GpuName = gpu.Name,
                    GpuPercent = gpu.Percent,
                    GpuTempC = gpu.TempC,
                    Uptime = uptime,
                    WeatherTemp = weather?.Temp ?? "--",
                    WeatherDesc = weather?.Desc ?? "Sem internet",
                    LocalIp = ip,
                    Timestamp = DateTime.Now.ToString("HH:mm:ss")
                };
            }
            catch
            {
                // Nunca derrubar o app por falha de coleta
            }
        }

        // ── CPU ─────────────────────────────────────────────────────────────
        private (double Percent, int Count, double FreqGhz) GetCpu()
        {
            double percent = 0;
            try
            {
                // Descarta a primeira leitura "0" logo após o construtor
                var raw = _cpuCounter.NextValue();
                if (_firstCpuRead) { _firstCpuRead = false; raw = _cpuCounter.NextValue(); }
                percent = Math.Round(raw, 1);
            }
            catch { }

            double freqGhz = _cpuFreqGhzCache;
            if ((DateTime.Now - _cpuFreqTime).TotalSeconds > 10)
            {
                freqGhz = GetCpuFreqGhzWmi();
                _cpuFreqGhzCache = freqGhz;
                _cpuFreqTime = DateTime.Now;
            }

            return (percent, Environment.ProcessorCount, freqGhz);
        }

        private static double GetCpuFreqGhzWmi()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT CurrentClockSpeed FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                    return Math.Round((double)(uint)obj["CurrentClockSpeed"] / 1000.0, 1);
            }
            catch { }
            return 0;
        }

        // ── Memória RAM ──────────────────────────────────────────────────────
        private static (double Percent, double UsedGb, double TotalGb) GetMemory()
        {
            try
            {
                ulong totalKb = 0, freeKb = 0;
                using var searcher = new ManagementObjectSearcher(
                    "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    totalKb = (ulong)obj["TotalVisibleMemorySize"];
                    freeKb = (ulong)obj["FreePhysicalMemory"];
                }
                if (totalKb == 0) return (0, 0, 0);
                double totalGb = Math.Round(totalKb / 1024.0 / 1024.0, 1);
                double freeGb = Math.Round(freeKb / 1024.0 / 1024.0, 1);
                double usedGb = Math.Round(totalGb - freeGb, 1);
                double pct = Math.Round(usedGb / totalGb * 100, 1);
                return (pct, usedGb, totalGb);
            }
            catch { return (0, 0, 0); }
        }

        // ── Disco ────────────────────────────────────────────────────────────
        private static (double Percent, double UsedGb, double FreeGb, double TotalGb) GetDisk()
        {
            try
            {
                var drives = System.IO.DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == System.IO.DriveType.Fixed)
                    .ToArray();

                double totalBytes = drives.Sum(d => (double)d.TotalSize);
                double freeBytes = drives.Sum(d => (double)d.TotalFreeSpace);
                double usedBytes = totalBytes - freeBytes;

                double totalGb = Math.Round(totalBytes / 1e9, 1);
                double freeGb = Math.Round(freeBytes / 1e9, 1);
                double usedGb = Math.Round(usedBytes / 1e9, 1);
                double pct = totalGb > 0 ? Math.Round(usedGb / totalGb * 100, 1) : 0;

                return (pct, usedGb, freeGb, totalGb);
            }
            catch { return (0, 0, 0, 0); }
        }

        // ── GPU (via WMI Performance) ────────────────────────────────────────
        private static (string Name, int Percent, int TempC) GetGpu()
        {
            string name = "N/A";
            int percent = 0;
            int temp = 0;

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name FROM Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    name = obj["Name"]?.ToString() ?? "N/A";
                    break;
                }
            }
            catch { }

            try
            {
                // Uso da GPU via EngineUtilization (funciona para AMD/Intel/NVIDIA via WDDM)
                using var searcher = new ManagementObjectSearcher(
                    @"\\.\root\cimv2",
                    "SELECT UtilizationDefault FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine " +
                    "WHERE Name LIKE '%engtype_3D%'");
                int max = 0;
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["UtilizationDefault"] is ulong v && (int)v > max) max = (int)v;
                }
                percent = Math.Min(max, 100);
            }
            catch { }

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    @"\\.\root\cimv2",
                    "SELECT Temperature FROM MSAcpi_ThermalZoneTemperature");
                foreach (ManagementObject obj in searcher.Get())
                {
                    // kelvin * 10 → celsius
                    if (obj["Temperature"] is uint k)
                    { temp = (int)(k / 10.0 - 273.15); break; }
                }
            }
            catch { }

            return (name, percent, temp);
        }

        // ── Uptime ───────────────────────────────────────────────────────────
        private static string GetUptime()
        {
            try
            {
                var ts = TimeSpan.FromMilliseconds(Environment.TickCount64);
                if (ts.Days > 0) return $"{ts.Days}d {ts.Hours}h {ts.Minutes}m";
                if (ts.Hours > 0) return $"{ts.Hours}h {ts.Minutes}m";
                return $"{ts.Minutes}m";
            }
            catch { return "--"; }
        }

        // ── Clima (open-meteo, São Paulo) ────────────────────────────────────
        private async Task<SystemStats.WeatherInfo?> GetWeatherAsync()
        {
            if (_weatherCache != null &&
                (DateTime.Now - _weatherCacheTime).TotalMinutes < WeatherCacheMinutes)
                return _weatherCache;

            try
            {
                const string url =
                    "https://api.open-meteo.com/v1/forecast" +
                    "?latitude=-23.5505&longitude=-46.6333" +
                    "&current_weather=true&temperature_unit=celsius";

                var json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var cw = doc.RootElement.GetProperty("current_weather");
                double tmp = cw.GetProperty("temperature").GetDouble();
                int code = cw.GetProperty("weathercode").GetInt32();

                var descs = new System.Collections.Generic.Dictionary<int, string>
                {
                    {0,"Céu claro"},{1,"Poucas nuvens"},{2,"Nuvens parciais"},{3,"Nublado"},
                    {45,"Nevoeiro"},{48,"Nevoeiro com queda"},{51,"Chuvas leves"},
                    {53,"Chuvas moderadas"},{61,"Chuvas fortes"},{80,"Chuva torrencial"}
                };

                _weatherCache = new SystemStats.WeatherInfo
                {
                    Temp = ((int)Math.Round(tmp)).ToString(),
                    Desc = descs.TryGetValue(code, out var d) ? d : "Normal"
                };
                _weatherCacheTime = DateTime.Now;
                return _weatherCache;
            }
            catch { return _weatherCache; }
        }

        // ── IP local ─────────────────────────────────────────────────────────
        private static string GetLocalIp()
        {
            try
            {
                using var s = new System.Net.Sockets.UdpClient();
                s.Connect("8.8.8.8", 80);
                return ((System.Net.IPEndPoint)s.Client.LocalEndPoint!).Address.ToString();
            }
            catch { return "127.0.0.1"; }
        }

        public void Dispose()
        {
            _timer.Dispose();
            _cpuCounter.Dispose();
            _http.Dispose();
        }
    }

    /// <summary>Snapshot das métricas coletadas.</summary>
    public sealed class SystemStats
    {
        public double CpuPercent { get; init; }
        public int CpuCount { get; init; }
        public double CpuFreqGhz { get; init; }

        public double MemPercent { get; init; }
        public double MemUsedGb { get; init; }
        public double MemTotalGb { get; init; }

        public double DiskPercent { get; init; }
        public double DiskUsedGb { get; init; }
        public double DiskFreeGb { get; init; }
        public double DiskTotalGb { get; init; }

        public string GpuName { get; init; } = "N/A";
        public int GpuPercent { get; init; }
        public int GpuTempC { get; init; }

        public string Uptime { get; init; } = "--";
        public string WeatherTemp { get; init; } = "--";
        public string WeatherDesc { get; init; } = "--";
        public string LocalIp { get; init; } = "127.0.0.1";
        public string Timestamp { get; init; } = "--:--:--";

        public sealed class WeatherInfo
        {
            public string Temp { get; set; } = "--";
            public string Desc { get; set; } = "--";
        }
    }
}
