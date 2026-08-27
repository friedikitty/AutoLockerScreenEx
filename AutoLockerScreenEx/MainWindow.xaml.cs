using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace AutoLockerScreenEx;

public partial class MainWindow : Window
{
    private readonly WinForms.NotifyIcon _tray;
    private readonly DispatcherTimer _monitorTimer = new();
    private bool _reallyExit;

    public MainWindow()
    {
        InitializeComponent();

        _tray = new WinForms.NotifyIcon
        {
            Text = "Auto Locker Screen",
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!),
            Visible = true,
        };
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowMain());
        menu.Items.Add("Exit", null, (_, _) => { _reallyExit = true; Close(); });
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => ShowMain();

        _monitorTimer.Tick += Monitor_Tick;
    }

    private void Monitor_Changed(object sender, RoutedEventArgs e)
    {
        _monitorTimer.Stop();
        if (MonitorBox.IsChecked == true
            && uint.TryParse(IntervalBox.Text, out uint n) && n > 0)
        {
            _monitorTimer.Interval = TimeSpan.FromSeconds(n);
            _monitorTimer.Start();
        }
    }

    private void Monitor_Tick(object? sender, EventArgs e)
    {
        try
        {
            var (ac, dc) = ScreenOffTimeout.Get();
            if (ac <= 1 || dc <= 1)
            {
                // ponytail: re-warns on every failed check while the lock stays off;
                // add a "already warned" state flag if the repeat spam bothers you.
                _tray.ShowBalloonTip(10000, "Screen auto-lock is OFF!",
                    $"Screen-off timeout is AC={ac}s, DC={dc}s (0 = never). Set a real value!",
                    WinForms.ToolTipIcon.Warning);
            }
        }
        catch
        {
            // transient read failure; try again next tick
        }
    }

    private void ShowMain()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_reallyExit)
        {
            // Closing the window just sits to the tray; Exit menu does the real exit.
            e.Cancel = true;
            Hide();
            return;
        }
        _monitorTimer.Stop();
        _tray.Dispose();
        base.OnClosing(e);
    }

    private void Check_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var (ac, dc) = ScreenOffTimeout.Get();
            AcBox.Text = ac.ToString();
            DcBox.Text = dc.ToString();
            StatusText.Text = $"Auto-lock after AC: {Format(ac)}, DC: {Format(dc)} of inactivity.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Check failed: " + ex.Message;
        }
    }

    private void Set_Click(object sender, RoutedEventArgs e)
    {
        if (!uint.TryParse(AcBox.Text, out uint ac) || !uint.TryParse(DcBox.Text, out uint dc))
        {
            StatusText.Text = "Enter non-negative integers for both AC and DC seconds.";
            return;
        }
        try
        {
            ScreenOffTimeout.SetScreenOffTimeoutAC(ac);
            ScreenOffTimeout.SetScreenOffTimeoutDC(dc);
            Check_Click(sender, e);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Set failed: " + ex.Message;
        }
    }

    private static string Format(uint s) =>
        s == 0 ? "never (0)" : $"{s} sec ({s / 60} min {s % 60} sec)";
}
