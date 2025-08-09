namespace FMWOTB.Models;
using System.Text.Json.Serialization;

public class PlayerListItem
{
	[JsonPropertyName("nickname")]
	public string Nickname
	{
		get; set;
	}

	[JsonPropertyName("account_id")]
	public long AccountId
	{
		get; set;
	}
}
