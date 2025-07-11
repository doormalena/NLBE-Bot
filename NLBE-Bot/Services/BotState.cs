namespace NLBE_Bot.Services;

using NLBE_Bot.Interfaces;
using System;
using System.Threading;

internal class BotState : IBotState
{
	private readonly Lock _lock = new();

	private bool _ignoreCommands;
	public bool IgnoreCommands
	{
		get
		{
			lock (_lock)
			{
				return _ignoreCommands;
			}
		}
		set
		{
			lock (_lock)
			{
				_ignoreCommands = value;
			}
		}
	}

	private bool _ignoreEvents;
	public bool IgnoreEvents
	{
		get
		{
			lock (_lock)
			{
				return _ignoreEvents;
			}
		}
		set
		{
			lock (_lock)
			{
				_ignoreEvents = value;
			}
		}
	}

	private Tuple<ulong, DateTime> _weeklyEventWinner = new(0, DateTime.Now);
	public Tuple<ulong, DateTime> WeeklyEventWinner
	{
		get
		{
			lock (_lock)
			{
				return _weeklyEventWinner;
			}
		}
		set
		{
			lock (_lock)
			{
				_weeklyEventWinner = value;
			}
		}
	}

	private IDiscordMessage _lastCreatedDiscordMessage;
	public IDiscordMessage LastCreatedDiscordMessage
	{
		get
		{
			lock (_lock)
			{
				return _lastCreatedDiscordMessage;
			}
		}
		set
		{
			lock (_lock)
			{
				_lastCreatedDiscordMessage = value;
			}
		}
	}

	private DateTime? _lasTimeNamesWereUpdated;

	public DateTime? LasTimeNamesWereUpdated
	{
		get
		{
			lock (_lock)
			{
				return _lasTimeNamesWereUpdated;
			}
		}
		set
		{
			lock (_lock)
			{
				_lasTimeNamesWereUpdated = value;
			}
		}
	}

	private DateTime? _lastWeeklyWinnerAnnouncement;
	public DateTime? LastWeeklyWinnerAnnouncement
	{
		get
		{
			lock (_lock)
			{
				return _lastWeeklyWinnerAnnouncement;
			}
		}
		set
		{
			lock (_lock)
			{
				_lastWeeklyWinnerAnnouncement = value;
			}
		}
	}
}
