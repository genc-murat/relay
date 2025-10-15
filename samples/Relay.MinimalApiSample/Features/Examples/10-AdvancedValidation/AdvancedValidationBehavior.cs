using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;

namespace Relay.MinimalApiSample.Features.Examples.AdvancedValidation;

/// <summary>
/// Pipeline behavior that tracks advanced validation performance metrics.
/// Demonstrates how validation integrates with pipeline behaviors.
/// </summary>
public class AdvancedValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<AdvancedValidationBehavior<TRequest, TResponse>> _logger;

    public AdvancedValidationBehavior(ILogger<AdvancedValidationBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var requestType = typeof(TRequest).Name;

        _logger.LogInformation("Starting advanced validation pipeline for {RequestType}", requestType);

        // Execute the request (validation happens here)
        var response = await next();

        var totalTime = DateTime.UtcNow - startTime;

        // Log comprehensive validation metrics
        _logger.LogInformation(
            "Advanced validation pipeline completed for {RequestType} in {TotalTime}ms. " +
            "Comprehensive validation passed.",
            requestType,
            totalTime.TotalMilliseconds);

        // Additional validation-specific logging for AdvancedUserRegistrationRequest
        if (request is AdvancedUserRegistrationRequest advancedRequest)
        {
            _logger.LogInformation(
                "Advanced validation summary for user registration: " +
                "Username={Username}, Email={Email}, Age={Age}, " +
                "ValidationFields={FieldCount}, ProcessingTime={Time}ms",
                advancedRequest.Username,
                advancedRequest.Email,
                advancedRequest.Age,
                CountValidationFields(advancedRequest),
                totalTime.TotalMilliseconds);
        }

        return response;
    }

    private static int CountValidationFields(AdvancedUserRegistrationRequest request)
    {
        // Count how many validation fields were provided
        var count = 0;
        if (!string.IsNullOrEmpty(request.PhoneNumber)) count++;
        if (!string.IsNullOrEmpty(request.Website)) count++;
        if (!string.IsNullOrEmpty(request.CreditCardNumber)) count++;
        if (!string.IsNullOrEmpty(request.PostalCode)) count++;
        if (!string.IsNullOrEmpty(request.Location)) count++;
        if (request.ProfilePictureSize.HasValue) count++;
        if (!string.IsNullOrEmpty(request.ProfilePictureExtension)) count++;
        if (request.DiscountRate.HasValue) count++;
        if (!string.IsNullOrEmpty(request.MonthlyBudget)) count++;
        if (!string.IsNullOrEmpty(request.NotificationSchedule)) count++;
        if (!string.IsNullOrEmpty(request.AppVersion)) count++;
        if (!string.IsNullOrEmpty(request.PreferredTime)) count++;
        if (!string.IsNullOrEmpty(request.SessionTimeout)) count++;
        if (!string.IsNullOrEmpty(request.ContentType)) count++;
        if (!string.IsNullOrEmpty(request.ThemeColor)) count++;
        if (!string.IsNullOrEmpty(request.CompanyDomain)) count++;
        if (!string.IsNullOrEmpty(request.ServerIp)) count++;
        if (!string.IsNullOrEmpty(request.BankAccount)) count++;
        if (!string.IsNullOrEmpty(request.VehicleVin)) count++;
        if (!string.IsNullOrEmpty(request.BrandColor)) count++;
        if (!string.IsNullOrEmpty(request.DeviceMac)) count++;
        if (!string.IsNullOrEmpty(request.UserPreferences)) count++;
        if (!string.IsNullOrEmpty(request.ConfigXml)) count++;
        if (!string.IsNullOrEmpty(request.AuthToken)) count++;
        if (!string.IsNullOrEmpty(request.PreferredCurrency)) count++;
        if (!string.IsNullOrEmpty(request.Language)) count++;
        if (!string.IsNullOrEmpty(request.Country)) count++;
        if (!string.IsNullOrEmpty(request.TimeZone)) count++;
        if (!string.IsNullOrEmpty(request.ProfileImageData)) count++;
        if (!string.IsNullOrEmpty(request.ReferralCode)) count++;
        if (!string.IsNullOrEmpty(request.BirthDate)) count++;
        return count + 6; // Add the 6 required fields (Username, Email, Password, Age, AcceptTerms)
    }
}