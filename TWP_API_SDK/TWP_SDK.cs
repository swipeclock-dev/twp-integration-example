
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using JWT;
using JWT.Algorithms;
using JWT.Serializers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TWP_API_SDK
{
    public static class TWP_SDK
    {
        public const int API_PAGE_SIZE = 50;

        public static async Task<string> GetJWTToken(string APISecret, int partnerId, int siteId,
            APIProduct product = APIProduct.TWP_Partner)
        {
            return await GetJWTToken(APISecret, new JWT_Payload(partnerId, siteId, product));
        }

        public static async Task<string> GetJWTToken(string APISecret, JWT_Payload payload)
        {
            JwtEncoder jwt = new JwtEncoder(new HMACSHA256Algorithm(), new JsonNetSerializer(),
                new JwtBase64UrlEncoder());

            string requestToken = jwt.Encode(payload, APISecret);

#if DEBUG_MESSAGES
            Console.WriteLine($"Getting JWT Auth token for payload:");
            Console.WriteLine($"{JsonConvert.SerializeObject(payload, Formatting.Indented)}");
            Console.WriteLine($"With request token: {requestToken}");
#endif
            HttpWebResponse response = await SendWebRequest(HttpMethod.Post, TWP_API_UTILS.AUTH_SERVICE_ENDPOINT, requestToken, null);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                JObject result = null;

                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    string responseString = await sr.ReadToEndAsync();
                    result = JObject.Parse(responseString);

                    string authToken = result["token"].ToString();

                    return authToken;
                }
            }

            throw new InvalidOperationException($"Received an error while requesting a JWT token: " +
                $"{response.StatusCode} - {response.StatusDescription}");
        }

        public static async Task<string> CallTWPAPI(int siteId, string apiToken,
            string API_Endpoint, HttpMethod method = null, object content = null)
        {
            string API_URL = null;

            method = method ?? HttpMethod.Get;

            if (API_Endpoint.StartsWith("http"))
            {
                API_URL = API_Endpoint;
            }
            else
            {
                API_URL = $"{TWP_API_UTILS.BASE_API_URL}/{siteId}/{API_Endpoint}";
            }

            HttpWebResponse wr = await SendWebRequest(method, API_URL, apiToken, content);

            if (wr.StatusCode == HttpStatusCode.OK || wr.StatusCode == HttpStatusCode.Created)
            {
                using (StreamReader sr = new StreamReader(wr.GetResponseStream()))
                {
                    string result = sr.ReadToEnd();

#if DEBUG_MESSAGES
                    Console.WriteLine($"API Call Results:");
                    Console.WriteLine($"{method} {API_URL}");
                    Console.WriteLine($"{result}");
#endif

                    return result;
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"Received an error while calling Client API '{API_URL}': " +
                    $"{wr.StatusCode} - {wr.StatusDescription}");
            }
        }

        public static async Task<HttpWebResponse> SendWebRequest(HttpMethod method,
            string url, string authToken, object content = null)
        {

            WebRequest request = WebRequest.Create(url);
            request.Method = method.ToString();
            request.ContentType = "application/json";
            request.Headers.Set("Authorization", String.Format("Bearer {0}", authToken));

#if DEBUG_MESSAGES
            Console.WriteLine("Sending API Request:");
            Console.WriteLine($"{request.Method} {url}");
#endif

            if (content != null)
            {
                string stringContent = TWP_API_UTILS.SerializeAPIBody(content);
                using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(stringContent);
                    streamWriter.Flush();
                }

#if DEBUG_MESSAGES
                Console.WriteLine($"Body Content:");
                Console.WriteLine(stringContent);
#endif
            }

            return (HttpWebResponse)(await request.GetResponseAsync());
        }

        public static async Task<List<TWP_Employee>> ListEmployees(int siteId, string apiToken)
        {
            List<TWP_Employee> allEmployees = new List<TWP_Employee>();
            string pagedURL = $"{TWP_API_UTILS.EMPLOYEES_ENDPOINT}?pageSize={API_PAGE_SIZE}";

            while (!String.IsNullOrEmpty(pagedURL))
            {
                string apiResponse = await CallTWPAPI(siteId, apiToken, pagedURL, HttpMethod.Get);

                TWP_API_List_Response result =
                    JsonConvert.DeserializeObject<TWP_API_List_Response>(apiResponse);

                allEmployees.AddRange(result.Results.Select(empJson => empJson.ToObject<TWP_Employee>()));
                pagedURL = result.NextPageLink;
            }

            return allEmployees;
        }

        public static async Task UpsertEmployee(int siteId, string apiToken, TWP_Employee updateEmployee)
        {
            string upsertURL = $"{TWP_API_UTILS.EMPLOYEES_ENDPOINT}/{updateEmployee.EmployeeCode}?upsert=true";

            await CallTWPAPI(siteId, apiToken, upsertURL, HttpMethod.Post, updateEmployee);
        }

        public static async Task<JObject> GetEmployeeSchema(int siteId, string apiToken)
        {
            string schemaResult = await CallTWPAPI(siteId, apiToken, TWP_API_UTILS.EMPLOYEE_SCHEMA_ENDPOINT);

            return JObject.Parse(schemaResult);
        }

        public static async Task<List<string>> GetPayrollFormats(int siteId, string apiToken)
        {
            string schemaResult = await CallTWPAPI(siteId, apiToken, TWP_API_UTILS.PAYROLL_SCHEMA_ENDPOINT);

            return JArray.Parse(schemaResult).Select(jo => jo["format"].ToString()).ToList();
        }

        public static async Task<TWP_PayrollActivities> GetPayrollActivities(int siteId, string apiToken,
            DateTime? payPeriodDate = null, List<string> employeeIds = null, string payrollFormat = null)
        {
            string dateParam = TWP_API_UTILS.FormatAPIDate(payPeriodDate ?? DateTime.Now);

            string payrollActivityURL = $"{TWP_API_UTILS.PAYROLL_ACTIVITIES_ENDPOINT}?periodDate={dateParam}";

            if (!String.IsNullOrEmpty(payrollFormat))
            {
                payrollActivityURL += $"&format={payrollFormat}";
            }

            TWP_PayrollActivitiesRequest requestBody = null;

            if (employeeIds.SafeCount() > 0)
            {
                requestBody = new TWP_PayrollActivitiesRequest(employeeIds);
            }

            return JsonConvert.DeserializeObject<TWP_PayrollActivities>(
                await CallTWPAPI(siteId, apiToken, payrollActivityURL, HttpMethod.Post, requestBody));
        }

        public static async Task<JObject> GetTimecardDetails(int siteId, string apiToken,
            DateTime? payPeriodDate = null, List<string> employeeIds = null)
        {
            string dateParam = TWP_API_UTILS.FormatAPIDate(payPeriodDate ?? DateTime.Now);

            string timecardDetailsURL = $"{TWP_API_UTILS.TIMECARD_DETAILS_ENDPOINT}?periodDate={dateParam}";

            if (employeeIds.SafeCount() > 0)
            {
                timecardDetailsURL += $"&ids={String.Join(",", employeeIds)}";
            }

            string timecardDetailsJSON = await CallTWPAPI(siteId, apiToken, timecardDetailsURL);

            return JObject.Parse(timecardDetailsJSON);
        }

        public static async Task<string> GetSSOLink(string apiSecret, int partnerId, int siteId, string apiToken, APIProduct ssoType, string ssoUserIdentifier)
        {
            JWT_Payload ssoPayload = new JWT_Payload(partnerId, siteId, ssoType)
            {
                User = new JWT_UserInfo(ssoUserIdentifier)
            };

            string ssoUrl = TWP_API_UTILS.BASE_EMPLOYEE_SSO_ENDPOINT;

            if (ssoType == APIProduct.TWP_Supervisor_SSO)
            {
                ssoUrl = TWP_API_UTILS.BASE_SUPERVISOR_SSO_ENDPOINT;
                ssoPayload.User.Type = JWT_Payload.JWT_SUPERVISOR_TYPE_ID;
            }

            return ssoUrl + await GetJWTToken(apiSecret, ssoPayload);
        }

        public static async Task<List<TWP_AccrualsSchema>> GetAccrualSchema(int siteId, string apiToken)
        {
            string schemaResult = await CallTWPAPI(siteId, apiToken, TWP_API_UTILS.ACCRUALS_SCHEMA_ENDPOINT);

            return JsonConvert.DeserializeObject<List<TWP_AccrualsSchema>>(schemaResult);
        }

        public static async Task UpdateAccrual(int siteId, string apiToken, List<TWP_AccrualUpdate> updateAccrual)
        {
            string upsertURL = $"{TWP_API_UTILS.ACCRUALS_ENDPOINT}";

            await CallTWPAPI(siteId, apiToken, upsertURL, HttpMethod.Post, updateAccrual);
        }

        public static async Task<List<TWP_Accruals>> GetAccruals(int siteId, string apiToken,
            DateTime? asOfDate = null, string category = null)
        {
            List<TWP_Accruals> allAccruals = new List<TWP_Accruals>();
            string pagedURL = $"{TWP_API_UTILS.ACCRUALS_ENDPOINT}?pageSize={API_PAGE_SIZE}";

            if (asOfDate != null)
            {
                pagedURL += $"&asOfDate={TWP_API_UTILS.FormatAPIDate(asOfDate.Value)}";
            }

            if (category != null)
            {
                pagedURL += $"&category={category}";
            }

            while (!String.IsNullOrEmpty(pagedURL))
            {
                string apiResponse = await CallTWPAPI(siteId, apiToken, pagedURL, HttpMethod.Get);

                TWP_API_List_Response result = JsonConvert.DeserializeObject<TWP_API_List_Response>(apiResponse);

                allAccruals.AddRange(result.Results.Select(accJson => accJson.ToObject<TWP_Accruals>()));
                pagedURL = result.NextPageLink;
            }

            return allAccruals;
        }

        public static async Task<List<TWP_AccrualActivities>> GetAccrualActivity(int siteId, string apiToken,
            DateTime? startDate = null, DateTime? endDate = null, string category = null,
            List<string> employeeIds = null)
        {
            List<TWP_AccrualActivities> allAccruals = new List<TWP_AccrualActivities>();
            string pagedURL = $"{TWP_API_UTILS.ACCRUALS_ACTIVITY_ENDPOINT}?pageSize={API_PAGE_SIZE}";

            if (startDate != null)
            {
                pagedURL += $"&startDate={TWP_API_UTILS.FormatAPIDate(startDate.Value)}";
            }

            if (endDate != null)
            {
                pagedURL += $"&endDate={TWP_API_UTILS.FormatAPIDate(endDate.Value)}";
            }

            if (category != null)
            {
                pagedURL += $"&category={category}";
            }

            if (employeeIds.SafeCount() > 0)
            {
                pagedURL += $"&ids={String.Join(",", employeeIds)}";
            }

            while (!String.IsNullOrEmpty(pagedURL))
            {
                string apiResponse = await CallTWPAPI(siteId, apiToken, pagedURL, HttpMethod.Get);

                TWP_API_List_Response result = JsonConvert.DeserializeObject<TWP_API_List_Response>(apiResponse);

                allAccruals.AddRange(result.Results.Select(accJson => accJson.ToObject<TWP_AccrualActivities>()));
                pagedURL = result.NextPageLink;
            }

            return allAccruals;
        }
    }
}
