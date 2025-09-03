namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[TestClass]
public class MapServiceTests
{
	private ILogger<MapService>? _loggerMock;
	private IOptions<BotOptions>? _optionsMock;
	private MapService? _mapService;

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
		_mapService = new MapService(_optionsMock, _loggerMock);
	}

	[TestMethod]
	public async Task GetAllMaps_ShouldReturnSortedMapList_WhenAttachmentsExist()
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

		// Act.
		List<Tuple<string, string>> result = await _mapService!.GetAllMaps(guildMock);

		// Assert.
		Assert.AreEqual(2, result.Count);
		Assert.AreEqual("map alpha", result[0].Item1);
		Assert.AreEqual("map beta", result[1].Item1);
	}

	[TestMethod]
	public async Task GetAllMaps_ShouldReturnEmptyList_WhenChannelIsNull()
	{
		// Arrange.
		IDiscordGuild guildMock = Substitute.For<IDiscordGuild>();
		guildMock.GetChannel(_optionsMock!.Value.ChannelIds.Maps).Returns((IDiscordChannel?) null);

		// Act.
		List<Tuple<string, string>> result = await _mapService!.GetAllMaps(guildMock);

		// Assert.
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
	public async Task GetAllMaps_ShouldLogError_WhenMessageFetchFails()
	{
		// Arrange.
		IDiscordGuild guildMock = Substitute.For<IDiscordGuild>();
		IDiscordChannel channelMock = Substitute.For<IDiscordChannel>();
		channelMock.Name.Returns("maps-channel");
		Exception exception = new("Fetch failed");
		channelMock.GetMessagesAsync(100)
				.Returns(Task.FromException<IReadOnlyList<IDiscordMessage>>(exception));
		guildMock.GetChannel(_optionsMock!.Value.ChannelIds.Maps).Returns(channelMock);

		// Act.
		List<Tuple<string, string>> result = await _mapService!.GetAllMaps(guildMock);

		// Assert.
		_loggerMock!.Received().Log(
							LogLevel.Error,
							Arg.Any<EventId>(),
							Arg.Is<object>(v => v.ToString()!.Contains("Error while getting map images from channel maps-channel.")),
							exception,
							Arg.Any<Func<object, Exception?, string>>());
	}

	[TestMethod]
	public void GetProperFileName_ShouldReturnCleanName()
	{
		// Arrange.
		string input = "https://cdn.discordapp.com/maps/map_alpha_test.jpg";

		// Act.
		string result = MapService.GetProperFileName(input);

		// Assert.
		Assert.AreEqual("map alpha test", result);
	}
}
