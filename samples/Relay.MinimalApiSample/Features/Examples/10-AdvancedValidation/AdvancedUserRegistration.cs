using Relay.Core.Contracts.Requests;

namespace Relay.MinimalApiSample.Features.Examples.AdvancedValidation;

/// <summary>
/// Advanced user registration request demonstrating comprehensive validation
/// with 40+ validation rules and pipeline integration.
/// </summary>
public record AdvancedUserRegistrationRequest(
    string Username,
    string Email,
    string Password,
    int Age,
    string? PhoneNumber,
    string? Website,
    string? CreditCardNumber,
    string? PostalCode,
    string? Location,
    long? ProfilePictureSize,
    string? ProfilePictureExtension,
    double? DiscountRate,
    string? MonthlyBudget,
    string? NotificationSchedule,
    string? AppVersion,
    string? PreferredTime,
    string? SessionTimeout,
    string? ContentType,
    string? ThemeColor,
    string? CompanyDomain,
    string? ServerIp,
    string? BankAccount,
    string? VehicleVin,
    string? BrandColor,
    string? DeviceMac,
    string? UserPreferences,
    string? ConfigXml,
    string? AuthToken,
    string? PreferredCurrency,
    string? Language,
    string? Country,
    string? TimeZone,
    string? ProfileImageData,
    string? ReferralCode,
    string? BirthDate,
    bool AcceptTerms
) : IRequest<AdvancedUserRegistrationResponse>;