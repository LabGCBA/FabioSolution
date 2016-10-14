namespace Caba.RedMonitoreo.Common
{
	public interface IDequeuer<out TMessage> where TMessage : class
	{
		TMessage Dequeue(int timeoutMilliseconds);
	}
}