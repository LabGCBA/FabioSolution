using System;
using System.Threading.Tasks;
using Caba.RedMonitoreo.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace Caba.RedMonitoreo.Azure.Queues
{
	public class MessageEnqueuer<TMessage>: IEnqueuer<TMessage> where TMessage:class
	{
		private readonly CloudStorageAccount account;
		private readonly string queueName = typeof(TMessage).Name.ToLowerInvariant();
		public MessageEnqueuer(CloudStorageAccount account)
		{
			if (account == null) throw new ArgumentNullException(nameof(account));
			this.account = account;
		}

		public Task Enqueue(TMessage message)
		{
			if (message == null)
			{
				throw new ArgumentNullException(nameof(message));
			}
			var queueClient = account.CreateCloudQueueClient();
			var queueRef = queueClient.GetQueueReference(queueName);
			var serializedMessage = JsonConvert.SerializeObject(message);
			var qmessage = new CloudQueueMessage(serializedMessage);
			return queueRef.AddMessageAsync(qmessage);
		}
	}
}