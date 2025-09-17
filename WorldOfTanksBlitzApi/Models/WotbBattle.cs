namespace WorldOfTanksBlitzApi.Models;

using System.Text.Json.Serialization;

public class WotbBattle
{
	[JsonInclude, JsonPropertyName("view_url")]
	public string ViewUrl
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("summary")]
	public WotbBattleSummary Summary
	{
		get; internal set;
	} = new();
}
