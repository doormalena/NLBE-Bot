namespace NLBE_Bot.Tests.Jobs;

using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Jobs;
using NSubstitute;
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
		// Arrange: Monday, 14:00, last announcement was a week ago
		DateTime monday14 = new(2025, 6, 23, 14, 0, 0, DateTimeKind.Local); // Monday 14:00
		DateTime lastAnnouncement = monday14.AddDays(-7);
		_botStateMock!.LastWeeklyWinnerAnnouncement.Returns(lastAnnouncement);

		// Act.
		await _job!.Execute(monday14);

		// Assert.
		await _weeklyEventServiceMock!.Received(1).ReadWeeklyEvent();
		_botStateMock.Received().LastWeeklyWinnerAnnouncement = monday14;
	}

	[TestMethod]
	public async Task Execute_DoesNotAnnounceWeeklyWinner_WhenNotMondayAfter14()
	{
		// Arrange: Tuesday, 14:00, last announcement was yesterday
		DateTime tuesday14 = new(2025, 6, 24, 14, 0, 0, DateTimeKind.Local); // Tuesday 14:00
		DateTime lastAnnouncement = tuesday14.AddDays(-1);
		_botStateMock!.LastWeeklyWinnerAnnouncement.Returns(lastAnnouncement);

		// Act.
		await _job!.Execute(tuesday14);

		// Assert.
		await _weeklyEventServiceMock!.DidNotReceive().ReadWeeklyEvent();
		_botStateMock.DidNotReceive().LastWeeklyWinnerAnnouncement = Arg.Any<DateTime>();
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
