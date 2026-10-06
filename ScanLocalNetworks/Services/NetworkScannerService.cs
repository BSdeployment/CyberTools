using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.InteropServices;
using NetworkScanner.Models;

namespace NetworkScanner.Services;

public readonly record struct ScanProgress(double Percent, string Message);

/// <summary>כרטיס רשת פעיל עם כתובת IPv4.</summary>
public sealed class NetworkAdapterInfo
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public IPAddress Address { get; init; } = IPAddress.None;
    public IPAddress Mask { get; init; } = IPAddress.None;
    public IPAddress? Gateway { get; init; }
    public int InterfaceIndex { get; init; }
    public string MacAddress { get; init; } = "";

    public override string ToString() => $"{Name}  –  {Address}  ({Description})";
}

public sealed class NetworkScannerService
{
    private readonly OuiLookup _oui;

    public NetworkScannerService(OuiLookup oui) => _oui = oui;

    // ------------------------------------------------------------------
    //  כרטיסי רשת
    // ------------------------------------------------------------------

    public static List<NetworkAdapterInfo> GetAdapters()
    {
        var list = new List<NetworkAdapterInfo>();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;

            try
            {
                var props = ni.GetIPProperties();
                var ua = props.UnicastAddresses.FirstOrDefault(a =>
                    a.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !a.Address.ToString().StartsWith("169.254."));

                if (ua?.IPv4Mask is null || ua.IPv4Mask.Equals(IPAddress.Any)) continue;

                var v4 = props.GetIPv4Properties();
                var gateway = props.GatewayAddresses
                    .Select(g => g.Address)
                    .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !a.Equals(IPAddress.Any));

                list.Add(new NetworkAdapterInfo
                {
                    Name = ni.Name,
                    Description = ni.Description,
                    Address = ua.Address,
                    Mask = ua.IPv4Mask,
                    Gateway = gateway,
                    InterfaceIndex = v4.Index,
                    MacAddress = FormatMac(ni.GetPhysicalAddress().GetAddressBytes()),
                });
            }
            catch (NetworkInformationException)
            {
                // כרטיס בלי IPv4 – מדלגים
            }
        }

        return list.OrderByDescending(a => a.Gateway is not null).ThenBy(a => a.Name).ToList();
    }

    // ------------------------------------------------------------------
    //  שלב 1: גילוי מכשירים (Ping + טבלת ARP)
    // ------------------------------------------------------------------

    public async Task<List<NetworkDevice>> DiscoverAsync(
        NetworkAdapterInfo adapter, bool deepScan, IProgress<ScanProgress> progress, CancellationToken ct)
    {
        uint ip = ToUInt(adapter.Address);
        uint mask = ToUInt(adapter.Mask);
        uint network = ip & mask;
        uint broadcast = network | ~mask;

        string note = "";
        if ((long)broadcast - network - 1 > 1022) // רשת גדולה מדי (גדולה מ-/22)
        {
            mask = 0xFFFFFF00;
            network = ip & mask;
            broadcast = network | ~mask;
            note = " (הרשת גדולה – נסרק טווח /24 סביב המחשב)";
        }

        int prefix = BitOperations.PopCount(mask);
        var targets = new List<uint>();
        for (uint a = network + 1; a < broadcast; a++)
            if (a != ip) targets.Add(a);

        progress.Report(new ScanProgress(0, $"סורק {targets.Count} כתובות ברשת {FromUInt(network)}/{prefix}{note}…"));

        // --- Ping sweep ---
        var replies = new ConcurrentDictionary<uint, (long Rtt, int Ttl)>();
        int rounds = deepScan ? 2 : 1;
        int timeoutMs = deepScan ? 1500 : 800;
        int totalWork = targets.Count * rounds;
        int done = 0;
        var buffer = new byte[32];

        for (int round = 0; round < rounds; round++)
        {
            ct.ThrowIfCancellationRequested();

            var pending = round == 0 ? targets : targets.Where(t => !replies.ContainsKey(t)).ToList();
            if (round > 0) Interlocked.Add(ref done, targets.Count - pending.Count);

            using var gate = new SemaphoreSlim(128);
            var tasks = pending.Select(async target =>
            {
                await gate.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    ct.ThrowIfCancellationRequested();
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(
                        FromUInt(target), timeoutMs, buffer, new PingOptions(128, false)).ConfigureAwait(false);

                    if (reply.Status == IPStatus.Success)
                        replies[target] = (reply.RoundtripTime, reply.Options?.Ttl ?? 0);
                }
                catch (OperationCanceledException) { throw; }
                catch { /* כתובת לא זמינה – מתעלמים */ }
                finally
                {
                    gate.Release();
                    int d = Interlocked.Increment(ref done);
                    if (d % 8 == 0 || d == totalWork)
                        progress.Report(new ScanProgress(
                            Math.Min(85.0, d * 85.0 / totalWork),
                            $"סורק… נמצאו עד כה {replies.Count} מכשירים מגיבים"));
                }
            }).ToList();

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        // נותנים ל-Windows רגע לסיים החלפות ARP, ואז קוראים את הטבלה
        progress.Report(new ScanProgress(88, "קורא את טבלת ה-ARP…"));
        await Task.Delay(400, ct).ConfigureAwait(false);
        var arp = ReadArpTable(adapter.InterfaceIndex);

        // --- איחוד: מי שענה ל-Ping + מי שמופיע ב-ARP (גם אם חסם Ping) ---
        var allIps = new SortedSet<uint>(replies.Keys);
        foreach (var a in arp.Keys)
            if (a > network && a < broadcast && a != ip) allIps.Add(a);

        var devices = new List<NetworkDevice>();

        // המחשב הזה
        devices.Add(new NetworkDevice
        {
            IpAddress = adapter.Address.ToString(),
            IpSort = ip,
            MacAddress = adapter.MacAddress,
            Vendor = _oui.Lookup(adapter.MacAddress),
            HostName = Environment.MachineName,
            Role = "המחשב הזה",
            PingText = "—",
            OsGuess = "Windows (המחשב הזה)",
            Status = "המחשב הזה",
        });

        uint? gatewayIp = adapter.Gateway is null ? null : ToUInt(adapter.Gateway);

        foreach (var addr in allIps)
        {
            bool pinged = replies.TryGetValue(addr, out var r);
            string mac = arp.TryGetValue(addr, out var macBytes) ? FormatMac(macBytes) : "לא זוהה";

            devices.Add(new NetworkDevice
            {
                IpAddress = FromUInt(addr).ToString(),
                IpSort = addr,
                MacAddress = mac,
                Vendor = macBytes is null ? "" : _oui.Lookup(mac),
                Role = gatewayIp == addr ? "נתב (Gateway)" : "",
                PingText = pinged ? $"{Math.Max(r.Rtt, 1)} ms" : "—",
                OsGuess = pinged ? GuessOs(r.Ttl) : "—",
                Status = pinged ? "מחובר (Ping)" : "זוהה ב-ARP (חוסם Ping)",
            });
        }

        devices.Sort((x, y) => x.IpSort.CompareTo(y.IpSort));
        progress.Report(new ScanProgress(90, $"נמצאו {devices.Count} מכשירים. מחפש שמות…"));
        return devices;
    }

    // ------------------------------------------------------------------
    //  שלב 2: שמות מכשירים (Reverse DNS)
    // ------------------------------------------------------------------

    public async Task ResolveNamesAsync(
        IReadOnlyList<NetworkDevice> devices, IProgress<ScanProgress> progress, CancellationToken ct)
    {
        var todo = devices.Where(d => d.HostName == "מחפש…").ToList();
        if (todo.Count == 0) return;

        int done = 0;
        using var gate = new SemaphoreSlim(32);

        var tasks = todo.Select(async d =>
        {
            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                d.HostName = await ResolveHostAsync(d.IpAddress, 2500).ConfigureAwait(false) ?? "—";
            }
            finally
            {
                gate.Release();
                int n = Interlocked.Increment(ref done);
                progress.Report(new ScanProgress(90 + n * 10.0 / todo.Count, $"מחפש שמות… ({n}/{todo.Count})"));
            }
        }).ToList();

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static async Task<string?> ResolveHostAsync(string ip, int timeoutMs)
    {
        try
        {
            var lookup = Dns.GetHostEntryAsync(ip);
            // אם נגמר הזמן, מונעים חריגה לא נצפית מהמשימה שממשיכה ברקע
            _ = lookup.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);

            var finished = await Task.WhenAny(lookup, Task.Delay(timeoutMs)).ConfigureAwait(false);
            if (finished != lookup) return null;

            var name = (await lookup.ConfigureAwait(false)).HostName;
            return string.IsNullOrWhiteSpace(name) || name == ip ? null : name;
        }
        catch
        {
            return null;
        }
    }

    // ------------------------------------------------------------------
    //  טבלת ARP דרך iphlpapi.dll
    // ------------------------------------------------------------------

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern int GetIpNetTable(IntPtr pIpNetTable, ref int pdwSize, bool bOrder);

    private const int ErrorInsufficientBuffer = 122;
    private const int MibIpNetTypeInvalid = 2;

    /// <summary>
    /// מחזיר IP (כמספר) → כתובת MAC עבור כרטיס הרשת הנתון.
    /// מבנה MIB_IPNETROW: Index(4) PhysAddrLen(4) PhysAddr(8) Addr(4) Type(4) = 24 בתים.
    /// </summary>
    private static Dictionary<uint, byte[]> ReadArpTable(int interfaceIndex)
    {
        var result = new Dictionary<uint, byte[]>();

        int size = 0;
        int rc = GetIpNetTable(IntPtr.Zero, ref size, false);
        if (rc != ErrorInsufficientBuffer && rc != 0) return result;
        if (size <= 0) return result;

        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            rc = GetIpNetTable(buffer, ref size, false);
            if (rc != 0) return result;

            int count = Marshal.ReadInt32(buffer);
            int offset = 4;
            const int rowSize = 24;

            for (int i = 0; i < count; i++, offset += rowSize)
            {
                int index = Marshal.ReadInt32(buffer, offset);
                int macLen = Marshal.ReadInt32(buffer, offset + 4);
                int addr = Marshal.ReadInt32(buffer, offset + 16);
                int type = Marshal.ReadInt32(buffer, offset + 20);

                if (index != interfaceIndex || macLen != 6 || type == MibIpNetTypeInvalid) continue;

                var mac = new byte[6];
                Marshal.Copy(IntPtr.Add(buffer, offset + 8), mac, 0, 6);

                if (mac.All(b => b == 0xFF) || mac.All(b => b == 0x00)) continue;

                var b4 = BitConverter.GetBytes(addr); // הבתים כבר בסדר רשת
                uint ipValue = ((uint)b4[0] << 24) | ((uint)b4[1] << 16) | ((uint)b4[2] << 8) | b4[3];
                result[ipValue] = mac;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return result;
    }

    // ------------------------------------------------------------------
    //  עזרים
    // ------------------------------------------------------------------

    private static string GuessOs(int ttl) => ttl switch
    {
        <= 0 => "—",
        <= 64 => $"Linux / Android / iOS / macOS / IoT (TTL {ttl})",
        <= 128 => $"Windows (TTL {ttl})",
        _ => $"ציוד רשת (TTL {ttl})",
    };

    private static string FormatMac(byte[] bytes) => string.Join(":", bytes.Select(b => b.ToString("X2")));

    private static uint ToUInt(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }

    private static IPAddress FromUInt(uint v) =>
        new(new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v });
}
