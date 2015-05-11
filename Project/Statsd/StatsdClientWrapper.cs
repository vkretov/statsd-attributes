using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTable.Services.Statsd.Attributes.Common;

namespace OpenTable.Services.Statsd.Attributes.Statsd
{
	internal class StatsdClientWrapper
	{
		public static bool IsEnabled { get; set; }

		public static void Counter(string statName, int value = 1, double sampleRate = 1)
		{
			if (!IsEnabled)
			{
				return;
			}

			StatsdClient.Metrics.Counter(CommonHelpers.Sanitize(statName), value, sampleRate);
		}

		public static void Timer(string statName, int value, double sampleRate = 1)
		{
			if (!IsEnabled)
			{
				return;
			}

			StatsdClient.Metrics.Timer(CommonHelpers.Sanitize(statName), value, sampleRate);
		}
	}
}
