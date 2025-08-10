namespace FMWOTB.Models;

using System;
using System.Text.Json.Serialization;

public class WotbClanListItem
{
	[JsonPropertyName("name")]
	public string Name
	{
		get; set;
	}

	[JsonPropertyName("clan_id")]
	public long ClanId
	{
		get; set;
	}

	[JsonPropertyName("tag")]
	public string Tag
	{
		get; set;
	}

	[JsonPropertyName("members_count")]
	public int MembersCount
	{
		get; set;
	}

	[JsonPropertyName("created_at")]
	public DateTime? CreatedAt
	{
		get; set;
	}
}
