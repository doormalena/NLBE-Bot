namespace WorldOfTanksBlitzApi.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class WotbClanList
{
	[JsonInclude, JsonPropertyName("data")]
	public List<WotbClanListItem> Data
	{
		get; internal set;
	}
}
