using System;
using System.Windows.Forms;

namespace ThGameMode
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Rodamos o frmConfig como app de tray
            Application.Run(new Screens.frmConfig());
        }
    }
}
