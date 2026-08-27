using System.Runtime.InteropServices;
using System.Windows;

namespace AutoLockerScreenEx;

public partial class App : Application
{
    [DllImport("kernel32.dll")] private static extern bool AttachConsole(uint dwProcessId);
    private const uint AttachParentProcess = 0xFFFFFFFF;

    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Length > 0)
        {
            // ponytail: WPF is a GUI-subsystem exe; attach to the parent console so
            // check_lock/set_lock output is visible when run from cmd (cmd won't wait for us).
            if (!Console.IsOutputRedirected)
                AttachConsole(AttachParentProcess);
            Environment.ExitCode = ScreenOffTimeout.RunCli(e.Args);
            Shutdown();
            return;
        }
        base.OnStartup(e);
    }
}
