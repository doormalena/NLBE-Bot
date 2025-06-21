namespace NLBE_Bot.Interfaces;

using DSharpPlus.Entities;
using System;

internal interface IBotState
{
	public bool IgnoreCommands
	{
		get; set;
	}

	public bool IgnoreEvents
	{
		get; set;
	}

	public Tuple<ulong, DateTime> WeeklyEventWinner
	{
		get; set;
	}

	public IDiscordMessage LastCreatedDiscordMessage
	{
		get; set;
	}

	public DateTime? LasTimeNamesWereUpdated
	{
		get; set;
	}

	public DateTime? LastWeeklyWinnerAnnouncement
	{
		get; set;
	}
}
