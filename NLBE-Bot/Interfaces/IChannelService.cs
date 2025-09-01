namespace NLBE_Bot.Interfaces;

using System.Threading.Tasks;

internal interface IChannelService
{
	public Task<IDiscordChannel?> GetHallOfFameChannelAsync();

	public Task<IDiscordChannel?> GetLogChannelAsync();

	public Task<IDiscordChannel?> GetDeputiesChannelAsync();

	public Task<IDiscordChannel?> GetPollsChannelAsync(bool isDeputyPoll);

	public Task<IDiscordChannel?> GetBotTestChannelAsync();

	public Task<IDiscordChannel?> GetToernooiAanmeldenChannelAsync();

	public Task<IDiscordChannel?> GetWeeklyEventChannelAsync();

	public Task<IDiscordChannel?> GetReplayResultsChannelAsync();

	public Task<IDiscordChannel?> GetAlgemeenChannelAsync();

	public Task<IDiscordChannel?> GetRegelsChannelAsync();

	public Task<IDiscordChannel?> GetMasteryReplaysChannelAsync();

	public Task<IDiscordChannel?> GetMappenChannelAsync();

	public Task<IDiscordChannel?> GetOudLedenChannelAsync();

	public Task<IDiscordChannel?> GetChannelAsync(ulong channelId);

	public Task CleanChannelAsync(ulong channelId);

	public Task CleanWelkomChannelAsync(ulong userId);
}
