namespace NLBE_Bot.Tests.EventHandlers;

using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.EventArgs;
using FMWOTB.Clans;
using FMWOTB.Interfaces;
using FMWOTB.Models;
using FMWOTB.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.EventHandlers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using NSubstitute;

[TestClass]
public class GuildMemberEventHandlerTests
{
	private IDiscordClient? _discordClientMock;
	private ILogger<GuildMemberEventHandler>? _loggerMock;
	private IOptions<BotOptions>? _optionsMock;
	private IChannelService? _channelServiceMock;
	private IUserService? _userServiceMock;
	private IMessageService? _messageServiceMock;
	private IAccountsRepository? _accountRepository;
	private GuildMemberEventHandler? _handler;

	[TestInitialize]
	public void Setup()
	{
		_discordClientMock = Substitute.For<IDiscordClient>();
		_loggerMock = Substitute.For<ILogger<GuildMemberEventHandler>>();
		_optionsMock = Substitute.For<IOptions<BotOptions>>();
		_channelServiceMock = Substitute.For<IChannelService>();
		_userServiceMock = Substitute.For<IUserService>();
		_messageServiceMock = Substitute.For<IMessageService>();
		_accountRepository = Substitute.For<IAccountsRepository>();

		_optionsMock.Value.Returns(new BotOptions { ServerId = 12345 });

		_handler = new GuildMemberEventHandler(
			_loggerMock,
			_optionsMock,
			_channelServiceMock,
			_userServiceMock,
			_messageServiceMock,
			_accountRepository
		);
	}

	[TestMethod]
	public void Register_RegistersAllHandlersAndEvents()
	{
		// Act.
		_handler!.Register(_discordClientMock, Substitute.For<IBotState>());

		// Assert.
		_discordClientMock!.Received(1).GuildMemberAdded += Arg.Any<AsyncEventHandler<DiscordClient, GuildMemberAddEventArgs>>();
		_discordClientMock!.Received(1).GuildMemberUpdated += Arg.Any<AsyncEventHandler<DiscordClient, GuildMemberUpdateEventArgs>>();
		_discordClientMock!.Received(1).GuildMemberRemoved += Arg.Any<AsyncEventHandler<DiscordClient, GuildMemberRemoveEventArgs>>();
	}

	[TestMethod]
	public async Task HandleMemberAdded_ShouldGrantRulesNotReadRole_WhenConditionsAreMet()
	{
		// Arrange.
		IDiscordGuild guild = Substitute.For<IDiscordGuild>();
		guild.Id.Returns(12345ul);

		IDiscordRole noobRole = Substitute.For<IDiscordRole>();
		noobRole.Id.Returns(Constants.NOOB_ROLE);
		guild.GetRole(Constants.NOOB_ROLE).Returns(noobRole);

		IDiscordChannel regelsChannel = Substitute.For<IDiscordChannel>();
		regelsChannel.Mention.Returns("#regels");

		IDiscordChannel welkomChannel = Substitute.For<IDiscordChannel>();
		welkomChannel.SendMessageAsync(Arg.Any<string>()).Returns(Task.FromResult(Substitute.For<IDiscordMessage>()));

		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.DisplayName.Returns("Newbie");
		member.Username.Returns("Tester");
		member.Discriminator.Returns("1234");
		member.Id.Returns(1000UL);
		member.Mention.Returns("@Newbie");
		member.Roles.Returns([]);

		member.GrantRoleAsync(noobRole).Returns(Task.CompletedTask);
		member.SendMessageAsync(Arg.Any<string>()).Returns(Task.FromResult(Substitute.For<IDiscordMessage>()));
		member.RevokeRoleAsync(noobRole).Returns(Task.CompletedTask);

		IDiscordUser user = Substitute.For<IDiscordUser>();
		user.Mention.Returns("@Newbie");

		_channelServiceMock!.GetWelkomChannel().Returns(welkomChannel);
		_channelServiceMock.GetRegelsChannel().Returns(regelsChannel);
		_channelServiceMock.CleanWelkomChannel(member.Id).Returns(Task.CompletedTask);

		_discordClientMock!.GetUserAsync(member.Id).Returns(Task.FromResult(user));

		_messageServiceMock!.AskQuestion(welkomChannel, user, guild, Arg.Any<string>())
			.Returns(Task.FromResult("WargamingUser"));

		_messageServiceMock.WaitForReply(welkomChannel, user, Arg.Any<string>(), Arg.Any<int>())
			.Returns(Task.FromResult(0));

		_userServiceMock!.ChangeMemberNickname(member, Arg.Any<string>()).Returns(Task.CompletedTask);

		WGClan wgClan = Substitute.For<WGClan>();
		wgClan.clan_id.Returns((int) Constants.NLBE2_CLAN_ID);
		wgClan.tag.Returns("NLBE2");
		PlayerInfo playerInfo = new()
		{
			Nickname = "WargamingUser"
		};

		_accountRepository!.SearchByNameAsync(Arg.Is(SearchAccuracy.EXACT), Arg.Is("WargamingUser"), Arg.Is(false), Arg.Is(true), Arg.Is(false))
			.Returns(Task.FromResult<IReadOnlyList<PlayerInfo>>([playerInfo]));

		IDiscordRole rulesNotReadRole = Substitute.For<IDiscordRole>();
		rulesNotReadRole.Id.Returns(Constants.MOET_REGELS_NOG_LEZEN_ROLE);
		guild.GetRole(Constants.MOET_REGELS_NOG_LEZEN_ROLE).Returns(rulesNotReadRole);

		IBotState botState = Substitute.For<IBotState>();
		botState.IgnoreEvents.Returns(false);
		_handler!.Register(_discordClientMock, botState);

		// Act.
		await _handler!.HandleMemberAdded(_discordClientMock, guild, member);

		// Assert.
		await member.Received().GrantRoleAsync(noobRole);
		await welkomChannel.Received().SendMessageAsync(Arg.Is<string>(msg => msg.Contains("welkom")));
		await _messageServiceMock.Received().AskQuestion(welkomChannel, user, guild, Arg.Any<string>());
		await member.Received().SendMessageAsync(Arg.Is<string>(msg => msg.Contains("regels")));
		await member.Received().GrantRoleAsync(rulesNotReadRole);
		await member.Received().RevokeRoleAsync(noobRole);
		await _userServiceMock.Received().ChangeMemberNickname(member, Arg.Is<string>(name => name.Contains(playerInfo.Nickname)));
	}

	[TestMethod]
	public async Task HandleMemberUpdated_ShouldRenameMember_IfEligible()
	{
		// Arrange.
		IDiscordGuild guild = Substitute.For<IDiscordGuild>();
		guild.Id.Returns(12345ul);

		IDiscordRole nonNoobRole = Substitute.For<IDiscordRole>();
		nonNoobRole.Id.Returns(999ul);

		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.Roles.Returns([nonNoobRole]);
		member.DisplayName.Returns("OldNickname");

		List<IDiscordRole> rolesAfter = [nonNoobRole];
		string nicknameAfter = "NewNickname";

		_optionsMock!.Value.Returns(new BotOptions { ServerId = 12345 });
		_userServiceMock!.UpdateName(member, member.DisplayName).Returns("UpdatedNickname");

		IBotState botState = Substitute.For<IBotState>();
		botState.IgnoreEvents.Returns(false);
		_handler!.Register(_discordClientMock, botState);

		// Act.
		await _handler.HandleMemberUpdated(guild, member, rolesAfter, nicknameAfter);

		// Assert.
		await _userServiceMock.Received().ChangeMemberNickname(member, "UpdatedNickname");
	}

	[TestMethod]
	public async Task HandleMemberUpdated_ShouldSkip_WhenMemberIsNoob()
	{
		// Arrange.
		IDiscordGuild guild = Substitute.For<IDiscordGuild>();
		guild.Id.Returns(12345ul);

		IDiscordRole noobRole = Substitute.For<IDiscordRole>();
		noobRole.Id.Returns(Constants.NOOB_ROLE);

		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.Roles.Returns([noobRole]);

		List<IDiscordRole> rolesAfter = [noobRole];
		string nicknameAfter = "Nickname";

		_optionsMock!.Value.Returns(new BotOptions { ServerId = 12345 });

		IBotState botState = Substitute.For<IBotState>();
		botState.IgnoreEvents.Returns(false);
		_handler!.Register(_discordClientMock, botState);

		// Act.
		await _handler.HandleMemberUpdated(guild, member, rolesAfter, nicknameAfter);

		// Assert.
		await _userServiceMock!.DidNotReceive().ChangeMemberNickname(Arg.Any<IDiscordMember>(), Arg.Any<string>());
	}

	[TestMethod]
	public async Task HandleMemberRemoved_ShouldNotLog_WhenEventIgnoredOrWrongServer()
	{
		// Arrange.
		IDiscordGuild guild = Substitute.For<IDiscordGuild>();
		guild.Id.Returns(123ul); // Doesn't match options.ServerId
		IDiscordMember member = Substitute.For<IDiscordMember>();

		IBotState botState = Substitute.For<IBotState>();
		botState.IgnoreEvents.Returns(false);
		_handler!.Register(_discordClientMock, botState);

		// Act.
		await _handler!.HandleMemberRemoved(guild, member);

		// Assert.
		await _channelServiceMock!.DidNotReceive().GetOudLedenChannel();
		await _messageServiceMock!.DidNotReceive().CreateEmbed(Arg.Any<IDiscordChannel>(), Arg.Any<EmbedOptions>());
	}

	[TestMethod]
	public async Task HandleMemberRemoved_ShouldLogWarning_IfOudLedenChannelIsNull()
	{
		// Arrange.
		IDiscordGuild guild = Substitute.For<IDiscordGuild>();
		guild.Id.Returns(12345ul);

		IDiscordMember member = Substitute.For<IDiscordMember>();
		_channelServiceMock!.GetOudLedenChannel().Returns((IDiscordChannel?) null);

		IBotState botState = Substitute.For<IBotState>();
		botState.IgnoreEvents.Returns(false);
		_handler!.Register(_discordClientMock, botState);

		// Act.
		await _handler!.HandleMemberRemoved(guild, member);

		// Assert.
		_loggerMock!.Received().Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("Could not find the Oud Leden channel")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task HandleMemberRemoved_ShouldLogWarning_IfNoServerRolesFound()
	{
		// Arrange.
		IDiscordGuild guild = Substitute.For<IDiscordGuild>();
		guild.Id.Returns(12345ul);
		guild.Roles.Returns(new Dictionary<ulong, IDiscordRole>());

		IDiscordMember member = Substitute.For<IDiscordMember>();
		_channelServiceMock!.GetOudLedenChannel().Returns(Substitute.For<IDiscordChannel>());

		IBotState botState = Substitute.For<IBotState>();
		botState.IgnoreEvents.Returns(false);
		_handler!.Register(_discordClientMock, botState);

		// Act.
		await _handler!.HandleMemberRemoved(guild, member);

		// Assert.
		_loggerMock!.Received().Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("Could not find server roles")),
			null,
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[TestMethod]
	public async Task HandleMemberRemoved_ShouldCleanWelkomChannel_IfMemberHasNoobRole()
	{
		// Arrange.
		IDiscordGuild guild = Substitute.For<IDiscordGuild>();
		guild.Id.Returns(12345ul);

		IDiscordMember member = Substitute.For<IDiscordMember>();
		IDiscordRole role = Substitute.For<IDiscordRole>();
		role.Id.Returns(Constants.NOOB_ROLE);
		member.Roles.Returns([role]);
		guild.Roles.Returns(new Dictionary<ulong, IDiscordRole> { { Constants.NOOB_ROLE, role } });
		_channelServiceMock!.GetOudLedenChannel().Returns(Substitute.For<IDiscordChannel>());

		IBotState botState = Substitute.For<IBotState>();
		botState.IgnoreEvents.Returns(false);
		_handler!.Register(_discordClientMock, botState);

		// Act.
		await _handler!.HandleMemberRemoved(guild, member);

		// Assert.
		await _channelServiceMock.Received(1).CleanWelkomChannel();
	}

	[TestMethod]
	public async Task HandleMemberRemoved_ShouldSendEmbed_WhenAllDataPresent()
	{
		// Arrange.
		IDiscordGuild guild = Substitute.For<IDiscordGuild>();
		guild.Id.Returns(12345ul);

		IDiscordChannel channel = Substitute.For<IDiscordChannel>();
		IDiscordMember member = Substitute.For<IDiscordMember>();
		member.Username.Returns("alex");
		member.Discriminator.Returns("1234");
		member.DisplayName.Returns("Alex");
		member.Id.Returns(100ul);

		IDiscordRole role = Substitute.For<IDiscordRole>();
		role.Id.Returns(999ul);
		role.Name.Returns("Contributor");
		IDiscordRole role2 = Substitute.For<IDiscordRole>();
		role2.Id.Returns(888ul);
		role2.Name.Returns("Reader");

		guild.Roles.Returns(new Dictionary<ulong, IDiscordRole> { { 999ul, role }, { 888ul, role2 } });
		member.Roles.Returns([role, role2]);
		_channelServiceMock!.GetOudLedenChannel().Returns(channel);

		IBotState botState = Substitute.For<IBotState>();
		botState.IgnoreEvents.Returns(false);
		_handler!.Register(_discordClientMock, botState);

		// Act.
		await _handler!.HandleMemberRemoved(guild, member);

		// Assert.
		await _messageServiceMock!.Received(1).CreateEmbed(channel, Arg.Is<EmbedOptions>(embed =>
			embed.Title.Contains("heeft de server verlaten") &&
			embed.Fields.Any(f => f.Name == "Gebruiker:" && f.Value == "alex#1234") &&
			embed.Fields.Any(f => f.Name == "GebruikersID:" && f.Value == 100ul.ToString()) &&
			embed.Fields.Any(f => f.Name == "Bijnaam:" && f.Value == "Alex") &&
			embed.Fields.Any(f => f.Name == "Rollen:" && f.Value == "Contributor, Reader")
		));
	}

	[TestMethod]
	public void Constructor_ThrowsArgumentNullException_WhenAnyDependencyIsNull()
	{
		// Act & Assert.
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new GuildMemberEventHandler(null, _optionsMock, _channelServiceMock, _userServiceMock, _messageServiceMock, _accountRepository));
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new GuildMemberEventHandler(_loggerMock, null, _channelServiceMock, _userServiceMock, _messageServiceMock, _accountRepository));
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new GuildMemberEventHandler(_loggerMock, _optionsMock, null, _userServiceMock, _messageServiceMock, _accountRepository));
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new GuildMemberEventHandler(_loggerMock, _optionsMock, _channelServiceMock, null, _messageServiceMock, _accountRepository));
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new GuildMemberEventHandler(_loggerMock, _optionsMock, _channelServiceMock, _userServiceMock, null, _accountRepository));
		Assert.ThrowsException<ArgumentNullException>(() =>
			  new GuildMemberEventHandler(_loggerMock, _optionsMock, _channelServiceMock, _userServiceMock, _messageServiceMock, null));
	}
}
