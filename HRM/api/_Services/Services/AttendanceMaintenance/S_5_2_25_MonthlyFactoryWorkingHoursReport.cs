using System.Globalization;
using AgileObjects.AgileMapper;
using AgileObjects.AgileMapper.Extensions;
using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_2_25_MonthlyFactoryWorkingHoursReport : BaseServices, I_5_2_25_MonthlyFactoryWorkingHoursReport
    {
        public S_5_2_25_MonthlyFactoryWorkingHoursReport(DBContext dbContext) : base(dbContext)
        {
        }

        private async Task<List<MonthlyFactoryWorkingHoursReportDto>> GetData(MonthlyFactoryWorkingHoursReportParam param)
        {
            var start_Date = DateTime.Parse(param.Start_Date);
            var end_Date = DateTime.Parse(param.End_Date);

            var leave_Code = new List<string>() { "J0", "J1", "J2", "J5" };

            var proc_cur1 = await _repositoryAccessor.HRMS_Emp_Personal
                .FindAll(x => x.Factory == param.Factory
                        && (!x.Resign_Date.HasValue || (x.Resign_Date.HasValue && x.Resign_Date.Value.Date >= end_Date.Date))
                        && param.Permission_Group.Contains(x.Permission_Group)
                        // && x.USER_GUID == "8490efa3-d915-4636-b2e3-59fa83d36a44"
                ).Project().To<MonthlyFactoryWorkingHoursReportPT>().ToListAsync();

            var HACR = _repositoryAccessor.HRMS_Att_Change_Record
                .FindAll(x => x.Factory == param.Factory
                        && x.Att_Date >= start_Date
                        && x.Att_Date <= end_Date)
                .OrderBy(x => x.Att_Date)
                .ToList();

            var HAOM = _repositoryAccessor.HRMS_Att_Overtime_Maintain
                .FindAll(x => x.Factory == param.Factory
                        && x.Overtime_Date >= start_Date
                        && x.Overtime_Date <= end_Date)
                .ToList();

            var HALM = _repositoryAccessor.HRMS_Att_Leave_Maintain
                .FindAll(x => x.Factory == param.Factory
                        && x.Leave_Date >= start_Date
                        && x.Leave_Date <= end_Date)
                .ToList();

            var HAWS = _repositoryAccessor.HRMS_Att_Work_Shift
                .FindAll(x => x.Factory == param.Factory)
                .ToList();

            var HODS = _repositoryAccessor.HRMS_Org_Direct_Section
                .FindAll(x => x.Factory == param.Factory)
                .AsEnumerable()
                .ToList();

            var HODD = _repositoryAccessor.HRMS_Org_Direct_Department
                .FindAll(x => x.Factory == param.Factory)
                .ToList();

            var basicCode = _repositoryAccessor.HRMS_Basic_Code.FindAll(true).ToHashSet();
            var basicLanguage = _repositoryAccessor.HRMS_Basic_Code_Language
                .FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true).ToHashSet();

            var codeLang = basicCode
                .GroupJoin(basicLanguage,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { Code = x, Language = y })
                .SelectMany(x => x.Language.DefaultIfEmpty(),
                    (x, y) => new { x.Code, Language = y })
                .Select(x => new
                {
                    x.Code.Code,
                    x.Code.Type_Seq,
                    Code_Name = x.Language != null ? x.Language.Code_Name : x.Code.Code_Name
                })
                .Distinct().ToList();

            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll(true).ToList();
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true).ToList();

            var Departments = HOD
                .GroupJoin(HODL,
                    HOD => new { HOD.Division, HOD.Factory, HOD.Department_Code },
                    HODL => new { HODL.Division, HODL.Factory, HODL.Department_Code },
                    (HOD, HODL) => new { HOD, HODL })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, HODL) => new { x.HOD, HODL })
                .Select(x => new
                {
                    x.HOD.Factory,
                    x.HOD.Division,
                    x.HOD.Department_Code,
                    Department_Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name
                }).ToList();

            var result = new List<MonthlyFactoryWorkingHoursReportDto>();

            foreach (var PT in proc_cur1)
            {
                var dat_cur = HACR
                    .Where(x => x.Employee_ID == PT.Employee_ID)
                    .Select(x => new { x.Att_Date, x.Work_Shift_Type })
                    .Distinct()
                    .ToList();
                var w_dat1 = 0m;
                foreach (var w_dat in dat_cur)
                {
                    // 2. Weekday overtime hours
                    var wk_ovhrd = HAOM.Where(x =>
                        x.Employee_ID == PT.Employee_ID &&
                        x.Overtime_Date.Date == w_dat.Att_Date.Date &&
                        x.Overtime_Hours != 0 &&
                        x.Holiday == "XXX"
                    )?.Sum(x => x.Overtime_Hours) ?? 0m;

                    // 3. Night shift overtime hours (excluding national holidays)
                    var wk_ovhrnt = HAOM.Where(x =>
                        x.Employee_ID == PT.Employee_ID &&
                        x.Overtime_Date.Date == w_dat.Att_Date.Date &&
                        x.Holiday != "C00"
                    )?.Sum(x => x.Night_Overtime_Hours) ?? 0m;
                    PT.ovhrd = PT.ovhrd + wk_ovhrd + wk_ovhrnt;

                    // 4. Normal working hours
                    w_dat1 = HALM.Where(x =>
                        x.Employee_ID == PT.Employee_ID &&
                        x.Leave_Date.Date == w_dat.Att_Date.Date
                    )?.Sum(x => x.Days) ?? 0m;
                    int weekDay = (int)w_dat.Att_Date.DayOfWeek;
                    var HRMS_Att_Work_Shift = HAWS.FirstOrDefault(ws =>
                        ws.Work_Shift_Type == w_dat.Work_Shift_Type &&
                        ws.Week == weekDay.ToString()
                    );
                    decimal wk_nor_hr = HRMS_Att_Work_Shift.Work_Hours - (w_dat1 * 8);
                    PT.nor_hr += wk_nor_hr;

                    // 5. Business trip hours
                    w_dat1 = HALM.Where(x =>
                            x.Employee_ID == PT.Employee_ID &&
                            x.Leave_Date.Date == w_dat.Att_Date.Date &&
                            x.Leave_code == "M0"
                        )?.Sum(x => x.Days) ?? 0m;
                    decimal wk_publ = w_dat1 > 0 ? w_dat1 * 8 : w_dat1;
                    PT.publ += wk_publ;
                }
                // 6. Organizational Function
                var _HODS = HODS
                    .Where(x =>
                        x.Division == PT.Division &&
                        x.Work_Type_Code == PT.Work_Type &&
                        DateTime.ParseExact(x.Effective_Date, "yyyy/MM", new CultureInfo("en-US"), DateTimeStyles.None).Date <= end_Date.Date)
                    .OrderByDescending(x => x.Effective_Date)
                    .FirstOrDefault();
                string FUN = _HODS != null ? _HODS.Section_Code : string.Empty;
                string FUN_Name = FUN;
                if (!string.IsNullOrWhiteSpace(FUN))
                {
                    var fun_CodeLang = codeLang.FirstOrDefault(y => y.Type_Seq == BasicCodeTypeConstant.Fucntion && y.Code == FUN);
                    if (fun_CodeLang != null)
                        FUN_Name = $"{FUN} - {fun_CodeLang.Code_Name}";
                }

                // 7. Department Name
                var _HODD = HODD.Where(y => y.Division == PT.Division && y.Factory == PT.Factory && y.Line_Code == PT.Department);
                var DEPT = string.Join(",", _HODD.Select(x => x.Department_Code));

                // 8. Working hours of waiting for materials
                w_dat1 = HALM.Where(x =>
                    x.Employee_ID == PT.Employee_ID &&
                    leave_Code.Contains(x.Leave_code)
                )?.Sum(x => x.Days) ?? 0m;
                if (w_dat1 > 0)
                    PT.stp = w_dat1 * 8;

                // 9. Direct/Indirect Department
                var Dept_kind = string.Join(", ", _HODD.Select(y => y.Direct_Department_Attribute).Distinct());

                // 10. Direct/indirect labor
                var Flag = _HODS != null ? _HODS.Direct_Section : string.Empty;

                // Others
                var Department = Departments.FirstOrDefault(y => y.Factory == PT.Factory && y.Division == PT.Division && y.Department_Code == PT.Department);
                var Dept_Name = string.Join(", ", _HODD.Select(y => $"{y.Department_Code} - {Department?.Department_Name}").Distinct());

                var dto = new MonthlyFactoryWorkingHoursReportDto
                {
                    Dept_Kind = Dept_kind,
                    Flag = Flag,
                    Employee_ID = PT.Employee_ID,
                    Department = PT.Department,
                    Local_Full_Name = PT.Local_Full_Name,
                    Position_Title = PT.Position_Title,
                    Work_Type = PT.Work_Type,
                    Onboard_Date = PT.Onboard_Date,
                    Resign_Date = PT.Resign_Date,
                    Fun = FUN,
                    Dept = DEPT,

                    report_hr = PT.nor_hr + PT.ovhrd,
                    actual_hr = PT.nor_hr + PT.ovhrd + PT.publ,
                    nor_hr = PT.nor_hr,
                    ovhrd = PT.ovhrd,
                    publ = PT.publ,
                    stp = PT.stp,

                    Department_Name = $"{PT.Department} - {Department?.Department_Name}",
                    Position_Title_Name = $"{PT.Position_Title} - {codeLang.FirstOrDefault(y => y.Type_Seq == BasicCodeTypeConstant.JobTitle && y.Code == PT.Position_Title)?.Code_Name}",
                    Work_Type_Name = $"{PT.Work_Type} - {codeLang.FirstOrDefault(y => y.Type_Seq == BasicCodeTypeConstant.WorkType && y.Code == PT.Work_Type)?.Code_Name}",
                    Fun_Name = FUN_Name,
                    Dept_Name = Dept_Name,
                };
                result.Add(dto);
            }
            return result;
        }

        public async Task<int> GetTotalRows(MonthlyFactoryWorkingHoursReportParam param)
        {
            var result = await GetData(param);
            return result.Count;
        }

        public async Task<OperationResult> Download(MonthlyFactoryWorkingHoursReportParam param)
        {
            var data = await GetData(param);

            if (data.Count == 0)
                return new OperationResult(false, "System.Message.NoData");

            DateTime start_Date = DateTime.Parse(param.Start_Date);
            DateTime end_Date = DateTime.Parse(param.End_Date);

            var HBC = _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup
                           && param.Permission_Group.Contains(x.Code));
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language
                .FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true);

            var Permission_Group = await HBC
                .GroupJoin(HBCL,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => $"{x.HBC.Code}-{(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}")
                .ToListAsync();

            List<Cell> cells = new()
            {
                new Cell("B2", param.Factory),
                new Cell("B3", param.UserName),
                new Cell("E2", start_Date.ToString("yyyy/MM/dd")),
                new Cell("E3", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
                new Cell("G2", end_Date.ToString("yyyy/MM/dd")),
                new Cell("J2", string.Join(", ", Permission_Group)),
            };

            List<Table> tables = new() { new("result", data) };
            ConfigDownload configDownload = new(false);
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                tables,
                cells,
                "Resources\\Template\\AttendanceMaintenance\\5_2_25_MonthlyFactoryWorkingHoursReport\\Download.xlsx",
                configDownload
            );
            if (excelResult.IsSuccess)
                return new OperationResult(excelResult.IsSuccess, new { TotalRows = data.Count, Excel = excelResult.Result });
            else
                return new OperationResult(excelResult.IsSuccess, excelResult.Error);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string language, List<string> roleList)
        {
            var factoriesByAccount = await Queryt_Factory_AddList(roleList);
            var factories = await GetDataBasicCode(BasicCodeTypeConstant.Factory, language);

            return factories.IntersectBy(factoriesByAccount, x => x.Key).ToList();
        }

        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language)
        {
            return await Query_BasicCode_PermissionGroup(factory, language);
        }
    }
}