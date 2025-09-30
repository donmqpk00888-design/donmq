using System.Globalization;
using API.Data;
using API._Services.Interfaces.SalaryReport;
using API.DTOs.SalaryReport;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace API._Services.Services.SalaryReport
{
    public class S_7_2_17_MonthlyPersonalIncomeTaxAmountReport : BaseServices, I_7_2_17_MonthlyPersonalIncomeTaxAmountReport
    {
        public S_7_2_17_MonthlyPersonalIncomeTaxAmountReport(DBContext dbContext) : base(dbContext)
        {
        }

        private async Task<OperationResult> GetData(D_7_2_17_MonthlyPersonalIncomeTaxAmountReportParam param)
        {
            var YearMonth = Convert.ToDateTime(param.Year_Month);

            var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory
                && param.Permission_Group.Contains(x.Permission_Group));

            var predHST = PredicateBuilder.New<HRMS_Sal_Tax>(x => x.Factory == param.Factory
                && x.Sal_Month.Date == YearMonth.Date);

            if (!string.IsNullOrWhiteSpace(param.Department))
            {
                predHST.And(x => x.Department == param.Department);
            }
            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
            {
                predHST.And(x => x.Employee_ID.Contains(param.Employee_ID.Trim()));
            }

            var data = await _repositoryAccessor.HRMS_Sal_Tax.FindAll(predHST, true)
                                            .Join(_repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP,true),
                                            x => x.Employee_ID,
                                            y => y.Employee_ID,
                                            (x, y) => new DataSave { HST = x, HEP = y }).ToListAsync();

            return new OperationResult(true, data);
        }

        public async Task<OperationResult> GetTotalRows(D_7_2_17_MonthlyPersonalIncomeTaxAmountReportParam param)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;
            var data = (List<DataSave>)result.Data;
            return new OperationResult(true, data.Count());
        }

        public async Task<OperationResult> Download(D_7_2_17_MonthlyPersonalIncomeTaxAmountReportParam param)
        {

            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;

            var data = (List<DataSave>)result.Data;

            if (data.Count == 0)
                return new OperationResult(false, "System.Message.NoData");
            var YearMonth = Convert.ToDateTime(param.Year_Month);
            var departmentList = await GetListDepartment(param.Factory, param.Language);
            List<D_7_2_17_MonthlyPersonalIncomeTaxAmountReportData> dataTable = new();
            var type_Seq = new List<string>()
            {
                BasicCodeTypeConstant.Factory,
                BasicCodeTypeConstant.PermissionGroup,
            };

            var HBC = _repositoryAccessor.HRMS_Basic_Code
                            .FindAll(x => type_Seq.Contains(x.Type_Seq), true);
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language
                .FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true);

            var BasicCodeLanguage = await HBC
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
                    Code_Name = $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                })
                .ToListAsync();

            var factory = BasicCodeLanguage
                .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.Factory
                                  && x.Code == param.Factory).Code_Name;

            var permissionGroup = BasicCodeLanguage
                .Where(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup
                         && param.Permission_Group.Contains(x.Code))
                .Select(x => x.Code_Name);
            var HSTN = await _repositoryAccessor.HRMS_Sal_Tax_Number.FindAll(x => x.Factory == param.Factory && data.Select(x => x.HST).Select(x => x.Employee_ID).Contains(x.Employee_ID)).ToListAsync();
            var HSTaxFree = await _repositoryAccessor.HRMS_Sal_TaxFree.FindAll(x => x.Factory == param.Factory && new List<string> { "A", "K" }.Contains(x.Type)).ToListAsync();
            var HSMD = await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == param.Factory &&
                                 x.Sal_Month == YearMonth &&
                                 new List<string> { "45", "42", "49" }.Contains(x.Type_Seq) &&
                                 new List<string> { "A", "B" }.Contains(x.AddDed_Type), true).ToListAsync();
            var HSPMD = await _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail
                   .FindAll(x => x.Factory == param.Factory &&
                                x.Sal_Month == YearMonth, true).ToListAsync();
            var stt = 1;
            var HEP = data.Select(x=> x.HEP);
            foreach (var item in data)
            {
                var Sal_Tax = item.HST;
                var Sal_Tax_Number = HSTN.FirstOrDefault(x => x.Employee_ID == Sal_Tax.Employee_ID && x.Year <= new DateTime(Sal_Tax.Sal_Month.Year, 1, 1));
                var A = HSTaxFree.Where(x => x.Type == "A" && x.Effective_Month <= Sal_Tax.Sal_Month).OrderByDescending(x => x.Effective_Month).FirstOrDefault();
                var K = HSTaxFree.Where(x => x.Type == "K" && x.Effective_Month <= Sal_Tax.Sal_Month).OrderByDescending(x => x.Effective_Month).FirstOrDefault();
                var TOT = Sal_Tax.Salary_Amt - A.Amount - (K.Amount * Sal_Tax.Num_Dependents);
                if (TOT < 0)
                    TOT = 0;
                var SUBQTY_AMT = K.Amount * Sal_Tax.Num_Dependents;
                var department = departmentList.FirstOrDefault(x => x.Key == Sal_Tax.Department).Value.Split(" - ");
                int total1 = Query_Sal_Monthly_Detail_Sum("Y", Sal_Tax.Employee_ID, "45", "A", HSMD);
                int total2 = Query_Sal_Monthly_Detail_Sum("Y", Sal_Tax.Employee_ID, "42", "A", HSMD);
                int total3 = Query_Sal_Monthly_Detail_Sum("Y", Sal_Tax.Employee_ID, "49", "A", HSMD);
                int total4 = Query_Sal_Monthly_Detail_Sum("Y", Sal_Tax.Employee_ID, "49", "B", HSMD);

                int totalAdditionItem = total1 + total2 + total3 + total4;
                dataTable.Add(new D_7_2_17_MonthlyPersonalIncomeTaxAmountReportData
                {
                    No = stt++,
                    Factory = Sal_Tax.Factory,
                    Department = Sal_Tax.Department,
                    DepartmentName = department.Length == 2 ? department[1] : "",
                    EmployeeID = Sal_Tax.Employee_ID,
                    LocalFullName = HEP.FirstOrDefault(x => x.USER_GUID == Sal_Tax.USER_GUID).Local_Full_Name,
                    TaxNo = Sal_Tax_Number?.TaxNo,
                    NumberofDependents = Sal_Tax.Num_Dependents,
                    TotalAllowableDeductionAmountfromTaxableIncomeBasedonFamilyCircumstances = Sal_Tax.Salary_Amt,
                    DeductionAmountBasedonNumberofDependents = SUBQTY_AMT,
                    TaxableAmountofPersonalIncome = TOT,
                    Tax = Sal_Tax.Tax,
                    TotalAdditionItem = totalAdditionItem
                });
            };

            List<Cell> cells = new()
            {
                new Cell("B2", factory),
                new Cell("D2", param.Year_Month),
                new Cell("F2", string.Join(", ", permissionGroup)),
                new Cell("H2", departmentList.FirstOrDefault(x=> x.Key == param.Department).Value),
                new Cell("J2", param.Employee_ID),
                new Cell("B3", param.UserName),
                new Cell("D3", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
            };

            List<Table> tables = new()
            {
                new Table("result", dataTable)
            };

            ConfigDownload configDownload = new(false);
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                tables,
                cells,
                "Resources\\Template\\SalaryReport\\7_2_17_MonthlyPersonalIncomeTaxAmountReport\\7.2.17_Download.xlsx",
                configDownload
            );
            if (excelResult.IsSuccess)
                return new OperationResult(excelResult.IsSuccess, new { TotalRows = data.Count, Excel = excelResult.Result });
            else
                return new OperationResult(excelResult.IsSuccess, excelResult.Error);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
        {
            var departments = await Query_Department_List(factory);
            var departmentsWithLanguage = await _repositoryAccessor.HRMS_Org_Department
                .FindAll(x => x.Factory == factory
                           && departments.Select(y => y.Department_Code).Contains(x.Department_Code), true)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    x => new { x.Division, x.Factory, x.Department_Code },
                    y => new { y.Division, y.Factory, y.Department_Code },
                    (HOD, HODL) => new { HOD, HODL })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, HODL = y })
                .Select(x => new KeyValuePair<string, string>(x.HOD.Department_Code, $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}"))
                .Distinct()
                .ToListAsync();
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
        #region Query_Sal_Monthly_Detail_Add_Sum
        public int Query_Sal_Monthly_Detail_Sum(string kind, string employeeId, string typeSeq, string addedType, List<HRMS_Sal_Monthly_Detail> HSMD)
        {
            return HSMD
                .FindAll(x =>
                             x.Employee_ID == employeeId &&
                             x.Type_Seq == typeSeq &&
                             x.AddDed_Type == addedType)
                .Sum(x => (int?)x.Amount ?? 0);
        }
        #endregion
    }
}