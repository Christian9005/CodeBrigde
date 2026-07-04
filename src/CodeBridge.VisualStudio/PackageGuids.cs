using System;

namespace CodeBridge.VisualStudio;

internal static class PackageGuids
{
    public const string PackageGuidString = "6f37e1c9-8792-4b0f-8e2c-7aa6e9d51d2f";
    public const string CommandSetGuidString = "83238aa0-53d0-4dcb-b327-7bfe95b5b681";

    public static readonly Guid CommandSet = new(CommandSetGuidString);
}
