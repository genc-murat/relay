using System;

namespace Relay.Core.HandlerVersioning
{
    /// <summary>
    /// Attribute to mark handlers with version information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class HandlerVersionAttribute : Attribute
    {
        /// <summary>
        /// Gets the version of the handler.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets or sets whether this is the default version of the handler.
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandlerVersionAttribute"/> class.
        /// </summary>
        /// <param name="version">The version of the handler.</param>
        public HandlerVersionAttribute(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException("Version cannot be null or empty.", nameof(version));
            }

            Version = version;
        }
    }
}