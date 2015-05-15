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
		public bool ExceptionThrown { get; set; }

		public void Dispose()
		{
			if (string.IsNullOrEmpty(ActionName))
			{
				return;
			}

			_stopwatch.Stop();

			//=======================================
			// .NET has a technique to detect whether an exception has been thrown from within a finally:
			// http://stackoverflow.com/questions/2830073/detecting-a-dispose-from-an-exception-inside-using-block
			// (Marshal.GetExceptionPointers() != IntPtr.Zero || Marshal.GetExceptionCode() != 0);
			// however, this is not yet implemented in Mono
			// http://www.go-mono.com/momareports/apis/System.Int32%20System.Runtime.InteropServices.Marshal;;GetExceptionCode%28%29
			// so: caller can manage this property manually in a try-catch nested witin the attribute, if desired
			//=======================================
			var metricName = CommonHelpers.MetricName(ExceptionThrown, ActionName);
			StatsdClientWrapper.Timer(metricName, (int) _stopwatch.ElapsedMilliseconds);
			StatsdClientWrapper.Counter(metricName);
		}
	}
}
