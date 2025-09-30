using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance;
public class S_5_1_19_LeaveRecordModificationMaintenance : BaseServices, I_5_1_19_LeaveRecordModificationMaintenance
{
    public S_5_1_19_LeaveRecordModificationMaintenance(DBContext dbContext) : base(dbContext)
    { }
    #region AddAsync
    public async Task<OperationResult> AddAsync(Leave_Record_Modification_MaintenanceDto param)
    {
        await _repositoryAccessor.BeginTransactionAsync();
        try
        {
            DateTime leave_Date = Convert.ToDateTime(param.Leave_Date_Str);
            if (await _repositoryAccessor.HRMS_Att_Leave_Maintain.AnyAsync(x =>
                x.Factory == param.Factory &&
                x.Leave_code == param.Leave_Code &&
                x.Leave_Date.Date == leave_Date.Date &&
                x.Employee_ID == param.Employee_ID))
                return new OperationResult(false, string.Format(
                   "Data Att_Leave_Maintain already exists: \nFactory: {0}\nDate: {1:yyyy/MM/dd}\nEmployee ID: {2}",
                   param.Factory, leave_Date, param.Employee_ID));
            /* Nếu loại nghỉ phép là G2 (khám thai)
            * ghi 6.4.5.6 Nghỉ phép theo ngày khám thai (Ngày nghỉ khám thai) HRMS_Att_Pregnancy_Data .Leave_Date theo ngày nghỉ phép.
            *  Nếu số ngày nghỉ vượt quá 1 ngày sẽ được tính vào ngày nghỉ 2, 3, 4, 5 theo thứ tự.
            */
            if (param.Work_Shift_Type == "G2")
            {
                // MAX(Due_Date) 
                var pregnancyData = await _repositoryAccessor.HRMS_Att_Pregnancy_Data
                    .FindAll(x => x.Factory == param.Factory && x.Employee_ID == param.Employee_ID && x.Close_Case == false)
                    .OrderByDescending(x => x.Due_Date.Date)
                    .FirstOrDefaultAsync();
                if (pregnancyData is not null)
                {
                    if (pregnancyData.Leave_Date2 is null)
                        pregnancyData.Leave_Date2 = leave_Date;
                    else if (pregnancyData.Leave_Date3 is null)
                        pregnancyData.Leave_Date3 = leave_Date;
                    else if (pregnancyData.Leave_Date4 is null)
                        pregnancyData.Leave_Date4 = leave_Date;
                    else pregnancyData.Leave_Date5 ??= leave_Date;
                }
            }
            if (await _repositoryAccessor.HRMS_Basic_Code.AnyAsync(x =>
                x.Type_Seq == BasicCodeTypeConstant.Leave &&
                x.Char1 == "Leave" &&
                x.Code == param.Leave_Code))
            {
                // System background save the  HRMS_Att_Yearly
                OperationResult updateYearlyResult = await Upd_HRMS_Att_Yearly(
                    param.Update_By, Convert.ToDateTime(param.Update_Time_Str), param.USER_GUID, param.Factory,
                    param.Employee_ID, leave_Date, param.Leave_Code, param.Days
                );
                if (!updateYearlyResult.IsSuccess)
                {
                    await _repositoryAccessor.RollbackAsync();
                    return new OperationResult(false, updateYearlyResult.Error);
                }
            }
            HRMS_Att_Leave_Maintain dataAdd = new()
            {
                USER_GUID = param.USER_GUID,
                Factory = param.Factory,
                Employee_ID = param.Employee_ID,
                Department = param.Department_Code,
                Work_Shift_Type = param.Work_Shift_Type,
                Leave_code = param.Leave_Code,
                Leave_Date = leave_Date,
                Days = param.Days,
                Update_By = param.Update_By,
                Update_Time = Convert.ToDateTime(param.Update_Time_Str)
            };
            _repositoryAccessor.HRMS_Att_Leave_Maintain.Add(dataAdd);
            await _repositoryAccessor.Save();
            await _repositoryAccessor.CommitAsync();
            return new OperationResult(true, "Create successfully");
        }
        catch (Exception ex)
        {
            await _repositoryAccessor.RollbackAsync();
            return new OperationResult(false, $"Inner exception: {ex.InnerException?.Message ?? "No inner exception message available"}");
        }
    }
    #endregion

    #region UpdateAsync
    public async Task<OperationResult> UpdateAsync(Leave_Record_Modification_MaintenanceDto param)
    {
        await _repositoryAccessor.BeginTransactionAsync();
        try
        {
            DateTime leave_Date = Convert.ToDateTime(param.Leave_Date_Str);
            HRMS_Att_Leave_Maintain data = await _repositoryAccessor.HRMS_Att_Leave_Maintain.FirstOrDefaultAsync(r =>
                r.Factory == param.Factory &&
                r.Employee_ID == param.Employee_ID &&
                r.Leave_code == param.Leave_Code &&
                r.Leave_Date.Date == leave_Date.Date);
            if (data is null)
                return new OperationResult(false, string.Format(
                    "Update HRMS_Att_Leave_Maintain Fail! Record not found: \nFactory: {0}\nEmployee_ID: {1}\nLeave_Date: {2:yyyy/MM/dd}\nLeave_Code: {3}.",
                    param.Factory, param.Employee_ID, leave_Date, param.Leave_Code));
            var daysDeducted = param.Days - data.Days;
            // System background save the  HRMS_Att_Yearly
            OperationResult updateYearlyResult = await Upd_HRMS_Att_Yearly(
                param.Update_By, Convert.ToDateTime(param.Update_Time_Str), param.USER_GUID, param.Factory,
                param.Employee_ID, leave_Date, param.Leave_Code, daysDeducted
            );
            if (!updateYearlyResult.IsSuccess)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, updateYearlyResult.Error);
            }
            data.Work_Shift_Type = param.Work_Shift_Type;
            data.Days = param.Days;
            data.Update_By = param.Update_By;
            data.Update_Time = Convert.ToDateTime(param.Update_Time_Str);
            _repositoryAccessor.HRMS_Att_Leave_Maintain.Update(data);

            await _repositoryAccessor.Save();
            await _repositoryAccessor.CommitAsync();
            return new OperationResult(true, "Update successfully");
        }
        catch (Exception ex)
        {
            await _repositoryAccessor.RollbackAsync();
            return new OperationResult(false, $"Inner exception: {ex.InnerException?.Message ?? "No inner exception message available"}");
        }
    }
    #endregion

    #region DeleteAsync
    /*
     // HRMS_Att_Leave_Maintain
     // input: Factory, Employee_ID, Leave_code,Leave_Date    
    */
    public async Task<OperationResult> DeleteAsync(Leave_Record_Modification_MaintenanceDto param, string userAccount)
    {
        await _repositoryAccessor.BeginTransactionAsync();
        try
        {
            DateTime leave_Date = Convert.ToDateTime(param.Leave_Date_Str);
            HRMS_Att_Leave_Maintain item = await _repositoryAccessor.HRMS_Att_Leave_Maintain.FirstOrDefaultAsync(r =>
                r.Factory == param.Factory &&
                r.Employee_ID == param.Employee_ID &&
                r.Leave_code == param.Leave_Code &&
                r.Leave_Date.Date == leave_Date.Date);
            if (item is null)
                return new OperationResult(false, string.Format(
                   "Item not found: \nFactory: {0}\nEmployee ID: {1}\nLeave Date: {2:yyyy/MM/dd}\nLeave_code: {3}",
                   param.Factory, param.Employee_ID, leave_Date, param.Leave_Code));
            DateTime now = DateTime.Now;
            // System background save the  HRMS_Att_Yearly
            // step 1. Cần trừ số ngày cộng dồn trong năm
            OperationResult updateYearlyResult = await Upd_HRMS_Att_Yearly(
                userAccount, now, item.USER_GUID, item.Factory,
                item.Employee_ID, item.Leave_Date, item.Leave_code, 0 - item.Days);
            if (!updateYearlyResult.IsSuccess)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, updateYearlyResult.Error);
            }

            var mark = await _repositoryAccessor.HRMS_Att_Change_Record.FindAll(x =>
                        x.Factory == item.Factory &&
                        x.Employee_ID == item.Employee_ID &&
                        x.Att_Date == item.Leave_Date, true).CountAsync();
            // step 2. Khôi phục leave_code 5.18 về 00
            if (leave_Date.Date >= DateTime.Today.AddDays(-40).Date && mark > 0)
            {
                OperationResult updateChangeRecordResult = await Upd_HRMS_Att_Change_Record(param, userAccount, now);
                if (!updateChangeRecordResult.IsSuccess)
                {
                    await _repositoryAccessor.RollbackAsync();
                    return new OperationResult(false, updateChangeRecordResult.Error);
                }
            }
            _repositoryAccessor.HRMS_Att_Leave_Maintain.Remove(item);
            await _repositoryAccessor.Save();
            await _repositoryAccessor.CommitAsync();
            return new OperationResult(true, "Delete successfully");
        }
        catch (Exception ex)
        {
            await _repositoryAccessor.RollbackAsync();
            return new OperationResult(false, $"Inner exception: {ex.InnerException?.Message ?? "No inner exception message available"}");
        }
    }
    #endregion

    #region  GetDataPagination
    public async Task<PaginationUtility<Leave_Record_Modification_MaintenanceDto>> GetDataPagination(PaginationParam pagination, Leave_Record_Modification_MaintenanceSearchParamDto param, List<string> roleList)
    {
        var result = await GetData(param);
        return PaginationUtility<Leave_Record_Modification_MaintenanceDto>.Create(result, pagination.PageNumber, pagination.PageSize);
    }
    private async Task<List<Leave_Record_Modification_MaintenanceDto>> GetData(Leave_Record_Modification_MaintenanceSearchParamDto param)
    {

        ExpressionStarter<HRMS_Att_Leave_Maintain> predicate_HALM = PredicateBuilder.New<HRMS_Att_Leave_Maintain>(true);
        ExpressionStarter<HRMS_Emp_Personal> predicate_HEP = PredicateBuilder.New<HRMS_Emp_Personal>(true);

        var permissionGroupQuery = _repositoryAccessor.HRMS_Basic_Role.FindAll(x => x.Factory == param.Factory, true).Select(x => x.Permission_Group);
        if (!string.IsNullOrWhiteSpace(param.Factory))
        {
            predicate_HALM.And(x => x.Factory == param.Factory);
            predicate_HEP.And(x => (x.Factory == param.Factory) && permissionGroupQuery.Contains(x.Permission_Group));
        }
        if (!string.IsNullOrWhiteSpace(param.Employee_ID))
            predicate_HALM.And(x => x.Employee_ID.Contains(param.Employee_ID.Trim()));

        if (!string.IsNullOrWhiteSpace(param.Work_Shift_Type))
            predicate_HALM.And(x => x.Work_Shift_Type == param.Work_Shift_Type);

        if (!string.IsNullOrWhiteSpace(param.Permission_Group))
            predicate_HEP.And(x => x.Permission_Group == param.Permission_Group);
        if (!string.IsNullOrWhiteSpace(param.Leave))
            predicate_HALM.And(x => x.Leave_code == param.Leave);

        if (!string.IsNullOrWhiteSpace(param.Date_Start_Str) && !string.IsNullOrWhiteSpace(param.Date_End_Str))
        {
            predicate_HALM.And(x =>
                x.Leave_Date >= Convert.ToDateTime(param.Date_Start_Str) &&
                x.Leave_Date <= Convert.ToDateTime(param.Date_End_Str)
            );
        }
        var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == param.Factory);
        var HODL = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower());
        var HOD_Lang = HOD
            .GroupJoin(HODL,
                x => new { x.Department_Code, x.Factory },
                y => new { y.Department_Code, y.Factory },
                (x, y) => new { HOD = x, HODL = y })
            .SelectMany(x => x.HODL.DefaultIfEmpty(),
                (x, y) => new { x.HOD, HODL = y })
            .Select(x => new
            {
                x.HOD.Factory,
                x.HOD.Department_Code,
                Department_Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name
            });

        var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predicate_HEP, true);
        var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.WorkShiftType
                                                                || x.Type_Seq == BasicCodeTypeConstant.Leave && x.IsActive);
        var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower());
        var HBC_Lang = HBC
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

        var result = await _repositoryAccessor.HRMS_Att_Leave_Maintain.FindAll(predicate_HALM)
            .Join(HEP,
                x => x.USER_GUID,
                y => y.USER_GUID,
                (x, y) => new { HALM = x, HEP = y })
            .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.WorkShiftType),
                x => x.HALM.Work_Shift_Type,
                y => y.Code,
                (x, y) => new { x.HALM, x.HEP, WorkShiftType = y })
            .SelectMany(x => x.WorkShiftType.DefaultIfEmpty(),
                (x, y) => new { x.HALM, x.HEP, WorkShiftType = y })
            .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.Leave),
                x => x.HALM.Leave_code,
                y => y.Code,
                (x, y) => new { x.HALM, x.HEP, x.WorkShiftType, Leave = y })
            .SelectMany(x => x.Leave.DefaultIfEmpty(),
                (x, y) => new { x.HALM, x.HEP, x.WorkShiftType, Leave = y })
            .GroupJoin(HOD_Lang,
                x => x.HALM.Department,
                y => y.Department_Code,
                (x, y) => new { x.HALM, x.HEP, x.WorkShiftType, x.Leave, HOD_Lang = y })
            .SelectMany(x => x.HOD_Lang.DefaultIfEmpty(),
                (x, y) => new { x.HALM, x.HEP, x.WorkShiftType, x.Leave, HOD_Lang = y })
            .Select(x => new Leave_Record_Modification_MaintenanceDto
            {
                USER_GUID = x.HALM.USER_GUID,
                Factory = x.HALM.Factory,
                Department_Code = x.HALM.Department,
                Department_Name = x.HOD_Lang.Department_Name,
                Department_Code_Name = (x.HOD_Lang != null && !string.IsNullOrWhiteSpace(x.HOD_Lang.Department_Name))
                    ? $"{x.HOD_Lang.Department_Code} - {x.HOD_Lang.Department_Name}"
                    : x.HALM.Department,
                Employee_ID = x.HALM.Employee_ID,
                Local_Full_Name = x.HEP.Local_Full_Name,
                Work_Shift_Type = x.HALM.Work_Shift_Type,
                Work_Shift_Type_Str = string.IsNullOrWhiteSpace(x.WorkShiftType.Code_Name)
                    ? x.HALM.Work_Shift_Type
                    : $"{x.HALM.Work_Shift_Type} - {x.WorkShiftType.Code_Name}",
                Leave_Code = x.HALM.Leave_code,
                Leave_Code_Str = string.IsNullOrWhiteSpace(x.Leave.Code_Name)
                    ? x.HALM.Leave_code
                    : $"{x.HALM.Leave_code} - {x.Leave.Code_Name}",
                Leave_Date = x.HALM.Leave_Date,
                Leave_Date_Str = x.HALM.Leave_Date.ToString("yyyy/MM/dd"),
                Days = x.HALM.Days,
                IsLeaveDate = x.HALM.Leave_Date.Date < DateTime.Today.AddDays(-90).Date,
                Update_By = x.HALM.Update_By,
                Update_Time_Str = x.HALM.Update_Time.ToString("yyyy/MM/dd HH:mm:ss")
            }).Distinct().ToListAsync();

        if (!string.IsNullOrWhiteSpace(param.Department))
            result = result.Where(x => x.Department_Code == param.Department).ToList();

        return result.OrderBy(x => x.Factory).ThenBy(x => x.Employee_ID).ThenBy(x => x.Leave_Code).ToList();
    }
    #endregion

    #region Upd_HRMS_Att_Yearly  background save(insert/update, delete (step 1))
    // 3.13 Upd_HRMS_Att_Yearly 
    // USER_GUID、Factory、Employee_ID、YYYY/01/01、Leave_Type、Leave_Code、Days
    private async Task<OperationResult> Upd_HRMS_Att_Yearly(string updateBy, DateTime updateTime, string USER_GUID, string factory, string employeeId, DateTime leave_Date, string leaveCode, decimal days)
    {
        try
        {
            DateTime leave_Date_Year = new(leave_Date.Year, 1, 1);
            HRMS_Att_Yearly data = await _repositoryAccessor.HRMS_Att_Yearly.FirstOrDefaultAsync(r =>
                r.Factory == factory &&
                r.Att_Year == leave_Date_Year &&
                r.Employee_ID == employeeId &&
                r.USER_GUID == USER_GUID &&
                r.Leave_Type == "1" &&
                r.Leave_Code == leaveCode);
            if (data is null)
                return new OperationResult(false, string.Format(
                    "Update Upd_HRMS_Att_Yearly Fail! Record not found: \nFactory: {0}\nAtt_Year: {1:yyyy/MM/dd}\nEmployee ID: {2}\nUser GUID: {3}\nLeave Type: 1\nLeave Code: {4}",
                    factory, leave_Date_Year, employeeId, USER_GUID, leaveCode));
            HRMS_Basic_Factory_Comparison tblDivision = await _repositoryAccessor.HRMS_Basic_Factory_Comparison.FirstOrDefaultAsync(r => r.Factory == factory);
            data.USER_GUID = USER_GUID;
            data.Division = tblDivision?.Division ?? "";
            data.Factory = factory;
            data.Att_Year = leave_Date_Year;
            data.Employee_ID = employeeId;
            data.Leave_Code = leaveCode;
            data.Days += days;
            data.Update_By = updateBy;
            data.Update_Time = updateTime;
            _repositoryAccessor.HRMS_Att_Yearly.Update(data);
            return new OperationResult(await _repositoryAccessor.Save());
        }
        catch (Exception)
        {
            return new OperationResult(false, "Delete failed due to Att Yearly background update failed.");
        }
    }
    #endregion

    #region Upd_HRMS_Att_Change_Record
    // Upd_HRMS_Att_Change_Record  background save when delete leave maintain
    // input param: ALL Column
    // 成功/失敗 Success/Failure
    private async Task<OperationResult> Upd_HRMS_Att_Change_Record(Leave_Record_Modification_MaintenanceDto record, string updateBy, DateTime updateTime)
    {
        try
        {
            DateTime leave_Date = Convert.ToDateTime(record.Leave_Date_Str);
            HRMS_Att_Change_Record data = await _repositoryAccessor.HRMS_Att_Change_Record.FirstOrDefaultAsync(r =>
                r.Factory == record.Factory &&
                r.Att_Date.Date == leave_Date.Date &&
                r.Employee_ID == record.Employee_ID);
            if (data is null)
                return new OperationResult(false, string.Format(
                   "Update HRMS_Att_Change_Record Fail! Record not found: \nFactory: {0}\nEmployee_ID: {1}\nAtt_Date: {2:yyyy/MM/dd}.",
                   record.Factory, record.Employee_ID, leave_Date));
            HRMS_Att_Work_Shift tblHRMS_Att_Work_Shift = await _repositoryAccessor.HRMS_Att_Work_Shift.FirstOrDefaultAsync(r =>
                r.Factory == record.Factory &&
                r.Work_Shift_Type == record.Work_Shift_Type &&
                r.Week == ((int)leave_Date.DayOfWeek).ToString());
            data.Clock_In = tblHRMS_Att_Work_Shift?.Clock_In ?? "";
            data.Leave_Code = "00";
            data.Update_By = updateBy;
            data.Update_Time = updateTime;
            _repositoryAccessor.HRMS_Att_Change_Record.Update(data);
            return new OperationResult(await _repositoryAccessor.Save());
        }
        catch (Exception)
        {
            return new OperationResult(false, "Delete failed due to Att Change Record background update failed.");
        }
    }
    #endregion
    public async Task<OperationResult> CheckExistedData(string Factory, string Employee_ID, string Leave_Code, string Leave_Date)
    {
        return new OperationResult(await _repositoryAccessor.HRMS_Att_Leave_Maintain.AnyAsync(x =>
            x.Employee_ID == Employee_ID &&
            x.Factory == Factory &&
            x.Leave_code == Leave_Code &&
            x.Leave_Date.Date == Convert.ToDateTime(Leave_Date).Date));
    }
    // code = 40 (2.2.Query_HRMS_Basic_Code_List )
    public async Task<List<KeyValuePair<string, string>>> GetListLeave(string language)
    {
        return await GetBasicCodeList(language, BasicCodeTypeConstant.Leave);
    }
    private async Task<List<KeyValuePair<string, string>>> GetBasicCodeList(string language, string typeSeq)
    {
        return await _repositoryAccessor.HRMS_Basic_Code
            .FindAll(x => x.Type_Seq == typeSeq && x.Char1 == "Leave" && x.IsActive == true, true)
            .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                HBC => new { HBC.Type_Seq, HBC.Code },
                HBCL => new { HBCL.Type_Seq, HBCL.Code },
                (HBC, HBCL) => new { HBC, HBCL })
            .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                (prev, HBCL) => new { prev.HBC, HBCL })
            .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
            .ToListAsync();
    }

    #region GetWorkShiftType
    public async Task<OperationResult> GetWorkShiftType(Leave_Record_Modification_MaintenanceSearchParamDto param)
    {
        DateTime leave_Date = Convert.ToDateTime(param.Leave_Date_Str);
        var predHBC = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.WorkShiftType);
        var HACR = _repositoryAccessor.HRMS_Att_Change_Record.FindAll(x =>
            x.Factory == param.Factory &&
            x.Employee_ID == param.Employee_ID &&
            x.Att_Date.Date == leave_Date.Date);
        var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll(predHBC);
        var data = await HACR
            .Join(HBC,
                x => x.Work_Shift_Type,
                y => y.Code,
                (x, y) => new { HACR = x, HBC = y })
            .FirstOrDefaultAsync();
        if (data == null)
            return new OperationResult(false);
        return new OperationResult(true, data.HACR);
    }
    #endregion
    public async Task<List<KeyValuePair<string, string>>> GetListFactoryByUser(string language, string userName)
    {
        var factorys = await Queryt_Factory_AddList(userName);
        var factories = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory && factorys.Contains(x.Code), true)
            .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                x => new { x.Type_Seq, x.Code },
                y => new { y.Type_Seq, y.Code },
                (x, y) => new { x, y })
            .SelectMany(x => x.y.DefaultIfEmpty(),
                (x, y) => new { x.x, y })
            .Select(x => new KeyValuePair<string, string>(x.x.Code, $"{x.x.Code} - {(x.y != null ? x.y.Code_Name : x.x.Code_Name)}")).ToListAsync();
        return factories;
    }
    public async Task<OperationResult> DownloadFileExcel(Leave_Record_Modification_MaintenanceSearchParamDto param, string userName)
    {
        var data = await GetData(param);
        if (!data.Any())
            return new OperationResult(false, "System.Message.NoData");

        List<Cell> dataCells = new()
        {
            new Cell("B" + 2, param.Factory),
            new Cell("E" + 2, userName),
            new Cell("G" + 2, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))
        };

        var index = 6;
        for (int i = 0; i < data.Count; i++)
        {
            dataCells.Add(new Cell("A" + index, data[i].Factory));
            dataCells.Add(new Cell("B" + index, data[i].Department_Code));
            dataCells.Add(new Cell("C" + index, data[i].Department_Name));
            dataCells.Add(new Cell("D" + index, data[i].Employee_ID));
            dataCells.Add(new Cell("E" + index, data[i].Local_Full_Name));
            dataCells.Add(new Cell("F" + index, data[i].Work_Shift_Type_Str));
            dataCells.Add(new Cell("G" + index, data[i].Leave_Code_Str));
            dataCells.Add(new Cell("H" + index, data[i].Leave_Date));
            dataCells.Add(new Cell("I" + index, data[i].Days));
            dataCells.Add(new Cell("J" + index, data[i].Update_By));
            dataCells.Add(new Cell("K" + index, data[i].Update_Time_Str));
            index += 1;
        }

        ExcelResult excelResult = ExcelUtility.DownloadExcel(dataCells, "Resources\\Template\\AttendanceMaintenance\\5_1_19_LeaveRecordModificationMaintenance\\Download.xlsx");
        return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
    }
}
