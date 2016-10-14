using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Caba.RedMonitoreo.Common;
using System.Web.Http;
using Caba.RedMonitoreo.Data;
using Caba.RedMonitoreo.Queues.Messages;

namespace Caba.RedMonitoreo.BulkServer.Controllers
{
    [RoutePrefix("api1/sensors")]
    public class SensorsController : ApiController
    {
        private readonly IStationSensorStatePersister sensorStatesPersister;
        private readonly IStationsSensorsYearActivityPersister stationsPersister;
        private readonly IStationSensorDailyStatePersister dailyStatePersister;
        private readonly IStationSensorHourlyStatePersister hourlyStatePersister;
        private readonly IStationSensorStateDeleter sensorStatesDeleter;
        private readonly IStationSensorStateAdder sensorStatesAdder;
        private readonly IEnqueuer<StationSensorHourlyStateChanged> hourlyEnqueuer;

        public SensorsController(IStationsSensorsYearActivityPersister stationsPersister
            , IStationSensorDailyStatePersister dailyStatePersister
            , IStationSensorHourlyStatePersister hourlyStatePersister
            , IStationSensorStatePersister sensorStatesPersister
            , IStationSensorStateDeleter sensorStatesDeleter
            , IEnqueuer<StationSensorHourlyStateChanged> HourlyEnqueuer,
            IStationSensorStateAdder sensorStatesAdder)
        {
            if (stationsPersister == null) throw new ArgumentNullException(nameof(stationsPersister));
            if (dailyStatePersister == null) throw new ArgumentNullException(nameof(dailyStatePersister));
            if (hourlyStatePersister == null) throw new ArgumentNullException(nameof(hourlyStatePersister));
            if (sensorStatesPersister == null) throw new ArgumentNullException(nameof(sensorStatesPersister));
            if (sensorStatesDeleter == null) throw new ArgumentNullException(nameof(sensorStatesDeleter));
            this.stationsPersister = stationsPersister;
            this.dailyStatePersister = dailyStatePersister;
            this.hourlyStatePersister = hourlyStatePersister;
            this.sensorStatesPersister = sensorStatesPersister;
            this.sensorStatesDeleter = sensorStatesDeleter;
            this.sensorStatesAdder = sensorStatesAdder;
            this.hourlyEnqueuer = HourlyEnqueuer;
        }

        [HttpGet]
        [Route("{stationId}/{sensorId}", Name = "SensorYearsOfActivity")]
        public Task<IEnumerable<Models.SensorYearLink>> Years(string stationId, string sensorId)
        {
            var years = stationsPersister.AvailableHistoryOfSensosr(stationId, sensorId);
            return Task.FromResult(years.Select(x => new Models.SensorYearLink
            {
                Year = x,
                YearStatisticLink = Url.Route("SensorYearDailyStatistic", new { stationId = stationId, sensorId = sensorId, year = x })
            }));
        }

        [HttpGet]
        [Route("{stationId}/{sensorId}/{year:int}", Name = "SensorYearDailyStatistic")]
        public Task<IEnumerable<Models.SensorYearDailyState>> Year(string stationId, string sensorId, int year)
        {
            var yearStates = dailyStatePersister.YearStates(stationId, sensorId, year);
            return Task.FromResult(yearStates.Select(x => new Models.SensorYearDailyState
            {
                Day = x.Day,
                State = x.Average,
                DayStatisticLink = Url.Route("SensorDayStatistic", new { stationId = stationId, sensorId = sensorId, year = x.Day.Year, month = x.Day.Month, day = x.Day.Day })
            }));
        }

        [HttpGet]
        [Route("{stationId}/{sensorId}/{year:int}/{month:int}", Name = "SensorMonthDailyStatistic")]
        public Task<IEnumerable<Models.SensorYearDailyState>> Month(string stationId, string sensorId, int year, int month)
        {
            if (month < 1 || month > 12)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            var yearStates = dailyStatePersister.YearStates(stationId, sensorId, year);
            return Task.FromResult(yearStates.Where(x => x.Day.Month == month).Select(x => new Models.SensorYearDailyState
            {
                Day = x.Day,
                State = x.Average,
                DayStatisticLink = Url.Route("SensorDayStatistic", new { stationId = stationId, sensorId = sensorId, year = x.Day.Year, month = x.Day.Month, day = x.Day.Day })
            }));
        }

        [HttpGet]
        [Route("{stationId}/{sensorId}/{year:int}/{month:int}/{day:int}", Name = "SensorDayStatistic")]
        public Task<IEnumerable<Models.SensorHourlyState>> Day(string stationId, string sensorId, int year, int month, int day)
        {
            DateTime dayDate;
            if (!DateTime.TryParseExact($"{year:D4}{month:D2}{day:D2}", "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out dayDate))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            var dayStates = hourlyStatePersister.DayStates(stationId, sensorId, dayDate);
            return Task.FromResult(dayStates.Select(x => new Models.SensorHourlyState
            {
                At = x.At,
                State = x.State,
                Active = x.Active,
            }));
        }

        // http://localhost:2781/api1/sensors/centenario/atmpressure/detail/2016/9/1
        [HttpGet]
        [Route("{stationId}/{sensorId}/detail/{year:int}/{month:int}/{day:int}", Name = "SensorDayDetailStatistic")]
        public Task<IEnumerable<Models.SensorHourlyState>> DayDetail(string stationId, string sensorId, int year, int month, int day)
        {
            DateTime dayDate;
            if (!DateTime.TryParseExact($"{year:D4}{month:D2}{day:D2}", "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out dayDate))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            var dayStates = sensorStatesPersister.DayStates(stationId, sensorId, dayDate);
            return Task.FromResult(dayStates.Select(x => new Models.SensorHourlyState
            {
                At = x.At,
                State = x.State,
                Active = x.Active
            }));
        }

        [HttpPost]
        [Route("{stationId}/{sensorId}/delete/{year:int}/{month:int}/{day:int}/{hora:int}/{min:int}", Name = "SensorStateDelete")]
        public async Task<HttpResponseMessage> Post([FromUri] int min, [FromUri] int hora, [FromUri] string sensorId, [FromUri] string stationId, [FromUri] int day, [FromUri] int month, [FromUri] int year)
        {
            if  (hora<24 && min < 60)
            {
                string monthMM;
                string dayDD;
                string horaHH;
                string minMM;
                if (month < 10)
                    monthMM = "0" + month.ToString();
                else
                    monthMM = month.ToString();
                if (day < 10)
                    dayDD = "0" + day.ToString();
                else
                    dayDD = day.ToString();
                if (hora < 10)
                    horaHH = "0" + hora.ToString();
                else
                    horaHH = hora.ToString();
                if (min < 10)
                    minMM = "0" + min.ToString();
                else
                    minMM = min.ToString();
                var partition = year.ToString() + monthMM + dayDD;
                var rowkey = horaHH + minMM;
                await sensorStatesDeleter.Delete(stationId, sensorId, partition, rowkey);
                DateTime dayDate;
                if (!DateTime.TryParseExact($"{year:D4}{month:D2}{day:D2}", "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out dayDate))
                {
                    throw new HttpResponseException(HttpStatusCode.BadRequest);
                }
                await hourlyEnqueuer.Enqueue(new StationSensorHourlyStateChanged {
                    StationId = stationId,
                    SensorId = sensorId,
                    Day = dayDate
                });
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.Conflict);
            }
        }



        [HttpPost]
        [Route("{stationId}/{sensorId}/add/{year:int}/{month:int}/{day:int}/{hora:int}/{min:int}", Name = "SensorStateAdd")]
        public async Task<HttpResponseMessage> PostAdd([FromUri] int min, [FromUri] int hora, [FromUri] string sensorId, [FromUri] string stationId, [FromUri] int day, [FromUri] int month, [FromUri] int year)
        {
            if (hora < 24 && min < 60)
            {
                string monthMM;
                string dayDD;
                string horaHH;
                string minMM;
                if (month < 10)
                    monthMM = "0" + month.ToString();
                else
                    monthMM = month.ToString();
                if (day < 10)
                    dayDD = "0" + day.ToString();
                else
                    dayDD = day.ToString();
                if (hora < 10)
                    horaHH = "0" + hora.ToString();
                else
                    horaHH = hora.ToString();
                if (min < 10)
                    minMM = "0" + min.ToString();
                else
                    minMM = min.ToString();
                var partition = year.ToString() + monthMM + dayDD;
                var rowkey = horaHH + minMM;
                await sensorStatesAdder.Add(stationId, sensorId, partition, rowkey);
                DateTime dayDate;
                if (!DateTime.TryParseExact($"{year:D4}{month:D2}{day:D2}", "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out dayDate))
                {
                    throw new HttpResponseException(HttpStatusCode.BadRequest);
                }
                await hourlyEnqueuer.Enqueue(new StationSensorHourlyStateChanged
                {
                    StationId = stationId,
                    SensorId = sensorId,
                    Day = dayDate
                });
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.Conflict);
            }
        }
    }

}