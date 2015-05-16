using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using OpenTable.Services.Statsd.Attributes.Common;
using OpenTable.Services.Statsd.Attributes.Statsd;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace OpenTable.Services.Statsd.Attributes.Filters
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class StatsdPerformanceMeasureAttribute : ActionFilterAttribute
    {
        private const string StopwatchKey = "StatsdPerformanceMeasureAttribute_stopwatchKey";

        private const int MaxUserAgentLength = 60;

        // regular expression to match verion number of Api, e.g.: @"availability/(\w+)/"
        public string ApiVersionPattern { get; set; }

        public string DefaultApiVersion { get; set; }

        public string SuppliedActionName { get; set; }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            try
            {
                var stopwatch = new Stopwatch();
                actionContext.Request.Properties[StopwatchKey] = stopwatch;
                stopwatch.Start();
                CommonHelpers.Headers = actionContext.Request.Headers;
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
                if (!StatsdClientWrapper.IsEnabled)
                {
                    return;
                }

                var stopwatch = actionExecutedContext.Request.Properties[StopwatchKey] as Stopwatch;

                if (stopwatch == null) return;

                var elapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds;
                stopwatch.Reset();

                var actionName = string.IsNullOrEmpty(SuppliedActionName)
                                    ? actionExecutedContext.ActionContext.ActionDescriptor.ActionName
                                    : SuppliedActionName;

                var apiVersion = GetApiVersion(actionExecutedContext);

                var httpVerb = actionExecutedContext.Request.Method.ToString();

                int statusCode = 0;
                var otEndpoint = string.Format("{0}-{1}", actionName, apiVersion).ToLower();
                if (actionExecutedContext.Response != null)
                {
                    statusCode = (int)actionExecutedContext.Response.StatusCode;

                    // Look for endpoint name in the response headers, if one exists we will use it to 
                    // publish metircs.  If not will default to action name and will set the header
                    // value as such for upstream services. 
                    IEnumerable<string> headerValues;
                    if (actionExecutedContext.Response.Headers.TryGetValues(StatsdConstants.OtEndpoint, out headerValues))
                    {
                        otEndpoint = string.Format("{0}-{1}", headerValues.FirstOrDefault() ?? actionName, apiVersion).ToLower();
                    }
                    else
                    {
                        actionExecutedContext.Response.Headers.Add(StatsdConstants.OtEndpoint, otEndpoint);
                    }
                }
                else if (actionExecutedContext.Exception != null)
                {
                    if (CommonHelpers.ExceptionToStatusCode != null)
                    {
                        var statusCodeEnum = CommonHelpers.ExceptionToStatusCode(actionExecutedContext.Exception,
                            actionExecutedContext);
                        statusCode = (int)statusCodeEnum;
                        actionExecutedContext.Response = actionExecutedContext.Request.CreateResponse(statusCodeEnum);
                        actionExecutedContext.Response.Headers.Add(StatsdConstants.OtEndpoint, otEndpoint);
                    }
                }


                var status = IsSuccessStatusCode(statusCode) ? StatsdConstants.HighlevelStatus.Success : StatsdConstants.HighlevelStatus.Failure;
                // not being able to read status code will result in status code being set to undefined
                var statusCodeString = statusCode == 0
                    ? StatsdConstants.Undefined
                    : statusCode.ToString(CultureInfo.InvariantCulture);
                var metricName = string.Format(
                    "{0}.{1}.{2}.{3}.{4}.{5}",
                    StatsdConstants.HttpRequestIn,
                    GetReferringService(actionExecutedContext),
                    otEndpoint,
                    status,
                    httpVerb,
                    statusCodeString).Replace('_', '-').ToLower();

                StatsdClientWrapper.Timer(metricName, elapsedMilliseconds);
                StatsdClientWrapper.Counter(metricName);

                EnrichResponseHeaders(actionExecutedContext);
            }
            finally
            {
                base.OnActionExecuted(actionExecutedContext);
            }
        }

        private static bool IsSuccessStatusCode(int statusCode)
        {
            return statusCode >= 200 && statusCode <= 399;
        }

        private static void EnrichResponseHeaders(HttpActionExecutedContext actionExecutedContext)
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
            if (ApiVersionPattern == null)
            {
                return (DefaultApiVersion ?? "v0");
            }

            var path = httpActionExecutedContext.Request.RequestUri.AbsolutePath;
            var match = Regex.Match(path, ApiVersionPattern, RegexOptions.IgnoreCase);

            if (match.Success)
                return match.Groups[1].Value;

            return (DefaultApiVersion ?? "v0");
        }

        private static string GetReferringService(HttpActionExecutedContext httpActionExecutedContext)
        {
            return CommonHelpers.GetReferringService(httpActionExecutedContext.Request.Headers);
        }
    }
}
