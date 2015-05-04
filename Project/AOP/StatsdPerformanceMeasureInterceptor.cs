using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Castle.DynamicProxy;
using OpenTable.Services.Statsd.Attributes.Statsd;
using OpenTable.Services.Statsd.Attributes.Common;

namespace OpenTable.Services.Statsd.Attributes.AOP
{
	public class StatsdPerformanceMeasureInterceptor : IInterceptor
	{
		// Since Windsor Castel's Interceptor annotation/mechanisme is a class level, 
		// meaning that all public calls will get intercepted, we use this list here 
		// to specify which method to intercept.  In order to do this check once,
		// results are cached in a dictionary.
		public List<string> RegexSelector { get; set; }

		private readonly ConcurrentDictionary<RuntimeMethodHandle, bool> _scanned =
			new ConcurrentDictionary<RuntimeMethodHandle, bool>();

		private ThreadLocal<ConcurrentDictionary<RuntimeMethodHandle, Stopwatch>> _stopWatchThreadLocal;

		public StatsdPerformanceMeasureInterceptor()
		{
			_stopWatchThreadLocal = new ThreadLocal<ConcurrentDictionary<RuntimeMethodHandle, Stopwatch>>();
			RegexSelector = new List<string>();
		}

		public void Intercept(IInvocation invocation)
		{
			if (!CanIntercept(invocation))
			{
				invocation.Proceed();
				return;
			}

			var exceptionThrown = false;
			try
			{
				StartTimer(invocation);
				invocation.Proceed();
			}
			catch (Exception)
			{
				exceptionThrown = true;
				throw;
			}
			finally
			{
				StopTimerAndReport(invocation, exceptionThrown);
			}
		}

		private void StartTimer(IInvocation invocation)
		{
			if (!_stopWatchThreadLocal.IsValueCreated) 
			{
				_stopWatchThreadLocal.Value = new ConcurrentDictionary<RuntimeMethodHandle, Stopwatch>();
			}

			var stopwatch = new Stopwatch();
			stopwatch.Start();
			_stopWatchThreadLocal.Value[invocation.Method.MethodHandle] = stopwatch;
		}

		private void StopTimerAndReport(IInvocation invocation, bool exceptionThrown = false)
		{
			if (!StatsdClientWrapper.IsEnabled)
			{
				return;
			}

			var stopwatch = _stopWatchThreadLocal.Value[invocation.Method.MethodHandle];

			var elapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds;
			stopwatch.Reset();

			var actionName = invocation.Method.Name.ToLower();

			var metricName = string.Format(
				"{0}.{1}.{2}.{3}.{4}.{5}",
				StatsdConstants.MethodCall,
				CommonHelpers.GetReferringServiceFromHttpContext(),
				actionName,
				exceptionThrown
					? "failure"
					: "success",
				StatsdConstants.Undefined,
				StatsdConstants.Undefined).ToLower();

			StatsdClient.Metrics.Timer(metricName, elapsedMilliseconds);
			StatsdClient.Metrics.Counter(metricName);
		}

		private bool CanIntercept(IInvocation invocation)
		{
			if (RegexSelector == null || RegexSelector.Count == 0)
			{
				return true;
			}

			if (!_scanned.ContainsKey(invocation.Method.MethodHandle))
			{
				if (RegexSelector.Any(regex => Regex.IsMatch(invocation.Method.Name, regex)))
				{
					_scanned[invocation.Method.MethodHandle] = true;
					return true;
				}

				_scanned[invocation.Method.MethodHandle] = false;
			}

			return _scanned[invocation.Method.MethodHandle];
		}
	}
}
