using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace ThGameMode.WebServer
{
    /// <summary>
    /// Estima o consumo de energia dos componentes do PC a partir da utilização
    /// da CPU/GPU e do hardware detectado, integrando o total ao longo do tempo.
    ///
    /// O Windows não expõe a potência real de cada componente em desktops comuns,
    /// por isso os valores são estimativas (baseadas em TDP + utilização).
    /// O acumulado (kWh) é persistido em disco e somado apenas enquanto o PC está ligado.
    /// </summary>
    public sealed class PowerConsumptionService : IDisposable
    {
        // ── Parâmetros do modelo de estimativa ───────────────────────────────
        private const double PsuEfficiency = 0.88;      // eficiência típica de uma fonte 80 Plus
        private const double CpuIdleWatts = 8.0;
        private const double GpuIdleWatts = 8.0;
        private const double MotherboardWatts = 18.0;   // chipset, VRM, controladores
        private const double FansWatts = 6.0;
        private const double PeripheralsWatts = 8.0;    // USB, rede, áudio, etc.
        private const double DiskWattsPerDrive = 4.0;
        private const double MemoryBaseWatts = 3.0;
        private const double MemoryWattsPerGb = 0.4;

        // Energia (W·s) → kWh: 1 kWh = 3.600.000 W·s
        private const double WattsSecondToKwh = 1.0 / 3_600_000.0;

        private readonly object _lock = new();
        private readonly string _statePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ThGameMode",
            "power-usage.json");

        private long _lastTimestamp;
        private double _lastWatts;
        private DateTime _lastPersistUtc = DateTime.UtcNow;

        private double _sessionKwh;
        private double _totalKwh;

        // Hardware detectado (cache para não reavaliar TDP a cada coleta)
        private string _cpuName = string.Empty;
        private string _gpuName = string.Empty;
        private int _cpuCores = -1;
        private double _cpuTdpWatts = 95;
        private double _gpuTdpWatts = 150;

        private volatile PowerUsage _current = new();

        public PowerConsumptionService()
        {
            LoadState();
            _lastTimestamp = Stopwatch.GetTimestamp();
        }

        public PowerUsage Current => _current;

        /// <summary>
        /// Recalcula o consumo atual e acumula a energia desde a última chamada.
        /// Deve ser chamado periodicamente (o SystemMonitorService chama a cada 2 s).
        /// </summary>
        public PowerUsage Update(
            double cpuPercent, string? cpuName, int cpuCores,
            double gpuPercent, string? gpuName,
            double memTotalGb, int diskCount)
        {
            UpdateHardware(cpuName, cpuCores, gpuName);

            double cpuWatts = EstimateLoadWatts(CpuIdleWatts, _cpuTdpWatts, cpuPercent);
            double gpuWatts = EstimateLoadWatts(GpuIdleWatts, _gpuTdpWatts, gpuPercent);
            double memWatts = MemoryBaseWatts + Math.Max(0, memTotalGb) * MemoryWattsPerGb;
            double diskWatts = Math.Max(0, diskCount) * DiskWattsPerDrive;

            double totalWatts = cpuWatts + gpuWatts + memWatts + diskWatts
                                + MotherboardWatts + FansWatts + PeripheralsWatts;
            double wallWatts = totalWatts / PsuEfficiency;

            lock (_lock)
            {
                long now = Stopwatch.GetTimestamp();
                double elapsedSeconds = (now - _lastTimestamp) / (double)Stopwatch.Frequency;
                _lastTimestamp = now;

                // Ignora intervalos muito longos (suspensão/hibernação) para não inflar o total.
                if (elapsedSeconds > 0 && elapsedSeconds <= 30)
                {
                    double kwh = totalWatts * elapsedSeconds * WattsSecondToKwh;
                    _sessionKwh += kwh;
                    _totalKwh += kwh;
                }

                _lastWatts = totalWatts;

                if ((DateTime.UtcNow - _lastPersistUtc).TotalSeconds >= 30)
                    SaveState();

                _current = new PowerUsage
                {
                    CpuWatts = Round1(cpuWatts),
                    GpuWatts = Round1(gpuWatts),
                    MemoryWatts = Round1(memWatts),
                    DiskWatts = Round1(diskWatts),
                    MotherboardWatts = Round1(MotherboardWatts),
                    FansWatts = Round1(FansWatts),
                    PeripheralsWatts = Round1(PeripheralsWatts),
                    TotalWatts = Round1(totalWatts),
                    WallWatts = Round1(wallWatts),
                    CpuTdpWatts = Round1(_cpuTdpWatts),
                    GpuTdpWatts = Round1(_gpuTdpWatts),
                    SessionKwh = Math.Round(_sessionKwh, 4),
                    TotalKwh = Math.Round(_totalKwh, 4),
                    IsEstimated = true
                };

                return _current;
            }
        }

        private void UpdateHardware(string? cpuName, int cpuCores, string? gpuName)
        {
            if (!string.Equals(cpuName, _cpuName, StringComparison.OrdinalIgnoreCase) || cpuCores != _cpuCores)
            {
                _cpuName = cpuName ?? string.Empty;
                _cpuCores = cpuCores;
                _cpuTdpWatts = EstimateCpuTdp(_cpuName, cpuCores);
            }

            if (!string.Equals(gpuName, _gpuName, StringComparison.OrdinalIgnoreCase))
            {
                _gpuName = gpuName ?? string.Empty;
                _gpuTdpWatts = EstimateGpuTdp(_gpuName);
            }
        }

        /// <summary>
        /// Consumo em função da utilização: parte do idle e sobe até o TDP.
        /// A curva ^1.25 aproxima o comportamento real (consumo cresce mais no fim).
        /// </summary>
        private static double EstimateLoadWatts(double idleWatts, double tdpWatts, double percent)
        {
            double load = Math.Clamp(percent, 0, 100) / 100.0;
            double headroom = Math.Max(0, tdpWatts - idleWatts);
            return idleWatts + headroom * Math.Pow(load, 1.25);
        }

        private static double EstimateCpuTdp(string name, int cores)
        {
            if (cores <= 0) cores = Environment.ProcessorCount;

            double tdp = cores switch
            {
                <= 2 => 35,
                <= 4 => 45,
                <= 6 => 65,
                <= 8 => 95,
                <= 12 => 125,
                <= 16 => 150,
                <= 20 => 180,
                <= 24 => 210,
                _ => 250
            };

            string n = name.ToLowerInvariant();
            if (n.Contains("ryzen 9") || n.Contains("i9"))
                tdp = Math.Max(tdp, cores >= 16 ? 180 : 125);
            else if (n.Contains("ryzen 7") || n.Contains("i7"))
                tdp = Math.Max(tdp, cores >= 12 ? 125 : 95);
            else if (n.Contains("ryzen 5") || n.Contains("i5"))
                tdp = Math.Max(tdp, 65);

            // Modelos mobile "H/HX" consomem mais que os "U" de baixa voltagem.
            if (n.Contains("hx") || System.Text.RegularExpressions.Regex.IsMatch(n, @"\b\d{3,5}h\b"))
                tdp *= 1.15;
            else if (System.Text.RegularExpressions.Regex.IsMatch(n, @"\b\d{3,5}u\b"))
                tdp *= 0.75;

            return Math.Clamp(tdp, 35, 280);
        }

        private static double EstimateGpuTdp(string name)
        {
            string n = System.Text.RegularExpressions.Regex
                .Replace(name.ToLowerInvariant(), @"\(r\)|\(tm\)|\(c\)", string.Empty)
                .Replace("  ", " ")
                .Trim();
            if (string.IsNullOrWhiteSpace(n) || n == "n/a") return 150;

            // GPUs integradas (usa pouca energia e divide memória com o sistema)
            if (!n.Contains("radeon rx") && !n.Contains("arc a") &&
                (n.Contains("uhd graphics") || n.Contains("hd graphics") ||
                 n.Contains("iris") || n.Contains("radeon graphics") ||
                 n.Contains("vega") || n.Contains("arc graphics")))
                return 20;

            // Modelos conhecidos (do mais específico para o mais genérico)
            (string Model, double Tdp)[] models =
            {
                ("rtx 5090", 575), ("rtx 5080", 360), ("rtx 5070 ti", 300), ("rtx 5070", 250),
                ("rtx 5060 ti", 180), ("rtx 5060", 145),
                ("rtx 4090", 450), ("rtx 4080", 320), ("rtx 4070 ti super", 285),
                ("rtx 4070 ti", 285), ("rtx 4070 super", 220), ("rtx 4070", 200),
                ("rtx 4060 ti", 160), ("rtx 4060", 115),
                ("rtx 3090", 350), ("rtx 3080", 320), ("rtx 3070 ti", 290), ("rtx 3070", 220),
                ("rtx 3060 ti", 200), ("rtx 3060", 170), ("rtx 3050", 130),
                ("rtx 2080 ti", 260), ("rtx 2080", 225), ("rtx 2070", 185),
                ("rtx 2060", 160), ("gtx 1660", 120), ("gtx 1650", 75),
                ("gtx 1080 ti", 250), ("gtx 1080", 180), ("gtx 1070", 150),
                ("gtx 1060", 120), ("gtx 1050", 75),
                ("rx 7900 xtx", 355), ("rx 7900 xt", 315), ("rx 7800 xt", 263),
                ("rx 7700 xt", 245), ("rx 7600", 165),
                ("rx 6900 xt", 300), ("rx 6800 xt", 300), ("rx 6800", 250),
                ("rx 6700 xt", 230), ("rx 6600 xt", 160), ("rx 6600", 132),
                ("rx 5700 xt", 225), ("rx 5600 xt", 150), ("rx 5500 xt", 130),
                ("arc a770", 225), ("arc a750", 225), ("arc a580", 185), ("arc a380", 75)
            };

            foreach (var (model, tdp) in models)
                if (n.Contains(model)) return tdp;

            if (n.Contains("rtx")) return 180;
            if (n.Contains("gtx")) return 130;
            if (n.Contains("radeon rx") || n.Contains("arc")) return 180;
            return 150;
        }

        private static double Round1(double value) => Math.Round(value, 1);

        // ── Persistência do acumulado ────────────────────────────────────────
        private sealed class PersistedState
        {
            public double TotalKwh { get; set; }
            public double LastWatts { get; set; }
            public DateTime LastSaveUtc { get; set; }
            public DateTime BootTimeUtc { get; set; }
        }

        private static DateTime CurrentBootTimeUtc =>
            DateTime.UtcNow.AddMilliseconds(-Environment.TickCount64);

        private void LoadState()
        {
            try
            {
                if (!File.Exists(_statePath)) return;

                var json = File.ReadAllText(_statePath);
                var state = JsonSerializer.Deserialize<PersistedState>(json);
                if (state == null) return;

                if (state.TotalKwh > 0 && double.IsFinite(state.TotalKwh))
                    _totalKwh = state.TotalKwh;

                // Se a máquina não reiniciou, soma o período em que o app ficou fechado
                // (o PC continuou ligado). Reinícios não entram na conta.
                var boot = CurrentBootTimeUtc;
                bool sameBoot = Math.Abs((boot - state.BootTimeUtc).TotalMinutes) < 3;
                var gap = DateTime.UtcNow - state.LastSaveUtc;

                if (sameBoot && state.LastWatts > 0 &&
                    gap > TimeSpan.Zero && gap <= TimeSpan.FromHours(12))
                {
                    _totalKwh += state.LastWatts * gap.TotalSeconds * WattsSecondToKwh;
                }
            }
            catch
            {
                // Estado corrompido/inacessível: recomeça do zero sem derrubar o app.
                _totalKwh = 0;
            }
        }

        private void SaveState()
        {
            try
            {
                string? dir = Path.GetDirectoryName(_statePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var state = new PersistedState
                {
                    TotalKwh = _totalKwh,
                    LastWatts = _lastWatts,
                    LastSaveUtc = DateTime.UtcNow,
                    BootTimeUtc = CurrentBootTimeUtc
                };

                // Grava em arquivo temporário e troca para evitar estado parcial.
                string tmp = _statePath + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(state));
                File.Move(tmp, _statePath, overwrite: true);

                _lastPersistUtc = DateTime.UtcNow;
            }
            catch
            {
                // Falha de I/O não pode interromper o monitoramento.
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                SaveState();
            }
        }
    }

    /// <summary>Snapshot do consumo estimado de energia.</summary>
    public sealed class PowerUsage
    {
        public double CpuWatts { get; init; }
        public double GpuWatts { get; init; }
        public double MemoryWatts { get; init; }
        public double DiskWatts { get; init; }
        public double MotherboardWatts { get; init; }
        public double FansWatts { get; init; }
        public double PeripheralsWatts { get; init; }

        /// <summary>Soma de todos os componentes (watts).</summary>
        public double TotalWatts { get; init; }

        /// <summary>Total considerando a eficiência da fonte (watts na tomada).</summary>
        public double WallWatts { get; init; }

        public double CpuTdpWatts { get; init; }
        public double GpuTdpWatts { get; init; }

        /// <summary>Energia consumida desde que o app iniciou (kWh).</summary>
        public double SessionKwh { get; init; }

        /// <summary>Energia acumulada enquanto o PC esteve ligado (kWh).</summary>
        public double TotalKwh { get; init; }

        public bool IsEstimated { get; init; }
    }
}
