# auto_locker_screen_ex

WPF tool to check/set the Windows screen auto-lock (display off) timeout via powrprof.
Runs as a GUI with a tray icon, or as a CLI (`check_lock` / `set_lock`, see `-h`).

![main window](README.assets/main-window.png)

## Features

- **Check / Set**: read or set the AC (plugged in) and DC (on battery) screen-off
  timeouts in seconds (0 = never).
- **Monitor every N sec**: warns via a persistent topmost window while either
  timeout is <= 1s (effectively off); auto-closes once fixed.
- **Auto reset auto locker for N sec**: (requires Monitor) when a monitored timeout
  is 0, automatically sets it back to N seconds and pops a notification that
  auto-dismisses after 5 seconds.

Settings persist to `~/.config/auto_locker_screen_ex/config.json`.

## CI/CD

GitHub Actions (`.github/workflows/build.yml`) builds every push to `main` as a
compile check. To publish a release:

- **CLI**: `git tag v1.0.0 && git push origin v1.0.0` (bare `1.0.0` also works) —
  the workflow builds and creates the GitHub Release with `AutoLockerScreenEx.zip`.
- **Web**: Releases → Draft a new release → Publish — the workflow then attaches
  the built zip to it automatically.

See `CI-CD.md` (中文) for the full pipeline documentation.

## Rule

- Build via `build.ps1`, test via `test.ps1`. Both prompt for Debug/Release in the
  console (or take `-Configuration Debug|Release`). Do not invoke `dotnet build`
  directly for this folder.
- Deploy via `build.ps1 -Deploy [-DeployDir <path>]`, or just answer the console
  prompts: it asks Debug/Release, then deploy y/n (Enter = no) and the target dir
  (Enter = default `D:\auto_locker_screen_ex`). Deploy copies the built app files
  to the target dir, offering to kill a running instance there first and to start
  it after copying.
