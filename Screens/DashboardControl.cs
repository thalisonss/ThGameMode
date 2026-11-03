using System;
using System.Windows.Forms;
using ThGameMode.Services;

namespace ThGameMode.Screens
{
    public class DashboardControl : UserControl
    {
        private readonly DashboardToggleService _dashboardService = new DashboardToggleService();
        private Button btnToggleDashboard;
        private Label lblDashboardStatus;

        public DashboardControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new System.Drawing.Size(400, 150);
            this.lblDashboardStatus = new Label();
            this.btnToggleDashboard = new Button();

            this.lblDashboardStatus.AutoSize = true;
            this.lblDashboardStatus.Location = new System.Drawing.Point(20, 20);
            this.lblDashboardStatus.Name = "lblDashboardStatus";
            this.lblDashboardStatus.Size = new System.Drawing.Size(100, 20);
            this.lblDashboardStatus.TabIndex = 0;
            this.lblDashboardStatus.Text = "Dashboard: Desligado";

            this.btnToggleDashboard.Location = new System.Drawing.Point(20, 50);
            this.btnToggleDashboard.Name = "btnToggleDashboard";
            this.btnToggleDashboard.Size = new System.Drawing.Size(150, 40);
            this.btnToggleDashboard.TabIndex = 1;
            this.btnToggleDashboard.Text = "Ligar Dashboard";
            this.btnToggleDashboard.UseVisualStyleBackColor = true;
            this.btnToggleDashboard.Click += new System.EventHandler(this.btnToggleDashboard_Click);

            this.Controls.Add(this.lblDashboardStatus);
            this.Controls.Add(this.btnToggleDashboard);
            this.Name = "DashboardControl";
            
            // Atualizar status inicial
            AtualizarStatus();
        }

        private async void btnToggleDashboard_Click(object sender, EventArgs e)
        {
            if (_dashboardService.DashboardEstaAtivo())
            {
                _dashboardService.DesligarDashboard();
                btnToggleDashboard.Text = "Ligar Dashboard";
            }
            else
            {
                btnToggleDashboard.Text = "Iniciando...";
                await _dashboardService.LigarDashboard();
            }
            AtualizarStatus();
        }

        private void AtualizarStatus()
        {
            if (_dashboardService.DashboardEstaAtivo())
            {
                lblDashboardStatus.Text = "Dashboard: Ligado";
                lblDashboardStatus.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                lblDashboardStatus.Text = "Dashboard: Desligado";
                lblDashboardStatus.ForeColor = System.Drawing.Color.Red;
            }
        }
    }
}