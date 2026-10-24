#nullable enable
namespace EfMigrationDiff.Models;

/// <summary>
/// Counts of schema changes grouped by kind, derived from a <see cref="MigrationDiff"/>.
/// Table and column counts are distinct by name; index counts are per change operation.
/// </summary>
/// <param name="TablesAdded">Distinct tables created (CREATE TABLE).</param>
/// <param name="TablesRemoved">Distinct tables dropped (DROP TABLE).</param>
/// <param name="TablesModified">Distinct tables altered (ALTER TABLE).</param>
/// <param name="ColumnsAdded">Distinct table/column pairs added.</param>
/// <param name="ColumnsRemoved">Distinct table/column pairs dropped.</param>
/// <param name="ColumnsModified">Distinct table/column pairs modified.</param>
/// <param name="IndexesChanged">Index operations (CREATE INDEX and DROP INDEX).</param>
public sealed record DiffStatistics(
    int TablesAdded,
    int TablesRemoved,
    int TablesModified,
    int ColumnsAdded,
    int ColumnsRemoved,
    int ColumnsModified,
    int IndexesChanged);
