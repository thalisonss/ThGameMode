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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using ThGameMode.Utils;

namespace ThGameMode.Screens
{
    public partial class frmConfig : Form
    {
        // Caminho do JSON na raiz do programa
        private readonly string _configPath = Path.Combine(AppContext.BaseDirectory, "ThGameModeConfig.json");

        // Itens disponíveis e adicionados
        private List<string> _itemsAvailable = new();
        private List<string> _itemsAdded = new();
        private readonly Dictionary<string, string> _itemDisplayNameCache = new(StringComparer.OrdinalIgnoreCase);

        // Monitor background
        private CancellationTokenSource? _ctsMonitor;
        private Task? _monitorTask;
        private bool _monitorActive = false;
        private bool _modoAltoAtivo = false;
        private string? _planoEnergiaOriginalGuid;

        // Tray
        private NotifyIcon? _trayIcon;
        private Icon? _iconEconomia;
        private Icon? _iconAltoDesempenho;
        private Icon? _iconPadrao;
        private ContextMenuStrip? _trayMenu;
        private bool _quitRequested = false;
        private readonly bool _startMinimized;

        // Config atual em memória
        private Configuracao _config = new();

        // Estado do tray
        private DateTime _modoAtivadoEm = DateTime.Now;
        private string _ultimoProcessoDetectado = "Nenhum";
        private DateTime _ultimaMudanca = DateTime.Now;
        private System.Windows.Forms.Timer? _tooltipTimer;



        public frmConfig(bool startMinimized = false)
        {
            _startMinimized = startMinimized;
            InitializeComponent();
            InitializeTray();
        }

        #region Form Load / Init
        private void frmConfig_Load(object sender, EventArgs e)
        {
            try
            {
                // Carrega planos de energia para os combo boxes
                AppLogger.Write(AppLogger.LogLevel.Info, "Inicializando interface e carregando configurações...");

                var planos = GetEnergyPlans();
                AppLogger.Write(AppLogger.LogLevel.Info, $"Planos de energia carregados: {planos.Count()} encontrados.");

                cboPowerPlanOpenApp.DataSource = planos.ToList();
                cboPowerPlanOpenApp.DisplayMember = "Name";
                cboPowerPlanOpenApp.ValueMember = "Guid";

                cboPowerPlanClosedApp.DataSource = planos.ToList();
                cboPowerPlanClosedApp.DisplayMember = "Name";
                cboPowerPlanClosedApp.ValueMember = "Guid";

                // Carrega config (se existir) e popula UI
                LoadConfig();
                EnsureStartupUsesMinimizedArgument();

                _tooltipTimer = new System.Windows.Forms.Timer();
                _tooltipTimer.Interval = 1000; // 1 segundo
                _tooltipTimer.Tick += (s, e) =>
                {
                    // Atualiza tooltip a cada 1s sem mudar estado
                    UpdateTrayTooltip(_modoAltoAtivo);
                };
                _tooltipTimer.Start();

                // Carrega serviços/processos em execução para popular grid
                LoadAvailableItems();
                AtualizaRefresh();

                // Inicia monitor automaticamente (modo padrão ligado)
                StartMonitor();
                AppLogger.Write(AppLogger.LogLevel.Info, $"Monitor iniciado automaticamente.");

                if (_startMinimized)
                {
                    BeginInvoke(new Action(HideToTray));
                }
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Error, "Erro na inicialização do formulário frmConfig: " + ex.Message);
                MessageBox.Show("Erro ao iniciar o aplicativo: " + ex.Message);
            }
        }
        #endregion

        #region Tray (NotifyIcon)
        private void InitializeTray()
        {
            // Carrega ícones a partir da pasta do executável.
            _iconEconomia = LoadIconFromAppDirectory("icon_economia.ico");
            _iconAltoDesempenho = LoadIconFromAppDirectory("icon_alto_desempenho.ico");
            _iconPadrao = LoadIconFromAppDirectory("icon_padrao.ico");

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
                if (_trayIcon != null)
                    _trayIcon.Visible = false; // remove ícone da bandeja
                Application.Exit();        // fecha o app
            });

            _trayIcon = new NotifyIcon
            {
                Icon = _iconPadrao,
                Text = "ThGameMode — Aguardando...",
                ContextMenuStrip = _trayMenu,
                Visible = true
            };
            _trayIcon.DoubleClick += (s, e) => ShowWindow();

           

        }

        private static Icon LoadIconFromAppDirectory(string iconFileName)
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, iconFileName);
            if (!File.Exists(iconPath))
                throw new FileNotFoundException($"Could not find icon file '{iconPath}'.", iconPath);

            return new Icon(iconPath);
        }

        private void ShowWindow()
        {
            this.WindowState = FormWindowState.Normal;
            this.Show();
            this.ShowInTaskbar = true;
            this.BringToFront();
            this.Activate();
        }

        private void HideToTray()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Hide();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                HideToTray(); // esconde o form, mantém tray
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

        private static string GetStartupCommand()
        {
            return $"\"{Application.ExecutablePath}\" --minimized";
        }

        private void EnsureStartupUsesMinimizedArgument()
        {
            using var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            var currentValue = rk?.GetValue("ThGameMode") as string;

            if (string.IsNullOrWhiteSpace(currentValue))
                return;

            if (currentValue.IndexOf("--minimized", StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            rk?.SetValue("ThGameMode", GetStartupCommand());
        }

        private void EnableStartup()
        {
            using var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            rk?.SetValue("ThGameMode", GetStartupCommand());
        }

        private void DisableStartup()
        {
            using var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            rk?.DeleteValue("ThGameMode", false);
        }

        public void UpdateTrayStatus(bool altoDesempenhoAtivo, string processoDetectado = "")
        {
            AppLogger.Write(AppLogger.LogLevel.Info, $"Atualizando tray. Estado: {(altoDesempenhoAtivo ? "Alto desempenho" : "Economia")}, Processo detectado: {processoDetectado}");

            if (!string.IsNullOrWhiteSpace(processoDetectado))
                _ultimoProcessoDetectado = processoDetectado;

            if (_trayIcon == null)
                return;

            _ultimaMudanca = DateTime.Now;

            // ícone + texto curto
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

            // reinicia contagem do tempo
            _modoAtivadoEm = DateTime.Now;

            // atualiza tooltip completo
            UpdateTrayTooltip(altoDesempenhoAtivo);
        }

        private void ApplyTrayVisualState(bool altoDesempenhoAtivo)
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

            UpdateTrayTooltip(altoDesempenhoAtivo);
        }

        private void UpdateTrayTooltip(bool altoDesempenhoAtivo)
        {
            if (_trayIcon == null) return;

            TimeSpan tempo = DateTime.Now - _modoAtivadoEm;

            string modo = altoDesempenhoAtivo ? "Alto desempenho" : "Economia";
            string tempoFormatado = tempo.ToString(@"hh\:mm\:ss");

            string tooltip =
                $"Modo atual: {modo}\n" +
                $"Tempo ativo: {tempoFormatado}\n" +
                $"Último jogo: {_ultimoProcessoDetectado}\n" +
                $"Última mudança: {_ultimaMudanca:HH:mm:ss}";

            _trayIcon.Text = tooltip.Length > 63
                ? tooltip.Substring(0, 63)
                : tooltip;
        }

        #endregion

        #region Config JSON I/O
        private void LoadConfig(bool applyToUi = true)
        {
            try
            {
                AppLogger.Write(AppLogger.LogLevel.Info, "Carregando configuração do arquivo JSON...");

                if (!File.Exists(_configPath))
                {
                    AppLogger.Write(AppLogger.LogLevel.Warning, "Arquivo de configuração não encontrado. Usando valores padrão.");
                    _config = new Configuracao(); // default
                    _itemsAdded = _config.ListServices ?? new List<string>();

                    if (applyToUi)
                        ApplyConfigToUI();

                    return;
                }

                string json = File.ReadAllText(_configPath, Encoding.UTF8);
                _config = JsonSerializer.Deserialize<Configuracao>(json) ?? new Configuracao();
                _itemsAdded = _config.ListServices ?? new List<string>();

                AppLogger.Write(AppLogger.LogLevel.Info, "Configuração carregada com sucesso.");

                if (applyToUi)
                    ApplyConfigToUI();
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Error, "Erro ao carregar configuração: " + ex.Message);
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
            AppLogger.Write(AppLogger.LogLevel.Info, "Salvando configuração no arquivo JSON...");

            try
            {
                _config.CheckInterval = (int)nudCheckInterval.Value;
                _config.PowerPlanOpenApp = cboPowerPlanOpenApp.SelectedValue?.ToString() ?? string.Empty;
                _config.PowerPlanClosedApp = cboPowerPlanClosedApp.SelectedValue?.ToString() ?? string.Empty;
                _config.ListServices = _itemsAdded.ToList();

                string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json, Encoding.UTF8);

                AppLogger.Write(AppLogger.LogLevel.Info, "Configuração salva com sucesso.");
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Error, "Erro ao salvar configuração: " + ex.Message);
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
                string displayName = GetItemDisplayName(item);
                if (displayName.ToLower().Contains(filtro))
                {
                    int rowIndex = dgvListServicesAdded.Rows.Add(displayName);
                    dgvListServicesAdded.Rows[rowIndex].Tag = item;
                }
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
                    int rowIndex = dgvListServices.Rows.Add(GetItemDisplayName(item));
                    dgvListServices.Rows[rowIndex].Tag = item;
                }
            }
        }

        private void txtSearchService_TextChanged(object sender, EventArgs e)
        {
            string filtro = txtSearchService.Text.Trim().ToLower();
            dgvListServices.Rows.Clear();

            var adicionados = new HashSet<string>(_itemsAdded, StringComparer.OrdinalIgnoreCase);

            foreach (var item in _itemsAvailable)
            {
                string displayName = GetItemDisplayName(item);
                if (!adicionados.Contains(item) && displayName.ToLower().Contains(filtro))
                {
                    int rowIndex = dgvListServices.Rows.Add(displayName);
                    dgvListServices.Rows[rowIndex].Tag = item;
                }
            }
        }

        private void txtSearchServiceAdded_TextChanged(object sender, EventArgs e)
        {
            AtualizarGridServicesAdded();
        }

        private void dgvListServices_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;

            var item = dgvListServices.Rows[e.RowIndex].Tag?.ToString()
                ?? dgvListServices.Rows[e.RowIndex].Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(item)) return;

            if (!_itemsAdded.Contains(item, StringComparer.OrdinalIgnoreCase))
            {
                _itemsAdded.Add(item);
                AtualizarGridServicesAdded();
                dgvListServices.Rows.RemoveAt(e.RowIndex);
                SaveConfigAndRefreshMonitorState();
            }
        }

        private void dgvListServicesAdded_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;

            var item = dgvListServicesAdded.Rows[e.RowIndex].Tag?.ToString()
                ?? dgvListServicesAdded.Rows[e.RowIndex].Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(item)) return;

            _itemsAdded.RemoveAll(x => string.Equals(x, item, StringComparison.OrdinalIgnoreCase));
            AtualizarGridServicesAdded();
            AtualizaRefresh();
            SaveConfigAndRefreshMonitorState();
        }
        #endregion

        #region Load available items
        private void LoadAvailableItems()
        {
            _itemsAvailable.Clear();
            _itemDisplayNameCache.Clear();

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
                    .OfType<string>();
                _itemsAvailable.AddRange(processos);
            }
            catch { }

            _itemsAvailable = _itemsAvailable.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();
            PopulateDisplayNameCache();
        }

        private string GetItemDisplayName(string item)
        {
            if (string.IsNullOrWhiteSpace(item))
                return string.Empty;

            if (_itemDisplayNameCache.TryGetValue(item, out var cachedDisplayName))
                return cachedDisplayName;

            try
            {
                using var sc = new ServiceController(item);
                _ = sc.Status;
                return _itemDisplayNameCache[item] = item;
            }
            catch { }

            string procName = item;
            if (item.Contains("|"))
            {
                var parts = item.Split('|', 2);
                procName = parts[0];
            }

            try
            {
                var processo = Process.GetProcessesByName(procName).FirstOrDefault();
                if (processo != null)
                {
                    string friendlyName = TryGetFriendlyProcessName(processo);
                    if (!string.IsNullOrWhiteSpace(friendlyName) &&
                        !string.Equals(friendlyName, procName, StringComparison.OrdinalIgnoreCase))
                    {
                        return _itemDisplayNameCache[item] = $"{friendlyName} - {procName}";
                    }
                }
            }
            catch { }

            return _itemDisplayNameCache[item] = procName;
        }

        private void PopulateDisplayNameCache()
        {
            var processFriendlyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        if (!processFriendlyNames.ContainsKey(process.ProcessName))
                            processFriendlyNames[process.ProcessName] = TryGetFriendlyProcessName(process);
                    }
                    catch { }
                }
            }
            catch { }

            foreach (var item in _itemsAvailable.Concat(_itemsAdded).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(item) || _itemDisplayNameCache.ContainsKey(item))
                    continue;

                try
                {
                    using var sc = new ServiceController(item);
                    _ = sc.Status;
                    _itemDisplayNameCache[item] = item;
                    continue;
                }
                catch { }

                string procName = item;
                if (item.Contains("|"))
                {
                    var parts = item.Split('|', 2);
                    procName = parts[0];
                }

                if (processFriendlyNames.TryGetValue(procName, out var friendlyName) &&
                    !string.IsNullOrWhiteSpace(friendlyName) &&
                    !string.Equals(friendlyName, procName, StringComparison.OrdinalIgnoreCase))
                {
                    _itemDisplayNameCache[item] = $"{friendlyName} - {procName}";
                }
                else
                {
                    _itemDisplayNameCache[item] = procName;
                }
            }
        }

        private string TryGetFriendlyProcessName(Process process)
        {
            try
            {
                string? fileName = process.MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(fileName) && File.Exists(fileName))
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(fileName);

                    if (!string.IsNullOrWhiteSpace(versionInfo.FileDescription))
                        return versionInfo.FileDescription.Trim();

                    if (!string.IsNullOrWhiteSpace(versionInfo.ProductName))
                        return versionInfo.ProductName.Trim();
                }
            }
            catch { }

            try
            {
                if (!string.IsNullOrWhiteSpace(process.MainWindowTitle))
                    return process.MainWindowTitle.Trim();
            }
            catch { }

            try
            {
                return process.ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }
        #endregion

        #region Planos de energia
        public class PowerPlan
        {
            public string Name { get; set; } = string.Empty;
            public string Guid { get; set; } = string.Empty;
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
                AppLogger.Write(AppLogger.LogLevel.Info, $"Trocando plano de energia para GUID: {guid}");

                p.Start();
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Error, "Erro ao trocar plano de energia: " + ex.Message);
                _trayIcon?.ShowBalloonTip(1500, "ThGameMode", $"Erro ao trocar plano: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private string? GetActivePowerPlanGuid()
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powercfg",
                        Arguments = "/getactivescheme",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var match = Regex.Match(output, @"\b([0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12})\b");
                if (match.Success)
                    return match.Groups[1].Value;
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Warning, "Não foi possível obter o plano de energia ativo: " + ex.Message);
            }

            return null;
        }

        private void EnsureOriginalPowerPlanCaptured()
        {
            if (!string.IsNullOrWhiteSpace(_planoEnergiaOriginalGuid))
                return;

            _planoEnergiaOriginalGuid = GetActivePowerPlanGuid();

            if (!string.IsNullOrWhiteSpace(_planoEnergiaOriginalGuid))
                AppLogger.Write(AppLogger.LogLevel.Info, $"Plano de energia original capturado: {_planoEnergiaOriginalGuid}");
        }

        private void RestoreOriginalPowerPlan()
        {
            if (string.IsNullOrWhiteSpace(_planoEnergiaOriginalGuid))
                return;

            string guidOriginal = _planoEnergiaOriginalGuid;
            _planoEnergiaOriginalGuid = null;

            string? guidAtual = GetActivePowerPlanGuid();
            if (string.Equals(guidAtual, guidOriginal, StringComparison.OrdinalIgnoreCase))
                return;

            AppLogger.Write(AppLogger.LogLevel.Info, $"Restaurando plano de energia original: {guidOriginal}");
            TrocarPlano(guidOriginal);
        }
        #endregion

        #region Monitor
        private void StartMonitor()
        {
            if (_monitorActive)
            {
                AppLogger.Write(AppLogger.LogLevel.Warning, "Monitor já está ativo. Ignorando StartMonitor.");
                return;
            }

            AppLogger.Write(AppLogger.LogLevel.Info, "Iniciando monitor de serviços/processos...");

            try
            {
                _ctsMonitor = new CancellationTokenSource();
                EvaluateMonitorStateAsync(_ctsMonitor.Token).GetAwaiter().GetResult();
                _monitorActive = true;
                _monitorTask = Task.Run(() => MonitorLoopAsync(_ctsMonitor.Token));
                _trayIcon?.ShowBalloonTip(1000, "ThGameMode", "Detecção ativada", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Error, "Erro ao carregar configuração antes de iniciar o monitor: " + ex.Message);
                MessageBox.Show("Erro ao carregar configuração: " + ex.Message);
            }         
        }

        private void StopMonitor()
        {
            if (!_monitorActive)
            {
                AppLogger.Write(AppLogger.LogLevel.Warning, "Monitor não está ativo. Ignorando StopMonitor.");
                return;
            }

            AppLogger.Write(AppLogger.LogLevel.Info, "Parando monitor de serviços/processos...");

            try
            {
                _ctsMonitor?.Cancel();
                _monitorTask?.Wait(2000);

                AppLogger.Write(AppLogger.LogLevel.Info, "Monitor parado com sucesso.");
            }
            catch
            { 
                AppLogger.Write(AppLogger.LogLevel.Error, "Monitor não respondeu ao cancelamento em tempo hábil.");
            }
            finally
            {
                RestoreOriginalPowerPlan();
                _monitorActive = false;
                _modoAltoAtivo = false;

                if (!_quitRequested && _trayIcon != null)
                {
                    _trayIcon.ShowBalloonTip(1000, "ThGameMode", "Detecção desativada", ToolTipIcon.Info);
                    _trayIcon.Icon = _iconPadrao;
                    _trayIcon.Text = "ThGameMode — Desativado";
                }
            }
        }

        private async Task MonitorLoopAsync(CancellationToken token)
        {
            AppLogger.Write(AppLogger.LogLevel.Info, "Monitor iniciado.");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await EvaluateMonitorStateAsync(token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    AppLogger.Write(AppLogger.LogLevel.Error, "Erro no monitor: " + ex.Message);
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
            string? windowText = null;
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

        private async Task EvaluateMonitorStateAsync(CancellationToken token)
        {
            bool itemRodando = false;

            foreach (var item in _config.ListServices ?? Enumerable.Empty<string>())
            {
                token.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(item)) continue;

                bool rodando = await IsItemRunningAsync(item);
                if (rodando)
                {
                    AppLogger.Write(AppLogger.LogLevel.Info, $"Item detectado em execução: {item}");
                    itemRodando = true;
                    break;
                }
            }

            if (itemRodando && !_modoAltoAtivo)
            {
                EnsureOriginalPowerPlanCaptured();
                TrocarPlano(_config.PowerPlanOpenApp);
                _modoAltoAtivo = true;
                UpdateTrayStatus(true);
            }
            else if (!itemRodando && _modoAltoAtivo)
            {
                EnsureOriginalPowerPlanCaptured();
                TrocarPlano(_config.PowerPlanClosedApp);
                _modoAltoAtivo = false;
                UpdateTrayStatus(false);
            }
            else if (itemRodando)
            {
                ApplyTrayVisualState(true);
            }
            else
            {
                ApplyTrayVisualState(false);
            }

            AppLogger.Write(AppLogger.LogLevel.Info, $"Monitor verificação concluída. Estado atual: {(_modoAltoAtivo ? "Alto desempenho" : "Economia")}");
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

        private void SaveConfigAndRefreshMonitorState()
        {
            SaveConfig();

            if (_monitorActive)
                EvaluateMonitorStateAsync(CancellationToken.None).GetAwaiter().GetResult();
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
            try
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                }
            }
            catch { }
            StopMonitor();

            base.OnFormClosing(e);
        }
        #endregion
    }
}
