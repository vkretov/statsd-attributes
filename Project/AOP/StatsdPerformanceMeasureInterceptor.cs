using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

		// memoized result of InterceptAllMethods, run once on first call to intercept
		private bool? _interceptAllMethods = null;

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

			var metricName = CommonHelpers.MetricName(exceptionThrown, actionName);

			StatsdClientWrapper.Timer(metricName, elapsedMilliseconds);
			StatsdClientWrapper.Counter(metricName);
		}

		private bool CanIntercept(IInvocation invocation)
		{
			if (_interceptAllMethods == null)
			{
				_interceptAllMethods = InterceptAllMethods(invocation);
			}

			if (_interceptAllMethods.Value)
			{
				return true;
			}

			if (!_scanned.ContainsKey(invocation.Method.MethodHandle))
			{
				if (RegexSelector != null 
					&& RegexSelector.Any(regex => Regex.IsMatch(invocation.Method.Name, regex)))
				{
					_scanned[invocation.Method.MethodHandle] = true;
					return true;
				}

				if (invocation.MethodInvocationTarget.GetCustomAttributes(typeof (StatsdMeasuredMethodAttribute), false).Any())
				{
					_scanned[invocation.Method.MethodHandle] = true;
					return true;
				}

				_scanned[invocation.Method.MethodHandle] = false;
			}

			return _scanned[invocation.Method.MethodHandle];
		}

		private bool InterceptAllMethods(IInvocation invocation)
		{
			// if we have a non-empty regex selector, return false
			if (RegexSelector != null && RegexSelector.Any())
			{
				return false;
			}

			// if we have any methods which are explicitly marked as statsd measured methods, return false
			var type = invocation.InvocationTarget.GetType();
			if (type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly)
						.Any(m => m.GetCustomAttributes(typeof (StatsdMeasuredMethodAttribute), false).Any()))
			{
				return false;
			}

			// otherwise true
			return true;
		}
	}
}
