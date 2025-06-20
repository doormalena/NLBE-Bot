namespace NLBE_Bot.Services;

using DiscordHelper;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using FMWOTB;
using FMWOTB.Account;
using FMWOTB.Clans;
using FMWOTB.Exceptions;
using FMWOTB.Tools;
using FMWOTB.Tournament;
using JsonObjectConverter;
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

internal class Bot(DiscordClient discordClient, IServiceProvider serviceProvider, IErrorHandler errorHandler, ILogger<Bot> logger,
		IConfiguration configuration, ICommandHandler commandHandler, IWeeklyEventHandler weeklyEventHandler,
		IDiscordMessageUtils discordMessageUtils, IGuildMemberHandler guildMemberService, IBotState botState,
		IChannelService channelService, IGuildProvider guildProvider, IUserService userService, IMessageService messageService,
		IMessageHandler messageHandler) : IBot
{
	private readonly DiscordClient _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly ILogger<Bot> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	private readonly ICommandHandler _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
	private readonly IWeeklyEventHandler _weeklyEventHandler = weeklyEventHandler ?? throw new ArgumentNullException(nameof(weeklyEventHandler));
	private readonly IDiscordMessageUtils _discordMessageUtils = discordMessageUtils ?? throw new ArgumentNullException(nameof(discordMessageUtils));
	private readonly IGuildMemberHandler _guildMemberHandler = guildMemberService ?? throw new ArgumentNullException(nameof(guildMemberService));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IBotState _botState = botState ?? throw new ArgumentNullException(nameof(botState));
	private readonly IGuildProvider _guildProvider = guildProvider ?? throw new ArgumentNullException(nameof(guildProvider));
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
	private readonly IMessageHandler _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
	private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

	private DateTime? lasTimeNamesWereUpdated;
	private short heartBeatCounter = 0;

	public virtual async Task RunAsync()
	{
		// Register Bot commands.
		CommandsNextConfiguration commandsConfig = new()
		{
			StringPrefixes = [Constants.Prefix],
			EnableDms = false,
			EnableMentionPrefix = true,
			DmHelp = false,
			EnableDefaultHelp = false,
			Services = _serviceProvider
		};

		CommandsNextExtension commands = _discordClient.UseCommandsNext(commandsConfig);
		commands.RegisterCommands<BotCommands>();

		// Subscribe to events.
		commands.CommandExecuted += _commandHandler.OnCommandExecuted;
		commands.CommandErrored += _commandHandler.OnCommandErrored;

		_discordClient.Heartbeated += Discord_Heartbeated;
		_discordClient.Ready += Discord_Ready;

		_discordClient.GuildMemberAdded += _guildMemberHandler.OnMemberAdded;
		_discordClient.GuildMemberUpdated += _guildMemberHandler.OnMemberUpdated;
		_discordClient.GuildMemberRemoved += _guildMemberHandler.OnMemberRemoved;

		_discordClient.MessageCreated += _messageHandler.OnMessageCreated;
		_discordClient.MessageDeleted += _messageHandler.OnMessageDeleted;
		_discordClient.MessageReactionAdded += _messageHandler.OnMessageReactionAdded;
		_discordClient.MessageReactionRemoved += _messageHandler.OnMessageReactionRemoved;

		DiscordActivity activity = new(Constants.Prefix, ActivityType.ListeningTo);
		await _discordClient.ConnectAsync(activity, UserStatus.Online);

		await Task.Delay(-1);
	}

	#region Events

	private async Task Discord_Heartbeated(DiscordClient sender, HeartbeatEventArgs e)
	{
		if (_botState.IgnoreEvents)
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
					await _userService.UpdateUsers();
				}
				catch (InternalServerErrorException ex)
				{
					string message = "\nERROR updating users:\nInternal server exception from api request\n" + ex.Message;
					await _messageService.SendThibeastmo(message, string.Empty, string.Empty);
					DiscordChannel bottestChannel = await _channelService.GetBottestChannel();
					await bottestChannel.SendMessageAsync(message);
				}
				catch (Exception ex)
				{
					string message = "\nERROR updating users:\n" + ex.Message;
					await _messageService.SendThibeastmo(message, string.Empty, string.Empty);
					DiscordChannel bottestChannel = await _channelService.GetBottestChannel();
					await bottestChannel.SendMessageAsync(message);
				}
			}
		}
		else if (DateTime.Now.DayOfWeek == dayToCheck && DateTime.Now.Hour == hourToCheck && heartBeatCounter == 2)//14u omdat wotb ook wekelijks op maandag 14u restart
		{
			//We have a weekly winner
			string winnerMessage = "Het wekelijkse event is afgelopen.";
			DiscordChannel bottestChannel = await _channelService.GetBottestChannel();
			try
			{
				_discordClient.Logger.LogInformation(winnerMessage);

				await _weeklyEventHandler.ReadWeeklyEvent();
				if (_weeklyEventHandler.WeeklyEvent.StartDate.DayOfYear == DateTime.Now.DayOfYear - 7)//-7 omdat het dan zeker een nieuwe week is maar niet van twee weken geleden
				{
					winnerMessage += "\nNa 1 week...";
					WeeklyEventItem weeklyEventItemMostDMG = _weeklyEventHandler.WeeklyEvent.WeeklyEventItems.Find(weeklyEventItem => weeklyEventItem.WeeklyEventType == WeeklyEventType.Most_damage);
					if (weeklyEventItemMostDMG.Player != null && weeklyEventItemMostDMG.Player.Length > 0)
					{
						foreach (KeyValuePair<ulong, DiscordGuild> guild in _guildProvider.Guilds)
						{
							if (guild.Key is Constants.NLBE_SERVER_ID or Constants.DA_BOIS_ID)
							{
								await WeHaveAWinner(guild.Value, weeklyEventItemMostDMG, _weeklyEventHandler.WeeklyEvent.Tank);
								break;
							}
						}
					}
				}
				await bottestChannel.SendMessageAsync(winnerMessage);
				await _messageService.SendThibeastmo(winnerMessage, string.Empty, string.Empty);
			}
			catch (Exception ex)
			{
				string message = winnerMessage + "\nERROR:\n" + ex.Message;
				await bottestChannel.SendMessageAsync(message);
				await _messageService.SendThibeastmo(message, string.Empty, string.Empty);
			}
		}
	}
	public async Task WeHaveAWinner(DiscordGuild guild, WeeklyEventItem weeklyEventItemMostDMG, string tank)
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
					Tuple<string, string> gebruiker = _userService.GetIGNFromMember(member.DisplayName);
					string x = gebruiker.Item2
						.Replace("\\", string.Empty)
						.Replace(Constants.UNDERSCORE_REPLACEMENT_CHAR, '_')
						.ToLower();
					if (x == weeklyEventItemMostDMGPlayer
						|| (member.Id == Constants.THIBEASTMO_ID
							&& guild.Id == Constants.DA_BOIS_ID))
					{
						userNotFound = false;

						_botState.WeeklyEventWinner = new Tuple<ulong, DateTime>(member.Id, DateTime.Now);

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
							await _errorHandler.HandleErrorAsync("Could not send private message towards winner of weekly event.", ex);
						}
						try
						{
							DiscordChannel algemeenChannel = await _channelService.GetAlgemeenChannel();
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
							await _errorHandler.HandleErrorAsync("Could not send message in algemeen channel for weekly event winner announcement.", ex);
						}
						break;
					}
				}
			}
		}
		else
		{
			DiscordChannel algemeenChannel = await _channelService.GetAlgemeenChannel();
			if (algemeenChannel != null)
			{
				await algemeenChannel.SendMessageAsync("Het wekelijkse event is gedaan, helaas heeft er __niemand__ deelgenomen en is er dus geen winnaar.");
			}
		}
		DiscordChannel bottestChannel = await _channelService.GetBottestChannel();
		if (userNotFound)
		{
			string message = "Weekly event winnaar was niet gevonden! Je zal het zelf moeten regelen met het `weekly` commando.";
			if (bottestChannel != null)
			{
				await bottestChannel.SendMessageAsync(message);
			}
			else
			{
				await _errorHandler.HandleErrorAsync(message);
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
				await _errorHandler.HandleErrorAsync(message);
			}
		}
	}

	private Task Discord_Ready(DiscordClient sender, ReadyEventArgs e)
	{
		foreach (KeyValuePair<ulong, DiscordGuild> guild in _guildProvider.Guilds)
		{
			if (!guild.Key.Equals(Constants.NLBE_SERVER_ID) && !guild.Key.Equals(Constants.DA_BOIS_ID))
			{
				guild.Value.LeaveAsync();
			}
		}
		_discordClient.Logger.Log(LogLevel.Information, "Client (v{Version}) is ready to process events.", Constants.version);

		return Task.CompletedTask;
	}
	#endregion

	public async Task<List<Tier>> ReadTeams(DiscordChannel channel, DiscordMember member, string guildName, string[] parameters_as_in_hoeveelste_team)
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
					DiscordChannel toernooiAanmeldenChannel = await _channelService.GetToernooiAanmeldenChannel(channel.Guild.Id);
					if (toernooiAanmeldenChannel != null)
					{
						List<DiscordMessage> messages = [];
						try
						{
							IReadOnlyList<DiscordMessage> xMessages = toernooiAanmeldenChannel.GetMessagesAsync(hoeveelste + 1).Result;
							foreach (DiscordMessage message in xMessages)
							{
								messages.Add(message);
							}
						}
						catch (Exception ex)
						{
							await _errorHandler.HandleErrorAsync("Could not load messages from " + toernooiAanmeldenChannel.Name + ":", ex);
						}
						if (messages.Count == hoeveelste + 1)
						{
							DiscordMessage theMessage = messages[hoeveelste];
							if (theMessage != null)
							{
								if (theMessage.Author.Id.Equals(Constants.NLBE_BOT) || theMessage.Author.Id.Equals(Constants.TESTBEASTV2_BOT))
								{
									IDiscordChannel logChannel = new DiscordChannelWrapper(await _channelService.GetLogChannel(channel.Guild.Id));

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
										await _errorHandler.HandleErrorAsync("Could not find log channel!");
									}
								}
								else
								{
									IDiscordMessage message = new DiscordMessageWrapper(theMessage);
									Dictionary<IDiscordEmoji, List<IDiscordUser>> reactions = _discordMessageUtils.SortReactions(message);

									List<Tier> teams = [];
									foreach (KeyValuePair<IDiscordEmoji, List<IDiscordUser>> reaction in reactions)
									{
										Tier aTeam = new();
										foreach (IDiscordUser user in reaction.Value)
										{
											string displayName = user.Inner.Username;
											DiscordMember memberx = toernooiAanmeldenChannel.Guild.GetMemberAsync(user.Inner.Id).Result;
											if (memberx != null)
											{
												displayName = memberx.DisplayName;
											}
											aTeam.AddDeelnemer(displayName, user.Inner.Id);
										}
										if (aTeam.Organisator.Equals(string.Empty))
										{
											foreach (KeyValuePair<ulong, DiscordGuild> aGuild in _guildProvider.Guilds)
											{
												if (aGuild.Key.Equals(Constants.NLBE_SERVER_ID))
												{
													DiscordMember theMemberAuthor = await _userService.GetDiscordMember(aGuild.Value, theMessage.Author.Id);
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
	public async Task<List<Tuple<ulong, string>>> GetIndividualParticipants(List<Tier> teams, DiscordGuild guild)
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

						temp = string.IsNullOrEmpty(temp) ? participant.Item2 : RemoveSyntax(participant.Item2);
						bool alreadyInList = false;
						foreach (Tuple<ulong, string> participantX in participants)
						{
							if ((participantX.Item1.Equals(participant.Item1) && participant.Item1 > 0) || participantX.Item2.Equals(RemoveSyntax(participant.Item2)))
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
	private static string RemoveSyntax(string stringItem)
	{
		stringItem = stringItem.Replace("\\", string.Empty);

		if (stringItem.StartsWith("**") && stringItem.EndsWith("**"))
		{
			return stringItem.Trim('*');
		}

		if (stringItem.StartsWith('`') && stringItem.EndsWith('`'))
		{
			return stringItem.Trim('`');
		}

		return stringItem;
	}

	public List<Tuple<ulong, string>> RemoveSyntaxes(List<Tuple<ulong, string>> stringList)
	{
		return stringList.Select(item => Tuple.Create(item.Item1, RemoveSyntax(item.Item2))).ToList();
	}

	public async Task<List<string>> GetMentions(List<Tuple<ulong, string>> memberList, ulong guildID)
	{
		DiscordGuild guild = await _guildProvider.GetGuild(guildID);
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

	public DiscordRole GetDiscordRole(ulong serverID, ulong id)
	{
		foreach (KeyValuePair<ulong, DiscordGuild> guild in _guildProvider.Guilds)
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

	public bool HasRight(DiscordMember member, Command command)
	{
		if (member.Guild.Id.Equals(Constants.DA_BOIS_ID))
		{
			return true;
		}

		if (!member.Guild.Id.Equals(Constants.NLBE_SERVER_ID))
		{
			return false;
		}

		return command.Name.ToLower() switch
		{
			"help" or "map" or "gebruiker" or "gebruikerslijst" or "clan" or "clanmembers" or "spelerinfo" => true,
			"toernooi" or "toernooien" => HasAnyRole(member, Constants.TOERNOOI_DIRECTIE),
			"teams" => HasAnyRole(member, Constants.NLBE_ROLE, Constants.NLBE2_ROLE, Constants.DISCORD_ADMIN_ROLE, Constants.DEPUTY_ROLE, Constants.BEHEERDER_ROLE, Constants.TOERNOOI_DIRECTIE),
			"tagteams" => HasAnyRole(member, Constants.DISCORD_ADMIN_ROLE, Constants.BEHEERDER_ROLE, Constants.TOERNOOI_DIRECTIE),
			"hof" or "hofplayer" => HasAnyRole(member, Constants.NLBE_ROLE, Constants.NLBE2_ROLE),
			"resethof" or "weekly" or "updategebruikers" => HasAnyRole(member, Constants.BEHEERDER_ROLE, Constants.DISCORD_ADMIN_ROLE),
			"removeplayerhof" or "renameplayerhof" => HasAnyRole(member, Constants.DISCORD_ADMIN_ROLE, Constants.DEPUTY_ROLE),
			"poll" => member.Id.Equals(414421187888676875) || HasAnyRole(member, Constants.DISCORD_ADMIN_ROLE, Constants.DEPUTY_ROLE, Constants.BEHEERDER_ROLE, Constants.TOERNOOI_DIRECTIE),
			"deputypoll" => HasAnyRole(member, Constants.DEPUTY_ROLE, Constants.DEPUTY_NLBE_ROLE, Constants.DEPUTY_NLBE2_ROLE, Constants.DISCORD_ADMIN_ROLE),
			_ => HasAnyRole(member, Constants.DISCORD_ADMIN_ROLE, Constants.DEPUTY_ROLE, Constants.BEHEERDER_ROLE, Constants.TOERNOOI_DIRECTIE),
		};
	}

	private static bool HasAnyRole(DiscordMember member, params ulong[] roleIds)
	{
		return member.Roles.Any(role => roleIds.Contains(role.Id));
	}

	public async Task ShowMemberInfo(DiscordChannel channel, object gebruiker)
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
			await _messageService.CreateEmbed(channel, options);
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
							Value = member.statistics.rating.battles > 0 ? string.Format("{0:.##}", CalculateWinRate(member.statistics.rating.wins, member.statistics.rating.battles)) : "Nog geen rating gespeeld",
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

				await _messageService.CreateEmbed(channel, options);
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
			await _messageService.CreateEmbed(channel, options);
		}
	}

	private static double CalculateWinRate(int wins, int battles)
	{
		return wins / battles * 100;
	}

	public async Task ShowClanInfo(DiscordChannel channel, WGClan clan)
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
		await _messageService.CreateEmbed(channel, options);
	}

	public async Task<WGClan> SearchForClan(DiscordChannel channel, DiscordMember member, string guildName, string clan_naam, bool loadMembers, DiscordUser user, Command command)
	{
		try
		{
			IReadOnlyList<WGClan> clans = await WGClan.searchByName(SearchAccuracy.STARTS_WITH_CASE_INSENSITIVE, clan_naam, _configuration["NLBEBOT:WarGamingAppId"], loadMembers);
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
					sbFound.AppendLine(i + 1 + ". `" + clanList[i].tag + "`");
				}
				if (sbFound.Length < 1024)
				{
					int index = await _messageService.WaitForReply(channel, user, clan_naam, clanList.Count);
					if (index >= 0)
					{
						return clanList[index];
					}
				}
				else
				{
					await _messageService.SayBeMoreSpecific(channel);
				}
			}
			else if (clanList.Count == 1)
			{
				return clanList[0];
			}
			else if (clanList.Count == 0)
			{
				await _messageService.SendMessage(channel, member, guildName, "**Clan(" + clan_naam + ") is niet gevonden! (In een lijst van " + aantalClans + " clans)**");
			}
		}
		catch (TooManyResultsException ex)
		{
			_discordClient.Logger.LogWarning("({Command}) {Message}", command.Name, ex.Message);
			await _messageService.SendMessage(channel, member, guildName, "**Te veel resultaten waren gevonden, wees specifieker!**");
		}
		return null;
	}

	public List<DEF> ListInMemberEmbed(int columns, List<DiscordMember> memberList, string searchTerm)
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

					if (counter == membersPerColumn + (columnCounter == columns - 1 ? rest : 0) || memberList.Count == 1)
					{
						if (columnCounter < columns - 1)
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
					_errorHandler.HandleErrorAsync("Error in gebruikerslijst:", ex).Wait();
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
					string firstChar = RemoveSyntax(splitted[0]).Substring(0, 1);
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

	public List<DEF> ListInPlayerEmbed(int columns, List<Members> memberList, string searchTerm, DiscordGuild guild)
	{
		if (memberList.Count == 0)
		{
			return [];
		}

		List<string> nameList;

		if (searchTerm.Contains('d'))
		{
			List<WGAccount> wgAccountList = memberList.Select(member => new WGAccount(_configuration["NLBEBOT:WarGamingAppId"], member.account_id, false, false, false))
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

				if (counter == membersPerColumn + (columnCounter == columns - 1 ? rest : 0) || nameList.Count == 1)
				{
					if (columnCounter < columns - 1)
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
				_errorHandler.HandleErrorAsync("Error in listInPlayerEmbed:", ex).Wait();
			}
		}

		List<DEF> deflist = [];
		bool firstTime = true;
		foreach (StringBuilder item in sbs)
		{
			if (item.Length > 0)
			{
				string[] splitted = item.ToString().Split(Environment.NewLine);
				string firstChar = RemoveSyntax(splitted[0]).Substring(0, 1);
				string lastChar = string.Empty;
				for (int i = splitted.Length - 1; i > 0; i--)
				{
					if (splitted[i] != string.Empty)
					{
						lastChar = RemoveSyntax(splitted[i]).ToUpper().First().ToString();
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
					defName = firstChar.ToUpper() + (splitted.Length > 2 ? " - " + lastChar.ToUpper() : "");
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

	public List<string> GetSearchTermAndCondition(params string[] parameter)
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

	public async Task<WGAccount> SearchPlayer(DiscordChannel channel, DiscordMember member, DiscordUser user, string guildName, string naam)
	{
		try
		{
			IReadOnlyList<WGAccount> searchResults = await WGAccount.searchByName(SearchAccuracy.STARTS_WITH_CASE_INSENSITIVE, naam, _configuration["NLBEBOT:WarGamingAppId"], false, false, true);
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
					index = await _messageService.WaitForReply(channel, user, sb.ToString(), searchResults.Count);
				}
				if (index >= 0 && searchResults.Count >= 1)
				{
					WGAccount account = new(_configuration["NLBEBOT:WarGamingAppId"], searchResults[index].account_id, false, true, true);
					await ShowMemberInfo(channel, account);
					return account;
				}
				else
				{
					await _messageService.SendMessage(channel, member, guildName, "**Gebruiker (**`" + naam + "`**) kon niet gevonden worden!**");
				}
			}
			else
			{
				await _messageService.SendMessage(channel, member, guildName, "**Gebruiker (**`" + naam.adaptToDiscordChat() + "`**) kon niet gevonden worden!**");
			}
		}
		catch (TooManyResultsException ex)
		{
			_discordClient.Logger.LogWarning("While searching for player by name: {Message}", ex.Message);
			await _messageService.SendMessage(channel, member, guildName, "**Te veel resultaten waren gevonden, wees specifieker!**");
		}
		return null;
	}
}
