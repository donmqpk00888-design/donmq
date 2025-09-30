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
    public class S_5_2_8_DailyNoNightShiftHoursList : BaseServices, I_5_2_8_DailyNoNightShiftHoursList
    {
        private readonly TimeSpan timeSpan0000 = "0000".ToTimeSpan();

        public S_5_2_8_DailyNoNightShiftHoursList(DBContext dbContext) : base(dbContext)
        {
        }

        public async Task<List<KeyValuePair<string, string>>> GetDropDownList(DailyNoNightShiftHoursList_Param param, List<string> roleList)
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

        public async Task<OperationResult> Search(DailyNoNightShiftHoursList_Param param)
        {
            var result = await GetData(param);
            return result.IsSuccess ? new OperationResult(result.IsSuccess, ((List<HRMS_Att_Change_Record>)result.Data).Count) : result;
        }

        public async Task<OperationResult> Excel(DailyNoNightShiftHoursList_Param param, string userName)
        {
            var res = await GetData(param);
            if (!res.IsSuccess)
                return res;
            var HACR = (List<HRMS_Att_Change_Record>)res.Data;
            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == param.Factory);
            var data = HACR
                .GroupJoin(HEP,
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { HACR = x, HEP = y })
                .SelectMany(x => x.HEP.DefaultIfEmpty(),
                    (x, y) => new { x.HACR, HEP = y }).ToHashSet();

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
            List<string> allowEmpStatus = new() { "A", "S" };
            var result = data.Select(x =>
                {
                    var dept = x.HEP == null
                        ? null
                        : !string.IsNullOrWhiteSpace(x.HEP.Employment_Status) && allowEmpStatus.Contains(x.HEP.Employment_Status)
                            ? depLang.FirstOrDefault(y => y.Division == x.HEP.Assigned_Division && y.Factory == x.HEP.Assigned_Factory && y.Department_Code == x.HEP.Assigned_Department)
                            : depLang.FirstOrDefault(y => y.Division == x.HEP.Division && y.Factory == x.HEP.Factory && y.Department_Code == x.HEP.Department);
                    var leave_Code_Lang = Leave_Code_Langs.FirstOrDefault(y => y.Code == x.HACR.Leave_Code);
                    var work_Shift_Type_Lang = Work_Shift_Type_Langs.FirstOrDefault(y => y.Code == x.HACR.Work_Shift_Type);
                    return new DailyNoNightShiftHoursList_Table()
                    {
                        Department_Code = dept?.Department_Code,
                        Department_Name = dept?.Department_Name,
                        Employee_Id = x.HACR.Employee_ID,
                        Local_Full_Name = x.HEP?.Local_Full_Name,
                        Date = x.HACR.Att_Date.ToString("yyyy/MM/dd"),
                        Work_Shift_Type = work_Shift_Type_Lang != null
                            ? $"{work_Shift_Type_Lang.Code} - {work_Shift_Type_Lang.Code_Name}"
                            : x.HACR.Work_Shift_Type,
                        Leave = leave_Code_Lang != null
                            ? $"{leave_Code_Lang.Code} - {leave_Code_Lang.Code_Name}"
                            : x.HACR.Leave_Code,
                        Clock_Out = x.HACR.Overtime_ClockOut.ToTimeSpan() == timeSpan0000
                            ? x.HACR.Overtime_ClockIn.ToTimeSpan() == timeSpan0000
                                ? x.HACR.Clock_Out
                                : x.HACR.Overtime_ClockIn
                            : x.HACR.Overtime_ClockOut
                    };
                })
                .OrderBy(x => x.Department_Code)
                .ThenBy(x => x.Employee_Id)
                .ThenBy(x => x.Date)
                .ToList();
            var dataTables = new List<Table>() { new("result", result) };
            var dataCells = new List<Cell>(){
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
                "Resources\\Template\\AttendanceMaintenance\\5_2_8_DailyNoNightShiftHoursList\\Download.xlsx", 
                configDownload
            );
            var dataResult = new DataResult
            {
                Result = excelResult.Result,
                Count = HACR.Count
            };
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, dataResult);
        }
        private async Task<OperationResult> GetData(DailyNoNightShiftHoursList_Param param)
        {
            if (string.IsNullOrWhiteSpace(param.Factory)
            || string.IsNullOrWhiteSpace(param.Clock_Out_Date_From_Str)
            || string.IsNullOrWhiteSpace(param.Clock_Out_Date_To_Str)
            || !DateTime.TryParseExact(param.Clock_Out_Date_From_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime clockOutDateFromValue)
            || !DateTime.TryParseExact(param.Clock_Out_Date_To_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime clockOutDateToValue))
                return new OperationResult(false, "InvalidInput");
            var allow_WorkShiftType = _repositoryAccessor.HRMS_Att_Work_Shift.FindAll(x => x.Overnight.Trim() == "Y").Select(x => x.Work_Shift_Type).Distinct();
            var predicateChangeRecord = PredicateBuilder.New<HRMS_Att_Change_Record>(x =>
                x.Factory == param.Factory &&
                allow_WorkShiftType.Contains(x.Work_Shift_Type) &&
                x.Att_Date.Date >= clockOutDateFromValue.Date &&
                x.Att_Date.Date <= clockOutDateToValue.Date &&
                x.Leave_Code == "00"
            );

            if (!string.IsNullOrWhiteSpace(param.Employee_Id))
                predicateChangeRecord.And(x => x.Employee_ID.ToLower().Contains(param.Employee_Id.ToLower().Trim()));
            if (!string.IsNullOrWhiteSpace(param.Department))
                predicateChangeRecord.And(x => x.Department == param.Department);

            var HACR = _repositoryAccessor.HRMS_Att_Change_Record.FindAll(predicateChangeRecord);
            var HAOM = _repositoryAccessor.HRMS_Att_Overtime_Maintain.FindAll();
            var data = await HACR
                .GroupJoin(HAOM,
                    x => new { x.USER_GUID, x.Att_Date.Date },
                    y => new { y.USER_GUID, y.Overtime_Date.Date },
                    (x, y) => new { HACR = x, HAOM = y })
                .SelectMany(x => x.HAOM.DefaultIfEmpty(),
                    (x, y) => new { x.HACR, HAOM = y })
                .Where(x => x.HAOM == null || (x.HAOM != null && x.HAOM.Night_Hours == 0))
                .ToListAsync();
            var result = data.Select(x => x.HACR).ToList();
            return new OperationResult(true, result);
        }
    }
}