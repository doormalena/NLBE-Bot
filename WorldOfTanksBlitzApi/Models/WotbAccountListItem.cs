namespace WorldOfTanksBlitzApi.Models;

using System.Text.Json.Serialization;

public class WotbAccountListItem
{
	[JsonInclude, JsonPropertyName("nickname")]
	public string Nickname
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("account_id")]
	public long AccountId
	{
		get; internal set;
	}
}
