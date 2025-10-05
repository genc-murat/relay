using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Relay.CLI.Migration;

/// <summary>
/// Core migration engine that orchestrates the migration process
/// </summary>
public class MigrationEngine
{
    private readonly MediatRAnalyzer _analyzer;
    private readonly CodeTransformer _transformer;
    private readonly BackupManager _backupManager;

    public MigrationEngine()
    {
        _analyzer = new MediatRAnalyzer();
        _transformer = new CodeTransformer();
        _backupManager = new BackupManager();
    }

    public async Task<AnalysisResult> AnalyzeAsync(MigrationOptions options)
    {
        return await _analyzer.AnalyzeProjectAsync(options.ProjectPath);
    }

    public async Task<string> CreateBackupAsync(MigrationOptions options)
    {
        var backupPath = Path.Combine(options.ProjectPath, options.BackupPath, 
            $"backup_{DateTime.Now:yyyyMMdd_HHmmss}");
        
        await _backupManager.CreateBackupAsync(options.ProjectPath, backupPath);
        
        return backupPath;
    }

    public async Task<MigrationResult> MigrateAsync(MigrationOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult
        {
            Status = MigrationStatus.InProgress,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Analyze first
            var analysis = await _analyzer.AnalyzeProjectAsync(options.ProjectPath);
            
            if (!analysis.CanMigrate)
            {
                result.Status = MigrationStatus.Failed;
                result.Issues.AddRange(analysis.Issues.Where(i => i.Severity == IssueSeverity.Error)
                    .Select(i => i.Message));
                return result;
            }

            // Create backup if requested
            if (options.CreateBackup && !options.DryRun)
            {
                result.BackupPath = await CreateBackupAsync(options);
                result.CreatedBackup = true;
            }

            // Transform package references
            if (!options.DryRun)
            {
                await TransformPackageReferences(options.ProjectPath, result);
            }
            else
            {
                // Dry run - just record what would be done
                foreach (var pkg in analysis.PackageReferences)
                {
                    result.Changes.Add(new MigrationChange
                    {
                        Category = "Package References",
                        Type = ChangeType.Remove,
                        Description = $"Remove package: {pkg.Name} {pkg.CurrentVersion}",
                        FilePath = pkg.ProjectFile
                    });
                }
                result.Changes.Add(new MigrationChange
                {
                    Category = "Package References",
                    Type = ChangeType.Add,
                    Description = "Add package: Relay.Core (latest)",
                    FilePath = analysis.PackageReferences.FirstOrDefault()?.ProjectFile ?? ""
                });
            }

            // Transform code files
            var codeFiles = Directory.GetFiles(options.ProjectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\Migrations\\"))
                .ToList();

            foreach (var file in codeFiles)
            {
                var content = await File.ReadAllTextAsync(file);
                
                // Check if file needs transformation
                if (!NeedsTransformation(content))
                    continue;

                var transformed = await _transformer.TransformFileAsync(file, content, options);
                
                if (transformed.WasModified)
                {
                    if (!options.DryRun)
                    {
                        await File.WriteAllTextAsync(file, transformed.NewContent);
                    }

                    result.FilesModified++;
                    result.LinesChanged += transformed.LinesChanged;
                    result.Changes.AddRange(transformed.Changes);

                    if (transformed.IsHandler)
                    {
                        result.HandlersMigrated++;
                    }
                }
            }

            // Add manual steps if needed
            if (analysis.HasCustomMediator)
            {
                result.ManualSteps.Add("Review custom IMediator implementation - may need manual updates");
            }

            if (analysis.HasCustomBehaviors)
            {
                result.ManualSteps.Add("Review custom pipeline behaviors - signatures may need updates");
            }

            result.Status = result.Issues.Count == 0 ? MigrationStatus.Success : MigrationStatus.Partial;
        }
        catch (Exception ex)
        {
            result.Status = MigrationStatus.Failed;
            result.Issues.Add($"Migration failed: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.EndTime = DateTime.UtcNow;
        }

        return result;
    }

    public async Task<bool> RollbackAsync(string backupPath)
    {
        return await _backupManager.RestoreBackupAsync(backupPath);
    }

    private bool NeedsTransformation(string content)
    {
        return content.Contains("using MediatR") ||
               content.Contains("IRequest") ||
               content.Contains("IRequestHandler") ||
               content.Contains("INotification") ||
               content.Contains("INotificationHandler") ||
               content.Contains("IPipelineBehavior");
    }

    private async Task TransformPackageReferences(string projectPath, MigrationResult result)
    {
        var projectFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
            .ToList();

        foreach (var projFile in projectFiles)
        {
            var content = await File.ReadAllTextAsync(projFile);
            var doc = XDocument.Parse(content);
            var modified = false;

            // Remove MediatR packages
            var mediatrPackages = new[] { "MediatR", "MediatR.Extensions.Microsoft.DependencyInjection", "MediatR.Contracts" };
            
            foreach (var package in mediatrPackages)
            {
                var packageRefs = doc.Descendants("PackageReference")
                    .Where(e => e.Attribute("Include")?.Value == package)
                    .ToList();

                foreach (var packageRef in packageRefs)
                {
                    packageRef.Remove();
                    modified = true;

                    result.Changes.Add(new MigrationChange
                    {
                        Category = "Package References",
                        Type = ChangeType.Remove,
                        Description = $"Removed package: {package}",
                        FilePath = projFile
                    });
                }
            }

            // Add Relay.Core if not present
            var hasRelayCore = doc.Descendants("PackageReference")
                .Any(e => e.Attribute("Include")?.Value == "Relay.Core");

            if (!hasRelayCore && modified)
            {
                // Find an ItemGroup with PackageReferences or create one
                var itemGroup = doc.Descendants("ItemGroup")
                    .FirstOrDefault(ig => ig.Elements("PackageReference").Any());

                if (itemGroup != null)
                {
                    var relayReference = new XElement("PackageReference");
                    relayReference.SetAttributeValue("Include", "Relay.Core");
                    relayReference.SetAttributeValue("Version", "2.1.0");
                    itemGroup.Add(relayReference);

                    result.Changes.Add(new MigrationChange
                    {
                        Category = "Package References",
                        Type = ChangeType.Add,
                        Description = "Added package: Relay.Core 2.1.0",
                        FilePath = projFile
                    });
                }
            }

            if (modified)
            {
                await File.WriteAllTextAsync(projFile, doc.ToString());
            }
        }
    }
}
