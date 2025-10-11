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
    private Stopwatch? _operationStopwatch;
    private DateTime _operationStartTime;

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
        _operationStopwatch = stopwatch;
        _operationStartTime = DateTime.UtcNow;

        var result = new MigrationResult
        {
            Status = MigrationStatus.InProgress,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Initialize
            ReportProgress(options, new MigrationProgress
            {
                Stage = MigrationStage.Initializing,
                Message = "Initializing migration...",
                ElapsedTime = stopwatch.Elapsed
            });

            // Analyze first
            ReportProgress(options, new MigrationProgress
            {
                Stage = MigrationStage.Analyzing,
                Message = "Analyzing project...",
                ElapsedTime = stopwatch.Elapsed
            });

            var analysis = await _analyzer.AnalyzeProjectAsync(options.ProjectPath);

            if (!analysis.CanMigrate)
            {
                result.Status = MigrationStatus.Failed;
                result.Issues.AddRange(analysis.Issues.Where(i => i.Severity == IssueSeverity.Error)
                    .Select(i => i.Message));

                // Report completion before early return
                ReportProgress(options, new MigrationProgress
                {
                    Stage = MigrationStage.Completed,
                    Message = "Migration failed: Project cannot be migrated",
                    ElapsedTime = stopwatch.Elapsed
                });

                return result;
            }

            // Create backup if requested
            if (options.CreateBackup && !options.DryRun)
            {
                ReportProgress(options, new MigrationProgress
                {
                    Stage = MigrationStage.CreatingBackup,
                    Message = "Creating backup...",
                    ElapsedTime = stopwatch.Elapsed
                });

                result.BackupPath = await CreateBackupAsync(options);
                result.CreatedBackup = true;
            }

            // Transform package references
            ReportProgress(options, new MigrationProgress
            {
                Stage = MigrationStage.TransformingPackages,
                Message = "Transforming package references...",
                ElapsedTime = stopwatch.Elapsed
            });

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
            ReportProgress(options, new MigrationProgress
            {
                Stage = MigrationStage.TransformingCode,
                Message = "Discovering code files...",
                ElapsedTime = stopwatch.Elapsed
            });

            var codeFiles = Directory.GetFiles(options.ProjectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\Migrations\\"))
                .ToList();

            ReportProgress(options, new MigrationProgress
            {
                Stage = MigrationStage.TransformingCode,
                Message = $"Transforming {codeFiles.Count} code files...",
                TotalFiles = codeFiles.Count,
                ProcessedFiles = 0,
                IsParallel = options.EnableParallelProcessing && codeFiles.Count > 5,
                ElapsedTime = stopwatch.Elapsed
            });

            if (options.EnableParallelProcessing && codeFiles.Count > 5)
            {
                // Use parallel processing for better performance
                await ProcessFilesInParallelAsync(codeFiles, options, result);
            }
            else
            {
                // Use sequential processing for small projects or when disabled
                await ProcessFilesSequentiallyAsync(codeFiles, options, result);
            }

            // Finalize
            ReportProgress(options, new MigrationProgress
            {
                Stage = MigrationStage.Finalizing,
                Message = "Finalizing migration...",
                FilesModified = result.FilesModified,
                HandlersMigrated = result.HandlersMigrated,
                ElapsedTime = stopwatch.Elapsed
            });

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

            // Report completion
            ReportProgress(options, new MigrationProgress
            {
                Stage = MigrationStage.Completed,
                Message = "Migration completed successfully",
                FilesModified = result.FilesModified,
                HandlersMigrated = result.HandlersMigrated,
                ProcessedFiles = result.FilesModified,
                TotalFiles = result.FilesModified,
                ElapsedTime = stopwatch.Elapsed
            });
        }
        catch (Exception ex)
        {
            result.Status = MigrationStatus.Failed;
            result.Issues.Add($"Migration failed: {ex.Message}");

            // Report failure
            ReportProgress(options, new MigrationProgress
            {
                Stage = MigrationStage.Completed,
                Message = $"Migration failed: {ex.Message}",
                ElapsedTime = stopwatch.Elapsed
            });
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

    private async Task ProcessFilesSequentiallyAsync(List<string> codeFiles, MigrationOptions options, MigrationResult result)
    {
        var processedCount = 0;
        var lastReportTime = DateTime.UtcNow;

        foreach (var file in codeFiles)
        {
            var content = await File.ReadAllTextAsync(file);

            // Check if file needs transformation
            if (!NeedsTransformation(content))
            {
                processedCount++;
                continue;
            }

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

            processedCount++;

            // Report progress at intervals
            var now = DateTime.UtcNow;
            if ((now - lastReportTime).TotalMilliseconds >= options.ProgressReportInterval)
            {
                ReportProgress(options, new MigrationProgress
                {
                    Stage = MigrationStage.TransformingCode,
                    CurrentFile = Path.GetFileName(file),
                    TotalFiles = codeFiles.Count,
                    ProcessedFiles = processedCount,
                    FilesModified = result.FilesModified,
                    HandlersMigrated = result.HandlersMigrated,
                    Message = $"Processing {Path.GetFileName(file)}...",
                    ElapsedTime = _operationStopwatch?.Elapsed ?? TimeSpan.Zero,
                    IsParallel = false
                });

                lastReportTime = now;
            }
        }
    }

    private async Task ProcessFilesInParallelAsync(List<string> codeFiles, MigrationOptions options, MigrationResult result)
    {
        // Use a semaphore to limit concurrency and protect shared resources
        var semaphore = new SemaphoreSlim(options.MaxDegreeOfParallelism);
        var resultLock = new object();
        var processedCount = 0;
        var lastReportTime = DateTime.UtcNow;

        // Process files in batches to avoid overwhelming the system
        var batches = codeFiles
            .Select((file, index) => new { file, index })
            .GroupBy(x => x.index / options.ParallelBatchSize)
            .Select(g => g.Select(x => x.file).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            var tasks = batch.Select(async file =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var content = await File.ReadAllTextAsync(file);

                    // Check if file needs transformation
                    if (!NeedsTransformation(content))
                    {
                        lock (resultLock)
                        {
                            processedCount++;
                        }
                        return null;
                    }

                    var transformed = await _transformer.TransformFileAsync(file, content, options);

                    if (transformed.WasModified)
                    {
                        if (!options.DryRun)
                        {
                            await File.WriteAllTextAsync(file, transformed.NewContent);
                        }

                        // Thread-safe update of result
                        lock (resultLock)
                        {
                            result.FilesModified++;
                            result.LinesChanged += transformed.LinesChanged;
                            result.Changes.AddRange(transformed.Changes);

                            if (transformed.IsHandler)
                            {
                                result.HandlersMigrated++;
                            }

                            processedCount++;

                            // Report progress at intervals
                            var now = DateTime.UtcNow;
                            if ((now - lastReportTime).TotalMilliseconds >= options.ProgressReportInterval)
                            {
                                ReportProgress(options, new MigrationProgress
                                {
                                    Stage = MigrationStage.TransformingCode,
                                    CurrentFile = Path.GetFileName(file),
                                    TotalFiles = codeFiles.Count,
                                    ProcessedFiles = processedCount,
                                    FilesModified = result.FilesModified,
                                    HandlersMigrated = result.HandlersMigrated,
                                    Message = $"Processing {processedCount}/{codeFiles.Count} files...",
                                    ElapsedTime = _operationStopwatch?.Elapsed ?? TimeSpan.Zero,
                                    IsParallel = true
                                });

                                lastReportTime = now;
                            }
                        }

                        return transformed;
                    }

                    lock (resultLock)
                    {
                        processedCount++;
                    }

                    return null;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }
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
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\backup\\") && !f.Contains("\\.backup\\"))
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
                // Find an ItemGroup or create one
                var itemGroup = doc.Descendants("ItemGroup").FirstOrDefault();

                if (itemGroup == null)
                {
                    // Create new ItemGroup if none exists
                    var root = doc.Root;
                    if (root != null)
                    {
                        itemGroup = new XElement("ItemGroup");
                        root.Add(itemGroup);
                    }
                }

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

    private void ReportProgress(MigrationOptions options, MigrationProgress progress)
    {
        if (options.OnProgress == null)
            return;

        try
        {
            options.OnProgress(progress);
        }
        catch
        {
            // Ignore progress reporting errors
        }
    }
}
