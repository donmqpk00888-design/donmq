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
    public class S_5_2_5_DailySwipeCardAnomaliesList : BaseServices, I_5_2_5_DailySwipeCardAnomaliesList
    {
        public S_5_2_5_DailySwipeCardAnomaliesList(DBContext dbContext) : base(dbContext)
        {
        }

        public async Task<List<KeyValuePair<string, string>>> GetDropDownList(DailySwipeCardAnomaliesList_Param param, List<string> roleList)
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
        public async Task<OperationResult> Search(DailySwipeCardAnomaliesList_Param param)
        {
            var result = await GetData(param);
            return result.IsSuccess ? new OperationResult(result.IsSuccess, ((List<DailySwipeCardAnomaliesList_HACR_DTO>)result.Data).Count) : result;
        }
        public async Task<OperationResult> Excel(DailySwipeCardAnomaliesList_Param param, string userName)
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
                    x => new { x.Division, x.Factory, x.Department_Code },
                    y => new { y.Division, y.Factory, y.Department_Code },
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

            var data = ((List<DailySwipeCardAnomaliesList_HACR_DTO>)result.Data)
                .Select(T =>
                {
                    var leave_Code_Lang = !string.IsNullOrWhiteSpace(T.Leave_Code) ? Leave_Code_Langs.FirstOrDefault(y => y.Code == T.Leave_Code) : null;
                    var work_Shift_Type_Lang = Work_Shift_Type_Langs.FirstOrDefault(y => y.Code == T.Work_Shift_Type);

                    return new DailySwipeCardAnomaliesList_Table()
                    {
                        Department_Code = depLang.FirstOrDefault(y => y.Factory == T.Factory_HEP && y.Division == T.Division_HEP
                                                                         && y.Department_Code == T.Department_HEP)?.Department_Code,
                        Department_Name = depLang.FirstOrDefault(y => y.Factory == T.Factory_HEP && y.Division == T.Division_HEP
                                                                        && y.Department_Code == T.Department_HEP)?.Department_Name,
                        Employee_Id = T.Employee_ID,
                        Local_Full_Name = T.Local_Full_Name,
                        Date = T.Att_Date.ToString("yyyy/MM/dd"),
                        Work_Shift_Type = work_Shift_Type_Lang != null
                            ? $"{work_Shift_Type_Lang.Code} - {work_Shift_Type_Lang.Code_Name}"
                            : T.Work_Shift_Type,
                        Leave = leave_Code_Lang != null
                            ? $"{leave_Code_Lang.Code} - {leave_Code_Lang.Code_Name}"
                            : T.Leave_Code,
                        Clock_Out = T.C_OUT,
                        Clock_In = T.C_IN,
                        Overtime_Hour = T.Overtime_Hour.ToString("0.00000")
                    };
                })
                .ToList();

            var dataTables = new List<Table>() { new("result", data) };
            var dataCells = new List<Cell>(){
                new("B2", param.Factory),
                new("E2", param.Department),
                new("H2", param.Employee_Id),
                new("K2", $"{param.Clock_Out_Date_From_Str}~{param.Clock_Out_Date_To_Str}" ),
                new("B4", userName),
                new("D4", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))
            };
            var configDownload = new ConfigDownload(false);
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataTables,
                dataCells,
                "Resources\\Template\\AttendanceMaintenance\\5_2_5_DailySwipeCardAnomaliesList\\Download.xlsx",
                configDownload
            );
            var dataResult = new DataResult
            {
                Result = excelResult.Result,
                Count = ((List<DailySwipeCardAnomaliesList_HACR_DTO>)result.Data).Count
            };
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, dataResult);
        }
        private async Task<OperationResult> GetData(DailySwipeCardAnomaliesList_Param param)
        {
            if (string.IsNullOrWhiteSpace(param.Factory)
            || string.IsNullOrWhiteSpace(param.Clock_Out_Date_From_Str)
            || string.IsNullOrWhiteSpace(param.Clock_Out_Date_To_Str)
            || !DateTime.TryParseExact(param.Clock_Out_Date_From_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime clockOutDateFromValue)
            || !DateTime.TryParseExact(param.Clock_Out_Date_To_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime clockOutDateToValue))
                return new OperationResult(false, "InvalidInput");

            List<string> allow_LeaveCode = new() { "00", "06", "09", "11", "L0", "L1" };

            var predicateChangeRecord = PredicateBuilder.New<HRMS_Att_Change_Record>(x =>
                x.Factory == param.Factory &&
                x.Att_Date.Date >= clockOutDateFromValue.Date &&
                x.Att_Date.Date <= clockOutDateToValue.Date &&
                allow_LeaveCode.Contains(x.Leave_Code)
            );

            if (!string.IsNullOrWhiteSpace(param.Employee_Id))
                predicateChangeRecord.And(x => x.Employee_ID.ToLower().Contains(param.Employee_Id.ToLower().Trim()));
            if (!string.IsNullOrWhiteSpace(param.Department))
                predicateChangeRecord.And(x => x.Department == param.Department);

            var HACR = await _repositoryAccessor.HRMS_Att_Change_Record.FindAll(predicateChangeRecord).ToListAsync();
            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(true).ToList();
            var HASANS = _repositoryAccessor.HRMS_Att_SwipeCard_Anomalies_Set.FindAll(x => x.Factory == param.Factory).ToList();
            var HAOM = _repositoryAccessor.HRMS_Att_Overtime_Maintain.FindAll(y => y.Factory == param.Factory).ToHashSet();

            var Temp = HACR
                .Join(HEP,
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { HACR = x, HEP = y })
                .Join(HASANS,
                    x => new { x.HACR.Factory, x.HACR.Work_Shift_Type },
                    y => new { y.Factory, y.Work_Shift_Type },
                    (x, y) => new { x.HACR, x.HEP, HASANS = y })
                .DistinctBy(x => new { x.HACR.Att_Date, x.HEP.Employee_ID, x.HACR.Work_Shift_Type, x.HACR.Department, x.HACR.Leave_Code })
                .Select(item =>
                {
                    var T = new DailySwipeCardAnomaliesList_HACR_DTO
                    {
                        USER_GUID = item.HACR.USER_GUID,
                        Factory = item.HACR.Factory,
                        Department = item.HACR.Department,
                        Employee_ID = item.HACR.Employee_ID,
                        Local_Full_Name = item.HEP.Local_Full_Name,
                        Att_Date = item.HACR.Att_Date,
                        Work_Shift_Type = item.HACR.Work_Shift_Type,
                        Leave_Code = item.HACR.Leave_Code,
                        Clock_In = item.HACR.Clock_In,
                        Clock_Out = item.HACR.Clock_Out,
                        Overtime_ClockIn = item.HACR.Overtime_ClockIn,
                        Overtime_ClockOut = item.HACR.Overtime_ClockOut,
                        Kind = item.HASANS.Kind,
                        C_IN = "0000",
                        C_OUT = "0000",
                        Overtime_Hour = 0m,
                        Factory_HEP = string.IsNullOrEmpty(item.HEP.Employment_Status) ? item.HEP.Factory : item.HEP.Assigned_Factory,
                        Department_HEP = string.IsNullOrEmpty(item.HEP.Employment_Status) ? item.HEP.Department : item.HEP.Assigned_Department,
                        Division_HEP = string.IsNullOrEmpty(item.HEP.Employment_Status) ? item.HEP.Division : item.HEP.Assigned_Division,
                    };
                    //時間設定檔 是暫存檔create temp
                    var SetTemp = HASANS.Where(x => x.Work_Shift_Type == T.Work_Shift_Type);
                    //上班時間 要抓MAX(seq) -- maxSeq.Clock_In AS SetIn, MAX(Seq) AS SetSeq
                    var maxSeq = SetTemp.OrderByDescending(x => x.Seq).FirstOrDefault();
                    if (maxSeq == null)
                        return T;
                    (string SetIn, short SetSeq) = (maxSeq.Clock_In, maxSeq.Seq);
                    if (T.Clock_In != T.C_IN && string.Compare(T.Clock_In, SetIn) < 0)
                        T.C_IN = T.Clock_In;
                    //--下班時間分類1
                    var unpivot = new[]
                        {
                        new UnpivotRecord{ Factory = T.Factory, Att_Date = T.Att_Date, Work_Shift_Type = T.Work_Shift_Type, Employee_Id = T.Employee_ID, Clock_In = T.Clock_In, Out_name = "Clock_Out", Clock_O = T.Clock_Out},
                        new UnpivotRecord{ Factory = T.Factory, Att_Date = T.Att_Date, Work_Shift_Type = T.Work_Shift_Type, Employee_Id = T.Employee_ID, Clock_In = T.Clock_In, Out_name = "Overtime_ClockIn ", Clock_O = T.Overtime_ClockIn},
                        new UnpivotRecord{ Factory = T.Factory, Att_Date = T.Att_Date, Work_Shift_Type = T.Work_Shift_Type, Employee_Id = T.Employee_ID, Clock_In = T.Clock_In, Out_name = "Overtime_ClockOut", Clock_O = T.Overtime_ClockOut}
                    };
                    var UPV_Settemp = unpivot.Where(x => x.Clock_O != "0000")
                        .Join(SetTemp,
                            x => new { x.Factory, x.Work_Shift_Type },
                            y => new { y.Factory, y.Work_Shift_Type },
                            (x, y) => new DailySwipeCardAnomaliesList_Unpivot
                            {
                                Factory = x.Factory,
                                Att_Date = x.Att_Date,
                                Work_Shift_Type = x.Work_Shift_Type,
                                Employee_Id = x.Employee_Id,
                                Clock_In = x.Clock_In,
                                swpctime = x.Clock_O,
                                Out_name = x.Out_name,
                                Clock_Out_Start = y.Clock_Out_Start,
                                Clock_Out_End = y.Clock_Out_End,
                                Seq = y.Seq
                            })
                        .OrderBy(x => x.Employee_Id)
                        .ThenBy(x => x.Att_Date)
                        .ThenByDescending(x => x.Out_name);
                    DailySwipeCardAnomaliesList_Unpivot rs = new();
                    switch (T.Kind)
                    {
                        case "1":
                            rs = UPV_Settemp.FirstOrDefault(x => string.Compare(x.swpctime, x.Clock_Out_End) > 0); // unpivot.Clock_O > SetTemp.Clock_Out_End
                            if (rs != null)
                                T.C_OUT = rs.swpctime;
                            break;
                        case "2":
                            rs = UPV_Settemp.FirstOrDefault(x =>
                                x.Seq <= maxSeq.Seq - 1 &&
                                string.Compare(x.swpctime, x.Clock_Out_Start) > 0 &&
                                string.Compare(x.swpctime, x.Clock_Out_End) < 0);
                            if (rs != null)
                            {
                                T.C_OUT = rs.swpctime;
                                break;
                            }
                            if (T.C_OUT == "0000")
                            {
                                rs = UPV_Settemp.FirstOrDefault(x =>
                                    x.Seq == maxSeq.Seq &&
                                    string.Compare(x.swpctime, x.Clock_Out_End) > 0);
                                if (rs != null)
                                    T.C_OUT = rs.swpctime;
                            }
                            break;
                    }
                    if (T.C_IN == "0000" && T.C_OUT == "0000")
                        return T;
                    T.C_IN = T.C_IN == "0000" ? T.Clock_In : T.C_IN;
                    T.C_OUT = T.C_OUT == "0000" ?
                        T.Overtime_ClockOut != "0000" ? T.Overtime_ClockOut :
                        T.Overtime_ClockIn != "0000" ? T.Overtime_ClockIn :
                        T.Clock_Out != "0000" ? T.Clock_Out : T.C_OUT : T.C_OUT;
                    var _HAOM = HAOM.FirstOrDefault(y => y.USER_GUID == T.USER_GUID && y.Overtime_Date.Date == T.Att_Date.Date);
                    if (_HAOM != null)
                        T.Overtime_Hour = _HAOM.Overtime_Hours + _HAOM.Night_Hours + _HAOM.Night_Overtime_Hours + _HAOM.Training_Hours;
                    return T;
                }).Where(x => x.C_IN != "0000" && x.C_OUT != "0000"); 
            return new OperationResult(true, Temp.OrderBy(x => x.Department).ThenBy(x => x.Employee_ID).ThenBy(x => x.Att_Date).ToList());
        }
    }
}