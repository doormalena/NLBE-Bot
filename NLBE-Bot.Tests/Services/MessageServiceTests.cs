namespace NLBE_Bot.Tests.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Services;
using NSubstitute;
using System;
using System.Threading.Tasks;

[TestClass]
public class MessageServiceTests
{
	private IDiscordClient? _discordClientMock;
	private ILogger<MessageService>? _loggerMock;
	private IOptions<BotOptions>? _optionsMock;
	private IBotState? _botStateMock;
	private IChannelService? _channelServiceMock;
	private IDiscordMessageUtils? _discordMessageUtilsMock;
	private IMapService? _mapServiceMock;
	private MessageService? _service;
	private IDiscordChannel? _channelMock;
	private IDiscordMember? _memberMock;
	private IDiscordGuild? _guildMock;
	private IDiscordUser? _userMock;

	[TestInitialize]
	public void Setup()
	{
		_discordClientMock = Substitute.For<IDiscordClient>();
		_loggerMock = Substitute.For<ILogger<MessageService>>();
		_optionsMock = Options.Create(new BotOptions());
		_botStateMock = Substitute.For<IBotState>();
		_channelServiceMock = Substitute.For<IChannelService>();
		_discordMessageUtilsMock = Substitute.For<IDiscordMessageUtils>();
		_mapServiceMock = Substitute.For<IMapService>();

		_service = new MessageService(
			_discordClientMock,
			_loggerMock,
			_optionsMock,
			_botStateMock,
			_channelServiceMock,
			_discordMessageUtilsMock,
			_mapServiceMock
		);

		_channelMock = Substitute.For<IDiscordChannel>();
		_memberMock = Substitute.For<IDiscordMember>();
		_guildMock = Substitute.For<IDiscordGuild>();
		_userMock = Substitute.For<IDiscordUser>();
	}

	[TestMethod]
	public async Task AskQuestion_ShouldThrow_WhenChannelIsNull()
	{
		// Act & Assert.
		await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
			_service!.AskQuestion(null!, _userMock!, _guildMock!, "Q"));
	}

	[TestMethod]
	public async Task AskQuestion_ShouldReturnContent_WhenNotTimedOut()
	{
		// Arrange.
		IDiscordMessage discordMessage = Substitute.For<IDiscordMessage>();
		discordMessage.Content.Returns("Answer");

		IDiscordInteractivityResult<IDiscordMessage> resultMock =
			Substitute.For<IDiscordInteractivityResult<IDiscordMessage>>();
		resultMock.TimedOut.Returns(false);
		resultMock.Result.Returns(discordMessage);

		_channelMock!.GetNextMessageAsync(_userMock!, Arg.Any<TimeSpan>())
					 .Returns(Task.FromResult(resultMock));

		// Act.
		string answer = await _service!.AskQuestion(_channelMock!, _userMock!, _guildMock!, "Q");

		// Assert.
		Assert.AreEqual("Answer", answer);
	}

	[TestMethod]
	public async Task AskQuestion_ShouldCleanChannel_WhenTimedOut_AndMemberIsNull()
	{
		// Arrange.
		IDiscordInteractivityResult<IDiscordMessage> resultMock =
			Substitute.For<IDiscordInteractivityResult<IDiscordMessage>>();
		resultMock.TimedOut.Returns(true);

		_channelMock!.GetNextMessageAsync(_userMock!, Arg.Any<TimeSpan>())
					 .Returns(Task.FromResult(resultMock));
		_guildMock!.GetMemberAsync(_userMock!.Id).Returns((IDiscordMember?) null);

		// Act.
		string answer = await _service!.AskQuestion(_channelMock!, _userMock!, _guildMock!, "Q");

		// Assert.
		Assert.AreEqual(string.Empty, answer);
		await _channelServiceMock!.Received(1).CleanChannelAsync(_channelMock!);
	}

	[TestMethod]
	public async Task AskQuestion_ShouldRemoveMember_WhenTimedOut_AndRemoveSucceeds()
	{

		// Arrange.
		IDiscordInteractivityResult<IDiscordMessage> resultMock =
			Substitute.For<IDiscordInteractivityResult<IDiscordMessage>>();
		resultMock.TimedOut.Returns(true);

		_channelMock!.GetNextMessageAsync(_userMock!, Arg.Any<TimeSpan>())
					 .Returns(Task.FromResult(resultMock));

		IDiscordMember member = Substitute.For<IDiscordMember>();
		_guildMock!.GetMemberAsync(_userMock!.Id).Returns(member);

		// Act.
		string answer = await _service!.AskQuestion(_channelMock!, _userMock!, _guildMock!, "Q");

		// Assert.
		Assert.AreEqual(string.Empty, answer);
		await member.Received(1).RemoveAsync("[New member] No answer");
		await _channelServiceMock!.Received(1).CleanChannelAsync(_channelMock!);
	}

	[TestMethod]
	public async Task AskQuestion_ShouldBanAndUnban_WhenRemoveThrows()
	{
		// Arrange.
		IDiscordInteractivityResult<IDiscordMessage> resultMock =
			Substitute.For<IDiscordInteractivityResult<IDiscordMessage>>();
		resultMock.TimedOut.Returns(true);

		_channelMock!.GetNextMessageAsync(_userMock!, Arg.Any<TimeSpan>())
					 .Returns(Task.FromResult(resultMock));

		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.RemoveAsync(Arg.Any<string>()).Returns(_ => throw new Exception("remove fail"));
		_guildMock!.GetMemberAsync(_userMock!.Id).Returns(member);

		_guildMock.BanMemberAsync(member).Returns(Task.CompletedTask);
		_guildMock.UnbanMemberAsync(_userMock!).Returns(Task.CompletedTask);

		// Act.
		string answer = await _service!.AskQuestion(_channelMock!, _userMock!, _guildMock!, "Q");

		// Assert.
		Assert.AreEqual(string.Empty, answer);
		await _guildMock.Received(1).BanMemberAsync(member);
		await _guildMock.Received(1).UnbanMemberAsync(_userMock!);
		await _channelServiceMock!.Received(1).CleanChannelAsync(_channelMock!);
	}

	[TestMethod]
	public async Task AskQuestion_ShouldLogWarning_WhenBanFails()
	{
		// Arrange.
		IDiscordInteractivityResult<IDiscordMessage> resultMock =
			Substitute.For<IDiscordInteractivityResult<IDiscordMessage>>();
		resultMock.TimedOut.Returns(true);

		_channelMock!.GetNextMessageAsync(_userMock!, Arg.Any<TimeSpan>())
					 .Returns(Task.FromResult(resultMock));

		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.DisplayName.Returns("Name");
		member.Username.Returns("User");
		member.Discriminator.Returns("1234");
		member.RemoveAsync(Arg.Any<string>()).Returns(_ => throw new Exception("remove fail"));
		_guildMock!.GetMemberAsync(_userMock!.Id).Returns(member);

		_guildMock.BanMemberAsync(member).Returns(_ => throw new Exception("ban fail"));

		// Act.
		string answer = await _service!.AskQuestion(_channelMock!, _userMock!, _guildMock!, "Q");

		// Assert.
		Assert.AreEqual(string.Empty, answer);
		_loggerMock!.Received(1).Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>()
		);
		await _channelServiceMock!.Received(1).CleanChannelAsync(_channelMock!);
	}

	[TestMethod]
	public async Task SendMessage_ShouldReturnMessage_WhenNoException()
	{
		// Arrange.
		IDiscordMessage message = Substitute.For<IDiscordMessage>();
		_channelMock!.SendMessageAsync("hello").Returns(Task.FromResult(message));

		// Act.
		IDiscordMessage? result = await _service!.SendMessage(_channelMock!, _memberMock!, "GuildName", "hello");

		// Assert.
		Assert.AreSame(message, result);
	}

	[TestMethod]
	public async Task SendMessage_ShouldCallSayBotNotAuthorized_AndSendPrivateMessage_WhenUnauthorized()
	{
		// Arrange.
		_channelMock!.SendMessageAsync("msg")
			   .Returns<Task<IDiscordMessage>>(_ => throw new Exception("unauthorized"));

		// Create partial substitute so we can verify calls to public methods
		MessageService serviceSub = Substitute.ForPartsOf<MessageService>(
			_discordClientMock!,
			_loggerMock!,
			_optionsMock!,
			_botStateMock!,
			_channelServiceMock!,
			_discordMessageUtilsMock!,
			_mapServiceMock!
		);

		// Act.
		IDiscordMessage? result = await serviceSub.SendMessage(_channelMock!, _memberMock!, "GuildName", "msg");

		// Assert.
		Assert.IsNull(result);
		_loggerMock!.Received().Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v.ToString()!.Contains("Could not send message to channel")),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
		await serviceSub.Received(1).SayBotNotAuthorized(_channelMock!);
		await serviceSub.Received(1).SendPrivateMessage(_memberMock!, "GuildName", "msg");
		await serviceSub.DidNotReceiveWithAnyArgs().SayTooManyCharacters(default!);
	}

	[TestMethod]
	public async Task SendMessage_ShouldCallSayTooManyCharacters_AndSendPrivateMessage_WhenOtherException()
	{
		// Arrange.
		_channelMock!.SendMessageAsync("msg")
			   .Returns<Task<IDiscordMessage>>(_ => throw new Exception("Some other error"));

		// Create partial mock of MessageService so we can verify calls to public methods
		MessageService serviceSub = Substitute.ForPartsOf<MessageService>(
			_discordClientMock!,
			_loggerMock!,
			_optionsMock!,
			_botStateMock!,
			_channelServiceMock!,
			_discordMessageUtilsMock!,
			_mapServiceMock!
		);

		// Act.
		IDiscordMessage? result = await serviceSub.SendMessage(_channelMock!, _memberMock!, "GuildName", "msg");

		// Assert.
		Assert.IsNull(result);
		_loggerMock!.Received().Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v.ToString()!.Contains("Could not send message to channel")),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
		await serviceSub.Received(1).SayTooManyCharacters(_channelMock!);
		await serviceSub.Received(1).SendPrivateMessage(_memberMock!, "GuildName", "msg");
		await serviceSub.DidNotReceiveWithAnyArgs().SayBotNotAuthorized(default!);
	}

	[TestMethod]
	public async Task SendPrivateMessage_ShouldReturnTrue_WhenNoException()
	{
		// Arrange.
		IDiscordMessage message = Substitute.For<IDiscordMessage>();
		_memberMock!.SendMessageAsync("Test message").Returns(Task.FromResult(message));

		// Act.
		IDiscordMessage? result = await _service!.SendPrivateMessage(_memberMock!, "TestGuild", "Test message");

		// Assert.
		Assert.IsNotNull(result);
		_loggerMock!.Received(0).Log(
			   LogLevel.Error,
			   Arg.Any<EventId>(),
			   Arg.Any<object>(),
			   Arg.Any<Exception>(),
			   Arg.Any<Func<object, Exception?, string>>()
		   );
	}

	[TestMethod]
	public async Task SendPrivateMessage_ShouldReturnFalse_AndLogError_WhenExceptionThrown()
	{
		// Arrange.
		_memberMock!.DisplayName.Returns("TestUser");
		_memberMock.SendMessageAsync("Test message")
				   .Returns<Task>(_ => throw new Exception("fail"));

		// Act.
		IDiscordMessage? result = await _service!.SendPrivateMessage(_memberMock!, "TestGuild", "Test message");

		// Assert.
		Assert.IsNull(result);
		_loggerMock!.Received().Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v.ToString()!.Contains("Could not send private message to member")),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	[TestMethod]
	public async Task SaySomethingWentWrong_ShouldCallSendMessage_WithExpectedParameters()
	{
		// Arrange.
		string guildName = "TestGuild";

		// Create partial substitute so we can verify SendMessage was called
		MessageService serviceSub = Substitute.ForPartsOf<MessageService>(
			_discordClientMock!,
			_loggerMock!,
			_optionsMock!,
			_botStateMock!,
			_channelServiceMock!,
			_discordMessageUtilsMock!,
			_mapServiceMock!
		);

		// Stub SendMessage so it doesn't run the real implementation
		serviceSub.SendMessage(_channelMock!, _memberMock!, guildName, "**Er ging iets mis, probeer het opnieuw!**")
				  .Returns(Task.FromResult<IDiscordMessage?>(null));

		// Act.
		await serviceSub.SaySomethingWentWrong(_channelMock!, _memberMock!, guildName);

		// Assert.
		await serviceSub.Received(1).SendMessage(
			_channelMock!,
			_memberMock!,
			guildName,
			"**Er ging iets mis, probeer het opnieuw!**"
		);
	}

	[TestMethod]
	public async Task SayTooManyCharacters_ShouldSendEmbed_WhenNoException()
	{
		// Arrange.
		_channelMock!.SendMessageAsync(null, Arg.Any<IDiscordEmbed>())
			   .Returns(Task.FromResult<IDiscordMessage>(null!));

		// Act.
		await _service!.SayTooManyCharacters(_channelMock!);

		// Assert.
		await _channelMock!.Received(1).SendMessageAsync(Arg.Any<IDiscordEmbed>());
		_loggerMock!.Received(0).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task SayTooManyCharacters_ShouldLogError_WhenExceptionThrown()
	{
		// Arrange.
		_channelMock!.SendMessageAsync(Arg.Any<IDiscordEmbed>())
			   .Returns<Task<IDiscordMessage>>(_ => throw new Exception("fail"));

		// Act.
		await _service!.SayTooManyCharacters(_channelMock!);

		// Assert.
		_loggerMock!.Received(1).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v.ToString()!.Contains("Something went wrong while trying to send an embedded message")),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task SayBotNotAuthorized_ShouldSendEmbed_WhenNoException()
	{
		// Arrange.
		_channelMock!.SendMessageAsync(null!, Arg.Any<IDiscordEmbed>())
			   .Returns(Task.FromResult<IDiscordMessage>(null!));

		// Act.
		await _service!.SayBotNotAuthorized(_channelMock!);

		// Assert.
		await _channelMock!.Received(1).SendMessageAsync(Arg.Any<IDiscordEmbed>());
		_loggerMock!.Received(0).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task SayBotNotAuthorized_ShouldLogError_WhenExceptionThrown()
	{
		// Arrange.
		_channelMock!.SendMessageAsync(Arg.Any<IDiscordEmbed>())
			   .Returns<Task<IDiscordMessage>>(_ => throw new Exception("fail"));

		// Act.
		await _service!.SayBotNotAuthorized(_channelMock!);

		// Assert.
		_loggerMock!.Received(1).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v.ToString()!.Contains("Something went wrong while trying to send an embedded message")),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task ConfirmCommandExecuting_ShouldAddInProgressReaction()
	{
		// Arrange.
		IDiscordMessage message = Substitute.For<IDiscordMessage>();
		IDiscordEmoji inProgressEmoji = Substitute.For<IDiscordEmoji>();

		_discordMessageUtilsMock!
			.GetDiscordEmoji(Constants.IN_PROGRESS_REACTION)
			.Returns(inProgressEmoji);

		// Act.
		await _service!.ConfirmCommandExecuting(message);

		// Assert.
		_discordMessageUtilsMock.Received(1).GetDiscordEmoji(Constants.IN_PROGRESS_REACTION);
		await message.Received(1).CreateReactionAsync(inProgressEmoji);
	}

	[TestMethod]
	public async Task ConfirmCommandExecuted_ShouldRemoveInProgressAndAddCompletedReaction()
	{
		// Arrange.
		IDiscordMessage message = Substitute.For<IDiscordMessage>();
		IDiscordEmoji inProgressEmoji = Substitute.For<IDiscordEmoji>();
		IDiscordEmoji completedEmoji = Substitute.For<IDiscordEmoji>();

		_discordMessageUtilsMock!
			.GetDiscordEmoji(Constants.IN_PROGRESS_REACTION)
			.Returns(inProgressEmoji);
		_discordMessageUtilsMock!
			.GetDiscordEmoji(Constants.ACTION_COMPLETED_REACTION)
			.Returns(completedEmoji);

		// Act.
		await _service!.ConfirmCommandExecuted(message);

		// Assert.
		_discordMessageUtilsMock.Received(1).GetDiscordEmoji(Constants.IN_PROGRESS_REACTION);
		_discordMessageUtilsMock.Received(1).GetDiscordEmoji(Constants.ACTION_COMPLETED_REACTION);
		await message.Received(1).DeleteReactionsEmojiAsync(inProgressEmoji);
		await message.Received(1).CreateReactionAsync(completedEmoji);
	}

	[TestMethod]
	public async Task SayTheUserIsNotAllowed_ShouldSendEmbed_WhenNoException()
	{
		// Arrange.
		IDiscordChannel channel = Substitute.For<IDiscordChannel>();
		channel.SendMessageAsync(Arg.Any<IDiscordEmbed>())
			   .Returns(Task.FromResult<IDiscordMessage>(null));

		// Act.
		await _service!.SayTheUserIsNotAllowed(channel);

		// Assert.
		await channel.Received(1).SendMessageAsync(Arg.Any<IDiscordEmbed>());
		_loggerMock!.Received(0).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task SayTheUserIsNotAllowed_ShouldLogError_WhenExceptionThrown()
	{
		// Arrange.
		IDiscordChannel channel = Substitute.For<IDiscordChannel>();
		channel.SendMessageAsync(Arg.Any<IDiscordEmbed>())
			   .Returns<Task<IDiscordMessage>>(_ => throw new Exception("fail"));

		// Act.
		await _service!.SayTheUserIsNotAllowed(channel);

		// Assert.
		_loggerMock!.Received(1).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v.ToString()!.Contains("Something went wrong while trying to send an embedded message")),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task SayNoResults_ShouldSendEmbed_WhenNoException()
	{
		// Arrange.
		IDiscordChannel channel = Substitute.For<IDiscordChannel>();
		channel.SendMessageAsync(Arg.Any<IDiscordEmbed>())
			   .Returns(Task.FromResult<IDiscordMessage>(null));

		// Act.
		await _service!.SayNoResults(channel, "Test_description");

		// Assert.
		await channel.Received(1).SendMessageAsync(Arg.Any<IDiscordEmbed>());
		_loggerMock!.Received(0).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task SayNoResults_ShouldLogError_WhenExceptionThrown()
	{
		// Arrange.
		IDiscordChannel channel = Substitute.For<IDiscordChannel>();
		channel.SendMessageAsync(Arg.Any<IDiscordEmbed>())
			   .Returns<Task<IDiscordMessage>>(_ => throw new Exception("fail"));

		// Act.
		await _service!.SayNoResults(channel, "Test_description");

		// Assert.
		_loggerMock!.Received(1).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(v => v.ToString()!.Contains("Something went wrong while trying to send an embedded message")),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

}
