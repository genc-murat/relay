using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Diagnostics;
using System.Xml.Linq;

namespace Relay.CLI.Migration;

/// <summary>
/// Core migration engine that orchestrates the migration process
/// </summary>
public class MigrationEngine
{
    private readonly MediatRAnalyzer _analyzer;
    private readonly CodeTransformer _transformer;
    private readonly BackupManager _backupManager;
    private readonly MigrationLogger? _logger;
    private Stopwatch? _operationStopwatch;
    private DateTime _operationStartTime;

    public MigrationEngine(ILogger<MigrationLogger>? logger = null)
    {
        _analyzer = new MediatRAnalyzer();
        _transformer = new CodeTransformer();
        _backupManager = new BackupManager();
        _logger = logger != null ? new MigrationLogger(logger) : null;
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
            // Log migration start
            _logger?.LogMigrationStarted(options);

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
            _logger?.LogAnalysisCompleted(
                analysis.HandlersFound,
                analysis.Issues.Count,
                analysis.CanMigrate
            );

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
                _logger?.LogBackupCreated(result.BackupPath);
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

            // Log migration completion
            _logger?.LogMigrationCompleted(result);

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
        catch (MigrationException ex)
        {
            result.Status = MigrationStatus.Failed;
            result.Issues.Add($"Migration failed: {ex.Message}");
            _logger?.LogMigrationFailed(ex);

            // Report failure
            ReportProgress(options, new MigrationProgress
            {
                Stage = MigrationStage.Completed,
                Message = $"Migration failed: {ex.Message}",
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

    public async Task<MigrationResult> MigrateInteractiveAsync(MigrationOptions options)
    {
        var result = new MigrationResult
        {
            Status = MigrationStatus.InProgress,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Analyze project first
            var analysis = await _analyzer.AnalyzeProjectAsync(options.ProjectPath);
            _logger?.LogAnalysisCompleted(
                analysis.HandlersFound,
                analysis.Issues.Count,
                analysis.CanMigrate
            );

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
                _logger?.LogBackupCreated(result.BackupPath);
            }

            // Get all code files to process
            var codeFiles = Directory.GetFiles(options.ProjectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\Migrations\\"))
                .ToList();

            foreach (var file in codeFiles)
            {
                var content = await File.ReadAllTextAsync(file);

                // Check if file needs transformation
                if (!NeedsTransformation(content))
                    continue;

                // Get preview of transformation
                var preview = await _transformer.PreviewTransformAsync(file);

                // Display file name and change summary
                AnsiConsole.MarkupLine($"[cyan]File: {Path.GetFileName(file)}[/]");
                var (added, removed, modified) = DiffDisplayUtility.GetChangeSummary(preview.OriginalContent, preview.NewContent);
                AnsiConsole.MarkupLine($"[dim]  Changes: [green]+{added}[/] [red]-{removed}[/] [yellow]~{modified}[/][/]");
                
                DiffDisplayUtility.PreviewFileTransformation(file, preview.OriginalContent, preview.NewContent, options.UseSideBySideDiff);

                var choices = new[] { "Yes", "No", "Yes to All", "Cancel" };
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Apply this change?")
                        .AddChoices(choices)
                );

                if (choice == "Yes" || choice == "Yes to All")
                {
                    // Apply the transformation
                    if (!options.DryRun)
                    {
                        await File.WriteAllTextAsync(file, preview.NewContent);
                    }

                    result.FilesModified++;
                    result.Changes.AddRange(preview.Changes);

                    if (preview.IsHandler)
                    {
                        result.HandlersMigrated++;
                    }
                }

                if (choice == "Yes to All")
                {
                    // Apply remaining files without asking
                    for (int i = codeFiles.IndexOf(file) + 1; i < codeFiles.Count; i++)
                    {
                        var remainingFile = codeFiles[i];
                        var remainingContent = await File.ReadAllTextAsync(remainingFile);

                        if (!NeedsTransformation(remainingContent))
                            continue;

                        var remainingPreview = await _transformer.PreviewTransformAsync(remainingFile);
                        
                        if (!options.DryRun)
                        {
                            await File.WriteAllTextAsync(remainingFile, remainingPreview.NewContent);
                        }

                        result.FilesModified++;
                        result.Changes.AddRange(remainingPreview.Changes);

                        if (remainingPreview.IsHandler)
                        {
                            result.HandlersMigrated++;
                        }
                    }
                    break; // Exit the main loop since we've processed all remaining files
                }

                if (choice == "Cancel")
                {
                    result.Status = MigrationStatus.Cancelled;
                    return result;
                }
            }

            result.Status = MigrationStatus.Success;
        }
        catch (Exception ex)
        {
            result.Status = MigrationStatus.Failed;
            result.Issues.Add($"Migration failed: {ex.Message}");
            _logger?.LogMigrationFailed(new MigrationException("Interactive migration failed", ex));
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
            result.Duration = DateTime.UtcNow - result.StartTime;
        }

        return result;
    }

    /// <summary>
    /// Show preview of changes without applying them
    /// </summary>
    public async Task<MigrationResult> PreviewAsync(MigrationOptions options)
    {
        var result = new MigrationResult
        {
            Status = MigrationStatus.Preview,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Analyze project first
            var analysis = await _analyzer.AnalyzeProjectAsync(options.ProjectPath);
            _logger?.LogAnalysisCompleted(
                analysis.HandlersFound,
                analysis.Issues.Count,
                analysis.CanMigrate
            );

            if (!analysis.CanMigrate)
            {
                result.Status = MigrationStatus.Failed;
                result.Issues.AddRange(analysis.Issues.Where(i => i.Severity == IssueSeverity.Error)
                    .Select(i => i.Message));
                return result;
            }

            // Get all code files to process
            var codeFiles = Directory.GetFiles(options.ProjectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\Migrations\\"))
                .ToList();

            foreach (var file in codeFiles)
            {
                var content = await File.ReadAllTextAsync(file);

                // Check if file needs transformation
                if (!NeedsTransformation(content))
                    continue;

                // Get preview of transformation (dry run)
                var preview = await _transformer.PreviewTransformAsync(file);

                if (preview.WasModified)
                {
                    // Display file name and diff preview if --preview flag is set
                    if (options.ShowPreview)
                    {
                        AnsiConsole.MarkupLine($"[cyan]File: {Path.GetFileName(file)}[/]");
                        
                        // Show change summary
                        var (added, removed, modified) = DiffDisplayUtility.GetChangeSummary(preview.OriginalContent, preview.NewContent);
                        AnsiConsole.MarkupLine($"[dim]  Lines: [green]+{added}[/] [red]-{removed}[/] [yellow]~{modified}[/][/]");
                        
                        DiffDisplayUtility.PreviewFileTransformation(file, preview.OriginalContent, preview.NewContent, options.UseSideBySideDiff);
                    }
                    else
                    {
                        // Just show file name without full diff
                        AnsiConsole.MarkupLine($"[green]✓[/] [cyan]{Path.GetFileName(file)}[/]");
                    }

                    result.FilesModified++;
                    result.Changes.AddRange(preview.Changes);

                    if (preview.IsHandler)
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

            result.Status = MigrationStatus.Preview;
        }
        catch (Exception ex)
        {
            result.Status = MigrationStatus.Failed;
            result.Issues.Add($"Preview failed: {ex.Message}");
            _logger?.LogMigrationFailed(new MigrationException("Preview failed", ex));
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
            result.Duration = DateTime.UtcNow - result.StartTime;
        }

        return result;
    }

    private async Task ProcessFilesSequentiallyAsync(List<string> codeFiles, MigrationOptions options, MigrationResult result)
    {
        var processedCount = 0;
        var lastReportTime = DateTime.UtcNow;

        foreach (var file in codeFiles)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);

                // Check if file needs transformation
                if (!NeedsTransformation(content))
                {
                    processedCount++;
                    continue;
                }

                var transformed = await _transformer.TransformFileAsync(file, content, options);

                // Check if transformation had an error
                if (!string.IsNullOrEmpty(transformed.Error))
                {
                    var errorMessage = $"Failed to transform {file}: {transformed.Error}";
                    result.Issues.Add(errorMessage);
                    _logger?.LogTransformationError(new Exception(transformed.Error), file);

                    if (!options.ContinueOnError)
                    {
                        throw new MigrationException(errorMessage, file);
                    }

                    processedCount++;
                    continue;
                }

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

                    _logger?.LogFileTransformed(file, transformed.Changes.Count);
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
            catch (SyntaxException ex)
            {
                var errorMessage = $"Syntax error in {file}: {ex.Message}";
                result.Issues.Add(errorMessage);
                _logger?.LogSyntaxError(ex, file);

                // Continue to next file, allowing other files to be migrated
                processedCount++;
                if (!options.ContinueOnError)
                {
                    throw new MigrationException(errorMessage, file, ex);
                }
            }
            catch (IOException ex)
            {
                var errorMessage = $"File I/O error in {file}: {ex.Message}";
                result.Issues.Add(errorMessage);
                _logger?.LogTransformationError(ex, file);

                processedCount++;
                if (!options.ContinueOnError)
                {
                    throw new MigrationException(errorMessage, file, ex);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Unexpected error in {file}: {ex.Message}";
                result.Issues.Add(errorMessage);
                _logger?.LogTransformationError(ex, file);

                processedCount++;
                if (!options.ContinueOnError)
                {
                    throw new MigrationException(errorMessage, file, ex);
                }
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

                    // Check if transformation had an error
                    if (!string.IsNullOrEmpty(transformed.Error))
                    {
                        var errorMessage = $"Failed to transform {file}: {transformed.Error}";
                        lock (resultLock)
                        {
                            result.Issues.Add(errorMessage);
                            processedCount++;
                        }
                        _logger?.LogTransformationError(new Exception(transformed.Error), file);

                        if (!options.ContinueOnError)
                        {
                            throw new MigrationException(errorMessage, file);
                        }

                        return null;
                    }

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

                        _logger?.LogFileTransformed(file, transformed.Changes.Count);

                        return transformed;
                    }

                    lock (resultLock)
                    {
                        processedCount++;
                    }

                    return null;
                }
                catch (SyntaxException ex)
                {
                    var errorMessage = $"Syntax error in {file}: {ex.Message}";
                    lock (resultLock)
                    {
                        result.Issues.Add(errorMessage);
                        processedCount++;
                    }
                    _logger?.LogSyntaxError(ex, file);

                    if (!options.ContinueOnError)
                    {
                        throw new MigrationException(errorMessage, file, ex);
                    }

                    return null;
                }
                catch (IOException ex)
                {
                    var errorMessage = $"File I/O error in {file}: {ex.Message}";
                    lock (resultLock)
                    {
                        result.Issues.Add(errorMessage);
                        processedCount++;
                    }
                    _logger?.LogTransformationError(ex, file);

                    if (!options.ContinueOnError)
                    {
                        throw new MigrationException(errorMessage, file, ex);
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Unexpected error in {file}: {ex.Message}";
                    lock (resultLock)
                    {
                        result.Issues.Add(errorMessage);
                        processedCount++;
                    }
                    _logger?.LogTransformationError(ex, file);

                    if (!options.ContinueOnError)
                    {
                        throw new MigrationException(errorMessage, file, ex);
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
            try
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

                        _logger?.LogPackageTransformed(package, "Removed", projFile);
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

                        _logger?.LogPackageTransformed("Relay.Core", "Added", projFile);
                    }
                }

                if (modified)
                {
                    await File.WriteAllTextAsync(projFile, doc.ToString());
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to transform package references in {projFile}: {ex.Message}";
                result.Issues.Add(errorMessage);
                _logger?.LogTransformationError(ex, projFile);
                
                // Package transformation failures shouldn't stop the migration
                // since code transformation can still work
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
