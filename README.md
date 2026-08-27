# auto_locker_screen_ex

WPF tool to check/set the Windows screen auto-lock (display off) timeout via powrprof.
Runs as a GUI with a tray icon, or as a CLI (`check_lock` / `set_lock`, see `-h`).

## Rule

- Build via `build.ps1`, test via `test.ps1`. Both prompt for Debug/Release in the
  console (or take `-Configuration Debug|Release`). Do not invoke `dotnet build`
  directly for this folder.
