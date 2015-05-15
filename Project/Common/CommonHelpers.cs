using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OpenTable.Services.Statsd.Attributes.Statsd;

namespace OpenTable.Services.Statsd.Attributes.Common
{
	public class CommonHelpers
	{

		[ThreadStatic] public static HttpRequestHeaders Headers;
		
		private const int MaxUserAgentLength = 60;

		public static void TrySleepRetry(Action action, TimeSpan sleep, Action successCallback, Action failureCallback)
		{
			// on a separate thread, 
			// execute the action
			// if it doesn't throw an exception, return
			// otherwise log the exception, sleep, and repeat
			Task.Factory.StartNew(() =>
			{
				while (true)
				{
					try
					{
						action();
						successCallback();
						break;
					}
					catch (Exception e)
					{
						try
						{
							failureCallback();
						}
						catch (Exception)
						{
							// ignored
						}
					}

					Thread.Sleep(sleep);
				}
			},
				TaskCreationOptions.LongRunning);
		}

		public static string GetReferringService()
		{
			try
			{
				return GetReferringService(Headers);
			}
			catch
			{
				return StatsdConstants.OtSrviceNameValue;
			}
		}

		public static string GetReferringService(HttpRequestHeaders headers)
		{
			if (headers == null) return StatsdConstants.Undefined;

			// fetch referring service from request headers 
			IEnumerable<string> headerValues;
			if (headers.TryGetValues(StatsdConstants.OtReferringservice, out headerValues))
			{
				var otReferringservice = headerValues.FirstOrDefault();

				if (!string.IsNullOrEmpty(otReferringservice))
					return otReferringservice.Replace('.', '_');
			}

			// fetch user agent from request headers
			var match = Regex.Match(
				headers.UserAgent.ToString(),
				@"^(\w+)/",
				RegexOptions.IgnoreCase);

			var userAgent = (match.Success) ? match.Groups[1].Value.Replace('.', '_') : null;

			if (!string.IsNullOrEmpty(userAgent))
				userAgent = new string(userAgent.Take(MaxUserAgentLength).ToArray());

			return userAgent ?? "undefined";
		}

		public static string Sanitize(string input)
		{
			const string WhitespaceRegex = @"\s+";
			const string ForwardSlashRegex = @"/";
			const string UncleanCharacterRegex = @"[^a-zA-Z_\-0-9\.]";

			var sanitized = input;
			sanitized = Regex.Replace(sanitized, WhitespaceRegex, "_");
			sanitized = Regex.Replace(sanitized, ForwardSlashRegex, "-");
			sanitized = Regex.Replace(sanitized, UncleanCharacterRegex, "");
			return sanitized;
		}

		public static string MetricName(bool exceptionThrown, string actionName)
		{
			var metricName = string.Format(
				"{0}.{1}.{2}.{3}.{4}.{5}",
				StatsdConstants.MethodCall,
				CommonHelpers.GetReferringService(),
				actionName,
				exceptionThrown
					? "failure"
					: "success",
				StatsdConstants.Undefined,
				StatsdConstants.Undefined).ToLower();
			return metricName;
		}
	}
}
