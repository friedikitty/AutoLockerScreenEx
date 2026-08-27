using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoLockerScreenEx;

/// <summary>ViewModel for MainWindow. Every change is persisted to the config file.</summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly Config _config = Config.Load();

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool MonitorEnabled
    {
        get => _config.MonitorEnabled;
        set { _config.MonitorEnabled = value; Changed(); }
    }

    /// <summary>Interval textbox text; only persisted when it's a valid positive integer.</summary>
    public string MonitorIntervalText
    {
        get => _config.MonitorIntervalSeconds.ToString();
        set
        {
            if (uint.TryParse(value, out uint n) && n > 0)
            {
                _config.MonitorIntervalSeconds = n;
            }
            Changed();
        }
    }

    public uint MonitorIntervalSeconds => _config.MonitorIntervalSeconds;

    private void Changed([CallerMemberName] string? name = null)
    {
        _config.Save();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
