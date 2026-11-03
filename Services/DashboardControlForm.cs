using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThGameMode.Services
{
    public class DashboardControlForm : Form
    {
        private Button btnDashboard;
        private Button btnDesligar;

        public DashboardControlForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.btnDashboard = new Button();
            this.btnDesligar = new Button();

            this.btnDashboard.Location = new System.Drawing.Point(15, 15);
            this.btnDashboard.Size = new System.Drawing.Size(120, 40);
            this.btnDashboard.Text = "Ligar Dashboard";
            this.btnDashboard.Click += new System.EventHandler(this.btnDashboard_Click);

            this.btnDesligar.Location = new System.Drawing.Point(15, 60);
            this.btnDesligar.Size = new System.Drawing.Size(120, 40);
            this.btnDesligar.Text = "Desligar Dashboard";
            this.btnDesligar.Click += new System.EventHandler(this.btnDesligar_Click);

            this.Controls.Add(this.btnDashboard);
            this.Controls.Add(this.btnDesligar);

            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(160, 110);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DashboardControlForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Dashboard PC";
        }

        private async void btnDashboard_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar se já está rodando
                var processes = Process.GetProcessesByName("Dashboard");
                if (processes.Length > 0)
                {
                    btnDashboard.Text = "Aberto!";
                    return;
                }

                // Iniciar o Dashboard
                var path = @"C:\Thalison\Studies\Personal\Projects\ThGameMode\Dashboard\Dashboard.exe";
                if (System.IO.File.Exists(path))
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = path,
                            WorkingDirectory = System.IO.Path.GetDirectoryName(path),
                            UseShellExecute = true
                        }
                    };
                    process.Start();
                    btnDashboard.Text = "Aberto!";
                }
                else
                {
                    MessageBox.Show($"Dashboard não encontrado!\n\nCaminho esperado:\n{path}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDesligar_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (var proc in Process.GetProcessesByName("Dashboard"))
                {
                    proc.Kill();
                }
                btnDashboard.Text = "Ligar Dashboard";
                MessageBox.Show("Dashboard desligado!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}