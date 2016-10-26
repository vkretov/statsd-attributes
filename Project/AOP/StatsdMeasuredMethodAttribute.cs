using System;

namespace OpenTable.Services.Statsd.Attributes.AOP
{
	[AttributeUsage(AttributeTargets.Method)]
	public class StatsdMeasuredMethodAttribute : Attribute
	{
		public string Name { get; set; }
		public Type CountEmitter { get; set; }
	}
}
