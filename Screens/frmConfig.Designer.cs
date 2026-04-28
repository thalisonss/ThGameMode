namespace ThGameMode.Screens
{
    partial class frmConfig
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmConfig));
            btnInstallUninstall = new Button();
            cboLanguage = new ComboBox();
            lblLanguage = new Label();
            lblCheckInterval = new Label();
            nudCheckInterval = new NumericUpDown();
            dgvListServicesAdded = new DataGridView();
            dgvColumnNameAdded = new DataGridViewTextBoxColumn();
            dgvColumnDeleteAdded = new DataGridViewImageColumn();
            txtSearchServiceAdded = new TextBox();
            dgvListServices = new DataGridView();
            dgvColumnName = new DataGridViewTextBoxColumn();
            dgvColumnAdd = new DataGridViewImageColumn();
            txtSearchService = new TextBox();
            btnRefresh = new Button();
            progressBarConfig = new ProgressBar();
            btnSave = new Button();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            groupBox1 = new GroupBox();
            txtSearchServiceOpenClose = new TextBox();
            dgvListServicesOpenCloseAdded = new DataGridView();
            dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
            dataGridViewImageColumn2 = new DataGridViewImageColumn();
            button1 = new Button();
            txtSearchServiceOpenCloseAdded = new TextBox();
            dgvListServicesOpenClose = new DataGridView();
            dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
            dataGridViewImageColumn1 = new DataGridViewImageColumn();
            btnRefreshOpenClose = new Button();
            cboPowerPlanClosedApp = new ComboBox();
            lblPowerPlanClosedApp = new Label();
            lblPowerPlanOpenApp = new Label();
            cboPowerPlanOpenApp = new ComboBox();
            notifyIcon = new NotifyIcon(components);
            contextMenuStrip = new ContextMenuStrip(components);
            ativarDeteccaoToolStripMenuItem = new ToolStripMenuItem();
            desativarDeteccaoToolStripMenuItem = new ToolStripMenuItem();
            iniciarServicoToolStripMenuItem = new ToolStripMenuItem();
            pararServicoToolStripMenuItem = new ToolStripMenuItem();
            abrirConfiguracoesToolStripMenuItem = new ToolStripMenuItem();
            sairToolStripMenuItem = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)nudCheckInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvListServicesAdded).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvListServices).BeginInit();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvListServicesOpenCloseAdded).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvListServicesOpenClose).BeginInit();
            contextMenuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // btnInstallUninstall
            // 
            resources.ApplyResources(btnInstallUninstall, "btnInstallUninstall");
            btnInstallUninstall.Name = "btnInstallUninstall";
            btnInstallUninstall.UseVisualStyleBackColor = true;
            // 
            // cboLanguage
            // 
            resources.ApplyResources(cboLanguage, "cboLanguage");
            cboLanguage.FormattingEnabled = true;
            cboLanguage.Name = "cboLanguage";
            // 
            // lblLanguage
            // 
            resources.ApplyResources(lblLanguage, "lblLanguage");
            lblLanguage.Name = "lblLanguage";
            // 
            // lblCheckInterval
            // 
            resources.ApplyResources(lblCheckInterval, "lblCheckInterval");
            lblCheckInterval.Name = "lblCheckInterval";
            // 
            // nudCheckInterval
            // 
            resources.ApplyResources(nudCheckInterval, "nudCheckInterval");
            nudCheckInterval.Name = "nudCheckInterval";
            // 
            // dgvListServicesAdded
            // 
            dgvListServicesAdded.AllowUserToAddRows = false;
            dgvListServicesAdded.AllowUserToDeleteRows = false;
            dgvListServicesAdded.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            resources.ApplyResources(dgvListServicesAdded, "dgvListServicesAdded");
            dgvListServicesAdded.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvListServicesAdded.Columns.AddRange(new DataGridViewColumn[] { dgvColumnNameAdded, dgvColumnDeleteAdded });
            dgvListServicesAdded.Name = "dgvListServicesAdded";
            dgvListServicesAdded.ReadOnly = true;
            dgvListServicesAdded.CellClick += dgvListServicesAdded_CellClick;
            // 
            // dgvColumnNameAdded
            // 
            resources.ApplyResources(dgvColumnNameAdded, "dgvColumnNameAdded");
            dgvColumnNameAdded.FillWeight = 85F;
            dgvColumnNameAdded.Name = "dgvColumnNameAdded";
            dgvColumnNameAdded.ReadOnly = true;
            // 
            // dgvColumnDeleteAdded
            // 
            resources.ApplyResources(dgvColumnDeleteAdded, "dgvColumnDeleteAdded");
            dgvColumnDeleteAdded.FillWeight = 15F;
            dgvColumnDeleteAdded.Name = "dgvColumnDeleteAdded";
            dgvColumnDeleteAdded.ReadOnly = true;
            // 
            // txtSearchServiceAdded
            // 
            resources.ApplyResources(txtSearchServiceAdded, "txtSearchServiceAdded");
            txtSearchServiceAdded.Name = "txtSearchServiceAdded";
            txtSearchServiceAdded.TextChanged += txtSearchServiceAdded_TextChanged;
            // 
            // dgvListServices
            // 
            dgvListServices.AllowUserToAddRows = false;
            dgvListServices.AllowUserToDeleteRows = false;
            dgvListServices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            resources.ApplyResources(dgvListServices, "dgvListServices");
            dgvListServices.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvListServices.Columns.AddRange(new DataGridViewColumn[] { dgvColumnName, dgvColumnAdd });
            dgvListServices.Name = "dgvListServices";
            dgvListServices.ReadOnly = true;
            dgvListServices.CellClick += dgvListServices_CellClick;
            // 
            // dgvColumnName
            // 
            resources.ApplyResources(dgvColumnName, "dgvColumnName");
            dgvColumnName.FillWeight = 85F;
            dgvColumnName.Name = "dgvColumnName";
            dgvColumnName.ReadOnly = true;
            // 
            // dgvColumnAdd
            // 
            resources.ApplyResources(dgvColumnAdd, "dgvColumnAdd");
            dgvColumnAdd.FillWeight = 15F;
            dgvColumnAdd.Name = "dgvColumnAdd";
            dgvColumnAdd.ReadOnly = true;
            // 
            // txtSearchService
            // 
            resources.ApplyResources(txtSearchService, "txtSearchService");
            txtSearchService.Name = "txtSearchService";
            txtSearchService.TextChanged += txtSearchService_TextChanged;
            // 
            // btnRefresh
            // 
            resources.ApplyResources(btnRefresh, "btnRefresh");
            btnRefresh.Name = "btnRefresh";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // progressBarConfig
            // 
            resources.ApplyResources(progressBarConfig, "progressBarConfig");
            progressBarConfig.Name = "progressBarConfig";
            // 
            // btnSave
            // 
            resources.ApplyResources(btnSave, "btnSave");
            btnSave.Name = "btnSave";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // tabControl1
            // 
            resources.ApplyResources(tabControl1, "tabControl1");
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(groupBox1);
            tabPage1.Controls.Add(cboPowerPlanClosedApp);
            tabPage1.Controls.Add(lblPowerPlanClosedApp);
            tabPage1.Controls.Add(lblPowerPlanOpenApp);
            tabPage1.Controls.Add(cboPowerPlanOpenApp);
            tabPage1.Controls.Add(txtSearchService);
            tabPage1.Controls.Add(btnInstallUninstall);
            tabPage1.Controls.Add(btnSave);
            tabPage1.Controls.Add(nudCheckInterval);
            tabPage1.Controls.Add(dgvListServices);
            tabPage1.Controls.Add(lblCheckInterval);
            tabPage1.Controls.Add(btnRefresh);
            tabPage1.Controls.Add(txtSearchServiceAdded);
            tabPage1.Controls.Add(dgvListServicesAdded);
            resources.ApplyResources(tabPage1, "tabPage1");
            tabPage1.Name = "tabPage1";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            resources.ApplyResources(groupBox1, "groupBox1");
            groupBox1.Controls.Add(txtSearchServiceOpenClose);
            groupBox1.Controls.Add(dgvListServicesOpenCloseAdded);
            groupBox1.Controls.Add(button1);
            groupBox1.Controls.Add(txtSearchServiceOpenCloseAdded);
            groupBox1.Controls.Add(dgvListServicesOpenClose);
            groupBox1.Controls.Add(btnRefreshOpenClose);
            groupBox1.Name = "groupBox1";
            groupBox1.TabStop = false;
            // 
            // txtSearchServiceOpenClose
            // 
            resources.ApplyResources(txtSearchServiceOpenClose, "txtSearchServiceOpenClose");
            txtSearchServiceOpenClose.Name = "txtSearchServiceOpenClose";
            // 
            // dgvListServicesOpenCloseAdded
            // 
            dgvListServicesOpenCloseAdded.AllowUserToAddRows = false;
            dgvListServicesOpenCloseAdded.AllowUserToDeleteRows = false;
            dgvListServicesOpenCloseAdded.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            resources.ApplyResources(dgvListServicesOpenCloseAdded, "dgvListServicesOpenCloseAdded");
            dgvListServicesOpenCloseAdded.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvListServicesOpenCloseAdded.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn2, dataGridViewImageColumn2 });
            dgvListServicesOpenCloseAdded.Name = "dgvListServicesOpenCloseAdded";
            dgvListServicesOpenCloseAdded.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            resources.ApplyResources(dataGridViewTextBoxColumn2, "dataGridViewTextBoxColumn2");
            dataGridViewTextBoxColumn2.FillWeight = 85F;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // dataGridViewImageColumn2
            // 
            resources.ApplyResources(dataGridViewImageColumn2, "dataGridViewImageColumn2");
            dataGridViewImageColumn2.FillWeight = 15F;
            dataGridViewImageColumn2.Name = "dataGridViewImageColumn2";
            dataGridViewImageColumn2.ReadOnly = true;
            // 
            // button1
            // 
            resources.ApplyResources(button1, "button1");
            button1.Name = "button1";
            button1.UseVisualStyleBackColor = true;
            // 
            // txtSearchServiceOpenCloseAdded
            // 
            resources.ApplyResources(txtSearchServiceOpenCloseAdded, "txtSearchServiceOpenCloseAdded");
            txtSearchServiceOpenCloseAdded.Name = "txtSearchServiceOpenCloseAdded";
            // 
            // dgvListServicesOpenClose
            // 
            dgvListServicesOpenClose.AllowUserToAddRows = false;
            dgvListServicesOpenClose.AllowUserToDeleteRows = false;
            dgvListServicesOpenClose.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            resources.ApplyResources(dgvListServicesOpenClose, "dgvListServicesOpenClose");
            dgvListServicesOpenClose.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvListServicesOpenClose.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn1, dataGridViewImageColumn1 });
            dgvListServicesOpenClose.Name = "dgvListServicesOpenClose";
            dgvListServicesOpenClose.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn1
            // 
            resources.ApplyResources(dataGridViewTextBoxColumn1, "dataGridViewTextBoxColumn1");
            dataGridViewTextBoxColumn1.FillWeight = 85F;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // dataGridViewImageColumn1
            // 
            resources.ApplyResources(dataGridViewImageColumn1, "dataGridViewImageColumn1");
            dataGridViewImageColumn1.FillWeight = 15F;
            dataGridViewImageColumn1.Name = "dataGridViewImageColumn1";
            dataGridViewImageColumn1.ReadOnly = true;
            // 
            // btnRefreshOpenClose
            // 
            resources.ApplyResources(btnRefreshOpenClose, "btnRefreshOpenClose");
            btnRefreshOpenClose.Name = "btnRefreshOpenClose";
            btnRefreshOpenClose.UseVisualStyleBackColor = true;
            // 
            // cboPowerPlanClosedApp
            // 
            cboPowerPlanClosedApp.FormattingEnabled = true;
            resources.ApplyResources(cboPowerPlanClosedApp, "cboPowerPlanClosedApp");
            cboPowerPlanClosedApp.Name = "cboPowerPlanClosedApp";
            // 
            // lblPowerPlanClosedApp
            // 
            resources.ApplyResources(lblPowerPlanClosedApp, "lblPowerPlanClosedApp");
            lblPowerPlanClosedApp.Name = "lblPowerPlanClosedApp";
            // 
            // lblPowerPlanOpenApp
            // 
            resources.ApplyResources(lblPowerPlanOpenApp, "lblPowerPlanOpenApp");
            lblPowerPlanOpenApp.Name = "lblPowerPlanOpenApp";
            // 
            // cboPowerPlanOpenApp
            // 
            cboPowerPlanOpenApp.FormattingEnabled = true;
            resources.ApplyResources(cboPowerPlanOpenApp, "cboPowerPlanOpenApp");
            cboPowerPlanOpenApp.Name = "cboPowerPlanOpenApp";
            // 
            // notifyIcon
            // 
            notifyIcon.ContextMenuStrip = contextMenuStrip;
            resources.ApplyResources(notifyIcon, "notifyIcon");
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.Items.AddRange(new ToolStripItem[] { ativarDeteccaoToolStripMenuItem, desativarDeteccaoToolStripMenuItem, iniciarServicoToolStripMenuItem, pararServicoToolStripMenuItem, abrirConfiguracoesToolStripMenuItem, sairToolStripMenuItem });
            contextMenuStrip.Name = "contextMenuStrip";
            resources.ApplyResources(contextMenuStrip, "contextMenuStrip");
            // 
            // ativarDeteccaoToolStripMenuItem
            // 
            ativarDeteccaoToolStripMenuItem.Name = "ativarDeteccaoToolStripMenuItem";
            resources.ApplyResources(ativarDeteccaoToolStripMenuItem, "ativarDeteccaoToolStripMenuItem");
            // 
            // desativarDeteccaoToolStripMenuItem
            // 
            desativarDeteccaoToolStripMenuItem.Name = "desativarDeteccaoToolStripMenuItem";
            resources.ApplyResources(desativarDeteccaoToolStripMenuItem, "desativarDeteccaoToolStripMenuItem");
            // 
            // iniciarServicoToolStripMenuItem
            // 
            iniciarServicoToolStripMenuItem.Name = "iniciarServicoToolStripMenuItem";
            resources.ApplyResources(iniciarServicoToolStripMenuItem, "iniciarServicoToolStripMenuItem");
            // 
            // pararServicoToolStripMenuItem
            // 
            pararServicoToolStripMenuItem.Name = "pararServicoToolStripMenuItem";
            resources.ApplyResources(pararServicoToolStripMenuItem, "pararServicoToolStripMenuItem");
            // 
            // abrirConfiguracoesToolStripMenuItem
            // 
            abrirConfiguracoesToolStripMenuItem.Name = "abrirConfiguracoesToolStripMenuItem";
            resources.ApplyResources(abrirConfiguracoesToolStripMenuItem, "abrirConfiguracoesToolStripMenuItem");
            // 
            // sairToolStripMenuItem
            // 
            sairToolStripMenuItem.Name = "sairToolStripMenuItem";
            resources.ApplyResources(sairToolStripMenuItem, "sairToolStripMenuItem");
            // 
            // frmConfig
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tabControl1);
            Controls.Add(progressBarConfig);
            Controls.Add(lblLanguage);
            Controls.Add(cboLanguage);
            Name = "frmConfig";
            Load += frmConfig_Load;
            ((System.ComponentModel.ISupportInitialize)nudCheckInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvListServicesAdded).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvListServices).EndInit();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvListServicesOpenCloseAdded).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvListServicesOpenClose).EndInit();
            contextMenuStrip.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnInstallUninstall;
        private ComboBox cboLanguage;
        private Label lblLanguage;
        private Label lblCheckInterval;
        private NumericUpDown nudCheckInterval;
        private DataGridView dgvListServicesAdded;
        private TextBox txtSearchServiceAdded;
        private DataGridView dgvListServices;
        private TextBox txtSearchService;
        private Button btnRefresh;
        private DataGridViewTextBoxColumn dgvColumnNameAdded;
        private DataGridViewImageColumn dgvColumnDeleteAdded;
        private DataGridViewTextBoxColumn dgvColumnName;
        private DataGridViewImageColumn dgvColumnAdd;
        private ProgressBar progressBarConfig;
        private Button btnSave;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private ComboBox cboPowerPlanClosedApp;
        private Label lblPowerPlanClosedApp;
        private Label lblPowerPlanOpenApp;
        private ComboBox cboPowerPlanOpenApp;
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenuStrip;
        private ToolStripMenuItem ativarDeteccaoToolStripMenuItem;
        private ToolStripMenuItem desativarDeteccaoToolStripMenuItem;
        private ToolStripMenuItem iniciarServicoToolStripMenuItem;
        private ToolStripMenuItem pararServicoToolStripMenuItem;
        private ToolStripMenuItem abrirConfiguracoesToolStripMenuItem;
        private ToolStripMenuItem sairToolStripMenuItem;
        private TextBox txtSearchServiceOpenClose;
        private Button button1;
        private DataGridView dgvListServicesOpenClose;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewImageColumn dataGridViewImageColumn1;
        private Button btnRefreshOpenClose;
        private TextBox txtSearchServiceOpenCloseAdded;
        private DataGridView dgvListServicesOpenCloseAdded;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private DataGridViewImageColumn dataGridViewImageColumn2;
        private GroupBox groupBox1;
    }
}
