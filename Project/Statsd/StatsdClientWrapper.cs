using System;
using OpenTable.Services.Statsd.Attributes.Common;

namespace OpenTable.Services.Statsd.Attributes.Statsd
{
    internal class StatsdClientWrapper
    {
        public static bool IsFake { get; set; }

        public static bool IsEnabled { get; set; }

        public static void Counter(string statName, int value = 1, double sampleRate = 1)
        {
            if (!IsEnabled)
            {
                return;
            }

            statName = CommonHelpers.Sanitize(statName);
            if (IsFake)
            {
                Console.WriteLine("FakeStatsdClient - Counter Metric Published [name: {0}, value: {1}, sampleRate: {2}]", statName, value, sampleRate);
            }

            StatsdClient.Metrics.Counter(statName, value, sampleRate);
        }

        public static void Timer(string statName, int value, double sampleRate = 1)
        {
            if (!IsEnabled)
            {
                return;
            }

            statName = CommonHelpers.Sanitize(statName);
            if (IsFake)
            {
                Console.WriteLine("FakeStatsdClient - Timer Metric Published [name: {0}, value: {1}, sampleRate: {2}]", statName, value, sampleRate);
            }

            StatsdClient.Metrics.Timer(CommonHelpers.Sanitize(statName), value, sampleRate);
        }
    }
}
