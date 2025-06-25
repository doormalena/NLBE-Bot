namespace NLBE_Bot.Services;
using DiscordHelper;
using DSharpPlus;
using DSharpPlus.Entities;
using FMWOTB;
using FMWOTB.Tools.Replays;
using Microsoft.Extensions.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class HallOfFameService(IErrorHandler errorHandler, IConfiguration configuration,
		IDiscordMessageUtils discordMessageUtils, IChannelService channelService, IMessageService messageService, IMapService mapService, IReplayService replayService, IUserService userService) : IHallOfFameService
{
	private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly IDiscordMessageUtils _discordMessageUtils = discordMessageUtils ?? throw new ArgumentNullException(nameof(discordMessageUtils));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
	private readonly IMapService _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
	private readonly IReplayService _replayService = replayService ?? throw new ArgumentNullException(nameof(replayService));
	private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
	public async Task<Tuple<string, IDiscordMessage>> Handle(string titel, object discAttach, IDiscordChannel channel, string guildName, ulong guildID, string iets, IDiscordMember member)
	{
		if (discAttach is DiscordAttachment attachment)
		{
			discAttach = attachment;
		}
		WGBattle replayInfo = await _replayService.GetReplayInfo(titel, discAttach, _userService.GetIGNFromMember(member.DisplayName).Item2, iets);
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
					IDiscordChannel goodChannel = await _channelService.GetMasteryReplaysChannel(guildID);
					if (goodChannel != null && goodChannel.Id.Equals(channel.Id))
					{
						validChannel = true;
					}
					if (!validChannel)
					{
						goodChannel = await _channelService.GetBottestChannel();
						if (goodChannel.Id.Equals(channel.Id))
						{
							validChannel = true;
						}
					}
				}
				return validChannel
					? await GoHOFDetails(replayInfo, channel, member, guildName, guildID)
					: new Tuple<string, IDiscordMessage>("Kanaal is niet geschikt voor HOF.", null);
			}
			else
			{
				return new Tuple<string, IDiscordMessage>("Replayobject was null.", null);
			}
		}
		catch
		{
			return new Tuple<string, IDiscordMessage>("Er ging iets mis.", null);
		}
	}

	public async Task<Tuple<string, IDiscordMessage>> GoHOFDetails(WGBattle replayInfo, IDiscordChannel channel, IDiscordMember member, string guildName, ulong guildID)
	{
		_ = (await channel.GetMessagesAsync(1))[0];
		IDiscordMessage tempMessage;

		if (replayInfo.battle_type is 0 or 1) // 0 = encounter, 1 = supremacy
		{
			if (replayInfo.room_type is 1 or 4 or 5 or 7) // 1 = normal, 4 = tournament, 5 = tournament, 7 = rating 
			{
				try
				{
					return replayInfo.details != null
						? await ReplayHOF(replayInfo, guildID, channel, member, guildName)
						: new Tuple<string, IDiscordMessage>("Replay bevatte geen details.", null);
				}
				catch (JsonNotFoundException ex)
				{
					_ = await _messageService.SaySomethingWentWrong(channel, member, guildName, "**Er ging iets mis tijdens het inlezen van de gegevens!**");
					await _errorHandler.HandleErrorAsync("While reading json from a replay:\n", ex);
				}
				catch (Exception ex)
				{
					_ = await _messageService.SaySomethingWentWrong(channel, member, guildName, "**Er ging iets mis bij het controleren van de HOF!**");
					await _errorHandler.HandleErrorAsync("While checking HOF with a replay:\n", ex);
				}
				tempMessage = await _messageService.SendMessage(channel, member, guildName, "**Dit is een speciale replay waardoor de gegevens niet fatsoenlijk ingelezen konden worden!**");
				return new Tuple<string, IDiscordMessage>(tempMessage.Content, tempMessage);
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

				tempMessage = await _messageService.SayCannotBePlayedAt(channel, member, guildName, roomTypeName);
			}
		}
		else
		{
			tempMessage = await _messageService.SaySomethingWentWrong(channel, member, guildName, "**Je mag enkel de standaardbattles gebruiken! (Geen speciale gamemodes)**");
		}
		string thumbnail = string.Empty;
		List<Tuple<string, string>> images = await _mapService.GetAllMaps(channel.Guild.Id);
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
					await _errorHandler.HandleErrorAsync("Could not set thumbnail for embed:", ex);
				}
				break;
			}
		}

		EmbedOptions options = new()
		{
			Thumbnail = thumbnail,
			Title = "Resultaat",
			Description = await _replayService.GetDescriptionForReplay(replayInfo, -1),
		};
		await _messageService.CreateEmbed(channel, options);
		return new Tuple<string, IDiscordMessage>(tempMessage.Content, tempMessage);
	}
	public async Task<Tuple<string, IDiscordMessage>> ReplayHOF(WGBattle battle, ulong guildID, IDiscordChannel channel, IDiscordMember member, string guildName)
	{
		if (battle.details.clanid.Equals(Constants.NLBE_CLAN_ID) || battle.details.clanid.Equals(Constants.NLBE2_CLAN_ID))
		{
			IDiscordMessage message = await GetHOFMessage(guildID, battle.vehicle_tier, battle.vehicle);
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
										string extraDescription = await _replayService.GetDescriptionForReplay(battle, position);
										IDiscordMessage tempMessage = await _messageService.SayReplayIsWorthy(channel, battle, extraDescription, position);
										return new Tuple<string, IDiscordMessage>(tempMessage.Content, tempMessage);
									}
									else
									{
										string extraDescription = await _replayService.GetDescriptionForReplay(battle, 0);
										IDiscordMessage tempMessage = await _messageService.SayReplayNotWorthy(channel, battle, extraDescription);
										return new Tuple<string, IDiscordMessage>(tempMessage.Content, tempMessage);
									}
								}
								else
								{
									IDiscordMessage tempMessage = await AddReplayToMessage(battle, message, channel, tierHOF);
									return new Tuple<string, IDiscordMessage>(tempMessage != null ? tempMessage.Content : string.Empty, tempMessage);
								}
							}
						}
					}
					else
					{
						string thumbnail = string.Empty;
						List<Tuple<string, string>> images = await _mapService.GetAllMaps(channel.Guild.Id);
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
									await _errorHandler.HandleErrorAsync("Could not set thumbnail for embed:", ex);
								}
								break;
							}
						}

						EmbedOptions options = new()
						{
							Thumbnail = thumbnail,
							Title = "Helaas... Deze replay staat er al in.",
							Description = await _replayService.GetDescriptionForReplay(battle, 0),
						};
						IDiscordMessage tempMessage = await _messageService.CreateEmbed(channel, options);
						return new Tuple<string, IDiscordMessage>(string.Empty, tempMessage);//string empty omdat dan hofafterupload het niet verkeerd opvat
					}
				}
				else
				{
					IDiscordMessage tempMessage = await AddReplayToMessage(battle, message, channel, []);
					return new Tuple<string, IDiscordMessage>(tempMessage != null ? tempMessage.Content : string.Empty, tempMessage);
				}
			}
			else
			{
				IDiscordMessage tempMessage = await _messageService.SaySomethingWentWrong(channel, member, guildName, "**Het bericht van de tier van de replay kon niet gevonden worden!**");
				return new Tuple<string, IDiscordMessage>(tempMessage.Content, tempMessage);
			}
		}
		else
		{
			IDiscordMessage tempMessage = await _messageService.SaySomethingWentWrong(channel, member, guildName, "**Enkel replays van NLBE-clanleden mogen gebruikt worden!**");
			return new Tuple<string, IDiscordMessage>(tempMessage.Content, tempMessage);
		}
		return null;
	}
	public async Task<IDiscordMessage> GetHOFMessage(ulong GuildID, int tier, string vehicle)
	{
		IDiscordChannel channel = await _channelService.GetHallOfFameChannel(GuildID);
		if (channel != null)
		{
			IReadOnlyList<IDiscordMessage> messages = await channel.GetMessagesAsync(100);
			if (messages != null)
			{
				List<IDiscordMessage> tierMessages = GetTierMessages(tier, messages);
				foreach (IDiscordMessage tierMessage in tierMessages)
				{
					if (tierMessage.Embeds[0].Fields != null)
					{
						if (tierMessage.Embeds[0].Fields.Count() > 0)
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
				foreach (IDiscordMessage tierMessage in tierMessages)
				{
					if (tierMessage.Embeds[0].Fields != null)
					{
						if (tierMessage.Embeds[0].Fields.Count() > 0)
						{
							if (tierMessage.Embeds[0].Fields.Count() < 15)//15 fields in embed
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
				List<IDiscordMessage> LTmessages = [];
				foreach (IDiscordMessage tierMessage in messages)
				{
					if (tierMessage.Embeds != null)
					{
						if (tierMessage.Embeds.Count > 0)
						{
							string emojiAsString = tierMessage.Embeds[0].Title.Replace("Tier ", string.Empty);
							int index = Emoj.GetIndex(_discordMessageUtils.GetEmojiAsString(emojiAsString));
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
	public List<IDiscordMessage> GetTierMessages(int tier, IReadOnlyList<IDiscordMessage> messages)
	{
		messages = messages.Reverse().ToList();
		List<IDiscordMessage> tierMessages = [];
		foreach (IDiscordMessage message in messages)
		{
			if (message.Embeds != null)
			{
				if (message.Embeds.Count > 0)
				{
					if (message.Embeds[0].Title.Contains(_discordMessageUtils.GetDiscordEmoji(Emoj.GetName(tier)).ToString()))
					{
						tierMessages.Add(message);
					}
				}
			}
		}
		return tierMessages;
	}
	public List<Tuple<string, List<TankHof>>> ConvertHOFMessageToTupleListAsync(IDiscordMessage message, int TIER)
	{
		if (message.Embeds != null)
		{
			if (message.Embeds.Count > 0)
			{
				foreach (IDiscordEmbed embed in message.Embeds)
				{
					if (embed.Fields != null)
					{
						if (embed.Fields.Count() > 0)
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

	public async Task EditHOFMessage(IDiscordMessage message, List<Tuple<string, List<TankHof>>> tierHOF)
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
						sb.AppendLine(i + 1 + ". [" + sortedTankHofList[i].Speler.Replace("\\", string.Empty).Replace('_', Constants.UNDERSCORE_REPLACEMENT_CHAR) + "](" + sortedTankHofList[i].Link + ") `" + sortedTankHofList[i].Damage + " dmg`");
					}
					newDiscEmbedBuilder.AddField(item.Item1, sb.ToString().adaptToDiscordChat());
				}
			}

			newDiscEmbedBuilder.Title = "Tier " + _discordMessageUtils.GetDiscordEmoji(Emoj.GetName(tier));

			IDiscordEmbed embed = new DiscordEmbedWrapper(newDiscEmbedBuilder.Build());
			await message.ModifyAsync(embed);
		}
		catch (Exception ex)
		{
			await _errorHandler.HandleErrorAsync("While editing HOF message: ", ex);
			await message.CreateReactionAsync(_discordMessageUtils.GetDiscordEmoji(Constants.MAINTENANCE_REACTION));
		}
	}
	public async Task<IDiscordMessage> AddReplayToMessage(WGBattle battle, IDiscordMessage message, IDiscordChannel channel, List<Tuple<string, List<TankHof>>> tierHOF)
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

		string extraDescription = await _replayService.GetDescriptionForReplay(battle, position);
		return await _messageService.SayReplayIsWorthy(channel, battle, extraDescription, position);
	}
	public async Task<List<Tuple<string, List<TankHof>>>> GetTankHofsPerPlayer(ulong guildID)
	{
		List<Tuple<string, List<TankHof>>> players = [];
		IDiscordChannel channel = await _channelService.GetHallOfFameChannel(guildID);
		if (channel != null)
		{
			IReadOnlyList<IDiscordMessage> messages = await channel.GetMessagesAsync(100);
			if (messages != null && messages.Count > 0)
			{
				List<Tuple<IDiscordMessage, int>> allTierMessages = [];
				for (int i = 1; i <= 10; i++)
				{
					List<IDiscordMessage> tierMessages = GetTierMessages(i, messages);
					foreach (IDiscordMessage tempMessage in tierMessages)
					{
						allTierMessages.Add(new Tuple<IDiscordMessage, int>(tempMessage, i));
					}
				}

				//Has all HOF messages
				foreach (Tuple<IDiscordMessage, int> message in allTierMessages)
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

	public async Task<bool> CreateOrCleanHOFMessages(IDiscordChannel HOFchannel, List<Tuple<int, IDiscordMessage>> tiersFound)
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
						tiersFound[j] = new Tuple<int, IDiscordMessage>(i, tiersFound[j].Item2);
						made = true;
					}
					else
					{
						await tiersFound[j].Item2.DeleteAsync();
						tiersFound[j] = new Tuple<int, IDiscordMessage>(i, null);
					}
				}
				else if (!made && tiersFound[j].Item1 < i)
				{
					await tiersFound[j].Item2.ModifyAsync(CreateHOFResetEmbed(i));
					tiersFound[j] = new Tuple<int, IDiscordMessage>(i, tiersFound[j].Item2);
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
	private IDiscordEmbed CreateHOFResetEmbed(int tier)
	{
		return _messageService.CreateStandardEmbed("Tier " + _discordMessageUtils.GetDiscordEmoji(Emoj.GetName(tier)), "Nog geen replays aan deze tier toegevoegd.", Constants.HOF_COLOR);
	}
	public static TankHof InitializeTankHof(WGBattle battle)
	{
		return new TankHof(battle.view_url, battle.player_name, battle.vehicle, battle.details.damage_made, battle.vehicle_tier);
	}

	public async Task HofAfterUpload(Tuple<string, IDiscordMessage> returnedTuple, IDiscordMessage uploadMessage)
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
						foreach (IDiscordEmbed embed in returnedTuple.Item2.Embeds)
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
				await uploadMessage.CreateReactionAsync(_discordMessageUtils.GetDiscordEmoji(":thumbsup:"));
			}
			else
			{
				await uploadMessage.CreateReactionAsync(_discordMessageUtils.GetDiscordEmoji(":thumbsdown:"));
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
				IDiscordEmbed embed = new DiscordEmbedWrapper(new DiscordEmbedBuilder()
				{
					Title = "Resultaat",
					Description = sb.ToString(),
					Color = Constants.BOT_COLOR,
					Thumbnail = new()
					{
						Url = thumbnail
					}
				}.Build());
				await returnedTuple.Item2.ModifyAsync(embed);
			}
			catch (Exception ex)
			{
				await _errorHandler.HandleErrorAsync("While editing Resultaat message: ", ex);
			}
		}
	}
}
