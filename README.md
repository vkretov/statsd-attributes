# statsd-attributes<br />
C# attributes for statsd performance measurement<br />
Authors:
--------
* Lu Tahmazyan<br />
* Bill Stanton<br />

MyGet Pacakge and Build:
------------------------
* MyGet Feed: https://opentable.myget.org/feed/dev/package/nuget/OpenTable.Services.Statsd.Attributes<br />
* Teamcity Build: http://teamcity.otcorp.opentable.com/viewType.html?buildTypeId=OpenTablePackages_PackageOpenTableServicesStatsdAttributes<br />

How To
-----
###Configuration
To configure our StatsD client, all one has to do is call `OpenTable.Services.Statsd.Attributes.Statsd.StatsdClientFactory`.

```C#
namespace OpenTable.Services.Statsd.Attributes.Statsd
{
	public class StatsdClientFactory
	{
		public static void MakeStatsdClient(
			string statsdServerName, 
			int statsdServerPort, 
			string appEnvironment, 
			string serviceType,
			int failureBackoffSecs = 60, 
			Action failureCallback = null){}
	}
}
```

Example client configuration 
```C# 
protected void Application_Start()
{
	//// init stuff

	StatsdClientFactory.MakeStatsdClient(
					"statsd-qa-sf.otenv.com",
					8125,
					"dev",
					"availability-na",
					60,
					() => _logger.LogError("statsd initialization failed"));
}
```
*It is important that the service type follows this format, `appname-region`*  In the above example appname is **availability** and region is **na**.  Failure back off is the time in seconds to wait between StatsD client initialization in case of a failure.  Failure callback is just that, a facility to pass in an action to be executed up on Statsd client initialization, in our case we just log it.


###Controller Annotation
Annotate controller method with specifying a name, in this case the method name, *Transactions*.  Second parameter *ApiVersionPattern*, is used in specifying a regular expression for the purposes of determining the version of the API.
```C#
[HttpGet]
[StatsdPerformanceMeasure(SuppliedActionName = "Transactions", ApiVersionPattern = @"user/(\w+)/", DefaultApiVersion = "v1")]
public HttpResponseMessage Transactions(){}
```

#####Published Metrics
List of some of the published metrics:
-  statsd.timers.userservice-na.dev.vm.vmmbpltahmazyan.http-request-in.curl-lustest.**transactions-v2**.success.get.200.mean_90
-  statsd.timers.userservice-na.dev.vm.vmmbpltahmazyan.http-request-in.curl-lustest.**transactions-v1**.success.get.200.mean_90


*For description of each of the metric key fields, please see https://docs.google.com/document/d/1FtVW1j46BMo_T9oFYvXLqQru8E_wYxFaJFnc_JjW6R4/edit#heading=h.sc77bui2ektl*

###Method Annotation
First, we'll need to register an interceptor with Castle Windsor IoC container. 

```C#
namespace OT.Services.UserService.API.Utilities
{
	public class WindsorInstaller : IWindsorInstaller
	{
		public void Install(IWindsorContainer container, IConfigurationStore store)
		{
			container.Register(
				Component
					.For<StatsdPerformanceMeasureInterceptor>()
					.ImplementedBy<StatsdPerformanceMeasureInterceptor>()
					.LifestyleSingleton());
		}
	}
}
```

Next, we'll annotate the class of interest and mark methods we want metrics for.
```C#

namespace OT.Services.UserService.DataAccess.ServicesAccess.RestaurantService
{
	//// 
	//// (1) class level annotation, marking this class for interception 
	//// 
	[Interceptor(typeof(StatsdPerformanceMeasureInterceptor))]
	public class RestaurantInfoProvider : IRestaurantInfoProvider
	{

		////
		//// (2) marking method, now it will publish count and execution duration.
		////
		[StatsdMeasuredMethod]
		public IEnumerable<Restaurant> GetRestaurantsInfo(List<int> restaurantIds, string language = null) {}

	}
}
```

We can also publish metrics for a specific block of code.
```C#

namespace OT.Services.UserService.DataAccess.ServicesAccess.RestaurantService
{
	public class RestaurantInfoProvider : IRestaurantInfoProvider
	{
		public void SomeMethod() {
			using (var m = new StatsdPerformanceMeasure("TestMetric"))
			{
				// any code that we want to time and count invocations for
				// <arbitrary code>
			}
		}
	}
}
```

#####Published Metrics
List of some of the published metrics:
-  statsd.timers.userservice-na.dev.vm.vmmbpltahmazyan.method-call.curl-lustest.getrestaurantsbylanguage.success.undefined.undefined.mean_90
-  statsd.timers.userservice-na.dev.vm.vmmbpltahmazyan.method-call.curl-lustest.getrestaurantsinfo.success.undefined.undefined.mean_90

*For description of each of the metric key fields, please see https://docs.google.com/document/d/1FtVW1j46BMo_T9oFYvXLqQru8E_wYxFaJFnc_JjW6R4/edit#heading=h.sc77bui2ektl*

