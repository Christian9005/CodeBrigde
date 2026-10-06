using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Enums;

namespace CodeBridge.Core;

/// <summary>
/// Fluent builder for connecting to a board.
/// Usage: var board = await CodeBridge.Connect().Via(TransportProtocol.Serial, "COM3").ToESP32();
/// </summary>
public static class CodeBridgeBuilder
{
    public static ConnectionBuilder Connect() => new();
}

public class ConnectionBuilder
{
    private TransportProtocol _protocol = TransportProtocol.Serial;
    private string _connectionString = "";
    private int _baudRate = 115200;
    private string? _accessToken;

    /// <summary>
    /// Specify the communication protocol and connection target.
    /// </summary>
    public ConnectionBuilder Via(TransportProtocol protocol, string connectionString)
    {
        _protocol = protocol;
        _connectionString = connectionString;
        return this;
    }

    /// <summary>
    /// Specify serial port and baud rate.
    /// </summary>
    public ConnectionBuilder Serial(string port, int baudRate = 115200)
    {
        _protocol = TransportProtocol.Serial;
        _connectionString = port;
        _baudRate = baudRate;
        return this;
    }

    /// <summary>
    /// Specify WiFi connection.
    /// </summary>
    public ConnectionBuilder WiFi(string ipAddress, int port = 8080, string? accessToken = null)
    {
        _protocol = TransportProtocol.WiFi;
        _connectionString = $"{ipAddress}:{port}";
        _accessToken = accessToken;
        return this;
    }

    /// <summary>
    /// Use the built-in virtual board instead of hardware (demos, tests, learning). Needs no cable and no firmware.
    /// </summary>
    public ConnectionBuilder Simulator()
    {
        _protocol = TransportProtocol.Simulator;
        _connectionString = "simulator";
        return this;
    }

    /// <summary>
    /// Connect to an ESP32 board. Returns a configured IBoard instance.
    /// </summary>
    public BoardBuilder ToESP32() => new(_protocol, _connectionString, _baudRate, BoardFamily.ESP32, _accessToken);

    /// <summary>
    /// Connect to an Arduino board. Returns a configured IBoard instance.
    /// </summary>
    public BoardBuilder ToArduino() => new(_protocol, _connectionString, _baudRate, BoardFamily.Arduino, _accessToken);

    /// <summary>
    /// Connect to an STM32 board. Returns a configured IBoard instance.
    /// </summary>
    public BoardBuilder ToSTM32() => new(_protocol, _connectionString, _baudRate, BoardFamily.STM32, _accessToken);
}

public class BoardBuilder
{
    internal TransportProtocol Protocol { get; }
    internal string ConnectionString { get; }
    internal int BaudRate { get; }
    internal BoardFamily Family { get; }
    internal string? AccessToken { get; }

    internal BoardBuilder(TransportProtocol protocol, string connectionString, int baudRate, BoardFamily family, string? accessToken = null)
    {
        AccessToken = accessToken;
        Protocol = protocol;
        ConnectionString = connectionString;
        BaudRate = baudRate;
        Family = family;
    }

    // Board implementations will provide extension methods here.
    // e.g., CodeBridge.ESP32 adds: .BuildAsync() that returns an ESP32Board
}
