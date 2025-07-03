namespace NLBE_Bot.Tests.EventHandlers;

using DSharpPlus;
using NLBE_Bot.EventHandlers;
using NLBE_Bot.Interfaces;
using NSubstitute;
using System;

[TestClass]
public class TimedEventHandlersTests
{
	private IUserService? _userService;
	private IWeeklyEventService? _weeklyEventService;
	private IErrorHandler? _errorHandler;
	private IBotState? _botState;
	private TimedEventHandler? _handler;

	[TestInitialize]
	public void Setup()
	{
		_userService = Substitute.For<IUserService>();
		_weeklyEventService = Substitute.For<IWeeklyEventService>();
		_errorHandler = Substitute.For<IErrorHandler>();
		_botState = Substitute.For<IBotState>();

		_handler = new(_userService, _weeklyEventService, _errorHandler, _botState);
	}

	[TestMethod]
	public async Task Execute_DoesNothing_WhenIgnoreEvents()
	{
		// Arrange.
		DateTime now = DateTime.Now;
		_botState!.IgnoreEvents.Returns(true);

		// Act.
		await _handler.Execute(now);

		// Assert.
		await _userService!.DidNotReceive().UpdateUsers();
		await _weeklyEventService!.DidNotReceive().AnnounceWeeklyWinner();
	}

	[TestMethod]
	public async Task Execute_UpdateUsernames_Updates_WhenNotUpdatedToday()
	{
		// Arrange.
		DateTime now = DateTime.Now;
		DateTime yesterday = DateTime.Now.AddDays(-1);
		_botState!.LasTimeNamesWereUpdated.Returns(yesterday);

		// Act.
		await _handler.Execute(now);

		// Assert.
		await _userService!.Received(1).UpdateUsers();
		_botState.Received().LasTimeNamesWereUpdated = Arg.Is<DateTime>(dt => dt.Date == now.Date);
	}

	[TestMethod]
	public async Task Execute_DoesNotUpdateUsernames_WhenAlreadyUpdatedToday()
	{
		// Arrange.
		DateTime now = DateTime.Now;
		_botState!.LasTimeNamesWereUpdated.Returns(now);

		// Act.
		await _handler.Execute(now);

		// Assert
		await _userService!.DidNotReceive().UpdateUsers();
		_botState.DidNotReceive().LasTimeNamesWereUpdated = Arg.Any<DateTime>();
	}

	[TestMethod]
	public async Task Execute_AnnouncesWeeklyWinner_WhenMondayAfter14_AndNotAnnouncedThisWeek()
	{
		// Arrange: Monday, 14:00, last announcement was a week ago
		DateTime monday14 = new(2025, 6, 23, 14, 0, 0, DateTimeKind.Local); // Monday 14:00
		DateTime lastAnnouncement = monday14.AddDays(-7);
		_botState!.LastWeeklyWinnerAnnouncement.Returns(lastAnnouncement);

		// Act.
		await _handler.Execute(monday14);

		// Assert.
		await _weeklyEventService!.Received(1).AnnounceWeeklyWinner();
		_botState.Received().LastWeeklyWinnerAnnouncement = monday14;
	}

	[TestMethod]
	public async Task Execute_DoesNotAnnounceWeeklyWinner_WhenNotMondayAfter14()
	{
		// Arrange: Tuesday, 14:00, last announcement was yesterday
		DateTime tuesday14 = new(2025, 6, 24, 14, 0, 0, DateTimeKind.Local); // Tuesday 14:00
		DateTime lastAnnouncement = tuesday14.AddDays(-1);
		_botState!.LastWeeklyWinnerAnnouncement.Returns(lastAnnouncement);

		// Act.
		await _handler.Execute(tuesday14);

		// Assert.
		await _weeklyEventService!.DidNotReceive().AnnounceWeeklyWinner();
		_botState.DidNotReceive().LastWeeklyWinnerAnnouncement = Arg.Any<DateTime>();
	}

	[TestMethod]
	public async Task Execute_UpdateUsernames_HandlesException()
	{
		// Arrange.
		DateTime yesterday = DateTime.Now.AddDays(-1);
		DateTime now = DateTime.Now;
		_botState!.LasTimeNamesWereUpdated.Returns(yesterday);
		_userService!.UpdateUsers().Returns(x => { throw new Exception("fail"); });

		// Act.
		await _handler.Execute(now);

		// Assert.
		await _errorHandler!.Received().HandleErrorAsync(Arg.Is<string>(s => s.Contains("ERROR updating users")), Arg.Any<Exception>());
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new TimedEventHandler(null, _weeklyEventService, _errorHandler, _botState));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new TimedEventHandler(_userService, null, _errorHandler, _botState));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new TimedEventHandler(_userService, _weeklyEventService, null, _botState));
		Assert.ThrowsException<ArgumentNullException>(() =>
			new TimedEventHandler(_userService, _weeklyEventService, _errorHandler, null));
	}
}
