using System.Globalization;
using API._Services.Interfaces.SalaryReport;
using API.Data;
using API.DTOs.SalaryReport;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryReport
{
    public class S_7_2_11_SalarySlipPrintingExitedEmployee : BaseServices, I_7_2_11_SalarySlipPrintingExitedEmployee
    {
        public S_7_2_11_SalarySlipPrintingExitedEmployee(DBContext dbContext) : base(dbContext)
        {
        }

        public async Task<OperationResult> GetTotalRows(SalarySlipPrintingExitedEmployeeParam param)
        {
            var result = await GetData(param, true);
            if (!result.IsSuccess)
                return result;
            var data = (List<dynamic>)result.Data;
            return new OperationResult(true, data.Count);
        }
        #region Download
        public Task<OperationResult> Download(SalarySlipPrintingExitedEmployeeParam param)
        {
            throw new NotImplementedException();
        }
        #endregion
        #region GetData
        private async Task<OperationResult> GetData(SalarySlipPrintingExitedEmployeeParam param, bool countOnly = false)
        {
            if (string.IsNullOrWhiteSpace(param.Factory)
                || !param.Permission_Group.Any()
                || !param.Language.Any()
                || string.IsNullOrWhiteSpace(param.Year_Month)
                || !DateTime.TryParseExact(param.Year_Month, "yyyy/MM", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime yearMonth))
                return new OperationResult(false, "SalaryReport.TaxPayingEmployeeMonthlyNightShiftExtraPayAndOvertimePay.InvalidInput");

            var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory
                && param.Permission_Group.Contains(x.Permission_Group));

            if (!string.IsNullOrWhiteSpace(param.Department))
                predHEP.And(x => x.Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.EmployeeID))
                predHEP.And(x => x.Employee_ID.Contains(param.EmployeeID));
            
            if (!string.IsNullOrWhiteSpace(param.StartDate) && !string.IsNullOrWhiteSpace(param.EndDate))
            {
                var start_Date = DateTime.Parse(param.StartDate);
                var end_Date = DateTime.Parse(param.EndDate);
                predHEP.And(x => x.Resign_Date.Value.Date >= start_Date.Date && x.Resign_Date.Value.Date <= end_Date.Date);
            }
                
            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP);
            var HSC = _repositoryAccessor.HRMS_Sal_Close.FindAll(x => x.Factory == param.Factory
                && x.Sal_Month == yearMonth
                && x.Close_Status == "Y");
            var HSRM = _repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(x => x.Factory == param.Factory
                && x.Sal_Month == yearMonth 
                && !HSC.Select(x => x.Employee_ID).Contains(x.Employee_ID));

            var wk_sql = await HEP
                .GroupJoin(HSRM,
                    x => x.Employee_ID,
                    y => y.Employee_ID,
                    (x, y) => new { HEP = x, HSRM = y })
                .SelectMany(x => x.HSRM.DefaultIfEmpty(),
                    (x, y) => new { x.HEP, HSRM = y })
                .Select(x => new {
                    x.HEP.USER_GUID,
                    x.HEP.Employee_ID,
                    x.HEP.Nationality,
                    x.HEP.Division,
                    x.HEP.Factory,
                    x.HEP.Department,
                    x.HEP.Permission_Group,
                    x.HSRM.Salary_Type,
                })
                .OrderBy(x => x.Employee_ID)
                .ToListAsync();
            if(param.Kind == "A")
            {
                foreach(var item in wk_sql)
                {
                    decimal wk_add = await Query_Sal_Monthly_Detail_Add_Sum("Y", param.Factory, yearMonth.AddMonths(-1), item.Employee_ID);

                    // --扣項合計 (Deduct)
                    decimal wk_sub = (await Query_Sal_Monthly_Detail_Ded_Sum("Y", param.Factory, yearMonth, item.Employee_ID)) + tax;
                    var LSalary = 
                }
            }

            if (countOnly == true)
                return new OperationResult(true, wk_sql);
            throw new NotImplementedException();
        }
        #endregion

        #region Query_Sal_Monthly_Detail_Add_Sum
        private async Task<int> Query_Sal_Monthly_Detail_Add_Sum(string Kind, string Factory, DateTime Year_Month, string Employee_ID)
        {
            int total1 = await Query_Sal_Monthly_Detail_Sum(Kind, Factory, Year_Month, Employee_ID, "45", "A");
            int total2 = await Query_Sal_Monthly_Detail_Sum(Kind, Factory, Year_Month, Employee_ID, "42", "A");
            int total3 = await Query_Sal_Monthly_Detail_Sum(Kind, Factory, Year_Month, Employee_ID, "49", "A");
            int total4 = await Query_Sal_Monthly_Detail_Sum(Kind, Factory, Year_Month, Employee_ID, "49", "B");

            int addTotal = total1 + total2 + total3 + total4;
            return addTotal;
        }
        #endregion

        #region Query_Sal_Monthly_Detail_Ded_Sum
        private async Task<int> Query_Sal_Monthly_Detail_Ded_Sum(string Kind, string Factory, DateTime Year_Month, string Employee_ID)
        {
            int total1 = await Query_Sal_Monthly_Detail_Sum(Kind, Factory, Year_Month, Employee_ID, "57", "D");
            int total2 = await Query_Sal_Monthly_Detail_Sum(Kind, Factory, Year_Month, Employee_ID, "49", "C");
            int total3 = await Query_Sal_Monthly_Detail_Sum(Kind, Factory, Year_Month, Employee_ID, "49", "D");

            int addTotal = total1 + total2 + total3;
            return addTotal;
        }
        #endregion

        #region GetList
        private async Task<List<KeyValuePair<string, string>>> GetPermissionGroup(string factory, string Language)
        {
            return await Query_BasicCode_PermissionGroup(factory, Language);
        }
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

        public Task<List<KeyValuePair<string, string>>> GetListLanguage(string factory, string language)
        {
            throw new NotImplementedException();
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
        #endregion
    }
}