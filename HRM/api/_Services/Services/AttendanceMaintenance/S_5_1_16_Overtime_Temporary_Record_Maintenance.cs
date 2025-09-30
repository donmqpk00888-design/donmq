using System.Data.SqlTypes;
using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_1_16_Overtime_Temporary_Record_Maintenance : BaseServices, I_5_1_16_OvertimeTemporaryRecordMaintenance
    {
        public S_5_1_16_Overtime_Temporary_Record_Maintenance(DBContext dbContext) : base(dbContext)
        {
        }

        #region Create Update Delete
        public async Task<OperationResult> Create(HRMS_Att_Overtime_TempDto data, string userName)
        {

            var checkData = _repositoryAccessor.HRMS_Att_Overtime_Temp.Any(x =>
                x.Overtime_Date.Date == DateTime.Parse(data.Date_Str).Date &&
                x.Employee_ID == data.Employee_ID &&
                x.Factory == data.Factory);
            if (checkData)
                return new OperationResult(false, "Data already exists");
            var Detail = new HRMS_Att_Overtime_Temp
            {
                USER_GUID = data.USER_GUID,
                Factory = data.Factory,
                Employee_ID = data.Employee_ID,
                Overtime_Date = Convert.ToDateTime(data.Date),
                Work_Shift_Type = data.Work_Shift_Type,
                Overtime_Start = data.Overtime_Start,
                Overtime_End = data.Overtime_End,
                Overtime_Hours = data.Overtime_Hours,
                Night_Hours = data.Night_Hours,
                Night_Overtime_Hours = data.Night_Overtime_Hours,
                Training_Hours = data.Training_Hours,
                Night_Eat_Times = data.Night_Eat,
                Holiday = data.Holiday,
                Update_By = userName,
                Update_Time = DateTime.Now,
                Department = data.Department_Code
            };
            try
            {
                _repositoryAccessor.HRMS_Att_Overtime_Temp.Add(Detail);
                await _repositoryAccessor.Save();
                return new OperationResult(true, "Create Successfully");
            }
            catch (Exception ex)
            {
                return new OperationResult(false, $"Inner exception: {ex.InnerException?.Message ?? "No inner exception message available"}");
            }

        }
        public async Task<OperationResult> Update(HRMS_Att_Overtime_TempDto data, string userName)
        {
            var editItem = await _repositoryAccessor.HRMS_Att_Overtime_Temp.FirstOrDefaultAsync(x =>
                x.Overtime_Date.Date == DateTime.Parse(data.Date_Str).Date &&
                x.Employee_ID == data.Employee_ID &&
                x.Factory == data.Factory);
            if (editItem == null)
                return new OperationResult(false, "No data");
            editItem.Work_Shift_Type = data.Work_Shift_Type;
            editItem.Overtime_Start = data.Overtime_Start;
            editItem.Overtime_End = data.Overtime_End;
            editItem.Overtime_Hours = data.Overtime_Hours;
            editItem.Night_Hours = data.Night_Hours;
            editItem.Night_Overtime_Hours = data.Night_Overtime_Hours;
            editItem.Training_Hours = data.Training_Hours;
            editItem.Night_Eat_Times = data.Night_Eat;
            editItem.Holiday = data.Holiday;
            editItem.Update_By = userName;
            editItem.Update_Time = DateTime.Now;
            _repositoryAccessor.HRMS_Att_Overtime_Temp.Update(editItem);
            try
            {
                await _repositoryAccessor.Save();
                return new OperationResult(true, "Update Successfully");
            }
            catch (Exception ex)
            {
                return new OperationResult(false, $"Inner exception: {ex.InnerException?.Message ?? "No inner exception message available"}");
            }
        }

        public async Task<OperationResult> Delete(HRMS_Att_Overtime_TempDto data)
        {
            var item = await _repositoryAccessor.HRMS_Att_Overtime_Temp.FirstOrDefaultAsync(x =>
                x.USER_GUID == data.USER_GUID &&
                x.Factory == data.Factory &&
                x.Employee_ID == data.Employee_ID &&
                x.Overtime_Date.Date == DateTime.Parse(data.Date_Str).Date);
            if (item == null)
                return new OperationResult(false, "Data not exist");
            _repositoryAccessor.HRMS_Att_Overtime_Temp.Remove(item);
            if (await _repositoryAccessor.Save())
                return new OperationResult(true, "Delete Successfully");
            return new OperationResult(false, "Delete failed");
        }
        #endregion
        #region GetData
        public async Task<PaginationUtility<HRMS_Att_Overtime_TempDto>> GetData(PaginationParam pagination, HRMS_Att_Overtime_TempParam param)
        {
            var data = await GetData(param);
            return PaginationUtility<HRMS_Att_Overtime_TempDto>.Create(data, pagination.PageNumber, pagination.PageSize);
        }

        private async Task<List<HRMS_Att_Overtime_TempDto>> GetData(HRMS_Att_Overtime_TempParam param)
        {
            var permissionGroupQuery = _repositoryAccessor.HRMS_Basic_Role.FindAll(x => x.Factory == param.Factory, true).Select(x => x.Permission_Group);
            var pred = PredicateBuilder.New<HRMS_Att_Overtime_Temp>(x => x.Factory == param.Factory);
            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                pred = pred.And(x => x.Employee_ID.ToLower().Contains(param.Employee_ID.Trim().ToLower()));
            if (!string.IsNullOrWhiteSpace(param.Shift))
                pred = pred.And(x => x.Work_Shift_Type == param.Shift);
            if (!string.IsNullOrWhiteSpace(param.DateFrom))
                pred.And(x => x.Overtime_Date.Date >= Convert.ToDateTime(param.DateFrom).Date);
            if (!string.IsNullOrWhiteSpace(param.DateTo))
                pred.And(x => x.Overtime_Date.Date <= Convert.ToDateTime(param.DateTo).Date);

            var HAOT = _repositoryAccessor.HRMS_Att_Overtime_Temp.FindAll(pred);
            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => (x.Factory == param.Factory) && permissionGroupQuery.Contains(x.Permission_Group));
            var HAWS = _repositoryAccessor.HRMS_Att_Work_Shift.FindAll(x => x.Factory == param.Factory && x.Effective_State == true);
            var HATR = _repositoryAccessor.HRMS_Att_Temp_Record.FindAll(x => x.Factory == param.Factory);
            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.IsActive == true);
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.lang.ToLower());
            var HBC_Holiday = HBC.Where(x => x.Type_Seq == BasicCodeTypeConstant.Holiday && x.Char1 == "Attendance")
                .GroupJoin(HBCL.Where(x => x.Type_Seq == BasicCodeTypeConstant.Holiday),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Code,
                    Code_Name = $"{x.HBC.Code}-{(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                });
            var HBC_WorkShiftType = HBC.Where(x => x.Type_Seq == BasicCodeTypeConstant.WorkShiftType)
                .GroupJoin(HBCL.Where(x => x.Type_Seq == BasicCodeTypeConstant.WorkShiftType),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Code,
                    Code_Name = $"{x.HBC.Code}-{(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                });
            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == param.Factory);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.lang.ToLower());
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

            var result = await HAOT
                .Join(HEP,
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { HAOT = x, HEP = y })
                .GroupJoin(HAWS,
                    x => new { x.HAOT.Work_Shift_Type, Week = (EF.Functions.DateDiffDay(new DateTime(1, 1, 1), x.HAOT.Overtime_Date.AddDays(1)) % 7).ToString() },
                    y => new { y.Work_Shift_Type, y.Week },
                    (x, y) => new { x.HAOT, x.HEP, HAWS = y })
                .SelectMany(x => x.HAWS.DefaultIfEmpty(),
                    (x, y) => new { x.HAOT, x.HEP, HAWS = y })
                .GroupJoin(HATR,
                    x => new { x.HAOT.Employee_ID, Date = x.HAOT.Overtime_Date },
                    y => new { y.Employee_ID, Date = y.Att_Date },
                    (x, y) => new { x.HAOT, x.HEP, x.HAWS, HATR = y })
                .SelectMany(x => x.HATR.DefaultIfEmpty(),
                    (x, y) => new { x.HAOT, x.HEP, x.HAWS, HATR = y })
                .GroupJoin(HBC_WorkShiftType,
                    x => new { Key = x.HAOT.Work_Shift_Type },
                    y => new { Key = y.Code },
                    (x, y) => new { x.HAOT, x.HEP, x.HAWS, x.HATR, HBC_WorkShiftType = y })
                .SelectMany(x => x.HBC_WorkShiftType.DefaultIfEmpty(),
                    (x, y) => new { x.HAOT, x.HEP, x.HAWS, x.HATR, HBC_WorkShiftType = y })
                .GroupJoin(HBC_Holiday,
                    x => new { Key = x.HAOT.Holiday },
                    y => new { Key = y.Code },
                    (x, y) => new { x.HAOT, x.HEP, x.HAWS, x.HATR, x.HBC_WorkShiftType, HBC_Holiday = y })
                .SelectMany(x => x.HBC_Holiday.DefaultIfEmpty(),
                    (x, y) => new { x.HAOT, x.HEP, x.HAWS, x.HATR, x.HBC_WorkShiftType, HBC_Holiday = y })
                .GroupJoin(HOD_Lang,
                    x => new { x.HAOT.Factory, x.HAOT.Department },
                    y => new { y.Factory, y.Department },
                    (x, y) => new { x.HAOT, x.HEP, x.HAWS, x.HATR, x.HBC_WorkShiftType, x.HBC_Holiday, HOD_Lang = y })
                .SelectMany(x => x.HOD_Lang.DefaultIfEmpty(),
                    (x, y) => new { x.HAOT, x.HEP, x.HAWS, x.HATR, x.HBC_WorkShiftType, x.HBC_Holiday, HOD_Lang = y })
                .Select(x => new HRMS_Att_Overtime_TempDto
                {
                    USER_GUID = x.HAOT.USER_GUID,
                    Factory = x.HAOT.Factory,
                    Department_Code = x.HAOT.Department,
                    Department_Name = x.HOD_Lang.Department_Name,
                    Department_Code_Name = x.HOD_Lang != null && !string.IsNullOrWhiteSpace(x.HOD_Lang.Department_Name)
                        ? x.HOD_Lang.Department + "-" + x.HOD_Lang.Department_Name : x.HATR.Department,
                    Employee_ID = x.HAOT.Employee_ID,
                    Local_Full_Name = x.HEP.Local_Full_Name,
                    Date = x.HAOT.Overtime_Date,
                    Date_Str = x.HAOT.Overtime_Date.ToString("yyyy/MM/dd"),
                    Work_Shift_Type = x.HAOT.Work_Shift_Type,
                    Work_Shift_Type_Str = x.HBC_WorkShiftType != null ? x.HBC_WorkShiftType.Code_Name : x.HAOT.Work_Shift_Type,
                    Shift_Time = x.HAWS != null ? x.HAWS.Clock_In + " - " + x.HAWS.Clock_Out : "",
                    Clock_In_Time = x.HATR.Clock_In ,
                    Clock_Out_Time = x.HATR.Clock_Out ,
                    Overtime_Start = x.HAOT.Overtime_Start,
                    Overtime_End = x.HAOT.Overtime_End,
                    Overtime_Hours = x.HAOT.Overtime_Hours,
                    Night_Hours = x.HAOT.Night_Hours,
                    Night_Overtime_Hours = x.HAOT.Night_Overtime_Hours,
                    Training_Hours = x.HAOT.Training_Hours,
                    Night_Eat = x.HAOT.Night_Eat_Times,
                    Holiday = x.HAOT.Holiday,
                    Holiday_Str = x.HBC_Holiday != null ? x.HBC_Holiday.Code_Name : x.HAOT.Holiday,
                    Update_By = x.HAOT.Update_By,
                    Update_Time = x.HAOT.Update_Time,
                    Update_Time_Str = x.HAOT.Update_Time.ToString("yyyy/MM/dd HH:mm:ss")
                }).ToListAsync();
            if (!string.IsNullOrWhiteSpace(param.Department))
                result = result.FindAll(x => x.Department_Code == param.Department);
            return result;
        }
        #endregion
        #region GetList
        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string lang, List<string> roleList)
        {
            var predHBC = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.Factory);

            var factorys = await Queryt_Factory_AddList(roleList);
            predHBC.And(x => factorys.Contains(x.Code));

            var data = await _repositoryAccessor.HRMS_Basic_Code.FindAll(predHBC, true)
                        .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == lang.ToLower(), true),
                           x => new { x.Type_Seq, x.Code },
                           y => new { y.Type_Seq, y.Code },
                           (x, y) => new { HBC = x, HBCL = y }
                        ).SelectMany(x => x.HBCL.DefaultIfEmpty(),
                            (x, y) => new { x.HBC, HBCL = y }
                        ).Select(x => new KeyValuePair<string, string>(
                            x.HBC.Code.Trim(),
                            x.HBC.Code.Trim() + " - " + (x.HBCL != null ? x.HBCL.Code_Name.Trim() : x.HBC.Code_Name.Trim())
                        )).Distinct().ToListAsync();
            return data;
        }
        public async Task<List<KeyValuePair<string, string>>> GetListWorkShiftType(string lang) => await GetDataBasicCode(BasicCodeTypeConstant.WorkShiftType, lang);
        public async Task<ClockInOutTempRecord> GetClockInOutByTempRecord(OvertimeTempPersonalParam param)
        {
            var data = await _repositoryAccessor.HRMS_Att_Temp_Record.FindAll(x => x.Employee_ID == param.EmployeeID && x.Att_Date == Convert.ToDateTime(param.Date), true)
                .Select(x => new ClockInOutTempRecord
                {
                    Clock_In_Time = $"{x.Clock_In:HH:mm}",
                    Clock_Out_Time = x.Clock_Out
                })
                .ToListAsync();
            return data.FirstOrDefault();
        }
        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string lang)
        {
            var data = await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == factory, true)
                .Join(_repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(b => b.Kind == "1" && b.Factory == factory, true),
                    x => x.Division,
                    y => y.Division,
                    (x, y) => x)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == lang.ToLower(), true),
                    x => new { x.Factory, x.Department_Code },
                    y => new { y.Factory, y.Department_Code },
                    (x, y) => new { Department = x, Language = y })
                .SelectMany(
                    x => x.Language.DefaultIfEmpty(),
                    (x, y) => new { x.Department, Language = y })
                .OrderBy(x => x.Department.Department_Code)
                .Select(
                    x => new KeyValuePair<string, string>(
                        x.Department.Department_Code,
                        $"{x.Department.Department_Code} - {(x.Language != null ? x.Language.Name : x.Department.Department_Name)}"
                    )
                ).Distinct().ToListAsync();
            return data;
        }
        public async Task<KeyValuePair<string, string>> GetShiftTimeByWorkShift(string factory, string workShiftType, string date)
        {
            string week = (int)Convert.ToDateTime(date).DayOfWeek + "";
            var data = await _repositoryAccessor.HRMS_Att_Work_Shift.FirstOrDefaultAsync(x => x.Factory == factory && x.Work_Shift_Type == workShiftType && x.Week == week);
            if (data != null)
                return new KeyValuePair<string, string>(data.Work_Shift_Type, $"{data.Clock_In:HH:mm} - {data.Clock_Out:HH:mm}");
            else
                return new KeyValuePair<string, string>(string.Empty, string.Empty);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListHoliday(string lang)
        {
            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Holiday && x.Char1 == "Attendance");
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == lang.ToLower());
            var result = await HBC
               .GroupJoin(HBCL,
                   x => new { x.Code },
                   y => new { y.Code },
                   (x, y) => new { Code = x, CodeLang = y })
               .SelectMany(
                   x => x.CodeLang.DefaultIfEmpty(),
                   (x, y) => new { x.Code, CodeLang = y })
               .Select(
                   x => new KeyValuePair<string, string>(
                       x.Code.Code,
                       $"{x.Code.Code} - {(x.CodeLang != null ? x.CodeLang.Code_Name : x.Code.Code_Name)}"
                   )
               ).Distinct().ToListAsync();
            return result;
        }
        #endregion

        public async Task<OperationResult> DownloadFileExcel(HRMS_Att_Overtime_TempParam param, string userName)
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
                dataCells.Add(new Cell("F" + index, data[i].Date));
                dataCells.Add(new Cell("G" + index, data[i].Work_Shift_Type_Str));
                dataCells.Add(new Cell("H" + index, data[i].Shift_Time));
                dataCells.Add(new Cell("I" + index, data[i].Clock_In_Time));
                dataCells.Add(new Cell("J" + index, data[i].Clock_Out_Time));
                dataCells.Add(new Cell("K" + index, data[i].Overtime_Start));
                dataCells.Add(new Cell("L" + index, data[i].Overtime_End));
                dataCells.Add(new Cell("M" + index, data[i].Overtime_Hours));
                dataCells.Add(new Cell("N" + index, data[i].Night_Hours));
                dataCells.Add(new Cell("O" + index, data[i].Night_Overtime_Hours));
                dataCells.Add(new Cell("P" + index, data[i].Training_Hours));
                dataCells.Add(new Cell("Q" + index, data[i].Night_Eat));
                dataCells.Add(new Cell("R" + index, data[i].Holiday_Str));
                dataCells.Add(new Cell("S" + index, data[i].Update_By));
                dataCells.Add(new Cell("T" + index, data[i].Update_Time_Str));
                index += 1;
            }

            ExcelResult excelResult = ExcelUtility.DownloadExcel(dataCells, "Resources\\Template\\AttendanceMaintenance\\5_1_16_OvertimeTemporaryRecordMaintenance\\Download.xlsx");
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }
    }
}