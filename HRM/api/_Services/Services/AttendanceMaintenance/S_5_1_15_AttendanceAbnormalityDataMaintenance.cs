using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_1_15_AttendanceAbnormalityDataMaintenance : BaseServices, I_5_1_15_AttendanceAbnormalityDataMaintenance
    {
        private enum BasicCodeEnum { Null, Leave, Attendance }
        public S_5_1_15_AttendanceAbnormalityDataMaintenance(DBContext dbContext) : base(dbContext)
        {
        }

        #region GetList
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
        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
        {
            return await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == factory, true)
                .Join(
                    _repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(b => b.Kind == "1" && b.Factory == factory, true),
                    department => department.Division,
                    factoryComparison => factoryComparison.Division,
                    (department, factoryComparison) => department
                )
                .GroupJoin(
                    _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    department => new { department.Factory, department.Department_Code },
                    language => new { language.Factory, language.Department_Code },
                    (department, language) => new { Department = department, Language = language }
                )
                .SelectMany(
                    x => x.Language.DefaultIfEmpty(),
                    (x, language) => new { x.Department, Language = language }
                )
                .OrderBy(x => x.Department.Department_Code)
                .Select(
                    x => new KeyValuePair<string, string>(
                        x.Department.Department_Code,
                        $"{x.Department.Department_Code} - {(x.Language != null ? x.Language.Name : x.Department.Department_Name)}"
                    )
                )
                .ToListAsync();
        }

        public async Task<List<KeyValuePair<string, string>>> GetListWorkShiftType(string language)
        {
            return await GetListByTypeSeq(BasicCodeTypeConstant.WorkShiftType, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListAttendance(string language)
        {
            return await GetListByTypeSeq(BasicCodeTypeConstant.Leave, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListUpdateReason(string language)
        {
            return await GetListByTypeSeq(BasicCodeTypeConstant.ReasonCode, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListHoliday(string language)
        {
            return await GetListByTypeSeq(BasicCodeTypeConstant.Holiday, language, BasicCodeEnum.Attendance);
        }

        private async Task<List<KeyValuePair<string, string>>> GetListByTypeSeq(string typeSeq, string language, BasicCodeEnum char1 = BasicCodeEnum.Null)
        {
            var predHBC = char1 switch
            {
                BasicCodeEnum.Null => PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == typeSeq),
                BasicCodeEnum.Leave => PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == typeSeq && x.Char1 == "Leave"),
                BasicCodeEnum.Attendance => PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == typeSeq && x.Char1 == "Attendance"),
                _ => throw new ArgumentException("Invalid char1")
            };
            var predHBCL = PredicateBuilder.New<HRMS_Basic_Code_Language>(x => x.Type_Seq == typeSeq);
            if (!string.IsNullOrWhiteSpace(language))
                predHBCL.And(x => x.Language_Code.ToLower() == language.ToLower());
            var query = _repositoryAccessor.HRMS_Basic_Code.FindAll(predHBC, true);
            var data = await query
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(predHBCL, true),
                    code => code.Code,
                    language => language.Code,
                    (code, language) => new { Code = code, Language = language })
                .SelectMany(
                    x => x.Language.DefaultIfEmpty(),
                    (x, language) => new { x.Code, Language = language })
                .Select(x => new KeyValuePair<string, string>(x.Code.Code, $"{x.Code.Code} - {x.Language.Code_Name ?? x.Code.Code_Name}"))
                .Distinct()
                .ToListAsync();
            return data;
        }
        #endregion

        #region GetData
        private async Task<List<HRMS_Att_Temp_RecordDto>> GetData(AttendanceAbnormalityDataMaintenanceParam param)
        {
            var pred = PredicateBuilder.New<HRMS_Att_Temp_Record>(x => x.Factory == param.Factory);

            if (!string.IsNullOrWhiteSpace(param.Department))
                pred.And(x => x.Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                pred.And(x => x.Employee_ID.Contains(param.Employee_ID.Trim()));

            if (!string.IsNullOrWhiteSpace(param.Work_Shift_Type))
                pred.And(x => x.Work_Shift_Type == param.Work_Shift_Type);

            if (param.List_Attendance != null && param.List_Attendance.Count > 0)
                pred.And(x => param.List_Attendance.Contains(x.Leave_Code));

            if (!string.IsNullOrWhiteSpace(param.Att_Date_From_Str))
                pred.And(x => x.Att_Date.Date >= Convert.ToDateTime(param.Att_Date_From_Str).Date);

            if (!string.IsNullOrWhiteSpace(param.Att_Date_To_Str))
                pred.And(x => x.Att_Date.Date <= Convert.ToDateTime(param.Att_Date_To_Str).Date);

            var permissionGroupQuery = _repositoryAccessor.HRMS_Basic_Role.FindAll(x => x.Factory == param.Factory, true).Select(x => x.Permission_Group);
            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => (x.Factory == param.Factory) && permissionGroupQuery.Contains(x.Permission_Group));
            var HAC_Reason = _repositoryAccessor.HRMS_Att_Change_Reason.FindAll(x => x.Factory == param.Factory);
            var HATR = _repositoryAccessor.HRMS_Att_Temp_Record.FindAll(pred, true);
            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.IsActive);
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
                    x.HBC.Char1,
                    Code_Name = $"{x.HBC.Code}-{(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                });
            var HBC_Holiday = HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.Holiday && x.Char1 == "Attendance");
            var HBC_Reason = HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.ReasonCode);
            var HBC_WorkTypeShift = HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.WorkShiftType);
            var HBC_Leave = HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.Leave);

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
                    Department = x.HOD.Department_Code,
                    Department_Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name
                });

            var result = await HATR
                .Join(HEP,
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { HATR = x, HEP = y })
                .GroupJoin(HAC_Reason,
                    x => new { x.HATR.Att_Date, x.HATR.Employee_ID },
                    y => new { y.Att_Date, y.Employee_ID },
                    (x, y) => new { x.HATR, x.HEP, HAC_Reason = y })
                .SelectMany(x => x.HAC_Reason.DefaultIfEmpty(),
                    (x, y) => new { x.HATR, x.HEP, HAC_Reason = y })
                .GroupJoin(HBC_Holiday,
                    x => x.HATR.Holiday,
                    y => y.Code,
                    (x, y) => new { x.HATR, x.HEP, x.HAC_Reason, HBC_Holiday = y })
                .SelectMany(x => x.HBC_Holiday.DefaultIfEmpty(),
                    (x, y) => new { x.HATR, x.HEP, x.HAC_Reason, HBC_Holiday = y })
                .GroupJoin(HBC_Reason,
                    x => x.HAC_Reason.Reason_Code,
                    y => y.Code,
                    (x, y) => new { x.HATR, x.HEP, x.HAC_Reason, x.HBC_Holiday, HBC_Reason = y })
                .SelectMany(x => x.HBC_Reason.DefaultIfEmpty(),
                    (x, y) => new { x.HATR, x.HEP, x.HAC_Reason, x.HBC_Holiday, HBC_Reason = y })
                .GroupJoin(HBC_WorkTypeShift,
                    x => x.HATR.Work_Shift_Type,
                    y => y.Code,
                    (x, y) => new { x.HATR, x.HEP, x.HAC_Reason, x.HBC_Holiday, x.HBC_Reason, HBC_WorkTypeShift = y })
                .SelectMany(x => x.HBC_WorkTypeShift.DefaultIfEmpty(),
                    (x, y) => new { x.HATR, x.HEP, x.HAC_Reason, x.HBC_Holiday, x.HBC_Reason, HBC_WorkTypeShift = y })
                .GroupJoin(HBC_Leave,
                    x => x.HATR.Leave_Code,
                    y => y.Code,
                    (x, y) => new { x.HATR, x.HEP, x.HAC_Reason, x.HBC_Holiday, x.HBC_Reason, x.HBC_WorkTypeShift, HBC_Leave = y })
                .SelectMany(x => x.HBC_Leave.DefaultIfEmpty(),
                    (x, y) => new { x.HATR, x.HEP, x.HAC_Reason, x.HBC_Holiday, x.HBC_Reason, x.HBC_WorkTypeShift, HBC_Leave = y })
                .GroupJoin(HOD_Lang,
                    x => new { x.HATR.Factory, x.HATR.Department },
                    y => new { y.Factory, y.Department },
                    (x, y) => new { x.HATR, x.HEP, x.HAC_Reason, x.HBC_Holiday, x.HBC_Reason, x.HBC_WorkTypeShift, x.HBC_Leave, HOD_Lang = y })
                .SelectMany(x => x.HOD_Lang.DefaultIfEmpty(),
                    (x, y) => new { x.HATR, x.HEP, x.HAC_Reason, x.HBC_Holiday, x.HBC_Reason, x.HBC_WorkTypeShift, x.HBC_Leave, HOD_Lang = y })
                .Select(x => new HRMS_Att_Temp_RecordDto
                {
                    USER_GUID = x.HATR.USER_GUID,
                    Factory = x.HATR.Factory,
                    Employee_ID = x.HATR.Employee_ID,
                    Att_Date = x.HATR.Att_Date,
                    Att_Date_Str = x.HATR.Att_Date.ToString("yyyy/MM/dd"),

                    Local_Full_Name = x.HEP.Local_Full_Name,
                    Department_Code = x.HATR.Department,
                    Department_Name = x.HOD_Lang.Department_Name,
                    Department_Code_Name = x.HOD_Lang != null && !string.IsNullOrWhiteSpace(x.HOD_Lang.Department_Name)
                            ? x.HOD_Lang.Department + "-" + x.HOD_Lang.Department_Name : x.HATR.Department,

                    Work_Shift_Type = x.HATR.Work_Shift_Type,
                    Work_Shift_Type_Str = x.HBC_WorkTypeShift.Code_Name ,

                    Leave_Code = x.HATR.Leave_Code,
                    Leave_Code_Str = x.HBC_Leave.Code_Name ,

                    Reason_Code = x.HAC_Reason.Reason_Code,
                    Reason_Code_Str = x.HBC_Reason.Code_Name ,

                    Holiday = x.HATR.Holiday,
                    Holiday_Str = x.HBC_Holiday.Code_Name ,

                    Clock_In = x.HAC_Reason != null ? x.HAC_Reason.Clock_In : x.HATR.Clock_In,
                    Clock_Out = x.HAC_Reason != null ? x.HAC_Reason.Clock_Out : x.HATR.Clock_Out,
                    Overtime_ClockIn = x.HAC_Reason != null ? x.HAC_Reason.Overtime_ClockIn : x.HATR.Overtime_ClockIn,
                    Overtime_ClockOut = x.HAC_Reason != null ? x.HAC_Reason.Overtime_ClockOut : x.HATR.Overtime_ClockOut,

                    Modified_Clock_In = x.HATR.Clock_In,
                    Modified_Clock_Out = x.HATR.Clock_Out,
                    Modified_Overtime_ClockIn = x.HATR.Overtime_ClockIn,
                    Modified_Overtime_ClockOut = x.HATR.Overtime_ClockOut,

                    Days = Math.Round(Convert.ToDecimal(x.HATR.Days), 5).ToString("0.#####") ?? "0.0",
                    Update_By = x.HATR.Update_By,
                    Update_Time = x.HATR.Update_Time,
                    Update_Time_Str = x.HATR.Update_Time.ToString("yyyy/MM/dd HH:mm:ss")
                })
                .OrderBy(x => x.Factory).ThenBy(x => x.Att_Date).ThenBy(x => x.Employee_ID)
                .ToListAsync();
            if (!string.IsNullOrWhiteSpace(param.Reason_Code))
                result = result.FindAll(x => x.Reason_Code == param.Reason_Code);
            return result;
        }
        public async Task<PaginationUtility<HRMS_Att_Temp_RecordDto>> GetDataPagination(PaginationParam pagination, AttendanceAbnormalityDataMaintenanceParam param)
        {
            var result = await GetData(param);
            return PaginationUtility<HRMS_Att_Temp_RecordDto>.Create(result, pagination.PageNumber, pagination.PageSize);
        }
        #endregion

        #region Add
        public async Task<OperationResult> AddNew(HRMS_Att_Temp_RecordDto data, string userName)
        {
            if (data == null)
                return new OperationResult(false, "Data list is empty");
            var now = DateTime.Now;

            var leaveCodeList = await GetListByTypeSeq(BasicCodeTypeConstant.Leave, null, BasicCodeEnum.Leave);
            var attDate = Convert.ToDateTime(data.Att_Date);

            var existingTemp = await _repositoryAccessor.HRMS_Att_Temp_Record
                .FirstOrDefaultAsync(x => x.Factory == data.Factory && x.Att_Date == attDate && x.Employee_ID == data.Employee_ID);

            if (existingTemp != null)
                return new OperationResult(false, $"Data already exists in HRMS_Att_Temp_Record \n Employee ID: {data.Employee_ID} \n AttDate: {attDate:yyyy/MM/dd} \n");

            var existingReason = await _repositoryAccessor.HRMS_Att_Change_Reason
                .FirstOrDefaultAsync(x => x.Factory == data.Factory && x.Att_Date == attDate && x.Employee_ID == data.Employee_ID);

            if (existingReason != null)
                _repositoryAccessor.HRMS_Att_Change_Reason.Remove(existingReason);
            // Thêm dữ liệu mới vào HRMS_Att_Temp_Record
            var newDataTemp = new HRMS_Att_Temp_Record
            {
                USER_GUID = data.USER_GUID,
                Factory = data.Factory,
                Att_Date = attDate,
                Department = data.Department_Code,
                Employee_ID = data.Employee_ID,
                Work_Shift_Type = data.Work_Shift_Type,
                Leave_Code = data.Leave_Code,
                Clock_In = data.Modified_Clock_In,
                Clock_Out = data.Modified_Clock_Out,
                Overtime_ClockIn = data.Modified_Overtime_ClockIn,
                Overtime_ClockOut = data.Modified_Overtime_ClockOut,
                Days = Convert.ToDecimal(data.Days),
                Holiday = data.Holiday,
                Update_By = userName,
                Update_Time = now
            };

            // Thêm dữ liệu mới vào HRMS_Att_Change_Reason
            var newDataReason = new HRMS_Att_Change_Reason
            {
                USER_GUID = data.USER_GUID,
                Factory = data.Factory,
                Att_Date = attDate,
                Employee_ID = data.Employee_ID,
                Work_Shift_Type = data.Work_Shift_Type,
                Leave_Code = data.Leave_Code,
                Reason_Code = data.Reason_Code,
                Clock_In = data.Modified_Clock_In,
                Clock_Out = data.Modified_Clock_Out,
                Overtime_ClockIn = data.Modified_Overtime_ClockIn,
                Overtime_ClockOut = data.Modified_Overtime_ClockOut,
                Update_By = userName,
                Update_Time = now
            };

            // Cập nhật HRMS_Att_Yearly nếu Leave_Code nằm trong danh sách leaveCodeList
            if (leaveCodeList.Any(x => x.Key.Equals(data.Leave_Code, StringComparison.OrdinalIgnoreCase)))
            {
                var yearlyRecord = await _repositoryAccessor.HRMS_Att_Yearly
                    .FirstOrDefaultAsync(x => x.USER_GUID == data.USER_GUID && x.Factory == data.Factory && x.Employee_ID == data.Employee_ID
                                            && x.Att_Year == new DateTime(attDate.Year, 1, 1) && x.Leave_Type == "1"
                                            && x.Leave_Code == data.Leave_Code);

                if (yearlyRecord == null)
                    return new OperationResult(false, $"UPDATE HRMS_Att_Yearly Fail! \n Employee ID: {data.Employee_ID} \n AttDate: {attDate:yyyy/MM/dd} \n");

                yearlyRecord.Days += Convert.ToDecimal(data.Days);
                yearlyRecord.Update_By = userName;
                yearlyRecord.Update_Time = now;
                _repositoryAccessor.HRMS_Att_Yearly.Update(yearlyRecord);
            }

            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                _repositoryAccessor.HRMS_Att_Temp_Record.Add(newDataTemp);
                _repositoryAccessor.HRMS_Att_Change_Reason.Add(newDataReason);
                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true, "Add Successfully");
            }
            catch (Exception ex)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, $"Inner exception: {ex.InnerException?.Message ?? "No inner exception message available"}");
            }
        }
        #endregion

        #region Edit
        public async Task<OperationResult> Edit(HRMS_Att_Temp_RecordDto data, string userName)
        {
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                var attDate = Convert.ToDateTime(data.Att_Date);
                var now = DateTime.Now;
                var existingTemp = await _repositoryAccessor.HRMS_Att_Temp_Record
                  .FirstOrDefaultAsync(x => x.Factory == data.Factory && x.Att_Date == attDate && x.Employee_ID == data.Employee_ID);
                if (existingTemp == null)
                {
                    await _repositoryAccessor.RollbackAsync();
                    return new OperationResult(false, $"No Data in HRMS_Att_Temp_Record");
                }
                var beforeLeaveCode = existingTemp.Leave_Code; // Leave_Code trước khi chỉnh sửa
                var beforeDays = Convert.ToDecimal(existingTemp.Days);
                var afterLeaveCode = data.Leave_Code;          // Leave_Code sau khi chỉnh sửa
                var afterDays = Convert.ToDecimal(data.Days);

                // Cập nhật HRMS_Att_Temp_Record mới 
                existingTemp.Leave_Code = afterLeaveCode;
                existingTemp.Clock_In = data.Modified_Clock_In;
                existingTemp.Clock_Out = data.Modified_Clock_Out;
                existingTemp.Overtime_ClockIn = data.Modified_Overtime_ClockIn;
                existingTemp.Overtime_ClockOut = data.Modified_Overtime_ClockOut;
                existingTemp.Days = afterDays;
                existingTemp.Update_By = userName;
                existingTemp.Update_Time = now;
                _repositoryAccessor.HRMS_Att_Temp_Record.Update(existingTemp);

                // Cập nhật HRMS_Att_Change_Reason
                var existingReason = await _repositoryAccessor.HRMS_Att_Change_Reason
                  .FirstOrDefaultAsync(x => x.Factory == data.Factory && x.Att_Date == attDate && x.Employee_ID == data.Employee_ID);
                if (existingReason != null)
                {
                    existingReason.Leave_Code = afterLeaveCode;
                    existingReason.Reason_Code = data.Reason_Code;
                    existingReason.Clock_In = data.Modified_Clock_In_Old;
                    existingReason.Clock_Out = data.Modified_Clock_Out_Old;
                    existingReason.Overtime_ClockIn = data.Modified_Overtime_ClockIn_Old;
                    existingReason.Overtime_ClockOut = data.Modified_Overtime_ClockOut_Old;
                    existingReason.Update_By = userName;
                    existingReason.Update_Time = now;
                    _repositoryAccessor.HRMS_Att_Change_Reason.Update(existingReason);
                }
                else
                {
                    var newDataReason = new HRMS_Att_Change_Reason
                    {
                        USER_GUID = data.USER_GUID,
                        Factory = data.Factory,
                        Att_Date = attDate,
                        Employee_ID = data.Employee_ID,
                        Work_Shift_Type = data.Work_Shift_Type,
                        Leave_Code = data.Leave_Code,
                        Reason_Code = data.Reason_Code,
                        Clock_In = data.Modified_Clock_In,
                        Clock_Out = data.Modified_Clock_Out,
                        Overtime_ClockIn = data.Modified_Overtime_ClockIn,
                        Overtime_ClockOut = data.Modified_Overtime_ClockOut,
                        Update_By = userName,
                        Update_Time = now
                    };
                    _repositoryAccessor.HRMS_Att_Change_Reason.Add(newDataReason);
                }

                // Cập nhật HRMS_Att_Yearly
                // Lấy danh sách Leave_Code
                var leaveCodeList = await GetListByTypeSeq(BasicCodeTypeConstant.Leave, null, BasicCodeEnum.Leave);

                // Kiểm tra trước khi thay đổi Leave_Code
                if (leaveCodeList.Any(x => x.Key.Equals(beforeLeaveCode, StringComparison.OrdinalIgnoreCase)))
                {
                    var yearlyRecordBefore = await _repositoryAccessor.HRMS_Att_Yearly
                        .FirstOrDefaultAsync(x => x.USER_GUID == data.USER_GUID && x.Factory == data.Factory && x.Employee_ID == data.Employee_ID
                                               && x.Att_Year == new DateTime(attDate.Year, 1, 1) && x.Leave_Type == "1"
                                               && x.Leave_Code == beforeLeaveCode);
                    if (yearlyRecordBefore == null)
                    {
                        await _repositoryAccessor.RollbackAsync();
                        return new OperationResult(false, $"UPDATE HRMS_Att_Yearly Fail!");
                    }
                    yearlyRecordBefore.Days -= beforeDays;
                    if (yearlyRecordBefore.Days < 0)
                        yearlyRecordBefore.Days = 0;
                    yearlyRecordBefore.Update_By = userName;
                    yearlyRecordBefore.Update_Time = now;
                    _repositoryAccessor.HRMS_Att_Yearly.Update(yearlyRecordBefore);
                }
                // Kiểm tra sau khi thay đổi Leave_Code
                if (leaveCodeList.Any(x => x.Key.Equals(afterLeaveCode, StringComparison.OrdinalIgnoreCase)))
                {
                    var yearlyRecordAfter = await _repositoryAccessor.HRMS_Att_Yearly
                        .FirstOrDefaultAsync(x => x.USER_GUID == data.USER_GUID && x.Factory == data.Factory && x.Employee_ID == data.Employee_ID
                                               && x.Att_Year == new DateTime(attDate.Year, 1, 1) && x.Leave_Type == "1"
                                               && x.Leave_Code == afterLeaveCode);
                    if (yearlyRecordAfter == null)
                    {
                        await _repositoryAccessor.RollbackAsync();
                        return new OperationResult(false, $"UPDATE HRMS_Att_Yearly Fail!");
                    }
                    yearlyRecordAfter.Days += afterDays;
                    yearlyRecordAfter.Update_By = userName;
                    yearlyRecordAfter.Update_Time = now;
                    _repositoryAccessor.HRMS_Att_Yearly.Update(yearlyRecordAfter);
                }

                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true, "Edit Successfully");
            }
            catch (Exception ex)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, $"Inner exception: {ex.InnerException?.Message ?? "No inner exception message available"}");
            }
        }
        #endregion

        #region Delete
        public async Task<OperationResult> Delete(HRMS_Att_Temp_RecordDto data, string userName)
        {
            var attDate = Convert.ToDateTime(data.Att_Date);

            var existingData = await _repositoryAccessor.HRMS_Att_Temp_Record
                .FirstOrDefaultAsync(x => x.Factory == data.Factory && x.Att_Date == attDate && x.Employee_ID == data.Employee_ID);

            if (existingData == null)
                return new OperationResult(false, $"No Data in HRMS_Att_Temp_Record");

            _repositoryAccessor.HRMS_Att_Temp_Record.Remove(existingData);

            // Lấy danh sách Leave_Code
            var leaveCodeList = await GetListByTypeSeq(BasicCodeTypeConstant.Leave, null, BasicCodeEnum.Leave);
            var leaveCode = leaveCodeList.Select(x => x.Key).ToHashSet();

            if (leaveCode.Contains(data.Leave_Code))
            {
                var yearlyRecord = await _repositoryAccessor.HRMS_Att_Yearly
                    .FirstOrDefaultAsync(x => x.USER_GUID == data.USER_GUID && x.Factory == data.Factory && x.Employee_ID == data.Employee_ID
                                           && x.Att_Year == new DateTime(attDate.Year, 1, 1) && x.Leave_Type == "1"
                                           && x.Leave_Code == data.Leave_Code);

                if (yearlyRecord == null)
                    return new OperationResult(false, $"UPDATE HRMS_Att_Yearly Fail!");

                yearlyRecord.Days -= Convert.ToDecimal(data.Days);
                yearlyRecord.Update_By = userName;
                yearlyRecord.Update_Time = DateTime.Now;
                _repositoryAccessor.HRMS_Att_Yearly.Update(yearlyRecord);
            }

            try
            {
                await _repositoryAccessor.Save();
                return new OperationResult(true, "Delete Successfully");
            }
            catch
            {
                return new OperationResult(false, "Delete failed");
            }
        }
        public async Task<OperationResult> DownloadFileExcel(AttendanceAbnormalityDataMaintenanceParam param, string userName)
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
                dataCells.Add(new Cell("F" + index, data[i].Att_Date));
                dataCells.Add(new Cell("G" + index, data[i].Work_Shift_Type_Str));
                dataCells.Add(new Cell("H" + index, data[i].Leave_Code_Str));
                dataCells.Add(new Cell("I" + index, data[i].Reason_Code_Str == " - " ? string.Empty : data[i].Reason_Code_Str));
                dataCells.Add(new Cell("J" + index, data[i].Clock_In));
                dataCells.Add(new Cell("K" + index, data[i].Modified_Clock_In));
                dataCells.Add(new Cell("L" + index, data[i].Clock_Out));
                dataCells.Add(new Cell("M" + index, data[i].Modified_Clock_Out));
                dataCells.Add(new Cell("N" + index, data[i].Overtime_ClockIn));
                dataCells.Add(new Cell("O" + index, data[i].Modified_Overtime_ClockIn));
                dataCells.Add(new Cell("P" + index, data[i].Overtime_ClockOut));
                dataCells.Add(new Cell("Q" + index, data[i].Modified_Overtime_ClockOut));
                dataCells.Add(new Cell("R" + index, data[i].Days));
                dataCells.Add(new Cell("S" + index, data[i].Holiday_Str));
                dataCells.Add(new Cell("T" + index, data[i].Update_By));
                dataCells.Add(new Cell("U" + index, data[i].Update_Time_Str));
                index += 1;
            }

            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataCells,
                "Resources\\Template\\AttendanceMaintenance\\5_1_15_AttendanceAbnormalityDataMaintenance\\Download.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }
        #endregion
    }
}