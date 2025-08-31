namespace NLBE_Bot.Services;

using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

internal class MapService(ILogger<MapService> logger, IChannelService channelService) : IMapService
{
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly ILogger<MapService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async Task<List<Tuple<string, string>>> GetAllMaps(ulong guildId)
	{
		IDiscordChannel mapChannel = await _channelService.GetMappenChannelAsync();

		if (mapChannel == null)
		{
			return null;
		}

		List<Tuple<string, string>> images = [];
		try
		{
			IReadOnlyList<IDiscordMessage> xMessages = mapChannel.GetMessagesAsync(100).Result;
			foreach (IDiscordMessage message in xMessages)
			{
				IReadOnlyList<DiscordAttachment> attachments = message.Attachments;
				foreach (DiscordAttachment item in attachments)
				{
					images.Add(new Tuple<string, string>(GetProperFileName(item.Url), item.Url));
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error while getting map images from channel {ChannelName}.", mapChannel.Name);
		}

		images.Sort((x, y) => y.Item1.CompareTo(x.Item1));
		images.Reverse();
		return images;
	}
	public static string GetProperFileName(string file)
	{
		string[] splitted = file.Split('\\');
		string name = splitted[splitted.Length - 1];
		return Path.GetFileNameWithoutExtension(name).Replace('_', ' ');
	}
}
