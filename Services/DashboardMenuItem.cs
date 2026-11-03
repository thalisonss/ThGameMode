using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThGameMode.Services
{
    public static class DashboardMenuItemManager
    {
        public static void AdicionarItensMenu(ContextMenuStrip contextMenu)
        {
            var separador = new ToolStripSeparator();
            var menuItemLigar = new ToolStripMenuItem("🖥️ Abrir Dashboard");
            var menuItemDesligar = new ToolStripMenuItem("🛑 Fechar Dashboard");

            menuItemLigar.Click += async (s, e) => await LigarDashboardAsync();
            menuItemDesligar.Click += (s, e) => DesligarDashboard();

            contextMenu.Items.AddRange(new ToolStripItem[] { separador, menuItemLigar, menuItemDesligar });
        }

        private static async Task LigarDashboardAsync()
        {
            try
            {
                var processes = Process.GetProcessesByName("Dashboard");
                if (processes.Length > 0)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "http://localhost:5000",
                        UseShellExecute = true
                    });
                    return;
                }

                var path = @"C:\Thalison\Studies\Personal\Projects\ThGameMode\Dashboard\Dashboard.exe";
                if (System.IO.File.Exists(path))
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = path,
                            WorkingDirectory = System.IO.Path.GetDirectoryName(path),
                            UseShellExecute = true
                        }
                    };
                    process.Start();
                    MessageBox.Show("Dashboard iniciado!\n\nAcesse: http://localhost:5000", 
                        "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Dashboard não encontrado em:\n{path}", 
                        "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void DesligarDashboard()
        {
            try
            {
                foreach (var proc in Process.GetProcessesByName("Dashboard"))
                {
                    proc.Kill();
                }
                MessageBox.Show("Dashboard fechado!", "Sucesso", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static bool DashboardEstaAtivo()
        {
            return Process.GetProcessesByName("Dashboard").Length > 0;
        }

        public static void AbrirDashboard()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "http://localhost:5000",
                UseShellExecute = true
            });
        }
    }
}