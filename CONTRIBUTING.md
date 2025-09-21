# Contributing to Relay

Thank you for your interest in contributing to Relay! This document provides guidelines and information for contributors.

## 🤝 How to Contribute

### Reporting Issues

Before creating an issue, please:

1. **Search existing issues** to avoid duplicates
2. **Use the issue templates** when available
3. **Provide detailed information** including:
   - Relay version
   - .NET version and target framework
   - Complete error messages and stack traces
   - Minimal reproduction code
   - Expected vs actual behavior

### Suggesting Features

We welcome feature suggestions! Please:

1. **Check existing feature requests** first
2. **Describe the use case** and problem you're trying to solve
3. **Provide examples** of how the feature would be used
4. **Consider the performance impact** - Relay prioritizes performance
5. **Be open to discussion** about implementation approaches

### Contributing Code

#### Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:

   ```bash
   git clone https://github.com/your-username/relay.git
   cd relay
   ```

3. **Create a feature branch**:

   ```bash
   git checkout -b feature/your-feature-name
   ```

#### Development Setup

**Prerequisites:**

- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider
- Git

**Build and Test:**

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run specific test project
dotnet test tests/Relay.Core.Tests
dotnet test tests/Relay.SourceGenerator.Tests
```

#### Project Structure

```
Relay/
├── src/
│   ├── Relay/                      # Main package (meta-package)
│   ├── Relay.Core/                 # Core runtime components
│   └── Relay.SourceGenerator/      # Roslyn source generator
├── tests/
│   ├── Relay.Core.Tests/          # Unit tests for core
│   ├── Relay.SourceGenerator.Tests/ # Source generator tests
│   └── Relay.Packaging.Tests/     # Package validation tests
├── docs/                           # Documentation
├── build/                          # Build scripts
└── benchmarks/                     # Performance benchmarks
```

#### Coding Standards

**General Guidelines:**

- Follow existing code style and conventions
- Use meaningful names for variables, methods, and classes
- Write self-documenting code with clear intent
- Add XML documentation for public APIs
- Keep methods focused and single-purpose

**Performance Guidelines:**

- Prefer `ValueTask<T>` over `Task<T>` when appropriate
- Minimize allocations in hot paths
- Use `Span<T>` and `Memory<T>` for efficient data handling
- Consider object pooling for frequently created objects
- Profile performance-critical changes

**C# Style:**

```csharp
// ✅ Good
public async ValueTask<User> GetUserAsync(int userId, CancellationToken cancellationToken)
{
    if (userId <= 0)
        throw new ArgumentException("User ID must be positive", nameof(userId));
    
    return await _repository.GetByIdAsync(userId, cancellationToken);
}

// ❌ Avoid
public async Task<User> getUserAsync(int userId, CancellationToken cancellationToken)
{
    return await _repository.GetByIdAsync(userId, cancellationToken);
}
```

**Source Generator Guidelines:**

- Generate readable, well-formatted code
- Include helpful comments in generated code
- Provide clear diagnostic messages for errors
- Test generated code thoroughly
- Consider incremental generation for performance

#### Testing Requirements

**All contributions must include appropriate tests:**

1. **Unit Tests** for new functionality
2. **Integration Tests** for end-to-end scenarios
3. **Performance Tests** for performance-critical changes
4. **Source Generator Tests** for generator changes

**Test Examples:**

```csharp
// Unit test
[Test]
public async Task GetUser_WithValidId_ReturnsUser()
{
    // Arrange
    var handler = new UserService(mockRepository.Object);
    var relay = RelayTestHarness.CreateTestRelay(handler);
    
    // Act
    var result = await relay.SendAsync(new GetUserQuery(123));
    
    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Id, Is.EqualTo(123));
}

// Performance test
[Test]
public async Task SendRequest_Performance_ShouldBeFast()
{
    var relay = CreateOptimizedRelay();
    var query = new GetUserQuery(123);
    
    var stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < 10_000; i++)
    {
        await relay.SendAsync(query);
    }
    stopwatch.Stop();
    
    var avgTime = stopwatch.Elapsed.TotalMicroseconds / 10_000;
    Assert.That(avgTime, Is.LessThan(1.0)); // Less than 1 microsecond average
}
```

#### Documentation

**Update documentation for:**

- New public APIs
- Breaking changes
- New features
- Performance improvements
- Migration guides

**Documentation locations:**

- XML comments for API documentation
- `docs/` folder for guides and examples
- `README.md` for overview and quick start
- `CHANGELOG.md` for version changes

#### Performance Benchmarks

**For performance-related changes:**

1. **Create benchmarks** using BenchmarkDotNet:

   ```csharp
   [MemoryDiagnoser]
   [SimpleJob(RuntimeMoniker.Net80)]
   public class MyBenchmark
   {
       [Benchmark]
       public async ValueTask<User> SendRequest()
       {
           return await _relay.SendAsync(new GetUserQuery(123));
       }
   }
   ```

2. **Run before and after** your changes
3. **Include results** in your pull request
4. **Explain any regressions** and justify trade-offs

#### Pull Request Process

1. **Ensure all tests pass** locally
2. **Update documentation** as needed
3. **Add changelog entry** if applicable
4. **Create pull request** with:
   - Clear title and description
   - Reference to related issues
   - Summary of changes
   - Performance impact (if applicable)
   - Breaking changes (if any)

**Pull Request Template:**

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update
- [ ] Performance improvement

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Performance tests added/updated
- [ ] All tests pass

## Performance Impact
Describe any performance implications

## Breaking Changes
List any breaking changes and migration path

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] Changelog updated (if applicable)
```

## 🏗️ Development Guidelines

### Source Generator Development

**Key Principles:**

- Generate efficient, readable code
- Provide clear error messages
- Support incremental compilation
- Minimize generator execution time

**Testing Source Generators:**

```csharp
[Test]
public void Generator_WithValidHandler_GeneratesCorrectCode()
{
    var source = @"
        public class UserService
        {
            [Handle]
            public ValueTask<User> GetUser(GetUserQuery query, CancellationToken cancellationToken)
            {
                return ValueTask.FromResult(new User());
            }
        }";

    var result = GeneratorTestHelper.RunGenerator(source);
    
    Assert.That(result.GeneratedSources, Has.Count.EqualTo(1));
    Assert.That(result.GeneratedSources[0].SourceText.ToString(), 
        Contains.Substring("GetUser"));
}
```

### Core Library Development

**Performance Considerations:**

- Profile hot paths regularly
- Use allocation-free patterns where possible
- Prefer stack allocation over heap allocation
- Consider object pooling for frequently used objects

**Error Handling:**

- Provide clear, actionable error messages
- Include context information in exceptions
- Use appropriate exception types
- Don't swallow exceptions without good reason

### Documentation Standards

**API Documentation:**

```csharp
/// <summary>
/// Sends a request and returns the response.
/// </summary>
/// <typeparam name="TResponse">The type of response expected.</typeparam>
/// <param name="request">The request to send.</param>
/// <param name="cancellationToken">Cancellation token for the operation.</param>
/// <returns>A ValueTask containing the response.</returns>
/// <exception cref="HandlerNotFoundException">Thrown when no handler is found for the request type.</exception>
/// <exception cref="HandlerExecutionException">Thrown when handler execution fails.</exception>
public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
```

**Guide Documentation:**

- Use clear, concise language
- Include complete, runnable examples
- Explain the "why" not just the "how"
- Provide troubleshooting information
- Keep examples up-to-date

## 🚀 Release Process

### Versioning

We follow [Semantic Versioning](https://semver.org/):

- **Major** (X.0.0): Breaking changes
- **Minor** (0.X.0): New features, backward compatible
- **Patch** (0.0.X): Bug fixes, backward compatible

### Changelog

Update `CHANGELOG.md` with:

- New features
- Bug fixes
- Breaking changes
- Performance improvements
- Deprecations

### Release Checklist

- [ ] All tests pass
- [ ] Documentation updated
- [ ] Changelog updated
- [ ] Version numbers updated
- [ ] Performance benchmarks run
- [ ] NuGet packages validated

## 🎯 Areas for Contribution

### High Priority

- Performance optimizations
- Bug fixes
- Documentation improvements
- Test coverage improvements

### Medium Priority

- New features (discuss first)
- Additional examples
- Tooling improvements
- Integration guides

### Low Priority

- Code style improvements
- Refactoring (without functional changes)
- Additional benchmarks

## 📞 Getting Help

**Questions about contributing?**

- Create a [GitHub Discussion](https://github.com/relay-framework/relay/discussions)
- Join our community chat (link in README)
- Email: <contributors@relay-framework.dev>

**Before starting major work:**

- Create an issue to discuss the approach
- Get feedback from maintainers
- Ensure alignment with project goals

## 🏆 Recognition

Contributors are recognized in:

- `CONTRIBUTORS.md` file
- Release notes
- Project documentation
- Community highlights

Thank you for contributing to Relay! 🚀
