namespace NLBE_Bot.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Jobs;
using NLBE_Bot.Services;

[TestClass]
public class ProgramTests
{
	[TestMethod]
	public void Host_Builds_Services()
	{
		// Arrange.
		IHost host = Program.CreateHostBuilder([])
							.ConfigureServices(services =>
							{
								// Remove the real BotOptions registration.
								ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IOptions<BotOptions>));
								if (descriptor != null)
								{
									services.Remove(descriptor);
								}

								// Add a valid BotOptions instance usable during tests.
								services.AddSingleton(Options.Create(new BotOptions
								{
									DiscordToken = "dummy-token",
									MemberDefaultRoleId = 1234567890,
									WarGamingAppId = "dummy-appid"
								}));
							})
							.Build();
		IServiceProvider services = host.Services;

		// Act & Assert.
		IEnumerable<IHostedService> hostedServices = services.GetServices<IHostedService>();
		IHostedService? botHostedService = hostedServices.OfType<Bot>().FirstOrDefault();

		Assert.IsNotNull(services.GetService<IOptions<BotOptions>>());
		Assert.IsNotNull(services.GetService<IDiscordClient>());
		Assert.IsNotNull(services.GetService<IBotState>());
		Assert.IsNotNull(botHostedService);
		Assert.IsNotNull(services.GetService<BotCommands>());
		Assert.IsNotNull(services.GetService<IBotEventHandlers>());
		Assert.IsNotNull(services.GetService<IWeeklyEventService>());
		Assert.IsNotNull(services.GetService<ICommandEventHandler>());
		Assert.IsNotNull(services.GetService<IGuildMemberEventHandler>());
		Assert.IsNotNull(services.GetService<IMessageEventHandler>());
		Assert.IsNotNull(services.GetService<IUserService>());
		Assert.IsNotNull(services.GetService<IChannelService>());
		Assert.IsNotNull(services.GetService<IMessageService>());
		Assert.IsNotNull(services.GetService<IMapService>());
		Assert.IsNotNull(services.GetService<IReplayService>());
		Assert.IsNotNull(services.GetService<IHallOfFameService>());
		Assert.IsNotNull(services.GetService<ITournamentService>());
		Assert.IsNotNull(services.GetService<IBlitzstarsService>());
		Assert.IsNotNull(services.GetService<IClanService>());
		Assert.IsNotNull(services.GetService<IWGAccountService>());
		Assert.IsNotNull(services.GetService<IJob<AnnounceWeeklyWinnerJob>>());
		Assert.IsNotNull(services.GetService<IJob<VerifyServerNicknamesJob>>());
		Assert.IsNotNull(services.GetService<IDiscordMessageUtils>());
		Assert.IsNotNull(services.GetService<IPublicIpAddress>());
		Assert.IsNotNull(services.GetService<IApiRequester>());
	}
}
