using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThGameMode.Utils
{
    /// <summary>
    /// Gerencia notificações visuais modernas para o ThGameMode.
    /// Usa forms flutuantes desaparecendo automaticamente.
    /// </summary>
    public static class ToastNotifier
    {
        private static Form? _notificationForm;
        private static readonly object _lock = new object();

        /// <summary>
        /// Mostra notificação moderna (form flutuante desaparecendo automaticamente).
        /// </summary>
        public static async void Mostrar(string titulo, string mensagem, Color? corFundo = null, int duracaoMs = 4000)
        {
            lock (_lock)
            {
                try
                {
                    // Fecha notificação anterior se existir
                    if (_notificationForm?.IsDisposed == false)
                    {
                        _notificationForm.Close();
                    }

                    _notificationForm = new Form
                    {
                        FormBorderStyle = FormBorderStyle.None,
                        TopMost = true,
                        ShowInTaskbar = false,
                        BackColor = corFundo ?? Color.FromArgb(33, 150, 243),
                        Size = new Size(350, 85),
                        StartPosition = FormStartPosition.Manual
                    };

                    // Panel com cantos arredondados
                    var panel = new Panel
                    {
                        Dock = DockStyle.Fill,
                        BackColor = corFundo ?? Color.FromArgb(33, 150, 243)
                    };
                    panel.Location = new Point(0, 0);
                    panel.Size = new Size(350, 85);
                    panel.Region = CreateRoundedRegion(15);

                    var lblTitulo = new Label
                    {
                        Text = titulo,
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                        ForeColor = Color.White,
                        Dock = DockStyle.Top,
                        Height = 25,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Padding = new Padding(15, 5, 5, 0)
                    };

                    var lblMensagem = new Label
                    {
                        Text = mensagem,
                        Font = new Font("Segoe UI", 9F),
                        ForeColor = Color.White,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Padding = new Padding(15, 0, 15, 5)
                    };

                    panel.Controls.Add(lblMensagem);
                    panel.Controls.Add(lblTitulo);
                    _notificationForm.Controls.Add(panel);

                    // Posiciona no canto superior direito
                    var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
                    _notificationForm.Location = new Point(
                        screen.Width - panel.Width - 20,
                        20);

                    _notificationForm.Show();

                    // Fecha após a duração especificada
                    Task.Run(async () =>
                    {
                        await Task.Delay(duracaoMs);
                        if (_notificationForm != null && !_notificationForm.IsDisposed)
                        {
                            _notificationForm.Invoke(new Action(() => _notificationForm.Close()));
                        }
                    });
                }
                catch (Exception ex)
                {
                    AppLogger.Write(AppLogger.LogLevel.Warning, $"ToastNotifier: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Cria um GraphicsPath com cantos arredondados para um retângulo.
        /// </summary>
        private static Region? CreateRoundedRegion(int radius)
        {
            var path = new GraphicsPath();
            var rect = new Rectangle(0, 0, 350, 85);
            var r = radius;

            path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
            path.AddArc(rect.X + rect.Width - r * 2, rect.Y, r * 2, r * 2, 270, 90);
            path.AddArc(rect.X + rect.Width - r * 2, rect.Y + rect.Height - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();

            return new Region(path);
        }

        /// <summary>
        /// Mostra notificação de modo Alto Desempenho.
        /// </summary>
        public static void MostrarModoAltoDesempenho(string? jogoAtivo = null)
        {
            var titulo = "⚡ ThGameMode";
            var mensagem = jogoAtivo != null
                ? $"Modo Alto Desempenho ativado!\nJogo detectado: {jogoAtivo}"
                : "Modo Alto Desempenho ativado!";

            Mostrar(titulo, mensagem, Color.FromArgb(244, 67, 54));
        }

        /// <summary>
        /// Mostra notificação de modo Economia.
        /// </summary>
        public static void MostrarModoEconomia()
        {
            Mostrar("🍃 ThGameMode",
                "Modo Economia ativado!\nTodos os jogos foram desativados.",
                Color.FromArgb(76, 175, 80));
        }

        /// <summary>
        /// Mostra notificação de erro.
        /// </summary>
        public static void MostrarErro(string mensagem)
        {
            Mostrar("❌ ThGameMode - Erro", mensagem, Color.FromArgb(244, 67, 54));
        }

        /// <summary>
        /// Mostra notificação de atualização disponível.
        /// </summary>
        public static void MostrarAtualizacaoDisponivel(string versaoNova)
        {
            if (string.IsNullOrWhiteSpace(versaoNova))
                return;

            Mostrar("🎉 ThGameMode - Atualização",
                $"Nova versão disponível: {versaoNova}",
                Color.FromArgb(255, 193, 7));
        }

        /// <summary>
        /// Mostra notificação de backup criado.
        /// </summary>
        public static void MostrarBackupCriado()
        {
            Mostrar("💾 ThGameMode",
                "Backup de configuração criado com sucesso!",
                Color.FromArgb(33, 150, 243));
        }

        /// <summary>
        /// Mostra notificação de detecção ativada.
        /// </summary>
        public static void MostrarDeteccaoAtivada()
        {
            Mostrar("✅ ThGameMode",
                "Detecção ativada\nOs jogos serão monitorados automaticamente",
                Color.FromArgb(76, 175, 80));
        }

        /// <summary>
        /// Mostra notificação de detecção desativada.
        /// </summary>
        public static void MostrarDeteccaoDesativada()
        {
            Mostrar("⚠️ ThGameMode",
                "Deteção desativada\nO controle automático foi desligado",
                Color.FromArgb(255, 193, 7));
        }

        /// <summary>
        /// Fecha a notificação atual.
        /// </summary>
        public static void Fechar()
        {
            lock (_lock)
            {
                if (_notificationForm != null && !_notificationForm.IsDisposed)
                {
                    _notificationForm.Close();
                }
                _notificationForm = null;
            }
        }
    }
}