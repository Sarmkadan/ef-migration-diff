using System;
using System.Collections.Generic;

namespace EfMigrationDiff.Exceptions
{
    /// <summary>
    /// Provides extension methods for <see cref="EfMigrationDiffException"/> to enhance error handling and reporting.
    /// </summary>
    public static class EfMigrationDiffExceptionExtensions
    {
        /// <summary>
        /// Formats the exception and all inner exceptions into a single, detailed message string.
        /// </summary>
        /// <param name="exception">The exception to format.</param>
        /// <returns>A formatted string containing the full exception chain.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
        public static string FormatDetailedMessage(this EfMigrationDiffException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            var messages = new List<string>();
            Exception? current = exception;
            while (current != null)
            {
                messages.Add($"[{current.GetType().Name}] {current.Message}");
                current = current.InnerException as EfMigrationDiffException;
            }

            return string.Join(Environment.NewLine, messages);
        }

        /// <summary>
        /// Extracts the root cause exception from the chain of inner exceptions.
        /// </summary>
        /// <param name="exception">The exception to analyze.</param>
        /// <returns>The deepest inner exception, or the original exception if no inner exceptions exist.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
        public static Exception GetRootCause(this EfMigrationDiffException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            Exception current = exception;
            while (current.InnerException != null)
            {
                current = current.InnerException;
            }

            return current;
        }

        /// <summary>
        /// Determines if the exception chain contains any <see cref="MigrationConflictException"/> instances.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>True if any inner exception is a <see cref="MigrationConflictException"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
        public static bool HasMigrationConflict(this EfMigrationDiffException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            Exception? current = exception;
            while (current != null)
            {
                if (current is MigrationConflictException)
                {
                    return true;
                }

                current = current.InnerException as EfMigrationDiffException;
            }

            return false;
        }
    }
}
