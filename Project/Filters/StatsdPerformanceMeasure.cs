using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenTable.Services.Statsd.Attributes.Common;
using OpenTable.Services.Statsd.Attributes.Statsd;

namespace OpenTable.Services.Statsd.Attributes.Filters
{
	public class StatsdPerformanceMeasure : IDisposable
	{
		private Stopwatch _stopwatch = new Stopwatch();
		
		public StatsdPerformanceMeasure(string actionName)
		{
			ActionName = actionName;
			_stopwatch.Start();
		}

		public string ActionName { get; private set; }

		public void Dispose()
		{
			if (string.IsNullOrEmpty(ActionName))
			{
				return;
			}
			_stopwatch.Stop();

			// http://stackoverflow.com/questions/2830073/detecting-a-dispose-from-an-exception-inside-using-block
			bool isInException = (Marshal.GetExceptionPointers() != IntPtr.Zero || Marshal.GetExceptionCode() != 0);

			var metricName = CommonHelpers.MetricName(isInException, ActionName);
			StatsdClientWrapper.Timer(metricName, (int) _stopwatch.ElapsedMilliseconds);
			StatsdClientWrapper.Counter(metricName);
		}
	}
}
