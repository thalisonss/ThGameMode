using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThGameMode.Services
{
    public class DashboardToggleService
    {
        private const string DashboardExePath = @"C:\Thalison\Studies\Personal\Projects\ThGameMode\Dashboard\Dashboard.exe";
        private const string DashboardUrl = "http://localhost:5000";
        private Process? _dashboardProcess;

        public async Task<bool> LigarDashboard()
        {
            try
            {
                if (DashboardEstaAtivo())
                {
                    Process.Start(new ProcessStartInfo { FileName = DashboardUrl, UseShellExecute = true });
                    return true;
                }

                if (!System.IO.File.Exists(DashboardExePath))
                {
                    MessageBox.Show($"Dashboard não encontrado em:\n{DashboardExePath}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                _dashboardProcess = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = DashboardExePath,
                        WorkingDirectory = System.IO.Path.GetDirectoryName(DashboardExePath),
                        UseShellExecute = true
                    }
                };

                _dashboardProcess.Start();
                await Task.Delay(1500);

                return !_dashboardProcess.HasExited;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar Dashboard:\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public void DesligarDashboard()
        {
            try
            {
                foreach (var proc in Process.GetProcessesByName("Dashboard"))
                {
                    try { proc.Kill(); proc.WaitForExit(2000); }
                    catch { }
                }

                _dashboardProcess?.Kill();
                _dashboardProcess = null;
            }
            catch { }
        }

        public bool DashboardEstaAtivo()
        {
            return Process.GetProcessesByName("Dashboard").Length > 0;
        }

        public async Task ToggleDashboard()
        {
            if (DashboardEstaAtivo())
                DesligarDashboard();
            else
                await LigarDashboard();
        }
    }
}