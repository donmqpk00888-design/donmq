using System.Globalization;
using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_2_6_DailyDinnerAllowanceList : BaseServices, I_5_2_6_DailyDinnerAllowanceList
    {
        public S_5_2_6_DailyDinnerAllowanceList(DBContext dbContext) : base(dbContext)
        {
        }

        public async Task<List<KeyValuePair<string, string>>> GetDropDownList(DailyDinnerAllowanceList_Param param, List<string> roleList)
        {
            var factory_Addlist = await Queryt_Factory_AddList(roleList);
            var HBC = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory && factory_Addlist.Contains(x.Code)).ToListAsync();
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


        public async Task<OperationResult> Search(DailyDinnerAllowanceList_Param param)
        {
            var result = await GetData(param);
            return result.IsSuccess ? new OperationResult(result.IsSuccess, ((List<DailyDinnerAllowanceList_HAOM_DTO>)result.Data).Count) : result;
        }
        public async Task<OperationResult> Excel(DailyDinnerAllowanceList_Param param, string userName)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;
            var HACR = _repositoryAccessor.HRMS_Att_Change_Record.FindAll(x => x.Factory == param.Factory).ToHashSet();
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
                .FindAll(x =>
                    x.Language_Code.ToLower() == param.Lang.ToLower());
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
                })
                .ToHashSet();
            var data = ((List<DailyDinnerAllowanceList_HAOM_DTO>)result.Data).Select(x =>
                {
                    var leave_Code = "";
                    var clock_Out = "";
                    var _HACR = HACR.FirstOrDefault(y => y.Att_Date.Date == x.Overtime_Date.Date && y.USER_GUID == x.USER_GUID);
                    if (_HACR != null)
                    {
                        leave_Code = _HACR.Leave_Code;
                        clock_Out = _HACR.Overtime_ClockOut == "0000"
                            ? _HACR.Overtime_ClockIn == "0000"
                                ? _HACR.Clock_Out
                                : _HACR.Overtime_ClockIn
                            : _HACR.Overtime_ClockOut;
                    }
                    var leave_Code_Lang = !string.IsNullOrWhiteSpace(leave_Code) ? Leave_Code_Langs.FirstOrDefault(y => y.Code == leave_Code) : null;
                    var work_Shift_Type_Lang = Work_Shift_Type_Langs.FirstOrDefault(y => y.Code == x.Work_Shift_Type);
                    return new DailyDinnerAllowanceList_Table()
                    {
                       Department_Code = depLang.FirstOrDefault(y => y.Factory == x.Factory_HEP && y.Division == x.Division_HEP
                                                                        && y.Department_Code == x.Department_HEP)?.Department_Code,
                        Department_Name = depLang.FirstOrDefault(y => y.Factory == x.Factory_HEP && y.Division == x.Division_HEP
                                                                        && y.Department_Code == x.Department_HEP)?.Department_Name,
                        Employee_Id = x.Employee_ID,
                        Local_Full_Name = x.Local_Full_Name,
                        Date = x.Overtime_Date.ToString("yyyy/MM/dd"),
                        Work_Shift_Type = work_Shift_Type_Lang != null
                            ? $"{work_Shift_Type_Lang.Code} - {work_Shift_Type_Lang.Code_Name}"
                            : x.Work_Shift_Type,
                        Leave = leave_Code_Lang != null
                            ? $"{leave_Code_Lang.Code} - {leave_Code_Lang.Code_Name}"
                            : leave_Code,
                        Clock_Out = clock_Out,
                        Overtime_Hours = x.Overtime_Hours
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
                dataTables, dataCells, 
                "Resources\\Template\\AttendanceMaintenance\\5_2_6_DailyDinnerAllowanceList\\Download.xlsx", 
                configDownload
            );
            var dataResult = new DataResult
            {
                Result = excelResult.Result,
                Count = ((List<DailyDinnerAllowanceList_HAOM_DTO>)result.Data).Count
            };
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, dataResult);
        }
        private async Task<OperationResult> GetData(DailyDinnerAllowanceList_Param param)
        {
            if (string.IsNullOrWhiteSpace(param.Factory)
            || string.IsNullOrWhiteSpace(param.Clock_Out_Date_From_Str)
            || string.IsNullOrWhiteSpace(param.Clock_Out_Date_To_Str)
            || !DateTime.TryParseExact(param.Clock_Out_Date_From_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime clockOutDateFromValue)
            || !DateTime.TryParseExact(param.Clock_Out_Date_To_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime clockOutDateToValue))
                return new OperationResult(false, "InvalidInput");
            List<string> allow_WorkShiftType = new() { "00", "10", "20", "30", "40", "50", "60", "80", "S0", "T0" };
            var predicateOvertimeMaintain = PredicateBuilder.New<HRMS_Att_Overtime_Maintain>(x =>
                x.Factory == param.Factory &&
                allow_WorkShiftType.Contains(x.Work_Shift_Type) &&
                x.Overtime_Date.Date >= clockOutDateFromValue.Date &&
                x.Overtime_Date.Date <= clockOutDateToValue.Date &&
                x.Holiday == "XXX" &&
                (x.Overtime_Hours + x.Training_Hours) > 1.5M
            );
            var predicatePersonal = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory);

            if (!string.IsNullOrWhiteSpace(param.Employee_Id))
                predicateOvertimeMaintain.And(x => x.Employee_ID.ToLower().Contains(param.Employee_Id.ToLower().Trim()));
            if (!string.IsNullOrWhiteSpace(param.Department))
                predicateOvertimeMaintain.And(x => x.Department == param.Department);

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predicatePersonal).ToList();
            var HAOM = await _repositoryAccessor.HRMS_Att_Overtime_Maintain.FindAll(predicateOvertimeMaintain).ToListAsync();
            var data = HAOM
                .GroupJoin(HEP,
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { HAOM = x, HEP = y })
                .SelectMany(x => x.HEP.DefaultIfEmpty(),
                    (x, y) => new { x.HAOM, HEP = y });
            List<DailyDinnerAllowanceList_HAOM_DTO> result = data.Select(x => new DailyDinnerAllowanceList_HAOM_DTO()
            {
                USER_GUID = x.HAOM.USER_GUID,
                Factory = x.HAOM.Factory,
                Overtime_Date = x.HAOM.Overtime_Date,
                Employee_ID = x.HAOM.Employee_ID,
                Department = x.HAOM.Department,
                Work_Shift_Type = x.HAOM.Work_Shift_Type,
                Overtime_Start = x.HAOM.Overtime_Start,
                Overtime_End = x.HAOM.Overtime_End,
                Overtime_Hours = x.HAOM.Overtime_Hours,
                Night_Hours = x.HAOM.Night_Hours,
                Night_Overtime_Hours = x.HAOM.Night_Overtime_Hours,
                Training_Hours = x.HAOM.Training_Hours,
                Night_Eat_Times = x.HAOM.Night_Eat_Times,
                Holiday = x.HAOM.Holiday,
                Update_By = x.HAOM.Update_By,
                Update_Time = x.HAOM.Update_Time,
                Local_Full_Name = x.HEP?.Local_Full_Name,
                Factory_HEP = string.IsNullOrEmpty(x.HEP?.Employment_Status) ? x.HEP?.Factory : x.HEP?.Assigned_Factory,
                Department_HEP = string.IsNullOrEmpty(x.HEP?.Employment_Status) ? x.HEP?.Department : x.HEP?.Assigned_Department,
                Division_HEP = string.IsNullOrEmpty(x.HEP?.Employment_Status) ? x.HEP?.Division : x.HEP?.Assigned_Division,
            }).ToList();
            return new OperationResult(true, result);
        }
    }
}