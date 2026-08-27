using System.Windows;

namespace AutoLockerScreenEx;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
