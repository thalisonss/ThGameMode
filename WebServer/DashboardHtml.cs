namespace ThGameMode.WebServer
{
    /// <summary>
    /// Gera o HTML do dashboard em C# — conversão fiel do templates/index.html do pc-dashboard Python.
    /// Serve arquivos estáticos (CSS/JS) via /css/style.css e /js/app.js,
    /// ou inline para evitar dependência de arquivos externos.
    /// </summary>
    internal static class DashboardHtml
    {
        public static string Render(SystemStats s)
        {
            // Limita a largura das barras de progresso entre 0 e 100
            static int Bar(double v) => (int)System.Math.Max(0, System.Math.Min(100, v));
            static string W(double v) => System.Math.Round(v).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            static string Kwh(double v) => v.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

            return $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no"">
    <meta http-equiv=""cache-control"" content=""no-cache, no-store, must-revalidate"">
    <meta http-equiv=""expires"" content=""0"">
    <title>Dashboard PC</title>
    <link rel=""stylesheet"" href=""/css/style.css"">
</head>
<body>
    <div class=""status-bar"">
        <span class=""dot online""></span>
        <span id=""timestamp"" class=""time-big"">{s.Timestamp}</span>
        <span class=""uptime-badge"" style=""margin-left: auto; font-size: 0.9rem; color: #fff; background: rgba(255,255,255,0.1); padding: 4px 10px; border-radius: 6px;"">⏱️ <strong id=""uptime"">{s.Uptime}</strong></span>
    </div>

    <div class=""weather-row"">
        <span id=""weather-city"">São Paulo</span>
        <span id=""weather-temp"" class=""weather-temp"">{s.WeatherTemp}°C</span>
    </div>

    <main class=""main-content"">
        <div class=""grid grid-2"">
            <div class=""card"">
                <div class=""card-header""><h2>CPU</h2><span class=""value cpu"" id=""cpu-percent"">{s.CpuPercent}%</span></div>
                <div class=""progress-bar""><div class=""progress-fill cpu"" id=""cpu-bar"" style=""width:{Bar(s.CpuPercent)}%""></div></div>
                <div class=""card-footer"">
                    <span>Núcleos: <strong id=""cpu-count"">{s.CpuCount}</strong></span>
                    <span>Freq: <strong id=""cpu-freq"">{s.CpuFreqGhz}</strong> GHz</span>
                    <span>Temp: <strong id=""cpu-temp"">{s.CpuTempC}</strong>°C</span>
                </div>
            </div>
            <div class=""card"">
                <div class=""card-header""><h2>GPU</h2><span class=""value gpu"" id=""gpu-percent"">{s.GpuPercent}%</span></div>
                <div class=""progress-bar""><div class=""progress-fill gpu"" id=""gpu-bar"" style=""width:{Bar(s.GpuPercent)}%""></div></div>
                <div class=""card-footer"">
                    <span id=""gpu-name"" title=""{s.GpuName}"">{s.GpuName}</span>
                    <span>Temp: <strong id=""gpu-temp"">{s.GpuTempC}</strong>°C</span>
                </div>
            </div>
        </div>

        <div class=""grid grid-2"">
            <div class=""card"">
                <div class=""card-header""><h2>RAM</h2><span class=""value mem"" id=""mem-percent"">{s.MemPercent}%</span></div>
                <div class=""progress-bar""><div class=""progress-fill mem"" id=""mem-bar"" style=""width:{Bar(s.MemPercent)}%""></div></div>
                <div class=""card-footer"">
                    <span>Usado: <strong id=""mem-used"">{s.MemUsedGb}</strong> GB</span>
                    <span>Total: <strong id=""mem-total"">{s.MemTotalGb}</strong> GB</span>
                    <span>Temp: <strong id=""mem-temp"">{s.MemTempC}</strong>°C</span>
                </div>
            </div>
            <div class=""card"">
                <div class=""card-header""><h2>Disco</h2><span class=""value disk"" id=""disk-percent"">{s.DiskPercent}%</span></div>
                <div class=""progress-bar""><div class=""progress-fill disk"" id=""disk-bar"" style=""width:{Bar(s.DiskPercent)}%""></div></div>
                <div class=""card-footer"">
                    <span>Usado: <strong id=""disk-used"">{s.DiskUsedGb}</strong> GB</span>
                    <span>Livre: <strong id=""disk-free"">{s.DiskFreeGb}</strong> GB</span>
                    <span>Temp: <strong id=""disk-temp"">{s.DiskTempC}</strong>°C</span>
                </div>
            </div>
        </div>

        <div class=""grid grid-1"">
            <div class=""card power-card"">
                <div class=""card-header""><h2>⚡ Consumo de Energia</h2><span class=""value power"" id=""power-total"">{W(s.Power.TotalWatts)} W</span></div>
                <div class=""power-breakdown"">
                    <span>CPU <strong id=""power-cpu"">{W(s.Power.CpuWatts)}</strong> W</span>
                    <span>GPU <strong id=""power-gpu"">{W(s.Power.GpuWatts)}</strong> W</span>
                    <span>RAM <strong id=""power-memory"">{W(s.Power.MemoryWatts)}</strong> W</span>
                    <span>Disco <strong id=""power-disk"">{W(s.Power.DiskWatts)}</strong> W</span>
                    <span>Placa-mãe <strong id=""power-mb"">{W(s.Power.MotherboardWatts)}</strong> W</span>
                    <span>Fans <strong id=""power-fans"">{W(s.Power.FansWatts)}</strong> W</span>
                    <span>Outros <strong id=""power-other"">{W(s.Power.PeripheralsWatts)}</strong> W</span>
                </div>
                <div class=""card-footer"">
                    <span>Nesta sessão: <strong id=""power-session"">{Kwh(s.Power.SessionKwh)}</strong> kWh</span>
                    <span>Total com o PC ligado: <strong id=""power-total-energy"" title=""Soma de todo o consumo medido enquanto o PC esteve ligado"">{Kwh(s.Power.TotalKwh)}</strong> kWh</span>
                    <span>Na tomada: ~<strong id=""power-wall"">{W(s.Power.WallWatts)}</strong> W</span>
                </div>
            </div>
        </div>

        <div class=""grid grid-1"">
            <div class=""card"">
                <div class=""card-header""><h2>Placa-Mãe (Múltiplos Sensores)</h2><span class=""value"" id=""mb-system-temp"">{s.MbSystemTempC}°C</span></div>
                <div class=""card-footer"">
                    <span>Sistema: <strong id=""mb-system-temp"">{s.MbSystemTempC}</strong>°C</span>
                    <span>VRM: <strong id=""mb-vrm-temp"">{s.MbVrmTempC}</strong>°C</span>
                    <span>PCH Hub: <strong id=""mb-pch-temp"">{s.MbPchTempC}</strong>°C</span>
                    <span>Soquete: <strong id=""mb-aux-temp"">{s.MbAuxTempC}</strong>°C</span>
                </div>
            </div>
        </div>

        <div class=""grid grid-1"">
            <div class=""card ip-card"">
                <div class=""card-header""><h2>Acesso pela rede</h2><span class=""value ip-val"" id=""local-ip"">http://{s.LocalIp}:5000</span></div>
            </div>
        </div>
    </main>
    <script src=""/js/app.js""></script>
</body>
</html>";
        }
    }
}
