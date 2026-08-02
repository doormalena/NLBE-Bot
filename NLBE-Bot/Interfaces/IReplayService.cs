namespace NLBE_Bot.Interfaces;

using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Models;

internal interface IReplayService
{
	public Task<string> GetDescriptionForReplay(IDiscordGuild guild, WotInspectorBattle battle, int position, string preDescription = "");

	public Task<WotInspectorBattle?> GetReplayInfo(string title, IDiscordAttachment attachment);
}
