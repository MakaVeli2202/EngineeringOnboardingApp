using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EngineeringOnboardingApp.Models;

public class VsConfig
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("components")]
    public List<string> Components { get; set; } = new();

    [JsonPropertyName("extensions")]
    public List<string> Extensions { get; set; } = new();
}
