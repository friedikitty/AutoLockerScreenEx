using System.Windows;

namespace AutoLockerScreenEx;

/// <summary>
/// Persistent topmost warning: unlike a balloon tip (auto-dismissed to the Action
/// Center), this stays on screen until the user clicks Open settings or Dismiss.
/// </summary>
public partial class NotificationWindow : Window
{
    private readonly Action _openMain;

    public NotificationWindow(string message, Action openMain)
    {
        InitializeComponent();
        MessageText.Text = message;
        _openMain = openMain;

        // bottom-right, above the taskbar
        var wa = SystemParameters.WorkArea;
        Left = wa.Right - Width - 12;
        Top = wa.Bottom - Height - 12;
    }

    public void UpdateMessage(string message) => MessageText.Text = message;

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        _openMain();
        Close();
    }

    private void Dismiss_Click(object sender, RoutedEventArgs e) => Close();
}
