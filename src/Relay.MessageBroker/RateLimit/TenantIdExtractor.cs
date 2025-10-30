namespace Relay.MessageBroker.RateLimit;

/// <summary>
/// Utility class for extracting tenant IDs from message headers or authentication context.
/// </summary>
public static class TenantIdExtractor
{
    /// <summary>
    /// Extracts the tenant ID from message headers.
    /// </summary>
    /// <param name="headers">The message headers.</param>
    /// <returns>The tenant ID, or null if not found.</returns>
    public static string? ExtractFromHeaders(Dictionary<string, object>? headers)
    {
        if (headers == null)
        {
            return null;
        }

        // Try common tenant ID header names
        var tenantHeaderNames = new[]
        {
            "TenantId",
            "X-Tenant-Id",
            "X-Tenant",
            "tenant-id",
            "tenant_id"
        };

        foreach (var headerName in tenantHeaderNames)
        {
            if (headers.TryGetValue(headerName, out var tenantId))
            {
                var tenantIdString = tenantId?.ToString();
                if (!string.IsNullOrWhiteSpace(tenantIdString))
                {
                    return tenantIdString;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the tenant ID from JWT claims in the Authorization header.
    /// </summary>
    /// <param name="headers">The message headers.</param>
    /// <returns>The tenant ID from JWT claims, or null if not found.</returns>
    public static string? ExtractFromJwtClaims(Dictionary<string, object>? headers)
    {
        if (headers == null)
        {
            return null;
        }

        // Try to get token from Authorization header
        if (!headers.TryGetValue("Authorization", out var authHeader))
        {
            return null;
        }

        var authValue = authHeader?.ToString();
        if (string.IsNullOrWhiteSpace(authValue))
        {
            return null;
        }

        // Remove "Bearer " prefix if present
        var token = authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authValue.Substring(7)
            : authValue;

        try
        {
            // Parse JWT token (simple base64 decode of payload)
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }

            var payload = parts[1];
            // Add padding if needed
            var padding = payload.Length % 4;
            if (padding > 0)
            {
                payload += new string('=', 4 - padding);
            }

            var payloadBytes = Convert.FromBase64String(payload);
            var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);

            // Simple JSON parsing to extract tenant_id or tid claim
            // This is a basic implementation; for production, use a proper JSON parser
            if (payloadJson.Contains("\"tenant_id\"") || payloadJson.Contains("\"tid\""))
            {
                var tenantIdMatch = System.Text.RegularExpressions.Regex.Match(
                    payloadJson,
                    @"""(?:tenant_id|tid)""\s*:\s*""([^""]+)""");

                if (tenantIdMatch.Success)
                {
                    return tenantIdMatch.Groups[1].Value;
                }
            }
        }
        catch
        {
            // If JWT parsing fails, return null
            return null;
        }

        return null;
    }

    /// <summary>
    /// Extracts the tenant ID from message headers or authentication context.
    /// Tries multiple extraction methods in order of preference.
    /// </summary>
    /// <param name="headers">The message headers.</param>
    /// <param name="defaultTenantId">The default tenant ID to use if extraction fails.</param>
    /// <returns>The tenant ID.</returns>
    public static string Extract(Dictionary<string, object>? headers, string defaultTenantId = "default")
    {
        // Try to extract from explicit tenant headers first
        var tenantId = ExtractFromHeaders(headers);
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            return tenantId;
        }

        // Try to extract from JWT claims
        tenantId = ExtractFromJwtClaims(headers);
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            return tenantId;
        }

        // Return default tenant ID
        return defaultTenantId;
    }
}
