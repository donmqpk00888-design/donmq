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
    public class S_7_2_7_MonthlySalaryAdditionsDeductionsSummaryReport : BaseServices, I_7_2_7_MonthlySalaryAdditionsDeductionsSummaryReport
    {
        private static readonly List<string> Kind = new() { "OnJob", "Resigned" };
        public S_7_2_7_MonthlySalaryAdditionsDeductionsSummaryReport(DBContext dbContext) : base(dbContext)
        {
        }

        private async Task<OperationResult> GetData(MonthlySalaryAdditionsDeductionsSummaryReportParam param)
        {
            if (string.IsNullOrWhiteSpace(param.Factory)
             || string.IsNullOrWhiteSpace(param.Kind)
             || !Kind.Contains(param.Kind)
             || !param.Permission_Group.Any()
             || string.IsNullOrWhiteSpace(param.Year_Month)
             || !DateTime.TryParseExact(param.Year_Month, "yyyy/MM", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime yearMonth))

                return new OperationResult(false, "SalaryReport.MonthlySalaryAdditionsDeductionsSummaryReport.InvalidInput");

            var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory
                && param.Permission_Group.Contains(x.Permission_Group));

            var predHSADM = PredicateBuilder.New<HRMS_Sal_AddDedItem_Monthly>(x => x.Factory == param.Factory
                && x.Sal_Month.Date == yearMonth.Date);

            var HSRM = _repositoryAccessor.HRMS_Sal_Resign_Monthly
                .FindAll(x => x.Factory == param.Factory
                           && x.Sal_Month.Date == yearMonth.Date)
                .Select(x => x.Employee_ID)
                .DefaultIfEmpty();

            if (!string.IsNullOrWhiteSpace(param.Department))
                predHEP.And(x => x.Department == param.Department);

            if (param.Kind == "OnJob")
                predHSADM.And(x => !HSRM.Contains(x.Employee_ID));
            else
                predHSADM.And(x => HSRM.Contains(x.Employee_ID));

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP);
            var HSADM = _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.FindAll(predHSADM);

            var data = await HEP
                .Join(HSADM,
                    x => new { x.Factory, x.USER_GUID },
                    y => new { y.Factory, y.USER_GUID },
                    (HEP, HSADM) => new { HEP, HSADM })
                .GroupBy(x => new
                {
                    x.HSADM.AddDed_Type,
                    x.HSADM.AddDed_Item
                })
                .Select(x => new MonthlySalaryAdditionsDeductionsSummaryReportData
                {
                    AddDed_Type = x.Key.AddDed_Type,
                    AddDed_Item = x.Key.AddDed_Item,
                    Amount = x.Sum(y => (decimal)y.HSADM.Amount) // Chuyển sang decimal do bị out range Int32
                })
                .OrderBy(x => x.AddDed_Type)
                .ThenBy(x => x.AddDed_Item)
                .ToListAsync();

            return new OperationResult(true, data);
        }

        public async Task<OperationResult> GetTotalRows(MonthlySalaryAdditionsDeductionsSummaryReportParam param)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;
            var data = (List<MonthlySalaryAdditionsDeductionsSummaryReportData>)result.Data;
            return new OperationResult(true, data.Count);
        }

        public async Task<OperationResult> Download(MonthlySalaryAdditionsDeductionsSummaryReportParam param)
        {
            var department = string.Empty;

            var type_Seq = new List<string>()
            {
                BasicCodeTypeConstant.Factory,
                BasicCodeTypeConstant.PermissionGroup,
                BasicCodeTypeConstant.AdditionsAndDeductionsItem,
                BasicCodeTypeConstant.AdditionsAndDeductionsType
            };

            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;

            var data = (List<MonthlySalaryAdditionsDeductionsSummaryReportData>)result.Data;

            if (data.Count == 0)
                return new OperationResult(false, "System.Message.NoData");

            var HBC = _repositoryAccessor.HRMS_Basic_Code
                            .FindAll(x => type_Seq.Contains(x.Type_Seq));
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

            if (!string.IsNullOrWhiteSpace(param.Department))
            {
                department = await _repositoryAccessor.HRMS_Org_Department
                    .FindAll(x => x.Department_Code == param.Department, true)
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

            var permissionGroup = BasicCodeLanguage
                .Where(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup
                         && param.Permission_Group.Contains(x.Code))
                .Select(x => x.Code_Name);

            data.ForEach(item =>
            {
                item.AddDed_Type_Title = BasicCodeLanguage
                    .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsType
                                      && x.Code == item.AddDed_Type)?.Code_Name;

                item.AddDed_Item_Title = BasicCodeLanguage
                    .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsItem
                                      && x.Code == item.AddDed_Item)?.Code_Name;
            });

            List<Cell> cells = new()
            {
                new Cell("B2", factory),
                new Cell("D2", param.Year_Month),
                new Cell("F2", param.Kind == "OnJob" ?  "在職 On Job" : "離職 Resigned") ,
                new Cell("H2", string.Join(", ", permissionGroup)),
                new Cell("J2", department),
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
                "Resources\\Template\\SalaryReport\\7_2_7_MonthlySalaryAdditionsDeductionsSummaryReport\\Download.xlsx",
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