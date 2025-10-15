# Validation Guide

Relay provides a comprehensive validation system that allows you to validate requests before they reach your handlers. The validation system includes 78+ built-in validation rules covering common validation scenarios, plus the ability to create custom validators.

## Table of Contents

- [Quick Start](#quick-start)
- [Built-in Validation Rules](#built-in-validation-rules)
- [Custom Validators](#custom-validators)
- [Validation Attributes](#validation-attributes)
- [Configuration](#configuration)
- [Error Handling](#error-handling)
- [Performance Considerations](#performance-considerations)

## Quick Start

### 1. Enable Validation

```csharp
// In Program.cs or Startup.cs
services.AddRelayValidation(); // Enables automatic request validation
```

### 2. Create a Request with Validation

```csharp
public record CreateUserCommand(
    [Email] string Email,
    [NotEmpty, MinLength(8)] string Password,
    [Range(18, 120)] int Age,
    [CreditCard] string CreditCardNumber
) : IRequest<User>;
```

### 3. Handle the Request

```csharp
public class UserService
{
    [Handle]
    public async ValueTask<User> CreateUser(CreateUserCommand command, CancellationToken ct)
    {
        // Request is automatically validated before this method is called
        var user = new User
        {
            Email = command.Email,
            Age = command.Age
        };

        // Your business logic here
        return await _userRepository.CreateAsync(user);
    }
}
```

That's it! The validation pipeline automatically validates the request and throws a `ValidationException` if validation fails.

## Built-in Validation Rules

Relay includes 64+ built-in validation rules covering:

### String Validation

#### Email Validation
Validates email address format.
```csharp
[Email] string Email
// Validates: user@example.com, test.email+tag@domain.co.uk
// Rejects: invalid-email, @domain.com, user@
```

#### URL Validation
Validates URL format.
```csharp
[Url] string Website
// Validates: https://example.com, http://localhost:3000/path
// Rejects: not-a-url, ftp://invalid
```

#### IP Address Validation
Validates IPv4 addresses.
```csharp
[IpAddress] string IpAddress
// Validates: 192.168.1.1, 10.0.0.1, 172.16.0.1
// Rejects: 256.1.1.1, invalid-ip
```

#### IPv6 Address Validation
Validates IPv6 addresses.
```csharp
[Ipv6] string Ipv6Address
// Validates: 2001:0db8:85a3:0000:0000:8a2e:0370:7334, ::1, fe80::1%lo0
// Rejects: invalid-ipv6, 2001:gggg:: (invalid characters)
```

#### Phone Number Validation
Validates phone number format.
```csharp
[PhoneNumber] string Phone
// Validates: +1-555-123-4567, (555) 123-4567, 555-123-4567
// Rejects: invalid-phone, 123
```

#### Postal Code Validation
Validates postal/ZIP codes.
```csharp
[PostalCode] string ZipCode
// Validates: 12345, 12345-6789, K1A 0A1 (Canada)
// Rejects: invalid-postal, 123
```

#### Credit Card Validation
Validates credit card numbers using Luhn algorithm.
```csharp
[CreditCard] string CardNumber
// Validates: 4111111111111111 (Visa), 5555555555554444 (MasterCard)
// Rejects: 4111111111111112 (invalid checksum)
```

#### IBAN Validation
Validates International Bank Account Numbers.
```csharp
[Iban] string BankAccount
// Validates: GB29 NWBK 6016 1331 9268 19, DE89 3704 0044 0532 0130 00
// Rejects: INVALIDIBAN, GB29 NWBK 6016 1331 9268 18 (invalid checksum)
```

#### Currency Amount Validation
Validates currency amounts with proper formatting.
```csharp
[CurrencyAmount] string Price
// Validates: 123.45, 1,234.56, €123.45
// Rejects: 123.456 (too many decimals), abc (non-numeric)
```

#### ISBN Validation
Validates ISBN-10 and ISBN-13 with check digits.
```csharp
[Isbn] string BookIsbn
// Validates: 978-0-123456-78-9, 0123456789
// Rejects: 978-0-123456-78-0 (invalid check digit)
```

#### VIN Validation
Validates Vehicle Identification Numbers.
```csharp
[Vin] string VehicleVin
// Validates: 1HGCM82633A123456 (17 characters, valid format)
// Rejects: INVALIDVIN123 (wrong length/format)
```

#### Base64 Validation
Validates Base64 encoded strings.
```csharp
[Base64] string EncodedData
// Validates: U29tZSBEYXRh (valid Base64)
// Rejects: Invalid@Base64! (invalid characters)
```

#### Hex Color Validation
Validates hexadecimal color codes.
```csharp
[HexColor] string Color
// Validates: #FF0000, #3366CC, #ABC
// Rejects: #GGG, invalid-color
```

#### MAC Address Validation
Validates MAC addresses in various formats.
```csharp
[MacAddress] string Mac
// Validates: 00:11:22:33:44:55, 00-11-22-33-44-55, 001122334455
// Rejects: invalid-mac, 00:11:22:33:44
```

#### JSON Validation
Validates JSON format.
```csharp
[Json] string JsonData
// Validates: {"name": "John", "age": 30}
// Rejects: {"name": "John", "age": } (invalid JSON)
```

#### XML Validation
Validates XML format.
```csharp
[Xml] string XmlData
// Validates: <user><name>John</name></user>
// Rejects: <user><name>John<name></user> (invalid XML)
```

#### File Extension Validation
Validates file extensions.
```csharp
[FileExtension(".jpg", ".png", ".gif")] string ImageFile
// Validates: photo.jpg, image.png
// Rejects: document.pdf, script.js
```

#### File Size Validation
Validates file sizes in bytes.
```csharp
[FileSize(1024, 10485760)] long FileSizeBytes  // 1KB to 10MB
// Validates: 2048 (2KB), 5242880 (5MB)
// Rejects: 512 (too small), 20971520 (too large)
```

#### JWT Validation
Validates JWT token structure.
```csharp
[Jwt] string Token
// Validates: eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIn0.TJVA95OrM7E2cBab30RMHrHDcEfxjoYZgeFONFh7HgQ
// Rejects: invalid.jwt.token (malformed)
```

#### Cron Expression Validation
Validates cron expressions.
```csharp
[CronExpression] string Schedule
// Validates: "0 0 * * *", "*/15 * * * *", "0 9-17 * * 1-5"
// Rejects: "invalid cron", "70 * * * *" (invalid minute)
```

#### Semantic Version Validation
Validates semantic version strings.
```csharp
[SemVer] string Version
// Validates: 1.0.0, 2.1.3-alpha, 3.0.0-beta.1+build.123
// Rejects: 1.0, v1.0.0 (invalid format)
```

#### Time Validation
Validates time formats (HH:mm:ss).
```csharp
[Time] string TimeValue
// Validates: 14:30:00, 09:15:30, 23:59:59
// Rejects: 25:00:00 (invalid hour), 14:30 (missing seconds)
```

#### Duration Validation
Validates ISO 8601 duration format.
```csharp
[Duration] string TimeSpan
// Validates: PT1H30M, P1DT2H, P1Y2M3DT4H5M6S
// Rejects: invalid-duration, 1h30m (invalid format)
```

#### MIME Type Validation
Validates MIME type formats.
```csharp
[MimeType] string ContentType
// Validates: text/plain, application/json, image/jpeg
// Rejects: invalid/mime, text (missing subtype)
```

#### Color Validation
Validates color names and hex codes.
```csharp
[Color] string ColorValue
// Validates: red, #FF0000, rgb(255,0,0), hsl(0,100%,50%)
// Rejects: invalid-color, #GGG (invalid hex)
```

#### Password Strength Validation
Validates password complexity requirements.
```csharp
[PasswordStrength] string SecurePassword
// Validates: MySecureP@ss123 (meets complexity requirements)
// Rejects: password (too weak), 123456 (no letters)
```

#### Currency Code Validation
Validates ISO 4217 currency codes.
```csharp
[CurrencyCode] string Currency
// Validates: USD, EUR, GBP, JPY
// Rejects: INVALID, USDD
```

#### Language Code Validation
Validates ISO 639 language codes.
```csharp
[LanguageCode] string Language
// Validates: en, es, eng, spa, zh-CN
// Rejects: invalid-lang, en-US (use CountryCode for regions)
```

#### Country Code Validation
Validates ISO 3166 country codes.
```csharp
[CountryCode] string Country
// Validates: US, GB, USA, GBR, DE
// Rejects: INVALID, USAA
```

#### Time Zone Validation
Validates IANA time zone identifiers.
```csharp
[TimeZone] string TimeZone
// Validates: America/New_York, Europe/London, UTC
// Rejects: Invalid/Timezone, America/Invalid
```

#### Domain Validation
Validates domain names.
```csharp
[Domain] string WebsiteDomain
// Validates: example.com, sub.example.co.uk
// Rejects: invalid..domain, domain (missing TLD)
```

#### Username Validation
Validates username formats.
```csharp
[Username] string Username
// Validates: john_doe, user123, test-user
// Rejects: user@domain, user name (spaces), user! (special chars)
```

#### Coordinate Validation
Validates geographic coordinates.
```csharp
[Coordinate] string GpsCoordinate
// Validates: 40.7128,-74.0060, 51.5074; -0.1278
// Rejects: 91.0000,0.0000 (invalid latitude), 0.0000,181.0000 (invalid longitude)
```

### Numeric Validation

#### Range Validation
Validates numeric values within a range.
```csharp
[Range(1, 100)] int Quantity
[Range(0.0, 5.0)] double Rating
// Validates: 50, 3.5
// Rejects: 150, -5
```

#### Positive/Negative Validation
```csharp
[Positive] int PositiveNumber  // > 0
[Negative] int NegativeNumber  // < 0
[NonZero] int NonZeroNumber    // != 0
// Validates: 5, -3, 1
// Rejects: 0, -5 (for Positive), 3 (for Negative)
```

#### Age Validation
Validates age values with realistic constraints.
```csharp
[Age] int PersonAge
// Validates: 0-150 (reasonable age range)
// Rejects: -5 (negative), 200 (unrealistic)
```

#### Percentage Validation
Validates percentage values between 0 and 100.
```csharp
[Percentage] double DiscountRate
// Validates: 0.0, 50.5, 100.0
// Rejects: -5.0 (negative), 150.0 (over 100%)
```

#### Even/Odd Validation
```csharp
[Even] int EvenNumber
[Odd] int OddNumber
// Validates: 2, 4 (Even), 1, 3 (Odd)
// Rejects: 1 (Even), 2 (Odd)
```

### String Length Validation

#### Length Validation
```csharp
[ExactLength(10)] string ExactLengthString
[MinLength(5)] string MinLengthString
[MaxLength(100)] string MaxLengthString
[Length(5, 50)] string LengthString
// Validates: "Hello" (MinLength), "Hello World" (Length)
// Rejects: "Hi" (MinLength), "This is a very long string..." (MaxLength)
```

### Collection Validation

#### Count Validation
```csharp
[MinCount(1)] List<string> Items
[MaxCount(10)] string[] Tags
[ExactCount(3)] int[] Numbers
// Validates: ["item1", "item2"], [1,2,3] (ExactCount)
// Rejects: [] (MinCount), [1,2,3,4,5,6,7,8,9,10,11] (MaxCount)
```

#### Unique Validation
```csharp
[Unique] List<string> UniqueItems
// Validates: ["a", "b", "c"]
// Rejects: ["a", "b", "a"] (duplicates)
```

### Date/Time Validation

#### Future/Past Validation
```csharp
[Future] DateTime FutureDate
[Past] DateTime PastDate
// Validates: DateTime.Now.AddDays(1) (Future), DateTime.Now.AddDays(-1) (Past)
// Rejects: DateTime.Now.AddDays(-1) (Future), DateTime.Now.AddDays(1) (Past)
```

#### Today Validation
```csharp
[Today] DateTime TodayDate
// Validates: DateTime.Today (exactly today)
// Rejects: DateTime.Today.AddDays(1)
```

### Text Content Validation

#### Case Validation
```csharp
[LowerCase] string LowerCaseText
[UpperCase] string UpperCaseText
// Validates: "hello", "HELLO"
// Rejects: "Hello", "WORLD"
```

#### Character Validation
```csharp
[HasDigits] string HasDigitsText      // Contains at least one digit
[HasLetters] string HasLettersText    // Contains at least one letter
// Validates: "abc123", "123abc"
// Rejects: "abc", "123"
```

#### Pattern Matching
```csharp
[Regex(@"^\d{3}-\d{2}-\d{4}$")] string Ssn  // Social Security Number pattern
[StartsWith("prefix")] string PrefixedText
[EndsWith(".txt")] string FileName
[Contains("substring")] string ContainsText
// Validates: "123-45-6789", "prefix_data", "file.txt", "hello world"
// Rejects: "invalid", "data", "file.pdf", "goodbye"
```

### Comparison Validation

#### Equality Validation
```csharp
[Equal("constant")] string EqualToConstant
[NotEqual("forbidden")] string NotEqualToForbidden
[IsIn("option1", "option2")] string InList
[NotIn("banned1", "banned2")] string NotInList
// Validates: "constant", "allowed", "option1"
// Rejects: "different", "forbidden", "banned1"
```

### Boolean Validation

#### Boolean Validation
```csharp
[IsTrue] bool MustBeTrue
[IsFalse] bool MustBeFalse
// Validates: true (IsTrue), false (IsFalse)
// Rejects: false (IsTrue), true (IsFalse)
```

### Null/Empty Validation

#### Null/Empty Validation
```csharp
[NotNull] object NotNullObject
[NotEmpty] string NotEmptyString
[IsEmpty] string EmptyString
// Validates: new object(), "text", ""
// Rejects: null, "", "text" (for IsEmpty)
```

## Custom Validators

### Using AbstractValidator

For complex validation logic, create a custom validator:

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    protected override void ConfigureRules()
    {
        RuleFor(x => x.Email)
            .NotEmpty("Email is required")
            .EmailAddress("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty("Password is required")
            .MinLength(8, "Password must be at least 8 characters")
            .Matches(@"[A-Z]", "Password must contain uppercase letter")
            .Matches(@"[a-z]", "Password must contain lowercase letter")
            .Matches(@"[0-9]", "Password must contain digit");

        RuleFor(x => x.Age)
            .GreaterThan(17, "Must be 18 or older");

        RuleFor(x => x.CreditCardNumber)
            .CreditCard("Invalid credit card number");

        // Custom validation rule
        RuleFor(x => x.Email)
            .Must(email => !IsDisposableEmail(email), "Disposable email addresses not allowed");
    }

    private bool IsDisposableEmail(string email)
    {
        // Check against disposable email list
        return false; // Implementation here
    }
}
```

### Custom Validation Rules

Create reusable validation rules:

```csharp
public class StrongPasswordValidationRule : IValidationRule<string>
{
    public async ValueTask<IEnumerable<string>> ValidateAsync(
        string request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(request))
            return errors;

        if (request.Length < 8)
            errors.Add("Password must be at least 8 characters");

        if (!Regex.IsMatch(request, @"[A-Z]"))
            errors.Add("Password must contain uppercase letter");

        if (!Regex.IsMatch(request, @"[a-z]"))
            errors.Add("Password must contain lowercase letter");

        if (!Regex.IsMatch(request, @"[0-9]"))
            errors.Add("Password must contain digit");

        if (!Regex.IsMatch(request, @"[^a-zA-Z0-9]"))
            errors.Add("Password must contain special character");

        return errors;
    }
}
```

### Registering Custom Rules

```csharp
// Register custom validation rules
services.AddValidationRulesFromAssembly(typeof(Program).Assembly);

// Or register specific rules
services.AddTransient<IValidationRule<string>, StrongPasswordValidationRule>();
```

## Validation Attributes

Relay supports validation attributes for declarative validation:

```csharp
public record CreateUserCommand : IRequest<User>
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be 8-100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "Password must contain lowercase, uppercase, and digit")]
    public string Password { get; init; }

    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120")]
    public int Age { get; init; }

    [CreditCard(ErrorMessage = "Invalid credit card number")]
    public string? CreditCardNumber { get; init; }

    [Url(ErrorMessage = "Invalid website URL")]
    public string? Website { get; init; }

    [Phone(ErrorMessage = "Invalid phone number")]
    public string? PhoneNumber { get; init; }
}
```

## Configuration

### Validation Options

```csharp
services.AddRelayValidation(options =>
{
    options.ThrowOnValidationError = true;  // Default: true
    options.IncludePropertyNames = true;    // Default: true
    options.MaxValidationErrors = 10;       // Default: 10
    options.ValidationTimeout = TimeSpan.FromSeconds(5); // Default: 5s
});
```

### Conditional Validation

```csharp
public class ConditionalValidator : AbstractValidator<UpdateUserCommand>
{
    protected override void ConfigureRules()
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email)); // Only validate if provided

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinLength(8)
            .When(x => x.ChangePassword); // Only validate when changing password
    }
}
```

### Validation Groups

```csharp
[ValidationGroup("Create")]
[ValidationGroup("Update")]
public record UserData(string Email, string Name);

[ValidationGroup("Update")]
public record UpdateUserCommand : IRequest<User>
{
    public UserData Data { get; init; }
    public bool ChangePassword { get; init; }
    public string? NewPassword { get; init; }
}

// Configure validation groups
services.AddRelayValidation(options =>
{
    options.ValidationGroups = new[] { "Create" }; // Only validate Create group
});
```

## Error Handling

### ValidationException

When validation fails, a `ValidationException` is thrown:

```csharp
try
{
    var result = await _relay.SendAsync(new CreateUserCommand(...));
}
catch (ValidationException ex)
{
    // Handle validation errors
    foreach (var error in ex.Errors)
    {
        _logger.LogWarning("Validation error: {Error}", error);
    }

    // Return validation errors to client
    return BadRequest(new
    {
        Errors = ex.Errors,
        RequestType = ex.RequestType.Name
    });
}
```

### Custom Error Handling

Create custom validation exception handlers:

```csharp
public class ValidationExceptionHandler : IRequestExceptionHandler<CreateUserCommand, User, ValidationException>
{
    public ValueTask<ExceptionHandlerResult<User>> HandleAsync(
        CreateUserCommand request,
        ValidationException exception,
        CancellationToken cancellationToken)
    {
        // Log validation errors
        _logger.LogWarning("Validation failed for {Request}: {Errors}",
            typeof(CreateUserCommand).Name, string.Join(", ", exception.Errors));

        // Return default user or custom response
        var result = new User { Id = 0, Email = "validation-failed@example.com" };
        return new ValueTask<ExceptionHandlerResult<User>>(
            ExceptionHandlerResult<User>.Handle(result));
    }
}
```

## Performance Considerations

### Validation Caching

Validation rules are cached for performance:

```csharp
// Validators are singleton instances
services.AddRelayValidation(); // Rules cached per request type
```

### Async Validation

All validation is async to prevent blocking:

```csharp
// Validation runs asynchronously
var errors = await validator.ValidateAsync(request, cancellationToken);
```

### Short-Circuiting

Validation stops on first error by default:

```csharp
[ValidationRule(ContinueOnError = false)] // Default behavior
public class EmailValidationRule : IValidationRule<string>
{
    // Stops validation on first error
}
```

### Performance Monitoring

Monitor validation performance:

```csharp
services.AddRelayValidation(options =>
{
    options.EnablePerformanceMonitoring = true;
});

// Access metrics
var metrics = _validationMetrics.GetMetrics();
Console.WriteLine($"Validation time: {metrics.AverageValidationTime}ms");
```

## Advanced Usage

### Nested Object Validation

```csharp
public record Address(string Street, string City, string ZipCode);
public record CreateUserCommand(string Email, Address Address) : IRequest<User>;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    protected override void ConfigureRules()
    {
        RuleFor(x => x.Email).EmailAddress();

        // Nested validation
        RuleFor(x => x.Address.Street).NotEmpty();
        RuleFor(x => x.Address.City).NotEmpty();
        RuleFor(x => x.Address.ZipCode).Matches(@"^\d{5}(-\d{4})?$");
    }
}
```

### Collection Validation

```csharp
public record CreateOrderCommand(
    string CustomerEmail,
    List<OrderItem> Items
) : IRequest<Order>;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    protected override void ConfigureRules()
    {
        RuleFor(x => x.CustomerEmail).EmailAddress();

        RuleFor(x => x.Items)
            .NotEmpty("Order must have at least one item")
            .Must(items => items.Sum(i => i.Quantity) > 0, "Total quantity must be positive");

        // Validate each item
        RuleForEach(x => x.Items, item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitPrice).GreaterThan(0);
        });
    }
}
```

### Cross-Property Validation

```csharp
public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
) : IRequest;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    protected override void ConfigureRules()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinLength(8);

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword, "Passwords do not match");

        // Custom cross-property validation
        Custom(async (request, ct) =>
        {
            if (request.CurrentPassword == request.NewPassword)
            {
                return new[] { "New password must be different from current password" };
            }
            return Array.Empty<string>();
        });
    }
}
```

This comprehensive validation system provides enterprise-grade validation capabilities with excellent performance and developer experience.</content>
</xai:function_call">The validation guide has been created successfully! It covers all 64+ validation rules with detailed examples, usage patterns, and advanced features. The documentation includes:

- Complete coverage of all built-in validation rules (Email, URL, Phone, Credit Card, ISBN, VIN, Base64, Hex Color, MAC Address, JSON, XML, JWT, Currency/Language/Country/Time Zone codes, numeric ranges, string lengths, collections, dates, text content, comparisons, booleans, null/empty checks)
- Quick start guide for immediate use
- Custom validator creation with AbstractValidator
- Validation attributes for declarative validation
- Configuration options and conditional validation
- Error handling with ValidationException
- Performance considerations and monitoring
- Advanced usage patterns (nested objects, collections, cross-property validation)

The guide provides both simple examples for beginners and advanced patterns for enterprise scenarios. All validation rules are documented with clear examples of what validates and what gets rejected.

Would you like me to proceed with the next item on the todo list (performance optimization) or would you prefer to review/modify the validation guide first?</content>
</xai:function_call/>
<xai:function_call name="todowrite">
<parameter name="todos">[{"status":"completed","id":"validation_docs"}]