# Relay.CLI.Tests

Comprehensive test suite for Relay CLI Template System using **xUnit**.

## Test Framework

This project uses **xUnit** as the test framework because:
- ✅ Most popular .NET test framework
- ✅ Modern, extensible architecture
- ✅ Excellent parallelization support
- ✅ Clean, attribute-based API
- ✅ Great Visual Studio & Rider integration

## Test Categories

### Unit Tests

#### TemplateEngine Tests
- **TemplateGeneratorTests** - Tests for project generation engine (12 tests)
- **TemplateValidatorTests** - Tests for template validation (15 tests)
- **TemplatePublisherTests** - Tests for template packaging and publishing (11 tests)
- **DataStructureTests** - Tests for data models and structures (25 tests)

### Integration Tests
- **TemplateEndToEndTests** - End-to-end workflow tests (11 tests)

**Total: 74 tests**

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Category
```bash
# Unit tests only
dotnet test --filter Category!=Integration

# Integration tests only
dotnet test --filter "Category=Integration"
```

### Run Specific Test Class
```bash
dotnet test --filter TemplateGeneratorTests
```

### Run Specific Test Method
```bash
dotnet test --filter "FullyQualifiedName~GenerateAsync_WithValidTemplate"
```

### Run with Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Test Structure

```
Relay.CLI.Tests/
├── Commands/                  # Command tests (future)
├── TemplateEngine/            # Template engine unit tests
│   ├── TemplateGeneratorTests.cs
│   ├── TemplateValidatorTests.cs
│   ├── TemplatePublisherTests.cs
│   └── DataStructureTests.cs
├── Integration/               # Integration tests
│   └── TemplateEndToEndTests.cs
└── TestData/                  # Test fixtures
    ├── ValidTemplate/
    │   ├── .template.config/
    │   │   └── template.json
    │   └── content/
    │       ├── README.md
    │       ├── TestProject.csproj
    │       └── Program.cs
    └── InvalidTemplate/
```

## Test Coverage

Current test coverage goals:
- Unit Tests: 85%+
- Integration Tests: Key workflows
- Total Coverage: 80%+

### Coverage by Component

| Component | Coverage | Tests |
|-----------|----------|-------|
| TemplateGenerator | 85%+ | 12 |
| TemplateValidator | 90%+ | 15 |
| TemplatePublisher | 85%+ | 11 |
| Data Structures | 100% | 25 |
| Integration | Key flows | 3 |

## Test Data

### ValidTemplate
A complete, valid template used for positive test scenarios.

### InvalidTemplate
Various invalid template configurations for negative testing.

## Writing New Tests

### Unit Test Example
```csharp
[Test]
public async Task GenerateAsync_WithValidTemplate_CreatesProjectSuccessfully()
{
    // Arrange
    var projectName = "TestProject";
    var options = new GenerationOptions();

    // Act
    var result = await _generator.GenerateAsync("relay-webapi", projectName, _testOutputPath, options);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
}
```

### Integration Test Example
```csharp
[Test]
[Category("Integration")]
public async Task CompleteWorkflow_CreateValidatePackPublish_Succeeds()
{
    // Test complete workflow
    var templatePath = CreateTestTemplate();
    var validationResult = await _validator.ValidateAsync(templatePath);
    var packResult = await _publisher.PackTemplateAsync(templatePath, _outputPath);
    
    // Assertions
    validationResult.IsValid.Should().BeTrue();
    packResult.Success.Should().BeTrue();
}
```

## Test Conventions

1. **Naming**: `MethodName_Scenario_ExpectedResult`
2. **AAA Pattern**: Arrange, Act, Assert
3. **FluentAssertions**: Use for readable assertions
4. **Cleanup**: Always cleanup in TearDown
5. **Isolation**: Tests should not depend on each other

## Continuous Integration

Tests are run automatically on:
- Pull Requests
- Main branch commits
- Release builds

### CI Pipeline
```yaml
- dotnet restore
- dotnet build
- dotnet test --no-build --verbosity normal
- dotnet test --collect:"XPlat Code Coverage"
```

## Troubleshooting

### Tests Failing Locally
1. Clean solution: `dotnet clean`
2. Restore packages: `dotnet restore`
3. Rebuild: `dotnet build`
4. Run tests: `dotnet test`

### Permission Issues
- Ensure test output directories are writable
- Check temp directory permissions

### Flaky Tests
- Check for race conditions
- Verify cleanup in TearDown
- Look for file locking issues

## Contributing

When adding new features:
1. Write tests first (TDD)
2. Ensure >80% coverage
3. Add integration tests for new workflows
4. Update this README

## License

MIT License - See LICENSE file in root directory
