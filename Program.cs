using System;
using System.Windows.Forms;

namespace ThGameMode
{
    /// <summary>
    /// Ponto de entrada da aplicação WinForms.
    /// </summary>
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Configura o ambiente visual antes de qualquer formulário ser criado.
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Permite que o app seja iniciado já minimizado quando vier do startup do Windows.
            bool startMinimized = args.Any(arg =>
                string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase));

            // O formulário principal também gerencia o ícone da bandeja e o monitor em segundo plano.
            Application.Run(new Screens.frmConfig(startMinimized));
        }
    }
}
