namespace CodeBridge.Flow.Streaming;

/// <summary>
/// Designer/runtime contract for one high-rate board acquisition channel.
/// </summary>
public sealed record SamplingPlan(
    int Pin,
    SamplingMode Mode,
    int SampleRateHz,
    int BufferCapacity,
    BackpressurePolicy Backpressure,
    bool Analog = false,
    int BatchSize = 32)
{
    public IReadOnlyList<string> Validate(BoardProfile board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var errors = new List<string>();
        var pin = board.FindPin(Pin);
        if (pin is null || pin.IsReserved)
        {
            errors.Add($"GPIO {Pin} is not available on {board.DisplayName}.");
            return errors;
        }

        if (Analog && !pin.SupportsAnalogRead)
            errors.Add($"GPIO {Pin} does not support analog sampling.");

        if (!Analog && !pin.SupportsDigitalRead)
            errors.Add($"GPIO {Pin} does not support digital sampling.");

        if (Mode == SamplingMode.Interrupt && (!board.Runtime.SupportsInterrupts || !pin.SupportsInterrupts))
            errors.Add($"GPIO {Pin} does not support interrupt sampling.");

        if (Mode == SamplingMode.HardwareTimer && !board.Runtime.SupportsHardwareTimers)
            errors.Add($"{board.DisplayName} does not expose hardware timer sampling.");

        if (SampleRateHz <= 0)
        {
            errors.Add("Sample rate must be greater than zero.");
        }
        else
        {
            var maxRate = Analog
                ? Math.Min(pin.MaxAnalogSampleRateHz, board.Runtime.MaxAnalogSampleRateHz)
                : Math.Min(pin.MaxDigitalSampleRateHz, board.Runtime.MaxDigitalSampleRateHz);

            if (SampleRateHz > maxRate)
                errors.Add($"Sample rate {SampleRateHz} Hz exceeds GPIO {Pin} limit of {maxRate} Hz.");
        }

        if (BufferCapacity <= 0)
            errors.Add("Buffer capacity must be greater than zero.");
        else if (BufferCapacity > board.Runtime.MaxBufferCapacity)
            errors.Add($"Buffer capacity {BufferCapacity} exceeds board limit of {board.Runtime.MaxBufferCapacity} samples.");

        if (BatchSize <= 0)
            errors.Add("Batch size must be greater than zero.");
        else if (BatchSize > BufferCapacity)
            errors.Add("Batch size cannot be larger than buffer capacity.");

        return errors;
    }
}
