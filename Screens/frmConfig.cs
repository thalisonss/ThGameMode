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

namespace ThGameMode.Screens
{
    public partial class frmConfig : Form
    {
        public frmConfig()
        {
            InitializeComponent();
        }

        #region | Variables |
        static string nameService = "ThGameMode.exe";
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

        private void frmConfig_Load(object sender, EventArgs e)
        {
            if (CheckInstallation())
            {
                btnInstallUninstall.Text = "Uninstall";
                // btnInstallUninstall.Image = Properties.Resources.uninstall;
                // lblStatus.Text = "Service installed";
            }
            else
            {
                DisableEnableControls(false);

                if (DialogResult.Yes == MessageBox.Show("Do you want to install the service now?", "Install Service", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                {
                    Close();
                    frmInstall frmInstall = new frmInstall();
                    frmInstall.Show();
                }
            }
        }
    }
}
