namespace NLBE_Bot.EventHandlers;

using DSharpPlus;
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Tools.Replays;
using WorldOfTanksBlitzApi.Vehicles;

internal class MessageEventHandler(IOptions<BotOptions> options,
								   ILogger<MessageEventHandler> logger,
								   IChannelService channelService,
								   IUserService userService,
								   IDiscordMessageUtils discordMessageUtils,
								   IWeeklyEventService weeklyEventService,
								   IMapService mapService,
								   IReplayService replayService,
								   ITournamentService tournamentService,
								   IHallOfFameService hallOfFameService,
								   IMessageService messageService) : EventHandlerBase(options), IMessageEventHandler
{
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly IDiscordMessageUtils _discordMessageUtils = discordMessageUtils ?? throw new ArgumentNullException(nameof(discordMessageUtils));
	private readonly IWeeklyEventService _weeklyEventService = weeklyEventService ?? throw new ArgumentNullException(nameof(weeklyEventService));
	private readonly IMapService _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
	private readonly IReplayService _replayService = replayService ?? throw new ArgumentNullException(nameof(replayService));
	private readonly ITournamentService _tournamentService = tournamentService ?? throw new ArgumentNullException(nameof(tournamentService));
	private readonly IHallOfFameService _hallOfFameService = hallOfFameService ?? throw new ArgumentNullException(nameof(hallOfFameService));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
	private readonly ILogger<MessageEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public override void Register(IDiscordClient client, IBotState botState)
	{
		_botState = botState ?? throw new ArgumentNullException(nameof(botState));

		client.MessageCreated += OnMessageCreated;
		client.MessageDeleted += OnMessageDeleted;
		client.MessageReactionAdded += OnMessageReactionAdded;
		client.MessageReactionRemoved += OnMessageReactionRemoved;
	}

	[ExcludeFromCodeCoverage(Justification = "Not testable due to DSharpPlus limitations.")]
	private Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
	{
		return HandleMessageCreated(new DiscordGuildWrapper(e.Guild), new DiscordChannelWrapper(e.Channel),
									new DiscordMessageWrapper(e.Message), new DiscordUserWrapper(e.Author));
	}

	[ExcludeFromCodeCoverage(Justification = "Not testable due to DSharpPlus limitations.")]
	private Task OnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
	{
		return HandleMessageDeleted(new DiscordMessageWrapper(e.Message), new DiscordGuildWrapper(e.Guild), new DiscordChannelWrapper(e.Channel));
	}

	[ExcludeFromCodeCoverage(Justification = "Not testable due to DSharpPlus limitations.")]
	private async Task OnMessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
	{
		await HandleMessageReactionAdded(new DiscordMessageWrapper(e.Message), new DiscordGuildWrapper(e.Guild), new DiscordChannelWrapper(e.Channel),
										 new DiscordUserWrapper(e.User), new DiscordEmojiWrapper(e.Emoji));
	}

	[ExcludeFromCodeCoverage(Justification = "Not testable due to DSharpPlus limitations.")]
	private Task OnMessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs e)
	{
		return HandleMessageReactionRemoved(new DiscordMessageWrapper(e.Message), new DiscordGuildWrapper(e.Guild), new DiscordChannelWrapper(e.Channel),
											new DiscordUserWrapper(e.User), new DiscordEmojiWrapper(e.Emoji));
	}

	internal async Task HandleMessageCreated(IDiscordGuild guild, IDiscordChannel channel, IDiscordMessage message, IDiscordUser author)
	{
		await ExecuteIfAllowedAsync(guild, async () =>
		{
			if (Guard.ReturnIfNull(await _channelService.GetWeeklyEventChannelAsync(), _logger, "Weekly Event channel", out IDiscordChannel weeklyEventChannel) ||
				Guard.ReturnIfNull(await _channelService.GetMasteryReplaysChannelAsync(), _logger, "Mastery Replays channel", out IDiscordChannel masteryChannel) ||
				Guard.ReturnIfNull(await _channelService.GetReplayResultsChannelAsync(), _logger, "Replay Results channel", out IDiscordChannel replayChannel) ||
				Guard.ReturnIfNull(guild.GetChannel(_options.ChannelIds.BotTest), _logger, "Bot Test channel", out IDiscordChannel botTestChannel))
			{
				return;
			}

			if (!author.IsBot)
			{
				List<ulong> allowedChannelIds =
				[
					masteryChannel.Id,
					replayChannel.Id,
					botTestChannel.Id
				];

				if (allowedChannelIds.Contains(channel.Id))
				{
					_botState!.LastCreatedDiscordMessage = message;
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
									Tuple<string, IDiscordMessage> returnedTuple = await _hallOfFameService.Handle(string.Empty, attachment, channel, guild, string.Empty, await guild.GetMemberAsync(author.Id));
									await _hallOfFameService.HofAfterUpload(returnedTuple, message);
									break;
								}
							}
						}
						else if (message.Content.StartsWith("http") && message.Content.Contains("wotinspector"))
						{
							string[] splitted = message.Content.Split(' ');
							string url = splitted[0];
							Tuple<string, IDiscordMessage> returnedTuple = await _hallOfFameService.Handle(string.Empty, string.Empty, channel, guild, url, await guild.GetMemberAsync(author.Id));
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
								replayInfo = await _replayService.GetReplayInfo(string.Empty, attachment, _userService.GetWotbPlayerNameFromDisplayName(member.DisplayName).PlayerName, null!);
							}
						}
						else if (message.Content.StartsWith("http") && message.Content.Contains("wotinspector"))
						{
							await _messageService.ConfirmCommandExecuting(message);
							wasReplay = true;
							replayInfo = await _replayService.GetReplayInfo(string.Empty, string.Empty, _userService.GetWotbPlayerNameFromDisplayName(member.DisplayName).PlayerName, message.Content);
						}

						if (wasReplay && replayInfo != null)
						{
							string thumbnail = string.Empty;
							string eventDescription = string.Empty;
							try
							{
								eventDescription = await _weeklyEventService.GetStringForWeeklyEvent(replayInfo);
							}
							catch (Exception ex)
							{
								_logger.LogError(ex, "Error while getting weekly event description for replay.");
							}

							List<Tuple<string, string>> images = await _mapService.GetAllMaps(guild.Id);

							foreach (Tuple<string, string> map in from Tuple<string, string> map in images
																  where replayInfo.map_name.Contains(map.Item1, StringComparison.OrdinalIgnoreCase)
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
							IDiscordEmoji inProgressEmoji = _discordMessageUtils.GetDiscordEmoji(Constants.IN_PROGRESS_REACTION)!;
							IDiscordEmoji errorEmoji = _discordMessageUtils.GetDiscordEmoji(Constants.ERROR_REACTION)!;

							await message.DeleteReactionsEmojiAsync(inProgressEmoji);
							await message.CreateReactionAsync(errorEmoji);
						}
					}
				}

				_botState!.LastCreatedDiscordMessage = null;
			}
			else if (channel.IsPrivate)
			{
				await HandleWeeklyEventDM(channel, message, weeklyEventChannel);
			}
		});
	}

	internal async Task HandleMessageDeleted(IDiscordMessage message, IDiscordGuild guild, IDiscordChannel channel)
	{
		await ExecuteIfAllowedAsync(guild, async () =>
		{
			if (Guard.ReturnIfNull(await _channelService.GetToernooiAanmeldenChannelAsync(), _logger, "Toernooi Aanmelden channel", out IDiscordChannel toernooiAanmeldenChannel) ||
				Guard.ReturnIfNull(await _channelService.GetLogChannelAsync(), _logger, "Log channel", out IDiscordChannel logChannel))
			{
				return;
			}

			if (channel.Id != toernooiAanmeldenChannel.Id)
			{
				return;
			}

			// TODO: what is this even doing?

			DateTime timeStamp = message.Timestamp.LocalDateTime;
			IReadOnlyList<IDiscordMessage> messages = await logChannel.GetMessagesAsync(100); // TODO: why this specific number?

			foreach (IDiscordMessage messageTmp in messages)
			{
				string[] splitted = messageTmp.Content.Split('|');

				if (DateTime.TryParse(splitted[0], new CultureInfo("nl-NL"), out DateTime tempDateTime) && tempDateTime.CompareDateTime(timeStamp))
				{
					await messageTmp.DeleteAsync();
					await Task.Delay(875); // TODO: why this arbitrary delay?
				}
			}
		});
	}

	internal async Task HandleMessageReactionAdded(IDiscordMessage message, IDiscordGuild guild, IDiscordChannel channel, IDiscordUser user, IDiscordEmoji emoji)
	{
		await ExecuteIfAllowedAsync(guild, async () =>
		{
			if (user.IsBot ||
				Guard.ReturnIfNull(await _channelService.GetToernooiAanmeldenChannelAsync(), _logger, "Toernooi Aanmelden channel", out IDiscordChannel toernooiAanmeldenChannel) ||
				Guard.ReturnIfNull(guild.GetChannel(_options.ChannelIds.Rules), _logger, "Rules channel", out IDiscordChannel rulesChannel) ||
				Guard.ReturnIfNull(await _channelService.GetAlgemeenChannelAsync(), _logger, "Algemeen channel", out IDiscordChannel algemeenChannel) ||
				Guard.ReturnIfNull(guild.GetRole(_options.RoleIds.Members), _logger, "Members rol", out IDiscordRole membersRole))
			{
				return;
			}

			if (channel.Id == toernooiAanmeldenChannel.Id)
			{
				IDiscordMessage messageTmp = await toernooiAanmeldenChannel.GetMessageAsync(message.Id); // TODO: why not use message directly?
				await _tournamentService.GenerateLogMessage(messageTmp, toernooiAanmeldenChannel, user.Id, _discordMessageUtils.GetDiscordEmoji(emoji!.Name)?.ToString() ?? string.Empty);
				return;
			}

			if (channel.Id == rulesChannel.Id)
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

									await member.GrantRoleAsync(membersRole);
									await algemeenChannel.SendMessageAsync(user.Mention + " , welkom op de NLBE discord server. GLHF!");
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
		});
	}

	internal async Task HandleMessageReactionRemoved(IDiscordMessage message, IDiscordGuild guild, IDiscordChannel channel, IDiscordUser user, IDiscordEmoji emoji)
	{
		await ExecuteIfAllowedAsync(guild, async () =>
		{
			if (Guard.ReturnIfNull(await _channelService.GetToernooiAanmeldenChannelAsync(), _logger, "Toernooi Aanmelden channel", out IDiscordChannel toernooiAanmeldenChannel) ||
				Guard.ReturnIfNull(await _channelService.GetLogChannelAsync(), _logger, "Log channel", out IDiscordChannel logChannel))
			{
				return;
			}

			if (channel.Id != toernooiAanmeldenChannel.Id)
			{
				return;
			}

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
		});
	}

	private async Task HandleWeeklyEventDM(IDiscordChannel channel, IDiscordMessage lastMessage, IDiscordChannel weeklyEventChannel)
	{
		if (!channel.IsPrivate || _botState!.WeeklyEventWinner == null || _botState!.WeeklyEventWinner.UserId == 0)
		{
			return;
		}

		if (lastMessage.Author.IsBot || lastMessage.CreationTimestamp <= _botState.WeeklyEventWinner.LastEventDate)
		{
			return;
		}

		string vehiclesInString = await WGVehicle.vehiclesToString(_options.WotbApi.ApplicationId, ["name"]);
		Json json = new(vehiclesInString, string.Empty);
		List<string> tanks = [.. json.subJsons[1].subJsons.Select(item => item.tupleList[0].Item2.Item1.Trim('"').Replace("\\", string.Empty))];

		string? chosenTank = tanks.Find(tank => tank == lastMessage.Content);

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
			await _weeklyEventService.CreateNewWeeklyEvent(chosenTank, weeklyEventChannel);
			_botState.WeeklyEventWinner = null;//dit vermijdt dat deze event telkens opnieuw zal opgeroepen worden + dat anderen het zomaar kunnen aanpassen
		}
	}
}
