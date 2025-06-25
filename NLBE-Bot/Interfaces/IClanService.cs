namespace NLBE_Bot.Interfaces;

using FMWOTB.Clans;
using System.Threading.Tasks;

internal interface IClanService
{
	public Task ShowClanInfo(IDiscordChannel channel, WGClan clan);

	public Task<WGClan> SearchForClan(IDiscordChannel channel, IDiscordMember member, string guildName, string clan_naam, bool loadMembers, IDiscordUser user, IDiscordCommand command);
}
