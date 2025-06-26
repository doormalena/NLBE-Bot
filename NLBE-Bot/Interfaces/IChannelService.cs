namespace NLBE_Bot.Interfaces;

using System.Threading.Tasks;

internal interface IChannelService
{
	public Task<IDiscordChannel> GetHallOfFameChannel(ulong GuildID);

	public Task<IDiscordChannel> GetLogChannel(ulong GuildID);

	public Task<IDiscordChannel> GetDeputiesChannel();

	public Task<IDiscordChannel> GetPollsChannel(bool isDeputyPoll, ulong GuildID);

	public Task<IDiscordChannel> GetTestChannel();

	public Task<IDiscordChannel> GetBottestChannel();

	public Task<IDiscordChannel> GetToernooiAanmeldenChannel(ulong GuildID);

	public Task<IDiscordChannel> GetWeeklyEventChannel();

	public Task<IDiscordChannel> GetReplayResultsChannel();

	public Task<IDiscordChannel> GetChannel(ulong serverID, ulong chatID);

	public Task<IDiscordChannel> GetWelkomChannel();

	public Task CleanWelkomChannel();

	public Task CleanWelkomChannel(ulong userID);

	public Task<IDiscordChannel> GetAlgemeenChannel();

	public Task<IDiscordChannel> GetRegelsChannel();

	public Task<IDiscordChannel> GetMasteryReplaysChannel(ulong GuildID);

	public Task<IDiscordChannel> GetMappenChannel(ulong GuildID);

	public Task<IDiscordChannel> GetOudLedenChannel();

	public Task CleanChannel(ulong serverID, ulong channelID);
}
