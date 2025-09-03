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
	public ulong Deputies
	{
		get; set;
	}

	[Required]
	public ulong General
	{
		get; set;
	}

	[Required]
	public ulong HallOfFame
	{
		get; set;
	}

	[Required]
	public ulong Log
	{
		get; set;
	}

	[Required]
	public ulong Maps
	{
		get; set;
	}

	[Required]
	public ulong MasteryReplays
	{
		get; set;
	}

	[Required]
	public ulong OldMembers
	{
		get; set;
	}

	[Required]
	public ulong ReplayResults
	{
		get; set;
	}

	[Required]
	public ulong Rules
	{
		get; set;
	}

	[Required]
	public ulong TournamentSignUp
	{
		get; set;
	}

	[Required]
	public ulong WeeklyEvent
	{
		get; set;
	}

	[Required]
	public ulong Welcome
	{
		get; set;
	}
}
