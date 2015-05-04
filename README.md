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
*It is important that the service type follows this format, `appname-region`*  In the above example appname is **availability** and region is **na**.  Failure backoff is the time in seconds to wait between StatsD client initialization in case of a failure.  Failure callback is just that, a facility to pass in an action to be executed up on Statsd client initialization, in our case we just log it.


###Controller Annotation
Annotating controller method without specifing a custom name, in which case the method name, `Transactions`, will be used in publishing metrics.  Second parameter `ApiVersionPattern`, is used in specifing a regular expression for the purposes of determining the verson of the API.
```C#
[HttpGet]
[LogOutliers]
[StatsdPerformanceMeasure(ApiVersionPattern = @"user/(\w+)/", DefaultApiVersion = "v1")]
public HttpResponseMessage Transactions(){}
```

Annotating a controller method with a custom name. 
```C#
[StatsdPerformanceMeasure("GlobalUser", ApiVersionPattern = @"user/(\w+)/", DefaultApiVersion = "v1")]
[LogOutliers("GlobalUser")]
public HttpResponseMessage Get(){}
```

###Method Annotation

