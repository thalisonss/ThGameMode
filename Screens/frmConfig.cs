using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThGameMode.Screens
{
    public partial class frmConfig : Form
    {
        // Caminho do JSON na raiz do programa
        private readonly string _configPath = Path.Combine(AppContext.BaseDirectory, "ThGameModeConfig.json");

        // Itens disponíveis e adicionados
        private List<string> _itemsAvailable = new();
        private List<string> _itemsAdded = new();

        // Monitor background
        private CancellationTokenSource _ctsMonitor;
        private Task _monitorTask;
        private bool _monitorActive = false;
        private bool _modoAltoAtivo = false;

        // Tray
        private NotifyIcon _trayIcon;
        private Icon _iconEconomia;
        private Icon _iconAltoDesempenho;
        private Icon _iconPadrao;
        private ContextMenuStrip _trayMenu;
        private bool _quitRequested = false;

        // Config atual em memória
        private Configuracao _config = new();

        public frmConfig()
        {
            InitializeComponent();
            InitializeTray();
        }

        #region Form Load / Init
        private void frmConfig_Load(object sender, EventArgs e)
        {
            // Carrega planos de energia para os combo boxes
            var planos = GetEnergyPlans();
            cboPowerPlanOpenApp.DataSource = planos.ToList();
            cboPowerPlanOpenApp.DisplayMember = "Name";
            cboPowerPlanOpenApp.ValueMember = "Guid";

            cboPowerPlanClosedApp.DataSource = planos.ToList();
            cboPowerPlanClosedApp.DisplayMember = "Name";
            cboPowerPlanClosedApp.ValueMember = "Guid";

            // Carrega config (se existir) e popula UI
            LoadConfig();

            // Carrega serviços/processos em execução para popular grid
            LoadAvailableItems();
            AtualizaRefresh();

            // Inicia monitor automaticamente (modo padrão ligado)
            StartMonitor();
        }
        #endregion

        #region Tray (NotifyIcon)
        private void InitializeTray()
        {
            // Carrega ícones (coloque eles na mesma pasta do EXE)
            _iconEconomia = new Icon("icon_economia.ico");
            _iconAltoDesempenho = new Icon("icon_alto_desempenho.ico");
            _iconPadrao = new Icon("icon_padrao.ico");

            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Ativar detecção", null, (s, e) => StartMonitor());
            _trayMenu.Items.Add("Desativar detecção", null, (s, e) => StopMonitor());
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Abrir configurações", null, (s, e) => ShowWindow());

            var startupItem = new ToolStripMenuItem("Iniciar com Windows");
            startupItem.Checked = IsStartupEnabled();
            startupItem.Click += (s, e) => ToggleStartup(startupItem);
            _trayMenu.Items.Add(startupItem);

            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Sair", null, (s, e) =>
            {
                _quitRequested = true;
                _trayIcon.Visible = false; // remove ícone da bandeja
                Application.Exit();        // fecha o app
            });

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application, // substitua pelo seu ícone
                Text = "ThGameMode — Aguardando...",
                ContextMenuStrip = _trayMenu,
                Visible = true
            };
            _trayIcon.DoubleClick += (s, e) => ShowWindow();
        }

        private void ShowWindow()
        {
            this.WindowState = FormWindowState.Normal;
            this.Show();
            this.BringToFront();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide(); // esconde o form, mantém tray
            }
        }

        private void ToggleStartup(ToolStripItem menuItem)
        {
            try
            {
                var mi = (ToolStripMenuItem)menuItem;
                if (IsStartupEnabled())
                {
                    DisableStartup();
                    mi.Checked = false;
                }
                else
                {
                    EnableStartup();
                    mi.Checked = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao alterar startup: " + ex.Message);
            }
        }

        private bool IsStartupEnabled()
        {
            using var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return rk?.GetValue("ThGameMode") != null;
        }

        private void EnableStartup()
        {
            using var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            rk.SetValue("ThGameMode", Application.ExecutablePath);
        }

        private void DisableStartup()
        {
            using var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            rk.DeleteValue("ThGameMode", false);
        }

        public void UpdateTrayStatus(bool altoDesempenhoAtivo)
        {
            if (_trayIcon == null)
                return;

            if (altoDesempenhoAtivo)
            {
                _trayIcon.Icon = _iconAltoDesempenho;
                _trayIcon.Text = "ThGameMode — Alto desempenho ativo";
            }
            else
            {
                _trayIcon.Icon = _iconEconomia;
                _trayIcon.Text = "ThGameMode — Economia de energia ativa";
            }
        }

        #endregion

        #region Config JSON I/O
        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    _config = new Configuracao(); // default
                    ApplyConfigToUI();
                    return;
                }

                string json = File.ReadAllText(_configPath, Encoding.UTF8);
                _config = JsonSerializer.Deserialize<Configuracao>(json) ?? new Configuracao();
                _itemsAdded = _config.ListServices ?? new List<string>();
                ApplyConfigToUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar configuração: " + ex.Message);
            }
        }

        private void ApplyConfigToUI()
        {
            try
            {
                if (nudCheckInterval.InvokeRequired)
                {
                    nudCheckInterval.Invoke(new Action(ApplyConfigToUI));
                    return;
                }

                nudCheckInterval.Value = Math.Max(nudCheckInterval.Minimum, Math.Min(nudCheckInterval.Maximum, _config.CheckInterval));
                cboPowerPlanOpenApp.SelectedValue = _config.PowerPlanOpenApp;
                cboPowerPlanClosedApp.SelectedValue = _config.PowerPlanClosedApp;

                AtualizarGridServicesAdded();
            }
            catch { }
        }

        private void SaveConfig()
        {
            try
            {
                _config.CheckInterval = (int)nudCheckInterval.Value;
                _config.PowerPlanOpenApp = cboPowerPlanOpenApp.SelectedValue?.ToString() ?? string.Empty;
                _config.PowerPlanClosedApp = cboPowerPlanClosedApp.SelectedValue?.ToString() ?? string.Empty;
                _config.ListServices = _itemsAdded.ToList();

                string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar configuração: " + ex.Message);
            }
        }
        #endregion

        #region Grid helpers
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
                    dgvListServices.Rows.Add(item);
            }
        }

        private void txtSearchService_TextChanged(object sender, EventArgs e)
        {
            string filtro = txtSearchService.Text.Trim().ToLower();
            dgvListServices.Rows.Clear();

            var adicionados = new HashSet<string>(_itemsAdded, StringComparer.OrdinalIgnoreCase);

            foreach (var item in _itemsAvailable)
            {
                if (!adicionados.Contains(item) && item.ToLower().Contains(filtro))
                    dgvListServices.Rows.Add(item);
            }
        }

        private void txtSearchServiceAdded_TextChanged(object sender, EventArgs e)
        {
            AtualizarGridServicesAdded();
        }

        private void dgvListServices_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;

            var item = dgvListServices.Rows[e.RowIndex].Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(item)) return;

            if (!_itemsAdded.Contains(item, StringComparer.OrdinalIgnoreCase))
            {
                _itemsAdded.Add(item);
                AtualizarGridServicesAdded();
                dgvListServices.Rows.RemoveAt(e.RowIndex);
                SaveConfigAndRestartMonitor();
            }
        }

        private void dgvListServicesAdded_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;

            var item = dgvListServicesAdded.Rows[e.RowIndex].Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(item)) return;

            _itemsAdded.RemoveAll(x => string.Equals(x, item, StringComparison.OrdinalIgnoreCase));
            AtualizarGridServicesAdded();

            LoadAvailableItems();
            AtualizaRefresh();
            SaveConfigAndRestartMonitor();
        }
        #endregion

        #region Load available items
        private void LoadAvailableItems()
        {
            _itemsAvailable.Clear();

            try
            {
                var runningServices = ServiceController.GetServices()
                    .Where(s => s.Status == ServiceControllerStatus.Running)
                    .Select(s => s.ServiceName);
                _itemsAvailable.AddRange(runningServices);
            }
            catch { }

            try
            {
                var processos = Process.GetProcesses()
                    .Select(p => { try { return p.ProcessName; } catch { return null; } })
                    .Where(n => !string.IsNullOrEmpty(n));
                _itemsAvailable.AddRange(processos);
            }
            catch { }

            _itemsAvailable = _itemsAvailable.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();
        }
        #endregion

        #region Planos de energia
        public class PowerPlan
        {
            public string Name { get; set; }
            public string Guid { get; set; }
            public override string ToString() => $"{Name} ({Guid})";
        }

        private List<PowerPlan> GetEnergyPlans()
        {
            var planos = new List<PowerPlan>();

            try
            {
                using var process = new Process
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

                foreach (string linha in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (linha.Contains("GUID", StringComparison.OrdinalIgnoreCase))
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
            catch { }

            if (!planos.Any())
                planos.Add(new PowerPlan { Guid = "", Name = "Padrão do sistema" });

            return planos;
        }

        private void TrocarPlano(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid)) return;

            try
            {
                using var p = new Process
                {
                    StartInfo = new ProcessStartInfo("powercfg", $"/setactive {guid}")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                };
                p.Start();
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                _trayIcon?.ShowBalloonTip(1500, "ThGameMode", $"Erro ao trocar plano: {ex.Message}", ToolTipIcon.Error);
            }
        }
        #endregion

        #region Monitor
        private void StartMonitor()
        {
            if (_monitorActive) return;

            _ctsMonitor = new CancellationTokenSource();
            _monitorActive = true;
            _monitorTask = Task.Run(() => MonitorLoopAsync(_ctsMonitor.Token));
            _trayIcon?.ShowBalloonTip(1000, "ThGameMode", "Detecção ativada", ToolTipIcon.Info);
        }

        private void StopMonitor()
        {
            if (!_monitorActive) return;

            try
            {
                _ctsMonitor.Cancel();
                _monitorTask?.Wait(2000);
            }
            catch { }
            finally
            {
                _monitorActive = false;
                _modoAltoAtivo = false;
                _trayIcon?.ShowBalloonTip(1000, "ThGameMode", "Detecção desativada", ToolTipIcon.Info);
                _trayIcon.Icon = _iconPadrao;
                _trayIcon.Text = "ThGameMode — Desativado";
            }
        }

        private async Task MonitorLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    LoadConfig();

                    bool itemRodando = false;

                    foreach (var item in _config.ListServices)
                    {
                        if (string.IsNullOrWhiteSpace(item)) continue;

                        bool rodando = await IsItemRunningAsync(item);
                        if (rodando)
                        {
                            itemRodando = true;
                            break;
                        }
                    }

                    if (itemRodando && !_modoAltoAtivo)
                    {
                        TrocarPlano(_config.PowerPlanOpenApp);
                        _modoAltoAtivo = true;
                        _trayIcon!.Text = "ThGameMode — Alto desempenho ativo";
                        UpdateTrayStatus(true);
                    }
                    else if (!itemRodando && _modoAltoAtivo)
                    {
                        TrocarPlano(_config.PowerPlanClosedApp);
                        _modoAltoAtivo = false;
                        _trayIcon!.Text = "ThGameMode — Economia ativa";
                        UpdateTrayStatus(false);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _trayIcon?.ShowBalloonTip(1000, "ThGameMode", $"Erro no monitor: {ex.Message}", ToolTipIcon.Warning);
                }

                int intervalo = Math.Max(1, _config.CheckInterval);
                try { await Task.Delay(intervalo * 1000, token); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task<bool> IsItemRunningAsync(string item)
        {
            try
            {
                using var sc = new ServiceController(item);
                return sc.Status == ServiceControllerStatus.Running;
            }
            catch { }

            string procName = item;
            string windowText = null;
            if (item.Contains("|"))
            {
                var parts = item.Split('|', 2);
                procName = parts[0];
                windowText = parts[1];
            }

            try
            {
                var processos = Process.GetProcessesByName(procName);
                if (processos.Length == 0) return false;

                if (string.IsNullOrEmpty(windowText)) return true;

                foreach (var p in processos)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(p.MainWindowTitle) &&
                            p.MainWindowTitle.IndexOf(windowText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return false;
        }
        #endregion

        #region Save & Restart Monitor
        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveConfigAndRestartMonitor();
        }

        private void SaveConfigAndRestartMonitor()
        {
            SaveConfig();
            StopMonitor();
            LoadAvailableItems();
            AtualizaRefresh();
            StartMonitor();
        }
        #endregion

        #region Buttons e Refresh
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadAvailableItems();
            AtualizaRefresh();
        }
        #endregion

        #region Dispose / FormClosing
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_quitRequested && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide(); // apenas esconde o form
                return;
            }

            // Limpa tray e finaliza monitor
            try { _trayIcon.Visible = false; _trayIcon.Dispose(); } catch { }
            StopMonitor();

            base.OnFormClosing(e);
        }
        #endregion
    }
}
