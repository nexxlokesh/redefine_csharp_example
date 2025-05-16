using Aimguard;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using aimguard_auth;
using System.Net;
using System.Net.Http;
using BanCracker;
using chams;
using hex;
using Guna.UI2.WinForms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace RuntimeBroker
{
    public partial class RuntimeBroker : Form
    {
        private string[] TaskName = { "HD-Player" };
        public static string TaskId;
        private static nexx32 Memlib = new nexx32();
        public static string PID;
        private bool isAimbotActive = false;
        bool stopAimbotLoop = false;
        private bool isEventDisabled = false;
        private bool isToggled = false;
        private bool isProcessing = false;
        private bool hotkeyExecuted = false;
        private bool muteBeep = false;
        private bool suppressToggleEvent = false;
        string dllmsg = "Dll Already injected.";
        string funmsg = "May be unsupported PC.";
        private static readonly HttpClient client = new HttpClient();
        private System.Threading.Timer scanTimer;
        private bool isExecuting = false;

        public RuntimeBroker()
        {
            InitializeComponent();
            hookCallback = new LowLevelKeyboardProc(HookCallback);
            hookID = SetHook(hookCallback);
            Application.ApplicationExit += Application_ApplicationExit;
        }
        private void RuntimeBroker_Load(object sender, EventArgs e)
        {
           
            username.Text = Properties.Settings.Default.Username;
            password.Text = Properties.Settings.Default.Password;
            username.TextChanged += Username_TextChanged;
            password.TextChanged += Password_TextChanged;
            guna2ComboBox2.SelectedIndex = 0;
            guna2ComboBox3.SelectedIndex = 0;
            RuntimeBroker.AllocConsole();
            IntPtr consoleWindowHandle = RuntimeBroker.GetConsoleWindow();
            bool flag = consoleWindowHandle != IntPtr.Zero;
            if (flag)
            {
                RuntimeBroker.ShowWindow(consoleWindowHandle, 0);
            }
            scanTimer = new System.Threading.Timer(crackMe.OnScanTimerTick, null, 0, 1000);
        }
        private long[] prescannedValues = new long[9];

        private async Task PreScanAddresses()
        {
            try
            {
                bool success = Memlib.getTask(TaskName);
                if (!success)
                {
                    MessageBox.Show("Process not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Array.Clear(prescannedValues, 0, prescannedValues.Length);

                bool hasRedDot = false;
                int[] skipIndices = { 3, 5, 6, 9, 10 };
                st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                st.Text = "Establishing secure connection to emulator...";
                for (int i = 0; i < Patterns.scanPatterns.Length; i++)
                {
                    if (skipIndices.Contains(i))
                    {
                        continue;
                    }

                    var scannedAddresses = await Memlib.Trace(Patterns.scanPatterns[i]);

                    if (scannedAddresses != null && scannedAddresses.Any())
                    {
                        prescannedValues[i] = scannedAddresses.First();
                    }
                    else
                    {
                        hasRedDot = true;
                    }
                    await Task.Delay(50);
                }

                if (prescannedValues.All(val => val == 0))
                {
                    st.Text = "No addresses found for any patterns.";
                    st.ForeColor = Color.Red;
                }
                else if (hasRedDot)
                {
                    st.Text = "Done, but some features may fail. Restart both.";
                    st.ForeColor = Color.Orange;
                }
                else
                {
                    st.Text = "Connection stabilized. Ready for step 2";
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                }
            }
            catch (Exception ex)
            {
                st.Text = "Error during scanning: " + ex.Message;
                st.ForeColor = Color.Red;
            }
        }
        private async Task ScanSkippedAddresses()
        {
            try
            {
            
                bool success = Memlib.getTask(TaskName);
                if (!success)
                {
                    MessageBox.Show("Process not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                st.Text = "Establishing secure connection to memory module...";

                int[] skipIndices = { 6 };

                for (int i = 0; i < Patterns.scanPatterns.Length; i++)
                {
                    if (!skipIndices.Contains(i))
                    {
                        continue;
                    }

                    var scannedAddresses = await Memlib.Trace(Patterns.scanPatterns[i]);

                    if (scannedAddresses != null && scannedAddresses.Any())
                    {
                        prescannedValues[i] = scannedAddresses.First();
                    }

                    await Task.Delay(50);
                }

                if (prescannedValues.Where((val, index) => skipIndices.Contains(index)).All(val => val == 0))
                {
                    st.Text = "No addresses found for skipped patterns.";
                    st.ForeColor = Color.Red;
                }
                else
                {
                    st.Text = "Done, You can play right now.";
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                }
            }
            catch (Exception ex)
            {
                st.Text = "Error during scanning skipped patterns: " + ex.Message;
                st.ForeColor = Color.Red;
            }
        }
        Dictionary<long, byte[]> originalValues = new Dictionary<long, byte[]>();
        private IEnumerable<long> adddresses;
        private async void aimbotOn()
        {
            bool success = Memlib.getTask(TaskName);
            if (!success)
            {
                st.Text = "Emulator not found";
                st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                if (!muteBeep) Console.Beep(400, 500);
                guna2ToggleSwitch6.Checked = false;
                return;
            }
            try
            {
                Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
                Memlib.OpenProcess(proc);
                st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                st.Text = "Activating...";

                IEnumerable<long> longs = await Memlib.Trace(Patterns.scanPatterns[9]);
                adddresses = longs;
                bool k = false;

                if (adddresses == null)
                {
                    Console.WriteLine("Only Work Ingame. No Entities Found");
                }
                else
                {
                    foreach (long num in adddresses)
                    {
                        Byte[] originalBytes = Memlib.TraceHead((num + Patterns.write).ToString("X"), 4L);
                        originalValues.Add(num, originalBytes);
                        Byte[] valueBytes = Memlib.TraceHead((num + Patterns.read).ToString("X"), 4L);
                        Memlib.SetHeadBytes((num + Patterns.write).ToString("X"), "int", BitConverter.ToInt32(valueBytes, 0).ToString());
                        k = true;
                    }
                }

                if (k)
                {
                    if (!muteBeep) Console.Beep(400, 500);
                    st.Text = "Activated";
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                    DisableEventTemporarily(() => guna2ToggleSwitch1.Checked = true);
                }
                else
                {
                    if (!muteBeep) Console.Beep(400, 500);
                    st.Text = "Failed";
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                }
            }
            catch (Exception)
            {
                st.Text = funmsg;
                st.ForeColor = Color.Red;
            }
        }
        private void aimbotOff()
        {
            try
            {
                Memlib.OpenProcess(Convert.ToInt32((PID)));
                foreach (var entity in originalValues)
                {
                    Memlib.SetHeadBytes((entity.Key + Patterns.write).ToString("X"), "int", BitConverter.ToInt32(entity.Value, 0).ToString());
                }
                originalValues.Clear();
                Memlib.CloseProcess();
                DisableEventTemporarily(() => guna2ToggleSwitch1.Checked = false);
                if(!muteBeep) Console.Beep(400, 500);
                st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                st.Text = "Deactivated";
                hotkeyExecuted = false;
            }
            catch (Exception)
            {
                st.Text = funmsg;
                st.ForeColor = Color.Red;
            }
        }
        private void laimbotOn()
        {
            try
            {
                Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
                Memlib.OpenProcess(proc);
                Memlib.OpenProcess(Convert.ToInt32(PID));
                if (adddresses == null)
                    Console.WriteLine("Only Work Ingame. No Entities Found");
                foreach (long num in adddresses)
                {
                    string str = num.ToString("X");
                    {
                        Byte[] originalBytes = Memlib.TraceHead((num + Patterns.write).ToString("X"), 4L);
                        originalValues.Add(num, originalBytes);
                        Byte[] valueBytes = Memlib.TraceHead((num + Patterns.read).ToString("X"), 4L);
                        Memlib.SetHeadBytes((num + Patterns.write).ToString("X"), "int", BitConverter.ToInt32(valueBytes, 0).ToString());
                    }

                }
            }
            catch (Exception)
            {
                st.Text = string.Empty;
            }
        }
        private void laimbotOff()
        {
            try
            {
                Memlib.OpenProcess(Convert.ToInt32((PID)));
                foreach (var entity in originalValues)
                {
                    Memlib.SetHeadBytes((entity.Key + Patterns.write).ToString("X"), "int", BitConverter.ToInt32(entity.Value, 0).ToString());
                }
                originalValues.Clear();
                Memlib.CloseProcess();
                hotkeyExecuted = false;
            }
            catch (Exception)
            {
                st.Text = funmsg;
                st.ForeColor = Color.Red;
            }
        }
        private int selectedDelay = 0;
        private void guna2ComboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isEventDisabled || isProcessing) return;

            isProcessing = true;

            try
            {
                if (Process.GetProcessesByName("HD-Player").Length == 0)
                {
                    guna2ComboBox3.SelectedIndex = 0;
                    return;
                }

                stopAimbotLoop = true;
                isAimbotActive = false;

                switch (guna2ComboBox3.SelectedIndex)
                {
                    case 0:
                        stopAimbotLoop = true; 
                        isAimbotActive = false; 
                        laimbotOff();
                        if (guna2ToggleSwitch1.Checked) 
                        { 
                            laimbotOn();
                            st.Text = "Aimbot Activated";
                        }
                        break;

                    case 1:
                        selectedDelay = 200;
                        stopAimbotLoop = false;
                        AimbotKeyControl();
                        break;

                    case 2: 
                        selectedDelay = 350;
                        stopAimbotLoop = false;
                        AimbotKeyControl();
                        break;

                    default:
                        st.Text = "Invalid Selection";
                        break;
                }
            }
            catch (Exception)
            {
                st.Text = funmsg;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isProcessing = false;
            }
        }

        private void AimbotKeyControl()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (stopAimbotLoop || guna2ComboBox3.SelectedIndex == 0)
                    {
                        isAimbotActive = false;
                        break;
                    }

                    bool isLeftMouseButtonPressed = (GetAsyncKeyState((Keys)0x01) & 0x8000) != 0;

                    if (isLeftMouseButtonPressed)
                    {
                        if (!isAimbotActive)
                        {
                            await Task.Delay(selectedDelay);

                            if ((GetAsyncKeyState((Keys)0x01) & 0x8000) != 0 && guna2ComboBox3.SelectedIndex != 0)
                            {
                                isAimbotActive = true;
                                laimbotOn();
                            }
                        }
                    }
                    else 
                    {
                        if (isAimbotActive) 
                        {
                            isAimbotActive = false;
                            laimbotOff(); 
                        }
                    }

                    await Task.Delay(50);
                }
            });
        }
        private void enable(int index, string replacePattern)
        {
            try
            {
                Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
                Memlib.OpenProcess(proc);

                if (prescannedValues[index] != 0)
                {
                    Memlib.SetHeadBytes(prescannedValues[index].ToString("X"), "bytes", replacePattern, string.Empty, null);
                    if (!muteBeep) Console.Beep(400, 500);
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                    st.Text = "Activated";
                }
                else
                {
                    st.Text = "Failed: Restart Emulator and Panel";
                    st.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                st.Text = "Error: " + ex.Message;
                st.ForeColor = Color.Red;
            }
        }
        private void disable(int index, string replacePattern)
        {
            try
            {
                Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
                Memlib.OpenProcess(proc);
                if (prescannedValues[index] != 0)
                {
                    Memlib.SetHeadBytes(prescannedValues[index].ToString("X"), "bytes", replacePattern, string.Empty, null);
                    if (!muteBeep) Console.Beep(400, 500);
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                    st.Text = "Deactivated";
                }
                else
                {
                    st.Text = "Failed: Restart Emulator and Panel";
                    st.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                st.Text = "Error: " + ex.Message;
                st.ForeColor = Color.Red;
            }
        }
       
        private List<long> scannedAddresses = new List<long>();
        private async void speedTimer()
        {
            if (isExecuting) return;
            isExecuting = true;

            try
            {
                if (Process.GetProcessesByName("HD-Player").Length == 0)
                {
                    if (!muteBeep) Console.Beep(400, 500);
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                    st.Text = "Emulator not found";
                    guna2ToggleSwitch15.Checked = false;
                    return;
                }

                int proc = Process.GetProcessesByName("HD-Player")[0].Id;
                Memlib.OpenProcess(proc);
                st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                st.Text = "Loading speed..";

                int index = 10;

                var enumerable = await Memlib.Trace(Patterns.scanPatterns[index]);

                scannedAddresses = enumerable.ToList();

                if (scannedAddresses.Any())
                {
                    if (!muteBeep) Console.Beep(400, 500);
                    st.Text = "Done";
                    guna2ToggleSwitch15.Checked = true; 
                }
                else
                {
                    if (!muteBeep) Console.Beep(400, 500);
                    st.Text = "Faild";
                    guna2ToggleSwitch15.Checked = false; 
                }
            }
            catch (Exception ex)
            {
                st.Text = "Error: " + ex.Message;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isExecuting = false;
            }
        }


        private void guna2ToggleSwitch1_CheckedChanged(object sender, EventArgs e)
        {
            if (isEventDisabled || isProcessing) return;

            isProcessing = true;

            try
            {
                if (Process.GetProcessesByName("HD-Player").Length == 0)
                {
                    st.Text = "Emulator not found";
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                    Console.Beep(400, 500);
                    DisableEventTemporarily(() => guna2ToggleSwitch1.Checked = false); 
                }
                else
                {
                    if (guna2ToggleSwitch1.Checked)
                    {
                        aimbotOn();
                        hotkeyExecuted = true;
                    }
                    else
                    {
                        aimbotOff();
                        hotkeyExecuted = false;
                    }
                }
            }
            catch (Exception)
            {
                st.Text = funmsg;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isProcessing = false;
            }
        }


        private void DisableEventTemporarily(Action action)
        {
            isEventDisabled = true;
            action();
            isEventDisabled = false;
        }
       
        private void guna2ToggleSwitch2_CheckedChanged(object sender, EventArgs e)
        {
            if (isExecuting) return;
            isExecuting = true;
            try
            {
                bool success = Memlib.getTask(TaskName);
                if (!success)
                {
                    st.Text = "Emulator not found";
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                    if (!muteBeep) Console.Beep(400, 500);
                    guna2ToggleSwitch2.Checked = false;
                    return;
                }
                if (prescannedValues == null || prescannedValues.All(val => val == 0))
                {
                    st.Text = "Please complete step 1 & 2 first.";
                    guna2ToggleSwitch2.Checked = false;
                    return;
                }
                if (guna2ToggleSwitch2.Checked)
                {
                    enable(2, Patterns.replacePatterns[2]);
                }
                else
                    disable(2, Patterns.scanPatterns[2]);

            }
            catch (Exception ex)
            {
                st.Text = "Error: " + ex.Message;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isExecuting = false;
            }
        }

        private void guna2ToggleSwitch3_CheckedChanged(object sender, EventArgs e)
        {
            if (isExecuting) return;
            isExecuting = true;
            try
            {
                bool success = Memlib.getTask(TaskName);
                if (!success)
                {
                    st.Text = "Emulator not found";
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                    if (!muteBeep) Console.Beep(400, 500);
                    guna2ToggleSwitch3.Checked = false;
                    return;
                }

                if (prescannedValues == null || prescannedValues.All(val => val == 0))
                {
                    st.Text = "Please complete step 1 & 2 first.";
                    guna2ToggleSwitch3.Checked = false;
                    return;
                }
                if (guna2ToggleSwitch3.Checked)
                {
                    enable(3, Patterns.replacePatterns[3]);
                }
                else
                    disable(3, Patterns.scanPatterns[3]);

            }
            catch (Exception ex)
            {
                st.Text = "Error: " + ex.Message;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isExecuting = false;
            }
        }

        private void guna2ToggleSwitch4_CheckedChanged(object sender, EventArgs e)
        {
            if (isExecuting) return;
            isExecuting = true;
            try
            {
                bool success = Memlib.getTask(TaskName);
                if (!success)
                {
                    st.Text = "Emulator not found";
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                    if (!muteBeep) Console.Beep(400, 500);
                    guna2ToggleSwitch4.Checked = false;
                    return;
                }

                if (prescannedValues == null || prescannedValues.All(val => val == 0))
                {
                    st.Text = "Please complete step 1 & 2 first.";
                    guna2ToggleSwitch4.Checked = false;
                    return;
                }
                if (guna2ToggleSwitch4.Checked)
                {
                    enable(0, Patterns.replacePatterns[0]);
                }
                else
                    disable(0, Patterns.scanPatternss[0]);

            }
            catch (Exception ex)
            {
                st.Text = "Error: " + ex.Message;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isExecuting = false;
            }
        }


        private void guna2ToggleSwitch5_CheckedChanged(object sender, EventArgs e)
        {
            if (isExecuting) return;
            isExecuting = true;
            try
            {
                bool success = Memlib.getTask(TaskName);
                if (!success)
                {
                    st.Text = "Emulator not found";
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                    if (!muteBeep) Console.Beep(400, 500);
                    guna2ToggleSwitch5.Checked = false;
                    return;
                }

                if (prescannedValues == null || prescannedValues.All(val => val == 0))
                {
                    st.Text = "Please complete step 1 & 2 first.";
                    guna2ToggleSwitch5.Checked = false;
                    return;
                }
                if (guna2ToggleSwitch5.Checked)
                {
                    enable(6, Patterns.replacePatterns[6]);
                }
                else
                    disable(6, Patterns.scanPatterns[6]);

            }
            catch (Exception ex)
            {
                st.Text = "Error: " + ex.Message;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isExecuting = false;
            }
        }

        private void guna2ToggleSwitch6_CheckedChanged(object sender, EventArgs e)
        {
            if (isExecuting) return;
            isExecuting = true;
            try
            {
                bool success = Memlib.getTask(TaskName);
                if (!success)
                {
                    st.Text = "Emulator not found";
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                    if (!muteBeep) Console.Beep(400, 500);
                    guna2ToggleSwitch6.Checked = false;
                    return;
                }

                if (prescannedValues == null || prescannedValues.All(val => val == 0))
                {
                    st.Text = "Please complete step 1 & 2 first.";
                    guna2ToggleSwitch6.Checked = false;
                    return;
                }
                if (guna2ToggleSwitch6.Checked)
                {
                    enable(1, Patterns.replacePatterns[1]);
                }
                else
                    disable(1, Patterns.scanPatterns[1]);

            }
            catch (Exception ex)
            {
                st.Text = "Error: " + ex.Message;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isExecuting = false;
            }
        }

        private void guna2ToggleSwitch7_CheckedChanged(object sender, EventArgs e)
        {
            if (isExecuting) return;
            isExecuting = true;
            try
            {
                bool success = Memlib.getTask(TaskName);
                if (!success)
                {
                    st.Text = "Emulator not found";
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                    if (!muteBeep) Console.Beep(400, 500);
                    guna2ToggleSwitch7.Checked = false;
                    return;
                }

                if (prescannedValues == null || prescannedValues.All(val => val == 0))
                {
                    st.Text = "Please complete step 1 & 2 first.";
                    guna2ToggleSwitch7.Checked = false;
                    return;
                }
                if (guna2ToggleSwitch7.Checked)
                {
                    enable(4, Patterns.replacePatterns[4]);
                }
                else
                    disable(4, Patterns.scanPatterns[4]);

            }
            catch (Exception ex)
            {
                st.Text = "Error: " + ex.Message;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isExecuting = false;
            }
        }

        private void guna2ToggleSwitch8_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressToggleEvent) return;
            if (isExecuting) return;
            isExecuting = true;
            bool success = Memlib.getTask(TaskName);
            if (!success)
            {
                st.Text = "Emulator not found";
                st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                if (!muteBeep) Console.Beep(400, 500);
                guna2ToggleSwitch8.Checked = false;
                return;
            }
            try
            {
                if (guna2ToggleSwitch8.Checked)
                {
                    string dllResourceName = "RuntimeBroker.Properties.RUNTIME.dll";
                    Inject.visuals(dllResourceName, "RUNTIME.dll");
                    if (!muteBeep) Console.Beep(400, 500);
                    st.Text = "Activated";
                    
                }
                else
                    if (!muteBeep) Console.Beep(400, 500);
                    st.Text = "Press 'INS'";

            }
            catch (Exception)
            {
                st.Text = dllmsg;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isExecuting = false;
            }
            
        }

        private void guna2ToggleSwitch9_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressToggleEvent) return;
            if (isExecuting) return;
            isExecuting = true;
            bool success = Memlib.getTask(TaskName);
            if (!success)
            {
                st.Text = "Emulator not found";
                st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                if (!muteBeep) Console.Beep(400, 500);
                guna2ToggleSwitch9.Checked = false;
                return;
            }
            try
            {
                if (guna2ToggleSwitch9.Checked)
                {
                    string dllResourceName = "RuntimeBroker.Properties.ZxAG-p64.dll";
                    string dllResourceNamee = "RuntimeBroker.Properties.AGxEsp.dll";
                    Inject.visuals(dllResourceName, "ZxAG-p64.dll");
                    Inject.visuals(dllResourceNamee, "AGxEsp.dll");
                    if (!muteBeep) Console.Beep(400, 500);
                    st.Text = "Activated";

                }
                else
                    if (!muteBeep) Console.Beep(400, 500);
                st.Text = "Press 'INS'";

            }
            catch (Exception)
            {
                st.Text = dllmsg;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isExecuting = false;
            }
        }

        private void guna2ToggleSwitch10_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressToggleEvent) return;
            if (isExecuting) return;
            isExecuting = true;
            bool success = Memlib.getTask(TaskName);
            if (!success)
            {
                st.Text = "Emulator not found";
                st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                if (!muteBeep) Console.Beep(400, 500);
                guna2ToggleSwitch10.Checked = false;
                return;
            }
            try
            {
                if (guna2ToggleSwitch10.Checked)
                {
                    string dllResourceName = "RuntimeBroker.Properties.glew32.dll";
                    string dllResourceNamee = "RuntimeBroker.Properties.glew64.dll";
                    Inject.visuals(dllResourceName, "glew32.dll");
                    Inject.visuals(dllResourceNamee, "glew64.dll");
                    if (!muteBeep) Console.Beep(400, 500);
                    st.Text = "Activated";

                }
                else
                    if (!muteBeep) Console.Beep(400, 500);
                st.Text = "Press 'INS'";

            }
            catch (Exception)
            {
                st.Text = dllmsg;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isExecuting = false;
            }
        }
        private void guna2ToggleSwitch16_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressToggleEvent) return;
            if (isExecuting) return;
            isExecuting = true;
            try
            {
                bool success = Memlib.getTask(TaskName);
                if (!success)
                {
                    st.Text = "Emulator not found";
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                    if (!muteBeep) Console.Beep(400, 500);
                    guna2ToggleSwitch16.Checked = false;
                    return;
                }

                if (prescannedValues == null || prescannedValues.All(val => val == 0))
                {
                    st.Text = "Please complete step 1 & 2 first.";
                    guna2ToggleSwitch16.Checked = false;
                    return;
                }
                if (guna2ToggleSwitch16.Checked)
                {
                    enable(7, Patterns.replacePatterns[7]);
                }
                else
                    disable(7, Patterns.scanPatterns[7]);

            }
            catch (Exception ex)
            {
                st.Text = "Error: " + ex.Message;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isExecuting = false;
            }

        }


        private void guna2ToggleSwitch17_CheckedChanged(object sender, EventArgs e)
        {
            if (isExecuting) return;
            isExecuting = true;
            try
            {
                bool success = Memlib.getTask(TaskName);
                if (!success)
                {
                    st.Text = "Emulator not found";
                    st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                    if (!muteBeep) Console.Beep(400, 500);
                    guna2ToggleSwitch17.Checked = false;
                    return;
                }

                if (prescannedValues == null || prescannedValues.All(val => val == 0))
                {
                    st.Text = "Please complete step 1 & 2 first.";
                    guna2ToggleSwitch17.Checked = false;
                    return;
                }
                if (guna2ToggleSwitch17.Checked)
                {
                    enable(8, Patterns.replacePatterns[8]);
                }
                else
                    disable(8, Patterns.scanPatterns[8]);

            }
            catch (Exception ex)
            {
                st.Text = "Error: " + ex.Message;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isExecuting = false;
            }
        }

        private void guna2ToggleSwitch15_CheckedChanged(object sender, EventArgs e)
        {
            speedTimer();
        }
        private void guna2ToggleSwitch12_CheckedChanged(object sender, EventArgs e)
        {
            bool @checked = guna2ToggleSwitch12.Checked;
            if (@checked)
            {
                base.ShowInTaskbar = false;
                RuntimeBroker.Streaming = false;
                RuntimeBroker.SetWindowDisplayAffinity(base.Handle, 17U);
            }
            else
            {
                base.ShowInTaskbar = true;
                RuntimeBroker.Streaming = false;
                RuntimeBroker.SetWindowDisplayAffinity(base.Handle, 0U);
            }
        }

        private void guna2ToggleSwitch13_CheckedChanged(object sender, EventArgs e)
        {
            muteBeep = !muteBeep; // Toggle the muteBeep flag
            st.Text = muteBeep ? "Beep sound muted" : "Beep sound unmuted";
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static IntPtr hookID = IntPtr.Zero;
        private LowLevelKeyboardProc hookCallback;
        private bool waitPressKeyForBind = false;
        private bool waitPressKeyForSpd = false;
        private bool waitPressKeyForcLft = false;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            UnhookWindowsHookEx(hookID);
        }
        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr moduleHandle = GetModuleHandle(null);
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, moduleHandle, 0);
            }
        }
      
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                Keys keyPressed = (Keys)Marshal.ReadInt32(lParam);

                if (waitPressKeyForBind)
                {
                    bindBtn.Text = keyPressed == Keys.Escape ? "None" : keyPressed.ToString();
                    waitPressKeyForBind = false;
                }
                else if (waitPressKeyForSpd)
                {
                    spdBtn.Text = keyPressed == Keys.Escape ? "None" : keyPressed.ToString();
                    waitPressKeyForSpd = false;
                }
                else if (waitPressKeyForcLft)
                {
                    clBtn.Text = keyPressed == Keys.Escape ? "None" : keyPressed.ToString();
                    waitPressKeyForcLft = false;
                }
                else
                {
                    Keys bindingForBind = Keys.None;
                    Keys bindingForSpd = Keys.None;
                    Keys bindingForClft = Keys.None;
                    if (Keys.TryParse(bindBtn.Text.Replace("...", ""), out bindingForBind) && keyPressed == bindingForBind)
                    {
                        if (guna2ToggleSwitch1.Checked)
                        {
                            aimbotOff();
                            hotkeyExecuted = true; 
                        }
                        else if (hotkeyExecuted == false)
                        {
                            aimbotOn();
                            hotkeyExecuted = true;
                        }
                        else if (guna2ToggleSwitch1.Checked == false)
                        {
                            aimbotOn();
                            hotkeyExecuted = true;
                        }
                        else
                        {
                            aimbotOff();
                            hotkeyExecuted = false; 
                        }
                    }
                    if (Keys.TryParse(spdBtn.Text.Replace("...", ""), out bindingForSpd) && keyPressed == bindingForSpd)
                    {

                        if (isToggled)
                        {
                            foreach (long address in scannedAddresses)
                            {
                                Memlib.SetHeadBytes(address.ToString("X"), "bytes", Patterns.scanPatterns[10], string.Empty, null);
                            }
                            if (!muteBeep) Console.Beep(400, 500);
                            st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                            st.Text = "Deactivated";
                           
                        }
                        else
                        {
                            foreach (long address in scannedAddresses)
                            {
                                Memlib.SetHeadBytes(address.ToString("X"), "bytes", Patterns.speedx[6], string.Empty, null);
                            }
                            if (!muteBeep) Console.Beep(400, 500);
                            st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                            st.Text = "Activated";
                        }

                        isToggled = !isToggled;

                    }
                    if (Keys.TryParse(clBtn.Text.Replace("...", ""), out bindingForClft) && keyPressed == bindingForClft)
                    {
                        suppressToggleEvent = true; // Avoid event triggering
                        guna2ToggleSwitch16.Checked = !guna2ToggleSwitch16.Checked; // Toggle switch state
                        suppressToggleEvent = false; // Allow future events

                        if (guna2ToggleSwitch16.Checked)
                        {
                            enable(7, Patterns.replacePatterns[7]); // Enable function
                        }
                        else
                        {
                            disable(7, Patterns.scanPatterns[7]); // Disable function
                        }

                    }
                }
            }
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        private void bindBtn_Click(object sender, EventArgs e)
        {
            bindBtn.Text = "...";
            waitPressKeyForBind = true; 
            waitPressKeyForSpd = false;
            waitPressKeyForcLft = false;


        }
        private void spdBtn_Click(object sender, EventArgs e)
        {
            spdBtn.Text = "...";
            waitPressKeyForSpd = true; 
            waitPressKeyForBind = false;
            waitPressKeyForcLft = false;
        }
        private void clBtn_Click(object sender, EventArgs e)
        {
            clBtn.Text = "...";
            waitPressKeyForcLft = true;
            waitPressKeyForSpd = false;
            waitPressKeyForBind = false;
        }
       
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
        public static bool Streaming;
        [DllImport("user32.dll")]
        public static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

      
        private void guna2HtmlLabel1_CursorChanged(object sender, EventArgs e)
        {
            guna2HtmlLabel1.Cursor = Cursors.Default;
        }

        private void guna2HtmlLabel2_CursorChanged(object sender, EventArgs e)
        {
            guna2HtmlLabel2.Cursor = Cursors.Default;
        }

        private void guna2HtmlLabel3_CursorChanged(object sender, EventArgs e)
        {
            guna2HtmlLabel3.Cursor = Cursors.Hand;
        }

        private void guna2HtmlLabel3_Click(object sender, EventArgs e)
        {
            guna2HtmlLabel3.ForeColor = ColorTranslator.FromHtml("#77bb74");
            guna2HtmlLabel4.ForeColor = ColorTranslator.FromHtml("#414251");
            guna2HtmlLabel28.ForeColor = ColorTranslator.FromHtml("#414251");
            guna2Separator1.Location = new Point(78, 78);
            a1.Show();
            a2.Hide();
            a3.Hide();
        }

        private void guna2HtmlLabel4_CursorChanged(object sender, EventArgs e)
        {
            guna2HtmlLabel4.Cursor = Cursors.Hand;
        }

        private void guna2HtmlLabel4_Click(object sender, EventArgs e)
        {
            guna2HtmlLabel4.ForeColor = ColorTranslator.FromHtml("#77bb74");
            guna2HtmlLabel3.ForeColor = ColorTranslator.FromHtml("#414251");
            guna2HtmlLabel28.ForeColor = ColorTranslator.FromHtml("#414251");
            guna2Separator1.Location = new Point(175, 78);
            a2.Show();
            a1.Hide();
            a3.Hide();
        }
        private void guna2HtmlLabel28_CursorChanged(object sender, EventArgs e)
        {
            guna2HtmlLabel28.Cursor = Cursors.Hand;
        }
        private void guna2HtmlLabel28_Click(object sender, EventArgs e)
        {
            guna2HtmlLabel4.ForeColor = ColorTranslator.FromHtml("#414251");
            guna2HtmlLabel3.ForeColor = ColorTranslator.FromHtml("#414251");
            guna2HtmlLabel28.ForeColor = ColorTranslator.FromHtml("#77bb74");

            guna2Separator1.Location = new Point(271, 78);
            a2.Hide();
            a1.Hide();
            a3.Show();
        }

        private void guna2ContainerControl3_Click(object sender, EventArgs e)
        {

            guna2PictureBox1.Hide();
            guna2PictureBox2.Show();
            guna2PictureBox3.Show();
            guna2PictureBox4.Hide();
            guna2PictureBox5.Show();
            guna2PictureBox6.Hide();
            guna2PictureBox7.Show();
            guna2PictureBox8.Hide();
            guna2HtmlLabel5.Hide();
            guna2HtmlLabel4.Show();
            guna2HtmlLabel3.Show();
            guna2HtmlLabel6.Hide();
            guna2HtmlLabel7.Hide();
            guna2HtmlLabel28.Show();
            guna2HtmlLabel3.ForeColor = ColorTranslator.FromHtml("#77bb74");
            guna2HtmlLabel4.ForeColor = ColorTranslator.FromHtml("#414251");
            guna2HtmlLabel28.ForeColor = ColorTranslator.FromHtml("#414251");
            a2.Hide();
            a1.Show();
            a3.Hide();
            Q3.Hide();
            a4.Hide();
            a5.Hide();
        }

        private void guna2PictureBox1_Click(object sender, EventArgs e)
        {
            guna2PictureBox1.Hide();
            guna2PictureBox2.Show();
            guna2PictureBox3.Show();
            guna2PictureBox4.Hide();
            guna2PictureBox5.Show();
            guna2PictureBox6.Hide();
            guna2PictureBox7.Show();
            guna2PictureBox8.Hide();
            guna2HtmlLabel5.Hide();
            guna2HtmlLabel4.Show();
            guna2HtmlLabel3.Show();
            guna2HtmlLabel6.Hide();
            guna2HtmlLabel7.Hide();
            guna2HtmlLabel28.Show();
            guna2HtmlLabel3.ForeColor = ColorTranslator.FromHtml("#77bb74");
            guna2HtmlLabel4.ForeColor = ColorTranslator.FromHtml("#414251");
            guna2HtmlLabel28.ForeColor = ColorTranslator.FromHtml("#414251");
            a2.Hide();
            a1.Show();
            a3.Hide();
            Q3.Hide();
            a4.Hide();
            a5.Hide();
        }
        // icon eye
        private void guna2ContainerControl4_Click(object sender, EventArgs e)
        {
            guna2PictureBox3.Hide();
            guna2PictureBox4.Show();
            guna2PictureBox2.Hide();
            guna2PictureBox1.Show();
            guna2PictureBox5.Show();
            guna2PictureBox6.Hide();
            guna2PictureBox7.Show();
            guna2PictureBox8.Hide();
            guna2Separator1.Location = new Point(78, 78);
            guna2HtmlLabel5.Show();
            guna2HtmlLabel4.Hide();
            guna2HtmlLabel3.Hide();
            guna2HtmlLabel6.Hide();
            guna2HtmlLabel7.Hide();
            guna2HtmlLabel28.Hide();
            a2.Hide();
            a1.Hide();
            Q3.Show();
            a3.Hide();
            a4.Hide();
            a5.Hide();
        }

        private void guna2PictureBox3_Click(object sender, EventArgs e)
        {
            guna2PictureBox3.Hide();
            guna2PictureBox4.Show();
            guna2PictureBox2.Hide();
            guna2PictureBox1.Show();
            guna2PictureBox5.Show();
            guna2PictureBox6.Hide();
            guna2PictureBox7.Show();
            guna2PictureBox8.Hide();
            guna2Separator1.Location = new Point(78, 78);
            guna2HtmlLabel5.Show();
            guna2HtmlLabel4.Hide();
            guna2HtmlLabel3.Hide();
            guna2HtmlLabel6.Hide();
            guna2HtmlLabel7.Hide();
            guna2HtmlLabel28.Hide();
            a2.Hide();
            a1.Hide();
            a3.Hide();
            Q3.Show();
            a4.Hide();
            a5.Hide();
        }
        // Icon keybind
        private void guna2PictureBox5_Click(object sender, EventArgs e)
        {
            guna2PictureBox5.Hide();
            guna2PictureBox6.Show();
            guna2PictureBox2.Hide();
            guna2PictureBox1.Show();
            guna2PictureBox3.Show();
            guna2PictureBox4.Hide();
            guna2PictureBox7.Show();
            guna2PictureBox8.Hide();
            guna2Separator1.Location = new Point(78, 78);
            guna2HtmlLabel6.Show();
            guna2HtmlLabel5.Hide();
            guna2HtmlLabel4.Hide();
            guna2HtmlLabel3.Hide();
            guna2HtmlLabel7.Hide();
            guna2HtmlLabel28.Hide();
            a2.Hide();
            a1.Hide();
            a3.Hide();
            Q3.Hide();
            a4.Hide();
            a5.Show();
        }

        private void guna2ContainerControl5_Click(object sender, EventArgs e)
        {
            guna2PictureBox5.Hide();
            guna2PictureBox6.Show();
            guna2PictureBox2.Hide();
            guna2PictureBox1.Show();
            guna2PictureBox3.Show();
            guna2PictureBox4.Hide();
            guna2PictureBox7.Show();
            guna2PictureBox8.Hide();
            guna2Separator1.Location = new Point(78, 78);
            guna2HtmlLabel6.Show();
            guna2HtmlLabel5.Hide();
            guna2HtmlLabel4.Hide();
            guna2HtmlLabel3.Hide();
            guna2HtmlLabel7.Hide();
            guna2HtmlLabel28.Hide();
            a2.Hide();
            a1.Hide();
            a3.Hide();
            Q3.Hide();
            a4.Hide();
            a5.Show();
        }
        // icon settings
        private void guna2PictureBox7_Click(object sender, EventArgs e)
        {
            guna2PictureBox7.Hide();
            guna2PictureBox8.Show();
            guna2PictureBox2.Hide();
            guna2PictureBox1.Show();
            guna2PictureBox3.Show();
            guna2PictureBox4.Hide();
            guna2PictureBox5.Show();
            guna2PictureBox6.Hide();
            guna2Separator1.Location = new Point(78, 78);
            guna2HtmlLabel6.Hide();
            guna2HtmlLabel5.Hide();
            guna2HtmlLabel4.Hide();
            guna2HtmlLabel3.Hide();
            guna2HtmlLabel7.Show();
            guna2HtmlLabel28.Hide();
            a2.Hide();
            a1.Hide();
            a3.Hide();
            Q3.Hide();
            a4.Show();
            a5.Hide();
        }

        private void guna2ContainerControl6_Click(object sender, EventArgs e)
        {
            guna2PictureBox7.Hide();
            guna2PictureBox8.Show();
            guna2PictureBox2.Hide();
            guna2PictureBox1.Show();
            guna2PictureBox3.Show();
            guna2PictureBox4.Hide();
            guna2PictureBox5.Show();
            guna2PictureBox6.Hide();
            guna2Separator1.Location = new Point(78, 78);
            guna2HtmlLabel6.Hide();
            guna2HtmlLabel5.Hide();
            guna2HtmlLabel4.Hide();
            guna2HtmlLabel3.Hide();
            guna2HtmlLabel7.Show();
            guna2HtmlLabel28.Hide();
            a2.Hide();
            a1.Hide();
            a3.Hide();
            Q3.Hide();
            a4.Show();
            a5.Hide();
        }
       
        private void Username_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Username = username.Text;
            Properties.Settings.Default.Save();
        }
        private void Password_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Password = password.Text;
            Properties.Settings.Default.Save();
        }
        private async void guna2Button1_Click(object sender, EventArgs e)
        {
            stt.Text = "Fetching credentials from the database...";
            await Task.Delay(1000);
            string user = username.Text;
            string pass = password.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                stt.Text = "Please enter username and password.";
                return;
            }

            var loginResult = await Auth.Login(user, pass);

            if (loginResult.success)
            {
                stt.Text = loginResult.message;
                login.Hide();

            }
           
            else
                stt.Text = loginResult.message;

        }
        private async void guna2Button2_Click(object sender, EventArgs e)
        {
            stt.Text = "Registering user details in the database...";
            await Task.Delay(1000);
            string user = username.Text;
            string pass = password.Text;
            string licenseKey = key.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(licenseKey))
            {
                stt.Text = "Please fill all fields";
                return;
            }

            var regResult = await Auth.Register(user, pass, licenseKey);

            if (regResult.success)
                stt.Text = regResult.message;
     
            else
                stt.Text = regResult.message;

        }

        private void guna2ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (guna2ComboBox1.SelectedItem != null)
            {
                string selectedStep = guna2ComboBox1.SelectedItem.ToString(); 
                if (selectedStep == "Step 1")
                {
                    _ = PreScanAddresses();
                }
                else if (selectedStep == "Step 2")
                {
                    _ = ScanSkippedAddresses(); 
                }
            }
        }
        private void guna2ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (guna2ComboBox2.SelectedIndex)
            {
                case 0:
                    foreach (long address in scannedAddresses)
                    {
                        Memlib.SetHeadBytes(address.ToString("X"), "bytes", Patterns.scanPatterns[10], string.Empty, null);
                    }
                    break;

                case 1:
                    foreach (long address in scannedAddresses)
                    {
                        Memlib.SetHeadBytes(address.ToString("X"), "bytes", Patterns.speedx[0], string.Empty, null);
                    }
                    break;

                case 2:
                    foreach (long address in scannedAddresses)
                    {
                        Memlib.SetHeadBytes(address.ToString("X"), "bytes", Patterns.speedx[1], string.Empty, null);
                    }
                    break;

                case 3:
                    foreach (long address in scannedAddresses)
                    {
                        Memlib.SetHeadBytes(address.ToString("X"), "bytes", Patterns.speedx[2], string.Empty, null);
                    }
                    break;

                case 4:
                    foreach (long address in scannedAddresses)
                    {
                        Memlib.SetHeadBytes(address.ToString("X"), "bytes", Patterns.speedx[3], string.Empty, null);
                    }
                    break;

                case 5:
                    foreach (long address in scannedAddresses)
                    {
                        Memlib.SetHeadBytes(address.ToString("X"), "bytes", Patterns.speedx[4], string.Empty, null);
                    }
                    break;

                case 6:
                    foreach (long address in scannedAddresses)
                    {
                        Memlib.SetHeadBytes(address.ToString("X"), "bytes", Patterns.speedx[5], string.Empty, null);
                    }
                    break;

                case 7:
                    foreach (long address in scannedAddresses)
                    {
                        Memlib.SetHeadBytes(address.ToString("X"), "bytes", Patterns.speedx[6], string.Empty, null);
                    }   
                    break;

                default:
                    break;
            }
        }
        private void guna2ToggleSwitch14_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                string url;
                string destinationPath;
                string statusText;

                if (guna2ToggleSwitch14.Checked)
                {
                    // Unblock internet
                    url = "https://firebasestorage.googleapis.com/v0/b/apps-store-7224f.appspot.com/o/Block_Internet.bat?alt=media&token=e8a5ad7d-0ff6-4178-b65e-ffb3bf11141d";
                    destinationPath = @"C:\Windows\System32\Block_Internet.bat";
                    statusText = "Internet Blocked";

                }
                else
                {
                    // Block internet
                    url = "https://firebasestorage.googleapis.com/v0/b/apps-store-7224f.appspot.com/o/Unblock_Internet.bat?alt=media&token=84067179-a905-4247-a67d-bcfc6e484015";
                    destinationPath = @"C:\Windows\System32\Unblock_Internet.bat";
                    statusText = "Internet Unblocked";
                }

                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(url, destinationPath);
                }

                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = destinationPath,
                        RedirectStandardInput = false,
                        UseShellExecute = false,
                        CreateNoWindow = true // Set this to true to run without showing a window
                    }
                };

                process.Start();
                process.WaitForExit();
                MessageBox.Show(statusText); // Display status using MessageBox
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void guna2ToggleSwitch11_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressToggleEvent) return;
            if (isExecuting) return;
            isExecuting = true;
            bool success = Memlib.getTask(TaskName);
            if (!success)
            {
                st.Text = "Emulator not found";
                st.ForeColor = ColorTranslator.FromHtml("#77bb74");
                if (!muteBeep) Console.Beep(400, 500);
                guna2ToggleSwitch11.Checked = false;
                return;
            }
            try
            {
                if (guna2ToggleSwitch11.Checked)
                {
                    string dllResourceName = "RuntimeBroker.Properties.AGEsp.dll";
                    Inject.visuals(dllResourceName, "AGEsp.dll");
                    if (!muteBeep) Console.Beep(400, 500);
                    st.Text = "Activated";

                }
                else
                    if (!muteBeep) Console.Beep(400, 500);
                st.Text = "Press 'INS'";

            }
            catch (Exception)
            {
                st.Text = dllmsg;
                st.ForeColor = Color.Red;
            }
            finally
            {
                isExecuting = false;
            }
        }
    }
}
