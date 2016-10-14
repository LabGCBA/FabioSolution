using System;

namespace Caba.RedMonitoreo.Queues.Messages
{
	public class StreamSensorAverage
	{
		public string StationId { get; set; }
		public string SensorId { get; set; }
		public DateTimeOffset Time { get; set; }
		public double State { get; set; }
	}
}