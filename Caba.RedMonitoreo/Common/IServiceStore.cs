using System;

namespace Caba.RedMonitoreo.Common
{
	public interface IServiceStore
	{
		void RegisterSingleton<T>(Func<IServiceContainer, T> ctor) where T : class;
		void RegisterSingleton<T>(T instance) where T : class;
		void RegisterTransient<T>(Func<IServiceContainer, T> ctor) where T : class;
	}
}