namespace NLBE_Bot.Services;

using DiscordHelper;
using DSharpPlus.Entities;
using FMWOTB.Tournament;
using JsonObjectConverter;
using Microsoft.Extensions.Configuration;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

internal class TournamentService(IErrorHandler errorHandler, IConfiguration configuration, IUserService userService, IChannelService channelService, IMessageService messageService, IDiscordMessageUtils discordMessageUtils) : ITournamentService
{
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IDiscordMessageUtils _discordMessageUtils = discordMessageUtils ?? throw new ArgumentNullException(nameof(discordMessageUtils));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

	public async Task GenerateLogMessage(DiscordMessage message, DiscordChannel toernooiAanmeldenChannel, ulong userID, string emojiAsEmoji)
	{
		bool addInLog = true;
		if (message.Author != null)
		{
			if (!message.Author.Id.Equals(Constants.NLBE_BOT) && !message.Author.Id.Equals(Constants.TESTBEASTV2_BOT))
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
					IReadOnlyList<DiscordUser> userListOfThisEmoji = await message.GetReactionsAsync(_discordMessageUtils.GetDiscordEmoji(emojiAsEmoji).Inner);
					foreach (DiscordUser user in userListOfThisEmoji)
					{
						if (user.Id.Equals(Constants.NLBE_BOT) || user.Id.Equals(Constants.TESTBEASTV2_BOT))
						{
							botReactedWithThisEmoji = true;
						}
					}
					if (botReactedWithThisEmoji)
					{
						DiscordMember member = await toernooiAanmeldenChannel.Guild.GetMemberAsync(userID);
						if (member != null)
						{
							string organisator = await GetOrganisator(await toernooiAanmeldenChannel.GetMessageAsync(message.Id));
							string logMessage = "Teams|" + member.DisplayName.adaptToDiscordChat() + "|" + emojiAsEmoji + "|" + organisator + "|" + userID;
							await WriteInLog(toernooiAanmeldenChannel.Guild.Id, message.Timestamp.LocalDateTime.ConvertToDate(), logMessage);
						}
					}
					else
					{
						await message.DeleteReactionsEmojiAsync(_discordMessageUtils.GetDiscordEmoji(emojiAsEmoji).Inner);
					}
				}
				catch (Exception ex)
				{
					await _errorHandler.HandleErrorAsync("While adding to log: ", ex);
				}
			}
		}
	}

	public async Task<List<WGTournament>> InitialiseTournaments(bool all)
	{
		string tournamentJson = await Tournaments.tournamentsToString(_configuration["NLBEBOT:WarGamingAppId"]);
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
								string wgTournamentJsonString = await WGTournament.tournamentsToString(_configuration["NLBEBOT:WarGamingAppId"], tournaments.tournament_id);
								Json wgTournamentJson = new(wgTournamentJsonString, "WGTournament");
								WGTournament eenToernooi = new(wgTournamentJson, _configuration["NLBEBOT:WarGamingAppId"]);
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

	public async Task ShowTournamentInfo(DiscordChannel channel, WGTournament tournament, string titel)
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
					string tempRules = tournament.rules.adaptDiscordLink().adaptToDiscordChat().adaptMutlipleLines();
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
											Name = splitted[i].adaptToDiscordChat(),
											Value = sbTemp.ToString().adaptDiscordLink().adaptToDiscordChat(),
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
											Name = splitted[i].adaptToDiscordChat(),
											Value = sbTemp.ToString().adaptDiscordLink().adaptToDiscordChat(),
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
						tempRules = sbRules.ToString().adaptDiscordLink().adaptToDiscordChat().adaptMutlipleLines();

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
		string tempDescription = tournament.description.adaptDiscordLink().adaptToDiscordChat().adaptMutlipleLines();
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
								Name = splitted[i].adaptToDiscordChat(),
								Value = sbTemp.ToString().adaptDiscordLink().adaptToDiscordChat(),
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
								Name = splitted[i].adaptToDiscordChat(),
								Value = sbTemp.ToString().adaptDiscordLink().adaptToDiscordChat(),
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
			tempDescription = sbDescription.ToString().adaptDiscordLink().adaptToDiscordChat().adaptMutlipleLines();

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

	private async Task WriteInLog(ulong guildID, string date, string message)
	{
		DiscordChannel logChannel = await _channelService.GetLogChannel(guildID);
		if (logChannel != null)
		{
			await logChannel.SendMessageAsync(date + "|" + message);
		}
		else
		{
			await _errorHandler.HandleErrorAsync("Could not find log channel, message: " + date + "|" + message);
		}
	}

	private async Task<string> GetOrganisator(DiscordMessage message)
	{
		IReadOnlyList<DiscordEmbed> embeds = message.Embeds;
		if (embeds.Count > 0)
		{
			foreach (DiscordEmbed anEmbed in embeds)
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
				DiscordMember member = await _userService.GetDiscordMember(message.Channel.Guild, message.Author.Id);
				if (member != null)
				{
					return member.DisplayName;
				}
			}
		}
		return string.Empty;
	}
}
