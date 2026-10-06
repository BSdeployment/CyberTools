using Microsoft.Win32;
using NetworkScanner.Models;
using NetworkScanner.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Navigation;

namespace NetworkScanner;

public partial class MainWindow : Window
{
    private static readonly string[] ColumnHeaders =
    {
        "כתובת IP", "כתובת MAC", "יצרן", "שם מכשיר", "תפקיד", "זמן תגובה", "מערכת הפעלה (הערכה)", "זיהוי"
    };

    private readonly ObservableCollection<NetworkDevice> _devices = new();
    private readonly OuiLookup _oui = new();
    private readonly NetworkScannerService _scanner;
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();

        _scanner = new NetworkScannerService(_oui);
        DevicesGrid.ItemsSource = _devices;
        _devices.CollectionChanged += (_, _) => CountText.Text = $"{_devices.Count} מכשירים";

        LoadAdapters();
        Loaded += async (_, _) => await EnsureVendorDatabaseAsync();
    }

    // ---------------------------------------------------------------
    //  כרטיסי רשת
    // ---------------------------------------------------------------

    private void LoadAdapters()
    {
        var adapters = NetworkScannerService.GetAdapters();
        AdapterBox.ItemsSource = adapters;
        AdapterBox.SelectedIndex = adapters.Count > 0 ? 0 : -1;
        ScanButton.IsEnabled = adapters.Count > 0;

        if (adapters.Count == 0)
            StatusText.Text = "לא נמצא כרטיס רשת פעיל עם כתובת IPv4. התחבר לרשת ולחץ על ↻";
    }


    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        // פתיחת הקישור בדפדפן ברירת המחדל
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true // נדרש ב-.NET Core / .NET 5+ כדי לפתוח דפדפן
        });

        // סימון האירוע כטופל
        e.Handled = true;
    }
    private void RefreshAdapters_Click(object sender, RoutedEventArgs e) => LoadAdapters();

    // ---------------------------------------------------------------
    //  סריקה
    // ---------------------------------------------------------------

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        if (AdapterBox.SelectedItem is not NetworkAdapterInfo adapter)
        {
            MessageBox.Show(this, "בחר כרטיס רשת לסריקה.", "סורק רשת", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SetScanning(true);
        _devices.Clear();
        ScanProgressBar.Value = 0;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        // Progress<T> שנוצר ב-UI thread מחזיר את העדכונים אליו אוטומטית
        var progress = new Progress<ScanProgress>(p =>
        {
            ScanProgressBar.Value = p.Percent;
            StatusText.Text = p.Message;
        });

        try
        {
            var found = await _scanner.DiscoverAsync(adapter, DeepScan.IsChecked == true, progress, ct);

            foreach (var device in found) _devices.Add(device);

            await _scanner.ResolveNamesAsync(found, progress, ct);

            ScanProgressBar.Value = 100;
            StatusText.Text = $"הסריקה הושלמה – נמצאו {found.Count} מכשירים ({DateTime.Now:HH:mm:ss})";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "הסריקה הופסקה";
        }
        catch (Exception ex)
        {
            StatusText.Text = "הסריקה נכשלה";
            MessageBox.Show(this, ex.Message, "שגיאה בסריקה", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            SetScanning(false);
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "עוצר…";
        _cts?.Cancel();
    }

    private void SetScanning(bool scanning)
    {
        ScanButton.IsEnabled = !scanning;
        StopButton.IsEnabled = scanning;
        AdapterBox.IsEnabled = !scanning;
        RefreshButton.IsEnabled = !scanning;
        DeepScan.IsEnabled = !scanning;
        UpdateVendorsButton.IsEnabled = !scanning;
    }

    // ---------------------------------------------------------------
    //  מאגר יצרנים
    // ---------------------------------------------------------------

    private async Task EnsureVendorDatabaseAsync()
    {
        if (_oui.HasCache)
        {
            StatusText.Text = $"מאגר יצרנים נטען ({_oui.Count:N0} רשומות). מוכן לסריקה";
            return;
        }

        StatusText.Text = "מוריד מאגר יצרנים מ-IEEE (פעם אחת בלבד)…";
        await DownloadVendorsAsync(silent: true);
    }

    private async void UpdateVendors_Click(object sender, RoutedEventArgs e)
    {
        UpdateVendorsButton.IsEnabled = false;
        StatusText.Text = "מוריד מאגר יצרנים מ-IEEE…";
        await DownloadVendorsAsync(silent: false);
        UpdateVendorsButton.IsEnabled = true;
    }

    private async Task DownloadVendorsAsync(bool silent)
    {
        try
        {
            int count = await _oui.UpdateFromIeeeAsync(CancellationToken.None);

            // מרעננים את היצרנים של המכשירים שכבר מוצגים
            foreach (var d in _devices)
                if (d.MacAddress.Contains(':')) d.Vendor = _oui.Lookup(d.MacAddress);

            StatusText.Text = $"מאגר היצרנים עודכן ({count:N0} רשומות)";
        }
        catch (Exception ex)
        {
            StatusText.Text = "לא ניתן להוריד את מאגר היצרנים – נעשה שימוש במאגר מצומצם";
            if (!silent)
                MessageBox.Show(this, "הורדת מאגר היצרנים נכשלה:\n" + ex.Message,
                    "סורק רשת", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ---------------------------------------------------------------
    //  העתקה וייצוא
    // ---------------------------------------------------------------

    private void CopyAll_Click(object sender, RoutedEventArgs e) => CopyDevices(_devices);

    private void CopySelected_Click(object sender, RoutedEventArgs e)
    {
        var selected = DevicesGrid.SelectedItems.OfType<NetworkDevice>().ToList();
        if (selected.Count == 0)
        {
            StatusText.Text = "לא נבחרו שורות. סמן שורות בטבלה, או השתמש ב״העתק הכל״";
            return;
        }
        CopyDevices(selected);
    }

    private void CopyDevices(IEnumerable<NetworkDevice> devices)
    {
        var list = devices.ToList();
        if (list.Count == 0)
        {
            StatusText.Text = "אין מה להעתיק – בצע סריקה קודם";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine(string.Join("\t", ColumnHeaders));
        foreach (var d in list) sb.AppendLine(string.Join("\t", d.ToFields()));

        try
        {
            Clipboard.SetText(sb.ToString());
            StatusText.Text = $"הועתקו {list.Count} שורות ללוח (אפשר להדביק באקסל או בכל מקום)";
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            StatusText.Text = "הלוח תפוס על ידי תוכנה אחרת – נסה שוב";
        }
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_devices.Count == 0)
        {
            StatusText.Text = "אין נתונים לייצוא – בצע סריקה קודם";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "קובץ CSV (*.csv)|*.csv",
            FileName = $"network-scan-{DateTime.Now:yyyyMMdd-HHmm}.csv",
        };
        if (dialog.ShowDialog(this) != true) return;

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", ColumnHeaders.Select(CsvEscape)));
        foreach (var d in _devices) sb.AppendLine(string.Join(",", d.ToFields().Select(CsvEscape)));

        try
        {
            // UTF-8 עם BOM כדי שאקסל יציג עברית נכון
            File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));
            StatusText.Text = $"הקובץ נשמר: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "שגיאה בשמירה", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string CsvEscape(string value) =>
        value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}
