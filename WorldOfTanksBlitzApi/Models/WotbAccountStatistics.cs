namespace WorldOfTanksBlitzApi.Models;

using System.Text.Json.Serialization;

public class WotbAccountStatistics
{
	[JsonInclude, JsonPropertyName("all")]
	public WotbAccountStatisticsDetails All
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("clan")]
	public WotbAccountStatisticsDetails Clan
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("rating")]
	public WotbAccountStatisticsRating Rating
	{
		get; internal set;
	}
}
