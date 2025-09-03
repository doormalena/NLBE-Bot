namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using NSubstitute;

[TestClass]
public class ChannelServiceTests
{
	private IDiscordClient? _discordClientMock;
	private ILogger<ChannelService>? _loggerMock;
	private IDiscordGuild? _guildMock;
	private IOptions<BotOptions>? _options;
	private ChannelService? _channelService;

	[TestInitialize]
	public void Setup()
	{
		BotOptions botOptions = new()
		{
			ChannelIds = new()
			{
				OldMembers = 1234567890,
				Rules = 9876543210,
				Welcome = 1122334455
			},
			ServerId = 12345
		};
		_options = Options.Create(botOptions);
		_guildMock = Substitute.For<IDiscordGuild>();
		_discordClientMock = Substitute.For<IDiscordClient>();
		_discordClientMock!.GetGuildAsync(_options!.Value.ServerId).Returns(Task.FromResult(_guildMock));
		_loggerMock = Substitute.For<ILogger<ChannelService>>();
		_channelService = new ChannelService(_loggerMock);
	}

	[TestMethod]
	public async Task CleanChannelAsync_ShouldDeleteBotAndUserMessages()
	{
		// Arrange.
		IDiscordChannel channelMock = Substitute.For<IDiscordChannel>();
		_guildMock!.GetChannel(_options!.Value.ChannelIds.Welcome).Returns(channelMock);

		ulong userId = 777UL;
		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.Id.Returns(userId);

		IDiscordMessage botMessageMentioningUser = Substitute.For<IDiscordMessage>();
		botMessageMentioningUser.Pinned.Returns(false);
		botMessageMentioningUser.Author.Id.Returns(Constants.NLBE_BOT);
		botMessageMentioningUser.Content.Returns($"<@{userId}> Welcome!");

		IDiscordMessage botMessageNotMentioningUser = Substitute.For<IDiscordMessage>();
		botMessageNotMentioningUser.Pinned.Returns(false);
		botMessageNotMentioningUser.Author.Id.Returns(Constants.NLBE_BOT);
		botMessageNotMentioningUser.Content.Returns("Hello world");

		IDiscordMessage userMessage = Substitute.For<IDiscordMessage>();
		userMessage.Pinned.Returns(false);
		userMessage.Author.Id.Returns(userId);
		userMessage.Content.Returns("Hi!");

		IDiscordMessage pinnedMessage = Substitute.For<IDiscordMessage>();
		pinnedMessage.Pinned.Returns(true);

		channelMock.GetMessagesAsync(100).Returns(Task.FromResult<IReadOnlyList<IDiscordMessage>>(
			[botMessageMentioningUser, botMessageNotMentioningUser, userMessage, pinnedMessage]
		));

		// Act.
		await _channelService!.CleanChannelAsync(channelMock, member);

		// Assert.
		await channelMock.Received(1).DeleteMessageAsync(botMessageMentioningUser);
		await channelMock.Received(1).DeleteMessageAsync(userMessage);
		await channelMock.DidNotReceive().DeleteMessageAsync(botMessageNotMentioningUser);
		await channelMock.DidNotReceive().DeleteMessageAsync(pinnedMessage);
	}

	[TestMethod]
	public async Task CleanChannelAsync_ShouldDeleteOnlyUnpinnedMessages()
	{
		// Arrange.
		ulong channelId = 555UL;
		IDiscordChannel channelMock = Substitute.For<IDiscordChannel>();
		_guildMock!.GetChannel(channelId).Returns(channelMock);

		IDiscordMessage unpinnedMessage = Substitute.For<IDiscordMessage>();
		unpinnedMessage.Pinned.Returns(false);

		IDiscordMessage pinnedMessage = Substitute.For<IDiscordMessage>();
		pinnedMessage.Pinned.Returns(true);

		channelMock.GetMessagesAsync(100).Returns(Task.FromResult<IReadOnlyList<IDiscordMessage>>(
		   [unpinnedMessage, pinnedMessage]
		 ));

		// Act.
		await _channelService!.CleanChannelAsync(channelMock);

		// Assert.
		await channelMock.Received(1).DeleteMessageAsync(unpinnedMessage);
		await channelMock.DidNotReceive().DeleteMessageAsync(pinnedMessage);
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() => new ChannelService(null!));
	}
}
