namespace OpenTable.Services.Statsd.Attributes.Statsd
{
	public class StatsdConstants
	{
		public static readonly string HttpRequestIn = "http-request-in";

		public static readonly string HttpRequestOut = "http-request-out";

		public static readonly string MethodCall = "method-call";

		public static readonly string DbCall = "db-call";

		public static readonly string OtReferringservice = "ot-referringservice";

		public static readonly string OtSrviceName = "ot-service-name";

		public static string OtSrviceNameValue { get; set; }

		public static readonly string OtSrviceEnvirnoment = "ot-service-envirnoment";

		public static string OtSrviceEnvirnomentValue { get; set; }

		public static readonly string OtEndpoint = "ot-endpoint";

		public enum HighlevelStatus
		{
			Success,
			Failure,
			Timeout,
			Noconnect
		}
	}
}
