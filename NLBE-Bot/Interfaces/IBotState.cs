namespace NLBE_Bot.Interfaces;

using NLBE_Bot.Models;
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

	public WeeklyEventWinner? WeeklyEventWinner
	{
		get; set;
	}
	public IDiscordMessage? LastCreatedDiscordMessage
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
