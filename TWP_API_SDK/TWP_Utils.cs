using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace TWP_API_SDK
{
    public enum APIProduct
    {
        TWP_Partner = 1,
        TWP_Employee_SSO,
        TWP_Supervisor_SSO
    }

    public static class TWP_API_UTILS
    {
        public const string API_DATE_FORMAT = "yyyy-MM-dd";
        public const string API_UNSET_TOKEN = "<unset>";

        public const string AUTH_SERVICE_ENDPOINT =
            "https://clock.payrollservers.us/AuthenticationService/oauth2/userToken";
        public const string BASE_API_URL = "https://twpapi.payrollservers.us/api";
        public const string BASE_EMPLOYEE_SSO_ENDPOINT = "https://clock.payrollservers.us/?jwt=";
        public const string BASE_SUPERVISOR_SSO_ENDPOINT = "https://payrollservers.us/pg/Login.aspx?jwt=";

        public const string EMPLOYEE_SCHEMA_ENDPOINT = "employees/schema";
        public const string EMPLOYEES_ENDPOINT = "employees";

        public const string PAYROLL_SCHEMA_ENDPOINT = "payrollactivities/schema";
        public const string PAYROLL_ACTIVITIES_ENDPOINT = "payrollactivities";
        public const string TIMECARD_DETAILS_ENDPOINT = "timecards";

        public const string ACCRUALS_SCHEMA_ENDPOINT = "accruals/schema";
        public const string ACCRUALS_ENDPOINT = "accruals";
        public const string ACCRUALS_ACTIVITY_ENDPOINT = "accruals/activity";

        private static readonly string[] API_PRODUCT_TOKEN = new string[]
        {
            "",  "twppartner", "twpemp", "twplogin"
        };

        public class APIProductTokenClass
        {
            public string this[APIProduct product] => API_PRODUCT_TOKEN[(int)product];
        }

        public static APIProductTokenClass APIProductToken { get; private set; } = new APIProductTokenClass();

        public static readonly DateTime UNIX_EPOCH_START = new DateTime(1970, 1, 1);
        public static readonly TimeSpan UNIX_EPOCH_OFFSET = new TimeSpan(0, 4, 30);

        public static long GetUnixEpochTimestamp(DateTime? timestamp = null)
        {
            timestamp = timestamp ?? DateTime.UtcNow;

            return (long)timestamp.Value.Add(UNIX_EPOCH_OFFSET).Subtract(UNIX_EPOCH_START).TotalSeconds;
        }

        public static string SerializeAPIBody(object bodyObject)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            return JsonConvert.SerializeObject(bodyObject, settings);
        }

        public static string FormatAPIDate(this DateTime dateTime)
        {
            return dateTime.ToString(API_DATE_FORMAT);
        }

        public static string FormatAPIDate(this DateTime? dateTime)
        {
            return dateTime?.ToString(API_DATE_FORMAT) ?? API_UNSET_TOKEN;
        }

        public static int SafeCount<T>(this IEnumerable<T> source)
        {
            return source?.Count() ?? 0;
        }

        public static IEnumerable<T> SafeEnumeration<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }
    }
}
