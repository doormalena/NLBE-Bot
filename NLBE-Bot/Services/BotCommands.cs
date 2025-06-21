namespace NLBE_Bot.Services;

using DiscordHelper;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using FMWOTB;
using FMWOTB.Account;
using FMWOTB.Clans;
using FMWOTB.Tools;
using FMWOTB.Tournament;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLBE_Bot;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class BotCommands(IDiscordClientWrapper discordClient, IErrorHandler errorHandler, ILogger<BotCommands> logger, IConfiguration configuration, IClanService clanService,
							IBlitzstarsService handler, IDiscordMessageUtils discordMessageUtils, IBotState botState, IChannelService channelService, IUserService userService,
							IMessageService messageService, IMapService mapService, ITournamentService tournamentService, IHallOfFameService hallOfFameService, IWeeklyEventService weeklyEventHandler) : BaseCommandModule
{
	private const int MAX_NAME_LENGTH_IN_WOTB = 25;
	private const int MAX_TANK_NAME_LENGTH_IN_WOTB = 14;

	private readonly IDiscordClientWrapper _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly ILogger<BotCommands> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	private readonly IBlitzstarsService _handler = handler ?? throw new ArgumentNullException(nameof(handler));
	private readonly IDiscordMessageUtils _discordMessageUtils = discordMessageUtils ?? throw new ArgumentNullException(nameof(discordMessageUtils));
	private readonly IBotState _botState = botState ?? throw new ArgumentNullException(nameof(botState));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
	private readonly IMapService _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
	private readonly IHallOfFameService _hallOfFameService = hallOfFameService ?? throw new ArgumentNullException(nameof(hallOfFameService));
	private readonly ITournamentService _tournamentService = tournamentService ?? throw new ArgumentNullException(nameof(tournamentService));
	private readonly IWeeklyEventService _weeklyEventHandler = weeklyEventHandler ?? throw new ArgumentNullException(nameof(weeklyEventHandler));
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly IClanService _clanService = clanService ?? throw new ArgumentNullException(nameof(clanService));

	[Command("Toernooi")]
	[Aliases("to", "toer", "t")]
	[Description("Creëert het aanmelden van een nieuw toernooi." +
		"Bijvoorbeeld:`" + Constants.Prefix + "toernooi \"Quick Tournament\" \"Morgen 20u\" 6 8 10`\n" +
		"`" + Constants.Prefix + "toernooi \"\" \"Morgen 20u\" 6 8 10` --> \"\" = Quick Tournament (is default waarde)")]
	public async Task Tournament(CommandContext ctx, string type, string wanneer, params string[] tiers_gesplitst_met_spatie)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			if (tiers_gesplitst_met_spatie.Length > 0)
			{
				bool allInt = true;
				for (int i = 0; i < tiers_gesplitst_met_spatie.Length; i++)
				{
					try
					{
						int x = Convert.ToInt32(tiers_gesplitst_met_spatie[i]);
					}
					catch
					{
						allInt = false;
						break;
					}
				}
				if (allInt)
				{
					if (_tournamentService.CheckIfAllWithinRange(tiers_gesplitst_met_spatie, 1, 10))
					{
						DiscordChannel toernooiAanmeldenChannel = await _channelService.GetToernooiAanmeldenChannel(ctx.Guild.Id);
						if (toernooiAanmeldenChannel != null)
						{
							List<DEF> deflist = [];
							DEF newDef1 = new()
							{
								Name = "Type",
								Value = (string.IsNullOrEmpty(type) ? "Quick Tournament" : type).adaptToDiscordChat(),
								Inline = true
							};
							deflist.Add(newDef1);
							DEF newDef2 = new()
							{
								Name = "Wanneer?",
								Value = wanneer.adaptToDiscordChat(),
								Inline = true
							};
							deflist.Add(newDef2);
							DEF newDef3 = new()
							{
								Name = "Organisator",
								Value = ctx.Member.DisplayName.adaptToDiscordChat(),
								Inline = true
							};
							deflist.Add(newDef3);

							List<DiscordEmoji> emojiList = [];
							for (int i = 0; i < tiers_gesplitst_met_spatie.Length; i++)
							{
								emojiList.Add(_discordMessageUtils.GetDiscordEmoji(Emoj.GetName(Convert.ToInt32(tiers_gesplitst_met_spatie[i]))).Inner);
							}

							EmbedOptions options = new()
							{
								Content = "@everyone",
								Title = "Toernooi",
								Fields = deflist,
								Emojis = emojiList
							};
							await _messageService.CreateEmbed(toernooiAanmeldenChannel, options);
						}
						else
						{
							await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Het kanaal #Toernooi-aanmelden kon niet gevonden worden!**");
						}
					}
					else
					{
						await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De tiers moeten groter dan 0 en maximum 10 zijn!**");
					}
				}
				else
				{
					await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je moet als tiers getallen opgeven!**");
				}
			}
			else
			{
				await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je moet minstens één tier geven!**");
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("Toernooien")]
	[Aliases("trn")]
	[Description("Geeft zowel de recente toernooien als de komende toernooien.")]
	public async Task Tournaments(CommandContext ctx, params string[] optioneel_nummer)
	{
		await _messageService.ConfirmCommandExecuting(ctx.Message);
		if (optioneel_nummer.Length <= 1)
		{
			bool isInt = true;
			int theNumber = 1;
			if (optioneel_nummer.Length > 0)
			{
				try
				{
					theNumber = Convert.ToInt32(optioneel_nummer[0]);
					if (theNumber <= 0)
					{
						isInt = false;
					}
				}
				catch
				{
					isInt = false;
				}
			}
			else
			{
				isInt = false;
			}
			theNumber--;
			List<WGTournament> tournamentsList = await _tournamentService.InitialiseTournaments(true);
			if (isInt)
			{
				await _tournamentService.ShowTournamentInfo(ctx.Channel, tournamentsList[theNumber], (theNumber + 1) + (theNumber == 0 ? "ste" : "de") + " toernooi");
			}
			else
			{
				if (tournamentsList.Count > 0)
				{
					StringBuilder sb = new();
					for (int i = 0; i < tournamentsList.Count; i++)
					{
						int laagste = -1;
						int hoogste = -1;
						if (tournamentsList[i].stages != null)
						{
							foreach (Stage stage in tournamentsList[i].stages)
							{
								if (laagste > stage.min_tier)
								{
									laagste = stage.min_tier;
								}
								if (hoogste < stage.max_tier)
								{
									hoogste = stage.max_tier;
									if (laagste == -1)
									{
										laagste = hoogste;
									}
								}
							}
						}
						sb.AppendLine((i + 1) + ": " + tournamentsList[i].title + (tournamentsList[i].stages != null ? " -> Tier" + (hoogste != laagste ? "s" : "") + ": " + laagste + (laagste != hoogste ? " - " + hoogste : "") : "") + " -> Registraties: " + tournamentsList[i].registration_start_at + " - " + tournamentsList[i].registration_end_at + "\n");
					}
					await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, sb.ToString());
				}
				else
				{
					await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Geen enkel toernooi kon ingeladen worden.**");
				}
			}
		}
		else
		{
			await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je mag maar 1 extra waarde meegeven.**");
		}
		await _messageService.ConfirmCommandExecuted(ctx.Message);
	}

	[Command("Bonuscode")]
	[Aliases("bc", "boncode", "bonscode", "bonc", "bonuscod", "bonusco", "bonusc", "bonus", "bonu", "bon", "bo", "b")]
	[Description("Geeft de link om een bonuscode in te vullen (enkel nodig voor pc spelers, anderen kunnen dit via het spel doen).")]
	public async Task BonusCode(CommandContext ctx)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name,
				"**Redeem code:**\nhttps://eu.wargaming.net/shop/redeem/?utm_content=bonus-code&utm_source=global-nav&utm_medium=link&utm_campaign=wotb-portal");
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("Tagteams")]
	[Aliases("tt", "tagt", "tagte", "tagtea", "tgt", "tgte", "tgtea", "tgteam", "tte", "ttea", "tteam")]
	[Description("Tagt alle gebruikers die zich voor het bepaalde toernooi aangemeld hebben.\n" +
		"Voer deze commando uit in het kanaal waar het bericht geplaatst moet worden. " +
		"De bot zal dan je commando verwijderen en zelf een bericht plaatsen met dezelfde inhoud en tagt de mensen die zich aangemeld hebben voor het toernooi.")]
	public async Task TagTeams(CommandContext ctx, params string[] optioneel_wat_je_wilt_zeggen)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			//remove message
			await ctx.Channel.DeleteMessageAsync(ctx.Message);

			//execute rest of command
			List<Tier> tiers = await _tournamentService.ReadTeams(ctx.Channel, ctx.Member, ctx.Guild.Name, ["1"]);
			if (tiers != null)
			{
				List<Tuple<ulong, string>> uniqueMemberList = await _tournamentService.GetIndividualParticipants(tiers, ctx.Guild);
				List<string> mentionList = await _tournamentService.GetMentions(uniqueMemberList, ctx.Guild.Id);
				if (mentionList != null)
				{
					if (mentionList.Count > 0)
					{
						StringBuilder sb = new("**");
						bool firstTime = true;
						foreach (string gebruiker in mentionList)
						{
							if (firstTime)
							{
								firstTime = false;
							}
							else
							{
								sb.Append(' ');
							}
							sb.Append(gebruiker);
						}
						sb.Append("**");
						if (optioneel_wat_je_wilt_zeggen.Length > 0)
						{
							StringBuilder sbTekst = new();
							firstTime = true;
							foreach (string word in optioneel_wat_je_wilt_zeggen)
							{
								if (firstTime)
								{
									firstTime = false;
								}
								else
								{
									sbTekst.Append(' ');
								}
								sbTekst.Append(word);
							}
							sb.Append("\n\n");
							sb.Append(sbTekst.ToString());
						}
						await ctx.Channel.SendMessageAsync(sb.ToString());
					}
					else
					{
						await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Er konden geen mentions geladen worden.**");
					}
				}
				else
				{
					await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De mentions konden niet geladen worden.**");
				}
				//zet individuele spelers in lijst --> pas bot.getindividual... aan naar Tuple<ulong, string>
				//Maak een list van tags (op basis van ID, maar indien het niet gaad gewoon letterlijk #item2
			}
			else
			{
				await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De teams konden niet geladen worden.**");
			}
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("Teams")]
	[Aliases("te", "tea", "team")]
	[Description("Geeft de teams voor het gegeven toernooi." +
		"Bijvoorbeeld:`" + Constants.Prefix + "teams` --> geeft de teams van het meest recente bericht in Toernooi-aanmelden\n`" + Constants.Prefix + "teams 1` --> geeft de teams van het meest recente bericht in Toernooi-aanmelden\n`" + Constants.Prefix + "teams 2` --> geeft de teams van het 2de meest recente bericht in Toernooi-aanmelden")]
	public async Task Teams(CommandContext ctx, params string[] optioneel_hoeveelste_toernooi_startende_vanaf_1_wat_de_recentste_voorstelt)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			List<Tier> tiers = await _tournamentService.ReadTeams(ctx.Channel, ctx.Member, ctx.Guild.Name, optioneel_hoeveelste_toernooi_startende_vanaf_1_wat_de_recentste_voorstelt);
			if (tiers != null && tiers.Count > 0)
			{
				List<DEF> deflist = [];
				foreach (Tier aTier in tiers)
				{
					DEF def = new()
					{
						Inline = true,
						Name = "Tier " + aTier.TierNummer
					};
					int counter = 1;
					StringBuilder sb = new();
					foreach (Tuple<ulong, string> user in aTier.Deelnemers)
					{
						string tempName = string.Empty;
						if (aTier.IsEditedWithRedundance())
						{
							tempName = user.Item2;
						}
						else
						{
							try
							{
								DiscordMember tempMember = await ctx.Guild.GetMemberAsync(user.Item1);
								if (tempName != null)
								{
									if (tempMember.DisplayName != null)
									{
										if (tempMember.DisplayName.Length > 0)
										{
											tempName = tempMember.DisplayName;
										}
									}
								}
							}
							catch (Exception ex)
							{
								_logger.LogDebug(ex, "Error while getting member for user ID {UserId} in guild {GuildName}.", user.Item1, ctx.Guild.Name);
							}

							if (string.IsNullOrEmpty(tempName))
							{
								tempName = user.Item2;
							}
						}
						sb.AppendLine(counter + ". " + tempName);
						counter++;
					}
					def.Value = sb.ToString();
					deflist.Add(def);
				}
				List<Tuple<ulong, string>> tempParticipants = await _tournamentService.GetIndividualParticipants(tiers, ctx.Guild);
				List<Tuple<ulong, string>> participants = tempParticipants.RemoveSyntax();
				if (tiers.Count > 1)
				{
					participants.Sort();
					participants.Reverse();
					StringBuilder sb = new();
					foreach (Tuple<ulong, string> participant in participants)
					{
						sb.AppendLine(participant.Item2);
					}
					DEF newDef = new()
					{
						Inline = true,
						Name = "Alle deelnemers (" + participants.Count + "):",
						Value = sb.ToString().adaptToDiscordChat()
					};
					deflist.Add(newDef);
				}

				EmbedOptions options = new()
				{
					Title = "Teams",
					Description = tiers.Count > 0 ? "Organisator: " + tiers[0].Organisator : "Geen teams",
					Fields = deflist,
				};
				await _messageService.CreateEmbed(ctx.Channel, options);
			}
			else if (tiers == null)
			{
				await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De teams konden niet geladen worden.**");
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("Poll")]
	[Aliases("p", "po", "pol")]
	[Description("Creëert een nieuwe poll." +
		"Bijvoorbeeld:`" + Constants.Prefix + "poll \"Een titel tussen aanhalingstekens indien er spaties zijn\" Vlaanderen :one: Wallonië :two:`\n`" + Constants.Prefix + "poll test de hemel :thumbsup: de hemel, de hel :thinking: de hel :thumbsdown:`")]
	public async Task Poll(CommandContext ctx, string uitleg, params string[] opties_gesplitst_met_emoji_als_laatste_en_mag_met_spaties)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			DiscordChannel pollChannel = await _channelService.GetPollsChannel(false, ctx.Guild.Id);
			if (pollChannel != null)
			{
				List<DEF> deflist = [];
				Dictionary<string, DiscordEmoji> theList = [];
				List<DiscordEmoji> emojiList = [];
				StringBuilder sb = new();
				for (int i = 0; i < opties_gesplitst_met_emoji_als_laatste_en_mag_met_spaties.Length; i++)
				{
					bool isEmoji = false;
					IDiscordEmoji emoji = null;
					try
					{
						emoji = _discordMessageUtils.GetDiscordEmoji(opties_gesplitst_met_emoji_als_laatste_en_mag_met_spaties[i]);
						string temp = emoji.GetDiscordName();
						DiscordEmoji tempEmoji = DiscordEmoji.FromName(_discordClient.Inner, temp);
						isEmoji = true;
					}
					catch (Exception ex)
					{
						_logger.LogDebug(ex, "Error while trying to get emoji from string: {EmojiString}", opties_gesplitst_met_emoji_als_laatste_en_mag_met_spaties[i]);
					}

					if (isEmoji)
					{
						theList.Add(sb.ToString(), emoji.Inner);
						emojiList.Add(emoji.Inner);
						sb.Clear();
					}
					else
					{
						if (sb.Length > 0)
						{
							sb.Append(' ');
						}
						sb.Append(opties_gesplitst_met_emoji_als_laatste_en_mag_met_spaties[i]);
					}
				}
				foreach (KeyValuePair<string, DiscordEmoji> item in theList)
				{
					DEF def = new()
					{
						Inline = true,
						Name = item.Key.adaptToDiscordChat(),
						Value = item.Value
					};
					deflist.Add(def);
				}

				EmbedOptions options = new()
				{
					Title = "Poll",
					Description = uitleg.adaptToDiscordChat(),
					Fields = deflist,
					Emojis = emojiList,
				};
				await _messageService.CreateEmbed(pollChannel, options);
			}
			else
			{
				await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Het kanaal #polls kon niet gevonden worden!**");
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("Deputypoll")]
	[Aliases("dp", "dpo", "dpol", "dpoll", "depoll", "deppoll", "depupoll", "deputpoll")]
	[Description("Creëert een nieuwe poll ivm de kandidaat/inactieve clanleden.\n\n" +
		"De mogelijke Tags zijn:\n`nlbe`\n`nlbe2`\n`all` (= algemene deputies rol)\n\n" +
		"De mogelijke ondewerwerpen zijn:\n`nieuw` (= indien er een nieuw kandidaat-clanclid is)\n`inactief` (= indien een clanclid inactief is)\n`overstap` (= indien clanlid van NLBE2 naar NLBE wilt overstappen)\n\n" +
		"De mogelijke reacties zijn:\n:thumbsup: = akkoord\n:thinking: = neutraal\n:thumbsdown: = Niet akkoord\n\n" +
		"Indien je opnieuw een naam mee geeft dan kan je kiezen uit:\n`ja`\n`stop`\nindien iets anders herhaalt hij het gewoon\n\nIndien je niet antwoord binnen de 30s dan stopt de bot gewoon met vragen en stopt hij ook met de commando verder uit te voeren.")]
	public async Task DeputyPoll(CommandContext ctx, string Tag, string Onderwerp, string speler_naam, params string[] optioneel_clan_naam_indien_nieuwe_kandidaat)
	{
		// 3 reacties voorzien, :thumbsup: :thinking: :thumbsdown:
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			bool validChannel = false;
			DiscordChannel deputiesChannel = await _channelService.GetDeputiesChannel();
			if (deputiesChannel != null && ctx.Channel.Id.Equals(deputiesChannel.Id))
			{
				validChannel = true;
			}
			if (!validChannel)
			{
				DiscordChannel bottestChannel = await _channelService.GetBottestChannel();
				if (bottestChannel != null && ctx.Channel.Id.Equals(bottestChannel.Id))
				{
					validChannel = true;
				}
			}
			if (!validChannel)
			{
				DiscordChannel bottestChannel = await _channelService.GetTestChannel();
				if (bottestChannel != null && ctx.Channel.Id.Equals(bottestChannel.Id))
				{
					validChannel = true;
				}
			}
			if (validChannel)
			{
				StringBuilder sb = new();
				for (int i = 0; i < optioneel_clan_naam_indien_nieuwe_kandidaat.Length; i++)
				{
					if (i > 0)
					{
						sb.Append(' ');
					}
					sb.Append(optioneel_clan_naam_indien_nieuwe_kandidaat[i]);
				}
				DiscordChannel deputiesPollsChannel = await _channelService.GetPollsChannel(true, ctx.Guild.Id);
				//https://www.blitzstars.com/player/eu/
				bool goodOption = true;
				DiscordRole deputiesNLBERole = ctx.Guild.GetRole(Constants.DEPUTY_NLBE_ROLE);
				DiscordRole deputiesNLBE2Role = ctx.Guild.GetRole(Constants.DEPUTY_NLBE2_ROLE);
				switch (Tag.ToLower())
				{
					case "nlbe":
						Tag = deputiesNLBERole != null ? deputiesNLBERole.Mention : "@Deputy-NLBE";
						break;
					case "nlbe2":
						Tag = deputiesNLBE2Role != null ? deputiesNLBE2Role.Mention : "@Deputy-NLBE2";
						break;
					case "all":
						DiscordRole deputiesRole = ctx.Guild.GetRole(Constants.DEPUTY_ROLE);
						Tag = "@Deputy";

						if (deputiesRole != null)
						{
							Tag = deputiesRole.Mention;
						}
						else if (deputiesNLBERole != null && deputiesNLBE2Role != null)
						{
							Tag = deputiesNLBERole.Mention + " " + deputiesNLBE2Role.Mention;
						}
						break;
					default:
						goodOption = false;
						break;
				}
				if (goodOption)
				{
					string originalWat = Onderwerp;
					switch (Onderwerp.ToLower())
					{
						case "nieuw":
							Onderwerp = "Er heeft zich een nieuwe kandidaat voor <clan> gemeld, <|>. Dit zijn zijn stats:\n<link>.\n\nGraag hieronder stemmen.";
							break;
						case "inactief":
							Onderwerp = "<|><clan> heeft zijn laatste battle gespeeld op <dd-mm-jjjj> en heeft de laatste 90 dagen **<90>** battles gespeeld.\nDeze speler sloot zich op <dd-mm-yyyy> aan in de clan.\nZullen we afscheid van hem nemen?\n\nGraag hieronder stemmen.";
							break;
						case "overstap":
							Onderwerp = "<|> zou graag willen overstappen van NLBE2 naar NLBE.\nGaan jullie hiermee akkoord?\n\nGraag hieronder stemmen.";
							break;
						default:
							goodOption = false;
							break;
					}
					if (goodOption)
					{
						bool hasAnswered = false;
						bool hasConfirmed = false;
						bool firstTime = true;
						WGAccount account = new(_configuration["NLBEBOT:WarGamingAppId"], 552887317, false, true, false);

						while (true)
						{
							if (firstTime)
							{
								firstTime = false;
							}
							else
							{
								await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Geef opnieuw een naam:**");
								DSharpPlus.Interactivity.InteractivityExtension interactivityx = ctx.Client.GetInteractivity();
								DSharpPlus.Interactivity.InteractivityResult<DiscordMessage> messagex = await interactivityx.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.User);
								if (!messagex.TimedOut)
								{
									if (messagex.Result != null && messagex.Result.Content != null)
									{
										speler_naam = messagex.Result.Content;
									}
								}
								else
								{
									await _messageService.SayNoResponse(ctx.Channel);
									break;
								}
							}
							account = await _userService.SearchPlayer(ctx.Channel, ctx.Member, ctx.User, ctx.Guild.Name, speler_naam);
							if (account != null)
							{
								await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Is dit de gebruiker dat je zocht? ( ja / nee )**");
								DSharpPlus.Interactivity.InteractivityExtension interactivity = ctx.Client.GetInteractivity();
								DSharpPlus.Interactivity.InteractivityResult<DiscordMessage> message = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.User);

								if (!message.TimedOut && message.Result != null && message.Result.Content != null)
								{
									if (message.Result.Content.ToLower().Equals("ja"))
									{
										hasAnswered = true;
										hasConfirmed = true;
										break;
									}
									else if (message.Result.Content.ToLower().Equals("stop"))
									{
										hasAnswered = true;
										hasConfirmed = false;
										break;
									}
								}
								else
								{
									await _messageService.SayNoResponse(ctx.Channel);
									break;
								}
							}
							else
							{
								break;
							}
						}

						if (hasAnswered && hasConfirmed)
						{
							goodOption = false;
							if (account != null)
							{
								if (account.nickname != null)
								{
									if (account.nickname.Length > 0)
									{
										bool allGood = true;
										goodOption = true;
										string link = "www.blitzstars.com/player/eu/" + account.nickname;
										Onderwerp = Onderwerp.Replace("<|>", "**" + account.nickname.adaptToDiscordChat() + "**");
										Onderwerp = Onderwerp.Replace("<link>", "[" + link + "](https://" + link + ")");
										if (account.last_battle_time.HasValue)
										{
											Onderwerp = Onderwerp.Replace("<dd-mm-jjjj>", account.last_battle_time.Value.Day + "-" + account.last_battle_time.Value.Month + "-" + account.last_battle_time.Value.Year);
										}
										if (account.clan != null && account.clan.joined_at.HasValue)
										{
											Onderwerp = Onderwerp.Replace("<dd-mm-yyyy>", account.clan.joined_at.Value.Day + "-" + account.clan.joined_at.Value.Month + "-" + account.clan.joined_at.Value.Year);
										}
										int amountOfBattles90 = _handler.Get90DayBattles(account.account_id);
										Onderwerp = Onderwerp.Replace("<90>", amountOfBattles90.ToString());
										if (originalWat.ToLower().Equals("nieuw"))
										{
											if (sb.Length > 0)
											{
												Onderwerp = Onderwerp.Replace("<clan>", "**" + sb + "**");
											}
											else
											{
												await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je moet de clan meegeven waarin de speler wilt joinen!**");
												allGood = false;
											}
										}
										else
										{
											bool clanFound = false;
											if (account.clan != null && account.clan.tag != null)
											{
												clanFound = true;
												Onderwerp = Onderwerp.Replace("<clan>", " van **" + account.clan.tag + "**");
											}
											if (!clanFound)
											{
												Onderwerp = Onderwerp.Replace("<clan>", string.Empty);
											}
										}
										if (allGood)
										{
											List<DiscordEmoji> emojies = [];
											emojies.Add(_discordMessageUtils.GetDiscordEmoji(":thumbsup:").Inner);
											emojies.Add(_discordMessageUtils.GetDiscordEmoji(":thinking:").Inner);
											emojies.Add(_discordMessageUtils.GetDiscordEmoji(":thumbsdown:").Inner);
											DiscordEmbedBuilder.EmbedAuthor author = new()
											{
												Name = ctx.Member.DisplayName,
												IconUrl = ctx.Member.AvatarUrl
											};
											EmbedOptions options = new()
											{
												Content = Tag,
												Title = "Poll",
												Description = Onderwerp,
												Emojis = emojies,
												Author = author,
											};
											await _messageService.CreateEmbed(deputiesPollsChannel, options);
										}
									}
								}
								else
								{
									goodOption = false;
								}
							}
							if (!goodOption)
							{
								await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kon de speler niet vinden.**");
							}
						}
					}
					else
					{
						await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je hebt een verkeerde `Wat` meegegeven.**");
					}
				}
				else
				{
					await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je hebt een verkeerde `Tag` meegegeven.**");
				}
			}
			else
			{
				await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Je mag deze commando enkel vanuit " + (deputiesChannel != null ? deputiesChannel.Mention : "#deputies") + " uitvoeren!**");
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("Map")]
	[Aliases("m", "ma", "maps")]
	[Description("Laadt de map in de chat." +
		"Bijvoorbeeld:`" + Constants.Prefix + "map` --> geeft de lijst van mappen\n`" + Constants.Prefix + "map list` --> geeft de lijst van mappen\n`" + Constants.Prefix + "map mines` --> geeft de map \"Mines\"")]
	public async Task MapLoader(CommandContext ctx, params string[] map)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			List<Tuple<string, string>> images = await _mapService.GetAllMaps(ctx.Guild.Id);
			if (images != null)
			{
				StringBuilder sbMap = new();
				for (int i = 0; i < map.Count(); i++)
				{
					if (i > 0)
					{
						sbMap.Append(' ');
					}
					sbMap.Append(map[i]);
				}
				if (sbMap.ToString().ToLower().Equals("list") || sbMap.Length == 0)
				{
					StringBuilder sb = new();
					foreach (Tuple<string, string> item in images)
					{
						sb.AppendLine(item.Item1);
					}
					EmbedOptions options = new()
					{
						Title = "Mappen",
						Description = sb.ToString(),
					};
					await _messageService.CreateEmbed(ctx.Channel, options);
				}
				else
				{
					bool mapFound = false;
					foreach (Tuple<string, string> item in images)
					{
						if (item.Item1.ToLower().Contains(sbMap.ToString().ToLower()))
						{
							mapFound = true;

							EmbedOptions options = new()
							{
								Title = item.Item1,
								ImageUrl = item.Item2
							};
							await _messageService.CreateEmbed(ctx.Channel, options);
							break;
						}
					}
					if (!mapFound)
					{
						EmbedOptions options = new()
						{
							Title = "De map `" + sbMap.ToString() + "` kon niet gevonden worden."
						};
						await _messageService.CreateEmbed(ctx.Channel, options);
					}
				}
			}
			else
			{
				await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kon de mappen niet uit een kanaal halen.**");
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("Reageer")]
	[Aliases("r", "re", "rea", "reag", "reage", "reagee")]
	[Description("Geeft een reactie op het gegeven bericht in het gegeven kanaal met de gegeven emoji." +
		"Bijvoorbeeld:`" + Constants.Prefix + "reageer toernooi-aanmelden 1 :two:`--> zorgt ervoor dat de bot in toernooi-aanmelden bij het meest recente bericht de emoji :two: zet\n`" + Constants.Prefix + "reageer polls 4 :tada:` --> zorgt ervoor dat de bot in polls bij het 4de meest recente bericht de emoji :tada: zet")]
	public async Task Respond(CommandContext ctx, string naam_van_kanaal, int hoeveelste_bericht, string emoji)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			IEnumerable<DiscordChannel> channels = ctx.Guild.Channels.Values;
			foreach (DiscordChannel channel in channels)
			{
				if (channel.Name.Equals(naam_van_kanaal))
				{
					IReadOnlyList<DiscordMessage> xMessages = channel.GetMessagesAsync(hoeveelste_bericht).Result;
					for (int i = 0; i < xMessages.Count; i++)
					{
						if (i == hoeveelste_bericht - 1)
						{
							IDiscordEmoji theEmoji = _discordMessageUtils.GetDiscordEmoji(emoji);
							string temp = theEmoji.Inner.GetDiscordName();
							bool isEmoji = false;
							try
							{
								DiscordEmoji tempEmoji = DiscordEmoji.FromName(_discordClient.Inner, temp);
								isEmoji = true;
							}
							catch (Exception ex)
							{
								_logger.LogDebug(ex, "Emoji {Emoji} could not be parsed.", temp);
							}

							if (isEmoji)
							{
								try
								{
									await xMessages[i].CreateReactionAsync(theEmoji.Inner);
									await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Reactie(" + emoji + ") van bericht(" + hoeveelste_bericht + ") in kanaal(" + naam_van_kanaal + ") is toegevoegd!**");
								}
								catch (Exception ex)
								{
									await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kon geen reactie(" + emoji + ") toevoegen bij bericht(" + hoeveelste_bericht + ") in kanaal(" + naam_van_kanaal + ")!**");
									_logger.LogWarning(ex, "Could not add reaction(" + emoji + ") for message(" + hoeveelste_bericht + ") in channel(" + naam_van_kanaal + "):" + ex.Message);
								}
							}
							else
							{
								await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De emoji(" + emoji + ") geen bestaande emoji!**");
							}
						}
					}
				}
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("Verwijderreactie")]
	[Aliases("vr", "v", "ve", "ver", "verw", "verwi", "verwij", "verwijd", "verwijde", "verwijder", "verwijderr", "verwijderre", "verwijderrea", "verwijderreac", "verwijderreact", "verwijderreacti", "verwijdereactie")]
	[Description("Verwijdert een reactie van het gegeven bericht in het gegeven kanaal met de gegeven emoji." +
		"Bijvoorbeeld:`" + Constants.Prefix + "verwijderreactie toernooi-aanmelden 1 :two:`--> zorgt ervoor dat de bot in toernooi-aanmelden bij het meest recente bericht de emoji :two: verwijdert\n`" + Constants.Prefix + "verwijderreactie polls 4 :tada:` --> zorgt ervoor dat de bot in polls bij het 4de meest recente bericht de emoji :tada: verwijdert")]
	public async Task RemoveResponse(CommandContext ctx, string naam_van_kanaal, string hoeveelste_bericht, string emoji)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			int hoeveelste = -1;
			bool goodNumber = true;
			try
			{
				hoeveelste = Convert.ToInt32(hoeveelste_bericht);
			}
			catch
			{
				goodNumber = false;
			}
			if (hoeveelste > 0)
			{
				hoeveelste--;
				IEnumerable<DiscordChannel> channels = ctx.Guild.Channels.Values;
				bool channelFound = false;
				foreach (DiscordChannel channel in channels)
				{
					if (channel.Name.Equals(naam_van_kanaal))
					{
						channelFound = true;
						IReadOnlyList<DiscordMessage> zMessages = channel.GetMessagesAsync(hoeveelste + 1).Result;
						IReadOnlyList<DiscordUser> userReactionsFromTheEmoji = [];
						IDiscordEmoji theEmoji = _discordMessageUtils.GetDiscordEmoji(emoji);
						string temp = theEmoji.GetDiscordName();
						bool isEmoji = false;
						try
						{
							DiscordEmoji tempEmoji = DiscordEmoji.FromName(_discordClient.Inner, temp);
							isEmoji = true;
						}
						catch (Exception ex)
						{
							_logger.LogDebug(ex, "Emoji {Emoji} could not be parsed.", temp);
						}

						if (isEmoji)
						{
							try
							{
								userReactionsFromTheEmoji = await zMessages[hoeveelste].GetReactionsAsync(theEmoji.Inner);
								await zMessages[hoeveelste].DeleteReactionsEmojiAsync(theEmoji.Inner);
								await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Reactie(" + emoji + ") van bericht(" + (hoeveelste + 1) + ") in kanaal(" + naam_van_kanaal + ") is verwijderd!**");
							}
							catch (Exception ex)
							{
								await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kon reactie(" + emoji + ") van bericht(" + (hoeveelste + 1) + ") in kanaal(" + naam_van_kanaal + ") niet verwijderen!**");
								_logger.LogWarning(ex, "Could not remove reaction(" + emoji + ") from message(" + (hoeveelste + 1) + ") in channel(" + naam_van_kanaal + "):" + ex.Message);
							}
							if (channel.Id.Equals(Constants.NLBE_TOERNOOI_AANMELDEN_KANAAL_ID) || channel.Id.Equals(Constants.DA_BOIS_TOERNOOI_AANMELDEN_KANAAL_ID))
							{
								List<DiscordMessage> messages = [];
								try
								{
									IReadOnlyList<DiscordMessage> xMessages = channel.GetMessagesAsync(hoeveelste + 1).Result;
									foreach (DiscordMessage message in xMessages)
									{
										messages.Add(message);
									}
								}
								catch (Exception ex)
								{
									await _errorHandler.HandleErrorAsync("Could not load messages from " + channel.Name + ": ", ex);
								}
								if (messages.Count == hoeveelste + 1)
								{
									DiscordMessage theMessage = messages[hoeveelste];
									if (theMessage != null)
									{
										if (theMessage.Author.Id.Equals(Constants.NLBE_BOT) || theMessage.Author.Id.Equals(Constants.TESTBEASTV2_BOT))
										{
											IDiscordChannel logChannel = new DiscordChannelWrapper(await _channelService.GetLogChannel(ctx.Guild.Id));

											if (logChannel.Inner != null)
											{
												IReadOnlyList<IDiscordMessage> logMessages = await logChannel.GetMessagesAsync(100);
												Dictionary<DateTime, List<IDiscordMessage>> sortedMessages = _discordMessageUtils.SortMessages(logMessages);
												foreach (KeyValuePair<DateTime, List<IDiscordMessage>> sMessage in sortedMessages)
												{
													string xdate = theMessage.Timestamp.ConvertToDate();
													string ydate = sMessage.Key.ConvertToDate();
													if (xdate.Equals(ydate))
													{
														List<IDiscordMessage> messagesToDelete = [];
														sMessage.Value.Sort((x, y) => x.Inner.Timestamp.CompareTo(y.Inner.Timestamp));
														foreach (IDiscordMessage discMessage in sMessage.Value)
														{
															string[] splitted = discMessage.Content.Split(Constants.LOG_SPLIT_CHAR);
															if (splitted[1].ToLower().Equals("teams"))
															{
																//splitted[2] = naam speler
																foreach (DiscordUser user in userReactionsFromTheEmoji)
																{
																	DiscordMember tempMemberByUser = await ctx.Guild.GetMemberAsync(user.Id);
																	if (tempMemberByUser != null && tempMemberByUser.DisplayName.Equals(splitted[2]) && _discordMessageUtils.GetEmojiAsString(theEmoji.ToString()).Equals(_discordMessageUtils.GetEmojiAsString(splitted[3])))
																	{
																		messagesToDelete.Add(discMessage);
																	}
																}
															}
														}
														foreach (DiscordMessage toDeleteMessage in messagesToDelete)
														{
															await toDeleteMessage.DeleteAsync();
														}
														if (messagesToDelete.Count > 0)
														{
															await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**In de log werden er ook aanpassingen gedaan om het teams commando up-to-date te houden.**");
														}
														break;
													}
												}
											}
											else
											{
												await _errorHandler.HandleErrorAsync("Could not find log channel!");
											}
										}
									}
									else
									{
										await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Het bericht kon niet gevonden worden!**");
									}
								}
							}
						}
						else
						{
							await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Het gegeven emoji is geen bestaande emoji!**");
						}
						break;
					}
				}
				if (!channelFound)
				{
					await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kanaal kon niet gevonden worden.**");
				}
			}
			else
			{
				if (goodNumber)
				{
					await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Dat getal is te klein, het moet groter dan 0 zijn!**");
				}
				else
				{
					await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Dat getal is geen bruikbaar getal!**");
				}
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("Help")]
	[Aliases("h", "he", "hel")]
	[Description("Geeft alle commando's of geeft uitleg voor het gegeven commando." +
		"Bijvoorbeeld:`" + Constants.Prefix + "help`\n`" + Constants.Prefix + "help teams`")]
	public async Task Help(CommandContext ctx, params string[] optioneel_commando)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			if (optioneel_commando.Length == 0)
			{
				StringBuilder sb = new();
				IEnumerable<Command> commands = ctx.CommandsNext.RegisteredCommands.Values;
				List<string> commandoList = [];
				foreach (Command command in commands)
				{
					if (!commandoList.Contains(command.Name) && _userService.HasPermission(ctx.Member, command))
					{
						commandoList.Add(command.Name);
						sb.AppendLine(command.Name);
					}
				}
				List<DEF> deflist = [];
				DEF newDef1 = new()
				{
					Inline = true,
					Name = "Commando's",
					Value = sb.ToString()
				};
				deflist.Add(newDef1);
				EmbedOptions options = new()
				{
					Title = "Help",
					Description = "Versie: `" + Constants.version + "`",
					Fields = deflist
				};
				await _messageService.CreateEmbed(ctx.Channel, options);
			}
			else if (optioneel_commando.Length == 1)
			{
				IReadOnlyDictionary<string, Command> commands = ctx.CommandsNext.RegisteredCommands;
				bool commandFound = false;
				foreach (KeyValuePair<string, Command> command in commands)
				{
					if (command.Key.ToLower().Equals(optioneel_commando[0].ToLower()))
					{
						commandFound = true;
						List<DEF> deflist = [];
						DEF newDef1 = new()
						{
							Inline = true,
							Name = "Commando",
							Value = command.Value.Name
						};
						deflist.Add(newDef1);
						if (command.Value.Overloads.Count > 0)
						{
							StringBuilder overloadSB = new();
							bool firstTime = true;
							foreach (CommandArgument argument in command.Value.Overloads[0].Arguments)
							{
								if (firstTime)
								{
									firstTime = false;
								}
								else
								{
									overloadSB.Append(" ");
								}
								string argumentName = argument.Name;
								if (argument.Name.Contains("optioneel_"))
								{
									string[] splitted = argumentName.Split("__");
									argumentName = "[" + splitted[0].Replace('_', ' ').Replace("optioneel_", string.Empty) + "]";
									if (splitted.Length > 1)
									{
										for (int i = 1; i < splitted.Length; i++)
										{
											argumentName += " (" + splitted[i].Replace('_', ' ') + ")";
										}
									}
								}
								else
								{
									argumentName = "(" + argumentName + ")";
								}
								overloadSB.Append(argumentName.Replace("optioneel", "OPTIONEEL").Replace('_', ' '));
							}
							if (overloadSB.Length > 0)
							{
								DEF newDef2 = new()
								{
									Inline = true,
									Name = "Argument" + (command.Value.Overloads[0].Arguments.Count > 1 ? "en" : ""),
									Value = overloadSB.ToString()
								};
								deflist.Add(newDef2);
							}
						}
						if (command.Value.Aliases.Count > 0)
						{
							StringBuilder aliasSB = new();
							bool firstTime = true;
							foreach (string alias in command.Value.Aliases)
							{
								if (firstTime)
								{
									firstTime = false;
								}
								else
								{
									aliasSB.Append(", ");
								}
								aliasSB.Append(alias);
							}
							if (aliasSB.Length > 0)
							{
								DEF newDef2 = new()
								{
									Inline = true,
									Name = "Alias" + (command.Value.Aliases.Count > 1 ? "sen" : ""),
									Value = aliasSB.ToString()
								};
								deflist.Add(newDef2);
							}
						}
						if (command.Value.Description.Length > 0)
						{
							DEF newDef3 = new()
							{
								Inline = true,
								Name = "Omschrijving"
							};
							if (command.Value.Description.Contains("Bijvoorbeeld:"))
							{
								DEF newDef4 = new()
								{
									Inline = true,
									Name = "Bijvoorbeeld"
								};
								string[] splitted = command.Value.Description.Split("Bijvoorbeeld:");
								newDef3.Value = splitted[0];
								newDef4.Value = splitted[1];
								deflist.Add(newDef4);
							}
							else
							{
								newDef3.Value = command.Value.Description;
							}
							deflist.Add(newDef3);
						}
						EmbedOptions options = new()
						{
							Title = "Help voor `" + command.Key + "`",
							Fields = deflist,
						};
						await _messageService.CreateEmbed(ctx.Channel, options);
						break;
					}
				}
				if (!commandFound)
				{
					await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kan info voor " + optioneel_commando[0] + " niet vinden omdat deze commando niet bestaat!**");
				}
			}
			else
			{
				await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Te veel parameters! Max 1 parameter!**");
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}
	[Command("Ignore")]
	[Description("Negeert alle commando's behalve deze commando zelf tot de gebruiker dit weer inschakelt. Indien \"event\" of \"events\" als parameter meegegeven wordt, negeert hij de events. Je kan de events met dezelfde commando terug inschakelen.")]
	public async Task Ignore(CommandContext ctx, params string[] optioneel_events)
	{
		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);

			if (optioneel_events.Length > 0)
			{
				if (!optioneel_events[0].Contains("event", StringComparison.OrdinalIgnoreCase) && !optioneel_events[0].Contains("events", StringComparison.OrdinalIgnoreCase))
				{
					_botState.IgnoreCommands = !_botState.IgnoreCommands;
					_logger.LogWarning(">>> NLBE-Bot negeert nu de commando's" + (_botState.IgnoreCommands ? "" : " niet meer") + "! <<<");
					await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**NLBE-Bot (`v " + Constants.version + "`) negeert nu de commando's" + (_botState.IgnoreCommands ? "" : " niet meer") + "!**");
				}
				else
				{
					_botState.IgnoreEvents = !_botState.IgnoreEvents;
					_logger.LogWarning(">>> NLBE-Bot negeert nu de events" + (_botState.IgnoreEvents ? "" : " niet meer") + "! <<<");
					await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**NLBE-Bot (`v " + Constants.version + "`) negeert nu de events" + (_botState.IgnoreEvents ? "" : " niet meer") + "!**");
				}
			}
			else
			{
				_botState.IgnoreCommands = !_botState.IgnoreCommands;
				_logger.LogWarning(">>> NLBE-Bot negeert nu de commando's" + (_botState.IgnoreCommands ? "" : " niet meer") + "! <<<");
				await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**NLBE-Bot (`v " + Constants.version + "`) negeert nu de commando's" + (_botState.IgnoreCommands ? "" : " niet meer") + "!**");
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("Gebruiker")]
	[Aliases("speler", "spele", "spel", "spe", "sp", "s", "g", "ge", "geb", "gebr", "gebru", "gebrui", "gebruik", "gebruike", "gbruiker", "gbruikr", "gbrkr")]
	[Description("Geeft info over een speler.\n-i --> op ID zoeken (zoekt ook buiten de discord server)\nAnders zoekt de bot op basis van de originele gebruikersnamen van de personen in deze server." +
		"Bijvoorbeeld:`" + Constants.Prefix + "gebruiker 1`\n`" + Constants.Prefix + "gebruiker sjt`")]
	public async Task Player(CommandContext ctx, params string[] optioneel_zoeken_op_id__zoekterm)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			string searchTerm = "";
			string conditie = "";
			List<string> temp = GetSearchTermAndCondition(optioneel_zoeken_op_id__zoekterm);
			if (temp[0] != string.Empty)
			{
				searchTerm = temp[0];
			}
			conditie = temp[1];
			int aantalGebruikers = 0;
			if (searchTerm.ToLower().Contains('i'))
			{
				bool isInt = false;
				ulong tempID = 0;
				try
				{
					tempID = Convert.ToUInt64(conditie);
					isInt = true;
				}
				catch
				{
					isInt = false;
				}
				if (isInt)
				{
					bool found = false;
					bool error = false;
					try
					{
						DiscordUser discordUser = await _discordClient.GetUserAsync(tempID);
						if (discordUser != null)
						{
							await _userService.ShowMemberInfo(ctx.Channel, discordUser);
							found = true;
						}
					}
					catch (Exception ex)
					{
						error = true;
						await _errorHandler.HandleErrorAsync("Something went wrong while showing the memberInfo:\n", ex);
						await _messageService.SaySomethingWentWrong(ctx.Channel, ctx.Member, ctx.Guild.Name);
					}
					if (!found && !error)
					{
						await _messageService.SayNoResults(ctx.Channel, "**Gebruiker met ID `" + conditie + "` kon niet gevonden worden!**");
					}
				}
				else
				{
					await _messageService.SayMustBeNumber(ctx.Channel);
				}
			}
			else
			{
				IReadOnlyCollection<DiscordMember> members = ctx.Guild.GetAllMembersAsync().Result;
				aantalGebruikers = members.Count;
				List<DiscordMember> foundMemberList = [];
				foreach (DiscordMember member in members)
				{
					if ((member.Username.ToLower() + "#" + member.Discriminator).Contains(conditie.ToLower()))
					{
						foundMemberList.Add(member);
					}
				}
				if (foundMemberList.Count > 1)
				{
					StringBuilder sbFound = new();
					for (int i = 0; i < foundMemberList.Count; i++)
					{
						sbFound.AppendLine(i + 1 + ". `" + foundMemberList[i].Username + "#" + foundMemberList[i].Discriminator.ToString() + "`");
					}
					if (sbFound.Length < 1024)
					{
						DiscordMessage discMessage = _messageService.SayMultipleResults(ctx.Channel, sbFound.ToString());
						DSharpPlus.Interactivity.InteractivityExtension interactivity = ctx.Client.GetInteractivity();
						DSharpPlus.Interactivity.InteractivityResult<DiscordMessage> message = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.User);
						if (!message.TimedOut)
						{
							bool isInt = false;
							int number = -1;
							try
							{
								number = Convert.ToInt32(message.Result.Content);
								isInt = true;
							}
							catch
							{
								isInt = false;
							}
							if (isInt)
							{
								if (number > 0 && number <= foundMemberList.Count)
								{
									await _userService.ShowMemberInfo(ctx.Channel, foundMemberList[number - 1]);
								}
								else if (number > foundMemberList.Count)
								{
									await _messageService.SayNumberTooBig(ctx.Channel);
								}
								else if (1 > number)
								{
									await _messageService.SayNumberTooSmall(ctx.Channel);
								}
							}
							else
							{
								await _messageService.SayMustBeNumber(ctx.Channel);
							}
						}
						else if (discMessage != null)
						{
							List<DiscordEmoji> reacted = [];
							for (int i = 1; i <= 10; i++)
							{
								IDiscordEmoji emoji = _discordMessageUtils.GetDiscordEmoji(Emoj.GetName(i));
								if (emoji != null)
								{
									IReadOnlyList<DiscordUser> users = discMessage.GetReactionsAsync(emoji.Inner).Result;
									foreach (DiscordUser user in users)
									{
										if (user.Id.Equals(ctx.User.Id))
										{
											reacted.Add(emoji.Inner);
										}
									}
								}
							}

							if (reacted.Count == 1)
							{
								int index = Emoj.GetIndex(_discordMessageUtils.GetEmojiAsString(reacted[0].Name));
								if (index > 0 && index <= foundMemberList.Count)
								{
									await _userService.ShowMemberInfo(ctx.Channel, foundMemberList[index - 1]);
								}
								else
								{
									await ctx.Channel.SendMessageAsync("**Dat was geen van de optionele emoji's!**");
								}
							}
							else if (reacted.Count > 1)
							{
								await ctx.Channel.SendMessageAsync("**Je mocht maar 1 reactie geven!**");
							}
							else
							{
								await _messageService.SayNoResponse(ctx.Channel);
							}
						}
						else
						{
							await _messageService.SayNoResponse(ctx.Channel);
						}
					}
					else
					{
						await _messageService.SayBeMoreSpecific(ctx.Channel);
					}
				}
				else if (foundMemberList.Count == 1)
				{
					await _userService.ShowMemberInfo(ctx.Channel, foundMemberList[0]);
				}
				else if (foundMemberList.Count == 0)
				{
					await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Gebruiker(**`" + conditie.Replace("\\", string.Empty) + "`**) kon niet gevonden worden! (In een lijst van " + aantalGebruikers + " gebruikers)**");
				}
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("Gebruikerslijst")]
	[Aliases("gl")]
	[Description("Geeft alle members van de server als format: username#discriminator.\n-u --> username\n-d --> discriminator\n-n --> nickname\n-! --> not\n-b --> geeft bijnamen ipv standaard format (enkel voor de weergave, niet voor de filtering)\n-o --> sorteert op datum van creatie van WarGaming account (de niet gevonden accounts sorteert ie alfabetisch)\n-c --> sorteert op clanjoindatum (de niet gevonden accounts sorteert ie alfabetisch)"
	+ "Bijvoorbeeld:\n`" + Constants.Prefix + "gl -n [NLBE]` --> geeft alle leden waarbij \"[NLBE]\" in de bijnaam voorkomt (die dus de NLBE rol hebben)\n" +
		"`" + Constants.Prefix + "gl -n!u [NLBE]` --> geeft de gebruikers waarbij \"[NLBE]\" niet in de gebruikersnaam voorkomt maar wel in de bijnaam\n" +
		"`" + Constants.Prefix + "gl -!n [NLBE` --> geeft alle leden waarbij \"[NLBE\" niet in de bijnaam voorkomt (dus de personen die niet in een NLBE clan zitten)\n" +
		"`" + Constants.Prefix + "gl -d 98` --> geeft alle leden waarbij de discriminator \"98\" bevat\n" +
		"`" + Constants.Prefix + "gl -nu [NLBE]` --> geeft alle leden waarvan zowel de gebruikersnaam als de bijnaam \"[NLBE]\" bevat\n" +
		"`" + Constants.Prefix + "gl -!nu [NLBE]` --> geeft de leden waarbij \"[NLBE]\" noch in de gebruikersnaam noch in de bijnaam voorkomt" +
		"`" + Constants.Prefix + "gl -on [NLBE]` --> geeft de leden waarbij \"[NLBE]\" in de bijnaam voorkomt en sorteert dit op de creatie van het WG account")]
	public async Task PlayerList(CommandContext ctx, params string[] optioneel_optie_met_als_default_ud__waarde)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			const int COLUMNS = 3;
			string searchTerm = "ud";
			string conditie = "";
			bool usersFound = false;
			List<string> temp = GetSearchTermAndCondition(optioneel_optie_met_als_default_ud__waarde);
			if (temp[0] != string.Empty)
			{
				searchTerm = temp[0];
			}
			conditie = temp[1];
			IReadOnlyCollection<DiscordMember> members = ctx.Guild.GetAllMembersAsync().Result;
			List<Tuple<StringBuilder, StringBuilder>> sbs = [];
			for (int i = 0; i < COLUMNS; i++)
			{
				sbs.Add(new Tuple<StringBuilder, StringBuilder>(new StringBuilder(), new StringBuilder()));
			}

			List<DiscordMember> memberList = [];
			List<DiscordMember> dateNotFoundList = [];
			foreach (DiscordMember member in members)
			{
				bool goodSearchTerm = false;
				List<bool> addList = [];
				if (searchTerm.ToLower().Contains('u'))
				{
					addList.Add(false);
					goodSearchTerm = true;
					if (!memberList.Contains(member) && member.Username.ToLower().Contains(conditie.Split('*')[0].ToLower()))
					{
						addList.RemoveAt(addList.Count - 1);
						addList.Add(true);
					}
				}
				if (searchTerm.ToLower().Contains('d'))
				{
					addList.Add(false);
					goodSearchTerm = true;
					if (!memberList.Contains(member) && member.Discriminator.ToString().Contains(conditie.Split('*')[0].ToLower()))
					{
						addList.RemoveAt(addList.Count - 1);
						addList.Add(true);
					}
				}
				if (searchTerm.ToLower().Contains('n'))
				{
					addList.Add(false);
					goodSearchTerm = true;
					if (!memberList.Contains(member) && member.DisplayName.ToLower().Contains(conditie.Split('*')[0].ToLower()))
					{
						addList.RemoveAt(addList.Count - 1);
						addList.Add(true);
					}
				}
				if (searchTerm.ToLower().Contains('b'))
				{
					goodSearchTerm = true;
				}
				if (searchTerm.ToLower().Contains('o'))
				{
					goodSearchTerm = true;
				}
				if (!addList.Contains(false) && goodSearchTerm)
				{
					memberList.Add(member);
				}
				if (!goodSearchTerm)
				{
					sbs[0].Item1.Append("Oei!");
					sbs[0].Item2.Append("De parameter waarop gezocht moet worden bestaat niet!");
					break;
				}
			}

			if (memberList.Count > 0)
			{
				usersFound = true;
			}
			if ((searchTerm.Contains('o') && !searchTerm.Contains('c')) || (!searchTerm.Contains('o') && searchTerm.Contains('c')))
			{
				Dictionary<DateTime, DiscordMember> dateMemberList = [];
				foreach (DiscordMember member in memberList)
				{
					string tempIGNName = string.Empty;
					string[] splitted = member.DisplayName.Split(']');
					if (splitted.Length > 1)
					{
						StringBuilder sb = new();
						for (int i = 1; i < splitted.Length; i++)
						{
							sb.Append(splitted[i]);
						}
						tempIGNName = sb.ToString().Trim();
					}
					else
					{
						tempIGNName = splitted[0].ToString().Trim();
					}

					IReadOnlyList<WGAccount> searchResults = await WGAccount.searchByName(SearchAccuracy.EXACT, tempIGNName, _configuration["NLBEBOT:WarGamingAppId"], false, false, false);
					if (searchResults != null)
					{
						if (searchResults.Count > 0)
						{
							bool used = false;
							foreach (WGAccount account in searchResults)
							{
								if (account.nickname.ToLower().Equals(tempIGNName.ToLower()))
								{
									try
									{
										if (searchTerm.Contains('o'))
										{
											if (account.created_at != null && account.created_at.HasValue)
											{
												dateMemberList.Add(account.created_at.Value.ConvertToDateTime(), member);
												used = true;
											}
										}
										else
										{
											if (account.clan != null && account.clan.joined_at.HasValue)
											{
												dateMemberList.Add(account.clan.joined_at.Value.ConvertToDateTime(), member);
												used = true;
											}
										}
									}
									catch
									{
										await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Kon gegevens niet nakijken bij **`" + account.nickname.Replace("\\", string.Empty) + "`** met als ID **`" + account.account_id + "`");
									}
								}
							}
							if (!used)
							{
								dateNotFoundList.Add(member);
							}
						}
						else
						{
							dateNotFoundList.Add(member);
						}
					}
					else
					{
						dateNotFoundList.Add(member);
					}
				}
				List<KeyValuePair<DateTime, DiscordMember>> sortedDateMemberList = dateMemberList.OrderBy(p => p.Key).ToList();
				sortedDateMemberList.Reverse();
				memberList = [];
				foreach (KeyValuePair<DateTime, DiscordMember> item in sortedDateMemberList)
				{
					memberList.Add(item.Value);
				}
			}
			else
			{
				memberList = searchTerm.Contains('b') ? memberList.OrderBy(p => p.DisplayName).ToList() : memberList.OrderBy(p => p.Username).ToList();
			}

			int amountOfMembers = memberList.Count;
			List<DEF> deflist = [];

			if (amountOfMembers > 0)
			{
				deflist = _userService.ListInMemberEmbed(COLUMNS, memberList, searchTerm);
			}

			string sortedBy = "alfabetisch";
			if (searchTerm.Contains('o'))
			{
				sortedBy = "Creatie WG account";
			}
			else if (searchTerm.Contains('c'))
			{
				sortedBy = "Clanjoindatum";
			}
			EmbedOptions options = new()
			{
				Title = "Gebruikerslijst [" + ctx.Guild.Name.adaptToDiscordChat() + ": " + members.Count + "] (Gevonden: " + amountOfMembers + ") " + "(Gesorteerd: " + sortedBy + ")",
				Description = usersFound ? string.Empty : "Geen gebruikers gevonden die voldoen aan de zoekterm!",
				Fields = usersFound ? deflist : null
			};
			await _messageService.CreateEmbed(ctx.Channel, options);
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("Clan")]
	[Aliases("c", "cl", "cla")]
	[Description("Geeft info over de clan.")]
	public async Task Clan(CommandContext ctx, string clan_naam)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			try
			{
				WGClan clan = await _clanService.SearchForClan(ctx.Channel, ctx.Member, ctx.Guild.Name, clan_naam, false, ctx.User, ctx.Command);
				if (clan != null)
				{
					await _clanService.ShowClanInfo(ctx.Channel, clan);
				}
				else
				{
					await _messageService.SayNoResults(ctx.Channel, "Geen clan gevonden met deze naam");
				}
			}
			catch (TooManyResultsException ex)
			{
				_logger.LogWarning(ex, "Too many results found for clan search with name {ClanName}. Cause: {Message}", clan_naam, ex.Message);
				await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Te veel resultaten waren gevonden, wees specifieker!**");
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("Clanmembers")]
	[Aliases("cm", "clanm", "clanme", "clanmem", "clanmembe", "clanmember")]
	[Description("Geeft spelers van de clan.\n-s --> duid discordmembers aan\n-d --> sorteren op laatst actief")]
	public async Task ClanMembers(CommandContext ctx, params string[] optioneel_discordmembers_aanduiden_en_of_sorteren_op_laatst_actief__clan_naam)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			string searchTerm = "";
			string conditie = "";
			List<string> temp = GetSearchTermAndCondition(optioneel_discordmembers_aanduiden_en_of_sorteren_op_laatst_actief__clan_naam);
			if (temp[0] != string.Empty)
			{
				searchTerm = temp[0];
			}
			conditie = temp[1];

			WGClan clan = await _clanService.SearchForClan(ctx.Channel, ctx.Member, ctx.Guild.Name, conditie, true, ctx.User, ctx.Command);
			if (clan != null)
			{
				List<Members> playersList = !searchTerm.Contains('d') ? clan.members.OrderBy(p => p.account_name.ToLower()).ToList() : clan.members;

				List<DEF> defList = _userService.ListInPlayerEmbed(3, playersList, searchTerm, ctx.Guild);
				string sorting = "alfabetisch";
				if (searchTerm.Contains('d'))
				{
					sorting = "laatst actief";
				}
				EmbedOptions options = new()
				{
					Title = "Clanmembers van [" + clan.tag.adaptToDiscordChat() + "] (Gevonden: " + clan.members.Count + ") (Gesorteerd: " + sorting + ")",
					Fields = defList
				};
				await _messageService.CreateEmbed(ctx.Channel, options);
			}
			else
			{
				await _messageService.SayNoResults(ctx.Channel, "Geen clan gevonden met deze naam");
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("SpelerInfo")]
	[Aliases("si")]
	[Description("Geeft wotb info van een account.\n-i --> zoekt op spelerID")]
	public async Task PlayerInfo(CommandContext ctx, params string[] optioneel_zoeken_op_ID__ign_naam)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			// -i --> zoek op ID
			string searchTerm = "";
			string conditie;
			List<string> temp = GetSearchTermAndCondition(optioneel_zoeken_op_ID__ign_naam);
			if (temp[0] != string.Empty)
			{
				searchTerm = temp[0];
			}
			conditie = temp[1];

			if (searchTerm.Contains('i'))
			{
				if (long.TryParse(conditie, out long id))
				{
					try
					{
						WGAccount account = new(_configuration["NLBEBOT:WarGamingAppId"], id, false, true, true);
						await _userService.ShowMemberInfo(ctx.Channel, account);
					}
					catch (Exception ex)
					{
						await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**GebruikersID (`" + id + "`) kon niet gevonden worden!**");
						await _errorHandler.HandleErrorAsync($"User `{id}` could not be found (or loaded).", ex);
					}
				}
				else
				{
					await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Geef een ID!**");
				}
			}
			else
			{
				await _userService.SearchPlayer(ctx.Channel, ctx.Member, ctx.User, ctx.Guild.Name, conditie);
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("ResetHOF")]
	[Aliases("res", "rese", "rest", "rst", "rset", "reset")]
	[Description("Verwijdert alle opgeslagen replays in de Hall Of Fame.")]
	public async Task ResetHof(CommandContext ctx)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			DiscordChannel channel = await _channelService.GetHallOfFameChannel(ctx.Guild.Id);
			if (channel != null)
			{
				bool noErrors = true;
				List<Tuple<int, DiscordMessage>> tiersFound = [];
				try
				{
					IReadOnlyList<DiscordMessage> messages = await channel.GetMessagesAsync(100);

					foreach (DiscordMessage message in messages)
					{
						if (!message.Pinned)
						{
							if (message.Embeds != null)
							{
								if (message.Embeds.Count > 0)
								{
									for (int i = 1; i <= 10; i++)
									{
										bool containsItem = false;
										foreach (DiscordEmbed embed in message.Embeds)
										{
											if (embed.Title != null)
											{
												if (embed.Title.Contains(_discordMessageUtils.GetDiscordEmoji(Emoj.GetName(i)).ToString()))
												{
													tiersFound.Add(new Tuple<int, DiscordMessage>(i, message));
													containsItem = true;
													break;
												}
											}
										}
										if (containsItem)
										{
											break;
										}
									}
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					await _errorHandler.HandleErrorAsync("While getting the HOF messages (" + ctx.Command.Name + "): ", ex);
					noErrors = false;
				}
				if (noErrors)
				{
					if (await _hallOfFameService.CreateOrCleanHOFMessages(channel, tiersFound))
					{
						await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De Hall Of Fame is gereset!**");
					}
					else
					{
						await _messageService.SaySomethingWentWrong(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De Hall Of Fame kon de berichten niet resetten!**");
					}
				}
				else
				{
					await _messageService.SaySomethingWentWrong(ctx.Channel, ctx.Member, ctx.Guild.Name, "**De Hall Of Fame kon niet gereset worden door een interne reden!**");
				}
			}
			else
			{
				await _messageService.SaySomethingWentWrong(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Hall Of Fame kanaal kon niet gereset worden!**");
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("VerwijderSpelerHOF")]
	[Description("Verwijdert een bepaalde persoon van de HOF. (Hoofdlettergevoelig)")]
	public async Task RemovePlayerFromHOF(CommandContext ctx, string naam)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			bool foundAtLeastOnce = false;
			naam = naam.Replace(Constants.UNDERSCORE_REPLACEMENT_CHAR, '_');
			naam = naam.Replace('_', Constants.UNDERSCORE_REPLACEMENT_CHAR);
			DiscordChannel channel = await _channelService.GetHallOfFameChannel(ctx.Guild.Id);
			if (channel != null)
			{
				IReadOnlyList<DiscordMessage> messages = await channel.GetMessagesAsync(100);
				for (int i = 1; i <= 10; i++)
				{
					List<DiscordMessage> tempTierMessages = _hallOfFameService.GetTierMessages(i, messages);
					foreach (DiscordMessage message in tempTierMessages)
					{
						bool playerRemoved = false;
						List<Tuple<string, List<TankHof>>> tupleList = _hallOfFameService.ConvertHOFMessageToTupleListAsync(message, i);
						if (tupleList != null)
						{
							List<Tuple<string, List<TankHof>>> tempTupleList = [];
							for (int j = 0; j < tupleList.Count; j++)
							{
								if (tupleList[j].Item2 != null)
								{
									List<TankHof> tempTupleListItem2 = [];
									for (int k = 0; k < tupleList[j].Item2.Count; k++)
									{
										if (!tupleList[j].Item2[k].Speler.Equals(naam))
										{
											tempTupleListItem2.Add(tupleList[j].Item2[k]);
										}
										else
										{
											playerRemoved = true;
										}
									}
									if (tempTupleListItem2.Count > 0)
									{
										tempTupleList.Add(new Tuple<string, List<TankHof>>(tempTupleListItem2[0].Tank, tempTupleListItem2));
									}
								}
							}
							tupleList = tempTupleList;
						}
						if (playerRemoved)
						{
							if (!foundAtLeastOnce)
							{
								foundAtLeastOnce = true;
							}
							await _hallOfFameService.EditHOFMessage(message, tupleList);
							await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**" + naam + " werd verwijdert uit tier " + _discordMessageUtils.GetDiscordEmoji(Emoj.GetName(i)) + "**");
						}
					}
				}
			}
			if (!foundAtLeastOnce)
			{
				await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Persoon met `" + naam + "` als naam komt niet voor in de HOF.**");
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("HernoemSpelerHOF")]
	[Description("Verandert de naam in de HOF naar een andere naam. (Hoofdlettergevoelig)")]
	public async Task RenamePlayerHOF(CommandContext ctx, string oldName, string niewe_naam)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			bool foundAtLeastOnce = false;
			oldName = oldName.Replace(Constants.UNDERSCORE_REPLACEMENT_CHAR, '_');
			oldName = oldName.Replace('_', Constants.UNDERSCORE_REPLACEMENT_CHAR);
			niewe_naam = niewe_naam.Replace('_', Constants.UNDERSCORE_REPLACEMENT_CHAR);
			DiscordChannel channel = await _channelService.GetHallOfFameChannel(ctx.Guild.Id);
			if (channel != null)
			{
				IReadOnlyList<DiscordMessage> messages = await channel.GetMessagesAsync(100);
				for (int i = 1; i <= 10; i++)
				{
					List<DiscordMessage> tempTierMessages = _hallOfFameService.GetTierMessages(i, messages);
					foreach (DiscordMessage message in tempTierMessages)
					{
						bool nameChanged = false;
						List<Tuple<string, List<TankHof>>> tupleList = _hallOfFameService.ConvertHOFMessageToTupleListAsync(message, i);
						if (tupleList != null)
						{
							foreach (Tuple<string, List<TankHof>> tupleItem in tupleList)
							{
								if (tupleItem.Item2 != null)
								{
									foreach (TankHof tankHofItem in tupleItem.Item2)
									{
										if (tankHofItem.Speler.Equals(oldName))
										{
											nameChanged = true;
											tankHofItem.Speler = niewe_naam;
										}
									}
								}
							}
						}
						if (nameChanged)
						{
							if (!foundAtLeastOnce)
							{
								foundAtLeastOnce = true;
							}
							await _hallOfFameService.EditHOFMessage(message, tupleList);
							await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**" + oldName + " werd verandert naar " + niewe_naam + " in tier " + _discordMessageUtils.GetDiscordEmoji(Emoj.GetName(i)) + "**");
						}
					}
				}
			}
			if (!foundAtLeastOnce)
			{
				await _messageService.SendMessage(ctx.Channel, ctx.Member, ctx.Guild.Name, "**Persoon met `" + oldName + "` als naam komt niet voor in de HOF.**");
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("hof")]
	[Aliases("hf")]
	[Description("Geeft een lijst van de spelers die in de Hall of Fame voorkomen.")]
	public async Task Hof(CommandContext ctx)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			List<Tuple<string, List<TankHof>>> playerList = await _hallOfFameService.GetTankHofsPerPlayer(ctx.Guild.Id);
			playerList = playerList.OrderBy(x => x.Item2.Count).ToList();
			playerList.Reverse();
			StringBuilder sb = new("```");
			bool firstTime = true;
			foreach (Tuple<string, List<TankHof>> player in playerList)
			{
				if (firstTime)
				{
					firstTime = false;
				}
				else
				{
					sb.Append("\n");
				}
				sb.Append(player.Item1.Replace(Constants.UNDERSCORE_REPLACEMENT_CHAR, '_'));
				for (int i = player.Item1.Length; i < MAX_NAME_LENGTH_IN_WOTB + 7; i++) //25 = max name length in wotb (minimum 2)
				{
					sb.Append(" ");
				}
				sb.Append(player.Item2.Count.ToString());
			}
			sb.Append("```");
			EmbedOptions options = new()
			{
				Title = "Hall Of Fame plekken per speler",
				Description = sb.ToString()
			};
			await _messageService.CreateEmbed(ctx.Channel, options);
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("hofplayer")]
	[Aliases("hofp", "hp", "hofplaye", "hofplay", "hofpla", "hofpl", "hfplayer")]
	[Description("Geeft een lijst van plekken dat de speler in de Hall Of Fame gehaald heeft.")]
	public async Task HofPlayer(CommandContext ctx, string name)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			name = name.Replace('_', Constants.UNDERSCORE_REPLACEMENT_CHAR);
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			List<Tuple<string, List<TankHof>>> playerList = await _hallOfFameService.GetTankHofsPerPlayer(ctx.Guild.Id);
			List<DEF> defList = [];
			playerList.Reverse();
			bool found = false;
			StringBuilder sb = new();
			foreach (Tuple<string, List<TankHof>> player in playerList)
			{
				if (name.ToLower().Equals(player.Item1.ToLower()))
				{
					List<int> alreadyUsedTiers = [];
					List<string> alreadyUsedTanks = [];
					bool firstTime = true;
					int lastTier = 0;
					foreach (TankHof tank in player.Item2)
					{
						if (!alreadyUsedTiers.Contains(tank.Tier))
						{
							alreadyUsedTiers.Add(tank.Tier);
							alreadyUsedTanks = [];
							if (lastTier > 0)
							{
								sb.Append("```");
								DEF tempDef = new()
								{
									Name = "Tier " + lastTier,
									Inline = false,
									Value = sb.ToString()
								};
								defList.Add(tempDef);
								sb = new StringBuilder();
							}
							sb.Append("```");
						}
						if (firstTime)
						{
							firstTime = false;
						}
						else
						{
							sb.Append("\n");
						}
						if (!alreadyUsedTanks.Contains(tank.Tank))
						{
							alreadyUsedTanks.Add(tank.Tank);
							if (alreadyUsedTanks.Count > 1)
							{
								sb.Append("\n");
							}
						}
						if (tank.Tank.Length > MAX_TANK_NAME_LENGTH_IN_WOTB)
						{
							sb.Append(tank.Tank.Substring(0, MAX_TANK_NAME_LENGTH_IN_WOTB));
						}
						else
						{
							sb.Append(tank.Tank);
						}
						for (int i = tank.Tank.Length < MAX_TANK_NAME_LENGTH_IN_WOTB ? tank.Tank.Length : MAX_TANK_NAME_LENGTH_IN_WOTB; i < MAX_TANK_NAME_LENGTH_IN_WOTB + 2; i++)
						{
							sb.Append(" ");
						}
						sb.Append("nr." + tank.Place.ToString());
						for (int i = 0; i < 7; i++)
						{
							sb.Append(" ");
						}
						sb.Append(tank.Damage + " dmg");
						lastTier = tank.Tier;
					}
					sb.Append("```");
					DEF tempDef2 = new()
					{
						Name = "Tier " + lastTier,
						Inline = false,
						Value = sb.ToString()
					};
					defList.Add(tempDef2);
					found = true;
					break;
				}
			}
			EmbedOptions options = new()
			{
				Title = "Hall Of Fame plekken van " + name.Replace(Constants.UNDERSCORE_REPLACEMENT_CHAR, '_'),
				Description = found ? string.Empty : "Deze speler heeft nog geen plekken in de Hall Of Fame gehaald.",
				Fields = defList,
			};
			await _messageService.CreateEmbed(ctx.Channel, options);
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	[Command("Weekly")]
	[Description("Start het proces van het instellen van de tank voor het wekelijkse event.")]
	public async Task Weekly(CommandContext ctx, params string[] optioneel_tank_naam)
	{
		if (_botState.IgnoreCommands)
		{
			return;
		}

		if (_userService.HasPermission(ctx.Member, ctx.Command))
		{
			await _messageService.ConfirmCommandExecuting(ctx.Message);
			if (optioneel_tank_naam.Length > 0)
			{
				StringBuilder sb = new();
				for (short i = 0; i < optioneel_tank_naam.Length; i++)
				{
					if (i > 0)
					{
						sb.Append(' ');
					}
					sb.Append(optioneel_tank_naam[i]);
				}
				await _weeklyEventHandler.CreateNewWeeklyEvent(sb.ToString(), await _channelService.GetWeeklyEventChannel());
			}
			else
			{
				await ctx.Member.SendMessageAsync("Hallo\nWelke tank wil je bij het volgende wekelijkse event instellen?"); //deze triggert OOK het dmchannelcreated event
				_botState.WeeklyEventWinner = new Tuple<ulong, DateTime>(ctx.Member.Id, DateTime.Now);
			}
			await _messageService.ConfirmCommandExecuted(ctx.Message);
		}
		else
		{
			await _messageService.SayTheUserIsNotAllowed(ctx.Channel);
		}
	}

	private static List<string> GetSearchTermAndCondition(params string[] parameter)
	{
		string searchTerm = string.Empty;
		string conditie = string.Empty;
		if (parameter.Length > 1)
		{
			// -s --> duid discordmembers aan met ``
			StringBuilder sb = new();
			for (int i = 0; i < parameter.Length; i++)
			{
				if (i == 0)
				{
					if (parameter[0].StartsWith('-'))
					{
						searchTerm = parameter[0];
					}
					else
					{
						sb.Append(parameter[0]);
					}
				}
				else
				{
					if (sb.Length > 0)
					{
						sb.Append(' ');
					}
					sb.Append(parameter[i]);
				}
			}
			conditie = sb.ToString();
		}
		else if (parameter.Length == 1)
		{
			conditie = parameter[0];
		}
		List<string> temp = [];
		temp.Add(searchTerm);
		temp.Add(conditie);
		return temp;
	}
}
