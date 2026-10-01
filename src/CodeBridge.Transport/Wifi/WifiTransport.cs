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
    private TaskCompletionSource<string>? _responseWaiter;
    private TaskCompletionSource<bool>? _readyWaiter;
    private CancellationTokenSource? _readLoopCts;
    private Task? _readLoopTask;
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

                // Start background read loop
                _readyWaiter = new TaskCompletionSource<bool>();
                _readLoopCts = new CancellationTokenSource();
                _readLoopTask = Task.Run(() => ReadLoopAsync(_readLoopCts.Token), ct);

                try
                {
                    using var readyTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    readyTimeoutCts.CancelAfter(TimeSpan.FromSeconds(15));
                    using var reg = readyTimeoutCts.Token.Register(() =>
                        _readyWaiter.TrySetException(
                            new TimeoutException("CodeBridge firmware did not send CODEBRIDGE_READY within 15 seconds.")));

                    await _readyWaiter.Task;
                }
                catch (TimeoutException)
                {
                    if (!await TryPingExistingSessionAsync(ct))
                        throw;
                }
                finally
                {
                    _readyWaiter = null;
                }

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

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_readLoopCts is not null)
        {
            _readLoopCts.Cancel();
            try { await (_readLoopTask ?? Task.CompletedTask); } catch { }
            _readLoopCts.Dispose();
            _readLoopCts = null;
        }

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
    }

    public async Task<string> SendCommandAsync(string command, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_writer is null || _reader is null || _tcpClient is null || !_tcpClient.Connected)
            throw new InvalidOperationException("Not connected. Call ConnectAsync() first.");

        await _commandLock.WaitAsync(ct);
        try
        {
            _responseWaiter = new TaskCompletionSource<string>();
            using var registration = ct.Register(() => _responseWaiter.TrySetCanceled(ct));

            await _writer.WriteAsync(command);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
            using var timeoutRegistration = timeoutCts.Token.Register(() =>
                _responseWaiter.TrySetException(
                    new TimeoutException($"No response received for command: {command.Trim()}")));

            return await _responseWaiter.Task;
        }
        finally
        {
            _responseWaiter = null;
            _commandLock.Release();
        }
    }

    private async Task<bool> TryPingExistingSessionAsync(CancellationToken ct)
    {
        _readyWaiter = null;
        try
        {
            await Task.Delay(250, ct);
            var response = await SendCommandAsync(BridgeProtocol.BuildCommand(BridgeProtocol.CMD_PING), ct);
            var (success, data) = BridgeProtocol.ParseResponse(response);
            return success && data == "PONG";
        }
        catch
        {
            return false;
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

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        if (_reader is null) return;
        try
        {
            while (!ct.IsCancellationRequested && _tcpClient?.Connected == true)
            {
                var line = await _reader.ReadLineAsync(ct);
                if (string.IsNullOrEmpty(line)) continue;
                
                line = line.Trim();

                if (_readyWaiter is not null && line.Contains("CODEBRIDGE_READY"))
                {
                    _readyWaiter.TrySetResult(true);
                    continue;
                }

                if (_readyWaiter is not null)
                    continue;

                if (_responseWaiter is not null)
                {
                    if (line.StartsWith("OK") || line.StartsWith("ERR"))
                    {
                        _responseWaiter.TrySetResult(line);
                    }
                    continue;
                }

                DataReceived?.Invoke(this, new DataReceivedEventArgs(line));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { /* Handle disconnects gracefully */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_readLoopCts is not null)
        {
            _readLoopCts.Cancel();
            _readLoopCts.Dispose();
        }

        _reader?.Dispose();
        _writer?.Dispose();
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _commandLock.Dispose();

        GC.SuppressFinalize(this);
    }
}
