namespace NLBE_Bot.Configuration;

using System.ComponentModel.DataAnnotations;

internal class BotOptions
{
	[Required]
	public ChannelIdsOptions ChannelIds
	{
		get; set;
	}

	[Range(1, 300)]
	public int DiscordTimeOutInSeconds
	{
		get; set;
	} = 30;

	[Required]
	public string DiscordToken
	{
		get; set;
	}

	public int HofWaitTimeInSeconds
	{
		get; set;
	} = 120;

	[Required]
	public RoleIdsOptions RoleIds
	{
		get; set;
	}

	public int NewPlayerWaitTimeInDays
	{
		get; set;
	} = 1;

	[Required]
	public ulong ServerId
	{
		get; set;
	}

	[Required]
	public WotbApiOptions WotbApi
	{
		get; set;
	}
}



