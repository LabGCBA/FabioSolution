using System;

namespace Caba.RedMonitoreo.Queues.Messages
{
	public class StationSensorHourlyStateChanged
	{
		public string StationId { get; set; }
		public string SensorId { get; set; }
		public DateTime Day { get; set; }
	}
}