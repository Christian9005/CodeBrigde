using CodeBridge.Flow;

namespace CodeBridge.Flow.Execution;

internal static class FlowTypeCompatibility
{
    public static bool AreCompatible(FlowValueKind output, FlowValueKind input)
    {
        if (output == FlowValueKind.Any || input == FlowValueKind.Any)
            return true;

        if (output == input)
            return true;

        return output == FlowValueKind.Integer && input == FlowValueKind.Number;
    }
}
