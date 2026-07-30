using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace REDXMem
{
    public class REDX
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
        public static extern IntPtr OpenProcessDll(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemoryDll(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

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

        private void InjectDll(string DLLPath)
        {
            Process[] processes = Process.GetProcessesByName("HD-Player");
            if (processes.Length > 0)
            {
                int processId = processes[0].Id;

                IntPtr hProcess = OpenProcessDll(0x1F0FFF, false, processId);

                if (hProcess != IntPtr.Zero)
                {
                    byte[] buffer = System.Text.Encoding.ASCII.GetBytes(DLLPath);

                    IntPtr remoteBuffer = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)buffer.Length, 0x1000, 0x40);

                    int bytesWritten;

                    WriteProcessMemoryDll(hProcess, remoteBuffer, buffer, (uint)buffer.Length, out bytesWritten);

                    IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

                    IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, remoteBuffer, 0, IntPtr.Zero);

                    WaitForSingleObject(hThread, 0xFFFFFFFF);

                    CloseHandle(hProcess);
                }
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

        public bool SetProcess(string[] processNames)
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
                return;

            foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
            {
                IntPtr intPtr = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
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

        public async Task<IEnumerable<long>> AoBScan(string bytePattern)
        {
            return await AobScan(bytePattern);
        }

        private async Task<IEnumerable<long>> AobScan(string pattern)
        {
            PatternData patternData = GetPatternDataFromPattern(pattern);
            List<long> addressRet = new List<long>();

            await Task.Run(() =>
            {
                List<MemoryPage> pages = new List<MemoryPage>();
                IntPtr address = IntPtr.Zero;
                MEMORY_BASIC_INFORMATION mbInfo;

                while (VirtualQueryEx(_processHandle, address, out mbInfo, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) == (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)))
                {
                    if (CanReadPage(mbInfo))
                    {
                        pages.Add(new MemoryPage(address, (int)mbInfo.RegionSize.ToUInt64()));
                    }
                    address = (IntPtr)((long)mbInfo.BaseAddress + (long)mbInfo.RegionSize);
                }

                int patternLength = patternData.pattern.Length;

                Parallel.ForEach(pages, page =>
                {
                    byte[] buffer = new byte[page.Size];
                    if (ReadProcessMemory(_processHandle, page.Start, buffer, (IntPtr)page.Size, out var bytesRead))
                    {
                        int offset = -patternLength;
                        do
                        {
                            offset = FindPattern(buffer, patternData.pattern, patternData.mask, offset + patternLength);
                            if (offset >= 0)
                            {
                                lock (addressRet)
                                {
                                    addressRet.Add((long)page.Start + offset);
                                }
                            }
                        } while (offset != -1);
                    }
                    Array.Resize(ref buffer, (int)bytesRead);
                });
            });

            return addressRet.OrderBy(addr => addr).AsEnumerable();
        }

        public bool CanReadPage(MEMORY_BASIC_INFORMATION page)
        {
            if (page.State == MEM_COMMIT && page.Type == MEM_PRIVATE)
            {
                return page.Protect == PAGE_READWRITE;
            }
            return false;
        }

        private PatternData GetPatternDataFromPattern(string pattern)
        {
            string[] parts = pattern.Split(' ');
            return new PatternData
            {
                pattern = parts.Select(s => s.Contains("??") ? (byte)0x00 : byte.Parse(s, NumberStyles.HexNumber)).ToArray(),
                mask = parts.Select(s => s.Contains("??") ? (byte)0x00 : (byte)0xFF).ToArray()
            };
        }

        public bool AobReplace(long address, string bytePattern)
        {
            try
            {
                byte[] bytes = StringToByteArray(bytePattern);
                return WriteProcessMemory(_processHandle, (IntPtr)address, bytes, (IntPtr)bytes.Length, IntPtr.Zero);
            }
            catch
            {
                return false;
            }
        }

        public bool AobReplace(long address, int bytePattern)
        {
            byte[] bytes = BitConverter.GetBytes(bytePattern);
            return WriteProcessMemory(_processHandle, (IntPtr)address, bytes, (IntPtr)bytes.Length, IntPtr.Zero);
        }

        public async Task<int> ReadIntAsync(long address)
        {
            return await Task.Run(() => ReadInt(address));
        }

        public int ReadInt(long address)
        {
            byte[] buffer = new byte[4];
            if (ReadProcessMemory(_processHandle, (IntPtr)address, buffer, (IntPtr)buffer.Length, out _))
            {
                return BitConverter.ToInt32(buffer, 0);
            }
            return 0;
        }

        public float ReadFloat(long address)
        {
            byte[] buffer = new byte[4];
            if (ReadProcessMemory(_processHandle, (IntPtr)address, buffer, (IntPtr)buffer.Length, out _))
            {
                return BitConverter.ToSingle(buffer, 0);
            }
            return 0f;
        }

        public byte ReadHexByte(long address)
        {
            byte[] buffer = new byte[1];
            if (ReadProcessMemory(_processHandle, (IntPtr)address, buffer, (IntPtr)buffer.Length, out _))
            {
                return buffer[0];
            }
            return 0;
        }

        public short ReadInt16(long address)
        {
            byte[] buffer = new byte[2];
            if (ReadProcessMemory(_processHandle, (IntPtr)address, buffer, (IntPtr)buffer.Length, out _))
            {
                return BitConverter.ToInt16(buffer, 0);
            }
            return 0;
        }

        public string ReadString(long address, int size)
        {
            byte[] buffer = new byte[size];
            IntPtr bytesRead;
            bool success = ReadProcessMemory(_processHandle, (IntPtr)address, buffer, (IntPtr)size, out bytesRead);
            if (success && bytesRead.ToInt64() == size)
            {
                return BitConverter.ToString(buffer).Replace("-", " ");
            }
            return "";
        }

        private byte[] StringToByteArray(string hexString)
        {
            return hexString.Split(' ').Select(hex => byte.Parse(hex, NumberStyles.HexNumber)).ToArray();
        }

        private int FindPattern(byte[] body, byte[] pattern, byte[] masks, int start = 0)
        {
            int result = -1;
            if (body.Length == 0 || pattern.Length == 0 || start > body.Length - pattern.Length || pattern.Length > body.Length)
                return result;

            for (int i = start; i <= body.Length - pattern.Length; i++)
            {
                if ((body[i] & masks[0]) != (pattern[0] & masks[0]))
                    continue;

                bool found = true;
                for (int j = pattern.Length - 1; j >= 1; j--)
                {
                    if ((body[i + j] & masks[j]) != (pattern[j] & masks[j]))
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    result = i;
                    break;
                }
            }
            return result;
        }
    }


    #region ProcessAccessFlags
    [Flags]
    public enum ProcessAccessFlags
    {
        AllAccess = 0x001F0FFF,
        CreateProcess = 0x0080,
        CreateThread = 0x0002,
        DupHandle = 0x0040,
        QueryInformation = 0x0400,
        QueryLimitedInformation = 0x1000,
        SetInformation = 0x0200,
        SetQuota = 0x0100,
        SuspendResume = 0x0800,
        Terminate = 0x0001,
        VmOperation = 0x0008,
        VmRead = 0x0010,
        VmWrite = 0x0020,
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
}