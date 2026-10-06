using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace CodeBridge.HomeAssistant;

/// <summary>A Home Assistant installation announced on the local network.</summary>
public sealed record HomeAssistantInstance(string Name, string Host, string? Address, int Port, string? BaseUrl, string? Version)
{
    /// <summary>The address to give to <see cref="HomeAssistantOptions.Broker"/>: the MQTT broker normally runs next to Home Assistant.</summary>
    public string BrokerHost => Address ?? Host;
}

/// <summary>
/// Looks for Home Assistant on the local network with mDNS (it announces <c>_home-assistant._tcp</c>), so a user does not have to
/// know its address. The MQTT broker normally lives on the same machine.
/// </summary>
public static class HomeAssistantFinder
{
    private const string ServiceName = "_home-assistant._tcp.local";
    private static readonly IPAddress MulticastGroup = IPAddress.Parse("224.0.0.251");

    /// <summary>Asks the network and collects answers for <paramref name="timeout"/> (default 3 seconds).</summary>
    public static async Task<IReadOnlyList<HomeAssistantInstance>> FindAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var wait = timeout ?? TimeSpan.FromSeconds(3);
        var records = new List<MdnsRecord>();

        try
        {
            using var client = new UdpClient(AddressFamily.InterNetwork) { ExclusiveAddressUse = false };
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            client.MulticastLoopback = false;

            var query = BuildQuery();
            foreach (var address in LocalIPv4Addresses())
            {
                try
                {
                    client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, address.GetAddressBytes());
                    await client.SendAsync(query, query.Length, new IPEndPoint(MulticastGroup, 5353));
                }
                catch (SocketException)
                {
                    // this interface cannot multicast: try the others
                }
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(wait);
            try
            {
                while (true)
                {
                    var result = await client.ReceiveAsync(cts.Token);
                    records.AddRange(MdnsParser.Parse(result.Buffer));
                }
            }
            catch (OperationCanceledException)
            {
                // the wait is over
            }
        }
        catch (SocketException)
        {
            return Array.Empty<HomeAssistantInstance>(); // no usable network
        }

        return MdnsParser.ToInstances(records);
    }

    internal static byte[] BuildQuery()
    {
        var bytes = new List<byte> { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 }; // id 0, flags 0, one question
        foreach (var label in ServiceName.Split('.'))
        {
            var text = Encoding.ASCII.GetBytes(label);
            bytes.Add((byte)text.Length);
            bytes.AddRange(text);
        }

        bytes.Add(0);
        bytes.AddRange(new byte[] { 0, 12, 0, 1 }); // PTR, IN
        return bytes.ToArray();
    }

    private static IEnumerable<IPAddress> LocalIPv4Addresses() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(i => i.OperationalStatus == OperationalStatus.Up && i.SupportsMulticast && i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(i => i.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.Address);
}

internal sealed record MdnsRecord(string Name, int Type, string? Target, int Port, IReadOnlyDictionary<string, string>? Text, IPAddress? Address);

/// <summary>A small DNS message reader: just what mDNS service announcements need.</summary>
internal static class MdnsParser
{
    private const int Ptr = 12, Txt = 16, A = 1, Srv = 33;

    public static IReadOnlyList<MdnsRecord> Parse(byte[] packet)
    {
        var records = new List<MdnsRecord>();
        try
        {
            if (packet.Length < 12)
                return records;

            int questions = Be16(packet, 4);
            int count = Be16(packet, 6) + Be16(packet, 8) + Be16(packet, 10);
            var pos = 12;

            for (var i = 0; i < questions; i++)
            {
                ReadName(packet, ref pos);
                pos += 4;
            }

            for (var i = 0; i < count && pos < packet.Length; i++)
            {
                var name = ReadName(packet, ref pos);
                var type = Be16(packet, pos);
                var length = Be16(packet, pos + 8);
                var data = pos + 10;
                pos = data + length;
                if (pos > packet.Length)
                    break;

                switch (type)
                {
                    case Ptr:
                    {
                        var p = data;
                        records.Add(new MdnsRecord(name, Ptr, ReadName(packet, ref p), 0, null, null));
                        break;
                    }
                    case Srv:
                    {
                        var p = data + 6;
                        records.Add(new MdnsRecord(name, Srv, ReadName(packet, ref p), Be16(packet, data + 4), null, null));
                        break;
                    }
                    case Txt:
                        records.Add(new MdnsRecord(name, Txt, null, 0, ReadText(packet, data, length), null));
                        break;
                    case A when length == 4:
                        records.Add(new MdnsRecord(name, A, null, 0, null, new IPAddress(new ReadOnlySpan<byte>(packet, data, 4))));
                        break;
                }
            }
        }
        catch (Exception ex) when (ex is IndexOutOfRangeException or ArgumentException or FormatException)
        {
            // A malformed or hostile packet only loses its own records.
        }

        return records;
    }

    public static IReadOnlyList<HomeAssistantInstance> ToInstances(IEnumerable<MdnsRecord> records)
    {
        var list = records.ToList();
        var instances = new Dictionary<string, HomeAssistantInstance>(StringComparer.OrdinalIgnoreCase);

        foreach (var ptr in list.Where(r => r.Type == 12 && r.Name.StartsWith("_home-assistant._tcp", StringComparison.OrdinalIgnoreCase) && r.Target is not null))
        {
            var instanceName = ptr.Target!;
            var srv = list.FirstOrDefault(r => r.Type == 33 && string.Equals(r.Name, instanceName, StringComparison.OrdinalIgnoreCase));
            var txt = list.FirstOrDefault(r => r.Type == 16 && string.Equals(r.Name, instanceName, StringComparison.OrdinalIgnoreCase))?.Text;
            var host = srv?.Target ?? instanceName;
            var address = list.FirstOrDefault(r => r.Type == 1 && string.Equals(r.Name, host, StringComparison.OrdinalIgnoreCase))?.Address?.ToString();

            var label = instanceName.Split('.')[0];
            if (txt is not null && txt.TryGetValue("location_name", out var location) && !string.IsNullOrWhiteSpace(location))
                label = location;

            instances[instanceName] = new HomeAssistantInstance(
                label,
                host.TrimEnd('.'),
                address,
                srv?.Port ?? 8123,
                txt is not null && txt.TryGetValue("base_url", out var baseUrl) ? baseUrl : null,
                txt is not null && txt.TryGetValue("version", out var version) ? version : null);
        }

        return instances.Values.ToList();
    }

    private static int Be16(byte[] data, int offset) => (data[offset] << 8) | data[offset + 1];

    private static string ReadName(byte[] data, ref int pos)
    {
        var labels = new List<string>();
        var cursor = pos;
        var jumped = false;
        var guard = 0;

        while (true)
        {
            if (++guard > 128)
                throw new FormatException("DNS name loop");

            int length = data[cursor];
            if (length == 0)
            {
                cursor++;
                break;
            }

            if ((length & 0xC0) == 0xC0)
            {
                var target = ((length & 0x3F) << 8) | data[cursor + 1];
                if (!jumped)
                    pos = cursor + 2;

                jumped = true;
                cursor = target;
                continue;
            }

            labels.Add(Encoding.UTF8.GetString(data, cursor + 1, length));
            cursor += 1 + length;
        }

        if (!jumped)
            pos = cursor;

        return string.Join('.', labels);
    }

    private static IReadOnlyDictionary<string, string> ReadText(byte[] data, int start, int length)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pos = start;
        var end = start + length;
        while (pos < end)
        {
            int size = data[pos++];
            if (size == 0 || pos + size > end)
                break;

            var entry = Encoding.UTF8.GetString(data, pos, size);
            pos += size;
            var equals = entry.IndexOf('=');
            if (equals > 0)
                result[entry[..equals]] = entry[(equals + 1)..];
        }

        return result;
    }
}
