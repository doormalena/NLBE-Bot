namespace NLBE_Bot.Services;

using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using NLBE_Bot.Interfaces;
using NLBE_Bot.Models;
using System;
using System.Threading.Tasks;

internal class CommandHandler(ILogger<CommandHandler> logger, IErrorHandler errorHandler, IDiscordMessageUtils discordMessageUtils) : ICommandHandler
{
	private readonly ILogger<CommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IErrorHandler _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
	private readonly IDiscordMessageUtils _discordMessageUtils = discordMessageUtils ?? throw new ArgumentNullException(nameof(discordMessageUtils));

	public Task OnCommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
	{
		ICommand commandInfo = e.Command != null ? new CommandWrapper(e.Command) : null;
		return HandleCommandExecuted(commandInfo);
	}

	public Task OnCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
	{
		CommandContextWrapper contextInfo = new(e.Context);
		ICommand commandInfo = e.Command != null ? new CommandWrapper(e.Command) : null;
		return HandleCommandError(contextInfo, commandInfo, e.Exception);
	}

	public Task HandleCommandExecuted(ICommand command)
	{
		_logger.LogInformation("Command executed: {CommandName}", command.Name);
		return Task.CompletedTask;
	}

	public async Task HandleCommandError(ICommandContext context, ICommand command, Exception exception)
	{
		if (!context.GuildId.Equals(Constants.NLBE_SERVER_ID) && !context.GuildId.Equals(Constants.DA_BOIS_ID))
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
}
