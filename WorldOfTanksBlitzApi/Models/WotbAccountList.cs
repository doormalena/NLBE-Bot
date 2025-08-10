namespace FMWOTB.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class WotbAccountList
{
	[JsonPropertyName("data")]
	public List<WotbAccountListItem> Data
	{
		get; set;
	}
}
