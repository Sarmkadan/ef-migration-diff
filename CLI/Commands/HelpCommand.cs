#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Utilities;

namespace EfMigrationDiff.CLI.Commands;

/// <summary>
/// Implements the help command to display usage information and command details.
/// Provides formatted help text with examples and option descriptions.
/// </summary>
public class HelpCommand : ICommand
{
    public string GetDescription() => "Display help information";

    public async Task<CommandResult> ExecuteAsync(CommandContext context)
    {
        var helpText = new System.Text.StringBuilder();

        helpText.AppendLine("\n╔════════════════════════════════════════════════════════════╗");
        helpText.AppendLine("║     EF Migration Diff - Entity Framework Migration Tool     ║");
        helpText.AppendLine("╚════════════════════════════════════════════════════════════╝\n");

        helpText.AppendLine("DESCRIPTION:");
        helpText.AppendLine("  Analyzes Entity Framework migrations across git branches");
        helpText.AppendLine("  to detect conflicts, schema changes, and incompatibilities.\n");

        helpText.AppendLine("COMMANDS:");
        helpText.AppendLine("  compare <src> <tgt>    Compare migrations between branches");
        helpText.AppendLine("  validate               Validate migration file structure");
        helpText.AppendLine("  analyze                Analyze migration impact");
        helpText.AppendLine("  report                 Generate detailed reports");
        helpText.AppendLine("  help                   Show this help message\n");

        helpText.AppendLine("OPTIONS:");
        helpText.AppendLine("  --format <fmt>         Output format (text/json/html)");
        helpText.AppendLine("  --output <path>        Custom output directory");
        helpText.AppendLine("  --verbose, -v          Enable verbose output");
        helpText.AppendLine("  --help, -h             Show help");
        helpText.AppendLine("  --version, --ver       Show version\n");

        helpText.AppendLine("EXAMPLES:");
        helpText.AppendLine("  ef-migration-diff compare develop main");
        helpText.AppendLine("  ef-migration-diff validate");
        helpText.AppendLine("  ef-migration-diff compare develop main --format json");
        helpText.AppendLine("  ef-migration-diff analyze --verbose\n");

        helpText.AppendLine("ENVIRONMENT VARIABLES:");
        helpText.AppendLine("  EF_MIGRATION_DIFF_REPO  Override repository path");
        helpText.AppendLine("  EF_MIGRATION_DIFF_OUTPUT Override output directory\n");

        context.WriteOutput(helpText.ToString());

        return CommandResult.Ok("Help displayed");
    }
}
