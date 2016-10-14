using System.Collections.Generic;
using System.Threading.Tasks;

namespace Caba.RedMonitoreo.Data
{
	public interface IStationSensorDailyStatePersister
	{
		IEnumerable<SensorDayAverageState> YearStates(string stationId, string sensorId, int year);
		Task Persist(string stationId, string sensorId, SensorDayAverageState state);
	}
}