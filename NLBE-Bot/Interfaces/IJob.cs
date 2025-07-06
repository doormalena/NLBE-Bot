namespace NLBE_Bot.Interfaces;

using System;
using System.Threading.Tasks;

internal interface IJob<T>
{
	public Task Execute(DateTime now);
}
