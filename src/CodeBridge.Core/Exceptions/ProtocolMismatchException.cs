using System;

namespace CodeBridge.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when the board's firmware version is incompatible with the SDK version.
    /// </summary>
    public class ProtocolMismatchException : CodeBridgeException
    {
        public string ExpectedVersion { get; }
        public string ActualVersion { get; }

        public ProtocolMismatchException(string expectedVersion, string actualVersion)
            : base($"Protocol version mismatch. Expected: {expectedVersion}, Actual: {actualVersion}. Please update the board firmware.")
        {
            ExpectedVersion = expectedVersion;
            ActualVersion = actualVersion;
        }

        public ProtocolMismatchException(string message)
            : base(message)
        {
            ExpectedVersion = string.Empty;
            ActualVersion = string.Empty;
        }

        public ProtocolMismatchException(string message, Exception inner)
            : base(message, inner)
        {
            ExpectedVersion = string.Empty;
            ActualVersion = string.Empty;
        }
    }
}
