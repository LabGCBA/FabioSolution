using System.Threading.Tasks;

namespace Caba.RedMonitoreo.Common
{
	public interface IEnqueuer<in TMessage> where TMessage: class
	{
		Task Enqueue(TMessage message);
	}
}