namespace NLBE_Bot.Interfaces;

using DSharpPlus.Entities;
using System.Threading.Tasks;

internal interface IChannelService
{
	public Task<DiscordChannel> GetHallOfFameChannel(ulong GuildID);

	public Task<DiscordChannel> GetLogChannel(ulong GuildID);

	public Task<DiscordChannel> GetDeputiesChannel();

	public Task<DiscordChannel> GetPollsChannel(bool isDeputyPoll, ulong GuildID);

	public Task<DiscordChannel> GetTestChannel();

	public Task<DiscordChannel> GetBottestChannel();

	public Task<DiscordChannel> GetToernooiAanmeldenChannel(ulong GuildID);

	public Task<DiscordChannel> GetWeeklyEventChannel();

	public Task<DiscordChannel> GetReplayResultsChannel();

	public Task<DiscordChannel> GetChannel(ulong serverID, ulong chatID);

	public Task<DiscordChannel> GetWelkomChannel();

	public Task CleanWelkomChannel();

	public Task CleanWelkomChannel(ulong userID);

	public Task<DiscordChannel> GetAlgemeenChannel();

	public Task<DiscordChannel> GetRegelsChannel();

	public Task<DiscordChannel> GetMasteryReplaysChannel(ulong GuildID);

	public Task<DiscordChannel> GetMappenChannel(ulong GuildID);

	public Task<DiscordChannel> GetOudLedenChannel();

	public Task CleanChannel(ulong serverID, ulong channelID);
}
