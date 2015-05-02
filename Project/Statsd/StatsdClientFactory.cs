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
		public static TimeSpan? FailureBackoff { get; set; }

		public static Action FailureCallback { get; set; }

		public static void MakeStatsdClient(
			string statsdServerName, 
			int statsdServerPort, 
			string appEnvironment, 
			string serviceType,
			int failureBackoffSecs = 60, 
			Action failureCallback = null)
		{
			CommonHelpers.TrySleepRetry(

				// action
				() => ConfigureStatsd(statsdServerName, statsdServerPort, appEnvironment, serviceType),

				// backoff between attempts
				TimeSpan.FromSeconds(failureBackoffSecs),

				// enable logging on success
				() => { StatsdClientWrapper.IsEnabled = true; },

				// default failure callback to no-op
				FailureCallback ?? new Action(() => { }));
		}


		private static void ConfigureStatsd(string statsdServerName, int statsdServerPort, string appEnvironment, string serviceType)
		{
			// Save these values for later use in StatsD filter
			StatsdConstants.OtSrviceNameValue = serviceType.ToLower();
			StatsdConstants.OtSrviceEnvirnomentValue = appEnvironment.ToLower();

			// Will be using the frist two characters of the machine name as application serving region.
			// In case machine name is not set will default to xx
			var appServingRegion = "xx";
			if (Environment.MachineName.Length >= 2)
				appServingRegion = Environment.MachineName.Substring(0, 2).ToLower();

			var prefix = string.Format(
				"{0}.{1}.{2}.{3}",
				serviceType,
				appEnvironment,
				appServingRegion,
				Environment.MachineName).Replace('_', '-').ToLower();

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
