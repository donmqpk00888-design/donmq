using System.Globalization;
using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Helper.Utilities;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_2_7_DailyUnregisteredOvertimeList : BaseServices, I_5_2_7_DailyUnregisteredOvertimeList
    {
        public S_5_2_7_DailyUnregisteredOvertimeList(DBContext dbContext) : base(dbContext)
        {
        }

        public async Task<List<KeyValuePair<string, string>>> GetDropDownList(DailyUnregisteredOvertimeList_Param param, List<string> roleList)
        {
            var factory_Addlist = await Queryt_Factory_AddList(roleList);
            var HBC = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "2" && factory_Addlist.Contains(x.Code)).ToListAsync();
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower()).ToList();
            var result = new List<KeyValuePair<string, string>>();
            var data = HBC.GroupJoin(HBCL,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { hbc = x, hbcl = y })
                    .SelectMany(x => x.hbcl.DefaultIfEmpty(),
                    (x, y) => new { x.hbc, hbcl = y }).ToList();
            result.AddRange(data.Select(x => new KeyValuePair<string, string>("FA", $"{x.hbc.Code}-{(x.hbcl != null ? x.hbcl.Code_Name : x.hbc.Code_Name)}")).Distinct().ToList());
            if (!string.IsNullOrWhiteSpace(param.Factory))
            {
                var comparisonDepartment = await Query_Department_List(param.Factory);
                var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                    .FindAll(x =>
                        x.Factory == param.Factory &&
                        x.Language_Code.ToLower() == param.Lang.ToLower())
                    .ToList();
                var dataDept = comparisonDepartment.GroupJoin(HODL,
                        x => new {x.Division, x.Department_Code},
                        y => new {y.Division, y.Department_Code},
                        (x, y) => new { dept = x, hodl = y })
                        .SelectMany(x => x.hodl.DefaultIfEmpty(),
                        (x, y) => new { x.dept, hodl = y });
                result.AddRange(dataDept.Select(x => new KeyValuePair<string, string>("DE", $"{x.dept.Department_Code}-{(x.hodl != null ? x.hodl.Name : x.dept.Department_Name)}")).Distinct().ToList());
            }
            return result;
        }
        public async Task<OperationResult> Search(DailyUnregisteredOvertimeList_Param param)
        {
            var result = await GetData(param);
            return result.IsSuccess ? new OperationResult(result.IsSuccess, ((List<DailyUnregisteredOvertimeList_HACR_DTO>)result.Data).Count) : result;
        }

        public async Task<OperationResult> Excel(DailyUnregisteredOvertimeList_Param param, string userName)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;
            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll();
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower());
            var codeLang = HBC
                .GroupJoin(HBCL,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Type_Seq,
                    x.HBC.Code,
                    Code_Name = x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name
                });
            var Leave_Code_Langs = codeLang.Where(y => y.Type_Seq == BasicCodeTypeConstant.Leave).ToHashSet();
            var Work_Shift_Type_Langs = codeLang.Where(y => y.Type_Seq == BasicCodeTypeConstant.WorkShiftType).ToHashSet();
            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll(true);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower());
            var depLang = HOD
               .GroupJoin(HODL,
                    x => new { x.Factory, x.Department_Code },
                    y => new { y.Factory, y.Department_Code },
                    (x, y) => new { HOD = x, HODL = y })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, HODL = y })
                .Select(x => new
                {
                    x.HOD.Division,
                    x.HOD.Factory,
                    x.HOD.Department_Code,
                    Department_Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name
                }).ToHashSet();
            var data = ((List<DailyUnregisteredOvertimeList_HACR_DTO>)result.Data).Select(x =>
                {
                    var clock_Out = x.Overtime_ClockOut == "0000"
                        ? x.Overtime_ClockIn == "0000"
                            ? x.Clock_Out
                            : x.Overtime_ClockIn
                        : x.Overtime_ClockOut;
                    var leave_Code_Lang = !string.IsNullOrWhiteSpace(x.Leave_Code) ? Leave_Code_Langs.FirstOrDefault(y => y.Code == x.Leave_Code) : null;
                    var work_Shift_Type_Lang = Work_Shift_Type_Langs.FirstOrDefault(y => y.Code == x.Work_Shift_Type);
                    return new DailyUnregisteredOvertimeList_Table()
                    {
                        Department_Code = depLang.FirstOrDefault(y => y.Factory == x.Factory_HEP && y.Division == x.Division_HEP
                                                                        && y.Department_Code == x.Department_HEP)?.Department_Code,
                        Department_Name = depLang.FirstOrDefault(y => y.Factory == x.Factory_HEP && y.Division == x.Division_HEP
                                                                        && y.Department_Code == x.Department_HEP)?.Department_Name,
                        Employee_Id = x.Employee_ID,
                        Local_Full_Name = x.Local_Full_Name,
                        Date = x.Att_Date.ToString("yyyy/MM/dd"),
                        Work_Shift_Type = work_Shift_Type_Lang != null
                            ? $"{work_Shift_Type_Lang.Code} - {work_Shift_Type_Lang.Code_Name}"
                            : x.Work_Shift_Type,
                        Leave = leave_Code_Lang != null
                            ? $"{leave_Code_Lang.Code} - {leave_Code_Lang.Code_Name}"
                            : x.Leave_Code,
                        Clock_In = x.Clock_In,
                        Clock_Out = clock_Out
                    };
                })
                .OrderBy(x => x.Department_Code)
                .ThenBy(x => x.Employee_Id)
                .ThenBy(x => x.Date)
                .ToList();
            var dataTables = new List<Table>() { new("result", data) };
            var dataCells = new List<SDCores.Cell>(){
                new("B1", param.Factory),
                new("E1", param.Department),
                new("H1", param.Employee_Id),
                new("K1", $"{param.Clock_Out_Date_From_Str}~{param.Clock_Out_Date_To_Str}" ),
                new("B3", userName),
                new("D3", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))
            };
            var configDownload = new ConfigDownload(false);
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataTables, 
                dataCells, 
                "Resources\\Template\\AttendanceMaintenance\\5_2_7_DailyUnregisteredOvertimeList\\Download.xlsx", 
                configDownload
            );
            var dataResult = new DataResult
            {
                Result = excelResult.Result,
                Count = ((List<DailyUnregisteredOvertimeList_HACR_DTO>)result.Data).Count
            };
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, dataResult);
        }
        private async Task<OperationResult> GetData(DailyUnregisteredOvertimeList_Param param)
        {
            if (string.IsNullOrWhiteSpace(param.Factory)
            || string.IsNullOrWhiteSpace(param.Clock_Out_Date_From_Str)
            || string.IsNullOrWhiteSpace(param.Clock_Out_Date_To_Str)
            || !DateTime.TryParseExact(param.Clock_Out_Date_From_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime clockOutDateFromValue)
            || !DateTime.TryParseExact(param.Clock_Out_Date_To_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime clockOutDateToValue))
                return new OperationResult(false, "InvalidInput");
            List<string> allow_WorkShiftType = new() { "00", "10", "20", "30", "40", "50", "60", "70", "90", "S0", "T0" };
            var predicateChangeRecord = PredicateBuilder.New<HRMS_Att_Change_Record>(x =>
                x.Factory == param.Factory &&
                allow_WorkShiftType.Contains(x.Work_Shift_Type) &&
                x.Att_Date.Date >= clockOutDateFromValue.Date &&
                x.Att_Date.Date <= clockOutDateToValue.Date
            );
            var predicatePersonal = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory);

            if (!string.IsNullOrWhiteSpace(param.Employee_Id))
                predicateChangeRecord.And(x => x.Employee_ID.ToLower().Contains(param.Employee_Id.ToLower().Trim()));
            if (!string.IsNullOrWhiteSpace(param.Department))
                predicateChangeRecord.And(x => x.Department == param.Department);

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predicatePersonal).ToList();

            var HACR = await _repositoryAccessor.HRMS_Att_Change_Record.FindAll(predicateChangeRecord).ToListAsync();
            var HAWS = _repositoryAccessor.HRMS_Att_Work_Shift.FindAll(x => x.Factory == param.Factory).ToHashSet();
            var data = HACR
                .GroupJoin(HEP,
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { HACR = x, HEP = y })
                .SelectMany(x => x.HEP.DefaultIfEmpty(),
                    (x, y) => new { x.HACR, HEP = y });
            List<DailyUnregisteredOvertimeList_HACR_DTO> result = new();
            foreach (var item in data)
            {
                int week = (int)item.HACR.Att_Date.DayOfWeek;
                var _HAWS = HAWS.FirstOrDefault(x => x.Work_Shift_Type == item.HACR.Work_Shift_Type && x.Week == week.ToString());
                if (_HAWS != null)
                {
                    if (item.HACR.Clock_In.IsTimeSpanFormat() &&
                        item.HACR.Clock_Out.IsTimeSpanFormat() &&
                        item.HACR.Overtime_ClockIn.IsTimeSpanFormat() &&
                        item.HACR.Overtime_ClockOut.IsTimeSpanFormat() &&
                        _HAWS.Clock_In.IsTimeSpanFormat() &&
                        _HAWS.Clock_Out.IsTimeSpanFormat() &&
                        _HAWS.Overtime_ClockIn.IsTimeSpanFormat() &&
                        _HAWS.Overtime_ClockOut.IsTimeSpanFormat()
                    )
                    {
                        var wk_flag = 'N';
                        // case #1
                        if (item.HACR.Overtime_ClockOut.ToTimeSpan() != "0000".ToTimeSpan() &&
                        item.HACR.Overtime_ClockOut.ToTimeSpan().Hours * 60 + item.HACR.Overtime_ClockOut.ToTimeSpan().Minutes >= _HAWS.Clock_Out.ToTimeSpan().Hours * 60 + _HAWS.Clock_Out.ToTimeSpan().Minutes + 30)
                        {
                            item.HACR.Clock_Out = item.HACR.Overtime_ClockOut;
                            wk_flag = 'Y';
                        }
                        //case #2
                        else if (item.HACR.Overtime_ClockIn.ToTimeSpan() != "0000".ToTimeSpan() &&
                        item.HACR.Overtime_ClockIn.ToTimeSpan().Hours * 60 + item.HACR.Overtime_ClockIn.ToTimeSpan().Minutes >= _HAWS.Clock_Out.ToTimeSpan().Hours * 60 + _HAWS.Clock_Out.ToTimeSpan().Minutes + 30)
                        {
                            item.HACR.Clock_Out = item.HACR.Overtime_ClockIn;
                            wk_flag = 'Y';
                        }
                        //case #3
                        else if (item.HACR.Clock_Out.ToTimeSpan() != "0000".ToTimeSpan() &&
                        item.HACR.Clock_Out.ToTimeSpan().Hours * 60 + item.HACR.Clock_Out.ToTimeSpan().Minutes >= _HAWS.Clock_Out.ToTimeSpan().Hours * 60 + _HAWS.Clock_Out.ToTimeSpan().Minutes + 30)
                        {
                            wk_flag = 'Y';
                        }
                        //case #4
                        else if (item.HACR.Clock_In.ToTimeSpan() != "0000".ToTimeSpan() &&
                        item.HACR.Clock_In.ToTimeSpan().Hours * 60 + item.HACR.Clock_In.ToTimeSpan().Minutes >= _HAWS.Clock_Out.ToTimeSpan().Hours * 60 + _HAWS.Clock_Out.ToTimeSpan().Minutes + 30)
                        {
                            item.HACR.Clock_Out = item.HACR.Clock_In;
                            wk_flag = 'Y';
                        }
                        var TotalHr = _repositoryAccessor.HRMS_Att_Overtime_Maintain.FindAll(x=> x.Factory == item.HACR.Factory && x.Overtime_Date == item.HACR.Att_Date
                        && x.Employee_ID == item.HACR.Employee_ID).Sum(x => x.Overtime_Hours + x.Training_Hours);

                        if (wk_flag == 'Y' && TotalHr == 0)
                        {
                            var addData = new DailyUnregisteredOvertimeList_HACR_DTO()
                            {
                                USER_GUID = item.HACR.USER_GUID,
                                Factory = item.HACR.Factory,
                                Att_Date = item.HACR.Att_Date,
                                Employee_ID = item.HACR.Employee_ID,
                                Department = item.HACR.Department,
                                Work_Shift_Type = item.HACR.Work_Shift_Type,
                                Leave_Code = item.HACR.Leave_Code,
                                Clock_In = item.HACR.Clock_In,
                                Clock_Out = item.HACR.Clock_Out,
                                Overtime_ClockIn = item.HACR.Overtime_ClockIn,
                                Overtime_ClockOut = item.HACR.Overtime_ClockOut,
                                Days = item.HACR.Days,
                                Holiday = item.HACR.Holiday,
                                Update_By = item.HACR.Update_By,
                                Update_Time = item.HACR.Update_Time,
                                Local_Full_Name = item.HEP?.Local_Full_Name,
                                Factory_HEP = string.IsNullOrEmpty(item.HEP?.Employment_Status) ? item.HEP?.Factory : item.HEP?.Assigned_Factory,
                                Department_HEP = string.IsNullOrEmpty(item.HEP?.Employment_Status) ? item.HEP?.Department : item.HEP?.Assigned_Department,
                                Division_HEP = string.IsNullOrEmpty(item.HEP?.Employment_Status) ? item.HEP?.Division : item.HEP?.Assigned_Division,
                            };
                            result.Add(addData);
                        }
                    }
                }
            }
            return new OperationResult(true, result);
        }
    }
}