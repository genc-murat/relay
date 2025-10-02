# Relay CLI v2.1.0 - Changelog

## 🎉 Release Highlights

Relay CLI v2.1.0 introduces powerful new commands and significantly enhanced existing features to streamline your Relay development workflow.

## 🆕 New Features

### 1. **Init Command** - Project Initialization
- Complete project scaffolding from templates
- Automatic solution and project structure generation
- Sample handlers and tests included
- Docker support with Dockerfile and docker-compose
- CI/CD configuration (GitHub Actions)
- Git initialization with .gitignore
- Three templates: minimal, standard, enterprise

**Example:**
```bash
relay init --name MyAwesomeApp --template enterprise --docker --ci
```

### 2. **Doctor Command** - Health Checks
- Comprehensive project health diagnostics
- Validates project structure and dependencies
- Checks handler patterns and best practices
- Performance settings verification
- Color-coded diagnostic results
- Auto-fix capability for common issues

**Example:**
```bash
relay doctor --verbose --fix
```

## ✨ Enhanced Features

### 3. **Validate Command** - Enhanced Validation
- **New:** Roslyn-based syntax analysis
- **New:** Handler pattern validation (ValueTask vs Task)
- **New:** CancellationToken usage checks
- **New:** Request/Response type analysis
- **New:** Export validation reports (JSON, Markdown)
- **Improved:** Better error messages and suggestions
- **Improved:** Severity-based issue categorization

**Example:**
```bash
relay validate --strict --output report.md --format markdown
```

### 4. **Performance Command** - Real Analysis
- **New:** Actual code analysis (not simulated)
- **New:** Async pattern detection
- **New:** Memory pattern analysis
- **New:** Performance scoring (0-100)
- **New:** LINQ and string concatenation checks
- **New:** Actionable recommendations with priority
- **Improved:** Detailed HTML/Markdown reports

**Example:**
```bash
relay performance --detailed --output performance-report.md
```

## 🛠️ Improvements

### Error Handling
- ✅ Graceful exception handling
- ✅ User-friendly error messages
- ✅ Standardized exit codes (0: success, 1: error, 2: validation failed, 130: cancelled)
- ✅ Better stack trace formatting

### User Experience
- ✅ Enhanced progress indicators with Spectre.Console
- ✅ Color-coded output (green: success, yellow: warning, red: error)
- ✅ Beautiful ASCII art banners
- ✅ Consistent table formatting
- ✅ Version information display (`--version`)

### Code Quality
- ✅ Microsoft.CodeAnalysis integration for real code analysis
- ✅ Roslyn syntax tree parsing
- ✅ Pattern matching improvements
- ✅ Better async/await patterns

## 📊 Technical Details

### Dependencies
- .NET 8.0 target framework
- Spectre.Console 0.49.1 for rich console output
- System.CommandLine 2.0.0-beta4 for CLI framework
- Microsoft.CodeAnalysis.CSharp 4.14.0 for code analysis
- BenchmarkDotNet 0.13.12 for performance testing

### Performance
- ✅ Fast file scanning with parallel processing support
- ✅ Incremental analysis capabilities
- ✅ Efficient memory usage
- ✅ Optimized for large codebases

## 🔄 Breaking Changes

None. v2.1.0 is fully backward compatible with v2.0.0.

## 📝 Migration Guide

No migration needed from v2.0.0 to v2.1.0. Simply update:

```bash
dotnet tool update -g Relay.CLI
```

## 🚀 Quick Start

### Initialize a new project:
```bash
relay init --name MyProject --template enterprise
cd MyProject
dotnet build
```

### Run health check:
```bash
relay doctor --verbose
```

### Validate project:
```bash
relay validate --strict --output validation-report.md
```

### Analyze performance:
```bash
relay performance --detailed --report
```

### Scaffold new handler:
```bash
relay scaffold --handler UserHandler --request GetUserQuery --response UserResponse
```

## 🎯 Coming Soon (v2.2.0)

- 🔄 **Migration Command** - Automated migration from MediatR to Relay
- 👁️ **Watch Mode** - Real-time file monitoring and analysis
- 🎮 **Interactive Mode** - REPL-style interactive CLI
- 📚 **Recipe Book** - Pre-built optimization recipes
- 🧪 **Test Generation** - Automated test scaffolding
- 🔌 **Plugin System** - Extensible architecture for community plugins

## 🐛 Bug Fixes

- Fixed version option conflict in root command
- Fixed Spectre.Console Rule alignment API changes
- Improved error handling for missing projects
- Better handling of edge cases in code analysis

## 💝 Credits

Thanks to all contributors and the .NET community for feedback and suggestions!

## 📄 License

MIT License - See LICENSE file for details

---

**Full Changelog:** v2.0.0...v2.1.0
**Release Date:** 2025-01-10
