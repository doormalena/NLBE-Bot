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
	private async Task<IDiscordChannel?> GetChannelAsync(ulong channelId)
	{
		IDiscordGuild guild = await _discordClient.GetGuildAsync(_options.ServerId);
		return guild?.GetChannel(channelId);
	}

	public Task CleanChannelAsync(IDiscordChannel channel)
	{
		return CleanChannelInternalAsync(channel, m => true);
	}

	public Task CleanChannelAsync(IDiscordChannel channel, IDiscordMember member)
	{
		return CleanChannelInternalAsync(
			channel,
			m =>
				(m.Author.Id == Constants.NLBE_BOT && m.Content.Contains($"<@{member.Id}>")) ||
				m.Author.Id == member.Id
		);
	}

	private async Task CleanChannelInternalAsync(IDiscordChannel channel, Func<IDiscordMessage, bool> shouldDelete)
	{
		channel = channel ?? throw new ArgumentNullException(nameof(channel));

		// Note: 100 is the max page limit, normally this amount should be sufficient.
		IReadOnlyList<IDiscordMessage> messages = await channel.GetMessagesAsync(100);

		foreach (IDiscordMessage message in messages)
		{
			if (!message.Pinned && shouldDelete(message))
			{
				_logger.LogDebug("Deleting message {MessageId} in channel {ChannelId}", message.Id, channel.Id);
				await channel.DeleteMessageAsync(message);
			}
		}
	}
}
