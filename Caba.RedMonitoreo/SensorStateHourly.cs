using System;

namespace Caba.RedMonitoreo
{
	public class SensorStateHourly
	{
		public DateTimeOffset At { get; set; }
		public double State { get; set; }
        public bool Active { get; set; }
        public double EightHour { get; set; }
        public double FullDay { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }
	}
}