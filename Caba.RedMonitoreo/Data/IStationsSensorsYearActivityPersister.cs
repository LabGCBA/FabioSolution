using System.Collections.Generic;
using System.Threading.Tasks;

namespace Caba.RedMonitoreo.Data
{
	public interface IStationsSensorsYearActivityPersister
	{
		IEnumerable<Station> Stations();
		IEnumerable<Sensor> Sensors(string stationId);
		IEnumerable<int> AvailableHistoryOfSensosr(string stationId, string sensorId);
		Task Persist(string stationId, string sensorId, int year);
	}
}