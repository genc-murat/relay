using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.ContractValidation;

/// <summary>
/// Interface for contract validators.
/// </summary>
public interface IContractValidator
{
    /// <summary>
    /// Validates a request contract against its schema.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="schema">The JSON schema to validate against.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of validation errors, empty if valid.</returns>
    ValueTask<IEnumerable<string>> ValidateRequestAsync(object request, JsonSchemaContract schema, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a response contract against its schema.
    /// </summary>
    /// <param name="response">The response to validate.</param>
    /// <param name="schema">The JSON schema to validate against.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of validation errors, empty if valid.</returns>
    ValueTask<IEnumerable<string>> ValidateResponseAsync(object response, JsonSchemaContract schema, CancellationToken cancellationToken = default);
}