namespace NLBE_Bot.Interfaces;

using NLBE_Bot.Models;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Tools.Replays;

internal interface IWeeklyEventService
{
	public WeeklyEvent WeeklyEvent
	{
		get; set;
	}

	public Task WeHaveAWinner(IDiscordGuild guild, WeeklyEventItem weeklyEventItemMostDMG, string tank);

	public Task ReadWeeklyEvent();

	public Task<string> GetStringForWeeklyEvent(WGBattle battle);

	public Task CreateNewWeeklyEvent(string tank, IDiscordChannel weeklyEventChannel);
}
