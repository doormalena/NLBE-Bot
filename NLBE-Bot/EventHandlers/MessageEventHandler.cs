namespace NLBE_Bot.EventHandlers;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using FMWOTB.Tools.Replays;
using FMWOTB.Vehicles;
using JsonObjectConverter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using NLBE_Bot.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class MessageEventHandler(IConfiguration configuration, IErrorHandler errorHandler, IBotState botState, ILogger<MessageEventHandler> logger,
								IChannelService channelService, IUserService userService, IDiscordMessageUtils discordMessageUtils, IWeeklyEventService weeklyEventHandler,
								IMapService mapService, IReplayService replayService, ITournamentService tournamentService, IHallOfFameService hallOfFameService, IMessageService messageService) : IMessageEventHandler
{
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	private readonly IBotState _botState = botState ?? throw new ArgumentNullException(nameof(botState));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly IDiscordMessageUtils _discordMessageUtils = discordMessageUtils ?? throw new ArgumentNullException(nameof(discordMessageUtils));
	private readonly IWeeklyEventService _weeklyEventHandler = weeklyEventHandler ?? throw new ArgumentNullException(nameof(weeklyEventHandler));
	private readonly IMapService _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
	private readonly IReplayService _replayService = replayService ?? throw new ArgumentNullException(nameof(replayService));
	private readonly ITournamentService _tournamentService = tournamentService ?? throw new ArgumentNullException(nameof(tournamentService));
	private readonly IHallOfFameService _hallOfFameService = hallOfFameService ?? throw new ArgumentNullException(nameof(hallOfFameService));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
	private readonly ILogger<MessageEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public void Register(IDiscordClientWrapper client)
	{
		client.MessageCreated += OnMessageCreated;
		client.MessageDeleted += OnMessageDeleted;
		client.MessageReactionAdded += OnMessageReactionAdded;
		client.MessageReactionRemoved += OnMessageReactionRemoved;
	}

	internal async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
	{
		if (_botState.IgnoreEvents)
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
				_logger.LogInformation("CommandsNext is not enabled.");
				return;
			}

			Command command = commandsNext.FindCommand(commandName, out string rawArguments);
			if (command == null)
			{
				_logger.LogInformation("Unknown command.");
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
				_logger.LogError(ex, "Error executing command: {0}", ex.Message);
			}

			await commandsNext.ExecuteCommandAsync(ctx);

			// Handle the command
			return;
		}
		if (!e.Author.IsBot && e.Channel.Guild != null)
		{
			bool validChannel = false;
			DiscordChannel masteryChannel = await _channelService.GetMasteryReplaysChannel(e.Guild.Id);
			if (masteryChannel != null)
			{
				if (masteryChannel.Equals(e.Channel) || e.Channel.Id.Equals(Constants.BOTTEST_ID))
				{
					validChannel = true;
				}
			}
			if (!validChannel)
			{
				masteryChannel = await _channelService.GetBottestChannel();
				if (masteryChannel != null && masteryChannel.Equals(e.Channel))
				{
					validChannel = true;
				}
				if (!validChannel)
				{
					masteryChannel = await _channelService.GetReplayResultsChannel();
					if (masteryChannel != null)
					{
						if (masteryChannel.Equals(e.Channel))
						{
							validChannel = true;
						}
					}
					if (!validChannel)
					{
						masteryChannel = await _channelService.GetReplayResultsChannel();
						if (masteryChannel != null)
						{
							if (masteryChannel.Equals(e.Channel))
							{
								validChannel = true;
							}
						}
						if (!validChannel)
						{
							masteryChannel = await _channelService.GetTestChannel();
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
				_botState.LastCreatedDiscordMessage = e.Message;
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
								Tuple<string, DiscordMessage> returnedTuple = await _hallOfFameService.Handle(string.Empty, attachment, e.Channel, e.Guild.Name, e.Guild.Id, null, await e.Guild.GetMemberAsync(e.Author.Id));
								await _hallOfFameService.HofAfterUpload(returnedTuple, e.Message);
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
								Tuple<string, DiscordMessage> returnedTuple = await _hallOfFameService.Handle(string.Empty, null, e.Channel, e.Guild.Name, e.Guild.Id, url, await e.Guild.GetMemberAsync(e.Author.Id));
								await _hallOfFameService.HofAfterUpload(returnedTuple, e.Message);
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
								await _messageService.ConfirmCommandExecuting(e.Message);
								wasReplay = true;
								replayInfo = await _replayService.GetReplayInfo(string.Empty, attachment, _userService.GetIGNFromMember(member.DisplayName).Item2, null);
							}
						}
					}
					else
					{
						if (e.Message != null)
						{
							if (e.Message.Content.StartsWith("http") && e.Message.Content.Contains("wotinspector"))
							{
								await _messageService.ConfirmCommandExecuting(e.Message);
								wasReplay = true;
								replayInfo = await _replayService.GetReplayInfo(string.Empty, null, _userService.GetIGNFromMember(member.DisplayName).Item2, e.Message.Content);
							}
						}
					}
					if (wasReplay && replayInfo != null)
					{
						string thumbnail = string.Empty;
						string eventDescription = string.Empty;
						try
						{
							eventDescription = await _weeklyEventHandler.GetStringForWeeklyEvent(replayInfo);
						}
						catch (Exception ex)
						{
							await _errorHandler.HandleErrorAsync("Tijdens het nakijken van het wekelijkse event: ", ex);
						}
						List<Tuple<string, string>> images = await _mapService.GetAllMaps(e.Guild.Id);
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
									await _errorHandler.HandleErrorAsync("Could not set thumbnail for embed:", ex);
								}
								break;
							}
						}
						EmbedOptions options = new()
						{
							Thumbnail = thumbnail,
							Title = "Resultaat",
							Description = await _replayService.GetDescriptionForReplay(replayInfo, -1, eventDescription),
							IsForReplay = true,
						};
						await _messageService.CreateEmbed(e.Channel, options);
						await _messageService.ConfirmCommandExecuted(e.Message);
					}
					else if (wasReplay)
					{
						await e.Message.DeleteReactionsEmojiAsync(_discordMessageUtils.GetDiscordEmoji(Constants.IN_PROGRESS_REACTION).Inner);
						await e.Message.CreateReactionAsync(_discordMessageUtils.GetDiscordEmoji(Constants.ERROR_REACTION).Inner);
					}
				}
			}

			_botState.LastCreatedDiscordMessage = null;
		}
		else if (e.Channel.IsPrivate)
		{
			await HandleWeeklyEventDM(e.Channel, e.Message);
		}
	}

	internal async Task OnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
	{
		if (_botState.IgnoreEvents)
		{
			return;
		}

		DiscordChannel toernooiAanmeldenChannel = await _channelService.GetToernooiAanmeldenChannel(e.Guild.Id);
		if (e.Channel.Equals(toernooiAanmeldenChannel))
		{
			DateTime timeStamp = e.Message.Timestamp.LocalDateTime;
			DiscordChannel logChannel = await _channelService.GetLogChannel(e.Guild.Id);
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

	internal async Task OnMessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
	{
		if (_botState.IgnoreEvents)
		{
			return;
		}

		if (!e.User.IsBot && e.Guild.Id is Constants.NLBE_SERVER_ID or Constants.DA_BOIS_ID)
		{
			DiscordChannel toernooiAanmeldenChannel = await _channelService.GetToernooiAanmeldenChannel(e.Guild.Id);
			if (toernooiAanmeldenChannel != null && e.Channel.Equals(toernooiAanmeldenChannel))
			{
				DiscordMessage message = await toernooiAanmeldenChannel.GetMessageAsync(e.Message.Id);
				await _tournamentService.GenerateLogMessage(message, toernooiAanmeldenChannel, e.User.Id, _discordMessageUtils.GetDiscordEmoji(e.Emoji.Name).ToString());
			}
			else
			{
				DiscordChannel regelsChannel = await _channelService.GetRegelsChannel();
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
												await _userService.ChangeMemberNickname(member, "[] " + tempName);//een oorzaak

											}
											else if (member.DisplayName.Contains("[NLBE]"))
											{
												await _userService.ChangeMemberNickname(member, "[] " + member.DisplayName.Replace("[NLBE]", string.Empty).Trim(' '));//een oorzaak
											}
											else if (member.DisplayName.Contains("[NLBE2]"))
											{
												await _userService.ChangeMemberNickname(member, "[] " + member.DisplayName.Replace("[NLBE2]", string.Empty).Trim(' '));//een oorzaak
											}
										}
										else
										{
											await _userService.ChangeMemberNickname(member, "[] " + member.Username);//een oorzaak
										}
										DiscordRole ledenRole = e.Guild.GetRole(Constants.LEDEN_ROLE);
										if (ledenRole != null)
										{
											await member.GrantRoleAsync(ledenRole);//een oorzaak
										}
										DiscordChannel algemeenChannel = await _channelService.GetAlgemeenChannel();//een oorzaak
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

	internal async Task OnMessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs e)
	{
		if (_botState.IgnoreEvents)
		{
			return;
		}

		if (e.Guild.Id is Constants.NLBE_SERVER_ID or Constants.DA_BOIS_ID)
		{
			DiscordChannel toernooiAanmeldenChannel = await _channelService.GetToernooiAanmeldenChannel(e.Guild.Id);
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
						IDiscordChannel logChannel = new DiscordChannelWrapper(await _channelService.GetLogChannel(e.Guild.Id));

						if (logChannel.Inner != null)
						{
							IReadOnlyList<IDiscordMessage> logMessages = await logChannel.GetMessagesAsync(100);
							Dictionary<DateTime, List<IDiscordMessage>> sortedMessages = _discordMessageUtils.SortMessages(logMessages);
							foreach (KeyValuePair<DateTime, List<IDiscordMessage>> messageList in sortedMessages)
							{
								try
								{
									if (e.Message.CreationTimestamp.LocalDateTime.CompareDateTime(messageList.Key))
									{
										foreach (IDiscordMessage aMessage in messageList.Value)
										{
											DiscordMember member = await _userService.GetDiscordMember(e.Guild, e.User.Id);
											if (member != null)
											{
												string[] splitted = aMessage.Content.Split(Constants.LOG_SPLIT_CHAR);
												string theEmoji = _discordMessageUtils.GetEmojiAsString(e.Emoji.Name);
												if (splitted[2].Replace("\\", string.Empty).ToLower().Equals(member.DisplayName.ToLower()) && _discordMessageUtils.GetEmojiAsString(splitted[3]).Equals(theEmoji))
												{
													await aMessage.Inner.DeleteAsync("Log updated: reaction was removed from message in Toernooi-aanmelden for this user.");
												}
											}
										}
									}
								}
								catch (Exception ex)
								{
									await _errorHandler.HandleErrorAsync("Could not compare TimeStamps in MessageReactionRemoved:", ex);
								}
							}
						}
						else
						{
							await _errorHandler.HandleErrorAsync("Could not find log channel at MessageReactionRemoved!");
						}
					}
				}
			}
		}
	}

	private async Task HandleWeeklyEventDM(DiscordChannel Channel, DiscordMessage lastMessage)
	{
		if (!Channel.IsPrivate || _botState.WeeklyEventWinner == null || _botState.WeeklyEventWinner.Item1 == 0)
		{
			return;
		}

		if (lastMessage.Author.IsBot || Channel.Guild != null || lastMessage.CreationTimestamp <= _botState.WeeklyEventWinner.Item2)
		{
			return;
		}

		string vehiclesInString = await WGVehicle.vehiclesToString(_configuration["NLBEBOT:WarGamingAppId"], ["name"]);
		Json json = new(vehiclesInString, string.Empty);
		List<string> tanks = [.. json.subJsons[1].subJsons.Select(item => item.tupleList[0].Item2.Item1.Trim('"').Replace("\\", string.Empty))];

		string chosenTank = tanks.Find(tank => tank == lastMessage.Content);

		if (string.IsNullOrEmpty(chosenTank))
		{
			//specifieker vragen
			IEnumerable<string> containsStringList = tanks.Where(tank => tank.Contains(lastMessage.Content, StringComparison.OrdinalIgnoreCase));
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
			await _weeklyEventHandler.CreateNewWeeklyEvent(chosenTank, await _channelService.GetWeeklyEventChannel());
			_botState.WeeklyEventWinner = new Tuple<ulong, DateTime>(0, DateTime.Now);//dit vermijdt dat deze event telkens opnieuw zal opgeroepen worden + dat anderen het zomaar kunnen aanpassen
		}
	}
}
