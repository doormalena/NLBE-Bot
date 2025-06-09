namespace NLBE_Bot;

using DiscordHelper;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Net.Models;
using FMWOTB;
using FMWOTB.Account;
using FMWOTB.Account.Statistics;
using FMWOTB.Clans;
using FMWOTB.Exceptions;
using FMWOTB.Tools;
using FMWOTB.Tools.Replays;
using FMWOTB.Tournament;
using FMWOTB.Vehicles;
using JsonObjectConverter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Helpers;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

internal class Bot
{
	public static DiscordClient discordClient;
	public static IReadOnlyDictionary<ulong, DiscordGuild> discGuildslist;
	public static bool ignoreCommands = false;
	public static bool ignoreEvents = false;
	public static Tuple<ulong, DateTime> weeklyEventWinner = new(0, DateTime.Now);
	private static DiscordMessage discordMessage;//temp message
	private DateTime? lasTimeNamesWereUpdated;
	private short heartBeatCounter = 0;
	private readonly IConfiguration _configuration;

	public static string WarGamingAppId
	{
		get; private set;
	}
	private static ILogger _logger;

	public Bot(ILogger logger, IConfiguration configuration)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Note: temporary workarround to access the logger due to excessive usage of static methods.
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

		discordClient = new DiscordClient(new DiscordConfiguration
		{
			Token = _configuration["NLBEBOT:DiscordToken"],
			TokenType = TokenType.Bot,
			AutoReconnect = true,
			Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
		});

		discordClient.UseInteractivity(new InteractivityConfiguration
		{
			Timeout = TimeSpan.FromSeconds(int.TryParse(_configuration["NLBEBOT:DiscordTimeOutInSeconds"], out int timeout) ? timeout : 0)
		});

		WarGamingAppId = _configuration["NLBEBOT:WarGamingAppId"];
	}

	public async Task RunAsync()
	{
		CommandsNextConfiguration commandsConfig = new()
		{
			StringPrefixes = [Constants.Prefix],
			EnableDms = false,
			EnableMentionPrefix = true,
			DmHelp = false,
			EnableDefaultHelp = false
		};

		CommandsNextExtension commands = discordClient.UseCommandsNext(commandsConfig);
		commands.RegisterCommands<BotCommands>();
		commands.CommandErrored += Commands_CommandErrored;
		commands.CommandExecuted += Commands_CommandExecuted;

		DiscordActivity act = new(Constants.Prefix, ActivityType.ListeningTo);
		await discordClient.ConnectAsync(act, UserStatus.Online);

		//Events
		discordClient.Heartbeated += Discord_Heartbeated;
		discordClient.Ready += Discord_Ready;
		discordClient.GuildMemberAdded += Discord_GuildMemberAdded;
		discordClient.MessageReactionAdded += Discord_MessageReactionAdded;
		discordClient.GuildMemberRemoved += Discord_GuildMemberRemoved;
		discordClient.MessageReactionRemoved += Discord_MessageReactionRemoved;
		discordClient.MessageDeleted += Discord_MessageDeleted;
		discordClient.GuildMemberUpdated += Discord_GuildMemberUpdated;
		discordClient.MessageCreated += Discord_MessageCreated;

		await Task.Delay(-1);
	}

	private Task Commands_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs args)
	{
		discordClient.Logger.Log(LogLevel.Information, "Command executed: {CommandName}", args.Command.Name);
		return Task.CompletedTask;
	}

	public static async Task<DiscordMessage> SendMessage(DiscordChannel channel, DiscordMember member, string guildName, string Message)
	{
		try
		{
			return await channel.SendMessageAsync(Message);
		}
		catch (Exception ex)
		{
			await HandleError("[" + guildName + "] (" + channel.Name + ") Could not send message: ", ex.Message, ex.StackTrace);

			if (ex.Message.Contains("unauthorized", StringComparison.CurrentCultureIgnoreCase))
			{
				await SayBotNotAuthorized(channel);
			}
			else
			{
				await SayTooManyCharacters(channel);
			}

			if (member != null)
			{
				await SendPrivateMessage(member, guildName, Message);
			}
		}

		return null;
	}

	public static async Task<bool> SendPrivateMessage(DiscordMember member, string guildName, string Message)
	{
		try
		{
			await member.SendMessageAsync(Message);
			return true;
		}
		catch (Exception ex)
		{
			await HandleError("[" + guildName + "] Could not send private message: ", ex.Message, ex.StackTrace);
		}
		return false;
	}

	public static async Task<DiscordMessage> CreateEmbed(DiscordChannel channel, EmbedOptions options)
	{
		DiscordEmbedBuilder newDiscEmbedBuilder = new()
		{
			Color = options.Color,
			Title = options.Title,
			Description = options.Description
		};

		if (!string.IsNullOrEmpty(options.Thumbnail))
		{
			try
			{
				newDiscEmbedBuilder.WithThumbnail(options.Thumbnail);
			}
			catch (Exception ex)
			{
				await HandleError("Could not set imageurl for embed: ", ex.Message, ex.StackTrace);
			}
		}
		if (options.Author != null)
		{
			newDiscEmbedBuilder.Author = options.Author;
		}

		if (!string.IsNullOrEmpty(options.ImageUrl))
		{
			try
			{
				newDiscEmbedBuilder.ImageUrl = options.ImageUrl;
			}
			catch (Exception ex)
			{
				_logger.LogDebug(ex, ex.Message);

				try
				{
					newDiscEmbedBuilder.WithImageUrl(new Uri(options.ImageUrl.Replace("\\", string.Empty)));
				}
				catch (Exception innerEx)
				{
					await HandleError("Could not set imageurl for embed: ", innerEx.Message, innerEx.StackTrace);
				}
			}
		}

		if (options.Fields != null && options.Fields.Count > 0)
		{
			foreach (DEF field in options.Fields)
			{
				if (field.Value.Length > 0)
				{
					try
					{
						newDiscEmbedBuilder.AddField(field.Name, field.Value, field.Inline);
					}
					catch (Exception ex)
					{
						await HandleError("Something went wrong while trying to add a field to an embedded message:", ex.Message, ex.StackTrace);
					}
				}
			}
		}
		if (options.Footer != null && options.Footer.Length > 0)
		{
			DiscordEmbedBuilder.EmbedFooter embedFooter = new()
			{
				Text = options.Footer
			};
			newDiscEmbedBuilder.Footer = embedFooter;
		}
		DiscordEmbed embed = newDiscEmbedBuilder.Build();
		try
		{
			DiscordMessage theMessage = options.IsForReplay
				? discordMessage.RespondAsync(options.Content, embed).Result
				: discordClient.SendMessageAsync(channel, options.Content, embed).Result;

			try
			{
				if (options.Emojis != null)
				{
					foreach (DiscordEmoji anEmoji in options.Emojis)
					{
						await theMessage.CreateReactionAsync(anEmoji);
					}
				}
			}
			catch (Exception ex)
			{
				await HandleError("Error while adding emoji's:", ex.Message, ex.StackTrace);
			}
			if (!string.IsNullOrEmpty(options.NextMessage))
			{
				await channel.SendMessageAsync(options.NextMessage);
			}
			return theMessage;
		}
		catch (Exception ex)
		{
			await HandleError("Error in createEmbed:", ex.Message, ex.StackTrace);
		}
		return null;
	}

	#region Events

	private async Task Discord_Heartbeated(DiscordClient sender, HeartbeatEventArgs e)
	{
		if (ignoreEvents)
		{
			return;
		}

		heartBeatCounter++;
		const int hourToCheck = 14;
		const DayOfWeek dayToCheck = DayOfWeek.Monday;

		if ((DateTime.Now.DayOfWeek != dayToCheck || DateTime.Now.Hour != hourToCheck) && heartBeatCounter > 2)
		{
			//update usernames
			heartBeatCounter = 0;
			bool update = false;
			if (lasTimeNamesWereUpdated.HasValue)
			{
				if (lasTimeNamesWereUpdated.Value.DayOfYear != DateTime.Now.DayOfYear)
				{
					update = true;
					lasTimeNamesWereUpdated = DateTime.Now;
				}
			}
			else
			{
				update = true;
				lasTimeNamesWereUpdated = DateTime.Now;
			}
			if (update)
			{
				try
				{
					await UpdateUsers();
				}
				catch (InternalServerErrorException ex)
				{
					string message = "\nERROR updating users:\nInternal server exception from api request\n" + ex.Message;
					await SendThibeastmo(message, string.Empty, string.Empty);
					DiscordChannel bottestChannel = await GetBottestChannel();
					await bottestChannel.SendMessageAsync(message);
				}
				catch (Exception ex)
				{
					string message = "\nERROR updating users:\n" + ex.Message;
					await SendThibeastmo(message, string.Empty, string.Empty);
					DiscordChannel bottestChannel = await GetBottestChannel();
					await bottestChannel.SendMessageAsync(message);
				}
			}
		}
		else if (DateTime.Now.DayOfWeek == dayToCheck && DateTime.Now.Hour == hourToCheck && heartBeatCounter == 2)//14u omdat wotb ook wekelijks op maandag 14u restart
		{
			//We have a weekly winner
			string winnerMessage = "We hebben een wekelijkse winnaar.";
			DiscordChannel bottestChannel = await GetBottestChannel();
			try
			{
				discordClient.Logger.LogInformation(winnerMessage);
				WeeklyEventHandler weeklyEventHandler = new();
				await weeklyEventHandler.ReadWeeklyEvent();
				if (weeklyEventHandler.WeeklyEvent.StartDate.DayOfYear == DateTime.Now.DayOfYear - 7)//-7 omdat het dan zeker een nieuwe week is maar niet van twee weken gelden
				{
					winnerMessage += "\nNa 1 week...";
					WeeklyEventItem weeklyEventItemMostDMG = weeklyEventHandler.WeeklyEvent.WeeklyEventItems.Find(weeklyEventItem => weeklyEventItem.WeeklyEventType == WeeklyEventType.Most_damage);
					if (weeklyEventItemMostDMG.Player != null && weeklyEventItemMostDMG.Player.Length > 0)
					{
						foreach (KeyValuePair<ulong, DiscordGuild> guild in discGuildslist)
						{
							if (guild.Key is Constants.NLBE_SERVER_ID or Constants.DA_BOIS_ID)
							{
								await WeHaveAWinner(guild.Value, weeklyEventItemMostDMG, weeklyEventHandler.WeeklyEvent.Tank);
								break;
							}
						}
					}
				}
				await bottestChannel.SendMessageAsync(winnerMessage);
				await SendThibeastmo(winnerMessage, string.Empty, string.Empty);
			}
			catch (Exception ex)
			{
				string message = winnerMessage + "\nERROR:\n" + ex.Message;
				await bottestChannel.SendMessageAsync(message);
				await SendThibeastmo(message, string.Empty, string.Empty);
			}
		}
	}
	public static async Task WeHaveAWinner(DiscordGuild guild, WeeklyEventItem weeklyEventItemMostDMG, string tank)
	{
		bool userNotFound = true;
		IReadOnlyCollection<DiscordMember> members = await guild.GetAllMembersAsync();
		if (weeklyEventItemMostDMG.Player != null)
		{
			string weeklyEventItemMostDMGPlayer = weeklyEventItemMostDMG.Player
				.Replace("\\", string.Empty)
				.Replace(Constants.UNDERSCORE_REPLACEMENT_CHAR, '_')
				.ToLower();
			foreach (DiscordMember member in members)
			{
				if (!member.IsBot)
				{
					Tuple<string, string> gebruiker = GetIGNFromMember(member.DisplayName);
					string x = gebruiker.Item2
						.Replace("\\", string.Empty)
						.Replace(Constants.UNDERSCORE_REPLACEMENT_CHAR, '_')
						.ToLower();
					if (x == weeklyEventItemMostDMGPlayer
						|| (member.Id == Constants.THIBEASTMO_ID
							&& guild.Id == Constants.DA_BOIS_ID))
					{
						userNotFound = false;
						weeklyEventWinner = new Tuple<ulong, DateTime>(member.Id, DateTime.Now);
						try
						{
							await member.SendMessageAsync("Hallo " + member.Mention + ",\n\nProficiat! Je hebt het wekelijkse event gewonnen van de **" + tank + "** met **" + weeklyEventItemMostDMG.Value + "** damage.\n" +
														  "Dit wilt zeggen dat jij de tank voor het wekelijkse event mag kiezen.\n" +
														  "Je kan je keuze maken door enkel de naam van de tank naar mij te sturen. Indien ik de tank niet kan vinden dan zal ik je voorthelpen.\n" +
														  "De enige voorwaarde is wel dat je niet een recent gekozen tank opnieuw kiest."
														  + "\n\nSucces met je keuze!");
						}
						catch (Exception ex)
						{
							await HandleError("Could not send private message towards winner of weekly event.", ex.Message, ex.StackTrace);
						}
						try
						{
							DiscordChannel algemeenChannel = await GetAlgemeenChannel();
							if (algemeenChannel != null)
							{
								await algemeenChannel.SendMessageAsync("Feliciteer **" + weeklyEventItemMostDMG.Player.Replace(Constants.UNDERSCORE_REPLACEMENT_CHAR, '_').adaptToDiscordChat() + "** want hij heeft het wekelijkse event gewonnen! **Proficiat!**" +
																	   "\n" +
																	   "`" + tank + "` met `" + weeklyEventItemMostDMG.Value + "` damage" +
																	   "\n\n" +
																	   "We wachten nu af tot de winnaar een nieuwe tank kiest.");
							}
						}
						catch (Exception ex)
						{
							await HandleError("Could not send message in algemeen channel for weekly event winner announcement.", ex.Message, ex.StackTrace);
						}
						break;
					}
				}
			}
		}
		else
		{
			DiscordChannel algemeenChannel = await GetAlgemeenChannel();
			if (algemeenChannel != null)
			{
				await algemeenChannel.SendMessageAsync("Het wekelijkse event is gedaan, helaas heeft er __niemand__ deelgenomen en is er dus geen winnaar.");
			}
		}
		DiscordChannel bottestChannel = await GetBottestChannel();
		if (userNotFound)
		{
			string message = "Weekly event winnaar was niet gevonden! Je zal het zelf moeten regelen met het `weekly` commando.";
			if (bottestChannel != null)
			{
				await bottestChannel.SendMessageAsync(message);
			}
			else
			{
				await HandleError(message, string.Empty, string.Empty);
			}
		}
		else
		{
			string message = "Weekly event winnaar gevonden!";
			if (bottestChannel != null)
			{
				await bottestChannel.SendMessageAsync(message);
			}
			else
			{
				await HandleError(message, string.Empty, string.Empty);
			}
		}
	}

	private Task Commands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
	{
		if (e.Context.Guild.Id.Equals(Constants.NLBE_SERVER_ID) || e.Context.Guild.Id.Equals(Constants.DA_BOIS_ID))
		{
			if (e.Exception.Message.ToLower().Contains("unauthorized"))
			{
				e.Context.Channel.SendMessageAsync("**De bot heeft hier geen rechten voor!**");
			}
			else if (e.Command != null)
			{
				e.Context.Message.DeleteReactionsEmojiAsync(GetDiscordEmoji(Constants.IN_PROGRESS_REACTION));
				e.Context.Message.CreateReactionAsync(GetDiscordEmoji(Constants.ERROR_REACTION));
				HandleError("Error with command (" + e.Command.Name + "):\n", e.Exception.Message.Replace("`", "'"), e.Exception.StackTrace).Wait();
			}
		}
		return Task.CompletedTask;
	}

	private Task Discord_Ready(DiscordClient sender, ReadyEventArgs e)
	{
		discGuildslist = sender.Guilds;
		foreach (KeyValuePair<ulong, DiscordGuild> guild in discGuildslist)
		{
			if (!guild.Key.Equals(Constants.NLBE_SERVER_ID) && !guild.Key.Equals(Constants.DA_BOIS_ID))
			{
				guild.Value.LeaveAsync();
			}
		}
		discordClient.Logger.Log(LogLevel.Information, "Client (v{Version}) is ready to process events.", Constants.version);

		return Task.CompletedTask;
	}

	private async Task Discord_MessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
	{
		if (ignoreEvents)
		{
			return;
		}

		if (!e.User.IsBot && e.Guild.Id is Constants.NLBE_SERVER_ID or Constants.DA_BOIS_ID)
		{
			DiscordChannel toernooiAanmeldenChannel = await GetToernooiAanmeldenChannel(e.Guild.Id);
			if (toernooiAanmeldenChannel != null && e.Channel.Equals(toernooiAanmeldenChannel))
			{
				DiscordMessage message = await toernooiAanmeldenChannel.GetMessageAsync(e.Message.Id);
				await GenerateLogMessage(message, toernooiAanmeldenChannel, e.User.Id, GetDiscordEmoji(e.Emoji.Name));
			}
			else
			{
				DiscordChannel regelsChannel = await GetRegelsChannel();
				if (regelsChannel != null && e.Channel.Equals(regelsChannel))
				{
					string rulesReadEmoji = ":ok:";
					if (e.Emoji.GetDiscordName().Equals(rulesReadEmoji))
					{
						IReadOnlyList<DiscordUser> users = await e.Message.GetReactionsAsync(e.Emoji);

						foreach (DiscordUser aUser in users)
						{
							if (!aUser.IsBot)
							{
								await e.Message.DeleteReactionAsync(e.Emoji, aUser);
								DiscordMember member = await e.Guild.GetMemberAsync(aUser.Id);
								if (member != null)
								{
									bool hadRulesNotReadrole = false;
									if (member.Roles != null)
									{
										foreach (DiscordRole memberRole in member.Roles)
										{
											if (memberRole.Id.Equals(Constants.MOET_REGELS_NOG_LEZEN_ROLE))
											{
												hadRulesNotReadrole = true;
												await member.RevokeRoleAsync(memberRole);//een oorzaak
												break;
											}
										}
									}
									if (hadRulesNotReadrole || member.Roles == null || member.Roles.Count() == 0)
									{
										if (member.DisplayName.Split(']').Length > 1)
										{
											if (member.DisplayName.Split(']').Length > 2)
											{
												string[] splitted = member.DisplayName.Split(']');
												string tempName = splitted[splitted.Length - 1].Trim(' ');
												await ChangeMemberNickname(member, "[] " + tempName);//een oorzaak

											}
											else if (member.DisplayName.Contains("[NLBE]"))
											{
												await ChangeMemberNickname(member, "[] " + member.DisplayName.Replace("[NLBE]", string.Empty).Trim(' '));//een oorzaak
											}
											else if (member.DisplayName.Contains("[NLBE2]"))
											{
												await ChangeMemberNickname(member, "[] " + member.DisplayName.Replace("[NLBE2]", string.Empty).Trim(' '));//een oorzaak
											}
										}
										else
										{
											await ChangeMemberNickname(member, "[] " + member.Username);//een oorzaak
										}
										DiscordRole ledenRole = e.Guild.GetRole(Constants.LEDEN_ROLE);
										if (ledenRole != null)
										{
											await member.GrantRoleAsync(ledenRole);//een oorzaak
										}
										DiscordChannel algemeenChannel = await GetAlgemeenChannel();//een oorzaak
										if (algemeenChannel != null)
										{
											await algemeenChannel.SendMessageAsync(e.User.Mention + " , welkom op de NLBE discord server. GLHF!");
										}
									}
								}
							}
						}
					}
					else
					{
						IReadOnlyList<DiscordUser> users = await e.Message.GetReactionsAsync(e.Emoji);
						foreach (DiscordUser user in users)
						{
							if (!user.IsBot)
							{
								await e.Message.DeleteReactionAsync(e.Emoji, user);
							}
						}
					}
				}
			}
		}
	}
	private async Task Discord_MessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs e)
	{
		if (ignoreEvents)
		{
			return;
		}

		if (e.Guild.Id is Constants.NLBE_SERVER_ID or Constants.DA_BOIS_ID)
		{
			DiscordChannel toernooiAanmeldenChannel = await GetToernooiAanmeldenChannel(e.Guild.Id);
			if (toernooiAanmeldenChannel != null)
			{
				if (e.Channel.Equals(toernooiAanmeldenChannel))
				{
					bool removeInLog = true;
					DiscordMessage message = await toernooiAanmeldenChannel.GetMessageAsync(e.Message.Id);
					if (message.Author != null && !message.Author.Id.Equals(Constants.NLBE_BOT) && !message.Author.Id.Equals(Constants.TESTBEASTV2_BOT))
					{
						removeInLog = false;
					}
					if (removeInLog)
					{
						//check if reaction has to be added by bot (from 0 reactions to 1 reaction)
						IReadOnlyList<DiscordUser> users = await e.Message.GetReactionsAsync(e.Emoji);
						if (users == null || users.Count == 0)
						{
							await e.Message.CreateReactionAsync(e.Emoji);
						}

						//remove in log
						DiscordChannel logchannel = await GetLogChannel(e.Guild.Id);
						if (logchannel != null)
						{
							Dictionary<DateTime, List<DiscordMessage>> sortedMessages = (await logchannel.GetMessagesAsync(100)).SortMessages();
							foreach (KeyValuePair<DateTime, List<DiscordMessage>> messageList in sortedMessages)
							{
								try
								{
									if (e.Message.CreationTimestamp.LocalDateTime.CompareDateTime(messageList.Key))
									{
										foreach (DiscordMessage aMessage in messageList.Value)
										{
											DiscordMember member = await GetDiscordMember(e.Guild, e.User.Id);
											if (member != null)
											{
												string[] splitted = aMessage.Content.Split(Constants.LOG_SPLIT_CHAR);
												string theEmoji = GetEmojiAsString(e.Emoji.Name);
												if (splitted[2].Replace("\\", string.Empty).ToLower().Equals(member.DisplayName.ToLower()) && GetEmojiAsString(splitted[3]).Equals(theEmoji))
												{
													await aMessage.DeleteAsync("Log updated: reaction was removed from message in Toernooi-aanmelden for this user.");
												}
											}
										}
									}
								}
								catch (Exception ex)
								{
									await HandleError("Could not compare TimeStamps in MessageReactionRemoved:", ex.Message, ex.StackTrace);
								}
							}
						}
						else
						{
							await HandleError("Could not find log channel at MessageReactionRemoved!", string.Empty, string.Empty);
						}
					}
				}
			}
		}
	}

	private async Task Discord_MessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
	{
		if (ignoreEvents)
		{
			return;
		}

		DiscordChannel toernooiAanmeldenChannel = await GetToernooiAanmeldenChannel(e.Guild.Id);
		if (e.Channel.Equals(toernooiAanmeldenChannel))
		{
			DateTime timeStamp = e.Message.Timestamp.LocalDateTime;
			DiscordChannel logChannel = await GetLogChannel(e.Guild.Id);
			if (logChannel != null)
			{
				IReadOnlyList<DiscordMessage> messages = await logChannel.GetMessagesAsync(100);
				foreach (DiscordMessage message in messages)
				{
					string[] splitted = message.Content.Split('|');

					if (DateTime.TryParse(splitted[0], new CultureInfo("nl-NL"), out DateTime tempDateTime) && tempDateTime.CompareDateTime(timeStamp))
					{
						await message.DeleteAsync();
						await Task.Delay(875);
					}
				}
			}
		}
	}

	private async Task Discord_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
	{
		if (ignoreEvents)
		{
			return;
		}

		if (e.Guild.Id == Constants.NLBE_SERVER_ID)
		{
			DiscordRole noobRole = e.Guild.GetRole(Constants.NOOB_ROLE);
			if (noobRole != null)
			{
				e.Member.GrantRoleAsync(noobRole).Wait();
				DiscordChannel welkomChannel = GetWelkomChannel().Result;
				if (welkomChannel != null)
				{
					DiscordChannel regelsChannel = GetRegelsChannel().Result;
					welkomChannel.SendMessageAsync(e.Member.Mention + " welkom op de NLBE discord server. Beantwoord eerst de vraag en lees daarna de " + (regelsChannel != null ? regelsChannel.Mention : "#regels") + " aub.").Wait();
					DiscordGuild guild = GetGuild(e.Guild.Id).Result;
					if (guild != null)
					{
						DiscordUser user = discordClient.GetUserAsync(e.Member.Id).Result;
						if (user != null)
						{
							IReadOnlyList<WGAccount> searchResults = [];
							bool resultFound = false;
							StringBuilder sbDescription = new();
							int counter = 0;
							bool firstTime = true;
							while (!resultFound)
							{
								string question = user.Mention + " Wat is je gebruikersnaam van je wargaming account?";
								if (firstTime)
								{
									firstTime = false;
								}
								else
								{
									question = "**We konden dit Wargamingaccount niet vinden, probeer opnieuw! (Hoofdlettergevoelig)**\n" + question;
								}
								string ign = AskQuestion(await GetWelkomChannel(), user, guild, question).Result;
								searchResults = await WGAccount.searchByName(SearchAccuracy.EXACT, ign, WarGamingAppId, false, true, false);
								if (searchResults != null && searchResults.Count > 0)
								{
									resultFound = true;
									foreach (WGAccount tempAccount in searchResults)
									{
										string tempClanName = string.Empty;
										if (tempAccount.clan != null)
										{
											tempClanName = tempAccount.clan.tag;
										}
										try
										{
											sbDescription.AppendLine(++counter + ". " + tempAccount.nickname + " " + (tempClanName.Length > 0 ? '`' + tempClanName + '`' : string.Empty));
										}
										catch (Exception ex)
										{
											discordClient.Logger.LogWarning(ex, "Error while looking for basicInfo for {Ign}:\n {StackTrace}", ign, ex.StackTrace);
										}
									}
								}
							}

							int selectedAccount = 0;
							if (searchResults.Count > 1)
							{
								selectedAccount = -1;
								while (selectedAccount == -1)
								{
									selectedAccount = await WaitForReply(welkomChannel, user, sbDescription.ToString(), counter);
								}
							}

							WGAccount account = searchResults[selectedAccount];

							string clanName = string.Empty;
							if (account.clan != null && account.clan.tag != null)
							{
								if (account.clan.clan_id.Equals(Constants.NLBE_CLAN_ID) || account.clan.clan_id.Equals(Constants.NLBE2_CLAN_ID))
								{
									await e.Member.SendMessageAsync("Indien je echt van **" + account.clan.tag + "** bent dan moet je even vragen of iemand jouw de **" + account.clan.tag + "** rol wilt geven.");
								}
								else
								{
									clanName = account.clan.tag;
								}
							}
							ChangeMemberNickname(e.Member, "[" + clanName + "] " + account.nickname).Wait();
							await e.Member.SendMessageAsync("We zijn er bijna. Als je nog even de regels wilt lezen in **#regels** dan zijn we klaar.");
							DiscordRole rulesNotReadRole = e.Guild.GetRole(Constants.MOET_REGELS_NOG_LEZEN_ROLE);
							if (rulesNotReadRole != null)
							{
								e.Member.RevokeRoleAsync(noobRole).Wait();
								e.Member.GrantRoleAsync(rulesNotReadRole).Wait();
							}
							IReadOnlyCollection<DiscordMember> allMembers = await e.Guild.GetAllMembersAsync();
							bool atLeastOneOtherPlayerWithNoobRole = false;
							foreach (DiscordMember aMember in allMembers)
							{
								if (aMember.Roles.Contains(noobRole))
								{
									atLeastOneOtherPlayerWithNoobRole = true;
									break;
								}
							}
							if (atLeastOneOtherPlayerWithNoobRole)
							{
								await CleanWelkomChannel(e.Member.Id);
							}
							else
							{
								await CleanWelkomChannel();
							}
						}
					}
				}
			}
			else
			{
				await HandleError("Could not grant new member[" + e.Member.DisplayName + " (" + e.Member.Username + "#" + e.Member.Discriminator + ")] the Noob role.", string.Empty, string.Empty);
			}
		}
	}

	private async Task Discord_GuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
	{
		if (ignoreEvents)
		{
			return;
		}

		if (!e.Member.Id.Equals(Constants.THIBEASTMO_ALT_ID))
		{
			if (e.Guild.Id.Equals(Constants.NLBE_SERVER_ID))
			{
				DiscordChannel oudLedenChannel = await GetOudLedenChannel();
				if (oudLedenChannel != null)
				{
					IReadOnlyDictionary<ulong, DiscordRole> serverRoles = null;
					foreach (KeyValuePair<ulong, DiscordGuild> guild in discGuildslist)
					{
						if (guild.Value.Id.Equals(Constants.NLBE_SERVER_ID))
						{
							serverRoles = guild.Value.Roles;
						}
					}
					if (serverRoles != null && serverRoles.Count > 0)
					{
						IEnumerable<DiscordRole> memberRoles = e.Member.Roles;
						StringBuilder sbRoles = new();
						bool firstRole = true;
						foreach (DiscordRole role in memberRoles)
						{
							foreach (KeyValuePair<ulong, DiscordRole> serverRole in serverRoles)
							{
								if (serverRole.Key.Equals(role.Id))
								{
									if (role.Id.Equals(Constants.NOOB_ROLE))
									{
										await CleanWelkomChannel();
									}
									if (firstRole)
									{
										firstRole = false;
									}
									else
									{
										sbRoles.Append(", ");
									}
									sbRoles.Append(role.Name);
								}
							}
						}
						List<DEF> defList = [];
						DEF newDef1 = new()
						{
							Inline = true,
							Name = "Bijnaam:",
							Value = e.Member.DisplayName
						};
						defList.Add(newDef1);
						DEF newDef2 = new()
						{
							Inline = true,
							Name = "Gebruiker:",
							Value = e.Member.Username + "#" + e.Member.Discriminator
						};
						defList.Add(newDef2);
						DEF newDef3 = new()
						{
							Inline = true,
							Name = "GebruikersID:",
							Value = e.Member.Id.ToString()
						};
						defList.Add(newDef3);
						if (sbRoles.Length > 0)
						{
							DEF newDef = new()
							{
								Inline = true,
								Name = "Rollen:",
								Value = sbRoles.ToString()
							};
							defList.Add(newDef);
						}

						EmbedOptions options = new()
						{
							Title = e.Member.Username + " heeft de server verlaten",
							Fields = defList,
						};
						await CreateEmbed(oudLedenChannel, options);
					}
				}
			}
		}
	}

	private async Task Discord_GuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs e)
	{
		if (!ignoreEvents)
		{
			return;
		}

		foreach (KeyValuePair<ulong, DiscordGuild> guild in discGuildslist.Where(g => g.Key != Constants.NLBE_SERVER_ID))
		{
			DiscordMember member = await GetDiscordMember(guild.Value, e.Member.Id);

			if (member == null)
			{
				continue;
			}

			IEnumerable<DiscordRole> userRoles = member.Roles;
			bool isNoob = userRoles.Any(role => role.Id.Equals(Constants.NOOB_ROLE));
			bool hasRoles = userRoles.Any();

			if (!isNoob && hasRoles && (e.RolesAfter != null || !string.IsNullOrEmpty(e.NicknameAfter)))
			{
				string editedName = UpdateName(member, member.DisplayName);
				if (!editedName.Equals(member.DisplayName, StringComparison.Ordinal) && !string.IsNullOrEmpty(editedName))
				{
					await ChangeMemberNickname(member, editedName);
				}
			}
		}
	}

	private async Task Discord_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
	{
		if (ignoreEvents)
		{
			return;
		}

		if (false && e.Message.Content.StartsWith(Constants.Prefix))
		// VOOR IN HET GEVAL DAT OOIT JE COMMANDS MOET MAKEN OP BASIS VAN DE MESSAGE CREATED EVENT
		{
			// Remove the prefix and split the message into command and arguments
			string content = e.Message.Content[Constants.Prefix.Length..];
			string[] parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length == 0)
			{
				return;
			}

			// Extract the command name and arguments
			string commandName = parts[0];
			string args = content.Substring(Constants.Prefix.Length, content.Length - Constants.Prefix.Length);

			CommandsNextExtension commandsNext = sender.GetCommandsNext();
			if (commandsNext == null)
			{
				discordClient.Logger.Log(LogLevel.Information, "CommandsNext is not enabled.");
				return;
			}

			Command command = commandsNext.FindCommand(commandName, out string rawArguments);
			if (command == null)
			{
				discordClient.Logger.Log(LogLevel.Information, "Unknown command.");
				return;
			}

			CommandContext ctx = commandsNext.CreateContext(e.Message, Constants.Prefix, command, args);

			try
			{
				// Execute the command (this will internally create and manage a CommandContext)
				await commandsNext.ExecuteCommandAsync(ctx);
			}
			catch (Exception ex)
			{
				// Log or handle any errors that occur during command execution
				Console.WriteLine();
				discordClient.Logger.Log(LogLevel.Error, ex, "Error executing command: {0}", ex.Message);
			}

			await commandsNext.ExecuteCommandAsync(ctx);

			// Handle the command
			return;
		}
		if (!e.Author.IsBot && e.Channel.Guild != null)
		{
			bool validChannel = false;
			DiscordChannel masteryChannel = await GetMasteryReplaysChannel(e.Guild.Id);
			if (masteryChannel != null)
			{
				if (masteryChannel.Equals(e.Channel) || e.Channel.Id.Equals(Constants.BOTTEST_ID))
				{
					validChannel = true;
				}
			}
			if (!validChannel)
			{
				masteryChannel = await GetBottestChannel();
				if (masteryChannel != null && masteryChannel.Equals(e.Channel))
				{
					validChannel = true;
				}
				if (!validChannel)
				{
					masteryChannel = await GetReplayResultsChannel();
					if (masteryChannel != null)
					{
						if (masteryChannel.Equals(e.Channel))
						{
							validChannel = true;
						}
					}
					if (!validChannel)
					{
						masteryChannel = await GetReplayResultsChannel();
						if (masteryChannel != null)
						{
							if (masteryChannel.Equals(e.Channel))
							{
								validChannel = true;
							}
						}
						if (!validChannel)
						{
							masteryChannel = await GetTestChannel();
							if (masteryChannel != null)
							{
								if (masteryChannel.Equals(e.Channel))
								{
									validChannel = true;
								}
							}
						}
					}
				}
			}
			if (validChannel)
			{
				discordMessage = e.Message;
				DiscordMember member = await e.Guild.GetMemberAsync(e.Author.Id);
				if (e.Channel.Id.Equals(Constants.PRIVE_ID) || (member.Roles.Contains(e.Guild.GetRole(Constants.NLBE_ROLE)) && (e.Channel.Id.Equals(Constants.MASTERY_REPLAYS_ID) || e.Channel.Id.Equals(Constants.BOTTEST_ID))) || (member.Roles.Contains(e.Guild.GetRole(Constants.NLBE2_ROLE)) && (e.Channel.Id.Equals(Constants.MASTERY_REPLAYS_ID) || e.Channel.Id.Equals(Constants.BOTTEST_ID))))
				{
					//MasteryChannel (komt wel in HOF)
					if (e.Message.Attachments.Count > 0)
					{
						foreach (DiscordAttachment attachment in e.Message.Attachments)
						{
							if (attachment.FileName.EndsWith(".wotbreplay"))
							{
								Tuple<string, DiscordMessage> returnedTuple = await Handle(string.Empty, e.Channel, await e.Guild.GetMemberAsync(e.Author.Id), e.Guild.Name, e.Guild.Id, attachment);
								await HofAfterUpload(returnedTuple, e.Message);
								break;
							}
						}
					}
					else
					{
						if (e.Message != null)
						{
							if (e.Message.Content.StartsWith("http") && e.Message.Content.Contains("wotinspector"))
							{
								string[] splitted = e.Message.Content.Split(' ');
								string url = splitted[0];
								Tuple<string, DiscordMessage> returnedTuple = await Handle(string.Empty, e.Channel, await e.Guild.GetMemberAsync(e.Author.Id), e.Guild.Name, e.Guild.Id, url);
								await HofAfterUpload(returnedTuple, e.Message);
							}
						}
					}
				}
				else
				{
					//ReplayResults die niet in HOF komen
					WGBattle replayInfo = new(string.Empty);
					bool wasReplay = false;
					if (e.Message.Attachments.Count > 0)
					{
						foreach (DiscordAttachment attachment in e.Message.Attachments)
						{
							if (attachment.FileName.EndsWith(".wotbreplay"))
							{
								await ConfirmCommandExecuting(e.Message);
								wasReplay = true;
								replayInfo = await GetReplayInfo(string.Empty, attachment, GetIGNFromMember(member.DisplayName).Item2, null);
							}
						}
					}
					else
					{
						if (e.Message != null)
						{
							if (e.Message.Content.StartsWith("http") && e.Message.Content.Contains("wotinspector"))
							{
								await ConfirmCommandExecuting(e.Message);
								wasReplay = true;
								replayInfo = await GetReplayInfo(string.Empty, null, GetIGNFromMember(member.DisplayName).Item2, e.Message.Content);
							}
						}
					}
					if (wasReplay && replayInfo != null)
					{
						string thumbnail = string.Empty;
						string eventDescription = string.Empty;
						try
						{
							WeeklyEventHandler weeklyEventHandler = new();
							eventDescription = await weeklyEventHandler.GetStringForWeeklyEvent(replayInfo);
						}
						catch (Exception ex)
						{
							await HandleError("Tijdens het nakijken van het wekelijkse event: ", ex.Message, ex.StackTrace);
						}
						List<Tuple<string, string>> images = await GetAllMaps(e.Guild.Id);
						foreach (Tuple<string, string> map in images)
						{
							if (replayInfo.map_name.ToLower().Contains(map.Item1.ToLower()))
							{
								try
								{
									if (map.Item1 != string.Empty)
									{
										thumbnail = map.Item2;
									}
								}
								catch (Exception ex)
								{
									await HandleError("Could not set thumbnail for embed:", ex.Message, ex.StackTrace);
								}
								break;
							}
						}
						EmbedOptions options = new()
						{
							Thumbnail = thumbnail,
							Title = "Resultaat",
							Description = await GetDescriptionForReplay(replayInfo, -1, eventDescription),
							IsForReplay = true,
						};
						await CreateEmbed(e.Channel, options);
						await ConfirmCommandExecuted(e.Message);
					}
					else if (wasReplay)
					{
						await e.Message.DeleteReactionsEmojiAsync(GetDiscordEmoji(Constants.IN_PROGRESS_REACTION));
						await e.Message.CreateReactionAsync(GetDiscordEmoji(Constants.ERROR_REACTION));
					}
				}
			}
			discordMessage = null;
		}
		else if (e.Channel.IsPrivate)
		{
			await HandleWeeklyEventDM(e.Channel, e.Message);
		}
	}

	private async Task HandleWeeklyEventDM(DiscordChannel Channel, DiscordMessage lastMessage)
	{
		if (Channel.IsPrivate && weeklyEventWinner != null && weeklyEventWinner.Item1 != 0)
		{
			if (!lastMessage.Author.IsBot && Channel.Guild == null && lastMessage.CreationTimestamp > weeklyEventWinner.Item2)
			{
				string vehiclesInString = await WGVehicle.vehiclesToString(WarGamingAppId, ["name"]);
				Json json = new(vehiclesInString, string.Empty);
				List<Json> jsons = json.subJsons[1].subJsons;
				List<string> tanks = [];
				foreach (Json item in jsons)
				{
					tanks.Add(item.tupleList[0].Item2.Item1.Trim('"').Replace("\\", string.Empty));
				}

				string chosenTank = tanks.Find(tank => tank == lastMessage.Content);
				if (chosenTank == null || chosenTank.Length == 0)
				{
					//specifieker vragen
					tanks.Sort();
					IEnumerable<string> containsStringList = tanks.Where(tank => tank.ToLower().Contains(lastMessage.Content.ToLower()));
					if (containsStringList.Count() > 20)
					{
						await Channel.SendMessageAsync("Wees iets specifieker want er werden te veel resultaten gevonden!");
					}
					else if (!containsStringList.Any())
					{
						await Channel.SendMessageAsync("Die tank kon niet gevonden worden! Zoekterm: `" + lastMessage.Content + "`");
					}
					else
					{
						StringBuilder sb = new("```");
						sb.Append(Environment.NewLine);
						foreach (string tank in containsStringList)
						{
							sb.Append(tank + Environment.NewLine);
						}
						sb.AppendLine("```");
						await Channel.SendMessageAsync("Deze tanks bevatten je zoekterm. **Kopieer** de naam van de tank en stuur hem naar mij door om zo de juiste te selecteren. (**Hoofdlettergevoelig**):");
						await Channel.SendMessageAsync(sb.ToString());
					}
				}
				else
				{
					//tank was chosen
					await Channel.SendMessageAsync("Je hebt de **" + chosenTank + "** geselecteerd. Goede keuze!\nIk zal hem onmiddelijk instellen als nieuwe tank voor het wekelijks event.");
					WeeklyEventHandler weeklyEventHandler = new();
					await weeklyEventHandler.CreateNewWeeklyEvent(chosenTank, await GetWeeklyEventChannel());
					weeklyEventWinner = new Tuple<ulong, DateTime>(0, DateTime.Now);//dit vermijdt dat deze event telkens opnieuw zal opgeroepen worden + dat anderen het zomaar kunnen aanpassen
				}
			}
		}
	}

	#endregion

	#region getChannel

	public static async Task<DiscordChannel> GetHallOfFameChannel(ulong GuildID)
	{
		long ChatID = GuildID.Equals(Constants.NLBE_SERVER_ID) ? 793268894454251570 : 793429499403304960;
		return await GetChannel(GuildID, (ulong) ChatID);
	}
	public static async Task<DiscordChannel> GetMasteryReplaysChannel(ulong GuildID)
	{
		ulong ChatID = GuildID.Equals(Constants.NLBE_SERVER_ID) ? Constants.MASTERY_REPLAYS_ID : Constants.PRIVE_ID;
		return await GetChannel(GuildID, ChatID);
	}
	public static async Task<DiscordChannel> GetReplayResultsChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 583958593129414677;
		return await GetChannel(ServerID, ChatID);
	}
	public static async Task<DiscordChannel> GetWeeklyEventChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 897749692895596565;
		if (Constants.version.ToLower().Contains("local"))
		{
			ServerID = Constants.DA_BOIS_ID;
			ChatID = 901480697011777538;
		}
		return await GetChannel(ServerID, ChatID);
	}
	public static async Task<DiscordChannel> GetAlgemeenChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 507575682046492692;
		return await GetChannel(ServerID, ChatID);
	}
	public static async Task<DiscordChannel> GetOudLedenChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 744462244951228507;
		return await GetChannel(ServerID, ChatID);
	}
	public static async Task<DiscordChannel> GetDeputiesChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 668211371522916389;
		return await GetChannel(ServerID, ChatID);
	}
	public static async Task<DiscordChannel> GetWelkomChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 681960256296976405;
		return await GetChannel(ServerID, ChatID);
	}
	public static async Task<DiscordChannel> GetRegelsChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 679531304882012165;
		return await GetChannel(ServerID, ChatID);
	}
	public static async Task<DiscordChannel> GetLogChannel(ulong GuildID)
	{
		return GuildID == Constants.NLBE_SERVER_ID ? await GetChannel(GuildID, 782308602882031660) : await GetChannel(GuildID, 808319637447376899);
	}
	public static async Task<DiscordChannel> GetToernooiAanmeldenChannel(ulong GuildID)
	{
		return GuildID == Constants.NLBE_SERVER_ID
			? await GetChannel(GuildID, Constants.NLBE_TOERNOOI_AANMELDEN_KANAAL_ID)
			: await GetChannel(GuildID, Constants.DA_BOIS_TOERNOOI_AANMELDEN_KANAAL_ID);
	}
	public static async Task<DiscordChannel> GetMappenChannel(ulong GuildID)
	{
		long ChatID = GuildID.Equals(Constants.NLBE_SERVER_ID) ? 782240999190953984 : 804856157918855209;
		return await GetChannel(GuildID, (ulong) ChatID);
	}
	public static async Task<DiscordChannel> GetBottestChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 781617141069774898;
		return await GetChannel(ServerID, ChatID);
	}
	public static async Task<DiscordChannel> GetTestChannel()
	{
		ulong ServerID = Constants.DA_BOIS_ID;
		ulong ChatID = 804477788676685874;
		return await GetChannel(ServerID, ChatID);
	}
	public static async Task<DiscordChannel> GetPollsChannel(bool isDeputyPoll, ulong GuildID)
	{
		if (GuildID == Constants.NLBE_SERVER_ID)
		{
			long ChatID = isDeputyPoll ? 805800443178909756 : 781522161159897119;
			return await GetChannel(GuildID, (ulong) ChatID);
		}
		else
		{
			return await GetTestChannel();
		}
	}
	private static async Task<DiscordChannel> GetChannel(ulong serverID, ulong chatID)
	{
		try
		{
			DiscordGuild guild = await discordClient.GetGuildAsync(serverID);
			if (guild != null)
			{
				return guild.GetChannel(chatID);
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, ex.Message);
		}

		return null;
	}
	private static async Task<DiscordGuild> GetGuild(ulong serverID)
	{
		return await discordClient.GetGuildAsync(serverID);
	}
	public static async Task<DiscordChannel> GetChannelBasedOnString(string guildNameOrTag, ulong guildID)
	{
		bool isId = false;
		if (guildNameOrTag.StartsWith('<'))
		{
			isId = true;
			guildNameOrTag = guildNameOrTag.TrimStart('<');
			guildNameOrTag = guildNameOrTag.TrimStart('#');
			guildNameOrTag = guildNameOrTag.TrimEnd('>');
		}
		DiscordGuild guild = await GetGuild(guildID);
		if (guild != null)
		{
			foreach (KeyValuePair<ulong, DiscordChannel> channel in guild.Channels)
			{
				if (isId)
				{
					if (channel.Value.Id.ToString().Equals(guildNameOrTag.ToLower()))
					{
						return channel.Value;
					}
				}
				else
				{
					if (channel.Value.Name.ToLower().Equals(guildNameOrTag.ToLower()))
					{
						return channel.Value;
					}
				}
			}
		}
		return null;
	}

	#endregion

	public static async Task UpdateUsers()
	{
		DiscordChannel bottestChannel = await GetBottestChannel();
		if (bottestChannel != null)
		{
			bool sjtubbersUserNameIsOk = true;
			DiscordMember sjtubbersMember = await bottestChannel.Guild.GetMemberAsync(359817512109604874);
			if (sjtubbersMember.DisplayName != "[NLBE] sjtubbers")
			{
				sjtubbersUserNameIsOk = false;
			}

			if (!sjtubbersUserNameIsOk)
			{
				await SendMessage(bottestChannel, await bottestChannel.Guild.GetMemberAsync(Constants.THIBEASTMO_ID), bottestChannel.Guild.Name, "**De bijnaam van sjtubbers was al incorrect dus ben ik gestopt voor ik begon met nakijken van de rest van de bijnamen.**");
				return;
			}
			const int maxMemberChangesAmount = 7;
			IReadOnlyCollection<DiscordMember> members = await bottestChannel.Guild.GetAllMembersAsync();
			Dictionary<DiscordMember, string> memberChanges = [];
			List<DiscordMember> membersNotFound = [];
			List<DiscordMember> correctNicknamesOfMembers = [];
			//test 3x if names are correct
			for (int i = 0; i < 3; i++)
			{
				foreach (DiscordMember member in members)
				{
					if ((memberChanges.Count + membersNotFound.Count) >= maxMemberChangesAmount)
					{
						break;
					}

					if (!member.IsBot && member.Roles != null && member.Roles.Contains(bottestChannel.Guild.GetRole(Constants.LEDEN_ROLE)))
					{
						bool accountFound = false;
						bool goodClanTag = false;
						Tuple<string, string> gebruiker = GetIGNFromMember(member.DisplayName);
						IReadOnlyList<WGAccount> wgAccounts = await WGAccount.searchByName(SearchAccuracy.EXACT, gebruiker.Item2, WarGamingAppId, false, true, false);
						if (wgAccounts != null && wgAccounts.Count > 0)
						{
							//Account met exact deze gebruikersnaam gevonden
							accountFound = true;
							string clanTag = string.Empty;
							if (gebruiker.Item1.Length > 1 && gebruiker.Item1.StartsWith('[') && gebruiker.Item1.EndsWith(']'))
							{
								goodClanTag = true;
								string currentClanTag = string.Empty;
								if (wgAccounts[0].clan != null && wgAccounts[0].clan.tag != null)
								{
									currentClanTag = wgAccounts[0].clan.tag;
								}
								string goodDisplayName = '[' + currentClanTag + "] " + wgAccounts[0].nickname;
								if (wgAccounts[0].nickname != null && !member.DisplayName.Equals(goodDisplayName))
								{
									memberChanges.TryAdd(member, goodDisplayName);
								}
								else if (member.DisplayName.Equals(goodDisplayName))
								{
									correctNicknamesOfMembers.Add(member);
								}
							}
							if (!goodClanTag)
							{
								if (wgAccounts[0].clan != null && wgAccounts[0].clan.tag != null)
								{
									clanTag = wgAccounts[0].clan.tag;
								}
								string goodDisplayName = '[' + clanTag + "] " + wgAccounts[0].nickname;
								memberChanges.TryAdd(member, goodDisplayName);
							}
						}
						if (!accountFound)
						{
							membersNotFound.Add(member);
						}
					}
				}
			}
			IEnumerable<DiscordMember> correctNicknames = correctNicknamesOfMembers.Distinct();
			for (int i = 0; i < memberChanges.Count; i++)
			{
				DiscordMember currentMember = memberChanges.Keys.ElementAt(i);
				if (correctNicknames.Contains(currentMember))
				{
					memberChanges.Remove(currentMember);
					i--;
				}
			}
			for (int i = 0; i < membersNotFound.Count; i++)
			{
				DiscordMember currentMember = membersNotFound[i];
				if (correctNicknames.Contains(currentMember))
				{
					memberChanges.Remove(currentMember);
					i--;
				}
			}
			if (memberChanges.Count + membersNotFound.Count == 0)
			{
				string bericht = "Bijnamen van gebruikers nagekeken maar geen namen moesten aangepast worden.";
				await bottestChannel.SendMessageAsync("**" + bericht + "**");
				discordClient.Logger.LogInformation(bericht);
			}
			else if (memberChanges.Count + membersNotFound.Count < maxMemberChangesAmount)
			{
				foreach (KeyValuePair<DiscordMember, string> memberChange in memberChanges)
				{
					await SendMessage(bottestChannel, await bottestChannel.Guild.GetMemberAsync(Constants.THIBEASTMO_ID), bottestChannel.Guild.Name, "**Gaat bijnaam van **`" + memberChange.Key.DisplayName + "`** aanpassen naar **`" + memberChange.Value + "`");
					await ChangeMemberNickname(memberChange.Key, memberChange.Value);
				}
				foreach (DiscordMember memberNotFound in membersNotFound)
				{
					await SendMessage(bottestChannel, await bottestChannel.Guild.GetMemberAsync(Constants.THIBEASTMO_ID), bottestChannel.Guild.Name, "**Bijnaam van **`" + memberNotFound.DisplayName + "` (Discord ID: `" + memberNotFound.Id + "`)** komt niet overeen met WoTB account.**");
					await SendPrivateMessage(memberNotFound, bottestChannel.Guild.Name, "Hallo,\n\nEr werd voor iedere gebruiker in de NLBE discord server gecontroleerd of je bijnaam overeenkomt met je wargaming account.\nHelaas is dit voor jou niet het geval.\nZou je dit zelf even willen aanpassen aub?\nPas je bijnaam aan naargelang de vereisten het #regels kanaal.\n\nAlvast bedankt!\n- [NLBE] sjtubbers#4241");
				}
			}
			else
			{
				StringBuilder sb = new("Deze spelers hadden een verkeerde bijnaam:\n```");
				if (memberChanges.Count > 0)
				{
					foreach (KeyValuePair<DiscordMember, string> memberChange in memberChanges)
					{
						sb.AppendLine(memberChange.Key.DisplayName + " -> " + memberChange.Value);
					}
					sb.Append("```");
				}
				if (membersNotFound.Count > 0)
				{
					if (sb.Length > 3)
					{
						sb.Append("Deze spelers konden niet gevonden worden:\n```");
					}
					foreach (DiscordMember memberNotFound in membersNotFound)
					{
						sb.AppendLine(memberNotFound.DisplayName);
					}
					sb.Append("```");
				}
				await SendMessage(bottestChannel, await bottestChannel.Guild.GetMemberAsync(Constants.THIBEASTMO_ID), bottestChannel.Guild.Name, "**De bijnamen van 7 of meer spelers waren incorrect of niet gevonden dus ben ik gestopt voor ik begon met nakijken van de rest van de bijnamen.\nHier is een lijstje van aanpassingen die zouden gemaakt zijn:**\n" + sb);
			}
		}
	}

	public static async Task GenerateLogMessage(DiscordMessage message, DiscordChannel toernooiAanmeldenChannel, ulong userID, string emojiAsEmoji)
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
			if (Emoj.GetIndex(GetEmojiAsString(emojiAsEmoji)) > 0)
			{
				try
				{
					bool botReactedWithThisEmoji = false;
					IReadOnlyList<DiscordUser> userListOfThisEmoji = await message.GetReactionsAsync(GetDiscordEmoji(emojiAsEmoji));
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
						await message.DeleteReactionsEmojiAsync(GetDiscordEmoji(emojiAsEmoji));
					}
				}
				catch (Exception ex)
				{
					await HandleError("While adding to log: ", ex.Message, ex.StackTrace);
				}
			}
		}
	}

	public async Task<string> AskQuestion(DiscordChannel channel, DiscordUser user, DiscordGuild guild, string question)
	{
		if (channel != null)
		{
			try
			{
				await channel.SendMessageAsync(question);
				TimeSpan newPlayerWaitTime = TimeSpan.FromDays(int.TryParse(_configuration["NLBEBOT:NewPlayerWaitTimeInDays"], out int newPlayerWaitTimeInt) ? newPlayerWaitTimeInt : 0);
				InteractivityResult<DiscordMessage> message = await channel.GetNextMessageAsync(user, newPlayerWaitTime);

				if (!message.TimedOut)
				{
					return message.Result.Content;
				}
				else
				{
					await SayNoResponse(channel);
					DiscordMember member = await guild.GetMemberAsync(user.Id);
					if (member != null)
					{
						try
						{
							await member.RemoveAsync("[New member] No answer");
						}
						catch
						{
							bool isBanned = false;
							try
							{
								await guild.BanMemberAsync(member);
								isBanned = true;
							}
							catch
							{
								discordClient.Logger.LogWarning("{DisplayName}({Username}#{Discriminator}) could not be kicked from the server!", member.DisplayName, member.Username, member.Discriminator);
							}
							if (isBanned)
							{
								try
								{
									await guild.UnbanMemberAsync(user);
								}
								catch
								{
									try
									{
										await user.UnbanAsync(guild);
									}
									catch
									{
										discordClient.Logger.LogWarning("{DisplayName}({Username}#{Discriminator}) could not be unbanned from the server!", member.DisplayName, member.Username, member.Discriminator);
										DiscordMember thibeastmo = await guild.GetMemberAsync(Constants.THIBEASTMO_ID);
										if (thibeastmo != null)
										{
											await thibeastmo.SendMessageAsync("**Gebruiker [" + member.DisplayName + "(" + member.Username + "#" + member.Discriminator + ")] kon niet geünbanned worden!**");
										}
									}
								}
							}
						}
					}
					await CleanChannel(guild.Id, channel.Id);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("goOverAllQuestions:\n" + e.StackTrace);
			}
			return null;
		}
		else
		{
			discordClient.Logger.LogWarning("Channel for new members couldn't be found! Giving the noob role to user: {Username}#{Discriminator}", user.Username, user.Discriminator);
			DiscordRole noobRole = guild.GetRole(Constants.NOOB_ROLE);
			bool roleWasGiven = false;
			if (noobRole != null)
			{
				DiscordMember member = guild.GetMemberAsync(user.Id).Result;
				if (member != null)
				{
					await member.GrantRoleAsync(noobRole);
					roleWasGiven = true;
				}
			}
			if (!roleWasGiven)
			{
				discordClient.Logger.LogWarning("The noob role could not be given to user: {Username}#{Discriminator}", user.Username, user.Discriminator);
			}
		}
		return null;
	}

	public static async Task ConfirmCommandExecuting(DiscordMessage message)
	{
		await Task.Delay(875);
		await message.CreateReactionAsync(GetDiscordEmoji(Constants.IN_PROGRESS_REACTION));
	}
	public static async Task ConfirmCommandExecuted(DiscordMessage message)
	{
		await Task.Delay(875);
		await message.DeleteReactionsEmojiAsync(GetDiscordEmoji(Constants.IN_PROGRESS_REACTION));
		await Task.Delay(875);
		await message.CreateReactionAsync(GetDiscordEmoji(Constants.ACTION_COMPLETED_REACTION));
	}

	public static DiscordEmoji GetDiscordEmoji(string name)
	{
		try
		{
			return DiscordEmoji.FromName(discordClient, name);
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, ex.Message);
		}

		DiscordEmoji theEmoji = DiscordEmoji.FromUnicode(name);
		if (theEmoji != null)
		{
			return theEmoji;
		}
		else
		{
			try
			{
				theEmoji = DiscordEmoji.FromName(discordClient, name);
			}
			catch (Exception ex)
			{
				HandleError("Could not load emoji:", ex.Message, ex.StackTrace).Wait();
			}
			return theEmoji;
		}
	}

	public static string GetEmojiAsString(string emoji)
	{
		DiscordEmoji theEmoji = GetDiscordEmoji(emoji);
		if (!theEmoji.GetDiscordName().Equals(emoji))
		{
			return theEmoji.GetDiscordName();
		}

		try
		{
			return DiscordEmoji.FromUnicode(discordClient, emoji).Name;
		}
		catch
		{
			return emoji;
		}
	}

	public static string GetProperFileName(string file)
	{
		string[] splitted = file.Split('\\');
		string name = splitted[splitted.Length - 1];
		return Path.GetFileNameWithoutExtension(name).Replace('_', ' ');
	}

	public static async Task<DiscordMember> GetDiscordMember(DiscordGuild guild, ulong userID)
	{
		return await guild.GetMemberAsync(userID);
	}

	public static async Task<string> GetOrganisator(DiscordMessage message)
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
				DiscordMember member = await GetDiscordMember(message.Channel.Guild, message.Author.Id);
				if (member != null)
				{
					return member.DisplayName;
				}
			}
		}
		return string.Empty;
	}

	public static async Task<List<Tier>> ReadTeams(DiscordChannel channel, DiscordMember member, string guildName, string[] parameters_as_in_hoeveelste_team)
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
						await SendMessage(channel, member, guildName, "**Het getal mag maximum 100 zijn!**");
						goodNumber = false;
					}
					else
					{
						hoeveelste--;
					}
				}
				else if (hoeveelste < 1)
				{
					await SendMessage(channel, member, guildName, "**Het getal moet groter zijn dan 0!**");
					goodNumber = false;
				}

				if (goodNumber)
				{
					DiscordChannel toernooiAanmeldenChannel = await GetToernooiAanmeldenChannel(channel.Guild.Id);
					if (toernooiAanmeldenChannel != null)
					{
						List<DiscordMessage> messages = [];
						try
						{
							IReadOnlyList<DiscordMessage> xMessages = toernooiAanmeldenChannel.GetMessagesAsync((hoeveelste + 1)).Result;
							foreach (DiscordMessage message in xMessages)
							{
								messages.Add(message);
							}
						}
						catch (Exception ex)
						{
							await HandleError("Could not load messages from " + toernooiAanmeldenChannel.Name + ":", ex.Message, ex.StackTrace);
						}
						if (messages.Count == (hoeveelste + 1))
						{
							DiscordMessage theMessage = messages[hoeveelste];
							if (theMessage != null)
							{
								if (theMessage.Author.Id.Equals(Constants.NLBE_BOT) || theMessage.Author.Id.Equals(Constants.TESTBEASTV2_BOT))
								{
									DiscordChannel logChannel = await GetLogChannel(channel.Guild.Id);
									if (logChannel != null)
									{
										IReadOnlyList<DiscordMessage> logMessages = await logChannel.GetMessagesAsync(100);
										Dictionary<DateTime, List<DiscordMessage>> sortedMessages = logMessages.SortMessages();
										List<Tier> tiers = [];

										foreach (KeyValuePair<DateTime, List<DiscordMessage>> sMessage in sortedMessages)
										{
											string xdate = theMessage.Timestamp.ConvertToDate();
											string ydate = sMessage.Key.ConvertToDate();

											if (xdate.Equals(ydate))
											{
												sMessage.Value.Sort((x, y) => x.Timestamp.CompareTo(y.Timestamp));
												foreach (DiscordMessage discMessage in sMessage.Value)
												{
													string[] splitted = discMessage.Content.Split(Constants.LOG_SPLIT_CHAR);
													if (splitted[1].ToLower().Equals("teams"))
													{
														Tier newTeam = new();
														bool found = false;
														foreach (Tier aTeam in tiers)
														{
															if (aTeam.TierNummer.Equals(GetEmojiAsString(splitted[3])))
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
																newTeam.TierNummer = GetEmojiAsString(splitted[3]);
																string emojiAsString = GetEmojiAsString(splitted[3]);
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
										await HandleError("Could not find log channel!", string.Empty, string.Empty);
									}
								}
								else
								{
									Dictionary<DiscordEmoji, List<DiscordUser>> reactions = theMessage.SortReactions();

									List<Tier> teams = [];
									foreach (KeyValuePair<DiscordEmoji, List<DiscordUser>> reaction in reactions)
									{
										Tier aTeam = new();
										int counter = 1;
										foreach (DiscordUser user in reaction.Value)
										{
											string displayName = user.Username;
											DiscordMember memberx = toernooiAanmeldenChannel.Guild.GetMemberAsync(user.Id).Result;
											if (memberx != null)
											{
												displayName = memberx.DisplayName;
											}
											aTeam.AddDeelnemer(displayName, user.Id);
											counter++;
										}
										if (aTeam.Organisator.Equals(string.Empty))
										{
											foreach (KeyValuePair<ulong, DiscordGuild> aGuild in discGuildslist)
											{
												if (aGuild.Key.Equals(Constants.NLBE_SERVER_ID))
												{
													DiscordMember theMemberAuthor = await GetDiscordMember(aGuild.Value, theMessage.Author.Id);
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
											aTeam.TierNummer = reaction.Key;
											string emojiAsString = GetEmojiAsString(reaction.Key);
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
											DiscordMember tempUser = await channel.Guild.GetMemberAsync(user.Item1);
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
										Description = (teams.Count > 0 ? string.Empty : "Geen teams"),
										Fields = deflist,
									};
									await CreateEmbed(channel, options);
									return [];
								}
							}
							else
							{
								await SendMessage(channel, member, guildName, "**Het bericht kon niet gevonden worden!**");
							}
						}
						else
						{
							await SendMessage(channel, member, guildName, "**Dit bericht kon niet gevonden worden!**");
						}
					}
					else
					{
						await SendMessage(channel, member, guildName, "**Het kanaal #Toernooi-aanmelden kon niet gevonden worden!**");
					}
				}
			}
			else
			{
				await SendMessage(channel, member, guildName, "**Je moet cijfer meegeven!**");
			}
		}
		else
		{
			await SendMessage(channel, member, guildName, "**Je mag maar één cijfer meegeven!**");
		}
		return null;
	}

	public static List<Tier> EditWhenRedundance(List<Tier> teams)
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
	public static async Task<List<Tuple<ulong, string>>> GetIndividualParticipants(List<Tier> teams, DiscordGuild guild)
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
							DiscordMember tempMember = await guild.GetMemberAsync(participant.Item1);
							if (tempMember != null && tempMember.DisplayName != null && tempMember.DisplayName.Length > 0)
							{
								temp = tempMember.DisplayName;
							}
						}
						catch (Exception ex)
						{
							_logger.LogDebug(ex, ex.Message);
						}

						temp = string.IsNullOrEmpty(temp) ? participant.Item2 : RemoveSyntaxe(participant.Item2);
						bool alreadyInList = false;
						foreach (Tuple<ulong, string> participantX in participants)
						{
							if ((participantX.Item1.Equals(participant.Item1) && participant.Item1 > 0) || participantX.Item2.Equals(RemoveSyntaxe(participant.Item2)))
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
	private static string RemoveSyntaxe(string stringItem)
	{
		stringItem = stringItem.Replace("\\", string.Empty);
		if (stringItem.StartsWith("**") && stringItem.EndsWith("**"))
		{
			return stringItem.Trim('*');
		}
		else if (stringItem.StartsWith('`') && stringItem.EndsWith('`'))
		{
			return stringItem.Trim('`');
		}
		else
		{
			return stringItem;
		}
	}
	public static List<string> RemoveSyntaxes(List<string> stringList)
	{
		List<string> tempList = [];
		foreach (string item in stringList)
		{
			tempList.Add(RemoveSyntaxe(item));
		}
		return tempList;
	}
	public static List<Tuple<ulong, string>> RemoveSyntaxes(List<Tuple<ulong, string>> stringList)
	{
		List<Tuple<ulong, string>> tempList = [];
		foreach (Tuple<ulong, string> item in stringList)
		{
			tempList.Add(new Tuple<ulong, string>(item.Item1, RemoveSyntaxe(item.Item2)));
		}
		return tempList;
	}
	public static async Task<List<string>> GetMentions(List<Tuple<ulong, string>> memberList, ulong guildID)
	{
		DiscordGuild guild = await GetGuild(guildID);
		if (guild != null)
		{
			List<string> mentionList = [];
			foreach (Tuple<ulong, string> member in memberList)
			{
				bool addByString = true;
				if (member.Item1 > 1)
				{
					DiscordMember tempMember = await guild.GetMemberAsync(member.Item1);
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
					IReadOnlyCollection<DiscordMember> usersList = await guild.GetAllMembersAsync();
					if (usersList != null)
					{
						foreach (DiscordMember memberItem in usersList)
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

	public static async Task WriteInLog(ulong guildID, string date, string message)
	{
		DiscordChannel logChannel = await GetLogChannel(guildID);
		if (logChannel != null)
		{
			await logChannel.SendMessageAsync(date + "|" + message);
		}
		else
		{
			await HandleError("Could not find log channel, message: " + date + "|" + message, string.Empty, string.Empty);
		}
	}
	public static async Task WriteInLog(ulong guildID, string message)
	{
		DiscordChannel logChannel = await GetLogChannel(guildID);
		if (logChannel != null)
		{
			await logChannel.SendMessageAsync(message);
		}
		else
		{
			await HandleError("Could not find log channel, message: " + message, string.Empty, string.Empty);
		}
	}
	public static async Task ClearLog(ulong guildID, int amount)
	{
		DiscordChannel logChannel = await GetLogChannel(guildID);
		if (logChannel != null)
		{
			IReadOnlyList<DiscordMessage> messages = await logChannel.GetMessagesAsync(amount);
			foreach (DiscordMessage message in messages)
			{
				try
				{
					await message.DeleteAsync();
					await Task.Delay(875);
				}
				catch (Exception ex)
				{
					await HandleError("Could not delete message:", ex.Message, ex.StackTrace);
				}
			}
		}
		else
		{
			await HandleError("Could not find log channel!", string.Empty, string.Empty);
		}
	}

	public static async Task ChangeMemberNickname(DiscordMember member, string nickname)
	{
		try
		{
			void mem(MemberEditModel item)
			{
				item.Nickname = nickname;
				item.AuditLogReason = "Changed by NLBE-";
			}
			await member.ModifyAsync(mem);
		}
		catch (Exception ex)
		{
			await HandleError("Could not edit displayname for " + member.Username + ":", ex.Message, ex.StackTrace);
		}
	}
	public static string UpdateName(DiscordMember member, string oldName)
	{
		string returnString = oldName;
		IEnumerable<DiscordRole> memberRoles = member.Roles;
		if (oldName.Contains('[') && oldName.Contains(']'))
		{
			string[] splitted = oldName.Split('[');
			StringBuilder sb = new();
			if (oldName.StartsWith('['))
			{
				for (int i = 1; i < splitted.Length; i++)
				{
					sb.Append(splitted[i]);
				}
				splitted = sb.ToString().Split(']');

				sb.Clear();

				foreach (DiscordRole role in memberRoles)
				{
					if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
					{
						if (!oldName.StartsWith("[" + role.Name + "]"))
						{
							returnString = oldName.Replace("[" + splitted[0] + "]", "[" + role.Name + "]");
						}
						if (!returnString.StartsWith("[" + role.Name + "] "))
						{
							returnString = returnString.Replace("[" + role.Name + "]", "[" + role.Name + "] ");
						}
						break;
					}
				}
			}
			else
			{
				foreach (DiscordRole role in memberRoles)
				{
					if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
					{
						if (oldName.StartsWith(role.Name + "] "))
						{
							returnString = "[" + oldName;
						}
						else
						{
							for (int i = 1; i < splitted.Length; i++)
							{
								if (i > 1)
								{
									sb.Append('[');
								}
								sb.Append(splitted[i]);
							}
							splitted = sb.ToString().Split(']');
							sb = new StringBuilder();
							for (int i = 1; i < splitted.Length; i++)
							{
								if (i > 1)
								{
									sb.Append(']');
								}
								sb.Append(splitted[i]);
							}
							returnString = "[" + role.Name + "] " + sb.ToString();
						}
					}
				}
			}

		}
		else if (oldName.Contains('['))
		{
			foreach (DiscordRole role in memberRoles)
			{
				if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
				{
					if (!oldName.StartsWith("[" + role.Name))
					{
						string[] splitted = oldName.Split(' ');
						if (splitted.Length > 1)
						{
							StringBuilder sb = new();
							for (int i = 1; i < splitted.Length; i++)
							{
								if (i > 1)
								{
									sb.Append(' ');
								}
								sb.Append(splitted[i]);
							}
							returnString = "[" + role.Name + "] " + sb.ToString();
						}
						else
						{
							returnString = "[" + role.Name + "] " + oldName.Replace("[", string.Empty);
						}
					}
					else
					{
						string[] splitted = oldName.Split(' ');
						if (splitted.Length > 1)
						{
							StringBuilder sb = new();
							for (int i = 1; i < splitted.Length; i++)
							{
								if (i > 1)
								{
									sb.Append(' ');
								}
								sb.Append(splitted[i]);
							}
							returnString = "[" + role.Name + "] " + sb.ToString();
						}
						else
						{
							returnString = oldName.Replace("[" + role.Name, "[" + role.Name + "] ") + oldName.Replace("[" + role.Name, string.Empty);
						}
					}
				}
			}
		}
		else if (oldName.Contains(']'))
		{
			foreach (DiscordRole role in memberRoles)
			{
				if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
				{
					if (!oldName.StartsWith(role.Name + "]"))
					{
						string[] splitted = oldName.Split(' ');
						if (splitted.Length > 1)
						{
							StringBuilder sb = new();
							for (int i = 1; i < splitted.Length; i++)
							{
								if (i > 1)
								{
									sb.Append(' ');
								}
								sb.Append(splitted[i]);
							}
							returnString = "[" + role.Name + "] " + sb.ToString().Replace("]", string.Empty);
						}
						else
						{
							returnString = "[" + role.Name + "] " + oldName.Replace("]", string.Empty);
						}
					}
					else
					{
						string[] splitted = oldName.Split(' ');
						if (splitted.Length > 1)
						{
							StringBuilder sb = new();
							for (int i = 1; i < splitted.Length; i++)
							{
								if (i > 1)
								{
									sb.Append(' ');
								}
								sb.Append(splitted[i]);
							}
							returnString = "[" + role.Name + "] " + sb.ToString();
						}
						else
						{
							returnString = oldName.Replace(role.Name + "]", "[" + role.Name + "] ") + oldName.Replace("]" + role.Name, string.Empty);
						}
					}
				}
			}
		}
		else
		{
			foreach (DiscordRole role in memberRoles)
			{
				if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
				{
					string[] splitted = oldName.Split(' ');
					if (splitted.Length > 1)
					{
						StringBuilder sb = new();
						for (int i = 0; i < splitted.Length; i++)
						{
							if (i > 0)
							{
								sb.Append(' ');
							}
							if (!splitted[i].Equals(string.Empty) && !splitted[i].Equals(" "))
							{
								sb.Append(splitted[i]);
							}
						}
						returnString = "[" + role.Name + "] " + sb.ToString();
					}
					else
					{
						returnString = "[" + role.Name + "] " + oldName;
					}
				}
			}
		}

		bool isFromNLBE = false;
		foreach (DiscordRole role in member.Roles)
		{
			if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
			{
				isFromNLBE = true;
			}
		}
		if (!isFromNLBE)
		{
			if (returnString.StartsWith("[NLBE]") || returnString.StartsWith("[NLBE2]"))
			{
				returnString = returnString.Replace("[NLBE2]", "[]").Replace("[NLBE]", "[]");
			}
		}

		while (returnString.EndsWith(' '))
		{
			returnString = returnString.Remove(returnString.Length - 1);
		}

		return returnString;
	}

	public static bool CheckIfAllWithinRange(string[] tiers, int min, int max)
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

	public static async Task CleanChannel(ulong serverID, ulong channelID)
	{
		DiscordChannel channel = await GetChannel(serverID, channelID);
		IReadOnlyList<DiscordMessage> messages = channel.GetMessagesAsync(100).Result;
		foreach (DiscordMessage message in messages)
		{
			if (!message.Pinned)
			{
				await channel.DeleteMessageAsync(message);
				await Task.Delay(875);
			}
		}
	}
	public static async Task CleanWelkomChannel()
	{
		DiscordChannel welkomChannel = await GetWelkomChannel();
		IReadOnlyList<DiscordMessage> messages = welkomChannel.GetMessagesAsync(100).Result;
		foreach (DiscordMessage message in messages)
		{
			if (!message.Pinned)
			{
				await welkomChannel.DeleteMessageAsync(message);
				await Task.Delay(875);
			}
		}
	}
	public static async Task CleanWelkomChannel(ulong userID)
	{
		DiscordChannel welkomChannel = await GetWelkomChannel();
		IReadOnlyList<DiscordMessage> messages = welkomChannel.GetMessagesAsync(100).Result;
		foreach (DiscordMessage message in messages)
		{
			bool deleteMessage = false;
			if (!message.Pinned)
			{
				if (message.Author.Id.Equals(Constants.NLBE_BOT))
				{
					if (message.Content.Contains("<@" + userID + ">"))
					{
						deleteMessage = true;
					}
				}
				else if (message.Author.Id.Equals(userID))
				{
					deleteMessage = true;
				}
			}
			if (deleteMessage)
			{
				await welkomChannel.DeleteMessageAsync(message);
				await Task.Delay(875);
			}
		}
	}

	public static DiscordRole GetDiscordRole(ulong serverID, ulong id)
	{
		foreach (KeyValuePair<ulong, DiscordGuild> guild in discGuildslist)
		{
			if (guild.Key.Equals(serverID))
			{
				foreach (KeyValuePair<ulong, DiscordRole> role in guild.Value.Roles)
				{
					if (role.Key.Equals(id))
					{
						return role.Value;
					}
				}
				return null;
			}
		}
		return null;
	}

	public static bool HasRight(DiscordMember member, Command command)
	{
		if (member.Guild.Id.Equals(Constants.DA_BOIS_ID) || member.Guild.Id.Equals(Constants.NLBE_SERVER_ID))
		{
			bool hasRights = false;
			if (member.Guild.Id.Equals(Constants.DA_BOIS_ID))
			{
				return true;
			}
			switch (command.Name.ToLower())
			{
				case "help":
					hasRights = true;
					break;
				case "map":
					hasRights = true;
					break;
				case "gebruiker":
					hasRights = true;
					break;
				case "gebruikerslijst":
					hasRights = true;
					break;
				case "clan":
					hasRights = true;
					break;
				case "clanmembers":
					hasRights = true;
					break;
				case "spelerinfo":
					hasRights = true;
					break;
				case "toernooi":
					foreach (DiscordRole role in member.Roles)
					{
						if (role.Id.Equals(Constants.TOERNOOI_DIRECTIE))
						{
							hasRights = true;
							break;
						}
					}
					break;
				case "toernooien":
					foreach (DiscordRole role in member.Roles)
					{
						if (role.Id.Equals(Constants.TOERNOOI_DIRECTIE))
						{
							hasRights = true;
							break;
						}
					}
					break;
				case "teams":
					foreach (DiscordRole role in member.Roles)
					{
						if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE) || role.Id.Equals(Constants.DISCORD_ADMIN_ROLE) || role.Id.Equals(Constants.DEPUTY_ROLE) || role.Id.Equals(Constants.BEHEERDER_ROLE) || role.Id.Equals(Constants.TOERNOOI_DIRECTIE))
						{
							hasRights = true;
							break;
						}
					}
					break;
				case "tagteams":
					foreach (DiscordRole role in member.Roles)
					{
						if (role.Id.Equals(Constants.DISCORD_ADMIN_ROLE) || role.Id.Equals(Constants.BEHEERDER_ROLE) || role.Id.Equals(Constants.TOERNOOI_DIRECTIE))
						{
							hasRights = true;
							break;
						}
					}
					break;
				case "hof":
					foreach (DiscordRole role in member.Roles)
					{
						if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
						{
							hasRights = true;
							break;
						}
					}
					break;
				case "hofplayer":
					foreach (DiscordRole role in member.Roles)
					{
						if (role.Id.Equals(Constants.NLBE_ROLE) || role.Id.Equals(Constants.NLBE2_ROLE))
						{
							hasRights = true;
							break;
						}
					}
					break;
				case "resethof":
					foreach (DiscordRole role in member.Roles)
					{
						if (role.Id.Equals(Constants.BEHEERDER_ROLE) || role.Id.Equals(Constants.DISCORD_ADMIN_ROLE))
						{
							hasRights = true;
							break;
						}
					}
					break;
				case "weekly":
					foreach (DiscordRole role in member.Roles)
					{
						if (role.Id.Equals(Constants.BEHEERDER_ROLE) || role.Id.Equals(Constants.DISCORD_ADMIN_ROLE))
						{
							hasRights = true;
							break;
						}
					}
					break;
				case "removeplayerhof":
					foreach (DiscordRole role in member.Roles)
					{
						if (role.Id.Equals(Constants.DISCORD_ADMIN_ROLE) || role.Id.Equals(Constants.DEPUTY_ROLE))
						{
							hasRights = true;
							break;
						}
					}
					break;
				case "renameplayerhof":
					foreach (DiscordRole role in member.Roles)
					{
						if (role.Id.Equals(Constants.DISCORD_ADMIN_ROLE) || role.Id.Equals(Constants.DEPUTY_ROLE))
						{
							hasRights = true;
							break;
						}
					}
					break;
				case "poll":
					if (member.Id.Equals(414421187888676875))
					{
						hasRights = true;
					}
					foreach (DiscordRole role in member.Roles)
					{
						if (role.Id.Equals(Constants.DISCORD_ADMIN_ROLE) || role.Id.Equals(Constants.DEPUTY_ROLE) || role.Id.Equals(Constants.BEHEERDER_ROLE) || role.Id.Equals(Constants.TOERNOOI_DIRECTIE))
						{
							hasRights = true;
							break;
						}
					}
					break;
				case "updategebruikers":
					foreach (DiscordRole role in member.Roles)
					{
						if (role.Id.Equals(Constants.BEHEERDER_ROLE) || role.Id.Equals(Constants.DISCORD_ADMIN_ROLE))
						{
							hasRights = true;
							break;
						}
					}
					break;
				case "deputypoll":
					foreach (DiscordRole role in member.Roles)
					{
						if (role.Id.Equals(Constants.DEPUTY_ROLE) || role.Id.Equals(Constants.DEPUTY_NLBE_ROLE) || role.Id.Equals(Constants.DEPUTY_NLBE2_ROLE) || role.Id.Equals(Constants.DISCORD_ADMIN_ROLE))
						{
							hasRights = true;
							break;
						}
					}
					break;
				default:
					foreach (DiscordRole role in member.Roles)
					{
						if (role.Id.Equals(Constants.DISCORD_ADMIN_ROLE) || role.Id.Equals(Constants.DEPUTY_ROLE) || role.Id.Equals(Constants.BEHEERDER_ROLE) || role.Id.Equals(Constants.TOERNOOI_DIRECTIE))
						{
							hasRights = true;
							break;
						}
					}
					break;
			}
			return hasRights;
		}
		else
		{
			return false;
		}
	}

	public static async Task ShowMemberInfo(DiscordChannel channel, object gebruiker)
	{
		if (gebruiker is DiscordMember discordMember)
		{
			DiscordEmbedBuilder.EmbedAuthor newAuthor = new()
			{
				Name = discordMember.Username.Replace('_', '▁'),
				IconUrl = discordMember.AvatarUrl
			};

			List<DEF> deflist = [];
			DEF newDef1 = new()
			{
				Name = "Gebruiker",
				Value = (discordMember.Username + "#" + discordMember.Discriminator).adaptToDiscordChat(),
				Inline = true
			};
			deflist.Add(newDef1);
			DEF newDef2 = new()
			{
				Name = "Bijnaam",
				Value = discordMember.DisplayName.adaptToDiscordChat(),
				Inline = true
			};
			deflist.Add(newDef2);
			DEF newDef3 = new()
			{
				Name = "GebruikersID",
				Value = discordMember.Id.ToString(),
				Inline = true
			};
			deflist.Add(newDef3);
			DEF newDef4 = new()
			{
				Name = "Rol" + (discordMember.Roles.Count() > 1 ? "len" : string.Empty)
			};
			StringBuilder sbRoles = new();
			bool firstTime = true;
			foreach (DiscordRole role in discordMember.Roles)
			{
				if (firstTime)
				{
					firstTime = false;
				}
				else
				{
					sbRoles.Append(", ");
				}
				sbRoles.Append(role.Name.Replace('_', '▁'));
			}
			if (sbRoles.Length == 0)
			{
				sbRoles.Append("`Had geen rol`");
			}
			newDef4.Value = sbRoles.ToString().adaptToDiscordChat();
			newDef4.Inline = true;
			deflist.Add(newDef4);
			DEF newDef5 = new()
			{
				Name = "Gejoined op"
			};
			string[] splitted = discordMember.JoinedAt.ConvertToDate().Split(' ');
			newDef5.Value = splitted[0] + " " + splitted[1];
			newDef5.Inline = true;
			deflist.Add(newDef5);
			if (discordMember.Presence != null)
			{
				StringBuilder sb = new();
				sb.AppendLine(discordMember.Presence.Status.ToString());
				if (discordMember.Presence.Activity != null)
				{
					if (discordMember.Presence.Activity.CustomStatus != null)
					{
						if (discordMember.Presence.Activity.CustomStatus.Name != null)
						{
							sb.AppendLine(discordMember.Presence.Activity.CustomStatus.Name);
						}
					}
				}
				DEF newDef6 = new()
				{
					Name = "Status",
					Value = sb.ToString(),
					Inline = true
				};
				deflist.Add(newDef6);
			}
			if (discordMember.Verified.HasValue)
			{
				if (!discordMember.Verified.Value)
				{
					DEF newDef6 = new()
					{
						Name = "Niet bevestigd!",
						Value = "Dit account is niet bevestigd!",
						Inline = true
					};
					deflist.Add(newDef6);
				}
			}

			EmbedOptions options = new()
			{
				Title = "Info over " + discordMember.DisplayName.adaptToDiscordChat() + (discordMember.IsBot ? " [BOT]" : ""),
				Fields = deflist,
				Author = newAuthor,
			};
			await CreateEmbed(channel, options);
		}
		else if (gebruiker is WGAccount account)
		{
			List<DEF> deflist = [];
			WGAccount member = account;
			try
			{
				DEF newDef1 = new()
				{
					Name = "Gebruikersnaam",
					Value = member.nickname.adaptToDiscordChat(),
					Inline = true
				};
				deflist.Add(newDef1);
				if (member.clan != null)
				{
					if (member.clan.tag != null)
					{
						DEF newDef2 = new()
						{
							Name = "Clan",
							Value = member.clan.tag.adaptToDiscordChat(),
							Inline = true
						};
						deflist.Add(newDef2);
						DEF newDef4 = new()
						{
							Name = "Rol",
							Value = member.clan.role.ToString().adaptToDiscordChat(),
							Inline = true
						};
						deflist.Add(newDef4);
						DEF newDef5 = new()
						{
							Name = "Clan gejoined op"
						};
						string[] splitted = member.clan.joined_at.Value.ConvertToDate().Split(' ');
						newDef5.Value = splitted[0] + " " + splitted[1];
						newDef5.Inline = true;
						deflist.Add(newDef5);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogDebug(ex, ex.Message);
			}
			finally
			{
				DEF newDef3 = new()
				{
					Name = "SpelerID",
					Value = member.account_id.ToString(),
					Inline = true
				};
				deflist.Add(newDef3);
				if (member.created_at.HasValue)
				{
					DEF newDef6 = new()
					{
						Name = "Gestart op",
						Value = member.created_at.Value.ConvertToDate(),
						Inline = true
					};
					deflist.Add(newDef6);
				}
				if (member.last_battle_time.HasValue)
				{
					DEF newDef6 = new()
					{
						Name = "Laatst actief",
						Value = member.last_battle_time.Value.ConvertToDate(),
						Inline = true
					};
					deflist.Add(newDef6);
				}
				if (member.statistics != null)
				{
					if (member.statistics.rating != null)
					{
						DEF newDef4 = new()
						{
							Name = "Rating (WR)",
							Value = (member.statistics.rating.battles > 0 ? string.Format("{0:.##}", CalculateWinRate(member.statistics.rating.wins, member.statistics.rating.battles)) : "Nog geen rating gespeeld"),
							Inline = true
						};
						deflist.Add(newDef4);
					}
					if (member.statistics.all != null)
					{
						DEF newDef5 = new()
						{
							Name = "Winrate",
							Value = string.Format("{0:.##}", CalculateWinRate(member.statistics.all.wins, member.statistics.all.battles)),
							Inline = true
						};
						deflist.Add(newDef5);
						DEF newDef6 = new()
						{
							Name = "Gem. damage",
							Value = (member.statistics.all.damage_dealt / member.statistics.all.battles).ToString(),
							Inline = true
						};
						deflist.Add(newDef6);
						DEF newDef7 = new()
						{
							Name = "Battles",
							Value = member.statistics.all.battles.ToString(),
							Inline = true
						};
						deflist.Add(newDef7);
					}
				}

				EmbedOptions options = new()
				{
					Title = "Info over " + member.nickname.adaptToDiscordChat(),
					Fields = deflist,
					Color = Constants.BOT_COLOR,
					NextMessage = member.blitzstars
				};

				await CreateEmbed(channel, options);
			}
		}
		else if (gebruiker is DiscordUser discordUser)
		{
			DiscordEmbedBuilder.EmbedAuthor newAuthor = new()
			{
				Name = discordUser.Username.adaptToDiscordChat(),
				IconUrl = discordUser.AvatarUrl
			};

			List<DEF> deflist = [];
			DEF newDef1 = new()
			{
				Name = "Gebruikersnaam",
				Value = discordUser.Username.adaptToDiscordChat(),
				Inline = true
			};
			deflist.Add(newDef1);
			DEF newDef3 = new()
			{
				Name = "GebruikersID",
				Value = discordUser.Id.ToString(),
				Inline = true
			};
			deflist.Add(newDef3);
			DEF newDef4 = new()
			{
				Name = "Gecreëerd op"
			};
			string[] splitted = discordUser.CreationTimestamp.ConvertToDate().Split(' ');
			newDef4.Value = splitted[0] + " " + splitted[1];
			newDef4.Inline = true;
			deflist.Add(newDef4);
			if (discordUser.Flags.HasValue)
			{
				if (!discordUser.Flags.Value.ToString().Equals("None"))
				{
					DEF newDef2 = new()
					{
						Name = "Discord Medailles",
						Value = discordUser.Flags.Value.ToString(),
						Inline = true
					};
					deflist.Add(newDef2);
				}
			}
			if (discordUser.Email != null && discordUser.Email.Length > 0)
			{
				DEF newDef5 = new()
				{
					Name = "E-mail",
					Value = discordUser.Email,
					Inline = true
				};
				deflist.Add(newDef5);
			}
			if (discordUser.Locale != null && discordUser.Locale.Length > 0)
			{
				DEF newDef5 = new()
				{
					Name = "Taal",
					Value = discordUser.Locale,
					Inline = true
				};
				deflist.Add(newDef5);
			}
			if (discordUser.Presence != null)
			{
				if (discordUser.Presence.Activity != null && discordUser.Presence.Activity.CustomStatus != null && discordUser.Presence.Activity.CustomStatus.Name != null)
				{
					DEF newDef7 = new()
					{
						Name = "Custom status",
						Value = (discordUser.Presence.Activity.CustomStatus.Emoji != null ? discordUser.Presence.Activity.CustomStatus.Emoji.Name : string.Empty) + discordUser.Presence.Activity.CustomStatus.Name.adaptToDiscordChat(),
						Inline = true
					};
					deflist.Add(newDef7);
				}
				if (discordUser.Presence.Activities != null && discordUser.Presence.Activities.Count > 0)
				{
					StringBuilder sb = new();
					foreach (DiscordActivity item in discordUser.Presence.Activities)
					{
						string temp = string.Empty;
						bool customStatus = false;
						if (item.CustomStatus != null)
						{
							if (item.CustomStatus.Name.Length > 0)
							{
								customStatus = true;
								temp = (item.CustomStatus.Emoji != null ? item.CustomStatus.Emoji.Name : string.Empty) + item.CustomStatus.Name;
							}
						}
						if (!customStatus)
						{
							temp = item.Name;
						}
						bool streaming = false;
						if (item.StreamUrl != null)
						{
							if (item.StreamUrl.Length > 0)
							{
								streaming = true;
								sb.AppendLine("[" + temp + "](" + item.StreamUrl + ")");
							}
						}
						if (!streaming)
						{
							sb.AppendLine(temp);
						}
					}
					DEF newDef7 = new()
					{
						Name = "Recente activiteiten",
						Value = sb.ToString().adaptToDiscordChat(),
						Inline = true
					};
					deflist.Add(newDef7);
				}
				if (discordUser.Presence.Activity != null && discordUser.Presence.Activity.Name != null)
				{
					string temp = string.Empty;
					bool customStatus = false;
					if (discordUser.Presence.Activity.CustomStatus != null)
					{
						if (discordUser.Presence.Activity.CustomStatus.Name.Length > 0)
						{
							customStatus = true;
							temp = (discordUser.Presence.Activity.CustomStatus.Emoji != null ? discordUser.Presence.Activity.CustomStatus.Emoji.Name : string.Empty) + discordUser.Presence.Activity.CustomStatus.Name;
						}
					}
					if (!customStatus)
					{
						bool streaming = false;
						if (discordUser.Presence.Activity.StreamUrl != null)
						{
							if (discordUser.Presence.Activity.StreamUrl.Length > 0)
							{
								streaming = true;
								temp = "[" + discordUser.Presence.Activity.Name + "](" + discordUser.Presence.Activity.StreamUrl + ")";
							}
						}
						if (!streaming)
						{
							temp = discordUser.Presence.Activity.Name;
						}
					}
					DEF newDefx = new()
					{
						Name = "Activiteit",
						Value = temp,
						Inline = true
					};
					deflist.Add(newDefx);
				}
				if (discordUser.Presence.ClientStatus != null)
				{
					StringBuilder sb = new();

					if (discordUser.Presence.ClientStatus.Desktop.HasValue)
					{
						sb.AppendLine("Desktop");
					}

					if (discordUser.Presence.ClientStatus.Mobile.HasValue)
					{
						sb.AppendLine("Mobiel");
					}

					if (discordUser.Presence.ClientStatus.Web.HasValue)
					{
						sb.AppendLine("Web");
					}

					if (sb.Length > 0)
					{
						DEF newDef7 = new()
						{
							Name = "Op discord via",
							Value = sb.ToString(),
							Inline = true
						};
						deflist.Add(newDef7);
					}
				}
			}
			if (discordUser.PremiumType.HasValue)
			{
				DEF newDef7 = new()
				{
					Name = "Premiumtype",
					Value = discordUser.PremiumType.Value.ToString(),
					Inline = true
				};
				deflist.Add(newDef7);
			}
			if (discordUser.Verified.HasValue)
			{
				if (!discordUser.Verified.Value)
				{
					DEF newDef6 = new()
					{
						Name = "Niet bevestigd!",
						Value = "Dit account is niet bevestigd!",
						Inline = true
					};
					deflist.Add(newDef6);
				}
			}

			EmbedOptions options = new()
			{
				Title = "Info over " + discordUser.Username.adaptToDiscordChat() + "#" + discordUser.Discriminator + (discordUser.IsBot ? " [BOT]" : ""),
				Fields = deflist,
				Author = newAuthor,
			};
			await CreateEmbed(channel, options);
		}
	}

	private static double CalculateWinRate(int wins, int battles)
	{
		return (wins / battles) * 100;
	}

	public static async Task ShowClanInfo(DiscordChannel channel, WGClan clan)
	{
		List<DEF> deflist = [];
		DEF newDef1 = new()
		{
			Name = "Clannaam",
			Value = clan.name.adaptToDiscordChat(),
			Inline = true
		};
		deflist.Add(newDef1);
		DEF newDef2 = new()
		{
			Name = "Aantal leden",
			Value = clan.members_count.ToString(),
			Inline = true
		};
		deflist.Add(newDef2);
		DEF newDef3 = new()
		{
			Name = "ClanID",
			Value = clan.clan_id.ToString(),
			Inline = true
		};
		deflist.Add(newDef3);
		DEF newDef4 = new()
		{
			Name = "ClanTag",
			Value = clan.tag.adaptToDiscordChat(),
			Inline = true
		};
		deflist.Add(newDef4);
		if (clan.created_at.HasValue)
		{
			DEF newDef5 = new()
			{
				Name = "Gemaakt op"
			};
			string[] splitted = clan.created_at.Value.ConvertToDate().Split(' ');
			newDef5.Value = splitted[0] + " " + splitted[1];
			newDef5.Inline = true;
			deflist.Add(newDef5);
		}
		DEF newDef6 = new()
		{
			Name = "Clan motto",
			Value = clan.motto.adaptDiscordLink().adaptToDiscordChat(),
			Inline = false
		};
		deflist.Add(newDef6);
		DEF newDef7 = new()
		{
			Name = "Clan beschrijving",
			Value = clan.description.adaptDiscordLink().adaptToDiscordChat(),
			Inline = false
		};
		deflist.Add(newDef7);

		EmbedOptions options = new()
		{
			Title = "Info over " + clan.name.adaptToDiscordChat(),
			Fields = deflist,
		};
		await CreateEmbed(channel, options);
	}

	public static async Task ShowTournamentInfo(DiscordChannel channel, WGTournament tournament, string titel)
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
					Value = tournament.fee.amount.ToString() + (tournament.fee.currency != null ? (tournament.fee.currency.Length > 0 ? " (" + tournament.fee.currency + ")" : string.Empty) : string.Empty),
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
					Value = tournament.winner_award.amount.ToString() + (tournament.winner_award.currency != null ? (tournament.winner_award.currency.Length > 0 ? " (" + tournament.winner_award.currency + ")" : string.Empty) : string.Empty),
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
			ImageUrl = (tournament.logo != null ? (tournament.logo.original ?? string.Empty) : string.Empty),
		};
		await CreateEmbed(channel, options);
	}

	public static async Task<DiscordMessage> SayCannotBePlayedAt(DiscordChannel channel, DiscordMember member, string guildName, string roomType)
	{
		if (roomType.Length == 0)
		{
			return await member.SendMessageAsync("Geef aub even door welk type room dit is want het werd niet herkent door de  Tag gebruiker thibeastmo#9998");
		}

		return await SendMessage(channel, member, guildName, "**De battle mag niet in een " + roomType + " room gespeeld zijn!**");
	}
	public static async Task SaySomethingWentWrong(DiscordChannel channel, DiscordMember member, string guildName)
	{
		await SaySomethingWentWrong(channel, member, guildName, "**Er ging iets mis, probeer het opnieuw!**");
	}
	public static async Task<DiscordMessage> SaySomethingWentWrong(DiscordChannel channel, DiscordMember member, string guildName, string text)
	{
		return await SendMessage(channel, member, guildName, text);
	}
	public static async Task SayWrongAttachments(DiscordChannel channel, DiscordMember member, string guildName)
	{
		await SendMessage(channel, member, guildName, "**Geen bruikbare documenten in de bijlage gevonden!**");
	}
	public static async Task SayNoAttachments(DiscordChannel channel, DiscordMember member, string guildName)
	{
		await SendMessage(channel, member, guildName, "**Geen documenten in de bijlage gevonden!**");
	}
	public static async Task SayNoResponse(DiscordChannel channel)
	{
		await channel.SendMessageAsync("`Time-out: Geen antwoord.`");
	}
	public static async Task SayNoResponse(DiscordChannel channel, DiscordMember member, string guildName)
	{
		await SendMessage(channel, member, guildName, "`Time-out: Geen antwoord.`");
	}
	public static async Task SayMustBeNumber(DiscordChannel channel)
	{
		await channel.SendMessageAsync("**Je moest een cijfer geven!**");
	}
	public static async Task SayNumberTooSmall(DiscordChannel channel)
	{
		await channel.SendMessageAsync("**Dat cijfer was te klein!**");
	}
	public static async Task SayNumberTooBig(DiscordChannel channel)
	{
		await channel.SendMessageAsync("**Dat cijfer was te groot!**");
	}
	public static async Task SayBeMoreSpecific(DiscordChannel channel)
	{
		EmbedOptions options = new()
		{
			Title = "Wees specifieker",
			Description = "Er waren te veel resultaten, probeer iets specifieker te zijn!",
		};
		await CreateEmbed(channel, options);
	}
	public static DiscordMessage SayMultipleResults(DiscordChannel channel, string description)
	{
		DiscordEmbedBuilder newDiscEmbedBuilder = new()
		{
			Color = DiscordColor.Red,
			Title = "Meerdere resultaten gevonden",
			Description = description.adaptToDiscordChat()
		};
		DiscordEmbed embed = newDiscEmbedBuilder.Build();
		try
		{
			return channel.SendMessageAsync(null, embed).Result;
		}
		catch (Exception ex)
		{
			HandleError("Something went wrong while trying to send an embedded message:", ex.Message, ex.StackTrace).Wait();
			return null;
		}
	}
	public static async Task SayNoResults(DiscordChannel channel, string description)
	{
		DiscordEmbedBuilder newDiscEmbedBuilder = new()
		{
			Color = DiscordColor.Red,
			Title = "Geen resultaten gevonden",
			Description = description.Replace('_', '▁')
		};
		DiscordEmbed embed = newDiscEmbedBuilder.Build();
		try
		{
			await channel.SendMessageAsync(null, embed);
		}
		catch (Exception ex)
		{
			await HandleError("Something went wrong while trying to send an embedded message:", ex.Message, ex.StackTrace);
		}
	}
	public static async Task SayTheUserIsNotAllowed(DiscordChannel channel)
	{
		DiscordEmbedBuilder newDiscEmbedBuilder = new()
		{
			Color = DiscordColor.Red,
			Title = "Geen toegang",
			Description = ":raised_back_of_hand: Je hebt niet voldoende rechten om deze commando uit te voeren!"
		};
		DiscordEmbed embed = newDiscEmbedBuilder.Build();
		try
		{
			await channel.SendMessageAsync(null, embed);
		}
		catch (Exception ex)
		{
			await HandleError("Something went wrong while trying to send an embedded message:", ex.Message, ex.StackTrace);
		}
	}
	public static async Task SayBotNotAuthorized(DiscordChannel channel)
	{
		DiscordEmbedBuilder newDiscEmbedBuilder = new()
		{
			Color = DiscordColor.Red,
			Title = "Onvoldoende rechten",
			Description = ":raised_back_of_hand: De bot heeft voldoende rechten om dit uit te voeren!"
		};
		DiscordEmbed embed = newDiscEmbedBuilder.Build();
		try
		{
			await channel.SendMessageAsync(null, embed);
		}
		catch (Exception ex)
		{
			await HandleError("Something went wrong while trying to send an embedded message:", ex.Message, ex.StackTrace);
		}
	}
	public static async Task SayTooManyCharacters(DiscordChannel channel)
	{
		DiscordEmbedBuilder newDiscEmbedBuilder = new()
		{
			Color = DiscordColor.Red,
			Title = "Onvoldoende rechten",
			Description = ":raised_back_of_hand: Er zaten te veel characters in het bericht dat de bot wilde verzenden!"
		};
		DiscordEmbed embed = newDiscEmbedBuilder.Build();
		try
		{
			await channel.SendMessageAsync(null, embed);
		}
		catch (Exception ex)
		{
			await HandleError("Something went wrong while trying to send an embedded message:", ex.Message, ex.StackTrace);
		}
	}
	public static async Task<DiscordMessage> SayReplayNotWorthy(DiscordChannel channel, WGBattle battle)
	{
		DiscordEmbedBuilder newDiscEmbedBuilder = new()
		{
			Color = DiscordColor.Red,
			Title = "Helaas..."
		};
		string extraDescription = await GetDescriptionForReplay(battle, 0);
		newDiscEmbedBuilder.Description = "De statistieken van deze replay waren onvoldoende om in de Hall Of Fame te komen te staan!\n\n" + extraDescription;
		List<Tuple<string, string>> images = await GetAllMaps(channel.Guild.Id);
		foreach (Tuple<string, string> map in images)
		{
			if (map.Item1.ToLower() == battle.map_name.ToLower())
			{
				try
				{
					if (map.Item1 != string.Empty)
					{
						newDiscEmbedBuilder.Thumbnail = new()
						{
							Url = map.Item2
						};
					}
				}
				catch (Exception ex)
				{
					await HandleError("Could not set thumbnail for embed:", ex.Message, ex.StackTrace);
				}
				break;
			}
		}
		DiscordEmbed embed = newDiscEmbedBuilder.Build();
		if (discordMessage != null)
		{
			try
			{
				return await discordMessage.RespondAsync(null, embed);
			}
			catch (Exception ex)
			{
				await HandleError("Something went wrong while trying to send an embedded message:", ex.Message, ex.StackTrace);
			}
		}
		else
		{
			return await channel.SendMessageAsync(embed: embed);
		}
		return null;
	}
	public static async Task<DiscordMessage> SayReplayIsWorthy(DiscordChannel channel, WGBattle battle, int position)
	{
		DiscordEmbedBuilder newDiscEmbedBuilder = new()
		{
			Color = DiscordColor.Red,
			Title = "Hoera! :trophy:"
		};
		string extraDescription = await GetDescriptionForReplay(battle, position);
		newDiscEmbedBuilder.Description = "Je replay heeft een plaatsje gekregen in onze Hall Of Fame!\n\n" + extraDescription;
		List<Tuple<string, string>> images = await GetAllMaps(channel.Guild.Id);
		foreach (Tuple<string, string> map in images)
		{
			if (map.Item1.ToLower() == battle.map_name.ToLower())
			{
				try
				{
					if (map.Item1 != string.Empty)
					{
						newDiscEmbedBuilder.Thumbnail = new()
						{
							Url = map.Item2
						};
					}
				}
				catch (Exception ex)
				{
					await HandleError("Could not set thumbnail for embed:", ex.Message, ex.StackTrace);
				}
				break;
			}
		}
		DiscordEmbed embed = newDiscEmbedBuilder.Build();
		if (discordMessage != null)
		{
			try
			{
				return await discordMessage.RespondAsync(null, embed);
			}
			catch (Exception ex)
			{
				await HandleError("Something went wrong while trying to send an embedded message:", ex.Message, ex.StackTrace);
			}
		}
		{
			return await channel.SendMessageAsync(embed: embed);
		}
	}
	private static async Task<string> GetDescriptionForReplay(WGBattle battle, int position, string preDescription = "")
	{
		StringBuilder sb = new(preDescription);
		try
		{
			WeeklyEventHandler weeklyEventHandler = new();
			string weeklyEventDescription = await weeklyEventHandler.GetStringForWeeklyEvent(battle);
			if (weeklyEventDescription.Length > 0)
			{
				sb.Append(Environment.NewLine + weeklyEventDescription);
			}
		}
		catch (Exception ex)
		{
			await HandleError("Tijdens het nakijken van het wekelijkse event: ", ex.Message, ex.StackTrace);
		}
		sb.Append(GetSomeReplayInfoAsText(battle, position).Replace(Constants.REPLACEABLE_UNDERSCORE_CHAR, '_'));
		return sb.ToString();
	}

	private static string GetSomeReplayInfoAsText(WGBattle battle, int position)
	{
		StringBuilder sb = new();
		sb.AppendLine(GetInfoInFormat("Link", "[" + battle.title.adaptToDiscordChat().Replace('_', Constants.UNDERSCORE_REPLACEMENT_CHAR) + "](" + battle.view_url.adaptToDiscordChat() + ")", false));
		sb.AppendLine(GetInfoInFormat("Speler", battle.player_name.adaptToDiscordChat()));
		sb.AppendLine(GetInfoInFormat("Clan", battle.details.clan_tag));
		sb.AppendLine(GetInfoInFormat("Tank", battle.vehicle));
		sb.AppendLine(GetInfoInFormat("Tier", Emoj.GetName(battle.vehicle_tier), false));
		sb.AppendLine(GetInfoInFormat("Damage", battle.details.damage_made.ToString()));
		sb.AppendLine(GetInfoInFormat("Damage bounced", battle.details.damage_blocked.ToString()));
		sb.AppendLine(GetInfoInFormat("Assist damage", (battle.details.damage_assisted + battle.details.damage_assisted_track).ToString()));
		sb.AppendLine(GetInfoInFormat("exp", battle.details.exp.ToString()));
		sb.AppendLine(GetInfoInFormat("Hits", battle.details.shots_pen.ToString()));
		sb.AppendLine(GetInfoInFormat("Tanks vernietigd", battle.details.enemies_destroyed.ToString()));
		sb.AppendLine(GetInfoInFormat("Map", battle.map_name));
		string resultaat = "Gewonnen";
		if (battle.protagonist_team != battle.winner_team)
		{
			resultaat = battle.winner_team is not 2 and not 1 ? "Gelijk gespeeld" : "Verloren";
		}
		sb.AppendLine(GetInfoInFormat("Resultaat", resultaat));
		if (battle.battle_start_time.HasValue)
		{
			sb.AppendLine(GetInfoInFormat("Datum", (battle.battle_start_time.Value.Day < 10 ? "0" : string.Empty) + battle.battle_start_time.Value.Day + "-" + battle.battle_start_time.Value.Month + "-" + battle.battle_start_time.Value.Year + " " + battle.battle_start_time.Value.Hour + ":" + (battle.battle_start_time.Value.Minute < 10 ? "0" : string.Empty) + battle.battle_start_time.Value.Minute + ":" + (battle.battle_start_time.Value.Second < 10 ? "0" : string.Empty) + battle.battle_start_time.Value.Second));
		}
		sb.AppendLine(GetInfoInFormat("Type", WGBattle.getBattleType(battle.battle_type)));
		sb.AppendLine(GetInfoInFormat("Mode", WGBattle.getBattleRoom(battle.room_type)));
		if (position > 0)
		{
			sb.AppendLine(GetInfoInFormat("Positie in HOF", position.ToString()));
		}
		if (battle.details.achievements != null && battle.details.achievements.Count > 0)
		{
			List<FMWOTB.Achievement> achievementList = [];
			for (int i = 0; i < battle.details.achievements.Count; i++)
			{
				FMWOTB.Achievement tempAchievement = FMWOTB.Achievement.getAchievement(WarGamingAppId, battle.details.achievements.ElementAt(i).t).Result;
				if (tempAchievement != null)
				{
					achievementList.Add(tempAchievement);
				}
			}
			if (achievementList.Count > 0)
			{
				achievementList = achievementList.OrderBy(x => x.order).ToList();
				sb.AppendLine("Achievements:");
				sb.Append("```");
				foreach (FMWOTB.Achievement tempAchievement in achievementList)
				{
					sb.AppendLine(tempAchievement.name.Replace("\n", string.Empty).Replace("(" + tempAchievement.achievement_id + ")", string.Empty));
				}
				sb.Append("```");
			}
		}
		return sb.ToString();
	}
	private static string GetInfoInFormat(string key, string value, bool bold = true)
	{
		if (value != null)
		{
			if (bold)
			{
				if (value != string.Empty)
				{
					value = "**" + value + "**";
				}
			}
		}
		return key + ": " + (value ?? string.Empty);
	}


	public static async Task<WGClan> SearchForClan(DiscordChannel channel, DiscordMember member, string guildName, string clan_naam, bool loadMembers, DiscordUser user, Command command)
	{
		try
		{
			IReadOnlyList<WGClan> clans = await WGClan.searchByName(SearchAccuracy.STARTS_WITH_CASE_INSENSITIVE, clan_naam, WarGamingAppId, loadMembers);
			int aantalClans = clans.Count;
			List<WGClan> clanList = [];
			foreach (WGClan clan in clans)
			{
				if (clan_naam.ToLower().Equals(clan.tag.ToLower()))
				{
					clanList.Add(clan);
				}
			}

			if (clanList.Count > 1)
			{
				StringBuilder sbFound = new();
				for (int i = 0; i < clanList.Count; i++)
				{
					sbFound.AppendLine((i + 1) + ". `" + clanList[i].tag + "`");
				}
				if (sbFound.Length < 1024)
				{
					int index = await WaitForReply(channel, user, clan_naam, clanList.Count);
					if (index >= 0)
					{
						return clanList[index];
					}
				}
				else
				{
					await SayBeMoreSpecific(channel);
				}
			}
			else if (clanList.Count == 1)
			{
				return clanList[0];
			}
			else if (clanList.Count == 0)
			{
				await SendMessage(channel, member, guildName, "**Clan(" + clan_naam + ") is niet gevonden! (In een lijst van " + aantalClans + " clans)**");
			}
		}
		catch (TooManyResultsException ex)
		{
			discordClient.Logger.LogWarning("({Command}) {Message}", command.Name, ex.Message);
			await SendMessage(channel, member, guildName, "**Te veel resultaten waren gevonden, wees specifieker!**");
		}
		return null;
	}

	public static async Task<int> WaitForReply(DiscordChannel channel, DiscordUser user, string description, int count)
	{
		DiscordMessage discMessage = SayMultipleResults(channel, description);
		InteractivityExtension interactivity = discordClient.GetInteractivity();
		InteractivityResult<DiscordMessage> message = await interactivity.WaitForMessageAsync(x => x.Channel == channel && x.Author == user);
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
				if (number > 0 && number <= count)
				{
					return (number - 1);
				}
				else if (number > count)
				{
					await SayNumberTooBig(channel);
				}
				else if (1 > number)
				{
					await SayNumberTooSmall(channel);
				}
			}
			else
			{
				await SayMustBeNumber(channel);
			}
		}
		else if (discMessage != null)
		{
			List<DiscordEmoji> reacted = [];
			for (int i = 1; i <= 10; i++)
			{
				DiscordEmoji emoji = GetDiscordEmoji(Emoj.GetName(i));
				if (emoji != null)
				{
					IReadOnlyList<DiscordUser> users = discMessage.GetReactionsAsync(emoji).Result;
					foreach (DiscordUser tempUser in users)
					{
						if (tempUser.Id.Equals(user.Id))
						{
							reacted.Add(emoji);
						}
					}
				}
			}

			if (reacted.Count == 1)
			{
				int index = Emoj.GetIndex(GetEmojiAsString(reacted[0].Name));
				if (index > 0 && index <= count)
				{
					return (index - 1);
				}
				else
				{
					await channel.SendMessageAsync("**Dat was geen van de optionele emoji's!**");
				}
			}
			else if (reacted.Count > 1)
			{
				await channel.SendMessageAsync("**Je mocht maar 1 reactie geven!**");
			}
			else
			{
				await SayNoResponse(channel);
			}
		}
		else
		{
			await SayNoResponse(channel);
		}
		return -1;
	}

	public static List<DEF> ListInMemberEmbed(int columns, List<DiscordMember> memberList, string searchTerm)
	{
		List<DiscordMember> backupMemberList = [];
		backupMemberList.AddRange(memberList);
		List<StringBuilder> sbs = [];
		for (int i = 0; i < columns; i++)
		{
			sbs.Add(new StringBuilder());
		}
		int counter = 0;
		int columnCounter = 0;
		int rest = memberList.Count % columns;
		int membersPerColumn = (memberList.Count - rest) / columns;
		int amountOfMembers = memberList.Count;
		if (amountOfMembers > 0)
		{
			while (memberList.Count > 0)
			{
				try
				{
					if (searchTerm.ToLower().Contains('b'))
					{
						sbs[columnCounter].AppendLine(memberList[0].DisplayName.adaptToDiscordChat());
					}
					else
					{
						sbs[columnCounter].AppendLine(memberList[0].Username + "#" + memberList[0].Discriminator);
					}

					//hier

					memberList.RemoveAt(0);

					if (counter == (membersPerColumn + (columnCounter == columns - 1 ? rest : 0)) || memberList.Count == 1)
					{
						if (columnCounter < (columns - 1))
						{
							columnCounter++;
						}
						counter = 0;
					}
					else
					{
						counter++;
					}
				}
				catch (Exception ex)
				{
					HandleError("Error in gebruikerslijst:", ex.Message, ex.StackTrace).Wait();
				}
			}
			List<DEF> deflist = [];
			bool firstTime = true;
			foreach (StringBuilder item in sbs)
			{
				if (item.Length > 0)
				{
					string defValue = item.ToString().adaptToDiscordChat();
					if (defValue.Length > 1024)
					{
						return ListInMemberEmbed(columns + 1, backupMemberList, searchTerm);
					}
					string[] splitted = item.ToString().Split(Environment.NewLine);
					string firstChar = RemoveSyntaxe(splitted[0]).Substring(0, 1);
					string lastChar = string.Empty;
					string defName = string.Empty;
					if (searchTerm.Contains('o') || searchTerm.Contains('c'))
					{
						if (firstTime)
						{
							firstTime = false;
							defName = "Recentst";
						}
						else
						{
							defName = item.Equals(sbs[sbs.Count - 1]) ? "minst recent" : "minder recent";
						}
					}
					else
					{
						defName = firstChar.ToUpper() + (splitted.Length > 2 ? " - " + lastChar.ToUpper() : "");
					}

					DEF newDef = new()
					{
						Inline = true,
						Name = defName.adaptToDiscordChat(),
						Value = defValue
					};
					deflist.Add(newDef);
				}
			}
			return deflist;
		}
		return [];
	}

	public static List<DEF> ListInPlayerEmbed(int columns, List<Members> memberList, string searchTerm, DiscordGuild guild)
	{
		if (memberList.Count == 0)
		{
			return [];
		}

		List<string> nameList;

		if (searchTerm.Contains('d'))
		{
			List<WGAccount> wgAccountList = memberList.Select(member => new WGAccount(WarGamingAppId, member.account_id, false, false, false))
													  .OrderBy(p => p.last_battle_time).Reverse().ToList();

			nameList = wgAccountList.Select(member => member.nickname).ToList();
		}
		else
		{
			nameList = memberList.Select(member => member.account_name).ToList();
		}

		List<StringBuilder> sbs = [];
		for (int i = 0; i < columns; i++)
		{
			sbs.Add(new StringBuilder());
		}

		int counter = 0;
		int columnCounter = 0;
		int rest = nameList.Count % columns;
		int membersPerColumn = (nameList.Count - rest) / columns;

		IReadOnlyCollection<DiscordMember> members = [];
		if (searchTerm.Contains('s'))
		{
			members = guild.GetAllMembersAsync().Result;
		}
		while (nameList.Count > 0)
		{
			try
			{
				if (searchTerm.Contains('s'))
				{
					bool found = false;
					foreach (DiscordMember memberx in members)
					{
						string[] splittedName = memberx.DisplayName.Split(']');
						if (splittedName.Length > 1)
						{
							string tempName = splittedName[1].Trim(' ');
							if (tempName.ToLower().Equals(nameList[0].ToLower()))
							{
								sbs[columnCounter].AppendLine("`" + nameList[0] + "`");
								found = true;
								break;
							}
						}
					}
					if (!found)
					{
						sbs[columnCounter].AppendLine("**" + nameList[0].adaptToDiscordChat() + "**");
					}
				}
				else
				{
					sbs[columnCounter].AppendLine(nameList[0].adaptToDiscordChat());
				}

				nameList.RemoveAt(0);

				if (counter == (membersPerColumn + (columnCounter == columns - 1 ? rest : 0)) || nameList.Count == 1)
				{
					if (columnCounter < (columns - 1))
					{
						columnCounter++;
					}
					counter = 0;
				}
				else
				{
					counter++;
				}
			}
			catch (Exception ex)
			{
				HandleError("Error in listInPlayerEmbed:", ex.Message, ex.StackTrace).Wait();
			}
		}

		List<DEF> deflist = [];
		bool firstTime = true;
		foreach (StringBuilder item in sbs)
		{
			if (item.Length > 0)
			{
				string[] splitted = item.ToString().Split(Environment.NewLine);
				string firstChar = RemoveSyntaxe(splitted[0]).Substring(0, 1);
				string lastChar = string.Empty;
				for (int i = splitted.Length - 1; i > 0; i--)
				{
					if (splitted[i] != string.Empty)
					{
						lastChar = RemoveSyntaxe(splitted[i]).ToUpper().First().ToString();
						break;
					}
				}
				string defName = string.Empty;
				if (searchTerm.Contains('d'))
				{
					if (firstTime)
					{
						firstTime = false;
						defName = "Recentst";
					}
					else
					{
						defName = item.Equals(sbs[sbs.Count - 1]) ? "minst recent" : "minder recent";
					}
				}
				else
				{
					defName = (firstChar.ToUpper() + (splitted.Length > 2 ? " - " + lastChar.ToUpper() : ""));
				}
				DEF newDef = new()
				{
					Inline = true,
					Name = defName.adaptToDiscordChat(),
					Value = item.ToString()
				};
				deflist.Add(newDef);
			}
		}
		return deflist;
	}

	public static List<string> GetSearchTermAndCondition(params string[] parameter)
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

	public static async Task<List<Tuple<string, string>>> GetAllMaps(ulong GuildID)
	{
		DiscordChannel mapChannel = await GetMappenChannel(GuildID);
		if (mapChannel != null)
		{
			List<Tuple<string, string>> images = [];
			try
			{
				IReadOnlyList<DiscordMessage> xMessages = mapChannel.GetMessagesAsync(100).Result;
				foreach (DiscordMessage message in xMessages)
				{
					IReadOnlyList<DiscordAttachment> attachments = message.Attachments;
					foreach (DiscordAttachment item in attachments)
					{
						images.Add(new Tuple<string, string>(GetProperFileName(item.Url), item.Url));
					}
				}
			}
			catch (Exception ex)
			{
				await HandleError("Could not load messages from " + mapChannel.Name + ":", ex.Message, ex.StackTrace);
			}
			images.Sort((x, y) => y.Item1.CompareTo(x.Item1));
			images.Reverse();
			return images;
		}
		return null;
	}

	public static Tuple<string, string> GetIGNFromMember(string displayName)
	{
		string[] splitted = displayName.Split(']');
		StringBuilder sb = new();
		for (int i = 1; i < splitted.Length; i++)
		{
			if (i > 1)
			{
				sb.Append(' ');
			}
			sb.Append(splitted[i]);
		}
		string clan = string.Empty;
		if (splitted.Length > 1)
		{
			clan = splitted[0] + ']';
		}
		return new Tuple<string, string>(clan, sb.ToString().Trim(' '));
	}

	public static async Task<WGAccount> SearchPlayer(DiscordChannel channel, DiscordMember member, DiscordUser user, string guildName, string naam)
	{
		try
		{
			IReadOnlyList<WGAccount> searchResults = await WGAccount.searchByName(SearchAccuracy.STARTS_WITH_CASE_INSENSITIVE, naam, WarGamingAppId, false, false, true);
			StringBuilder sb = new();
			int index = 0;
			if (searchResults != null)
			{
				if (searchResults.Count > 1)
				{
					int counter = 0;
					foreach (WGAccount account in searchResults)
					{
						counter++;
						sb.AppendLine(counter + ". " + account.nickname.adaptToDiscordChat());
					}
					index = await WaitForReply(channel, user, sb.ToString(), searchResults.Count);
				}
				if (index >= 0 && searchResults.Count >= 1)
				{
					WGAccount account = new(WarGamingAppId, searchResults[index].account_id, false, true, true);
					await ShowMemberInfo(channel, account);
					return account;
				}
				else
				{
					await SendMessage(channel, member, guildName, "**Gebruiker (**`" + naam + "`**) kon niet gevonden worden!**");
				}
			}
			else
			{
				await SendMessage(channel, member, guildName, "**Gebruiker (**`" + naam.adaptToDiscordChat() + "`**) kon niet gevonden worden!**");
			}
		}
		catch (TooManyResultsException ex)
		{
			discordClient.Logger.LogWarning("While searching for player by name: {Message}", ex.Message);
			await SendMessage(channel, member, guildName, "**Te veel resultaten waren gevonden, wees specifieker!**");
		}
		return null;
	}

	public static async Task<List<WGTournament>> InitialiseTournaments(bool all)
	{
		string tournamentJson = await Tournaments.tournamentsToString(WarGamingAppId);
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
								string wgTournamentJsonString = await WGTournament.tournamentsToString(WarGamingAppId, tournaments.tournament_id);
								Json wgTournamentJson = new(wgTournamentJsonString, "WGTournament");
								WGTournament eenToernooi = new(wgTournamentJson, WarGamingAppId);
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

	#region Hall Of Fame

	//Methods are set chronological
	public static async Task<Tuple<string, DiscordMessage>> Handle(string titel, DiscordChannel channel, DiscordMember member, string guildName, ulong guildID, string url)
	{
		return await Handle(titel, null, channel, guildName, guildID, url, member);
	}
	public static async Task<Tuple<string, DiscordMessage>> Handle(string titel, DiscordChannel channel, DiscordMember member, string guildName, ulong guildID, DiscordAttachment attachment)
	{
		return await Handle(titel, attachment, channel, guildName, guildID, null, member);
	}
	private static async Task<Tuple<string, DiscordMessage>> Handle(string titel, object discAttach, DiscordChannel channel, string guildName, ulong guildID, string iets, DiscordMember member)
	{
		if (discAttach is DiscordAttachment attachment)
		{
			discAttach = attachment;
		}
		WGBattle replayInfo = await GetReplayInfo(titel, discAttach, GetIGNFromMember(member.DisplayName).Item2, iets);
		try
		{
			if (replayInfo != null)
			{
				bool validChannel = false;
				if (guildID.Equals(Constants.DA_BOIS_ID))
				{
					validChannel = true;
				}
				else
				{
					DiscordChannel goodChannel = await GetMasteryReplaysChannel(guildID);
					if (goodChannel != null && goodChannel.Id.Equals(channel.Id))
					{
						validChannel = true;
					}
					if (!validChannel)
					{
						goodChannel = await GetBottestChannel();
						if (goodChannel.Id.Equals(channel.Id))
						{
							validChannel = true;
						}
					}
				}
				return validChannel
					? await GoHOFDetails(replayInfo, channel, member, guildName, guildID)
					: new Tuple<string, DiscordMessage>("Kanaal is niet geschikt voor HOF.", null);
			}
			else
			{
				return new Tuple<string, DiscordMessage>("Replayobject was null.", null);
			}
		}
		catch
		{
			return new Tuple<string, DiscordMessage>("Er ging iets mis.", null);
		}
	}
	private static async Task<Tuple<string, DiscordMessage>> GoHOFDetails(WGBattle replayInfo, DiscordChannel channel, DiscordMember member, string guildName, ulong guildID)
	{
		_ = (await channel.GetMessagesAsync(1))[0];
		DiscordMessage tempMessage;

		if (replayInfo.battle_type is 0 or 1) // 0 = encounter, 1 = supremacy
		{
			if (replayInfo.room_type is 1 or 4 or 5 or 7) // 1 = normal, 4 = tournament, 5 = tournament, 7 = rating 
			{
				try
				{
					return replayInfo.details != null
						? await ReplayHOF(replayInfo, guildID, channel, member, guildName)
						: new Tuple<string, DiscordMessage>("Replay bevatte geen details.", null);
				}
				catch (JsonNotFoundException e)
				{
					_ = await SaySomethingWentWrong(channel, member, guildName, "**Er ging iets mis tijdens het inlezen van de gegevens!**");
					await HandleError("While reading json from a replay:\n", e.Message, e.StackTrace);
				}
				catch (Exception e)
				{
					_ = await SaySomethingWentWrong(channel, member, guildName, "**Er ging iets mis bij het controleren van de HOF!**");
					await HandleError("While checking HOF with a replay:\n", e.Message, e.StackTrace);
				}
				tempMessage = await SendMessage(channel, member, guildName, "**Dit is een speciale replay waardoor de gegevens niet fatsoenlijk ingelezen konden worden!**");
				return new Tuple<string, DiscordMessage>(tempMessage.Content, tempMessage);
			}
			else
			{
				string roomTypeName = replayInfo.room_type switch
				{
					2 => "training",
					8 => "mad games",
					22 => "realistic",
					23 => "uprising",
					24 => "gravity force",
					25 => "skirmish",
					26 => "burning",
					_ => string.Empty
				};

				tempMessage = await SayCannotBePlayedAt(channel, member, guildName, roomTypeName);
			}
		}
		else
		{
			tempMessage = await SaySomethingWentWrong(channel, member, guildName, "**Je mag enkel de standaardbattles gebruiken! (Geen speciale gamemodes)**");
		}
		string thumbnail = string.Empty;
		List<Tuple<string, string>> images = await GetAllMaps(channel.Guild.Id);
		foreach (Tuple<string, string> map in images)
		{
			if (map.Item1.ToLower() == replayInfo.map_name.ToLower())
			{
				try
				{
					if (map.Item1 != string.Empty)
					{
						thumbnail = map.Item2;
					}
				}
				catch (Exception ex)
				{
					await HandleError("Could not set thumbnail for embed:", ex.Message, ex.StackTrace);
				}
				break;
			}
		}

		EmbedOptions options = new()
		{
			Thumbnail = thumbnail,
			Title = "Resultaat",
			Description = await GetDescriptionForReplay(replayInfo, -1),
		};
		await CreateEmbed(channel, options);
		return new Tuple<string, DiscordMessage>(tempMessage.Content, tempMessage);
	}
	public static async Task<WGBattle> GetReplayInfo(string titel, object attachment, string ign, string url)
	{
		string json = string.Empty;
		bool playerIDFound = false;
		IReadOnlyList<WGAccount> accountInfo = await WGAccount.searchByName(SearchAccuracy.EXACT, ign, WarGamingAppId, false, true, false);
		if (accountInfo != null)
		{
			if (accountInfo.Count > 0)
			{
				playerIDFound = true;
				if (attachment != null)
				{
					DiscordAttachment attach = (DiscordAttachment) attachment;
					url = attach.Url;
				}
				json = await ReplayToString(url, titel, accountInfo[0].account_id);
			}
		}
		if (!playerIDFound)
		{
			if (attachment != null)
			{
				DiscordAttachment attach = (DiscordAttachment) attachment;
				url = attach.Url;
			}
			json = await ReplayToString(url, titel, null);
		}
		try
		{
			return new WGBattle(json);
		}
		catch (Exception ex)
		{
			string attachUrl = string.Empty;
			if (attachment != null)
			{
				DiscordAttachment attach = (DiscordAttachment) attachment;
				attachUrl = attach.Url;
			}
			await HandleError("Initializing WGBattle object from (" + (url != null && url.Length > 0 ? url : attachment != null ? attachUrl : "Nothing") + "):\n", ex.Message, ex.StackTrace);
		}
		return null;
	}
	public static async Task<string> ReplayToString(string pathOrKey, string title, long? wg_id)
	{
		string url = @"https://wotinspector.com/api/replay/upload?url=";
		HttpClient httpClient = new();
		MultipartFormDataContent form1 = [];
		string AsBase64String = null;
		if (pathOrKey.StartsWith("http"))
		{
			if (pathOrKey.Contains("wotinspector"))
			{
				//return een json in deze else
				HttpResponseMessage iets = await httpClient.GetAsync("https://api.wotinspector.com/replay/upload?details=full&key=" + Path.GetFileName(pathOrKey));
				if (iets != null && iets.Content != null)
				{
					return await iets.Content.ReadAsStringAsync();
				}
				return null;
			}
			else
			{
				AsBase64String = Convert.ToBase64String(await httpClient.GetByteArrayAsync(pathOrKey));
			}
		}
		else if (pathOrKey.Contains('\\') || pathOrKey.Contains('/'))
		{
			AsBase64String = Convert.ToBase64String(await File.ReadAllBytesAsync(pathOrKey));
		}

		form1.Add(new StringContent(Path.GetFileName(pathOrKey)), "filename");
		form1.Add(new StringContent(AsBase64String), "file");
		if (!string.IsNullOrEmpty(title))
		{
			form1.Add(new StringContent(title), "title");
		}
		if (wg_id != null)
		{
			form1.Add(new StringContent(wg_id.ToString()), "loaded_by");
		}

		HttpResponseMessage response = await httpClient.PostAsync(url, form1);
		return await response.Content.ReadAsStringAsync();
	}
	public static async Task<Tuple<string, DiscordMessage>> ReplayHOF(WGBattle battle, ulong guildID, DiscordChannel channel, DiscordMember member, string guildName)
	{
		if (battle.details.clanid.Equals(Constants.NLBE_CLAN_ID) || battle.details.clanid.Equals(Constants.NLBE2_CLAN_ID))
		{
			DiscordMessage message = await GetHOFMessage(guildID, battle.vehicle_tier, battle.vehicle);
			if (message != null)
			{
				List<Tuple<string, List<TankHof>>> tierHOF = ConvertHOFMessageToTupleListAsync(message, battle.vehicle_tier);
				bool alreadyAdded = false;
				if (tierHOF != null)
				{
					foreach (Tuple<string, List<TankHof>> tank in tierHOF)
					{
						foreach (TankHof hof in tank.Item2)
						{
							if (Path.GetFileName(hof.Link).Equals(battle.hexKey))
							{
								alreadyAdded = true;
								break;
							}
						}
					}
					if (!alreadyAdded)
					{
						foreach (Tuple<string, List<TankHof>> tank in tierHOF)
						{
							if (tank.Item1.ToLower().Equals(battle.vehicle.ToLower()))
							{
								if (tank.Item2.Count == Constants.HOF_AMOUNT_PER_TANK)
								{
									if (tank.Item2[Constants.HOF_AMOUNT_PER_TANK - 1].Damage < battle.details.damage_made)
									{
										tank.Item2.Add(InitializeTankHof(battle));
										List<TankHof> sortedTankHofList = tank.Item2.OrderBy(x => x.Damage).Reverse().ToList();
										sortedTankHofList.RemoveAt(sortedTankHofList.Count - 1);
										tank.Item2.Clear();
										int counter = 1;
										int position = 0;
										foreach (TankHof item in sortedTankHofList)
										{
											tank.Item2.Add(item);
											if (item.Link.Equals(battle.view_url))
											{
												position = counter;
											}
											else
											{
												counter++;
											}
											item.Place = (short) position;
										}
										await EditHOFMessage(message, tierHOF);
										DiscordMessage tempMessage = await SayReplayIsWorthy(channel, battle, position);
										return new Tuple<string, DiscordMessage>(tempMessage.Content, tempMessage);
									}
									else
									{
										DiscordMessage tempMessage = await SayReplayNotWorthy(channel, battle);
										return new Tuple<string, DiscordMessage>(tempMessage.Content, tempMessage);
									}
								}
								else
								{
									DiscordMessage tempMessage = await AddReplayToMessage(battle, message, channel, tierHOF);
									return new Tuple<string, DiscordMessage>((tempMessage != null ? tempMessage.Content : string.Empty), tempMessage);
								}
							}
						}
					}
					else
					{
						string thumbnail = string.Empty;
						List<Tuple<string, string>> images = await GetAllMaps(channel.Guild.Id);
						foreach (Tuple<string, string> map in images)
						{
							if (map.Item1.ToLower() == battle.map_name.ToLower())
							{
								try
								{
									if (map.Item1 != string.Empty)
									{
										thumbnail = map.Item2;
									}
								}
								catch (Exception ex)
								{
									await HandleError("Could not set thumbnail for embed:", ex.Message, ex.StackTrace);
								}
								break;
							}
						}

						EmbedOptions options = new()
						{
							Thumbnail = thumbnail,
							Title = "Helaas... Deze replay staat er al in.",
							Description = await GetDescriptionForReplay(battle, 0),
						};
						DiscordMessage tempMessage = await CreateEmbed(channel, options);
						return new Tuple<string, DiscordMessage>(string.Empty, tempMessage);//string empty omdat dan hofafterupload het niet verkeerd opvat
					}
				}
				else
				{
					DiscordMessage tempMessage = await AddReplayToMessage(battle, message, channel, []);
					return new Tuple<string, DiscordMessage>((tempMessage != null ? tempMessage.Content : string.Empty), tempMessage);
				}
			}
			else
			{
				DiscordMessage tempMessage = await SaySomethingWentWrong(channel, member, guildName, "**Het bericht van de tier van de replay kon niet gevonden worden!**");
				return new Tuple<string, DiscordMessage>(tempMessage.Content, tempMessage);
			}
		}
		else
		{
			DiscordMessage tempMessage = await SaySomethingWentWrong(channel, member, guildName, "**Enkel replays van NLBE-clanleden mogen gebruikt worden!**");
			return new Tuple<string, DiscordMessage>(tempMessage.Content, tempMessage);
		}
		return null;
	}
	public static async Task<DiscordMessage> GetHOFMessage(ulong GuildID, int tier, string vehicle)
	{
		DiscordChannel channel = await GetHallOfFameChannel(GuildID);
		if (channel != null)
		{
			IReadOnlyList<DiscordMessage> messages = await channel.GetMessagesAsync(100);
			if (messages != null)
			{
				List<DiscordMessage> tierMessages = GetTierMessages(tier, messages);
				foreach (DiscordMessage tierMessage in tierMessages)
				{
					if (tierMessage.Embeds[0].Fields != null)
					{
						if (tierMessage.Embeds[0].Fields.Count > 0)
						{
							foreach (DiscordEmbedField field in tierMessage.Embeds[0].Fields)
							{
								if (field.Name.Equals(vehicle))
								{
									return tierMessage;
								}
							}
						}
					}
				}
				foreach (DiscordMessage tierMessage in tierMessages)
				{
					if (tierMessage.Embeds[0].Fields != null)
					{
						if (tierMessage.Embeds[0].Fields.Count > 0)
						{
							if (tierMessage.Embeds[0].Fields.Count < 15)//15 fields in embed
							{
								return tierMessage;
							}
						}
						else
						{
							return tierMessage;
						}
					}
					else
					{
						return tierMessage;
					}
				}
				//Tier exists but message is must be created (move all the lower tiers to the front)
				//Get messages that should be moved
				List<DiscordMessage> LTmessages = [];
				foreach (DiscordMessage tierMessage in messages)
				{
					if (tierMessage.Embeds != null)
					{
						if (tierMessage.Embeds.Count > 0)
						{
							string emojiAsString = tierMessage.Embeds[0].Title.Replace("Tier ", string.Empty);
							int index = Emoj.GetIndex(GetEmojiAsString(emojiAsString));
							if (index < tier)
							{
								LTmessages.Add(tierMessage);
							}
							else
							{
								break;
							}
						}
					}
				}
				LTmessages.Reverse();
				ulong messageToReturnID = 0;
				//Move them
				for (int i = 0; i <= LTmessages.Count; i++)
				{
					if (i == 0)
					{
						//set new message for the tier
						await LTmessages[i].ModifyAsync(CreateHOFResetEmbed(tier));
						messageToReturnID = LTmessages[i].Id;
					}
					else if (i == LTmessages.Count)
					{
						//Create new message for tier 1
						await channel.SendMessageAsync(null, LTmessages[i - 1].Embeds[0]);
					}
					else
					{
						//modify
						await LTmessages[i].ModifyAsync(null, LTmessages[i - 1].Embeds[0]);
					}
				}
				return await channel.GetMessageAsync(messageToReturnID);
			}
			return null;
		}
		else
		{
			return null;
		}
	}
	public static List<DiscordMessage> GetTierMessages(int tier, IReadOnlyList<DiscordMessage> messages)
	{
		messages = messages.Reverse().ToList();
		List<DiscordMessage> tierMessages = [];
		foreach (DiscordMessage message in messages)
		{
			if (message.Embeds != null)
			{
				if (message.Embeds.Count > 0)
				{
					if (message.Embeds[0].Title.Contains(GetDiscordEmoji(Emoj.GetName(tier))))
					{
						tierMessages.Add(message);
					}
				}
			}
		}
		return tierMessages;
	}
	public static List<Tuple<string, List<TankHof>>> ConvertHOFMessageToTupleListAsync(DiscordMessage message, int TIER)
	{
		if (message.Embeds != null)
		{
			if (message.Embeds.Count > 0)
			{
				foreach (DiscordEmbed embed in message.Embeds)
				{
					if (embed.Fields != null)
					{
						if (embed.Fields.Count > 0)
						{
							List<Tuple<string, List<TankHof>>> generatedTupleListFromMessage = [];
							foreach (DiscordEmbedField field in embed.Fields)
							{
								List<TankHof> hofList = [];
								string[] lines = field.Value.Split('\n');
								short counter = -1;
								foreach (string line in lines)
								{
									counter++;
									string speler = string.Empty;
									string link = string.Empty;
									string damage = string.Empty;
									bool firstTime = true;
									string[] splitted = line.Split(" `");
									splitted[1].Insert(0, "`");
									foreach (string item in splitted)
									{
										if (firstTime)
										{
											firstTime = false;
											string[] split = item.Split(']');
											StringBuilder sb = new();
											string[] firstPartSplitted = split[0].Split(' ');
											for (int i = 1; i < firstPartSplitted.Length; i++)
											{
												if (i > 1)
												{
													sb.Append(' ');
												}
												sb.Append(firstPartSplitted[i]);
											}
											speler = sb.ToString().Trim('[').Trim(']');
											link = split[1].Trim('(').Trim(')');
										}
										else
										{
											damage = item.Replace(" dmg`", string.Empty).Trim('`');
										}
									}
									string fieldName = field.Name.Replace("\\_", "_");
									hofList.Add(new TankHof(link, speler.Replace("\\", string.Empty), fieldName, Convert.ToInt32(damage), TIER));
									hofList[counter].Place = (short) (counter + 1);
								}
								generatedTupleListFromMessage.Add(new Tuple<string, List<TankHof>>(field.Name, hofList));
							}
							return generatedTupleListFromMessage;
						}
					}
				}
			}
		}
		return null;
	}
	public static async Task EditHOFMessage(DiscordMessage message, List<Tuple<string, List<TankHof>>> tierHOF)
	{
		try
		{
			DiscordEmbedBuilder newDiscEmbedBuilder = new()
			{
				Color = Constants.HOF_COLOR,
				Description = string.Empty
			};

			int tier = 0;
			foreach (Tuple<string, List<TankHof>> item in tierHOF)
			{
				if (item.Item2.Count > 0)
				{
					List<TankHof> sortedTankHofList = item.Item2.OrderBy(x => x.Damage).Reverse().ToList();
					StringBuilder sb = new();
					for (int i = 0; i < sortedTankHofList.Count; i++)
					{
						if (tier == 0)
						{
							tier = sortedTankHofList[i].Tier;
						}
						// ˍ
						// ＿
						// ̲
						// _ --> underscore
						// ▁
						sb.AppendLine((i + 1) + ". [" + sortedTankHofList[i].Speler.Replace("\\", string.Empty).Replace('_', Constants.UNDERSCORE_REPLACEMENT_CHAR) + "](" + sortedTankHofList[i].Link + ") `" + sortedTankHofList[i].Damage + " dmg`");
					}
					newDiscEmbedBuilder.AddField(item.Item1, sb.ToString().adaptToDiscordChat());
				}
			}

			newDiscEmbedBuilder.Title = "Tier " + GetDiscordEmoji(Emoj.GetName(tier));

			DiscordEmbed embed = newDiscEmbedBuilder.Build();
			await message.ModifyAsync(embed);
		}
		catch (Exception e)
		{
			await HandleError("While editing HOF message: ", e.Message, e.StackTrace);
			await discordMessage.CreateReactionAsync(DiscordEmoji.FromName(discordClient, Constants.MAINTENANCE_REACTION));
		}
	}
	public static async Task<DiscordMessage> AddReplayToMessage(WGBattle battle, DiscordMessage message, DiscordChannel channel, List<Tuple<string, List<TankHof>>> tierHOF)
	{
		bool foundItem = false;
		int position = 1;
		foreach (Tuple<string, List<TankHof>> item in tierHOF)
		{
			if (item.Item1.Equals(battle.vehicle))
			{
				item.Item2.Add(InitializeTankHof(battle));
				foundItem = true;
				break;
			}
		}
		if (!foundItem)
		{
			List<TankHof> list = [InitializeTankHof(battle)];
			tierHOF.Add(new Tuple<string, List<TankHof>>(battle.vehicle, list));
		}
		else
		{
			foreach (Tuple<string, List<TankHof>> item in tierHOF)
			{
				if (item.Item1.Equals(battle.vehicle))
				{
					List<TankHof> sortedTankHofList = item.Item2.OrderBy(x => x.Damage).Reverse().ToList();
					for (int i = 0; i < sortedTankHofList.Count; i++)
					{
						sortedTankHofList[i].Place = (short) (i + 1);
						if (sortedTankHofList[i].Link.Equals(battle.view_url))
						{
							position = i + 1;
							break;
						}
					}
					break;
				}
			}
		}
		await EditHOFMessage(message, tierHOF);
		return await SayReplayIsWorthy(channel, battle, position);
	}
	public static async Task<List<Tuple<string, List<TankHof>>>> GetTankHofsPerPlayer(ulong guildID)
	{
		List<Tuple<string, List<TankHof>>> players = [];
		DiscordChannel channel = await GetHallOfFameChannel(guildID);
		if (channel != null)
		{
			IReadOnlyList<DiscordMessage> messages = await channel.GetMessagesAsync(100);
			if (messages != null && messages.Count > 0)
			{
				List<Tuple<DiscordMessage, int>> allTierMessages = [];
				for (int i = 1; i <= 10; i++)
				{
					List<DiscordMessage> tierMessages = GetTierMessages(i, messages);
					foreach (DiscordMessage tempMessage in tierMessages)
					{
						allTierMessages.Add(new Tuple<DiscordMessage, int>(tempMessage, i));
					}
				}

				//Has all HOF messages
				foreach (Tuple<DiscordMessage, int> message in allTierMessages)
				{
					List<Tuple<string, List<TankHof>>> tempTanks = ConvertHOFMessageToTupleListAsync(message.Item1, message.Item2);
					if (tempTanks != null)
					{
						foreach (Tuple<string, List<TankHof>> tank in tempTanks)
						{
							foreach (TankHof th in tank.Item2)
							{
								bool found = false;
								for (int i = 0; i < players.Count; i++)
								{
									if (players[i].Item1.Equals(th.Speler))
									{
										found = true;
										players[i].Item2.Add(th);
									}
								}
								if (!found)
								{
									players.Add(new Tuple<string, List<TankHof>>(th.Speler, []));
								}
							}
						}
					}
				}
			}
		}
		return players;
	}

	public static async Task<bool> CreateOrCleanHOFMessages(DiscordChannel HOFchannel, List<Tuple<int, DiscordMessage>> tiersFound)
	{
		tiersFound.Reverse();
		for (int i = 10; i >= 1; i--)
		{
			bool made = false;
			for (int j = 0; j < tiersFound.Count; j++)
			{
				if (tiersFound[j].Item1.Equals(i))
				{
					if (!made)
					{
						await tiersFound[j].Item2.ModifyAsync(CreateHOFResetEmbed(i));
						tiersFound[j] = new Tuple<int, DiscordMessage>(i, tiersFound[j].Item2);
						made = true;
					}
					else
					{
						await tiersFound[j].Item2.DeleteAsync();
						tiersFound[j] = new Tuple<int, DiscordMessage>(i, null);
					}
				}
				else if (!made && tiersFound[j].Item1 < i)
				{
					await tiersFound[j].Item2.ModifyAsync(CreateHOFResetEmbed(i));
					tiersFound[j] = new Tuple<int, DiscordMessage>(i, tiersFound[j].Item2);
					made = true;
					break;
				}
			}
			if (!made)
			{
				await HOFchannel.SendMessageAsync(null, CreateHOFResetEmbed(i));
			}
		}
		return true;
	}
	private static DiscordEmbed CreateHOFResetEmbed(int tier)
	{
		DiscordEmbedBuilder newDiscEmbedBuilder = new()
		{
			Color = Constants.HOF_COLOR,
			Description = "Nog geen replays aan deze tier toegevoegd.",

			Title = "Tier " + GetDiscordEmoji(Emoj.GetName(tier))
		};

		return newDiscEmbedBuilder.Build();
	}
	public static TankHof InitializeTankHof(WGBattle battle)
	{
		return new TankHof(battle.view_url, battle.player_name, battle.vehicle, battle.details.damage_made, battle.vehicle_tier);
	}
	private async Task HofAfterUpload(Tuple<string, DiscordMessage> returnedTuple, DiscordMessage uploadMessage)
	{
		bool good = false;
		if (returnedTuple.Item1.Equals(string.Empty))
		{
			TimeSpan hofWaitTime = TimeSpan.FromSeconds(int.TryParse(_configuration["NLBEBOT:HofWaitTimeInSeconds"], out int hofWaitTimeInt) ? hofWaitTimeInt : 0);
			await Task.Delay(hofWaitTime);
			string description = string.Empty;
			string thumbnail = string.Empty;
			if (returnedTuple.Item2 != null)
			{
				if (returnedTuple.Item2.Embeds != null)
				{
					if (returnedTuple.Item2.Embeds.Count > 0)
					{
						foreach (DiscordEmbed embed in returnedTuple.Item2.Embeds)
						{
							if (embed.Description != null)
							{
								description = embed.Description;
								if (embed.Thumbnail != null)
								{
									if (embed.Thumbnail.Url.ToString().Length > 0)
									{
										thumbnail = embed.Thumbnail.Url.ToString();
									}
								}
							}
							if (embed.Title.ToLower().Contains("hoera"))
							{
								good = true;
								break;
							}
						}
					}
				}
			}
			if (good)
			{
				await uploadMessage.CreateReactionAsync(DiscordEmoji.FromName(discordClient, ":thumbsup:"));
			}
			else
			{
				await uploadMessage.CreateReactionAsync(DiscordEmoji.FromName(discordClient, ":thumbsdown:"));
			}
			//Pas bericht aan
			string[] splitted = description.Split('\n');
			StringBuilder sb = new();
			bool emptyLineFound = false;
			if (!splitted.Contains(string.Empty))
			{
				emptyLineFound = true;
			}
			foreach (string line in splitted)
			{
				if (emptyLineFound)
				{
					sb.AppendLine(line.Replace("\n", string.Empty).Replace("\r", string.Empty));
				}
				else if (line.Length == 0)
				{
					emptyLineFound = true;
				}
			}
			try
			{
				DiscordEmbedBuilder newDiscEmbedBuilder = new()
				{
					Color = Constants.BOT_COLOR
				};
				WeeklyEventHandler weeklyEventHandler = new();
				newDiscEmbedBuilder.Description = sb.ToString();
				newDiscEmbedBuilder.Thumbnail = new()
				{
					Url = thumbnail
				};

				newDiscEmbedBuilder.Title = "Resultaat";

				DiscordEmbed embed = newDiscEmbedBuilder.Build();
				await returnedTuple.Item2.ModifyAsync(embed);
			}
			catch (Exception e)
			{
				await HandleError("While editing Resultaat message: ", e.Message, e.StackTrace);
			}
		}
	}

	#endregion

	public static async Task HandleError(string message, string exceptionMessage, string stackTrace)
	{
		string formattedMessage = message + exceptionMessage + Environment.NewLine + stackTrace;
		_logger.LogError(formattedMessage);
		discordClient.Logger.LogError(formattedMessage);
		await SendThibeastmo(message, exceptionMessage, stackTrace);
	}

	//no longer sends to thibeastmo. This used to be a method to send towards thibeastmo but since thibeastmo is no longer hosting this it's moved towards the test channel
	public static async Task SendThibeastmo(string message, string exceptionMessage = "", string stackTrace = "")
	{
		DiscordChannel bottestChannel = await GetBottestChannel();
		if (bottestChannel != null)
		{
			StringBuilder sb = new();
			if (!string.IsNullOrEmpty(exceptionMessage) && !string.IsNullOrEmpty(stackTrace))
			{
				for (int i = 0; i < message.Length / 2; i++)
				{
					sb.Append('━');
				}
			}
			StringBuilder firstMessage = new((sb.Length > 0 ? "**" + sb.ToString() + "**\n" : string.Empty) + message);
			if (!string.IsNullOrEmpty(exceptionMessage))
			{
				firstMessage.Append("\n" + "`" + exceptionMessage + "`");
			}
			await bottestChannel.SendMessageAsync(firstMessage.ToString());
		}
	}
}
