namespace WorldOfTanksBlitzApi.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class MapInfo
{
	[JsonInclude, JsonPropertyName("id")]
	public int Id
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("key")]
	public string Key
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("name")]
	public string Name
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("localization_code")]
	public string LocalizationCode
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("modes")]
	public List<int> Modes
	{
		get; internal set;
	} = [];

	public string? ImageUrl
	{
		get; set;
	} = string.Empty;
}
