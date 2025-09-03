namespace NLBE_Bot.Interfaces;

using System.Threading.Tasks;

internal interface IChannelService
{
	public Task<IDiscordChannel?> GetPollsChannelAsync(bool isDeputyPoll);

	public Task<IDiscordChannel?> GetMappenChannelAsync();

	public Task CleanChannelAsync(IDiscordChannel channel);

	public Task CleanChannelAsync(IDiscordChannel channel, IDiscordMember member);
}
