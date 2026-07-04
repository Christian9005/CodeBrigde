using System.Globalization;
using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Acquisition;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 firmware-buffered acquisition controller.
/// </summary>
public sealed class ESP32AcquisitionController : IBoardAcquisitionController
{
    private readonly ITransport _transport;
    private readonly Dictionary<string, BoardSampleChannelRequest> _channels = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public ESP32AcquisitionController(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public async Task<BoardSampleChannel> StartSamplingAsync(
        BoardSampleChannelRequest request,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        if (request.SampleRateHz <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Sample rate must be greater than zero.");

        if (request.BufferCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Buffer capacity must be greater than zero.");

        if (request.BatchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Batch size must be greater than zero.");

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(
                BridgeProtocol.CMD_SAMPLE_CONFIG,
                request.Pin,
                request.IsAnalog ? 1 : 0,
                (int)request.Mode,
                request.SampleRateHz,
                request.BufferCapacity,
                (int)request.Backpressure,
                request.BatchSize),
            ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Sample channel start failed: {data}");

        var channel = new BoardSampleChannel(data, request);
        _channels[data] = request;
        return channel;
    }

    public async Task<IReadOnlyList<BoardSampleFrame>> ReadSamplesAsync(
        string channelId,
        int maxFrames,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);

        if (maxFrames <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxFrames), "Max frames must be greater than zero.");

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SAMPLE_READ, channelId, maxFrames),
            ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Sample read failed: {data}");

        if (string.Equals(data, "NONE", StringComparison.OrdinalIgnoreCase))
            return Array.Empty<BoardSampleFrame>();

        var frames = new List<BoardSampleFrame>();
        foreach (var item in data.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = item.Split(',');
            if (parts.Length != 3)
                throw new InvalidOperationException($"Invalid sample frame '{item}'.");

            frames.Add(new BoardSampleFrame(
                channelId,
                long.Parse(parts[0], CultureInfo.InvariantCulture),
                TimeSpan.FromMicroseconds(long.Parse(parts[1], CultureInfo.InvariantCulture)),
                double.Parse(parts[2], CultureInfo.InvariantCulture)));
        }

        return frames;
    }

    public async Task StopSamplingAsync(string channelId, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SAMPLE_STOP, channelId),
            ct);

        var (success, data) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Sample channel stop failed: {data}");

        _channels.Remove(channelId);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _channels.Clear();
        GC.SuppressFinalize(this);
    }
}
