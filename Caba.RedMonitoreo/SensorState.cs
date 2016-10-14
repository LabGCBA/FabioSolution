using System;

namespace Caba.RedMonitoreo
{
	public class SensorState
	{
		public DateTimeOffset At { get; set; }
		public double State { get; set; }
        public bool Active { get; set; }
	}
}