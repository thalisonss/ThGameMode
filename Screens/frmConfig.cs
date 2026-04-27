using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThGameMode.Utils;

namespace ThGameMode.Screens
{
    /// <summary>
    /// Tela principal e centro de orquestração da aplicação:
    /// carrega configurações, mantém o ícone de bandeja e monitora processos/serviços.
    /// </summary>
    public partial class frmConfig : Form
    {
        // Caminho do arquivo persistido com as preferências do usuário.
        private readonly string _configPath = Path.Combine(AppContext.BaseDirectory, "ThGameModeConfig.json");

        // _itemsAvailable: itens detectados no sistema e exibidos para seleção.
        // _itemsAdded: itens efetivamente monitorados e salvos na configuração.
        private List<string> _itemsAvailable = new();
        private List<string> _itemsAdded = new();
        private List<string> _itemsOpenCloseAvailable = new();
        private List<string> _itemsOpenCloseAdded = new();
        private Dictionary<string, string> _manualExecutablePaths = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _openCloseExecutablePaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _closedApplicationsToReopen = new(StringComparer.OrdinalIgnoreCase);

        // Cache para evitar consultar metadados de processos/serviços repetidamente ao montar a UI.
        private readonly Dictionary<string, string> _itemDisplayNameCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly GitHubReleaseChecker _releaseChecker = new();

        // Controle do monitor assíncrono executado em segundo plano.
        private CancellationTokenSource? _ctsMonitor;
        private Task? _monitorTask;
        private bool _monitorActive = false;
        private bool _modoAltoAtivo = false;
        private string? _planoEnergiaOriginalGuid;

        // Estado visual e comportamental do ícone da bandeja do sistema.
        private NotifyIcon? _trayIcon;
        private Icon? _iconEconomia;
        private Icon? _iconAltoDesempenho;
        private Icon? _iconPadrao;
        private ContextMenuStrip? _trayMenu;
        private ToolStripMenuItem? _downloadUpdateMenuItem;
        private ToolStripMenuItem? _monitorToggleMenuItem;
        private bool _quitRequested = false;
        private readonly bool _startMinimized;
        private bool _updateNotificationPendingOpen = false;
        private string? _latestReleaseUrl;

        // Espelho em memória do JSON carregado/salvo no disco.
        private Configuracao _config = new();

        // Informações usadas para compor o tooltip e dar feedback visual ao usuário.
        private DateTime _modoAtivadoEm = DateTime.Now;
        private string _ultimoProcessoDetectado = "Nenhum";
        private DateTime _ultimaMudanca = DateTime.Now;
        private System.Windows.Forms.Timer? _tooltipTimer;



        /// <summary>
        /// startMinimized é usado no startup automático para abrir a aplicação direto na bandeja.
        /// </summary>
        public frmConfig(bool startMinimized = false)
        {
            _startMinimized = startMinimized;
            InitializeComponent();
            InitializeTray();
            InitializeManualExecutableButtons();
            InitializeOpenCloseControls();
        }

        #region Form Load / Init
        private void frmConfig_Load(object sender, EventArgs e)
        {
            try
            {
                // A inicialização prepara a UI, restaura configuração, popula listas e sobe o monitor.
                AppLogger.Write(AppLogger.LogLevel.Info, "Inicializando interface e carregando configurações...");

                var planos = GetEnergyPlans();
                AppLogger.Write(AppLogger.LogLevel.Info, $"Planos de energia carregados: {planos.Count()} encontrados.");

                cboPowerPlanOpenApp.DataSource = planos.ToList();
                cboPowerPlanOpenApp.DisplayMember = "Name";
                cboPowerPlanOpenApp.ValueMember = "Guid";

                cboPowerPlanClosedApp.DataSource = planos.ToList();
                cboPowerPlanClosedApp.DisplayMember = "Name";
                cboPowerPlanClosedApp.ValueMember = "Guid";

                // Depois de preencher os combos, aplicamos a configuração salva para selecionar os valores corretos.
                LoadConfig();
                EnsureStartupUsesMinimizedArgument();

                _tooltipTimer = new System.Windows.Forms.Timer();
                _tooltipTimer.Interval = 1000; // 1 segundo
                _tooltipTimer.Tick += (s, e) =>
                {
                    // O tooltip é recalculado continuamente para mostrar tempo ativo sem reiniciar o monitor.
                    UpdateTrayTooltip(_modoAltoAtivo);
                };
                _tooltipTimer.Start();

                // Carrega serviços/processos em execução para popular grid
                LoadAvailableItems();
                AtualizaRefresh();
                AtualizaRefreshOpenClose();

                // O monitor já sobe na abertura para que o comportamento automático funcione sem ação do usuário.
                StartMonitor();
                AppLogger.Write(AppLogger.LogLevel.Info, $"Monitor iniciado automaticamente.");
                _ = CheckForUpdatesAsync(false);

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
            // Cada ícone representa um estado visual do app na bandeja.
            _iconEconomia = LoadIconFromAppDirectory("icon_economia.ico");
            _iconAltoDesempenho = LoadIconFromAppDirectory("icon_alto_desempenho.ico");
            _iconPadrao = LoadIconFromAppDirectory("icon_padrao.ico");

            // O menu contextual expõe as ações mais comuns sem precisar abrir a janela.
            _trayMenu = new ContextMenuStrip();
            _monitorToggleMenuItem = new ToolStripMenuItem("Detecção ativada")
            {
                Checked = _monitorActive
            };
            _monitorToggleMenuItem.Click += (s, e) => ToggleMonitor(_monitorToggleMenuItem);
            _trayMenu.Items.Add(_monitorToggleMenuItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Abrir configurações", null, (s, e) => ShowWindow());
            _trayMenu.Items.Add("Verificar atualizações", null, async (s, e) => await CheckForUpdatesAsync(true));

            _downloadUpdateMenuItem = new ToolStripMenuItem("Baixar nova versão")
            {
                Enabled = false
            };
            _downloadUpdateMenuItem.Click += (s, e) => OpenLatestReleasePage();
            _trayMenu.Items.Add(_downloadUpdateMenuItem);

            var startupItem = new ToolStripMenuItem("Iniciar com Windows");
            startupItem.Checked = IsStartupEnabled();
            startupItem.Click += (s, e) => ToggleStartup(startupItem);
            _trayMenu.Items.Add(startupItem);

            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Sair", null, (s, e) =>
            {
                // Essa flag diferencia um fechamento intencional de um simples "fechar janela".
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
            _trayIcon.BalloonTipClicked += (s, e) =>
            {
                if (_updateNotificationPendingOpen)
                    OpenLatestReleasePage();
            };



        }

        private static Icon LoadIconFromAppDirectory(string iconFileName)
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, iconFileName);
            if (!File.Exists(iconPath))
                throw new FileNotFoundException($"Could not find icon file '{iconPath}'.", iconPath);

            return new Icon(iconPath);
        }

        private void InitializeManualExecutableButtons()
        {
            btnInstallUninstall.Enabled = true;
            btnInstallUninstall.Text = "Adicionar .exe";
            btnInstallUninstall.Click -= btnInstallUninstall_Click;
            btnInstallUninstall.Click += btnInstallUninstall_Click;

            button1.Enabled = true;
            button1.Text = "Adicionar .exe";
            button1.Click -= button1_Click;
            button1.Click += button1_Click;
        }

        private void InitializeOpenCloseControls()
        {
            txtSearchServiceOpenClose.TextChanged -= txtSearchServiceOpenClose_TextChanged;
            txtSearchServiceOpenClose.TextChanged += txtSearchServiceOpenClose_TextChanged;

            txtSearchServiceOpenCloseAdded.TextChanged -= txtSearchServiceOpenCloseAdded_TextChanged;
            txtSearchServiceOpenCloseAdded.TextChanged += txtSearchServiceOpenCloseAdded_TextChanged;

            btnRefreshOpenClose.Click -= btnRefreshOpenClose_Click;
            btnRefreshOpenClose.Click += btnRefreshOpenClose_Click;

            dgvListServicesOpenClose.CellClick -= dgvListServicesOpenClose_CellClick;
            dgvListServicesOpenClose.CellClick += dgvListServicesOpenClose_CellClick;

            dgvListServicesOpenCloseAdded.CellClick -= dgvListServicesOpenCloseAdded_CellClick;
            dgvListServicesOpenCloseAdded.CellClick += dgvListServicesOpenCloseAdded_CellClick;
        }

        private void btnInstallUninstall_Click(object? sender, EventArgs e)
        {
            string? executablePath = SelectExecutablePath();
            if (string.IsNullOrWhiteSpace(executablePath))
                return;

            AddManualExecutable(executablePath);
        }

        private void button1_Click(object? sender, EventArgs e)
        {
            string? executablePath = SelectExecutablePath();
            if (string.IsNullOrWhiteSpace(executablePath))
                return;

            AddManualExecutableToOpenClose(executablePath);
        }

        private string? SelectExecutablePath()
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Aplicativos (*.exe)|*.exe",
                Title = "Selecionar executável para monitorar",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return null;

            return dialog.FileName;
        }

        private void AddManualExecutable(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                MessageBox.Show("Selecione um executável válido.", "Arquivo inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string processName = Path.GetFileNameWithoutExtension(executablePath);
            if (string.IsNullOrWhiteSpace(processName))
                return;

            _manualExecutablePaths[processName] = executablePath;
            _itemDisplayNameCache[processName] = BuildDisplayNameFromExecutablePath(processName, executablePath);

            if (!_itemsAdded.Contains(processName, StringComparer.OrdinalIgnoreCase))
                _itemsAdded.Add(processName);

            AtualizarGridServicesAdded();
            AtualizaRefresh();
            SaveConfigAndRefreshMonitorState();
        }

        private void PopulateManualExecutableDisplayCache()
        {
            foreach (var pair in _manualExecutablePaths)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                    continue;

                if (!File.Exists(pair.Value))
                    continue;

                _itemDisplayNameCache[pair.Key] = BuildDisplayNameFromExecutablePath(pair.Key, pair.Value);
            }
        }

        private void PopulateOpenCloseExecutableDisplayCache()
        {
            foreach (var pair in _openCloseExecutablePaths)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                    continue;

                if (!File.Exists(pair.Value))
                    continue;

                _itemDisplayNameCache[pair.Key] = BuildDisplayNameFromExecutablePath(pair.Key, pair.Value);
            }
        }

        private void AddManualExecutableToOpenClose(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                MessageBox.Show("Selecione um executável válido.", "Arquivo inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string processName = Path.GetFileNameWithoutExtension(executablePath);
            if (string.IsNullOrWhiteSpace(processName))
                return;

            _openCloseExecutablePaths[processName] = executablePath;
            _itemDisplayNameCache[processName] = BuildDisplayNameFromExecutablePath(processName, executablePath);

            if (!_itemsOpenCloseAdded.Contains(processName, StringComparer.OrdinalIgnoreCase))
                _itemsOpenCloseAdded.Add(processName);

            AtualizarGridServicesOpenCloseAdded();
            AtualizaRefreshOpenClose();
            SaveConfigAndRefreshMonitorState();
        }

        private static string BuildDisplayNameFromExecutablePath(string processName, string executablePath)
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
                string? friendlyName = !string.IsNullOrWhiteSpace(versionInfo.FileDescription)
                    ? versionInfo.FileDescription.Trim()
                    : versionInfo.ProductName?.Trim();

                if (!string.IsNullOrWhiteSpace(friendlyName) &&
                    !string.Equals(friendlyName, processName, StringComparison.OrdinalIgnoreCase))
                {
                    return $"{friendlyName} - {processName}";
                }
            }
            catch { }

            return processName;
        }

        private void ShowWindow()
        {
            this.WindowState = FormWindowState.Normal;
            this.Show();
            this.ShowInTaskbar = true;
            this.BringToFront();
            this.Activate();
        }

        /// <summary>
        /// Mantém a aplicação viva, mas invisível, para continuar monitorando em segundo plano.
        /// </summary>
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
                HideToTray(); // Minimizar funciona como "enviar para a bandeja".
            }
        }

        /// <summary>
        /// Liga ou desliga a inicialização automática no registro do Windows.
        /// </summary>
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

        /// <summary>
        /// Sempre iniciamos com o argumento --minimized para não "piscar" a janela ao logar no Windows.
        /// </summary>
        private static string GetStartupCommand()
        {
            return $"\"{Application.ExecutablePath}\" --minimized";
        }

        /// <summary>
        /// Corrige entradas antigas do registro que ainda não possuam o argumento de inicialização minimizada.
        /// </summary>
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

            // Ao trocar de modo, atualizamos o ícone e reiniciamos o contador de permanência no estado.
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

            // O tempo mostrado no tooltip representa quanto tempo o modo atual está ativo.
            _modoAtivadoEm = DateTime.Now;

            UpdateTrayTooltip(altoDesempenhoAtivo);
        }

        /// <summary>
        /// Atualiza somente a aparência do tray, sem reiniciar contadores ou registrar mudança de modo.
        /// </summary>
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

        /// <summary>
        /// Monta o texto do tooltip com o contexto mais recente do monitor.
        /// </summary>
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

            // O Windows limita o texto do NotifyIcon; por isso truncamos o tooltip.
            _trayIcon.Text = tooltip.Length > 63
                ? tooltip.Substring(0, 63)
                : tooltip;
        }

        #endregion

        #region Updates
        /// <summary>
        /// Verifica no GitHub se existe uma release mais nova do que a versão atual do executável.
        /// </summary>
        private async Task CheckForUpdatesAsync(bool manualCheck)
        {
            try
            {
                Version currentVersion = GetCurrentVersion();
                AppLogger.Write(AppLogger.LogLevel.Info, $"Verificando atualização no GitHub. Versão atual: {currentVersion}");

                var result = await _releaseChecker.CheckForUpdateAsync(currentVersion);

                if (result.IsUpdateAvailable && !string.IsNullOrWhiteSpace(result.ReleaseUrl))
                {
                    // Guardamos a URL para reutilizar tanto no menu do tray quanto no clique do balão.
                    _latestReleaseUrl = result.ReleaseUrl;
                    _updateNotificationPendingOpen = true;

                    if (_downloadUpdateMenuItem != null)
                        _downloadUpdateMenuItem.Enabled = true;

                    AppLogger.Write(AppLogger.LogLevel.Info, $"Nova versão encontrada: {result.LatestTag}");
                    _trayIcon?.ShowBalloonTip(
                        4000,
                        "Atualização disponível",
                        $"Nova versão encontrada ({result.LatestTag}). Clique para abrir a release no GitHub.",
                        ToolTipIcon.Info);

                    // Em uso interativo, podemos oferecer a abertura imediata da página da release.
                    if (!_startMinimized && Visible && WindowState != FormWindowState.Minimized)
                    {
                        var answer = MessageBox.Show(
                            $"Uma nova versão do ThGameMode está disponível no GitHub.\n\nVersão atual: {currentVersion}\nNova versão: {result.LatestTag}\n\nDeseja abrir a página da release agora?",
                            "Atualização disponível",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (answer == DialogResult.Yes)
                            OpenLatestReleasePage();
                    }

                    return;
                }

                _updateNotificationPendingOpen = false;
                if (_downloadUpdateMenuItem != null)
                    _downloadUpdateMenuItem.Enabled = false;

                if (!string.IsNullOrWhiteSpace(result.FailureReason))
                {
                    AppLogger.Write(AppLogger.LogLevel.Warning, "Não foi possível validar a release mais recente: " + result.FailureReason);

                    if (manualCheck)
                    {
                        MessageBox.Show(
                            result.FailureReason,
                            "Falha ao verificar atualizações",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }

                    return;
                }

                if (manualCheck)
                {
                    MessageBox.Show(
                        $"Você já está na versão mais recente ({currentVersion}).",
                        "Sem atualizações",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (HttpRequestException ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Warning, "Falha ao consultar atualização no GitHub: " + ex.Message);
                if (manualCheck)
                {
                    MessageBox.Show(
                        "Não foi possível verificar atualizações no GitHub no momento.",
                        "Falha ao verificar atualizações",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (TaskCanceledException ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Warning, "Tempo esgotado ao verificar atualização: " + ex.Message);
                if (manualCheck)
                {
                    MessageBox.Show(
                        "A verificação de atualização expirou. Tente novamente em instantes.",
                        "Falha ao verificar atualizações",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Error, "Erro inesperado ao verificar atualização: " + ex.Message);
                if (manualCheck)
                {
                    MessageBox.Show(
                        "Ocorreu um erro inesperado ao verificar atualizações.",
                        "Falha ao verificar atualizações",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// Lê a versão do assembly em execução, usada na comparação de updates.
        /// </summary>
        private static Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
        }

        /// <summary>
        /// Abre a página da última release detectada no navegador padrão do Windows.
        /// </summary>
        private void OpenLatestReleasePage()
        {
            if (string.IsNullOrWhiteSpace(_latestReleaseUrl))
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _latestReleaseUrl,
                    UseShellExecute = true
                });
                _updateNotificationPendingOpen = false;
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Error, "Erro ao abrir página da release: " + ex.Message);
                MessageBox.Show(
                    "Não foi possível abrir a página da nova versão.",
                    "Erro ao abrir release",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        #endregion

        #region Config JSON I/O
        /// <summary>
        /// Carrega o arquivo JSON para memória e, opcionalmente, reflete o estado na interface.
        /// </summary>
        private void LoadConfig(bool applyToUi = true)
        {
            try
            {
                AppLogger.Write(AppLogger.LogLevel.Info, "Carregando configuração do arquivo JSON...");

                if (!File.Exists(_configPath))
                {
                    AppLogger.Write(AppLogger.LogLevel.Warning, "Arquivo de configuração não encontrado. Usando valores padrão.");
                    _config = new Configuracao(); // Mantém o app funcional mesmo no primeiro uso.
                    _itemsAdded = _config.ListServices ?? new List<string>();
                    _itemsOpenCloseAdded = _config.ListServicesOpenClose ?? new List<string>();
                    _manualExecutablePaths = new Dictionary<string, string>(_config.ManualExecutablePaths ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
                    _openCloseExecutablePaths = new Dictionary<string, string>(_config.OpenCloseExecutablePaths ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
                    PopulateManualExecutableDisplayCache();
                    PopulateOpenCloseExecutableDisplayCache();

                    if (applyToUi)
                        ApplyConfigToUI();

                    return;
                }

                string json = File.ReadAllText(_configPath, Encoding.UTF8);
                _config = JsonSerializer.Deserialize<Configuracao>(json) ?? new Configuracao();
                _itemsAdded = _config.ListServices ?? new List<string>();
                _itemsOpenCloseAdded = _config.ListServicesOpenClose ?? new List<string>();
                _manualExecutablePaths = new Dictionary<string, string>(_config.ManualExecutablePaths ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
                _openCloseExecutablePaths = new Dictionary<string, string>(_config.OpenCloseExecutablePaths ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
                PopulateManualExecutableDisplayCache();
                PopulateOpenCloseExecutableDisplayCache();

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

        /// <summary>
        /// Copia a configuração carregada para os controles da tela.
        /// </summary>
        private void ApplyConfigToUI()
        {
            try
            {
                if (nudCheckInterval.InvokeRequired)
                {
                    // Caso o método seja chamado fora da thread da UI, voltamos com segurança para ela.
                    nudCheckInterval.Invoke(new Action(ApplyConfigToUI));
                    return;
                }

                nudCheckInterval.Value = Math.Max(nudCheckInterval.Minimum, Math.Min(nudCheckInterval.Maximum, _config.CheckInterval));
                cboPowerPlanOpenApp.SelectedValue = _config.PowerPlanOpenApp;
                cboPowerPlanClosedApp.SelectedValue = _config.PowerPlanClosedApp;

                AtualizarGridServicesAdded();
                AtualizarGridServicesOpenCloseAdded();
            }
            catch { }
        }

        /// <summary>
        /// Lê o estado atual da interface e persiste tudo no JSON.
        /// </summary>
        private void SaveConfig()
        {
            AppLogger.Write(AppLogger.LogLevel.Info, "Salvando configuração no arquivo JSON...");

            try
            {
                _config.CheckInterval = (int)nudCheckInterval.Value;
                _config.PowerPlanOpenApp = cboPowerPlanOpenApp.SelectedValue?.ToString() ?? string.Empty;
                _config.PowerPlanClosedApp = cboPowerPlanClosedApp.SelectedValue?.ToString() ?? string.Empty;
                _config.ListServices = _itemsAdded.ToList();
                _config.ListServicesOpenClose = _itemsOpenCloseAdded.ToList();
                _config.ManualExecutablePaths = _manualExecutablePaths
                    .Where(pair => _itemsAdded.Contains(pair.Key, StringComparer.OrdinalIgnoreCase))
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
                _config.OpenCloseExecutablePaths = _openCloseExecutablePaths
                    .Where(pair => _itemsOpenCloseAdded.Contains(pair.Key, StringComparer.OrdinalIgnoreCase))
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

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
        /// <summary>
        /// Preenche a grade da direita com os itens que já estão sendo monitorados.
        /// </summary>
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

        private void ToggleMonitor(ToolStripItem menuItem)
        {
            var mi = (ToolStripMenuItem)menuItem;

            if (mi.Checked)
                StopMonitor();
            else
                StartMonitor();
        }

        private void SyncMonitorToggleMenuItem()
        {
            if (_monitorToggleMenuItem == null)
                return;

            _monitorToggleMenuItem.Checked = _monitorActive;
            _monitorToggleMenuItem.Text = _monitorActive ? "Detecção ativada" : "Detecção ativada";
        }

        private void AtualizarGridServicesOpenCloseAdded()
        {
            string filtro = txtSearchServiceOpenCloseAdded.Text.Trim().ToLower();
            dgvListServicesOpenCloseAdded.Rows.Clear();

            foreach (var item in _itemsOpenCloseAdded)
            {
                string displayName = GetItemDisplayName(item);
                if (displayName.ToLower().Contains(filtro))
                {
                    int rowIndex = dgvListServicesOpenCloseAdded.Rows.Add(displayName);
                    dgvListServicesOpenCloseAdded.Rows[rowIndex].Tag = item;
                }
            }
        }

        /// <summary>
        /// Recarrega a grade da esquerda com tudo que está disponível, exceto o que já foi adicionado.
        /// </summary>
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

        private void AtualizaRefreshOpenClose()
        {
            dgvListServicesOpenClose.Rows.Clear();

            var adicionados = new HashSet<string>(_itemsOpenCloseAdded, StringComparer.OrdinalIgnoreCase);

            foreach (var item in _itemsOpenCloseAvailable)
            {
                if (!adicionados.Contains(item))
                {
                    int rowIndex = dgvListServicesOpenClose.Rows.Add(GetItemDisplayName(item));
                    dgvListServicesOpenClose.Rows[rowIndex].Tag = item;
                }
            }
        }

        /// <summary>
        /// Filtra a lista de itens disponíveis conforme o usuário digita.
        /// </summary>
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

        /// <summary>
        /// O filtro da lista monitorada reaproveita o método central de atualização da grade.
        /// </summary>
        private void txtSearchServiceAdded_TextChanged(object sender, EventArgs e)
        {
            AtualizarGridServicesAdded();
        }

        private void txtSearchServiceOpenClose_TextChanged(object? sender, EventArgs e)
        {
            string filtro = txtSearchServiceOpenClose.Text.Trim().ToLower();
            dgvListServicesOpenClose.Rows.Clear();

            var adicionados = new HashSet<string>(_itemsOpenCloseAdded, StringComparer.OrdinalIgnoreCase);

            foreach (var item in _itemsOpenCloseAvailable)
            {
                string displayName = GetItemDisplayName(item);
                if (!adicionados.Contains(item) && displayName.ToLower().Contains(filtro))
                {
                    int rowIndex = dgvListServicesOpenClose.Rows.Add(displayName);
                    dgvListServicesOpenClose.Rows[rowIndex].Tag = item;
                }
            }
        }

        private void txtSearchServiceOpenCloseAdded_TextChanged(object? sender, EventArgs e)
        {
            AtualizarGridServicesOpenCloseAdded();
        }

        /// <summary>
        /// Clique no botão de adicionar dentro da grade de disponíveis.
        /// </summary>
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

        /// <summary>
        /// Clique no botão de remover dentro da grade de monitorados.
        /// </summary>
        private void dgvListServicesAdded_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;

            var item = dgvListServicesAdded.Rows[e.RowIndex].Tag?.ToString()
                ?? dgvListServicesAdded.Rows[e.RowIndex].Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(item)) return;

            _itemsAdded.RemoveAll(x => string.Equals(x, item, StringComparison.OrdinalIgnoreCase));
            _manualExecutablePaths.Remove(item);
            AtualizarGridServicesAdded();
            AtualizaRefresh();
            SaveConfigAndRefreshMonitorState();
        }

        private void dgvListServicesOpenClose_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;

            var item = dgvListServicesOpenClose.Rows[e.RowIndex].Tag?.ToString()
                ?? dgvListServicesOpenClose.Rows[e.RowIndex].Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(item)) return;

            if (!_itemsOpenCloseAdded.Contains(item, StringComparer.OrdinalIgnoreCase))
            {
                _itemsOpenCloseAdded.Add(item);
                AtualizarGridServicesOpenCloseAdded();
                dgvListServicesOpenClose.Rows.RemoveAt(e.RowIndex);
                SaveConfigAndRefreshMonitorState();
            }
        }

        private void dgvListServicesOpenCloseAdded_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;

            var item = dgvListServicesOpenCloseAdded.Rows[e.RowIndex].Tag?.ToString()
                ?? dgvListServicesOpenCloseAdded.Rows[e.RowIndex].Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(item)) return;

            _itemsOpenCloseAdded.RemoveAll(x => string.Equals(x, item, StringComparison.OrdinalIgnoreCase));
            _openCloseExecutablePaths.Remove(item);
            _closedApplicationsToReopen.Remove(item);
            AtualizarGridServicesOpenCloseAdded();
            AtualizaRefreshOpenClose();
            SaveConfigAndRefreshMonitorState();
        }
        #endregion

        #region Load available items
        /// <summary>
        /// Faz um snapshot dos serviços em execução e dos processos atualmente visíveis no sistema.
        /// </summary>
        private void LoadAvailableItems()
        {
            _itemsAvailable.Clear();
            _itemsOpenCloseAvailable.Clear();
            _itemDisplayNameCache.Clear();

            try
            {
                // Serviços entram pelo ServiceName porque é esse identificador que conseguiremos consultar depois.
                var runningServices = ServiceController.GetServices()
                    .Where(s => s.Status == ServiceControllerStatus.Running)
                    .Select(s => s.ServiceName);
                _itemsAvailable.AddRange(runningServices);
            }
            catch { }

            try
            {
                // Para processos, usamos o nome do processo pois é o critério mais estável para reconsulta.
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        string processName = process.ProcessName;
                        if (string.IsNullOrWhiteSpace(processName))
                            continue;

                        _itemsAvailable.Add(processName);
                        _itemsOpenCloseAvailable.Add(processName);

                        string? fileName = process.MainModule?.FileName;
                        if (!string.IsNullOrWhiteSpace(fileName) &&
                            File.Exists(fileName) &&
                            !_openCloseExecutablePaths.ContainsKey(processName))
                        {
                            _openCloseExecutablePaths[processName] = fileName;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // Distinct remove duplicatas entre serviços/processos e OrderBy deixa a seleção mais amigável.
            _itemsAvailable = _itemsAvailable.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();
            _itemsOpenCloseAvailable = _itemsOpenCloseAvailable.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();
            PopulateDisplayNameCache();
            PopulateManualExecutableDisplayCache();
            PopulateOpenCloseExecutableDisplayCache();
        }

        /// <summary>
        /// Traduz um identificador técnico para um nome mais amigável sem perder o valor real monitorado.
        /// </summary>
        private string GetItemDisplayName(string item)
        {
            if (string.IsNullOrWhiteSpace(item))
                return string.Empty;

            if (_itemDisplayNameCache.TryGetValue(item, out var cachedDisplayName))
                return cachedDisplayName;

            try
            {
                // Se abrir como serviço, mantemos o nome original porque ele já é o identificador correto.
                using var sc = new ServiceController(item);
                _ = sc.Status;
                return _itemDisplayNameCache[item] = item;
            }
            catch { }

            // Alguns itens podem carregar "processo|trecho da janela" para distinguir instâncias específicas.
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

        /// <summary>
        /// Pré-aquece o cache de nomes amigáveis para reduzir custo durante filtros e refresh da UI.
        /// </summary>
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

        /// <summary>
        /// Tenta obter um nome mais legível do processo a partir de metadados do executável.
        /// </summary>
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
        /// <summary>
        /// Representa uma opção retornada pelo comando powercfg.
        /// </summary>
        public class PowerPlan
        {
            public string Name { get; set; } = string.Empty;
            public string Guid { get; set; } = string.Empty;
            public override string ToString() => $"{Name} ({Guid})";
        }

        /// <summary>
        /// Consulta os planos de energia disponíveis no Windows via powercfg /list.
        /// </summary>
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

                // O output é texto livre; por isso fazemos um parse simples procurando GUID + nome.
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

        /// <summary>
        /// Ativa um plano de energia pelo GUID usando a ferramenta nativa do Windows.
        /// </summary>
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

        /// <summary>
        /// Descobre qual plano está ativo agora para podermos restaurá-lo depois.
        /// </summary>
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

        /// <summary>
        /// Guarda o plano original apenas uma vez por ciclo de monitoramento.
        /// </summary>
        private void EnsureOriginalPowerPlanCaptured()
        {
            if (!string.IsNullOrWhiteSpace(_planoEnergiaOriginalGuid))
                return;

            _planoEnergiaOriginalGuid = GetActivePowerPlanGuid();

            if (!string.IsNullOrWhiteSpace(_planoEnergiaOriginalGuid))
                AppLogger.Write(AppLogger.LogLevel.Info, $"Plano de energia original capturado: {_planoEnergiaOriginalGuid}");
        }

        /// <summary>
        /// Ao parar o monitor, devolve o computador para o plano que estava ativo antes da intervenção do app.
        /// </summary>
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
        /// <summary>
        /// Sobe o loop assíncrono que vigia serviços/processos e decide qual plano de energia ativar.
        /// </summary>
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

                // Fazemos uma avaliação imediata para aplicar o estado correto sem esperar o primeiro intervalo.
                EvaluateMonitorStateAsync(_ctsMonitor.Token).GetAwaiter().GetResult();
                _monitorActive = true;
                SyncMonitorToggleMenuItem();
                _monitorTask = Task.Run(() => MonitorLoopAsync(_ctsMonitor.Token));
                _trayIcon?.ShowBalloonTip(1000, "ThGameMode", "Detecção ativada", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Error, "Erro ao carregar configuração antes de iniciar o monitor: " + ex.Message);
                MessageBox.Show("Erro ao carregar configuração: " + ex.Message);
            }
        }

        /// <summary>
        /// Cancela o monitor e normaliza o estado visual/energético ao encerrar a detecção.
        /// </summary>
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
                ReopenClosedApplications();
                RestoreOriginalPowerPlan();
                _monitorActive = false;
                _modoAltoAtivo = false;
                SyncMonitorToggleMenuItem();

                if (!_quitRequested && _trayIcon != null)
                {
                    _trayIcon.ShowBalloonTip(1000, "ThGameMode", "Detecção desativada", ToolTipIcon.Info);
                    _trayIcon.Icon = _iconPadrao;
                    _trayIcon.Text = "ThGameMode — Desativado";
                }
            }
        }

        /// <summary>
        /// Loop principal do monitor; reavalia o estado em intervalos definidos pelo usuário.
        /// </summary>
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

                // Nunca deixamos o intervalo cair abaixo de 1 segundo para evitar loop agressivo.
                int intervalo = Math.Max(1, _config.CheckInterval);
                try { await Task.Delay(intervalo * 1000, token); }
                catch (OperationCanceledException) { break; }
            }
        }

        /// <summary>
        /// Verifica se o item configurado está ativo, tratando tanto serviços quanto processos.
        /// </summary>
        private Task<bool> IsItemRunningAsync(string item)
        {
            try
            {
                // Primeiro tentamos como serviço porque essa consulta é direta e barata.
                using var sc = new ServiceController(item);
                return Task.FromResult(sc.Status == ServiceControllerStatus.Running);
            }
            catch { }

            // Quando o formato é "processo|janela", usamos o nome do processo e um filtro opcional por título.
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
                if (processos.Length == 0) return Task.FromResult(false);

                if (string.IsNullOrEmpty(windowText)) return Task.FromResult(true);

                foreach (var p in processos)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(p.MainWindowTitle) &&
                            p.MainWindowTitle.IndexOf(windowText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return Task.FromResult(true);
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Decide se deve ficar em alto desempenho ou economia com base nos itens monitorados.
        /// </summary>
        private async Task EvaluateMonitorStateAsync(CancellationToken token)
        {
            bool itemRodando = false;
            string? itemDetectado = null;

            // Basta um item monitorado estar ativo para entrar no modo de alto desempenho.
            foreach (var item in _config.ListServices ?? Enumerable.Empty<string>())
            {
                token.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(item)) continue;

                bool rodando = await IsItemRunningAsync(item);
                if (rodando)
                {
                    itemRodando = true;
                    itemDetectado = item;
                    break;
                }
            }

            if (itemRodando && !_modoAltoAtivo)
            {
                EnsureOriginalPowerPlanCaptured();
                if (!string.IsNullOrWhiteSpace(itemDetectado))
                    AppLogger.Write(AppLogger.LogLevel.Info, $"Item detectado em execução: {itemDetectado}");
                TrocarPlano(_config.PowerPlanOpenApp);
                CloseConfiguredApplications();
                _modoAltoAtivo = true;
                UpdateTrayStatus(true, itemDetectado is null ? string.Empty : GetItemDisplayName(itemDetectado));
            }
            else if (!itemRodando && _modoAltoAtivo)
            {
                // Ao sair do modo de alto desempenho, aplicamos o plano escolhido para o estado ocioso.
                EnsureOriginalPowerPlanCaptured();
                TrocarPlano(_config.PowerPlanClosedApp);
                ReopenClosedApplications();
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
        }

        private void CloseConfiguredApplications()
        {
            foreach (var item in _config.ListServicesOpenClose ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(item) || _closedApplicationsToReopen.Contains(item))
                    continue;

                if (CloseApplicationProcesses(item))
                {
                    _closedApplicationsToReopen.Add(item);
                    AppLogger.Write(AppLogger.LogLevel.Info, $"Aplicativo fechado para o modo de alto desempenho: {item}");
                }
            }
        }

        private void ReopenClosedApplications()
        {
            foreach (var item in _closedApplicationsToReopen.ToList())
            {
                try
                {
                    if (IsItemRunningAsync(item).GetAwaiter().GetResult())
                    {
                        _closedApplicationsToReopen.Remove(item);
                        continue;
                    }

                    if (!TryGetExecutablePathForOpenCloseItem(item, out string executablePath))
                    {
                        AppLogger.Write(AppLogger.LogLevel.Warning, $"Sem caminho conhecido para reabrir o aplicativo: {item}");
                        continue;
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = executablePath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory
                    });

                    _closedApplicationsToReopen.Remove(item);
                    AppLogger.Write(AppLogger.LogLevel.Info, $"Aplicativo reaberto ao voltar para economia: {item}");
                }
                catch (Exception ex)
                {
                    AppLogger.Write(AppLogger.LogLevel.Error, $"Erro ao reabrir aplicativo '{item}': {ex.Message}");
                }
            }
        }

        private bool CloseApplicationProcesses(string item)
        {
            string processName = item.Contains('|') ? item.Split('|', 2)[0] : item;
            bool closedAny = false;

            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName(processName);
            }
            catch
            {
                return false;
            }

            foreach (var process in processes)
            {
                try
                {
                    if (process.Id == Environment.ProcessId || process.HasExited)
                        continue;

                    string? fileName = null;
                    try { fileName = process.MainModule?.FileName; } catch { }

                    if (!string.IsNullOrWhiteSpace(fileName) &&
                        File.Exists(fileName) &&
                        !_openCloseExecutablePaths.ContainsKey(processName))
                    {
                        _openCloseExecutablePaths[processName] = fileName;
                    }

                    bool exited = false;

                    if (!string.IsNullOrWhiteSpace(process.MainWindowTitle))
                        exited = process.CloseMainWindow() && process.WaitForExit(5000);

                    if (!exited)
                    {
                        process.Kill(true);
                        exited = process.WaitForExit(5000);
                    }

                    closedAny |= exited;
                }
                catch (Exception ex)
                {
                    AppLogger.Write(AppLogger.LogLevel.Warning, $"Erro ao fechar aplicativo '{processName}': {ex.Message}");
                }
            }

            return closedAny;
        }

        private bool TryGetExecutablePathForOpenCloseItem(string item, out string executablePath)
        {
            executablePath = string.Empty;

            if (_openCloseExecutablePaths.TryGetValue(item, out string? directPath) && !string.IsNullOrWhiteSpace(directPath) && File.Exists(directPath))
            {
                executablePath = directPath;
                return true;
            }

            string processName = item.Contains('|') ? item.Split('|', 2)[0] : item;
            if (_openCloseExecutablePaths.TryGetValue(processName, out string? processPath) && !string.IsNullOrWhiteSpace(processPath) && File.Exists(processPath))
            {
                executablePath = processPath;
                return true;
            }

            executablePath = string.Empty;
            return false;
        }
        #endregion

        #region Save & Restart Monitor
        /// <summary>
        /// Evento do botão Salvar.
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveConfigAndRestartMonitor();
        }

        /// <summary>
        /// Reinicia o monitor para garantir que novas configurações passem a valer imediatamente.
        /// </summary>
        private void SaveConfigAndRestartMonitor()
        {
            SaveConfig();
            StopMonitor();
            LoadAvailableItems();
            AtualizaRefresh();
            AtualizaRefreshOpenClose();
            StartMonitor();
        }

        /// <summary>
        /// Salva e força uma reavaliação instantânea sem derrubar o loop quando ele já está ativo.
        /// </summary>
        private void SaveConfigAndRefreshMonitorState()
        {
            SaveConfig();

            if (_monitorActive)
                EvaluateMonitorStateAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        #endregion

        #region Buttons e Refresh
        /// <summary>
        /// Recarrega a lista de itens detectados no sistema.
        /// </summary>
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadAvailableItems();
            AtualizaRefresh();
            AtualizaRefreshOpenClose();
        }

        private void btnRefreshOpenClose_Click(object? sender, EventArgs e)
        {
            LoadAvailableItems();
            AtualizaRefreshOpenClose();
        }
        #endregion

        #region Dispose / FormClosing
        /// <summary>
        /// Fechar a janela normalmente apenas minimiza para o tray; sair de verdade depende da flag _quitRequested.
        /// </summary>
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
