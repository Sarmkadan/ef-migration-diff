# MigrationImpactAnalyzer

The `MigrationImpactAnalyzer` class evaluates the potential impact of Entity Framework Core migrations by analyzing schema changes, data loss risks, and behavioral modifications. It produces a comprehensive report with a risk score, severity level, and a list of detected issues. The analyzer can process a single migration or a chain of migrations, and it exposes properties that describe the analysis context and results.

## API

### `public MigrationImpactReport AnalyzeMigration()`
Executes an impact analysis on the migration specified by the `MigrationName` property.  
- **Returns**: A `MigrationImpactReport` containing the detailed findings for the migration.  
- **Throws**: `InvalidOperationException` if `MigrationName` is `null` or empty.  
- **Throws**: `MigrationNotFoundException` if the specified migration does not exist in the project.

### `public MigrationChainAnalysis AnalyzeMigrationChain()`
Performs a cumulative impact analysis across all migrations in the chain defined by the `MigrationReports` collection.  
- **Returns**: A `MigrationChainAnalysis` object that aggregates risks, counts high-risk migrations, and indicates whether critical risks exist.  
- **Throws**: `InvalidOperationException` if `MigrationReports` is `null` or empty.

### `public string MigrationName { get; set; }`
Gets or sets the name of the migration to analyze. This property must be set before calling `AnalyzeMigration()`.

### `public DateTime AnalyzedAt { get; }`
The timestamp when the last analysis was performed. Updated automatically by `AnalyzeMigration()` and `AnalyzeMigrationChain()`.

### `public List<MigrationIssue> IssuesDetected { get; }`
A list of `MigrationIssue` objects identified during the most recent analysis. Each issue contains details such as severity, message, and line number.

### `public double RiskScore { get; }`
A numeric score (0.0 to 1.0) representing the overall risk of the analyzed migration or chain. Higher values indicate greater risk.

### `public RiskLevel RiskLevel { get; }`
The categorical risk level derived from the `RiskScore`. Possible values are defined by the `RiskLevel` enum (e.g., Low, Medium, High, Critical).

### `public IssueSeverity Severity { get; set; }`
Gets or sets the severity level of the analyzer’s own operational state. This property can be used to flag warnings or errors that occurred during analysis setup (e.g., missing dependencies).

### `public string Message { get; set; }`
A human-readable message associated with the analyzer’s current state. Typically used to describe a warning or error condition.

### `public int LineNumber { get; set; }`
An optional line number reference (e.g., in a configuration file or script) that relates to the `Message` or `Severity` state.

### `public List<MigrationImpactReport> MigrationReports { get; set; }`
A collection of `MigrationImpactReport` objects representing individual migration analyses. This property must be populated before calling `AnalyzeMigrationChain()`.

### `public int TotalMigrations { get; }`
The total number of migrations included in the last chain analysis. Derived from the `MigrationReports` collection.

### `public int HighRiskCount { get; }`
The number of migrations in the chain that were classified as high or critical risk during the last chain analysis.

### `public bool HasCriticalRisks { get; }`
Indicates whether any migration in the chain contains at least one critical-risk issue.

### `public double GetAverageRiskScore()`
Calculates the mean risk score across all migrations in the `MigrationReports` collection.  
- **Returns**: The average risk score as a `double`.  
- **Throws**: `InvalidOperationException` if `MigrationReports` is `null` or empty.

## Usage

### Example 1: Analyze a single migration

```csharp
var analyzer = new MigrationImpactAnalyzer
{
    MigrationName = "20250315_AddUserTable"
};

MigrationImpactReport report = analyzer.AnalyzeMigration();

Console.WriteLine($"Analyzed at: {analyzer.AnalyzedAt}");
Console.WriteLine($"Risk score: {analyzer.RiskScore:F2} ({analyzer.RiskLevel})");
Console.WriteLine($"Issues detected: {analyzer.IssuesDetected.Count}");

foreach (var issue in analyzer.IssuesDetected)
{
    Console.WriteLine($"  [{issue.Severity}] {issue.Message} (line {issue.LineNumber})");
}
```

### Example 2: Analyze a chain of migrations and retrieve aggregate metrics

```csharp
var analyzer = new MigrationImpactAnalyzer();

// Simulate loading reports from previous analyses
analyzer.MigrationReports = new List<MigrationImpactReport>
{
    new MigrationImpactReport { RiskScore = 0.2 },
    new MigrationImpactReport { RiskScore = 0.8 },
    new MigrationImpactReport { RiskScore = 0.95 }
};

MigrationChainAnalysis chain = analyzer.AnalyzeMigrationChain();

Console.WriteLine($"Total migrations: {analyzer.TotalMigrations}");
Console.WriteLine($"High risk count: {analyzer.HighRiskCount}");
Console.WriteLine($"Has critical risks: {analyzer.HasCriticalRisks}");
Console.WriteLine($"Average risk score: {analyzer.GetAverageRiskScore():F2}");
```

## Notes

- **Edge cases**:  
  - If `MigrationName` is set to a value that does not correspond to an existing migration, `AnalyzeMigration()` throws `MigrationNotFoundException`.  
  - Calling `AnalyzeMigrationChain()` with an empty or null `MigrationReports` list throws `InvalidOperationException`.  
  - `GetAverageRiskScore()` returns `0` if the `MigrationReports` list contains only entries with a `RiskScore` of `0`; it does not throw for zero-length lists (throws only for null/empty).  
  - The `Severity`, `Message`, and `LineNumber` properties are intended for diagnostic purposes and do not affect the analysis results. They can be set independently of the analysis methods.

- **Thread safety**:  
  Instances of `MigrationImpactAnalyzer` are not thread-safe. Concurrent calls to `AnalyzeMigration()` or `AnalyzeMigrationChain()` from multiple threads may produce inconsistent state. External synchronization is required if the same instance is shared across threads.
