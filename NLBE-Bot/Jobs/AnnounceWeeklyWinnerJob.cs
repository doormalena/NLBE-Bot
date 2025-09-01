namespace NLBE_Bot.Jobs;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

internal class AnnounceWeeklyWinnerJob(IWeeklyEventService weeklyEventService,
									IOptions<BotOptions> options,
									IBotState botState,
									ILogger<AnnounceWeeklyWinnerJob> logger) : IJob<AnnounceWeeklyWinnerJob>
{
	private readonly IWeeklyEventService? _weeklyEventService = weeklyEventService ?? throw new ArgumentNullException(nameof(weeklyEventService));
	private readonly IBotState _botState = botState ?? throw new ArgumentNullException(nameof(botState));
	private readonly ILogger<AnnounceWeeklyWinnerJob> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

	public async Task Execute(IDiscordGuild guild, DateTime now)
	{
		if (!ShouldAnnounceWeeklyWinner(now, _botState.LastWeeklyWinnerAnnouncement))
		{
			return;
		}

		await AnnounceWeeklyWinner(guild, now);
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

	private async Task AnnounceWeeklyWinner(IDiscordGuild guild, DateTime now)
	{
		DateTime? lastSuccessfull = _botState.LastWeeklyWinnerAnnouncement; // Temporary store the last successful announce time.

		try
		{
			_botState.LastWeeklyWinnerAnnouncement = now;

			if (Guard.ReturnIfNull(guild.GetChannel(_options.ChannelIds.BotTest), _logger, "Bot Test channel", out IDiscordChannel bottestChannel))
			{
				return;
			}

			await _weeklyEventService!.ReadWeeklyEvent();

			StringBuilder winnerMessage = new("Het wekelijkse event is afgelopen.");
			winnerMessage.AppendLine("Na 1 week...");

			List<WeeklyEventItem> weeklyEventItems = _weeklyEventService!.WeeklyEvent.WeeklyEventItems;
			WeeklyEventItem? weeklyEventItemMostDMG = weeklyEventItems.Find(weeklyEventItem => weeklyEventItem.WeeklyEventType == WeeklyEventType.Most_damage);

			if (weeklyEventItemMostDMG != null && !string.IsNullOrEmpty(weeklyEventItemMostDMG.Player))
			{
				await _weeklyEventService.WeHaveAWinner(bottestChannel.Guild, weeklyEventItemMostDMG, _weeklyEventService.WeeklyEvent.Tank);
			}

			await bottestChannel.SendMessageAsync(winnerMessage.ToString());
		}
		catch (Exception ex)
		{
			_botState.LastWeeklyWinnerAnnouncement = lastSuccessfull; // Reset the last announce time to the last known good state.
			_logger.LogError(ex, "An error occured while anouncing the weekly winner.");
		}
	}
}
