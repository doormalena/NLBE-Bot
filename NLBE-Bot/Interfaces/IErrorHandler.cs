namespace NLBE_Bot.Interfaces;

using System;
using System.Threading.Tasks;

internal interface IErrorHandler
{
	public Task HandleErrorAsync(string message, Exception ex = null);
}
