namespace WorldOfTanksBlitzApi.Models;

using System;
using System.Text.Json.Serialization;
using WorldOfTanksBlitzApi.Account.Statistics;
using WorldOfTanksBlitzApi.Tools;

public class WotbAccountInfo : WotbAccountListItem
{
	[JsonInclude, JsonPropertyName("created_at")]
	[JsonConverter(typeof(UnixTimestampConverter))]
	public DateTime? CreatedAt
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("last_battle_time")]
	[JsonConverter(typeof(UnixTimestampConverter))]
	public DateTime? LastBattleTime
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("updated_at")]
	[JsonConverter(typeof(UnixTimestampConverter))]
	public DateTime? UpdatedAt
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("statistics")]
	public Statistics Statistics
	{
		get; internal set;
	}

	public string BlitzStars => AccountId > 0 ? "https://www.blitzstars.com/sigs/" + AccountId : null;
}
