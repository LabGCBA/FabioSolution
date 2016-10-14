using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Caba.RedMonitoreo.Azure.Queues
{
	/// <summary>
	///   Initialize a queue storage specific for a message type.
	/// </summary>
	/// <typeparam name="TMessage">The typeof the message</typeparam>
	public class QueueStorageInitializer<TMessage> : IStorageInitializer where TMessage : class
	{
		private readonly CloudStorageAccount account;
		private readonly string queueName = GetName();

		public QueueStorageInitializer(CloudStorageAccount account)
		{
			if (account == null)
			{
				throw new ArgumentNullException(nameof(account));
			}
			this.account = account;
		}

		public static string GetName() => typeof (TMessage).Name.ToLowerInvariant();

		public void Initialize()
		{
			CloudQueueClient queueClient = account.CreateCloudQueueClient();
			CloudQueue queue = queueClient.GetQueueReference(queueName);
			queue.CreateIfNotExists();
		}

		public void Drop()
		{
			CloudQueueClient queueClient = account.CreateCloudQueueClient();
			CloudQueue queue = queueClient.GetQueueReference(queueName);
			queue.DeleteIfExists();
		}
	}
}