using System;

namespace CodeBridge.VisualStudio;

internal static class PackageGuids
{
    public const string PackageGuidString = "6f37e1c9-8792-4b0f-8e2c-7aa6e9d51d2f";
    public const string CommandSetGuidString = "83238aa0-53d0-4dcb-b327-7bfe95b5b681";
    public const string FlowEditorFactoryGuidString = "e6db146a-55bc-4835-b220-4eaf18db351b";

    public static readonly Guid CommandSet = new(CommandSetGuidString);
    public static readonly Guid FlowEditorFactory = new(FlowEditorFactoryGuidString);
}
