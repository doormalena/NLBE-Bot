namespace NLBE_Bot.Tests.Jobs;

using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Jobs;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;

[TestClass]
public class AnnounceWeeklyWinnerJobTests
{
	private IWeeklyEventService? _weeklyEventServiceMock;
	private IChannelService? _channelServiceMock;
	private IErrorHandler? _errorHandlerMock;
	private IBotState? _botStateMock;
	private ILogger<AnnounceWeeklyWinnerJob>? _loggerMock;
	private AnnounceWeeklyWinnerJob? _job;

	[TestInitialize]
	public void Setup()
	{
		_weeklyEventServiceMock = Substitute.For<IWeeklyEventService>();
		_channelServiceMock = Substitute.For<IChannelService>();
		_errorHandlerMock = Substitute.For<IErrorHandler>();
		_botStateMock = Substitute.For<IBotState>();
		_loggerMock = Substitute.For<ILogger<AnnounceWeeklyWinnerJob>>();

		_job = new(_weeklyEventServiceMock, _channelServiceMock, _errorHandlerMock, _botStateMock, _loggerMock);
	}

	[TestMethod]
	public async Task Execute_AnnouncesWeeklyWinner_WhenMondayAfter14_AndNotAnnouncedThisWeek()
	{
		// Arrange.
		DateTime monday14 = new(2025, 6, 23, 14, 0, 0, DateTimeKind.Local); // Monday, 14:00, last announcement was a week ago.
		DateTime lastAnnouncement = monday14.AddDays(-7);
		_botStateMock!.LastWeeklyWinnerAnnouncement.Returns(lastAnnouncement);
		_weeklyEventServiceMock!.ReadWeeklyEvent().Returns(Task.CompletedTask);

		// Act.
		await _job!.Execute(monday14);

		// Assert.
		await _weeklyEventServiceMock!.Received(1).ReadWeeklyEvent();
		_botStateMock.Received().LastWeeklyWinnerAnnouncement = monday14;
	}

	[TestMethod]
	public async Task Execute_SendsMessage_WhenChannelIsValid()
	{
		// Arrange.
		IDiscordChannel channelMock = Substitute.For<IDiscordChannel>();
		IDiscordGuild guildMock = Substitute.For<IDiscordGuild>();
		channelMock.Guild.Returns(guildMock);
		_channelServiceMock!.GetBotTestChannel().Returns(channelMock);

		// Setup WeeklyEventService to return a valid WeeklyEvent with a Most_damage item and player.
		WeeklyEventItem mostDmgItem = new(WeeklyEventType.Most_damage)
		{
			Player = "TestPlayer"
		};
		WeeklyEvent weeklyEvent = new("TestTank", [mostDmgItem])
		{
			StartDate = DateTime.Now.AddDays(-7)
		};
		_weeklyEventServiceMock!.WeeklyEvent.Returns(weeklyEvent);


		_weeklyEventServiceMock.WeHaveAWinner(guildMock, mostDmgItem, "TestTank").Returns(Task.CompletedTask); // Make WeHaveAWinner a no-op.

		_botStateMock!.LastWeeklyWinnerAnnouncement.Returns((DateTime?) null); // Set up state to trigger announcement.

		// Act.
		await _job!.Execute(DateTime.Now.Date.AddDays(-(int) DateTime.Now.DayOfWeek + (int) DayOfWeek.Monday).AddHours(15)); // Monday after 14:00.

		// Assert.
		await channelMock.Received().SendMessageAsync(Arg.Any<string>());
	}

	[TestMethod]
	public async Task Execute_DoesNotAnnounceWeeklyWinner_WhenNotMondayAfter14()
	{
		// Arrange.
		DateTime tuesday14 = new(2025, 6, 24, 14, 0, 0, DateTimeKind.Local); // Tuesday, 14:00, last announcement was yesterday.
		DateTime lastAnnouncement = tuesday14.AddDays(-1);
		_botStateMock!.LastWeeklyWinnerAnnouncement.Returns(lastAnnouncement);

		// Act.
		await _job!.Execute(tuesday14);

		// Assert.
		await _weeklyEventServiceMock!.DidNotReceive().ReadWeeklyEvent();
		_botStateMock.DidNotReceive().LastWeeklyWinnerAnnouncement = Arg.Any<DateTime>();
	}

	[TestMethod]
	public async Task Execute_DoesNotAnnounce_WhenMondayBefore14()
	{
		// Arrange.
		DateTime mondayBefore14 = new(2025, 6, 23, 13, 59, 0, DateTimeKind.Local); // Monday, 13:59.
		_botStateMock!.LastWeeklyWinnerAnnouncement.Returns((DateTime?) null);

		// Act.
		await _job!.Execute(mondayBefore14);

		// Assert.
		await _weeklyEventServiceMock!.DidNotReceive().ReadWeeklyEvent();
		_botStateMock.DidNotReceive().LastWeeklyWinnerAnnouncement = Arg.Any<DateTime>();
	}

	[TestMethod]
	public async Task Execute_DoesNotAnnounce_WhenAlreadyAnnouncedToday()
	{
		// Arrange.
		DateTime monday15 = new(2025, 6, 23, 15, 0, 0, DateTimeKind.Local); // Monday, 15:00, last announcement is today.
		_botStateMock!.LastWeeklyWinnerAnnouncement.Returns(monday15.Date);

		// Act.
		await _job!.Execute(monday15);

		// Assert.
		await _weeklyEventServiceMock!.DidNotReceive().ReadWeeklyEvent();
		_botStateMock.DidNotReceive().LastWeeklyWinnerAnnouncement = Arg.Any<DateTime>();
	}

	[TestMethod]
	public async Task Execute_Announces_WhenLastAnnouncementIsNull()
	{
		// Arrange.
		DateTime monday15 = new(2025, 6, 23, 15, 0, 0, DateTimeKind.Local); // Monday, 15:00, last announcement is null.
		_botStateMock!.LastWeeklyWinnerAnnouncement.Returns((DateTime?) null);

		// Act.
		await _job!.Execute(monday15);

		// Assert.
		await _weeklyEventServiceMock!.Received(1).ReadWeeklyEvent();
		_botStateMock.Received().LastWeeklyWinnerAnnouncement = monday15;
	}

	[TestMethod]
	public async Task Execute_HandlesException()
	{
		// Arrange.
		DateTime monday15 = new(2025, 6, 23, 15, 0, 0, DateTimeKind.Local); // Monday, 15:00, last announcement is null.
		DateTime monday15Minus1Day = monday15.AddDays(-1);
		_botStateMock!.LastWeeklyWinnerAnnouncement.Returns(monday15Minus1Day);
		_weeklyEventServiceMock!.ReadWeeklyEvent().Throws(new Exception("Test exception"));

		// Act.
		await _job!.Execute(monday15);

		// Assert.
		await _errorHandlerMock!.Received().HandleErrorAsync(Arg.Any<string>(), Arg.Any<Exception>());
		Assert.AreEqual(_botStateMock!.LastWeeklyWinnerAnnouncement, monday15Minus1Day);
	}

	[TestMethod]
	public async Task Execute_DoesNothing_WhenChannelIsNull()
	{
		// Arrange.
		DateTime monday15 = new(2025, 6, 23, 15, 0, 0, DateTimeKind.Local);
		_botStateMock!.LastWeeklyWinnerAnnouncement.Returns((DateTime?) null);
		_channelServiceMock!.GetBotTestChannel().Returns((IDiscordChannel?) null);

		// Act.
		await _job!.Execute(monday15);

		// Assert.
		await _weeklyEventServiceMock!.DidNotReceive().ReadWeeklyEvent();
		await _errorHandlerMock!.DidNotReceive().HandleErrorAsync(Arg.Any<string>(), Arg.Any<Exception>());
		_loggerMock!.Received().Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("Could not find the bot test channel")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new AnnounceWeeklyWinnerJob(null, _channelServiceMock, _errorHandlerMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new AnnounceWeeklyWinnerJob(_weeklyEventServiceMock, null, _errorHandlerMock, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new AnnounceWeeklyWinnerJob(_weeklyEventServiceMock, _channelServiceMock, null, _botStateMock, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new AnnounceWeeklyWinnerJob(_weeklyEventServiceMock, _channelServiceMock, _errorHandlerMock, null, _loggerMock));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new AnnounceWeeklyWinnerJob(_weeklyEventServiceMock, _channelServiceMock, _errorHandlerMock, _botStateMock, null));
	}
}
