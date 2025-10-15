using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is valid XML.
    /// </summary>
    public class XmlValidationRule : IValidationRule<string>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    ValidationType = ValidationType.None
                };

                using var stringReader = new System.IO.StringReader(request);
                using var xmlReader = XmlReader.Create(stringReader, settings);

                while (xmlReader.Read())
                {
                    // Just read through the document to validate structure
                }

                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }
            catch (XmlException)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid XML format." });
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Invalid XML format." });
            }
        }
    }
}