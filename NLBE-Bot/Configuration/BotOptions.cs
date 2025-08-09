namespace NLBE_Bot.Configuration;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

internal class BotOptions
{

	[Required]
	public Dictionary<string, ulong> ChannelIds
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
	public ulong MemberDefaultRoleId
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
	public string WarGamingAppId
	{
		get; set;
	}
}
