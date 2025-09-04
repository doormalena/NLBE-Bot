namespace NLBE_Bot.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		IReadOnlyList<IDiscordMessage> messages = await mapChannel.GetMessagesAsync(100);

		foreach (IReadOnlyList<IDiscordAttachment>? attachments in from IDiscordMessage message in messages
																   let attachments = message.Attachments
																   select attachments)
		{
			images.AddRange(from IDiscordAttachment item in attachments
							select new Tuple<string, string>(GetProperFileName(item.Url), item.Url));
		}

		images.Sort((x, y) => y.Item1.CompareTo(x.Item1));
		images.Reverse();

		return images;
	}
	private static string GetProperFileName(string file)
	{
		string[] splitted = file.Split('\\');
		string name = splitted[^1];
		return Path.GetFileNameWithoutExtension(name).Replace('_', ' ');
	}
}
