# Relay CLI - Developer Tools

The Relay CLI is a comprehensive command-line interface for the Relay ultra high-performance mediator framework. It provides scaffolding, analysis, optimization, and benchmarking capabilities to enhance your development experience.

## ✨ What's New in v2.2.0 🚀

### 🎯 Major Features

#### 1. **Complete MediatR Migration Workflow** 🔄
Automated migration from MediatR to Relay with full support for:
- Analysis and detection of MediatR usage
- Automatic backup before migration
- Code transformation (Task→ValueTask, Handle→HandleAsync, etc.)
- Post-migration validation
- Comprehensive reporting (Markdown, JSON, HTML)
- Rollback support

```bash
relay migrate --backup --output migration-report.md
```

#### 2. **Plugin Lifecycle Management** 🔌
Full plugin ecosystem with complete lifecycle:
- **Install** - From local, ZIP, or NuGet
- **Load** - With AssemblyLoadContext isolation  
- **Initialize** - With context and services
- **Execute** - Safe execution environment
- **Cleanup & Unload** - Proper resource management

```bash
relay plugin create --name my-plugin
relay plugin install .
relay plugin run my-plugin
```

#### 3. **Project Development Pipeline** 🚀
Integrated end-to-end workflow:
- **Init** - Project initialization with templates
- **Doctor** - Health checks and diagnostics
- **Validate** - Code pattern validation
- **Optimize** - Performance optimizations

```bash
relay pipeline --name MyProject --template enterprise --auto-fix
```

### 📊 Test Coverage
- **225 tests** passing (100% pass rate)
- **+58 new tests** for v2.2.0 features
- **~95% code coverage**

## ✨ What's New in v2.1.0

### 🆕 New Commands

- **`relay init`** - Initialize new Relay projects with complete scaffolding
  - Multiple templates (minimal, standard, enterprise)
  - Docker and CI/CD support
  - Auto-generates solution structure, handlers, and tests

- **`relay doctor`** - Comprehensive health check for your Relay projects
  - Checks project structure and dependencies
  - Validates handlers and best practices
  - Identifies performance issues
  - Auto-fix capability (--fix flag)

### ✨ Enhanced Commands

- **`relay validate`** - Now includes:
  - Roslyn-based code analysis
  - Handler pattern validation
  - Request/Response type checking
  - Best practices validation
  - Export reports (JSON, Markdown)

- **`relay performance`** - Real performance analysis:
  - Async pattern analysis
  - Memory pattern detection
  - Performance scoring (0-100)
  - Actionable recommendations
  - Detailed HTML/Markdown reports

### 🎨 Improvements

- Better error handling and user feedback
- Standardized exit codes
- Enhanced progress indicators
- Color-coded output for better readability
- Version information display (`--version`)

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

### 🔄 Migrate - MediatR to Relay Migration (NEW v2.2.0)

Automated migration from MediatR with complete workflow:

```bash
# Analyze project for migration
relay migrate --analyze-only

# Preview changes (dry run)
relay migrate --dry-run --preview

# Preview with side-by-side diff
relay migrate --dry-run --preview --side-by-side

# Interactive migration - review each file
relay migrate --interactive

# Interactive with side-by-side diff
relay migrate --interactive --side-by-side

# Full migration with backup
relay migrate --backup --output migration-report.md

# Aggressive optimizations during migration
relay migrate --aggressive

# Rollback migration
relay migrate rollback --backup .backup/backup_20250110
```

**Features:**
- 🔍 Automatic MediatR detection
- 💾 Backup creation before changes
- 🔄 Code transformation (Task→ValueTask, using statements, etc.)
- 👁️ **NEW: Inline and side-by-side diff preview**
- 🎯 **NEW: Interactive mode with per-file approval**
- 📊 **NEW: Change summary (lines added/removed/modified)**
- ✅ Post-migration validation
- 📊 Comprehensive reports (MD, JSON, HTML)
- ⏮️ Rollback support
- 🤝 Interactive mode for granular control

### 🔌 Plugin - Plugin Management (NEW v2.2.0)

Full plugin lifecycle management:

```bash
# List installed plugins
relay plugin list --all

# Search marketplace
relay plugin search swagger

# Install plugin
relay plugin install relay-plugin-swagger --version 1.0.0

# Create new plugin
relay plugin create --name my-plugin --template advanced

# Get plugin info
relay plugin info relay-plugin-swagger

# Uninstall plugin
relay plugin uninstall my-plugin

# Update plugins
relay plugin update
```

**Features:**
- 📦 Install/uninstall plugins
- 🔍 Search and discover plugins
- 🎨 Create custom plugins from templates
- 🔄 Plugin lifecycle (Load→Initialize→Execute→Cleanup→Unload)
- 🛡️ AssemblyLoadContext isolation
- 📝 Full context access (FileSystem, Config, Logger, DI)

### 🚀 Pipeline - Complete Project Workflow (NEW v2.2.0)

Integrated development pipeline:

```bash
# Run complete pipeline
relay pipeline --name MyProject --template enterprise

# Skip specific stages
relay pipeline --skip init,doctor

# Aggressive optimizations
relay pipeline --aggressive --auto-fix

# CI/CD mode
relay pipeline --ci --report pipeline-report.md

# Custom configuration
relay pipeline --path . --skip init --auto-fix --report results.md
```

**Pipeline Stages:**
1. **🎬 Init** - Project initialization
2. **🏥 Doctor** - Health checks
3. **✅ Validate** - Code validation
4. **⚡ Optimize** - Performance optimization

**Features:**
- 🔄 Complete end-to-end workflow
- ⚙️ Stage skipping and customization
- 🤖 Auto-fix capability
- 📊 Comprehensive reporting
- 🎯 CI/CD friendly
- ⚡ Fast execution (<5s for full pipeline)

### 🆕 Init - Initialize New Project

Create a new Relay project with complete scaffolding:

```bash
# Basic initialization
relay init --name MyProject

# Enterprise template with Docker and CI
relay init --name MyProject \
  --template enterprise \
  --framework net8.0 \
  --docker \
  --ci

# Template options: minimal, standard, enterprise
```

**Features:**
- Creates complete solution structure
- Generates sample handlers and tests
- Includes README and configuration files
- Optional Docker support
- Optional CI/CD configuration (GitHub Actions)
- Git initialization

### 🆕 Doctor - Health Check

Run comprehensive health checks on your project:

```bash
# Basic health check
relay doctor

# Verbose output with auto-fix
relay doctor --verbose --fix

# Check specific path
relay doctor --path ./src
```

**Checks:**
- ✅ Project structure
- ✅ Dependencies and versions
- ✅ Handler configuration
- ✅ Performance settings
- ✅ Best practices
- 🔧 Auto-fix capability

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