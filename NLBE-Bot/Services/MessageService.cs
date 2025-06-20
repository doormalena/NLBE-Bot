namespace NLBE_Bot.Services;

using DiscordHelper;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using FMWOTB.Tools.Replays;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

internal class MessageService(IDiscordClientWrapper discordClient, ILogger<MessageService> logger, IConfiguration configuration, IErrorHandler errorHandler, IBotState botState,
								IChannelService channelService, IDiscordMessageUtils discordMessageUtils,
								IMapService mapService) : IMessageService
{
	private readonly IDiscordClientWrapper _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
	private readonly ILogger<MessageService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly IBotState _botState = botState ?? throw new ArgumentNullException(nameof(botState));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IDiscordMessageUtils _discordMessageUtils = discordMessageUtils ?? throw new ArgumentNullException(nameof(discordMessageUtils));
	private readonly IMapService _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));

	public async Task<DiscordMessage> SendMessage(DiscordChannel channel, DiscordMember member, string guildName, string message)
	{
		try
		{
			return await channel.SendMessageAsync(message);
		}
		catch (Exception ex)
		{
			await _errorHandler.HandleErrorAsync("[" + guildName + "] (" + channel.Name + ") Could not send message: ", ex);

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
				await SendPrivateMessage(member, guildName, message);
			}
		}

		return null;
	}

	public async Task<bool> SendPrivateMessage(DiscordMember member, string guildName, string Message)
	{
		try
		{
			await member.SendMessageAsync(Message);
			return true;
		}
		catch (Exception ex)
		{
			await _errorHandler.HandleErrorAsync("[" + guildName + "] Could not send private message: ", ex);
		}

		return false;
	}

	public async Task<DiscordMessage> SayCannotBePlayedAt(DiscordChannel channel, DiscordMember member, string guildName, string roomType)
	{
		if (roomType.Length == 0)
		{
			return await member.SendMessageAsync("Geef aub even door welk type room dit is want het werd niet herkent door de  Tag gebruiker thibeastmo#9998");
		}

		return await SendMessage(channel, member, guildName, "**De battle mag niet in een " + roomType + " room gespeeld zijn!**");
	}
	public async Task SaySomethingWentWrong(DiscordChannel channel, DiscordMember member, string guildName)
	{
		await SaySomethingWentWrong(channel, member, guildName, "**Er ging iets mis, probeer het opnieuw!**");
	}
	public async Task<DiscordMessage> SaySomethingWentWrong(DiscordChannel channel, DiscordMember member, string guildName, string text)
	{
		return await SendMessage(channel, member, guildName, text);
	}
	public async Task SayWrongAttachments(DiscordChannel channel, DiscordMember member, string guildName)
	{
		await SendMessage(channel, member, guildName, "**Geen bruikbare documenten in de bijlage gevonden!**");
	}
	public async Task SayNoAttachments(DiscordChannel channel, DiscordMember member, string guildName)
	{
		await SendMessage(channel, member, guildName, "**Geen documenten in de bijlage gevonden!**");
	}
	public async Task SayNoResponse(DiscordChannel channel)
	{
		await channel.SendMessageAsync("`Time-out: Geen antwoord.`");
	}
	public async Task SayNoResponse(DiscordChannel channel, DiscordMember member, string guildName)
	{
		await SendMessage(channel, member, guildName, "`Time-out: Geen antwoord.`");
	}
	public async Task SayMustBeNumber(DiscordChannel channel)
	{
		await channel.SendMessageAsync("**Je moest een cijfer geven!**");
	}
	public async Task SayNumberTooSmall(DiscordChannel channel)
	{
		await channel.SendMessageAsync("**Dat cijfer was te klein!**");
	}
	public async Task SayNumberTooBig(DiscordChannel channel)
	{
		await channel.SendMessageAsync("**Dat cijfer was te groot!**");
	}
	public async Task SayBeMoreSpecific(DiscordChannel channel)
	{
		EmbedOptions options = new()
		{
			Title = "Wees specifieker",
			Description = "Er waren te veel resultaten, probeer iets specifieker te zijn!",
		};
		await CreateEmbed(channel, options);
	}
	public DiscordMessage SayMultipleResults(DiscordChannel channel, string description)
	{
		try
		{
			DiscordEmbed embed = CreateStandardEmbed("Meerdere resultaten gevonden", description.adaptToDiscordChat(), DiscordColor.Red);
			return channel.SendMessageAsync(null, embed).Result;
		}
		catch (Exception ex)
		{
			_errorHandler.HandleErrorAsync("Something went wrong while trying to send an embedded message:", ex).Wait();
			return null;
		}
	}
	public async Task SayNoResults(DiscordChannel channel, string description)
	{
		try
		{
			DiscordEmbed embed = CreateStandardEmbed("Geen resultaten gevonden", description.Replace('_', '▁'), DiscordColor.Red);
			await channel.SendMessageAsync(null, embed);
		}
		catch (Exception ex)
		{
			await _errorHandler.HandleErrorAsync("Something went wrong while trying to send an embedded message:", ex);
		}
	}
	public async Task SayTheUserIsNotAllowed(DiscordChannel channel)
	{
		try
		{
			DiscordEmbed embed = CreateStandardEmbed("Geen toegang", ":raised_back_of_hand: Je hebt niet voldoende rechten om deze commando uit te voeren!", DiscordColor.Red);
			await channel.SendMessageAsync(null, embed);
		}
		catch (Exception ex)
		{
			await _errorHandler.HandleErrorAsync("Something went wrong while trying to send an embedded message:", ex);
		}
	}
	public async Task SayBotNotAuthorized(DiscordChannel channel)
	{
		try
		{
			DiscordEmbed embed = CreateStandardEmbed("Onvoldoende rechten", ":raised_back_of_hand: De bot heeft voldoende rechten om dit uit te voeren!", DiscordColor.Red);
			await channel.SendMessageAsync(null, embed);
		}
		catch (Exception ex)
		{
			await _errorHandler.HandleErrorAsync("Something went wrong while trying to send an embedded message:", ex);
		}
	}
	public async Task SayTooManyCharacters(DiscordChannel channel)
	{
		try
		{
			DiscordEmbed embed = CreateStandardEmbed("Onvoldoende rechten", ":raised_back_of_hand: Er zaten te veel characters in het bericht dat de bot wilde verzenden!", DiscordColor.Red);
			await channel.SendMessageAsync(null, embed);
		}
		catch (Exception ex)
		{
			await _errorHandler.HandleErrorAsync("Something went wrong while trying to send an embedded message:", ex);
		}
	}
	public async Task<DiscordMessage> SayReplayNotWorthy(DiscordChannel channel, WGBattle battle, string extraDescription)
	{
		DiscordEmbedBuilder newDiscEmbedBuilder = new()
		{
			Color = DiscordColor.Red,
			Title = "Helaas...",
			Description = "De statistieken van deze replay waren onvoldoende om in de Hall Of Fame te komen te staan!\n\n" + extraDescription
		};
		List<Tuple<string, string>> images = await _mapService.GetAllMaps(channel.Guild.Id);
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
					await _errorHandler.HandleErrorAsync("Could not set thumbnail for embed:", ex);
				}
				break;
			}
		}
		DiscordEmbed embed = newDiscEmbedBuilder.Build();

		if (_botState.LastCreatedDiscordMessage != null)
		{
			try
			{
				return await _botState.LastCreatedDiscordMessage.RespondAsync(null, embed);
			}
			catch (Exception ex)
			{
				await _errorHandler.HandleErrorAsync("Something went wrong while trying to send an embedded message:", ex);
			}
		}
		else
		{
			return await channel.SendMessageAsync(embed: embed);
		}
		return null;
	}

	public async Task<DiscordMessage> SayReplayIsWorthy(DiscordChannel channel, WGBattle battle, string extraDescription, int position)
	{
		DiscordEmbedBuilder newDiscEmbedBuilder = new()
		{
			Color = DiscordColor.Red,
			Title = "Hoera! :trophy:",
			Description = "Je replay heeft een plaatsje gekregen in onze Hall Of Fame!\n\n" + extraDescription
		};
		List<Tuple<string, string>> images = await _mapService.GetAllMaps(channel.Guild.Id);
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
					await _errorHandler.HandleErrorAsync("Could not set thumbnail for embed:", ex);
				}
				break;
			}
		}
		DiscordEmbed embed = newDiscEmbedBuilder.Build();
		if (_botState.LastCreatedDiscordMessage != null)
		{
			try
			{
				return await _botState.LastCreatedDiscordMessage.RespondAsync(null, embed);
			}
			catch (Exception ex)
			{
				await _errorHandler.HandleErrorAsync("Something went wrong while trying to send an embedded message:", ex);
			}
		}

		return await channel.SendMessageAsync(embed: embed);
	}

	//no longer sends to thibeastmo. This used to be a method to send towards thibeastmo but since thibeastmo is no longer hosting this it's moved towards the test channel
	public async Task SendThibeastmo(string message, string exceptionMessage = "", string stackTrace = "")
	{
		DiscordChannel bottestChannel = await _channelService.GetBottestChannel();
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

	public async Task<int> WaitForReply(DiscordChannel channel, DiscordUser user, string description, int count)
	{
		DiscordMessage discMessage = SayMultipleResults(channel, description);
		InteractivityExtension interactivity = _discordClient.GetInteractivity();
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
					return number - 1;
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
				IDiscordEmoji emoji = _discordMessageUtils.GetDiscordEmoji(Emoj.GetName(i));
				if (emoji != null)
				{
					IReadOnlyList<DiscordUser> users = discMessage.GetReactionsAsync(emoji.Inner).Result;
					foreach (DiscordUser tempUser in users)
					{
						if (tempUser.Id.Equals(user.Id))
						{
							reacted.Add(emoji.Inner);
						}
					}
				}
			}

			if (reacted.Count == 1)
			{
				int index = Emoj.GetIndex(_discordMessageUtils.GetEmojiAsString(reacted[0].Name));
				if (index > 0 && index <= count)
				{
					return index - 1;
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
								_logger.LogWarning("{DisplayName}({Username}#{Discriminator}) could not be kicked from the server!", member.DisplayName, member.Username, member.Discriminator);
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
										_logger.LogWarning("{DisplayName}({Username}#{Discriminator}) could not be unbanned from the server!", member.DisplayName, member.Username, member.Discriminator);
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
					await _channelService.CleanChannel(guild.Id, channel.Id);
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
			_logger.LogWarning("Channel for new members couldn't be found! Giving the noob role to user: {Username}#{Discriminator}", user.Username, user.Discriminator);
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
				_logger.LogWarning("The noob role could not be given to user: {Username}#{Discriminator}", user.Username, user.Discriminator);
			}
		}
		return null;
	}

	public async Task ConfirmCommandExecuting(DiscordMessage message)
	{
		await Task.Delay(875);
		await message.CreateReactionAsync(_discordMessageUtils.GetDiscordEmoji(Constants.IN_PROGRESS_REACTION).Inner);
	}

	public async Task ConfirmCommandExecuted(DiscordMessage message)
	{
		await Task.Delay(875);
		await message.DeleteReactionsEmojiAsync(_discordMessageUtils.GetDiscordEmoji(Constants.IN_PROGRESS_REACTION).Inner);
		await Task.Delay(875);
		await message.CreateReactionAsync(_discordMessageUtils.GetDiscordEmoji(Constants.ACTION_COMPLETED_REACTION).Inner);
	}

	public DiscordEmbed CreateStandardEmbed(string title, string description, DiscordColor color)
	{
		return new DiscordEmbedBuilder
		{
			Title = title,
			Description = description,
			Color = color
		}.Build();
	}

	public async Task<DiscordMessage> CreateEmbed(DiscordChannel channel, EmbedOptions options)
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
				await _errorHandler.HandleErrorAsync("Could not set imageurl for embed: ", ex);
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
					await _errorHandler.HandleErrorAsync("Could not set imageurl for embed: ", innerEx);
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
						await _errorHandler.HandleErrorAsync("Something went wrong while trying to add a field to an embedded message:", ex);
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
				? _botState.LastCreatedDiscordMessage.RespondAsync(options.Content, embed).Result
				: _discordClient.SendMessageAsync(channel, options.Content, embed).Result;

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
				await _errorHandler.HandleErrorAsync("Error while adding emoji's:", ex);
			}
			if (!string.IsNullOrEmpty(options.NextMessage))
			{
				await channel.SendMessageAsync(options.NextMessage);
			}
			return theMessage;
		}
		catch (Exception ex)
		{
			await _errorHandler.HandleErrorAsync("Error in createEmbed:", ex);
		}
		return null;
	}
}
