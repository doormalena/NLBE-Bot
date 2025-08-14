namespace NLBE_Bot.Interfaces;

using DSharpPlus.Entities;
using NLBE_Bot.Models;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Tools.Replays;

internal interface IMessageService
{
	public Task<IDiscordMessage> SendMessage(IDiscordChannel channel, IDiscordMember member, string guildName, string message);

	public Task<bool> SendPrivateMessage(IDiscordMember member, string guildName, string Message);

	public Task SayTheUserIsNotAllowed(IDiscordChannel channel);

	public Task SayNumberTooSmall(IDiscordChannel channel);

	public Task SayNumberTooBig(IDiscordChannel channel);

	public Task SayMustBeNumber(IDiscordChannel channel);

	public Task SayBeMoreSpecific(IDiscordChannel channel);

	public Task SayNoResults(IDiscordChannel channel, string description);

	public Task SayNoResponse(IDiscordChannel channel);

	public IDiscordMessage SayMultipleResults(IDiscordChannel channel, string description);

	public Task SaySomethingWentWrong(IDiscordChannel channel, IDiscordMember member, string guildName);

	public Task<IDiscordMessage> SaySomethingWentWrong(IDiscordChannel channel, IDiscordMember member, string guildName, string text);

	public Task<IDiscordMessage> SayCannotBePlayedAt(IDiscordChannel channel, IDiscordMember member, string guildName, string roomType);

	public Task<IDiscordMessage> SayReplayNotWorthy(IDiscordChannel channel, WGBattle battle, string extraDescription);

	public Task<IDiscordMessage> SayReplayIsWorthy(IDiscordChannel channel, WGBattle battle, string extraDescription, int position);

	public Task<int> WaitForReply(IDiscordChannel channel, IDiscordUser user, string description, int count);

	public Task<string> AskQuestion(IDiscordChannel channel, IDiscordUser user, IDiscordGuild guild, string question);

	public Task ConfirmCommandExecuted(IDiscordMessage message);

	public Task ConfirmCommandExecuting(IDiscordMessage message);

	public IDiscordEmbed CreateStandardEmbed(string title, string description, DiscordColor color);

	public Task<IDiscordMessage> CreateEmbed(IDiscordChannel channel, EmbedOptions options);
}
