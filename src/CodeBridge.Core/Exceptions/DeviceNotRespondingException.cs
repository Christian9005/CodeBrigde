using System;

namespace CodeBridge.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when a board fails to respond within the expected timeout.
    /// </summary>
    public class DeviceNotRespondingException : CodeBridgeException
    {
        public DeviceNotRespondingException()
            : base("The device did not respond within the expected timeout.")
        {
        }

        public DeviceNotRespondingException(string message)
            : base(message)
        {
        }

        public DeviceNotRespondingException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
