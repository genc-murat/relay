namespace Relay.Core.ContractValidation.Models;

/// <summary>
/// Constants for validation error codes.
/// </summary>
public static class ValidationErrorCodes
{
    /// <summary>
    /// Schema not found error code.
    /// </summary>
    public const string SchemaNotFound = "CV001";

    /// <summary>
    /// Schema parsing failed error code.
    /// </summary>
    public const string SchemaParsingFailed = "CV002";

    /// <summary>
    /// Validation timeout error code.
    /// </summary>
    public const string ValidationTimeout = "CV003";

    /// <summary>
    /// Required property missing error code.
    /// </summary>
    public const string RequiredPropertyMissing = "CV004";

    /// <summary>
    /// Type mismatch error code.
    /// </summary>
    public const string TypeMismatch = "CV005";

    /// <summary>
    /// Constraint violation error code.
    /// </summary>
    public const string ConstraintViolation = "CV006";

    /// <summary>
    /// Custom validation failed error code.
    /// </summary>
    public const string CustomValidationFailed = "CV007";

    /// <summary>
    /// Schema cache error code.
    /// </summary>
    public const string SchemaCacheError = "CV008";

    /// <summary>
    /// General validation error code.
    /// </summary>
    public const string GeneralValidationError = "CV999";
}
