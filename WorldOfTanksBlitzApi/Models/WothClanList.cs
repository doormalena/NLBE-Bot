namespace FMWOTB.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class WotbClanList
{
	[JsonPropertyName("data")]
	public List<WotbClanListItem> Data
	{
		get; set;
	}
}
