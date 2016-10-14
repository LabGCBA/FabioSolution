using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Caba.RedMonitoreo.Data;

namespace Caba.RedMonitoreo.BulkServer.Controllers
{
	[RoutePrefix("api1/stations")]
	public class StationsController : ApiController
	{
		private readonly IStationsSensorsYearActivityPersister stationsPersister;
		public StationsController(IStationsSensorsYearActivityPersister stationsPersister)
		{
			if (stationsPersister == null) throw new ArgumentNullException(nameof(stationsPersister));
			this.stationsPersister = stationsPersister;
		}

		[HttpGet]
		[Route("")]
		public Task<IEnumerable<Models.Station>> All()
		{
			return Task.FromResult(stationsPersister.Stations().Select(x=> new Models.Station
			{
				Id = x.Id,
				SensosrsLink = Url.Route("SensorsOfStation", new { stationId = x.Id})
			}));
		}

		[HttpGet]
		[Route("{stationId}", Name = "SensorsOfStation")]
		public Task<IEnumerable<Models.SensorLink>> Sensors(string stationId)
		{
			var sensors = stationsPersister.Sensors(stationId);
			return Task.FromResult(sensors.Select(x => new Models.SensorLink
			{
				Id = x.Id,
				YearOfActivitiesLink = Url.Route("SensorYearsOfActivity", new { stationId = stationId, sensorId= x.Id })
			}));
		}
	}
}