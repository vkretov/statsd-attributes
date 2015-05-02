using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenTable.Services.Statsd.Attributes.Common
{
	public class CommonHelpers
	{
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
						}
					}

					Thread.Sleep(sleep);
				}
			});
		}
	}
}
