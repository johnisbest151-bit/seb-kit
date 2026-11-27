using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

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
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")] static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CreateProcess(string lpApp, string lpCmd, IntPtr a, IntPtr b, bool c, uint d, IntPtr e, string f, ref STARTUPINFO g, out PROCESS_INFORMATION h);

    delegate bool EnumWindowsProc(IntPtr h, IntPtr lp);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct STARTUPINFO { public int cb; public string lpReserved; public string lpDesktop; public string lpTitle; public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags; public short wShowWindow, cbReserved2; public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError; }
    struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public int dwProcessId, dwThreadId; }
    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }
    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    delegate bool EnumDesktopProc2(string name, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool EnumDesktops(IntPtr hWinStation, EnumDesktopProc2 proc, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr GetProcessWindowStation();
    static string sebDesktopName = null;
    static string FindSebDesktop() { var n = new List<string>(); EnumDesktops(GetProcessWindowStation(), (name, lp) => { n.Add(name); return true; }, IntPtr.Zero); foreach (string s in n) if (s != "Default" && s != "Winlogon" && s != "LongRunBG" && s != "") return s; return null; }

    static volatile bool shouldToggle = false, shouldCapture = false, onSeb = true;
    static int conurePid = 0;
    static IntPtr conureHwnd = IntPtr.Zero;
    static string captureDir;

    // ===== AGY DISGUISE v4: renamed copy + integrity guardian =====
    static string agyOrigPath;
    static string agyFakePath; // edge_feedback.exe in our dir
    static byte[] agyOrigHash; // MD5 of original for integrity check
    static FileStream agyLock;
    const string AGY_FAKE_NAME = "edge_feedback.exe";

    static byte[] FileHash(string path) { try { using (var s = File.OpenRead(path)) using (var md5 = MD5.Create()) return md5.ComputeHash(s); } catch { return null; } }
    static bool ByteEq(byte[] a, byte[] b) { if (a == null || b == null || a.Length != b.Length) return false; for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false; return true; }

    static void EnsureAgyDisguise()
    {
        if (agyOrigPath == null) {
            agyOrigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "agy", "bin", "agy.exe");
            if (!File.Exists(agyOrigPath)) foreach (string d in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';')) { string t = Path.Combine(d.Trim(), "agy.exe"); if (File.Exists(t)) { agyOrigPath = t; break; } }
        }
        if (!File.Exists(agyOrigPath)) return;
        if (agyFakePath == null) agyFakePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AGY_FAKE_NAME);
        if (agyOrigHash == null) agyOrigHash = FileHash(agyOrigPath);

        bool needCopy = !File.Exists(agyFakePath) || !ByteEq(FileHash(agyFakePath), agyOrigHash);
        if (needCopy) {
            // Release old lock
            if (agyLock != null) try { agyLock.Close(); agyLock = null; } catch {}
            try { File.Copy(agyOrigPath, agyFakePath, true); } catch {}
        }
        // Re-lock if not locked
        if (agyLock == null && File.Exists(agyFakePath))
            try { agyLock = new FileStream(agyFakePath, FileMode.Open, FileAccess.Read, FileShare.Read); } catch {}
    }

    // Background thread: check integrity every 2 seconds, re-copy if corrupted
    static void AgyGuardianThread()
    {
        while (true) { Thread.Sleep(2000); try { EnsureAgyDisguise(); } catch {} }
    }

    // ===== HELPERS =====
    static void SwitchTo(string d) { IntPtr h = OpenDesktop(d, 0, false, 0x01FF); if (h != IntPtr.Zero) { SwitchDesktop(h); CloseDesktop(h); } }
    static void WaitRelease(int k1, int k2) { while ((GetAsyncKeyState(k1) & 0x8000) != 0 || (GetAsyncKeyState(k2) & 0x8000) != 0) Thread.Sleep(10); Thread.Sleep(150); }

    // ===== CONURE =====
    static string FindPythonW() { return null; } // Not used — conure replaced by AltA2dpConfig.exe
    static string FindConureScript() { string[] p = { @"C:\Users\Rahul\agy-conure\agy_conure.py", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "agy-conure", "agy_conure.py") }; foreach (string s in p) if (File.Exists(s)) return s; return null; }

    static void LaunchConure()
    {
        // Try A2DP path first, then local folder
        string launcher = @"C:\Program Files\Luculent Systems\AltA2DP\AltA2dpConfig.exe";
        if (!File.Exists(launcher)) launcher = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AltA2dpConfig.exe");
        if (!File.Exists(launcher)) return;

        STARTUPINFO si = new STARTUPINFO();
        si.cb = Marshal.SizeOf(si);
        si.lpDesktop = "Default"; // Launch on Default desktop — always safe
        PROCESS_INFORMATION pi;
        if (CreateProcess(launcher, null, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero,
                          Path.GetDirectoryName(launcher), ref si, out pi))
        { conurePid = pi.dwProcessId; conureHwnd = IntPtr.Zero; }
    }

    static IntPtr FindConureWindow() { if (conurePid == 0) return IntPtr.Zero; try { Process.GetProcessById(conurePid); } catch { conurePid = 0; return IntPtr.Zero; } IntPtr f = IntPtr.Zero; uint t = (uint)conurePid; EnumWindows(delegate(IntPtr h, IntPtr lp) { uint pid; GetWindowThreadProcessId(h, out pid); if (pid == t) { StringBuilder sb = new StringBuilder(256); GetWindowText(h, sb, 256); if (sb.Length > 0) { f = h; return false; } } return true; }, IntPtr.Zero); return f; }
    static void ToggleConureVisibility() { if (conureHwnd == IntPtr.Zero) conureHwnd = FindConureWindow(); if (conureHwnd == IntPtr.Zero) return; StringBuilder sb = new StringBuilder(256); GetWindowText(conureHwnd, sb, 256); if (sb.Length == 0) { conureHwnd = FindConureWindow(); if (conureHwnd == IntPtr.Zero) return; } RECT r; GetWindowRect(conureHwnd, out r); bool v = IsWindowVisible(conureHwnd) && (r.Right-r.Left) > 0; if (v) ShowWindow(conureHwnd, 0); else { ShowWindow(conureHwnd, 5); SetWindowPos(conureHwnd, HWND_TOPMOST, 0, 0, 0, 0, 0x0002|0x0001|0x0040); } }

    static int noWindowCount = 0;
    static long lastLaunchTick = 0;
    static void ConureWatchdogThread()
    {
        // Attach to SEB's desktop where AltA2dpConfig now runs.
        // AltA2dpConfig is launched on sebDesktopName (not Default) so that
        // SetParent can embed it in SEB's window on the same desktop.
        IntPtr hDefDesk = OpenDesktop("Default", 0, false, 0x01FF);
        if (hDefDesk != IntPtr.Zero) SetThreadDesktop(hDefDesk);
        Thread.Sleep(12000);
        while (true)
        {
            Thread.Sleep(500); // Check every 500ms for fast restart
            bool alive = false;
            if (conurePid != 0)
            {
                try { Process.GetProcessById(conurePid); alive = true; } catch { alive = false; }
            }
            if (!alive)
            {
                long now = DateTime.Now.Ticks;
                if (now - lastLaunchTick < 15000000L) continue; // 1.5 sec cooldown
                conurePid = 0; conureHwnd = IntPtr.Zero;
                lastLaunchTick = now;
                LaunchConure();
            }
        }
    }

    // ===== WDA REMOVAL =====
    static void WdaRemovalThread() { while (sebDesktopName == null) Thread.Sleep(500); IntPtr hD = IntPtr.Zero; for (int i = 0; i < 60; i++) { hD = OpenDesktop(sebDesktopName, 0, false, 0x01FF); if (hD != IntPtr.Zero) break; Thread.Sleep(1000); } if (hD == IntPtr.Zero) return; SetThreadDesktop(hD); while (true) { Thread.Sleep(2000); try { foreach (string pn in new[]{"SafeExamBrowser.Client","SafeExamBrowser","SecureExamBrowser","CefSharp.BrowserSubprocess","HPReadyBootstrap"}) foreach (Process p in Process.GetProcessesByName(pn)) try { uint tp=(uint)p.Id; EnumWindows(delegate(IntPtr h, IntPtr lp){uint pid;GetWindowThreadProcessId(h,out pid);if(pid==tp)SetWindowDisplayAffinity(h,0);return true;},IntPtr.Zero);} catch{} } catch{} } }

    // ===== SCREENSHOT =====
    static volatile bool captureReady=false,captureRequest=false,captureDone=false; static string lastCapturedFile=null;
    static string MakeCF(){return Path.Combine(captureDir,"seb_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".png");}
    static void CaptureThread(){while(sebDesktopName==null)Thread.Sleep(500);IntPtr hD=IntPtr.Zero;for(int i=0;i<60;i++){hD=OpenDesktop(sebDesktopName,0,false,0x01FF);if(hD!=IntPtr.Zero)break;Thread.Sleep(1000);}if(hD==IntPtr.Zero)return;SetThreadDesktop(hD);captureReady=true;while(true){if(captureRequest){captureRequest=false;lastCapturedFile=null;try{DoBB();}catch{}if(lastCapturedFile!=null&&File.Exists(lastCapturedFile)&&IsBlack(lastCapturedFile)){try{File.Delete(lastCapturedFile);}catch{}lastCapturedFile=null;try{DoPW(0x2);}catch{}}if(lastCapturedFile!=null&&File.Exists(lastCapturedFile)&&IsBlack(lastCapturedFile)){try{File.Delete(lastCapturedFile);}catch{}lastCapturedFile=null;try{DoPW(0);}catch{}}captureDone=true;}Thread.Sleep(30);}}
    static void DoBB(){int w=GetSystemMetrics(0),h=GetSystemMetrics(1);IntPtr hs=GetDC(IntPtr.Zero),hm=CreateCompatibleDC(hs),hb=CreateCompatibleBitmap(hs,w,h),ho=SelectObject(hm,hb);BitBlt(hm,0,0,w,h,hs,0,0,0x00CC0020);SelectObject(hm,ho);string p=MakeCF();using(Bitmap bmp=Image.FromHbitmap(hb))bmp.Save(p,ImageFormat.Png);DeleteDC(hm);DeleteObject(hb);ReleaseDC(IntPtr.Zero,hs);lastCapturedFile=p;}
    static IntPtr FLW(string[] pn){IntPtr b=IntPtr.Zero;int ba=0;foreach(string s in pn)foreach(Process p in Process.GetProcessesByName(s))try{uint tp=(uint)p.Id;EnumWindows(delegate(IntPtr h,IntPtr lp){uint pid;GetWindowThreadProcessId(h,out pid);if(pid==tp&&IsWindowVisible(h)){RECT r;GetWindowRect(h,out r);int a=(r.Right-r.Left)*(r.Bottom-r.Top);if(a>ba){ba=a;b=h;}}return true;},IntPtr.Zero);}catch{}return b;}
    static void DoPW(uint f){IntPtr w=FLW(new[]{"SafeExamBrowser.Client","SafeExamBrowser","SecureExamBrowser"});if(w==IntPtr.Zero)return;RECT wr;GetWindowRect(w,out wr);int ww=wr.Right-wr.Left,wh=wr.Bottom-wr.Top;if(ww<=0||wh<=0)return;using(Bitmap bmp=new Bitmap(ww,wh,PixelFormat.Format32bppArgb)){using(Graphics g=Graphics.FromImage(bmp)){IntPtr hdc=g.GetHdc();PrintWindow(w,hdc,f);g.ReleaseHdc(hdc);}string p=MakeCF();bmp.Save(p,ImageFormat.Png);lastCapturedFile=p;}}
    static bool IsBlack(string p){try{using(Bitmap b=new Bitmap(p)){Random r=new Random();int n=0;for(int i=0;i<50;i++){Color c=b.GetPixel(r.Next(b.Width),r.Next(b.Height));if(c.R>10||c.G>10||c.B>10)n++;}return n<3;}}catch{return false;}}

    // ===== KEY POLLING =====
    static void EventThread(){EventWaitHandle e=new EventWaitHandle(false,EventResetMode.AutoReset,"HpSebToggleEvent");while(true){if(e.WaitOne(100))shouldToggle=true;}}
    static void KeyPollDefault(){IntPtr h=OpenDesktop("Default",0,false,0x01FF);if(h!=IntPtr.Zero)SetThreadDesktop(h);while(true){bool c=(GetAsyncKeyState(0x11)&0x8000)!=0;if(c&&(GetAsyncKeyState(0x20)&0x8000)!=0){WaitRelease(0x11,0x20);shouldToggle=true;Thread.Sleep(500);}if(c&&(GetAsyncKeyState(0x5A)&0x8000)!=0){WaitRelease(0x11,0x5A);ToggleConureVisibility();Thread.Sleep(500);}Thread.Sleep(30);}}
    static void KeyPollSeb(){while(sebDesktopName==null)Thread.Sleep(500);IntPtr h=OpenDesktop(sebDesktopName,0,false,0x01FF);while(h==IntPtr.Zero){Thread.Sleep(500);h=OpenDesktop(sebDesktopName,0,false,0x01FF);}SetThreadDesktop(h);while(true){bool c=(GetAsyncKeyState(0x11)&0x8000)!=0;if(c&&(GetAsyncKeyState(0x20)&0x8000)!=0){WaitRelease(0x11,0x20);shouldToggle=true;Thread.Sleep(500);}if(c&&(GetAsyncKeyState(0x58)&0x8000)!=0&&onSeb){WaitRelease(0x11,0x58);shouldCapture=true;Thread.Sleep(500);}Thread.Sleep(30);}}

    // ===== MAIN =====
    static void Main(string[] args)
    {
        SetProcessDPIAware();
        int autoEscape=0; if(args.Length>0)int.TryParse(args[0],out autoEscape);
        captureDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads","SEB_Captures"); Directory.CreateDirectory(captureDir);
        IntPtr hw=GetConsoleWindow();if(hw!=IntPtr.Zero)ShowWindow(hw,0);FreeConsole();

        // 1. Create disguised agy copy + lock it + start guardian
        EnsureAgyDisguise();
        new Thread(AgyGuardianThread){IsBackground=true}.Start();
        // DON'T set env vars globally - only pass them in the CreateProcess env block
        // Setting PYTHONHOME globally breaks Tcl for ALL python processes

        // 2. Wait for SEB (timeout after 120s — start hotkeys anyway)
        for(int w=0;w<240;w++){
            if(Process.GetProcessesByName("SecureExamBrowser").Length>0||Process.GetProcessesByName("SafeExamBrowser.Client").Length>0||Process.GetProcessesByName("SafeExamBrowser").Length>0||Process.GetProcessesByName("SafeExamBrowser.Service").Length>0||Process.GetProcessesByName("SebWindowsServiceWCF").Length>0)break;
            Thread.Sleep(500);
        }

        // 3. Find SEB desktop (also try immediately — SEB service may already have created it)
        for(int i=0;i<60;i++){sebDesktopName=FindSebDesktop();if(sebDesktopName!=null)break;Thread.Sleep(1000);}if(sebDesktopName==null)sebDesktopName="SebDesktop";

        // 4. Threads
        new Thread(WdaRemovalThread){IsBackground=true}.Start();
        new Thread(CaptureThread){IsBackground=true}.Start();

        // 5. Launch conure after SEB's initial scan
        Thread.Sleep(10000); LaunchConure();

        if(autoEscape>0){Thread.Sleep(autoEscape*1000);SwitchTo("Default");onSeb=false;}
        new Thread(EventThread){IsBackground=true}.Start();
        new Thread(KeyPollDefault){IsBackground=true}.Start();
        new Thread(KeyPollSeb){IsBackground=true}.Start();
        new Thread(ConureWatchdogThread){IsBackground=true}.Start();

        // 6. Main loop
        while(true){
            if(Process.GetProcessesByName("SecureExamBrowser").Length==0&&Process.GetProcessesByName("SafeExamBrowser.Client").Length==0&&Process.GetProcessesByName("SafeExamBrowser").Length==0){if(onSeb)SwitchTo("Default");if(conurePid!=0)try{Process.GetProcessById(conurePid).Kill();}catch{}if(agyLock!=null)try{agyLock.Close();}catch{}try{File.Delete(agyFakePath);}catch{}return;}
            if(shouldCapture){shouldCapture=false;if(onSeb&&captureReady){captureRequest=true;captureDone=false;for(int i=0;i<100&&!captureDone;i++)Thread.Sleep(30);}Thread.Sleep(300);}
            if(shouldToggle){shouldToggle=false;if(onSeb){SwitchTo("Default");onSeb=false;}else if(Process.GetProcessesByName("SecureExamBrowser").Length>0||Process.GetProcessesByName("SafeExamBrowser.Client").Length>0||Process.GetProcessesByName("SafeExamBrowser").Length>0){SwitchTo(sebDesktopName);onSeb=true;}Thread.Sleep(300);}
            Thread.Sleep(30);
        }
    }
}
