using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTable.Services.Statsd.Attributes.Common;
using StatsdClient;

namespace OpenTable.Services.Statsd.Attributes.Statsd
{
	public class StatsdClientFactory
	{
		public static void MakeStatsdClient(
			string statsdServerName, 
			int statsdServerPort, 
			string appEnvironment, 
			string dataCenterRegion,
			string serviceType,
			int failureBackoffSecs = 60, 
			Action failureCallback = null)
		{
			CommonHelpers.TrySleepRetry(

				// action
				() => ConfigureStatsd(statsdServerName, statsdServerPort, appEnvironment, dataCenterRegion, serviceType),

				// backoff between attempts
				TimeSpan.FromSeconds(failureBackoffSecs),

				// enable logging on success
				() => { StatsdClientWrapper.IsEnabled = true; },

				// default failure callback to no-op
				failureCallback ?? new Action(() => { }));
		}


		private static void ConfigureStatsd(string statsdServerName, int statsdServerPort, string appEnvironment, string dataCenterRegion, string serviceType)
		{
			// Save these values for later use in StatsD filter
			StatsdConstants.OtSrviceNameValue = serviceType.ToLower();
			StatsdConstants.OtSrviceEnvirnomentValue = appEnvironment.ToLower();

			if (string.IsNullOrEmpty(dataCenterRegion))
			{
				dataCenterRegion = (Environment.MachineName.Length >= 2 ? Environment.MachineName.Substring(0, 2).ToLower() : "xx");
			}

			var prefix = string.Format(
				"{0}.{1}.{2}.{3}",
				serviceType,
				appEnvironment,
				dataCenterRegion,
				Environment.MachineName).Replace('_', '-').ToLower();

			prefix = CommonHelpers.Sanitize(prefix);

			var metricsConfig = new MetricsConfig
			{
				StatsdServerName = statsdServerName,
				StatsdServerPort = statsdServerPort,
				Prefix = prefix
			};

			Metrics.Configure(metricsConfig);
		}
	}
}
