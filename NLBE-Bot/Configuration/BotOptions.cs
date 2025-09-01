namespace NLBE_Bot.Configuration;

using System.ComponentModel.DataAnnotations;

internal class BotOptions
{
	[Required]
	public ChannelIdsOptions ChannelIds
	{
		get; set;
	} = new();

	[Range(1, 300)]
	public int DiscordTimeOutInSeconds
	{
		get; set;
	} = 30;

	[Required]
	public string DiscordToken
	{
		get; set;
	} = string.Empty;

	public int HofWaitTimeInSeconds
	{
		get; set;
	} = 120;

	[Required]
	public RoleIdsOptions RoleIds
	{
		get; set;
	} = new();

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
	} = new();
}



