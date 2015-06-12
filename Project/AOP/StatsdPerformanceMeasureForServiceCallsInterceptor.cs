using System;
using System.Linq;
using System.Net;
using Castle.DynamicProxy;
using OpenTable.Services.Statsd.Attributes.Common;
using OpenTable.Services.Statsd.Attributes.Statsd;

namespace OpenTable.Services.Statsd.Attributes.AOP
{
	public class StatsdPerformanceMeasureForServiceCallsInterceptor : StatsdPerformanceMeasureInterceptorBase
	{
		private string GetStatusCode(Exception exception)
		{
			var statusCodeEnum = HttpStatusCode.OK;
			if (exception != null)
			{
				if (CommonHelpers.ExceptionToStatusCode != null)
				{
					statusCodeEnum = CommonHelpers.ExceptionToStatusCode(exception, null);
				}
				else
				{
					//default to 500 if an exception occurred, but ExceptionToStatusCode is not provided.
					statusCodeEnum = HttpStatusCode.InternalServerError;
				}
			}

			return ((int) statusCodeEnum).ToString();
		}

		protected override string GetMetricName(IInvocation invocation, string actionName, Exception exception)
		{
			string httpMethod = null;
			string serviceName = null;

			var attr = GetAttribute(invocation);
			if (attr != null)
			{
				serviceName =  attr.Name;
				httpMethod = attr.HttpMethod;
			}

			var statusCode = GetStatusCode(exception);
			var metricName = string.Format(
			 "{0}.{1}.{2}.{3}.{4}.{5}",
			 StatsdConstants.ServiceCall,
			 serviceName ?? StatsdConstants.Undefined,
			 actionName,
			 CommonHelpers.GetHighlevelStatus(exception == null),
			 httpMethod ?? StatsdConstants.Undefined,
			 statusCode).ToLower();
			return metricName;
		}

		private StatsdMeasuredServiceCallAttribute GetAttribute(IInvocation invocation)
		{
			//get attribute from method or from class.
			var methodAttr = invocation.MethodInvocationTarget.GetCustomAttributes(typeof(StatsdMeasuredServiceCallAttribute), false).FirstOrDefault();
			return  (StatsdMeasuredServiceCallAttribute) (methodAttr ?? invocation.InvocationTarget.GetType().GetCustomAttributes(typeof(StatsdMeasuredServiceCallAttribute), false).FirstOrDefault());
		}
	}
}
