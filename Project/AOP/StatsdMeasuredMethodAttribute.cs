using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTable.Services.Statsd.Attributes.AOP
{
	[AttributeUsage(AttributeTargets.Method)]
	public class StatsdMeasuredMethodAttribute : Attribute
	{
	}
}
