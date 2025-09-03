namespace NLBE_Bot.Services;

using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

internal class MapService(IOptions<BotOptions> options, ILogger<MapService> logger) : IMapService
{
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	private readonly ILogger<MapService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async Task<List<Tuple<string, string>>> GetAllMaps(IDiscordGuild guild)
	{
		if (Guard.ReturnIfNull(guild.GetChannel(_options.ChannelIds.Maps), _logger, "Maps channel", out IDiscordChannel mapChannel))
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
