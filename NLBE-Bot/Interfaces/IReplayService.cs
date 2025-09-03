namespace NLBE_Bot.Interfaces;

using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Tools.Replays;

internal interface IReplayService
{
	public Task<string> GetDescriptionForReplay(IDiscordGuild guild, WGBattle battle, int position, string preDescription = "");

	public Task<WGBattle> GetReplayInfo(string title, object attachment, string ign, string url);
}
