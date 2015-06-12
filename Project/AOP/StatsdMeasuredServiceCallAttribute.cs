using System;

namespace OpenTable.Services.Statsd.Attributes.AOP
{
	public class StatsdMeasuredServiceCallAttribute : Attribute
	{
		public string Name { get; set; }
		public string HttpMethod { get; set; }
	}
}
