namespace NLBE_Bot.Models;

using System;

internal class BotStateData
{
	public bool IgnoreCommands
	{
		get; set;
	}

	public bool IgnoreEvents
	{
		get; set;
	}

	public WeeklyEventWinner? WeeklyEventWinner
	{
		get; set;
	}

	public DateTime? LasTimeServerNicknamesWereVerified
	{
		get; set;
	}

	public DateTime? LastWeeklyWinnerAnnouncement
	{
		get; set;
	}
}
