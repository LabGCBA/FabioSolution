using System;
using Microsoft.ServiceBus;

namespace Caba.RedMonitoreo.Azure.Queues
{
	/// <summary>
	///   Initialize a servicebus queue specific for a message type.
	/// </summary>
	/// <typeparam name="TMessage">The typeof the message</typeparam>
	public class ServiceBusInitializer<TMessage> : IStorageInitializer where TMessage : class
	{
		private readonly string serviceBusConnectionString;
		public ServiceBusInitializer(string serviceBusConnectionString)
		{
			if (string.IsNullOrWhiteSpace(serviceBusConnectionString)) throw new ArgumentNullException(nameof(serviceBusConnectionString));
			this.serviceBusConnectionString = serviceBusConnectionString;
		}

		public static string GetName() => typeof(TMessage).Name.ToLowerInvariant();

		public void Initialize()
		{
			NamespaceManager namespaceManager = NamespaceManager.CreateFromConnectionString(serviceBusConnectionString);
			var queueName = GetName();
			if (!namespaceManager.QueueExists(queueName))
			{
				namespaceManager.CreateQueue(queueName);
			}
		}

		public void Drop()
		{
			NamespaceManager namespaceManager = NamespaceManager.CreateFromConnectionString(serviceBusConnectionString);
			var queueName = GetName();
			if (namespaceManager.QueueExists(queueName))
			{
				namespaceManager.DeleteQueue(queueName);
			}
		}
	}
}