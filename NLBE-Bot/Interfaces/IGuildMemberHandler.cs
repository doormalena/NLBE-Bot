namespace NLBE_Bot.Interfaces;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

internal interface IGuildMemberHandler
{
	public Task OnMemberAdded(DiscordClient client, GuildMemberAddEventArgs e);

	public Task OnMemberUpdated(DiscordClient client, GuildMemberUpdateEventArgs e);

	public Task OnMemberRemoved(DiscordClient client, GuildMemberRemoveEventArgs e);
}
