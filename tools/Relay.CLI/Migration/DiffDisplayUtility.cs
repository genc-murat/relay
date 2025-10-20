using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Spectre.Console;

namespace Relay.CLI.Migration
{
    /// <summary>
    /// Utility class for displaying diffs between original and modified code
    /// </summary>
    public static class DiffDisplayUtility
    {
        /// <summary>
        /// Displays a diff between original and modified content using Spectre.Console formatting
        /// </summary>
        /// <param name="original">Original content</param>
        /// <param name="modified">Modified content</param>
        /// <param name="maxLines">Maximum number of lines to display (0 for all lines)</param>
        public static void DisplayDiff(string original, string modified, int maxLines = 0)
        {
            if (string.IsNullOrEmpty(original) && string.IsNullOrEmpty(modified))
                return;

            var diff = InlineDiffBuilder.Diff(original ?? string.Empty, modified ?? string.Empty, false);

            var displayedLines = 0;
            var totalChanges = diff.Lines.Count(l => l.Type != DiffPlex.DiffBuilder.Model.ChangeType.Unchanged);
            
            foreach (var line in diff.Lines)
            {
                // Skip unchanged lines if maxLines is set and we're over the limit
                if (maxLines > 0 && displayedLines >= maxLines && line.Type == DiffPlex.DiffBuilder.Model.ChangeType.Unchanged)
                    continue;

                var color = line.Type switch
                {
                    DiffPlex.DiffBuilder.Model.ChangeType.Inserted => "green",
                    DiffPlex.DiffBuilder.Model.ChangeType.Deleted => "red",
                    DiffPlex.DiffBuilder.Model.ChangeType.Modified => "yellow",
                    _ => "white"
                };

                var prefix = line.Type switch
                {
                    DiffPlex.DiffBuilder.Model.ChangeType.Inserted => "+ ",
                    DiffPlex.DiffBuilder.Model.ChangeType.Deleted => "- ",
                    DiffPlex.DiffBuilder.Model.ChangeType.Modified => "~ ",
                    _ => "  "
                };

                // Escape square brackets to prevent Spectre.Console from interpreting them as markup
                var escapedLine = line.Text?.Replace("[", "[[")?.Replace("]", "]]") ?? string.Empty;

                // Truncate very long lines to prevent rendering issues
                const int maxLineLength = 500;
                if (escapedLine.Length > maxLineLength)
                {
                    escapedLine = escapedLine.Substring(0, maxLineLength) + "... (truncated)";
                }

                AnsiConsole.MarkupLine($"[{color}]{prefix}{escapedLine}[/]");
                
                displayedLines++;
            }

            // Show truncation message if needed
            if (maxLines > 0 && displayedLines < diff.Lines.Count)
            {
                var skippedLines = diff.Lines.Count - displayedLines;
                AnsiConsole.MarkupLine($"[dim]... ({skippedLines} more lines not shown, {totalChanges} total changes)[/]");
            }
        }

        /// <summary>
        /// Creates a side-by-side diff display
        /// </summary>
        /// <param name="original">Original content</param>
        /// <param name="modified">Modified content</param>
        public static void DisplaySideBySideDiff(string original, string modified)
        {
            if (string.IsNullOrEmpty(original) && string.IsNullOrEmpty(modified))
                return;

            var diff = SideBySideDiffBuilder.Diff(original ?? string.Empty, modified ?? string.Empty, false);

            // Create a table to display side by side
            var table = new Table()
                .Border(TableBorder.Minimal)
                .AddColumn(new TableColumn("[bold]Original[/]").Width(50))
                .AddColumn(new TableColumn("[bold]Modified[/]").Width(50));

            for (int i = 0; i < Math.Max(diff.OldText.Lines.Count, diff.NewText.Lines.Count); i++)
            {
                var oldLine = i < diff.OldText.Lines.Count ? 
                    FormatSideBySideLine(diff.OldText.Lines[i]) : string.Empty;
                var newLine = i < diff.NewText.Lines.Count ? 
                    FormatSideBySideLine(diff.NewText.Lines[i]) : string.Empty;

                var oldColor = i < diff.OldText.Lines.Count && 
                    diff.OldText.Lines[i].Type != DiffPlex.DiffBuilder.Model.ChangeType.Unchanged ? 
                    "red" : "white";
                var newColor = i < diff.NewText.Lines.Count && 
                    diff.NewText.Lines[i].Type != DiffPlex.DiffBuilder.Model.ChangeType.Unchanged ? 
                    "green" : "white";

                table.AddRow(
                    $"[{oldColor}]{oldLine}[/]", 
                    $"[{newColor}]{newLine}[/]"
                );
            }

            AnsiConsole.Write(table);
        }

        private static string FormatSideBySideLine(DiffPiece line)
        {
            var prefix = line.Type switch
            {
                DiffPlex.DiffBuilder.Model.ChangeType.Inserted => "+ ",
                DiffPlex.DiffBuilder.Model.ChangeType.Deleted => "- ",
                DiffPlex.DiffBuilder.Model.ChangeType.Modified => "~ ",
                _ => "  "
            };

            return prefix + (line.Text?.Replace("[", "[[")?.Replace("]", "]]") ?? string.Empty);
        }

        /// <summary>
        /// Shows a preview of the transformation for a specific file
        /// </summary>
        /// <param name="filePath">Path of the file</param>
        /// <param name="originalContent">Original content of the file</param>
        /// <param name="modifiedContent">Modified content of the file</param>
        /// <param name="useSideBySide">Use side-by-side diff display instead of inline</param>
        public static void PreviewFileTransformation(string filePath, string originalContent, string modifiedContent, bool useSideBySide = false)
        {
            var fileName = Path.GetFileName(filePath);
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule($"[bold cyan]Preview for {fileName}[/]").RuleStyle("cyan"));
            AnsiConsole.WriteLine();

            if (useSideBySide)
            {
                DisplaySideBySideDiff(originalContent, modifiedContent);
            }
            else
            {
                DisplayDiff(originalContent, modifiedContent);
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule().RuleStyle("cyan"));
        }

        /// <summary>
        /// Gets a summary of changes between original and modified content
        /// </summary>
        /// <param name="original">Original content</param>
        /// <param name="modified">Modified content</param>
        /// <returns>Summary of changes (lines added, removed, modified)</returns>
        public static (int LinesAdded, int LinesRemoved, int LinesModified) GetChangeSummary(string original, string modified)
        {
            if (string.IsNullOrEmpty(original) && string.IsNullOrEmpty(modified))
                return (0, 0, 0);

            var diff = InlineDiffBuilder.Diff(original ?? string.Empty, modified ?? string.Empty, false);

            int added = 0, removed = 0, modifiedLines = 0;

            foreach (var line in diff.Lines)
            {
                switch (line.Type)
                {
                    case DiffPlex.DiffBuilder.Model.ChangeType.Inserted:
                        added++;
                        break;
                    case DiffPlex.DiffBuilder.Model.ChangeType.Deleted:
                        removed++;
                        break;
                    case DiffPlex.DiffBuilder.Model.ChangeType.Modified:
                        modifiedLines++;
                        break;
                }
            }

            return (added, removed, modifiedLines);
        }
    }
}