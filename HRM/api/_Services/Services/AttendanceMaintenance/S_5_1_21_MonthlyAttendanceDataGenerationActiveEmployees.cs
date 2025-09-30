using System.Linq;
using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Helper.Utilities;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_1_21_MonthlyAttendanceDataGenerationActiveEmployees : BaseServices, I_5_1_21_MonthlyAttendanceDataGenerationActiveEmployees
    {
        private static readonly SemaphoreSlim semaphore = new(1, 1);

        public S_5_1_21_MonthlyAttendanceDataGenerationActiveEmployees(DBContext dbContext) : base(dbContext)
        {
        }

        #region Monthly Attendance Data Generation
        public async Task<OperationResult> CheckParam(GenerationActiveParam param)
        {
            var att_Month = DateTime.Parse(param.Att_Month);
            var deadline_Start = DateTime.Parse(param.Deadline_Start);
            var deadline_End = DateTime.Parse(param.Deadline_End);

            var alreadyData = await _repositoryAccessor.HRMS_Att_Monthly_Period
                .AnyAsync(x => x.Factory == param.Factory
                           && x.Att_Month != att_Month
                           && ((x.Deadline_Start <= deadline_Start && x.Deadline_End >= deadline_Start)
                           || (x.Deadline_Start <= deadline_End && x.Deadline_End >= deadline_End)
                           || (x.Deadline_Start >= deadline_Start && x.Deadline_End <= deadline_End)));

            if (alreadyData)
                return new OperationResult(false, "Already data or between deadline dates");

            var connectionDate = await _repositoryAccessor.HRMS_Att_Monthly_Period
                .FindAll(x => x.Factory == param.Factory
                           && x.Att_Month < att_Month)
                .Select(x => x.Deadline_End)
                .ToListAsync();

            if (connectionDate.Any())
            {
                if ((deadline_Start - connectionDate.Max()).Days > 1)
                    return new OperationResult(false, $"開始日與上次 {param.Att_Month} 結算期間不連續，請再確認...");
            }

            var predEmpPersonal = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory);

            if (!string.IsNullOrWhiteSpace(param.Department))
                predEmpPersonal.And(x => x.Department == param.Department);

            var emp_Personal = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predEmpPersonal).Select(x => x.USER_GUID).Distinct();

            if (await _repositoryAccessor.HRMS_Att_Monthly
                .AnyAsync(x => x.Factory == param.Factory
                            && x.Att_Month == att_Month
                            && x.Pass == false
                            && x.Employee_ID.CompareTo(param.Employee_ID_Start) >= 0
                            && x.Employee_ID.CompareTo(param.Employee_ID_End) <= 0
                            && emp_Personal.Contains(x.USER_GUID)))
                return new OperationResult(true, "DeleteData");

            return new OperationResult(true);
        }

        public async Task<OperationResult> MonthlyAttendanceDataGeneration(GenerationActiveParam param)
        {
            await semaphore.WaitAsync();
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {

                var att_Month = DateTime.Parse(param.Att_Month);
                var deadline_Start = DateTime.Parse(param.Deadline_Start);
                var deadline_End = DateTime.Parse(param.Deadline_End);

                var month_First_Day = att_Month.ToFirstDateOfMonth();
                var month_Last_Day = att_Month.ToLastDateOfMonth();
                var actual_Days_Codes = new List<string>()
                {
                    "A0", "B0", "C0", "D0", "E0",
                    "F0", "J0", "J1", "J2","J3",
                    "J4", "J5", "I0", "I1", "N0",
                    "G0", "G1", "G2", "H0", "K0",
                    "O0",
                };

                List<HRMS_Att_Monthly> att_Monthly = new();
                List<HRMS_Att_Monthly_Detail> att_Monthly_Detail = new();


                var predEmpPersonal = PredicateBuilder.New<HRMS_Emp_Personal>(x
                                        => x.Factory == param.Factory
                                        && x.Onboard_Date <= month_Last_Day
                                        && (x.Resign_Date.HasValue == false || x.Resign_Date.Value >= month_First_Day)
                                        && x.Employee_ID.CompareTo(param.Employee_ID_Start) >= 0
                                        && x.Employee_ID.CompareTo(param.Employee_ID_End) <= 0);

                if (!string.IsNullOrWhiteSpace(param.Department))
                    predEmpPersonal.And(x => x.Department == param.Department);

                // Nhân viên
                var HEPs = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predEmpPersonal).OrderBy(x => x.Employee_ID).ToList();

                if (!HEPs.Any())
                {
                    await _repositoryAccessor.RollbackAsync();
                    return new OperationResult(false, "Empty employee personal!");
                }

                // Step 1:
                if (param.Is_Delete)
                {
                    var delete = await DeleteData(param);
                    if (!delete.IsSuccess)
                    {
                        await _repositoryAccessor.RollbackAsync();
                        return new OperationResult(false, "Delete failed!");
                    }
                }

                // Step 2: 
                var maxEffectiveMonth = _repositoryAccessor.HRMS_Att_Use_Monthly_Leave
                                        .FindAll(x => x.Factory == param.Factory && x.Effective_Month <= month_First_Day)
                                        .Max(x => x.Effective_Month);

                var HAUML = _repositoryAccessor.HRMS_Att_Use_Monthly_Leave.FindAll(x => x.Factory == param.Factory && x.Effective_Month == maxEffectiveMonth).ToList();
                var HACR = _repositoryAccessor.HRMS_Att_Change_Record.FindAll(x => x.Factory == param.Factory).ToList();
                var HALM = _repositoryAccessor.HRMS_Att_Leave_Maintain.FindAll(x => x.Factory == param.Factory).ToList();
                var HAOM = _repositoryAccessor.HRMS_Att_Overtime_Maintain.FindAll(x => x.Factory == param.Factory).ToList();
                var HAC = _repositoryAccessor.HRMS_Att_Calendar.FindAll(x => x.Factory == param.Factory && (x.Type_Code == "C05" || x.Type_Code == "C00")).ToList();
                var HECM_HECT = _repositoryAccessor.HRMS_Emp_Contract_Management.FindAll(x => x.Contract_Start.Date > month_First_Day.Date)
                    .Join(_repositoryAccessor.HRMS_Emp_Contract_Type.FindAll(x => !x.Probationary_Period),
                        x => new { x.Factory, x.Contract_Type },
                        y => new { y.Factory, y.Contract_Type },
                        (x, y) => new { HECM = x, HECT = y }).ToList();
                var basic_Code = _repositoryAccessor.HRMS_Basic_Code
                    .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Allowance).ToList();

                // lich từ ngày DeadLine được chọn
                var att_Calendar = HAC.Where(x => x.Att_Date >= deadline_Start
                                                && x.Att_Date <= deadline_End)
                                    .Select(x => x.Att_Date)
                                    .ToList();

                // Danh sách nhân viên
                foreach (var emp_Personal in HEPs)
                {
                    var department = string.IsNullOrEmpty(emp_Personal.Employment_Status)
                                ? emp_Personal.Department
                                : (emp_Personal.Employment_Status == "A" || emp_Personal.Employment_Status == "S") ? emp_Personal.Assigned_Department : null;

                    var contract = HECM_HECT.FirstOrDefault(x => x.HECM.Employee_ID == emp_Personal.Employee_ID);
                    var HACR_with_employees = HACR.Where(x => x.Employee_ID == emp_Personal.Employee_ID).ToList();

                    // Trường hợp nếu có hợp đồng
                    if (contract != null)
                    {
                        // Ngày bắt đầu = Thời gian bắt đầu hợp đồng
                        var start_Day = contract.HECM.Contract_Start;

                        if (start_Day.ToYearMonthString() == att_Month.ToYearMonthString()
                            && start_Day > month_First_Day)
                        {
                            //FUNCTION  proc(Emp_Personal_Values)
                            var end_Date = start_Day.AddDays(-1);
                            List<HRMS_Att_Probation_Monthly_Detail> add_HAPMDs = new();

                            var HAPM = new HRMS_Att_Probation_Monthly
                            {
                                USER_GUID = emp_Personal.USER_GUID,
                                Division = emp_Personal.Division,
                                Factory = param.Factory,
                                Att_Month = att_Month,
                                Employee_ID = emp_Personal.Employee_ID,
                                Probation = "Y",
                                Department = department,
                                Pass = false,
                                Permission_Group = emp_Personal.Permission_Group,
                                Salary_Type = "10",
                                Resign_Status = "N",
                                Update_By = param.UserName,
                                Update_Time = param.Current
                            };
                            int Holiday = 0;
                            // Tổng số ngày nghỉ trong tháng từ [Dealine]
                            if (emp_Personal.Onboard_Date > month_First_Day && emp_Personal.Onboard_Date <= month_Last_Day)
                            {
                                Holiday = HAC.Count(x => x.Att_Date.Date >= emp_Personal.Onboard_Date.Date &&
                                                        x.Att_Date.Date <= end_Date.Date &&
                                                        x.Type_Code == "C05");
                                // Số ngày làm viêc (tính lương) = (ngày cuối cùng của thời gian thử việc – ngày bắt đầu giải quyết) + 1 – ngày nghỉ lễ C05 theo lịch
                        
                                HAPM.Salary_Days = (decimal)(end_Date.Date - emp_Personal.Onboard_Date.Date).TotalDays + 1 - Holiday;

                            }
                            else
                            {
                                Holiday = HAC.Count(x => x.Att_Date.Date >= deadline_Start.Date &&
                                                        x.Att_Date.Date <= end_Date.Date &&
                                                        x.Type_Code == "C05");

                                // Số ngày làm viêc (tính lương) = (ngày cuối cùng của thời gian thử việc – ngày bắt đầu giải quyết) + 1 – ngày nghỉ lễ C05 theo lịch
                                HAPM.Salary_Days = (decimal)(end_Date.Date - deadline_Start.Date).TotalDays + 1 - Holiday;
                         
                            }


                            var HALMs = HALM.FindAll(x =>
                                x.Leave_Date.Date >= deadline_Start.Date &&
                                x.Leave_Date.Date <= end_Date.Date &&
                                x.Employee_ID == emp_Personal.Employee_ID);

                            add_HAPMDs.AddRange(HAUML.FindAll(x => x.Leave_Type == "1")
                                                .OrderBy(x => x.Code)
                                                .GroupJoin(HALMs,
                                                    x => x.Code,
                                                    y => y.Leave_code,
                                                    (x, y) => new { HAUML = x, HALM = y })
                                                .SelectMany(x => x.HALM.DefaultIfEmpty(),
                                                    (x, y) => new { x.HAUML, HALM = y })
                                                .GroupBy(x => new { x.HAUML.Code, x.HAUML.Leave_Type })
                                                .Select(x => new HRMS_Att_Probation_Monthly_Detail
                                                {
                                                    USER_GUID = emp_Personal.USER_GUID,
                                                    Division = emp_Personal.Division,
                                                    Factory = param.Factory,
                                                    Att_Month = att_Month,
                                                    Employee_ID = emp_Personal.Employee_ID,
                                                    Leave_Type = x.Key.Leave_Type,
                                                    Leave_Code = x.Key.Code,
                                                    Days = x.Sum(y => y.HALM != null ? y.HALM.Days : 0),
                                                    Update_By = param.UserName,
                                                    Update_Time = param.Current
                                                }));

                            // Chi tiết danh sách thử việc theo Leave_Code
                            var HAPMDs = add_HAPMDs.FindAll(x => actual_Days_Codes.Contains(x.Leave_Code)).ToList();

                            HAPM.Actual_Days = HAPM.Salary_Days - HAPMDs.Sum(x => x.Days);

                            // Danh sách tăng ca
                            var HAOMs = HAOM.FindAll(x => x.Overtime_Date.Date >= deadline_Start.Date &&
                                                        x.Overtime_Date.Date <= end_Date.Date &&
                                                        x.Employee_ID == emp_Personal.Employee_ID);

                            add_HAPMDs.AddRange(HAUML
                                .FindAll(x => x.Leave_Type == "2")
                                .Join(basic_Code,
                                    x => x.Code,
                                    y => y.Code,
                                    (x, y) => new { HAUML = x, HBC = y })
                                .GroupJoin(HAOMs,
                                    x => x.HBC.Char1,
                                    y => y.Holiday,
                                    (x, y) => new { x.HAUML, x.HBC, HAOM = y })
                                .SelectMany(x => x.HAOM.DefaultIfEmpty(),
                                    (x, y) => new { x.HAUML, x.HBC, HAOM = y })
                                .GroupBy(x => new { x.HAUML.Code, x.HAUML.Leave_Type })
                                .Select(x =>
                                new HRMS_Att_Probation_Monthly_Detail
                                {
                                    USER_GUID = emp_Personal.USER_GUID,
                                    Division = emp_Personal.Division,
                                    Factory = param.Factory,
                                    Att_Month = att_Month,
                                    Employee_ID = emp_Personal.Employee_ID,
                                    Leave_Type = x.Key.Leave_Type,
                                    Leave_Code = x.Key.Code,
                                    Days = x.Sum(y => y.HAOM != null ? (decimal)y.HAOM.GetType().GetProperty(y.HBC.Char2).GetValue(y.HAOM) : 0),
                                    Update_By = param.UserName,
                                    Update_Time = param.Current
                                }));

                            // Dữ liệu chấm công

                            var HACRs = HACR_with_employees.Where(x => x.Att_Date.Date >= deadline_Start.Date &&
                                                                x.Att_Date.Date <= end_Date.Date)
                                                            .ToList();

                            // Cập nhật thông tin thử việc
                            HAPM.Delay_Early = HACRs.Count(x => x.Leave_Code == "L0" || x.Leave_Code == "L1");
                            HAPM.No_Swip_Card = HACRs.Count(x => x.Leave_Code == "11");
                            HAPM.Food_Expenses = HACRs.Aggregate(0, (result, i) => Query_Food_Expenses_Sum(i, result));

                            // 白班伙食次數 - Số ngày ăn ca ngày
                            HAPM.DayShift_Food = await Query_DayShift_Meal_Sum(param.Factory, deadline_Start, end_Date, emp_Personal.Employee_ID);
                            // 白班伙食次數 - Số ngày ăn ca đêm
                            HAPM.NightShift_Food = Query_OverNight_Shift_Meal_Sum(param.Factory == "SHC", HACR_with_employees, HAC, HAOM,
                                                                                deadline_Start,
                                                                                end_Date, true);

                            var HAC_Att_Dates = HAC.Where(x => x.Att_Date >= deadline_Start && x.Att_Date <= end_Date)
                                                    .Select(x => x.Att_Date.Date)
                                                    .ToList();


                            // 2.1.12 Night Eat Times
                            HAPM.Night_Eat_Times = HACRs.Where(x => !HAC_Att_Dates.Contains(x.Att_Date.Date))
                                                        .GroupJoin(HAOMs,
                                                            x => new { x.Att_Date, x.Employee_ID },
                                                            y => new { Att_Date = y.Overtime_Date, y.Employee_ID },
                                                            (x, y) => new { HACR = x, HAOM = y })
                                                        .SelectMany(x => x.HAOM.DefaultIfEmpty(),
                                                            (x, y) => new { x.HACR, HAOM = y })
                                                        .Aggregate(0, (result, i) => Query_Night_Eat_Sum(i.HACR, i.HAOM, result));

                            _repositoryAccessor.HRMS_Att_Probation_Monthly.Add(HAPM);
                            _repositoryAccessor.HRMS_Att_Probation_Monthly_Detail.AddMultiple(add_HAPMDs);
                            await _repositoryAccessor.Save();
                        }
                    }


                    var att_Change_Record = HACR.Where(x => x.Employee_ID == emp_Personal.Employee_ID
                                                        && x.Att_Date >= deadline_Start
                                                        && x.Att_Date <= deadline_End)
                                                .ToList();

                    var att_Leave_Maintain = HALM.Where(x => x.Leave_Date >= deadline_Start &&
                                                        x.Leave_Date <= deadline_End &&
                                                        x.Employee_ID == emp_Personal.Employee_ID)
                                                .ToList();

                    var att_Overtime_Maintain = HAOM.Where(x => x.Employee_ID == emp_Personal.Employee_ID &&
                                                        x.Overtime_Date >= deadline_Start &&
                                                        x.Overtime_Date <= deadline_End)
                                                    .ToList();

                    // 白班伙食次數 - Số ngày ăn ca ngày
                    int? dayShift_Food = await Query_DayShift_Meal_Sum(param.Factory, month_First_Day, month_Last_Day, emp_Personal.Employee_ID);
                    // 白班伙食次數 - Số ngày ăn ca đêm
                    int? nightShift_Food = Query_OverNight_Shift_Meal_Sum(param.Factory == "SHC", HACR_with_employees, HAC, HAOM,
                                                                        month_First_Day,
                                                                        month_Last_Day, true);

                    var monthly = new HRMS_Att_Monthly
                    {
                        USER_GUID = emp_Personal.USER_GUID,
                        Division = emp_Personal.Division,
                        Factory = param.Factory,
                        Att_Month = att_Month,
                        Department = department,
                        Employee_ID = emp_Personal.Employee_ID,
                        Pass = false,
                        Salary_Days = param.Working_Days,
                        Permission_Group = emp_Personal.Permission_Group,
                        Salary_Type = "10",
                        Delay_Early = att_Change_Record.Where(x => x.Leave_Code == "L0" || x.Leave_Code == "L1").Count(),
                        No_Swip_Card = att_Change_Record.Where(x => x.Leave_Code == "11").Count(),

                        DayShift_Food = dayShift_Food, // Số ngày ăn trưa
                        NightShift_Food = nightShift_Food, // Số ngày ăn đêm

                        Resign_Status = ((emp_Personal.Onboard_Date >= month_First_Day && emp_Personal.Onboard_Date <= month_Last_Day)
                                 || (emp_Personal.Resign_Date >= month_First_Day && emp_Personal.Resign_Date <= month_Last_Day)) ? "Y" : "N",
                        Probation = "N",
                        Update_By = param.UserName,
                        Update_Time = param.Current
                    };

                    List<HRMS_Att_Monthly_Detail> add_HAMDs = new();
                    add_HAMDs.AddRange(HAUML.Where(x => x.Leave_Type == "1")
                        .GroupJoin(att_Leave_Maintain,
                            x => new { x.Factory, Leave_code = x.Code },
                            y => new { y.Factory, y.Leave_code },
                            (x, y) => new { HAUML = x, HALM = y })
                        .SelectMany(x => x.HALM.DefaultIfEmpty(),
                            (x, y) => new { x.HAUML, HALM = y })
                        .OrderBy(x => x.HAUML.Seq)
                        .GroupBy(x => new { x.HAUML.Code, x.HAUML.Leave_Type })
                        .Select(x => new HRMS_Att_Monthly_Detail
                        {
                            USER_GUID = monthly.USER_GUID,
                            Division = monthly.Division,
                            Factory = monthly.Factory,
                            Att_Month = monthly.Att_Month,
                            Employee_ID = monthly.Employee_ID,
                            Leave_Code = x.Key.Code,
                            Leave_Type = x.Key.Leave_Type,
                            Days = x.Sum(y => y.HALM != null ? y.HALM.Days : 0),
                            Update_By = monthly.Update_By,
                            Update_Time = monthly.Update_Time
                        }));

                    // Actual_Days
                    if (monthly.Resign_Status == "N")
                    {
                        // Active Employee
                        var actual_Days = add_HAMDs.Where(x => actual_Days_Codes.Contains(x.Leave_Code)).Sum(x => x.Days);
                        monthly.Actual_Days = param.Working_Days - actual_Days;
                    }
                    else
                    {
                        // New Employee or Resign
                        var count_Values = att_Change_Record.Where(x => x.Leave_Code != "K0" && x.Holiday == "XXX").Count();
                        var sum_Leave = att_Leave_Maintain.Where(x => x.Leave_code != "K0").Sum(x => x.Days);

                        // Nếu nhân viên không cần phải bấm thẻ
                        if (emp_Personal.Swipe_Card_Option != false)
                            monthly.Actual_Days = count_Values - sum_Leave;
                        else
                        {
                            var count_Holiday = 0;
                            var x_ymd = emp_Personal.Onboard_Date;
                            while (month_Last_Day >= x_ymd)
                            {
                                if (x_ymd.DayOfWeek == 0)
                                    count_Holiday += 1;

                                x_ymd = x_ymd.AddDays(1);

                                if (x_ymd > deadline_End)
                                    break;
                            }
                            monthly.Actual_Days = (deadline_End - emp_Personal.Onboard_Date).Days - sum_Leave + 1 - count_Holiday;
                        }
                    }


                    add_HAMDs.AddRange(HAUML.Where(x => x.Leave_Type == "2")
                            .Join(basic_Code,
                                x => x.Code,
                                y => y.Code,
                                (x, y) => new { HAUML = x, HBC = y })
                            .GroupJoin(att_Overtime_Maintain,
                                x => new { x.HAUML.Factory, Holiday = x.HBC.Char1 },
                                y => new { y.Factory, y.Holiday },
                                (x, y) => new { x.HAUML, x.HBC, HAOM = y })
                            .SelectMany(x => x.HAOM.DefaultIfEmpty(),
                                (x, y) => new { x.HAUML, x.HBC, HAOM = y })
                            .OrderBy(x => x.HAUML.Seq)
                            .GroupBy(x => new
                            {
                                x.HAUML.Code,
                                x.HAUML.Factory,
                                x.HBC.Char2,
                                x.HAUML.Leave_Type,
                            })
                            .Select(x => new HRMS_Att_Monthly_Detail
                            {
                                USER_GUID = monthly.USER_GUID,
                                Division = monthly.Division,
                                Factory = monthly.Factory,
                                Att_Month = monthly.Att_Month,
                                Employee_ID = monthly.Employee_ID,
                                Leave_Code = x.Key.Code,
                                Leave_Type = x.Key.Leave_Type,
                                Days = x.Sum(y => y.HAOM != null ? (decimal)y.HAOM.GetType().GetProperty(x.Key.Char2).GetValue(y.HAOM) : 0),
                                Update_By = monthly.Update_By,
                                Update_Time = monthly.Update_Time,
                            }));


                    if (emp_Personal.Onboard_Date > month_First_Day && emp_Personal.Onboard_Date <= month_Last_Day)
                    {
                        var holiday = HAC.Where(x => x.Att_Date >= emp_Personal.Onboard_Date && x.Att_Date <= month_Last_Day && x.Type_Code == "C05").Count();
                        monthly.Salary_Days = (month_Last_Day - emp_Personal.Onboard_Date).Days + 1 - holiday;
                    }

                    monthly.Food_Expenses = att_Change_Record.Aggregate(0, (result, i) => Query_Food_Expenses_Sum(i, result));
                    monthly.Night_Eat_Times = att_Change_Record
                                            .Where(x => !att_Calendar.Contains(x.Att_Date))
                                            .GroupJoin(att_Overtime_Maintain,
                                                x => new { x.Att_Date, x.Employee_ID },
                                                y => new { Att_Date = y.Overtime_Date, y.Employee_ID },
                                                (x, y) => new { HACR = x, HAOM = y })
                                            .SelectMany(x => x.HAOM.DefaultIfEmpty(),
                                                (x, y) => new { x.HACR, HAOM = y })
                                            .Aggregate(0, (result, i) => Query_Night_Eat_Sum(i.HACR, i.HAOM, result));
                    att_Monthly.Add(monthly);
                    att_Monthly_Detail.AddRange(add_HAMDs);
                };


                var att_Monthly_Period = new HRMS_Att_Monthly_Period()
                {
                    Factory = param.Factory,
                    Att_Month = att_Month,
                    Deadline_Start = DateTime.Parse(param.Deadline_Start),
                    Deadline_End = DateTime.Parse(param.Deadline_End),
                    Update_By = param.UserName,
                    Update_Time = param.Current
                };
                _repositoryAccessor.HRMS_Att_Monthly.AddMultiple(att_Monthly);
                _repositoryAccessor.HRMS_Att_Monthly_Detail.AddMultiple(att_Monthly_Detail);
                _repositoryAccessor.HRMS_Att_Monthly_Period.Add(att_Monthly_Period);

                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, e);
            }
            finally
            {
                semaphore.Release();
            }
        }

        #region Query_Food_Expenses_Sum
        private readonly Func<HRMS_Att_Change_Record, int, int> Query_Food_Expenses_Sum = (change_Record, food_Expenses) =>
        {
            var work_Shift_Type = new List<string>() { "A0", "B0", "C0" };

            if (!work_Shift_Type.Contains(change_Record.Work_Shift_Type))
            {
                if ((change_Record.Leave_Code == "00" || change_Record.Leave_Code == "02")
                    && (change_Record.Overtime_ClockIn.CompareTo("1900") >= 0
                     || change_Record.Overtime_ClockOut.CompareTo("1900") >= 0
                     || change_Record.Clock_Out.CompareTo("1900") >= 0))
                    food_Expenses += 1;

                if (change_Record.Leave_Code == "D0")
                    food_Expenses += 1;
            }
            else
            {
                if (change_Record.Days < 1 && change_Record.Clock_In != "0000")
                    food_Expenses += 1;
            }
            return food_Expenses;
        };
        #endregion

        #region Query_Night_Eat_Sum
        private readonly Func<HRMS_Att_Change_Record, HRMS_Att_Overtime_Maintain, int, int> Query_Night_Eat_Sum = (change_Record, overtime_Maintain, night_Eat) =>
        {
            string formattedClockIn = change_Record.Overtime_ClockIn.Insert(2, ":");
            string formattedClockOut = change_Record.Overtime_ClockOut.Insert(2, ":");

            // Chuyển đổi chuỗi thành TimeSpan hoặc DateTime
            TimeSpan clockInTime = TimeSpan.ParseExact(formattedClockIn, @"hh\:mm", null);
            TimeSpan clockOutTime = TimeSpan.ParseExact(formattedClockOut, @"hh\:mm", null);

            // Tính sự chênh lệch thời gian
            TimeSpan difference = clockOutTime.Subtract(clockInTime);

            // Lấy sự chênh lệch theo phút
            int minutesDifference = (int)difference.TotalMinutes;
            var work_Shift_Type = new List<string>() { "00", "10", "40", "50", "60", "G0", "H0", "S0", "T0" };
            var flag = "N";
            if (work_Shift_Type.Contains(change_Record.Work_Shift_Type))
            {
                if (minutesDifference >= 90 && minutesDifference <= 120 && change_Record.Clock_Out.CompareTo("1900") < 0)
                    flag = "Y";
            }
            else if (minutesDifference >= 90 && minutesDifference <= 120)
                flag = "Y";

            if (flag == "Y")
                night_Eat += 1;

            return night_Eat;
        };
        #endregion

        private async Task<OperationResult> DeleteData(GenerationActiveParam param)
        {
            var att_Month = DateTime.Parse(param.Att_Month);
            var predEmp = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory);

            if (!string.IsNullOrWhiteSpace(param.Department))
                predEmp.And(x => x.Department == param.Department);

            var emp_Personal = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predEmp).Select(x => x.USER_GUID).Distinct();

            var data_Att_Monthly_Detail = await _repositoryAccessor.HRMS_Att_Monthly_Detail
                .FindAll(x => x.Factory == param.Factory
                           && x.Att_Month == att_Month
                           && x.Employee_ID.CompareTo(param.Employee_ID_Start) >= 0
                           && x.Employee_ID.CompareTo(param.Employee_ID_End) <= 0
                           && emp_Personal.Contains(x.USER_GUID))
                .ToListAsync();

            var data_Att_Monthly = await _repositoryAccessor.HRMS_Att_Monthly
                .FindAll(x => x.Factory == param.Factory
                           && x.Att_Month == att_Month
                           && x.Employee_ID.CompareTo(param.Employee_ID_Start) >= 0
                           && x.Employee_ID.CompareTo(param.Employee_ID_End) <= 0
                           && emp_Personal.Contains(x.USER_GUID))
                .ToListAsync();

            var data_Att_Probation_Monthly_Detail = await _repositoryAccessor.HRMS_Att_Probation_Monthly_Detail
                .FindAll(x => x.Factory == param.Factory
                           && x.Att_Month == att_Month
                           && x.Employee_ID.CompareTo(param.Employee_ID_Start) >= 0
                           && x.Employee_ID.CompareTo(param.Employee_ID_End) <= 0
                           && emp_Personal.Contains(x.USER_GUID))
                .ToListAsync();

            var data_Att_Probation_Monthly = await _repositoryAccessor.HRMS_Att_Probation_Monthly
                           .FindAll(x => x.Factory == param.Factory
                                      && x.Att_Month == att_Month
                                      && x.Employee_ID.CompareTo(param.Employee_ID_Start) >= 0
                                      && x.Employee_ID.CompareTo(param.Employee_ID_End) <= 0
                                      && emp_Personal.Contains(x.USER_GUID))
                           .ToListAsync();
            var data_Att_Monthly_Period = await _repositoryAccessor.HRMS_Att_Monthly_Period
                .FindAll(x => x.Factory == param.Factory
                           && x.Att_Month == att_Month)
                .ToListAsync();

            try
            {
                if (data_Att_Monthly_Detail.Any())
                    _repositoryAccessor.HRMS_Att_Monthly_Detail.RemoveMultiple(data_Att_Monthly_Detail);
                if (data_Att_Monthly.Any())
                    _repositoryAccessor.HRMS_Att_Monthly.RemoveMultiple(data_Att_Monthly);
                if (data_Att_Probation_Monthly_Detail.Any())
                    _repositoryAccessor.HRMS_Att_Probation_Monthly_Detail.RemoveMultiple(data_Att_Probation_Monthly_Detail);
                if (data_Att_Probation_Monthly.Any())
                    _repositoryAccessor.HRMS_Att_Probation_Monthly.RemoveMultiple(data_Att_Probation_Monthly);
                if (data_Att_Monthly_Period.Any())
                    _repositoryAccessor.HRMS_Att_Monthly_Period.RemoveMultiple(data_Att_Monthly_Period);

                return new OperationResult(await _repositoryAccessor.Save());
            }
            catch (Exception)
            {
                return new OperationResult(false);
            }
        }
        #endregion

        #region Search Already Deadline Data
        public async Task<PaginationUtility<SearchAlreadyDeadlineDataMain>> SearchAlreadyDeadlineData(PaginationParam pagination, SearchAlreadyDeadlineDataParam param)
        {
            var pred = PredicateBuilder.New<HRMS_Att_Monthly_Period>(true);

            if (!string.IsNullOrWhiteSpace(param.Factory))
                pred.And(x => x.Factory == param.Factory.Trim());

            if (!string.IsNullOrWhiteSpace(param.Att_Month_Start) && !string.IsNullOrWhiteSpace(param.Att_Month_End))
                pred.And(x => x.Att_Month >= DateTime.Parse(param.Att_Month_Start)
                           && x.Att_Month <= DateTime.Parse(param.Att_Month_End));

            var data = await _repositoryAccessor.HRMS_Att_Monthly_Period.FindAll(pred, true)
                .Select(x => new SearchAlreadyDeadlineDataMain
                {
                    Factory = x.Factory,
                    Att_Month = x.Att_Month.ToString("yyyy/MM"),
                    Deadline_Start = x.Deadline_Start.ToString("yyyy/MM/dd"),
                    Deadline_End = x.Deadline_End.ToString("yyyy/MM/dd"),
                    Update_By = x.Update_By,
                    Update_Time = x.Update_Time.ToString("yyyy/MM/dd HH:mm:ss"),
                }).ToListAsync();

            return PaginationUtility<SearchAlreadyDeadlineDataMain>.Create(data, pagination.PageNumber, pagination.PageSize);
        }
        #endregion

        #region Monthly Data Close Execute
        public async Task<OperationResult> MonthlyDataCloseExecute(MonthlyAttendanceDataGenerationActiveEmployees_MonthlyDataCloseParam param)
        {
            var att_Monthly = await _repositoryAccessor.HRMS_Att_Monthly
                .FindAll(x => x.Factory == param.Factory
                           && x.Att_Month == DateTime.Parse(param.Att_Month))
                .ToListAsync();

            if (!att_Monthly.Any())
                return new OperationResult(false, "No data");

            if (param.Pass == "Y")
            {
                if (att_Monthly.Any(x => x.Pass))
                    return new OperationResult(false, "The data has been closed");

                att_Monthly.ForEach(x => x.Pass = true);
            }
            else if (param.Pass == "N")
            {
                if (att_Monthly.Any(x => !x.Pass))
                    return new OperationResult(false, "The account has not been closed yet");

                att_Monthly.ForEach(x => x.Pass = false);
            }

            try
            {
                _repositoryAccessor.HRMS_Att_Monthly.UpdateMultiple(att_Monthly);
                await _repositoryAccessor.Save();
                return new OperationResult(true);
            }
            catch (Exception)
            {
                return new OperationResult(false);
            }
        }
        #endregion

        #region Get List
        public async Task<List<KeyValuePair<string, string>>> Queryt_Factory_AddList(string userName, string language)
        {
            var factoriesByAccount = await GetFactoryByAccount(userName);
            var factories = await Query_HRMS_Basic_Code(BasicCodeTypeConstant.Factory, language);

            return factories.IntersectBy(factoriesByAccount, x => x.Key).ToList();
        }
        public async Task<List<KeyValuePair<string, string>>> Query_DropDown_List(string factory, string language)
        {
            var comparisonDepartment = await Query_Department_List(factory);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                    .FindAll(x =>
                        x.Factory == factory &&
                        x.Language_Code.ToLower() == language.ToLower())
                    .ToList();
            var dataDept = comparisonDepartment.GroupJoin(HODL,
                    x => new {x.Division, x.Department_Code},
                    y => new {y.Division, y.Department_Code},
                    (x, y) => new { dept = x, hodl = y })
                    .SelectMany(x => x.hodl.DefaultIfEmpty(),
                    (x, y) => new { x.dept, hodl = y });
            return dataDept.Select(x => new KeyValuePair<string, string>(x.dept.Department_Code, $"{(x.hodl != null ? x.hodl.Name : x.dept.Department_Name)}")).Distinct().ToList();
        }

        private async Task<List<string>> GetFactoryByAccount(string userName)
        {
            return await _repositoryAccessor.HRMS_Basic_Role.FindAll(true)
                .Join(_repositoryAccessor.HRMS_Basic_Account_Role.FindAll(x => x.Account == userName, true),
                 HBR => HBR.Role,
                 HBAR => HBAR.Role,
                 (x, y) => new { HBR = x, HBAR = y })
                .Select(x => x.HBR.Factory)
                .Distinct()
                .ToListAsync();
        }

        private async Task<List<KeyValuePair<string, string>>> Query_HRMS_Basic_Code(string Type_Seq, string Language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == Type_Seq, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
        }


        /// <summary>
        /// 2.1.10 Query_OverNight_Shift_Meal_Sum
        /// Tổng số giờ ăn ca đêm của nhân viên theo thời gian
        /// </summary>
        /// <param name="isSHCFactory"> Có phải tính cho nhà máy SHC </param>
        /// <param name="HAC_with_employees"> Danh sách công theo nhà máy & nhân viên</param>
        /// <param name="calendarsByFactoryAndTypeCode"> Danh sách lịch làm việc theo nhà máy & ca làm</param>
        /// <param name="overtime_MaintainsByFactory"> Lịch tăng ca của nhân viên theo nhà máy</param>
        /// <param name="month_First_day">Thời  gian bắt đầu</param>
        /// <param name="month_Last_day"> Thời gian kết thúc</param>
        /// <param name="isNightEat">Có tính thời gian ăn đêm hay không ?</param>
        /// <returns></returns>      
        private static int? Query_OverNight_Shift_Meal_Sum(bool isSHCFactory,
                                                        List<HRMS_Att_Change_Record> HAC_with_employees,
                                                        List<HRMS_Att_Calendar> calendarsByFactoryAndTypeCode,
                                                        List<HRMS_Att_Overtime_Maintain> overtime_MaintainsByFactory,
                                                        DateTime month_First_day,
                                                        DateTime month_Last_day,
                                                        bool isNightEat = false)
        {
            if (!isSHCFactory) return 0; // default = 0

            int totalEatTimes = 0;

            var calendars = calendarsByFactoryAndTypeCode
                                .Where(c => c.Att_Date >= month_First_day
                                        && c.Att_Date <= month_Last_day)
                                .Select(x => x.Att_Date)
                                .ToList();

            // Mặc định lấy thời gian ban ngày
            var records = HAC_with_employees
                            .Where(r => r.Att_Date >= month_First_day
                                    && r.Att_Date <= month_Last_day
                                    && !calendars.Any(att_Date => att_Date == r.Att_Date))
                            .ToList();

            // Nếu là tính giờ ăn đêm
            if (isNightEat) records = records.Where(r => r.Work_Shift_Type == "C0").ToList(); // Tính giờ cho ca đêm

            foreach (var record in records)
            {
                // Lấy giờ ăn theo giờ ăn tăng ca
                var overtimeHours = overtime_MaintainsByFactory
                                        .FindAll(o => o.Overtime_Date == record.Att_Date
                                                && o.Employee_ID == record.Employee_ID)
                                        .Sum(o => o.Overtime_Hours + o.Night_Overtime_Hours + o.Training_Hours);

                // Nếu có tăng ca thì có ăn đêm (Tính giờ ăn đêm)
                if (overtimeHours > 0) totalEatTimes++;
            }

            return totalEatTimes;
        }
        #endregion


    }
}