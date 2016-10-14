using System.IO;
using System.Threading.Tasks;
using Caba.RedMonitoreo.IO;

namespace Caba.RedMonitoreo.BulkServerTests.OtfsControllerTests
{
	public class NoOpStationFileStore:IStationFileStore
	{
		private readonly string storePath;
		public NoOpStationFileStore(string storePath = null)
		{
			this.storePath = storePath;
		}

		public Task<string> Store(string stationId, Stream fileContent)
		{
			return Task.FromResult(storePath ?? "NoOp");
		}

		public Task<bool> TryGet(string filePath, Stream fileContent)
		{
			return Task.FromResult(false); ;
		}
	}
}