using System;
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

        public static void MakeFakeStatsdClient(
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
                () => ConfigureFakeStatsd(statsdServerName, statsdServerPort, appEnvironment, dataCenterRegion, serviceType),

                // backoff between attempts
                TimeSpan.FromSeconds(failureBackoffSecs),

                // enable logging on success
                () =>
                {
                    StatsdClientWrapper.IsEnabled = true;
                    StatsdClientWrapper.IsFake = true;
                },

                // default failure callback to no-op
                failureCallback ?? new Action(() => { }));
        }


        private static void ConfigureStatsd(string statsdServerName, int statsdServerPort, string appEnvironment, string dataCenterRegion, string serviceType)
        {
            var prefix = GetPrefix(appEnvironment, dataCenterRegion, serviceType);
            var metricsConfig = new MetricsConfig
            {
                StatsdServerName = statsdServerName,
                StatsdServerPort = statsdServerPort,
                Prefix = prefix
            };

            Metrics.Configure(metricsConfig);
        }

        private static void ConfigureFakeStatsd(string statsdServerName, int statsdServerPort, string appEnvironment, string dataCenterRegion, string serviceType)
        {
            var prefix = GetPrefix(appEnvironment, dataCenterRegion, serviceType);
            Console.WriteLine("Fake Statsd client configured with serverName: [{0}], port: [{1}], prefix: [{2}].", statsdServerName, statsdServerPort, prefix);
        }

        private static string GetPrefix(string appEnvironment, string dataCenterRegion, string serviceType)
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

            return prefix;
        }
    }
}
