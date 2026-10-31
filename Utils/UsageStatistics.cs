using System;

namespace ThGameMode.Utils
{
    /// <summary>
    /// Rastreia estatísticas de uso do ThGameMode para exibição no dashboard.
    /// </summary>
    public class UsageStatistics
    {
        private DateTime _inicioSessao;
        private DateTime _inicioModoAtual;
        private TimeSpan _tempoTotalAltoDesempenho;
        private TimeSpan _tempoTotalEconomia;
        private int _contadorTrocasPlano;
        private int _contadorAppsAbertos;
        private int _contadorAppsFechados;
        private string _jogoMaisJogado = "Nenhum";
        private TimeSpan _tempoJogoMaisJogado;
        private readonly Dictionary<string, TimeSpan> _tempoPorJogo = new(StringComparer.OrdinalIgnoreCase);

        public DateTime InicioSessao => _inicioSessao;
        public TimeSpan TempoTotalAltoDesempenho => _tempoTotalAltoDesempenho;
        public TimeSpan TempoTotalEconomia => _tempoTotalEconomia;
        public int ContadorTrocasPlano => _contadorTrocasPlano;
        public int ContadorAppsAbertos => _contadorAppsAbertos;
        public int ContadorAppsFechados => _contadorAppsFechados;
        public string JogoMaisJogado => _jogoMaisJogado;
        public TimeSpan TempoJogoMaisJogado => _tempoJogoMaisJogado;

        public UsageStatistics()
        {
            _inicioSessao = DateTime.Now;
            _inicioModoAtual = DateTime.Now;
        }

        /// <summary>
        /// Registra troca de modo (economia <-> alto desempenho).
        /// </summary>
        public void RegistrarTrocaModo(bool altoDesempenho, string? jogoAtivo = null)
        {
            var tempoDecorrido = DateTime.Now - _inicioModoAtual;

            if (altoDesempenho)
            {
                _tempoTotalEconomia += tempoDecorrido;
            }
            else
            {
                _tempoTotalAltoDesempenho += tempoDecorrido;

                // Registra tempo do jogo
                if (!string.IsNullOrWhiteSpace(jogoAtivo))
                {
                    if (!_tempoPorJogo.ContainsKey(jogoAtivo))
                        _tempoPorJogo[jogoAtivo] = TimeSpan.Zero;

                    _tempoPorJogo[jogoAtivo] += tempoDecorrido;

                    // Atualiza jogo mais jogado
                    if (_tempoPorJogo[jogoAtivo] > _tempoJogoMaisJogado)
                    {
                        _jogoMaisJogado = jogoAtivo;
                        _tempoJogoMaisJogado = _tempoPorJogo[jogoAtivo];
                    }
                }
            }

            _contadorTrocasPlano++;
            _inicioModoAtual = DateTime.Now;

            AppLogger.Write(AppLogger.LogLevel.Info,
                $"[Stats] Troca #{_contadorTrocasPlano}: {(altoDesempenho ? "Alto" : "Economia")} | Jogo: {jogoAtivo ?? "N/A"}");
        }

        /// <summary>
        /// Registra aplicativo fechado.
        /// </summary>
        public void RegistrarAppFechado(string nomeApp)
        {
            _contadorAppsFechados++;
            AppLogger.Write(AppLogger.LogLevel.Info, $"[Stats] App fechado: {nomeApp} (Total: {_contadorAppsFechados})");
        }

        /// <summary>
        /// Registra aplicativo reaberto.
        /// </summary>
        public void RegistrarAppReaberto(string nomeApp)
        {
            _contadorAppsAbertos++;
            AppLogger.Write(AppLogger.LogLevel.Info, $"[Stats] App reaberto: {nomeApp} (Total: {_contadorAppsAbertos})");
        }

        /// <summary>
        /// Obtém o tempo total da sessão atual.
        /// </summary>
        public TimeSpan GetTempoSessao()
        {
            return DateTime.Now - _inicioSessao;
        }

        /// <summary>
        /// Obtém o tempo no modo atual.
        /// </summary>
        public TimeSpan GetTempoModoAtual()
        {
            return DateTime.Now - _inicioModoAtual;
        }

        /// <summary>
        /// Retorna os top 5 jogos mais jogados.
        /// </summary>
        public List<(string Jogo, TimeSpan Tempo)> GetTop5Jogos()
        {
            return _tempoPorJogo
                .OrderByDescending(x => x.Value)
                .Take(5)
                .Select(x => (x.Key, x.Value))
                .ToList();
        }

        /// <summary>
        /// Reseta as estatísticas (para um novo dia, por exemplo).
        /// </summary>
        public void Reset()
        {
            _inicioSessao = DateTime.Now;
            _inicioModoAtual = DateTime.Now;
            _tempoTotalAltoDesempenho = TimeSpan.Zero;
            _tempoTotalEconomia = TimeSpan.Zero;
            _contadorTrocasPlano = 0;
            _contadorAppsAbertos = 0;
            _contadorAppsFechados = 0;
            _jogoMaisJogado = "Nenhum";
            _tempoJogoMaisJogado = TimeSpan.Zero;
            _tempoPorJogo.Clear();

            AppLogger.Write(AppLogger.LogLevel.Info, "[Stats] Estatísticas resetadas");
        }

        /// <summary>
        /// Formata um TimeSpan para exibição amigável.
        /// </summary>
        public static string FormatarTempo(TimeSpan tempo)
        {
            if (tempo.TotalHours >= 1)
                return $"{(int)tempo.TotalHours}h {tempo.Minutes}m";
            if (tempo.TotalMinutes >= 1)
                return $"{(int)tempo.TotalMinutes}m {tempo.Seconds}s";
            return $"{tempo.Seconds}s";
        }
    }
}
