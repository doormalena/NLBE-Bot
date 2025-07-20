namespace NLBE_Bot.Tests.EventHandlers;

using FMWOTB.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.EventHandlers;
using NLBE_Bot.Interfaces;
using NSubstitute;

[TestClass]
public class GuildMemberEventHandlerTests
{
	private IDiscordClient? _discordClient;
	private IErrorHandler? _errorHandler;
	private ILogger<GuildMemberEventHandler>? _logger;
	private IOptions<BotOptions>? _options;
	private IChannelService? _channelService;
	private IUserService? _userService;
	private IMessageService? _messageService;
	private IWGAccountService? _wgAcacountServiceMock;
	private GuildMemberEventHandler? _handler;

	[TestInitialize]
	public void Setup()
	{
		_discordClient = Substitute.For<IDiscordClient>();
		_errorHandler = Substitute.For<IErrorHandler>();
		_logger = Substitute.For<ILogger<GuildMemberEventHandler>>();
		_options = Substitute.For<IOptions<BotOptions>>();
		_channelService = Substitute.For<IChannelService>();
		_userService = Substitute.For<IUserService>();
		_messageService = Substitute.For<IMessageService>();
		_wgAcacountServiceMock = Substitute.For<IWGAccountService>();

		_options.Value.Returns(new BotOptions { ServerId = 12345, WarGamingAppId = "appid" });

		_handler = new GuildMemberEventHandler(
			_errorHandler,
			_logger,
			_options,
			_channelService,
			_userService,
			_messageService,
			_wgAcacountServiceMock
		);

		_handler.Register(_discordClient, Substitute.For<IBotState>());
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

		_channelService!.GetWelkomChannel().Returns(welkomChannel);
		_channelService.GetRegelsChannel().Returns(regelsChannel);
		_channelService.CleanWelkomChannel(member.Id).Returns(Task.CompletedTask);

		_discordClient!.GetUserAsync(member.Id).Returns(Task.FromResult(user));

		_messageService!.AskQuestion(welkomChannel, user, guild, Arg.Any<string>())
			.Returns(Task.FromResult("WargamingUser"));

		_messageService.WaitForReply(welkomChannel, user, Arg.Any<string>(), Arg.Any<int>())
			.Returns(Task.FromResult(0));

		_userService!.ChangeMemberNickname(member, Arg.Any<string>()).Returns(Task.CompletedTask);

		IWGClan wgClan = Substitute.For<IWGClan>();
		wgClan.Id.Returns((int) Constants.NLBE2_CLAN_ID);
		wgClan.Tag.Returns("NLBE2");
		IWGAccount wgAccount = Substitute.For<IWGAccount>();
		wgAccount.Nickname.Returns("WargamingUser");

		_wgAcacountServiceMock!.SearchByName(Arg.Is(SearchAccuracy.EXACT), Arg.Is("WargamingUser"), Arg.Is("appid"), Arg.Is(false), Arg.Is(true), Arg.Is(false))
			.Returns(Task.FromResult<IReadOnlyList<IWGAccount>>([wgAccount]));

		IDiscordRole rulesNotReadRole = Substitute.For<IDiscordRole>();
		rulesNotReadRole.Id.Returns(Constants.MOET_REGELS_NOG_LEZEN_ROLE);
		guild.GetRole(Constants.MOET_REGELS_NOG_LEZEN_ROLE).Returns(rulesNotReadRole);

		// Act.
		await _handler!.HandleMemberAdded(_discordClient, guild, member);

		// Assert.
		await member.Received().GrantRoleAsync(noobRole);
		await welkomChannel.Received().SendMessageAsync(Arg.Is<string>(msg => msg.Contains("welkom")));
		await _messageService.Received().AskQuestion(welkomChannel, user, guild, Arg.Any<string>());
		await member.Received().SendMessageAsync(Arg.Is<string>(msg => msg.Contains("regels")));
		await member.Received().GrantRoleAsync(rulesNotReadRole);
		await member.Received().RevokeRoleAsync(noobRole);
		await _userService.Received().ChangeMemberNickname(member, Arg.Is<string>(name => name.Contains(wgAccount.Nickname)));
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

		_options!.Value.Returns(new BotOptions { ServerId = 12345 });
		_userService!.UpdateName(member, member.DisplayName).Returns("UpdatedNickname");

		IBotState botState = Substitute.For<IBotState>();
		botState.IgnoreEvents.Returns(false);
		_handler!.Register(_discordClient, botState);

		// Act.
		await _handler.HandleMemberUpdated(guild, member, rolesAfter, nicknameAfter);

		// Assert.
		await _userService.Received().ChangeMemberNickname(member, "UpdatedNickname");
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

		_options!.Value.Returns(new BotOptions { ServerId = 12345 });

		IBotState botState = Substitute.For<IBotState>();
		botState.IgnoreEvents.Returns(false);
		_handler!.Register(Substitute.For<IDiscordClient>(), botState);

		// Act.
		await _handler.HandleMemberUpdated(guild, member, rolesAfter, nicknameAfter);

		// Assert.
		await _userService!.DidNotReceive().ChangeMemberNickname(Arg.Any<IDiscordMember>(), Arg.Any<string>());
	}
}
