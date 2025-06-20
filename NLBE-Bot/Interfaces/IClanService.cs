namespace NLBE_Bot.Interfaces;

using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using FMWOTB.Clans;
using System.Threading.Tasks;

internal interface IClanService
{
	public Task ShowClanInfo(DiscordChannel channel, WGClan clan);

	public Task<WGClan> SearchForClan(DiscordChannel channel, DiscordMember member, string guildName, string clan_naam, bool loadMembers, DiscordUser user, Command command);
}
