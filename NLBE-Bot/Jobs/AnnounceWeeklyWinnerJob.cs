namespace NLBE_Bot.Jobs;

using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

internal class AnnounceWeeklyWinnerJob(IWeeklyEventService weeklyEventService,
									IChannelService channelService,
									IErrorHandler errorHandler,
									IBotState botState,
									ILogger<AnnounceWeeklyWinnerJob> logger) : IJob<AnnounceWeeklyWinnerJob>
{
	private readonly IWeeklyEventService _weeklyEventService = weeklyEventService ?? throw new ArgumentNullException(nameof(weeklyEventService));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IBotState _botState = botState ?? throw new ArgumentNullException(nameof(botState));
	private readonly ILogger<AnnounceWeeklyWinnerJob> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));

	public async Task Execute(DateTime now)
	{
		if (!ShouldAnnounceWeeklyWinner(now, _botState.LastWeeklyWinnerAnnouncement))
		{
			return;
		}

		await AnnounceWeeklyWinner();
		_botState.LastWeeklyWinnerAnnouncement = now;
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

	private async Task AnnounceWeeklyWinner()
	{
		try
		{
			IDiscordChannel bottestChannel = await _channelService.GetBotTestChannel();

			if (bottestChannel == null)
			{
				_logger.LogWarning("Could not find the bot test channel. Aborting user update.");
				return;
			}

			IDiscordGuild guild = bottestChannel.Guild;
			DateTime now = DateTime.Now;
			StringBuilder winnerMessage = new("Het wekelijkse event is afgelopen.");
			_logger.LogInformation(winnerMessage.ToString());

			await _weeklyEventService.ReadWeeklyEvent();

			if (_weeklyEventService.WeeklyEvent.StartDate.DayOfYear == now.DayOfYear - 7)
			{
				winnerMessage.AppendLine("Na 1 week...");
				WeeklyEventItem weeklyEventItemMostDMG = _weeklyEventService.WeeklyEvent.WeeklyEventItems.Find(weeklyEventItem => weeklyEventItem.WeeklyEventType == WeeklyEventType.Most_damage);

				if (weeklyEventItemMostDMG.Player != null && weeklyEventItemMostDMG.Player.Length > 0)
				{
					await _weeklyEventService.WeHaveAWinner(guild, weeklyEventItemMostDMG, _weeklyEventService.WeeklyEvent.Tank);
				}
			}

			await bottestChannel.SendMessageAsync(winnerMessage.ToString());
		}
		catch (Exception ex)
		{
			string message = "An error occured while anouncing the weekly winner.";
			await _errorHandler.HandleErrorAsync(message, ex);
		}
	}
}
