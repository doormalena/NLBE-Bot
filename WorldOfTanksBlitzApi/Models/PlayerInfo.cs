namespace FMWOTB.Models;

using FMWOTB.Account.Statistics;
using FMWOTB.Clans;
using System;
using System.Text.Json.Serialization;

public class PlayerInfo
{
	[JsonPropertyName("account_id")]
	public long AccountId
	{
		get; set;
	}

	[JsonPropertyName("clan_id")]
	public long ClanId
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

	[JsonPropertyName("nickname")]
	public string Nickname
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

	public WGClan Clan // TODO: move this to a separate repository.
	{
		get; set;
	}

	public string BlitzStars => AccountId > 0 ? "https://www.blitzstars.com/sigs/" + AccountId : null;
}
