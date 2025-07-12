namespace NLBE_Bot.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal class ChannelService(IOptions<BotOptions> options,
							  IDiscordClient discordClient,
							  ILogger<ChannelService> logger) : IChannelService
{
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	private readonly IDiscordClient _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
	private readonly ILogger<ChannelService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async Task<IDiscordChannel> GetHallOfFameChannel()
	{
		ulong ChatID = 793268894454251570;
		return await GetChannel(ChatID);
	}
	public async Task<IDiscordChannel> GetMasteryReplaysChannel()
	{
		ulong ChatID = Constants.MASTERY_REPLAYS_ID;
		return await GetChannel(ChatID);
	}
	public async Task<IDiscordChannel> GetReplayResultsChannel()
	{
		ulong ChatID = 583958593129414677;
		return await GetChannel(ChatID);
	}
	public async Task<IDiscordChannel> GetWeeklyEventChannel()
	{
		ulong ChatID = 897749692895596565;
		return await GetChannel(ChatID);
	}
	public async Task<IDiscordChannel> GetAlgemeenChannel()
	{
		ulong ChatID = 507575682046492692;
		return await GetChannel(ChatID);
	}
	public async Task<IDiscordChannel> GetOudLedenChannel()
	{
		ulong ChatID = 744462244951228507;
		return await GetChannel(ChatID);
	}
	public async Task<IDiscordChannel> GetDeputiesChannel()
	{
		ulong ChatID = 668211371522916389;
		return await GetChannel(ChatID);
	}
	public async Task<IDiscordChannel> GetWelkomChannel()
	{
		ulong ChatID = 681960256296976405;
		return await GetChannel(ChatID);
	}
	public async Task<IDiscordChannel> GetRegelsChannel()
	{
		ulong ChatID = 679531304882012165;
		return await GetChannel(ChatID);
	}
	public async Task<IDiscordChannel> GetLogChannel()
	{
		ulong ChatID = 782308602882031660;
		return await GetChannel(ChatID);
	}
	public async Task<IDiscordChannel> GetToernooiAanmeldenChannel()
	{
		ulong ChatID = Constants.NLBE_TOERNOOI_AANMELDEN_KANAAL_ID;
		return await GetChannel(ChatID);
	}
	public async Task<IDiscordChannel> GetMappenChannel()
	{
		ulong ChatID = 782240999190953984;
		return await GetChannel(ChatID);
	}

	public async Task<IDiscordChannel> GetBotTestChannel()
	{
		return await GetChannel(_options.ChannelIds["BotTest"]);
	}

	public async Task<IDiscordChannel> GetTestChannel()
	{
		ulong ChatID = 804477788676685874;
		return await GetChannel(ChatID);
	}
	public async Task<IDiscordChannel> GetPollsChannel(bool isDeputyPoll)
	{
		long ChatID = isDeputyPoll ? 805800443178909756 : 781522161159897119;
		return await GetChannel((ulong) ChatID);
	}

	public async Task<IDiscordChannel> GetChannel(ulong channelId)
	{
		try
		{
			IDiscordGuild guild = await _discordClient.GetGuildAsync(_options.ServerId);

			if (guild != null)
			{
				return guild.GetChannel(channelId);
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, ex.Message);
		}

		return null;
	}

	public async Task<IDiscordChannel> GetChannelBasedOnString(string guildNameOrTag, ulong guildID)
	{
		bool isId = false;

		if (guildNameOrTag.StartsWith('<'))
		{
			isId = true;
			guildNameOrTag = guildNameOrTag.TrimStart('<');
			guildNameOrTag = guildNameOrTag.TrimStart('#');
			guildNameOrTag = guildNameOrTag.TrimEnd('>');
		}

		IDiscordGuild guild = await _discordClient.GetGuildAsync(guildID);

		if (guild != null)
		{
			foreach (KeyValuePair<ulong, IDiscordChannel> channel in guild.Channels)
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
	public async Task CleanWelkomChannel()
	{
		IDiscordChannel welkomChannel = await GetWelkomChannel();
		IReadOnlyList<IDiscordMessage> messages = welkomChannel.GetMessagesAsync(100).Result;
		foreach (IDiscordMessage message in messages)
		{
			if (!message.Pinned)
			{
				await welkomChannel.DeleteMessageAsync(message);
				await Task.Delay(875);
			}
		}
	}
	public async Task CleanWelkomChannel(ulong userId)
	{
		IDiscordChannel welkomChannel = await GetWelkomChannel();
		IReadOnlyList<IDiscordMessage> messages = welkomChannel.GetMessagesAsync(100).Result;
		foreach (IDiscordMessage message in messages)
		{
			bool deleteMessage = false;
			if (!message.Pinned)
			{
				if (message.Author.Id.Equals(Constants.NLBE_BOT))
				{
					if (message.Content.Contains("<@" + userId + ">"))
					{
						deleteMessage = true;
					}
				}
				else if (message.Author.Id.Equals(userId))
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

	public async Task CleanChannel(ulong channelId)
	{
		IDiscordChannel channel = await GetChannel(channelId);
		IReadOnlyList<IDiscordMessage> messages = channel.GetMessagesAsync(100).Result;
		foreach (IDiscordMessage message in messages)
		{
			if (!message.Pinned)
			{
				await channel.DeleteMessageAsync(message);
				await Task.Delay(875);
			}
		}
	}
}
