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
	        string dataCenterRegion,
	        string serviceType,
	        int failureBackoffSecs = 60,
	        Action failureCallback = null,
	        Func<Exception, HttpActionExecutedContext, HttpStatusCode> exceptionToStatusCode = null){}
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
					null, //// <- null in this case will cause the lib to determine the datacenter
					      ////    from the machine name, basically using the first two chars machine name  
					"availability-na",
					60,
					() => _logger.LogError("statsd initialization failed"));
}
```
*It is important that the service type follows this format, `appname-region`*  In the above example appname is **availability** and region is **na**.  Failure back off is the time in seconds to wait between StatsD client initialization in case of a failure.  Failure callback is just that, a facility to pass in an action to be executed up on Statsd client initialization, in our case we just log it.

###Class Annotation
To get metrics reported on on all controller class methods just annotate the class. 
```C#
[StatsdPerformanceMeasure]
public class UserSyncController : ApiController
{
   //// tons of fantastic code....
}
```

#####Published Metrics
-  statsd.timers.userservice-na.dev.vm.vmmbpltahmazyan.http-request-in.curl.postsyncglobaluser-v0.success.post.200.mean

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

Another way to register the interceptor is by using Castle Windsor directly. This is equivalent to the class-level annotation above.
```C#
	container.Register(Component.For<IUserService>()
		.ImplementedBy<UserService>()
		.Interceptors(InterceptorReference.ForType<StatsdPerformanceMeasureInterceptor>()).Anywhere);
```

Another interceptor is StatsdPerformanceMeasureForServiceCallsInterceptor. This is meant to log external service calls made from the application. It log the service name, http method, and will attempt to capture the http status code.
- if no exception is thrown, then it will mark the request as "success" and default the httpStatus to 200.
- If an exception is thrown, then the exceptionToStatusCode callback will be called. When there is no exceptionToStatusCode provided, the status code will be defaulted to 500.

The service name and http method are passed through StatsdMeasuredServiceCallAttribute. This attribute can be set on the class-level or on method-level, or on both. Method-level attribute will have first priority.


```C#
	//register the interceptor with the service.
	container.Register(Component.For<IUserService>()
		.ImplementedBy<UserService>()
		.Interceptors(InterceptorReference.ForType<StatsdPerformanceMeasureForServiceCallsInterceptor>()).Anywhere);
		
	//UserService.cs:
	public class UserService : IUserService
	{
		[StatsdMeasuredServiceCall(Name = "UserService", HttpMethod = "Get")]
		public long GetUserGpIdByLogin(string loginName)
		{
			//call service
			return _service.GetUserGpIdByLogin(loginName);
		}

		[StatsdMeasuredServiceCall(Name = "UserService", HttpMethod = "Post")]
		public UserRegistrationDetails CreateUser(RegistrationCriteria registrationCriteria)
		{
			//call service
			return _service.CreateUser(registrationCriteria);
		}
		
		[StatsdMeasuredServiceCall(Name = "UserService", HttpMethod = "Patch")]
		public void UpdateUser(long globalPersonId, UserInfoUpdate newInfo)
		{
			//call service
			return _service.PatchUser(globalPersonId, newInfo);
		}
	}
```

List of some of the published metrics:
- statsd.timers.mobileweb-na.pp.wi.win-devbox.service-call.userservice.getusergpidbylogin.failure.get.404.mean
- statsd.timers.mobileweb-na.pp.wi.win-devbox.service-call.userservice.getusergpidbylogin.success.get.200.lower

- statsd.timers.mobileweb-na.pp.wi.win-devbox.service-call.userservice.updateuser.success.patch.200.mean
- statsd.timers.mobileweb-na.pp.wi.win-devbox.service-call.userservice.createuser.success.post.200.mean

###Publishing Metrics Without Annotation
We can publish metrics for a specific block of code by enclosing the block within a using block which creates a StatsdPerformanceMeasure.  This does not require annotations on either the class or the method.
```C#

namespace OT.Services.UserService.DataAccess.ServicesAccess.RestaurantService
{
	public class RestaurantInfoProvider : IRestaurantInfoProvider
	{
		public void SomeMethod() {
			using (var m = new StatsdPerformanceMeasure("TestMetric"))
			{
				// marking arbitrary code fragment, now it will publish count and execution duration.
				// <arbitrary code>
				// 
			}
		}
	}
}
```

If exception tracking is needed for the metric, StatsdPerformanceMeasure exposes the boolean ExceptionThrown property, which defaults to false.  An example follows.
```C#

namespace OT.Services.UserService.DataAccess.ServicesAccess.RestaurantService
{
	public class RestaurantInfoProvider : IRestaurantInfoProvider
	{
		public void SomeMethod() {
			using (var m = new StatsdPerformanceMeasure("TestMetric"))
			{
				try {
					// some code

				}
				catch (Exception)
				{
					m.ExceptionThrown = true;
				}
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

