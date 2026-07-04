using System.Net.Sockets;
using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Protocol;

namespace CodeBridge.Transport.Wifi;

/// <summary>
/// TCP socket transport for communicating with ESP32 over WiFi.
/// The ESP32 firmware runs a TCP server on port 8080 (default).
/// Same protocol as serial — just a different pipe.
/// </summary>
public class WifiTransport : ITransport
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly string _ipAddress;
    private readonly int _port;
    private readonly SemaphoreSlim _commandLock = new(1, 1);
    private bool _disposed;

    public bool IsConnected => _tcpClient?.Connected ?? false;

    public event EventHandler<DataReceivedEventArgs>? DataReceived;

    /// <summary>
    /// Creates a new WiFi transport.
    /// </summary>
    /// <param name="ipAddress">IP address of the ESP32 (e.g. "192.168.1.100")</param>
    /// <param name="port">TCP port (default: 8080)</param>
    public WifiTransport(string ipAddress, int port = 8080)
    {
        _ipAddress = ipAddress;
        _port = port;
    }

    /// <summary>
    /// Maximum connection attempts when the ESP32 is not yet reachable (e.g., booting).
    /// </summary>
    public int MaxConnectRetries { get; set; } = 5;

    /// <summary>
    /// Delay between connection retry attempts.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Exception? lastException = null;

        for (int attempt = 1; attempt <= MaxConnectRetries; attempt++)
        {
            try
            {
                _tcpClient = new TcpClient
                {
                    ReceiveTimeout = 5000,
                    SendTimeout = 5000,
                    NoDelay = true  // Disable Nagle for low-latency commands
                };

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

                await _tcpClient.ConnectAsync(_ipAddress, _port, timeoutCts.Token);

                _stream = _tcpClient.GetStream();
                _reader = new StreamReader(_stream);
                _writer = new StreamWriter(_stream) { AutoFlush = true };

                // Read the CODEBRIDGE_READY signal
                var readyCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                readyCts.CancelAfter(TimeSpan.FromSeconds(5));

                var ready = await _reader.ReadLineAsync(readyCts.Token);
                if (ready == null || !ready.Contains("CODEBRIDGE_READY"))
                    throw new InvalidOperationException(
                        $"CodeBridge firmware did not send ready signal. Got: {ready ?? "(null)"}");

                return; // Success!
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException or TimeoutException)
            {
                lastException = ex;
                _tcpClient?.Dispose();
                _tcpClient = null;
                _stream = null;
                _reader = null;
                _writer = null;

                if (attempt < MaxConnectRetries)
                {
                    await Task.Delay(RetryDelay, ct);
                }
            }
        }

        throw new InvalidOperationException(
            $"Could not connect to ESP32 at {_ipAddress}:{_port} after {MaxConnectRetries} attempts. " +
            $"Make sure the ESP32 is powered on and connected to WiFi.",
            lastException);
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _reader?.Dispose();
        _writer?.Dispose();
        _stream?.Dispose();

        if (_tcpClient?.Connected == true)
            _tcpClient.Close();

        _tcpClient?.Dispose();
        _tcpClient = null;
        _stream = null;
        _reader = null;
        _writer = null;

        return Task.CompletedTask;
    }

    public async Task<string> SendCommandAsync(string command, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_writer is null || _reader is null || _tcpClient is null || !_tcpClient.Connected)
            throw new InvalidOperationException("Not connected. Call ConnectAsync() first.");

        await _commandLock.WaitAsync(ct);
        try
        {
            // Send command
            await _writer.WriteAsync(command);

            // Read response with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _reader.ReadLineAsync(timeoutCts.Token);

            if (response is null)
                throw new InvalidOperationException("Connection closed by ESP32.");

            var trimmedResponse = response.Trim();
            DataReceived?.Invoke(this, new DataReceivedEventArgs(trimmedResponse));
            return trimmedResponse;
        }
        finally
        {
            _commandLock.Release();
        }
    }

    public async Task SendRawAsync(byte[] data, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_stream is null || _tcpClient is null || !_tcpClient.Connected)
            throw new InvalidOperationException("Not connected.");

        await _commandLock.WaitAsync(ct);
        try
        {
            await _stream.WriteAsync(data, ct);
        }
        finally
        {
            _commandLock.Release();
        }
    }

    public async Task<byte[]> ReceiveRawAsync(int length, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_stream is null || _tcpClient is null || !_tcpClient.Connected)
            throw new InvalidOperationException("Not connected.");

        var buffer = new byte[length];
        var totalRead = 0;

        while (totalRead < length)
        {
            var bytesRead = await _stream.ReadAsync(
                buffer.AsMemory(totalRead, length - totalRead), ct);

            if (bytesRead == 0)
                break; // Connection closed

            totalRead += bytesRead;
        }

        if (totalRead < length)
            Array.Resize(ref buffer, totalRead);

        return buffer;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _reader?.Dispose();
        _writer?.Dispose();
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _commandLock.Dispose();

        GC.SuppressFinalize(this);
    }
}
