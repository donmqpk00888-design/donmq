using System.Globalization;
using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_2_4_EmployeeAttendanceDataSheet : BaseServices, I_5_2_4_EmployeeAttendanceDataSheet
    {
        public S_5_2_4_EmployeeAttendanceDataSheet(DBContext dbContext) : base(dbContext)
        {
        }

        public async Task<OperationResult> DownloadFileExcel(EmployeeAttendanceDataSheetParam param, string userName)
        {
            var data = await GetData(param);
            if (!data.Any())
                return new OperationResult(false, "System.Message.NoData");

            List<Cell> dataCells = new()
            {
                new Cell("B2", param.Factory),
                new Cell("E2", $"{param.Att_Date_From} - {param.Att_Date_To}" ),
                new Cell("H2", param.Department),
                new Cell("K2", param.Work_Shift_Type),
                new Cell("N2", param.Employee_ID),
                new Cell("B4", userName),
                new Cell("D4", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))
            };
            var dataTables = new List<Table>() { new("result", data) };

            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataTables, dataCells, 
                "Resources\\Template\\AttendanceMaintenance\\5_2_4_EmployeeAttendanceDataSheet\\Download.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, new { excelResult.Result, data.Count });
        }

        public async Task<List<EmployeeAttendanceDataSheetDto>> GetData(EmployeeAttendanceDataSheetParam param)
        {
            if (string.IsNullOrWhiteSpace(param.Factory)
            || string.IsNullOrWhiteSpace(param.Att_Date_From)
            || string.IsNullOrWhiteSpace(param.Att_Date_To)
            || !DateTime.TryParseExact(param.Att_Date_From, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime Att_Date_From)
            || !DateTime.TryParseExact(param.Att_Date_To, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime Att_Date_To))
                return null;
            var preHACR = PredicateBuilder.New<HRMS_Att_Change_Record>(x => x.Factory == param.Factory && x.Att_Date.Date >= Att_Date_From.Date && x.Att_Date.Date <= Att_Date_To.Date);
           
            var preHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x => !x.Resign_Date.HasValue || (x.Resign_Date.HasValue && x.Resign_Date.Value.Date >= Att_Date_From.Date));
            if (!string.IsNullOrWhiteSpace(param.Work_Shift_Type))
                preHACR.And(x => x.Work_Shift_Type == param.Work_Shift_Type);
            if (!string.IsNullOrWhiteSpace(param.Department))
                preHACR.And(x => x.Department == param.Department);
            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                preHEP.And(x => x.Employee_ID == param.Employee_ID);
            var HEP = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(preHEP).ToListAsync();
            var HACR = _repositoryAccessor.HRMS_Att_Change_Record.FindAll(preHACR).ToList();
            List<string> selectedEmployee = HACR.Select(x => x.Employee_ID).ToList();
            List<string> selectedWorkShift = HACR.Select(x => x.Work_Shift_Type).ToList();
            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll(true).ToList();
            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.IsActive).ToList();
            var HAWS = _repositoryAccessor.HRMS_Att_Work_Shift.FindAll(x => x.Factory == param.Factory && selectedWorkShift.Contains(x.Work_Shift_Type)).ToList();
            var HALM = _repositoryAccessor.HRMS_Att_Leave_Maintain.FindAll(x => x.Factory == param.Factory && x.Leave_code != "D0" && selectedEmployee.Contains(x.Employee_ID));
            var HAOM = _repositoryAccessor.HRMS_Att_Overtime_Maintain.FindAll(x => x.Factory == param.Factory && selectedEmployee.Contains(x.Employee_ID));
            var depLang = await Query_Department_Lang_List(param.language);
            var dataWordShiftType = await GetDataBasicCode(BasicCodeTypeConstant.WorkShiftType, param.language, true);
            var dataLeaveCode = await GetDataBasicCode(BasicCodeTypeConstant.Leave, param.language, true);
            List<string> allowEmpStatus = new() { "A", "S" };
            var baseData = HEP
                        .GroupJoin(HACR,
                            x => x.USER_GUID,
                            y => y.USER_GUID,
                            (x, y) => new { HEP = x, HACR = y })
                        .SelectMany(x => x.HACR.DefaultIfEmpty(),
                            (x, y) => new { x.HEP, HACR = y }).ToList();
            var data = baseData.FindAll(x => x.HACR != null)
                        .GroupJoin(HOD,
                            x => new { x.HEP.Division, x.HEP.Factory, Department_Code = x.HEP.Department },
                            y => new { y.Division, y.Factory, y.Department_Code },
                            (x, y) => new { x.HEP, x.HACR, HOD = y })
                        .SelectMany(x => x.HOD.DefaultIfEmpty(),
                            (x, y) => new { x.HEP, x.HACR, HOD = y })
                        .GroupJoin(HOD,
                             x => new { Division = x.HEP.Assigned_Division, Factory = x.HEP.Assigned_Factory, Department_Code = x.HEP.Assigned_Department },
                            y => new { y.Division, y.Factory, y.Department_Code },
                            (x, y) => new { x.HEP, x.HACR, x.HOD, HOD_assigned = y })
                        .SelectMany(x => x.HOD_assigned.DefaultIfEmpty(),
                            (x, y) => new { x.HEP, x.HACR, x.HOD, HOD_assigned = y })
                        .GroupJoin(HBC.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.WorkShiftType),
                            x => x.HACR.Work_Shift_Type,
                            y => y.Code,
                            (x, y) => new { x.HEP, x.HACR, x.HOD, x.HOD_assigned, HBC_workshift = y })
                        .SelectMany(x => x.HBC_workshift.DefaultIfEmpty(),
                            (x, y) => new { x.HEP, x.HACR, x.HOD, x.HOD_assigned, HBC_workshift = y })
                        .GroupJoin(HBC.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Leave),
                            x => x.HACR.Leave_Code,
                            y => y.Code,
                            (x, y) => new { x.HEP, x.HACR, x.HOD, x.HOD_assigned, x.HBC_workshift, HBC_leave = y })
                        .SelectMany(x => x.HBC_leave.DefaultIfEmpty(),
                            (x, y) => new { x.HEP, x.HACR, x.HOD, x.HOD_assigned, x.HBC_workshift, HBC_leave = y })
                        .GroupJoin(HAWS,
                            x => new { x.HACR.Work_Shift_Type, Week = ((int)x.HACR?.Att_Date.DayOfWeek).ToString() },
                            y => new { y.Work_Shift_Type, y.Week },
                            (x, y) => new { x.HEP, x.HACR, x.HOD, x.HOD_assigned, x.HBC_workshift, x.HBC_leave, HAWS_F = y })
                        .SelectMany(x => x.HAWS_F.DefaultIfEmpty(),
                            (x, y) => new { x.HEP, x.HACR, x.HOD, x.HOD_assigned, x.HBC_workshift, x.HBC_leave, HAWS_F = y })
                        .GroupJoin(HAWS.FindAll(x => x.Week == "1"),
                            x => x.HACR.Work_Shift_Type,
                            y => y.Work_Shift_Type,
                            (x, y) => new { x.HEP, x.HACR, x.HOD, x.HOD_assigned, x.HBC_workshift, x.HBC_leave, x.HAWS_F, HAWS_H = y })
                        .SelectMany(x => x.HAWS_H.DefaultIfEmpty(),
                            (x, y) => new { x.HEP, x.HACR, x.HOD, x.HOD_assigned, x.HBC_workshift, x.HBC_leave, x.HAWS_F, HAWS_H = y })
                        .GroupJoin(HALM,
                            x => new { x.HACR.Employee_ID, x.HACR.Att_Date.Date },
                            y => new { y.Employee_ID, y.Leave_Date.Date },
                            (x, y) => new { x.HEP, x.HACR, x.HOD, x.HOD_assigned, x.HBC_workshift, x.HBC_leave, x.HAWS_F, x.HAWS_H, HALM = y })
                        .SelectMany(x => x.HALM.DefaultIfEmpty(),
                            (x, y) => new { x.HEP, x.HACR, x.HOD, x.HOD_assigned, x.HBC_workshift, x.HBC_leave, x.HAWS_F, x.HAWS_H, HALM = y })
                        .GroupJoin(HAOM,
                            x => new { x.HACR.Employee_ID, x.HACR.Att_Date.Date },
                            y => new { y.Employee_ID, y.Overtime_Date.Date },
                            (x, y) => new { x.HEP, x.HACR, x.HOD, x.HOD_assigned, x.HBC_workshift, x.HBC_leave, x.HAWS_F, x.HAWS_H, x.HALM, HAOM = y })
                        .SelectMany(x => x.HAOM.DefaultIfEmpty(),
                            (x, y) => new { x.HEP, x.HACR, x.HOD, x.HOD_assigned, x.HBC_workshift, x.HBC_leave, x.HAWS_F, x.HAWS_H, x.HALM, HAOM = y })
                        .ToList();
            var result = data.Select(x =>
            {
                var dept = !string.IsNullOrWhiteSpace(x.HEP.Employment_Status) && allowEmpStatus.Contains(x.HEP.Employment_Status)
                 ? x.HOD_assigned != null
                    ? depLang.FirstOrDefault(y => y.Division == x.HOD_assigned.Division && y.Factory == x.HOD_assigned.Factory && y.Department_Code == x.HOD_assigned.Department_Code)
                    : null
                 : x.HOD != null
                    ? depLang.FirstOrDefault(y => y.Division == x.HOD.Division && y.Factory == x.HOD.Factory && y.Department_Code == x.HOD.Department_Code)
                    : null;
                return new EmployeeAttendanceDataSheetDto
                {
                    Department = dept?.Department_Code,
                    Department_Name = dept?.Department_Name,
                    Att_Date = x.HACR.Att_Date,
                    Employee_ID = x.HACR.Employee_ID,
                    LocalFullName = x.HEP.Local_Full_Name,
                    Work_Shift_Type = x.HACR.Work_Shift_Type,
                    Work_Shift_Type_Name = x.HACR.Work_Shift_Type + " - " + dataWordShiftType.FirstOrDefault(d => d.Key == x.HACR.Work_Shift_Type).Value,
                    Attendance = x.HACR.Leave_Code,
                    Attendance_Name = x.HACR.Leave_Code + " - " + dataLeaveCode.FirstOrDefault(d => d.Key == x.HACR.Leave_Code).Value,
                    Clock_In = x.HACR.Clock_In,
                    Clock_Out = x.HACR.Clock_Out,
                    Overtime_ClockIn = x.HACR.Overtime_ClockIn,
                    Overtime_ClockOut = x.HACR.Overtime_ClockOut,
                    NormalOvertime = x.HAOM != null && x.HAOM.Holiday == "XXX"
                        ? x.HAOM.Overtime_Hours
                        : null,
                    TrainingOvertime = x.HAOM != null && x.HAOM.Holiday == "XXX"
                        ? x.HAOM.Training_Hours
                        : null,
                    HolidayOvertime = x.HAOM != null && x.HAOM.Holiday == "C05"
                        ? x.HAOM.Overtime_Hours
                        : null,
                    Night = x.HAOM?.Night_Hours,
                    NightOvertime = x.HAOM?.Night_Overtime_Hours,
                    Total = x.HAOM != null ? x.HAOM.Overtime_Hours + x.HAOM.Training_Hours + x.HAOM.Overtime_Hours : null,
                    DelayHour = x.HACR.Leave_Code == "L0" && x.HAWS_F != null
                        ? (decimal.Parse(x.HACR.Clock_In[..2]) * 60 + decimal.Parse(x.HACR.Clock_In[2..]) - (decimal.Parse(x.HAWS_F.Clock_In[..2]) * 60 + decimal.Parse(x.HAWS_F.Clock_In[2..]))) / 60
                        : null,
                    WorkHour = x.HACR.Holiday == "XXX"
                        ? x.HEP.Swipe_Card_Option
                            ? x.HALM != null
                                ? x.HAWS_F != null
                                    ? x.HAWS_F.Work_Hours - x.HALM.Days * x.HAWS_F.Work_Hours
                                    : null
                                : x.HAWS_F?.Work_Hours
                            : 0
                        : null
                };
            }).OrderBy(x => x.Employee_ID).ThenBy(x => x.Att_Date).Distinct().ToList();
            return result;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
        {
            var pred = PredicateBuilder.New<HRMS_Org_Department>(true);
            var predCom = PredicateBuilder.New<HRMS_Basic_Factory_Comparison>(x => x.Kind == "1");

            if (!string.IsNullOrWhiteSpace(factory))
            {
                pred.And(x => x.Factory == factory);
                predCom.And(x => x.Factory == factory);
            }
            var data = await _repositoryAccessor.HRMS_Org_Department.FindAll(pred)
                .Join(_repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(predCom),
                    department => department.Division,
                    factoryComparison => factoryComparison.Division,
                    (department, factoryComparison) => department)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    department => new { department.Factory, department.Department_Code },
                    language => new { language.Factory, language.Department_Code },
                    (department, language) => new { Department = department, Language = language })
                .SelectMany(
                    x => x.Language.DefaultIfEmpty(),
                    (x, language) => new { x.Department, Language = language })
                .OrderBy(x => x.Department.Department_Code)
                .Select(
                    x => new KeyValuePair<string, string>(
                        x.Department.Department_Code,
                        $"{x.Department.Department_Code} - {(x.Language != null ? x.Language.Name : x.Department.Department_Name)}"
                    )
                ).Distinct().ToListAsync();

            return data;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(List<string> roleList, string language)
        => await Query_Factory_AddList(roleList, language);
        public async Task<List<KeyValuePair<string, string>>> GetListWorkShiftType(string language)
        => await GetDataBasicCode(BasicCodeTypeConstant.WorkShiftType, language);

        public async Task<int> GetCountRecords(EmployeeAttendanceDataSheetParam param)
        {
            var data = await GetData(param);
            return data.Count;
        }
    }
}