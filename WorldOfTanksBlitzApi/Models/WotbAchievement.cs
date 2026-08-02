namespace WorldOfTanksBlitzApi.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class WotbAchievement
{
	[JsonInclude, JsonPropertyName("achievement_id")]
	public string AchievementId
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("condition")]
	public string Condition
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("description")]
	public string Description
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("image")]
	public string Image
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("image_big")]
	public string ImageBig
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("name")]
	public string Name
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("order")]
	public int? Order
	{
		get; internal set;
	}

	[JsonInclude, JsonPropertyName("section")]
	public string Section
	{
		get; internal set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("options")]
	public List<WotbAchievementOption>? Options
	{
		get; internal set;
	}

	public int WotInspectorId
	{
		get; internal set;
	}
}
