namespace NLBE_Bot.Interfaces;

using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Models;

internal interface IReplayService
{
	public Task<string> GetDescriptionForReplay(IDiscordGuild guild, WotbBattle battle, int position, string preDescription = "");

	public Task<WotbBattle?> GetReplayInfo(string title, IDiscordAttachment attachment, string playerName);
}
