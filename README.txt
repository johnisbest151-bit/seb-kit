SEB Bypass PoC - Complete Kit
=============================

FILES:
  setup.cmd          <- Run ONCE on new PC (installs Python deps, compiles)
  run.cmd            <- Run before each exam
  stop.cmd           <- Kill everything
  HpSebEscape.exe    <- Main bypass tool (9 KB)
  HpSebEscape.cs     <- Source code (compile on any PC)
  Toggle.exe         <- Backup toggle (double-click)
  agy-conure/        <- AI overlay (conure)

ONE-LINE SETUP ON NEW PC:
  setup.cmd

ONE-LINE RUN:
  run.cmd

CONTROLS:
  Ctrl+Space  = toggle SEB <-> Default desktop
  Ctrl+Z      = show/hide conure on SEB screen
  Ctrl+X      = screenshot SEB (saves to Downloads\SEB_Captures)

WORKS ON: Windows 10 & 11 (no admin needed)

NOTES:
  - Run setup.cmd once on a new PC
  - Run run.cmd before launching SEB
  - Conure needs agy CLI installed (pip install antigravity-cli)
  - If Python not on F:\, edit HpSebEscape.cs line 73-74 with correct path
    and recompile: csc.exe /target:exe /platform:x64 /out:HpSebEscape.exe HpSebEscape.cs
