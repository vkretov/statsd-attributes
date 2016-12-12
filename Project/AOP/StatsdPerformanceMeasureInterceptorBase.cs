using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using OpenTable.Services.Statsd.Attributes.Statsd;

namespace OpenTable.Services.Statsd.Attributes.AOP
{
	public abstract class StatsdPerformanceMeasureInterceptorBase : IInterceptor
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

		private readonly ConcurrentDictionary<RuntimeMethodHandle, ICountEmitter> _countEmitters =
			new ConcurrentDictionary<RuntimeMethodHandle, ICountEmitter>();

		private ThreadLocal<ConcurrentDictionary<RuntimeMethodHandle, Stopwatch>> _stopWatchThreadLocal;

		protected StatsdPerformanceMeasureInterceptorBase()
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

			Exception exception = null;
			try
			{
				StartTimer(invocation);
				invocation.Proceed();
			}
			catch (Exception ex)
			{
				exception = ex;
				throw;
			}
			finally
			{
				var stopwatch = _stopWatchThreadLocal.Value[invocation.Method.MethodHandle];
				if (IsTask(invocation))
				{
					((Task) invocation.ReturnValue).ContinueWith((task) => StopTimerAndReport(invocation, stopwatch, task.Exception));
				}
				else
				{
					StopTimerAndReport(invocation, stopwatch, exception);
				}
			}
		}

		private bool IsTask(IInvocation invocation)
		{
			return (invocation.MethodInvocationTarget.ReturnType.IsGenericType &&
			        invocation.MethodInvocationTarget.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)) ||
			       invocation.MethodInvocationTarget.ReturnType == typeof(Task);
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

		private void StopTimerAndReport(IInvocation invocation, Stopwatch stopwatch, Exception exception = null)
		{
			if (!StatsdClientWrapper.IsEnabled)
			{
				return;
			}

			var elapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds;
			stopwatch.Reset();

			var count = 1;
			if (_countEmitters.ContainsKey(invocation.Method.MethodHandle))
			{
				var countEmitter = _countEmitters[invocation.Method.MethodHandle];
				if (countEmitter != null)
				{
					count = countEmitter.EmitCount(invocation);
				}
			}

			var actionName = invocation.Method.Name.ToLower();

			var metricName = GetMetricName(invocation,actionName, exception);

			StatsdClientWrapper.Timer(metricName, elapsedMilliseconds);
			StatsdClientWrapper.Counter(metricName, count);
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

				var statsdMeasuredMethodAttribute
					= invocation.MethodInvocationTarget
								.GetCustomAttributes(typeof (StatsdMeasuredMethodAttribute), false)
								.OfType<StatsdMeasuredMethodAttribute>()
								.FirstOrDefault();
				if (statsdMeasuredMethodAttribute != null)
				{
					_scanned[invocation.Method.MethodHandle] = true;
					if (statsdMeasuredMethodAttribute.CountEmitter != null)
					{
						// counting class should be a type that implements ICountEmitter;
						// capture an instance once for later use
						var obj = Activator.CreateInstance(statsdMeasuredMethodAttribute.CountEmitter) as ICountEmitter;
						if (obj != null)
						{
							_countEmitters[invocation.Method.MethodHandle] = obj;
						}
					}
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

		protected abstract string GetMetricName(IInvocation invocation, string actionName, Exception exception);
	}
}
