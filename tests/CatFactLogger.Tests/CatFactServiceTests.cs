using System.Net;
using System.Text.Json;
using CatFactLogger.Models;
using CatFactLogger.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace CatFactLogger.Tests;

public class CatFactServiceTests
{
    private readonly CatFact _sampleFact = new()
    {
        Fact   = "Cats sleep 70% of their lives.",
        Length = 30
    };

    [Fact]
    public async Task GetFactAsync_WhenApiReturns200_ReturnsCatFact()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(_sampleFact));
        var logger     = new Mock<ILogger<CatFactService>>();
        var sut        = new CatFactService(httpClient, logger.Object);

        // Act
        var result = await sut.GetFactAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Fact.Should().Be(_sampleFact.Fact);
        result.Length.Should().Be(_sampleFact.Length);
    }

    [Fact]
    public async Task GetFactAsync_WhenApiReturnsError_ReturnsNull()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.InternalServerError, string.Empty);
        var logger     = new Mock<ILogger<CatFactService>>();
        var sut        = new CatFactService(httpClient, logger.Object);

        // Act
        var result = await sut.GetFactAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFactAsync_WhenApiReturnsInvalidJson_ReturnsNull()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK, "invalid json");
        var logger     = new Mock<ILogger<CatFactService>>();
        var sut        = new CatFactService(httpClient, logger.Object);

        // Act
        var result = await sut.GetFactAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFactAsync_WhenCancelled_ReturnsNull()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(_sampleFact));
        var logger     = new Mock<ILogger<CatFactService>>();
        var sut        = new CatFactService(httpClient, logger.Object);
        using var cts  = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await sut.GetFactAsync(cts.Token);

        // Assert
        result.Should().BeNull();
    }

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content    = new StringContent(content)
            });

        return new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://catfact.ninja/fact")
        };
    }
}