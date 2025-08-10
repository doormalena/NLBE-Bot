namespace FMWOTB.Models;

using FMWOTB.Account.Statistics;
using System;
using System.Text.Json.Serialization;

public class WotbAccountInfo : WotbAccountListItem
{
	[JsonPropertyName("clan_id")]
	public long? ClanId
	{
		get; set;
	}

	[JsonPropertyName("created_at")]
	public DateTime? CreatedAt
	{
		get; set;
	}

	[JsonPropertyName("last_battle_time")]
	public DateTime? LastBattleTime
	{
		get; set;
	}

	[JsonPropertyName("updated_at")]
	public DateTime? UpdatedAt
	{
		get; set;
	}

	[JsonPropertyName("statistics")]
	public Statistics Statistics
	{
		get; set;
	}

	public string BlitzStars => AccountId > 0 ? "https://www.blitzstars.com/sigs/" + AccountId : null;
}
