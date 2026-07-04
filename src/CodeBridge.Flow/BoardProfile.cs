namespace CodeBridge.Flow;

/// <summary>
/// Groups the known pin map for a board family.
/// </summary>
public sealed record BoardProfile(
    string Id,
    string DisplayName,
    IReadOnlyList<BoardPinDefinition> Pins)
{
    public BoardRuntimeCapabilities Runtime { get; init; } = BoardRuntimeCapabilities.Default;

    public BoardPinDefinition? FindPin(int number) =>
        Pins.FirstOrDefault(pin => pin.Number == number);

    public IReadOnlyList<FlowPropertyOption> PinOptions =>
        PinOptionsFor(PinCapability.None);

    public IReadOnlyList<FlowPropertyOption> DigitalReadPinOptions =>
        PinOptionsFor(PinCapability.DigitalRead);

    public IReadOnlyList<FlowPropertyOption> DigitalWritePinOptions =>
        PinOptionsFor(PinCapability.DigitalWrite);

    public IReadOnlyList<FlowPropertyOption> AnalogReadPinOptions =>
        PinOptionsFor(PinCapability.AnalogRead);

    public IReadOnlyList<FlowPropertyOption> PwmPinOptions =>
        PinOptionsFor(PinCapability.Pwm | PinCapability.DigitalWrite);

    public IReadOnlyList<FlowPropertyOption> ServoPinOptions =>
        PinOptionsFor(
            PinCapability.ServoRecommended | PinCapability.Pwm | PinCapability.DigitalWrite,
            PinCapability.Reserved | PinCapability.BootStrap | PinCapability.InputOnly);

    public IReadOnlyList<FlowPropertyOption> InterruptPinOptions =>
        PinOptionsFor(PinCapability.Interrupt);

    public IReadOnlyList<FlowPropertyOption> SamplePinOptions =>
        ToOptions(pin => (pin.SupportsDigitalRead || pin.SupportsAnalogRead) && !pin.IsReserved);

    public IReadOnlyList<FlowPropertyOption> PinOptionsFor(
        PinCapability required,
        PinCapability disallowed = PinCapability.Reserved) =>
        ToOptions(pin =>
            pin.HasCapability(required) &&
            (pin.Capabilities & disallowed) == PinCapability.None);

    private IReadOnlyList<FlowPropertyOption> ToOptions(Func<BoardPinDefinition, bool> predicate) =>
        Pins.Where(predicate)
            .OrderBy(pin => pin.Number)
            .Select(pin => pin.ToOption())
            .ToArray();
}
