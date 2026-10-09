#nullable enable

namespace EfMigrationDiff.Models;

/// <summary>
/// Provides outcome helpers for <see cref="MergeResult"/>.
/// </summary>
public static class MergeResultExtensions
{
    /// <summary>
    /// Returns <c>true</c> if every conflict in the merge run was auto-resolved
    /// (equivalent to <see cref="MergeResult.IsFullyResolved"/>).
    /// </summary>
    /// <param name="result">The merge result to evaluate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    /// <returns><c>true</c> if the merge succeeded; otherwise, <c>false</c>.</returns>
    public static bool IsSuccessful(this MergeResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsFullyResolved;
    }
}
