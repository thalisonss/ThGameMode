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

        <div class=""grid grid-2"">
            <div class=""card"">
                <div class=""card-header""><h2>Placa-Mãe (Motherboard)</h2><span class=""value"" id=""mb-temp"">{s.MotherboardTempC}°C</span></div>
                <div class=""card-footer""><span>Sensor térmico principal</span></div>
            </div>
            <div class=""card uptime-card"">
                <div class=""card-header""><h2>Uptime</h2><span class=""value"" id=""uptime"">{s.Uptime}</span></div>
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
