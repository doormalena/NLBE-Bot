namespace WorldOfTanksBlitzApi.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class WotbAccountList
{
	[JsonInclude, JsonPropertyName("data")]
	public List<WotbAccountListItem> Data
	{
		get; internal set;
	}
}
