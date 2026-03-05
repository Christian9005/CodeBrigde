using CodeBridge.Core.Abstractions;

namespace CodeBridge.Core.Tests.Mocks;

/// <summary>
/// Mock transport that returns predefined responses for testing.
/// No real serial port needed!
/// </summary>
public class MockTransport : ITransport
{
    private readonly Queue<string> _responses = new();
    private readonly List<string> _sentCommands = new();
    private bool _connected;

    public bool IsConnected => _connected;

    public IReadOnlyList<string> SentCommands => _sentCommands.AsReadOnly();
    public string LastCommand => _sentCommands.LastOrDefault() ?? "";

    public event EventHandler<DataReceivedEventArgs>? DataReceived;

    /// <summary>
    /// Queue a response that will be returned by the next SendCommandAsync call.
    /// </summary>
    public void EnqueueResponse(string response)
    {
        _responses.Enqueue(response);
    }

    /// <summary>
    /// Queue multiple responses.
    /// </summary>
    public void EnqueueResponses(params string[] responses)
    {
        foreach (var r in responses)
            _responses.Enqueue(r);
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        _connected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _connected = false;
        return Task.CompletedTask;
    }

    public Task<string> SendCommandAsync(string command, CancellationToken ct = default)
    {
        _sentCommands.Add(command);

        if (_responses.Count == 0)
            throw new InvalidOperationException(
                $"No mock response queued for command: {command.Trim()}");

        return Task.FromResult(_responses.Dequeue());
    }

    public Task SendRawAsync(byte[] data, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<byte[]> ReceiveRawAsync(int length, CancellationToken ct = default)
    {
        return Task.FromResult(new byte[length]);
    }

    /// <summary>
    /// Simulate receiving async data from the board.
    /// </summary>
    public void SimulateDataReceived(string data)
    {
        DataReceived?.Invoke(this, new DataReceivedEventArgs(data));
    }

    public void Dispose()
    {
        _connected = false;
        GC.SuppressFinalize(this);
    }
}
