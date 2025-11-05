using System;

namespace Relay.Core.Testing
{
    public class VerificationException : Exception
    {
        public VerificationException(string message) : base(message) { }
    }
}