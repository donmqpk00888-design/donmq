using API._Services.Interfaces.SalaryReport;
using API.Data;
using API.DTOs.SalaryReport;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryReport
{
    public class S_7_2_12_MonthlySalaryTransferDetails : BaseServices, I_7_2_12_MonthlySalaryTransferDetails
    {
        public S_7_2_12_MonthlySalaryTransferDetails(DBContext dbContext) : base(dbContext)
        {
        }

        #region GetTotalRows
        public async Task<OperationResult> GetTotalRows(MonthlySalaryTransferDetailsParam param)
        {
            var data = await GetData(param).CountAsync();
            return new OperationResult(true, data);
        }
        #endregion
        #region Excel
        public async Task<OperationResult> Download(MonthlySalaryTransferDetailsParam param)
        {
            var type_Seq = new List<string>()
            {
                BasicCodeTypeConstant.Factory,
                BasicCodeTypeConstant.PermissionGroup,
                BasicCodeTypeConstant.BankNameOrBranch
            };

            List<MonthlySalaryTransferDetails> listData = new();

            var data = GetData(param);

            if (!await data.AnyAsync())
                return new OperationResult(false, "No data found");

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == param.Factory
                                                            && param.Permission_Group.Contains(x.Permission_Group), true).Select(x => new
                                                            {
                                                                x.USER_GUID,
                                                                x.Local_Full_Name,
                                                                Division = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Division : x.Division,
                                                                Factory = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Factory : x.Factory,
                                                                Department_Code = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Department : x.Department,
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

            var result = await data.Join(HEP,
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { data = x, HEP = y })
                  .GroupJoin(HOD_Lang,
                    x => new { x.HEP.Factory, x.HEP.Division, x.HEP.Department_Code },
                    y => new { y.Factory, y.Division, y.Department_Code },
                    (x, y) => new { x.data, x.HEP, HOD_Lang = y })
                    .SelectMany(x => x.HOD_Lang.DefaultIfEmpty(),
                    (x, y) => new { x.data, x.HEP, HOD_Lang = y }).
                Select(x => new MonthlySalaryTransferDetails
                {
                    Factory = x.data.Factory,
                    Department = x.HEP.Department_Code,
                    DepartmentName = x.HOD_Lang != null ? x.HOD_Lang.Department_Name : x.HEP.Department_Code,
                    EmployeeID = x.data.EmployeeID,
                    Permission_Group = x.data.Permission_Group,
                    LocalFullName = x.data.LocalFullName,
                    BankAccount = x.data.BankAccount,
                    IdentificationNumber = x.data.IdentificationNumber,
                    Sal_Month = x.data.Sal_Month,
                    Tax = x.data.Tax
                })
                .OrderBy(x => x.Department)
                .ThenBy(x => x.EmployeeID)
                .ToListAsync();

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

            var listResult = result;
            var employeeIds = listResult.Select(x => x.EmployeeID).Distinct().ToList();
            var yearMonthValue = DateTime.Parse(param.Year_Month);

            var total45A = await Query_Sal_Monthly_Detail_Sum("Y", param.Factory, yearMonthValue, employeeIds, "45", "A");
            var total42A = await Query_Sal_Monthly_Detail_Sum("Y", param.Factory, yearMonthValue, employeeIds, "42", "A");
            var total49A = await Query_Sal_Monthly_Detail_Sum("Y", param.Factory, yearMonthValue, employeeIds, "49", "A");
            var total49B = await Query_Sal_Monthly_Detail_Sum("Y", param.Factory, yearMonthValue, employeeIds, "49", "B");
            var total57D = await Query_Sal_Monthly_Detail_Sum("Y", param.Factory, yearMonthValue, employeeIds, "57", "D");
            var total49C = await Query_Sal_Monthly_Detail_Sum("Y", param.Factory, yearMonthValue, employeeIds, "49", "C");
            var total49D = await Query_Sal_Monthly_Detail_Sum("Y", param.Factory, yearMonthValue, employeeIds, "49", "D");

            var d45A = total45A.ToDictionary(x => x.Employee_ID, x => x.Amount);
            var d42A = total42A.ToDictionary(x => x.Employee_ID, x => x.Amount);
            var d49A = total49A.ToDictionary(x => x.Employee_ID, x => x.Amount);
            var d49B = total49B.ToDictionary(x => x.Employee_ID, x => x.Amount);
            var d57D = total57D.ToDictionary(x => x.Employee_ID, x => x.Amount);
            var d49C = total49C.ToDictionary(x => x.Employee_ID, x => x.Amount);
            var d49D = total49D.ToDictionary(x => x.Employee_ID, x => x.Amount);

            static decimal GetAmount(Dictionary<string, decimal> dict, string id)
            {
                return dict.TryGetValue(id, out var val) ? val : 0m;
            }

            foreach (var item in listResult)
            {
                seq++;
                var employeeID = item.EmployeeID;

                decimal addSum = GetAmount(d45A, employeeID)
                               + GetAmount(d42A, employeeID)
                               + GetAmount(d49A, employeeID)
                               + GetAmount(d49B, employeeID);

                decimal dedSum = GetAmount(d57D, employeeID)
                               + GetAmount(d49C, employeeID)
                               + GetAmount(d49D, employeeID);

                decimal amount = addSum - dedSum - ((int?)item.Tax ?? 0);
                listData.Add(new MonthlySalaryTransferDetails
                {
                    Seq = seq,
                    Factory = item.Factory,
                    Department = item.Department,
                    DepartmentName = item.DepartmentName,
                    EmployeeID = employeeID,
                    LocalFullName = item.LocalFullName,
                    BankAccount = item.BankAccount ?? string.Empty,
                    Amount = amount,
                    IdentificationNumber = item.IdentificationNumber,
                    BankName = bankRow.FirstOrDefault(x => x.Code_Name.Contains(item.Permission_Group))?.Char1,
                    Branch = bankRow.FirstOrDefault(x => x.Code_Name.Contains(item.Permission_Group))?.Char2,
                });
            }

            List<Cell> cells = new()
            {
                new Cell("B2", factory),
                new Cell("D2", DateTime.Parse(param.Year_Month).ToString("yyyy/MM")),
                new Cell("F2", !string.IsNullOrEmpty(department) ? department : param.Department),
                new Cell("H2", string.Join(", ", permissionGroupList)),
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
                "Resources\\Template\\SalaryReport\\7_2_12_MonthlySalaryTransferDetails\\Download.xlsx",
                configDownload
            );
            if (excelResult.IsSuccess)
                return new OperationResult(excelResult.IsSuccess, new { TotalRows = listData.Count, Excel = excelResult.Result });
            else
                return new OperationResult(excelResult.IsSuccess, excelResult.Error);

        }

        #endregion

        #region GetData
        private IQueryable<MonthlySalaryTransferDetails> GetData(MonthlySalaryTransferDetailsParam param)
        {
            var yearMonth = DateTime.Parse(param.Year_Month);

            var predHSM = PredicateBuilder.New<HRMS_Sal_Monthly>(x =>
                x.Factory == param.Factory &&
                x.Sal_Month == yearMonth &&
                x.BankTransfer == "Y");

            if (!string.IsNullOrWhiteSpace(param.Department))
                predHSM.And(x => x.Department == param.Department);

            var HSM = _repositoryAccessor.HRMS_Sal_Monthly.FindAll(predHSM, true);
            var HSBA = _repositoryAccessor.HRMS_Sal_Bank_Account.FindAll(x => x.Factory == param.Factory && x.BankNo != null && x.BankNo.Trim() != "", true);
            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == param.Factory &&
                                            param.Permission_Group.Contains(x.Permission_Group), true);

            var data = HSBA.Join(HEP,
                x => new { x.Factory, x.Employee_ID },
                y => new { y.Factory, y.Employee_ID },
                (x, y) => new { HSBA = x, HEP = y })
            .Join(HSM,
                x => new { x.HSBA.Factory, x.HSBA.Employee_ID },
                y => new { y.Factory, y.Employee_ID },
                (x, y) => new { x.HSBA, x.HEP, HSM = y })
            .Select(x => new MonthlySalaryTransferDetails
            {
                USER_GUID = x.HEP.USER_GUID,
                Factory = x.HEP.Factory,
                EmployeeID = x.HEP.Employee_ID,
                LocalFullName = x.HEP.Local_Full_Name,
                IdentificationNumber = x.HEP.Identification_Number,
                BankAccount = x.HSBA.BankNo,
                Sal_Month = x.HSM.Sal_Month,
                Tax = x.HSM.Tax,
                Permission_Group = x.HEP.Permission_Group
            });

            return data.AsNoTracking();
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