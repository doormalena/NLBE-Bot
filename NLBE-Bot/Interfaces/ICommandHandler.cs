namespace NLBE_Bot.Interfaces;

using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System.Threading.Tasks;

internal interface ICommandHandler
{
	public Task OnCommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e);

	public Task OnCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e);
}
