using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Aimguard
{
    #region Aimguard
    public class nexx32
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, IntPtr nSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, IntPtr nSize, IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);


        [DllImport("kernel32.dll")]
        public static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        public static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);


        [DllImport("kernel32.dll")]
        private static extern void GetSystemInfo(out nexx32.SYSTEM_INFO lpSystemInfo);
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32")]
        public static extern bool IsWow64Process(IntPtr hProcess, out bool lpSystemInfo);
        [DllImport("kernel32.dll")]
        private static extern bool VirtualProtectEx(IntPtr hProcess, UIntPtr lpAddress, IntPtr dwSize, nexx32.MemoryProtection flNewProtect, out nexx32.MemoryProtection lpflOldProtect);
        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
        public static extern UIntPtr Native_VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, out nexx32.MEMORY_BASIC_INFORMATION64 lpBuffer, UIntPtr dwLength);
        [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
        public static extern UIntPtr Native_VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, out nexx32.MEMORY_BASIC_INFORMATION32 lpBuffer, UIntPtr dwLength);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);
        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] IntPtr lpBuffer, UIntPtr nSize, out ulong lpNumberOfBytesRead);


        public struct PatternData
        {
            public byte[] pattern { get; set; }

            public byte[] mask { get; set; }
        }

        public struct MemoryPage
        {
            public IntPtr Start;

            public int Size;

            public MemoryPage(IntPtr start, int size)
            {
                Start = start;
                Size = size;
            }
        }

        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;

            public IntPtr AllocationBase;

            public uint AllocationProtect;

            public UIntPtr RegionSize;

            public uint State;

            public uint Protect;

            public uint Type;
        }

        public bool isPrivate;

        public int processId;

        public IntPtr _processHandle;

        private bool _enableCheck = true;

        public const uint MEM_COMMIT = 4096u;

        public const uint MEM_PRIVATE = 131072u;

        public const uint PAGE_READWRITE = 4u;

        public bool getTask(string[] processNames)
        {
            processId = 0;
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                string processName = process.ProcessName;
                if (Array.Exists(processNames, (string name) => name.Equals(processName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    processId = process.Id;
                    break;
                }
            }
            if (processId <= 0)
            {
                return false;
            }
            _processHandle = OpenProcess(ProcessAccessFlags.AllAccess, bInheritHandle: false, processId);
            if (_processHandle == IntPtr.Zero)
            {
                return false;
            }
            return true;
        }
        public void CheckProcess()
        {
            if (!_enableCheck)
            {
                return;
            }
            foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
            {
                IntPtr intPtr = OpenThread(ThreadAccess.SUSPEND_RESUME, bInheritHandle: false, (uint)thread.Id);
                if (intPtr != IntPtr.Zero)
                {
                    int num = 0;
                    do
                    {
                        num = ResumeThread(intPtr);
                    }
                    while (num > 0);
                    CloseHandle(intPtr);
                }
            }
        }

        public async Task<IEnumerable<long>> Trace(string bytePattern)
        {
            return await TraceBytes(bytePattern);
        }

        private async Task<IEnumerable<long>> TraceBytes(string pattern)
        {
            PatternData patternData = GetBytesFromPattern(pattern);
            List<long> addressRet = new List<long>();
            await Task.Run(delegate
            {
                List<MemoryPage> list = new List<MemoryPage>();
                IntPtr intPtr = IntPtr.Zero;
                MEMORY_BASIC_INFORMATION lpBuffer;
                while (VirtualQueryEx(_processHandle, intPtr, out lpBuffer, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) == (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)))
                {
                    if (CanReadPage(lpBuffer))
                    {
                        list.Add(new MemoryPage(intPtr, (int)lpBuffer.RegionSize.ToUInt64()));
                    }
                    intPtr = (IntPtr)((long)lpBuffer.BaseAddress + (long)(ulong)lpBuffer.RegionSize);
                }
                int patternLength = patternData.pattern.Length;
                Parallel.ForEach(list, delegate (MemoryPage addresss)
                {
                    byte[] array = new byte[addresss.Size];
                    if (ReadProcessMemory(_processHandle, addresss.Start, array, (IntPtr)addresss.Size, out var lpNumberOfBytesRead))
                    {
                        int num = -patternLength;
                        do
                        {
                            num = FindPattern(array, patternData.pattern, patternData.mask, num + patternLength);
                            if (num >= 0)
                            {
                                lock (addressRet)
                                {
                                    addressRet.Add((long)addresss.Start + num);
                                }
                            }
                        }
                        while (num != -1);
                    }
                    Array.Resize(ref array, (int)lpNumberOfBytesRead);
                });
            });
            return addressRet.OrderBy((long c) => c).AsEnumerable();
        }

        public bool CanReadPage(MEMORY_BASIC_INFORMATION page)
        {
            if (page.State == 4096 && page.Type == 131072)
            {
                return page.Protect == 4;
            }
            return false;
        }

        private PatternData GetBytesFromPattern(string pattern)
        {
            string[] patternParts = pattern.Split(' ');

            PatternData patternData = new PatternData
            {
                pattern = patternParts.Select(s => s.Contains("??") ? (byte)0x00 : byte.Parse(s, NumberStyles.HexNumber)).ToArray(),
                mask = patternParts.Select(s => s.Contains("??") ? (byte)0x00 : (byte)0xFF).ToArray()
            };

            return patternData;
        }


        public bool SetBytes(long address, string bytePattern)
        {
            try
            {
                byte[] array = StringToByteArray(bytePattern);
                return WriteProcessMemory(_processHandle, (IntPtr)address, array, (IntPtr)array.Length, IntPtr.Zero);
            }
            catch (Exception)
            {
            }
            return false;
        }

        public bool SetBytes(long address, int bytePattern)
        {
            byte[] bytes = BitConverter.GetBytes(bytePattern);
            return WriteProcessMemory(_processHandle, (IntPtr)address, bytes, (IntPtr)bytes.Length, IntPtr.Zero);
        }

        public async Task<int> ReadIntAsync(long addressToRead)
        {
            return await Task.Run(() => ReadInt(addressToRead));
        }

        public int ReadInt(long addressToRead)
        {
            byte[] array = new byte[4];
            if (ReadProcessMemory(_processHandle, (IntPtr)addressToRead, array, (IntPtr)array.Length, out var _))
            {
                return BitConverter.ToInt32(array, 0);
            }
            return 0;
        }

        public float ReadFloat(long addressToRead)
        {
            byte[] array = new byte[4];
            if (ReadProcessMemory(_processHandle, (IntPtr)addressToRead, array, (IntPtr)array.Length, out var _))
            {
                return BitConverter.ToSingle(array, 0);
            }
            return 0f;
        }
        public byte ReadHexByte(long addressToRead)
        {
            byte[] array = new byte[1];
            if (ReadProcessMemory(_processHandle, (IntPtr)addressToRead, array, (IntPtr)array.Length, out var _))
            {
                return array[0];
            }
            return 0;
        }

        public short ReadInt16(long addressToRead)
        {
            byte[] array = new byte[2];
            if (ReadProcessMemory(_processHandle, (IntPtr)addressToRead, array, (IntPtr)array.Length, out var _))
            {
                return BitConverter.ToInt16(array, 0);
            }
            return 0;
        }

        public string ReadString(long addressToRead, int size)
        {
            byte[] buffer = new byte[size];
            IntPtr bytesRead;

            bool readSuccess = ReadProcessMemory(_processHandle, (IntPtr)addressToRead, buffer, (IntPtr)size, out bytesRead);

            if (readSuccess && bytesRead.ToInt64() == size)
            {
                return BitConverter.ToString(buffer).Replace("-", " ");
            }
            return "";
        }
        private byte[] StringToByteArray(string hexString)
        {
            return (from hex in hexString.Split(' ')
                    select byte.Parse(hex, NumberStyles.HexNumber)).ToArray();
        }

        private int FindPattern(byte[] body, byte[] pattern, byte[] masks, int start = 0)
        {
            int result = -1;
            if (body.Length == 0 || pattern.Length == 0 || start > body.Length - pattern.Length || pattern.Length > body.Length)
            {
                return result;
            }
            for (int i = start; i <= body.Length - pattern.Length; i++)
            {
                if ((body[i] & masks[0]) != (pattern[0] & masks[0]))
                {
                    continue;
                }
                bool flag = true;
                for (int num = pattern.Length - 1; num >= 1; num--)
                {
                    if ((body[i + num] & masks[num]) != (pattern[num] & masks[num]))
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    result = i;
                    break;
                }
            }
            return result;
        }


        public string LoadCode(string name, string file)
        {
            StringBuilder stringBuilder = new StringBuilder(1024);
            bool flag = file != "";
            if (flag)
            {
                uint privateProfileString = nexx32.GetPrivateProfileString("codes", name, "", stringBuilder, (uint)stringBuilder.Capacity, file);
            }
            else
            {
                stringBuilder.Append(name);
            }
            return stringBuilder.ToString();
        }

        public byte[] TraceHead(string code, long length, string file = "")
        {
            byte[] array = new byte[length];
            UIntPtr code2 = this.GetCode(code, file, 8);
            bool flag = !nexx32.ReadProcessMemory(this.pHandle, code2, array, (UIntPtr)(checked((ulong)length)), IntPtr.Zero);
            byte[] result;
            if (flag)
            {
                result = null;
            }
            else
            {
                result = array;
            }
            return result;
        }

        public string MSize()
        {
            bool is64Bit = this.Is64Bit;
            string result;
            if (is64Bit)
            {
                result = "x16";
            }
            else
            {
                result = "x8";
            }
            return result;
        }
        public void CloseProcess()
        {
            IntPtr intPtr = this.pHandle;
            bool flag = false;
            if (!flag)
            {
                nexx32.CloseHandle(this.pHandle);
                this.theProc = null;
            }
        }
        private bool _is64Bit;
        public bool Is64Bit
        {
            get
            {
                return this._is64Bit;
            }
            private set
            {
                this._is64Bit = value;
            }
        }
        public static void notify(string message)
        {
            Process.Start(new ProcessStartInfo("cmd.exe", $"/c start cmd /C \"color b && title Error && echo {message} && timeout /t 5\"")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
            Environment.Exit(0);
        }
        public UIntPtr Get64BitCode(string name, string path = "", int size = 16)
        {
            bool flag = path != "";
            string text;
            if (flag)
            {
                text = this.LoadCode(name, path);
            }
            else
            {
                text = name;
            }
            bool flag2 = text == "";
            UIntPtr result;
            if (flag2)
            {
                result = UIntPtr.Zero;
            }
            else
            {
                bool flag3 = text.Contains(" ");
                if (flag3)
                {
                    text.Replace(" ", string.Empty);
                }
                string text2 = text;
                bool flag4 = text.Contains("+");
                if (flag4)
                {
                    text2 = text.Substring(text.IndexOf('+') + 1);
                }
                byte[] array = new byte[size];
                bool flag5 = !text.Contains("+") && !text.Contains(",");
                if (flag5)
                {
                    result = new UIntPtr(Convert.ToUInt64(text, 16));
                }
                else
                {
                    bool flag6 = text2.Contains(',');
                    if (flag6)
                    {
                        List<long> list = new List<long>();
                        string[] array2 = text2.Split(new char[]
                        {
                            ','
                        });
                        foreach (string text3 in array2)
                        {
                            string text4 = text3;
                            bool flag7 = text3.Contains("0x");
                            if (flag7)
                            {
                                text4 = text3.Replace("0x", "");
                            }
                            bool flag8 = !text3.Contains("-");
                            long num;
                            if (flag8)
                            {
                                num = long.Parse(text4, NumberStyles.AllowHexSpecifier);
                            }
                            else
                            {
                                text4 = text4.Replace("-", "");
                                num = long.Parse(text4, NumberStyles.AllowHexSpecifier);
                                num *= -1L;
                            }
                            list.Add(num);
                        }
                        long[] array4 = list.ToArray();
                        bool flag9 = text.Contains("base") || text.Contains("main");
                        if (flag9)
                        {
                            nexx32.ReadProcessMemory(this.pHandle, (UIntPtr)((ulong)((long)this.mainModule.BaseAddress + array4[0])), array, (UIntPtr)((ulong)((long)size)), IntPtr.Zero);
                        }
                        else
                        {
                            bool flag10 = !text.Contains("base") && !text.Contains("main") && text.Contains("+");
                            if (flag10)
                            {
                                string[] array5 = text.Split(new char[]
                                {
                                    '+'
                                });
                                IntPtr value = IntPtr.Zero;
                                bool flag11 = !array5[0].ToLower().Contains(".dll") && !array5[0].ToLower().Contains(".exe") && !array5[0].ToLower().Contains(".bin");
                                if (flag11)
                                {
                                    value = (IntPtr)long.Parse(array5[0], NumberStyles.HexNumber);
                                }
                                else
                                {
                                    try
                                    {
                                        value = this.modules[array5[0]];
                                    }
                                    catch
                                    {
                                        Debug.WriteLine("Module " + array5[0] + " was not found in module list!");
                                        Debug.WriteLine("Modules: " + string.Join<KeyValuePair<string, IntPtr>>(",", this.modules));
                                    }
                                }
                                nexx32.ReadProcessMemory(this.pHandle, (UIntPtr)((ulong)((long)value + array4[0])), array, (UIntPtr)((ulong)((long)size)), IntPtr.Zero);
                            }
                            else
                            {
                                nexx32.ReadProcessMemory(this.pHandle, (UIntPtr)((ulong)array4[0]), array, (UIntPtr)((ulong)((long)size)), IntPtr.Zero);
                            }
                        }
                        long num2 = BitConverter.ToInt64(array, 0);
                        UIntPtr uintPtr = (UIntPtr)0UL;
                        for (int j = 1; j < array4.Length; j++)
                        {
                            uintPtr = new UIntPtr(Convert.ToUInt64(num2 + array4[j]));
                            nexx32.ReadProcessMemory(this.pHandle, uintPtr, array, (UIntPtr)((ulong)((long)size)), IntPtr.Zero);
                            num2 = BitConverter.ToInt64(array, 0);
                        }
                        result = uintPtr;
                    }
                    else
                    {
                        long num3 = Convert.ToInt64(text2, 16);
                        IntPtr value2 = IntPtr.Zero;
                        bool flag12 = text.Contains("base") || text.Contains("main");
                        if (flag12)
                        {
                            value2 = this.mainModule.BaseAddress;
                        }
                        else
                        {
                            bool flag13 = !text.Contains("base") && !text.Contains("main") && text.Contains("+");
                            if (flag13)
                            {
                                string[] array6 = text.Split(new char[]
                                {
                                    '+'
                                });
                                bool flag14 = !array6[0].ToLower().Contains(".dll") && !array6[0].ToLower().Contains(".exe") && !array6[0].ToLower().Contains(".bin");
                                if (flag14)
                                {
                                    string text5 = array6[0];
                                    bool flag15 = text5.Contains("0x");
                                    if (flag15)
                                    {
                                        text5 = text5.Replace("0x", "");
                                    }
                                    value2 = (IntPtr)long.Parse(text5, NumberStyles.HexNumber);
                                }
                                else
                                {
                                    try
                                    {
                                        value2 = this.modules[array6[0]];
                                    }
                                    catch
                                    {
                                        Debug.WriteLine("Module " + array6[0] + " was not found in module list!");
                                        Debug.WriteLine("Modules: " + string.Join<KeyValuePair<string, IntPtr>>(",", this.modules));
                                    }
                                }
                            }
                            else
                            {
                                value2 = this.modules[text.Split(new char[]
                                {
                                    '+'
                                })[0]];
                            }
                        }
                        result = (UIntPtr)((ulong)((long)value2 + num3));
                    }
                }
            }
            return result;
        }
        public UIntPtr GetCode(string name, string path = "", int size = 8)
        {
            bool is64Bit = this.Is64Bit;
            UIntPtr result;
            if (is64Bit)
            {
                bool flag = size == 8;
                if (flag)
                {
                    size = 16;
                }
                result = this.Get64BitCode(name, path, size);
            }
            else
            {
                bool flag2 = path != "";
                string text;
                if (flag2)
                {
                    text = this.LoadCode(name, path);
                }
                else
                {
                    text = name;
                }
                bool flag3 = text == "";
                if (flag3)
                {
                    result = UIntPtr.Zero;
                }
                else
                {
                    bool flag4 = text.Contains(" ");
                    if (flag4)
                    {
                        text.Replace(" ", string.Empty);
                    }
                    bool flag5 = !text.Contains("+") && !text.Contains(",");
                    if (flag5)
                    {
                        result = new UIntPtr(Convert.ToUInt32(text, 16));
                    }
                    else
                    {
                        string text2 = text;
                        bool flag6 = text.Contains("+");
                        if (flag6)
                        {
                            text2 = text.Substring(text.IndexOf('+') + 1);
                        }
                        byte[] array = new byte[size];
                        bool flag7 = text2.Contains(',');
                        if (flag7)
                        {
                            List<int> list = new List<int>();
                            string[] array2 = text2.Split(new char[]
                            {
                                ','
                            });
                            foreach (string text3 in array2)
                            {
                                string text4 = text3;
                                bool flag8 = text3.Contains("0x");
                                if (flag8)
                                {
                                    text4 = text3.Replace("0x", "");
                                }
                                bool flag9 = !text3.Contains("-");
                                int num;
                                if (flag9)
                                {
                                    num = int.Parse(text4, NumberStyles.AllowHexSpecifier);
                                }
                                else
                                {
                                    text4 = text4.Replace("-", "");
                                    num = int.Parse(text4, NumberStyles.AllowHexSpecifier);
                                    num *= -1;
                                }
                                list.Add(num);
                            }
                            int[] array4 = list.ToArray();
                            bool flag10 = text.Contains("base") || text.Contains("main");
                            if (flag10)
                            {
                                nexx32.ReadProcessMemory(this.pHandle, (UIntPtr)((ulong)((long)((int)this.mainModule.BaseAddress + array4[0]))), array, (UIntPtr)((ulong)((long)size)), IntPtr.Zero);
                            }
                            else
                            {
                                bool flag11 = !text.Contains("base") && !text.Contains("main") && text.Contains("+");
                                if (flag11)
                                {
                                    string[] array5 = text.Split(new char[]
                                    {
                                        '+'
                                    });
                                    IntPtr value = IntPtr.Zero;
                                    bool flag12 = !array5[0].ToLower().Contains(".dll") && !array5[0].ToLower().Contains(".exe") && !array5[0].ToLower().Contains(".bin");
                                    if (flag12)
                                    {
                                        string text5 = array5[0];
                                        bool flag13 = text5.Contains("0x");
                                        if (flag13)
                                        {
                                            text5 = text5.Replace("0x", "");
                                        }
                                        value = (IntPtr)int.Parse(text5, NumberStyles.HexNumber);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            value = this.modules[array5[0]];
                                        }
                                        catch
                                        {
                                            Debug.WriteLine("Module " + array5[0] + " was not found in module list!");
                                            Debug.WriteLine("Modules: " + string.Join<KeyValuePair<string, IntPtr>>(",", this.modules));
                                        }
                                    }
                                    nexx32.ReadProcessMemory(this.pHandle, (UIntPtr)((ulong)((long)((int)value + array4[0]))), array, (UIntPtr)((ulong)((long)size)), IntPtr.Zero);
                                }
                                else
                                {
                                    nexx32.ReadProcessMemory(this.pHandle, (UIntPtr)((ulong)((long)array4[0])), array, (UIntPtr)((ulong)((long)size)), IntPtr.Zero);
                                }
                            }
                            uint num2 = BitConverter.ToUInt32(array, 0);
                            UIntPtr uintPtr = (UIntPtr)0UL;
                            for (int j = 1; j < array4.Length; j++)
                            {
                                uintPtr = new UIntPtr(Convert.ToUInt32((long)((ulong)num2 + (ulong)((long)array4[j]))));
                                nexx32.ReadProcessMemory(this.pHandle, uintPtr, array, (UIntPtr)((ulong)((long)size)), IntPtr.Zero);
                                num2 = BitConverter.ToUInt32(array, 0);
                            }
                            result = uintPtr;
                        }
                        else
                        {
                            int num3 = Convert.ToInt32(text2, 16);
                            IntPtr value2 = IntPtr.Zero;
                            bool flag14 = text.ToLower().Contains("base") || text.ToLower().Contains("main");
                            if (flag14)
                            {
                                value2 = this.mainModule.BaseAddress;
                            }
                            else
                            {
                                bool flag15 = !text.ToLower().Contains("base") && !text.ToLower().Contains("main") && text.Contains("+");
                                if (flag15)
                                {
                                    string[] array6 = text.Split(new char[]
                                    {
                                        '+'
                                    });
                                    bool flag16 = !array6[0].ToLower().Contains(".dll") && !array6[0].ToLower().Contains(".exe") && !array6[0].ToLower().Contains(".bin");
                                    if (flag16)
                                    {
                                        string text6 = array6[0];
                                        bool flag17 = text6.Contains("0x");
                                        if (flag17)
                                        {
                                            text6 = text6.Replace("0x", "");
                                        }
                                        value2 = (IntPtr)int.Parse(text6, NumberStyles.HexNumber);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            value2 = this.modules[array6[0]];
                                        }
                                        catch
                                        {
                                            Debug.WriteLine("Module " + array6[0] + " was not found in module list!");
                                            Debug.WriteLine("Modules: " + string.Join<KeyValuePair<string, IntPtr>>(",", this.modules));
                                        }
                                    }
                                }
                                else
                                {
                                    value2 = this.modules[text.Split(new char[]
                                    {
                                        '+'
                                    })[0]];
                                }
                            }
                            result = (UIntPtr)((ulong)((long)((int)value2 + num3)));
                        }
                    }
                }
            }
            return result;
        }
        public bool SetHeadBytes(string code, string type, string write, string file = "", Encoding stringEncoding = null)
        {
            byte[] array = new byte[4];
            int num = 4;
            UIntPtr code2 = this.GetCode(code, file, 8);
            bool flag = type.ToLower() == "float";
            if (flag)
            {
                array = BitConverter.GetBytes(Convert.ToSingle(write));
                num = 4;
            }
            else
            {
                bool flag2 = type.ToLower() == "int";
                if (flag2)
                {
                    array = BitConverter.GetBytes(Convert.ToInt32(write));
                    num = 4;
                }
                else
                {
                    bool flag3 = type.ToLower() == "byte";
                    if (flag3)
                    {
                        array = new byte[]
                        {
                            Convert.ToByte(write, 16)
                        };
                        num = 1;
                    }
                    else
                    {
                        bool flag4 = type.ToLower() == "2bytes";
                        if (flag4)
                        {
                            array = new byte[]
                            {
                                (byte)(Convert.ToInt32(write) % 256),
                                (byte)(Convert.ToInt32(write) / 256)
                            };
                            num = 2;
                        }
                        else
                        {
                            bool flag5 = type.ToLower() == "bytes";
                            if (flag5)
                            {
                                bool flag6 = write.Contains(",") || write.Contains(" ");
                                if (flag6)
                                {
                                    bool flag7 = write.Contains(",");
                                    string[] array2;
                                    if (flag7)
                                    {
                                        array2 = write.Split(new char[]
                                        {
                                            ','
                                        });
                                    }
                                    else
                                    {
                                        array2 = write.Split(new char[]
                                        {
                                            ' '
                                        });
                                    }
                                    int num2 = array2.Count<string>();
                                    array = new byte[num2];
                                    for (int i = 0; i < num2; i++)
                                    {
                                        array[i] = Convert.ToByte(array2[i], 16);
                                    }
                                    num = array2.Count<string>();
                                }
                                else
                                {
                                    array = new byte[]
                                    {
                                        Convert.ToByte(write, 16)
                                    };
                                    num = 1;
                                }
                            }
                            else
                            {
                                bool flag8 = type.ToLower() == "double";
                                if (flag8)
                                {
                                    array = BitConverter.GetBytes(Convert.ToDouble(write));
                                    num = 8;
                                }
                                else
                                {
                                    bool flag9 = type.ToLower() == "long";
                                    if (flag9)
                                    {
                                        array = BitConverter.GetBytes(Convert.ToInt64(write));
                                        num = 8;
                                    }
                                    else
                                    {
                                        bool flag10 = type.ToLower() == "string";
                                        if (flag10)
                                        {
                                            bool flag11 = stringEncoding == null;
                                            if (flag11)
                                            {
                                                array = Encoding.UTF8.GetBytes(write);
                                            }
                                            else
                                            {
                                                array = stringEncoding.GetBytes(write);
                                            }
                                            num = array.Length;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return nexx32.WriteProcessMemory(this.pHandle, code2, array, (UIntPtr)((ulong)((long)num)), IntPtr.Zero);
        }
        public bool IsAdmin()
        {
            bool result;
            using (WindowsIdentity current = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
                result = windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            return result;
        }
        public bool OpenProcess(int pid)
        {
            bool flag = !this.IsAdmin();
            if (flag)
            {
                Debug.WriteLine("WARNING: You are NOT running this program as admin! Visit https://discord.com/invite/B7TmWxdgpj");
                notify("WARNING: You are NOT running this program as admin! For More Help Visit https://discord.com/invite/B7TmWxdgpj");
            }
            bool flag2 = pid <= 0;
            bool result;
            if (flag2)
            {
                Debug.WriteLine("ERROR: OpenProcess given proc ID 0.");
                result = false;
            }
            else
            {
                bool flag3 = this.theProc != null && this.theProc.Id == pid;
                if (flag3)
                {
                    result = true;
                }
                else
                {
                    try
                    {
                        this.theProc = Process.GetProcessById(pid);
                        bool flag4 = this.theProc != null && !this.theProc.Responding;
                        if (flag4)
                        {
                            Debug.WriteLine("ERROR: OpenProcess: Process is not responding or null.");
                            result = false;
                        }
                        else
                        {
                            this.pHandle = nexx32.OpenProcess(2035711U, true, pid);
                            Process.EnterDebugMode();
                            bool flag5 = this.pHandle == IntPtr.Zero;
                            if (flag5)
                            {
                                Debug.WriteLine("ERROR: OpenProcess has failed opening a handle to the target process (GetLastWin32ErrorCode: " + Marshal.GetLastWin32Error().ToString() + ")");
                                Process.LeaveDebugMode();
                                this.theProc = null;
                                result = false;
                            }
                            else
                            {
                                this.mainModule = this.theProc.MainModule;
                                this.GetModules();
                                bool flag6;
                                this.Is64Bit = (Environment.Is64BitOperatingSystem && nexx32.IsWow64Process(this.pHandle, out flag6) && !flag6);
                                string str = "Program is operating at Administrative level. Process #";
                                Process process = this.theProc;
                                Debug.WriteLine(str + ((process != null) ? process.ToString() : null) + " is open and modules are stored.");
                                result = true;
                            }
                        }
                    }
                    catch
                    {
                        result = false;
                    }
                }
            }
            return result;
        }
        public void GetModules()
        {
            bool flag = this.theProc == null;
            if (!flag)
            {
                this.modules.Clear();
                foreach (object obj in this.theProc.Modules)
                {
                    ProcessModule processModule = (ProcessModule)obj;
                    bool flag2 = !string.IsNullOrEmpty(processModule.ModuleName) && !this.modules.ContainsKey(processModule.ModuleName);
                    if (flag2)
                    {
                        this.modules.Add(processModule.ModuleName, processModule.BaseAddress);
                    }
                }
            }
        }



        private Dictionary<string, IntPtr> modules = new Dictionary<string, IntPtr>();
        private ProcessModule mainModule;
        public Process theProc = null;
        public IntPtr pHandle;
        [Flags]
        public enum ThreadAccess
        {
            TERMINATE = 1,
            SUSPEND_RESUME = 2,
            GET_CONTEXT = 8,
            SET_CONTEXT = 16,
            SET_INFORMATION = 32,
            QUERY_INFORMATION = 64,
            SET_THREAD_TOKEN = 128,
            IMPERSONATE = 256,
            DIRECT_IMPERSONATION = 512
        }
        public struct MEMORY_BASIC_INFORMATION32
        {
            public UIntPtr BaseAddress;

            public UIntPtr AllocationBase;

            public uint AllocationProtect;

            public uint RegionSize;

            public uint State;

            public uint Protect;

            public uint Type;
        }
        public struct MEMORY_BASIC_INFORMATION64
        {
            public UIntPtr BaseAddress;

            public UIntPtr AllocationBase;

            public uint AllocationProtect;

            public uint __alignment1;

            public ulong RegionSize;

            public uint State;

            public uint Protect;

            public uint Type;

            public uint __alignment2;
        }
        [Flags]
        public enum MemoryProtection : uint
        {
            Execute = 16U,
            ExecuteRead = 32U,
            ExecuteReadWrite = 64U,
            ExecuteWriteCopy = 128U,
            NoAccess = 1U,
            ReadOnly = 2U,
            ReadWrite = 4U,
            WriteCopy = 8U,
            GuardModifierflag = 256U,
            NoCacheModifierflag = 512U,
            WriteCombineModifierflag = 1024U
        }
        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;

            public uint pageSize;

            public UIntPtr minimumApplicationAddress;

            public UIntPtr maximumApplicationAddress;

            public IntPtr activeProcessorMask;

            public uint numberOfProcessors;

            public uint processorType;

            public uint allocationGranularity;

            public ushort processorLevel;

            public ushort processorRevision;
        }

    }


    #region ProcessAccessFlags
    /// <summary>
    /// Process access rights list.
    /// </summary>
    [Flags]
    public enum ProcessAccessFlags
    {
        /// <summary>
        /// All possible access rights for a process object.
        /// </summary>
        AllAccess = 0x001F0FFF,
        /// <summary>
        /// Required to create a process.
        /// </summary>
        CreateProcess = 0x0080,
        /// <summary>
        /// Required to create a thread.
        /// </summary>
        CreateThread = 0x0002,
        /// <summary>
        /// Required to duplicate a handle using DuplicateHandle.
        /// </summary>
        DupHandle = 0x0040,
        /// <summary>
        /// Required to retrieve certain information about a process, such as its token, exit code, and priority class (see OpenProcessToken).
        /// </summary>
        QueryInformation = 0x0400,
        /// <summary>
        /// Required to retrieve certain information about a process (see GetExitCodeProcess, GetPriorityClass, IsProcessInJob, QueryFullProcessImageName). 
        /// A handle that has the PROCESS_QUERY_INFORMATION access right is automatically granted PROCESS_QUERY_LIMITED_INFORMATION.
        /// </summary>
        QueryLimitedInformation = 0x1000,
        /// <summary>
        /// Required to set certain information about a process, such as its priority class (see SetPriorityClass).
        /// </summary>
        SetInformation = 0x0200,
        /// <summary>
        /// Required to set memory limits using getTaskWorkingSetSize.
        /// </summary>
        SetQuota = 0x0100,
        /// <summary>
        /// Required to suspend or resume a process.
        /// </summary>
        SuspendResume = 0x0800,
        /// <summary>
        /// Required to terminate a process using TerminateProcess.
        /// </summary>
        Terminate = 0x0001,
        /// <summary>
        /// Required to perform an operation on the address space of a process (see VirtualProtectEx and WriteProcessMemory).
        /// </summary>
        VmOperation = 0x0008,
        /// <summary>
        /// Required to read memory in a process using <see cref="ReadProcessMemory"/>.
        /// </summary>
        VmRead = 0x0010,
        /// <summary>
        /// Required to write to memory in a process using WriteProcessMemory.
        /// </summary>
        VmWrite = 0x0020,
        /// <summary>
        /// Required to wait for the process to terminate using the wait functions.
        /// </summary>
        Synchronize = 0x00100000
    }
    #endregion


    [Flags]
    public enum ThreadAccess : int
    {
        TERMINATE = (0x0001),
        SUSPEND_RESUME = (0x0002),
        GET_CONTEXT = (0x0008),
        SET_CONTEXT = (0x0010),
        SET_INFORMATION = (0x0020),
        QUERY_INFORMATION = (0x0040),
        SET_THREAD_TOKEN = (0x0080),
        IMPERSONATE = (0x0100),
        DIRECT_IMPERSONATION = (0x0200)
    }

    public enum AllocationProtectEnum : uint
    {
        PAGE_EXECUTE = 0x00000010,
        PAGE_EXECUTE_READ = 0x00000020,
        PAGE_EXECUTE_READWRITE = 0x00000040,
        PAGE_EXECUTE_WRITECOPY = 0x00000080,
        PAGE_NOACCESS = 0x00000001,
        PAGE_READONLY = 0x00000002,
        PAGE_READWRITE = 0x00000004,
        PAGE_WRITECOPY = 0x00000008,
        PAGE_GUARD = 0x00000100,
        PAGE_NOCACHE = 0x00000200,
        PAGE_WRITECOMBINE = 0x00000400
    }

    public enum StateEnum : uint
    {
        MEM_COMMIT = 0x1000,
        MEM_FREE = 0x10000,
        MEM_RESERVE = 0x2000
    }
    public enum TypeEnum : uint
    {
        MEM_IMAGE = 0x1000000,
        MEM_MAPPED = 0x40000,
        MEM_PRIVATE = 0x20000
    }
    #endregion
    #region ag-region-result
    internal struct MemoryRegionResult
    {
        public UIntPtr CurrentBaseAddress { get; set; }

        public long RegionSize { get; set; }

        public UIntPtr RegionBase { get; set; }
    }
    #endregion
}