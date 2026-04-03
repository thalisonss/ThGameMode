using System;
using System.Windows.Forms;

namespace ThGameMode
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool startMinimized = args.Any(arg =>
                string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase));

            // Rodamos o frmConfig como app de tray
            Application.Run(new Screens.frmConfig(startMinimized));
        }
    }
}
