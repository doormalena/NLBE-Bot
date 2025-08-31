namespace NLBE_Bot.Jobs;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

internal class VerifyServerNicknamesJob(IUserService userService,
								 IChannelService channelService,
								 IMessageService messageService,
								 IAccountsRepository accountRepository,
								 IClansRepository clanRepository,
								 IOptions<BotOptions> options,
								 IBotState botState,
								 ILogger<VerifyServerNicknamesJob> logger) : IJob<VerifyServerNicknamesJob>
{
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
	private readonly IAccountsRepository _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
	private readonly IClansRepository _clanRepository = clanRepository ?? throw new ArgumentNullException(nameof(clanRepository));
	private readonly IBotState _botState = botState ?? throw new ArgumentNullException(nameof(botState));
	private readonly ILogger<VerifyServerNicknamesJob> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

	public async Task Execute(DateTime now)
	{
		if (!ShouldVerifyServerNicknames(now, _botState.LasTimeServerNicknamesWereVerified))
		{
			return;
		}

		await VerifyServerNicknames(now);
	}

	private static bool ShouldVerifyServerNicknames(DateTime now, DateTime? lastUpdate)
	{
		// Run once per day, at or after 00:00, but only if not already run today.
		return !lastUpdate.HasValue || lastUpdate.Value.Date != now.Date;
	}

	private async Task VerifyServerNicknames(DateTime now)
	{
		DateTime? lastSuccessfull = _botState.LasTimeServerNicknamesWereVerified; // Temporary store the last successful verification time.

		try
		{
			_botState.LasTimeServerNicknamesWereVerified = now; // Update the last successful verification time to now to prevent multiple executions in parallel.

			if (Guard.ReturnIfNull(await _channelService.GetBotTestChannelAsync(), _logger, "Bot Test channel", out IDiscordChannel bottestChannel))
			{
				return;
			}

			IDiscordGuild guild = bottestChannel.Guild;

			if (Guard.ReturnIfNull(guild.GetRole(_options.RoleIds.Members), _logger, $"Default member role with id `{_options.RoleIds.Members}`", out IDiscordRole memberRole))
			{
				return;
			}

			IReadOnlyCollection<IDiscordMember> members = await guild.GetAllMembersAsync();
			List<IDiscordMember> invalidPlayerMatches = [];
			Dictionary<IDiscordMember, string> invalidPlayerClanMatches = [];

			foreach (IDiscordMember member in members)
			{
				await EvaluateMember(memberRole, invalidPlayerMatches, invalidPlayerClanMatches, member);
			}

			await NotifyNicknameIssues(bottestChannel, guild, invalidPlayerMatches, invalidPlayerClanMatches);
		}
		catch (Exception ex)
		{
			_botState.LasTimeServerNicknamesWereVerified = lastSuccessfull; // Reset the last successful verification time to the last known good state.
			_logger.LogError(ex, "An error occured while verifing all server nicknames.");
		}
	}

	private async Task EvaluateMember(IDiscordRole memberRole, List<IDiscordMember> invalidPlayerMatches, Dictionary<IDiscordMember, string> invalidPlayerClanMatches, IDiscordMember member)
	{
		if (member.IsBot || member.Roles == null || !member.Roles.Any(r => r.Id == memberRole.Id))
		{
			return;
		}

		WotbPlayerNameInfo playerNameInfo = _userService.GetWotbPlayerNameFromDisplayName(member.DisplayName);
		IReadOnlyList<WotbAccountListItem> wotbAccounts = await _accountRepository.SearchByNameAsync(SearchType.Exact, playerNameInfo.PlayerName, 1);

		if (wotbAccounts.Count <= 0)
		{
			invalidPlayerMatches.Add(member); // No match has been found using the player name.
		}
		else
		{
			WotbAccountListItem account = wotbAccounts[0];
			WotbAccountClanInfo accountClanInfo = await _clanRepository.GetAccountClanInfoAsync(account.AccountId);

			string clanTag = accountClanInfo?.Clan.Tag;
			string expectedDisplayName = FormatExpectedDisplayName(account.Nickname, clanTag);

			if (!member.DisplayName.Equals(expectedDisplayName))
			{
				invalidPlayerClanMatches.TryAdd(member, expectedDisplayName); // An exact match has been found using the player name, however, the clan tag does not match.
			}
		}
	}

	private async Task NotifyNicknameIssues(IDiscordChannel bottestChannel, IDiscordGuild guild, List<IDiscordMember> invalidPlayerMatches, Dictionary<IDiscordMember, string> invalidPlayerClanMatches)
	{
		if (invalidPlayerClanMatches.Count + invalidPlayerMatches.Count == 0)
		{
			await bottestChannel.SendMessageAsync("De gebruikersbijnamen zijn nagekeken; geen wijzigingen waren nodig.");
			_logger.LogInformation("All nicknames have been reviewed; no changes were necessary.");
		}

		// Apply changes and notify users
		foreach (KeyValuePair<IDiscordMember, string> memberChange in invalidPlayerClanMatches)
		{
			try
			{
				await _userService.ChangeMemberNickname(memberChange.Key, memberChange.Value);
				await _messageService.SendMessage(bottestChannel, null, guild.Name, $"De gebruikersbijnaam van **{memberChange.Key.Username}** is aangepast van **{memberChange.Key.DisplayName}** naar **{memberChange.Value}**");
				_logger.LogInformation("Nickname for `{Username}` updated from `{DisplayName}` to `{Value}`", memberChange.Key.Username, memberChange.Key.DisplayName, memberChange.Value);
			}
			catch (UnauthorizedAccessException ex)
			{
				await SendPrivateMessageToUpdateNickname(memberChange.Key, guild);
				_logger.LogWarning(ex, "Failed to change nickname for user `{Username}`", memberChange.Key.Username);
			}
		}

		foreach (IDiscordMember memberNotFound in invalidPlayerMatches)
		{
			await SendPrivateMessageToUpdateNickname(memberNotFound, guild);
			await _messageService.SendMessage(bottestChannel, null, guild.Name, $"De gebruikersbijnaam **{memberNotFound.DisplayName}** van **{memberNotFound.Username}** komt niet overeen met een WoTB-spelersnaam. Er is een privébericht verstuurd met het verzoek tot correctie.");
			_logger.LogWarning("Nickname `{DisplayName}` for user `{Username}` does not match any WoTB player name. A private message has been sent requesting correction.", memberNotFound.DisplayName, memberNotFound.Username);
		}
	}

	private async Task SendPrivateMessageToUpdateNickname(IDiscordMember member, IDiscordGuild guild)
	{
		string message = "Hallo,\n\nVoor iedere gebruiker in de NLBE discord server wordt gecontroleerd of de ingestelde bijnaam overeenkomt met je WoTB spelersnaam.\nHelaas is dit voor jou niet het geval.\nWil je dit aanpassen?\nVoor meer informatie, zie het #regels kanaal.\n\nAlvast bedankt!\n- [NLBE] sjtubbers#4241";
		await _messageService.SendPrivateMessage(member, guild.Name, message);
	}

	private static string FormatExpectedDisplayName(string nickname, string currentClanTag)
	{
		return $"[{currentClanTag}] {nickname}";
	}
}
