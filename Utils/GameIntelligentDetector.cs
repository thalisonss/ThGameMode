using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace ThGameMode.Utils
{
    /// <summary>
    /// Gerencia a detecção automática de jogos e perfis predefinidos.
    /// </summary>
    public static class GameIntelligentDetector
    {
        private const string GAMES_CONFIG_NAME = "jogos_inteligentes.json";
        private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "config", GAMES_CONFIG_NAME);
        private static Dictionary<string, GameProfile> _gameProfiles = new();

        /// <summary>
        /// Perfil de jogo ou aplicativo predefinido.
        /// </summary>
        public class GameProfile
        {
            [JsonPropertyName("nome")]
            public string Nome { get; set; } = string.Empty;

            [JsonPropertyName("processos")]
            public List<string> Processos { get; set; } = new();

            [JsonPropertyName("modo")]
            public string Modo { get; set; } = "alto_desempenho"; // "alto_desempenho", "economia"

            [JsonPropertyName("aplicacoes_a_fechar")]
            public List<string> AplicacoesAFechar { get; set; } = new();

            [JsonPropertyName("aplicacoes_a_abrir")]
            public List<string> AplicacoesAAbrir { get; set; } = new();

            [JsonPropertyName("power_plan_alto")]
            public string? PowerPlanAlto { get; set; }

            [JsonPropertyName("power_plan_baixo")]
            public string? PowerPlanBaixo { get; set; }

            [JsonPropertyName("prioridade_processo")]
            public ProcessPriorityClass? PrioridadeProcesso { get; set; }

            [JsonPropertyName("detalhes")]
            public Dictionary<string, string> Detalhes { get; set; } = new();
        }

        static GameIntelligentDetector()
        {
            CarregarConfiguracao();
        }

        /// <summary>
        /// Carrega a configuração de jogos do arquivo.
        /// </summary>
        private static void CarregarConfiguracao()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, GameProfile>>(json);
                    if (data != null)
                        _gameProfiles = data;
                }
                else
                {
                    // Configuração padrão com jogos comuns
                    CriarConfiguracaoPadrao();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Warning, $"Erro ao carregar config de jogos: {ex.Message}");
                _gameProfiles = new Dictionary<string, GameProfile>();
            }
        }

        /// <summary>
        /// Cria configuração padrão com jogos populares.
        /// </summary>
        private static void CriarConfiguracaoPadrao()
        {
            _gameProfiles = new Dictionary<string, GameProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["Elden Ring"] = new GameProfile
                {
                    Nome = "Elden Ring",
                    Processos = new List<string> { "eldenring.exe", "eldenring" },
                    Modo = "alto_desempenho",
                    AplicacoesAFechar = new List<string> { "discord.exe", "steam.exe", "battle.net.exe" },
                    PrioridadeProcesso = ProcessPriorityClass.High
                },
                ["Valorant"] = new GameProfile
                {
                    Nome = "Valorant",
                    Processos = new List<string> { "VALORANT.exe", "RiotClientServices.exe" },
                    Modo = "alto_desempenho",
                    AplicacoesAFechar = new List<string> { "discord.exe", "steam.exe" },
                    PrioridadeProcesso = ProcessPriorityClass.High
                },
                ["Minecraft"] = new GameProfile
                {
                    Nome = "Minecraft",
                    Processos = new List<string> { "javaw.exe", " minecraft " },
                    Modo = "alto_desempenho",
                    PowerPlanAlto = "High performance"
                },
                ["League of Legends"] = new GameProfile
                {
                    Nome = "League of Legends",
                    Processos = new List<string> { "LeagueClient.exe", "LeagueClientUX.exe" },
                    Modo = "alto_desempenho",
                    AplicacoesAFechar = new List<string> { "discord.exe" },
                    PowerPlanAlto = "High performance"
                },
                ["CS:GO"] = new GameProfile
                {
                    Nome = "CS:GO",
                    Processos = new List<string> { "csgo.exe", "hl2.exe" },
                    Modo = "alto_desempenho",
                    AplicacoesAFechar = new List<string> { "discord.exe" },
                    PrioridadeProcesso = ProcessPriorityClass.High
                }
            };

            SalvarConfiguracao();
        }

        /// <summary>
        /// Salva a configuração no arquivo.
        /// </summary>
        public static void SalvarConfiguracao()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_gameProfiles, options);
                File.WriteAllText(ConfigPath, json);

                AppLogger.Write(AppLogger.LogLevel.Info, "[GameDetector] Configuracao salva");
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Error, $"Erro ao salvar config: {ex.Message}");
            }
        }

        /// <summary>
        /// Detecta qual jogo está rodando.
        /// </summary>
        public static GameProfile? DetectarJogoAtivo()
        {
            try
            {
                var processos = Process.GetProcesses()
                    .Where(p => !string.IsNullOrEmpty(p.ProcessName))
                    .Select(p => p.ProcessName.ToLowerInvariant());

                foreach (var kvp in _gameProfiles)
                {
                    var profile = kvp.Value;
                    if (profile.Processos.Any(proc =>
                        processos.Any(p => p.Contains(proc.ToLowerInvariant()))))
                    {
                        return profile;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Warning, $"Erro ao detectar jogo: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Verifica se um jogo personalizado está na lista.
        /// </summary>
        public static void AdicionarJogoPersonalizado(string nome, string processo, string modo = "alto_desempenho")
        {
            _gameProfiles[nome] = new GameProfile
            {
                Nome = nome,
                Processos = new List<string> { processo },
                Modo = modo
            };

            SalvarConfiguracao();
            AppLogger.Write(AppLogger.LogLevel.Info, $"[GameDetector] Jogo '{nome}' adicionado");
        }

        /// <summary>
        /// Remove um jogo da lista.
        /// </summary>
        public static bool RemoverJogo(string nome)
        {
            if (_gameProfiles.ContainsKey(nome))
            {
                _gameProfiles.Remove(nome);
                SalvarConfiguracao();
                AppLogger.Write(AppLogger.LogLevel.Info, $"[GameDetector] Jogo '{nome}' removido");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Obtém todos os perfis de jogo configurados.
        /// </summary>
        public static Dictionary<string, GameProfile> ObterPerfis()
        {
            return new Dictionary<string, GameProfile>(_gameProfiles, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Limpa a lista de jogos (restaura padrão).
        /// </summary>
        public static void LimparEFornecerPadrao()
        {
            _gameProfiles.Clear();
            CriarConfiguracaoPadrao();
            SalvarConfiguracao();
        }
    }
}