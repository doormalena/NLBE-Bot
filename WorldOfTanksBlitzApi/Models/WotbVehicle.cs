namespace WorldOfTanksBlitzApi.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class WotbVehicle
{
	[JsonInclude, JsonPropertyName("tank_id")]
	public long TankId
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("name")]
	public string Name { get; internal set; } = string.Empty;

	[JsonInclude, JsonPropertyName("description")]
	public string Description { get; internal set; } = string.Empty;

	[JsonInclude, JsonPropertyName("tier")]
	public int Tier
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("type")]
	public string Type { get; internal set; } = string.Empty;

	[JsonInclude, JsonPropertyName("nation")]
	public string Nation { get; internal set; } = string.Empty;

	[JsonInclude, JsonPropertyName("is_premium")]
	public bool IsPremium
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("engines")]
	public List<int> Engines { get; internal set; } = [];

	[JsonInclude, JsonPropertyName("guns")]
	public List<int> Guns { get; internal set; } = [];

	[JsonInclude, JsonPropertyName("suspensions")]
	public List<int> Suspensions { get; internal set; } = [];

	[JsonInclude, JsonPropertyName("turrets")]
	public List<int> Turrets { get; internal set; } = [];
}
