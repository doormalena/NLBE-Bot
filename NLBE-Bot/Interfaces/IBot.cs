namespace NLBE_Bot.Interfaces;

using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using FMWOTB.Account;
using FMWOTB.Clans;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal interface IBot
{
	public bool IgnoreCommands
	{
		get; set;
	}

	public bool IgnoreEvents
	{
		get; set;
	}

	public Task RunAsync();

	public bool CheckIfAllWithinRange(string[] tiers, int min, int max);

	public Task ConfirmCommandExecuted(DiscordMessage message);

	public Task ConfirmCommandExecuting(DiscordMessage message);

	public List<Tuple<string, List<TankHof>>> ConvertHOFMessageToTupleListAsync(DiscordMessage message, int TIER);

	public List<DiscordMessage> GetTierMessages(int tier, IReadOnlyList<DiscordMessage> messages);

	public Task<DiscordMessage> CreateEmbed(DiscordChannel channel, EmbedOptions options);

	public Task SaySomethingWentWrong(DiscordChannel channel, DiscordMember member, string guildName);

	public Task<DiscordMessage> SaySomethingWentWrong(DiscordChannel channel, DiscordMember member, string guildName, string text);

	public Task<DiscordMessage> SendMessage(DiscordChannel channel, DiscordMember member, string guildName, string message);

	public Task ShowMemberInfo(DiscordChannel channel, object gebruiker);

	public Task ShowClanInfo(DiscordChannel channel, WGClan clan);

	public Task<WGClan> SearchForClan(DiscordChannel channel, DiscordMember member, string guildName, string clan_naam, bool loadMembers, DiscordUser user, Command command);

	public Task<WGAccount> SearchPlayer(DiscordChannel channel, DiscordMember member, DiscordUser user, string guildName, string naam);

	public bool HasRight(DiscordMember member, Command command);

	public Task<bool> CreateOrCleanHOFMessages(DiscordChannel HOFchannel, List<Tuple<int, DiscordMessage>> tiersFound);

	public Task EditHOFMessage(DiscordMessage message, List<Tuple<string, List<TankHof>>> tierHOF);

	public Task<List<Tuple<string, string>>> GetAllMaps(ulong GuildID);

	public Task<DiscordChannel> GetHallOfFameChannel(ulong GuildID);

	public Task<DiscordChannel> GetLogChannel(ulong GuildID);

	public Task<DiscordChannel> GetDeputiesChannel();

	public Task<DiscordChannel> GetPollsChannel(bool isDeputyPoll, ulong GuildID);

	public Task<DiscordChannel> GetTestChannel();

	public Task<DiscordChannel> GetBottestChannel();

	public Task<DiscordChannel> GetToernooiAanmeldenChannel(ulong GuildID);

	public Task<List<Tuple<string, List<TankHof>>>> GetTankHofsPerPlayer(ulong guildID);

	public List<string> GetSearchTermAndCondition(params string[] parameter);

	public Task<List<Tuple<ulong, string>>> GetIndividualParticipants(List<Tier> teams, DiscordGuild guild);

	public Task<List<string>> GetMentions(List<Tuple<ulong, string>> memberList, ulong guildID);

	public List<DEF> ListInPlayerEmbed(int columns, List<Members> memberList, string searchTerm, DiscordGuild guild);

	public List<DEF> ListInMemberEmbed(int columns, List<DiscordMember> memberList, string searchTerm);

	public Task SayTheUserIsNotAllowed(DiscordChannel channel);

	public Task SayNumberTooSmall(DiscordChannel channel);

	public Task SayNumberTooBig(DiscordChannel channel);

	public Task SayMustBeNumber(DiscordChannel channel);

	public Task SayBeMoreSpecific(DiscordChannel channel);

	public Task SayNoResults(DiscordChannel channel, string description);

	public Task SayNoResponse(DiscordChannel channel);

	public DiscordMessage SayMultipleResults(DiscordChannel channel, string description);

	public List<Tuple<ulong, string>> RemoveSyntaxes(List<Tuple<ulong, string>> stringList);

	public Task<List<Tier>> ReadTeams(DiscordChannel channel, DiscordMember member, string guildName, string[] parameters_as_in_hoeveelste_team);

	public Task<DiscordChannel> GetWeeklyEventChannel();

	public Task SendThibeastmo(string message, string exceptionMessage = "", string stackTrace = "");
}
