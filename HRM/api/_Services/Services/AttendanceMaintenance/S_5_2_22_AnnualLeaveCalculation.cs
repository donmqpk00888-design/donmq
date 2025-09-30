using System.Drawing;
using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Helper.Utilities;
using API.Models;
using LinqKit;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_2_22_AnnualLeaveCalculation : BaseServices, I_5_2_22_AnnualLeaveCalculation
    {
        public S_5_2_22_AnnualLeaveCalculation(DBContext dbContext) : base(dbContext)
        {
        }
        #region Download
        public async Task<OperationResult> Download(AnnualLeaveCalculationParam param)
        {
            var data = await GetData(param);

            if (!data.Any())
                return new OperationResult(false, "No data!");

            var start_Month = DateTime.Parse(param.Start_Year_Month);
            var end_Month = DateTime.Parse(param.End_Year_Month);
            var Department = await GetListDepartment(param.Factory, param.Language);
            List<Cell> cells = new()
            {
                new Cell("B2", param.Factory),
                new Cell("B3", param.UserName),
                new Cell("D2", param.Kind == "O" ?  "在職 Currently Employed" : "離職 Resigned"),
                new Cell("D3", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
                new Cell("F2", start_Month.ToString("yyyy/MM")),
                new Cell("H2", end_Month.ToString("yyyy/MM")),
                new Cell("J2", Department.FirstOrDefault(x => x.Key == param.Department).Value)
            };

            var listYearMonth = GetListYearMonth(ToFirstDateOfMonth(start_Month), ToFirstDateOfMonth(end_Month));

            var columnMonth = 19;
            Aspose.Cells.Style style = new Aspose.Cells.CellsFactory().CreateStyle();
            style.Pattern = Aspose.Cells.BackgroundType.Solid;
            style.ForegroundColor = Color.FromArgb(255, 242, 204);
            style = AsposeUtility.SetAllBorders(style);

            foreach (var month in listYearMonth)
            {
                cells.Add(new Cell(4, columnMonth, month.ToString("yyyy/MM"), style));
                cells.Add(new Cell(5, columnMonth, month.ToString("yyyy/MM"), style));
                columnMonth++;
            }

            var row = 6;
            foreach (var item in data)
            {
                var column = 19;
                foreach (var value in item.YearMonth)
                    cells.Add(new Cell(row, column++, value));

                row++;
            }

            List<Table> tables = new() { new("result", data) };
            ConfigDownload configDownload = new(false);
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                tables, 
                cells, 
                "Resources\\Template\\AttendanceMaintenance\\5_2_22_AnnualLeaveCalculation\\Download.xlsx", 
                configDownload
            );
            if (excelResult.IsSuccess)
                return new OperationResult(excelResult.IsSuccess, new { TotalRows = data.Count, Excel = excelResult.Result });
            else
                return new OperationResult(excelResult.IsSuccess, excelResult.Error);
        }
        #endregion

        #region Get Total Rows
        public async Task<int> GetTotalRows(AnnualLeaveCalculationParam param)
        {
            var data = await GetData(param);
            return data.Count;
        }
        #endregion

        #region Get Data
        private async Task<List<AnnualLeaveCalculationDto>> GetData(AnnualLeaveCalculationParam param)
        {
            var Input_Start_Date = DateTime.Parse(param.Start_Year_Month);
            var Input_End_Date = DateTime.Parse(param.End_Year_Month);
            var First_Date = ToFirstDateOfMonth(Input_Start_Date);
            var End_Date = ToLastDateOfMonth(Input_End_Date);
            var listYearMonth = GetListYearMonth(First_Date, End_Date);
            var result = new List<AnnualLeaveCalculationDto>();

            var predPersonal = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory
                                                                        && x.Onboard_Date.Date <= End_Date.Date
                                                                        && param.Permission_Group.Contains(x.Permission_Group));

            if (param.Kind == "O")
                predPersonal.And(x => x.Resign_Date.HasValue == false || x.Resign_Date.Value >= DateTime.Now);
            else
                predPersonal.And(x => x.Resign_Date.HasValue
                                   && x.Resign_Date.Value.Date >= First_Date.Date
                                   && x.Resign_Date.Value.Date <= End_Date.Date);

            if (!string.IsNullOrWhiteSpace(param.Department))
                predPersonal.And(x => x.Department == param.Department);
            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predPersonal, true).ToList();

            var leave_Codes = new List<string>() { "A0", "B0", "C0", "G0", "H0", "I0", "I1", "J0", "J3", "J4", "O0" };
            var HALM = _repositoryAccessor.HRMS_Att_Leave_Maintain
                .FindAll(x => x.Factory == param.Factory
                           && leave_Codes.Contains(x.Leave_code), true).ToList();

            // lịch làm việc [Theo nhà máy và ....]
            var HAC = _repositoryAccessor.HRMS_Att_Calendar.FindAll(x => x.Factory == param.Factory && x.Type_Code == "C05", true).ToList();

            // Danh sách chấm công theo nhà máy
            var HACR = _repositoryAccessor.HRMS_Att_Change_Record.FindAll(x => x.Factory == param.Factory, true).ToList();

            // Danh sách nghỉ 
            var HAAL = _repositoryAccessor.HRMS_Att_Annual_Leave.FindAll(x => x.Factory == param.Factory
                                                                        && x.Annual_Start >= ToFirstDateOfYear(Input_Start_Date)
                                                                        && x.Annual_End <= ToLastDateOfYear(Input_Start_Date), true)
                                                                .GroupBy(x => new { x.USER_GUID, x.Employee_ID })
                                                                .Select(x => new
                                                                {
                                                                    x.Key.USER_GUID,
                                                                    x.Key.Employee_ID,
                                                                    Days = x.Sum(y => y.Total_Days)
                                                                }).ToList();

            var HAWTD = _repositoryAccessor.HRMS_Att_Work_Type_Days.FindAll(x => x.Factory == param.Factory, true);

            var PositionTitle = await GetDataBasicCode(BasicCodeTypeConstant.JobTitle, param.Language);
            var WorkType = await GetDataBasicCode(BasicCodeTypeConstant.WorkType, param.Language);
            List<string> filterStatus = new() { "A", "S" };

            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll(true).ToList();
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true).ToList();
            var Department = HOD
            .GroupJoin(HODL,
                HOD => new { HOD.Division, HOD.Factory, HOD.Department_Code },
                HODL => new { HODL.Division, HODL.Factory, HODL.Department_Code },
                (HOD, HODL) => new { HOD, HODL })
            .SelectMany(x => x.HODL.DefaultIfEmpty(),
                (x, HODL) => new { x.HOD, HODL })
            .Select(x => new
            {
                x.HOD.Factory,
                x.HOD.Division,
                x.HOD.Department_Code,
                Department_Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name
            }).ToList();

            HEP.ForEach(item =>
            {
                var data = new AnnualLeaveCalculationDto()
                {
                    Department_Code = !string.IsNullOrWhiteSpace(item.Employment_Status) && filterStatus.Contains(item.Employment_Status)
                        ? item.Assigned_Department
                        : item.Department,

                    Department_Name = !string.IsNullOrWhiteSpace(item.Employment_Status) && filterStatus.Contains(item.Employment_Status)
                        ? Department.FirstOrDefault(x => x.Factory == item.Assigned_Factory
                                                    && x.Division == item.Assigned_Division
                                                    && x.Department_Code == item.Assigned_Department)?.Department_Name
                        : Department.FirstOrDefault(x => x.Factory == item.Factory
                                                    && x.Division == item.Division
                                                    && x.Department_Code == item.Department)?.Department_Name,

                    Employee_ID = item.Employee_ID,
                    Local_Full_Name = item.Local_Full_Name,
                    Position_Title = PositionTitle.FirstOrDefault(x => x.Key == item.Position_Title).Value,
                    Work_Type = WorkType.FirstOrDefault(x => x.Key == item.Work_Type).Value,
                    Hazardous_Job = "N",
                    Onboard_Date = item.Onboard_Date.ToString("yyyy/MM/dd"),
                    Annual_Leave_Seniority_Start_Date = item.Annual_Leave_Seniority_Start_Date.ToString("yyyy/MM/dd"),
                    Resign_Date = item.Resign_Date.HasValue ? item.Resign_Date.Value.ToString("yyyy/MM/dd") : "",
                };

                // Công việc độc hại
                if (HAWTD.Any(x => x.Work_Type == item.Work_Type))
                    data.Hazardous_Job = "Y";
                if (item.Permission_Group == "V61")
                {
                    if (item.Work_Type == "B05")
                        data.Hazardous_Job = "Y";
                    var Normal_Jobs = new List<string>() { "A83", "B03", "B06" };
                    if (Normal_Jobs.Contains(item.Work_Type))
                        data.Hazardous_Job = "N";
                }

                var Year_Quota_Annual = HAAL.FirstOrDefault(x => x.Employee_ID == item.Employee_ID)?.Days ?? 0;
                data.Seniority_Day = Calculation_Seniority_Day(item, End_Date);
                data.Annual_Leave_Days = Year_Quota_Annual;

                var HALM_Employee = HALM.Where(x => x.Employee_ID == item.Employee_ID).ToList();
                var Salary_Suspend = Query_Leave_Maintain_Sum_Days(HALM_Employee, First_Date, End_Date, new() { "J0" }); // Tạm ngừng lương 
                var Personal = Query_Leave_Maintain_Sum_Days(HALM_Employee, First_Date, End_Date, new() { "A0" });
                var Sick = Query_Leave_Maintain_Sum_Days(HALM_Employee, First_Date, End_Date, new() { "B0" });
                var Absent = Calculation_Absent(item, HALM_Employee, First_Date, End_Date, new() { "C0" });
                var Work_Injury = Query_Leave_Maintain_Sum_Days(HALM_Employee, First_Date, End_Date, new() { "H0" });

                var Sub_Personal = Math.Max(Personal - 26 * 1, 0);
                var Sub_Sick = Math.Max(Sick - 26 * 2, 0);
                var Sub_Work_Injury = Math.Max(Work_Injury - 26 * 6, 0);
                var Sub_Month = (Sub_Personal + Sub_Sick + Sub_Work_Injury + Absent + Salary_Suspend) / 26;

                var query_Available_Leave_Days_Param = new Query_Available_Leave_Days_Param
                {
                    HEP = item,
                    HACs = HAC,
                    HACRs = HACR,
                    HALMs = HALM,
                    Total_Annual = Year_Quota_Annual + data.Seniority_Day,
                    Sub_Month = Sub_Month,
                    Start_Date = First_Date,
                    End_Date = End_Date
                };


                decimal Available_Days = Query_Available_Leave_Days(query_Available_Leave_Days_Param);

                int Available_Days_Int = Available_Days.ToRoundInt();

                decimal Available_Days_Dec = Available_Days - Available_Days_Int;
                // Tính phép năm
                if (Available_Days_Int >= 12)
                {
                    data.Allocation_Annual_Company = 6;
                    data.Allocation_Annual_Employee = Available_Days_Int - data.Allocation_Annual_Company;
                }
                else
                {
                    data.Allocation_Annual_Employee = Available_Days_Int / 2;
                    if (Available_Days_Int % 2 != 0) data.Allocation_Annual_Employee = Math.Round(data.Allocation_Annual_Employee + 0.5m);
                    data.Allocation_Annual_Company = Available_Days_Int - (int)data.Allocation_Annual_Employee;
                }

                data.Available_Days = Available_Days;
                data.Allocation_Annual_Employee += Available_Days_Dec;

                // Phép năm cá nhân và phép năm công ty
                data.Used_Annual_Leave_Employee = Query_Leave_Maintain_Sum_Days(HALM_Employee, First_Date, End_Date, new() { "I0" });
                data.Used_Annual_Leave_Company = Query_Leave_Maintain_Sum_Days(HALM_Employee, First_Date, End_Date, new() { "I1" });
                // Số ngày nghỉ hằng năm đã sử dụng
                data.Used_Annual = data.Used_Annual_Leave_Employee + data.Used_Annual_Leave_Company;
                // Số ngày nghỉ hằng năm chưa được sử dụng
                data.Unused_Annual = Available_Days_Int - data.Used_Annual;

                listYearMonth.ForEach(month =>
                {
                    var sum_Day = Query_Leave_Maintain_Sum_Days(HALM_Employee, month, ToLastDateOfMonth(month), new() { "I0", "I1" });
                    data.YearMonth.Add(sum_Day);
                });

                result.Add(data);
            });

            return result;
        }
        #endregion

        private static readonly Func<IEnumerable<HRMS_Att_Leave_Maintain>, DateTime, DateTime, List<string>, decimal> Query_Leave_Maintain_Sum_Days = (leave_Maintain, start_Date, end_Date, leave_Code) =>
        {
            return leave_Maintain
                .Where(x => leave_Code.Contains(x.Leave_code)
                         && x.Leave_Date >= start_Date
                         && x.Leave_Date <= end_Date).Sum(x => x.Days);
        };

        private readonly Func<Query_Available_Leave_Days_Param, decimal> Query_Available_Leave_Days = (param) =>
        {
            decimal available_days_after_deduction = 0m;
            decimal annual_Number_Months = 0;
            decimal vn_Annual_Days = 12;

            Percent_Param percent_Param = new()
            {
                HACs = param.HACs, // danh sách ngày
                HACRs = param.HACRs, // Danh sách chấm công 
                HALMs = param.HALMs, // Danh sách nghỉ
                Employee_ID = param.HEP.Employee_ID, // Mã nhân viên
                Start_Date = param.Start_Date, // Ngày bắt đầu [param]
                End_Date = param.End_Date// Ngày kết thúc [param]
            };

            // Nhân viên chưa nghỉ việc
            if (!param.HEP.Resign_Date.HasValue) // Chưa nghỉ 
            {
                if (param.HEP.Onboard_Date.Year == percent_Param.Start_Date.Year) // Tìm kiếm trong năm hiện tại
                {
                    decimal onboard_date = param.HEP.Onboard_Date.Month;

                    // Tính số phần trăm thời
                    var percent = Calculation_Percent(percent_Param);

                    // Nếu lớn hơn 50% tăng 1 tháng
                    if (percent >= 50)
                    {
                        onboard_date += 1m;
                        annual_Number_Months = vn_Annual_Days - onboard_date - param.Sub_Month;
                    }
                    else  // percent < 50 
                    {
                        onboard_date -= 1m;
                        annual_Number_Months = vn_Annual_Days - onboard_date - param.Sub_Month;
                    }
                }
                else // --在職員工 : Nhân viên hiện tại
                    annual_Number_Months = vn_Annual_Days - param.Sub_Month;

            }
            else // : Đã nghỉ
            {
                if (param.HEP.Onboard_Date.Year == percent_Param.Start_Date.Year) //New employees
                {
                    decimal onboard_date = param.HEP.Onboard_Date.Month;
                    decimal resign_Month_int = param.HEP.Resign_Date.Value.Month; // Tháng đăng ký nghỉ việc

                    var months_of_annual_leave = vn_Annual_Days - param.HEP.Onboard_Date.Month - param.Sub_Month;
                    var percent = Calculation_Percent(percent_Param);

                    if (percent < 50)
                        resign_Month_int -= 1m;

                    annual_Number_Months = resign_Month_int - onboard_date + 1 - param.Sub_Month;
                }
                else  // --不是當年度新進員工, 且離職: --Không phải là nhân viên mới trong năm hiện tại và đã nghỉ việc
                {
                    var percent = Calculation_Percent(percent_Param);

                    if (percent >= 50)
                        annual_Number_Months = param.HEP.Resign_Date.Value.Month - param.Sub_Month;
                    else
                        annual_Number_Months = param.HEP.Resign_Date.Value.Month - 1 - param.Sub_Month;
                }
            }

            if (annual_Number_Months <= 0)
            {
                annual_Number_Months = 0;
                available_days_after_deduction = 0;
            }
            else 
                available_days_after_deduction = param.Total_Annual / 12 * annual_Number_Months;

            return available_days_after_deduction;
        };

        private static decimal Calculation_Percent(Percent_Param param)
        {
            var leave_Code = new List<string>() { "A0", "B0", "C0", "G0", "J4", "J3", "O0" };
            var calendar_Cnt = param.HACs
                .Where(x => x.Att_Date >= param.Start_Date
                        && x.Att_Date <= param.End_Date).Count();

            var salary_Days = (param.End_Date - param.Start_Date).Days + 1 - calendar_Cnt;

            decimal days_of_attendance = param.HACRs
                .Where(x => x.Employee_ID == param.Employee_ID
                         && x.Att_Date >= param.Start_Date
                         && x.Att_Date <= param.End_Date).Count();

            decimal days_of_leave = param.HALMs
                .Where(x => x.Employee_ID == param.Employee_ID
                         && x.Leave_Date >= param.Start_Date
                         && x.Leave_Date <= param.End_Date
                         && leave_Code.Contains(x.Leave_code)).Sum(x => x.Days);

            // Số ngày làm việc thực tế = sô ngày làm việc - số ngày nghỉ
            var actual_Days = days_of_attendance - days_of_leave;
            var result = actual_Days * 100 / salary_Days;
            return result;
        }

        /// <summary>
        /// Tính số ngày thâm niên
        /// </summary>
        /// <returns></returns>
        private readonly Func<HRMS_Emp_Personal, DateTime, decimal> Calculation_Seniority_Day = (emp_Personal, end_Month) =>
        {
            // TH nhân viên chưa nghỉ việc
            if (!emp_Personal.Resign_Date.HasValue)
            {
                if (emp_Personal.Annual_Leave_Seniority_Start_Date > emp_Personal.Onboard_Date)
                    return (end_Month.Year - emp_Personal.Group_Date.Year) / 5; // năm hiện tại  - năm làm việc của nhóm
                else
                    return (end_Month.Year - emp_Personal.Annual_Leave_Seniority_Start_Date.Year) / 5;
            }
            else // Đã nghỉ việc
            {
                if (emp_Personal.Annual_Leave_Seniority_Start_Date > emp_Personal.Onboard_Date)
                    return (emp_Personal.Resign_Date.Value.Date - emp_Personal.Annual_Leave_Seniority_Start_Date.Date).Days / 365 / 5;
                else
                    return (emp_Personal.Resign_Date.Value.Date - emp_Personal.Onboard_Date.Date).Days / 365 / 5;
            }
        };

        // Tính số ngày vắng
        private readonly Func<HRMS_Emp_Personal, List<HRMS_Att_Leave_Maintain>, DateTime, DateTime, List<string>, decimal> Calculation_Absent = (emp_Personal, leave_Maintain, start, end, leave_Code) =>
        {
            var absent = Query_Leave_Maintain_Sum_Days(leave_Maintain, start, end, leave_Code);
            if (emp_Personal.Resign_Date.HasValue)
            {
                if (emp_Personal.Resign_Date.Value.Year == emp_Personal.Onboard_Date.Year)
                {
                    var onboard_First_Date = ToFirstDateOfMonth(emp_Personal.Onboard_Date);
                    var onboard_Last_Date = ToLastDateOfMonth(emp_Personal.Onboard_Date);
                    var resign_First_Date = ToFirstDateOfMonth(emp_Personal.Resign_Date.Value);
                    var resign_Last_Date = ToLastDateOfMonth(emp_Personal.Resign_Date.Value);

                    var absent_In = Query_Leave_Maintain_Sum_Days(leave_Maintain, onboard_First_Date, onboard_Last_Date, leave_Code);
                    var absent_Out = Query_Leave_Maintain_Sum_Days(leave_Maintain, resign_First_Date, resign_Last_Date, leave_Code);
                    absent -= absent_In + absent_Out;
                }
                else
                {
                    var resign_First_Date = ToFirstDateOfMonth(emp_Personal.Resign_Date.Value);
                    var resign_Last_Date = ToFirstDateOfMonth(emp_Personal.Resign_Date.Value);

                    absent -= Query_Leave_Maintain_Sum_Days(leave_Maintain, resign_First_Date, resign_Last_Date, leave_Code);
                }

            }
            return absent;
        };

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string language, List<string> roleList)
        {
            var factoriesByAccount = await Queryt_Factory_AddList(roleList);
            var factories = await GetDataBasicCode(BasicCodeTypeConstant.Factory, language);

            return factories.IntersectBy(factoriesByAccount, x => x.Key).ToList();
        }

        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
        {
            var HOD = await Query_Department_List(factory);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Factory == factory
                           && x.Language_Code.ToLower() == language.ToLower());

            var deparment = HOD.GroupJoin(HODL,
                        x => new {x.Division, x.Department_Code},
                        y => new {y.Division, y.Department_Code},
                        (x, y) => new { dept = x, hodl = y })
                        .SelectMany(x => x.hodl.DefaultIfEmpty(),
                        (x, y) => new { x.dept, hodl = y })
                        .Select(x => new KeyValuePair<string, string>(x.dept.Department_Code, $"{(x.hodl != null ? x.hodl.Name : x.dept.Department_Name)}"))
                        .ToList();
            return deparment;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language)
        {
            return await Query_BasicCode_PermissionGroup(factory, language);
        }

        private static List<DateTime> GetListYearMonth(DateTime Start_Month, DateTime End_Month)
        {
            var result = new List<DateTime>();
            var tmp = Start_Month;
            while (tmp <= End_Month)
            {
                result.Add(tmp);
                tmp = tmp.AddMonths(1);
            }
            return result;
        }

        private static DateTime ToFirstDateOfYear(DateTime dt) => new(dt.Year, 1, 1);
        private static DateTime ToLastDateOfYear(DateTime dt) => new(dt.Year, 12, 31);
        private static DateTime ToFirstDateOfMonth(DateTime dt) => new(dt.Year, dt.Month, 1);
        private static DateTime ToLastDateOfMonth(DateTime dt) => ToFirstDateOfMonth(dt.AddMonths(1)).AddDays(-1);

    }
}