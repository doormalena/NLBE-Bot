namespace NLBE_Bot.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Helpers;
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

	public async Task<IDiscordChannel?> GetHallOfFameChannelAsync()
	{
		ulong ChatID = 793268894454251570;
		return await GetChannelAsync(ChatID);
	}
	public async Task<IDiscordChannel?> GetMasteryReplaysChannelAsync()
	{
		ulong ChatID = Constants.MASTERY_REPLAYS_ID;
		return await GetChannelAsync(ChatID);
	}
	public async Task<IDiscordChannel?> GetReplayResultsChannelAsync()
	{
		ulong ChatID = 583958593129414677;
		return await GetChannelAsync(ChatID);
	}
	public async Task<IDiscordChannel?> GetWeeklyEventChannelAsync()
	{
		ulong ChatID = 897749692895596565;
		return await GetChannelAsync(ChatID);
	}
	public async Task<IDiscordChannel?> GetAlgemeenChannelAsync()
	{
		ulong ChatID = 507575682046492692;
		return await GetChannelAsync(ChatID);
	}

	public async Task<IDiscordChannel?> GetOudLedenChannelAsync()
	{
		return await GetChannelAsync(_options.ChannelIds.OldMembers);
	}

	public async Task<IDiscordChannel?> GetDeputiesChannelAsync()
	{
		ulong ChatID = 668211371522916389;
		return await GetChannelAsync(ChatID);
	}

	public async Task<IDiscordChannel?> GetRegelsChannelAsync()
	{
		return await GetChannelAsync(_options.ChannelIds.Rules);
	}

	public async Task<IDiscordChannel?> GetLogChannelAsync()
	{
		ulong ChatID = 782308602882031660;
		return await GetChannelAsync(ChatID);
	}
	public async Task<IDiscordChannel?> GetToernooiAanmeldenChannelAsync()
	{
		ulong ChatID = Constants.NLBE_TOERNOOI_AANMELDEN_KANAAL_ID;
		return await GetChannelAsync(ChatID);
	}
	public async Task<IDiscordChannel?> GetMappenChannelAsync()
	{
		ulong ChatID = 782240999190953984;
		return await GetChannelAsync(ChatID);
	}

	public async Task<IDiscordChannel?> GetBotTestChannelAsync()
	{
		return await GetChannelAsync(_options.ChannelIds.BotTest);
	}

	public async Task<IDiscordChannel?> GetTestChannelAsync()
	{
		ulong ChatID = 804477788676685874;
		return await GetChannelAsync(ChatID);
	}
	public async Task<IDiscordChannel?> GetPollsChannelAsync(bool isDeputyPoll)
	{
		long ChatID = isDeputyPoll ? 805800443178909756 : 781522161159897119;
		return await GetChannelAsync((ulong) ChatID);
	}

	public async Task CleanWelkomChannelAsync(ulong userId)
	{
		if (Guard.ReturnIfNull(await GetChannelAsync(_options.ChannelIds.Welcome), _logger, $"Welcome channel", out IDiscordChannel welkomChannel))
		{
			return;
		}

		IReadOnlyList<IDiscordMessage> messages = await welkomChannel.GetMessagesAsync(100); // TODO: why this specific number?

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
				await Task.Delay(875); // TODO: why this arbitrary delay?
			}
		}
	}

	public async Task CleanChannelAsync(ulong channelId)
	{
		if (Guard.ReturnIfNull(await GetChannelAsync(channelId), _logger, $"Channel with id {channelId}", out IDiscordChannel channel))
		{
			return;
		}

		IReadOnlyList<IDiscordMessage> messages = await channel.GetMessagesAsync(100); // TODO: why this specific number?

		foreach (IDiscordMessage message in messages)
		{
			if (!message.Pinned)
			{
				await channel.DeleteMessageAsync(message);
				await Task.Delay(875); // TODO: why this arbitrary delay?
			}
		}
	}

	public async Task<IDiscordChannel?> GetChannelAsync(ulong channelId)
	{
		IDiscordGuild guild = await _discordClient.GetGuildAsync(_options.ServerId);
		return guild?.GetChannel(channelId);
	}
}
