namespace NLBE_Bot.Interfaces;

using System.Threading.Tasks;

internal interface IChannelService
{
	public Task<IDiscordChannel> GetHallOfFameChannel();

	public Task<IDiscordChannel> GetLogChannel();

	public Task<IDiscordChannel> GetDeputiesChannel();

	public Task<IDiscordChannel> GetPollsChannel(bool isDeputyPoll);

	public Task<IDiscordChannel> GetTestChannel();

	public Task<IDiscordChannel> GetBotTestChannel();

	public Task<IDiscordChannel> GetToernooiAanmeldenChannel();

	public Task<IDiscordChannel> GetWeeklyEventChannel();

	public Task<IDiscordChannel> GetReplayResultsChannel();

	public Task<IDiscordChannel> GetChannel(ulong channeId);

	public Task<IDiscordChannel> GetWelkomChannel();

	public Task CleanWelkomChannel();

	public Task CleanWelkomChannel(ulong userId);

	public Task<IDiscordChannel> GetAlgemeenChannel();

	public Task<IDiscordChannel> GetRegelsChannel();

	public Task<IDiscordChannel> GetMasteryReplaysChannel();

	public Task<IDiscordChannel> GetMappenChannel();

	public Task<IDiscordChannel> GetOudLedenChannel();

	public Task CleanChannel(ulong channeId);
}
