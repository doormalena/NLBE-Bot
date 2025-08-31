namespace NLBE_Bot.EventHandlers;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using JsonObjectConverter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Tools.Replays;
using WorldOfTanksBlitzApi.Vehicles;

internal class MessageEventHandler(IOptions<BotOptions> options,
								   IBotState botState,
								   ILogger<MessageEventHandler> logger,
								   IChannelService channelService,
								   IUserService userService,
								   IDiscordMessageUtils discordMessageUtils,
								   IWeeklyEventService weeklyEventHandler,
								   IMapService mapService,
								   IReplayService replayService,
								   ITournamentService tournamentService,
								   IHallOfFameService hallOfFameService,
								   IMessageService messageService) : IMessageEventHandler
{
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
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

	public void Register(IDiscordClient client)
	{
		client.MessageCreated += OnMessageCreated;
		client.MessageDeleted += OnMessageDeleted;
		client.MessageReactionAdded += OnMessageReactionAdded;
		client.MessageReactionRemoved += OnMessageReactionRemoved;
	}

	private Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
	{
		return HandleMessageCreated(new DiscordClientWrapper(sender), new DiscordChannelWrapper(e.Channel), new DiscordMessageWrapper(e.Message), new DiscordUserWrapper(e.Author), new DiscordGuildWrapper(e.Guild));
	}

	internal async Task HandleMessageCreated(IDiscordClient client, IDiscordChannel channel, IDiscordMessage message, IDiscordUser author, IDiscordGuild guild)
	{
		if (_botState.IgnoreEvents)
		{
			return;
		}

		ICommandsNextExtension commandsNext = client.GetCommandsNext();

		if (commandsNext == null)
		{
			_logger.LogInformation("CommandsNext is not enabled.");
			return;
		}

		if (false && message.Content.StartsWith(Constants.Prefix))
		// VOOR IN HET GEVAL DAT OOIT JE COMMANDS MOET MAKEN OP BASIS VAN DE MESSAGE CREATED EVENT
		{
			// Remove the prefix and split the message into command and arguments
			string content = message.Content[Constants.Prefix.Length..];
			string[] parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length == 0)
			{
				return;
			}

			// Extract the command name and arguments
			string commandName = parts[0];
			string args = content.Substring(Constants.Prefix.Length, content.Length - Constants.Prefix.Length);



			Command command = commandsNext.FindCommand(commandName, out string rawArguments);
			if (command == null)
			{
				_logger.LogInformation("Unknown command.");
				return;
			}

			CommandContext ctx = commandsNext.CreateContext(message, Constants.Prefix, command, args);

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
		if (!author.IsBot && channel.Guild != null)
		{
			bool validChannel = false;
			IDiscordChannel masteryChannel = await _channelService.GetMasteryReplaysChannelAsync();

			if (masteryChannel != null && masteryChannel.Equals(channel))
			{
				validChannel = true;
			}

			if (!validChannel)
			{
				masteryChannel = await _channelService.GetBotTestChannelAsync();
				if (masteryChannel != null && masteryChannel.Equals(channel))
				{
					validChannel = true;
				}
				if (!validChannel)
				{
					masteryChannel = await _channelService.GetReplayResultsChannelAsync();
					if (masteryChannel != null && masteryChannel.Equals(channel))
					{
						validChannel = true;
					}
					if (!validChannel)
					{
						masteryChannel = await _channelService.GetReplayResultsChannelAsync();
						if (masteryChannel != null && masteryChannel.Equals(channel))
						{
							validChannel = true;
						}
						if (!validChannel)
						{
							masteryChannel = await _channelService.GetTestChannelAsync();
							if (masteryChannel != null && masteryChannel.Equals(channel))
							{
								validChannel = true;
							}
						}
					}
				}
			}
			if (validChannel)
			{
				_botState.LastCreatedDiscordMessage = message;
				IDiscordMember member = await guild.GetMemberAsync(author.Id);

				if ((member.Roles.Contains(guild.GetRole(Constants.NLBE_ROLE)) && (channel.Id.Equals(Constants.MASTERY_REPLAYS_ID))) ||
					(member.Roles.Contains(guild.GetRole(Constants.NLBE2_ROLE)) && (channel.Id.Equals(Constants.MASTERY_REPLAYS_ID))))
				{
					//MasteryChannel (komt wel in HOF)
					if (message.Attachments.Count > 0)
					{
						foreach (DiscordAttachment attachment in message.Attachments)
						{
							if (attachment.FileName.EndsWith(".wotbreplay"))
							{
								Tuple<string, IDiscordMessage> returnedTuple = await _hallOfFameService.Handle(string.Empty, attachment, channel, guild.Name, guild.Id, null, await guild.GetMemberAsync(author.Id));
								await _hallOfFameService.HofAfterUpload(returnedTuple, message);
								break;
							}
						}
					}
					else if (message.Content.StartsWith("http") && message.Content.Contains("wotinspector"))
					{
						string[] splitted = message.Content.Split(' ');
						string url = splitted[0];
						Tuple<string, IDiscordMessage> returnedTuple = await _hallOfFameService.Handle(string.Empty, null, channel, guild.Name, guild.Id, url, await guild.GetMemberAsync(author.Id));
						await _hallOfFameService.HofAfterUpload(returnedTuple, message);
					}
				}
				else
				{
					//ReplayResults die niet in HOF komen
					WGBattle replayInfo = new(string.Empty);
					bool wasReplay = false;

					if (message.Attachments.Count > 0)
					{
						foreach (DiscordAttachment attachment in from DiscordAttachment attachment in message.Attachments
																 where attachment.FileName.EndsWith(".wotbreplay")
																 select attachment)
						{
							await _messageService.ConfirmCommandExecuting(message);
							wasReplay = true;
							replayInfo = await _replayService.GetReplayInfo(string.Empty, attachment, _userService.GetWotbPlayerNameFromDisplayName(member.DisplayName).PlayerName, null);
						}
					}
					else if (message.Content.StartsWith("http") && message.Content.Contains("wotinspector"))
					{
						await _messageService.ConfirmCommandExecuting(message);
						wasReplay = true;
						replayInfo = await _replayService.GetReplayInfo(string.Empty, null, _userService.GetWotbPlayerNameFromDisplayName(member.DisplayName).PlayerName, message.Content);
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
							_logger.LogError(ex, "Error while getting weekly event description for replay.");
						}

						List<Tuple<string, string>> images = await _mapService.GetAllMaps(guild.Id);

						foreach (Tuple<string, string> map in from Tuple<string, string> map in images
															  where replayInfo.map_name.ToLower().Contains(map.Item1.ToLower())
															  select map)
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
								_logger.LogError(ex, "Could not set thumbnail for embed.");
								thumbnail = string.Empty;
							}
						}

						EmbedOptions embedOptions = new()
						{
							Thumbnail = thumbnail,
							Title = "Resultaat",
							Description = await _replayService.GetDescriptionForReplay(replayInfo, -1, eventDescription),
							IsForReplay = true,
						};
						await _messageService.CreateEmbed(channel, embedOptions);
						await _messageService.ConfirmCommandExecuted(message);
					}
					else if (wasReplay)
					{
						await message.DeleteReactionsEmojiAsync(_discordMessageUtils.GetDiscordEmoji(Constants.IN_PROGRESS_REACTION));
						await message.CreateReactionAsync(_discordMessageUtils.GetDiscordEmoji(Constants.ERROR_REACTION));
					}
				}
			}

			_botState.LastCreatedDiscordMessage = null;
		}
		else if (channel.IsPrivate)
		{
			await HandleWeeklyEventDM(channel, message);
		}
	}

	internal async Task OnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
	{
		if (_botState.IgnoreEvents)
		{
			return;
		}

		IDiscordChannel toernooiAanmeldenChannel = await _channelService.GetToernooiAanmeldenChannelAsync();
		if (e.Channel.Equals(toernooiAanmeldenChannel))
		{
			DateTime timeStamp = e.Message.Timestamp.LocalDateTime;
			IDiscordChannel logChannel = await _channelService.GetLogChannelAsync();
			if (logChannel != null)
			{
				IReadOnlyList<IDiscordMessage> messages = await logChannel.GetMessagesAsync(100);
				foreach (IDiscordMessage message in messages)
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

	private async Task OnMessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
	{
		await HandleMessageReactionAdded(new DiscordMessageWrapper(e.Message), new DiscordGuildWrapper(e.Guild), new DiscordChannelWrapper(e.Channel),
										 new DiscordUserWrapper(e.User), new DiscordEmojiWrapper(e.Emoji));
	}

	internal async Task HandleMessageReactionAdded(IDiscordMessage message, IDiscordGuild guild, IDiscordChannel channel, IDiscordUser user, IDiscordEmoji emoji)
	{
		if (_botState.IgnoreEvents)
		{
			return;
		}

		if (!user.IsBot && guild.Id == _options.ServerId)
		{
			IDiscordChannel toernooiAanmeldenChannel = await _channelService.GetToernooiAanmeldenChannelAsync();
			if (toernooiAanmeldenChannel != null && channel.Equals(toernooiAanmeldenChannel))
			{
				IDiscordMessage messageTmp = await toernooiAanmeldenChannel.GetMessageAsync(message.Id); // TODO: why not use message directly?
				await _tournamentService.GenerateLogMessage(messageTmp, toernooiAanmeldenChannel, user.Id, _discordMessageUtils.GetDiscordEmoji(emoji.Name).ToString());
			}
			else
			{
				IDiscordChannel regelsChannel = await _channelService.GetRegelsChannelAsync();
				if (regelsChannel != null && channel.Equals(regelsChannel))
				{
					string rulesReadEmoji = ":ok:";
					if (emoji.GetDiscordName().Equals(rulesReadEmoji))
					{
						IReadOnlyList<IDiscordUser> users = await message.GetReactionsAsync(emoji);

						foreach (IDiscordUser aUser in users)
						{
							if (!aUser.IsBot)
							{
								await message.DeleteReactionAsync(emoji, aUser);
								IDiscordMember member = await guild.GetMemberAsync(aUser.Id);
								if (member != null)
								{
									bool hadRulesNotReadrole = false;
									if (member.Roles != null)
									{
										foreach (IDiscordRole memberRole in from IDiscordRole memberRole in member.Roles
																			where memberRole.Id.Equals(_options.RoleIds.MustReadRules)
																			select memberRole)
										{
											hadRulesNotReadrole = true;
											await member.RevokeRoleAsync(memberRole);
										}
									}
									if (hadRulesNotReadrole || member.Roles == null || member.Roles.Any())
									{
										if (member.DisplayName.Split(']').Length > 1)
										{
											if (member.DisplayName.Split(']').Length > 2)
											{
												string[] splitted = member.DisplayName.Split(']');
												string tempName = splitted[^1].Trim(' ');
												await _userService.ChangeMemberNickname(member, "[] " + tempName);

											}
											else if (member.DisplayName.Contains("[NLBE]"))
											{
												await _userService.ChangeMemberNickname(member, "[] " + member.DisplayName.Replace("[NLBE]", string.Empty).Trim(' '));
											}
											else if (member.DisplayName.Contains("[NLBE2]"))
											{
												await _userService.ChangeMemberNickname(member, "[] " + member.DisplayName.Replace("[NLBE2]", string.Empty).Trim(' '));
											}
										}
										else
										{
											await _userService.ChangeMemberNickname(member, "[] " + member.Username);
										}
										IDiscordRole ledenRole = guild.GetRole(_options.RoleIds.Members);
										if (ledenRole != null)
										{
											await member.GrantRoleAsync(ledenRole);
										}
										IDiscordChannel algemeenChannel = await _channelService.GetAlgemeenChannelAsync();
										if (algemeenChannel != null)
										{
											await algemeenChannel.SendMessageAsync(user.Mention + " , welkom op de NLBE discord server. GLHF!");
										}
									}
								}
							}
						}
					}
					else
					{
						IReadOnlyList<IDiscordUser> users = await message.GetReactionsAsync(emoji);
						foreach (IDiscordUser u in users)
						{
							if (!u.IsBot)
							{
								await message.DeleteReactionAsync(emoji, u);
							}
						}
					}
				}
			}
		}
	}
	private Task OnMessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs e)
	{
		return HandleMessageReactionRemoved(new DiscordMessageWrapper(e.Message), new DiscordGuildWrapper(e.Guild), new DiscordChannelWrapper(e.Channel),
										 new DiscordUserWrapper(e.User), new DiscordEmojiWrapper(e.Emoji));
	}

	internal async Task HandleMessageReactionRemoved(IDiscordMessage message, IDiscordGuild guild, IDiscordChannel channel, IDiscordUser user, IDiscordEmoji emoji)
	{
		if (_botState.IgnoreEvents)
		{
			return;
		}

		if (guild.Id == _options.ServerId)
		{
			IDiscordChannel toernooiAanmeldenChannel = await _channelService.GetToernooiAanmeldenChannelAsync();
			if (toernooiAanmeldenChannel != null && channel.Equals(toernooiAanmeldenChannel))
			{
				bool removeInLog = true;
				IDiscordMessage messageTmp = await toernooiAanmeldenChannel.GetMessageAsync(message.Id); // TODO: why not use message directly?
				if (messageTmp.Author != null && !messageTmp.Author.Id.Equals(Constants.NLBE_BOT))
				{
					removeInLog = false;
				}
				if (removeInLog)
				{
					//check if reaction has to be added by bot (from 0 reactions to 1 reaction)
					IReadOnlyList<IDiscordUser> users = await message.GetReactionsAsync(emoji);
					if (users == null || users.Count == 0)
					{
						await message.CreateReactionAsync(emoji);
					}

					//remove in log
					IDiscordChannel logChannel = await _channelService.GetLogChannelAsync();

					if (logChannel.Inner != null)
					{
						IReadOnlyList<IDiscordMessage> logMessages = await logChannel.GetMessagesAsync(100);
						Dictionary<DateTime, List<IDiscordMessage>> sortedMessages = _discordMessageUtils.SortMessages(logMessages);
						foreach (KeyValuePair<DateTime, List<IDiscordMessage>> messageList in sortedMessages)
						{
							try
							{
								if (message.CreationTimestamp.LocalDateTime.CompareDateTime(messageList.Key))
								{
									foreach (IDiscordMessage aMessage in messageList.Value)
									{
										IDiscordMember member = await _userService.GetDiscordMember(guild, user.Id);
										if (member != null)
										{
											string[] splitted = aMessage.Content.Split(Constants.LOG_SPLIT_CHAR);
											string theEmoji = _discordMessageUtils.GetEmojiAsString(emoji.Name);
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
								_logger.LogError(ex, "Error while comparing timestamps in MessageReactionRemoved.");
							}
						}
					}
					else
					{
						_logger.LogError("Could not find log channel at MessageReactionRemoved.");
					}
				}
			}
		}
	}

	private async Task HandleWeeklyEventDM(IDiscordChannel channel, IDiscordMessage lastMessage)
	{
		if (!channel.IsPrivate || _botState.WeeklyEventWinner == null || _botState.WeeklyEventWinner.UserId == 0)
		{
			return;
		}

		if (lastMessage.Author.IsBot || channel.Guild != null || lastMessage.CreationTimestamp <= _botState.WeeklyEventWinner.LastEventDate)
		{
			return;
		}

		string vehiclesInString = await WGVehicle.vehiclesToString(_options.WotbApi.ApplicationId, ["name"]);
		Json json = new(vehiclesInString, string.Empty);
		List<string> tanks = [.. json.subJsons[1].subJsons.Select(item => item.tupleList[0].Item2.Item1.Trim('"').Replace("\\", string.Empty))];

		string chosenTank = tanks.Find(tank => tank == lastMessage.Content);

		if (string.IsNullOrEmpty(chosenTank))
		{
			//specifieker vragen
			IEnumerable<string> containsStringList = tanks.Where(tank => tank.Contains(lastMessage.Content, StringComparison.OrdinalIgnoreCase));
			if (containsStringList.Count() > 20)
			{
				await channel.SendMessageAsync("Wees iets specifieker want er werden te veel resultaten gevonden!");
			}
			else if (!containsStringList.Any())
			{
				await channel.SendMessageAsync("Die tank kon niet gevonden worden! Zoekterm: `" + lastMessage.Content + "`");
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
				await channel.SendMessageAsync("Deze tanks bevatten je zoekterm. **Kopieer** de naam van de tank en stuur hem naar mij door om zo de juiste te selecteren. (**Hoofdlettergevoelig**):");
				await channel.SendMessageAsync(sb.ToString());
			}
		}
		else
		{
			//tank was chosen
			await channel.SendMessageAsync("Je hebt de **" + chosenTank + "** geselecteerd. Goede keuze!\nIk zal hem onmiddelijk instellen als nieuwe tank voor het wekelijks event.");
			await _weeklyEventHandler.CreateNewWeeklyEvent(chosenTank, await _channelService.GetWeeklyEventChannelAsync());
			_botState.WeeklyEventWinner = null;//dit vermijdt dat deze event telkens opnieuw zal opgeroepen worden + dat anderen het zomaar kunnen aanpassen
		}
	}
}
