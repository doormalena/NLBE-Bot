namespace NLBE_Bot.Interfaces;

using NLBE_Bot.Models;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Models;

internal interface IWeeklyEventService
{
	public WeeklyEvent? WeeklyEvent
	{
		get; set;
	}

	public Task WeHaveAWinner(IDiscordGuild guild, WeeklyEventItem weeklyEventItemMostDMG, string tank);

	public Task ReadWeeklyEvent(IDiscordGuild guild);

	public Task<string> GetStringForWeeklyEvent(IDiscordGuild guild, WotInspectorBattle battle);

	public Task CreateNewWeeklyEvent(string tank, IDiscordChannel weeklyEventChannel);
}
