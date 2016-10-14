using System;
using System.Diagnostics;

namespace Caba.RedMonitoreo.Common
{
	public class DiagnosticLogger: ILogger
	{
		public void LogTrace(Func<string> logMessage)
		{
			if (logMessage == null)
			{
				return;
			}
			Trace.TraceInformation(logMessage());
		}

		public void LogWarning(Func<string> logMessage)
		{
			if (logMessage == null)
			{
				return;
			}
			Trace.TraceWarning(logMessage());
		}

		public void LogError(Func<string> logMessage)
		{
			if (logMessage == null)
			{
				return;
			}
			Trace.TraceError(logMessage());
		}

		public void LogCritical(Func<string> logMessage)
		{
			if (logMessage == null)
			{
				return;
			}
			Trace.TraceError(logMessage());
		}
	}
}