namespace NLBE_Bot.Services;

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
		List<Tuple<string, string>> images = [];

		if (Guard.ReturnIfNull(guild.GetChannel(_options.ChannelIds.Maps), _logger, "Maps channel", out IDiscordChannel mapChannel))
		{
			return images;
		}

		try
		{
			IReadOnlyList<IDiscordMessage> xMessages = await mapChannel.GetMessagesAsync(100);
			foreach (IDiscordMessage message in xMessages)
			{
				IReadOnlyList<IDiscordAttachment> attachments = message.Attachments;
				foreach (IDiscordAttachment item in attachments)
				{
					images.Add(new Tuple<string, string>(GetProperFileName(item.Url), item.Url));
				}
			}

			images.Sort((x, y) => y.Item1.CompareTo(x.Item1));
			images.Reverse();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error while getting map images from channel {ChannelName}.", mapChannel.Name);
		}

		return images;
	}
	public static string GetProperFileName(string file)
	{
		string[] splitted = file.Split('\\');
		string name = splitted[splitted.Length - 1];
		return Path.GetFileNameWithoutExtension(name).Replace('_', ' ');
	}
}
