namespace NLBE_Bot.Interfaces;

using DSharpPlus.Entities;
using FMWOTB.Tools.Replays;
using System.Threading.Tasks;

internal interface IWeeklyEventHandler
{
	public WeeklyEvent WeeklyEvent
	{
		get; set;
	}

	public Task ReadWeeklyEvent();

	public Task<string> GetStringForWeeklyEvent(WGBattle battle);

	public Task CreateNewWeeklyEvent(string tank, DiscordChannel weeklyEventChannel);
}
