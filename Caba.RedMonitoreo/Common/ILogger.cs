using System;

namespace Caba.RedMonitoreo.Common
{
	public interface ILogger
	{
		void LogTrace(Func<string> logMessage);
		void LogWarning(Func<string> logMessage);
		void LogError(Func<string> logMessage);
		void LogCritical(Func<string> logMessage);
	}

	public class NoOpLogger : ILogger
	{
		public static ILogger DoNothing { get; } = new NoOpLogger();
		public void LogTrace(Func<string> logMessage) {}
		public void LogWarning(Func<string> logMessage) {}
		public void LogError(Func<string> logMessage) {}
		public void LogCritical(Func<string> logMessage) {}
	}
}