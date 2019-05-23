using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using TWP_API_SDK;

namespace Integration_Example
{
    internal class Program
    {
        // Change these values to match your credentials.  We are using environment variables
        // here to avoid checking our credentials into a public repo, but as a user of the example
        // code, you can just hard-code your credentials here if you prefer.
        private static int ACCOUNTANT_ID => Convert.ToInt32(Environment.GetEnvironmentVariable("TWP_INTEGRATION_EXAMPLE_ACCOUNTANT_ID"));
        private static int SITE_ID => Convert.ToInt32(Environment.GetEnvironmentVariable("TWP_INTEGRATION_EXAMPLE_SITE_ID"));
        private static string API_SECRET => Environment.GetEnvironmentVariable("TWP_INTEGRATION_EXAMPLE_API_SECRET");

        private static void Main(string[] args)
        {
            try
            {
                if (ACCOUNTANT_ID <= 0)
                {
                    Console.WriteLine("Please set the ACCOUNTANT_ID property in order to run the example");
                    return;
                }

                if (SITE_ID <= 0)
                {
                    Console.WriteLine("Please set the SITE_ID property in order to run the example");
                    return;
                }

                if (String.IsNullOrEmpty(API_SECRET))
                {
                    Console.WriteLine("Please set the API_SECRET property in order to run the example");
                    return;
                }

                AuthorizeAPI().Wait();
                ValidateEmployeeSchema().Wait();
                ListEmployees().Wait();
                UploadEmployee().Wait();
                GetPayrollActivities().Wait();
                GetPayrollFormats().Wait();
                GetTimecardDetails().Wait();
                GetEmployeeSSO().Wait();
                GetSupervisorSSO().Wait();
                ValidateAccrualSchema().Wait();
                UpdateAccruals().Wait();
                ListAccruals().Wait();
                ListAccrualActivity().Wait();
            }
            catch (Exception ex)
            {
                do
                {
                    Console.WriteLine($"Integration Example Exception: {ex.Message}");
                    ex = ex.InnerException;
                } while (ex != null);
            }
            finally
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Console.WriteLine("Press enter to quit");
                    Console.ReadLine();
                }
            }
        }

        private static string PartnerAPIToken { get; set; } = null;

        public static async Task AuthorizeAPI()
        {
            if (PartnerAPIToken == null)
            {
                PartnerAPIToken = await TWP_SDK.GetJWTToken(API_SECRET, ACCOUNTANT_ID, SITE_ID, APIProduct.TWP_Partner);

                if (!String.IsNullOrEmpty(PartnerAPIToken))
                {
                    Console.WriteLine($"Partner Authorization succeeded");
                }
            }
        }

        public static async Task ValidateEmployeeSchema()
        {
            try
            {
                await AuthorizeAPI();

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine($"Validating Client Employee Schema...");

                JObject empSchema = await TWP_SDK.GetEmployeeSchema(SITE_ID, PartnerAPIToken);

                JToken checkHome4 = empSchema["States"]?[0]?["Variables"]?["Home4"];

                if (checkHome4 == null)
                {
                    Console.WriteLine("Client Schema is not valid, does not contain a Home4 State!");
                }
                else
                {
                    Console.WriteLine("Client Schema is valid");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Validating Client Employee Schema: An exception occured: {ex.Message}");
            }
        }

        public static async Task UploadEmployee()
        {
            try
            {
                await AuthorizeAPI();

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Uploading employee...");

                DateTime hireDate = new DateTime(2019, 04, 01);

                TWP_Employee newEmployee = new TWP_Employee
                {
                    EmployeeCode = "EMP042",
                    FirstName = "Tara",
                    LastName = "Thoris",
                    Email = "tthoris@example.com",
                    Phone = "(512)555-8821",
                    StartDate = hireDate,
                    States = new List<TWP_State>
                    {
                        new TWP_State {
                            EffectiveDate = hireDate,
                            Variables = new Dictionary<string, string>
                            {
                                { "Department", "Development" },
                                { "Location", "Austin, TX" },
                                { "Home4", "Infrastructure" },
                            }
                        }
                    }
                };

                await TWP_SDK.UpsertEmployee(SITE_ID, PartnerAPIToken, newEmployee);

                Console.WriteLine("Updating Employee...");

                // Update an existing employee
                // Note that we can send only the data fields that we want to change.  Any field not
                // sent will not be changed
                TWP_Employee updateEmployee = new TWP_Employee
                {
                    EmployeeCode = "EMP042",
                    Phone = "(512)555-4410",
                    States = new List<TWP_State>
                    {
                        new TWP_State {
                            EffectiveDate = DateTime.Now.AddDays(7),
                            Variables = new Dictionary<string, string>
                            {
                                { "Department", "DevOps" },
                                { "Home4", "Management" },
                            }
                        }
                    }
                };

                await TWP_SDK.UpsertEmployee(SITE_ID, PartnerAPIToken, updateEmployee);

                Console.WriteLine($"Employee Upload Successful");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload Employee: An exception occured: {ex.Message}");
            }
        }

        public static async Task ListEmployees()
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Listing Employees...");

                List<TWP_Employee> employees = await TWP_SDK.ListEmployees(SITE_ID, PartnerAPIToken);

                if (employees.SafeCount() < 1)
                {
                    Console.WriteLine("There are no TWP Employees yet");
                }
                else
                {
                    foreach (TWP_Employee thisEmp in employees)
                    {
                        Console.WriteLine($"{thisEmp.FullName} - EmpCode: {thisEmp.EmployeeCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"List Employees: An exception occured: {ex.Message}");
            }
        }

        public static async Task GetPayrollActivities()
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Getting Default Payroll Activity...");

                TWP_PayrollActivities result = await TWP_SDK.GetPayrollActivities(SITE_ID, PartnerAPIToken,
                    new DateTime(2019, 5, 8), new List<string> { "EMP042" });

                Console.WriteLine("Formatted Payroll Activities:");
                Console.WriteLine(result.FormatString);

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Getting ADP8 Payroll Activity...");

                result = await TWP_SDK.GetPayrollActivities(SITE_ID, PartnerAPIToken,
                    new DateTime(2019, 5, 8), new List<string> { "EMP042" }, "adp8");

                Console.WriteLine("Formatted Payroll Activities:");
                Console.WriteLine(result.FormatString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Payroll Activity: An exception occured: {ex.Message}");
            }
        }

        public static async Task GetPayrollFormats()
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Getting Payroll Formats...");

                List<string> payrollFormats = await TWP_SDK.GetPayrollFormats(SITE_ID, PartnerAPIToken);

                Console.WriteLine("Payroll Formats:");
                Console.WriteLine($"{String.Join(", ", payrollFormats.Take(10))} . . .");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Payroll Formats: An exception occured: {ex.Message}");
            }
        }

        public static async Task GetTimecardDetails()
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Getting Timecard Details...");

                JObject result = await TWP_SDK.GetTimecardDetails(SITE_ID, PartnerAPIToken,
                    new DateTime(2019, 5, 8), new List<string> { "EMP042" });

                Console.WriteLine($"Timecard Details: {result.ToString().Substring(0, 100)} . . .");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Timecard Details: An exception occured: {ex.Message}");
            }
        }

        public static async Task GetEmployeeSSO()
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Getting an Employee SSO Link...");

                string empSSOLink = await TWP_SDK.GetSSOLink(API_SECRET,
                    ACCOUNTANT_ID, SITE_ID, PartnerAPIToken,
                    APIProduct.TWP_Employee_SSO, "EMP042");

                Console.WriteLine($"Employee SSO Link: {empSSOLink}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Employee SSO: An exception occured: {ex.Message}");
            }
        }

        public static async Task GetSupervisorSSO()
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Getting a Supervisor SSO Link...");

                string superSSOLink = await TWP_SDK.GetSSOLink(API_SECRET,
                    ACCOUNTANT_ID, SITE_ID, PartnerAPIToken,
                    APIProduct.TWP_Supervisor_SSO, "dtc-super");

                Console.WriteLine($"Supervisor SSO Link: {superSSOLink}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Supervisor SSO: An exception occured: {ex.Message}");
            }
        }

        public static async Task ValidateAccrualSchema()
        {
            try
            {
                await AuthorizeAPI();

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine($"Validating Client Accrual Schema...");

                List<TWP_AccrualsSchema> accSchema = await TWP_SDK.GetAccrualSchema(SITE_ID, PartnerAPIToken);

                // Verify that this client has the Sabbattical Accrual bucket
                bool hasSabbatical = accSchema.Any(check => check.Category == "SABBATICAL");

                if (!hasSabbatical)
                {
                    Console.WriteLine("Client Accrual Schema is not valid, does not contain a Sabbatical bucket!");
                }
                else
                {
                    Console.WriteLine("Client Accrual Schema is valid");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Validating Client Accrual Schema: An exception occured: {ex.Message}");
            }
        }

        public static async Task UpdateAccruals()
        {
            try
            {
                await AuthorizeAPI();

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Updating Accrual Data...");

                List<TWP_AccrualUpdate> accrualUpdates = new List<TWP_AccrualUpdate>
                {
                    new TWP_AccrualUpdate
                    {
                        Id = "EMP042",
                        Category = "SABBATICAL",
                        Effective = new DateTime(2019, 5, 8).FormatAPIDate(),
                        Value = "2.38"
                    },
                    new TWP_AccrualUpdate
                    {
                        Id = "EMP042",
                        Category = "PERSONAL",
                        Effective = new DateTime(2019, 5, 8).FormatAPIDate(),
                        Value = "4.94"
                    },
                };

                await TWP_SDK.UpdateAccrual(SITE_ID, PartnerAPIToken, accrualUpdates);

                Console.WriteLine($"Accrual Update Successful");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Accrual Update: An exception occured: {ex.Message}");
            }
        }

        public static async Task ListAccruals()
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Listing Accrual Balances...");

                List<TWP_Accruals> accruals = await TWP_SDK.GetAccruals(SITE_ID, PartnerAPIToken,
                    new DateTime(2019, 05, 11));

                if (accruals.SafeCount() < 1)
                {
                    Console.WriteLine("There are no Accrual Balances yet");
                }
                else
                {
                    foreach (TWP_Accruals thisAcc in accruals)
                    {
                        foreach (TWP_AccrualValues thisBalance in thisAcc.Balances.SafeEnumeration())
                        {
                            Console.WriteLine($"{thisAcc.FullName} - Category: {thisBalance.Category}, Balance: {thisBalance.Value}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"List Accrual Balances: An exception occured: {ex.Message}");
            }
        }

        public static async Task ListAccrualActivity()
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Listing Accrual Activity...");

                List<TWP_AccrualActivities> accruals = await TWP_SDK.GetAccrualActivity(SITE_ID, PartnerAPIToken,
                    new DateTime(2019, 04, 28), new DateTime(2019, 05, 11),
                    employeeIds: new List<string> { "EMP042" });

                if (accruals.SafeCount() < 1)
                {
                    Console.WriteLine("There is no Accrual Activity yet");
                }
                else
                {
                    foreach (TWP_AccrualActivities thisAcc in accruals)
                    {
                        foreach (TWP_AccrualValues thisAccDay in thisAcc.Days.SafeEnumeration())
                        {
                            if (thisAccDay.Activity.SafeCount() < 1)
                            {
                                continue;
                            }

                            Console.WriteLine($"{thisAcc.FullName} - Date: {thisAccDay.Date.Value.FormatAPIDate()}, Category: {thisAccDay.Category}, Balance: {thisAccDay.Value}");

                            foreach (TWP_AccrualActivity thisActivity in thisAccDay.Activity)
                            {
                                string absoluteString = null;
                                string amountString = null;
                                string deltaString = null;
                                string savedByString = null;

                                if (thisActivity.IsAbsolute != null)
                                {
                                    absoluteString = $", IsAbsolute: {thisActivity.IsAbsolute}";
                                }
                                if (thisActivity.Amount != null)
                                {
                                    amountString = $", Amount: {thisActivity.Amount}";
                                }
                                if (thisActivity.Delta != null)
                                {
                                    deltaString = $", Delta: {thisActivity.Delta}";
                                }
                                if (thisActivity.SavedBy != null)
                                {
                                    savedByString = $", SavedBy: {thisActivity.SavedBy}";
                                }

                                Console.WriteLine($"    Activity: {thisActivity.ChangeType}{savedByString}{absoluteString}{deltaString}{amountString}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"List Accrual Activity: An exception occured: {ex.Message}");
            }
        }
    }
}
