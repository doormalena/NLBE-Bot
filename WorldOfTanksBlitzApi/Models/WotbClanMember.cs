namespace WorldOfTanksBlitzApi.Models;

using System;
using System.Text.Json.Serialization;
using WorldOfTanksBlitzApi.Tools;

public class WotbClanMember
{
	[JsonInclude, JsonPropertyName("account_id")]
	public long AccountId
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("account_name")]
	public string AccountName
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("joined_at")]
	[JsonConverter(typeof(UnixTimestampNullableConverter))]
	public DateTime? JoinedAt
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("role")]
	public string Role
	{
		get; internal set;
	}
}
