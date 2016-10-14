using System;

namespace Caba.IotUtils.Rest.SimulatedDevice
{
	public class SensorSimulator: ISensorSimulator
	{
		private readonly string stationId;
		private readonly string sensorId;
		private readonly double minValue;
		private readonly Random random;
		private readonly Func<double, double> round;
		private readonly double range;

		public SensorSimulator(string stationId, string sensorId, double minValue, double maxValue, Func<double, double> round= null)
		{
			this.stationId = stationId;
			this.sensorId = sensorId;
			this.minValue = minValue;
			random = new Random(DateTime.Now.Millisecond);
			range = maxValue - minValue;
			this.round = round ?? (x=> x);
		}

		private double CreateValue()
		{
			var ran = random.NextDouble();
			var value = range * ran + minValue;
			return round(value);
		}

		public string GetJsonMessage()
		{
			return $"{{\"stationId\": \"{stationId}\",\"sensorId\": \"{sensorId}\", \"state\": {CreateValue()} }}";
		}
	}
}