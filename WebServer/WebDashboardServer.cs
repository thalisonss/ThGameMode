using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ThGameMode.Utils;

namespace ThGameMode.WebServer
{
    /// <summary>
    /// Servidor web embutido usando HttpListener (.NET nativo, sem dependência de Kestrel/ASP.NET).
    /// Expõe o dashboard em http://0.0.0.0:5000 — acessível por IP na rede local.
    /// Conversão do pc-dashboard Python (Flask/Waitress) para C#.
    /// </summary>
    public sealed class WebDashboardServer : IDisposable
    {
        private readonly SystemMonitorService _monitor;
        private HttpListener? _listener;
        private Thread? _serverThread;
        private CancellationTokenSource _cts = new();

        // Pasta dos arquivos estáticos (css/js) relativa ao executável
        private static readonly string _wwwRoot =
            Path.Combine(AppContext.BaseDirectory, "wwwroot");

        public bool IsRunning { get; private set; }

        /// <summary>IP local para mostrar ao usuário no tooltip / menu.</summary>
        public string LocalIp => _monitor.GetCurrentStats().LocalIp;

        public WebDashboardServer()
        {
            _monitor = new SystemMonitorService();
        }

        /// <summary>Inicia o servidor em thread dedicada sem bloquear o UI.</summary>
        public void Start()
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();

            // Garante o urlacl para escutar em todas as interfaces sem precisar de admin toda vez
            EnsureUrlAcl();

            _listener = new HttpListener();
            // http://*:5000/ aceita qualquer Host header (IP, hostname, localhost)
            // sem rejeitar com HTTP 400 "Invalid Hostname" como faz o prefixo http://+:5000/
            _listener.Prefixes.Add("http://*:5000/");
            try { _listener.Start(); }
            catch (HttpListenerException ex)
            {
                // Fallback: só localhost (urlacl não registrado ou porta em uso)
                AppLogger.Write(AppLogger.LogLevel.Warning,
                    $"Falha ao ouvir em *:5000 ({ex.Message}). Modo só localhost ativo.");
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://localhost:5000/");
                _listener.Start();
            }

            _serverThread = new Thread(RunLoop)
            {
                IsBackground = true,
                Name = "ThGameMode-WebDashboard"
            };
            _serverThread.Start();
            IsRunning = true;

            AppLogger.Write(AppLogger.LogLevel.Info,
                $"Servidor Dashboard iniciado — http://{LocalIp}:5000");
        }

        /// <summary>
        /// Registra http://*:5000/ no urlacl do Windows com elevação de privilégio, se ainda não estiver registrado.
        /// Necessário para o HttpListener escutar em todas as interfaces sem rodar como Administrador.
        /// </summary>
        private static void EnsureUrlAcl()
        {
            try
            {
                // Verifica se já existe
                var check = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "http show urlacl url=http://*:5000/",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                var output = check!.StandardOutput.ReadToEnd();
                check.WaitForExit();

                if (output.Contains("*:5000")) return; // já registrado

                // Registra pedindo elevação (UAC)
                string user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                var reg = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"http add urlacl url=http://*:5000/ user=\"{user}\"",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true
                });
                reg?.WaitForExit(5000);

                AppLogger.Write(AppLogger.LogLevel.Info, "urlacl http://*:5000/ registrado com sucesso.");
            }
            catch (Exception ex)
            {
                // Se o usuário cancelar o UAC ou falhar, o servidor tenta assim mesmo
                AppLogger.Write(AppLogger.LogLevel.Warning, $"EnsureUrlAcl: {ex.Message}");
            }
        }

        /// <summary>Encerra o servidor graciosamente.</summary>
        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            _cts.Cancel();
            try { _listener?.Stop(); } catch { }
            AppLogger.Write(AppLogger.LogLevel.Info, "Servidor Dashboard encerrado.");
        }

        private void RunLoop()
        {
            while (!_cts.IsCancellationRequested && (_listener?.IsListening ?? false))
            {
                try
                {
                    var ctx = _listener!.GetContext();           // bloqueante
                    Task.Run(() => HandleRequest(ctx), _cts.Token);   // processa em paralelo
                }
                catch (HttpListenerException) { break; }        // Stop() foi chamado
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    AppLogger.Write(AppLogger.LogLevel.Warning,
                        $"Erro no loop do Dashboard: {ex.Message}");
                }
            }
        }

        private async Task HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                var req = ctx.Request;
                var res = ctx.Response;
                string path = req.Url?.AbsolutePath ?? "/";

                // ── GET /api/stats → JSON ────────────────────────────────
                if (path == "/api/stats")
                {
                    var s = _monitor.GetCurrentStats();
                    var json = JsonSerializer.Serialize(new
                    {
                        timestamp = s.Timestamp,
                        uptime = s.Uptime,
                        local_ip = s.LocalIp,
                        cpu = new
                        {
                            percent = s.CpuPercent,
                            count = s.CpuCount,
                            freq_current = s.CpuFreqGhz * 1000,   // MHz (compatível com app.js)
                            temperature = s.CpuTempC
                        },
                        memory = new
                        {
                            percent = s.MemPercent,
                            used = s.MemUsedGb,
                            total = s.MemTotalGb,
                            temperature = s.MemTempC
                        },
                        disk = new
                        {
                            percent = s.DiskPercent,
                            used = s.DiskUsedGb,
                            free = s.DiskFreeGb,
                            total = s.DiskTotalGb,
                            temperature = s.DiskTempC
                        },
                        gpu = new
                        {
                            name = s.GpuName,
                            percent = s.GpuPercent,
                            temperature = s.GpuTempC
                        },
                        motherboard = new
                        {
                            temperature = s.MotherboardTempC
                        },
                        weather = new
                        {
                            temp = int.TryParse(s.WeatherTemp, out var t) ? (object)t : s.WeatherTemp,
                            desc = s.WeatherDesc
                        }
                    });

                    await WriteResponse(res, json, "application/json");
                    return;
                }

                // ── GET / → HTML do dashboard ────────────────────────────
                if (path == "/" || path == "/index.html")
                {
                    var html = DashboardHtml.Render(_monitor.GetCurrentStats());
                    await WriteResponse(res, html, "text/html; charset=utf-8");
                    return;
                }

                // ── Arquivos estáticos (css, js, favicon) ─────────────────
                if (TryServeStaticFile(path, res))
                    return;

                // ── 404 ───────────────────────────────────────────────────
                res.StatusCode = 404;
                res.Close();
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Warning,
                    $"Erro ao responder requisição: {ex.Message}");
                try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { }
            }
        }

        private static async Task WriteResponse(HttpListenerResponse res,
            string body, string contentType)
        {
            res.ContentType = contentType;
            var bytes = Encoding.UTF8.GetBytes(body);
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes);
            res.OutputStream.Close();
        }

        private static bool TryServeStaticFile(string path, HttpListenerResponse res)
        {
            // Caminho seguro: evita path traversal
            string relative = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            string full = Path.GetFullPath(Path.Combine(_wwwRoot, relative));

            if (!full.StartsWith(_wwwRoot, StringComparison.OrdinalIgnoreCase))
                return false;   // tentativa de path traversal

            if (!File.Exists(full)) return false;

            string ext = Path.GetExtension(full).ToLowerInvariant();
            string mime = ext switch
            {
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".ico" => "image/x-icon",
                ".png" => "image/png",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };

            res.ContentType = mime;
            byte[] bytes = File.ReadAllBytes(full);
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes, 0, bytes.Length);
            res.OutputStream.Close();
            return true;
        }

        public void Dispose()
        {
            Stop();
            _monitor.Dispose();
            try { _listener?.Close(); } catch { }
            _cts.Dispose();
        }
    }
}
