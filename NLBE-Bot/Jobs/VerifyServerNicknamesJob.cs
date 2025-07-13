namespace NLBE_Bot.Jobs;

using FMWOTB.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

internal class VerifyServerNicknamesJob(IUserService userService,
								 IChannelService channelService,
								 IMessageService messageService,
								 IWGAccountService wgAccountService,
								 IErrorHandler errorHandler,
								 IOptions<BotOptions> options,
								 IBotState botState,
								 ILogger<VerifyServerNicknamesJob> logger) : IJob<VerifyServerNicknamesJob>
{
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
	private readonly IWGAccountService _wgAccountService = wgAccountService ?? throw new ArgumentNullException(nameof(wgAccountService));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly IBotState _botState = botState ?? throw new ArgumentNullException(nameof(botState));
	private readonly ILogger<VerifyServerNicknamesJob> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

	public async Task Execute(DateTime now)
	{
		if (!ShouldVerifyServerNicknames(now, _botState.LasTimeServerNicknamesWereVerified))
		{
			return;
		}

		await VerifyServerNicknames();
		_botState.LasTimeServerNicknamesWereVerified = now;
	}

	private static bool ShouldVerifyServerNicknames(DateTime now, DateTime? lastUpdate)
	{
		// Run once per day, at or after 00:00, but only if not already run today.
		return !lastUpdate.HasValue || lastUpdate.Value.Date != now.Date;
	}

	private async Task VerifyServerNicknames()
	{
		try
		{
			IDiscordChannel bottestChannel = await _channelService.GetBotTestChannel();

			if (bottestChannel == null)
			{
				_logger.LogWarning("Could not find the bot test channel. Aborting user update.");
				return;
			}

			IDiscordGuild guild = bottestChannel.Guild;
			IReadOnlyCollection<IDiscordMember> members = await guild.GetAllMembersAsync(); // TODO: unauthorized error with the dev/test server. Check permissions.
			IDiscordRole memberRole = guild.GetRole(Constants.LEDEN_ROLE); // TODO: make role configurable.

			List<IDiscordMember> invalidPlayerMatches = [];
			Dictionary<IDiscordMember, string> invalidPlayerClanMatches = [];
			List<IDiscordMember> validPlayerAndClanMatches = [];

			foreach (IDiscordMember member in members)
			{
				if (member.IsBot || member.Roles == null || !member.Roles.Contains(memberRole))
				{
					continue;
				}

				bool goodClanTag = false;
				Tuple<string, string> gebruiker = _userService.GetWotbPlayerNameFromDisplayName(member.DisplayName); // TODO: Refactor to an extension method returning a dynamic type with structured data.
				IReadOnlyList<IWGAccount> wgAccounts = await _wgAccountService.SearchByName(SearchAccuracy.EXACT, gebruiker.Item2, _options.WarGamingAppId, false, true, false);

				if (wgAccounts?.Count > 0) // TODO: what if more than 1 user is returned?
				{
					//Account met exact deze gebruikersnaam gevonden
					string clanTag = string.Empty;
					if (gebruiker.Item1.Length > 1 && gebruiker.Item1.StartsWith('[') && gebruiker.Item1.EndsWith(']'))
					{
						goodClanTag = true;
						string currentClanTag = string.Empty;
						if (wgAccounts[0].Clan != null && wgAccounts[0].Clan.Tag != null)
						{
							currentClanTag = wgAccounts[0].Clan.Tag;
						}
						string goodDisplayName = '[' + currentClanTag + "] " + wgAccounts[0].Nickname; // TODO: extract to a method.
						if (wgAccounts[0].Nickname != null && !member.DisplayName.Equals(goodDisplayName))
						{
							invalidPlayerClanMatches.TryAdd(member, goodDisplayName);
						}
						else if (member.DisplayName.Equals(goodDisplayName))
						{
							validPlayerAndClanMatches.Add(member);
						}
					}

					if (!goodClanTag)
					{
						if (wgAccounts[0].Clan != null && wgAccounts[0].Clan.Tag != null)
						{
							clanTag = wgAccounts[0].Clan.Tag;
						}
						string goodDisplayName = '[' + clanTag + "] " + wgAccounts[0].Nickname; // TODO: extract to a method.
						invalidPlayerClanMatches.TryAdd(member, goodDisplayName);
					}
				}
				else
				{
					invalidPlayerMatches.Add(member);
				}
			}

			// Apply changes and notify users
			if (invalidPlayerClanMatches.Count + invalidPlayerMatches.Count != 0)
			{
				foreach (KeyValuePair<IDiscordMember, string> memberChange in invalidPlayerClanMatches)
				{
					//await ChangeMemberNickname(memberChange.Key, memberChange.Value);
					await _messageService.SendMessage(bottestChannel, null, guild.Name, $"De gebruikersbijnaam van **{memberChange.Key.Username}** is aangepast van {memberChange.Key.DisplayName} naar **{memberChange.Value}");
					_logger.LogInformation("Nickname for `{Username}` updated from `{DisplayName}` to `{Value}`", memberChange.Key.Username, memberChange.Key.DisplayName, memberChange.Value);
				}

				foreach (IDiscordMember memberNotFound in invalidPlayerMatches)
				{
					//await _messageService.SendPrivateMessage(memberNotFound, guild.Name, "Hallo,\n\nVoor iedere gebruiker in de NLBE discord server wordt gecontroleerd of de ingestelde bijnaam overeenkomt met je WoTB spelersnaam.\nHelaas is dit voor jou niet het geval.\nWil je dit aanpassen?\nVoor meer informatie, zie het #regels kanaal.\n\nAlvast bedankt!\n- [NLBE] sjtubbers#4241");
					await _messageService.SendMessage(bottestChannel, null, guild.Name, $"De gebruikersbijnaam **{memberNotFound.DisplayName}** van **{memberNotFound.Username}** komt niet overeen met een WoTB-spelersnaam. Er is een privébericht verstuurd met het verzoek tot correctie.");
					_logger.LogWarning("Nickname `{DisplayName}` for user `{Username}` does not match any WoTB player name. A private message has been sent requesting correction.", memberNotFound.DisplayName, memberNotFound.Username);
				}
			}
			else
			{
				await bottestChannel.SendMessageAsync("De gebruikersbijnamen zijn nagekeken; geen wijzigingen waren nodig.");
				_logger.LogInformation("All nicknames have been reviewed; no changes were necessary.");
			}
		}
		catch (Exception ex)
		{
			string message = "An error occured while verifing all server nicknames.";
			await _errorHandler.HandleErrorAsync(message, ex);
		}
	}
}
