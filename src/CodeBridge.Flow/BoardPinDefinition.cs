namespace CodeBridge.Flow;

/// <summary>
/// Describes a physical board pin and the operations that are sensible for it.
/// </summary>
public sealed record BoardPinDefinition(
    int Number,
    string Name,
    string Description,
    PinCapability Capabilities,
    string? DisplayLabel = null,
    int MaxDigitalSampleRateHz = 1000,
    int MaxAnalogSampleRateHz = 500)
{
    public bool SupportsDigitalRead => HasCapability(PinCapability.DigitalRead);
    public bool SupportsDigitalWrite => HasCapability(PinCapability.DigitalWrite);
    public bool SupportsAnalogRead => HasCapability(PinCapability.AnalogRead);
    public bool SupportsPwm => HasCapability(PinCapability.Pwm);
    public bool SupportsServo => HasCapability(PinCapability.ServoRecommended);
    public bool SupportsInterrupts => HasCapability(PinCapability.Interrupt);
    public bool IsInputOnly => HasCapability(PinCapability.InputOnly);
    public bool IsBootStrap => HasCapability(PinCapability.BootStrap);
    public bool IsReserved => HasCapability(PinCapability.Reserved);

    public string Label
    {
        get
        {
            var label = DisplayLabel ?? $"GPIO {Number} - {Name}";

            if (IsReserved)
                return $"{label} (avoid)";

            if (IsInputOnly)
                return $"{label} (input only)";

            if (IsBootStrap)
                return $"{label} (boot strap)";

            return label;
        }
    }

    public bool HasCapability(PinCapability capability) =>
        (Capabilities & capability) == capability;

    public FlowPropertyOption ToOption() => new(Label, Number, Description);
}
