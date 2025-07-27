namespace NLBE_Bot;

using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot.Configuration;
using NLBE_Bot.EventHandlers;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Jobs;
using NLBE_Bot.Models;
using NLBE_Bot.Services;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public static class Program
{
	public static void Main(string[] args)
	{
		CreateHostBuilder(args).Build().Run();
	}

	internal static IHostBuilder CreateHostBuilder(string[] args)
	{
		return Host.CreateDefaultBuilder(args)
			.UseWindowsService()
			.ConfigureAppConfiguration((hostContext, config) =>
			{
				config.Sources.Clear();
				config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
				config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
				config.AddUserSecrets(Assembly.GetExecutingAssembly());
			})
			.ConfigureLogging(logging =>
			{
				logging.ClearProviders();
				logging.AddSimpleConsole(options =>
				{
					options.SingleLine = true;
					options.TimestampFormat = "HH:mm:ss ";
				});

				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					logging.AddEventLog();
				}
			})
			.ConfigureServices((hostContext, services) =>
			{
				services.AddOptions<BotOptions>().
					Bind(hostContext.Configuration.GetSection("NLBEBot")).
					ValidateDataAnnotations().
					ValidateOnStart();

				services.AddSingleton(provider =>
				{
					return CreateDiscordClient(provider) as IDiscordClient;
				});
				services.AddSingleton<IBotState>(provider =>
				{
					BotState botState = new();
					Task.Run(() => botState.LoadAsync()).Wait(); // Synchronously load state at startup.
					return botState;
				});
				services.AddHostedService<Bot>();
				services.AddSingleton<BotCommands>();
				services.AddSingleton<IBotEventHandlers, BotEventHandlers>();
				services.AddSingleton<IWeeklyEventService, WeeklyEventService>();
				services.AddSingleton<ICommandEventHandler, CommandEventHandler>();
				services.AddSingleton<IGuildMemberEventHandler, GuildMemberEventHandler>();
				services.AddSingleton<IMessageEventHandler, MessageEventHandler>();
				services.AddSingleton<IUserService, UserService>();
				services.AddSingleton<IChannelService, ChannelService>();
				services.AddSingleton<IMessageService, MessageService>();
				services.AddSingleton<IMapService, MapService>();
				services.AddSingleton<IReplayService, ReplayService>();
				services.AddSingleton<IHallOfFameService, HallOfFameService>();
				services.AddSingleton<ITournamentService, TournamentService>();
				services.AddSingleton<IBlitzstarsService, BlitzstarsService>();
				services.AddSingleton<IClanService, ClanService>();
				services.AddSingleton<IWGAccountService, WGAccountService>();
				services.AddSingleton<IJob<AnnounceWeeklyWinnerJob>, AnnounceWeeklyWinnerJob>();
				services.AddSingleton<IJob<VerifyServerNicknamesJob>, VerifyServerNicknamesJob>();
				services.AddSingleton<IDiscordMessageUtils, DiscordMessageUtils>();
				services.AddHttpClient<IPublicIpAddress, PublicIpAddress>();
				services.AddHttpClient<IApiRequester, ApiRequester>();
			});
	}

	private static DiscordClientWrapper CreateDiscordClient(IServiceProvider provider)
	{
		BotOptions options = provider.GetService<IOptions<BotOptions>>().Value;

		DiscordConfiguration config = new()
		{
			Token = options.DiscordToken,
			TokenType = TokenType.Bot,
			AutoReconnect = true,
			Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents | DiscordIntents.GuildMembers,
			LoggerFactory = provider.GetRequiredService<ILoggerFactory>(),
		};

		DiscordClient client = new(config);
		client.UseInteractivity(new InteractivityConfiguration
		{
			Timeout = TimeSpan.FromSeconds(options.DiscordTimeOutInSeconds)
		});

		return new DiscordClientWrapper(client);
	}
}

