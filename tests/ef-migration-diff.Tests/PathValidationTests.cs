using System;
using EfMigrationDiff.CLI.Commands;
using Xunit;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Contains unit tests for path validation and sanitization in the CompareCommand.
    /// These tests verify that the --dot export option properly validates file paths
    /// to prevent directory traversal attacks and arbitrary file writes.
    /// </summary>
    public class PathValidationTests
    {
        [Fact]
        /// <summary>
        /// Tests that ValidateAndResolvePath correctly handles a valid relative path.
        /// Verifies that normal relative paths are properly resolved and validated.
        /// </summary>
        public void ValidateAndResolvePath_ValidRelativePath_ShouldReturnResolvedPath()
        {
            // Arrange
            var repositoryPath = "/home/user/project";
            var relativePath = "output.dot";

            // Act
            var resolvedPath = CompareCommand.ValidateAndResolvePath(relativePath, repositoryPath);

            // Assert
            Assert.NotNull(resolvedPath);
            Assert.EndsWith("output.dot", resolvedPath);
            Assert.StartsWith(repositoryPath, resolvedPath);
        }

        [Fact]
        /// <summary>
        /// Tests that ValidateAndResolvePath correctly handles a valid relative path with subdirectories.
        /// Verifies that relative paths with subdirectories are properly resolved and validated.
        /// </summary>
        public void ValidateAndResolvePath_ValidRelativePathWithSubdirectories_ShouldReturnResolvedPath()
        {
            // Arrange
            var repositoryPath = "/home/user/project";
            var relativePath = "graphs/output.dot";

            // Act
            var resolvedPath = CompareCommand.ValidateAndResolvePath(relativePath, repositoryPath);

            // Assert
            Assert.NotNull(resolvedPath);
            Assert.EndsWith("graphs/output.dot", resolvedPath);
            Assert.StartsWith(repositoryPath, resolvedPath);
        }

        [Fact]
        /// <summary>
        /// Tests that ValidateAndResolvePath rejects paths with directory traversal sequences (..).
        /// Verifies that paths containing ".." are rejected to prevent directory traversal attacks.
        /// </summary>
        public void ValidateAndResolvePath_PathWithTraversal_ShouldThrowArgumentException()
        {
            // Arrange
            var repositoryPath = "/home/user/project";
            var traversalPath = "../../etc/cron.d/evil";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => CompareCommand.ValidateAndResolvePath(traversalPath, repositoryPath));

            Assert.Contains("outside the repository directory", exception.Message);
            Assert.Contains("..", traversalPath);
        }

        [Fact]
        /// <summary>
        /// Tests that ValidateAndResolvePath rejects absolute paths.
        /// Verifies that absolute paths starting with "/" are rejected.
        /// </summary>
        public void ValidateAndResolvePath_AbsolutePath_ShouldThrowArgumentException()
        {
            // Arrange
            var repositoryPath = "/home/user/project";
            var absolutePath = "/etc/passwd";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => CompareCommand.ValidateAndResolvePath(absolutePath, repositoryPath));

            Assert.Contains("outside the repository directory", exception.Message);
        }

        [Fact]
        /// <summary>
        /// Tests that ValidateAndResolvePath rejects paths that would write outside the repository.
        /// Verifies that paths using ".." to escape the repository are rejected.
        /// </summary>
        public void ValidateAndResolvePath_PathOutsideRepository_ShouldThrowArgumentException()
        {
            // Arrange
            var repositoryPath = "/home/user/project";
            var outsidePath = "../other-project/output.dot";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => CompareCommand.ValidateAndResolvePath(outsidePath, repositoryPath));

            Assert.Contains("outside the repository directory", exception.Message);
        }

        [Fact]
        /// <summary>
        /// Tests that ValidateAndResolvePath rejects null path.
        /// Verifies that null paths throw ArgumentNullException.
        /// </summary>
        public void ValidateAndResolvePath_NullPath_ShouldThrowArgumentNullException()
        {
            // Arrange
            var repositoryPath = "/home/user/project";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => CompareCommand.ValidateAndResolvePath(null!, repositoryPath));
        }

        [Fact]
        /// <summary>
        /// Tests that ValidateAndResolvePath rejects empty path.
        /// Verifies that empty paths throw ArgumentException.
        /// </summary>
        public void ValidateAndResolvePath_EmptyPath_ShouldThrowArgumentException()
        {
            // Arrange
            var repositoryPath = "/home/user/project";

            // Act & Assert
            Assert.Throws<ArgumentException>(
                () => CompareCommand.ValidateAndResolvePath(string.Empty, repositoryPath));
        }

        [Fact]
        /// <summary>
        /// Tests that ValidateAndResolvePath rejects null repository path.
        /// Verifies that null repository paths throw ArgumentNullException.
        /// </summary>
        public void ValidateAndResolvePath_NullRepositoryPath_ShouldThrowArgumentNullException()
        {
            // Arrange
            var path = "output.dot";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => CompareCommand.ValidateAndResolvePath(path, null!));
        }

        [Fact]
        /// <summary>
        /// Tests that ValidateAndResolvePath rejects empty repository path.
        /// Verifies that empty repository paths throw ArgumentException.
        /// </summary>
        public void ValidateAndResolvePath_EmptyRepositoryPath_ShouldThrowArgumentException()
        {
            // Arrange
            var path = "output.dot";

            // Act & Assert
            Assert.Throws<ArgumentException>(
                () => CompareCommand.ValidateAndResolvePath(path, string.Empty));
        }

        [Fact]
        /// <summary>
        /// Tests that ValidateAndResolvePath handles paths with special characters that are valid.
        /// Verifies that paths with valid special characters like spaces and hyphens work correctly.
        /// </summary>
        public void ValidateAndResolvePath_PathWithValidSpecialCharacters_ShouldSucceed()
        {
            // Arrange
            var repositoryPath = "/home/user/my-project";
            var pathWithSpaces = "output files/graph-data.dot";

            // Act
            var resolvedPath = CompareCommand.ValidateAndResolvePath(pathWithSpaces, repositoryPath);

            // Assert
            Assert.NotNull(resolvedPath);
            Assert.EndsWith("output files/graph-data.dot", resolvedPath);
            Assert.StartsWith(repositoryPath, resolvedPath);
        }

        [Fact]
        /// <summary>
        /// Tests that ValidateAndResolvePath handles paths with backslash.
        /// Verifies that path normalization handles different path separators correctly.
        /// </summary>
        public void ValidateAndResolvePath_PathWithBackslash_ShouldNormalizeCorrectly()
        {
            // Arrange
            var repositoryPath = "/home/user/project";
            var backslashPath = "output\\file.dot";  // Backslash on Unix

            // Act
            var resolvedPath = CompareCommand.ValidateAndResolvePath(backslashPath, repositoryPath);

            // Assert - should normalize the backslash to forward slash
            Assert.NotNull(resolvedPath);
            Assert.EndsWith("output/file.dot", resolvedPath);
            Assert.StartsWith(repositoryPath, resolvedPath);
        }
    }
}
