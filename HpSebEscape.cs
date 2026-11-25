using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Text;

class Program
{
    [DllImport("user32.dll", SetLastError = true)] static extern IntPtr OpenDesktop(string n, uint f, bool i, uint a);
    [DllImport("user32.dll", SetLastError = true)] static extern bool SwitchDesktop(IntPtr h);
    [DllImport("user32.dll")] static extern bool CloseDesktop(IntPtr h);
    [DllImport("user32.dll", SetLastError = true)] static extern bool SetThreadDesktop(IntPtr h);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);
    [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("kernel32.dll")] static extern bool FreeConsole();
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr h, IntPtr d);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr h);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr h, int w, int hh);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr h, IntPtr o);
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr hd, int x1, int y1, int w, int h, IntPtr hs, int x2, int y2, int r);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr h);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr h);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int i);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lp);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr hA, int x, int y, int w, int hh, uint f);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CreateProcess(string lpApp, string lpCmd, IntPtr a, IntPtr b, bool c, uint d, IntPtr e, string f, ref STARTUPINFO g, out PROCESS_INFORMATION h);

    delegate bool EnumWindowsProc(IntPtr h, IntPtr lp);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct STARTUPINFO { public int cb; public string lpReserved; public string lpDesktop; public string lpTitle; public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags; public short wShowWindow, cbReserved2; public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError; }
    struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public int dwProcessId, dwThreadId; }

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    static volatile bool shouldToggle = false;
    static volatile bool shouldCapture = false;
    static volatile bool shouldConureToggle = false;
    static volatile bool onSeb = true;
    static bool conureVisible = true;
    static int conurePid = 0;
    static IntPtr conureHwnd = IntPtr.Zero;
    static int captureCount = 0;
    static string captureDir;

    static void SwitchTo(string d) { IntPtr h = OpenDesktop(d, 0, false, 0x01FF); if (h != IntPtr.Zero) { SwitchDesktop(h); CloseDesktop(h); } }
    static void WaitRelease(int k1, int k2) { while ((GetAsyncKeyState(k1) & 0x8000) != 0 || (GetAsyncKeyState(k2) & 0x8000) != 0) Thread.Sleep(10); Thread.Sleep(150); }

    static IntPtr FindConureWindow()
    {
        if (conurePid == 0) return IntPtr.Zero;
        IntPtr found = IntPtr.Zero;
        uint target = (uint)conurePid;
        EnumWindows(delegate(IntPtr hWnd, IntPtr lp) {
            uint pid; GetWindowThreadProcessId(hWnd, out pid);
            if (pid == target) { StringBuilder sb = new StringBuilder(256); GetWindowText(hWnd, sb, 256); if (sb.Length > 0) { found = hWnd; return false; } }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    static void LaunchConure()
    {
        string py = null;
        string[] pyPaths = { @"F:\Python\pythonw.exe", @"C:\Python312\pythonw.exe", @"C:\Python311\pythonw.exe", @"C:\Python313\pythonw.exe" };
        foreach (string pp in pyPaths) { if (File.Exists(pp)) { py = pp; break; } }
        if (py == null) { foreach (string d in Environment.GetEnvironmentVariable("PATH").Split(';')) { string t = Path.Combine(d, "pythonw.exe"); if (File.Exists(t)) { py = t; break; } } }
        if (py == null) return;
        string script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "agy-conure", "agy_conure.py");
        if (!File.Exists(py) || !File.Exists(script)) return;
        // Wait for SebDesktop to exist
        IntPtr hWait = IntPtr.Zero;
        for (int i = 0; i < 30; i++) { hWait = OpenDesktop("SebDesktop", 0, false, 0x01FF); if (hWait != IntPtr.Zero) { CloseDesktop(hWait); break; } Thread.Sleep(1000); }
        if (hWait == IntPtr.Zero) return;
        Thread.Sleep(2000);
        STARTUPINFO si = new STARTUPINFO(); si.cb = Marshal.SizeOf(si); si.lpDesktop = "SebDesktop";
        PROCESS_INFORMATION pi;
        CreateProcess(py, "pythonw.exe \"" + script + "\"", IntPtr.Zero, IntPtr.Zero, false, 0x08000000, IntPtr.Zero, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "agy-conure"), ref si, out pi);
        conurePid = pi.dwProcessId;
        conureVisible = true;
    }

    static void ToggleConureVisibility()
    {
        // Find window if we don't have it yet
        if (conureHwnd == IntPtr.Zero) conureHwnd = FindConureWindow();
        if (conureHwnd == IntPtr.Zero) return;

        // Verify window still exists
        StringBuilder sb = new StringBuilder(256);
        GetWindowText(conureHwnd, sb, 256);
        if (sb.Length == 0) { conureHwnd = FindConureWindow(); if (conureHwnd == IntPtr.Zero) return; }

        if (conureVisible)
        {
            ShowWindow(conureHwnd, 0); // SW_HIDE
            conureVisible = false;
        }
        else
        {
            ShowWindow(conureHwnd, 5); // SW_SHOW
            SetWindowPos(conureHwnd, HWND_TOPMOST, 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0040);
            conureVisible = true;
        }
    }

    static void DoCaptureOnSebDesktop()
    {
        try {
            int w = GetSystemMetrics(0), h = GetSystemMetrics(1);
            IntPtr hs = GetDC(IntPtr.Zero), hm = CreateCompatibleDC(hs), hb = CreateCompatibleBitmap(hs, w, h), ho = SelectObject(hm, hb);
            BitBlt(hm, 0, 0, w, h, hs, 0, 0, 0x00CC0020); SelectObject(hm, ho);
            captureCount++;
            using (Bitmap bmp = Image.FromHbitmap(hb)) { bmp.Save(Path.Combine(captureDir, "seb_" + captureCount.ToString("D3") + ".png"), ImageFormat.Png); }
            DeleteDC(hm); DeleteObject(hb); ReleaseDC(IntPtr.Zero, hs);
        } catch {}
    }

    static void EventThread() { EventWaitHandle e = new EventWaitHandle(false, EventResetMode.AutoReset, "HpSebToggleEvent"); while (true) { if (e.WaitOne(100)) shouldToggle = true; } }

    static void KeyPollDefault()
    {
        IntPtr h = OpenDesktop("Default", 0, false, 0x01FF); if (h != IntPtr.Zero) SetThreadDesktop(h);
        while (true) {
            bool c = (GetAsyncKeyState(0x11) & 0x8000) != 0;
            if (c && (GetAsyncKeyState(0x20) & 0x8000) != 0) { WaitRelease(0x11, 0x20); shouldToggle = true; Thread.Sleep(500); }
            if (c && (GetAsyncKeyState(0x5A) & 0x8000) != 0) { WaitRelease(0x11, 0x5A); shouldConureToggle = true; Thread.Sleep(500); }
            Thread.Sleep(30);
        }
    }

    static void KeyPollSeb()
    {
        IntPtr h = OpenDesktop("SebDesktop", 0, false, 0x01FF);
        while (h == IntPtr.Zero) { Thread.Sleep(500); h = OpenDesktop("SebDesktop", 0, false, 0x01FF); }
        SetThreadDesktop(h);
        while (true) {
            bool c = (GetAsyncKeyState(0x11) & 0x8000) != 0;
            if (c && (GetAsyncKeyState(0x20) & 0x8000) != 0) { WaitRelease(0x11, 0x20); shouldToggle = true; Thread.Sleep(500); }
            if (c && (GetAsyncKeyState(0x58) & 0x8000) != 0 && onSeb) { WaitRelease(0x11, 0x58); DoCaptureOnSebDesktop(); Thread.Sleep(500); }
            if (c && (GetAsyncKeyState(0x5A) & 0x8000) != 0) { WaitRelease(0x11, 0x5A); ToggleConureVisibility(); Thread.Sleep(500); }
            Thread.Sleep(30);
        }
    }

    static void Main(string[] args)
    {
        SetProcessDPIAware();
        int autoEscape = 0;
        if (args.Length > 0) int.TryParse(args[0], out autoEscape);
        captureDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "SEB_Captures");
        Directory.CreateDirectory(captureDir);
        IntPtr hw = GetConsoleWindow(); if (hw != IntPtr.Zero) ShowWindow(hw, 0); FreeConsole();

        while (Process.GetProcessesByName("SecureExamBrowser").Length == 0) Thread.Sleep(500);

        // Launch conure once on SebDesktop
        LaunchConure();

        if (autoEscape > 0) { Thread.Sleep(autoEscape * 1000); SwitchTo("Default"); onSeb = false; }

        new Thread(EventThread) { IsBackground = true }.Start();
        new Thread(KeyPollDefault) { IsBackground = true }.Start();
        new Thread(KeyPollSeb) { IsBackground = true }.Start();

        while (true)
        {
            if (Process.GetProcessesByName("SecureExamBrowser").Length == 0)
            { if (onSeb) SwitchTo("Default"); if (conurePid != 0) try { Process.GetProcessById(conurePid).Kill(); } catch {} return; }

            // conure toggle handled in SebDesktop thread
            // capture handled in SebDesktop thread
            if (shouldToggle)
            {
                shouldToggle = false;
                if (onSeb) { SwitchTo("Default"); onSeb = false; }
                else if (Process.GetProcessesByName("SecureExamBrowser").Length > 0) { SwitchTo("SebDesktop"); onSeb = true; }
                Thread.Sleep(300);
            }
            Thread.Sleep(30);
        }
    }
}



