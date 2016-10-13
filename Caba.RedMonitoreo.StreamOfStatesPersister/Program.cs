using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caba.RedMonitoreo.Azure.Queues;
using Caba.RedMonitoreo.Common;
using Caba.RedMonitoreo.Queues.Messages;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.WindowsAzure.Storage;

namespace Caba.RedMonitoreo.StreamOfStatesPersister
{
	class Program
	{
		private static readonly SimpleServiceContainer container = new SimpleServiceContainer();

		static void Main(string[] args)
		{
			container.RegisterAllServices();
			container.RegisterJobs();
			StorageInitialize();
			var config = new JobHostConfiguration
			{
				DashboardConnectionString = ConfigurationManager.ConnectionStrings["RedMonitoreo:AzureLogsStorage"].ConnectionString,
				StorageConnectionString = ConfigurationManager.ConnectionStrings["RedMonitoreo:AzureStorage"].ConnectionString,
				JobActivator = new ContainerBasedJobActivator(container)
			};
			ServiceBusConfiguration serviceBusConfig = new ServiceBusConfiguration
			{
				ConnectionString = ConfigurationManager.ConnectionStrings["RedMonitoreo:ServiceBus:SensorsAverage"].ConnectionString
			};
			config.UseServiceBus(serviceBusConfig);

			var host = new JobHost(config);
			host.RunAndBlock();
		}

		private static void StorageInitialize()
		{
			var account = container.GetInstance<CloudStorageAccount>();
			new QueueStorageInitializer<StationSensorHourlyStateChanged>(account).Initialize();
		}
	}
}
