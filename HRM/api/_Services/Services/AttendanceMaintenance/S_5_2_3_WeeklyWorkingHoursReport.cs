using API.Data;
using API._Services.Interfaces;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_2_3_WeeklyWorkingHoursReport : BaseServices, I_5_2_3_WeeklyWorkingHoursReport
    {
        private readonly I_Common _common;
        public S_5_2_3_WeeklyWorkingHoursReport(DBContext dbContext, I_Common common) : base(dbContext)
        {
            _common = common;
        }

        public async Task<OperationResult> DownloadFileExcel(WeeklyWorkingHoursReportParam param, string userName)
        {
            var data = await GetData(param);
            if (!data.Any())
                return new OperationResult(false, "System.Message.NoData");
            var dataLevel = await GetListLevel(param.language);
            var dataDepartment = await GetListDepartment(param.language, param.Factory);
            List<Cell> dataCells = new()
            {
                new Cell("B" + 2, param.Factory),
                new Cell("F" + 2, param.Date_Start.ToString("yyyy/MM/dd") + " ~ " + param.Date_End.ToString("yyyy/MM/dd")),
                new Cell("I" + 2, dataDepartment.FirstOrDefault(x=> x.Key == param.Department).Value),
                new Cell("L" + 2, dataLevel.FirstOrDefault(x=> x.Key == param.Level).Value),
                new Cell("N" + 2, param.Kind == "Personal" ? param.language == "en" ? "Personal" : "個人" : param.language == "en" ? "Department" : "部門"),
                new Cell("B" + 4, userName),
                new Cell("D" + 4, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),

            };
            if (param.Kind == "Personal")
                data.OrderBy(x => x.Department).ThenBy(x => x.Employee_ID);
            else
                data.OrderBy(x => x.Department);

            var dataTables = new List<Table>() { new("result", data) };
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataTables, dataCells, 
                $"Resources\\Template\\AttendanceMaintenance\\5_2_3_WeeklyWorkingHoursReport\\DownloadBy{param.Kind}.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, new { excelResult.Result, data.Count });
        }

        public async Task<int> GetCountRecords(WeeklyWorkingHoursReportParam param)
        {
            var result = await GetData(param);
            return result.Count;
        }

        public async Task<List<WeeklyWorkingHoursReportExcel>> GetData(WeeklyWorkingHoursReportParam param)
        {
            var preHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory &&
            (!x.Resign_Date.HasValue || x.Resign_Date.HasValue && x.Resign_Date.Value.Date > param.Date_End.Date));
            TableData tableData = new()
            {
                dataHEP = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(preHEP, true).ToListAsync(),
                dataHAWS = _repositoryAccessor.HRMS_Att_Work_Shift.FindAll(x => x.Factory == param.Factory, true).ToList(),
                dataHACR = _repositoryAccessor.HRMS_Att_Change_Record.FindAll(x => x.Factory == param.Factory && x.Att_Date >= param.Date_Start && x.Att_Date <= param.Date_End, true).ToList(),
                dataHALM = _repositoryAccessor.HRMS_Att_Leave_Maintain.FindAll(x => x.Factory == param.Factory && x.Leave_Date >= param.Date_Start && x.Leave_Date <= param.Date_End && x.Leave_code != "D0", true).ToList(),
                dataHAOM = _repositoryAccessor.HRMS_Att_Overtime_Maintain.FindAll(x => x.Factory == param.Factory &&
                                        x.Overtime_Date >= param.Date_Start &&
                                        x.Overtime_Date <= param.Date_End, true)
                                    .ToList(),
            };
            var departmentByLevel = GetDepartmentByLevel(param.Factory, param.Department, param.Level);

            // Lấy thông tin phòng ban theo ngôn ngữ
            var deptLang = _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == param.Factory, true)
                                .GroupJoin(
                                    _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => 
                                        x.Factory == param.Factory && 
                                        x.Language_Code.ToLower() == param.language.ToLower()),
                                    x => new { x.Division, x.Factory, x.Department_Code },
                                    y => new { y.Division, y.Factory, y.Department_Code },
                                    (x, y) => new { dept = x, hodl = y })
                                .SelectMany(x => x.hodl.DefaultIfEmpty(),
                                    (x, y) => new { x.dept, hodl = y })
                                .Select(x => new
                                {
                                    Code = x.dept.Department_Code,
                                    Name = x.hodl != null ? x.hodl.Name : x.dept.Department_Name
                                }).Distinct().ToHashSet();

            List<WeeklyWorkingHoursReportExcel> result = new();
            foreach (var dept in departmentByLevel)
            {
                var deptList = GetDepartmentHierarchy(param.Factory, dept.Department_Code);
                var dataPersonal = HandleData(param, tableData, deptList);
                if (dataPersonal is not null)
                {
                    if (param.Kind == "Personal")
                    {
                        List<WeeklyWorkingHoursReportExcel> items = dataPersonal.listReport;
                        if (items.Count > 0)
                        {
                            foreach (var item in items)
                            {
                                item.Department = deptLang.FirstOrDefault(x => x.Code == dept.Department_Code)?.Code;
                                item.Department_Name = deptLang.FirstOrDefault(x => x.Code == dept.Department_Code)?.Name;
                            }
                            result.AddRange(items);
                        }
                    }
                    else
                    {
                        WeeklyWorkingHoursReportExcel item = dataPersonal.Report;
                        if (item is not null)
                        {
                            item.Department = deptLang.FirstOrDefault(x => x.Code == dept.Department_Code)?.Code;
                            item.Department_Name = deptLang.FirstOrDefault(x => x.Code == dept.Department_Code)?.Name;
                            result.Add(item);
                        }
                    }
                }
            }
            return result;
        }
        private List<HRMS_Org_Department> GetDepartmentByLevel(string factory, string department, string level)
        {
            var preHOD = PredicateBuilder.New<HRMS_Org_Department>(x => x.Factory == factory && x.IsActive);
            if (!string.IsNullOrWhiteSpace(department))
                preHOD.And(x => x.Department_Code == department);
            var departments = _repositoryAccessor.HRMS_Org_Department.FindAll(preHOD).ToList();
            if (departments.Count == 0 || !int.TryParse(level, out int levelInt))
                return new List<HRMS_Org_Department>();
            var result = departments.Where(x =>
                int.TryParse(x.Org_Level, out int orgLevelInt) &&
                (orgLevelInt == levelInt ||
                (orgLevelInt >= levelInt && string.IsNullOrWhiteSpace(x.Upper_Department)) ||
                (orgLevelInt > levelInt && departments.Any(y => int.TryParse(y.Org_Level, out int yOrgLevelInt) && yOrgLevelInt < levelInt && y.Department_Code == x.Upper_Department)))
            ).ToList();
            return result;
        }
        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string language, string factory)
        => await _common.GetListDepartment(language, factory);
        private List<HRMS_Org_Department> GetDepartmentHierarchy(string factory, string deparment)
        {
            List<HRMS_Org_Department> result = new();
            var departments = _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == factory && x.IsActive).ToList();
            var rootDepartment = departments.FirstOrDefault(x => x.Department_Code == deparment);
            if (rootDepartment == null) return result;
            result.Add(rootDepartment);
            var addedDepartments = new HashSet<string> { deparment };
            var queue = new Queue<string>();
            queue.Enqueue(deparment);
            while (queue.Count > 0)
            {
                var currentDepartmentCode = queue.Dequeue();
                var childDepartments = departments.Where(x => x.Upper_Department == currentDepartmentCode && !addedDepartments.Contains(x.Department_Code)).ToList();
                foreach (var child in childDepartments)
                {
                    result.Add(child);
                    addedDepartments.Add(child.Department_Code);
                    queue.Enqueue(child.Department_Code);
                }
            }
            return result;
        }
        #region xử lí tính giờ làm việc của nhân viên trong một phạm vi ngày
        private static ALLReportExcel HandleData(WeeklyWorkingHoursReportParam param, TableData tableData, List<HRMS_Org_Department> dept)
        {
            var result = new ALLReportExcel();
            // Danh sách nhân viên theo phòng ban
            var listEmpByDepartment = tableData.dataHEP.FindAll(x =>
                dept.Any(d => d.Department_Code == x.Department) &&
                (!x.Resign_Date.HasValue || x.Resign_Date.HasValue && x.Resign_Date.Value.Date > param.Date_End.Date));

            if (listEmpByDepartment.Count == 0)
                return null;

            var data = listEmpByDepartment
                .Select(x => new
                {
                    x.Employee_ID,
                    x.Local_Full_Name,
                    x.Swipe_Card_Option,
                    Work_Hours = GetWorkHours(x, tableData.dataHACR, tableData.dataHAWS),
                    Leave_hours = GetLeaveHours(x, tableData.dataHALM, tableData.dataHAWS),
                    Overtime_hours = GetOvertimeHours(x, tableData.dataHAOM)
                })
                .Select(x => new EmployeeHoursDetail
                {
                    Employee_ID = x.Employee_ID,
                    Local_Full_Name = x.Local_Full_Name,
                    Total = x.Swipe_Card_Option ? x.Work_Hours - x.Leave_hours + x.Overtime_hours : 0
                });

            if (param.Kind == "Personal")
                result.listReport = data.Select(x => new WeeklyWorkingHoursReportExcel
                {
                    Employee_ID = x.Employee_ID,
                    Local_Full_Name = x.Local_Full_Name,
                    Hours_0_48 = x.Total <= 48 ? 1 : 0,
                    Hours_48_60 = 48 < x.Total && x.Total <= 60 ? 1 : 0,
                    Hours_60_64 = 60 < x.Total && x.Total <= 64 ? 1 : 0,
                    Hours_64_70 = 64 < x.Total && x.Total <= 70 ? 1 : 0,
                    Hours_70 = 70 < x.Total ? 1 : 0
                }).ToList();
            else
                result.Report = new WeeklyWorkingHoursReportExcel
                {
                    Department_Headcount = listEmpByDepartment.Count,
                    Hours_0_48 = data.Count(x => x.Total <= 48),
                    Hours_48_60 = data.Count(x => 48 < x.Total && x.Total <= 60),
                    Hours_60_64 = data.Count(x => 60 < x.Total && x.Total <= 64),
                    Hours_64_70 = data.Count(x => 64 < x.Total && x.Total <= 70),
                    Hours_70 = data.Count(x => 70 < x.Total),
                };
            return result;
        }

        private static decimal GetWorkHours(HRMS_Emp_Personal HEP, List<HRMS_Att_Change_Record> HACRs, List<HRMS_Att_Work_Shift> HAWSs)
        => HACRs.Where(x => x.Employee_ID == HEP.Employee_ID)
            .GroupJoin(HAWSs,
                x => new { x.Work_Shift_Type, Week = ((int)x.Att_Date.DayOfWeek).ToString() },
                y => new { y.Work_Shift_Type, y.Week },
                (x, y) => new { HACR = x, HAWS = y })
            .SelectMany(x => x.HAWS.DefaultIfEmpty(), (x, y) => y)
            .Sum(y => y?.Work_Hours ?? 0);

        private static decimal GetLeaveHours(HRMS_Emp_Personal HEP, List<HRMS_Att_Leave_Maintain> HALMs, List<HRMS_Att_Work_Shift> HAWSs)
        => HALMs.Where(x => x.Employee_ID == HEP.Employee_ID)
            .GroupJoin(HAWSs,
                x => new { x.Work_Shift_Type, Week = ((int)x.Leave_Date.DayOfWeek).ToString() },
                y => new { y.Work_Shift_Type, y.Week },
                (x, y) => new { HALM = x, HAWS = y })
            .SelectMany(x => x.HAWS.DefaultIfEmpty(), (x, y) => new { x.HALM, HAWS = y })
            .Sum(y => y.HALM != null && y.HAWS != null ? y.HALM.Days * y.HAWS.Work_Hours : 0);

        private static decimal GetOvertimeHours(HRMS_Emp_Personal HEP, List<HRMS_Att_Overtime_Maintain> HAOMs)
        => HAOMs.Where(x => x.Employee_ID == HEP.Employee_ID)
            .Sum(y => y.Overtime_Hours + y.Night_Overtime_Hours);
        #endregion

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(List<string> roleList, string language)
        => await Query_Factory_AddList(roleList, language);

        public async Task<List<KeyValuePair<string, string>>> GetListLevel(string language)
        => await GetDataBasicCode(BasicCodeTypeConstant.Level, language);
    }
}