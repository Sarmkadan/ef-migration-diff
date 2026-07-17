namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Extension methods for <see cref="MigrationServicesTests"/>.
    /// </summary>
    public static class MigrationServicesTestsExtensions
    {
        /// <summary>
        /// Runs all tests and returns a list of failed test names.
        /// </summary>
        /// <param name="testInstance">The instance of <see cref="MigrationServicesTests"/>.</param>
        /// <returns>A list of failed test names. Empty if all tests pass.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="testInstance"/> is null.</exception>
        public static IReadOnlyList<string> GetFailedTestNames(this MigrationServicesTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            var failedTestNames = new List<string>();

            try
            {
                testInstance.DetectChanges_WithCreateTableContent_DetectsOneCreateTableChange();
            }
            catch (Exception ex)
            {
                failedTestNames.Add($"DetectChanges_WithCreateTableContent_DetectsOneCreateTableChange: {ex.Message}");
            }

            try
            {
                testInstance.IsMigrationSafe_WithDropTableContent_ReturnsFalse();
            }
            catch (Exception ex)
            {
                failedTestNames.Add($"IsMigrationSafe_WithDropTableContent_ReturnsFalse: {ex.Message}");
            }

            try
            {
                testInstance.DetectConflicts_WhenSameTableCreatedWithDifferentSchema_ReturnsNamingConflict();
            }
            catch (Exception ex)
            {
                failedTestNames.Add($"DetectConflicts_WhenSameTableCreatedWithDifferentSchema_ReturnsNamingConflict: {ex.Message}");
            }

            try
            {
                testInstance.DetectConflicts_WhenSameColumnModifiedWithDifferentDefaultValue_ReturnsColumnConflict();
            }
            catch (Exception ex)
            {
                failedTestNames.Add($"DetectConflicts_WhenSameColumnModifiedWithDifferentDefaultValue_ReturnsColumnConflict: {ex.Message}");
            }

            try
            {
                testInstance.DetectConflicts_WhenSameColumnModifiedWithSameDefaultValue_ReturnsNoConflicts();
            }
            catch (Exception ex)
            {
                failedTestNames.Add($"DetectConflicts_WhenSameColumnModifiedWithSameDefaultValue_ReturnsNoConflicts: {ex.Message}");
            }

            try
            {
                testInstance.ExecuteAsync_WithRegisteredMockedCommand_InvokesCommandExactlyOnce().Wait();
            }
            catch (Exception ex)
            {
                failedTestNames.Add($"ExecuteAsync_WithRegisteredMockedCommand_InvokesCommandExactlyOnce: {ex.Message}");
            }

            return failedTestNames.AsReadOnly();
        }

        /// <summary>
        /// Runs all tests and asserts that no exceptions are thrown.
        /// </summary>
        /// <param name="testInstance">The instance of <see cref="MigrationServicesTests"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="testInstance"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when any test fails.</exception>
        public static void RunAllTests(this MigrationServicesTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            var failedTests = testInstance.GetFailedTestNames();

            if (failedTests.Count > 0)
            {
                throw new InvalidOperationException(
                    $"One or more tests failed:{Environment.NewLine}{string.Join(Environment.NewLine, failedTests)}");
            }
        }
    }
}
