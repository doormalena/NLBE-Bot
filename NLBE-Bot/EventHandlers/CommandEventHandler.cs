namespace NLBE_Bot.EventHandlers;

using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLBE_Bot;
using NLBE_Bot.Configuration;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Threading.Tasks;

internal class CommandEventHandler(ILogger<CommandEventHandler> logger,
								   IErrorHandler errorHandler,
								   IDiscordMessageUtils discordMessageUtils,
								   IOptions<BotOptions> options) : ICommandEventHandler
{
	private readonly ILogger<CommandEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly IDiscordMessageUtils _discordMessageUtils = discordMessageUtils ?? throw new ArgumentNullException(nameof(discordMessageUtils));
	private readonly BotOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

	public void Register(ICommandsNextExtension commands)
	{
		commands.CommandErrored += OnCommandErrored;
		commands.CommandExecuted += OnCommandExecuted;
	}

	private Task OnCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
	{
		CommandContextWrapper contextInfo = new(e.Context);
		IDiscordCommand commandInfo = e.Command != null ? new DiscordCommandWrapper(e.Command) : null;
		return HandleCommandError(contextInfo, commandInfo, e.Exception);
	}

	private Task OnCommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
	{
		IDiscordCommand commandInfo = e.Command != null ? new DiscordCommandWrapper(e.Command) : null;
		return HandleCommandExecuted(commandInfo);
	}

	internal async Task HandleCommandError(IDiscordCommandContext context, IDiscordCommand command, Exception exception)
	{
		if (!context.GuildId.Equals(_options.ServerId))
		{
			return;
		}

		if (exception.Message.Contains("unauthorized", StringComparison.CurrentCultureIgnoreCase))
		{
			await context.SendUnauthorizedMessageAsync();
		}
		else if (command != null)
		{
			IDiscordEmoji inProgressEmoji = _discordMessageUtils.GetDiscordEmoji(Constants.IN_PROGRESS_REACTION);
			IDiscordEmoji errorEmoji = _discordMessageUtils.GetDiscordEmoji(Constants.ERROR_REACTION);

			await context.DeleteInProgressReactionAsync(inProgressEmoji);
			await context.AddErrorReactionAsync(errorEmoji);
			await _errorHandler.HandleErrorAsync($"Error with command ({command.Name}):\n", exception);
		}
	}

	internal Task HandleCommandExecuted(IDiscordCommand command)
	{
		_logger.LogInformation("Command executed: {CommandName}", command.Name);
		return Task.CompletedTask;
	}

}
