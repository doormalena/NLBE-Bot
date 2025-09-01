namespace NLBE_Bot.Tests.EventHandlers;

using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.EventHandlers;
using NLBE_Bot.Interfaces;
using NSubstitute;

[TestClass]
public class MessageEventHandlerTests
{
	private IDiscordClient? _discordClientMock;
	private ILogger<MessageEventHandler>? _loggerMock;
	private IOptions<BotOptions>? _options;
	private IChannelService? _channelServiceMock;
	private IUserService? _userServiceMock;
	private IDiscordMessageUtils? _discordMessageUtilsMock;
	private IWeeklyEventService? _weeklyEventServiceMock;
	private IMapService? _mapServiceMock;
	private IReplayService? _replayServiceMock;
	private ITournamentService? _tournamentServiceMock;
	private IHallOfFameService? _hallOfFameServiceMock;
	private IMessageService? _messageServiceMock;
	private MessageEventHandler? _handler;

	[TestInitialize]
	public void Setup()
	{
		BotOptions botOptions = new()
		{
		};
		_options = Options.Create(botOptions);
		_discordClientMock = Substitute.For<IDiscordClient>();
		_loggerMock = Substitute.For<ILogger<MessageEventHandler>>();
		_channelServiceMock = Substitute.For<IChannelService>();
		_userServiceMock = Substitute.For<IUserService>();
		_discordMessageUtilsMock = Substitute.For<IDiscordMessageUtils>();
		_weeklyEventServiceMock = Substitute.For<IWeeklyEventService>();
		_mapServiceMock = Substitute.For<IMapService>();
		_replayServiceMock = Substitute.For<IReplayService>();
		_tournamentServiceMock = Substitute.For<ITournamentService>();
		_hallOfFameServiceMock = Substitute.For<IHallOfFameService>();
		_messageServiceMock = Substitute.For<IMessageService>();

		_handler = new MessageEventHandler(
			_options,
			_loggerMock,
			_channelServiceMock,
			_userServiceMock,
			_discordMessageUtilsMock,
			_weeklyEventServiceMock,
			_mapServiceMock,
			_replayServiceMock,
			_tournamentServiceMock,
			_hallOfFameServiceMock,
			_messageServiceMock
		);
	}

	[TestMethod]
	public void Register_RegistersAllHandlersAndEvents()
	{
		// Act.
		_handler!.Register(_discordClientMock!, Substitute.For<IBotState>());

		// Assert.
		_discordClientMock!.Received(1).MessageCreated += Arg.Any<AsyncEventHandler<DiscordClient, MessageCreateEventArgs>>();
		_discordClientMock!.Received(1).MessageDeleted += Arg.Any<AsyncEventHandler<DiscordClient, MessageDeleteEventArgs>>();
		_discordClientMock!.Received(1).MessageReactionAdded += Arg.Any<AsyncEventHandler<DiscordClient, MessageReactionAddEventArgs>>();
		_discordClientMock!.Received(1).MessageReactionRemoved += Arg.Any<AsyncEventHandler<DiscordClient, MessageReactionRemoveEventArgs>>();
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new MessageEventHandler(null!, _loggerMock!, _channelServiceMock!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
					  new MessageEventHandler(_options!, null!, _channelServiceMock!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
					  new MessageEventHandler(_options!, _loggerMock!, null!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
					  new MessageEventHandler(_options!, _loggerMock!, _channelServiceMock!, null!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
					  new MessageEventHandler(_options!, _loggerMock!, _channelServiceMock!, _userServiceMock!, null!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
					  new MessageEventHandler(_options!, _loggerMock!, _channelServiceMock!, _userServiceMock!, _discordMessageUtilsMock!, null!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
					  new MessageEventHandler(_options!, _loggerMock!, _channelServiceMock!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, null!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
					  new MessageEventHandler(_options!, _loggerMock!, _channelServiceMock!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, null!, _tournamentServiceMock!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
					  new MessageEventHandler(_options!, _loggerMock!, _channelServiceMock!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, null!, _hallOfFameServiceMock!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
					  new MessageEventHandler(_options!, _loggerMock!, _channelServiceMock!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, null!, _messageServiceMock!));
		Assert.ThrowsException<ArgumentNullException>(() =>
					  new MessageEventHandler(_options!, _loggerMock!, _channelServiceMock!, _userServiceMock!, _discordMessageUtilsMock!, _weeklyEventServiceMock!, _mapServiceMock!, _replayServiceMock!, _tournamentServiceMock!, _hallOfFameServiceMock!, null!));
	}
}
