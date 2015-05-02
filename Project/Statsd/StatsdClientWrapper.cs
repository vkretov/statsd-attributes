using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTable.Services.Statsd.Attributes.Statsd
{
	public class StatsdClientWrapper
	{
		public static bool IsEnabled { get; set; }

		public static void Counter(string statName, int value = 1, double sampleRate = 1)
		{
			if (!IsEnabled)
			{
				return;
			}

			StatsdClient.Metrics.Counter(statName, value, sampleRate);
		}

		public static void Timer(string statName, int value, double sampleRate = 1)
		{
			if (!IsEnabled)
			{
				return;
			}

			StatsdClient.Metrics.Timer(statName, value, sampleRate);
		}
	}
}
