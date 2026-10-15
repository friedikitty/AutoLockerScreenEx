using System.Windows;
using System.Windows.Threading;

namespace AutoLockerScreenEx;

/// <summary>
/// Topmost notification: by default persistent (stays until Open settings or Dismiss);
/// with autoCloseSeconds > 0 it closes itself after that many seconds.
/// </summary>
public partial class NotificationWindow : Window
{
    private readonly Action _openMain;

    public NotificationWindow(string message, Action openMain, string? header = null, int autoCloseSeconds = 0)
    {
        InitializeComponent();
        if (header != null) HeaderText.Text = header;
        MessageText.Text = message;
        _openMain = openMain;

        // bottom-right, above the taskbar
        var wa = SystemParameters.WorkArea;
        Left = wa.Right - Width - 12;
        Top = wa.Bottom - Height - 12;

        if (autoCloseSeconds > 0)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(autoCloseSeconds) };
            timer.Tick += (_, _) => Close();
            timer.Start();
        }
    }

    public void UpdateMessage(string message) => MessageText.Text = message;

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        _openMain();
        Close();
    }

    private void Dismiss_Click(object sender, RoutedEventArgs e) => Close();
}
