using System.Globalization;
using CodeBridge.Core.Abstractions;

namespace CodeBridge.Transport;

/// <summary>What the board was just told to do with one pin.</summary>
/// <param name="Kind">
/// <c>mode</c> (pin mode set, value = mode number), <c>digital</c> (output written, 0/1), <c>input</c> (digital read, 0/1),
/// <c>analog</c> (analog read), <c>pwm</c> (duty 0-255), <c>servo</c> (angle), <c>servo-off</c> or <c>tone</c> (frequency, 0 = off).
/// </param>
public sealed record PinActivity(int Pin, string Kind, double Value)
{
    /// <summary>
    /// Reads a command that was sent to the board and the board's answer. Returns null for commands that are not about a pin
    /// and for commands the board rejected.
    /// </summary>
    public static PinActivity? Parse(string command, string response)
    {
        if (string.IsNullOrWhiteSpace(command) || response is null || !response.StartsWith("OK", StringComparison.Ordinal))
            return null;

        var parts = command.Trim().Split(':');
        if (parts.Length < 2 || !TryInt(parts[1], out var pin))
            return null;

        switch (parts[0])
        {
            case "PM": return parts.Length > 2 && TryNumber(parts[2], out var mode) ? new(pin, "mode", mode) : null;
            case "DW": return parts.Length > 2 && TryNumber(parts[2], out var level) ? new(pin, "digital", level != 0 ? 1 : 0) : null;
            case "PW": return parts.Length > 2 && TryNumber(parts[2], out var duty) ? new(pin, "pwm", duty) : null;
            case "SV": return parts.Length > 2 && TryNumber(parts[2], out var angle) ? new(pin, "servo", angle) : null;
            case "SD": return new(pin, "servo-off", 0);
            case "TN": return parts.Length > 2 && TryNumber(parts[2], out var frequency) ? new(pin, "tone", frequency) : null;
            case "NT": return new(pin, "tone", 0);
            case "DR": return Answer(pin, "input", response, v => v != 0 ? 1 : 0);
            case "AR": return Answer(pin, "analog", response, v => v);
            default: return null;
        }
    }

    private static PinActivity? Answer(int pin, string kind, string response, Func<double, double> map) =>
        response.Length > 3 && TryNumber(response[3..], out var value) ? new PinActivity(pin, kind, map(value)) : null;

    private static bool TryInt(string text, out int value) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    private static bool TryNumber(string text, out double value) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}

/// <summary>
/// Wraps a transport and reports every command with the board's answer, without changing anything. The editor uses it to draw
/// what the board is doing (which pin went HIGH, which servo moved) on real boards and on the simulator alike.
/// </summary>
public sealed class CommandTapTransport : ITransport
{
    private readonly ITransport _inner;
    private readonly Action<string, string> _observer;

    public CommandTapTransport(ITransport inner, Action<string, string> observer)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
    }

    public bool IsConnected => _inner.IsConnected;

    public event EventHandler<DataReceivedEventArgs>? DataReceived
    {
        add => _inner.DataReceived += value;
        remove => _inner.DataReceived -= value;
    }

    public Task ConnectAsync(CancellationToken ct = default) => _inner.ConnectAsync(ct);

    public Task DisconnectAsync(CancellationToken ct = default) => _inner.DisconnectAsync(ct);

    public async Task<string> SendCommandAsync(string command, CancellationToken ct = default)
    {
        var response = await _inner.SendCommandAsync(command, ct);
        try
        {
            _observer(command.Trim(), response);
        }
        catch (Exception)
        {
            // an observer must never break the conversation with the board
        }

        return response;
    }

    public Task SendRawAsync(byte[] data, CancellationToken ct = default) => _inner.SendRawAsync(data, ct);

    public Task<byte[]> ReceiveRawAsync(int length, CancellationToken ct = default) => _inner.ReceiveRawAsync(length, ct);

    public void Dispose() => _inner.Dispose();
}
