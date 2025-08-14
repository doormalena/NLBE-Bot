namespace NLBE_Bot.Configuration;

using System.ComponentModel.DataAnnotations;

internal class WotbApiOptions
{
	[Required]
	public string BaseUri
	{
		get; set;
	}

	[Required]
	public string ApplicationId
	{
		get; set;
	}
}
