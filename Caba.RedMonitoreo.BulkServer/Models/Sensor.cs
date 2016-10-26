using System;

namespace Caba.RedMonitoreo.BulkServer.Models
{
	public class SensorLink
	{
		public string Id { get; set; }
		public string YearOfActivitiesLink { get; set; }
        public bool Active { get; set;  }
	}

	public class SensorYearLink
	{
		public int Year { get; set; }
		public string YearStatisticLink { get; set; }
	}

	public class SensorYearDailyState
	{
		public DateTime Day { get; set; }
		public double State { get; set; }
		public string DayStatisticLink { get; set; }
	}

	public class SensorHourlyState
	{
		public DateTimeOffset At { get; set; }
		public double State { get; set; }
        public bool Active { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }
        public double EightHour { get; set; }
        public double FullDay { get; set; }
	}

    public class SensorDetailState
    {
        public DateTimeOffset At { get; set; }
        public double State { get; set; }
        public bool Active { get; set; }
    }
}