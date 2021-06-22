namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Extension methods for <see cref="MigrationServicesTests"/>.
    /// </summary>
    public static class MigrationServicesTestsExtensions
    {
        /// <summary>
        /// Asserts that all tests pass and returns a list of failed test names.
        /// </summary>
        /// <param name="testInstance">The instance of <see cref="MigrationServicesTests"/>.</param>
        /// <returns>A list of failed test names.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="testInstance"/> is null.</exception>
        public static IReadOnlyList<string> GetFailedTestNames(this MigrationServicesTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            var failedTestNames = new List<string>();

            testInstance.DetectChanges_WithCreateTableContent_DetectsOneCreateTableChange();
            testInstance.IsMigrationSafe_WithDropTableContent_ReturnsFalse();
            testInstance.DetectConflicts_WhenSameTableCreatedWithDifferentSchema_ReturnsNamingConflict();
            testInstance.DetectConflicts_WhenSameColumnModifiedWithDifferentDefaultValue_ReturnsColumnConflict();
            testInstance.DetectConflicts_WhenSameColumnModifiedWithSameDefaultValue_ReturnsNoConflicts();
            try
            {
                testInstance.ExecuteAsync_WithRegisteredMockedCommand_InvokesCommandExactlyOnce().Wait();
            }
            catch (AggregateException ex)
            {
                failedTestNames.Add(ex.InnerException.Message);
            }

            return failedTestNames.AsReadOnly();
        }

        /// <summary>
        /// Runs all tests and asserts that no exceptions are thrown.
        /// </summary>
        /// <param name="testInstance">The instance of <see cref="MigrationServicesTests"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="testInstance"/> is null.</exception>
        public static void RunAllTests(this MigrationServicesTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            testInstance.DetectChanges_WithCreateTableContent_DetectsOneCreateTableChange();
            testInstance.IsMigrationSafe_WithDropTableContent_ReturnsFalse();
            testInstance.DetectConflicts_WhenSameTableCreatedWithDifferentSchema_ReturnsNamingConflict();
            testInstance.DetectConflicts_WhenSameColumnModifiedWithDifferentDefaultValue_ReturnsColumnConflict();
            testInstance.DetectConflicts_WhenSameColumnModifiedWithSameDefaultValue_ReturnsNoConflicts();
            testInstance.ExecuteAsync_WithRegisteredMockedCommand_InvokesCommandExactlyOnce().Wait();
        }
    }
}
