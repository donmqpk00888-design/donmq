using System.Drawing;
using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_2_16_IndividualMonthlyWorkingHoursReport : BaseServices, I_5_2_16_IndividualMonthlyWorkingHoursReport
    {
        public S_5_2_16_IndividualMonthlyWorkingHoursReport(DBContext dbContext) : base(dbContext)
        {

        }
        public async Task<OperationResult> GetData(IndividualMonthlyWorkingHoursReportParam param, string userName)
        {
            var results = new IndividualMonthlyWorkingHoursReportDto
            {
                param = param,
                print_By = userName,
                DataExcels = new List<ExcelColumn_5_2_16>()
            };
            var permissionGroups = param.permission_Group.Split(",").ToList();
            DateTime? firstDate = null;
            DateTime? lastDate = null;
            var time = Convert.ToDateTime(param.yearMonth);
            firstDate = new DateTime(time.Year, time.Month, 1);
            lastDate = new DateTime(time.Year, time.Month, DateTime.DaysInMonth(time.Year, time.Month));
            var pred_Peronal = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.factory
                                                                            && permissionGroups.Contains(x.Permission_Group)
                                                                            && (x.Resign_Date > firstDate || !x.Resign_Date.HasValue)
                                                                            && x.Onboard_Date <= lastDate
                                                                        );
            var dataPeronals = _repositoryAccessor.HRMS_Emp_Personal.FindAll(pred_Peronal, true).OrderBy(x => x.Department);
            if (!await dataPeronals.AnyAsync())
                return new OperationResult(false, results);
            var dataExcel = new List<ExcelColumn_5_2_16>();
            var dataDepartments = _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == param.factory, true)
                    .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.language.ToLower(), true),
                        HOD => new { HOD.Division, HOD.Factory, HOD.Department_Code },
                        HODL => new { HODL.Division, HODL.Factory, HODL.Department_Code },
                        (HOD, HODL) => new { HOD, HODL })
                    .SelectMany(x => x.HODL.DefaultIfEmpty(),
                        (prev, HODL) => new { prev.HOD, HODL })
                    .Select(x => new
                    {
                        x.HOD.Department_Code,
                        Department_Name = $"{(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}"
                    }).ToHashSet();
            var HAWS = _repositoryAccessor.HRMS_Att_Work_Shift.FindAll(x => x.Factory == param.factory).ToHashSet();
            var HAM = _repositoryAccessor.HRMS_Att_Monthly.FindAll(x => x.Factory == param.factory && x.Att_Month == firstDate).ToHashSet();
            var HALM = _repositoryAccessor.HRMS_Att_Leave_Maintain.FindAll(x => x.Factory == param.factory && x.Leave_code != "D0").ToHashSet();
            var HACR = _repositoryAccessor.HRMS_Att_Change_Record.FindAll(x => x.Factory == param.factory
                                                                            && x.Att_Date >= firstDate
                                                                            && x.Att_Date <= lastDate).ToHashSet();
            var HAOM = _repositoryAccessor.HRMS_Att_Overtime_Maintain.FindAll(x => x.Factory == param.factory
                                                                                && x.Overtime_Date >= firstDate
                                                                                && x.Overtime_Date <= lastDate
                                                                                && x.Holiday != "C05").ToHashSet();
            foreach (var personal in dataPeronals)
            {
                var normal_Working_Hours = await CalculatorNormal_Working_Hours(personal, param.factory, firstDate.Value, lastDate.Value);
                var overtime_Hour = CalculatorOvertime_Hour(HAOM, personal);

                var data = new ExcelColumn_5_2_16
                {
                    department = dataDepartments.FirstOrDefault(x => x.Department_Code == personal.Department)?.Department_Code ?? personal.Department,
                    department_Name = dataDepartments.FirstOrDefault(x => x.Department_Code == personal.Department)?.Department_Name,
                    Employee_ID = personal.Employee_ID,
                    Local_Full_Name = personal.Local_Full_Name,
                    normal_Working_Hours = normal_Working_Hours,
                    overtime_Hour = overtime_Hour,
                    total_Working_Hours = (normal_Working_Hours ?? 0) + (overtime_Hour ?? 0),
                };
                dataExcel.Add(data);
            }
            results.DataExcels = dataExcel;
            return new OperationResult(true, results);
        }

        private static decimal? CalculatorOvertime_Hour(HashSet<HRMS_Att_Overtime_Maintain> HAOM, HRMS_Emp_Personal personal)
        {
            decimal? overtime_Hour = 0;
            var data = HAOM.Where(x => x.Employee_ID == personal.Employee_ID)
                            .GroupBy(x => new { x.Factory, x.Employee_ID })
                            .Select(x => new
                            {
                                x.Key.Factory,
                                x.Key.Employee_ID,
                                sumOvertime_Hours = x.Sum(p => p.Overtime_Hours),
                                sumNight_Overtime_Hours = x.Sum(p => p.Night_Overtime_Hours),
                                sumTraining_Hours = x.Sum(p => p.Training_Hours)
                            }).FirstOrDefault();
            overtime_Hour = (data?.sumOvertime_Hours ?? 0) + (data?.sumNight_Overtime_Hours ?? 0) + (data?.sumTraining_Hours ?? 0);
            return overtime_Hour ?? 0;
        }


        private async Task<decimal?> CalculatorNormal_Working_Hours(HRMS_Emp_Personal employee, string factory, DateTime firstDateOfMonth, DateTime lastDateOfMonth)
        {
            decimal? normal_Working_Hours;
            if (!employee.Swipe_Card_Option)
                normal_Working_Hours = await TotalHoursNotSwipeCard(factory, employee.Employee_ID, firstDateOfMonth, employee.Work_Shift_Type);
            else normal_Working_Hours = await TotalHoursHasSwipeCard(factory, employee.Employee_ID, firstDateOfMonth, lastDateOfMonth);

            return normal_Working_Hours;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string language, string account)
        {
            var factoryAccounts = await Queryt_Factory_AddList(account);
            var factorys = await GetDataBasicCode(BasicCodeTypeConstant.Factory, language);
            return factorys.Where(x => factoryAccounts.Contains(x.Key)).ToList();
        }

        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language)
        {
            var permissionGroups = await Query_Permission_List(factory);
            List<string> permissions = permissionGroups.Select(x => x.Permission_Group).ToList();
            var dataPermissionGroups = await GetDataBasicCode(BasicCodeTypeConstant.PermissionGroup, language);
            var results = dataPermissionGroups.Where(x => permissions.Contains(x.Key)).ToList();
            return results;
        }

        public async Task<OperationResult> Excel(IndividualMonthlyWorkingHoursReportParam param, string userName)
        {
            var data = await GetData(param, userName);
            var export = data.Data as IndividualMonthlyWorkingHoursReportDto;
            if (!export.DataExcels.Any())
                return new OperationResult(false, "System.Message.NoData");
            var results = export.DataExcels;
            var permissions = await GetListPermissionGroup(param.factory, param.language);
            var permissionParams = param.permission_Group.Split(",");
            var rs = new List<string>();
            permissionParams.ForEach(per =>
            {
                rs.Add(permissions.FirstOrDefault(x => x.Key == per).Value);
            });
            List<Table> tables = new()
            {
                new Table("result", results)
            };
            int totalIndex = results.Count + 7;
            Aspose.Cells.Style style = new Aspose.Cells.CellsFactory().CreateStyle();
            style.Pattern = Aspose.Cells.BackgroundType.Solid;
            style.ForegroundColor = Color.FromArgb(221, 235, 247);
            style = AsposeUtility.SetAllBorders(style);
            List<Cell> dataCells = new()
            {
                new Cell("B2", param.factory),
                new Cell("D2", Convert.ToDateTime(param.yearMonth).ToString("yyyy/MM")),
                new Cell("F2", string.Join(" / ", rs)),
                new Cell("B3", export.print_By),
                new Cell("D3", export.print_Date),
                new Cell("D" + totalIndex, "Total: ", style),
                new Cell("E" + totalIndex, results.Sum(x => x.normal_Working_Hours), style),
                new Cell("F" + totalIndex, results.Sum(x => x.overtime_Hour), style),
                new Cell("G" + totalIndex, results.Sum(x => x.total_Working_Hours), style)
            };
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                tables, dataCells, 
                "Resources\\Template\\AttendanceMaintenance\\5_2_16_IndividualMonthlyWorkingHoursReport\\Download.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }
    }
}