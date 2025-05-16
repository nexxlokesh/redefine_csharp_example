using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Principal;
using System.Web;
using System.Windows.Forms;

namespace aimguard_auth
{
    public static class AppConfig
    {
        public static readonly string Version = "1.0.0";  // App version
        public static readonly int Subscription = 1;      // Subscription level
        public static readonly string DatabaseName = "user_0x_Benz_1747421394467"; // Dynamic DB name
    }

    public static class Auth
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string baseUrl = "https://redefine-auth.onrender.com";
        /// <summary>
        /// Registers a new user with the provided username, password, and key.
        /// </summary>
        /// <param name="username">The username for registration.</param>
        /// <param name="password">The password for registration.</param>
        /// <param name="key">The key used for registration.</param>
        /// <returns>A tuple where the first element indicates success and the second is a message.</returns>

        public static async Task<(bool success, string message)> Register(string username, string password, string key)
        {
            string pcSid = GetPcSid();

            string dbName = AppConfig.DatabaseName;

            var payload = new
            {
                username = username,
                password = password,
                key = key,
                hwid = pcSid,
                dbName = dbName
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync($"{baseUrl}/auth/register", content);
                var responseString = await response.Content.ReadAsStringAsync();

                string msg = "Unknown error";
                try
                {
                    var json = JObject.Parse(responseString);
                    msg = json["msg"]?.ToString() ?? msg;
                }
                catch { }

                return (response.IsSuccessStatusCode, msg);
            }
            catch (Exception ex)
            {
                return (false, "Register error: " + ex.Message);
            }
        }

        /// <summary>
        /// Logs in a user with the provided username and password.
        /// </summary>
        /// <param name="username">The username for login.</param>
        /// <param name="password">The password for login.</param>
        /// <returns>A tuple where the first element indicates success and the second is a message.</returns>

        public static async Task<(bool success, string message)> Login(string username, string password)
        {
            string pcSid = GetPcSid();
            string dbName = AppConfig.DatabaseName;

            var payload = new
            {
                username = username,
                password = password,
                hwid = pcSid,
                subscription = AppConfig.Subscription,
                dbName = dbName
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync($"{baseUrl}/auth/login", content);
                var responseString = await response.Content.ReadAsStringAsync();

                string msg = "Unknown error";
                try
                {
                    var json = JObject.Parse(responseString);
                    msg = json["msg"]?.ToString() ?? msg;
                }
                catch { }

                return (response.IsSuccessStatusCode, msg);
            }
            catch (Exception ex)
            {
                return (false, "Login error: " + ex.Message);
            }
        }

        /// <summary>
        /// Fetches the application configuration from the server.
        /// </summary>
        /// <returns>
        /// A tuple containing:
        /// - success: Whether the configuration was fetched successfully.
        /// - appVersion: The current version of the application.
        /// - pauseApp: A flag indicating if the app should be paused.
        /// - pauseMsg: The message to display if the app is paused.
        /// - outdatedMsg: The message to display if the application version is outdated.
        /// </returns>

        public static async Task<(bool success, string appVersion, bool pauseApp, string pauseMsg, string outdatedMsg)> FetchAppConfig()
        {
            string dbName = AppConfig.DatabaseName;

            try
            {
                var response = await client.GetAsync($"{baseUrl}/auth/getAppConfig?dbName={dbName}");
                var responseString = await response.Content.ReadAsStringAsync();

                var json = JObject.Parse(responseString);

                var data = json["data"];
                if (data == null)
                {
                    MessageBox.Show("Invalid response from server: missing 'data' field.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return (false, null, false, null, null);
                }

                string appVersion = data["appVersion"]?.ToString();
                bool pauseApp = data["pauseApp"]?.ToObject<bool>() ?? false;

                var messages = data["messages"];
                string pauseMsg = messages?["app_pause"]?.ToString();
                string outdatedMsg = messages?["outdated_version"]?.ToString();

                return (true, appVersion, pauseApp, pauseMsg, outdatedMsg);
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to connect to the server. Please check your internet connection and try again.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (false, null, false, null, null);
            }
        }
        /// <summary>
        /// Retrieves the PC's security identifier (SID).
        /// </summary>
        /// <returns>A string containing the PC's SID, or "UNKNOWN" if it cannot be obtained.</returns>
        public static string GetPcSid()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                return identity.User.Value;
            }
            catch
            {
                return "UNKNOWN";
            }
        }
    }
}
