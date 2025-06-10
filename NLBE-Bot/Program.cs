namespace NLBE_Bot;

using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
					return CreateDiscordClient(hostContext.Configuration, provider.GetRequiredService<ILoggerFactory>());
				});

				services.AddHostedService<Worker>();
				services.AddSingleton<Bot>();
				services.AddSingleton<BotCommands>();
			});
	}

	private static DiscordClient CreateDiscordClient(IConfiguration configuration, ILoggerFactory loggerFactory)
	{
		DiscordConfiguration config = new()
		{
			Token = configuration["NLBEBOT:DiscordToken"],
			TokenType = TokenType.Bot,
			AutoReconnect = true,
			Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
			LoggerFactory = loggerFactory
		};

		DiscordClient client = new(config);
		client.UseInteractivity(new InteractivityConfiguration
		{
			Timeout = TimeSpan.FromSeconds(int.TryParse(configuration["NLBEBOT:DiscordTimeOutInSeconds"], out int timeout) ? timeout : 0)
		});

		return client;
	}
}
