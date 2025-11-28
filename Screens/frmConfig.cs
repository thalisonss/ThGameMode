using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using System.ServiceProcess;
using Microsoft.Win32;

namespace ThGameMode.Screens
{
    public partial class frmConfig : Form
    {
        #region | Variáveis |
        static string nameService = "ThGameModeService";
        private List<string> _itemsAvailable = new();  // Serviços e programas disponíveis
        private List<string> _itemsAdded = new();      // Serviços/programas adicionados
        #endregion

        public frmConfig()
        {
            InitializeComponent();
        }

        #region | Classes Auxiliares |
        public class PowerPlan
        {
            public string Name { get; set; }
            public string Guid { get; set; }
        }

        public class AppConfig
        {
            public int CheckInterval { get; set; }
            public string PowerPlanOpenApp { get; set; }
            public string PowerPlanClosedApp { get; set; }
            public List<string> ListServices { get; set; } = new();
        }
        #endregion

        #region | Load Form |
        private void frmConfig_Load(object sender, EventArgs e)
        {
            // Carrega planos de energia
            var planos = GetEnergyPlans();
            cboPowerPlanOpenApp.DataSource = planos.ToList();
            cboPowerPlanOpenApp.DisplayMember = "Name";
            cboPowerPlanOpenApp.ValueMember = "Guid";

            cboPowerPlanClosedApp.DataSource = planos.ToList();
            cboPowerPlanClosedApp.DisplayMember = "Name";
            cboPowerPlanClosedApp.ValueMember = "Guid";

            // Carrega configurações do JSON, se existir
            LoadConfigurationsFromJson();

            // Carrega serviços e programas
            LoadAvailableItems();

            // Atualiza o grid
            AtualizaRefresh();
        }
        #endregion

        #region | Carregamento Config JSON |
        private void LoadConfigurationsFromJson()
        {
            try
            {
                string serviceFolder = Path.Combine(AppContext.BaseDirectory, "ThGameModeService");
                string path = Path.Combine(serviceFolder, "ThGameModeConfig.json");

                if (!File.Exists(path))
                    return; // Se não existe, não faz nada

                string json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<AppConfig>(json);

                if (config == null)
                    return;

                nudCheckInterval.Value = config.CheckInterval;
                cboPowerPlanOpenApp.SelectedValue = config.PowerPlanOpenApp;
                cboPowerPlanClosedApp.SelectedValue = config.PowerPlanClosedApp;
                _itemsAdded = config.ListServices ?? new List<string>();

                AtualizarGridServicesAdded();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar configurações: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region | Salvando Configurações |
        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();

            RestartService("ThGameModeService");

            MessageBox.Show("Configuração salva e serviço reiniciado com sucesso!", "Salvar", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SaveSettings()
        {
            try
            {
                var config = new AppConfig
                {
                    CheckInterval = (int)nudCheckInterval.Value,
                    PowerPlanOpenApp = cboPowerPlanOpenApp.SelectedValue?.ToString(),
                    PowerPlanClosedApp = cboPowerPlanClosedApp.SelectedValue?.ToString(),
                    ListServices = _itemsAdded.ToList()
                };

                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

                string rootFolder = AppContext.BaseDirectory;
                string serviceFolder = Path.Combine(rootFolder, "ThGameModeService");
                Directory.CreateDirectory(serviceFolder);

                string path = Path.Combine(serviceFolder, "ThGameModeConfig.json");
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar configuração: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }

        private void RestartService(string serviceName)
        {
            try
            {
                ServiceController sc = new ServiceController(serviceName);

                if (sc.Status != ServiceControllerStatus.Stopped &&
                    sc.Status != ServiceControllerStatus.StopPending)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                }

                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao reiniciar o serviço: " + ex.Message);
            }
        }

        #endregion

        #region | Atualiza Grid |
        private void AtualizarGridServicesAdded()
        {
            string filtro = txtSearchServiceAdded.Text.Trim().ToLower();
            dgvListServicesAdded.Rows.Clear();

            foreach (var item in _itemsAdded)
            {
                if (item.ToLower().Contains(filtro))
                    dgvListServicesAdded.Rows.Add(item);
            }
        }

        private void AtualizaRefresh()
        {
            dgvListServices.Rows.Clear();

            var adicionados = new HashSet<string>(_itemsAdded, StringComparer.OrdinalIgnoreCase);

            foreach (var item in _itemsAvailable)
            {
                if (!adicionados.Contains(item))
                {
                    dgvListServices.Rows.Add(item);
                }
            }
        }
        #endregion

        #region | Buscar / Filtrar |
        private void txtSearchService_TextChanged(object sender, EventArgs e)
        {
            string filtro = txtSearchService.Text.Trim().ToLower();
            dgvListServices.Rows.Clear();

            var adicionados = new HashSet<string>(_itemsAdded, StringComparer.OrdinalIgnoreCase);

            foreach (var item in _itemsAvailable)
            {
                if (!adicionados.Contains(item) && item.ToLower().Contains(filtro))
                {
                    dgvListServices.Rows.Add(item);
                }
            }
        }

        private void txtSearchServiceAdded_TextChanged(object sender, EventArgs e)
        {
            AtualizarGridServicesAdded();
        }
        #endregion

        #region | Click Grid |
        private void dgvListServices_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Coluna do botão de adicionar é a coluna 1 (ajuste conforme sua grid)
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;

            string item = dgvListServices.Rows[e.RowIndex].Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(item)) return;

            if (!_itemsAdded.Contains(item, StringComparer.OrdinalIgnoreCase))
            {
                _itemsAdded.Add(item);
                AtualizarGridServicesAdded();
                dgvListServices.Rows.RemoveAt(e.RowIndex);
            }
            else
            {
                MessageBox.Show("Esse item já foi adicionado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }



        private void dgvListServicesAdded_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return; // botão de remover

            string item = dgvListServicesAdded.Rows[e.RowIndex].Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(item)) return;

            _itemsAdded.Remove(item);
            AtualizarGridServicesAdded();

            // Atualiza a lista disponível (recarrega serviços + processos em execução)
            LoadAvailableItems();
            AtualizaRefresh();
        }

        #endregion

        #region | Carregar Serviços + Programas |
        private void LoadAvailableItems()
        {
            _itemsAvailable.Clear();

            // 1️⃣ Adiciona serviços em execução
            _itemsAvailable.AddRange(ServiceController.GetServices()
                .Where(s => s.Status == ServiceControllerStatus.Running)
                .Select(s => s.ServiceName));

            // 2️⃣ Adiciona programas em execução (processos)
            var processos = Process.GetProcesses()
                .Select(p =>
                {
                    try { return p.ProcessName; }
                    catch { return null; }
                })
                .Where(n => !string.IsNullOrEmpty(n));

            _itemsAvailable.AddRange(processos);

            // Remove duplicados e ordena
            _itemsAvailable = _itemsAvailable
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();
        }


        private List<string> GetInstalledPrograms()
        {
            var programas = new List<string>();
            string[] paths = new string[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var path in paths)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path))
                {
                    if (key == null) continue;
                    foreach (var subkeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkeyName))
                        {
                            var displayName = subkey?.GetValue("DisplayName") as string;
                            if (!string.IsNullOrEmpty(displayName))
                                programas.Add(displayName);
                        }
                    }
                }
            }

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                if (key != null)
                {
                    foreach (var subkeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkeyName))
                        {
                            var displayName = subkey?.GetValue("DisplayName") as string;
                            if (!string.IsNullOrEmpty(displayName))
                                programas.Add(displayName);
                        }
                    }
                }
            }

            return programas;
        }
        #endregion

        #region | Planos de Energia |
        private List<PowerPlan> GetEnergyPlans()
        {
            var planos = new List<PowerPlan>();

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powercfg",
                        Arguments = "/list",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var linhas = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string linha in linhas)
                {
                    if (linha.Contains("GUID"))
                    {
                        var partes = linha.Trim().Split(':');
                        if (partes.Length > 1)
                        {
                            var guidNome = partes[1].Trim().Split('(');
                            var guid = guidNome[0].Trim();
                            var nome = guidNome.Length > 1 ? guidNome[1].Replace(")", "").Trim() : guid;
                            planos.Add(new PowerPlan { Guid = guid, Name = nome });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao obter planos de energia: " + ex.Message);
            }

            return planos;
        }
        #endregion

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadAvailableItems(); // Recarrega serviços + processos em execução
            AtualizaRefresh();    // Atualiza o DataGridView de serviços em andamento 
        }

        private void btnInstallUninstall_Click(object sender, EventArgs e)
        {

        }
    }
}
