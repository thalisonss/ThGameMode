using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Un_InstallThGameMode.Screens
{
    public partial class frmInstall : Form
    {
        private const string ServiceName = "ThGameModeService";
        private const string ServiceExeName = "ThGameModeService.exe";

        public frmInstall()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtInstallPath.Text = fbd.SelectedPath;
                }
            }

        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            try
            {
                // Pasta onde estão os arquivos de instalação junto com a subpasta ThGameModeService
                string sourceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ThGameMode");
                string destDir = txtInstallPath.Text;

                if (!Directory.Exists(sourceDir))
                {
                    MessageBox.Show("Pasta ThGameMode não encontrada!");
                    return;
                }

                // Copia toda a pasta ThGameMode para o destino
                foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));
                }

                foreach (string filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
                {
                    string newPath = filePath.Replace(sourceDir, destDir);
                    File.Copy(filePath, newPath, true);
                }

                // Caminho do executável do serviço dentro da pasta copiada
                string exePath = Path.Combine(destDir, "ThGameModeService", "ThGameModeService.exe");

                if (!File.Exists(exePath))
                {
                    MessageBox.Show("Executável do serviço não encontrado em: " + exePath);
                    return;
                }

                // Instala o serviço
                var psi = new ProcessStartInfo("sc", $"create {ServiceName} binPath= \"{exePath}\" start= auto")
                {
                    Verb = "runas",
                    UseShellExecute = true
                };
                Process.Start(psi);

                MessageBox.Show("Serviço instalado com sucesso!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}");
            }
        }


        private void CopyDirectory(string sourceDir, string destDir)
        {
            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));
            }

            foreach (string filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                string newPath = filePath.Replace(sourceDir, destDir);
                File.Copy(filePath, newPath, true);
            }
        }




        private void RunCmd(string command)
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(psi))
            {
                process.WaitForExit();
            }
        }

        private void txtInstallPath_TextChanged(object sender, EventArgs e)
        {

        }

        private void frmInstall_Load(object sender, EventArgs e)
        {
            if (CheckInstallation())
            {
                btnInstall.Enabled = false;
                btnUninstall.Enabled = true;

                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{ServiceName}"))
                    {
                        if (key != null)
                        {
                            object imagePath = key.GetValue("ImagePath");
                            if (imagePath != null)
                            {
                                // Remove aspas e variáveis de ambiente (%...%)
                                string path = Environment.ExpandEnvironmentVariables(imagePath.ToString().Trim('"'));

                                // Apenas o diretório
                                string folderPath = Path.GetDirectoryName(path);

                                txtInstallPath.Text = folderPath;
                            }
                            else
                            {
                                txtInstallPath.Text = "Não encontrado";
                            }
                        }
                        else
                        {
                            txtInstallPath.Text = "Serviço não encontrado";
                        }
                    }
                }
                catch (Exception ex)
                {
                    txtInstallPath.Text = "Erro: " + ex.Message;
                }
            }
            else
            {
                btnInstall.Enabled = true;
                btnUninstall.Enabled = false;
            }
        }

        private bool CheckInstallation()
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == ServiceName);
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            try
            {
                string serviceName = ServiceName;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = $"stop \"{serviceName}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                }).WaitForExit();

                Process.Start(new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = $"delete \"{serviceName}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                }).WaitForExit();

                string serviceFolder = txtInstallPath.Text.Trim();

                if (!string.IsNullOrEmpty(serviceFolder) && Directory.Exists(serviceFolder))
                {
                    Directory.Delete(serviceFolder, true);
                }

                MessageBox.Show("Serviço desinstalado e pasta removida com sucesso!", "Sucesso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao desinstalar: " + ex.Message, "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
