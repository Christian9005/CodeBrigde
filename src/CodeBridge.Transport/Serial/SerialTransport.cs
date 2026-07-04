using System.IO.Ports;
using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Protocol;

namespace CodeBridge.Transport.Serial;

/// <summary>
/// Serial port transport for communicating with microcontrollers via USB.
/// This is the primary transport for the MVP.
/// </summary>
public class SerialTransport : ITransport
{
    private SerialPort? _serialPort;
    private readonly string _portName;
    private readonly int _baudRate;
    private readonly SemaphoreSlim _commandLock = new(1, 1);
    private TaskCompletionSource<string>? _responseWaiter;
    private TaskCompletionSource<bool>? _readyWaiter;
    private bool _disposed;

    /// <summary>
    /// Command response timeout in seconds. Default is 5s. 
    /// Increase for long-running commands like WiFi configuration.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 5;

    public bool IsConnected => _serialPort?.IsOpen ?? false;

    public event EventHandler<DataReceivedEventArgs>? DataReceived;

    /// <summary>
    /// Creates a new serial transport.
    /// </summary>
    /// <param name="portName">COM port name (e.g., "COM3" on Windows, "/dev/ttyUSB0" on Linux)</param>
    /// <param name="baudRate">Baud rate (default: 115200)</param>
    public SerialTransport(string portName, int baudRate = 115200)
    {
        _portName = portName;
        _baudRate = baudRate;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _serialPort = new SerialPort(_portName, _baudRate)
        {
            ReadTimeout = 5000,
            WriteTimeout = 5000,
            DtrEnable = true,
            RtsEnable = true,
            NewLine = "\n"
        };

        // Set up ready signal waiter before opening (DTR causes reset)
        _readyWaiter = new TaskCompletionSource<bool>();

        _serialPort.DataReceived += OnSerialDataReceived;
        _serialPort.Open();

        // Wait for the firmware to send CODEBRIDGE_READY (up to 15s for WiFi boot).
        // If a board is already running and misses the ready banner, fall back to PING.
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));
            using var reg = timeoutCts.Token.Register(() =>
                _readyWaiter.TrySetException(
                    new TimeoutException("CodeBridge firmware did not send CODEBRIDGE_READY within 15 seconds.")));

            await _readyWaiter.Task;
        }
        catch (TimeoutException)
        {
            if (!await TryPingExistingSessionAsync(ct))
                throw;

            // The firmware is responsive even though the ready banner was not observed.
        }
        finally
        {
            _readyWaiter = null;
        }
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_serialPort?.IsOpen == true)
        {
            _serialPort.DataReceived -= OnSerialDataReceived;
            _serialPort.Close();
        }
        return Task.CompletedTask;
    }

    public async Task<string> SendCommandAsync(string command, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_serialPort is null || !_serialPort.IsOpen)
            throw new InvalidOperationException("Serial port is not connected. Call ConnectAsync() first.");

        await _commandLock.WaitAsync(ct);
        try
        {
            // Clear any pending data
            if (_serialPort.BytesToRead > 0)
                _serialPort.DiscardInBuffer();

            // Set up response waiter
            _responseWaiter = new TaskCompletionSource<string>();

            // Register cancellation
            using var registration = ct.Register(() =>
                _responseWaiter.TrySetCanceled(ct));

            // Send command
            _serialPort.Write(command);

            // Wait for response with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(CommandTimeoutSeconds));

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

    public async Task SendRawAsync(byte[] data, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_serialPort is null || !_serialPort.IsOpen)
            throw new InvalidOperationException("Serial port is not connected.");

        await _commandLock.WaitAsync(ct);
        try
        {
            _serialPort.Write(data, 0, data.Length);
        }
        finally
        {
            _commandLock.Release();
        }
    }

    private async Task<bool> TryPingExistingSessionAsync(CancellationToken ct)
    {
        _readyWaiter = null;

        try
        {
            await Task.Delay(250, ct);
            var response = await SendCommandAsync(
                BridgeProtocol.BuildCommand(BridgeProtocol.CMD_PING), ct);
            var (success, data) = BridgeProtocol.ParseResponse(response);
            return success && data == "PONG";
        }
        catch
        {
            return false;
        }
    }

    public Task<byte[]> ReceiveRawAsync(int length, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_serialPort is null || !_serialPort.IsOpen)
            throw new InvalidOperationException("Serial port is not connected.");

        var buffer = new byte[length];
        var bytesRead = _serialPort.Read(buffer, 0, length);

        if (bytesRead < length)
            Array.Resize(ref buffer, bytesRead);

        return Task.FromResult(buffer);
    }

    /// <summary>
    /// Lists all available serial ports on the system.
    /// </summary>
    public static string[] GetAvailablePorts() => SerialPort.GetPortNames();

    private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort is null) return;

        try
        {
            var line = _serialPort.ReadLine().Trim();

            if (string.IsNullOrEmpty(line))
                return;

            // Check for CODEBRIDGE_READY signal during boot
            if (_readyWaiter is not null && line.Contains("CODEBRIDGE_READY"))
            {
                _readyWaiter.TrySetResult(true);
                return;
            }

            // During boot phase, discard all other messages
            if (_readyWaiter is not null)
                return;

            // If we're waiting for a command response, only accept protocol responses
            if (_responseWaiter is not null)
            {
                if (line.StartsWith("OK") || line.StartsWith("ERR"))
                {
                    _responseWaiter.TrySetResult(line);
                }
                // Discard non-protocol lines (debug messages, WiFi status, etc.)
                return;
            }

            // Otherwise, raise as async data event
            DataReceived?.Invoke(this, new DataReceivedEventArgs(line));
        }
        catch (TimeoutException)
        {
            // Ignore read timeouts
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_serialPort?.IsOpen == true)
        {
            _serialPort.DataReceived -= OnSerialDataReceived;
            _serialPort.Close();
        }
        _serialPort?.Dispose();
        _commandLock.Dispose();

        GC.SuppressFinalize(this);
    }
}
