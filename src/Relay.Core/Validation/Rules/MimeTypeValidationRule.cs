using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid MIME type.
    /// Supports standard MIME type format: type/subtype[+suffix]
    /// </summary>
    public class MimeTypeValidationRule : IValidationRule<string>
    {
        private static readonly Regex MimeTypeRegex = new Regex(
            @"^(application|audio|font|image|message|model|multipart|text|video)\/[a-zA-Z0-9][a-zA-Z0-9!#$&^_.-]{0,126}(?:\+[a-zA-Z0-9][a-zA-Z0-9!#$&^_.-]{0,126})?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly HashSet<string> CommonMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Text
            "text/plain", "text/html", "text/css", "text/javascript", "text/csv", "text/xml",

            // Images
            "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml", "image/bmp", "image/tiff",

            // Audio
            "audio/mpeg", "audio/wav", "audio/ogg", "audio/mp4", "audio/webm",

            // Video
            "video/mp4", "video/webm", "video/ogg", "video/avi", "video/quicktime",

            // Application
            "application/json", "application/xml", "application/pdf", "application/zip",
            "application/octet-stream", "application/msword", "application/vnd.ms-excel",
            "application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

            // Font
            "font/ttf", "font/woff", "font/woff2"
        };

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var mimeType = request.Trim();

            // Check basic format
            if (!MimeTypeRegex.IsMatch(mimeType))
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid MIME type format." });
            }

            // Additional validation for common types
            if (!CommonMimeTypes.Contains(mimeType))
            {
                // Allow custom types but warn about uncommon ones
                var parts = mimeType.Split('/');
                if (parts.Length != 2)
                {
                    return new ValueTask<IEnumerable<string>>(new[] { "Invalid MIME type format." });
                }

                var mainType = parts[0];
                var subType = parts[1].Split('+')[0]; // Remove suffix

                // Basic validation of type and subtype
                if (string.IsNullOrEmpty(mainType) || string.IsNullOrEmpty(subType) ||
                    mainType.Length > 127 || subType.Length > 127)
                {
                    return new ValueTask<IEnumerable<string>>(new[] { "MIME type components too long or empty." });
                }
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}