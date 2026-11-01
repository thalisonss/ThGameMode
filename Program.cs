using System;
using System.Windows.Forms;
using ThGameMode.WebServer;

namespace ThGameMode
{
    /// <summary>
    /// Ponto de entrada da aplicação.
    /// Sobe o servidor web do Dashboard na porta 5000 (acessível por IP na rede local)
    /// antes de iniciar o formulário principal WinForms.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Instância global do servidor web. Mantida aqui para evitar coleta pelo GC
        /// e para permitir desligamento limpo ao fechar o app.
        /// </summary>
        internal static WebDashboardServer? DashboardServer { get; private set; }

        [STAThread]
        static void Main(string[] args)
        {
            // Configura o ambiente visual antes de qualquer formulário ser criado.
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Permite que o app seja iniciado já minimizado quando vier do startup do Windows.
            bool startMinimized = args.Length > 0 &&
                Array.Exists(args, a => string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));

            // ── Inicia o servidor web do Dashboard ──────────────────────────
            // O servidor fica em http://0.0.0.0:5000, acessível por qualquer
            // dispositivo na mesma rede via http://<IP-do-PC>:5000
            DashboardServer = new WebDashboardServer();
            DashboardServer.Start();

            // Garante encerramento limpo do servidor ao fechar o app
            Application.ApplicationExit += (_, _) => DashboardServer?.Dispose();

            // O formulário principal gerencia o ícone de bandeja e o monitor de planos de energia.
            Application.Run(new Screens.frmConfig(startMinimized));
        }
    }
}
