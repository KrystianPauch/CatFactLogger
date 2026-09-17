using CatFactLogger.Configuration;
using CatFactLogger.Models;
using CatFactLogger.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CatFactLogger.Tests;

public class FileLoggerServiceTests : IDisposable
{
    private readonly string _tempFilePath;
    private readonly FileLoggerService _sut;

    public FileLoggerServiceTests()
    {
        _tempFilePath = Path.Combine(
            Path.GetTempPath(), 
            $"catfact_test_{Guid.NewGuid():N}.txt");

        var settings = Options.Create(new AppSettings
        {
            OutputFilePath = _tempFilePath
        });

        var logger = new Mock<ILogger<FileLoggerService>>();
        _sut = new FileLoggerService(settings, logger.Object);
    }

    [Fact]
    public async Task AppendFactAsync_CreatesFileIfNotExists()
    {
        // Arrange
        var fact = new CatFact { Fact = "Cats sleep a lot.", Length = 17 };

        // Act
        await _sut.AppendFactAsync(fact);

        // Assert
        File.Exists(_tempFilePath).Should().BeTrue();
    }

    [Fact]
    public async Task AppendFactAsync_ContainsFactText()
    {
        // Arrange
        var fact = new CatFact { Fact = "Unique fact XYZ123.", Length = 19 };

        // Act
        await _sut.AppendFactAsync(fact);

        // Assert
        var content = await File.ReadAllTextAsync(_tempFilePath);
        content.Should().Contain("Unique fact XYZ123.");
    }

    [Fact]
    public async Task AppendFactAsync_AppendsMultipleLines()
    {
        // Arrange
        var fact1 = new CatFact { Fact = "First fact.", Length = 11 };
        var fact2 = new CatFact { Fact = "Second fact.", Length = 12 };

        // Act
        await _sut.AppendFactAsync(fact1);
        await _sut.AppendFactAsync(fact2);

        // Assert
        var lines = (await File.ReadAllLinesAsync(_tempFilePath))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        lines.Should().HaveCount(2);
        lines.Should().Contain(l => l.Contains("First fact."));
        lines.Should().Contain(l => l.Contains("Second fact."));
    }

    [Fact]
    public void GetOutputFilePath_ReturnsAbsolutePath()
    {
        // Act
        var path = _sut.GetOutputFilePath();

        // Assert
        Path.IsPathRooted(path).Should().BeTrue();
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (File.Exists(_tempFilePath))
            File.Delete(_tempFilePath);
    }
}