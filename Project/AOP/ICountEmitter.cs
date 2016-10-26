using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.DynamicProxy;

namespace OpenTable.Services.Statsd.Attributes.AOP
{
	public interface ICountEmitter
	{
		int EmitCount(IInvocation invocation);
	}
}
