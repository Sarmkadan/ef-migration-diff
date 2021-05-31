#nullable enable

using EfMigrationDiff.Models;
using EfMigrationDiff.Repositories;
using FluentAssertions;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Provides unit tests for the <see cref="MigrationRepository"/> class to verify migration storage and retrieval functionality.
/// </summary>
public class MigrationRepositoryTests
{
    private readonly MigrationRepository _repository = new();

    [Fact]
    public void Add_WithValidMigration_StoresMigration()
    {
        /// <summary>
        /// Tests that adding a valid migration with all required fields stores it correctly in the repository.
        /// </summary>
        /// <remarks>
        /// This test verifies that the repository properly stores migrations when they have valid IDs, names, and database context names.
        /// It ensures the basic CRUD operation of adding data works as expected.
        /// </remarks>
        // Arrange
        var migration = new Migration("20240115093045", "CreateUsers", "AppDbContext")
        {
            Content = "migrationBuilder.CreateTable(...)"
        };

        // Act
        _repository.Add(migration);

        // Assert
        var retrieved = _repository.GetById("20240115093045");
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("CreateUsers");
    }

    [Fact]
    public void Add_WithInvalidMigration_ThrowsException()
    {
        /// <summary>
        /// Tests that adding a migration with invalid data throws an appropriate exception.
        /// </summary>
        /// <remarks>
        /// This test verifies that the repository validates input data and rejects migrations that don't meet the minimum requirements.
        /// It ensures data integrity by preventing invalid or incomplete migration records from being stored.
        /// </remarks>
        // Arrange
        var migration = new Migration();

        // Act & Assert
        var act = () => _repository.Add(migration);
        act.Should().ThrowExactly<ArgumentException>()
            .WithMessage("*valid*");
    }

    [Fact]
    public void Add_WithDuplicateId_ThrowsException()
    {
        /// <summary>
        /// Tests that adding a migration with a duplicate ID throws an InvalidOperationException.
        /// </summary>
        /// <remarks>
        /// This test ensures that the repository enforces unique constraints on migration IDs.
        /// Each migration must have a unique identifier to prevent conflicts and ensure proper tracking.
        /// </remarks>
        // Arrange
        var migration1 = new Migration("20240115093045", "CreateUsers", "AppDbContext");
        var migration2 = new Migration("20240115093045", "DifferentName", "AppDbContext");

        // Act
        _repository.Add(migration1);
        var act = () => _repository.Add(migration2);

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void GetById_WithValidId_ReturnsMigration()
    {
        /// <summary>
        /// Tests that retrieving a migration by its valid ID returns the correct migration object.
        /// </summary>
        /// <remarks>
        /// This test verifies that the repository can correctly retrieve stored migrations using their unique identifiers.
        /// It ensures the basic read operation works as expected after migrations have been added to the repository.
        /// </remarks>
        // Arrange
        var migration = new Migration("20240115093045", "CreateUsers", "AppDbContext");
        _repository.Add(migration);

        // Act
        var result = _repository.GetById("20240115093045");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("20240115093045");
    }

    [Fact]
    public void GetById_WithNonexistentId_ReturnsNull()
    {
        /// <summary>
        /// Tests that retrieving a migration by a non-existent ID returns null instead of throwing an exception.
        /// </summary>
        /// <remarks>
        /// This test verifies graceful handling of requests for non-existent resources.
        /// The repository should return null rather than throwing exceptions for missing IDs to maintain consistent API behavior.
        /// </remarks>
        // Act
        var result = _repository.GetById("99999999999999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetByDbContext_WithMultipleMigrations_ReturnsOnlyMatchingContext()
    {
        /// <summary>
        /// Tests that retrieving migrations by database context name returns only migrations for that specific context.
        /// </summary>
        /// <remarks>
        /// This test verifies that the repository can filter migrations by their associated database context.
        /// It ensures that when working with multiple database contexts, migrations are properly isolated and can be retrieved context-specifically.
        /// </remarks>
        // Arrange
        _repository.Add(new Migration("20240115093045", "Mig1", "DbContext1"));
        _repository.Add(new Migration("20240115093046", "Mig2", "DbContext1"));
        _repository.Add(new Migration("20240115093047", "Mig3", "DbContext2"));

        // Act
        var result = _repository.GetByDbContext("DbContext1");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(m => m.DbContextName.Should().Be("DbContext1"));
    }

    [Fact]
    public void GetByDbContext_WithNonexistentContext_ReturnsEmptyList()
    {
        /// <summary>
        /// Tests that retrieving migrations by a non-existent database context returns an empty collection.
        /// </summary>
        /// <remarks>
        /// This test verifies that the repository handles queries for non-existent database contexts gracefully.
        /// It should return an empty collection rather than throwing exceptions, maintaining consistent API behavior.
        /// </remarks>
        // Arrange
        _repository.Add(new Migration("20240115093045", "Mig1", "DbContext1"));

        // Act
        var result = _repository.GetByDbContext("NonexistentContext");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetByStatus_WithMigrationsHavingDifferentStatuses_ReturnsFiltered()
    {
        /// <summary>
        /// Tests that retrieving migrations by status returns only migrations with the specified status.
        /// </summary>
        /// <remarks>
        /// This test verifies that the repository can filter migrations by their status (e.g., Pending, Applied).
        /// It ensures that migration tracking can distinguish between different states of migrations during the migration process.
        /// </remarks>
        // Arrange
        var mig1 = new Migration("20240115093045", "Mig1", "DbContext1") { Status = MigrationStatus.Pending };
        var mig2 = new Migration("20240115093046", "Mig2", "DbContext1") { Status = MigrationStatus.Applied };
        var mig3 = new Migration("20240115093047", "Mig3", "DbContext1") { Status = MigrationStatus.Pending };

        _repository.Add(mig1);
        _repository.Add(mig2);
        _repository.Add(mig3);

        // Act
        var result = _repository.GetByStatus(MigrationStatus.Pending);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(m => m.Status.Should().Be(MigrationStatus.Pending));
    }

    [Fact]
    public void Update_WithExistingMigration_UpdatesContent()
    {
        /// <summary>
        /// Tests that updating an existing migration with new content successfully replaces the stored content.
        /// </summary>
        /// <remarks>
        /// This test verifies that the repository supports updating existing migration records.
        /// It ensures that migration content can be modified after initial creation, which is useful for correcting or updating migrations.
        /// </remarks>
        // Arrange
        var migration = new Migration("20240115093045", "CreateUsers", "AppDbContext")
        {
            Content = "original content"
        };
        _repository.Add(migration);

        var updated = new Migration("20240115093045", "CreateUsers", "AppDbContext")
        {
            Content = "updated content"
        };

        // Act
        _repository.Update(updated);

        // Assert
        var result = _repository.GetById("20240115093045");
        result!.Content.Should().Be("updated content");
    }

    [Fact]
    public void Update_WithNonexistentMigration_ThrowsException()
    {
        /// <summary>
        /// Tests that attempting to update a non-existent migration throws a KeyNotFoundException.
        /// </summary>
        /// <remarks>
        /// This test verifies that the repository properly validates update operations.
        /// Attempting to update a migration that doesn't exist should result in an exception rather than silently failing.
        /// </remarks>
        // Arrange
        var migration = new Migration("99999999999999", "NonExistent", "DbContext");

        // Act & Assert
        var act = () => _repository.Update(migration);
        act.Should().ThrowExactly<KeyNotFoundException>();
    }

    [Fact]
    public void Delete_WithExistingMigration_RemovesMigration()
    {
        /// <summary>
        /// Tests that deleting an existing migration removes it from the repository and returns true.
        /// </summary>
        /// <returns>True if the migration was successfully deleted.</returns>
        /// <remarks>
        /// This test verifies that the repository supports deletion of migration records.
        /// It ensures that migrations can be removed when they are no longer needed or when they need to be replaced.
        /// </remarks>
        // Arrange
        var migration = new Migration("20240115093045", "CreateUsers", "AppDbContext");
        _repository.Add(migration);

        // Act
        var result = _repository.Delete("20240115093045");

        // Assert
        result.Should().BeTrue();
        _repository.GetById("20240115093045").Should().BeNull();
    }

    [Fact]
    public void Delete_WithNonexistentId_ReturnsFalse()
    {
        /// <summary>
        /// Tests that attempting to delete a non-existent migration returns false instead of throwing an exception.
        /// </summary>
        /// <returns>False if the migration ID does not exist in the repository.</returns>
        /// <remarks>
        /// This test verifies that the repository handles deletion of non-existent resources gracefully.
        /// It should return false rather than throwing exceptions, maintaining consistent API behavior.
        /// </remarks>
        // Act
        var result = _repository.Delete("99999999999999");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetAll_ReturnsAllAddedMigrations()
    {
        /// <summary>
        /// Tests that retrieving all migrations returns the complete collection of stored migrations.
        /// </summary>
        /// <returns>A collection containing all migrations stored in the repository.</returns>
        /// <remarks>
        /// This test verifies that the repository can return all stored migrations at once.
        /// It ensures that consumers can get a complete view of all migrations in the system.
        /// </remarks>
        // Arrange
        _repository.Add(new Migration("20240115093045", "Mig1", "DbContext1"));
        _repository.Add(new Migration("20240115093046", "Mig2", "DbContext2"));
        _repository.Add(new Migration("20240115093047", "Mig3", "DbContext1"));

        // Act
        var result = _repository.GetAll();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public void ConcurrentAdd_WithMultipleThreads_AllMigrationsStored()
    {
        /// <summary>
        /// Tests that adding migrations from multiple concurrent threads successfully stores all migrations.
        /// </summary>
        /// <remarks>
        /// This test verifies that the repository is thread-safe and can handle concurrent write operations.
        /// It ensures that multiple threads can add migrations simultaneously without data corruption or loss.
        /// </remarks>
        // Arrange
        const int threadCount = 10;
        const int migrationsPerThread = 5;
        var threads = new List<Thread>();

        // Act
        for (int t = 0; t < threadCount; t++)
        {
            var threadId = t;
            var thread = new Thread(() =>
            {
                for (int i = 0; i < migrationsPerThread; i++)
                {
                    var mig = new Migration(
                        $"20240115{threadId:D2}{i:D2}000000",
                        $"Mig_T{threadId}_M{i}",
                        "DbContext"
                    ) { Content = "migrationBuilder.CreateTable(...)" };
                    _repository.Add(mig);
                }
            });
            threads.Add(thread);
            thread.Start();
        }

        foreach (var thread in threads)
            thread.Join();

        // Assert
        _repository.GetAll().Should().HaveCount(threadCount * migrationsPerThread);
    }

    [Fact]
    public void ConcurrentGet_WithMultipleThreads_ReturnsConsistentResults()
    {
        /// <summary>
        /// Tests that retrieving migrations from multiple concurrent threads returns consistent results.
        /// </summary>
        /// <remarks>
        /// This test verifies that the repository handles concurrent read operations correctly.
        /// It ensures that multiple threads can safely read migration data simultaneously without data corruption.
        /// </remarks>
        // Arrange
        var migration = new Migration("20240115093045", "TestMigration", "DbContext");
        _repository.Add(migration);

        var results = new List<Migration?>();
        var threads = new List<Thread>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var thread = new Thread(() =>
            {
                var result = _repository.GetById("20240115093045");
                lock (results)
                {
                    results.Add(result);
                }
            });
            threads.Add(thread);
            thread.Start();
        }

        foreach (var thread in threads)
            thread.Join();

        // Assert
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
        results.Should().AllSatisfy(r => r!.Id.Should().Be("20240115093045"));
    }

    [Fact]
    public void Clear_RemovesAllMigrations()
    {
        /// <summary>
        /// Tests that clearing the repository removes all stored migrations.
        /// </summary>
        /// <remarks>
        /// This test verifies that the repository supports clearing all data at once.
        /// It ensures that the repository can be reset to an empty state when needed.
        /// </remarks>
        // Arrange
        _repository.Add(new Migration("20240115093045", "Mig1", "DbContext"));
        _repository.Add(new Migration("20240115093046", "Mig2", "DbContext"));

        // Act
        _repository.Clear();

        // Assert
        _repository.GetAll().Should().BeEmpty();
    }
}
