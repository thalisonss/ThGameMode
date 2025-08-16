namespace Un_InstallThGameMode.Screens
{
    partial class frmInstall
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
            txtInstallPath = new TextBox();
            btnBrowse = new Button();
            btnInstall = new Button();
            label1 = new Label();
            btnUninstall = new Button();
            SuspendLayout();
            // 
            // txtInstallPath
            // 
            txtInstallPath.Location = new Point(44, 62);
            txtInstallPath.Name = "txtInstallPath";
            txtInstallPath.Size = new Size(214, 23);
            txtInstallPath.TabIndex = 0;
            txtInstallPath.TextChanged += txtInstallPath_TextChanged;
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(293, 62);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(75, 23);
            btnBrowse.TabIndex = 1;
            btnBrowse.Text = "Change";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;
            // 
            // btnInstall
            // 
            btnInstall.Location = new Point(168, 136);
            btnInstall.Name = "btnInstall";
            btnInstall.Size = new Size(75, 23);
            btnInstall.TabIndex = 2;
            btnInstall.Text = "Instalar";
            btnInstall.UseVisualStyleBackColor = true;
            btnInstall.Click += btnInstall_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(44, 44);
            label1.Name = "label1";
            label1.Size = new Size(117, 15);
            label1.TabIndex = 5;
            label1.Text = "Installation Location:";
            // 
            // btnUninstall
            // 
            btnUninstall.Location = new Point(168, 107);
            btnUninstall.Name = "btnUninstall";
            btnUninstall.Size = new Size(75, 23);
            btnUninstall.TabIndex = 9;
            btnUninstall.Text = "Desinstalar";
            btnUninstall.UseVisualStyleBackColor = true;
            btnUninstall.Click += btnUninstall_Click;
            // 
            // frmInstall
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(416, 189);
            Controls.Add(btnUninstall);
            Controls.Add(label1);
            Controls.Add(btnInstall);
            Controls.Add(btnBrowse);
            Controls.Add(txtInstallPath);
            Name = "frmInstall";
            Text = "Install ThGameMode";
            Load += frmInstall_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtInstallPath;
        private Button btnBrowse;
        private Button btnInstall;
        private Label label1;
        private Button btnUninstall;
    }
}