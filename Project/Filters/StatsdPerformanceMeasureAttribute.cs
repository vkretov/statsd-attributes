using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using OpenTable.Services.Statsd.Attributes.Statsd;
using StatsdClient;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace OpenTable.Services.Statsd.Attributes.Filters
{
	[AttributeUsage(AttributeTargets.Method, Inherited = true)]
	public class StatsdPerformanceMeasureAttribute : ActionFilterAttribute
	{
		private readonly string _suppliedActionName;

		private readonly string _stopwatchKey;

		private readonly int _maxUserAgentLength = 60;

		// Had to add this "work around" of casting null to string to get team city to build.
		// http://blog.lukebennett.com/2013/06/18/c-compiler-error-cs0182-when-building-via-teamcity/
		public StatsdPerformanceMeasureAttribute(string suppliedActionName = (string) null)
		{
			_suppliedActionName = suppliedActionName;

			_stopwatchKey = "StatsdPerformanceMeasureAttribute_stopwatchKey";
		}

		public override void OnActionExecuting(HttpActionContext actionContext)
		{
			try
			{
				if (HttpContext.Current == null)
					return;

				var stopwatch = new Stopwatch();
				HttpContext.Current.Items[_stopwatchKey] = stopwatch;
				stopwatch.Start();
			}
			finally
			{
				base.OnActionExecuting(actionContext);
			}
		}

		public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
		{
			try
			{
				if (HttpContext.Current == null || actionExecutedContext.Response == null) return;

				var stopwatch = HttpContext.Current.Items[_stopwatchKey] as Stopwatch;

				if (stopwatch == null) return;

				var elapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds;
				stopwatch.Reset();

				var actionName = string.IsNullOrEmpty(_suppliedActionName)
									? actionExecutedContext.ActionContext.ActionDescriptor.ActionName
									: _suppliedActionName;

				var apiVersion = GetApiVersion(actionExecutedContext);

				var httpVerb = actionExecutedContext.Request.Method.ToString();

				var statusCode = (int)actionExecutedContext.Response.StatusCode;

				// Look for endpoint name in the response headers, if one exists we will use it to 
				// publish metircs.  If not will default to action name and will set the header
				// value as such for upstream services. 
				IEnumerable<string> headerValues;
				var otEndpoint = string.Format("{0}-{1}", actionName, apiVersion).ToLower();
				if (actionExecutedContext.Response.Headers.TryGetValues(StatsdConstants.OtEndpoint, out headerValues))
				{
					otEndpoint = string.Format("{0}-{1}", headerValues.FirstOrDefault() ?? actionName, apiVersion).ToLower();
				}
				else
				{
					actionExecutedContext.Response.Headers.Add(StatsdConstants.OtEndpoint, otEndpoint);
				}

				var metricName = string.Format(
					"{0}.{1}.{2}.{3}.{4}.{5}",
					StatsdConstants.HttpRequestIn,
					GetReferringService(actionExecutedContext),
					otEndpoint,
					actionExecutedContext.Response.IsSuccessStatusCode
						? StatsdConstants.HighlevelStatus.Success
						: StatsdConstants.HighlevelStatus.Failure,
					httpVerb,
					statusCode).Replace('_', '-').ToLower();

				Metrics.Timer(metricName, elapsedMilliseconds);
				Metrics.Counter(metricName);

				EnrichResponseHeaders(actionExecutedContext);
			}
			finally
			{
				base.OnActionExecuted(actionExecutedContext);
			}
		}

		private void EnrichResponseHeaders(HttpActionExecutedContext actionExecutedContext)
		{
			if (!actionExecutedContext.Response.Headers.Contains(StatsdConstants.OtSrviceName) &&
				!string.IsNullOrEmpty(StatsdConstants.OtSrviceNameValue))
			{
				actionExecutedContext.Response.Headers.Add(
					StatsdConstants.OtSrviceName,
					StatsdConstants.OtSrviceNameValue);
			}
		}

		private string GetApiVersion(HttpActionExecutedContext httpActionExecutedContext)
		{
			var path = httpActionExecutedContext.Request.RequestUri.AbsolutePath;
			var match = Regex.Match(path, @"availability/(\w+)/", RegexOptions.IgnoreCase);

			if (match.Success)
				return match.Groups[1].Value;
			return "v1";
		}

		private string GetReferringService(HttpActionExecutedContext httpActionExecutedContext)
		{
			// fetch referring service from request headers 
			IEnumerable<string> headerValues;
			string otReferringservice = null;
			if (httpActionExecutedContext.Request.Headers.TryGetValues(StatsdConstants.OtReferringservice, out headerValues))
			{
				otReferringservice = headerValues.FirstOrDefault();

				if (!string.IsNullOrEmpty(otReferringservice))
					otReferringservice = otReferringservice.Replace('.', '_');
			}

			// fetch user agent from request headers
			var match = Regex.Match(
				httpActionExecutedContext.Request.Headers.UserAgent.ToString(),
				@"^(\w+)/",
				RegexOptions.IgnoreCase);

			var userAgent = (match.Success) ? match.Groups[1].Value.Replace('.', '_') : null;

			if (!string.IsNullOrEmpty(userAgent))
				userAgent = new string(userAgent.Take(_maxUserAgentLength).ToArray());

			return otReferringservice ?? (userAgent ?? "undefined");
		}
	}
}
