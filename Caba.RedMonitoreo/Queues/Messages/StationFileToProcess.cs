using System;

namespace Caba.RedMonitoreo.Queues.Messages
{
	public class StationFileToProcess
	{
		public string StationId { get; set; }
		public DateTimeOffset RecivedAt { get; set; }
		public string FilePath { get; set; }
	}
}