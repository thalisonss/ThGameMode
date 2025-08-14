using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Text.Json;
using System.Diagnostics;

namespace ThGameMode.Screens
{
    public partial class frmConfig : Form
    {
        public frmConfig()
        {
            InitializeComponent();
        }

        #region | Variables |
        static string nameService = "ThGameModeService";
        string langCode;
        #endregion

        private bool CheckInstallation()
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == nameService);
        }

        private void DisableEnableControls(bool disableEnabled)
        {
            nudCheckInterval.Enabled = disableEnabled;
            cboPowerPlanOpenApp.Enabled = disableEnabled;
            cboPowerPlanClosedApp.Enabled = disableEnabled;
            txtSearchService.Enabled = disableEnabled;
            dgvListServices.Enabled = disableEnabled;
            txtSearchServiceAdded.Enabled = disableEnabled;
            dgvListServicesAdded.Enabled = disableEnabled;
            btnRefresh.Enabled = disableEnabled;
            btnSave.Enabled = disableEnabled;


        }



        private void LoadConfigurations()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "ThGameModeConfig.json");

            if (!File.Exists(path))
                return;

            try
            {
                string json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<AppConfig>(json);

                if (config == null)
                    return;

                nudCheckInterval.Value = config.CheckInterval;

                cboPowerPlanOpenApp.SelectedValue = config.PowerPlanOpenApp;
                cboPowerPlanClosedApp.SelectedValue = config.PowerPlanClosedApp;

                _servicesAdded = config.ListServices ?? new List<string>();
                AtualizarGridServicesAdded();


            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar configurações: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AtualizarGridServicesAdded()
        {
            string filtro = txtSearchServiceAdded.Text.Trim().ToLower();

            dgvListServicesAdded.Rows.Clear();

            foreach (var servico in _servicesAdded)
            {
                if (servico.ToLower().Contains(filtro))
                {
                    dgvListServicesAdded.Rows.Add(servico);
                }
            }
        }


        private void SaveSettings()
        {
            var config = new
            {
                CheckInterval = (int)nudCheckInterval.Value,
                PowerPlanOpenApp = cboPowerPlanOpenApp.SelectedValue?.ToString(),
                PowerPlanClosedApp = cboPowerPlanClosedApp.SelectedValue?.ToString(),
                ListServices = dgvListServicesAdded.Rows
                    .Cast<DataGridViewRow>()
                    .Where(r => r.Cells["dgvColumnNameAdded"].Value != null)
                    .Select(r => r.Cells["dgvColumnNameAdded"].Value.ToString())
                    .ToList()
            };

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

            var path = Path.Combine(AppContext.BaseDirectory, "ThGameModeConfig.json");
            File.WriteAllText(path, json);

            MessageBox.Show("Configuração salva com sucesso!", "Salvar", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

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

        private void LoadServicesRunning()
        {
            dgvListServices.Rows.Clear();

            _servicesRunning = ServiceController.GetServices()
                .Where(s => s.Status == ServiceControllerStatus.Running)
                .Select(s => s.ServiceName)
                .OrderBy(s => s)
                .ToList();

            foreach (var servico in _servicesRunning)
            {
                if (!_servicesAdded.Contains(servico, StringComparer.OrdinalIgnoreCase))
                {
                    dgvListServices.Rows.Add(servico);
                }
            }
        }




        private List<string> _servicesRunning = new();
        private List<string> _servicesAdded = new List<string>();





        private void frmConfig_Load(object sender, EventArgs e)
        {
            LoadServicesRunning();

            var planos = GetEnergyPlans();

            cboPowerPlanOpenApp.DataSource = planos.ToList();
            cboPowerPlanOpenApp.DisplayMember = "Name";
            cboPowerPlanOpenApp.ValueMember = "Guid";

            cboPowerPlanClosedApp.DataSource = planos.ToList();
            cboPowerPlanClosedApp.DisplayMember = "Name";
            cboPowerPlanClosedApp.ValueMember = "Guid";

            LoadConfigurations();

            AtualizaRefresh();

            if (CheckInstallation())
            {
                btnInstallUninstall.Text = "Uninstall";
            }
            else
            {
                DisableEnableControls(false);

                if (DialogResult.Yes == MessageBox.Show(
    "Do you want to install the service now?",
    "Install Service",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question))
                {
                    try
                    {
                        string installerPath = Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            "Un_InstallThGameMode",
                            "Un_InstallThGameMode.exe");

                        if (File.Exists(installerPath))
                        {
                            Process.Start(installerPath);
                            Application.Exit(); 
                        }
                        else
                        {
                            MessageBox.Show("O instalador não foi encontrado: " + installerPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erro ao abrir instalador: " + ex.Message);
                    }
                }
                else
                {
                    btnInstallUninstall.Text = "Install";
                }
            }
        }




        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void txtSearchService_TextChanged(object sender, EventArgs e)
        {
            string filtro = txtSearchService.Text.Trim().ToLower();

            var adicionados = new HashSet<string>(
                dgvListServicesAdded.Rows
                    .Cast<DataGridViewRow>()
                    .Where(r => r.Cells["dgvColumnNameAdded"].Value != null)
                    .Select(r => r.Cells["dgvColumnNameAdded"].Value.ToString())
                    , StringComparer.OrdinalIgnoreCase);

            dgvListServices.Rows.Clear();

            foreach (var servico in _servicesRunning)
            {
                if (servico.ToLower().Contains(filtro) && !adicionados.Contains(servico))
                {
                    dgvListServices.Rows.Add(servico);
                }
            }
        }

        private void dgvListServices_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 1)
            {
                DataGridViewRow clickedRow = dgvListServices.Rows[e.RowIndex];
                string serviceName = clickedRow.Cells[0].Value?.ToString() ?? "";

                bool jaExiste = false;
                foreach (DataGridViewRow row in dgvListServicesAdded.Rows)
                {
                    if (row.Cells.Count > 1 && row.Cells[1].Value?.ToString() == serviceName)
                    {
                        jaExiste = true;
                        break;
                    }
                }

                if (!jaExiste)
                {
                    _servicesAdded.Add(serviceName);
                    AtualizarGridServicesAdded();


                    dgvListServices.Rows.RemoveAt(e.RowIndex);
                }
                else
                {
                    MessageBox.Show("Esse serviço já foi adicionado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void dgvListServicesAdded_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 1)
            {
                DataGridViewRow clickedRow = dgvListServicesAdded.Rows[e.RowIndex];
                string serviceName = clickedRow.Cells[0].Value?.ToString() ?? "";

                _servicesAdded.Remove(serviceName);
                AtualizarGridServicesAdded();


                bool jaExiste = false;
                foreach (DataGridViewRow row in dgvListServices.Rows)
                {
                    if (row.Cells.Count > 1 && row.Cells[1].Value?.ToString() == serviceName)
                    {
                        jaExiste = true;
                        break;
                    }
                }

                if (!jaExiste)
                {
                    dgvListServices.Rows.Add(serviceName);
                }

                txtSearchService_TextChanged(null, null);
            }
        }

        private void txtSearchServiceAdded_TextChanged(object sender, EventArgs e)
        {
            AtualizarGridServicesAdded();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            AtualizaRefresh();
        }

        private void AtualizaRefresh()
        {
            dgvListServices.Rows.Clear();

            var adicionados = new HashSet<string>(
                dgvListServicesAdded.Rows
                    .Cast<DataGridViewRow>()
                    .Where(r => r.Cells["dgvColumnNameAdded"].Value != null)
                    .Select(r => r.Cells["dgvColumnNameAdded"].Value.ToString()),
                StringComparer.OrdinalIgnoreCase
            );

            var servicosRodando = ServiceController.GetServices()
                .Where(s => s.Status == ServiceControllerStatus.Running)
                .Select(s => s.ServiceName)
                .OrderBy(n => n);

            foreach (var nome in servicosRodando)
            {
                if (!adicionados.Contains(nome))
                {
                    dgvListServices.Rows.Add(nome);
                }
            }

            _servicesRunning = servicosRodando.ToList();
        }

        private void btnInstallUninstall_Click(object sender, EventArgs e)
        {

        }

        //private void btnInstallUninstall_Click(object sender, EventArgs e)
        //{
        //    frmInstall frmInstall = new frmInstall();
        //    frmInstall.Show();
        //    Hide();
        //}
    }
}
