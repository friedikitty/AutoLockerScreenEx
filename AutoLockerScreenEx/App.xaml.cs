using System.Runtime.InteropServices;
using System.Windows;

namespace AutoLockerScreenEx;

public partial class App : Application
{
    [DllImport("kernel32.dll")] private static extern bool FreeConsole();

    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Length > 0)
        {
            // Console-subsystem exe: cmd waits for us and shows our output.
            Environment.ExitCode = ScreenOffTimeout.RunCli(e.Args);
            Shutdown();
            return;
        }
        // GUI mode: drop the console window so double-clicking doesn't leave one behind.
        FreeConsole();
        base.OnStartup(e);
    }
}
