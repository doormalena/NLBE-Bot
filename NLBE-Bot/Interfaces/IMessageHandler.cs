namespace NLBE_Bot.Interfaces;

using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

internal interface IMessageHandler
{
	public Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e);

	public Task OnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs e);

	public Task OnMessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e);

	public Task OnMessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs e);
}
