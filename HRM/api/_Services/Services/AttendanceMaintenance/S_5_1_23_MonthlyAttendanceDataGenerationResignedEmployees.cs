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
    public class S_5_1_23_MonthlyAttendanceDataGenerationResignedEmployees : BaseServices, I_5_1_23_MonthlyAttendanceDataGenerationResignedEmployees
    {
        private static readonly SemaphoreSlim semaphore = new(1, 1);

        public S_5_1_23_MonthlyAttendanceDataGenerationResignedEmployees(DBContext dbContext) : base(dbContext)
        {
        }
        #region Monthly Attendance Data Generation
        public async Task<OperationResult> CheckParam(GenerationResignedParam param)
        {
            var att_Resign_Monthly = await _repositoryAccessor.HRMS_Att_Resign_Monthly
                .FindAll(x => x.Factory == param.Factory
                           && x.Att_Month == DateTime.Parse(param.Att_Month)
                           && x.Employee_ID.CompareTo(param.Employee_ID_Start) >= 0
                           && x.Employee_ID.CompareTo(param.Employee_ID_End) <= 0)
                .Select(x => x.Pass).ToListAsync();

            if (att_Resign_Monthly.Any(x => x == true))
                return new OperationResult(false);

            return new OperationResult(true, att_Resign_Monthly.Any() ? "DeleteData" : null);

        }

        public async Task<OperationResult> MonthlyAttendanceDataGeneration(GenerationResigned param)
        {
            await semaphore.WaitAsync();
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                var att_Month = DateTime.Parse(param.Att_Month);

                var month_First_Day = att_Month.ToFirstDateOfMonth(); // Ngày đầu tháng
                var month_Last_Day = att_Month.ToLastDateOfMonth(); // Ngày cuối tháng

                var resign_Date = DateTime.Parse(param.Resign_Date); // Ngày từ chức

                // Danh sách Code đi làm thực tế 
                var actual_Days_Codes = new List<string>()
                {
                    "A0", "B0", "C0", "D0", "E0",
                    "F0", "J0", "J1", "J2", "J3",
                    "J4", "J5", "I0", "I1", "N0",
                    "G0", "G1", "G2", "H0", "K0",
                    "O0",
                };

                // Danh sách Code (Nghỉ phép)
                var leave_Codes = new List<string>()
                {
                    "A0", "J3", "B0", "C0", "I0",
                    "I1", "N0", "D0", "E0", "F0",
                    "G0", "G2", "G1", "H0", "K0",
                    "J0", "J2"
                };

                // Danh sách từ chức hàng tháng
                List<HRMS_Att_Resign_Monthly> att_Resign_Monthly = new();
                // Chi tiết danh sách từ chức
                List<HRMS_Att_Resign_Monthly_Detail> att_Resign_Monthly_Detail = new();

                var predEmpPersonal = PredicateBuilder.New<HRMS_Emp_Personal>(x
                                            => x.Factory == param.Factory
                                            && x.Onboard_Date <= month_Last_Day
                                            && x.Resign_Date.HasValue == true
                                            && x.Resign_Date.Value >= month_First_Day
                                            && x.Resign_Date.Value <= resign_Date
                                            && x.Employee_ID.CompareTo(param.Employee_ID_Start) >= 0
                                            && x.Employee_ID.CompareTo(param.Employee_ID_End) <= 0);

                var HEPs = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(predEmpPersonal).OrderBy(x => x.Employee_ID).ToListAsync();

                if (!HEPs.Any()) return new OperationResult(false, "Empty employee personal!");

                // STEP 1
                if (param.Is_Delete)
                {
                    var delete = await DeleteData(param);
                    if (delete.IsSuccess == false)
                    {
                        await _repositoryAccessor.RollbackAsync();
                        return new OperationResult(false, "Delete failed!");
                    }
                }


                var maxEffectiveMonth = _repositoryAccessor.HRMS_Att_Use_Monthly_Leave.FindAll(x => x.Factory == param.Factory && x.Effective_Month <= month_First_Day).Max(x => x.Effective_Month);
                var HAUML = _repositoryAccessor.HRMS_Att_Use_Monthly_Leave.FindAll(x => x.Factory == param.Factory && x.Effective_Month == maxEffectiveMonth).ToList();
                var HACR = _repositoryAccessor.HRMS_Att_Change_Record.FindAll(x => x.Factory == param.Factory).ToList();
                var HALM = _repositoryAccessor.HRMS_Att_Leave_Maintain.FindAll(x => x.Factory == param.Factory).ToList();
                var HAOM = _repositoryAccessor.HRMS_Att_Overtime_Maintain.FindAll(x => x.Factory == param.Factory).ToList();
                var HAC = _repositoryAccessor.HRMS_Att_Calendar.FindAll(x => x.Factory == param.Factory && (x.Type_Code == "C05" || x.Type_Code == "C00")).ToList();

                var HECM_HECT = _repositoryAccessor.HRMS_Emp_Contract_Management
                                        .FindAll(x => x.Contract_Start.Date > month_First_Day.Date)
                                        .Join(_repositoryAccessor.HRMS_Emp_Contract_Type.FindAll(x => !x.Probationary_Period),
                                            x => new { x.Factory, x.Contract_Type },
                                            y => new { y.Factory, y.Contract_Type },
                                            (x, y) => new { HECM = x, HECT = y })
                                        .ToList();

                var basic_Code = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Allowance).ToList();
                var att_Calendar = HAC.FindAll(x => x.Att_Date >= month_First_Day && x.Att_Date <= resign_Date).ToList();


                foreach (var emp_Personal in HEPs)
                {
                    var department = string.IsNullOrEmpty(emp_Personal.Employment_Status)
                                    ? emp_Personal.Department
                                    : (emp_Personal.Employment_Status == "A" || emp_Personal.Employment_Status == "S")
                                            ? emp_Personal.Assigned_Department : null;
                    var contract = HECM_HECT.FirstOrDefault(x => x.HECM.Employee_ID == emp_Personal.Employee_ID);

                    // Danh sách chấm công của nhân viên
                    var HACR_with_employees = HACR.Where(x => x.Employee_ID == emp_Personal.Employee_ID).ToList();

                    if (contract != null)
                    {
                        // Từ ngày bắt đầu hợp đồng chính thức 
                        var start_Day = contract.HECM.Contract_Start;

                        if (start_Day.ToYearMonthString() == att_Month.ToYearMonthString()
                            && start_Day > att_Month)
                        {
                            //FUNCTION  proc(Emp_Personal_Values)
                            // Ngày cuối cùng của thời gian thử việc (trước ngày ký hợp đồng chính thức 1 ngày)
                            var end_Date = start_Day.AddDays(-1);

                            List<HRMS_Att_Probation_Monthly_Detail> add_HAPMDs = new();

                            // Danh sách thử việc hàng tháng
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
                                Update_By = param.UserName,
                                Update_Time = param.Current
                            };
                            var Holiday = 0;
                            // Số ngày nghỉ phép
                            if (emp_Personal.Onboard_Date > month_First_Day && emp_Personal.Onboard_Date <= month_Last_Day)
                            {
                                Holiday = HAC.Count(x => x.Att_Date.Date >= emp_Personal.Onboard_Date.Date
                                                        && x.Att_Date.Date <= end_Date.Date
                                                        && x.Type_Code == "C05");

                                HAPM.Salary_Days = (decimal)(end_Date.Date - emp_Personal.Onboard_Date.Date).TotalDays + 1 - Holiday;

                            }
                            else
                            {
                                Holiday = HAC.Count(x => x.Att_Date.Date >= month_First_Day.Date
                                                        && x.Att_Date.Date <= end_Date.Date
                                                        && x.Type_Code == "C05");

                                HAPM.Salary_Days = (decimal)(end_Date.Date - month_First_Day.Date).TotalDays - Holiday;
                                                           
                            }


                            var HALMs = HALM.FindAll(x => x.Leave_Date.Date >= month_First_Day.Date
                                                        && x.Leave_Date.Date <= end_Date.Date
                                                        && x.Employee_ID == emp_Personal.Employee_ID);
                            add_HAPMDs.AddRange(HAUML
                                        .FindAll(x => x.Leave_Type == "1")
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


                            var HAPMDs = add_HAPMDs.FindAll(x => actual_Days_Codes.Contains(x.Leave_Code)).ToList();
                            HAPM.Actual_Days = HAPM.Salary_Days - HAPMDs.Sum(x => x.Days);

                            var HAOMs = HAOM.FindAll(x => x.Overtime_Date.Date >= month_First_Day.Date
                                                        && x.Overtime_Date.Date <= end_Date.Date
                                                        && x.Employee_ID == emp_Personal.Employee_ID);
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
                                    .Select(x => new HRMS_Att_Probation_Monthly_Detail
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
                            HAPM.Resign_Status = (emp_Personal.Onboard_Date > month_First_Day && emp_Personal.Onboard_Date <= month_Last_Day)
                                                || (emp_Personal.Resign_Date > month_First_Day && emp_Personal.Resign_Date <= month_Last_Day) ? "Y" : "N";

                            // Danh sách chấm công theo nhân viên 
                            var HACRs = HACR.Where(x => x.Employee_ID == emp_Personal.Employee_ID
                                                    && x.Att_Date.Date >= month_First_Day.Date
                                                    && x.Att_Date.Date <= end_Date.Date)
                                                .ToList();

                            HAPM.Delay_Early = HACRs.Count(x => x.Leave_Code == "L0" || x.Leave_Code == "L1");
                            HAPM.No_Swip_Card = HACRs.Count(x => x.Leave_Code == "11");

                            HAPM.Food_Expenses = HACRs.Aggregate(0, (result, i) => Query_Food_Expenses_Sum(i, result));

                            // 白班伙食次數 - Số ngày ăn ca ngày
                            HAPM.DayShift_Food = await Query_DayShift_Meal_Sum(param.Factory,
                                                                                month_First_Day,
                                                                                end_Date,
                                                                                emp_Personal.Employee_ID
                                                                                );
                            // 白班伙食次數 - Số ngày ăn ca đêm
                            HAPM.NightShift_Food = Query_OverNight_Shift_Meal_Sum(param.Factory == "SHC", HACR_with_employees, HAC, HAOM,
                                                                                month_First_Day,
                                                                                end_Date, true);


                            var HAC_Att_Dates = HAC.Where(x => x.Att_Date >= month_First_Day
                                                            && x.Att_Date <= end_Date)
                                                    .Select(x => x.Att_Date.Date)
                                                    .ToList();

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
                                                        && x.Att_Date >= month_First_Day
                                                        && x.Att_Date <= month_Last_Day)
                                                    .ToList();
                    var att_Leave_Maintain = HALM.Where(x => x.Employee_ID == emp_Personal.Employee_ID
                                                            && x.Leave_Date >= month_First_Day
                                                            && x.Leave_Date <= month_Last_Day)
                                                    .ToList();

                    var att_Overtime_Maintain = HAOM.Where(x => x.Employee_ID == emp_Personal.Employee_ID
                                                            && x.Overtime_Date >= month_First_Day
                                                            && x.Overtime_Date <= month_Last_Day)
                                                    .ToList();

                    // 白班伙食次數 - Số ngày ăn ca ngày (trong tháng)
                    int? dayShift_Food = await Query_DayShift_Meal_Sum(param.Factory,
                                                                        month_First_Day,
                                                                        month_Last_Day, emp_Personal.Employee_ID);
                    // 白班伙食次數 - Số ngày ăn ca đêm
                    int? nightShift_Food = Query_OverNight_Shift_Meal_Sum(param.Factory == "SHC", HACR_with_employees, HAC, HAOM,
                                                                        month_First_Day,
                                                                        month_Last_Day, true);

                    var resign_Monthly = new HRMS_Att_Resign_Monthly
                    {
                        USER_GUID = emp_Personal.USER_GUID,
                        Division = emp_Personal.Division,
                        Factory = param.Factory,
                        Department = string.IsNullOrEmpty(emp_Personal.Employment_Status) ? emp_Personal.Department : emp_Personal.Assigned_Department,
                        Att_Month = att_Month,
                        Employee_ID = emp_Personal.Employee_ID,
                        Pass = false,
                        Salary_Days = param.Working_Days,
                        Permission_Group = emp_Personal.Permission_Group,
                        Salary_Type = "10",
                        Delay_Early = att_Change_Record.Where(x => x.Leave_Code == "L0" || x.Leave_Code == "L1").Count(),
                        No_Swip_Card = att_Change_Record.Where(x => x.Leave_Code == "11").Count(),
                        Resign_Status = ((emp_Personal.Onboard_Date >= month_First_Day && emp_Personal.Onboard_Date <= month_Last_Day)
                                      || (emp_Personal.Resign_Date >= month_First_Day && emp_Personal.Resign_Date <= month_Last_Day)) ? "Y" : "N",
                        Probation = "N",

                        DayShift_Food = dayShift_Food,
                        NightShift_Food = nightShift_Food,

                        Update_By = param.UserName,
                        Update_Time = param.Current
                    };

                    List<HRMS_Att_Resign_Monthly_Detail> add_HARMDs = new();
                    add_HARMDs.AddRange(HAUML.Where(x => x.Leave_Type == "1")
                        .GroupJoin(att_Leave_Maintain,
                            x => new { x.Factory, Leave_code = x.Code },
                            y => new { y.Factory, y.Leave_code },
                            (x, y) => new { HAUML = x, HALM = y })
                        .SelectMany(x => x.HALM.DefaultIfEmpty(),
                            (x, y) => new { x.HAUML, HALM = y })
                        .OrderBy(x => x.HAUML.Seq)
                        .GroupBy(x => new { x.HAUML.Code, x.HAUML.Leave_Type })
                        .Select(x => new HRMS_Att_Resign_Monthly_Detail
                        {
                            USER_GUID = resign_Monthly.USER_GUID,
                            Division = resign_Monthly.Division,
                            Factory = resign_Monthly.Factory,
                            Att_Month = resign_Monthly.Att_Month,
                            Employee_ID = resign_Monthly.Employee_ID,
                            Leave_Code = x.Key.Code,
                            Leave_Type = x.Key.Leave_Type,
                            Days = x.Key.Code != "J4"
                                ? x.Sum(y => y.HALM != null ? y.HALM.Days : 0)
                                : (x.Max(y => y.HALM?.Leave_Date) == null || x.Min(y => y.HALM?.Leave_Date) == null) ? 0 : (decimal)((x.Max(y => y.HALM?.Leave_Date) - x.Min(y => y.HALM?.Leave_Date))?.Days + 1),
                            Update_By = resign_Monthly.Update_By,
                            Update_Time = resign_Monthly.Update_Time,
                        }));

                    var holiday = att_Calendar.Where(x => x.Type_Code == "C05").Count();
                    var salary_Day_Count = resign_Date.Day - holiday;

                    // Actual_Days
                    if ((emp_Personal.Onboard_Date > month_First_Day && emp_Personal.Onboard_Date <= month_Last_Day)
                     || (emp_Personal.Resign_Date >= month_First_Day && emp_Personal.Resign_Date <= month_Last_Day))
                    {
                        resign_Monthly.Actual_Days = await Query_NewEmployee_Resign_Actual_Days(param.Factory,
                                                                        month_First_Day,
                                                                        month_Last_Day,
                                                                        emp_Personal.Employee_ID,
                                                                        "XXX");
                    }
                    else
                    {
                        // Resign
                        var actual_Days = add_HARMDs.Where(x => actual_Days_Codes.Contains(x.Leave_Code)).Sum(x => x.Days);
                        resign_Monthly.Actual_Days = param.Working_Days - actual_Days;
                    }

                    add_HARMDs.AddRange(HAUML.Where(x => x.Leave_Type == "2")
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
                            .Select(x => new HRMS_Att_Resign_Monthly_Detail
                            {
                                USER_GUID = resign_Monthly.USER_GUID,
                                Division = resign_Monthly.Division,
                                Factory = resign_Monthly.Factory,
                                Att_Month = resign_Monthly.Att_Month,
                                Employee_ID = resign_Monthly.Employee_ID,
                                Leave_Code = x.Key.Code,
                                Leave_Type = x.Key.Leave_Type,
                                Days = x.Sum(y => y.HAOM != null ?
                                        (decimal)y.HAOM.GetType().GetProperty(x.Key.Char2).GetValue(y.HAOM) : 0),
                                Update_By = resign_Monthly.Update_By,
                                Update_Time = resign_Monthly.Update_Time,
                            }));


                    resign_Monthly.Salary_Days = salary_Day_Count;
                    resign_Monthly.Food_Expenses = att_Change_Record.Aggregate(0, (result, i) => Query_Food_Expenses_Sum(i, result));

                    resign_Monthly.Night_Eat_Times = att_Change_Record.Where(x => att_Calendar.All(y => y.Att_Date != x.Att_Date))
                                                                    .GroupJoin(att_Overtime_Maintain,
                                                                        x => new { x.Att_Date, x.Employee_ID },
                                                                        y => new { Att_Date = y.Overtime_Date, y.Employee_ID },
                                                                        (x, y) => new { HACR = x, HAOM = y })
                                                                    .SelectMany(x => x.HAOM.DefaultIfEmpty(),
                                                                        (x, y) => new { x.HACR, HAOM = y })
                                                                    .Aggregate(0, (result, i) => Query_Night_Eat_Sum(i.HACR, i.HAOM, result));
                    att_Resign_Monthly.Add(resign_Monthly);
                    att_Resign_Monthly_Detail.AddRange(add_HARMDs);
                };

                _repositoryAccessor.HRMS_Att_Resign_Monthly.AddMultiple(att_Resign_Monthly);
                _repositoryAccessor.HRMS_Att_Resign_Monthly_Detail.AddMultiple(att_Resign_Monthly_Detail);

                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true);
            }
            catch (Exception)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false);
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



        private async Task<OperationResult> DeleteData(GenerationResigned param)
        {
            var att_Month = DateTime.Parse(param.Att_Month);

            var att_Resign_Monthly_Detail = await _repositoryAccessor.HRMS_Att_Resign_Monthly_Detail
                .FindAll(x => x.Factory == param.Factory
                           && x.Att_Month == att_Month
                           && x.Employee_ID.CompareTo(param.Employee_ID_Start) >= 0
                           && x.Employee_ID.CompareTo(param.Employee_ID_End) <= 0)
                .ToListAsync();

            var att_Resign_Monthly = await _repositoryAccessor.HRMS_Att_Resign_Monthly
                .FindAll(x => x.Factory == param.Factory
                           && x.Att_Month == att_Month
                           && x.Employee_ID.CompareTo(param.Employee_ID_Start) >= 0
                           && x.Employee_ID.CompareTo(param.Employee_ID_End) <= 0)
                .ToListAsync();

            var data_Att_Probation_Monthly_Detail = await _repositoryAccessor.HRMS_Att_Probation_Monthly_Detail
                .FindAll(x => x.Factory == param.Factory
                            && x.Att_Month == att_Month
                            && x.Employee_ID.CompareTo(param.Employee_ID_Start) >= 0
                            && x.Employee_ID.CompareTo(param.Employee_ID_End) <= 0)
                .ToListAsync();

            var data_Att_Probation_Monthly = await _repositoryAccessor.HRMS_Att_Probation_Monthly
                .FindAll(x => x.Factory == param.Factory
                            && x.Att_Month == att_Month
                            && x.Employee_ID.CompareTo(param.Employee_ID_Start) >= 0
                            && x.Employee_ID.CompareTo(param.Employee_ID_End) <= 0)
                .ToListAsync();
            try
            {
                if (att_Resign_Monthly_Detail.Any())
                    _repositoryAccessor.HRMS_Att_Resign_Monthly_Detail.RemoveMultiple(att_Resign_Monthly_Detail);
                if (att_Resign_Monthly.Any())
                    _repositoryAccessor.HRMS_Att_Resign_Monthly.RemoveMultiple(att_Resign_Monthly);
                if (data_Att_Probation_Monthly_Detail.Any())
                    _repositoryAccessor.HRMS_Att_Probation_Monthly_Detail.RemoveMultiple(data_Att_Probation_Monthly_Detail);
                if (data_Att_Probation_Monthly.Any())
                    _repositoryAccessor.HRMS_Att_Probation_Monthly.RemoveMultiple(data_Att_Probation_Monthly);
                return new OperationResult(await _repositoryAccessor.Save());
            }
            catch (Exception)
            {
                return new OperationResult(false);
            }
        }
        #endregion

        #region Monthly Data Close Execute
        public async Task<OperationResult> MonthlyDataCloseExecute(MonthlyAttendanceDataGenerationResignedEmployees_MonthlyDataCloseParam param)
        {
            var att_Resign_Monthly = await _repositoryAccessor.HRMS_Att_Resign_Monthly
                .FindAll(x => x.Factory == param.Factory
                           && x.Att_Month == DateTime.Parse(param.Att_Month))
                .ToListAsync();

            if (!att_Resign_Monthly.Any())
                return new OperationResult(false, "No data");

            if (att_Resign_Monthly.Any(x => x.Pass))
                return new OperationResult(false, "The data has been closed");

            att_Resign_Monthly.ForEach(x => x.Pass = param.Pass == "Y");

            try
            {
                _repositoryAccessor.HRMS_Att_Resign_Monthly.UpdateMultiple(att_Resign_Monthly);
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
        /// Tổng số giờ ăn ca đêm của nhân viên theo thời gian
        /// </summary>
        /// <param name="isSHCFactory"> Có phải tính cho nhà máy SHC </param>
        /// <param name="HAC_with_employees"> Danh sách công theo nhà máy và nhân viên</param>
        /// <param name="calendarsByFactoryAndTypeCode"> Danh sách lịch làm việc theo nhà máy và ca làm</param>
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
            if (!isSHCFactory) return 0; // Default = 0

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