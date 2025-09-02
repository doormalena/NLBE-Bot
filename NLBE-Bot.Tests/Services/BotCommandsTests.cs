namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using NSubstitute;
using WorldOfTanksBlitzApi;
using WorldOfTanksBlitzApi.Interfaces;

[TestClass]
public class BotCommandsTests
{
	private IDiscordClient? _discordClientMock;
	private ILogger<BotCommands>? _loggerMock;
	private IOptions<BotOptions>? _options;
	private IClanService? _clanServiceMock;
	private IBlitzstarsService? _blitzstarsServiceMock;
	private IDiscordMessageUtils? _discordMessageUtilsMock;
	private IBotState? _botStateMock;
	private IAccountsRepository? _accountsRepositoryMock;
	private IClansRepository? _clansRepositoryMock;
	private IChannelService? _channelServiceMock;
	private IUserService? _userServiceMock;
	private IMessageService? _messageServiceMock;
	private IMapService? _mapServiceMock;
	private ITournamentService? _tournamentServiceMock;
	private IHallOfFameService? _hallOfFameServiceMock;
	private IWeeklyEventService? _weeklyEventServiceMock;
	private BotCommands? _commands;
	private IDiscordCommandContext? _ctxMock;
	private IDiscordMessage? _messageMock;
	private IDiscordChannel? _channelMock;
	private IDiscordMember? _memberMock;
	private IDiscordGuild? _guildMock;

	[TestInitialize]
	public void Setup()
	{
		_discordClientMock = Substitute.For<IDiscordClient>();
		_loggerMock = Substitute.For<ILogger<BotCommands>>();
		_options = Options.Create(new BotOptions { ServerId = 12345 });
		_clanServiceMock = Substitute.For<IClanService>();
		_blitzstarsServiceMock = Substitute.For<IBlitzstarsService>();
		_discordMessageUtilsMock = Substitute.For<IDiscordMessageUtils>();
		_botStateMock = Substitute.For<IBotState>();
		_accountsRepositoryMock = Substitute.For<IAccountsRepository>();
		_clansRepositoryMock = Substitute.For<IClansRepository>();
		_channelServiceMock = Substitute.For<IChannelService>();
		_userServiceMock = Substitute.For<IUserService>();
		_messageServiceMock = Substitute.For<IMessageService>();
		_mapServiceMock = Substitute.For<IMapService>();
		_tournamentServiceMock = Substitute.For<ITournamentService>();
		_hallOfFameServiceMock = Substitute.For<IHallOfFameService>();
		_weeklyEventServiceMock = Substitute.For<IWeeklyEventService>();

		_commands = new BotCommands(
			_discordClientMock,
			_loggerMock,
			_options,
			_clanServiceMock,
			_blitzstarsServiceMock,
			_discordMessageUtilsMock,
			_botStateMock,
			_accountsRepositoryMock,
			_clansRepositoryMock,
			_channelServiceMock,
			_userServiceMock,
			_messageServiceMock,
			_mapServiceMock,
			_tournamentServiceMock,
			_hallOfFameServiceMock,
			_weeklyEventServiceMock
		);

		// Mock Discord context
		_ctxMock = Substitute.For<IDiscordCommandContext>();
		_messageMock = Substitute.For<IDiscordMessage>();
		_channelMock = Substitute.For<IDiscordChannel>();
		_memberMock = Substitute.For<IDiscordMember>();
		_guildMock = Substitute.For<IDiscordGuild>();
		_guildMock.Id.Returns(_options!.Value.ServerId);
		_guildMock.Name.Returns("TestGuild");

		_ctxMock.Message.Returns(_messageMock);
		_ctxMock.Channel.Returns(_channelMock);
		_ctxMock.Member.Returns(_memberMock);
		_ctxMock.Guild.Returns(_guildMock);
	}

	[TestMethod]
	public async Task HandleBonusCode_ShouldSendBonusCodeMessage()
	{
		// Arrange.
		IDiscordCommand commandMock = Substitute.For<IDiscordCommand>();
		commandMock.Name.Returns("Bonuscode");
		_ctxMock!.Command.Returns(commandMock);

		// Act.
		await _commands!.HandleBonusCode(_ctxMock!);

		// Assert.
		await _messageServiceMock!.Received(1).ConfirmCommandExecuting(_messageMock!);
		await _messageServiceMock!.Received(1).SendMessage(
			_channelMock!,
			_memberMock,
			"TestGuild",
			Arg.Is<string>(s => s.Contains("https://eu.wargaming.net/shop/redeem/"))
		);
		await _messageServiceMock!.Received(1).ConfirmCommandExecuted(_messageMock!);
	}
}
