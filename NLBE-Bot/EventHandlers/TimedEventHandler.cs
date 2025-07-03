namespace NLBE_Bot.EventHandlers;

using NLBE_Bot.Interfaces;
using System;
using System.Globalization;
using System.Threading.Tasks;

internal class TimedEventHandler(IUserService userService,
								 IWeeklyEventService weeklyEventService,
								 IErrorHandler errorHandler,
								 IBotState botState) : ITimedEventHandler
{
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly IWeeklyEventService _weeklyEventService = weeklyEventService ?? throw new ArgumentNullException(nameof(weeklyEventService));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly IBotState _botState = botState ?? throw new ArgumentNullException(nameof(botState));

	public async Task Execute(DateTime now)
	{
		if (_botState.IgnoreEvents)
		{
			return;
		}

		if (ShouldUpdateUsernames(now, _botState.LasTimeNamesWereUpdated))
		{
			await UpdateUsernames();
		}

		if (ShouldAnnounceWeeklyWinner(now, _botState.LastWeeklyWinnerAnnouncement))
		{
			await _weeklyEventService.AnnounceWeeklyWinner();
			_botState.LastWeeklyWinnerAnnouncement = now;
		}
	}

	private static bool ShouldUpdateUsernames(DateTime now, DateTime? lastUpdate)
	{
		// Run once per day, at or after 00:00, but only if not already run today.
		return !lastUpdate.HasValue || lastUpdate.Value.Date != now.Date;
	}

	private static bool ShouldAnnounceWeeklyWinner(DateTime now, DateTime? lastAnnouncement)
	{
		// Run once per week, on Monday at or after 14:00, but only if not already run this week.
		bool isMondayAtOrAfter14 = now.DayOfWeek == DayOfWeek.Monday && now.Hour >= 14;
		bool notAlreadyAnnouncedThisWeek = !lastAnnouncement.HasValue ||
			CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(lastAnnouncement.Value, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)
			!= CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

		return isMondayAtOrAfter14 && notAlreadyAnnouncedThisWeek;
	}

	private async Task UpdateUsernames()
	{
		bool update = false;
		DateTime now = DateTime.Now;

		if (_botState.LasTimeNamesWereUpdated.HasValue)
		{
			if (_botState.LasTimeNamesWereUpdated.Value.DayOfYear != now.DayOfYear)
			{
				update = true;
			}
		}
		else
		{
			update = true;
		}

		if (update)
		{
			_botState.LasTimeNamesWereUpdated = now;

			try
			{
				await _userService.UpdateUsers();
			}
			catch (Exception ex)
			{
				string message = "\nERROR updating users:\n" + ex.Message;
				await _errorHandler.HandleErrorAsync(message, ex);
			}
		}
	}
}
