using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Filters;
using OpenTable.Services.Statsd.Attributes.Statsd;

namespace OpenTable.Services.Statsd.Attributes.Common
{
	public class CommonHelpers
	{
		private static readonly int MaxUserAgentLength = 60;

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

		public static string GetReferringServiceFromHttpContext()
		{
			try
			{
				return GetReferringService(
					HttpContext.Current.Request.Headers,
					HttpContext.Current.Request.UserAgent);
			}
			catch
			{
				return StatsdConstants.OtSrviceNameValue;
			}
		}

		private static string GetReferringService(NameValueCollection headers, string userAgent)
		{
			// fetch referring service from request headers 
			string otReferringservice = null;
			if (ContainsKey(StatsdConstants.OtReferringservice, headers))
			{
				otReferringservice = headers.Get(StatsdConstants.OtReferringservice);

				if (!string.IsNullOrEmpty(otReferringservice))
					otReferringservice = otReferringservice.Replace('.', '_');
			}

			// fetch user agent from request headers
			var match = Regex.Match(
				userAgent,
				@"^(\w+)/",
				RegexOptions.IgnoreCase);

			var userAgentValue = (match.Success) ? match.Groups[1].Value.Replace('.', '_') : null;

			if (!string.IsNullOrEmpty(userAgentValue))
				userAgent = new string(userAgent.Take(MaxUserAgentLength).ToArray());

			return otReferringservice ?? (userAgent ?? "undefined");
		}
		
		private static bool ContainsKey(string key, NameValueCollection collection)
		{
			return collection.AllKeys.Contains(key);
		}
	}
}
