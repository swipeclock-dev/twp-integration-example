using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TWP_API_SDK
{
    public class JWT_SiteInfo
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        public override string ToString()
        {
            return $"Type: {Type}, Id: {Id}";
        }
    }

    public class JWT_UserInfo
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public JWT_UserInfo(string empCode)
        {
            Type = JWT_Payload.JWT_EMPLOYEE_TYPE_ID;
            Id = empCode;
        }

        public override string ToString()
        {
            return $"Type: {Type}, Id: {Id}";
        }
    }

    public class JWT_Payload
    {
        public const int JWT_EXPIRATION_SECONDS = 60;

        public const string JWT_PARTNER_SUBJECT = "partner";
        public const string JWT_SITE_TYPE_ID = "id";
        public const string JWT_SUPERVISOR_TYPE_ID = "login";
        public const string JWT_EMPLOYEE_TYPE_ID = "empcode";

        [JsonProperty(PropertyName = "iss")]
        public int Iss { get; set; }

        [JsonProperty(PropertyName = "exp")]
        public long Exp { get; set; }

        [JsonProperty(PropertyName = "sub")]
        public string Sub { get; set; }

        [JsonProperty(PropertyName = "siteInfo")]
        public JWT_SiteInfo SiteInfo { get; set; } = new JWT_SiteInfo();

        [JsonProperty(PropertyName = "user")]
        public JWT_UserInfo User { get; set; } = null;

        [JsonProperty(PropertyName = "product")]
        public string Product { get; set; }

        public JWT_Payload(int partnerId, int siteId, APIProduct apiProduct)
        {
            Iss = partnerId;
            Exp = TWP_API_UTILS.GetUnixEpochTimestamp(DateTime.UtcNow.AddSeconds(JWT_EXPIRATION_SECONDS));
            Sub = JWT_PARTNER_SUBJECT;
            SiteInfo.Type = JWT_SITE_TYPE_ID;
            SiteInfo.Id = siteId;
            Product = TWP_API_UTILS.APIProductToken[apiProduct];
        }

        public override string ToString()
        {
            string retval = $"iss: {Iss}, prod: {Product}, site: {(SiteInfo?.Id.ToString() ?? "<null>")}, exp: {Exp}";

            if (User != null && User.Id != null)
            {
                retval += $", user: {User.Id}";
            }

            return retval;
        }
    }

    public class TWP_API_List_Response
    {
        public long TotalCount { get; set; }
        public long TotalPages { get; set; }
        public string PrevPageLink { get; set; }
        public string NextPageLink { get; set; }
        public long ResultCount { get; set; }
        public List<JObject> Results { get; set; }

        public override string ToString()
        {
            return $"Total Entries: {TotalCount}, Page Entries: {ResultCount}";
        }
    }

    public class TWP_Employee
    {
        public string EmployeeCode { get; set; }

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        /// <summary>
        /// Name2 is a read-only property that is set by TWP to be a 'friendly' name for the 
        /// employee.  It is set to 'LastName, FirstName', and cannot be updated directly.
        /// </summary>
        [JsonProperty(PropertyName = "Name2")]
        public string FullName { get; }
        public string Designation { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public List<TWP_State> States { get; set; }

        public TWP_Employee()
        {
        }

        [JsonConstructor]
        public TWP_Employee(string fullName)
            {
            FullName = fullName;
            }

        public override string ToString()
            {
            return $"{FullName}({EmployeeCode})";
        }
    }

    public class TWP_State
    {
        public DateTime? EffectiveDate { get; set; }
        public Dictionary<string, string> Variables { get; set; }

        public override string ToString()
        {
            return $"{Variables.SafeCount()} Variables as of: {EffectiveDate.FormatAPIDate() ?? TWP_API_UTILS.API_UNSET_TOKEN}, ";
        }
    }

    public class TWP_ID
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}";
        }
    }

    public class TWP_PayrollActivitiesRequest : List<TWP_ID>
    {
        public TWP_PayrollActivitiesRequest(List<string> employeeIds)
        {
            this.AddRange(employeeIds.Select(empId => new TWP_ID { Id = empId }));
        }

        public override string ToString()
        {
            return $"{Count} IDs: {String.Join(", ", this.Take(5).Select(id => id.Id))}";
        }
    }

    public class TWP_PayrollActivities
    {
        public string Error { get; set; }
        public bool? FormatBinary { get; set; }
        public string FormatString { get; set; }
        public string MimeType { get; set; }

        public override string ToString()
        {
            if (!String.IsNullOrEmpty(Error))
            {
                return $"Error: {Error}";
            }

            string shortData = FormatString?.Substring(0, 100) ?? TWP_API_UTILS.API_UNSET_TOKEN;

            string retval = $"Data: {shortData}";

            if (FormatBinary ?? false)
            {
                retval = "Binary " + retval;
            }

            if (!String.IsNullOrEmpty(MimeType))
            {
                retval = $"MimeType: {MimeType}" + retval;
            }

            return retval;
        }
    }

    public class TWP_AccrualsSchema
    {
        [JsonProperty(PropertyName = "category")]
        public string Category { get; set; }

        [JsonProperty(PropertyName = "isHidden")]
        public string IsHidden { get; set; }

        [JsonProperty(PropertyName = "effective")]
        public string Effective { get; set; }

        [JsonProperty(PropertyName = "expires")]
        public string Expires { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

        public override string ToString()
        {
            return $"Category: {Category}, IsHidden: {IsHidden}";
        }
    }

    public partial class TWP_AccrualUpdate
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("isHidden")]
        public bool IsHidden { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("effective")]
        public string Effective { get; set; }

        [JsonProperty("expires")]
        public string Expires { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}, Category: {Category}, Value: {Value}";
        }
    }

    public partial class TWP_Accruals
    {
        public string EmployeeCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        [JsonProperty(PropertyName = "Name2")]
        public string FullName { get; set; }
        [JsonProperty(PropertyName = "StartDate")]
        public DateTime? EmployeeStartDate { get; set; }
        [JsonProperty(PropertyName = "EndDate")]
        public DateTime? EmployeeEndDate { get; set; }
        public DateTime? AsOfDate { get; set; }
        public List<TWP_AccrualValues> Balances { get; set; }

        public override string ToString()
        {
            return $"Emp: {FullName}, AsOf: {AsOfDate.FormatAPIDate()}, Balances: {Balances.SafeCount()}";
        }
    }

    public partial class TWP_AccrualActivities
    {
        public string EmployeeCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        [JsonProperty(PropertyName = "Name2")]
        public string FullName { get; set; }
        public DateTime? EmployeeStartDate { get; set; }
        public DateTime? EmployeeEndDate { get; set; }
        public List<TWP_AccrualValues> StartingValues { get; set; }
        public List<TWP_AccrualValues> EndingValues { get; set; }
        public List<TWP_AccrualValues> Days { get; set; }

        public override string ToString()
        {
            return $"Employee: {FullName}, Activity Days: {Days.SafeCount()}";
        }
    }

    public partial class TWP_AccrualValues
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("isHidden")]
        public bool IsHidden { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; }

        [JsonProperty("date")]
        public DateTime? Date { get; set; }

        [JsonProperty("expires")]
        public DateTime? Expires { get; set; }

        [JsonProperty("effective")]
        public DateTime? Effective { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }

        public List<TWP_AccrualActivity> Activity { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}, Category: {Category}, Date: {Date.FormatAPIDate()}, Value: {Value ?? 0}, Activity Count: {Activity.SafeCount()}";
        }
    }

    public partial class TWP_AccrualActivity
    {
        public string SavedBy { get; set; }
        public string SavedFrom { get; set; }
        public decimal? Amount { get; set; }
        public bool? IsAbsolute { get; set; }
        public string ChangeType { get; set; }
        public decimal? Delta { get; set; }

        public override string ToString()
        {
            return $"ChangeType: {ChangeType}, IsAbsolute: {IsAbsolute?.ToString() ?? TWP_API_UTILS.API_UNSET_TOKEN}, Amount: {Amount ?? 0}, Delta: {Delta ?? 0}, SavedBy: {SavedBy ?? TWP_API_UTILS.API_UNSET_TOKEN}";
        }
    }

}
