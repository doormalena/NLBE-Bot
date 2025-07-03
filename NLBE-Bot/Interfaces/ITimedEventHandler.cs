namespace NLBE_Bot.Interfaces;

using System;
using System.Threading.Tasks;

internal interface ITimedEventHandler
{
	public Task Execute(DateTime now);
}
