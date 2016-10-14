using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Caba.RedMonitoreo.Common;
using Caba.RedMonitoreo.IO;
using Caba.RedMonitoreo.Queues.Messages;

namespace Caba.RedMonitoreo.BulkServer.Controllers
{
	[RoutePrefix("api1/otfs")]
	public class OtfsController : ApiController
	{
		private readonly IStationFileStore fileStore;
		private readonly IEnqueuer<StationFileToProcess> enqueuer;

		public OtfsController(IStationFileStore fileStore, IEnqueuer<StationFileToProcess> enqueuer)
		{
			if (fileStore == null) throw new ArgumentNullException(nameof(fileStore));
			if (enqueuer == null) throw new ArgumentNullException(nameof(enqueuer));
			this.fileStore = fileStore;
			this.enqueuer = enqueuer;
		}

		[HttpPost]
		[Route("{stationId}")]
		public async Task<HttpResponseMessage> Post([FromUri] string stationId)
		{
			if (!"text/plain".Equals(Request.Content.Headers.ContentType.MediaType))
			{
				return new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType) {ReasonPhrase = "Solo text/plain." };
			}
			if ((Request.Content.Headers.ContentLength ?? 0L) <= 0)
			{
				return new HttpResponseMessage(HttpStatusCode.BadRequest) { ReasonPhrase = "Empty content." };
			}
			var fileUri = await fileStore.Store(stationId, await Request.Content.ReadAsStreamAsync());
			await enqueuer.Enqueue(new StationFileToProcess
			{
				RecivedAt = DateTimeOffset.UtcNow,
				FilePath = fileUri,
				StationId = stationId
			});
			return new HttpResponseMessage(HttpStatusCode.Accepted);
		}
	}
}