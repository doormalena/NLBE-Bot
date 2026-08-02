namespace NLBE_Bot.Services;

using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal class ChannelService(ILogger<ChannelService> logger) : IChannelService
{
	private readonly ILogger<ChannelService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
