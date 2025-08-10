namespace NLBE_Bot.Interfaces;

using FMWOTB.Models;
using System.Threading.Tasks;

internal interface IClanService
{
	public Task ShowClanInfo(IDiscordChannel channel, WotbClanInfo clan);

	public Task<WotbClanListItem> SearchForClan(IDiscordChannel channel, IDiscordMember member, string guildName, string name, bool loadMembers, IDiscordUser user, IDiscordCommand command);
}
