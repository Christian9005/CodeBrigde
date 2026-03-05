namespace CodeBridge.Core.Enums;

/// <summary>
/// GPIO interrupt edge trigger type.
/// </summary>
public enum InterruptEdge
{
    /// <summary>Trigger on rising edge (LOW → HIGH).</summary>
    Rising = 1,

    /// <summary>Trigger on falling edge (HIGH → LOW).</summary>
    Falling = 2,

    /// <summary>Trigger on any change.</summary>
    Change = 3
}
