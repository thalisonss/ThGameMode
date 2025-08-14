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
            cboPowerPlanClosedApp = new ComboBox();
            lblPowerPlanClosedApp = new Label();
            lblPowerPlanOpenApp = new Label();
            cboPowerPlanOpenApp = new ComboBox();
            ((System.ComponentModel.ISupportInitialize)nudCheckInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvListServicesAdded).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvListServices).BeginInit();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            SuspendLayout();
            // 
            // btnInstallUninstall
            // 
            resources.ApplyResources(btnInstallUninstall, "btnInstallUninstall");
            btnInstallUninstall.Name = "btnInstallUninstall";
            btnInstallUninstall.UseVisualStyleBackColor = true;
            btnInstallUninstall.Click += btnInstallUninstall_Click;
            // 
            // cboLanguage
            // 
            cboLanguage.FormattingEnabled = true;
            resources.ApplyResources(cboLanguage, "cboLanguage");
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
            dgvListServicesAdded.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvListServicesAdded.Columns.AddRange(new DataGridViewColumn[] { dgvColumnNameAdded, dgvColumnDeleteAdded });
            resources.ApplyResources(dgvListServicesAdded, "dgvListServicesAdded");
            dgvListServicesAdded.Name = "dgvListServicesAdded";
            dgvListServicesAdded.ReadOnly = true;
            dgvListServicesAdded.CellClick += dgvListServicesAdded_CellClick;
            // 
            // dgvColumnNameAdded
            // 
            resources.ApplyResources(dgvColumnNameAdded, "dgvColumnNameAdded");
            dgvColumnNameAdded.Name = "dgvColumnNameAdded";
            dgvColumnNameAdded.ReadOnly = true;
            // 
            // dgvColumnDeleteAdded
            // 
            resources.ApplyResources(dgvColumnDeleteAdded, "dgvColumnDeleteAdded");
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
            dgvListServices.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvListServices.Columns.AddRange(new DataGridViewColumn[] { dgvColumnName, dgvColumnAdd });
            resources.ApplyResources(dgvListServices, "dgvListServices");
            dgvListServices.Name = "dgvListServices";
            dgvListServices.ReadOnly = true;
            dgvListServices.CellClick += dgvListServices_CellClick;
            // 
            // dgvColumnName
            // 
            resources.ApplyResources(dgvColumnName, "dgvColumnName");
            dgvColumnName.Name = "dgvColumnName";
            dgvColumnName.ReadOnly = true;
            // 
            // dgvColumnAdd
            // 
            resources.ApplyResources(dgvColumnAdd, "dgvColumnAdd");
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
            tabControl1.Controls.Add(tabPage1);
            resources.ApplyResources(tabControl1, "tabControl1");
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(cboPowerPlanClosedApp);
            tabPage1.Controls.Add(lblPowerPlanClosedApp);
            tabPage1.Controls.Add(lblPowerPlanOpenApp);
            tabPage1.Controls.Add(cboPowerPlanOpenApp);
            tabPage1.Controls.Add(txtSearchService);
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
            // frmConfig
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tabControl1);
            Controls.Add(progressBarConfig);
            Controls.Add(lblLanguage);
            Controls.Add(cboLanguage);
            Controls.Add(btnInstallUninstall);
            Name = "frmConfig";
            Load += frmConfig_Load;
            ((System.ComponentModel.ISupportInitialize)nudCheckInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvListServicesAdded).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvListServices).EndInit();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
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
    }
}