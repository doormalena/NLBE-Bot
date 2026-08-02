namespace WorldOfTanksBlitzApi.Models;

using System.Text.Json.Serialization;

public class WotbAchievementOption
{
	[JsonInclude, JsonPropertyName("description")]
	public string Description
	{
		get; private set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("image")]
	public string Image
	{
		get; private set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("image_big")]
	public string ImageBig
	{
		get; private set;
	} = string.Empty;

	[JsonInclude, JsonPropertyName("name")]
	public string Name
	{
		get; private set;
	} = string.Empty;
}
