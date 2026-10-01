using System;

namespace CodeBridge.Core.Exceptions
{
    /// <summary>
    /// Base exception for all CodeBridge errors.
    /// </summary>
    public class CodeBridgeException : Exception
    {
        public CodeBridgeException()
        {
        }

        public CodeBridgeException(string message)
            : base(message)
        {
        }

        public CodeBridgeException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
