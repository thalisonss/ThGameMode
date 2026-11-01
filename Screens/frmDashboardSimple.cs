using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThGameMode.Services;
using ThGameMode.Services;

namespace ThGameMode.Screens
{
    public partial class frmDashboardSimple : Form
    {
        private DashboardToggleService _dashboardService;
        private Button btnLigar;
        private Button btnDesligar;
        private Label lblStatus;
        private System.Windows.Forms.Timer timerStatus;

        public frmDashboardSimple()
        {
            _dashboardService = new DashboardToggleService();
            InitializeComponent();
            IniciarTimerStatus();
        }

        private void InitializeComponent()
        {
            this.lblStatus = new Label();
            this.btnLigar = new Button();
            this.btnDesligar = new Button();

            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblStatus.Location = new System.Drawing.Point(15, 15);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(120, 20);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "Status: Desligado";

            this.btnLigar.Location = new System.Drawing.Point(15, 45);
            this.btnLigar.Name = "btnLigar";
            this.btnLigar.Size = new System.Drawing.Size(130, 40);
            this.btnLigar.TabIndex = 1;
            this.btnLigar.Text = "Ligar Dashboard";
            this.btnLigar.UseVisualStyleBackColor = true;
            this.btnLigar.Click += new System.EventHandler(this.btnLigar_Click);

            this.btnDesligar.Location = new System.Drawing.Point(15, 95);
            this.btnDesligar.Name = "btnDesligar";
            this.btnDesligar.Size = new System.Drawing.Size(130, 40);
            this.btnDesligar.TabIndex = 2;
            this.btnDesligar.Text = "Desligar Dashboard";
            this.btnDesligar.UseVisualStyleBackColor = true;
            this.btnDesligar.Click += new System.EventHandler(this.btnDesligar_Click);

            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnLigar);
            this.Controls.Add(this.btnDesligar);

            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(170, 140);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmDashboardSimple";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Dashboard PC";
        }

        private async void btnLigar_Click(object sender, EventArgs e)
        {
            btnLigar.Enabled = false;
            await _dashboardService.LigarDashboard();
            btnLigar.Text = "Aberto!";
            AtualizarStatus();
        }

        private void btnDesligar_Click(object sender, EventArgs e)
        {
            _dashboardService.DesligarDashboard();
            btnLigar.Text = "Ligar Dashboard";
            btnLigar.Enabled = true;
            AtualizarStatus();
        }

        private void IniciarTimerStatus()
        {
            timerStatus = new System.Windows.Forms.Timer();
            timerStatus.Interval = 2000;
            timerStatus.Tick += (s, e) => AtualizarStatus();
            timerStatus.Start();
        }

        private void AtualizarStatus()
        {
            lblStatus.Text = _dashboardService.DashboardEstaAtivo() 
                ? "Status: Ligado" 
                : "Status: Desligado";
            lblStatus.ForeColor = _dashboardService.DashboardEstaAtivo() 
                ? System.Drawing.Color.Green 
                : System.Drawing.Color.Red;
            
            btnLigar.Enabled = !_dashboardService.DashboardEstaAtivo();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            AtualizarStatus();
        }
    }
}