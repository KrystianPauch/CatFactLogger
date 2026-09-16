using System.Text.Json.Serialization;

namespace CatFactLogger.Models;

public sealed class CatFact
{
    [JsonPropertyName("fact")]
    public string Fact { get; init; } = string.Empty;

    [JsonPropertyName("length")]
    public int Length { get; init; }

    // Returns a formatted line ready to append to the output file
    public string ToLogLine(DateTime timestamp) =>
        $"[{timestamp:yyyy-MM-dd HH:mm:ss}] [len:{Length,3}] {Fact}";

    public override string ToString() => $"(len:{Length}) {Fact}";
}