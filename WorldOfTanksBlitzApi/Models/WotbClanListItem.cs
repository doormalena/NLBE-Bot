namespace WorldOfTanksBlitzApi.Models;

using System;
using System.Text.Json.Serialization;
using WorldOfTanksBlitzApi.Tools;

public class WotbClanListItem
{
	[JsonInclude, JsonPropertyName("name")]
	public string Name
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("clan_id")]
	public long ClanId
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("tag")]
	public string Tag
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("members_count")]
	public int MembersCount
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("created_at")]
	[JsonConverter(typeof(UnixTimestampNullableConverter))]
	public DateTime? CreatedAt
	{
		get; internal set;
	}
}
