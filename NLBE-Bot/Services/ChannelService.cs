namespace NLBE_Bot.Services;

using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal class ChannelService(IDiscordClient discordClient, ILogger<ChannelService> logger, IGuildProvider guildProvider) : IChannelService
{
	private readonly IDiscordClient _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
	private readonly ILogger<ChannelService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IGuildProvider _guildProvider = guildProvider ?? throw new ArgumentNullException(nameof(guildProvider));

	public async Task<DiscordChannel> GetHallOfFameChannel(ulong GuildID)
	{
		long ChatID = GuildID.Equals(Constants.NLBE_SERVER_ID) ? 793268894454251570 : 793429499403304960;
		return await GetChannel(GuildID, (ulong) ChatID);
	}
	public async Task<DiscordChannel> GetMasteryReplaysChannel(ulong GuildID)
	{
		ulong ChatID = GuildID.Equals(Constants.NLBE_SERVER_ID) ? Constants.MASTERY_REPLAYS_ID : Constants.PRIVE_ID;
		return await GetChannel(GuildID, ChatID);
	}
	public async Task<DiscordChannel> GetReplayResultsChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 583958593129414677;
		return await GetChannel(ServerID, ChatID);
	}
	public async Task<DiscordChannel> GetWeeklyEventChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 897749692895596565;
		if (Constants.version.ToLower().Contains("local"))
		{
			ServerID = Constants.DA_BOIS_ID;
			ChatID = 901480697011777538;
		}
		return await GetChannel(ServerID, ChatID);
	}
	public async Task<DiscordChannel> GetAlgemeenChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 507575682046492692;
		return await GetChannel(ServerID, ChatID);
	}
	public async Task<DiscordChannel> GetOudLedenChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 744462244951228507;
		return await GetChannel(ServerID, ChatID);
	}
	public async Task<DiscordChannel> GetDeputiesChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 668211371522916389;
		return await GetChannel(ServerID, ChatID);
	}
	public async Task<DiscordChannel> GetWelkomChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 681960256296976405;
		return await GetChannel(ServerID, ChatID);
	}
	public async Task<DiscordChannel> GetRegelsChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 679531304882012165;
		return await GetChannel(ServerID, ChatID);
	}
	public async Task<DiscordChannel> GetLogChannel(ulong GuildID)
	{
		return GuildID == Constants.NLBE_SERVER_ID ? await GetChannel(GuildID, 782308602882031660) : await GetChannel(GuildID, 808319637447376899);
	}
	public async Task<DiscordChannel> GetToernooiAanmeldenChannel(ulong GuildID)
	{
		return GuildID == Constants.NLBE_SERVER_ID
			? await GetChannel(GuildID, Constants.NLBE_TOERNOOI_AANMELDEN_KANAAL_ID)
			: await GetChannel(GuildID, Constants.DA_BOIS_TOERNOOI_AANMELDEN_KANAAL_ID);
	}
	public async Task<DiscordChannel> GetMappenChannel(ulong GuildID)
	{
		long ChatID = GuildID.Equals(Constants.NLBE_SERVER_ID) ? 782240999190953984 : 804856157918855209;
		return await GetChannel(GuildID, (ulong) ChatID);
	}
	public async Task<DiscordChannel> GetBottestChannel()
	{
		ulong ServerID = Constants.NLBE_SERVER_ID;
		ulong ChatID = 781617141069774898;
		return await GetChannel(ServerID, ChatID);
	}
	public async Task<DiscordChannel> GetTestChannel()
	{
		ulong ServerID = Constants.DA_BOIS_ID;
		ulong ChatID = 804477788676685874;
		return await GetChannel(ServerID, ChatID);
	}
	public async Task<DiscordChannel> GetPollsChannel(bool isDeputyPoll, ulong GuildID)
	{
		if (GuildID == Constants.NLBE_SERVER_ID)
		{
			long ChatID = isDeputyPoll ? 805800443178909756 : 781522161159897119;
			return await GetChannel(GuildID, (ulong) ChatID);
		}

		return await GetTestChannel();
	}
	public async Task<DiscordChannel> GetChannel(ulong serverID, ulong chatID)
	{
		try
		{
			DiscordGuild guild = await _discordClient.GetGuildAsync(serverID);

			if (guild != null)
			{
				return guild.GetChannel(chatID);
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, ex.Message);
		}

		return null;
	}

	public async Task<DiscordChannel> GetChannelBasedOnString(string guildNameOrTag, ulong guildID)
	{
		bool isId = false;

		if (guildNameOrTag.StartsWith('<'))
		{
			isId = true;
			guildNameOrTag = guildNameOrTag.TrimStart('<');
			guildNameOrTag = guildNameOrTag.TrimStart('#');
			guildNameOrTag = guildNameOrTag.TrimEnd('>');
		}

		IDiscordGuild guild = await _guildProvider.GetGuild(guildID);

		if (guild != null)
		{
			foreach (KeyValuePair<ulong, DiscordChannel> channel in guild.Channels)
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
		DiscordChannel welkomChannel = await GetWelkomChannel();
		IReadOnlyList<DiscordMessage> messages = welkomChannel.GetMessagesAsync(100).Result;
		foreach (DiscordMessage message in messages)
		{
			if (!message.Pinned)
			{
				await welkomChannel.DeleteMessageAsync(message);
				await Task.Delay(875);
			}
		}
	}
	public async Task CleanWelkomChannel(ulong userID)
	{
		DiscordChannel welkomChannel = await GetWelkomChannel();
		IReadOnlyList<DiscordMessage> messages = welkomChannel.GetMessagesAsync(100).Result;
		foreach (DiscordMessage message in messages)
		{
			bool deleteMessage = false;
			if (!message.Pinned)
			{
				if (message.Author.Id.Equals(Constants.NLBE_BOT))
				{
					if (message.Content.Contains("<@" + userID + ">"))
					{
						deleteMessage = true;
					}
				}
				else if (message.Author.Id.Equals(userID))
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

	public async Task CleanChannel(ulong serverID, ulong channelID)
	{
		DiscordChannel channel = await GetChannel(serverID, channelID);
		IReadOnlyList<DiscordMessage> messages = channel.GetMessagesAsync(100).Result;
		foreach (DiscordMessage message in messages)
		{
			if (!message.Pinned)
			{
				await channel.DeleteMessageAsync(message);
				await Task.Delay(875);
			}
		}
	}
}
