using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NetworkScanner.Models;

/// <summary>
/// מכשיר אחד שנמצא ברשת. מממש INotifyPropertyChanged כדי שאפשר יהיה
/// לעדכן את שם המכשיר / היצרן בזמן שהסריקה עדיין רצה.
/// </summary>
public sealed class NetworkDevice : INotifyPropertyChanged
{
    private string _hostName = "מחפש…";
    private string _vendor = "";

    public string IpAddress { get; init; } = "";

    /// <summary>ערך מספרי של ה-IP, משמש למיון נכון (192.168.1.2 לפני 192.168.1.10).</summary>
    public uint IpSort { get; init; }

    public string MacAddress { get; init; } = "";

    public string Vendor
    {
        get => _vendor;
        set => SetField(ref _vendor, value);
    }

    public string HostName
    {
        get => _hostName;
        set => SetField(ref _hostName, value);
    }

    /// <summary>למשל: "המחשב הזה" או "נתב (Gateway)".</summary>
    public string Role { get; init; } = "";

    public string PingText { get; init; } = "—";

    /// <summary>הערכה גסה לפי ערך TTL בתשובת ה-Ping.</summary>
    public string OsGuess { get; init; } = "—";

    /// <summary>איך המכשיר זוהה: Ping / ARP בלבד / המחשב הזה.</summary>
    public string Status { get; init; } = "";

    /// <summary>כל השדות לפי סדר העמודות – לשימוש בהעתקה וייצוא.</summary>
    public string[] ToFields() => new[]
    {
        IpAddress, MacAddress, Vendor, HostName, Role, PingText, OsGuess, Status
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
