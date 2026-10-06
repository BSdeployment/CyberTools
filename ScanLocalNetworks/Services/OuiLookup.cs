using System.IO;
using System.Net.Http;
using System.Text;

namespace NetworkScanner.Services;

/// <summary>
/// זיהוי יצרן לפי 3 הבתים הראשונים של כתובת ה-MAC (OUI).
/// מקורות, לפי סדר: מאגר מובנה קטן → קובץ מקומי שהורד מ-IEEE (אם קיים).
/// הקובץ המלא מכיל עשרות אלפי יצרנים ונשמר ב-%LocalAppData%\NetworkScanner\oui.csv.
/// </summary>
public sealed class OuiLookup
{
    private const string IeeeUrl = "https://standards-oui.ieee.org/oui/oui.csv";

    private volatile Dictionary<string, string> _map;

    public static string CachePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NetworkScanner", "oui.csv");

    public bool HasCache => File.Exists(CachePath);
    public int Count => _map.Count;

    public OuiLookup()
    {
        var map = BuiltIn();
        try
        {
            if (File.Exists(CachePath)) LoadCsvInto(map, CachePath);
        }
        catch
        {
            // קובץ פגום – נמשיך עם המאגר המובנה; אפשר להוריד מחדש מהכפתור.
        }
        _map = map;
    }

    /// <summary>מחזיר שם יצרן עבור MAC בפורמט AA:BB:CC:DD:EE:FF.</summary>
    public string Lookup(string mac)
    {
        var hex = new string(mac.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
        if (hex.Length < 6) return "";

        // ביט 2 של הבייט הראשון = כתובת מנוהלת מקומית (MAC אקראי של פרטיות בטלפונים ובמחשבים)
        int firstByte = Convert.ToInt32(hex.Substring(0, 2), 16);
        if ((firstByte & 0x02) != 0) return "כתובת MAC אקראית (פרטיות)";

        return _map.TryGetValue(hex.Substring(0, 6), out var vendor) ? vendor : "לא ידוע";
    }

    /// <summary>מוריד את הרשימה המלאה מ-IEEE, שומר מקומית וטוען. מחזיר מספר רשומות.</summary>
    public async Task<int> UpdateFromIeeeAsync(CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 NetworkScanner/1.0");

        var data = await http.GetByteArrayAsync(IeeeUrl, ct).ConfigureAwait(false);

        Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
        var tmp = CachePath + ".tmp";
        await File.WriteAllBytesAsync(tmp, data, ct).ConfigureAwait(false);

        var map = BuiltIn();
        LoadCsvInto(map, tmp);
        if (map.Count < 1000)
        {
            File.Delete(tmp);
            throw new InvalidDataException("הקובץ שהתקבל אינו מאגר יצרנים תקין.");
        }

        File.Move(tmp, CachePath, overwrite: true);
        _map = map;
        return map.Count;
    }

    // ---------- פנימי ----------

    private static void LoadCsvInto(Dictionary<string, string> map, string path)
    {
        bool first = true;
        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            if (first) { first = false; continue; } // שורת כותרת
            var f = ParseCsvLine(line);
            if (f.Count >= 3 && f[1].Length == 6)
                map[f[1].ToUpperInvariant()] = f[2].Trim();
        }
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>(4);
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == ',') { fields.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(c);
        }
        fields.Add(sb.ToString());
        return fields;
    }

    /// <summary>מאגר מצומצם של יצרנים מוכרים – גיבוי עד להורדת הרשימה המלאה.</summary>
    private static Dictionary<string, string> BuiltIn() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["000C29"] = "VMware, Inc.",
        ["005056"] = "VMware, Inc.",
        ["000569"] = "VMware, Inc.",
        ["080027"] = "PCS Systemtechnik GmbH (VirtualBox)",
        ["00155D"] = "Microsoft Corporation (Hyper-V)",
        ["0050F2"] = "Microsoft Corporation",
        ["B827EB"] = "Raspberry Pi Foundation",
        ["DCA632"] = "Raspberry Pi Trading Ltd",
        ["E45F01"] = "Raspberry Pi Trading Ltd",
        ["001788"] = "Philips Lighting BV (Hue)",
        ["18B430"] = "Nest Labs Inc.",
        ["00000C"] = "Cisco Systems, Inc",
        ["001B63"] = "Apple, Inc.",
        ["000393"] = "Apple, Inc.",
        ["000A95"] = "Apple, Inc.",
        ["001A11"] = "Google, Inc.",
        ["3C5AB4"] = "Google, Inc.",
        ["F4F5D8"] = "Google, Inc.",
        ["001B21"] = "Intel Corporate",
        ["001422"] = "Dell Inc.",
        ["00E04C"] = "Realtek Semiconductor Corp.",
        ["44650D"] = "Amazon Technologies Inc.",
        ["F0272D"] = "Amazon Technologies Inc.",
    };
}
