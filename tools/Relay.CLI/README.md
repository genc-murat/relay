# Relay CLI - Developer Tools for High-Performance Mediator Framework

[Relay](https://github.com/genc-murat/relay) is a high-performance mediator framework for .NET with compile-time source generation, designed as a faster alternative to MediatR. The Relay CLI provides powerful developer tools for scaffolding, analyzing, optimizing, and managing Relay-based applications.

## 🚀 Installation

```bash
dotnet tool install -g Relay.CLI
```

Or as a local tool:

```bash
dotnet new tool-manifest
dotnet tool install --local Relay.CLI
```

## 📋 Available Commands

### `relay init` - Project Scaffolding
Initialize new Relay projects with complete scaffolding:

```bash
relay init --name MyProject --template standard --framework net8.0
```

**Options:**
- `--name` - Project name (required)
- `--template` - Project template (minimal, standard, enterprise)
- `--framework` - Target framework (net6.0, net8.0, net9.0)
- `--output` - Output directory (default: . )
- `--git` - Initialize git repository (default: true)
- `--docker` - Include Docker support (default: false)
- `--ci` - Include CI/CD configuration (default: false)

### `relay doctor` - Health Check
Comprehensive health check for Relay projects:

```bash
relay doctor --path . --verbose --fix
```

**Options:**
- `--path` - Project path to check (default: . )
- `--verbose` - Show detailed diagnostic information (default: false)
- `--fix` - Attempt to automatically fix issues (default: false)

### `relay migrate` - Framework Migration
Automated migration from MediatR to Relay:

```bash
relay migrate --path . --analyze-only --preview
```

**Options:**
- `--from` - Source framework to migrate from (default: MediatR)
- `--to` - Target framework to migrate to (default: Relay)
- `--path` - Project path to migrate (default: . )
- `--analyze-only` - Only analyze without migrating (default: false)
- `--dry-run` - Show changes without applying them (default: false)
- `--preview` - Show detailed diff preview (default: false)
- `--side-by-side` - Use side-by-side diff display (default: false)
- `--backup` - Create backup before migration (default: true)
- `--backup-path` - Backup directory path (default: .backup)
- `--output` - Migration report output path
- `--format` - Report format (markdown, json, html) (default: markdown)
- `--aggressive` - Apply aggressive optimizations (default: false)
- `--interactive` - Prompt for each change (default: false)

### `relay pipeline` - Complete Development Pipeline
Run the complete project development pipeline: init → doctor → validate → optimize

```bash
relay pipeline --name MyProject --template enterprise --aggressive --auto-fix
```

**Options:**
- `--path` - Project path (default: . )
- `--name` - Project name (for new projects)
- `--template` - Template (minimal, standard, enterprise) (default: standard)
- `--skip` - Skip stages (init, doctor, validate, optimize)
- `--aggressive` - Use aggressive optimizations (default: false)
- `--auto-fix` - Automatically fix detected issues (default: false)
- `--report` - Generate pipeline report
- `--ci` - Run in CI mode (non-interactive) (default: false)

### `relay plugin` - Plugin Management
Manage Relay CLI plugins:

```bash
# List installed plugins
relay plugin list

# Search for plugins
relay plugin search relay-plugin-swagger

# Install a plugin
relay plugin install relay-plugin-swagger

# Create a new plugin
relay plugin create --name my-plugin --output ./plugins
```

**Subcommands:**
- `list` - List installed plugins
- `search <query>` - Search for plugins in the marketplace
- `install <name>` - Install a plugin
- `uninstall <name>` - Uninstall a plugin
- `update [name]` - Update installed plugins
- `info <name>` - Show detailed information about a plugin
- `create` - Create a new plugin from template

### `relay scaffold` - Code Generation
Generate boilerplate code for handlers, requests, and tests:

```bash
relay scaffold --handler GetUserHandler --request GetUserQuery --response GetUserResponse
```

**Options:**
- `--handler` - Handler class name (required)
- `--request` - Request class name (required)
- `--response` - Response class name (optional)
- `--namespace` - Target namespace (default: YourApp)
- `--output` - Output directory (default: . )
- `--template` - Template type (standard, minimal, enterprise) (default: standard)
- `--include-tests` - Generate test files (default: true)

### `relay benchmark` - Performance Testing
Run comprehensive performance benchmarks:

```bash
relay benchmark --iterations 100000 --format json --output results.json
```

**Options:**
- `--iterations` - Number of iterations per test (default: 100000)
- `--output` - Output file for results
- `--format` - Output format (console, json, html, csv) (default: console)

### `relay analyze` - Code Analysis
Analyze your project for performance optimization opportunities:

```bash
relay analyze --path . --include-tests
```

**Options:**
- `--path` - Project path to analyze (default: . )
- `--include-tests` - Include test projects in analysis (default: false)
- `--exclude` - Exclude patterns (e.g., "obj/*,bin/*")

### `relay optimize` - Performance Optimization
Apply automatic optimizations to improve performance:

```bash
relay optimize --path . --target all --aggressive --dry-run
```

**Options:**
- `--path` - Project path to optimize (default: . )
- `--dry-run` - Show what would be optimized without applying changes (default: false)
- `--target` - Optimization target (all, handlers, requests, config) (default: all)
- `--aggressive` - Apply aggressive optimizations (default: false)
- `--backup` - Create backup before applying changes (default: true)

### `relay validate` - Project Validation
Validate project structure and configuration:

```bash
relay validate --path .
```

**Options:**
- `--path` - Project path to validate (default: . )
- `--strict` - Enable strict validation mode (default: false)
- `--report` - Generate validation report

### `relay generate` - Generate Utilities
Generate additional components and utilities:

```bash
relay generate --type docs --output ./docs
```

**Options:**
- `--type` - Type to generate (docs, config, benchmark) (required)
- `--output` - Output directory (default: . )

### `relay performance` - Performance Analysis
Performance analysis and recommendations:

```bash
relay performance --path . --report --detailed
```

**Options:**
- `--path` - Project path to analyze (default: . )
- `--report` - Generate performance report (default: true)
- `--detailed` - Show detailed analysis (default: false)

### `relay ai` - AI-Powered Analysis
AI-powered analysis and optimization for Relay projects:

```bash
relay ai analyze --path . --depth comprehensive --format json
```

**Subcommands:**
- `ai analyze` - Analyze code for AI optimization opportunities
- `ai optimize` - Apply AI-recommended optimizations
- `ai predict` - Predict performance and generate recommendations
- `ai learn` - Learn from performance data to improve AI recommendations
- `ai insights` - Generate comprehensive AI-powered system insights

## 🏗️ Project Templates

Relay CLI comes with 10+ production-ready project templates:

- **relay-webapi** - Clean Architecture Web API
- **relay-microservice** - Event-Driven Microservice
- **relay-ddd** - Domain-Driven Design
- **relay-cqrs-es** - CQRS + Event Sourcing
- **relay-modular** - Modular Monolith
- **relay-graphql** - GraphQL API
- **relay-grpc** - gRPC Service
- **relay-serverless** - Serverless Functions
- **relay-blazor** - Blazor Application
- **relay-maui** - MAUI Mobile App

### Using Templates

```bash
# List all templates
relay new --list

# Create project from template
relay new relay-webapi --name MyApi --output ./MyProject

# With specific features
relay new relay-webapi --name MyApi --features "auth,swagger,docker"
```

## 🎯 Key Features

### 🚀 Performance Optimizations
- **ValueTask Generation**: Automatic conversion from `Task` to `ValueTask`
- **Source Generators**: Compile-time code generation for zero-overhead dispatch
- **Caching**: Built-in distributed caching with configurable strategies
- **Batching**: Automatic request batching for improved throughput
- **SIMD Operations**: Vectorized operations for data processing

### 🔧 Developer Experience
- **Real-time Analysis**: Live performance monitoring and recommendations
- **Project Scaffolding**: Complete project templates for different architectures
- **Migration Tools**: Seamless migration from MediatR to Relay
- **Testing Integration**: Built-in unit and integration testing support
- **CI/CD Pipeline**: Ready-to-use continuous integration/deployment pipelines

### 📊 Advanced Analysis
- **AI-Powered Insights**: Machine learning-based performance recommendations
- **Benchmarking Suite**: Comprehensive performance benchmarking tools
- **Health Monitoring**: Real-time system health and performance metrics
- **Predictive Analytics**: Performance prediction based on code patterns

## 🛠️ Usage Examples

### Quick Start
```bash
# Create a new project
relay init --name MyWebApi --template relay-webapi

# Navigate to project
cd MyWebApi

# Run complete development pipeline
relay pipeline --path . --aggressive --auto-fix
```

### Migration from MediatR
```bash
# Analyze project before migration
relay migrate --path . --analyze-only

# Preview changes
relay migrate --path . --dry-run --preview

# Perform migration with backup
relay migrate --path . --backup --backup-path ./backup
```

### Performance Optimization
```bash
# Analyze for optimization opportunities
relay analyze --path .

# Apply optimizations
relay optimize --path . --target handlers --aggressive

# Benchmark performance
relay benchmark --iterations 50000 --format json --output results.json
```

### AI-Powered Assistance
```bash
# Analyze code with AI
relay ai analyze --path . --depth comprehensive

# Apply AI-recommended optimizations
relay ai optimize --path . --risk-level low --confidence-threshold 0.8

# Generate performance insights
relay ai insights --path . --time-window 24h --include-predictions
```

## 🧩 Plugin System

Relay CLI features a robust plugin system that allows extending functionality:

```bash
# Create a new plugin
relay plugin create --name my-relay-plugin

# Install a plugin
relay plugin install relay-plugin-swagger

# List installed plugins
relay plugin list
```

### Creating Custom Plugins

1. **Generate plugin template**:
```bash
relay plugin create --name my-plugin --output ./plugins
```

2. **Implement your plugin**:
```csharp
[RelayPlugin("my-plugin", "1.0.0")]
public class MyPlugin : IRelayPlugin
{
    public string Name => "my-plugin";
    public string Version => "1.0.0";
    public string Description => "My awesome Relay plugin";

    public async Task<bool> InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation($"Initializing {Name}...");
        return true;
    }

    public async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Hello from {Name}!");
        return 0;
    }
}
```

3. **Build and install**:
```bash
cd my-plugin
dotnet build
relay plugin install .
```

## 📚 Documentation & Resources

- [Relay Core Documentation](https://github.com/genc-murat/relay)
- [Performance Benchmarks](https://github.com/genc-murat/relay/blob/main/docs/benchmarks.md)
- [Migration Guide](https://github.com/genc-murat/relay/blob/main/docs/migration.md)
- [Architecture Patterns](https://github.com/genc-murat/relay/blob/main/docs/architecture.md)
- [API Reference](https://github.com/genc-murat/relay/blob/main/docs/api.md)

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/genc-murat/relay/blob/main/CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/genc-murat/relay/blob/main/LICENSE) file for details.

## 🆘 Support

- 📖 [Documentation](https://github.com/genc-murat/relay)
- 🐛 [Issue Tracker](https://github.com/genc-murat/relay/issues)
- 💬 [Discussions](https://github.com/genc-murat/relay/discussions)