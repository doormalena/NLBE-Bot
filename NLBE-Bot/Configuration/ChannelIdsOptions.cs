namespace NLBE_Bot.Configuration;
using System.ComponentModel.DataAnnotations;

public class ChannelIdsOptions
{
	[Required]
	public ulong BotTest
	{
		get; set;
	}

	[Required]
	public ulong OldMembers
	{
		get; set;
	}

	[Required]
	public ulong Rules
	{
		get; set;
	}

	[Required]
	public ulong Welcome
	{
		get; set;
	}
}
