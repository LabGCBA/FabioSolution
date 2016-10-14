using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Caba.RedMonitoreo.Common;

namespace Caba.RedMonitoreo.BulkServer
{
	public class DependencyResolver : IDependencyResolver
	{
		private readonly IServiceContainer container;

		public DependencyResolver(IServiceContainer container)
		{
			this.container = container;
		}

		public void Dispose() {}

		public IDependencyScope BeginScope()
		{
			return this;
		}

		public object GetService(Type serviceType)
		{
			return container.GetInstance(serviceType);
		}

		public IEnumerable<object> GetServices(Type serviceType)
		{
			var instance = container.GetInstance(serviceType);
			if (instance != null) yield return instance;
		}
	}
}