namespace NLBE_Bot.Configuration;

using System.ComponentModel.DataAnnotations;

internal class WotbApiOptions
{
	[Required]
	public string BaseUri
	{
		get; set;
	} = string.Empty;

	[Required]
	public string ApplicationId
	{
		get; set;
	} = string.Empty;
}
