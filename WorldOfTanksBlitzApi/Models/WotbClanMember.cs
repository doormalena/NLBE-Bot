namespace FMWOTB.Models;

using System;
using System.Text.Json.Serialization;

public class WotbClanMember
{
	[JsonPropertyName("account_id")]
	public long AccountId
	{
		get; internal set;
	}

	[JsonPropertyName("account_name")]
	public string AccountName
	{
		get; internal set;
	}

	[JsonPropertyName("joined_at")]
	public DateTime? JoinedAt
	{
		get; internal set;
	}

	[JsonPropertyName("role")]
	public string Role
	{
		get; internal set;
	}
}
