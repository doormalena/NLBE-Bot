namespace NLBE_Bot.Interfaces;

using System.Threading.Tasks;

internal interface IChannelService
{
	public Task CleanChannelAsync(IDiscordChannel channel);

	public Task CleanChannelAsync(IDiscordChannel channel, IDiscordMember member);
}
