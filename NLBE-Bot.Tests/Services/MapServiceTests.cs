namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

[TestClass]
public class MapServiceTests
{
	private ILogger<MapService>? _loggerMock;
	private IOptions<BotOptions>? _optionsMock;
	private MapService? _mapService;
	private IMapsRepository? _mapRepositoryMock;

	[TestInitialize]
	public void Setup()
	{
		_loggerMock = Substitute.For<ILogger<MapService>>();

		BotOptions botOptions = new()
		{
			ChannelIds = new()
			{
				Maps = 1234567890
			}
		};

		_optionsMock = Options.Create(botOptions);
		_mapRepositoryMock = Substitute.For<IMapsRepository>();
		_mapService = new MapService(_optionsMock, _loggerMock, _mapRepositoryMock);
	}

	[TestMethod]
	public async Task GetAllMaps_ShouldReturnMaps_WithImages_WhenAttachmentsExist()
	{
		// Arrange.
		IDiscordGuild guildMock = Substitute.For<IDiscordGuild>();
		IDiscordChannel channelMock = Substitute.For<IDiscordChannel>();
		IDiscordMessage messageMock = Substitute.For<IDiscordMessage>();

		IDiscordAttachment attachment1 = Substitute.For<IDiscordAttachment>();
		attachment1.Url.Returns("https://cdn.discordapp.com/maps/map_alpha.jpg");
		IDiscordAttachment attachment2 = Substitute.For<IDiscordAttachment>();
		attachment2.Url.Returns("https://cdn.discordapp.com/maps/map_beta.jpg");

		messageMock.Attachments.Returns([attachment1, attachment2]);
		channelMock.GetMessagesAsync(100).Returns(Task.FromResult<IReadOnlyList<IDiscordMessage>>([messageMock]));
		guildMock.GetChannel(_optionsMock!.Value.ChannelIds.Maps).Returns(channelMock);

		_mapRepositoryMock!.GetAllAsync().Returns(Task.FromResult<Dictionary<string, MapInfo>?>(new()
		{
			{ "1", new MapInfo { Id = 1, Name = "Map Alpha" } },
			{ "2", new MapInfo { Id = 2, Name = "Map Beta" } }
		}));

		// Act.
		Dictionary<string, MapInfo> result = await _mapService!.GetAllMaps(guildMock);

		// Assert.
		Assert.AreEqual(2, result.Count);
		Assert.AreEqual("Map Alpha", result["1"].Name);
		Assert.AreEqual("https://cdn.discordapp.com/maps/map_alpha.jpg", result["1"].ImageUrl);
		Assert.AreEqual("Map Beta", result["2"].Name);
		Assert.AreEqual("https://cdn.discordapp.com/maps/map_beta.jpg", result["2"].ImageUrl);
	}
	[TestMethod]
	public async Task GetAllMaps_ShouldReturnMaps_WithoutImages_WhenAttachmentsDoNotExist()
	{
		// Arrange.
		IDiscordGuild guildMock = Substitute.For<IDiscordGuild>();
		IDiscordChannel channelMock = Substitute.For<IDiscordChannel>();
		IDiscordMessage messageMock = Substitute.For<IDiscordMessage>();

		channelMock.GetMessagesAsync(100).Returns(Task.FromResult<IReadOnlyList<IDiscordMessage>>([messageMock]));
		guildMock.GetChannel(_optionsMock!.Value.ChannelIds.Maps).Returns(channelMock);

		_mapRepositoryMock!.GetAllAsync().Returns(Task.FromResult<Dictionary<string, MapInfo>?>(new()
		{
			{ "1", new MapInfo { Id = 1, Name = "Map Alpha" } },
			{ "2", new MapInfo { Id = 2, Name = "Map Beta" } }
		}));

		// Act.
		Dictionary<string, MapInfo> result = await _mapService!.GetAllMaps(guildMock);

		// Assert.
		Assert.AreEqual(2, result.Count);
		Assert.AreEqual("Map Alpha", result["1"].Name);
		Assert.AreEqual("", result["1"].ImageUrl);
		Assert.AreEqual("Map Beta", result["2"].Name);
		Assert.AreEqual("", result["2"].ImageUrl);
	}

	[TestMethod]
	public async Task GetAllMaps_ShouldReturnEmptyList_WhenChannelIsNull()
	{
		// Arrange.
		IDiscordGuild guildMock = Substitute.For<IDiscordGuild>();
		guildMock.GetChannel(_optionsMock!.Value.ChannelIds.Maps).Returns((IDiscordChannel?) null);

		// Act.
		Dictionary<string, MapInfo> result = await _mapService!.GetAllMaps(guildMock);

		// Assert.
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
	public async Task GetAllMaps_ShouldReturnEmptyList_WhenRepositoryReturnsNull()
	{
		// Arrange.
		IDiscordGuild guildMock = Substitute.For<IDiscordGuild>();
		_mapRepositoryMock!.GetAllAsync().Returns(Task.FromResult<Dictionary<string, MapInfo>?>(null));

		// Act.
		Dictionary<string, MapInfo> result = await _mapService!.GetAllMaps(guildMock);

		// Assert.
		Assert.AreEqual(0, result.Count);
	}
}
