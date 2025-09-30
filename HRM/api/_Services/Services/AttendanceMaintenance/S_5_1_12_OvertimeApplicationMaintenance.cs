using System.Globalization;
using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Helper.Utilities;
using API.Models;
using Aspose.Cells;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_1_12_OvertimeApplicationMaintenance : BaseServices, I_5_1_12_OvertimeApplicationMaintenance
    {
        public S_5_1_12_OvertimeApplicationMaintenance(DBContext dbContext) : base(dbContext)
        {
        }
        #region Dropdown List
        public async Task<List<KeyValuePair<string, string>>> GetDropDownList(OvertimeApplicationMaintenance_Param param, List<string> roleList)
        {
            var HBC = await _repositoryAccessor.HRMS_Basic_Code.FindAll().ToListAsync();
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower()).ToList();
            var authFactories = await Queryt_Factory_AddList(roleList);
            var result = new List<KeyValuePair<string, string>>();
            var data = HBC.GroupJoin(HBCL,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { hbc = x, hbcl = y })
                    .SelectMany(x => x.hbcl.DefaultIfEmpty(),
                    (x, y) => new { x.hbc, hbcl = y });
            result.AddRange(data.Where(x => x.hbc.Type_Seq == "2" && authFactories.Contains(x.hbc.Code)).Select(x => new KeyValuePair<string, string>("FA", $"{x.hbc.Code}-{(x.hbcl != null ? x.hbcl.Code_Name : x.hbc.Code_Name)}")).Distinct().ToList());
            result.AddRange(data.Where(x => x.hbc.Type_Seq == "41").Select(x => new KeyValuePair<string, string>("WO", $"{x.hbc.Code}-{(x.hbcl != null ? x.hbcl.Code_Name : x.hbc.Code_Name)}")).Distinct().ToList());
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
        public async Task<OvertimeApplicationMaintenance_Main> GetShiftTime(OvertimeApplicationMaintenance_Param param)
        {
            var week_value = (int)Convert.ToDateTime(param.Overtime_Date_Str).DayOfWeek;
            var HAWS = await _repositoryAccessor.HRMS_Att_Work_Shift.FirstOrDefaultAsync(x =>
                x.Factory == param.Factory &&
                x.Work_Shift_Type == param.Work_Shift_Type &&
                x.Week == week_value.ToString() &&
                x.Effective_State);
            OvertimeApplicationMaintenance_Main result = new()
            {
                Clock_In_Str = HAWS != null ? HAWS.Clock_In : "",
                Clock_Out_Str = HAWS != null ? HAWS.Clock_Out : ""
            };
            return result;
        }
        #endregion

        #region Check Data
        public async Task<OperationResult> IsExistedData(OvertimeApplicationMaintenance_Param param)
        {
            OperationResult result = new();
            var predicate = PredicateBuilder.New<HRMS_Att_Overtime_Application>(true);
            if (string.IsNullOrWhiteSpace(param.Factory)
             || string.IsNullOrWhiteSpace(param.Employee_Id)
             || string.IsNullOrWhiteSpace(param.Overtime_Date_Str)
             || !DateTime.TryParseExact(param.Overtime_Date_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime overtimeDateValue))
                return new OperationResult(false);
            predicate.And(x => x.Factory == param.Factory && x.Employee_ID == param.Employee_Id && x.Overtime_Date == overtimeDateValue.Date);
            var isExisted = await _repositoryAccessor.HRMS_Att_Overtime_Application.AnyAsync(predicate);
            return new OperationResult(isExisted);
        }
        #endregion

        #region Query Data
        public async Task<PaginationUtility<OvertimeApplicationMaintenance_Main>> GetSearchDetail(PaginationParam paginationParams, OvertimeApplicationMaintenance_Param searchParam)
        {
            List<OvertimeApplicationMaintenance_Main> result = await GetData(searchParam);
            return PaginationUtility<OvertimeApplicationMaintenance_Main>.Create(result, paginationParams.PageNumber, paginationParams.PageSize);
        }
        private async Task<List<OvertimeApplicationMaintenance_Main>> GetData(OvertimeApplicationMaintenance_Param searchParam)
        {
            var predicate = PredicateBuilder.New<HRMS_Att_Overtime_Application>(x => x.Factory == searchParam.Factory);
            var predicateHAWS = PredicateBuilder.New<HRMS_Att_Work_Shift>(x => x.Factory == searchParam.Factory);
            if (!string.IsNullOrWhiteSpace(searchParam.Department))
                predicate.And(x => x.Department == searchParam.Department);
            if (!string.IsNullOrWhiteSpace(searchParam.Employee_Id))
                predicate.And(x => x.Employee_ID.Contains(searchParam.Employee_Id));
            if (!string.IsNullOrWhiteSpace(searchParam.Work_Shift_Type))
            {
                predicate.And(x => x.Work_Shift_Type == searchParam.Work_Shift_Type);
                predicateHAWS.And(x => x.Work_Shift_Type == searchParam.Work_Shift_Type);
            }
            if (!string.IsNullOrWhiteSpace(searchParam.Overtime_Date_From_Str) && DateTime.TryParseExact(searchParam.Overtime_Date_From_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime dateFromValue))
                predicate.And(x => x.Overtime_Date.Date >= dateFromValue.Date);
            if (!string.IsNullOrWhiteSpace(searchParam.Overtime_Date_To_Str) && DateTime.TryParseExact(searchParam.Overtime_Date_To_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime dateToValue))
                predicate.And(x => x.Overtime_Date.Date <= dateToValue.Date);

            var HAOA = _repositoryAccessor.HRMS_Att_Overtime_Application.FindAll(predicate);
            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll();
            var HAWS = _repositoryAccessor.HRMS_Att_Work_Shift.FindAll(predicateHAWS);
            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll();
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == searchParam.Lang.ToLower());
            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.WorkShiftType && x.IsActive);
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == searchParam.Lang.ToLower());
            var HOD_Lang = HOD
                .GroupJoin(HODL,
                     x => new { x.Factory, x.Division, x.Department_Code },
                    y => new { y.Factory, y.Division, y.Department_Code },
                    (x, y) => new { HOD = x, HODL = y })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, HODL = y })
                .Select(x => new
                {
                    x.HOD.Division,
                    x.HOD.Factory,
                    x.HOD.Department_Code,
                    Department_Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name
                });
            var HBC_WorkShiftType = HBC
                .GroupJoin(HBCL,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Code,
                    Code_Name = x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name
                });

            var result = await HAOA
                .GroupJoin(HEP,
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { HAOA = x, HEP = y })
                .SelectMany(x => x.HEP.DefaultIfEmpty(),
                    (x, y) => new { x.HAOA, HEP = y })
                .GroupJoin(HBC_WorkShiftType,
                    x => x.HAOA.Work_Shift_Type,
                    y => y.Code,
                    (x, y) => new { x.HAOA, x.HEP, HBC_WorkShiftType = y })
                .SelectMany(x => x.HBC_WorkShiftType.DefaultIfEmpty(),
                    (x, y) => new { x.HAOA, x.HEP, HBC_WorkShiftType = y })
                .GroupJoin(HOD_Lang,
                    x => new { x.HAOA.Factory, Department_Code = x.HAOA.Department },
                    y => new { y.Factory, y.Department_Code },
                    (x, y) => new { x.HAOA, x.HEP, x.HBC_WorkShiftType, HOD_Lang = y })
                .SelectMany(x => x.HOD_Lang.DefaultIfEmpty(),
                    (x, y) => new { x.HAOA, x.HEP, x.HBC_WorkShiftType, HOD_Lang = y })
                .GroupJoin(HAWS,
                    x => new { x.HAOA.Factory, x.HAOA.Work_Shift_Type, Week = (EF.Functions.DateDiffDay(new DateTime(1, 1, 1), x.HAOA.Overtime_Date.AddDays(1)) % 7).ToString() },
                    y => new { y.Factory, y.Work_Shift_Type, y.Week },
                    (x, y) => new { x.HAOA, x.HEP, x.HBC_WorkShiftType, x.HOD_Lang, HAWS = y })
                .SelectMany(x => x.HAWS.DefaultIfEmpty(),
                    (x, y) => new { x.HAOA, x.HEP, x.HBC_WorkShiftType, x.HOD_Lang, HAWS = y })
                .Select(x => new OvertimeApplicationMaintenance_Main
                {
                    Factory = x.HAOA.Factory,
                    USER_GUID = x.HAOA.USER_GUID,
                    Employee_Id = x.HAOA.Employee_ID,
                    Local_Full_Name = x.HEP.Local_Full_Name,
                    Department_Code = x.HAOA.Department,
                    Department_Name = x.HOD_Lang.Department_Name,
                    Department_Code_Name = x.HOD_Lang != null && !string.IsNullOrWhiteSpace(x.HOD_Lang.Department_Name)
                    ? x.HOD_Lang.Department_Code + "-" + x.HOD_Lang.Department_Name : x.HAOA.Department,
                    Work_Shift_Type = x.HAOA.Work_Shift_Type,
                    Work_Shift_Type_Name = x.HBC_WorkShiftType.Code_Name,
                    Work_Shift_Type_Str = x.HBC_WorkShiftType != null && !string.IsNullOrWhiteSpace(x.HBC_WorkShiftType.Code_Name)
                    ? x.HAOA.Work_Shift_Type + "-" + x.HBC_WorkShiftType.Code_Name : x.HAOA.Work_Shift_Type,
                    Overtime_Date_Str = x.HAOA.Overtime_Date.ToString("yyyy/MM/dd"),
                    Clock_In_Str = x.HAWS.Clock_In,
                    Clock_Out_Str = x.HAWS.Clock_Out,
                    Shift_Time_Str = $"{x.HAWS.Clock_In} ~ {x.HAWS.Clock_Out} ",
                    Overtime_Start_Str = x.HAOA.Overtime_Start,
                    Overtime_End_Str = x.HAOA.Overtime_End,
                    Apply_Time_Str = $"{x.HAOA.Overtime_Start} ~ {x.HAOA.Overtime_End}",
                    Overtime_Hours = x.HAOA.Overtime_Hours.ToString(),
                    Night_Hours = x.HAOA.Night_Hours.ToString(),
                    Training_Hours = x.HAOA.Training_Hours.ToString(),
                    Night_Eat_Times = x.HAOA.Night_Eat_Times,
                    Update_By = x.HAOA.Update_By,
                    Update_Time = x.HAOA.Update_Time,
                    Update_Time_Str = x.HAOA.Update_Time.ToString("yyyy/MM/dd HH:mm:ss"),
                }).ToListAsync();
            return result;
        }
        #endregion

        #region Add & Edit & Delete
        public async Task<OperationResult> PostData(OvertimeApplicationMaintenance_Main input, string username)
        {
            var predicate = PredicateBuilder.New<HRMS_Att_Overtime_Application>(true);
            if (string.IsNullOrWhiteSpace(input.USER_GUID)
             || string.IsNullOrWhiteSpace(input.Factory)
             || string.IsNullOrWhiteSpace(input.Employee_Id)
             || string.IsNullOrWhiteSpace(input.Work_Shift_Type)
             || string.IsNullOrWhiteSpace(input.Clock_In)
             || string.IsNullOrWhiteSpace(input.Clock_Out)
             || !input.Overtime_Hours.CheckDecimalValue(10, 5)
             || !input.Night_Hours.CheckDecimalValue(10, 5)
             || !input.Training_Hours.CheckDecimalValue(10, 5)
             || !isTimeString(input.Overtime_Start_Str)
             || !isTimeString(input.Overtime_End_Str)
             || input.Night_Eat_Times < 0
             || string.IsNullOrWhiteSpace(input.Overtime_Date_Str)
             || !DateTime.TryParseExact(input.Overtime_Date_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime overtimeDateValue))
                return new OperationResult(false, "InvalidInput");
            predicate.And(x => x.Factory == input.Factory && x.Employee_ID == input.Employee_Id && x.Overtime_Date.Date == overtimeDateValue.Date);
            if (await _repositoryAccessor.HRMS_Att_Overtime_Application.AnyAsync(predicate))
                return new OperationResult(false, "AlreadyExitedData");
            HRMS_Att_Overtime_Application data = new()
            {
                USER_GUID = input.USER_GUID,
                Factory = input.Factory,
                Overtime_Date = overtimeDateValue,
                Employee_ID = input.Employee_Id,
                Department = input.Department_Code,
                Work_Shift_Type = input.Work_Shift_Type,
                Overtime_Start = input.Overtime_Start_Str,
                Overtime_End = input.Overtime_End_Str,
                Overtime_Hours = decimal.Parse(input.Overtime_Hours),
                Night_Hours = decimal.Parse(input.Night_Hours),
                Training_Hours = decimal.Parse(input.Training_Hours),
                Night_Eat_Times = input.Night_Eat_Times,
                Update_By = username,
                Update_Time = DateTime.Now
            };
            try
            {
                _repositoryAccessor.HRMS_Att_Overtime_Application.Add(data);
                await _repositoryAccessor.Save();
                return new OperationResult(true);
            }
            catch (Exception)
            {
                return new OperationResult(false, "ErrorException");
            }
        }
        public async Task<OperationResult> PutData(OvertimeApplicationMaintenance_Main input, string username)
        {
            var predicate = PredicateBuilder.New<HRMS_Att_Overtime_Application>(true);
            if (string.IsNullOrWhiteSpace(input.USER_GUID)
             || string.IsNullOrWhiteSpace(input.Factory)
             || string.IsNullOrWhiteSpace(input.Employee_Id)
             || string.IsNullOrWhiteSpace(input.Work_Shift_Type)
             || string.IsNullOrWhiteSpace(input.Clock_In)
             || string.IsNullOrWhiteSpace(input.Clock_Out)
             || !isTimeString(input.Overtime_Start_Str)
             || !isTimeString(input.Overtime_End_Str)
             || !input.Overtime_Hours.CheckDecimalValue(10, 5)
             || !input.Night_Hours.CheckDecimalValue(10, 5)
             || !input.Training_Hours.CheckDecimalValue(10, 5)
             || input.Night_Eat_Times < 0
             || string.IsNullOrWhiteSpace(input.Overtime_Date_Str)
             || !DateTime.TryParseExact(input.Overtime_Date_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime overtimeDateValue))
                return new OperationResult(false, "InvalidInput");
            predicate.And(x => x.Factory == input.Factory && x.Employee_ID == input.Employee_Id && x.Overtime_Date.Date == overtimeDateValue.Date);
            var oldData = await _repositoryAccessor.HRMS_Att_Overtime_Application.FirstOrDefaultAsync(predicate);
            if (oldData == null)
                return new OperationResult(false, "NotExitedData");
            oldData.Work_Shift_Type = input.Work_Shift_Type;
            oldData.Overtime_Hours = decimal.Parse(input.Overtime_Hours);
            oldData.Overtime_Start = input.Overtime_Start_Str;
            oldData.Overtime_End = input.Overtime_End_Str;
            oldData.Night_Hours = decimal.Parse(input.Night_Hours);
            oldData.Training_Hours = decimal.Parse(input.Training_Hours);
            oldData.Night_Eat_Times = input.Night_Eat_Times;
            oldData.Update_By = username;
            oldData.Update_Time = DateTime.Now;
            try
            {
                _repositoryAccessor.HRMS_Att_Overtime_Application.Update(oldData);
                await _repositoryAccessor.Save();
                return new OperationResult(true);
            }
            catch (Exception)
            {
                return new OperationResult(false, "ErrorException");
            }
        }
        public async Task<OperationResult> DeleteData(OvertimeApplicationMaintenance_Main data)
        {
            var predicate = PredicateBuilder.New<HRMS_Att_Overtime_Application>(true);
            if (string.IsNullOrWhiteSpace(data.Factory)
             || string.IsNullOrWhiteSpace(data.Employee_Id)
             || string.IsNullOrWhiteSpace(data.Overtime_Date_Str)
             || !DateTime.TryParseExact(data.Overtime_Date_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime overtimeDateValue))
                return new OperationResult(false, "InvalidInput");
            predicate.And(x => x.Factory == data.Factory && x.Employee_ID == data.Employee_Id && x.Overtime_Date.Date == overtimeDateValue.Date);
            var removeData = await _repositoryAccessor.HRMS_Att_Overtime_Application.FirstOrDefaultAsync(predicate);
            if (removeData == null)
                return new OperationResult(false, "NotExitedData");
            try
            {
                _repositoryAccessor.HRMS_Att_Overtime_Application.Remove(removeData);
                await _repositoryAccessor.Save();
                return new OperationResult { IsSuccess = true };
            }
            catch (Exception)
            {
                return new OperationResult { IsSuccess = false };
            }
        }
        #endregion

        #region Excel
        public async Task<OperationResult> DownloadExcelTemplate()
        {
            string path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Resources\\Template\\AttendanceMaintenance\\5_1_12_OvertimeApplicationMaintenance\\Template.xlsx"
            );
            if (!File.Exists(path))
                return await Task.FromResult(new OperationResult(false, "NotExitedFile"));
            byte[] bytes = File.ReadAllBytes(path);
            return await Task.FromResult(new OperationResult { IsSuccess = true, Data = $"data:xlsx;base64,{Convert.ToBase64String(bytes)}" });
        }
        public async Task<OperationResult> UploadExcel(IFormFile file, List<string> role_List, string username)
        {
            ExcelResult resp = ExcelUtility.CheckExcel(
                file,
                "Resources\\Template\\AttendanceMaintenance\\5_1_12_OvertimeApplicationMaintenance\\Template.xlsx"
            );
            if (!resp.IsSuccess)
                return new OperationResult(false, resp.Error);
            List<HRMS_Att_Overtime_Application> excelDataList = new();
            List<OvertimeApplicationMaintenance_Table> excelReportList = new();
            var rolesFactory = _repositoryAccessor.HRMS_Basic_Role.FindAll(x => role_List.Contains(x.Role)).Select(x => x.Factory).Distinct().ToList();
            var codes = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory || x.Type_Seq == BasicCodeTypeConstant.WorkShiftType && x.IsActive).ToList();

            //  Query_EmpPersonal_Add(Factory,Employee_ID)   
            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(true).Select(x => new
            {
                HEP = x,
                Actual_Employee_ID = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Employee_ID : x.Employee_ID,
                Actual_Division = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Division : x.Division,
                Actual_Factory = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Factory : x.Factory,
                Actual_Department = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Department : x.Department,
            });
            var HBFC = _repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(x => x.Kind == "1");
            var employeeInfos = await HEP
                .Join(HBFC,
                    x => new { Division = x.Actual_Division, Factory = x.Actual_Factory },
                    HBFC => new { HBFC.Division, HBFC.Factory },
                    (x, y) => new { x.HEP.USER_GUID, x.HEP.Employment_Status, x.Actual_Employee_ID, x.Actual_Factory, x.Actual_Department })
                .ToListAsync();
            var now = DateTime.Now;
            bool isPassed = true;
            for (int i = resp.WsTemp.Cells.Rows.Count; i < resp.Ws.Cells.Rows.Count; i++)
            {
                bool isKeyPassed = true;
                string errorMessage = "";
                string factory = resp.Ws.Cells[i, 0].StringValue.Trim();
                string overtimeDate = resp.Ws.Cells[i, 1].StringValue.Trim();
                string employeeId = resp.Ws.Cells[i, 2].StringValue.Trim();
                string workShiftType = resp.Ws.Cells[i, 3].StringValue.Trim();
                string overtimeStart = resp.Ws.Cells[i, 4].StringValue.Trim();
                string overtimeEnd = resp.Ws.Cells[i, 5].StringValue.Trim();
                string overtimeHours = resp.Ws.Cells[i, 6].StringValue.Trim();
                string nightHours = resp.Ws.Cells[i, 7].StringValue.Trim();
                string trainingHours = resp.Ws.Cells[i, 8].StringValue.Trim();
                string nightEat = resp.Ws.Cells[i, 9].StringValue.Trim();

                if (string.IsNullOrWhiteSpace(factory))
                {
                    errorMessage += $"column Factory cannot be blank.\n";
                    isKeyPassed = false;
                }
                else
                {
                    if (factory.Length > 10)
                    {
                        errorMessage += $"column Factory's length higher than required.\n";
                        isKeyPassed = false;
                    }
                    if (!codes.Any(x => x.Code == factory && x.Type_Seq == "2"))
                    {
                        errorMessage += $"uploaded [Factory] data is not existed.\n";
                        isKeyPassed = false;
                    }
                    if (!rolesFactory.Contains(factory))
                    {
                        errorMessage += $"uploaded [Factory] data does not match the role group.\n";
                        isKeyPassed = false;
                    }
                }
                if (string.IsNullOrWhiteSpace(overtimeDate))
                {
                    errorMessage += $"column Date cannot be blank.\n";
                    isKeyPassed = false;
                }
                if (!DateTime.TryParseExact(overtimeDate, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime overtimeDateValue))
                {
                    errorMessage += $"uploaded [Date] data with wrong date format (Must be : yyyy/MM/dd).\n";
                    isKeyPassed = false;
                }
                if (string.IsNullOrWhiteSpace(employeeId))
                {
                    errorMessage += $"column Employee_ID cannot be blank.\n";
                    isKeyPassed = false;
                }
                else
                {
                    if (employeeId.Length > 16)
                    {
                        errorMessage += $"column Employee_ID's length higher than required.\n";
                        isKeyPassed = false;
                    }
                }
                if (string.IsNullOrWhiteSpace(workShiftType))
                {
                    errorMessage += $"column Leave_Code cannot be blank.\n";
                    isKeyPassed = false;
                }
                else
                {
                    if (workShiftType.Length > 10)
                    {
                        errorMessage += $"column Work Shift Type_Code's length higher than required.\n";
                        isKeyPassed = false;
                    }
                    if (!codes.Any(x => x.Code == workShiftType && x.Type_Seq == BasicCodeTypeConstant.WorkShiftType))
                    {
                        errorMessage += $"uploaded [Work Shift Type_Code] data is not existed.\n";
                        isKeyPassed = false;
                    }
                }
                if (string.IsNullOrWhiteSpace(overtimeStart))
                    errorMessage += $"column Apply Time Start cannot be blank.\n";
                else
                {
                    if (overtimeStart.Length != 4)
                        errorMessage += $"column Apply Time Start's length must be 4.\n";
                    else
                    {
                        if (!overtimeStart.All(char.IsNumber))
                            errorMessage += $"column Apply Time Start's value must contain number only.\n";
                        else
                        {
                            var hour = int.Parse(overtimeStart[..2]);
                            var minute = int.Parse(overtimeStart[2..]);
                            if (hour < 0 || hour >= 24 || minute < 0 || minute >= 60)
                                errorMessage += $"uploaded [Apply Time Start] data with wrong format (Must be : HHmm).\n";
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(overtimeEnd))
                    errorMessage += $"column Apply Time End cannot be blank.\n";
                else
                {
                    if (overtimeEnd.Length != 4)
                        errorMessage += $"column Apply Time End's length must be 4.\n";
                    else
                    {
                        if (!overtimeEnd.All(char.IsNumber))
                            errorMessage += $"column Apply Time End's value must contain number only.\n";
                        else
                        {
                            var hour = int.Parse(overtimeEnd[..2]);
                            var minute = int.Parse(overtimeEnd[2..]);
                            if (hour < 0 || hour >= 24 || minute < 0 || minute >= 60)
                                errorMessage += $"uploaded [Apply Time End] data with wrong format (Must be : HHmm).\n";
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(overtimeHours))
                    errorMessage += $"column Overtime Hour(s) cannot be blank.\n";
                if (!overtimeHours.CheckDecimalValue(10, 5))
                    errorMessage += $"uploaded [Overtime Hour(s)] data with wrong format.\n";
                if (string.IsNullOrWhiteSpace(nightHours))
                    errorMessage += $"column Night Hour(s) cannot be blank.\n";
                if (!nightHours.CheckDecimalValue(10, 5))
                    errorMessage += $"uploaded [Night Hour(s)] data with wrong format.\n";
                if (string.IsNullOrWhiteSpace(trainingHours))
                    errorMessage += $"column Training Hour(s) cannot be blank.\n";
                if (!trainingHours.CheckDecimalValue(10, 5))
                    errorMessage += $"uploaded [Training Hour(s)] data with wrong format.\n";
                if (string.IsNullOrWhiteSpace(nightEat))
                    errorMessage += $"column Night Eat cannot be blank.\n";
                if (!int.TryParse(nightEat, out int nightEatValue))
                    errorMessage += $"uploaded [Night Eat] data with wrong format.\n";

                var empInfo = employeeInfos.FirstOrDefault(x => x.Actual_Factory == factory && x.Actual_Employee_ID == employeeId);
                if (empInfo == null && !string.IsNullOrWhiteSpace(factory) && !string.IsNullOrWhiteSpace(employeeId))
                    if (empInfo.Employment_Status == "A" || empInfo.Employment_Status == "S")
                        errorMessage += $"Employee Information data is not existed. (Assigned/Supported)\n";
                    else
                        errorMessage += $"Employee Information data is not existed.\n";
                if (isKeyPassed)
                {
                    if (_repositoryAccessor.HRMS_Att_Overtime_Application.Any(x => x.Factory == factory && x.Employee_ID == employeeId && x.Overtime_Date.Date == overtimeDateValue))
                        errorMessage += $"Data is already existed.\n";
                    if (excelReportList.Any(x => x.Factory == factory && x.Employee_Id == employeeId && x.Overtime_Date.Date == overtimeDateValue.Date))
                        errorMessage += $"Identity Conflict Data.\n";
                }
                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    HRMS_Att_Overtime_Application excelData = new()
                    {
                        USER_GUID = empInfo.USER_GUID,
                        Factory = factory,
                        Overtime_Date = overtimeDateValue,
                        Employee_ID = employeeId,
                        Department = empInfo.Actual_Department,
                        Work_Shift_Type = workShiftType,
                        Overtime_Start = overtimeStart,
                        Overtime_End = overtimeEnd,
                        Overtime_Hours = decimal.Parse(overtimeHours),
                        Night_Hours = decimal.Parse(nightHours),
                        Training_Hours = decimal.Parse(trainingHours),
                        Night_Eat_Times = nightEatValue,
                        Update_By = username,
                        Update_Time = now
                    };
                    excelDataList.Add(excelData);
                }
                else
                {
                    isPassed = false;
                    errorMessage = errorMessage.Remove(errorMessage.Length - 1);
                }
                OvertimeApplicationMaintenance_Table report = new()
                {
                    Factory = factory,
                    Overtime_Date = overtimeDateValue,
                    Employee_Id = employeeId,
                    Work_Shift_Type = workShiftType,
                    Overtime_Start = overtimeStart,
                    Overtime_End = overtimeEnd,
                    Overtime_Hours = overtimeHours,
                    Night_Hours = nightHours,
                    Training_Hours = trainingHours,
                    Night_Eat_Times = nightEat,
                    Error_Message = errorMessage
                };
                excelReportList.Add(report);
            }
            if (!isPassed)
            {
                MemoryStream memoryStream = new();
                string fileLocation = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Resources\\Template\\AttendanceMaintenance\\5_1_12_OvertimeApplicationMaintenance\\Report.xlsx"
                );
                WorkbookDesigner workbookDesigner = new() { Workbook = new Workbook(fileLocation) };
                Worksheet worksheet = workbookDesigner.Workbook.Worksheets[0];
                workbookDesigner.SetDataSource("result", excelReportList);
                workbookDesigner.Process();
                worksheet.AutoFitColumns(worksheet.Cells.MinDataColumn, worksheet.Cells.MaxColumn);
                worksheet.AutoFitRows(worksheet.Cells.MinDataRow + 1, worksheet.Cells.MaxRow);
                workbookDesigner.Workbook.Save(memoryStream, SaveFormat.Xlsx);
                return new OperationResult { IsSuccess = false, Data = memoryStream.ToArray(), Error = "Please check downloaded Error Report" };
            }
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                if (excelDataList.Any())
                {
                    _repositoryAccessor.HRMS_Att_Overtime_Application.AddMultiple(excelDataList);
                    await _repositoryAccessor.Save();
                    string path = "uploaded\\AttendanceMaintenance\\5_1_12_OvertimeApplicationMaintenance";
                    await FilesUtility.SaveFile(file, path, $"OvertimeApplicationMaintenance_{DateTime.Now:yyyyMMddHHmmss}");
                }
                await _repositoryAccessor.CommitAsync();
                return new OperationResult { IsSuccess = true };
            }
            catch (Exception)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult { IsSuccess = false };
            }
        }
        public async Task<OperationResult> ExportExcel(OvertimeApplicationMaintenance_Param param)
        {
            List<OvertimeApplicationMaintenance_Main> data = await GetData(param);
            if (!data.Any()) return new OperationResult(false, "No Data");

            // xử lí report data 
            var dataTables = new List<Table>() { new("result", data) };

            // Thông tin print [Factory, PrintBy,  PrintDay]
            var dataCells = new List<SDCores.Cell>(){
                new("B1", param.Factory),
                new("E1", param.UserName),
                new("G1", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))
            };

            ConfigDownload config = new() { IsAutoFitColumn = true };
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataTables, dataCells,
                "Resources\\Template\\AttendanceMaintenance\\5_1_12_OvertimeApplicationMaintenance\\Download.xlsx",
                config
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }
        #endregion

        private static bool isTimeString(string time)
        {
            if (string.IsNullOrWhiteSpace(time) || time.Length != 4)
                return false;
            var hour = int.Parse(time[..2]);
            var minute = int.Parse(time[2..]);
            if (hour < 0 || hour >= 24 || minute < 0 || minute >= 60)
                return false;
            return true;
        }

        public async Task<OperationResult> GetOvertimeParam(OvertimeApplicationMaintenance_Param param)
        {
            var predicate = PredicateBuilder.New<HRMS_Att_Overtime_Parameter>(true);
            if (string.IsNullOrWhiteSpace(param.Factory)
             || string.IsNullOrWhiteSpace(param.Work_Shift_Type)
             || string.IsNullOrWhiteSpace(param.Overtime_Date_From_Str)
             || string.IsNullOrWhiteSpace(param.Overtime_Date_To_Str)
             || string.IsNullOrWhiteSpace(param.Overtime_Date_Str)
             || !DateTime.TryParseExact(param.Overtime_Date_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime overtimeDateValue))
                return new OperationResult(false, "InvalidInput");
            predicate.And(x =>
                x.Factory == param.Factory &&
                x.Work_Shift_Type == param.Work_Shift_Type &&
                x.Effective_Month.Date <= overtimeDateValue.Date &&
                x.Overtime_Start == param.Overtime_Date_From_Str &&
                x.Overtime_End == param.Overtime_Date_To_Str
            );
            if (!await _repositoryAccessor.HRMS_Att_Overtime_Parameter.AnyAsync(predicate))
                return new OperationResult(false);
            var data = await _repositoryAccessor.HRMS_Att_Overtime_Parameter.FindAll(predicate).OrderByDescending(x => x.Effective_Month).FirstOrDefaultAsync();
            return new OperationResult(true, data);
        }
    }
}