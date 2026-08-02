namespace NLBE_Bot.Configuration;

using System.ComponentModel.DataAnnotations;

internal class WotInspectorApiOptions
{
	[Required]
	public string BaseUri
	{
		get; set;
	} = string.Empty;
}
