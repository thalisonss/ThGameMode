using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThGameMode.Utils
{
    /// <summary>
    /// Gerencia relatórios diários e histórico de uso.
    /// </summary>
    public static class DailyReport
    {
        private const string REPORTS_FOLDER = "relatorios_diarios";
        private static readonly string ReportsPath = Path.Combine(AppContext.BaseDirectory, REPORTS_FOLDER);

        /// <summary>
        /// Registra uma troca de plano para o relatório.
        /// </summary>
        public static void RegistrarTroca(bool altoDesempenho, TimeSpan tempoNoModo, string? jogoAtivo = null)
        {
            try
            {
                Directory.CreateDirectory(ReportsPath);

                var date = DateTime.Today;
                var file = Path.Combine(ReportsPath, $"relatorio_{date:yyyyMMdd}.json");

                var report = CarregarRelatorio(date);
                report.Data.Add(date);

                if (altoDesempenho)
                {
                    report.TempoAltoDesempenho += tempoNoModo;
                }
                else
                {
                    report.TempoEconomia += tempoNoModo;
                }

                if (!string.IsNullOrWhiteSpace(jogoAtivo))
                {
                    if (!report.Jogos.ContainsKey(jogoAtivo))
                        report.Jogos[jogoAtivo] = TimeSpan.Zero;
                    report.Jogos[jogoAtivo] += tempoNoModo;
                }

                SalvarRelatorio(date, report);
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Warning, $"Erro ao registrar troca: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra abertura de app.
        /// </summary>
        public static void RegistrarAppAberto(string appName)
        {
            var date = DateTime.Today;
            var report = CarregarRelatorio(date);
            
            if (!report.AppsAbertos.Contains(appName))
            {
                report.AppsAbertos.Add(appName);
                SalvarRelatorio(date, report);
            }
        }

        /// <summary>
        /// Registra fechamento de app.
        /// </summary>
        public static void RegistrarAppFechado(string appName)
        {
            var date = DateTime.Today;
            var report = CarregarRelatorio(date);
            
            if (!report.AppsFechados.Contains(appName))
            {
                report.AppsFechados.Add(appName);
                SalvarRelatorio(date, report);
            }
        }

        /// <summary>
        /// Obtém o relatório de um dia específico.
        /// </summary>
        public static DailyReportData? ObterRelatorio(DateTime data)
        {
            return CarregarRelatorio(data);
        }

        /// <summary>
        /// Obtém relatórios dos últimos N dias.
        /// </summary>
        public static List<DailyReportData> ObterRelatorios(int ultimosDias = 7)
        {
            var relatorios = new List<DailyReportData>();

            for (int i = 0; i < ultimosDias; i++)
            {
                var data = DateTime.Today.AddDays(-i);
                var report = CarregarRelatorio(data);
                if (report.Data.Contains(data))
                {
                    relatorios.Add(report);
                }
            }

            return relatorios;
        }

        /// <summary>
        /// Carrega o relatório do disco.
        /// </summary>
        private static DailyReportData CarregarRelatorio(DateTime data)
        {
            var file = Path.Combine(ReportsPath, $"relatorio_{data:yyyyMMdd}.json");
            
            if (File.Exists(file))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    return JsonSerializer.Deserialize<DailyReportData>(json) ?? CriarRelatorioVazio(data);
                }
                catch
                {
                    return CriarRelatorioVazio(data);
                }
            }

            return CriarRelatorioVazio(data);
        }

        /// <summary>
        /// Salva o relatório no disco.
        /// </summary>
        private static void SalvarRelatorio(DateTime data, DailyReportData report)
        {
            var file = Path.Combine(ReportsPath, $"relatorio_{data:yyyyMMdd}.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(report, options);
            File.WriteAllText(file, json);
        }

        /// <summary>
        /// Cria um relatório vazio para uma data específica.
        /// </summary>
        private static DailyReportData CriarRelatorioVazio(DateTime data)
        {
            return new DailyReportData
            {
                Data = new List<DateTime> { data },
                TempoAltoDesempenho = TimeSpan.Zero,
                TempoEconomia = TimeSpan.Zero,
                Jogos = new Dictionary<string, TimeSpan>(),
                AppsAbertos = new List<string>(),
                AppsFechados = new List<string>(),
                DataSalvo = DateTime.Now
            };
        }

        /// <summary>
        /// Gera um resumo semanal formatado.
        /// </summary>
        public static string GerarResumoSemanal()
        {
            var relatorios = ObterRelatorios(7);
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("📅 RESUMO SEMANAL");
            sb.AppendLine();

            TimeSpan totalAlto = TimeSpan.Zero;
            TimeSpan totalEconomia = TimeSpan.Zero;

            foreach (var r in relatorios)
            {
                totalAlto += r.TempoAltoDesempenho;
                totalEconomia += r.TempoEconomia;

                var dateStr = r.Data.First().ToString("dd/MM");
                sb.AppendLine($"{dateStr}:");
                sb.AppendLine($"  ⚡ Alto: {Formatar(r.TempoAltoDesempenho)}, 🍃 Economia: {Formatar(r.TempoEconomia)}");
            }

            sb.AppendLine();
            sb.AppendLine($"💡 TOTAL: {Formatar(totalAlto)} em alto desempenho | {Formatar(totalEconomia)} em economia");

            var totalJogos = new Dictionary<string, TimeSpan>();
            foreach (var r in relatorios)
            {
                foreach (var jogo in r.Jogos)
                {
                    if (!totalJogos.ContainsKey(jogo.Key))
                        totalJogos[jogo.Key] = TimeSpan.Zero;
                    totalJogos[jogo.Key] += jogo.Value;
                }
            }

            if (totalJogos.Any())
            {
                sb.AppendLine();
                sb.AppendLine("🎮 TOP JOGOS DA SEMANA:");
                foreach (var jogo in totalJogos.OrderByDescending(j => j.Value).Take(5))
                {
                    sb.AppendLine($"  {jogo.Key}: {Formatar(jogo.Value)}");
                }
            }

            return sb.ToString();
        }

        private static string Formatar(TimeSpan t)
        {
            if (t.TotalHours >= 1) return $"{t.Hours}h {t.Minutes}m";
            if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds}s";
            return $"{t.Seconds}s";
        }

        /// <summary>
        /// Dados do relatório diário.
        /// </summary>
        public class DailyReportData
        {
            [JsonPropertyName("data")]
            public List<DateTime> Data { get; set; } = new();

            [JsonPropertyName("tempo_alto_desempenho")]
            public TimeSpan TempoAltoDesempenho { get; set; }

            [JsonPropertyName("tempo_economia")]
            public TimeSpan TempoEconomia { get; set; }

            [JsonPropertyName("jogos")]
            public Dictionary<string, TimeSpan> Jogos { get; set; } = new();

            [JsonPropertyName("apps_abertos")]
            public List<string> AppsAbertos { get; set; } = new();

            [JsonPropertyName("apps_fechados")]
            public List<string> AppsFechados { get; set; } = new();

            [JsonPropertyName("data_salvo")]
            public DateTime DataSalvo { get; set; } = DateTime.Now;
        }
    }
}