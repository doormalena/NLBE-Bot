namespace FMWOTB.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class WotbClanInfo : WotbClanListItem
{
	[JsonPropertyName("members_ids")]
	public List<int> MemberIds
	{
		get; internal set;
	}

	[JsonPropertyName("motto")]
	public string Motto
	{
		get; internal set;
	}

	[JsonPropertyName("description")]
	public string Description
	{
		get; internal set;
	}

	[JsonPropertyName("members")]
	public List<WotbClanMember> Members
	{
		get; internal set;
	}
}
