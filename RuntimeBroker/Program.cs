using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using aimguard_auth;

namespace RuntimeBroker
{
    internal static class Program
    {
        [STAThread]
        static async Task Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var (success, appVersion, pauseApp, pauseMsg, outdatedMsg) = await Auth.FetchAppConfig();

            if (!success)
            {
                return;
            }
            if (pauseApp)
            {
                MessageBox.Show(pauseMsg, "App Paused", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (appVersion != AppConfig.Version)
            {
                MessageBox.Show(outdatedMsg, "Outdated Version", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Application.Run(new RuntimeBroker());
        }
    }
}
