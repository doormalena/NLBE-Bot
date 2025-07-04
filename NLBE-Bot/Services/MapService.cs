namespace NLBE_Bot.Services;

using DSharpPlus.Entities;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

internal class MapService(IErrorHandler errorHandler, IChannelService channelService) : IMapService
{
	private readonly IChannelService _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));

	public async Task<List<Tuple<string, string>>> GetAllMaps(ulong guildId)
	{
		DiscordChannel mapChannel = await _channelService.GetMappenChannel(guildId);

		if (mapChannel == null)
		{
			return null;
		}

		List<Tuple<string, string>> images = [];
		try
		{
			IReadOnlyList<DiscordMessage> xMessages = mapChannel.GetMessagesAsync(100).Result;
			foreach (DiscordMessage message in xMessages)
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
			await _errorHandler.HandleErrorAsync("Could not load messages from " + mapChannel.Name + ":", ex);
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
