namespace CodeBridge.Core.Abstractions;

/// <summary>
/// Generates native C/C++ code from CodeBridge logic definitions.
/// This enables the "hybrid" mode: generate standalone firmware from C# definitions.
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// Generates C/C++ source code from a board configuration and logic.
    /// </summary>
    Task<GeneratedCode> GenerateAsync(CodeGenerationRequest request, CancellationToken ct = default);
}

/// <summary>
/// Request to generate native firmware code.
/// </summary>
public class CodeGenerationRequest
{
    /// <summary>
    /// Target platform (e.g., "esp32", "arduino-uno", "stm32f4").
    /// </summary>
    public required string TargetPlatform { get; init; }

    /// <summary>
    /// Pin configurations defined by the user.
    /// </summary>
    public List<PinConfiguration> Pins { get; init; } = [];

    /// <summary>
    /// Logic blocks (future: visual designer output).
    /// </summary>
    public List<LogicBlock> LogicBlocks { get; init; } = [];
}

/// <summary>
/// A pin configuration for code generation.
/// </summary>
public record PinConfiguration(int Pin, Enums.PinMode Mode, string Label);

/// <summary>
/// A logic block representing a piece of behavior (future: from visual designer).
/// </summary>
public class LogicBlock
{
    public required string Id { get; init; }
    public required string Type { get; init; } // "blink", "read-sensor", "if-then", etc.
    public Dictionary<string, object> Parameters { get; init; } = [];
}

/// <summary>
/// The result of code generation.
/// </summary>
public class GeneratedCode
{
    /// <summary>
    /// The generated C/C++ source files.
    /// </summary>
    public Dictionary<string, string> Files { get; init; } = [];

    /// <summary>
    /// Build instructions or platformio.ini content.
    /// </summary>
    public string? BuildConfiguration { get; init; }
}
