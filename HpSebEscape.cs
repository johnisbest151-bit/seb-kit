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
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CreateProcess(string lpApp, string lpCmd, IntPtr a, IntPtr b, bool c, uint d, IntPtr e, string f, ref STARTUPINFO g, out PROCESS_INFORMATION h);

    delegate bool EnumDesktopProc(string name, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool EnumDesktops(IntPtr hWinStation, EnumDesktopProc proc, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr GetProcessWindowStation();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct STARTUPINFO { public int cb; public string lpReserved; public string lpDesktop; public string lpTitle; public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags; public short wShowWindow, cbReserved2; public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError; }
    struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public int dwProcessId, dwThreadId; }

    static volatile bool shouldToggle = false;
    static volatile bool shouldCapture = false;
    static volatile bool onSeb = false;
    static string sebDesktopName = null;
    static string captureDir;
    static int captureCount = 0;

    static string FindSebDesktop()
    {
        var names = new System.Collections.Generic.List<string>();
        EnumDesktops(GetProcessWindowStation(), (name, lp) => { names.Add(name); return true; }, IntPtr.Zero);
        foreach (string n in names)
            if (n != "Default" && n != "Winlogon" && n != "LongRunBG" && n != "") return n;
        return null;
    }

    static void SwitchTo(string d) { IntPtr h = OpenDesktop(d, 0, false, 0x01FF); if (h != IntPtr.Zero) { SwitchDesktop(h); CloseDesktop(h); } }
    static void WaitRelease(int k1, int k2) { while ((GetAsyncKeyState(k1) & 0x8000) != 0 || (GetAsyncKeyState(k2) & 0x8000) != 0) Thread.Sleep(10); Thread.Sleep(150); }

    static void DoCaptureOnSebDesktop()
    {
        try {
            int w = GetSystemMetrics(0), h = GetSystemMetrics(1);
            IntPtr hs = GetDC(IntPtr.Zero), hm = CreateCompatibleDC(hs), hb = CreateCompatibleBitmap(hs, w, h), ho = SelectObject(hm, hb);
            BitBlt(hm, 0, 0, w, h, hs, 0, 0, 0x00CC0020); SelectObject(hm, ho);
            captureCount++;
            string path = Path.Combine(captureDir, "seb_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".png");
            using (Bitmap bmp = Image.FromHbitmap(hb)) { bmp.Save(path, ImageFormat.Png); }
            DeleteDC(hm); DeleteObject(hb); ReleaseDC(IntPtr.Zero, hs);
        } catch {}
    }

    static void KeyPollDefault()
    {
        IntPtr h = OpenDesktop("Default", 0, false, 0x01FF); if (h != IntPtr.Zero) SetThreadDesktop(h);
        while (true) {
            bool c = (GetAsyncKeyState(0x11) & 0x8000) != 0;
            if (c && (GetAsyncKeyState(0x20) & 0x8000) != 0) { WaitRelease(0x11, 0x20); shouldToggle = true; Thread.Sleep(500); }
            Thread.Sleep(30);
        }
    }

    static void KeyPollSeb()
    {
        while (sebDesktopName == null) Thread.Sleep(500);
        IntPtr h = OpenDesktop(sebDesktopName, 0, false, 0x01FF);
        while (h == IntPtr.Zero) { Thread.Sleep(500); h = OpenDesktop(sebDesktopName, 0, false, 0x01FF); }
        SetThreadDesktop(h);
        while (true) {
            bool c = (GetAsyncKeyState(0x11) & 0x8000) != 0;
            if (c && (GetAsyncKeyState(0x20) & 0x8000) != 0) { WaitRelease(0x11, 0x20); shouldToggle = true; Thread.Sleep(500); }
            if (c && (GetAsyncKeyState(0x58) & 0x8000) != 0 && onSeb) { WaitRelease(0x11, 0x58); shouldCapture = true; Thread.Sleep(500); }
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

        // Start hotkey polling IMMEDIATELY so Ctrl+Space works from second 1
        new Thread(KeyPollDefault) { IsBackground = true }.Start();

        // Wait for SEB process (any variant)
        while (true) {
            if (Process.GetProcessesByName("SecureExamBrowser").Length > 0) break;
            if (Process.GetProcessesByName("SafeExamBrowser.Client").Length > 0) break;
            if (Process.GetProcessesByName("SafeExamBrowser").Length > 0) break;
            // Also check if SEB desktop appeared
            string d = FindSebDesktop(); if (d != null) break;
            Thread.Sleep(500);
        }

        // Find SEB desktop
        for (int i = 0; i < 30; i++) { sebDesktopName = FindSebDesktop(); if (sebDesktopName != null) break; Thread.Sleep(1000); }
        if (sebDesktopName == null) sebDesktopName = "SebDesktop";

        onSeb = true;

        // Launch AltA2dpConfig on Default desktop
        string launcher = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AltA2dpConfig.exe");
        if (!File.Exists(launcher)) launcher = @"C:\Program Files\Luculent Systems\AltA2DP\AltA2dpConfig.exe";
        if (File.Exists(launcher))
        {
            STARTUPINFO si = new STARTUPINFO(); si.cb = Marshal.SizeOf(si); si.lpDesktop = "Default";
            PROCESS_INFORMATION pi;
            CreateProcess(launcher, null, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, Path.GetDirectoryName(launcher), ref si, out pi);
        }

        if (autoEscape > 0) { Thread.Sleep(autoEscape * 1000); SwitchTo("Default"); onSeb = false; }

        new Thread(KeyPollSeb) { IsBackground = true }.Start();

        // Main loop
        while (true)
        {
            if (Process.GetProcessesByName("SecureExamBrowser").Length == 0 &&
                Process.GetProcessesByName("SafeExamBrowser.Client").Length == 0 &&
                Process.GetProcessesByName("SafeExamBrowser").Length == 0 &&
                FindSebDesktop() == null)
            { if (onSeb) SwitchTo("Default"); return; }

            if (shouldCapture) { shouldCapture = false; if (onSeb) DoCaptureOnSebDesktop(); Thread.Sleep(300); }
            if (shouldToggle)
            {
                shouldToggle = false;
                if (onSeb) { SwitchTo("Default"); onSeb = false; }
                else { SwitchTo(sebDesktopName); onSeb = true; }
                Thread.Sleep(300);
            }
            Thread.Sleep(30);
        }
    }
}
