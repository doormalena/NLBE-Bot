namespace NLBE_Bot.Services;

using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Tools.Replays;

internal class MessageService(IDiscordClient discordClient, ILogger<MessageService> logger, IOptions<BotOptions> options, IBotState botState,
								IChannelService channelService, IDiscordMessageUtils discordMessageUtils,
								IMapService mapService) : IMessageService
{
	private readonly IDiscordClient _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
	private readonly ILogger<MessageService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	private readonly IBotState _botState = botState ?? throw new ArgumentNullException(nameof(botState));
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IDiscordMessageUtils _discordMessageUtils = discordMessageUtils ?? throw new ArgumentNullException(nameof(discordMessageUtils));
	private readonly IMapService _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));

	public async Task<IDiscordMessage?> SendMessage(IDiscordChannel channel, IDiscordMember? member, string guildName, string message)
	{
		try
		{
			return await channel.SendMessageAsync(message);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Could not send message to channel {ChannelName} in guild {GuildName}: {Message}", channel.Name, guildName, message);

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

	public async Task<IDiscordMessage?> SendPrivateMessage(IDiscordMember member, string guildName, string message)
	{
		try
		{
			return await member.SendMessageAsync(message);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Could not send private message to member {MemberName} in guild {GuildName}: {Message}", member.DisplayName, guildName, message);
		}

		return null;
	}

	public async Task<IDiscordMessage?> SayCannotBePlayedAt(IDiscordChannel channel, IDiscordMember member, string guildName, string roomType)
	{
		if (roomType.Length == 0)
		{
			// TODO: seen this happening once for a valid replay, possible issue with json parsing / exception handling.
			return await member.SendMessageAsync("Geef aub even door welk type room dit is want het werd niet herkent door de bot. Tag gebruiker thibeastmo#9998");
		}

		return await SendMessage(channel, member, guildName, "**De battle mag niet in een " + roomType + " room gespeeld zijn!**");
	}

	public async Task SaySomethingWentWrong(IDiscordChannel channel, IDiscordMember member, string guildName)
	{
		await SendMessage(channel, member, guildName, "**Er ging iets mis, probeer het opnieuw!**");
	}

	public Task<IDiscordMessage?> SaySomethingWentWrong(IDiscordChannel channel, IDiscordMember member, string guildName, string text)
	{
		return SendMessage(channel, member, guildName, text);
	}

	public async Task SayWrongAttachments(IDiscordChannel channel, IDiscordMember member, string guildName)
	{
		await SendMessage(channel, member, guildName, "**Geen bruikbare documenten in de bijlage gevonden!**");
	}

	public async Task SayNoAttachments(IDiscordChannel channel, IDiscordMember member, string guildName)
	{
		await SendMessage(channel, member, guildName, "**Geen documenten in de bijlage gevonden!**");
	}
	public async Task SayNoResponse(IDiscordChannel channel)
	{
		await channel.SendMessageAsync("`Time-out: Geen antwoord.`");
	}
	public async Task SayNoResponse(IDiscordChannel channel, IDiscordMember member, string guildName)
	{
		await SendMessage(channel, member, guildName, "`Time-out: Geen antwoord.`");
	}

	public async Task SayMustBeNumber(IDiscordChannel channel)
	{
		await channel.SendMessageAsync("**Je moest een cijfer geven!**");
	}

	public async Task SayNumberTooSmall(IDiscordChannel channel)
	{
		await channel.SendMessageAsync("**Dat cijfer was te klein!**");
	}

	public async Task SayNumberTooBig(IDiscordChannel channel)
	{
		await channel.SendMessageAsync("**Dat cijfer was te groot!**");
	}

	public async Task SayBeMoreSpecific(IDiscordChannel channel)
	{
		EmbedOptions embedOptions = new()
		{
			Title = "Wees specifieker",
			Description = "Er waren te veel resultaten, probeer iets specifieker te zijn!",
		};
		await CreateEmbed(channel, embedOptions);
	}

	public async Task<IDiscordMessage?> SayMultipleResults(IDiscordChannel channel, string description)
	{
		try
		{
			IDiscordEmbed embed = CreateStandardEmbed("Meerdere resultaten gevonden", description.AdaptToChat(), DiscordColor.Red);
			return await channel.SendMessageAsync(embed);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Something went wrong while trying to send an embedded message.");
		}

		return null;
	}

	public async Task SayNoResults(IDiscordChannel channel, string description)
	{
		try
		{
			IDiscordEmbed embed = CreateStandardEmbed("Geen resultaten gevonden", description.Replace('_', '▁'), DiscordColor.Red);
			await channel.SendMessageAsync(embed);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Something went wrong while trying to send an embedded message.");
		}
	}

	public async Task SayTheUserIsNotAllowed(IDiscordChannel channel)
	{
		try
		{
			IDiscordEmbed embed = CreateStandardEmbed("Geen toegang", ":raised_back_of_hand: Je hebt onvoldoende rechten om dit commando uit te voeren!", DiscordColor.Red);
			await channel.SendMessageAsync(embed);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Something went wrong while trying to send an embedded message.");
		}
	}

	public async Task SayBotNotAuthorized(IDiscordChannel channel)
	{
		try
		{
			IDiscordEmbed embed = CreateStandardEmbed("Onvoldoende rechten", ":raised_back_of_hand: De bot heeft onvoldoende rechten om dit uit te voeren!", DiscordColor.Red);
			await channel.SendMessageAsync(embed);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Something went wrong while trying to send an embedded message.");
		}
	}

	public async Task SayTooManyCharacters(IDiscordChannel channel)
	{
		try
		{
			IDiscordEmbed embed = CreateStandardEmbed("Onvoldoende rechten", ":raised_back_of_hand: Er zaten te veel characters in het bericht dat de bot wilde verzenden!", DiscordColor.Red);
			await channel.SendMessageAsync(embed);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Something went wrong while trying to send an embedded message.");
		}
	}

	public Task<IDiscordMessage> SayReplayNotWorthy(IDiscordChannel channel, WGBattle battle, string extraDescription)
	{
		string description = "De statistieken van deze replay waren onvoldoende om in de Hall Of Fame te komen te staan!\n\n"
							 + extraDescription;
		return SendReplayMessage(channel, battle, "Helaas...", description);
	}

	public Task<IDiscordMessage> SayReplayIsWorthy(IDiscordChannel channel, WGBattle battle, string extraDescription, int position)
	{
		string description = "Je replay heeft een plaatsje gekregen in onze Hall Of Fame!\n\n"
							 + extraDescription;
		return SendReplayMessage(channel, battle, "Hoera! :trophy:", description);
	}

	public async Task<int> WaitForReply(IDiscordChannel channel, IDiscordUser user, string description, int count)
	{
		IDiscordMessage? discMessage = await SayMultipleResults(channel, description);
		IDiscordInteractivityExtension interactivity = _discordClient.GetInteractivity();
		IDiscordInteractivityResult<IDiscordMessage> message = await interactivity.WaitForMessageAsync(x => x.Channel == channel && x.Author == user);
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
			List<IDiscordEmoji> reacted = [];
			for (int i = 1; i <= 10; i++)
			{
				IDiscordEmoji? emoji = _discordMessageUtils!.GetDiscordEmoji(Emoj.GetName(i));
				if (emoji != null)
				{
					IReadOnlyList<IDiscordUser> users = await discMessage.GetReactionsAsync(emoji);
					foreach (IDiscordUser tempUser in users)
					{
						if (tempUser.Id.Equals(user.Id))
						{
							reacted.Add(emoji);
						}
					}
				}
			}

			_logger.LogDebug("Reacted count: {Count}", reacted.Count);

			if (reacted.Count == 1)
			{
				int index = Emoj.GetIndex(_discordMessageUtils.GetEmojiAsString(reacted[0].Name));

				_logger.LogDebug("Index: {Index}", index);

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

	public async Task<string> AskQuestion(IDiscordChannel channel, IDiscordUser user, IDiscordGuild guild, string question)
	{
		channel = channel ?? throw new ArgumentNullException(nameof(channel));

		await channel.SendMessageAsync(question);
		TimeSpan newPlayerWaitTime = TimeSpan.FromDays(_options.NewPlayerWaitTimeInDays); // TODO: The NewPlayerWaitTimeInDays and Ban mechanics should be located elsewhere isolated for handling new members.
		IDiscordInteractivityResult<IDiscordMessage> message = await channel.GetNextMessageAsync(user, newPlayerWaitTime);

		if (!message.TimedOut)
		{
			return message.Result.Content;
		}
		else
		{
			await SayNoResponse(channel);
			IDiscordMember? member = await guild.GetMemberAsync(user.Id);

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
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "{DisplayName}({Username}#{Discriminator}) could not be kicked from the server!", member.DisplayName, member.Username, member.Discriminator);
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
							catch (Exception ex)
							{
								_logger.LogWarning(ex, "{DisplayName}({Username}#{Discriminator}) could not be unbanned from the server!", member.DisplayName, member.Username, member.Discriminator);
							}
						}
					}
				}
			}

			await _channelService.CleanChannelAsync(channel);
		}

		return string.Empty;
	}

	public async Task ConfirmCommandExecuting(IDiscordMessage message)
	{
		await Task.Delay(875);
		await message.CreateReactionAsync(_discordMessageUtils.GetDiscordEmoji(Constants.IN_PROGRESS_REACTION)!);
	}

	public async Task ConfirmCommandExecuted(IDiscordMessage message)
	{
		await Task.Delay(875);
		await message.DeleteReactionsEmojiAsync(_discordMessageUtils.GetDiscordEmoji(Constants.IN_PROGRESS_REACTION)!);
		await Task.Delay(875);
		await message.CreateReactionAsync(_discordMessageUtils.GetDiscordEmoji(Constants.ACTION_COMPLETED_REACTION)!);
	}

	public IDiscordEmbed CreateStandardEmbed(string title, string description, DiscordColor color)
	{
		return new DiscordEmbedWrapper(new DiscordEmbedBuilder
		{
			Title = title,
			Description = description,
			Color = color
		}.Build());
	}

	public virtual async Task<IDiscordMessage> CreateEmbed(IDiscordChannel channel, EmbedOptions options)
	{
		DiscordEmbedBuilder newDiscEmbedBuilder = new()
		{
			Color = options.Color,
			Title = options.Title,
			Description = options.Description
		};

		if (!string.IsNullOrEmpty(options.Thumbnail))
		{
			newDiscEmbedBuilder.WithThumbnail(options.Thumbnail);
		}
		if (options.Author != null)
		{
			newDiscEmbedBuilder.Author = options.Author;
		}

		if (!string.IsNullOrEmpty(options.ImageUrl))
		{
			newDiscEmbedBuilder.ImageUrl = options.ImageUrl;
		}

		if (options.Fields != null && options.Fields.Count > 0)
		{
			foreach (DEF field in options.Fields)
			{
				if (field.Value.Length > 0)
				{
					newDiscEmbedBuilder.AddField(field.Name, field.Value, field.Inline);
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

		IDiscordEmbed embed = new DiscordEmbedWrapper(newDiscEmbedBuilder.Build());

		IDiscordMessage theMessage = options.IsForReplay && _botState.LastCreatedDiscordMessage != null
			? await _botState.LastCreatedDiscordMessage.RespondAsync(options.Content, embed)
			: await _discordClient.SendMessageAsync(channel, options.Content, embed);

		if (options.Emojis != null)
		{
			foreach (IDiscordEmoji anEmoji in options.Emojis)
			{
				await theMessage.CreateReactionAsync(anEmoji);
			}
		}

		if (!string.IsNullOrEmpty(options.NextMessage))
		{
			await channel.SendMessageAsync(options.NextMessage);
		}

		return theMessage;
	}

	private async Task<IDiscordMessage> SendReplayMessage(IDiscordChannel channel, WGBattle battle, string title, string description)
	{
		DiscordEmbedBuilder embedBuilder = new()
		{
			Color = DiscordColor.Red,
			Title = title,
			Description = description
		};

		List<Tuple<string, string>> images = await _mapService.GetAllMaps(channel.Guild);

		Tuple<string, string>? matchingMap = images.FirstOrDefault(map =>
					!string.IsNullOrEmpty(map.Item1) &&
					string.Equals(map.Item1, battle.map_name, StringComparison.OrdinalIgnoreCase));

		if (matchingMap != null)
		{
			embedBuilder.Thumbnail = new()
			{
				Url = matchingMap.Item2
			};
		}

		IDiscordEmbed embed = new DiscordEmbedWrapper(embedBuilder.Build());

		return _botState.LastCreatedDiscordMessage != null
			? await _botState.LastCreatedDiscordMessage.RespondAsync(embed)
			: await channel.SendMessageAsync(embed);
	}
}
