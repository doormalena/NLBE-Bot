namespace NLBE_Bot.Interfaces;

using System;
using System.Threading.Tasks;

internal interface IJob<T> where T : class
{
	public Task Execute(IDiscordGuild guild, DateTime now);
}
