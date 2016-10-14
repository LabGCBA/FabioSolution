using System;

namespace Caba.RedMonitoreo.Common
{
	public interface IServiceContainer: IDisposable
	{
		T GetInstance<T>() where T : class;
		object GetInstance(Type type);
	}
}