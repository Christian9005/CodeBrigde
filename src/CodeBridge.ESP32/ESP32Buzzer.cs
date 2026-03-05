using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Abstractions.Actuators;
using CodeBridge.Core.Protocol;

namespace CodeBridge.ESP32;

/// <summary>
/// ESP32 buzzer driver using tone/noTone via LEDC PWM.
/// Compatible with passive buzzers and piezo speakers.
/// </summary>
public class ESP32Buzzer : IBuzzer
{
    private readonly ITransport _transport;
    private bool _disposed;

    public string Name => $"Buzzer (pin {Pin})";
    public bool IsReady => true; // No init needed for tone
    public int Pin { get; }

    public ESP32Buzzer(ITransport transport, int pin)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        Pin = pin;
    }

    public Task InitAsync(CancellationToken ct = default)
    {
        // Tone doesn't require initialization on ESP32
        return Task.CompletedTask;
    }

    public async Task ToneAsync(int frequencyHz, TimeSpan duration, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (frequencyHz <= 0)
            throw new ArgumentOutOfRangeException(nameof(frequencyHz), "Frequency must be positive.");

        var durationMs = (int)duration.TotalMilliseconds;
        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_TONE, Pin, frequencyHz, durationMs), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"Tone failed: {error}");
    }

    public async Task NoToneAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _transport.SendCommandAsync(
            BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NO_TONE, Pin), ct);

        var (success, error) = BridgeProtocol.ParseResponse(response);
        if (!success)
            throw new InvalidOperationException($"NoTone failed: {error}");
    }

    public async Task BeepAsync(CancellationToken ct = default)
    {
        await ToneAsync(1000, TimeSpan.FromMilliseconds(100), ct);
    }

    public async Task PlayMelodyAsync(IEnumerable<(int FrequencyHz, TimeSpan Duration)> notes, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var (freq, duration) in notes)
        {
            ct.ThrowIfCancellationRequested();

            if (freq <= 0)
            {
                // Rest note (silence)
                await NoToneAsync(ct);
                await Task.Delay(duration, ct);
            }
            else
            {
                await ToneAsync(freq, duration, ct);
                await Task.Delay(duration, ct);
            }
        }

        await NoToneAsync(ct);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
