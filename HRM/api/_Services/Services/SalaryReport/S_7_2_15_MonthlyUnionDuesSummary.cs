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
    public class S_7_2_15_MonthlyUnionDuesSummary : BaseServices, I_7_2_15_MonthlyUnionDuesSummary
    {
        public S_7_2_15_MonthlyUnionDuesSummary(DBContext dbContext) : base(dbContext)
        {
        }

        private async Task<OperationResult> GetData(MonthlyUnionDuesSummaryParam param)
        {
            if (string.IsNullOrWhiteSpace(param.Factory)
                || string.IsNullOrWhiteSpace(param.Year_Month)
                || !DateTime.TryParseExact(param.Year_Month, "yyyy/MM", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime yearMonth))
                return new OperationResult(false, "SalaryReport.MonthlyUnionDuesSummary.InvalidInput");

            var pred = PredicateBuilder.New<HRMS_Sal_Monthly>(x => x.Factory == param.Factory
                && x.Sal_Month.Date == yearMonth.Date);

            if (!string.IsNullOrWhiteSpace(param.Department))
                pred.And(x => x.Department == param.Department);

            var wk_sql = await _repositoryAccessor.HRMS_Sal_Monthly.FindAll(pred)
                .OrderBy(x => x.Department)
                .ThenBy(x => x.Employee_ID)
                .ToListAsync();
            
            var HSAM = _repositoryAccessor.HRMS_Sal_Monthly_Detail.FindAll(x => x.Factory == param.Factory
                        && x.Sal_Month == yearMonth
                        && x.Type_Seq == "49"
                        && x.AddDed_Type == "D", true)
                    .ToList();

            var listDepartments = await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == param.Factory)
                    .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true),
                        x => new { x.Division, x.Factory, x.Department_Code },
                        y => new { y.Division, y.Factory, y.Department_Code },
                        (HOD, HODL) => new { HOD, HODL })
                    .SelectMany(x => x.HODL.DefaultIfEmpty(),
                        (x, y) => new { x.HOD, HODL = y })
                    .ToListAsync();

            var fee = HSAM
                .GroupBy(x => x.Employee_ID)
                .Select(x => new
                {
                    Employee_ID = x.Key,
                    UnionFee = x.Where(x => x.Item == "D12").Sum(x => x.Amount),
                    InsuranceFee = x.Where(x => x.Item == "D11").Sum(x => x.Amount)
                })
                .ToList();

            var result = wk_sql
                .GroupBy(x => new { x.Factory, x.Department })
                .Select(x => new MonthlyUnionDuesSummaryParamReport
                {
                    Factory = x.Key.Factory,
                    Department = x.Key.Department,
                    DepartmentName = listDepartments.FirstOrDefault(y => y.HOD.Department_Code == x.Key.Department)?.HODL?.Name
                                    ?? listDepartments.FirstOrDefault(y => y.HOD.Department_Code == x.Key.Department)?.HOD.Department_Name,
                    Union_fee = x.Sum(item => fee.FirstOrDefault(y => y.Employee_ID == item.Employee_ID)?.UnionFee ?? 0),
                    Medical_Insurance_Fee = x.Sum(item => fee.FirstOrDefault(y => y.Employee_ID == item.Employee_ID)?.InsuranceFee ?? 0),
                    TotalAmount = x.Sum(item =>
                        (fee.FirstOrDefault(y => y.Employee_ID == item.Employee_ID)?.UnionFee ?? 0) +
                        (fee.FirstOrDefault(y => y.Employee_ID == item.Employee_ID)?.InsuranceFee ?? 0))
                })
                .ToList();

            return new OperationResult(true, result);
        }

        public async Task<OperationResult> Download(MonthlyUnionDuesSummaryParam param)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;

            var data = (List<MonthlyUnionDuesSummaryParamReport>)result.Data;

            if (!data.Any())
                return new OperationResult(false, "System.Message.NoData");

            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "2");
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

            var department = string.Empty;
            if (!string.IsNullOrWhiteSpace(param.Department))
            {
                department = await _repositoryAccessor.HRMS_Org_Department
                    .FindAll(x => x.Factory == param.Factory && x.Department_Code == param.Department, true)
                    .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true),
                        x => new { x.Division, x.Factory, x.Department_Code },
                        y => new { y.Division, y.Factory, y.Department_Code },
                        (HOD, HODL) => new { HOD, HODL })
                    .SelectMany(x => x.HODL.DefaultIfEmpty(),
                        (x, y) => new { x.HOD, HODL = y })
                    .Select(x => $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}")
                    .FirstOrDefaultAsync();
            }

            var factory = BasicCodeLanguage
                .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.Factory
                    && x.Code == param.Factory).Code_Name;

            List<Cell> cells = new()
            {
                new Cell("B2", factory),
                new Cell("D2", param.Year_Month),
                new Cell("F2", department),
                new Cell("B4", param.UserName),
                new Cell("D4", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
            };
            List<Table> tables = new()
            {
                new Table("result", data)
            };

            ConfigDownload configDownload = new(false);
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                tables,
                cells,
                "Resources\\Template\\SalaryReport\\7_2_15_MonthlyUnionDuesSummary\\Download.xlsx",
                configDownload
            );
            if (excelResult.IsSuccess)
                return new OperationResult(excelResult.IsSuccess, new { TotalRows = data.Count, Excel = excelResult.Result });
            else
                return new OperationResult(excelResult.IsSuccess, excelResult.Error);
        }

        public async Task<OperationResult> GetTotalRows(MonthlyUnionDuesSummaryParam param)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;
            var data = (List<MonthlyUnionDuesSummaryParamReport>)result.Data;
            return new OperationResult(true, data.Count);
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
    }
}