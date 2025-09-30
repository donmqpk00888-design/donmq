using System.Data;
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
    public partial class S_7_2_22_MonthlyAdditionsAndDeductionsSummaryReport : BaseServices, I_7_2_22_MonthlyAdditionsAndDeductionsSummaryReport
    {

        public S_7_2_22_MonthlyAdditionsAndDeductionsSummaryReport(DBContext dbContext) : base(dbContext) { }

        public async Task<List<KeyValuePair<string, string>>> GetFactoryList(string lang, List<string> roleList)
        {
            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll();
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == lang.ToLower());
            var result = new List<KeyValuePair<string, string>>();
            var data = HBC.GroupJoin(HBCL,
                x => new { x.Type_Seq, x.Code },
                y => new { y.Type_Seq, y.Code },
                (x, y) => new { hbc = x, hbcl = y })
                .SelectMany(x => x.hbcl.DefaultIfEmpty(),
                (x, y) => new { x.hbc, hbcl = y });
            var authFactories = await Queryt_Factory_AddList(roleList);
            result.AddRange(data
                .Where(x => x.hbc.Type_Seq == BasicCodeTypeConstant.Factory && authFactories.Contains(x.hbc.Code))
                .Select(x => new KeyValuePair<string, string>(x.hbc.Code, $"{x.hbc.Code}-{(x.hbcl != null ? x.hbcl.Code_Name : x.hbc.Code_Name)}"))
                .Distinct().ToList()
            );
            return result;
        }

        public async Task<List<KeyValuePair<string, string>>> GetDropDownList(MonthlyAdditionsAndDeductionsSummaryReport_Param param, List<string> roleList)
        {
            if (string.IsNullOrWhiteSpace(param.Factory))
                return new List<KeyValuePair<string, string>> { };
            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll();
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower());
            var result = new List<KeyValuePair<string, string>>();
            var data = HBC.GroupJoin(HBCL,
                x => new { x.Type_Seq, x.Code },
                y => new { y.Type_Seq, y.Code },
                (x, y) => new { hbc = x, hbcl = y })
                .SelectMany(x => x.hbcl.DefaultIfEmpty(),
                (x, y) => new { x.hbc, hbcl = y });
            var authPermission = await Query_Permission_Group_List(param.Factory);
            result.AddRange(data.Where(x => x.hbc.Type_Seq == BasicCodeTypeConstant.PermissionGroup && authPermission.Contains(x.hbc.Code)).Select(x => new KeyValuePair<string, string>("PE", $"{x.hbc.Code}-{(x.hbcl != null ? x.hbcl.Code_Name : x.hbc.Code_Name)}")).Distinct().ToList());
            var HOD = await Query_Department_List(param.Factory);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Factory == param.Factory && x.Language_Code.ToLower() == param.Lang.ToLower())
                .ToList();
            var dataDept = HOD
                .GroupJoin(HODL,
                    x => new {x.Division, x.Department_Code},
                    y => new {y.Division, y.Department_Code},
                    (x, y) => new { hod = x, hodl = y })
                .SelectMany(x => x.hodl.DefaultIfEmpty(),
                    (x, y) => new { x.hod, hodl = y });
            result.AddRange(dataDept.Select(x => new KeyValuePair<string, string>("DE", $"{x.hod.Department_Code}-{(x.hodl != null ? x.hodl.Name : x.hod.Department_Name)}")).Distinct().ToList());
            return result;
        }
        public async Task<OperationResult> Process(MonthlyAdditionsAndDeductionsSummaryReport_Param param, string userName)
        {
            if (string.IsNullOrWhiteSpace(param.Factory) ||
                string.IsNullOrWhiteSpace(param.Year_Month_Str) ||
                param.Permission_Group.Count == 0 ||
                !DateTime.TryParseExact(param.Year_Month_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime _Year_Month))
                return new OperationResult(false, "InvalidInput");
            param.Year_Month_Date = _Year_Month;
            OperationResult result = param.Function_Type switch
            {
                "search" => await Search(param),
                "excel" => await Excel(param, userName),
                _ => new OperationResult(false, "InvalidFunc")
            };
            return result;
        }
        private async Task<OperationResult> Search(MonthlyAdditionsAndDeductionsSummaryReport_Param param)
        {
            var data = GetData(param);
            return new OperationResult(true, await data.CountAsync());
        }
        private async Task<OperationResult> Excel(MonthlyAdditionsAndDeductionsSummaryReport_Param param, string userName)
        {
            var res = GetData(param);
            if (!res.Any())
                return new OperationResult(true, new { Count = 0 });
            var HBC_Lang = IQuery_Code_Lang(param.Lang);
            var HOD_Lang = IQuery_Department_Lang(param.Factory, param.Lang);
            var Permission_Group = HBC_Lang
                .Where(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup && param.Permission_Group.Contains(x.Code))
                .Select(x => x.Code_Name_Str);
            var Department = HOD_Lang.FirstOrDefault(x => x.Department_Code == param.Department);
            var _data = await res
                .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup),
                    x => x.Permission_Group,
                    y => y.Code,
                    (x, y) => new { data = x, PermissionGroup = y })
                .SelectMany(x => x.PermissionGroup.DefaultIfEmpty(),
                    (x, y) => new { x.data, PermissionGroup = y })
                .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.SalaryCategory),
                    x => x.data.VSortcod,
                    y => y.Code,
                    (x, y) => new { x.data, x.PermissionGroup, SalaryType = y })
                .SelectMany(x => x.SalaryType.DefaultIfEmpty(),
                    (x, y) => new { x.data, x.PermissionGroup, SalaryType = y })
                .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsType),
                    x => x.data.AddDed_Type,
                    y => y.Code,
                    (x, y) => new { x.data, x.PermissionGroup, x.SalaryType, AddDed_Type = y })
                .SelectMany(x => x.AddDed_Type.DefaultIfEmpty(),
                    (x, y) => new { x.data, x.PermissionGroup, x.SalaryType, AddDed_Type = y })
                .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsItem),
                    x => x.data.AddDed_Item,
                    y => y.Code,
                    (x, y) => new { x.data, x.PermissionGroup, x.SalaryType, x.AddDed_Type, AddDed_Item = y })
                .SelectMany(x => x.AddDed_Item.DefaultIfEmpty(),
                    (x, y) => new { x.data, x.PermissionGroup, x.SalaryType, x.AddDed_Type, AddDed_Item = y })
                .GroupBy(x => x.data)
                .Select(x => new MonthlyAdditionsAndDeductionsSummaryReport_Detail
                {
                    Permission_Group = x.Key.Permission_Group,
                    Permission_Group_Name = x.FirstOrDefault(y => y.PermissionGroup.Code != null).PermissionGroup.Code_Name_Str ?? x.Key.Permission_Group,
                    VSortcod = x.Key.VSortcod,
                    VSortcod_Name = x.FirstOrDefault(y => y.SalaryType.Code != null).SalaryType.Code_Name_Str ?? x.Key.VSortcod,
                    AddDed_Type = x.Key.AddDed_Type,
                    AddDed_Type_Name = x.FirstOrDefault(y => y.AddDed_Type.Code != null).AddDed_Type.Code_Name_Str ?? x.Key.AddDed_Type,
                    AddDed_Item = x.Key.AddDed_Item,
                    AddDed_Item_Name = x.FirstOrDefault(y => y.AddDed_Item.Code != null).AddDed_Item.Code_Name_Str ?? x.Key.AddDed_Item,
                    Amount = x.Key.Amount
                }).ToListAsync();
            var data = _data
                .OrderBy(x => x.Permission_Group)
                .ThenBy(x => x.AddDed_Type)
                .ThenBy(x => x.VSortcod)
                .ThenBy(x => x.AddDed_Item)
                .GroupBy(x => x.AddDed_Type)
                .Select(x => new MonthlyAdditionsAndDeductionsSummaryReport_ExcelData
                {
                    Sub_Total = x.Sum(y => y.Amount),
                    Detail = x.ToList()
                }).ToList();
            List<Cell> dataCells = new()
            {
                new Cell("B2", param.Factory),
                new Cell("D2", param.Year_Month_Date.ToString("yyyy/MM")),
                new Cell("F2", string.Join(" / ", Permission_Group)),
                new Cell("H2", Department?.Department_Code_Name ?? param.Department),
                new Cell("J2", param.Employee_ID),
                new Cell("B3", userName),
                new Cell("D3", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))
            };
            var initRow = 7;
            foreach (var groupValue in data)
            {
                groupValue.Detail.ForEach(item =>
                {
                    dataCells.Add(new Cell("A" + initRow, item.Permission_Group_Name));
                    dataCells.Add(new Cell("B" + initRow, item.AddDed_Type_Name));
                    dataCells.Add(new Cell("C" + initRow, item.VSortcod_Name));
                    dataCells.Add(new Cell("D" + initRow, item.AddDed_Item_Name));
                    dataCells.Add(new Cell("E" + initRow, item.Amount));
                    initRow++;
                });
                dataCells.Add(new Cell("D" + initRow, "小計 Sub Total"));
                dataCells.Add(new Cell("E" + initRow, groupValue.Sub_Total));
                initRow++;
            }
            initRow++;
            dataCells.Add(new Cell("A" + initRow, "核決:"));
            dataCells.Add(new Cell("D" + initRow, "審核:"));
            dataCells.Add(new Cell("G" + initRow, "製表:"));
            initRow++;
            dataCells.Add(new Cell("A" + initRow, "Approved by:"));
            dataCells.Add(new Cell("D" + initRow, "Checked by:"));
            dataCells.Add(new Cell("G" + initRow, "Applicant:"));
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataCells,
                "Resources\\Template\\SalaryReport\\7_2_22_MonthlyAdditionsAndDeductionsSummaryReport\\Download.xlsx",
                new ConfigDownload(false));
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, new { _data.Count, excelResult.Result });
        }
        private IQueryable<MonthlyAdditionsAndDeductionsSummaryReport_Detail> GetData(MonthlyAdditionsAndDeductionsSummaryReport_Param param)
        {
            var predHSM = PredicateBuilder.New<HRMS_Sal_Monthly>(x =>
               x.Factory == param.Factory &&
               param.Permission_Group.Contains(x.Permission_Group) &&
               x.Sal_Month.Date == param.Year_Month_Date.Date
            );
            var predHSRM = PredicateBuilder.New<HRMS_Sal_Resign_Monthly>(x =>
               x.Factory == param.Factory &&
               param.Permission_Group.Contains(x.Permission_Group) &&
               x.Sal_Month.Date == param.Year_Month_Date.Date
            );

            if (!string.IsNullOrWhiteSpace(param.Department))
            {
                predHSM = predHSM.And(x => x.Department == param.Department);
                predHSRM = predHSRM.And(x => x.Department == param.Department);
            }
            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
            {
                predHSM = predHSM.And(x => x.Employee_ID.Contains(param.Employee_ID));
                predHSRM = predHSRM.And(x => x.Employee_ID.Contains(param.Employee_ID));
            }
            var HSM = _repositoryAccessor.HRMS_Sal_Monthly.FindAll(predHSM)
                .Join(_repositoryAccessor.HRMS_Sal_MasterBackup.FindAll(x => x.Factory == param.Factory),
                    x => new { x.Sal_Month.Date, x.Employee_ID },
                    y => new { y.Sal_Month.Date, y.Employee_ID },
                    (x, y) => new { HSM = x, HSM_Backup = y })
                .Select(x => new
                {
                    x.HSM.USER_GUID,
                    x.HSM.Factory,
                    x.HSM.Sal_Month,
                    x.HSM.Employee_ID,
                    x.HSM.Department,
                    x.HSM.Permission_Group,
                    x.HSM_Backup.Position_Title
                });
            var HSRM = _repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(predHSRM)
                .Join(_repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == param.Factory),
                    x => x.Employee_ID,
                    y => y.Employee_ID,
                    (x, y) => new { HSM = x, HEP = y })
                .Select(x => new
                {
                    x.HSM.USER_GUID,
                    x.HSM.Factory,
                    x.HSM.Sal_Month,
                    x.HSM.Employee_ID,
                    x.HSM.Department,
                    x.HSM.Permission_Group,
                    x.HEP.Position_Title
                });
            var Employee_Temp = HSM.Union(HSRM);
            var result = Employee_Temp
                .Join(_repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.FindAll(x => x.Factory == param.Factory),
                    x => new { x.Sal_Month.Date, x.Employee_ID },
                    y => new { y.Sal_Month.Date, y.Employee_ID },
                    (x, y) => new { Employee_Temp = x, HSAM = y })
                .GroupJoin(_repositoryAccessor.HRMS_Sal_FinCategory.FindAll(x => x.Factory == param.Factory && (x.Kind == "1" || x.Kind == null)),
                    x => new { x.Employee_Temp.Department, x.Employee_Temp.Position_Title },
                    y => new { y.Department, Position_Title = y.Code },
                    (x, y) => new { x.Employee_Temp, x.HSAM, HSF = y })
                .SelectMany(x => x.HSF.DefaultIfEmpty(),
                    (x, y) => new { x.Employee_Temp, x.HSAM, VSortcod = y != null ? y.Sortcod : "1" })
                .GroupBy(x => new { x.Employee_Temp.Permission_Group, x.HSAM.AddDed_Type, x.HSAM.AddDed_Item, x.VSortcod })
                .Select(x => new MonthlyAdditionsAndDeductionsSummaryReport_Detail
                {
                    Permission_Group = x.Key.Permission_Group,
                    AddDed_Type = x.Key.AddDed_Type,
                    AddDed_Item = x.Key.AddDed_Item,
                    VSortcod = x.Key.VSortcod,
                    Amount = x.Sum(y => y.HSAM.Amount),
                });
            return result;
        }
    }
}
