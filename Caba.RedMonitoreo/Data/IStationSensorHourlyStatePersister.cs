using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Caba.RedMonitoreo.Data
{
	public interface IStationSensorHourlyStatePersister
	{
		IEnumerable<SensorState> DayStates(string stationId, string sensorId, DateTime day);
		Task Persist(string stationId, string sensorId, IEnumerable<SensorState> states);
	}
}