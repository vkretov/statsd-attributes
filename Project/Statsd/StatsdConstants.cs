namespace OpenTable.Services.Statsd.Attributes.Statsd
{
	public class StatsdConstants
	{
		public const string HttpRequestIn = "http-request-in";

		public const string HttpRequestOut = "http-request-out";

		public const string MethodCall = "method-call";

		public const string ServiceCall = "service-call";

		public const string DbCall = "db-call";

		public const string OtReferringservice = "ot-referringservice";

		public const string OtSrviceName = "ot-service-name";

		public static string OtSrviceNameValue { get; set; }

		public const string OtSrviceEnvirnoment = "ot-service-envirnoment";

		public static string OtSrviceEnvirnomentValue { get; set; }

		public const string OtEndpoint = "ot-endpoint";

		public const string Undefined = "undefined";

		public enum HighlevelStatus
		{
			Success,
			Failure,
			Timeout,
			Noconnect
		}
	}
}
