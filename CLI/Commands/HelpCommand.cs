#nullable enable
using EfMigrationDiff.Utilities;

namespace EfMigrationDiff.CLI.Commands;

/// <summary>
/// Implements the help command to display usage information and command details.
/// Provides formatted help text with examples and option descriptions.
/// </summary>
public class HelpCommand : ICommand
{
    private const string Description = "Display help information";

    private const string BannerTop = "\n╔════════════════════════════════════════════════════════════╗";
    private const string BannerTitle = "║     EF Migration Diff - Entity Framework Migration Tool     ║";
    private const string BannerBottom = "╚════════════════════════════════════════════════════════════╝\n";

    private const string DescriptionHeader = "DESCRIPTION:";
    private const string DescriptionLine1 = "  Analyzes Entity Framework migrations across git branches";
    private const string DescriptionLine2 = "  to detect conflicts, schema changes, and incompatibilities.\n";

    private const string CommandsHeader = "COMMANDS:";
    private const string CompareCommandLine = "  compare <src> <tgt>    Compare migrations between branches";
    private const string ValidateCommandLine = "  validate               Validate migration file structure";
    private const string AnalyzeCommandLine = "  analyze                Analyze migration impact";
    private const string ReportCommandLine = "  report                 Generate detailed reports";
    private const string HelpCommandLine = "  help                   Show this help message\n";

    private const string OptionsHeader = "OPTIONS:";
    private const string FormatOptionLine = "  --format <fmt>         Output format (text/json/html)";
    private const string OutputOptionLine = "  --output <path>        Custom output directory";
    private const string VerboseOptionLine = "  --verbose, -v          Enable verbose output";
    private const string HelpOptionLine = "  --help, -h             Show help";
    private const string VersionOptionLine = "  --version, --ver       Show version\n";

    private const string ExamplesHeader = "EXAMPLES:";
    private const string ExampleCompare = "  ef-migration-diff compare develop main";
    private const string ExampleValidate = "  ef-migration-diff validate";
    private const string ExampleCompareJson = "  ef-migration-diff compare develop main --format json";
    private const string ExampleAnalyzeVerbose = "  ef-migration-diff analyze --verbose\n";

    private const string EnvironmentHeader = "ENVIRONMENT VARIABLES:";
    private const string EnvRepoLine = "  EF_MIGRATION_DIFF_REPO  Override repository path";
    private const string EnvOutputLine = "  EF_MIGRATION_DIFF_OUTPUT Override output directory\n";

    public string GetDescription() => Description;

    public async Task<CommandResult> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var helpText = new System.Text.StringBuilder();

        helpText.AppendLine(BannerTop);
        helpText.AppendLine(BannerTitle);
        helpText.AppendLine(BannerBottom);

        helpText.AppendLine(DescriptionHeader);
        helpText.AppendLine(DescriptionLine1);
        helpText.AppendLine(DescriptionLine2);

        helpText.AppendLine(CommandsHeader);
        helpText.AppendLine(CompareCommandLine);
        helpText.AppendLine(ValidateCommandLine);
        helpText.AppendLine(AnalyzeCommandLine);
        helpText.AppendLine(ReportCommandLine);
        helpText.AppendLine(HelpCommandLine);

        helpText.AppendLine(OptionsHeader);
        helpText.AppendLine(FormatOptionLine);
        helpText.AppendLine(OutputOptionLine);
        helpText.AppendLine(VerboseOptionLine);
        helpText.AppendLine(HelpOptionLine);
        helpText.AppendLine(VersionOptionLine);

        helpText.AppendLine(ExamplesHeader);
        helpText.AppendLine(ExampleCompare);
        helpText.AppendLine(ExampleValidate);
        helpText.AppendLine(ExampleCompareJson);
        helpText.AppendLine(ExampleAnalyzeVerbose);

        helpText.AppendLine(EnvironmentHeader);
        helpText.AppendLine(EnvRepoLine);
        helpText.AppendLine(EnvOutputLine);

        context.WriteOutput(helpText.ToString());

        return CommandResult.Ok("Help displayed");
    }
}
