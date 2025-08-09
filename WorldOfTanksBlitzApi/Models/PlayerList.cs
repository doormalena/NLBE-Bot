namespace FMWOTB.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class PlayerList
{
	[JsonPropertyName("data")]
	public List<PlayerListItem> Data
	{
		get; set;
	}
}
