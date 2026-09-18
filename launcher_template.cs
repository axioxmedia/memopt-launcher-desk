using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

internal static class Native
{
    public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    public const uint TOKEN_QUERY = 0x0008;
    public const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    public const uint TH32CS_SNAPPROCESS = 0x00000002;
    public const uint PROCESS_QUERY_INFORMATION = 0x0400;
    public const uint PROCESS_SET_QUOTA = 0x0100;
    public const uint PROCESS_SET_INFORMATION = 0x0200;
    public const uint SYNCHRONIZE = 0x00100000;
    public const uint HIGH_PRIORITY_CLASS = 0x00000080;
    public const uint WAIT_TIMEOUT = 0x00000102;
    public const uint WAIT_OBJECT_0 = 0;
    public const int SystemMemoryListInformation = 80;
    public const int MemoryEmptyWorkingSets = 2;
    public const int MemoryFlushModifiedList = 3;
    public const int MemoryPurgeStandbyList = 4;
    public const uint SEE_MASK_NOCLOSEPROCESS = 0x00000040;

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct PROCESSENTRY32W
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public UIntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFOW
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SID_IDENTIFIER_AUTHORITY
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] Value;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SHELLEXECUTEINFOW
    {
        public int cbSize;
        public uint fMask;
        public IntPtr hwnd;
        public string lpVerb;
        public string lpFile;
        public string lpParameters;
        public string lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        public string lpClass;
        public IntPtr hkeyClass;
        public uint dwHotKey;
        public IntPtr hIcon;
        public IntPtr hProcess;
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool LookupPrivilegeValueW(string lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentProcessId();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern uint GetModuleFileNameW(IntPtr hModule, StringBuilder lpFilename, int nSize);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern uint GetFileAttributesW(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool Process32FirstW(IntPtr hSnapshot, ref PROCESSENTRY32W lppe);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool Process32NextW(IntPtr hSnapshot, ref PROCESSENTRY32W lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CreateProcessW(string lpApplicationName, StringBuilder lpCommandLine,
        IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags,
        IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFOW lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetPriorityClass(IntPtr hProcess, uint dwPriorityClass);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateMutexW(IntPtr lpMutexAttributes, bool bInitialOwner, string lpName);

    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();

    [DllImport("kernel32.dll")]
    public static extern bool ReleaseMutex(IntPtr hMutex);

    [DllImport("psapi.dll", SetLastError = true)]
    public static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("ntdll.dll")]
    public static extern int NtSetSystemInformation(int SystemInformationClass, ref int SystemInformation, int SystemInformationLength);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool AllocateAndInitializeSid(ref SID_IDENTIFIER_AUTHORITY pIdentifierAuthority,
        byte nSubAuthorityCount, uint nSubAuthority0, uint nSubAuthority1, uint nSubAuthority2, uint nSubAuthority3,
        uint nSubAuthority4, uint nSubAuthority5, uint nSubAuthority6, uint nSubAuthority7, out IntPtr pSid);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool CheckTokenMembership(IntPtr TokenHandle, IntPtr SidToCheck, out bool IsMember);

    [DllImport("advapi32.dll")]
    public static extern IntPtr FreeSid(IntPtr pSid);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool ShellExecuteExW(ref SHELLEXECUTEINFOW lpExecInfo);
}

internal static class Program
{
    const string Mode = "{{MODE}}";
    const string Target = "{{TARGET}}";
    const string InstanceMutex = @"{{MUTEX}}";
    const string GameProcessName = "{{PROCESS_NAME}}";
    const uint GameAppearTimeoutMs = 2 * 60 * 1000;
    const uint GameAppearPollMs = 250;
    const uint OptimizeIntervalMs = 60 * 1000;
    const uint RelaunchGraceMs = 30 * 1000;

    static readonly string[] ProtectedNames = new string[]
    {
        GameProcessName,
        "{{SELF_EXE}}",
        "steam.exe",
        "steamwebhelper.exe",
        "steamservice.exe",
        "GameOverlayUI.exe",
        "csrss.exe",
        "smss.exe",
        "wininit.exe",
        "winlogon.exe",
        "services.exe",
        "lsass.exe",
        "svchost.exe",
        "System",
        "Registry",
        "Memory Compression",
        "MemOptLauncherDesk.exe"
    };

    static readonly string[] SkipDirNames = new string[]
    {
        "Windows", "Windows.old", "WinSxS", "$Recycle.Bin", "System Volume Information",
        "ProgramData", "Recovery", "Boot", "Intel", "AMD", "NVIDIA", "node_modules"
    };

    [STAThread]
    static int Main()
    {
        IntPtr hMutex = Native.CreateMutexW(IntPtr.Zero, true, InstanceMutex);
        if (hMutex != IntPtr.Zero && Native.GetLastError() == 183)
        {
            Native.CloseHandle(hMutex);
            return 0;
        }

        string exePath = GetSelfPath();
        if (!IsRunAsAdmin())
        {
            if (hMutex != IntPtr.Zero)
            {
                Native.ReleaseMutex(hMutex);
                Native.CloseHandle(hMutex);
            }
            Native.SHELLEXECUTEINFOW sei = new Native.SHELLEXECUTEINFOW();
            sei.cbSize = Marshal.SizeOf(typeof(Native.SHELLEXECUTEINFOW));
            sei.lpVerb = "runas";
            sei.lpFile = exePath;
            sei.nShow = 1;
            if (!Native.ShellExecuteExW(ref sei))
                return 1;
            return 0;
        }

        OptimizeMemoryFull();

        IntPtr hGame = IntPtr.Zero;
        if (Mode == "url")
        {
            LaunchUri(Target);
            if (!string.IsNullOrEmpty(GameProcessName))
                hGame = WaitForGameProcess(GameAppearTimeoutMs);
            WatchUntilExit(hGame, allowResident: true);
        }
        else
        {
            string gamePath;
            string gameDir;
            bool found = ResolveGamePath(out gamePath, out gameDir);
            uint existing = 0;
            if (!string.IsNullOrEmpty(GameProcessName))
                existing = FindProcessIdByName(GameProcessName);
            if (existing != 0)
            {
                hGame = OpenGameProcess(existing);
            }
            else if (found)
            {
                hGame = LaunchGame(gamePath, gameDir);
                if (hGame == IntPtr.Zero)
                    hGame = WaitForGameProcess(GameAppearTimeoutMs);
            }
            else
            {
                hGame = WaitForGameProcess(GameAppearTimeoutMs);
            }
            if (hGame == IntPtr.Zero)
            {
                CleanupMutex(hMutex);
                return 2;
            }
            WatchUntilExit(hGame, allowResident: false);
        }

        CleanupMutex(hMutex);
        return 0;
    }

    static void CleanupMutex(IntPtr hMutex)
    {
        if (hMutex == IntPtr.Zero) return;
        Native.ReleaseMutex(hMutex);
        Native.CloseHandle(hMutex);
    }

    static string GetSelfPath()
    {
        StringBuilder sb = new StringBuilder(520);
        Native.GetModuleFileNameW(IntPtr.Zero, sb, sb.Capacity);
        return sb.ToString();
    }

    static string GetLauncherDirectory()
    {
        string path = GetSelfPath();
        string dir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir))
            return AppDomain.CurrentDomain.BaseDirectory;
        if (!dir.EndsWith("\\") && !dir.EndsWith("/"))
            dir += "\\";
        return dir;
    }

    static bool FileExists(string path)
    {
        uint attr = Native.GetFileAttributesW(path);
        return attr != 0xFFFFFFFFu && (attr & 0x10) == 0;
    }

    static bool ResolveGamePath(out string gamePath, out string gameDir)
    {
        gamePath = "";
        gameDir = "";
        string root = GetLauncherDirectory();
        string name = Target;
        string[] relatives = new string[]
        {
            name,
            Path.Combine("Binaries", "Win64", name),
            Path.Combine("Binaries", "Win32", name)
        };
        foreach (string rel in relatives)
        {
            string candidate = Path.Combine(root, rel);
            if (!FileExists(candidate)) continue;
            gamePath = candidate;
            gameDir = Path.GetDirectoryName(candidate) + "\\";
            return true;
        }

        string hit = BreadthFind(root, name, 8, 8000);
        if (hit == null)
            hit = SearchFixedDrives(name);
        if (hit == null)
            return false;
        gamePath = hit;
        gameDir = Path.GetDirectoryName(hit) + "\\";
        return true;
    }

    static string BreadthFind(string start, string fileName, int maxDepth, int maxNodes)
    {
        Queue<KeyValuePair<string, int>> q = new Queue<KeyValuePair<string, int>>();
        q.Enqueue(new KeyValuePair<string, int>(start, 0));
        int seen = 0;
        DateTime deadline = DateTime.UtcNow.AddSeconds(20);
        while (q.Count > 0 && seen < maxNodes && DateTime.UtcNow < deadline)
        {
            KeyValuePair<string, int> cur = q.Dequeue();
            string dir = cur.Key;
            int depth = cur.Value;
            seen++;
            string direct = Path.Combine(dir, fileName);
            if (FileExists(direct))
                return direct;
            if (depth >= maxDepth)
                continue;
            string[] kids;
            try { kids = Directory.GetDirectories(dir); }
            catch { continue; }
            foreach (string kid in kids)
            {
                string leaf = Path.GetFileName(kid);
                if (ShouldSkipDir(leaf)) continue;
                q.Enqueue(new KeyValuePair<string, int>(kid, depth + 1));
            }
        }
        return null;
    }

    static string SearchFixedDrives(string fileName)
    {
        DriveInfo[] drives;
        try { drives = DriveInfo.GetDrives(); }
        catch { return null; }
        foreach (DriveInfo d in drives)
        {
            try
            {
                if (!d.IsReady || d.DriveType != DriveType.Fixed)
                    continue;
                string hit = BreadthFind(d.RootDirectory.FullName, fileName, 6, 25000);
                if (hit != null)
                    return hit;
            }
            catch { }
        }
        return null;
    }

    static bool ShouldSkipDir(string name)
    {
        if (string.IsNullOrEmpty(name)) return true;
        if (name.StartsWith(".")) return true;
        foreach (string s in SkipDirNames)
        {
            if (string.Equals(s, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static bool EnablePrivilege(string name)
    {
        IntPtr token;
        if (!Native.OpenProcessToken(Native.GetCurrentProcess(), Native.TOKEN_ADJUST_PRIVILEGES | Native.TOKEN_QUERY, out token))
            return false;
        Native.TOKEN_PRIVILEGES tp = new Native.TOKEN_PRIVILEGES();
        tp.PrivilegeCount = 1;
        tp.Attributes = Native.SE_PRIVILEGE_ENABLED;
        if (!Native.LookupPrivilegeValueW(null, name, out tp.Luid))
        {
            Native.CloseHandle(token);
            return false;
        }
        bool ok = Native.AdjustTokenPrivileges(token, false, ref tp, Marshal.SizeOf(tp), IntPtr.Zero, IntPtr.Zero);
        Native.CloseHandle(token);
        return ok && Native.GetLastError() != 1300;
    }

    static bool ExecuteMemoryCommand(int command)
    {
        int cmd = command;
        int status = Native.NtSetSystemInformation(Native.SystemMemoryListInformation, ref cmd, 4);
        return status >= 0;
    }

    static bool OptimizeMemoryFull()
    {
        EnablePrivilege("SeProfileSingleProcessPrivilege");
        ExecuteMemoryCommand(Native.MemoryEmptyWorkingSets);
        ExecuteMemoryCommand(Native.MemoryFlushModifiedList);
        ExecuteMemoryCommand(Native.MemoryPurgeStandbyList);
        return true;
    }

    static bool IsProtectedProcess(string exeName)
    {
        if (string.IsNullOrEmpty(exeName)) return true;
        foreach (string p in ProtectedNames)
        {
            if (string.IsNullOrEmpty(p)) continue;
            if (string.Equals(p, exeName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static int TrimOtherWorkingSets()
    {
        EnablePrivilege("SeDebugPrivilege");
        IntPtr snap = Native.CreateToolhelp32Snapshot(Native.TH32CS_SNAPPROCESS, 0);
        if (snap == (IntPtr)(-1))
            return 0;
        uint self = Native.GetCurrentProcessId();
        int trimmed = 0;
        Native.PROCESSENTRY32W pe = new Native.PROCESSENTRY32W();
        pe.dwSize = (uint)Marshal.SizeOf(typeof(Native.PROCESSENTRY32W));
        if (Native.Process32FirstW(snap, ref pe))
        {
            do
            {
                if (pe.th32ProcessID == 0 || pe.th32ProcessID == 4 || pe.th32ProcessID == self)
                    continue;
                if (IsProtectedProcess(pe.szExeFile))
                    continue;
                IntPtr hp = Native.OpenProcess(Native.PROCESS_QUERY_INFORMATION | Native.PROCESS_SET_QUOTA, false, pe.th32ProcessID);
                if (hp == IntPtr.Zero)
                    continue;
                if (Native.EmptyWorkingSet(hp))
                    trimmed++;
                Native.CloseHandle(hp);
            } while (Native.Process32NextW(snap, ref pe));
        }
        Native.CloseHandle(snap);
        return trimmed;
    }

    static bool OptimizeMemoryWhilePlaying()
    {
        EnablePrivilege("SeProfileSingleProcessPrivilege");
        TrimOtherWorkingSets();
        ExecuteMemoryCommand(Native.MemoryFlushModifiedList);
        ExecuteMemoryCommand(Native.MemoryPurgeStandbyList);
        return true;
    }

    static bool IsRunAsAdmin()
    {
        Native.SID_IDENTIFIER_AUTHORITY nt = new Native.SID_IDENTIFIER_AUTHORITY();
        nt.Value = new byte[] { 0, 0, 0, 0, 0, 5 };
        IntPtr adminGroup;
        if (!Native.AllocateAndInitializeSid(ref nt, 2, 0x20, 0x220, 0, 0, 0, 0, 0, 0, out adminGroup))
            return false;
        bool isAdmin;
        Native.CheckTokenMembership(IntPtr.Zero, adminGroup, out isAdmin);
        Native.FreeSid(adminGroup);
        return isAdmin;
    }

    static uint FindProcessIdByName(string processName)
    {
        if (string.IsNullOrEmpty(processName))
            return 0;
        IntPtr snap = Native.CreateToolhelp32Snapshot(Native.TH32CS_SNAPPROCESS, 0);
        if (snap == (IntPtr)(-1))
            return 0;
        uint pid = 0;
        Native.PROCESSENTRY32W pe = new Native.PROCESSENTRY32W();
        pe.dwSize = (uint)Marshal.SizeOf(typeof(Native.PROCESSENTRY32W));
        if (Native.Process32FirstW(snap, ref pe))
        {
            do
            {
                if (string.Equals(pe.szExeFile, processName, StringComparison.OrdinalIgnoreCase))
                {
                    pid = pe.th32ProcessID;
                    break;
                }
            } while (Native.Process32NextW(snap, ref pe));
        }
        Native.CloseHandle(snap);
        return pid;
    }

    static IntPtr OpenGameProcess(uint pid)
    {
        return Native.OpenProcess(
            Native.PROCESS_QUERY_INFORMATION | Native.PROCESS_SET_INFORMATION | Native.SYNCHRONIZE,
            false, pid);
    }

    static IntPtr LaunchGame(string gamePath, string gameDir)
    {
        Native.STARTUPINFOW si = new Native.STARTUPINFOW();
        si.cb = Marshal.SizeOf(typeof(Native.STARTUPINFOW));
        StringBuilder cmd = new StringBuilder("\"" + gamePath + "\"");
        Native.PROCESS_INFORMATION pi;
        bool ok = Native.CreateProcessW(gamePath, cmd, IntPtr.Zero, IntPtr.Zero, false, 0,
            IntPtr.Zero, gameDir, ref si, out pi);
        if (!ok)
            return IntPtr.Zero;
        if (pi.hThread != IntPtr.Zero)
            Native.CloseHandle(pi.hThread);
        return pi.hProcess;
    }

    static void LaunchUri(string uri)
    {
        Native.SHELLEXECUTEINFOW sei = new Native.SHELLEXECUTEINFOW();
        sei.cbSize = Marshal.SizeOf(typeof(Native.SHELLEXECUTEINFOW));
        sei.fMask = Native.SEE_MASK_NOCLOSEPROCESS;
        sei.lpFile = uri;
        sei.nShow = 1;
        Native.ShellExecuteExW(ref sei);
        if (sei.hProcess != IntPtr.Zero)
            Native.CloseHandle(sei.hProcess);
    }

    static IntPtr WaitForGameProcess(uint timeoutMs)
    {
        if (string.IsNullOrEmpty(GameProcessName))
            return IntPtr.Zero;
        int waited = 0;
        while (waited < timeoutMs)
        {
            uint pid = FindProcessIdByName(GameProcessName);
            if (pid != 0)
            {
                IntPtr h = OpenGameProcess(pid);
                if (h != IntPtr.Zero)
                    return h;
            }
            Thread.Sleep((int)GameAppearPollMs);
            waited += (int)GameAppearPollMs;
        }
        return IntPtr.Zero;
    }

    static void RaisePriority(IntPtr hProcess)
    {
        if (hProcess != IntPtr.Zero)
            Native.SetPriorityClass(hProcess, Native.HIGH_PRIORITY_CLASS);
    }

    static void WatchUntilExit(IntPtr hGame, bool allowResident)
    {
        if (hGame == IntPtr.Zero)
        {
            if (!allowResident)
                return;
            while (true)
            {
                Thread.Sleep((int)OptimizeIntervalMs);
                OptimizeMemoryWhilePlaying();
                if (!string.IsNullOrEmpty(GameProcessName))
                {
                    uint pid = FindProcessIdByName(GameProcessName);
                    if (pid != 0)
                    {
                        IntPtr h = OpenGameProcess(pid);
                        if (h != IntPtr.Zero)
                        {
                            RaisePriority(h);
                            WatchUntilExit(h, false);
                            return;
                        }
                    }
                }
            }
        }

        RaisePriority(hGame);
        while (true)
        {
            uint wait = Native.WaitForSingleObject(hGame, OptimizeIntervalMs);
            if (wait == Native.WAIT_TIMEOUT)
            {
                OptimizeMemoryWhilePlaying();
                RaisePriority(hGame);
                continue;
            }
            Native.CloseHandle(hGame);
            hGame = WaitForGameProcess(RelaunchGraceMs);
            if (hGame == IntPtr.Zero)
                return;
            RaisePriority(hGame);
        }
    }
}
