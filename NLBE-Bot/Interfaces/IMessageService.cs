namespace NLBE_Bot.Interfaces;

using DSharpPlus.Entities;
using FMWOTB.Tools.Replays;
using NLBE_Bot.Models;
using System.Threading.Tasks;

internal interface IMessageService
{
	public Task<DiscordMessage> SendMessage(DiscordChannel channel, DiscordMember member, string guildName, string message);

	public Task<bool> SendPrivateMessage(DiscordMember member, string guildName, string Message);

	public Task SaySomethingWentWrong(DiscordChannel channel, DiscordMember member, string guildName);

	public Task SayTheUserIsNotAllowed(DiscordChannel channel);

	public Task SayNumberTooSmall(DiscordChannel channel);

	public Task SayNumberTooBig(DiscordChannel channel);

	public Task SayMustBeNumber(DiscordChannel channel);

	public Task SayBeMoreSpecific(DiscordChannel channel);

	public Task SayNoResults(DiscordChannel channel, string description);

	public Task SayNoResponse(DiscordChannel channel);

	public DiscordMessage SayMultipleResults(DiscordChannel channel, string description);

	public Task<DiscordMessage> SaySomethingWentWrong(DiscordChannel channel, DiscordMember member, string guildName, string text);

	public Task<DiscordMessage> SayCannotBePlayedAt(DiscordChannel channel, DiscordMember member, string guildName, string roomType);

	public Task<DiscordMessage> SayReplayNotWorthy(DiscordChannel channel, WGBattle battle, string extraDescription);

	public Task<DiscordMessage> SayReplayIsWorthy(DiscordChannel channel, WGBattle battle, string extraDescription, int position);

	public Task SendThibeastmo(string message, string exceptionMessage = "", string stackTrace = "");

	public Task<int> WaitForReply(DiscordChannel channel, DiscordUser user, string description, int count);

	public Task<string> AskQuestion(DiscordChannel channel, DiscordUser user, DiscordGuild guild, string question);

	public Task ConfirmCommandExecuted(DiscordMessage message);

	public Task ConfirmCommandExecuting(DiscordMessage message);

	public DiscordEmbed CreateStandardEmbed(string title, string description, DiscordColor color);

	public Task<DiscordMessage> CreateEmbed(DiscordChannel channel, EmbedOptions options);
}
