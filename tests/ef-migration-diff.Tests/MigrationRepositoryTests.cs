#nullable enable

using EfMigrationDiff.Models;
using EfMigrationDiff.Repositories;
using FluentAssertions;

namespace EfMigrationDiff.Tests;

public class MigrationRepositoryTests
{
    private readonly MigrationRepository _repository = new();

    [Fact]
    public void Add_WithValidMigration_StoresMigration()
    {
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
        // Act
        var result = _repository.GetById("99999999999999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetByDbContext_WithMultipleMigrations_ReturnsOnlyMatchingContext()
    {
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
        // Arrange
        var migration = new Migration("99999999999999", "NonExistent", "DbContext");

        // Act & Assert
        var act = () => _repository.Update(migration);
        act.Should().ThrowExactly<KeyNotFoundException>();
    }

    [Fact]
    public void Delete_WithExistingMigration_RemovesMigration()
    {
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
        // Act
        var result = _repository.Delete("99999999999999");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetAll_ReturnsAllAddedMigrations()
    {
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
                        $"2024011509304{threadId}{i}",
                        $"Mig_T{threadId}_M{i}",
                        "DbContext"
                    );
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
        // Arrange
        _repository.Add(new Migration("20240115093045", "Mig1", "DbContext"));
        _repository.Add(new Migration("20240115093046", "Mig2", "DbContext"));

        // Act
        _repository.Clear();

        // Assert
        _repository.GetAll().Should().BeEmpty();
    }
}
