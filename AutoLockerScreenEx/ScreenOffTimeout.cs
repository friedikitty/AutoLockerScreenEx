using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace AutoLockerScreenEx;

/// <summary>
/// Reads/writes the "turn off display" idle timeout of the active power scheme
/// (seconds, 0 = never) via powrprof.dll — AC (plugged in) and DC (battery) values.
/// Refs: PowerGetActiveScheme / PowerReadACValueIndex / PowerWriteACValueIndex /
/// PowerSetActiveScheme (Microsoft Learn, powrprof.h; GUID_VIDEO_POWERDOWN_TIMEOUT).
/// </summary>
public static class ScreenOffTimeout
{
    // GUID_VIDEO_SUBGROUP and GUID_VIDEO_POWERDOWN_TIMEOUT
    private static readonly Guid VideoSubgroup = new("7516b95f-f776-4464-8c53-06167f40cc99");
    private static readonly Guid VideoPowerdownTimeout = new("3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e");

    [DllImport("powrprof.dll")] private static extern uint PowerGetActiveScheme(IntPtr rootPowerKey, out IntPtr activeScheme);
    [DllImport("powrprof.dll")] private static extern uint PowerReadACValueIndex(IntPtr rootPowerKey, in Guid scheme, in Guid subGroup, in Guid setting, out uint value);
    [DllImport("powrprof.dll")] private static extern uint PowerReadDCValueIndex(IntPtr rootPowerKey, in Guid scheme, in Guid subGroup, in Guid setting, out uint value);
    [DllImport("powrprof.dll")] private static extern uint PowerWriteACValueIndex(IntPtr rootPowerKey, in Guid scheme, in Guid subGroup, in Guid setting, uint value);
    [DllImport("powrprof.dll")] private static extern uint PowerWriteDCValueIndex(IntPtr rootPowerKey, in Guid scheme, in Guid subGroup, in Guid setting, uint value);
    [DllImport("powrprof.dll")] private static extern uint PowerSetActiveScheme(IntPtr rootPowerKey, in Guid scheme);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr hMem);

    public static (uint Ac, uint Dc) Get()
    {
        Guid scheme = ActiveScheme();
        Check(PowerReadACValueIndex(IntPtr.Zero, scheme, VideoSubgroup, VideoPowerdownTimeout, out uint ac));
        Check(PowerReadDCValueIndex(IntPtr.Zero, scheme, VideoSubgroup, VideoPowerdownTimeout, out uint dc));
        return (ac, dc);
    }

    public static void SetScreenOffTimeoutAC(uint seconds) => Write(ac: true, seconds);
    public static void SetScreenOffTimeoutDC(uint seconds) => Write(ac: false, seconds);

    private static Guid ActiveScheme()
    {
        Check(PowerGetActiveScheme(IntPtr.Zero, out IntPtr ptr));
        try { return Marshal.PtrToStructure<Guid>(ptr); }
        finally { LocalFree(ptr); } // per docs: caller frees with LocalFree
    }

    private static void Write(bool ac, uint seconds)
    {
        Guid scheme = ActiveScheme();
        Check(ac
            ? PowerWriteACValueIndex(IntPtr.Zero, scheme, VideoSubgroup, VideoPowerdownTimeout, seconds)
            : PowerWriteDCValueIndex(IntPtr.Zero, scheme, VideoSubgroup, VideoPowerdownTimeout, seconds));
        Check(PowerSetActiveScheme(IntPtr.Zero, scheme)); // re-activate so the change applies immediately
    }

    private static void Check(uint err)
    {
        if (err != 0) throw new Win32Exception((int)err);
    }

    private static string ToJson()
    {
        var (ac, dc) = Get();
        return JsonSerializer.Serialize(new { ac, dc });
    }

    /// <summary>CLI entry: check_lock | set_lock &lt;ac_seconds&gt; [dc_seconds]. Returns exit code.</summary>
    public static int RunCli(string[] args)
    {
        try
        {
            switch (args[0])
            {
                case "check_lock":
                    Console.WriteLine(ToJson());
                    return 0;
                case "set_lock":
                    if (args.Length < 2 || !uint.TryParse(args[1], out uint ac))
                    {
                        Console.Error.WriteLine("usage: set_lock <ac_seconds> [dc_seconds]  (0 = never)");
                        return 2;
                    }
                    uint dc = args.Length > 2 && uint.TryParse(args[2], out uint d) ? d : ac;
                    SetScreenOffTimeoutAC(ac);
                    SetScreenOffTimeoutDC(dc);
                    Console.WriteLine(ToJson());
                    return 0;
                default:
                    Console.Error.WriteLine("usage: AutoLockerScreenEx [check_lock | set_lock <ac_seconds> [dc_seconds]]");
                    return 2;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
