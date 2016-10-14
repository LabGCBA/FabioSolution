using System.Threading.Tasks;

namespace Caba.RedMonitoreo.Common.Queues
{
	public interface IQueueMessageConsumer<in TMessage> where TMessage: class
	{
		Task ProcessMessage(TMessage message);
	}
}