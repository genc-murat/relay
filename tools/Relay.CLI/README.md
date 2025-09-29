# Relay CLI - Developer Tools

The Relay CLI is a comprehensive command-line interface for the Relay ultra high-performance mediator framework. It provides scaffolding, analysis, optimization, and benchmarking capabilities to enhance your development experience.

## Installation

### Global Tool Installation

```bash
dotnet tool install -g Relay.CLI
```

### Local Installation

```bash
dotnet add package Relay.CLI
```

## Commands

### 🏗️ Scaffold

Generate boilerplate code for handlers, requests, and tests:

```bash
# Basic scaffolding
relay scaffold --handler UserHandler --request GetUserQuery --response UserResponse

# With advanced options
relay scaffold --handler UserHandler --request GetUserQuery --response UserResponse \
  --namespace MyApp.Users \
  --template enterprise \
  --include-validation \
  --output ./src/Users

# Template options: standard, minimal, enterprise
```

### 🚀 Benchmark

Run comprehensive performance benchmarks:

```bash
# Basic benchmark
relay benchmark --iterations 100000

# Advanced benchmarking
relay benchmark --iterations 1000000 \
  --format html \
  --output benchmark-results.html \
  --tests all \
  --threads 4

# Format options: console, json, html, csv
# Tests: all, relay, mediatr, comparison
```

### 🔍 Analyze

Analyze your project for optimization opportunities:

```bash
# Basic analysis
relay analyze --path .

# Comprehensive analysis
relay analyze --path . \
  --depth full \
  --format html \
  --output analysis-report.html \
  --include-tests

# Depth options: quick, standard, full, deep
# Format options: console, json, html, markdown
```

### 🔧 Optimize

Apply automatic optimizations:

```bash
# Dry run (show what would be optimized)
relay optimize --dry-run

# Apply optimizations with backup
relay optimize --backup

# Target specific areas
relay optimize --target handlers --aggressive

# Target options: all, handlers, requests, config
```

### ✅ Validate

Validate project structure and configuration:

```bash
# Basic validation
relay validate

# Strict validation
relay validate --strict --path ./src
```

### 📝 Generate

Generate additional components:

```bash
# Generate documentation
relay generate --type docs

# Generate configuration files
relay generate --type config

# Generate benchmark templates
relay generate --type benchmark --output ./benchmarks
```

### ⚡ Performance

Performance analysis and recommendations:

```bash
# Analyze performance
relay performance --path .

# Generate performance report
relay performance --report --path .
```

## Examples

### Complete Project Setup

```bash
# 1. Create new handler
relay scaffold --handler OrderHandler --request CreateOrderCommand --response OrderResponse \
  --namespace Ecommerce.Orders \
  --template enterprise \
  --include-validation

# 2. Analyze the project
relay analyze --depth full --format html --output analysis.html

# 3. Apply optimizations
relay optimize --aggressive --backup

# 4. Run benchmarks
relay benchmark --iterations 1000000 --format html --output performance.html

# 5. Validate everything
relay validate --strict
```

### Performance Optimization Workflow

```bash
# 1. Check current performance
relay performance --report

# 2. Identify optimization opportunities  
relay analyze --depth deep

# 3. Apply optimizations (dry run first)
relay optimize --dry-run --aggressive

# 4. Apply actual optimizations
relay optimize --aggressive --backup

# 5. Verify improvements
relay benchmark --tests all --format console
```

## Features

### 🎯 Smart Scaffolding
- Multiple templates (minimal, standard, enterprise)
- Automatic test generation
- Validation and authorization attributes
- Performance optimized code patterns

### 📊 Comprehensive Analysis
- Performance opportunity detection
- Reliability pattern checking
- Dependency analysis
- Code quality assessment

### 🔧 Intelligent Optimization
- Task to ValueTask conversion
- CancellationToken addition
- Record conversion for requests
- Configuration optimizations
- Framework-specific enhancements

### 🚀 Advanced Benchmarking
- Multiple Relay implementations
- Comparison with other frameworks
- Memory allocation tracking
- Throughput measurements
- Beautiful HTML reports

### ✅ Project Validation
- Configuration validation
- Structure verification
- Best practices checking
- Compliance reporting

## Configuration

Create a `.relay-cli.json` file in your project root:

```json
{
  "defaultNamespace": "MyApp",
  "templatePreference": "enterprise",
  "optimizationLevel": "aggressive",
  "includeTests": true,
  "backupOnOptimize": true,
  "benchmarkIterations": 100000
}
```

## Templates

### Standard Template
- Basic handler and request structure
- Essential attributes and patterns
- Simple test cases

### Minimal Template
- Minimal code generation
- Essential functionality only
- Quick prototyping

### Enterprise Template
- Comprehensive logging
- Validation attributes
- Authorization patterns
- Performance optimizations
- Extensive test coverage
- Documentation generation

## Performance Benefits

Using Relay CLI optimizations can achieve:

- **67% faster** than MediatR
- **95% less** memory allocation
- **Zero overhead** vs direct calls
- **Sub-millisecond** response times

## Integration

### CI/CD Pipeline

```yaml
- name: Validate Relay Project
  run: relay validate --strict

- name: Optimize Performance
  run: relay optimize --target all

- name: Run Benchmarks
  run: relay benchmark --format json --output benchmark-results.json
```

### IDE Integration

Works seamlessly with:
- Visual Studio 2022
- VS Code
- JetBrains Rider
- Command line environments

## Support

- **Documentation**: [Relay Framework Docs](https://github.com/genc-murat/relay)
- **Issues**: [GitHub Issues](https://github.com/genc-murat/relay/issues)
- **Discussions**: [GitHub Discussions](https://github.com/genc-murat/relay/discussions)

## License

MIT License - see [LICENSE](LICENSE) file for details.