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
    public class S_5_1_11_LeaveApplicationMaintenance : BaseServices, I_5_1_11_LeaveApplicationMaintenance
    {
        public S_5_1_11_LeaveApplicationMaintenance(DBContext dbContext) : base(dbContext)
        {
        }
        #region Dropdown List
        public async Task<List<KeyValuePair<string, string>>> GetDropDownList(LeaveApplicationMaintenance_Param param, List<string> roleList)
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
            result.AddRange(data.Where(x => x.hbc.Type_Seq == "40" && x.hbc.Char1 == "Leave" && x.hbc.IsActive).Select(x => new KeyValuePair<string, string>("LE", $"{x.hbc.Code}-{(x.hbcl != null ? x.hbcl.Code_Name : x.hbc.Code_Name)}")).Distinct().ToList());
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
        #endregion

        #region Check Data
        public async Task<OperationResult> IsExistedData(LeaveApplicationMaintenance_Param param)
        {
            OperationResult result = new();
            var predicate = PredicateBuilder.New<HRMS_Att_Leave_Application>(true);
            if (string.IsNullOrWhiteSpace(param.Factory)
             || string.IsNullOrWhiteSpace(param.Employee_Id)
             || string.IsNullOrWhiteSpace(param.Leave)
             || string.IsNullOrWhiteSpace(param.Leave_Date_From_Str)
             || !DateTime.TryParseExact(param.Leave_Date_From_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime leaveStartDateValue))
                return new OperationResult(false);
            predicate.And(x => x.Factory == param.Factory && x.Employee_ID == param.Employee_Id && x.Leave_code == param.Leave && x.Leave_Start.Date == leaveStartDateValue.Date);
            var isExisted = await _repositoryAccessor.HRMS_Att_Leave_Application.AnyAsync(predicate);
            return new OperationResult(isExisted);
        }
        #endregion

        #region Query Data
        public async Task<PaginationUtility<LeaveApplicationMaintenance_Main>> GetSearchDetail(PaginationParam paginationParams, LeaveApplicationMaintenance_Param searchParam)
        {
            List<LeaveApplicationMaintenance_Main> result = await GetData(searchParam);
            return PaginationUtility<LeaveApplicationMaintenance_Main>.Create(result, paginationParams.PageNumber, paginationParams.PageSize);
        }
        private async Task<List<LeaveApplicationMaintenance_Main>> GetData(LeaveApplicationMaintenance_Param searchParam)
        {
            var predicate = PredicateBuilder.New<HRMS_Att_Leave_Application>(x => x.Factory == searchParam.Factory);
            if (!string.IsNullOrWhiteSpace(searchParam.Department))
                predicate.And(x => x.Department == searchParam.Department);
            if (!string.IsNullOrWhiteSpace(searchParam.Employee_Id))
                predicate.And(x => x.Employee_ID.Contains(searchParam.Employee_Id));
            if (!string.IsNullOrWhiteSpace(searchParam.Leave))
                predicate.And(x => x.Leave_code == searchParam.Leave);
            if (!string.IsNullOrWhiteSpace(searchParam.Leave_Date_From_Str) && DateTime.TryParseExact(searchParam.Leave_Date_From_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime fromDate)
             && !string.IsNullOrWhiteSpace(searchParam.Leave_Date_To_Str) && DateTime.TryParseExact(searchParam.Leave_Date_To_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime toDate))
            {
                predicate.And(x => (fromDate.Date <= x.Leave_Start.Date && x.Leave_End.Date <= toDate.Date) ||
                                    (fromDate.Date <= x.Leave_Start.Date && x.Leave_Start.Date <= toDate.Date) ||
                                    (fromDate.Date <= x.Leave_End.Date && x.Leave_End.Date <= toDate.Date));
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(searchParam.Leave_Date_From_Str) && DateTime.TryParseExact(searchParam.Leave_Date_From_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime dateFromValue))
                    predicate.And(x => x.Leave_Start.Date >= dateFromValue.Date);
                if (!string.IsNullOrWhiteSpace(searchParam.Leave_Date_To_Str) && DateTime.TryParseExact(searchParam.Leave_Date_To_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime dateToValue))
                    predicate.And(x => x.Leave_End.Date <= dateToValue.Date);
            }
            var permissionGroupQuery = _repositoryAccessor.HRMS_Basic_Role.FindAll(x => x.Factory == searchParam.Factory, true).Select(x => x.Permission_Group);
            var HALA = _repositoryAccessor.HRMS_Att_Leave_Application.FindAll(predicate);
            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => (x.Factory == searchParam.Factory) && permissionGroupQuery.Contains(x.Permission_Group));
            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll();
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == searchParam.Lang.ToLower());
            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Leave & x.IsActive);
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == searchParam.Lang.ToLower());
            var HOD_Lang = HOD.GroupJoin(HODL,
                    x => new { x.Factory, x.Division, x.Department_Code },
                    y => new { y.Factory, y.Division, y.Department_Code },
                    (x, y) => new { HOD = x, HODL = y })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, HODL = y })
                .Select(x => new
                {
                    x.HOD.Factory,
                    x.HOD.Department_Code,
                    Department_Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name
                });
            var HBC_LeaveCode = HBC
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

            var result = await HALA
                .Join(HEP,
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { HALA = x, HEP = y })
                .GroupJoin(HBC_LeaveCode,
                    x => x.HALA.Leave_code,
                    y => y.Code,
                    (x, y) => new { x.HALA, x.HEP, HBC_LeaveCode = y })
                .SelectMany(x => x.HBC_LeaveCode.DefaultIfEmpty(),
                    (x, y) => new { x.HALA, x.HEP, HBC_LeaveCode = y })
                .GroupJoin(HOD_Lang,
                    x => new { x.HALA.Factory, Department_Code = x.HALA.Department },
                    y => new { y.Factory, y.Department_Code },
                    (x, y) => new { x.HALA, x.HEP, x.HBC_LeaveCode, HOD_Lang = y })
                .SelectMany(x => x.HOD_Lang.DefaultIfEmpty(),
                    (x, y) => new { x.HALA, x.HEP, x.HBC_LeaveCode, HOD_Lang = y })
                .Select(x => new LeaveApplicationMaintenance_Main
                {
                    Factory = x.HALA.Factory,
                    USER_GUID = x.HALA.USER_GUID,
                    Employee_Id = x.HALA.Employee_ID,
                    Local_Full_Name = x.HEP.Local_Full_Name,
                    Department_Code = x.HALA.Department,
                    Department_Name = x.HOD_Lang.Department_Name,
                    Department_Code_Name = x.HOD_Lang != null && !string.IsNullOrWhiteSpace(x.HOD_Lang.Department_Name)
                        ? x.HOD_Lang.Department_Code + "-" + x.HOD_Lang.Department_Name : x.HALA.Department,
                    Leave_Code = x.HALA.Leave_code,
                    Leave_Name = x.HBC_LeaveCode.Code_Name,
                    Leave_Str = x.HBC_LeaveCode != null && !string.IsNullOrWhiteSpace(x.HBC_LeaveCode.Code_Name)
                        ? x.HALA.Leave_code + "-" + x.HBC_LeaveCode.Code_Name : x.HALA.Leave_code,
                    Leave_Start = x.HALA.Leave_Start.ToString("yyyy/MM/dd"),
                    Min_Start = x.HALA.Min_Start,
                    Leave_Start_Str = $"{x.HALA.Leave_Start:yyyy/MM/dd} {x.HALA.Min_Start}",
                    Leave_End = x.HALA.Leave_End.ToString("yyyy/MM/dd"),
                    Min_End = x.HALA.Min_End,
                    Leave_End_Str = $"{x.HALA.Leave_End:yyyy/MM/dd} {x.HALA.Min_End}",
                    Days = x.HALA.Days.ToString(),
                    Update_By = x.HALA.Update_By,
                    Update_Time = x.HALA.Update_Time,
                    Update_Time_Str = x.HALA.Update_Time.ToString("yyyy/MM/dd HH:mm:ss"),
                }).ToListAsync();
            return result;
        }
        #endregion

        #region Add & Edit & Delete
        public async Task<OperationResult> PostData(LeaveApplicationMaintenance_Main input, string username)
        {
            List<HRMS_Att_Pregnancy_Data> updateList = new();
            var predicate = PredicateBuilder.New<HRMS_Att_Leave_Application>(true);
            if (string.IsNullOrWhiteSpace(input.USER_GUID)
             || string.IsNullOrWhiteSpace(input.Factory)
             || string.IsNullOrWhiteSpace(input.Employee_Id)
             || string.IsNullOrWhiteSpace(input.Leave_Code)
             || string.IsNullOrWhiteSpace(input.Min_Start)
             || string.IsNullOrWhiteSpace(input.Min_End)
             || string.IsNullOrWhiteSpace(input.Days)
             || string.IsNullOrWhiteSpace(input.Leave_Start)
             || !DateTime.TryParseExact(input.Leave_Start, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime leaveStartDateValue)
             || string.IsNullOrWhiteSpace(input.Leave_End)
             || !DateTime.TryParseExact(input.Leave_End, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime leaveEndDateValue))
                return new OperationResult(false, "InvalidInput");
            predicate.And(x => x.Factory == input.Factory && x.Employee_ID == input.Employee_Id && x.Leave_code == input.Leave_Code && x.Leave_Start.Date == leaveStartDateValue.Date);
            if (await _repositoryAccessor.HRMS_Att_Leave_Application.AnyAsync(predicate))
                return new OperationResult(false, "AlreadyExitedData");
            HRMS_Att_Leave_Application addData = new()
            {
                USER_GUID = input.USER_GUID,
                Factory = input.Factory,
                Employee_ID = input.Employee_Id,
                Department = input.Department_Code,
                Leave_code = input.Leave_Code,
                Leave_Start = leaveStartDateValue,
                Min_Start = input.Min_Start,
                Leave_End = leaveEndDateValue,
                Min_End = input.Min_End,
                Days = decimal.Parse(input.Days),
                Update_By = username,
                Update_Time = DateTime.Now
            };
            if (addData.Leave_code == "G0")
            {
                var pregnancyData = _repositoryAccessor.HRMS_Att_Pregnancy_Data.FindAll(x => x.Factory == addData.Factory && x.Employee_ID == addData.Employee_ID).ToList();
                if (pregnancyData.Any())
                {
                    foreach (var pregnancyItem in pregnancyData)
                    {
                        if (pregnancyItem.Maternity_Start == null && pregnancyItem.Maternity_End == null && pregnancyItem.GoWork_Date == null
                        && pregnancyItem.Due_Date.Date >= leaveStartDateValue.Date && pregnancyItem.Due_Date.Date <= leaveEndDateValue.Date
                        && pregnancyItem.Close_Case != true)
                        {
                            pregnancyItem.Maternity_Start = leaveStartDateValue;
                            pregnancyItem.Maternity_End = leaveEndDateValue;
                            pregnancyItem.GoWork_Date = leaveEndDateValue.AddDays(1);
                            pregnancyItem.Update_Time = DateTime.Now;
                            pregnancyItem.Update_By = username;
                            updateList.Add(pregnancyItem);
                        }
                    }
                }
            }
            try
            {
                _repositoryAccessor.HRMS_Att_Leave_Application.Add(addData);
                if (updateList.Any())
                    _repositoryAccessor.HRMS_Att_Pregnancy_Data.UpdateMultiple(updateList);
                await _repositoryAccessor.Save();
                return new OperationResult(true);
            }
            catch (Exception)
            {
                return new OperationResult(false, "ErrorException");
            }
        }
        public async Task<OperationResult> PutData(LeaveApplicationMaintenance_Main input, string username)
        {
            var predicate = PredicateBuilder.New<HRMS_Att_Leave_Application>(true);
            if (string.IsNullOrWhiteSpace(input.USER_GUID)
             || string.IsNullOrWhiteSpace(input.Factory)
             || string.IsNullOrWhiteSpace(input.Employee_Id)
             || string.IsNullOrWhiteSpace(input.Leave_Code)
             || string.IsNullOrWhiteSpace(input.Min_Start)
             || string.IsNullOrWhiteSpace(input.Min_End)
             || string.IsNullOrWhiteSpace(input.Days)
             || string.IsNullOrWhiteSpace(input.Leave_Start)
             || !DateTime.TryParseExact(input.Leave_Start, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime leaveStartDateValue)
             || string.IsNullOrWhiteSpace(input.Leave_End)
             || !DateTime.TryParseExact(input.Leave_End, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime leaveEndDateValue))
                return new OperationResult(false, "InvalidInput");
            predicate.And(x => x.Factory == input.Factory && x.Employee_ID == input.Employee_Id && x.Leave_code == input.Leave_Code && x.Leave_Start.Date == leaveStartDateValue.Date);
            var oldData = await _repositoryAccessor.HRMS_Att_Leave_Application.FirstOrDefaultAsync(predicate);
            if (oldData == null)
                return new OperationResult(false, "NotExitedData");
            oldData.Leave_End = leaveEndDateValue;
            oldData.Min_End = input.Min_End;
            oldData.Days = decimal.Parse(input.Days);
            oldData.Update_By = username;
            oldData.Update_Time = DateTime.Now;
            try
            {
                _repositoryAccessor.HRMS_Att_Leave_Application.Update(oldData);
                await _repositoryAccessor.Save();
                return new OperationResult(true);
            }
            catch (Exception)
            {
                return new OperationResult(false, "ErrorException");
            }
        }
        public async Task<OperationResult> DeleteData(LeaveApplicationMaintenance_Main data)
        {
            var predicate = PredicateBuilder.New<HRMS_Att_Leave_Application>(true);
            if (string.IsNullOrWhiteSpace(data.Factory)
             || string.IsNullOrWhiteSpace(data.Employee_Id)
             || string.IsNullOrWhiteSpace(data.Leave_Code)
             || string.IsNullOrWhiteSpace(data.Leave_Start)
             || !DateTime.TryParseExact(data.Leave_Start, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime leaveStartDateValue))
                return new OperationResult(false, "InvalidInput");
            predicate.And(x => x.Factory == data.Factory && x.Employee_ID == data.Employee_Id && x.Leave_code == data.Leave_Code && x.Leave_Start.Date == leaveStartDateValue.Date);
            var removeData = await _repositoryAccessor.HRMS_Att_Leave_Application.FirstOrDefaultAsync(predicate);
            if (removeData == null)
                return new OperationResult(false, "NotExitedData");
            try
            {
                _repositoryAccessor.HRMS_Att_Leave_Application.Remove(removeData);
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
                "Resources\\Template\\AttendanceMaintenance\\5_1_11_LeaveApplicationMaintenance\\Template.xlsx"
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
                "Resources\\Template\\AttendanceMaintenance\\5_1_11_LeaveApplicationMaintenance\\Template.xlsx"
            );
            if (!resp.IsSuccess)
                return new OperationResult(false, resp.Error);
            List<HRMS_Att_Leave_Application> excelDataList = new();
            List<LeaveApplicationMaintenance_Table> excelReportList = new();
            List<HRMS_Att_Pregnancy_Data> updateList = new();
            var rolesFactory = _repositoryAccessor.HRMS_Basic_Role.FindAll(x => role_List.Contains(x.Role)).Select(x => x.Factory).Distinct().ToList();
            var codes = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "2" || (x.Type_Seq == "40" && x.Char1 == "Leave" && x.IsActive)).ToList();
            var employeeInfos = _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => rolesFactory.Contains(x.Factory)).ToList();
            bool isPassed = true;
            for (int i = resp.WsTemp.Cells.Rows.Count; i < resp.Ws.Cells.Rows.Count; i++)
            {
                bool isKeyPassed = true;
                string errorMessage = "";
                string factory = resp.Ws.Cells[i, 0].StringValue.Trim();
                string employeeId = resp.Ws.Cells[i, 1].StringValue.Trim();
                string leaveCode = resp.Ws.Cells[i, 2].StringValue.Trim();
                string leaveStart = resp.Ws.Cells[i, 3].StringValue.Trim();
                string minStart = resp.Ws.Cells[i, 4].StringValue.Trim();
                string leaveEnd = resp.Ws.Cells[i, 5].StringValue.Trim();
                string minEnd = resp.Ws.Cells[i, 6].StringValue.Trim();
                string days = resp.Ws.Cells[i, 7].StringValue.Trim();
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
                if (string.IsNullOrWhiteSpace(leaveCode))
                {
                    errorMessage += $"column Leave_Code cannot be blank.\n";
                    isKeyPassed = false;
                }
                else
                {
                    if (leaveCode.Length > 10)
                    {
                        errorMessage += $"column Leave_Code's length higher than required.\n";
                        isKeyPassed = false;
                    }
                    if (!codes.Any(x => x.Code == leaveCode && x.Type_Seq == "40"))
                    {
                        errorMessage += $"uploaded [Leave_Code] data is not existed.\n";
                        isKeyPassed = false;
                    }
                }
                if (string.IsNullOrWhiteSpace(leaveStart))
                {
                    errorMessage += $"column Leave_Start cannot be blank.\n";
                    isKeyPassed = false;
                }
                if (!DateTime.TryParseExact(leaveStart, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime leaveStartDateValue))
                {
                    errorMessage += $"uploaded [Leave_Start] data with wrong date format (Must be : yyyy/MM/dd).\n";
                    isKeyPassed = false;
                }
                if (string.IsNullOrWhiteSpace(minStart))
                    errorMessage += $"column Min_Start cannot be blank.\n";
                else
                {
                    if (minStart.Length != 4)
                        errorMessage += $"column Min_Start's length must be 4.\n";
                    else
                    {
                        if (!minStart.All(char.IsNumber))
                            errorMessage += $"column Min_Start's value must contain number only.\n";
                        else
                        {
                            var hour = int.Parse(minStart[..2]);
                            var minute = int.Parse(minStart[2..]);
                            if (hour < 0 || hour >= 24 || minute < 0 || minute >= 60)
                                errorMessage += $"uploaded [Min_Start] data with wrong format (Must be : HHmm).\n";
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(leaveEnd))
                    errorMessage += $"column Leave_End cannot be blank.\n";
                if (!DateTime.TryParseExact(leaveEnd, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime leaveEndDateValue))
                    errorMessage += $"uploaded [Leave_End] data with wrong date format (Must be : yyyy/MM/dd).\n";
                if (string.IsNullOrWhiteSpace(minEnd))
                    errorMessage += $"column Min_End cannot be blank.\n";
                else
                {
                    if (minEnd.Length != 4)
                        errorMessage += $"column Min_End's length must be 4.\n";
                    else
                    {
                        if (!minEnd.All(char.IsNumber))
                            errorMessage += $"column Min_End's value must contain number only.\n";
                        else
                        {
                            var hour = int.Parse(minEnd[..2]);
                            var minute = int.Parse(minEnd[2..]);
                            if (hour < 0 || hour >= 24 || minute < 0 || minute >= 60)
                                errorMessage += $"uploaded [Min_End] data with wrong format (Must be : HHmm).\n";
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(days))
                    errorMessage += $"column Days cannot be blank.\n";
                if (!days.CheckDecimalValue(10, 5))
                    errorMessage += $"uploaded [Days] data with wrong format.\n";
                var empInfo = employeeInfos.FirstOrDefault(x => x.Factory == factory && x.Employee_ID == employeeId);
                if (empInfo == null && !string.IsNullOrWhiteSpace(factory) && !string.IsNullOrWhiteSpace(employeeId))
                    errorMessage += $"Employee Information data is not existed.\n";
                if (isKeyPassed)
                {
                    if (_repositoryAccessor.HRMS_Att_Leave_Application.Any(x => x.Factory == factory && x.Employee_ID == employeeId && x.Leave_code == leaveCode && x.Leave_Start.Date == leaveStartDateValue.Date))
                        errorMessage += $"Data is already existed.\n";
                    if (excelReportList.Any(x => x.Factory == factory && x.Employee_Id == employeeId && x.Leave_Code == leaveCode && x.Leave_Start == leaveStart))
                        errorMessage += $"Identity Conflict Data.\n";
                }
                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    HRMS_Att_Leave_Application excelData = new()
                    {
                        USER_GUID = empInfo.USER_GUID,
                        Factory = factory,
                        Employee_ID = employeeId,
                        Leave_code = leaveCode,
                        Leave_Start = leaveStartDateValue,
                        Min_Start = minStart,
                        Leave_End = leaveEndDateValue,
                        Min_End = minEnd,
                        Days = decimal.Parse(days),
                        Update_By = username,
                        Update_Time = DateTime.Now
                    };
                    excelDataList.Add(excelData);
                    if (leaveCode == "G0")
                    {
                        var pregnancyData = _repositoryAccessor.HRMS_Att_Pregnancy_Data.FindAll(x => x.Factory == factory && x.Employee_ID == employeeId).ToList();
                        if (pregnancyData.Any())
                        {
                            foreach (var pregnancyItem in pregnancyData)
                            {
                                if (pregnancyItem.Maternity_Start == null && pregnancyItem.Maternity_End == null && pregnancyItem.GoWork_Date == null
                                && pregnancyItem.Due_Date.Date >= leaveStartDateValue.Date && pregnancyItem.Due_Date.Date <= leaveEndDateValue.Date
                                && pregnancyItem.Close_Case != true)
                                {
                                    pregnancyItem.Maternity_Start = leaveStartDateValue;
                                    pregnancyItem.Maternity_End = leaveEndDateValue;
                                    pregnancyItem.GoWork_Date = leaveEndDateValue.AddDays(1);
                                    pregnancyItem.Update_Time = DateTime.Now;
                                    pregnancyItem.Update_By = username;
                                    updateList.Add(pregnancyItem);
                                }
                            }
                        }
                    }
                }
                else
                {
                    isPassed = false;
                    errorMessage = errorMessage.Remove(errorMessage.Length - 1);
                }
                LeaveApplicationMaintenance_Table report = new()
                {
                    Factory = factory,
                    Employee_Id = employeeId,
                    Leave_Code = leaveCode,
                    Leave_Start = leaveStart,
                    Min_Start = minStart,
                    Leave_End = leaveEnd,
                    Min_End = minEnd,
                    Days = days,
                    Error_Message = errorMessage
                };
                excelReportList.Add(report);
            }
            if (!isPassed)
            {
                MemoryStream memoryStream = new();
                string fileLocation = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Resources\\Template\\AttendanceMaintenance\\5_1_11_LeaveApplicationMaintenance\\Report.xlsx"
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
                    if (updateList.Any())
                        _repositoryAccessor.HRMS_Att_Pregnancy_Data.UpdateMultiple(updateList);
                    _repositoryAccessor.HRMS_Att_Leave_Application.AddMultiple(excelDataList);
                    await _repositoryAccessor.Save();
                    string path = "uploaded\\AttendanceMaintenance\\5_1_11_LeaveApplicationMaintenance";
                    await FilesUtility.SaveFile(file, path, $"LeaveApplicationMaintenance_{DateTime.Now:yyyyMMddHHmmss}");
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
        public async Task<OperationResult> ExportExcel(LeaveApplicationMaintenance_Param param)
        {
            List<LeaveApplicationMaintenance_Main> data = await GetData(param);
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
                dataTables,
                dataCells,
                "Resources\\Template\\AttendanceMaintenance\\5_1_11_LeaveApplicationMaintenance\\Download.xlsx",
                config
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }
        #endregion

    }
}