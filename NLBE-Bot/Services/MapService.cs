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
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

internal class MapService(IOptions<BotOptions> options, ILogger<MapService> logger, IMapsRepository mapsRepository) : IMapService
{
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	private readonly ILogger<MapService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IMapsRepository _mapsRepository = mapsRepository ?? throw new ArgumentNullException(nameof(mapsRepository));

	public async Task<Dictionary<string, MapInfo>> GetAllMaps(IDiscordGuild guild)
	{
		if (Guard.ReturnIfNull(guild.GetChannel(_options.ChannelIds.Maps), _logger, "Maps channel", out IDiscordChannel mapChannel))
		{
			return [];
		}

		IReadOnlyList<IDiscordMessage> messages = await mapChannel.GetMessagesAsync(100);
		// TODO: investigate to move images to embeded resources or download them from a site such as https://wottactic.com/

		List<Tuple<string, string>> images = [];
		foreach (IDiscordMessage message in messages)
		{
			images.AddRange(from IDiscordAttachment attachment in message.Attachments
							let fileName = GetProperFileName(attachment.Url)
							select new Tuple<string, string>(fileName, attachment.Url));
		}

		Dictionary<string, MapInfo>? maps = await _mapsRepository.GetAllAsync();
		if (maps == null)
		{
			return [];
		}

		foreach ((MapInfo map, Tuple<string, string> match) in from KeyValuePair<string, MapInfo> entry in maps
															   let map = entry.Value
															   let match = images.FirstOrDefault(x => x.Item1.Equals(map.Name, StringComparison.OrdinalIgnoreCase))
															   select (map, match))
		{
			if (match != null)
			{
				map.ImageUrl = match.Item2;
			}
			else
			{
				_logger.LogWarning("Map image not found for map {MapName} in guild {GuildName}", map.Name, guild.Name);
			}
		}

		return maps;
	}

	private static string GetProperFileName(string file)
	{
		// Examples:
		// https://cdn.discordapp.com/attachments/{channel_id}/{attachment_id}/Yamato_Harbor.png?ex={expiry}&is={signature}&hm={hash}&
		// https://cdn.discordapp.com/attachments/{channel_id}/{attachment_id}/Canyon.png?ex={expiry}&is={signature}&hm={hash}&

		string[] splitted = file.Split('\\');
		string name = splitted[^1];
		return Path.GetFileNameWithoutExtension(name).Replace('_', ' ');
	}
}
