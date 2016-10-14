using System.Collections.Generic;
using System.Threading.Tasks;
using Caba.RedMonitoreo.Common;

namespace Caba.RedMonitoreo.BulkServerTests
{
	public class EnqueuerMock<T> : IEnqueuer<T> where T : class
	{
		public List<T> Messages { get; } = new List<T>();

		public Task Enqueue(T message)
		{
			Messages.Add(message);
			return Task.FromResult(true);
		}
	}
}