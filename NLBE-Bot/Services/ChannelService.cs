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

	public async Task<IDiscordChannel?> GetDeputiesChannelAsync()
	{
		ulong ChatID = 668211371522916389;
		return await GetChannelAsync(ChatID);
	}

	public async Task<IDiscordChannel?> GetLogChannelAsync()
	{
		ulong ChatID = 782308602882031660;
		return await GetChannelAsync(ChatID);
	}

	public async Task<IDiscordChannel?> GetMappenChannelAsync()
	{
		ulong ChatID = 782240999190953984;
		return await GetChannelAsync(ChatID);
	}

	public async Task<IDiscordChannel?> GetPollsChannelAsync(bool isDeputyPoll)
	{
		long ChatID = isDeputyPoll ? 805800443178909756 : 781522161159897119;
		return await GetChannelAsync((ulong) ChatID);
	}

	public Task CleanChannelAsync(ulong channelId)
	{
		return CleanChannelInternalAsync(channelId, m => true);
	}

	public Task CleanWelkomChannelAsync(ulong userId)
	{
		return CleanChannelInternalAsync(
			_options.ChannelIds.Welcome,
			m =>
				(m.Author.Id == Constants.NLBE_BOT && m.Content.Contains($"<@{userId}>")) ||
				m.Author.Id == userId
		);
	}

	public async Task<IDiscordChannel?> GetChannelAsync(ulong channelId)
	{
		IDiscordGuild guild = await _discordClient.GetGuildAsync(_options.ServerId);
		return guild?.GetChannel(channelId);
	}

	private async Task CleanChannelInternalAsync(ulong channelId, Func<IDiscordMessage, bool> shouldDelete)
	{
		if (Guard.ReturnIfNull(await GetChannelAsync(channelId), _logger, $"Welcome channel", out IDiscordChannel channel))
		{
			return;
		}

		// Note: 100 is the max page limit, normally this amount should be sufficient.
		IReadOnlyList<IDiscordMessage> messages = await channel.GetMessagesAsync(100);

		foreach (IDiscordMessage message in messages)
		{
			if (!message.Pinned && shouldDelete(message))
			{
				await channel.DeleteMessageAsync(message);
			}
		}
	}
}
