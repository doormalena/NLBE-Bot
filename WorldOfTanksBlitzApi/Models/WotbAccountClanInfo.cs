namespace WorldOfTanksBlitzApi.Models;

using System.Text.Json.Serialization;

public class WotbAccountClanInfo : WotbClanMember
{
	[JsonInclude, JsonPropertyName("clan_id")]
	public long? ClanId
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("clan")]
	public WotbClanInfo Clan
	{
		get; internal set;
	}
}
