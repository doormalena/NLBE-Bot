namespace NLBE_Bot.Services;

using DSharpPlus.Entities;
using JsonObjectConverter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Tournament;

internal class TournamentService(ILogger<TournamentService> logger, IOptions<BotOptions> options, IUserService userService, IChannelService channelService, IMessageService messageService,
		IDiscordMessageUtils discordMessageUtils, IDiscordClient discordClient) : ITournamentService
{
	private readonly ILogger<TournamentService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IDiscordMessageUtils _discordMessageUtils = discordMessageUtils ?? throw new ArgumentNullException(nameof(discordMessageUtils));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
	private readonly IDiscordClient _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));

	public async Task GenerateLogMessage(IDiscordMessage message, IDiscordChannel toernooiAanmeldenChannel, ulong userID, string emojiAsEmoji)
	{
		bool addInLog = true;
		if (message.Author != null)
		{
			if (!message.Author.Id.Equals(Constants.NLBE_BOT))
			{
				addInLog = false;
			}
		}
		if (addInLog)
		{
			if (Emoj.GetIndex(_discordMessageUtils.GetEmojiAsString(emojiAsEmoji)) > 0)
			{
				try
				{
					bool botReactedWithThisEmoji = false;
					IReadOnlyList<IDiscordUser> userListOfThisEmoji = await message.GetReactionsAsync(_discordMessageUtils.GetDiscordEmoji(emojiAsEmoji));
					foreach (IDiscordUser user in userListOfThisEmoji)
					{
						if (user.Id.Equals(Constants.NLBE_BOT))
						{
							botReactedWithThisEmoji = true;
						}
					}
					if (botReactedWithThisEmoji)
					{
						IDiscordMember member = await toernooiAanmeldenChannel.Guild.GetMemberAsync(userID);
						if (member != null)
						{
							string organisator = await GetOrganisator(await toernooiAanmeldenChannel.GetMessageAsync(message.Id));
							string logMessage = "Teams|" + member.DisplayName.AdaptToChat() + "|" + emojiAsEmoji + "|" + organisator + "|" + userID;
							await WriteInLog(message.Timestamp.LocalDateTime.ConvertToDate(), logMessage);
						}
					}
					else
					{
						await message.DeleteReactionsEmojiAsync(_discordMessageUtils.GetDiscordEmoji(emojiAsEmoji));
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error while adding to log.");
				}
			}
		}
	}

	public async Task<List<WGTournament>> InitialiseTournaments(bool all)
	{
		string tournamentJson = await Tournaments.tournamentsToString(_options.WotbApi.ApplicationId);
		Json json = new(tournamentJson, "Tournaments");
		List<WGTournament> tournamentsList = [];

		if (json.subJsons != null)
		{
			foreach (Json subjson in json.subJsons)
			{
				if (subjson.head.ToLower().Equals("data"))
				{
					foreach (Json subsubjson in subjson.subJsons)
					{
						Tournaments tournaments = new(subsubjson);
						if (tournaments.start_at.HasValue)
						{
							if (tournaments.start_at.Value > DateTime.Now || all)
							{
								string wgTournamentJsonString = await WGTournament.tournamentsToString(_options.WotbApi.ApplicationId, tournaments.tournament_id);
								Json wgTournamentJson = new(wgTournamentJsonString, "WGTournament");
								WGTournament eenToernooi = new(wgTournamentJson, _options.WotbApi.ApplicationId);
								tournamentsList.Add(eenToernooi);
							}
						}
					}
				}
			}
		}
		tournamentsList.Reverse();
		return tournamentsList;
	}

	public async Task ShowTournamentInfo(IDiscordChannel channel, WGTournament tournament, string titel)
	{
		List<DEF> deflist = [];
		DEF newDef1 = new()
		{
			Name = "Titel",
			Value = tournament.title,
			Inline = true
		};
		deflist.Add(newDef1);
		DEF newDef2 = new()
		{
			Name = "Status",
			Value = tournament.status.Replace('_', ' '),
			Inline = true
		};
		deflist.Add(newDef2);
		if (tournament.other_rules != null)
		{
			if (tournament.other_rules.Length > 0)
			{
				DEF newDef7 = new()
				{
					Name = "Prijsbeschrijving",
					Value = tournament.other_rules,
					Inline = true
				};
				deflist.Add(newDef7);
			}
		}
		if (tournament.start_at.HasValue)
		{
			DEF newDef5 = new()
			{
				Name = "Start op"
			};
			string[] splittedx = tournament.start_at.Value.ConvertToDate().Split(' ');
			newDef5.Value = splittedx[0] + " " + splittedx[1];
			newDef5.Inline = true;
			deflist.Add(newDef5);
		}
		if (tournament.registration_start_at.HasValue)
		{
			DEF newDef5 = new()
			{
				Name = "Registreren"
			};
			string[] splittedx = tournament.registration_start_at.Value.ConvertToDate().Split(' ');
			StringBuilder sb = new("Vanaf\n" + splittedx[0] + " " + splittedx[1]);
			if (tournament.registration_end_at.HasValue)
			{
				string[] splittedb = tournament.registration_end_at.Value.ConvertToDate().Split(' ');
				sb.Append("\ntot\n" + splittedb[0] + " " + splittedb[1]);
			}
			newDef5.Value = sb.ToString();
			newDef5.Inline = true;
			deflist.Add(newDef5);
		}
		if (tournament.matches_start_at.HasValue)
		{
			DEF newDef7 = new()
			{
				Name = "Matchen beginnen op"
			};
			string[] splittedb = tournament.matches_start_at.Value.ConvertToDate().Split(' ');
			newDef7.Value = splittedb[0] + " " + splittedb[1];
			newDef7.Inline = true;
			deflist.Add(newDef7);
		}
		if (tournament.end_at.HasValue)
		{
			DEF newDef7 = new()
			{
				Name = "Matchen eindigen op"
			};
			string[] splittedb = tournament.end_at.Value.ConvertToDate().Split(' ');
			newDef7.Value = splittedb[0] + " " + splittedb[1];
			newDef7.Inline = true;
			deflist.Add(newDef7);
		}
		if (tournament.min_players_count > 0)
		{
			DEF newDef7 = new()
			{
				Name = "Minimum spelers vereist",
				Value = tournament.min_players_count.ToString(),
				Inline = true
			};
			deflist.Add(newDef7);
		}
		if (tournament.prize_description != null)
		{
			if (tournament.prize_description.Length > 0)
			{
				DEF newDef7 = new()
				{
					Name = "Prijsbeschrijving",
					Value = tournament.prize_description,
					Inline = true
				};
				deflist.Add(newDef7);
			}
		}
		if (tournament.fee != null)
		{
			if (tournament.fee.amount > 0)
			{
				DEF newDef7 = new()
				{
					Name = "Inschrijvingsgeld",
					Value = tournament.fee.amount.ToString() + (!string.IsNullOrEmpty(tournament.fee.currency) ? " (" + tournament.fee.currency + ")" : string.Empty),
					Inline = true
				};
				deflist.Add(newDef7);
			}
		}
		if (tournament.winner_award != null)
		{
			if (tournament.winner_award.amount > 0)
			{
				DEF newDef7 = new()
				{
					Name = "Winnaarsgeld",
					Value = tournament.winner_award.amount.ToString() + (!string.IsNullOrEmpty(tournament.winner_award.currency) ? " (" + tournament.winner_award.currency + ")" : string.Empty),
					Inline = true
				};
				deflist.Add(newDef7);
			}
		}
		if (tournament.media_Links != null)
		{
			if (tournament.media_Links.url.Length > 0)
			{
				DEF newDef7 = new()
				{
					Name = "Extra media link",
					Value = "[" + tournament.media_Links.url.Replace('_', Constants.UNDERSCORE_REPLACEMENT_CHAR) + "](" + tournament.media_Links.url + ")",
					Inline = true
				};
				deflist.Add(newDef7);
			}
		}

		if (tournament.stages != null)
		{
			int hoogsteTier = 0;
			int laagsteTier = 0;
			int bestOff = 0;
			string type = string.Empty;
			string state = string.Empty;
			foreach (Stage stage in tournament.stages)
			{
				if (hoogsteTier < stage.max_tier)
				{
					hoogsteTier = stage.max_tier;
					if (laagsteTier == 0)
					{
						laagsteTier = hoogsteTier;
					}
				}
				if (laagsteTier < stage.min_tier)
				{
					laagsteTier = stage.min_tier;
				}
				if (bestOff == 0)
				{
					bestOff = stage.battle_limit;
				}
				if (stage.type != null)
				{
					if (stage.type.Length > 0)
					{
						if (type.Length > 0)
						{
							switch (stage.type.ToLower())
							{
								case "rr":
									type = "Round robin";
									break;
								case "se":
									type = "Single elimination";
									break;
								case "de":
									type = "Double elimination";
									break;
							}
						}
					}
				}
				if (stage.state != null)
				{
					if (stage.state.Length > 0)
					{
						if (state.Length > 0)
						{
							state = stage.state.Replace("_", string.Empty);
						}
					}
				}
			}
			if (hoogsteTier > 0)
			{
				string tiers = Emoj.GetName(laagsteTier) + (laagsteTier != hoogsteTier ? " tot " + Emoj.GetName(hoogsteTier) : string.Empty);
				DEF newDef3 = new()
				{
					Name = "Tiers",
					Value = tiers,
					Inline = true
				};
				deflist.Add(newDef3);
			}
			if (type.Length > 0)
			{
				DEF newDef3 = new()
				{
					Name = "Type",
					Value = type,
					Inline = true
				};
				deflist.Add(newDef3);
			}
			if (state.Length > 0)
			{
				DEF newDef3 = new()
				{
					Name = "Staat",
					Value = state,
					Inline = true
				};
				deflist.Add(newDef3);
			}
			if (bestOff > 0)
			{
				DEF newDef3 = new()
				{
					Name = "Best of",
					Value = bestOff.ToString(),
					Inline = true
				};
				deflist.Add(newDef3);
			}
		}

		if (tournament.rules != null)
		{
			if (tournament.rules.Length > 0)
			{
				bool voegToe = true;
				if (tournament.description != null)
				{
					if (tournament.rules.Equals(tournament.description))
					{
						voegToe = false;
					}
				}
				if (voegToe)
				{
					string tempRules = tournament.rules.AdaptLink().AdaptToChat().AdaptMutlipleLines();
					if (tempRules.Length > 1024)
					{
						StringBuilder sbRules = new();
						string[] splitted = tournament.description.Split('\n');
						for (int i = 0; i < splitted.Length; i++)
						{
							if (splitted[i].EndsWith(':'))
							{
								bool firstLine = true;
								StringBuilder sbTemp = new();
								for (int j = i + 1; j < splitted.Length; j++)
								{
									if (splitted[j].Length == 0 && !firstLine)
									{
										DEF newDefx = new()
										{
											Name = splitted[i].AdaptToChat(),
											Value = sbTemp.ToString().AdaptLink().AdaptToChat(),
											Inline = true
										};
										deflist.Add(newDefx);
										i = j - 1;
										break;
									}
									else
									{
										sbTemp.AppendLine(splitted[j]);
									}
									if (firstLine)
									{
										firstLine = false;
									}
									if (j + 1 == splitted.Length)
									{
										DEF newDefx = new()
										{
											Name = splitted[i].AdaptToChat(),
											Value = sbTemp.ToString().AdaptLink().AdaptToChat(),
											Inline = true
										};
										deflist.Add(newDefx);
										i = j;
									}
								}
							}
							else
							{
								sbRules.AppendLine(splitted[i]);
							}
						}
						tempRules = sbRules.ToString().AdaptLink().AdaptToChat().AdaptMutlipleLines();

					}
					DEF newDef4 = new()
					{
						Name = "Regels",
						Value = tempRules,
						Inline = true
					};
					deflist.Add(newDef4);
				}
			}
		}
		string tempDescription = tournament.description.AdaptLink().AdaptToChat().AdaptMutlipleLines();
		if (tempDescription.Length > 1024)
		{
			StringBuilder sbDescription = new();
			string[] splitted = tournament.description.Split('\n');
			for (int i = 0; i < splitted.Length; i++)
			{
				if (splitted[i].EndsWith(':'))
				{
					bool firstLine = true;
					StringBuilder sbTemp = new();
					for (int j = i + 1; j < splitted.Length; j++)
					{
						if (splitted[j].Length == 0 && !firstLine)
						{
							DEF newDefx = new()
							{
								Name = splitted[i].AdaptToChat(),
								Value = sbTemp.ToString().AdaptLink().AdaptToChat(),
								Inline = true
							};
							deflist.Add(newDefx);
							i = j - 1;
							break;
						}
						else
						{
							sbTemp.AppendLine(splitted[j]);
						}
						if (firstLine)
						{
							firstLine = false;
						}
						if (j + 1 == splitted.Length)
						{
							DEF newDefx = new()
							{
								Name = splitted[i].AdaptToChat(),
								Value = sbTemp.ToString().AdaptLink().AdaptToChat(),
								Inline = true
							};
							deflist.Add(newDefx);
							i = j;
						}
					}
				}
				else
				{
					sbDescription.AppendLine(splitted[i]);
				}
			}
			tempDescription = sbDescription.ToString().AdaptLink().AdaptToChat().AdaptMutlipleLines();

		}
		if (tempDescription.Length <= 1024)
		{
			DEF newDef3 = new()
			{
				Name = "Toernooi beschrijving",
				Value = tempDescription,
				Inline = false
			};
			deflist.Add(newDef3);
		}

		EmbedOptions options = new()
		{
			Title = titel,
			Fields = deflist,
			ImageUrl = tournament.logo != null ? tournament.logo.original ?? string.Empty : string.Empty,
		};
		await _messageService.CreateEmbed(channel, options);
	}

	private async Task WriteInLog(string date, string message)
	{
		IDiscordChannel logChannel = await _channelService.GetLogChannelAsync();
		if (logChannel != null)
		{
			await logChannel.SendMessageAsync(date + "|" + message);
		}
		else
		{
			_logger.LogError("Could not find log channel, message: {Date}|{Message}", date, message);
		}
	}

	private async Task<string> GetOrganisator(IDiscordMessage message)
	{
		IReadOnlyList<IDiscordEmbed> embeds = message.Embeds;
		if (embeds.Count > 0)
		{
			foreach (IDiscordEmbed anEmbed in embeds)
			{
				foreach (DiscordEmbedField field in anEmbed.Fields)
				{
					if (field.Name.ToLower().Equals("organisator"))
					{
						return field.Value;
					}
				}
			}
		}
		else
		{
			if (message.Author != null)
			{
				IDiscordMember member = await _userService.GetDiscordMember(message.Channel.Guild, message.Author.Id);
				if (member != null)
				{
					return member.DisplayName;
				}
			}
		}
		return string.Empty;
	}
	public async Task<List<Tier>> ReadTeams(IDiscordChannel channel, IDiscordMember member, string guildName, string[] parameters_as_in_hoeveelste_team)
	{
		if (parameters_as_in_hoeveelste_team.Length <= 1)
		{
			int hoeveelste = 1;
			bool isInt = true;
			if (parameters_as_in_hoeveelste_team.Length > 0)
			{
				try
				{
					hoeveelste = Convert.ToInt32(parameters_as_in_hoeveelste_team[0]);
				}
				catch
				{
					isInt = false;
				}
			}
			if (isInt)
			{
				bool goodNumber = true;
				if (hoeveelste >= 1)
				{
					if (hoeveelste > 100)
					{
						await _messageService.SendMessage(channel, member, guildName, "**Het getal mag maximum 100 zijn!**");
						goodNumber = false;
					}
					else
					{
						hoeveelste--;
					}
				}
				else if (hoeveelste < 1)
				{
					await _messageService.SendMessage(channel, member, guildName, "**Het getal moet groter zijn dan 0!**");
					goodNumber = false;
				}

				if (goodNumber)
				{
					IDiscordChannel toernooiAanmeldenChannel = await _channelService.GetToernooiAanmeldenChannelAsync();
					if (toernooiAanmeldenChannel != null)
					{
						List<IDiscordMessage> messages = [];
						try
						{
							IReadOnlyList<IDiscordMessage> xMessages = toernooiAanmeldenChannel.GetMessagesAsync(hoeveelste + 1).Result;
							foreach (IDiscordMessage message in xMessages)
							{
								messages.Add(message);
							}
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Could not load messages from {ChannelName}:", toernooiAanmeldenChannel.Name);
						}
						if (messages.Count == hoeveelste + 1)
						{
							IDiscordMessage theMessage = messages[hoeveelste];
							if (theMessage != null)
							{
								if (theMessage.Author.Id.Equals(Constants.NLBE_BOT))
								{
									IDiscordChannel logChannel = await _channelService.GetLogChannelAsync();

									if (logChannel.Inner != null)
									{
										IReadOnlyList<IDiscordMessage> logMessages = await logChannel.GetMessagesAsync(100);
										Dictionary<DateTime, List<IDiscordMessage>> sortedMessages = _discordMessageUtils.SortMessages(logMessages);
										List<Tier> tiers = [];

										foreach (KeyValuePair<DateTime, List<IDiscordMessage>> sMessage in sortedMessages)
										{
											string xdate = theMessage.Timestamp.ConvertToDate();
											string ydate = sMessage.Key.ConvertToDate();

											if (xdate.Equals(ydate))
											{
												sMessage.Value.Sort((x, y) => x.Inner.Timestamp.CompareTo(y.Inner.Timestamp));
												foreach (IDiscordMessage discMessage in sMessage.Value)
												{
													string[] splitted = discMessage.Content.Split(Constants.LOG_SPLIT_CHAR);
													if (splitted[1].ToLower().Equals("teams"))
													{
														Tier newTeam = new();
														bool found = false;
														foreach (Tier aTeam in tiers)
														{
															if (aTeam.TierNummer.Equals(_discordMessageUtils.GetEmojiAsString(splitted[3])))
															{
																found = true;
																newTeam = aTeam;
																break;
															}
														}
														ulong id = 0;
														if (splitted.Length > 4)
														{
															_ = ulong.TryParse(splitted[4], out id);
														}
														newTeam.AddDeelnemer(splitted[2], id);
														if (!found)
														{
															if (newTeam.TierNummer.Equals(string.Empty))
															{
																newTeam.TierNummer = _discordMessageUtils.GetEmojiAsString(splitted[3]);
																string emojiAsString = _discordMessageUtils.GetEmojiAsString(splitted[3]);
																int index = Emoj.GetIndex(emojiAsString);
																newTeam.Index = index;
															}
															if (newTeam.Organisator.Equals(string.Empty))
															{
																newTeam.Organisator = splitted[4].Replace("\\", string.Empty);
															}
															tiers.Add(newTeam);
														}
													}
												}
												break;
											}
										}

										tiers = EditWhenRedundance(tiers);
										tiers.Sort((x, y) => x.Index.CompareTo(y.Index));

										return tiers;

									}
									else
									{
										_logger.LogError("Could not find log channel for reading teams.");
									}
								}
								else
								{
									Dictionary<IDiscordEmoji, List<IDiscordUser>> reactions = _discordMessageUtils.SortReactions(theMessage);

									List<Tier> teams = [];
									foreach (KeyValuePair<IDiscordEmoji, List<IDiscordUser>> reaction in reactions)
									{
										Tier aTeam = new();
										foreach (IDiscordUser user in reaction.Value)
										{
											string displayName = user.Inner.Username;
											IDiscordMember memberx = await toernooiAanmeldenChannel.Guild.GetMemberAsync(user.Id);
											if (memberx != null)
											{
												displayName = memberx.DisplayName;
											}
											aTeam.AddDeelnemer(displayName, user.Inner.Id);
										}
										if (aTeam.Organisator.Equals(string.Empty))
										{
											foreach (KeyValuePair<ulong, IDiscordGuild> aGuild in _discordClient.Guilds)
											{
												if (aGuild.Key == _options.ServerId)
												{
													IDiscordMember theMemberAuthor = await _userService.GetDiscordMember(aGuild.Value, theMessage.Author.Id);
													if (theMemberAuthor != null)
													{
														aTeam.Organisator = theMemberAuthor.DisplayName;
													}
												}
											}
											if (aTeam.Organisator.Equals(string.Empty))
											{
												aTeam.Organisator = "Niet gevonden";
											}
										}
										if (aTeam.TierNummer.Equals(string.Empty))
										{
											aTeam.TierNummer = reaction.Key.Inner;
											string emojiAsString = _discordMessageUtils.GetEmojiAsString(reaction.Key.Inner);
											int index = Emoj.GetIndex(emojiAsString);
											if (index != 0)
											{
												aTeam.Index = index;
												teams.Add(aTeam);
											}
										}
									}
									teams = EditWhenRedundance(teams);
									teams.Sort((x, y) => x.Index.CompareTo(y.Index));
									List<DEF> deflist = [];
									foreach (Tier aTeam in teams)
									{
										DEF def = new()
										{
											Inline = true,
											Name = "Tier " + aTeam.TierNummer
										};
										int counter = 1;
										StringBuilder sb = new();
										foreach (Tuple<ulong, string> user in aTeam.Deelnemers)
										{
											string tempName = string.Empty;
											IDiscordMember tempUser = await channel.Guild.GetMemberAsync(user.Item1);
											tempName = tempUser != null ? tempUser.DisplayName : user.Item2;
											sb.AppendLine(counter + ". " + tempName);
											counter++;
										}
										def.Value = sb.ToString();
										deflist.Add(def);
									}

									EmbedOptions options = new()
									{
										Title = "Teams",
										Description = teams.Count > 0 ? string.Empty : "Geen teams",
										Fields = deflist,
									};
									await _messageService.CreateEmbed(channel, options);
									return [];
								}
							}
							else
							{
								await _messageService.SendMessage(channel, member, guildName, "**Het bericht kon niet gevonden worden!**");
							}
						}
						else
						{
							await _messageService.SendMessage(channel, member, guildName, "**Dit bericht kon niet gevonden worden!**");
						}
					}
					else
					{
						await _messageService.SendMessage(channel, member, guildName, "**Het kanaal #Toernooi-aanmelden kon niet gevonden worden!**");
					}
				}
			}
			else
			{
				await _messageService.SendMessage(channel, member, guildName, "**Je moet cijfer meegeven!**");
			}
		}
		else
		{
			await _messageService.SendMessage(channel, member, guildName, "**Je mag maar één cijfer meegeven!**");
		}
		return null;
	}
	private static List<Tier> EditWhenRedundance(List<Tier> teams)
	{
		if (teams.Count > 1)
		{
			List<Tier> newTeams = [];
			int aCounter = 0;
			foreach (Tier aTeam in teams)
			{
				Tier newTeam = new();
				foreach (Tuple<ulong, string> aDeelnemer in aTeam.Deelnemers)
				{
					bool neverFound = true;
					int bCounter = 0;
					int amountFound = 0;
					foreach (Tier bTeam in teams)
					{
						if (aCounter != bCounter)
						{
							foreach (Tuple<ulong, string> bDeelnemer in bTeam.Deelnemers)
							{
								if (aDeelnemer.Equals(bDeelnemer))
								{
									neverFound = false;
									amountFound++;
								}
							}
						}
						bCounter++;
					}
					if (neverFound)
					{
						newTeam.AddDeelnemer("**" + aDeelnemer.Item2 + "**", aDeelnemer.Item1);
						newTeam.Uniekelingen.Add(aDeelnemer.Item2);
					}
					else if (amountFound == 1)
					{
						newTeam.AddDeelnemer("`" + aDeelnemer.Item2.Replace("\\", string.Empty) + "`", aDeelnemer.Item1);
					}
					else
					{
						newTeam.AddDeelnemer(aDeelnemer.Item2, aDeelnemer.Item1);
					}
				}
				newTeam.Datum = aTeam.Datum;
				newTeam.Organisator = aTeam.Organisator;
				newTeam.TierNummer = aTeam.TierNummer;
				newTeam.Index = aTeam.Index;
				newTeams.Add(newTeam);
				aCounter++;
			}
			return newTeams;
		}
		else
		{
			return teams;
		}
	}
	public async Task<List<Tuple<ulong, string>>> GetIndividualParticipants(List<Tier> teams, IDiscordGuild guild)
	{
		List<Tuple<ulong, string>> participants = [];
		if (teams != null)
		{
			if (teams.Count > 0)
			{
				participants.Add(new Tuple<ulong, string>(0, teams[0].Organisator));
				foreach (Tier team in teams)
				{
					foreach (Tuple<ulong, string> participant in team.Deelnemers)
					{
						string temp = string.Empty;
						try
						{
							IDiscordMember tempMember = await guild.GetMemberAsync(participant.Item1);
							if (tempMember != null && tempMember.DisplayName != null && tempMember.DisplayName.Length > 0)
							{
								temp = tempMember.DisplayName;
							}
						}
						catch (Exception ex)
						{
							_logger.LogDebug(ex, ex.Message);
						}

						temp = string.IsNullOrEmpty(temp) ? participant.Item2 : participant.Item2.RemoveSyntax();
						bool alreadyInList = false;
						foreach (Tuple<ulong, string> participantX in participants)
						{
							if ((participantX.Item1.Equals(participant.Item1) && participant.Item1 > 0) || participantX.Item2.Equals(participant.Item2.RemoveSyntax()))
							{
								alreadyInList = true;
								break;
							}
						}
						if (!alreadyInList)
						{
							participants.Add(new Tuple<ulong, string>(participant.Item1, temp));
						}
					}
				}

				return participants.AsEnumerable().OrderBy(x => x.Item2).ToList();
			}
			else
			{
				return participants;
			}
		}
		else
		{
			return participants;
		}
	}
	public bool CheckIfAllWithinRange(string[] tiers, int min, int max)
	{
		bool allWithinRange = true;
		for (int i = 0; i < tiers.Length; i++)
		{
			int temp = Convert.ToInt32(tiers[i]);
			if (temp < min || temp > max)
			{
				allWithinRange = false;
				break;
			}
		}
		return allWithinRange;
	}
	public async Task<List<string>> GetMentions(List<Tuple<ulong, string>> memberList, ulong guildID)
	{
		IDiscordGuild guild = await _discordClient.GetGuildAsync(guildID);
		if (guild != null)
		{
			List<string> mentionList = [];
			foreach (Tuple<ulong, string> member in memberList)
			{
				bool addByString = true;
				if (member.Item1 > 1)
				{
					IDiscordMember tempMember = await guild.GetMemberAsync(member.Item1);
					if (tempMember != null)
					{
						if (tempMember.Mention != null)
						{
							if (tempMember.Mention.Length > 0)
							{
								addByString = false;
								mentionList.Add(tempMember.Mention);
							}
						}
					}
				}
				if (addByString)
				{
					bool added = false;
					IReadOnlyCollection<IDiscordMember> usersList = await guild.GetAllMembersAsync();
					if (usersList != null)
					{
						foreach (IDiscordMember memberItem in usersList)
						{
							if (memberItem.DisplayName != null)
							{
								if (memberItem.DisplayName.ToLower().Equals(member.Item2.ToLower()))
								{
									if (memberItem.Mention != null)
									{
										if (memberItem.Mention.Length > 0)
										{
											mentionList.Add(memberItem.Mention);
											added = true;
										}
									}
									break;
								}
							}
						}
					}
					if (!added)
					{
						mentionList.Add("@" + member.Item2);
					}
				}
			}
			return mentionList;
		}
		return null;
	}
}
