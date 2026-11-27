SEB Bypass PoC v2 - Complete Kit
================================

FILES:
  setup.cmd            <- Run ONCE on new PC (installs deps, compiles, deploys)
  run.cmd              <- Run before each exam
  stop.cmd             <- Kill everything
  HpSebEscape.exe      <- Desktop switcher + screenshot tool
  HpSebEscape.cs       <- Source (auto-compiled by setup)
  AltA2dpConfig.exe    <- AI chat app (disguised as A2DP driver)
  AltA2dpConfig.cs     <- Source (auto-compiled by setup)
  Toggle.exe           <- Backup toggle (double-click)
  agy-conure/          <- Original conure (not used in v2)

ONE-LINE SETUP ON NEW PC:
  1. Run: setup.cmd
  2. Run 'agy' in cmd once to login to Antigravity
  3. Done!

ONE-LINE RUN:
  run.cmd

CONTROLS:
  Ctrl+Space  = toggle SEB <-> Default desktop
  Ctrl+X      = screenshot SEB screen (saves to Downloads\SEB_Captures)
  Ctrl+Z      = show/hide AI chat app

AI CHAT APP:
  - Opens from: C:\Program Files\Luculent Systems\AltA2DP\AltA2dpConfig.exe
  - Or double-click 'Alternative A2DP Driver' from Start Menu
  - SEB doesn't kill it (trusted A2DP driver path)
  - Features: interview/meeting/sales/support modes, concise/detailed/STAR styles,
    model selector, context (resume/job desc/notes), screenshot analysis,
    conversation history, private mode (hidden from screen capture)
  - Say 'read recent screenshot' to analyze SEB captures
  - Say 'read last 2 screenshots' for multiple

HOW IT WORKS:
  1. HpSebEscape.exe: SwitchDesktop bypass (Ctrl+Space toggles)
  2. AltA2dpConfig.exe: Native C# chat app at trusted A2DP path
  3. edge_feedback.exe: Disguised agy CLI (runs briefly per query)
  4. SEB can't kill: A2DP app = trusted path, agy = too fast to catch

WORKS ON: Windows 10 & 11 (no admin needed for run, setup needs write to Program Files)

REQUIRES:
  - Alternative A2DP Driver installed (for the trusted path)
  - Python 3.11+ on PATH or F:\Python
  - agy CLI installed and logged in
