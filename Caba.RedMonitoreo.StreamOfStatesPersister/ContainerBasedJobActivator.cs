using System;
using Caba.RedMonitoreo.Common;
using Microsoft.Azure.WebJobs.Host;

namespace Caba.RedMonitoreo.StreamOfStatesPersister
{
	public class ContainerBasedJobActivator: IJobActivator
	{
		private readonly IServiceContainer container;
		public ContainerBasedJobActivator(IServiceContainer container)
		{
			if (container == null) throw new ArgumentNullException(nameof(container));
			this.container = container;
		}

		public T CreateInstance<T>()
		{
			return (T) container.GetInstance(typeof(T));
		}
	}
}