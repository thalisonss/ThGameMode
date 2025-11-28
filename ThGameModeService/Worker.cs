using System.Diagnostics;
using System.ServiceProcess;
using System.Text.Json;

namespace ThGameModeService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private Configuracao _config;
        private bool _modoAltoAtivo = false;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            var configPath = Path.Combine(AppContext.BaseDirectory, "ThGameModeConfig.json");

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    _config = JsonSerializer.Deserialize<Configuracao>(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao ler o arquivo de configuração");
                }
            }

            _config ??= new Configuracao();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    bool servicoRodando = false;

                    foreach (var servico in _config.ListServices)
                    {
                        if (string.IsNullOrWhiteSpace(servico)) continue;

                        try
                        {
                            using var sc = new ServiceController(servico);
                            if (sc.Status == ServiceControllerStatus.Running)
                            {
                                servicoRodando = true;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Serviço '{servico}' não encontrado ou inacessível.");
                        }
                    }

                    if (servicoRodando && !_modoAltoAtivo)
                    {
                        TrocarPlano(_config.PowerPlanOpenApp);
                        _modoAltoAtivo = true;
                        _logger.LogInformation("Serviço detectado rodando ? Alto desempenho");
                    }
                    else if (!servicoRodando && _modoAltoAtivo)
                    {
                        TrocarPlano(_config.PowerPlanClosedApp);
                        _modoAltoAtivo = false;
                        _logger.LogInformation("Nenhum serviço da lista rodando ? Economia de energia");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro inesperado no loop principal");
                }

                await Task.Delay(_config.CheckInterval * 1000, stoppingToken);
            }
        }

        private void TrocarPlano(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid)) return;

            try
            {
                Process.Start(new ProcessStartInfo("powercfg", $"/setactive {guid}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao trocar o plano de energia");
            }
        }
    }
}
