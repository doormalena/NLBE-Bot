namespace WorldOfTanksBlitzApi.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class WotbClanInfo : WotbClanListItem
{
	[JsonInclude, JsonPropertyName("members_ids")]
	public List<int> MemberIds
	{
		get; internal set;
	} = [];

	[JsonInclude, JsonPropertyName("motto")]
	public string Motto
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("description")]
	public string Description
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("members")]
	public List<WotbClanMember> Members
	{
		get; internal set;
	} = [];
}
