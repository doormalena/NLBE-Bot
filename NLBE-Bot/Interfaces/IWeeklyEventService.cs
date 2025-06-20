namespace NLBE_Bot.Interfaces;

using DSharpPlus.Entities;
using FMWOTB.Tools.Replays;
using System.Threading.Tasks;

internal interface IWeeklyEventService
{
	public WeeklyEvent WeeklyEvent
	{
		get; set;
	}

	public Task WeHaveAWinner(DiscordGuild guild, WeeklyEventItem weeklyEventItemMostDMG, string tank);

	public Task ReadWeeklyEvent();

	public Task<string> GetStringForWeeklyEvent(WGBattle battle);

	public Task CreateNewWeeklyEvent(string tank, DiscordChannel weeklyEventChannel);
}
