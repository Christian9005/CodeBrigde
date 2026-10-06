using CodeBridge.Core;
using CodeBridge.Core.Abstractions;
using CodeBridge.Transport.Serial;
using CodeBridge.Transport.Simulation;
using CodeBridge.Transport.Wifi;

namespace CodeBridge.ESP32;

/// <summary>
/// Extension methods to integrate ESP32 with the fluent builder.
/// Usage: var board = await CodeBridge.Connect().Serial("COM3").ToESP32().BuildAsync();
///        var board = await CodeBridge.Connect().WiFi("192.168.1.100").ToESP32().BuildAsync();
/// </summary>
public static class ESP32BoardExtensions
{
    /// <summary>
    /// Builds and connects to an ESP32 board using the configured transport.
    /// </summary>
    public static async Task<ESP32Board> BuildAsync(this BoardBuilder builder, CancellationToken ct = default)
    {
        ITransport transport = CreateTransport(builder);

        var board = new ESP32Board(transport);
        await board.ConnectAsync(ct);
        return board;
    }

    /// <summary>
    /// Creates an ESP32 board without connecting (for testing or deferred connection).
    /// </summary>
    public static ESP32Board Build(this BoardBuilder builder)
    {
        ITransport transport = CreateTransport(builder);
        return new ESP32Board(transport);
    }

    private static ITransport CreateTransport(BoardBuilder builder)
    {
        if (builder.Family != Core.Enums.BoardFamily.ESP32)
        {
            throw new NotSupportedException(
                $"The CodeBridge.ESP32 package only builds ESP32 boards, but the builder targets {builder.Family}. " +
                "Use .ToESP32(), or connect an Arduino Uno through CodeBridgeProtocolBoard.");
        }

        return builder.Protocol switch
        {
            Core.Enums.TransportProtocol.Serial => new SerialTransport(builder.ConnectionString, builder.BaudRate),
            Core.Enums.TransportProtocol.Simulator => new SimulatedTransport(),
            Core.Enums.TransportProtocol.WiFi => CreateWifiTransport(builder.ConnectionString, builder.AccessToken),
            _ => throw new NotSupportedException($"Protocol {builder.Protocol} is not yet supported for ESP32.")
        };
    }

    private static WifiTransport CreateWifiTransport(string connectionString, string? accessToken)
    {
        // ConnectionString format: "ip:port" or just "ip" (default port 8080)
        var parts = connectionString.Split(':');
        var ip = parts[0];
        var port = parts.Length > 1 ? int.Parse(parts[1]) : 8080;
        return new WifiTransport(ip, port, accessToken);
    }
}
