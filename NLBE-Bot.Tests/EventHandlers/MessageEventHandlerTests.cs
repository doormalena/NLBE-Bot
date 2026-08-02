namespace NLBE_Bot.Tests.EventHandlers;

using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.EventHandlers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using NSubstitute;
using System.Globalization;
using WorldOfTanksBlitzApi.Interfaces;
using WorldOfTanksBlitzApi.Models;

[TestClass]
public class MessageEventHandlerTests
{
	private IDiscordClient _discordClientMock = null!;
	private ILogger<MessageEventHandler> _loggerMock = null!;
	private IOptions<BotOptions> _options = null!;
	private IVehiclesRepository _vehiclesRepositoryMock = null!;
	private IUserService _userServiceMock = null!;
	private IDiscordMessageUtils _discordMessageUtilsMock = null!;
	private IWeeklyEventService _weeklyEventServiceMock = null!;
	private IMapService _mapServiceMock = null!;
	private IReplayService _replayServiceMock = null!;
	private ITournamentService _tournamentServiceMock = null!;
	private IHallOfFameService _hallOfFameServiceMock = null!;
	private IMessageService _messageServiceMock = null!;
	private IDiscordGuild _guildMock = null!;
	private MessageEventHandler _handler = null!;

	private IDiscordChannel _rulesChannelMock = null!;
	private IDiscordChannel _generalChannelMock = null!;
	private IDiscordChannel _tournamentSignUpChannelMock = null!;
	private IDiscordChannel _replayChannelMock = null!;

	#region TestInitialize

	[TestInitialize]
	public void Setup()
	{
		BotOptions botOptions = new()
		{
			ChannelIds = new()
			{
				Rules = 999UL,
				General = 888UL,
				TournamentSignUp = 777UL,
				WeeklyEvent = 666UL,
				MasteryReplays = 555UL,
				ReplayResults = 444UL,
				BotTest = 333UL
			},
			RoleIds = new()
			{
				MustReadRules = 111UL,
				Members = 222UL
			},
			ServerId = 12345
		};

		_options = Options.Create(botOptions);

		_rulesChannelMock = Substitute.For<IDiscordChannel>();
		_rulesChannelMock.Id.Returns(botOptions.ChannelIds.Rules);

		_generalChannelMock = Substitute.For<IDiscordChannel>();
		_generalChannelMock.Id.Returns(botOptions.ChannelIds.General);

		_replayChannelMock = Substitute.For<IDiscordChannel>();
		_replayChannelMock.Id.Returns(botOptions.ChannelIds.ReplayResults);

		_tournamentSignUpChannelMock = Substitute.For<IDiscordChannel>();
		_tournamentSignUpChannelMock.Id.Returns(botOptions.ChannelIds.TournamentSignUp);

		_guildMock = Substitute.For<IDiscordGuild>();
		_guildMock.Id.Returns(botOptions.ServerId);

		_guildMock.GetChannel(botOptions.ChannelIds.Rules).Returns(_rulesChannelMock);
		_guildMock.GetChannel(botOptions.ChannelIds.General).Returns(_generalChannelMock);
		_guildMock.GetChannel(botOptions.ChannelIds.TournamentSignUp).Returns(_tournamentSignUpChannelMock);
		_guildMock.GetChannel(botOptions.ChannelIds.ReplayResults).Returns(_replayChannelMock);

		_discordClientMock = Substitute.For<IDiscordClient>();
		_discordClientMock.GetGuildAsync(botOptions.ServerId).Returns(Task.FromResult(_guildMock));

		_loggerMock = Substitute.For<ILogger<MessageEventHandler>>();
		_vehiclesRepositoryMock = Substitute.For<IVehiclesRepository>();
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
			_vehiclesRepositoryMock,
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

	#endregion

	#region Register

	[TestMethod]
	public void Register_RegistersAllEvents()
	{
		// Act.
		_handler.Register(_discordClientMock, Substitute.For<IBotState>());

		// Assert.
		_discordClientMock.Received(1).MessageCreated += Arg.Any<AsyncEventHandler<DiscordClient, MessageCreateEventArgs>>();
		_discordClientMock.Received(1).MessageDeleted += Arg.Any<AsyncEventHandler<DiscordClient, MessageDeleteEventArgs>>();
		_discordClientMock.Received(1).MessageReactionAdded += Arg.Any<AsyncEventHandler<DiscordClient, MessageReactionAddEventArgs>>();
		_discordClientMock.Received(1).MessageReactionRemoved += Arg.Any<AsyncEventHandler<DiscordClient, MessageReactionRemoveEventArgs>>();
	}

	#endregion

	#region HandleMessageCreated

	[TestMethod]
	public async Task HandleMessageCreated_ProcessesMasteryReplay_CallsHallOfFame()
	{
		// Arrange
		_handler.Register(_discordClientMock, Substitute.For<IBotState>());

		IDiscordChannel weeklyEventChannel = Substitute.For<IDiscordChannel>();
		weeklyEventChannel.Id.Returns(_options.Value.ChannelIds.WeeklyEvent);
		IDiscordChannel masteryChannel = Substitute.For<IDiscordChannel>();
		masteryChannel.Id.Returns(_options.Value.ChannelIds.MasteryReplays);
		IDiscordChannel replayChannel = Substitute.For<IDiscordChannel>();
		replayChannel.Id.Returns(_options.Value.ChannelIds.ReplayResults);
		IDiscordChannel botTestChannel = Substitute.For<IDiscordChannel>();
		botTestChannel.Id.Returns(_options.Value.ChannelIds.BotTest);

		_guildMock.GetChannel(_options.Value.ChannelIds.WeeklyEvent).Returns(weeklyEventChannel);
		_guildMock.GetChannel(_options.Value.ChannelIds.MasteryReplays).Returns(masteryChannel);
		_guildMock.GetChannel(_options.Value.ChannelIds.ReplayResults).Returns(replayChannel);
		_guildMock.GetChannel(_options.Value.ChannelIds.BotTest).Returns(botTestChannel);

		IDiscordUser author = Substitute.For<IDiscordUser>();
		author.IsBot.Returns(false);
		author.Id.Returns(123UL);

		IDiscordChannel channel = masteryChannel;

		IDiscordAttachment replayAttachment = Substitute.For<IDiscordAttachment>();
		replayAttachment.FileName.Returns("battle.wotbreplay");

		IDiscordMessage message = Substitute.For<IDiscordMessage>();
		message.Attachments.Returns([replayAttachment]);

		IDiscordRole nlbeRole = Substitute.For<IDiscordRole>();
		nlbeRole.Id.Returns(Constants.NLBE_ROLE);
		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.Roles.Returns([nlbeRole]);
		_guildMock.GetMemberAsync(author.Id).Returns(member);
		_guildMock.GetRole(Constants.NLBE_ROLE).Returns(nlbeRole);

		Tuple<string, IDiscordMessage?> hofResult = new("ok", null);
		_hallOfFameServiceMock.Handle(
			Arg.Any<string>(),
			replayAttachment,
			channel,
			_guildMock,
			member,
			message).Returns(hofResult);

		// Act
		await _handler.HandleMessageCreated(_guildMock, channel, message, author);

		// Assert
		await _hallOfFameServiceMock.Received(1).Handle(
			Arg.Any<string>(),
			replayAttachment,
			channel,
			_guildMock,
			member,
			message);
		await _hallOfFameServiceMock.Received(1).HofAfterUpload(hofResult, message);
	}

	[TestMethod]
	public async Task HandleMessageCreated_ProcessesReplayResults_CallsReplayAndEmbed()
	{
		// Arrange
		_handler.Register(_discordClientMock, Substitute.For<IBotState>());

		IDiscordChannel weeklyEventChannel = Substitute.For<IDiscordChannel>();
		weeklyEventChannel.Id.Returns(_options.Value.ChannelIds.WeeklyEvent);
		IDiscordChannel masteryChannel = Substitute.For<IDiscordChannel>();
		masteryChannel.Id.Returns(_options.Value.ChannelIds.MasteryReplays);
		IDiscordChannel replayChannel = Substitute.For<IDiscordChannel>();
		replayChannel.Id.Returns(_options.Value.ChannelIds.ReplayResults);
		IDiscordChannel botTestChannel = Substitute.For<IDiscordChannel>();
		botTestChannel.Id.Returns(_options.Value.ChannelIds.BotTest);

		_guildMock.GetChannel(_options.Value.ChannelIds.WeeklyEvent).Returns(weeklyEventChannel);
		_guildMock.GetChannel(_options.Value.ChannelIds.MasteryReplays).Returns(masteryChannel);
		_guildMock.GetChannel(_options.Value.ChannelIds.ReplayResults).Returns(replayChannel);
		_guildMock.GetChannel(_options.Value.ChannelIds.BotTest).Returns(botTestChannel);

		IDiscordUser author = Substitute.For<IDiscordUser>();
		author.IsBot.Returns(false);
		author.Id.Returns(123UL);

		IDiscordChannel channel = replayChannel;

		IDiscordAttachment replayAttachment = Substitute.For<IDiscordAttachment>();
		replayAttachment.FileName.Returns("battle.wotbreplay");

		IDiscordMessage message = Substitute.For<IDiscordMessage>();
		message.Attachments.Returns([replayAttachment]);

		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.Roles.Returns([]);
		_guildMock.GetMemberAsync(author.Id).Returns(member);

		WotInspectorBattle battle = new();
		_replayServiceMock.GetReplayInfo(Arg.Any<string>(), replayAttachment).Returns(battle);
		_weeklyEventServiceMock.GetStringForWeeklyEvent(_guildMock, battle).Returns("event");
		_mapServiceMock.GetAllMaps(channel.Guild).Returns([]);
		_replayServiceMock.GetDescriptionForReplay(_guildMock, battle, -1, "event").Returns("desc");

		// Act
		await _handler.HandleMessageCreated(_guildMock, channel, message, author);

		// Assert
		await _messageServiceMock.Received(1).ConfirmCommandExecuting(message);
		await _replayServiceMock.Received(1).GetReplayInfo(Arg.Any<string>(), replayAttachment);
		await _messageServiceMock.Received(1).CreateEmbed(
			channel,
			Arg.Is<EmbedOptions>(e => e!.IsForReplay && e.Title == "Resultaat"),
			message);
		await _messageServiceMock.Received(1).ConfirmCommandExecuted(message);
	}

	[TestMethod]
	public async Task HandleMessageCreated_ReplayResults_UsesVehicleRepository()
	{
		// Arrange
		_handler.Register(_discordClientMock, Substitute.For<IBotState>());
		IDiscordUser author = Substitute.For<IDiscordUser>();
		author.IsBot.Returns(false);
		author.Id.Returns(123UL);

		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.Roles.Returns([]);
		_guildMock.GetMemberAsync(author.Id).Returns(member);

		IDiscordAttachment attachment = Substitute.For<IDiscordAttachment>();
		attachment.FileName.Returns("battle.wotbreplay");

		IDiscordMessage msg = Substitute.For<IDiscordMessage>();
		msg.Attachments.Returns([attachment]);

		WotInspectorBattle battle = new()
		{
			VehicleDescr = 777 // tank ID
		};

		_replayServiceMock.GetReplayInfo(Arg.Any<string>(), attachment).Returns(battle);

		_vehiclesRepositoryMock.GetByIdAsync(777)
			.Returns(new WotbVehicle { TankId = 777, Name = "IS-7" });

		_weeklyEventServiceMock.GetStringForWeeklyEvent(_guildMock, battle).Returns("event");
		_mapServiceMock.GetAllMaps(_replayChannelMock.Guild).Returns([]);
		_replayServiceMock.GetDescriptionForReplay(_guildMock, battle, -1, "event")
			.Returns("Replay description with IS-7");

		// Act
		await _handler.HandleMessageCreated(_guildMock, _replayChannelMock, msg, author);

		// Assert
		await _messageServiceMock.Received(1).CreateEmbed(
			_replayChannelMock,
			Arg.Is<EmbedOptions>(e => e!.Description.Contains("IS-7")),
			msg);
	}

	[TestMethod]
	public async Task HandleMessageCreated_WeeklyEventDM_SelectsExactTank_AndCreatesNewEvent()
	{
		// Arrange
		IBotState botState = Substitute.For<IBotState>();
		botState.WeeklyEventWinner = new WeeklyEventWinner
		{
			UserId = 123,
			LastEventDate = DateTime.UnixEpoch
		};
		_handler.Register(_discordClientMock, botState);

		IDiscordChannel dmChannel = Substitute.For<IDiscordChannel>();
		dmChannel.IsPrivate.Returns(true);

		IDiscordMessage msg = Substitute.For<IDiscordMessage>();
		msg.Author.IsBot.Returns(false);
		msg.CreationTimestamp.Returns(DateTimeOffset.UtcNow);
		msg.Content.Returns("Panther");

		Dictionary<string, WotbVehicle> vehicles = new()
		{
			{ "101", new WotbVehicle { TankId = 101, Name = "Panther" } },
			{ "102", new WotbVehicle { TankId = 102, Name = "Panzer" } }
		};
		_vehiclesRepositoryMock.GetAllAsync().Returns(vehicles);

		IDiscordChannel? weeklyEventChannel = _guildMock!.GetChannel(_options.Value.ChannelIds.WeeklyEvent);

		// Act
		await _handler.HandleMessageCreated(_guildMock, dmChannel, msg, msg.Author);

		// Assert
		await _weeklyEventServiceMock.Received(1)
			.CreateNewWeeklyEvent("Panther", weeklyEventChannel!);
	}
	[TestMethod]
	public async Task HandleMessageCreated_WeeklyEventDM_TooManyMatches_ShowsWarning()
	{
		// Arrange
		IBotState botState = Substitute.For<IBotState>();
		botState.WeeklyEventWinner = new WeeklyEventWinner
		{
			UserId = 123,
			LastEventDate = DateTime.UnixEpoch
		};
		_handler.Register(_discordClientMock, botState);

		IDiscordChannel dmChannel = Substitute.For<IDiscordChannel>();
		dmChannel.IsPrivate.Returns(true);

		IDiscordMessage msg = Substitute.For<IDiscordMessage>();
		msg.Author.IsBot.Returns(false);
		msg.CreationTimestamp.Returns(DateTimeOffset.UtcNow);
		msg.Content.Returns("Pan");

		Dictionary<string, WotbVehicle> vehicles = [];
		for (int i = 0; i < 30; i++)
		{
			vehicles.Add(i.ToString(), new WotbVehicle { Name = "Panther" });
		}

		_vehiclesRepositoryMock.GetAllAsync().Returns(vehicles);

		// Act
		await _handler.HandleMessageCreated(_guildMock, dmChannel, msg, msg.Author);

		// Assert
		await dmChannel.Received(1).SendMessageAsync(
			Arg.Is<string>(s => s!.Contains("te veel resultaten")));
	}

	[TestMethod]
	public async Task HandleMessageCreated_WeeklyEventDM_NoMatches_ShowsNotFound()
	{
		// Arrange
		IBotState botState = Substitute.For<IBotState>();
		botState.WeeklyEventWinner = new WeeklyEventWinner
		{
			UserId = 123,
			LastEventDate = DateTime.UnixEpoch
		};
		_handler.Register(_discordClientMock, botState);

		IDiscordChannel dmChannel = Substitute.For<IDiscordChannel>();
		dmChannel.IsPrivate.Returns(true);

		IDiscordMessage msg = Substitute.For<IDiscordMessage>();
		msg.Author.IsBot.Returns(false);
		msg.CreationTimestamp.Returns(DateTimeOffset.UtcNow);
		msg.Content.Returns("UnknownTank");
		_vehiclesRepositoryMock.GetAllAsync().Returns([]);

		// Act
		await _handler.HandleMessageCreated(_guildMock, dmChannel, msg, msg.Author);

		// Assert
		await dmChannel.Received(1).SendMessageAsync(
			Arg.Is<string>(s => s!.Contains("kon niet gevonden")));
	}

	[TestMethod]
	public async Task HandleMessageCreated_WeeklyEventDM_MultipleMatches_ShowsList()
	{
		// Arrange
		IBotState botState = Substitute.For<IBotState>();
		botState.WeeklyEventWinner = new WeeklyEventWinner
		{
			UserId = 123,
			LastEventDate = DateTime.UnixEpoch
		};
		_handler.Register(_discordClientMock, botState);

		IDiscordChannel dmChannel = Substitute.For<IDiscordChannel>();
		dmChannel.IsPrivate.Returns(true);

		IDiscordMessage msg = Substitute.For<IDiscordMessage>();
		msg.Author.IsBot.Returns(false);
		msg.CreationTimestamp.Returns(DateTimeOffset.UtcNow);
		msg.Content.Returns("Pan");

		_vehiclesRepositoryMock.GetAllAsync().Returns(new Dictionary<string, WotbVehicle>
		{
			{ "101", new WotbVehicle { TankId = 101, Name = "Panther" } },
			{ "102", new WotbVehicle { TankId = 102, Name = "Panzer" } }
		});

		// Act
		await _handler.HandleMessageCreated(_guildMock, dmChannel, msg, msg.Author);

		// Assert
		await dmChannel.Received(2).SendMessageAsync(Arg.Any<string>());
	}

	#endregion

	#region HandleMessageDeleted

	[TestMethod]
	public async Task HandleMessageDeleted_DeletesLogMessage_WhenTournamentChannel()
	{
		// Arrange
		_handler.Register(_discordClientMock, Substitute.For<IBotState>());

		IDiscordChannel tournamentChannel = Substitute.For<IDiscordChannel>();
		tournamentChannel.Id.Returns(_options.Value.ChannelIds.TournamentSignUp);
		IDiscordChannel logChannel = Substitute.For<IDiscordChannel>();

		_guildMock.GetChannel(_options.Value.ChannelIds.TournamentSignUp).Returns(tournamentChannel);
		_guildMock.GetChannel(_options.Value.ChannelIds.Log).Returns(logChannel);

		DateTime now = DateTime.Now;
		IDiscordMessage deletedMessage = Substitute.For<IDiscordMessage>();
		deletedMessage.Timestamp.Returns(new DateTimeOffset(now));

		IDiscordMessage logMsg = Substitute.For<IDiscordMessage>();
		logMsg.Content.Returns($"{now.ToString("dd-MM-yyyy HH:mm:ss", new CultureInfo("nl-NL"))}|rest");
		logMsg.DeleteAsync().Returns(Task.CompletedTask);

		logChannel.GetMessagesAsync(100).Returns([logMsg]);

		// Act
		await _handler.HandleMessageDeleted(deletedMessage, _guildMock, tournamentChannel);

		// Assert
		await logMsg.Received(1).DeleteAsync();
	}

	#endregion

	#region HandleMessageReactionAdded

	[TestMethod]
	public async Task HandleMessageReactionAdded_TournamentSignUp_CallsGenerateLogMessage()
	{
		// Arrange
		_handler.Register(_discordClientMock, Substitute.For<IBotState>());

		IDiscordChannel tournamentChannel = Substitute.For<IDiscordChannel>();
		tournamentChannel.Id.Returns(_options.Value.ChannelIds.TournamentSignUp);
		IDiscordChannel rulesChannel = Substitute.For<IDiscordChannel>();
		rulesChannel.Id.Returns(_options.Value.ChannelIds.Rules);
		IDiscordChannel generalChannel = Substitute.For<IDiscordChannel>();
		generalChannel.Id.Returns(_options.Value.ChannelIds.General);
		IDiscordRole membersRole = Substitute.For<IDiscordRole>();

		_guildMock.GetChannel(_options.Value.ChannelIds.TournamentSignUp).Returns(tournamentChannel);
		_guildMock.GetChannel(_options.Value.ChannelIds.Rules).Returns(rulesChannel);
		_guildMock.GetChannel(_options.Value.ChannelIds.General).Returns(generalChannel);
		_guildMock.GetRole(_options.Value.RoleIds.Members).Returns(membersRole);

		IDiscordMessage message = Substitute.For<IDiscordMessage>();
		IDiscordUser user = Substitute.For<IDiscordUser>();
		user.IsBot.Returns(false);
		user.Id.Returns(123UL);

		IDiscordEmoji emoji = Substitute.For<IDiscordEmoji>();
		emoji.Name.Returns("ok");
		_discordMessageUtilsMock.GetDiscordEmoji("ok").Returns(emoji);

		// Act
		await _handler.HandleMessageReactionAdded(message, _guildMock, tournamentChannel, user, emoji);

		// Assert
		await _tournamentServiceMock.Received(1).GenerateLogMessage(
			Arg.Any<IDiscordMessage>(),
			tournamentChannel,
			user.Id,
			Arg.Any<string>());
	}

	[TestMethod]
	public async Task HandleMessageReactionAdded_NonRulesEmoji_DeletesReactionsFromNonBotUsers()
	{
		// Arrange
		_handler.Register(_discordClientMock, Substitute.For<IBotState>());

		IDiscordChannel tournamentChannel = Substitute.For<IDiscordChannel>();
		tournamentChannel.Id.Returns(_options.Value.ChannelIds.TournamentSignUp);
		IDiscordChannel rulesChannel = Substitute.For<IDiscordChannel>();
		rulesChannel.Id.Returns(_options.Value.ChannelIds.Rules);
		IDiscordChannel generalChannel = Substitute.For<IDiscordChannel>();
		generalChannel.Id.Returns(_options.Value.ChannelIds.General);
		IDiscordRole membersRole = Substitute.For<IDiscordRole>();

		_guildMock.GetChannel(_options.Value.ChannelIds.TournamentSignUp).Returns(tournamentChannel);
		_guildMock.GetChannel(_options.Value.ChannelIds.Rules).Returns(rulesChannel);
		_guildMock.GetChannel(_options.Value.ChannelIds.General).Returns(generalChannel);
		_guildMock.GetRole(_options.Value.RoleIds.Members).Returns(membersRole);

		IDiscordMessage message = Substitute.For<IDiscordMessage>();
		IDiscordUser user = Substitute.For<IDiscordUser>();
		user.IsBot.Returns(false);

		IDiscordEmoji emoji = Substitute.For<IDiscordEmoji>();
		emoji.GetDiscordName().Returns(":other:");

		IDiscordUser reactingUser = Substitute.For<IDiscordUser>();
		reactingUser.IsBot.Returns(false);
		message.GetReactionsAsync(emoji).Returns([reactingUser]);

		// Act
		await _handler.HandleMessageReactionAdded(message, _guildMock, rulesChannel, user, emoji);

		// Assert
		await message.Received(1).DeleteReactionAsync(emoji, reactingUser);
	}

	#endregion

	#region HandleMessageReactionRemoved

	[TestMethod]
	public async Task HandleMessageReactionRemoved_DoesNotReAdd_WhenUsersRemain()
	{
		// Arrange
		_handler.Register(_discordClientMock, Substitute.For<IBotState>());

		IDiscordChannel tournamentChannel = Substitute.For<IDiscordChannel>();
		tournamentChannel.Id.Returns(_options.Value.ChannelIds.TournamentSignUp);
		IDiscordChannel logChannel = Substitute.For<IDiscordChannel>();

		_guildMock.GetChannel(_options.Value.ChannelIds.TournamentSignUp).Returns(tournamentChannel);
		_guildMock.GetChannel(_options.Value.ChannelIds.Log).Returns(logChannel);

		IDiscordEmoji emoji = Substitute.For<IDiscordEmoji>();
		IDiscordMessage msg = Substitute.For<IDiscordMessage>();
		msg.Id.Returns(123UL);

		IDiscordMessage messageTmp = Substitute.For<IDiscordMessage>();
		IDiscordUser botUser = Substitute.For<IDiscordUser>();
		botUser.Id.Returns(Constants.NLBE_BOT);
		messageTmp.Author.Returns(botUser);
		_tournamentSignUpChannelMock.GetMessageAsync(msg.Id).Returns(messageTmp);

		IDiscordUser remainingUser = Substitute.For<IDiscordUser>();
		msg.GetReactionsAsync(emoji).Returns([remainingUser]);

		logChannel.GetMessagesAsync(100).Returns([]);

		IDiscordUser user = Substitute.For<IDiscordUser>();
		user.Id.Returns(999UL);

		// Act
		await _handler.HandleMessageReactionRemoved(msg, _guildMock, tournamentChannel, user, emoji);

		// Assert
		await msg.DidNotReceive().CreateReactionAsync(emoji);
	}

	[TestMethod]
	public async Task HandleMessageReactionRemoved_IgnoresNonTournamentChannel()
	{
		// Arrange
		_handler.Register(_discordClientMock, Substitute.For<IBotState>());

		IDiscordChannel tournamentChannel = Substitute.For<IDiscordChannel>();
		tournamentChannel.Id.Returns(_options.Value.ChannelIds.TournamentSignUp);
		IDiscordChannel logChannel = Substitute.For<IDiscordChannel>();

		_guildMock.GetChannel(_options.Value.ChannelIds.TournamentSignUp).Returns(tournamentChannel);
		_guildMock.GetChannel(_options.Value.ChannelIds.Log).Returns(logChannel);

		IDiscordEmoji emoji = Substitute.For<IDiscordEmoji>();
		IDiscordMessage msg = Substitute.For<IDiscordMessage>();
		IDiscordChannel otherChannel = Substitute.For<IDiscordChannel>();
		otherChannel.Id.Returns(123UL);

		IDiscordUser user = Substitute.For<IDiscordUser>();

		// Act
		await _handler.HandleMessageReactionRemoved(msg, _guildMock, otherChannel, user, emoji);

		// Assert
		await msg.DidNotReceive().CreateReactionAsync(emoji);
	}

	#endregion
}
