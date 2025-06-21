namespace NLBE_Bot;

using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLBE_Bot.EventHandlers;
using NLBE_Bot.Helpers;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using NLBE_Bot.Services;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

public static class Program
{
	public static void Main(string[] args)
	{
		CreateHostBuilder(args).Build().Run();
	}

	public static IHostBuilder CreateHostBuilder(string[] args)
	{
		return Host.CreateDefaultBuilder(args)
			.UseWindowsService()
			.ConfigureAppConfiguration((hostContext, config) =>
			{
				config.Sources.Clear();
				config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
				config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
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
				services.AddSingleton(provider =>
				{
					return CreateDiscordClient(provider, hostContext.Configuration) as IDiscordClient;
				});

				services.AddHostedService<Bot>();
				services.AddSingleton<IBotState, BotState>();
				services.AddSingleton<IBotEventHandlers, BotEventHandlers>();
				services.AddSingleton<BotCommands>();
				services.AddSingleton<IWeeklyEventService, WeeklyEventService>();
				services.AddSingleton<IErrorHandler, ErrorHandler>();
				services.AddSingleton<ICommandEventHandler, CommandEventHandler>();
				services.AddSingleton<IGuildMemberEventHandler, GuildMemberEventHandler>();
				services.AddSingleton<IMessageEventHandler, MessageEventHandler>();
				services.AddSingleton<IGuildProvider, GuildProvider>();
				services.AddSingleton<IUserService, UserService>();
				services.AddSingleton<IChannelService, ChannelService>();
				services.AddSingleton<IMessageService, MessageService>();
				services.AddSingleton<IMapService, MapService>();
				services.AddSingleton<IReplayService, ReplayService>();
				services.AddSingleton<IHallOfFameService, HallOfFameService>();
				services.AddSingleton<ITournamentService, TournamentService>();
				services.AddSingleton<IBlitzstarsService, BlitzstarsService>();
				services.AddSingleton<IClanService, ClanService>();
				services.AddSingleton<IDiscordMessageUtils, DiscordMessageUtils>();
				services.AddHttpClient<IPublicIpAddress, PublicIpAddress>();
				services.AddHttpClient<IApiRequester, ApiRequester>();
			});
	}

	private static DiscordClientWrapper CreateDiscordClient(IServiceProvider provider, IConfiguration configuration)
	{
		DiscordConfiguration config = new()
		{
			Token = configuration["NLBEBOT:DiscordToken"],
			TokenType = TokenType.Bot,
			AutoReconnect = true,
			Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
			LoggerFactory = provider.GetRequiredService<ILoggerFactory>()
		};

		DiscordClient client = new(config);
		client.UseInteractivity(new InteractivityConfiguration
		{
			Timeout = TimeSpan.FromSeconds(int.TryParse(configuration["NLBEBOT:DiscordTimeOutInSeconds"], out int timeout) ? timeout : 0)
		});

		return new DiscordClientWrapper(client);
	}
}

