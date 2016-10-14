using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Caba.RedMonitoreo.Data
{
	public interface IStationSensorStateDeleter
	{
		Task Delete(string stationId, string sensorId, string partition, string rowkey);
	}
}