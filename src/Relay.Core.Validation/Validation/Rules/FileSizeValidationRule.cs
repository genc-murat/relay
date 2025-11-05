using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a long integer represents a valid file size.
    /// Supports size limits in bytes with human-readable error messages.
    /// </summary>
    public class FileSizeValidationRule : IValidationRule<long>
    {
        private readonly long _maxSizeBytes;
        private readonly long _minSizeBytes;

        /// <summary>
        /// Initializes a new instance of the FileSizeValidationRule class.
        /// </summary>
        /// <param name="maxSizeBytes">Maximum allowed file size in bytes.</param>
        /// <param name="minSizeBytes">Minimum allowed file size in bytes (default: 0).</param>
        public FileSizeValidationRule(long maxSizeBytes, long minSizeBytes = 0)
        {
            _maxSizeBytes = maxSizeBytes;
            _minSizeBytes = minSizeBytes;
        }

        /// <summary>
        /// Creates a FileSizeValidationRule with size limit in kilobytes.
        /// </summary>
        public static FileSizeValidationRule MaxKilobytes(long kilobytes, long minSizeBytes = 0)
        {
            return new FileSizeValidationRule(kilobytes * 1024, minSizeBytes);
        }

        /// <summary>
        /// Creates a FileSizeValidationRule with size limit in megabytes.
        /// </summary>
        public static FileSizeValidationRule MaxMegabytes(long megabytes, long minSizeBytes = 0)
        {
            return new FileSizeValidationRule(megabytes * 1024 * 1024, minSizeBytes);
        }

        /// <summary>
        /// Creates a FileSizeValidationRule with size limit in gigabytes.
        /// </summary>
        public static FileSizeValidationRule MaxGigabytes(long gigabytes, long minSizeBytes = 0)
        {
            return new FileSizeValidationRule(gigabytes * 1024 * 1024 * 1024, minSizeBytes);
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(long request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var errors = new List<string>();

            if (request < 0)
            {
                errors.Add("File size cannot be negative.");
                return new ValueTask<IEnumerable<string>>(errors);
            }

            if (request < _minSizeBytes)
            {
                errors.Add($"File size must be at least {FormatSize(_minSizeBytes)}.");
            }

            if (request > _maxSizeBytes)
            {
                errors.Add($"File size cannot exceed {FormatSize(_maxSizeBytes)}.");
            }

            return new ValueTask<IEnumerable<string>>(errors);
        }

        private static string FormatSize(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes >= GB)
            {
                return $"{bytes / (double)GB:F1} GB";
            }
            else if (bytes >= MB)
            {
                return $"{bytes / (double)MB:F1} MB";
            }
            else if (bytes >= KB)
            {
                return $"{bytes / (double)KB:F1} KB";
            }
            else
            {
                return $"{bytes} bytes";
            }
        }
    }
}