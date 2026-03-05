namespace CodeBridge.Core.Abstractions;

/// <summary>
/// Represents the transport layer for communicating with the microcontroller.
/// Serial, WiFi, Bluetooth — all implement this interface.
/// </summary>
public interface ITransport : IDisposable
{
    /// <summary>
    /// Whether the transport is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Opens the connection to the microcontroller.
    /// </summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Closes the connection.
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Sends a command string to the microcontroller and waits for a response.
    /// </summary>
    Task<string> SendCommandAsync(string command, CancellationToken ct = default);

    /// <summary>
    /// Sends raw bytes to the microcontroller.
    /// </summary>
    Task SendRawAsync(byte[] data, CancellationToken ct = default);

    /// <summary>
    /// Receives raw bytes from the microcontroller.
    /// </summary>
    Task<byte[]> ReceiveRawAsync(int length, CancellationToken ct = default);

    /// <summary>
    /// Event triggered when data is received from the microcontroller asynchronously.
    /// </summary>
    event EventHandler<DataReceivedEventArgs> DataReceived;
}

/// <summary>
/// Event arguments for data received from the microcontroller.
/// </summary>
public class DataReceivedEventArgs : EventArgs
{
    public string Data { get; }
    public byte[]? RawData { get; }

    public DataReceivedEventArgs(string data, byte[]? rawData = null)
    {
        Data = data;
        RawData = rawData;
    }
}
