using CatFactLogger.Models;
using FluentAssertions;

namespace CatFactLogger.Tests;

public class CatFactModelTests
{
    [Fact]
    public void ToLogLine_ContainsFactText()
    {
        // Arrange
        var fact = new CatFact { Fact = "Cats purr at 25Hz.", Length = 18 };

        // Act
        var line = fact.ToLogLine(DateTime.Now);

        // Assert
        line.Should().Contain("Cats purr at 25Hz.");
    }

    [Fact]
    public void ToLogLine_ContainsTimestamp()
    {
        // Arrange
        var fact      = new CatFact { Fact = "Test.", Length = 5 };
        var timestamp = new DateTime(2025, 9, 17, 14, 00, 1);

        // Act
        var line = fact.ToLogLine(timestamp);

        // Assert
        line.Should().StartWith("[2025-09-17 14:00:01]");
    }

    [Fact]
    public void ToLogLine_ContainsLength()
    {
        // Arrange
        var fact = new CatFact { Fact = "Test.", Length = 42 };

        // Act
        var line = fact.ToLogLine(DateTime.Now);

        // Assert
        line.Should().Contain("42");
    }

    [Fact]
    public void ToString_ContainsFactText()
    {
        // Arrange
        var fact = new CatFact { Fact = "Cats have 32 ear muscles.", Length = 25 };

        // Act
        var result = fact.ToString();

        // Assert
        result.Should().Contain("Cats have 32 ear muscles.");
    }

    [Fact]
    public void Fact_DefaultValue_IsEmptyString()
    {
        // Arrange & Act
        var fact = new CatFact();

        // Assert
        fact.Fact.Should().BeEmpty();
    }
}