using System;
using Castle.DynamicProxy;
using OpenTable.Services.Statsd.Attributes.Common;

namespace OpenTable.Services.Statsd.Attributes.AOP
{
	public class StatsdPerformanceMeasureInterceptor : StatsdPerformanceMeasureInterceptorBase
	{
		protected override string GetMetricName(IInvocation invocation, string actionName, Exception exception)
		{
			return CommonHelpers.MetricName(exception != null, actionName);
		}
	}
}
