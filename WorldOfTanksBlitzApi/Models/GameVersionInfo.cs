namespace WorldOfTanksBlitzApi.Models;

using System.Text.Json.Serialization;

public class GameVersionInfo
{
	[JsonInclude, JsonPropertyName("name")]
	public string Name { get; internal set; } = string.Empty;

	[JsonInclude, JsonPropertyName("package")]
	public string Package { get; internal set; } = string.Empty;

	[JsonInclude, JsonPropertyName("vehicles")]
	public string Vehicles { get; internal set; } = string.Empty;
}
