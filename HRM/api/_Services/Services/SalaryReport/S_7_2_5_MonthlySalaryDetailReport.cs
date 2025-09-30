using System.Drawing;
using System.Globalization;
using API.Data;
using API._Services.Interfaces.SalaryReport;
using API.DTOs.SalaryReport;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryReport
{
    public class S_7_2_5_MonthlySalaryDetailReport : BaseServices, I_7_2_5_MonthlySalaryDetailReport
    {
        public S_7_2_5_MonthlySalaryDetailReport(DBContext dbContext) : base(dbContext) { }

        #region GetList
        // List Factory
        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string language, string userName)
        {
            var factorys = await Queryt_Factory_AddList(userName);
            var factories = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory && factorys.Contains(x.Code), true)
                        .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                                    x => new { x.Type_Seq, x.Code },
                                    y => new { y.Type_Seq, y.Code },
                                    (x, y) => new { x, y })
                                    .SelectMany(x => x.y.DefaultIfEmpty(),
                                    (x, y) => new { x.x, y })
                        .Select(x => new KeyValuePair<string, string>(x.x.Code, $"{x.x.Code} - {(x.y != null ? x.y.Code_Name : x.x.Code_Name)}")).ToListAsync();
            return factories;
        }

        //List Permistion_Group
        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language)
        {
            return await Query_BasicCode_PermissionGroup(factory, language);
        }

        // List Department
        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
        {
            var data = await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == factory, true)
                .Join(_repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(b => b.Kind == "1" && b.Factory == factory, true),
                    x => x.Division,
                    y => y.Division,
                    (x, y) => x)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    x => new { x.Factory, x.Department_Code },
                    y => new { y.Factory, y.Department_Code },
                    (x, y) => new { Department = x, Language = y })
                .SelectMany(
                    x => x.Language.DefaultIfEmpty(),
                    (x, y) => new { x.Department, Language = y })
                .OrderBy(x => x.Department.Department_Code)
                .Select(
                    x => new KeyValuePair<string, string>(
                        x.Department.Department_Code,
                        $"{x.Department.Department_Code} - {(x.Language != null ? x.Language.Name : x.Department.Department_Name)}"
                    )
                ).Distinct().ToListAsync();
            return data;
        }

        private async Task<List<KeyValuePair<string, string>>> GetHRMS_Basic_Code(string Type_Seq, string language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == Type_Seq, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
        }

        private async Task<List<KeyValuePair<string, string>>> GetDepartmentName(string factory, string language)
        {
            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == factory, true);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Factory == factory && x.Language_Code.ToLower() == language.ToLower(), true);

            return await HOD
                .GroupJoin(HODL,
                    department => new { department.Factory, department.Department_Code },
                    lang => new { lang.Factory, lang.Department_Code },
                    (department, lang) => new { department, lang })
                    .SelectMany(x => x.lang.DefaultIfEmpty(),
                    (department, lang) => new { department.department, lang })
                .Select(x => new KeyValuePair<string, string>(x.department.Department_Code, $"{(x.lang != null ? x.lang.Name : x.department.Department_Name)}"))
                .ToListAsync();
        }
        #endregion

        #region GetData
        private async Task<OperationResult> GetData(MonthlySalaryDetailReportParam param, bool countOnly = false)
        {
            if (!DateTime.TryParseExact(param.Year_Month_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime yearMonth))
                return new OperationResult(false, "Invalid Year-Month");

            var startDate = new DateTime(yearMonth.Year, yearMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var pred = PredicateBuilder.New<HRMS_Emp_Personal>(x =>
                x.Factory == param.Factory &&
                param.Permission_Group.Contains(x.Permission_Group));

            if (!string.IsNullOrWhiteSpace(param.Department))
                pred.And(x => x.Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                pred.And(x => x.Employee_ID.Contains(param.Employee_ID));

            var isAll = param.Transfer == "All";
            var Emp_Personal = _repositoryAccessor.HRMS_Emp_Personal.FindAll(pred, true).ToList();
            var HSM = _repositoryAccessor.HRMS_Sal_Monthly
                        .FindAll(x => x.Factory == param.Factory &&
                                    x.Sal_Month == yearMonth &&
                                    (isAll || x.BankTransfer == param.Transfer), true)
                        .ToList();

            var HSRM = _repositoryAccessor.HRMS_Sal_Resign_Monthly
                        .FindAll(x => x.Factory == param.Factory &&
                                    x.Sal_Month == yearMonth &&
                                    (isAll || x.BankTransfer == param.Transfer), true)
                        .ToList();

            var Sal_Monthly = new List<Sal_Monthly>();

            if (param.Kind == "Y")
            // On job
            {
                Sal_Monthly = Emp_Personal.Where(x => x.Resign_Date > endDate || x.Resign_Date == null)
                                   .Join(HSM,
                                       personal => personal.Employee_ID,
                                       salary => salary.Employee_ID,
                                       (personal, salary) => new { Personal = personal, Salary = salary })
                                   .Select(x => new Sal_Monthly
                                   {
                                       Employee_ID = x.Personal.Employee_ID,
                                       Local_Full_Name = x.Personal.Local_Full_Name,
                                       Work_Type = x.Personal.Work_Type,
                                       Department = x.Salary.Department,
                                       Permission_Group = x.Salary.Permission_Group,
                                       Salary_Type = x.Salary.Salary_Type,
                                       Tax = x.Salary.Tax,
                                       Currency = x.Salary.Currency,
                                       Transfer = x.Salary.BankTransfer
                                   })
                                   .OrderBy(x => x.Department)
                                   .ThenBy(x => x.Employee_ID)
                                   .ToList();
            }
            else
            // Resigned
            {
                Sal_Monthly = Emp_Personal.Where(x => x.Resign_Date >= startDate && x.Resign_Date <= endDate && x.Resign_Date != null)
                                   .Join(HSRM,
                                       personal => personal.Employee_ID,
                                       salary => salary.Employee_ID,
                                       (personal, salary) => new { Personal = personal, Salary = salary })
                                   .Select(x => new Sal_Monthly
                                   {
                                       Employee_ID = x.Personal.Employee_ID,
                                       Local_Full_Name = x.Personal.Local_Full_Name,
                                       Work_Type = x.Personal.Work_Type,
                                       Department = x.Salary.Department,
                                       Permission_Group = x.Salary.Permission_Group,
                                       Salary_Type = x.Salary.Salary_Type,
                                       Tax = x.Salary.Tax,
                                       Currency = x.Salary.Currency,
                                       Transfer = x.Salary.BankTransfer
                                   })
                                   .OrderBy(x => x.Department)
                                   .ThenBy(x => x.Employee_ID)
                                   .ToList();
            }

            if (!Sal_Monthly.Any())
                return new OperationResult(false, Sal_Monthly);

            if (countOnly == true)
                return new OperationResult(true, Sal_Monthly);

            int seq = 1;
            var departmentNames = await GetDepartmentName(param.Factory, param.Lang);
            var employeeIds = Sal_Monthly.Select(x => x.Employee_ID).ToList();
            var permissionGroups = Sal_Monthly.Select(x => x.Permission_Group).ToList();
            var salaryTypes = Sal_Monthly.Select(x => x.Salary_Type).ToList();

            var salBackupDetail = await Sal_Backup(param.Kind, param.Factory, yearMonth, employeeIds);
            var listPositionTitle = await GetHRMS_Basic_Code(BasicCodeTypeConstant.JobTitle, param.Lang);
            var listworkType = await GetHRMS_Basic_Code(BasicCodeTypeConstant.WorkType, param.Lang);
            var listSalaryType = await GetHRMS_Basic_Code(BasicCodeTypeConstant.SalaryType, param.Lang);
            var leaveDayDetail = await Query_Att_Monthly_Detail(param.Kind, param.Factory, yearMonth, employeeIds, "1");
            var attMonthlyDetail = await Query_Att_Monthly(param.Kind, param.Factory, yearMonth, employeeIds);
            var overtimeHoursDetail = await Query_Att_Monthly_Detail(param.Kind, param.Factory, yearMonth, employeeIds, "2");
            var salaryAllowance = await Sal_Backup_SUM(param.Kind, param.Factory, yearMonth, employeeIds);
            var paidLeaveCodes = new List<string> { "D0", "H0", "I0", "I1", "K0", "E0", "F0", "J2", "G1", "J1" };
            var salaryItemDetail = await Query_Sal_Monthly_Detail(param.Kind, param.Factory, yearMonth, employeeIds, "45", "A", permissionGroups, salaryTypes, "0");
            var overtimeAllowanceDetail = await Query_Sal_Monthly_Detail(param.Kind, param.Factory, yearMonth, employeeIds, "42", "A", permissionGroups, salaryTypes, "2");
            var b49Amt = await Sal_Add_Ded(param.Kind, param.Factory, yearMonth, employeeIds, "49", "B", "B49");
            var total49A = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "49", "A");
            var total49B = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "49", "B");
            var total45A = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "45", "A");
            var total42A = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "42", "A");
            var insuranceDeductionDetail = await Query_Sal_Monthly_Detail(param.Kind, param.Factory, yearMonth, employeeIds, "57", "D", permissionGroups, salaryTypes, "0");
            var totalUnionFee = await Sal_Add_Ded(param.Kind, param.Factory, yearMonth, employeeIds, "49", "D", "D12");
            var total57D = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "57", "D");
            var total49C = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "49", "C");
            var total49D = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "49", "D");
            var salary_Day = await _repositoryAccessor.HRMS_Att_Monthly
                .FindAll(x => x.Factory == param.Factory
                           && x.Att_Month == yearMonth
                           && param.Permission_Group.Contains(x.Permission_Group))
                ?.MaxAsync(x => (decimal?)x.Salary_Days);

            var data_49_B_B54_Amt = await Sal_Add_Ded(param.Kind, param.Factory, yearMonth, employeeIds, "49", "B", "B54");
            var data_49_B_B55_Amt = await Sal_Add_Ded(param.Kind, param.Factory, yearMonth, employeeIds, "49", "B", "B55");
            var data_49_B_B56_Amt = await Sal_Add_Ded(param.Kind, param.Factory, yearMonth, employeeIds, "49", "B", "B56");
            var data_49_B_B57_Amt = await Sal_Add_Ded(param.Kind, param.Factory, yearMonth, employeeIds, "49", "B", "B57");

            var result = new List<MonthlySalaryDetailReportDto>();

            foreach (var emp in Sal_Monthly)
            {
                var sal_Backup = salBackupDetail.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID);
                var positionTitle = listPositionTitle.FirstOrDefault(x => x.Key == sal_Backup.Position_Title).Value;
                var workType = listworkType.FirstOrDefault(x => x.Key == emp.Work_Type).Value;
                var salary_Type = listSalaryType.FirstOrDefault(x => x.Key == emp.Salary_Type).Value;
                var att_Monthly = attMonthlyDetail.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID);
                var departmentName = departmentNames.FirstOrDefault(x => x.Key == emp.Department);
                var leave_Days = leaveDayDetail.Where(x => x.Employee_ID == emp.Employee_ID);
                var overtime_Hours = overtimeHoursDetail.Where(x => x.Employee_ID == emp.Employee_ID);
                // Total paid days
                var actual_Days = att_Monthly?.Actual_Days ?? 0;
                var totalLeaveDays = leave_Days.Where(x => paidLeaveCodes.Contains(x.Leave_Code)).Sum(x => x.Days);
                var paid_Days = actual_Days + totalLeaveDays;

                var salary_Allowance = salaryAllowance.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0;
                // Salary Item
                var salary_Item = salaryItemDetail.Where(x => x.Employee_ID == emp.Employee_ID &&
                                                                x.Permission_Group == emp.Permission_Group &&
                                                                x.Salary_Type == emp.Salary_Type);
                // Overtime and Night Shift Allowance
                var overtime_Allowance = overtimeAllowanceDetail.Where(x => x.Employee_ID == emp.Employee_ID);
                var b49_amt = b49Amt.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0;
                var amt_49A = total49A.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0;
                var amt_49B = total49B.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0;
                var amt_45A = total45A.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0;
                var amt_42A = total42A.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0;

                var amt_49BB54 = data_49_B_B54_Amt.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0;
                var amt_49BB55 = data_49_B_B55_Amt.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0;
                var amt_49BB56 = data_49_B_B56_Amt.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0;
                var amt_49BB57 = data_49_B_B57_Amt.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0;
                var meal_Total = amt_49BB54 + amt_49BB55 + amt_49BB56 + amt_49BB57;

                // Insurance Deduction
                var insurance_Deduction = insuranceDeductionDetail.Where(x => x.Employee_ID == emp.Employee_ID);
                var unionFee = totalUnionFee.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0;
                var amt_57D = total57D.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0;
                var amt_49C = total49C.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0;
                var amt_49D = total49D.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0;
                var Add_Sum = amt_49A + amt_49B + amt_45A + amt_42A;
                var Ded_Sum = amt_57D + amt_49C + amt_49D + emp.Tax;

                var data = new MonthlySalaryDetailReportDto
                {
                    Seq = seq++,
                    Department = emp.Department,
                    Department_Name = departmentName.Value,
                    Employee_ID = emp.Employee_ID,
                    Local_Full_Name = emp.Local_Full_Name,
                    Currency = emp.Currency,
                    Salary_Type = salary_Type,
                    Transfer = emp.Transfer,
                    Position_Title = positionTitle,
                    Work_Type = workType,
                    Actual_Days = actual_Days,
                    Leave_Days = leave_Days.Select(x => new KeyValuePair<string, decimal>(x.Leave_Code, x.Days)).ToList(),
                    Delay_Early = att_Monthly?.Delay_Early ?? 0,
                    Overtime_Hours = overtime_Hours.Select(x => new KeyValuePair<string, decimal>(x.Leave_Code, x.Days)).ToList(),
                    Total_Paid_Days = paid_Days,
                    Salary_Allowance = salary_Allowance,
                    Hourly_Wage = salary_Day != null ? Math.Round((decimal)salary_Allowance / (salary_Day.Value * 8), 3) : 0,
                    Salary_Item = salary_Item.Select(x => new KeyValuePair<string, decimal>(x.Item, x.Amount)).ToList(),
                    DayShift_Food = att_Monthly?.DayShift_Food ?? 0,
                    Food_Expenses = att_Monthly?.Food_Expenses ?? 0,
                    Night_Eat_Times = att_Monthly?.Night_Eat_Times ?? 0,
                    NightShift_Food = att_Monthly?.NightShift_Food ?? 0,
                    Overtime_Allowance = overtime_Allowance.Select(x => new KeyValuePair<string, decimal>(x.Item, x.Amount)).ToList(),
                    B49_amt = b49_amt,
                    Meal_Total = meal_Total,
                    Other_Additions = amt_49A + amt_49B - b49_amt - meal_Total,
                    Total_Addition_Item = Add_Sum,
                    Loaned_Amount = 0,
                    Insurance_Deduction = insurance_Deduction.Select(x => new KeyValuePair<string, decimal>(x.Item, x.Amount)).ToList(),
                    Union_Fee = unionFee,
                    Tax = emp.Tax,
                    Other_Deductions = amt_49C + amt_49D - unionFee,
                    Total_Deduction_Item = Ded_Sum,
                    Net_Amount_Received = Add_Sum - Ded_Sum
                };

                result.Add(data);
            }

            return new OperationResult(true, result);
        }
        #endregion

        #region Sal_Add_Ded
        private async Task<List<CalculationList>> Sal_Add_Ded(string kind, string factory, DateTime yearMonth, List<string> employeeIds, string typeSeq, string addedType, string item)
        {
            if (kind == "Y")
            {
                return await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                  employeeIds.Contains(x.Employee_ID) &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType &&
                                 x.Item == item, true)
                    .GroupBy(x => x.Employee_ID)
                    .Select(x => new CalculationList
                    {
                        Employee_ID = x.Key,
                        Amount = x.Sum(x => x.Amount)
                    })
                    .ToListAsync();
            }
            else
            {
                return await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID) &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType &&
                                 x.Item == item, true)
                    .GroupBy(x => x.Employee_ID)
                    .Select(x => new CalculationList
                    {
                        Employee_ID = x.Key,
                        Amount = x.Sum(x => x.Amount)
                    })
                    .ToListAsync();
            }
        }
        #endregion

        #region Sal_Backup
        private async Task<List<Sal_Backup>> Sal_Backup(string kind, string factory, DateTime yearMonth, List<string> employeeIds)
        {
            if (kind == "Y")
            {
                return await _repositoryAccessor.HRMS_Sal_MasterBackup
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID), true)
                    .Select(x => new Sal_Backup
                    {
                        Employee_ID = x.Employee_ID,
                        Position_Title = x.Position_Title
                    })
                    .ToListAsync();
            }
            else
            {
                return await _repositoryAccessor.HRMS_Sal_Master
                    .FindAll(x => x.Factory == factory &&
                                 employeeIds.Contains(x.Employee_ID), true)
                    .Select(x => new Sal_Backup
                    {
                        Employee_ID = x.Employee_ID,
                        Position_Title = x.Position_Title
                    })
                    .ToListAsync();
            }
        }
        #endregion

        #region Sal_Backup_SUM
        private async Task<List<CalculationList>> Sal_Backup_SUM(string kind, string factory, DateTime yearMonth, List<string> employeeIds)
        {
            List<string> salaryItems = new() { "A01", "A02" };
            if (kind == "Y")
            {
                return await _repositoryAccessor.HRMS_Sal_MasterBackup_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID) &&
                                 (salaryItems.Contains(x.Salary_Item) || x.Salary_Item.StartsWith("B")), true)
                    .GroupBy(x => x.Employee_ID)
                    .Select(x => new CalculationList
                    {
                        Employee_ID = x.Key,
                        Amount = x.Sum(x => x.Amount)
                    })
                    .ToListAsync();
            }
            else
            {
                return await _repositoryAccessor.HRMS_Sal_Master_Detail
                    .FindAll(x => x.Factory == factory &&
                                 employeeIds.Contains(x.Employee_ID) &&
                                 (salaryItems.Contains(x.Salary_Item) || x.Salary_Item.StartsWith("B")), true)
                    .GroupBy(x => x.Employee_ID)
                    .Select(x => new CalculationList
                    {
                        Employee_ID = x.Key,
                        Amount = x.Sum(x => x.Amount)
                    })
                    .ToListAsync();
            }
        }
        #endregion

        #region Query_Att_Monthly
        private async Task<List<Att_Monthly>> Query_Att_Monthly(string kind, string factory, DateTime yearMonth, List<string> employeeIds)
        {
            if (kind == "Y")
            {
                return await _repositoryAccessor.HRMS_Att_Monthly
                    .FindAll(x => x.Factory == factory &&
                                 x.Att_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID), true)
                    .Select(x => new Att_Monthly
                    {
                        Employee_ID = x.Employee_ID,
                        Actual_Days = x.Actual_Days,
                        Delay_Early = x.Delay_Early,
                        DayShift_Food = x.DayShift_Food,
                        Food_Expenses = x.Food_Expenses,
                        Night_Eat_Times = x.Night_Eat_Times,
                        NightShift_Food = x.NightShift_Food
                    })
                    .ToListAsync();
            }
            else
            {
                return await _repositoryAccessor.HRMS_Att_Resign_Monthly
                    .FindAll(x => x.Factory == factory &&
                                 x.Att_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID), true)
                    .Select(x => new Att_Monthly
                    {
                        Employee_ID = x.Employee_ID,
                        Actual_Days = x.Actual_Days,
                        Delay_Early = x.Delay_Early,
                        DayShift_Food = x.DayShift_Food,
                        Food_Expenses = x.Food_Expenses,
                        Night_Eat_Times = x.Night_Eat_Times,
                        NightShift_Food = x.NightShift_Food
                    })
                    .ToListAsync();
            }
        }
        #endregion

        #region Query_Att_Monthly_Detail
        private async Task<List<Att_Monthly_Values>> Query_Att_Monthly_Detail(string kind, string factory, DateTime yearMonth, List<string> employeeIds, string leaveType)
        {
            List<Att_Monthly_Detail_Temp> Att_Monthly_Detail_Temp;

            if (kind == "Y")
            {
                Att_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Att_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Att_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID) &&
                                 x.Leave_Type == leaveType, true)
                    .Select(x => new Att_Monthly_Detail_Temp
                    {
                        Employee_ID = x.Employee_ID,
                        Leave_Code = x.Leave_Code,
                        Days = x.Days
                    })
                    .ToListAsync();
            }
            else
            {
                Att_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Att_Resign_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Att_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID) &&
                                 x.Leave_Type == leaveType, true)
                    .Select(x => new Att_Monthly_Detail_Temp
                    {
                        Employee_ID = x.Employee_ID,
                        Leave_Code = x.Leave_Code,
                        Days = x.Days
                    })
                    .ToListAsync();
            }

            var maxEffectiveMonth = await _repositoryAccessor.HRMS_Att_Use_Monthly_Leave
                .FindAll(x => x.Factory == factory &&
                             x.Leave_Type == leaveType &&
                             x.Effective_Month <= yearMonth, true)
                .MaxAsync(x => (DateTime?)x.Effective_Month);

            if (!maxEffectiveMonth.HasValue)
                return new List<Att_Monthly_Values>();

            var Setting_Temp = await _repositoryAccessor.HRMS_Att_Use_Monthly_Leave
                .FindAll(x => x.Factory == factory &&
                             x.Leave_Type == leaveType &&
                             x.Effective_Month == maxEffectiveMonth.Value, true)
                .Select(x => new Setting_Temp
                {
                    Seq = x.Seq,
                    Code = x.Code
                })
                .ToListAsync();

            var result = Att_Monthly_Detail_Temp
                 .GroupJoin(Setting_Temp,
                    detail => detail.Leave_Code,
                    setting => setting.Code,
                    (detail, settings) => new { detail, settings })
                .SelectMany(x => x.settings.DefaultIfEmpty(),
                    (x, setting) => new Att_Monthly_Values
                    {
                        Employee_ID = x.detail.Employee_ID,
                        Leave_Code = x.detail.Leave_Code,
                        Seq = setting?.Seq ?? 0,
                        Days = x.detail.Days
                    })
                .OrderBy(x => x.Seq)
                .ThenBy(x => x.Leave_Code)
                .ToList();

            return result;
        }
        #endregion

        #region Get Total
        public async Task<int> GetTotal(MonthlySalaryDetailReportParam param)
        {
            var data = await GetData(param, true);

            if (data.Data == null)
                return 0;

            var result = (IEnumerable<dynamic>)data.Data;
            return result.Count();
        }
        #endregion

        #region Download Excel
        public async Task<OperationResult> DownloadExcel(MonthlySalaryDetailReportParam param, string userName)
        {
            var result = await GetData(param, false);
            if (!result.IsSuccess)
                return new OperationResult(false, "No data for excel download");
            var data = (List<MonthlySalaryDetailReportDto>)result.Data;

            var listFactory = await GetHRMS_Basic_Code(BasicCodeTypeConstant.Factory, param.Lang);
            var listPermissionGroup = await GetListPermissionGroup(param.Factory, param.Lang);
            var listDepartment = await GetListDepartment(param.Factory, param.Lang);
            var listPositionTitle = await GetHRMS_Basic_Code(BasicCodeTypeConstant.JobTitle, param.Lang);
            var listLeave = await GetHRMS_Basic_Code(BasicCodeTypeConstant.Leave, param.Lang);
            var listOvertime = await GetHRMS_Basic_Code(BasicCodeTypeConstant.Allowance, param.Lang);
            var listSalary = await GetHRMS_Basic_Code(BasicCodeTypeConstant.SalaryItem, param.Lang);
            var listInsurance = await GetHRMS_Basic_Code(BasicCodeTypeConstant.InsuranceType, param.Lang);

            var factory = listFactory.Where(x => x.Key == param.Factory).Select(x => x.Value).FirstOrDefault();
            var updatedPermissionGroup = param.Permission_Group.Select(item =>
                listPermissionGroup.FirstOrDefault(x => x.Key == item).Value ?? item).ToList();
            var department = listDepartment.Where(x => x.Key == param.Department).Select(x => x.Value).FirstOrDefault();

            var leaveDays = data.SelectMany(x => x.Leave_Days.Select(y => y.Key)).Distinct().ToList();
            var leave_Name = listLeave.Where(x => leaveDays.Contains(x.Key)).ToList();

            var overtimeHours = data.SelectMany(x => x.Overtime_Hours.Select(y => y.Key)).Distinct().ToList();
            var overtime_Name = listOvertime.Where(x => overtimeHours.Contains(x.Key)).ToList();

            var salaryItem = data.SelectMany(x => x.Salary_Item.Select(y => y.Key)).Distinct().ToList();
            var salary_Name = listSalary.Where(x => salaryItem.Contains(x.Key)).ToList();

            var overtimeAllowance = data.SelectMany(x => x.Overtime_Allowance.Select(y => y.Key)).Distinct().ToList();
            var overtimeAllowance_Name = listOvertime.Where(x => overtimeAllowance.Contains(x.Key)).ToList();

            var insuranceDeductionItem = data.SelectMany(x => x.Insurance_Deduction.Select(y => y.Key)).Distinct().ToList();
            var insuranceDeduction_Name = listInsurance.Where(x => insuranceDeductionItem.Contains(x.Key)).ToList();

            List<Cell> cells = new()
            {
                new Cell("A1", param.Lang == "en" ? "7.2.5 Monthly Salary Detail Report" : "7.2.5 月份薪資明細表"),
                new Cell("A2", param.Lang == "en" ? "Factory" : "廠別"),
                new Cell("C2", param.Lang == "en" ? "Year-Month" : "薪資年月"),
                new Cell("E2", param.Lang == "en" ? "Kind" : "類別"),
                new Cell("H2", param.Lang == "en" ? "Permission Group" : "權限身分別"),
                new Cell("H3", param.Lang == "en" ? "Transfer" : "轉帳"),
                new Cell("J2", param.Lang == "en" ? "Department" : "部門"),
                new Cell("L2", param.Lang == "en" ? "Employee ID" : "工號"),
                new Cell("A3", param.Lang == "en" ? "Print By" : "列印人員"),
                new Cell("C3", param.Lang == "en" ? "Print Date" : "列印日期"),
                new Cell("A5", param.Lang == "en" ? "Seq" : "序號"),
                new Cell("B5", param.Lang == "en" ? "Department" : "部門"),
                new Cell("C5", param.Lang == "en" ? "Department Name" : "部門名稱"),
                new Cell("D5", param.Lang == "en" ? "Employee ID" : "工號"),
                new Cell("E5", param.Lang == "en" ? "Local Full Name" : "本地姓名"),
                new Cell("F5", param.Lang == "en" ? "Currency" : "幣別"),
                new Cell("G5", param.Lang == "en" ? "Salary Type" : "薪資計別"),
                new Cell("H5", param.Lang == "en" ? "Transfer" : "轉帳否"),
                new Cell("I5", param.Lang == "en" ? "Position Title" : "職稱"),
                new Cell("J5", param.Lang == "en" ? "Work Type/Job" : "工種/職務"),
                new Cell("K5",param.Lang == "en" ? "Actual Work Days" : "實際上班天數"),

                new Cell("B2",factory),
                new Cell("D2", Convert.ToDateTime(param.Year_Month_Str).ToString("yyyy/MM")),
                new Cell("F2",param.Kind = param.Kind == "Y"
                                                      ? param.Lang == "en" ? "On job" : "在職"
                                                      : param.Lang == "en" ? "Resigned" : "離職"),
                new Cell("I2",string.Join(",\n", updatedPermissionGroup)),
                new Cell("I3",param.Transfer == "Y" ? "Yes"
                                                    : param.Transfer == "N" ? "No" : "All"),
                new Cell("K2",department),
                new Cell("M2",param.Employee_ID),
                new Cell("B3",userName),
                new Cell("D3",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))
            };

            Aspose.Cells.Style borderStyle = new Aspose.Cells.CellsFactory().CreateStyle();
            borderStyle = AsposeUtility.SetAllBorders(borderStyle);

            Aspose.Cells.Style amountStyle = new Aspose.Cells.CellsFactory().CreateStyle();
            amountStyle.Number = 3;

            Aspose.Cells.Style decimalStyle = new Aspose.Cells.CellsFactory().CreateStyle();
            decimalStyle.Number = 4;
            decimalStyle.Custom = "#,##0.00000";

            Aspose.Cells.Style decimalStyle3 = new Aspose.Cells.CellsFactory().CreateStyle();
            decimalStyle3.Number = 3;
            decimalStyle3.Custom = "#,##0.000";

            Aspose.Cells.Style labelStyle = new Aspose.Cells.CellsFactory().CreateStyle();
            labelStyle.Font.Color = Color.FromArgb(0, 0, 255);
            labelStyle.Font.IsBold = true;

            Aspose.Cells.Style labelAttStyle = new Aspose.Cells.CellsFactory().CreateStyle();
            labelAttStyle = AsposeUtility.SetAllBorders(labelAttStyle);
            labelAttStyle.Pattern = Aspose.Cells.BackgroundType.Solid;
            labelAttStyle.ForegroundColor = Color.FromArgb(226, 239, 218);

            Aspose.Cells.Style labelSalStyle = new Aspose.Cells.CellsFactory().CreateStyle();
            labelSalStyle = AsposeUtility.SetAllBorders(labelSalStyle);
            labelSalStyle.Pattern = Aspose.Cells.BackgroundType.Solid;
            labelSalStyle.ForegroundColor = Color.FromArgb(255, 242, 204);

            int currentColumn = 8;

            // Leave Days
            if (leave_Name.Any())
            {
                cells.Add(new Cell(3, currentColumn, "Leave Days", labelStyle));
                foreach (var leave in leave_Name)
                {
                    cells.Add(new Cell(4, currentColumn, leave.Value, labelAttStyle));
                    currentColumn++;
                }
            }

            cells.Add(new Cell(4, currentColumn, param.Lang == "en" ? "Delay/Early Times" : "遲到早退數", borderStyle));
            currentColumn++;

            // Overtime Hours
            if (overtime_Name.Any())
            {
                cells.Add(new Cell(3, currentColumn, "Overtime Hours", labelStyle));
                foreach (var overtime in overtime_Name)
                {
                    cells.Add(new Cell(4, currentColumn, overtime.Value, labelAttStyle));
                    currentColumn++;
                }
            }

            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Day Shift Meal Times" : "白班伙食次數", borderStyle));
            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Overtime Meal Times" : "加班伙食費", borderStyle));
            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Night Shift Allowance Times" : "夜點費次數", borderStyle));
            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Night Shift Meal Times" : "夜班伙食次數", borderStyle));

            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Total paid days" : "有薪天數", borderStyle));
            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Salary & Allowance" : "底薪&津貼", borderStyle));
            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Hourly Wage" : "時薪", borderStyle));

            // Salary 
            if (salary_Name.Any())
            {
                cells.Add(new Cell(3, currentColumn, "Salary Item", labelStyle));
                foreach (var salary in salary_Name)
                {
                    cells.Add(new Cell(4, currentColumn, salary.Value, labelSalStyle));
                    currentColumn++;
                }
            }


            // Overtime and Night Shift Allowance
            if (overtimeAllowance_Name.Any())
            {
                cells.Add(new Cell(3, currentColumn, "Overtime and Night Shift Allowance", labelStyle));
                foreach (var allowance in overtimeAllowance_Name)
                {
                    cells.Add(new Cell(4, currentColumn, allowance.Value, labelSalStyle));
                    currentColumn++;
                }
            }

            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Social-Health - Unemployment insurance 21.5%" : "醫/社保", borderStyle));
            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Meal Total" : "餐費合計", borderStyle));
            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Addition Item" : "其他加項", borderStyle));
            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Total Addition Item " : "正項合計", borderStyle));
            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Loaned Amount" : "借支金額扣", borderStyle));

            // Insurance Deduction
            if (insuranceDeduction_Name.Any())
            {
                cells.Add(new Cell(3, currentColumn, "Insurance Deduction", labelStyle));
                foreach (var insuranceDeduction in insuranceDeduction_Name)
                {
                    cells.Add(new Cell(4, currentColumn, insuranceDeduction.Value, labelSalStyle));
                    currentColumn++;
                }
            }

            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Union fee" : "工會費", borderStyle));
            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Tax" : "所得稅", borderStyle));
            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Other Deduction" : "其他扣項", borderStyle));
            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Total Deduction Item " : "負項合計", borderStyle));
            cells.Add(new Cell(4, currentColumn++, param.Lang == "en" ? "Net Amount Received" : "實領金額", borderStyle));

            for (int i = 0; i < data.Count; i++)
            {
                int colIndex = 8;
                var rowData = data[i];

                // Leave Days
                if (leave_Name.Any())
                {
                    foreach (var leave in leave_Name)
                    {
                        var leaveItem = rowData.Leave_Days.FirstOrDefault(x => x.Key == leave.Key);
                        SetCell(i + 5, colIndex++, cells, leaveItem.Value, amountStyle);
                    }
                }

                SetCell(i + 5, colIndex++, cells, rowData.Delay_Early, amountStyle);

                // Overtime Hours
                if (overtime_Name.Any())
                {
                    foreach (var overtime in overtime_Name)
                    {
                        var overtimeItem = rowData.Overtime_Hours.FirstOrDefault(x => x.Key == overtime.Key);
                        SetCell(i + 5, colIndex++, cells, overtimeItem.Value, amountStyle);
                    }
                }
                SetCell(i + 5, colIndex++, cells, (decimal)rowData.DayShift_Food, amountStyle);
                SetCell(i + 5, colIndex++, cells, rowData.Food_Expenses, amountStyle);
                SetCell(i + 5, colIndex++, cells, rowData.Night_Eat_Times, amountStyle);
                SetCell(i + 5, colIndex++, cells, (decimal)rowData.NightShift_Food, amountStyle);

                SetCell(i + 5, colIndex++, cells, rowData.Total_Paid_Days, decimalStyle);
                SetCell(i + 5, colIndex++, cells, rowData.Salary_Allowance, amountStyle);
                SetCell(i + 5, colIndex++, cells, rowData.Hourly_Wage, decimalStyle3);

                // Salary
                if (salary_Name.Any())
                {
                    foreach (var salary in salary_Name)
                    {
                        var salaryItems = rowData.Salary_Item.FirstOrDefault(x => x.Key == salary.Key);
                        SetCell(i + 5, colIndex++, cells, salaryItems.Value, amountStyle);
                    }
                }



                // Overtime and Night Shift Allowance
                if (overtimeAllowance_Name.Any())
                {
                    foreach (var allowance in overtimeAllowance_Name)
                    {
                        var overtimeAllowanceItem = rowData.Overtime_Allowance.FirstOrDefault(x => x.Key == allowance.Key);
                        SetCell(i + 5, colIndex++, cells, overtimeAllowanceItem.Value, amountStyle);
                    }
                }

                SetCell(i + 5, colIndex++, cells, rowData.B49_amt, amountStyle);
                SetCell(i + 5, colIndex++, cells, rowData.Meal_Total, amountStyle);
                SetCell(i + 5, colIndex++, cells, rowData.Other_Additions, amountStyle);
                SetCell(i + 5, colIndex++, cells, rowData.Total_Addition_Item, amountStyle);
                SetCell(i + 5, colIndex++, cells, rowData.Loaned_Amount, amountStyle);

                // Deduction
                if (insuranceDeduction_Name.Any())
                {
                    foreach (var insuranceDeduction in insuranceDeduction_Name)
                    {
                        var insuranceDeductionItems = rowData.Insurance_Deduction.FirstOrDefault(x => x.Key == insuranceDeduction.Key);
                        SetCell(i + 5, colIndex++, cells, insuranceDeductionItems.Value, amountStyle);
                    }
                }

                SetCell(i + 5, colIndex++, cells, rowData.Union_Fee, amountStyle);
                SetCell(i + 5, colIndex++, cells, rowData.Tax, amountStyle);
                SetCell(i + 5, colIndex++, cells, rowData.Other_Deductions, amountStyle);
                SetCell(i + 5, colIndex++, cells, rowData.Total_Deduction_Item, amountStyle);
                SetCell(i + 5, colIndex++, cells, rowData.Net_Amount_Received, amountStyle);
            }

            List<Table> tables = new() { new("result", data) };
            ConfigDownload configDownload = new(true);
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                tables,
                cells,
                "Resources\\Template\\SalaryReport\\7_2_5_MonthlySalaryDetailReport\\Download.xlsx",
                configDownload
            );

            if (excelResult.IsSuccess)
            {
                var downloadResult = new
                {
                    fileData = excelResult.Result,
                    totalCount = data.Count
                };
                return new OperationResult(true, downloadResult);
            }
            else
                return new OperationResult(false, excelResult.Error);
        }
        private static void SetCell(int rowIndex, int colIndex, List<Cell> cells, decimal value, Aspose.Cells.Style style)
        {
            decimal totalValue = value;
            var tempCell = new Cell(rowIndex, colIndex, value, style);
            var recentCell = cells.FirstOrDefault(x => x.Location == tempCell.Location);
            if (recentCell == null)
                cells.Add(tempCell);
            else
            {
                totalValue += (decimal)recentCell.Value;
                recentCell.Value = tempCell.Value;
            }
            cells.Add(new Cell(rowIndex + 1, colIndex, totalValue, style));
        }
        #endregion
    }
}