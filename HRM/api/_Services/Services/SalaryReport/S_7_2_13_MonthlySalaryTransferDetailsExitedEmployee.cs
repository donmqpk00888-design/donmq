using API._Services.Interfaces.SalaryReport;
using API.Data;
using API.DTOs.SalaryReport;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryReport
{
    public class S_7_2_13_MonthlySalaryTransferDetailsExitedEmployee : BaseServices, I_7_2_13_MonthlySalaryTransferDetailsExitedEmployee
    {
        public S_7_2_13_MonthlySalaryTransferDetailsExitedEmployee(DBContext dbContext) : base(dbContext)
        {
        }

        #region GetTotalRows
        public async Task<OperationResult> GetTotalRows(MonthlySalaryTransferDetailsExitedEmployeeParam param)
        {
            var data = await GetData(param);
            return new OperationResult(true, data.Count);
        }
        #endregion

        #region Excel
        public async Task<OperationResult> Download(MonthlySalaryTransferDetailsExitedEmployeeParam param)
        {
            var type_Seq = new List<string>()
            {
                BasicCodeTypeConstant.Factory,
                BasicCodeTypeConstant.PermissionGroup,
                BasicCodeTypeConstant.BankNameOrBranch
            };

            List<MonthlySalaryTransferDetailsExitedEmployee> listData = new();

            var rawData = await GetData(param);

            if (!rawData.Any())
                return new OperationResult(false, "No data found");

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == param.Factory, true)
                .Select(x => new
                {
                    x.USER_GUID,
                    x.Local_Full_Name,
                    x.Employee_ID,
                    x.Resign_Date,
                    x.Identification_Number,
                    Division = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Division : x.Division,
                    Factory = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Factory : x.Factory,
                    Department_Code = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Department : x.Department,
                    x.Permission_Group
                });

            var HOD = _repositoryAccessor.HRMS_Org_Department
                .FindAll(x => x.Factory == param.Factory, true);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Factory == param.Factory && x.Language_Code.ToLower() == param.Language.ToLower(), true);

            var HOD_Lang = HOD
                .GroupJoin(HODL,
                    x => new { x.Division, x.Factory, x.Department_Code },
                    y => new { y.Division, y.Factory, y.Department_Code },
                    (x, y) => new { HOD = x, HODL = y })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, HODL = y })
                .Select(x => new
                {
                    x.HOD.Division,
                    x.HOD.Factory,
                    x.HOD.Department_Code,
                    Department_Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name,
                    Department_Title = $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}"
                });

            var HBC = _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => type_Seq.Contains(x.Type_Seq));
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language
                .FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true);

            var BasicCodeLanguage = HBC
                .GroupJoin(HBCL,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Code,
                    x.HBC.Type_Seq,
                    x.HBC.Char1,
                    x.HBC.Char2,
                    Code_Name = $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                });

            var result = rawData
                .Join(HEP,
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { data = x, HEP = y })
                .GroupJoin(HOD_Lang,
                    x => new { x.HEP.Factory, x.HEP.Division, x.HEP.Department_Code },
                    y => new { y.Factory, y.Division, y.Department_Code },
                    (x, y) => new { x.data, x.HEP, HOD_Lang = y })
                .SelectMany(x => x.HOD_Lang.DefaultIfEmpty(),
                    (x, y) => new { x.data, x.HEP, HOD_Lang = y })
                .Select(x => new MonthlySalaryTransferDetailsExitedEmployee
                {
                    USER_GUID = x.data.USER_GUID,
                    Factory = x.HEP.Factory,
                    Department = x.HEP.Department_Code,
                    DepartmentName = x.HOD_Lang != null ? x.HOD_Lang.Department_Name : x.HEP.Department_Code,
                    EmployeeID = x.HEP.Employee_ID,
                    LocalFullName = x.HEP.Local_Full_Name,
                    BankAccount = x.data.BankAccount,
                    Amount = x.data.Amount,
                    IdentificationNumber = x.HEP.Identification_Number,
                    DateOfResignation = x.HEP.Resign_Date,
                    Permission_Group = x.HEP.Permission_Group
                })
                .OrderBy(x => x.Department)
                .ThenBy(x => x.EmployeeID);

            var factory = await BasicCodeLanguage
                .Where(x => x.Type_Seq == BasicCodeTypeConstant.Factory && x.Code == param.Factory)
                .Select(x => x.Code_Name).FirstOrDefaultAsync();
            var department = HOD_Lang
                .FirstOrDefault(x => x.Department_Code == param.Department)?.Department_Title;

            var permissionGroupList = await BasicCodeLanguage
                .Where(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup
                        && param.Permission_Group.Contains(x.Code))
                .Select(x => x.Code_Name)
                .ToListAsync();

            var bankRow = _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.BankNameOrBranch
                               && x.Code.StartsWith(param.Factory));

            var seq = 0;
            foreach (var item in result.ToList())
            {
                seq++;
                listData.Add(new MonthlySalaryTransferDetailsExitedEmployee
                {
                    Seq = seq,
                    Factory = item.Factory,
                    Department = item.Department,
                    DepartmentName = item.DepartmentName,
                    EmployeeID = item.EmployeeID,
                    LocalFullName = item.LocalFullName,
                    BankAccount = item.BankAccount ?? string.Empty,
                    Amount = item.Amount,
                    IdentificationNumber = item.IdentificationNumber,
                    BankName = bankRow.FirstOrDefault(x => x.Code_Name.Contains(item.Permission_Group))?.Char1,
                    Branch = bankRow.FirstOrDefault(x => x.Code_Name.Contains(item.Permission_Group))?.Char2,
                    DateOfResignation = item.DateOfResignation
                });
            }

            List<Cell> cells = new()
            {
                new Cell("B2", factory),
                new Cell("D2", DateTime.Parse(param.Year_Month).ToString("yyyy/MM")),
                new Cell("F2", DateTime.Parse(param.Start_Date).ToString("yyyy/MM/dd")),
                new Cell("H2", DateTime.Parse(param.End_Date).ToString("yyyy/MM/dd")),
                new Cell("J2", !string.IsNullOrEmpty(department) ? department : param.Department),
                new Cell("L2", string.Join(", ", permissionGroupList)) ,
                new Cell("B3", param.UserName),
                new Cell("D3", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
            };

            List<Table> tables = new()
            {
                new Table("result", listData)
            };

            ConfigDownload configDownload = new(false);
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                tables,
                cells,
                "Resources\\Template\\SalaryReport\\7_2_13_MonthlySalaryTransferDetailsExitedEmployee\\Download.xlsx",
                configDownload
            );
            if (excelResult.IsSuccess)
                return new OperationResult(excelResult.IsSuccess, new { TotalRows = listData.Count, Excel = excelResult.Result });
            else
                return new OperationResult(excelResult.IsSuccess, excelResult.Error);
        }
        #endregion

        #region GetData
        private async Task<List<MonthlySalaryTransferDetailsExitedEmployee>> GetData(MonthlySalaryTransferDetailsExitedEmployeeParam param)
        {
            var yearMonth = DateTime.Parse(param.Year_Month);
            var previousMonth = yearMonth.AddMonths(-1);
            var start_Date = DateTime.Parse(param.Start_Date);
            var end_Date = DateTime.Parse(param.End_Date);
            List<MonthlySalaryTransferDetailsExitedEmployee> results = new();

            var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory &&
                        x.Resign_Date.HasValue &&
                        (x.Resign_Date.Value.Date >= start_Date.Date && x.Resign_Date.Value.Date <= end_Date.Date) &&
                        param.Permission_Group.Contains(x.Permission_Group));

            if (!string.IsNullOrWhiteSpace(param.Department))
                predHEP = predHEP.And(x => x.Department == param.Department);

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true);
            var HSBA = _repositoryAccessor.HRMS_Sal_Bank_Account.FindAll(x => x.Factory == param.Factory, true);
            var HSRM = _repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(x => x.Factory == param.Factory &&
                                                                                x.Sal_Month.Date == yearMonth.Date, true);
            var HSC = _repositoryAccessor.HRMS_Sal_Close.FindAll(x => x.Factory == param.Factory &&
                                                                      x.Sal_Month.Date == previousMonth.Date &&
                                                                      x.Close_Status == "Y", true);
            var closedEmployeeIds = HSC.Select(x => x.Employee_ID).ToHashSet();

            var HSM = _repositoryAccessor.HRMS_Sal_Monthly.FindAll(x => x.Factory == param.Factory &&
                                                                        x.Sal_Month.Date == previousMonth.Date &&
                                                                        !closedEmployeeIds.Contains(x.Employee_ID), true);

            var HSAS = _repositoryAccessor.HRMS_Sal_AddDedItem_Settings.FindAll(x => x.Factory == param.Factory &&
                                                                                    x.Effective_Month <= yearMonth.Date, true);

            var _HSAS = _repositoryAccessor.HRMS_Sal_AddDedItem_Settings.FindAll(x => x.Factory == param.Factory &&
                                                                                    x.Resigned_Print == "Y", true);
            var _HSAM = _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.FindAll(x => x.Factory == param.Factory &&
                                                                                    x.Sal_Month.Date == yearMonth.Date, true);

            var allAmount = _HSAS.Join(_HSAM,
            x => new { x.Factory, x.AddDed_Item },
            y => new { y.Factory, y.AddDed_Item },
            (x, y) => new { x, y }).Select(x => new
            {
                x.x.Effective_Month,
                x.x.Permission_Group,
                x.y.AddDed_Type,
                x.y.Employee_ID,
                x.y.Amount
            });

            var rawDataList = await HEP
            .GroupJoin(HSBA,
                x => new { x.Factory, x.Employee_ID },
                y => new { y.Factory, y.Employee_ID },
                (x, y) => new { HEP = x, HSBA = y })
            .SelectMany(x => x.HSBA.DefaultIfEmpty(),
            (x, y) => new { x.HEP, HSBA = y })
            .GroupJoin(HSRM,
                x => new { x.HEP.Factory, x.HEP.Employee_ID },
                y => new { y.Factory, y.Employee_ID },
                (x, y) => new { x.HEP, x.HSBA, HSRM = y })
            .SelectMany(x => x.HSRM.DefaultIfEmpty(),
            (x, y) => new { x.HEP, x.HSBA, HSRM = y })
            .GroupJoin(HSM,
                x => new { x.HEP.Factory, x.HEP.Employee_ID },
                y => new { y.Factory, y.Employee_ID },
                (x, y) => new { x.HEP, x.HSBA, x.HSRM, HSM = y })
            .SelectMany(x => x.HSM.DefaultIfEmpty(),
            (x, y) => new { x.HEP, x.HSBA, x.HSRM, HSM = y })
            .Join(HSAS,
                x => new { x.HSRM.Factory, x.HSRM.Permission_Group },
                y => new { y.Factory, y.Permission_Group },
                (x, y) => new { x.HEP, x.HSBA, x.HSRM, x.HSM, HSAS = y })
            .Select(x => new
            {
                x.HEP.USER_GUID,
                x.HEP.Factory,
                x.HEP.Employee_ID,
                x.HEP.Local_Full_Name,
                x.HEP.Identification_Number,
                x.HSBA.BankNo,
                x.HEP.Resign_Date,
                HSMTax = x.HSM != null ? (int?)x.HSM.Tax : null,
                HSRMTax = x.HSRM != null ? (int?)x.HSRM.Tax : null,
                x.HSRM.Permission_Group,
                HSASEffective_Month = x.HSAS.Effective_Month
            })
            .ToListAsync();

            var baseData = rawDataList
            .GroupBy(x => new
            {
                x.USER_GUID,
                x.Factory,
                x.Employee_ID,
                x.BankNo,
                Tax1 = (int?)x.HSMTax ?? 0,
                Tax2 = (int?)x.HSRMTax ?? 0,
                x.Permission_Group
            })
            .Select(g => new MonthlySalaryTransferDetailsExitedEmployee
            {
                USER_GUID = g.Key.USER_GUID,
                Factory = g.Key.Factory,
                EmployeeID = g.Key.Employee_ID,
                BankAccount = g.Key.BankNo,
                Tax1 = g.Key.Tax1,
                Tax2 = g.Key.Tax2,
                Permission_Group = g.Key.Permission_Group,
                Effective_Month = g.Max(item => item.HSASEffective_Month)
            }).ToList();

            if (!baseData.Any()) return new List<MonthlySalaryTransferDetailsExitedEmployee>();

            var employeeIds = baseData.Select(x => x.EmployeeID).ToList();

            var d45A_Y = (await Query_Sal_Monthly_Detail_Sum("Y", param.Factory, previousMonth, employeeIds, "45", "A")).ToDictionary(x => x.Employee_ID, x => (decimal)x.Amount);
            var d42A_Y = (await Query_Sal_Monthly_Detail_Sum("Y", param.Factory, previousMonth, employeeIds, "42", "A")).ToDictionary(x => x.Employee_ID, x => (decimal)x.Amount);
            var d49A_Y = (await Query_Sal_Monthly_Detail_Sum("Y", param.Factory, previousMonth, employeeIds, "49", "A")).ToDictionary(x => x.Employee_ID, x => (decimal)x.Amount);
            var d49B_Y = (await Query_Sal_Monthly_Detail_Sum("Y", param.Factory, previousMonth, employeeIds, "49", "B")).ToDictionary(x => x.Employee_ID, x => (decimal)x.Amount);
            var d57D_Y = (await Query_Sal_Monthly_Detail_Sum("Y", param.Factory, previousMonth, employeeIds, "57", "D")).ToDictionary(x => x.Employee_ID, x => (decimal)x.Amount);
            var d49C_Y = (await Query_Sal_Monthly_Detail_Sum("Y", param.Factory, previousMonth, employeeIds, "49", "C")).ToDictionary(x => x.Employee_ID, x => (decimal)x.Amount);
            var d49D_Y = (await Query_Sal_Monthly_Detail_Sum("Y", param.Factory, previousMonth, employeeIds, "49", "D")).ToDictionary(x => x.Employee_ID, x => (decimal)x.Amount);

            var d45A_N = (await Query_Sal_Monthly_Detail_Sum("N", param.Factory, yearMonth, employeeIds, "45", "A")).ToDictionary(x => x.Employee_ID, x => (decimal)x.Amount);
            var d42A_N = (await Query_Sal_Monthly_Detail_Sum("N", param.Factory, yearMonth, employeeIds, "42", "A")).ToDictionary(x => x.Employee_ID, x => (decimal)x.Amount);
            var d49A_N = (await Query_Sal_Monthly_Detail_Sum("N", param.Factory, yearMonth, employeeIds, "49", "A")).ToDictionary(x => x.Employee_ID, x => (decimal)x.Amount);
            var d49B_N = (await Query_Sal_Monthly_Detail_Sum("N", param.Factory, yearMonth, employeeIds, "49", "B")).ToDictionary(x => x.Employee_ID, x => (decimal)x.Amount);
            var d57D_N = (await Query_Sal_Monthly_Detail_Sum("N", param.Factory, yearMonth, employeeIds, "57", "D")).ToDictionary(x => x.Employee_ID, x => (decimal)x.Amount);
            var d49C_N = (await Query_Sal_Monthly_Detail_Sum("N", param.Factory, yearMonth, employeeIds, "49", "C")).ToDictionary(x => x.Employee_ID, x => (decimal)x.Amount);
            var d49D_N = (await Query_Sal_Monthly_Detail_Sum("N", param.Factory, yearMonth, employeeIds, "49", "D")).ToDictionary(x => x.Employee_ID, x => (decimal)x.Amount);

            decimal GetAmount(Dictionary<string, decimal> dict, string id)
                => dict.TryGetValue(id, out var val) ? val : 0m;

            List<string> listAdd = new() { "A", "B" };
            List<string> listDed = new() { "C", "D" };

            foreach (var item in baseData)
            {
                //Onjob sums(previous month)
                decimal addSum_Y = GetAmount(d45A_Y, item.EmployeeID) + GetAmount(d42A_Y, item.EmployeeID) +
                                   GetAmount(d49A_Y, item.EmployeeID) + GetAmount(d49B_Y, item.EmployeeID);

                decimal dedSum_Y = GetAmount(d57D_Y, item.EmployeeID) + GetAmount(d49C_Y, item.EmployeeID) + GetAmount(d49D_Y, item.EmployeeID);

                // Resign sums (year month)
                decimal addSum_N = GetAmount(d45A_N, item.EmployeeID) + GetAmount(d42A_N, item.EmployeeID) +
                                   GetAmount(d49A_N, item.EmployeeID) + GetAmount(d49B_N, item.EmployeeID);

                decimal dedSum_N = GetAmount(d57D_N, item.EmployeeID) + GetAmount(d49C_N, item.EmployeeID) + GetAmount(d49D_N, item.EmployeeID);

                decimal Onjob_Salary = addSum_Y - dedSum_Y - ((int?)item.Tax1 ?? 0);

                decimal Resign_Salary = addSum_N - dedSum_N - ((int?)item.Tax2 ?? 0);

                decimal Resign_Oth_Add = allAmount.Where(x => x.Effective_Month == item.Effective_Month &&
                                                        x.Permission_Group == item.Permission_Group &&
                                                        listAdd.Contains(x.AddDed_Type) &&
                                                        x.Employee_ID == item.EmployeeID).Sum(x => (int?)x.Amount ?? 0);

                decimal Resign_Oth_Ded = allAmount.Where(x => x.Effective_Month == item.Effective_Month &&
                                                        x.Permission_Group == item.Permission_Group &&
                                                        listDed.Contains(x.AddDed_Type) &&
                                                        x.Employee_ID == item.EmployeeID).Sum(x => (int?)x.Amount ?? 0);

                decimal Actual_Amt = Resign_Salary + Onjob_Salary + Resign_Oth_Add - Resign_Oth_Ded;

                if (Actual_Amt <= 0)
                    continue;

                results.Add(new MonthlySalaryTransferDetailsExitedEmployee
                {
                    USER_GUID = item.USER_GUID,
                    BankAccount = item.BankAccount,
                    Amount = Actual_Amt,
                });
            };

            return results;
        }
        #endregion

        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
        {
            var departments = await Query_Department_List(factory);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
              .FindAll(x => x.Factory == factory && x.Language_Code.ToLower() == language.ToLower())
              .ToList();
            var departmentsWithLanguage = departments
              .GroupJoin(HODL,
                x => new { x.Division, x.Department_Code },
                y => new { y.Division, y.Department_Code },
                (HOD, HODL) => new { HOD, HODL })
              .SelectMany(x => x.HODL.DefaultIfEmpty(),
                (x, y) => new { x.HOD, HODL = y })
              .Select(x => new KeyValuePair<string, string>(x.HOD.Department_Code, $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}"))
              .Distinct()
              .ToList();
            return departmentsWithLanguage;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string userName, string language)
        {
            List<string> factories = await Queryt_Factory_AddList(userName);
            var factoriesWithLanguage = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory
                           && factories.Contains(x.Code), true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
            return factoriesWithLanguage;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language)
        {
            var permissionGroups = await Query_Permission_List(factory);

            var permissionGroupsWithLanguage = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup
                           && permissionGroups.Select(y => y.Permission_Group).Contains(x.Code), true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
            return permissionGroupsWithLanguage;
        }
    }
}