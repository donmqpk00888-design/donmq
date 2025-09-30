using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Aspose.Cells;
using System.Collections;
using System.Drawing;
using AgileObjects.AgileMapper.Extensions;
using AgileObjects.AgileMapper;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_1_22_MonthlyAttendanceDataMaintenanceActiveEmployees : BaseServices, I_5_1_22_MonthlyAttendanceDataMaintenanceActiveEmployees
    {
        public S_5_1_22_MonthlyAttendanceDataMaintenanceActiveEmployees(DBContext dbContext) : base(dbContext)
        {
        }

        public async Task<PaginationUtility<MaintenanceActiveEmployeesMain>> GetPagination(PaginationParam pagination, MaintenanceActiveEmployeesParam param)
        {
            var (predMonthly, predProbation, predEmp) = SetPredicate(param);

            var permissionGroup = await GetListPermissionGroup(param.Language);
            var salaryType = await GetListSalaryType(param.Language);
            var department = await GetDepartmentMain(param.Language);
            var HAM = _repositoryAccessor.HRMS_Att_Monthly
                .FindAll(predMonthly, true)
                .Project().To<MaintenanceActiveEmployeesDto>(cfg => cfg.Map(false).To(dto => dto.isProbation));
            var HAPM = _repositoryAccessor.HRMS_Att_Probation_Monthly
                        .FindAll(predProbation, true)
                        .Project().To<MaintenanceActiveEmployeesDto>(
                            cfg => cfg.Map(true).To(dto => dto.isProbation)
                        );
            var HAPM_HAM = HAPM
                        .Join(HAM,
                        hapm => new { hapm.Factory, hapm.Att_Month, hapm.Employee_ID },
                        ham => new { ham.Factory, ham.Att_Month, ham.Employee_ID },
                        (hapm, ham) => hapm);

            var data = await HAM.Union(HAPM_HAM)
                                .Join(_repositoryAccessor.HRMS_Emp_Personal.FindAll(predEmp, true),
                                        x => new { x.USER_GUID },
                                        y => new { y.USER_GUID },
                                        (x, y) => new { HAM = x, HEP = y })
                                .ToListAsync();

            List<MaintenanceActiveEmployeesMain> result = data.Select(x => new MaintenanceActiveEmployeesMain
            {
                Division = x.HAM.Division,
                Factory = x.HAM.Factory,
                Att_Month = x.HAM.Att_Month.ToString("yyyy/MM"),
                Department = string.IsNullOrWhiteSpace(x.HEP.Employment_Status)
                       ? department.FirstOrDefault(
                           d => d.Division == x.HEP.Division &&
                           d.Factory == x.HEP.Factory &&
                           d.Department_Code == x.HEP.Department)?.Department_Name
                       : x.HEP.Employment_Status == "A" || x.HEP.Employment_Status == "S"
                       ? department.FirstOrDefault(
                           d => d.Division == x.HEP.Assigned_Division &&
                           d.Factory == x.HEP.Assigned_Factory &&
                           d.Department_Code == x.HEP.Assigned_Department)?.Department_Name
                       : "",
                Employee_ID = x.HAM.Employee_ID,
                Local_Full_Name = x.HEP.Local_Full_Name,
                Pass = x.HAM.Pass == true ? "Y" : "N",
                Resign_Status = x.HAM.Resign_Status,
                Permission_Group = permissionGroup.FirstOrDefault(y => y.Key == x.HAM.Permission_Group).Value ?? x.HAM.Permission_Group,
                Salary_Type = salaryType.FirstOrDefault(y => y.Key == x.HAM.Salary_Type).Value ?? x.HAM.Salary_Type,
                Salary_Days = x.HAM.Salary_Days.ToString(),
                Actual_Days = x.HAM.Actual_Days.ToString(),
                Probation = x.HAM.Probation,
                isProbation = x.HAM.isProbation,
                Update_By = x.HAM.Update_By,
                Update_Time = x.HAM.Update_Time.ToString("yyyy/MM/dd HH:mm:ss"),
            }).ToList();
            return PaginationUtility<MaintenanceActiveEmployeesMain>.Create(result, pagination.PageNumber, pagination.PageSize);
        }

        #region GetEmpInfo
        public async Task<OperationResult> GetEmpInfo(ActiveEmployeeParam param)
        {
            var comparison = await _repositoryAccessor.HRMS_Basic_Factory_Comparison
                .FirstOrDefaultAsync(x => x.Factory == param.Factory && x.Kind == "1");

            if (comparison is null)
                return null;

            var emp = await _repositoryAccessor.HRMS_Emp_Personal
                .FirstOrDefaultAsync(
                    x => (x.Factory == param.Factory || x.Assigned_Factory == param.Factory) &&
                    (x.Employee_ID == param.Employee_ID || x.Assigned_Employee_ID == param.Employee_ID) &&
                    x.Division == comparison.Division, true);

            if (emp is null)
                return new OperationResult(false, "Employee not existed.");
            if (param.isAdd)
            {
                var checkResignDate = await _repositoryAccessor.HRMS_Emp_Personal
                .FirstOrDefaultAsync(x => x.Factory == param.Factory
                    && x.Employee_ID == param.Employee_ID && x.Resign_Date != null);
                if (checkResignDate != null)
                    return new OperationResult(false, $"{param.Employee_ID} have resigned and cannot be entered.");
            }

            var departments = await GetDepartmentMain(param.Language);
            DepartmentMain department = new();

            if (param.isMonthly)
            {
                var HAM = _repositoryAccessor.HRMS_Att_Monthly.FirstOrDefault(x => x.Factory == param.Factory &&
                    x.Employee_ID == param.Employee_ID &&
                    x.Att_Month == DateTime.Parse(param.Att_Month));
                department = departments.FirstOrDefault(x => x.Division == HAM.Division &&
                    x.Factory == HAM.Factory &&
                    x.Department_Code == HAM.Department);
            }
            else
            {
                department = string.IsNullOrWhiteSpace(emp.Employment_Status)
                ? departments.FirstOrDefault(
                    d => d.Division == emp.Division &&
                    d.Factory == emp.Factory &&
                    d.Department_Code == emp.Department)
                : emp.Employment_Status == "A" || emp.Employment_Status == "S"
                ? departments.FirstOrDefault(
                    d => d.Division == emp.Assigned_Division &&
                    d.Factory == emp.Assigned_Factory &&
                    d.Department_Code == emp.Assigned_Department)
                : null;
            }

            var result = new EmpInfo522
            {
                USER_GUID = emp.USER_GUID,
                Department = department?.Department_Name,
                Department_Code = department?.Department_Code,
                Local_Full_Name = emp.Local_Full_Name,
                Division = string.IsNullOrWhiteSpace(emp.Employment_Status)
                    ? emp.Division
                    : emp.Employment_Status == "A" || emp.Employment_Status == "S"
                    ? emp.Assigned_Division : "",
                Permission_Group = emp.Permission_Group
            };
            return new OperationResult(true, result);
        }
        #endregion

        #region Add
        public async Task<OperationResult> Add(MaintenanceActiveEmployeesDetail data, string userName)
        {
            if (await _repositoryAccessor.HRMS_Att_Monthly
                .AnyAsync(x => x.Factory == data.Factory
                            && x.Att_Month == DateTime.Parse(data.Att_Month_Str)
                            && x.Pass == true))
            {
                return new OperationResult(false, "Can’t save data");
            }

            if (await _repositoryAccessor.HRMS_Att_Monthly
                .AnyAsync(x => x.Factory == data.Factory
                            && x.Att_Month == DateTime.Parse(data.Att_Month_Str)
                            && x.Employee_ID == data.Employee_ID))
            {
                string message = $"Year-Month of Attendance: {data.Att_Month_Str}, Employee ID: {data.Employee_ID} exsited!";
                return new OperationResult(false, message);
            }

            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                DateTime now = DateTime.Now;
                HRMS_Att_Monthly monthly = new()
                {
                    Factory = data.Factory,
                    Employee_ID = data.Employee_ID,
                    USER_GUID = data.USER_GUID,
                    Division = data.Division,
                    Department = data.Department_Code,
                    Att_Month = Convert.ToDateTime(data.Att_Month).Date,
                    Actual_Days = data.Actual_Days,
                    Delay_Early = data.Delay_Early,
                    Food_Expenses = data.Food_Expenses,
                    Night_Eat_Times = data.Night_Eat_Times,
                    No_Swip_Card = data.No_Swip_Card,
                    DayShift_Food = data.DayShift_Food,
                    NightShift_Food = data.NightShift_Food,
                    Pass = data.Pass == "Y",
                    Permission_Group = data.Permission_Group,
                    Resign_Status = data.Resign_Status,
                    Salary_Days = data.Salary_Days,
                    Salary_Type = data.Salary_Type,
                    Probation = data.Probation,
                    Update_By = userName,
                    Update_Time = now
                };
                _repositoryAccessor.HRMS_Att_Monthly.Add(monthly);

                List<HRMS_Att_Monthly_Detail> details = new();
                List<HRMS_Att_Yearly> totals = new();

                if (data.Leaves is not null && data.Leaves.Any())
                {
                    data.Leaves.ForEach(item =>
                    {
                        details.Add(new()
                        {
                            Factory = monthly.Factory,
                            Employee_ID = monthly.Employee_ID,
                            USER_GUID = monthly.USER_GUID,
                            Division = monthly.Division,
                            Att_Month = monthly.Att_Month,
                            Leave_Type = "1",
                            Leave_Code = item.Code,
                            Days = decimal.Parse(item.Days),
                            Update_By = userName,
                            Update_Time = now
                        });

                    });

                    totals.AddRange(
                        await Upd_HRMS_Att_Yearly(
                            new Upd_HRMS_Att_Yearly_Param()
                            {
                                Factory = monthly.Factory,
                                Employee_ID = monthly.Employee_ID,
                                USER_GUID = monthly.USER_GUID,
                                Att_Year = new DateTime(monthly.Att_Month.Year, 1, 1),
                                Leave_Type = "1",
                                Account = userName,
                                Details = data.Leaves,
                            }
                    ));
                }

                if (data.Allowances is not null && data.Allowances.Any())
                {
                    data.Allowances.ForEach(item =>
                    {
                        details.Add(new()
                        {
                            Factory = monthly.Factory,
                            Employee_ID = monthly.Employee_ID,
                            USER_GUID = monthly.USER_GUID,
                            Division = monthly.Division,
                            Att_Month = monthly.Att_Month,
                            Leave_Type = "2",
                            Leave_Code = item.Code,
                            Days = decimal.Parse(item.Days),
                            Update_By = userName,
                            Update_Time = now
                        });
                    });

                    totals.AddRange(
                        await Upd_HRMS_Att_Yearly(
                            new Upd_HRMS_Att_Yearly_Param()
                            {
                                Factory = monthly.Factory,
                                Employee_ID = monthly.Employee_ID,
                                USER_GUID = monthly.USER_GUID,
                                Att_Year = new DateTime(monthly.Att_Month.Year, 1, 1),
                                Leave_Type = "2",
                                Account = userName,
                                Details = data.Allowances,
                            }
                    ));
                }

                if (details.Any())
                    _repositoryAccessor.HRMS_Att_Monthly_Detail.AddMultiple(details);

                if (totals.Any())
                    _repositoryAccessor.HRMS_Att_Yearly.UpdateMultiple(totals);

                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();

                return new OperationResult(true);
            }
            catch (Exception)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false);
            };
        }
        #endregion

        #region Edit
        public async Task<OperationResult> Edit(MaintenanceActiveEmployeesDetail data, string userName)
        {
            var monthly = await _repositoryAccessor.HRMS_Att_Monthly
                .FirstOrDefaultAsync(x => x.Factory == data.Factory
                                       && x.Att_Month == DateTime.Parse(data.Att_Month)
                                       && x.Employee_ID == data.Employee_ID);

            if (monthly is null)
            {
                return new OperationResult(false, "Data Employee not existed!");
            }

            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                monthly.Salary_Days = data.Salary_Days;
                monthly.Actual_Days = data.Actual_Days;
                monthly.Resign_Status = data.Resign_Status;
                monthly.Delay_Early = data.Delay_Early;
                monthly.No_Swip_Card = data.No_Swip_Card;
                monthly.Food_Expenses = data.Food_Expenses;
                monthly.Night_Eat_Times = data.Night_Eat_Times;

                monthly.DayShift_Food = data.DayShift_Food;
                monthly.NightShift_Food = data.NightShift_Food;

                monthly.Update_By = userName;
                monthly.Update_Time = DateTime.Now;
                monthly.Department = data.Department_Code;

                List<HRMS_Att_Monthly_Detail> details_Update = new();
                List<HRMS_Att_Monthly_Detail> details_Add = new();
                List<HRMS_Att_Yearly> totals_Update = new();
                List<HRMS_Att_Yearly> totals_Add = new();

                var HAMDs = _repositoryAccessor.HRMS_Att_Monthly_Detail.FindAll(x => x.Factory == data.Factory
                                                                            && x.Att_Month.Date == DateTime.Parse(data.Att_Month).Date
                                                                            && x.Employee_ID == data.Employee_ID).ToList();
                var HAYs = _repositoryAccessor.HRMS_Att_Yearly.FindAll(x => x.Factory == data.Factory
                                                                            && x.Att_Year.Date == new DateTime(monthly.Att_Month.Year, 1, 1).Date
                                                                            && x.Employee_ID == data.Employee_ID).ToList();

                if (data.Leaves is not null && data.Leaves.Any())
                {
                    var res = FilterData(data.Leaves, HAMDs, HAYs, monthly, "1");
                    details_Add.AddRange(res.Item1);
                    details_Update.AddRange(res.Item2);
                    totals_Add.AddRange(res.Item3);
                    totals_Update.AddRange(res.Item4);
                }

                if (data.Allowances is not null && data.Allowances.Any())
                {
                    var res = FilterData(data.Allowances, HAMDs, HAYs, monthly, "2");
                    details_Add.AddRange(res.Item1);
                    details_Update.AddRange(res.Item2);
                    totals_Add.AddRange(res.Item3);
                    totals_Update.AddRange(res.Item4);
                }

                _repositoryAccessor.HRMS_Att_Monthly.Update(monthly);
                if (details_Add.Any())
                    _repositoryAccessor.HRMS_Att_Monthly_Detail.AddMultiple(details_Add);
                if (details_Update.Any())
                    _repositoryAccessor.HRMS_Att_Monthly_Detail.UpdateMultiple(details_Update);
                if (totals_Add.Any())
                    _repositoryAccessor.HRMS_Att_Yearly.AddMultiple(totals_Add);
                if (totals_Update.Any())
                    _repositoryAccessor.HRMS_Att_Yearly.UpdateMultiple(totals_Update);
                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, e);
            }
        }
        //return (Detail_Add, Detail_Update, Year_Add, Year_Update)
        private static (List<HRMS_Att_Monthly_Detail>, List<HRMS_Att_Monthly_Detail>, List<HRMS_Att_Yearly>, List<HRMS_Att_Yearly>) FilterData(List<LeaveAllowance> data, List<HRMS_Att_Monthly_Detail> HAMDs, List<HRMS_Att_Yearly> HAYs, HRMS_Att_Monthly main_Monthly, string Leave_Type)
        {
            List<HRMS_Att_Monthly_Detail> details_Update = new();
            List<HRMS_Att_Monthly_Detail> details_Add = new();
            List<HRMS_Att_Yearly> totals_Update = new();
            List<HRMS_Att_Yearly> totals_Add = new();
            var HAMD = HAMDs.Where(x => x.Leave_Type == Leave_Type);
            var HAY = HAYs.Where(x => x.Leave_Type == Leave_Type);
            data.ForEach(item =>
            {
                // Retry to add data if not existed or deleted
                var leave = HAMD.FirstOrDefault(x => x.Leave_Code == item.Code);
                decimal dayYear = 0;
                if (leave != null)
                {
                    dayYear = decimal.Parse(item.Days) - leave.Days;
                    leave.Days = decimal.Parse(item.Days);
                    leave.Update_By = main_Monthly.Update_By;
                    leave.Update_Time = main_Monthly.Update_Time;
                    details_Update.Add(leave);
                }
                else
                    details_Add.Add(new()
                    {
                        Factory = main_Monthly.Factory,
                        Employee_ID = main_Monthly.Employee_ID,
                        USER_GUID = main_Monthly.USER_GUID,
                        Division = main_Monthly.Division,
                        Att_Month = main_Monthly.Att_Month,
                        Leave_Type = Leave_Type,
                        Leave_Code = item.Code,
                        Days = decimal.Parse(item.Days),
                        Update_By = main_Monthly.Update_By,
                        Update_Time = main_Monthly.Update_Time
                    });
                var year = HAY.FirstOrDefault(x => x.Leave_Code == item.Code);
                if (year != null)
                {
                    year.Days += dayYear;
                    year.Update_By = main_Monthly.Update_By;
                    year.Update_Time = main_Monthly.Update_Time;
                    totals_Update.Add(year);
                }
                else
                    totals_Add.Add(new()
                    {
                        Factory = main_Monthly.Factory,
                        Employee_ID = main_Monthly.Employee_ID,
                        USER_GUID = main_Monthly.USER_GUID,
                        Division = main_Monthly.Division,
                        Att_Year = new DateTime(main_Monthly.Att_Month.Year, 1, 1),
                        Leave_Type = Leave_Type,
                        Leave_Code = item.Code,
                        Days = decimal.Parse(item.Total),
                        Update_By = main_Monthly.Update_By,
                        Update_Time = main_Monthly.Update_Time
                    });
            });
            return (details_Add, details_Update, totals_Add, totals_Update);
        }
        #endregion

        #region Detail
        public async Task<MaintenanceActiveEmployeesDetail> Detail(MaintenanceActiveEmployeesDetailParam param)
        {
            var data = !param.isProbation
                ? _repositoryAccessor.HRMS_Att_Monthly.FindAll(x => x.Factory == param.Factory
                                && x.Employee_ID == param.Employee_ID
                                && x.Att_Month == DateTime.Parse(param.Att_Month), true)
                                .Project().To<MaintenanceActiveEmployeesDto>(cfg => cfg.Map(false).To(dto => dto.isProbation))
                : _repositoryAccessor.HRMS_Att_Probation_Monthly.FindAll(x => x.Factory == param.Factory
                                && x.Employee_ID == param.Employee_ID
                                && x.Att_Month == DateTime.Parse(param.Att_Month), true)
                                .Project().To<MaintenanceActiveEmployeesDto>(cfg => cfg.Map(true).To(dto => dto.isProbation));

            var result = await data
                .Join(_repositoryAccessor.HRMS_Emp_Personal.FindAll(true),
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { HAM = x, HEP = y })
                .GroupJoin(_repositoryAccessor.HRMS_Att_Monthly_Period.FindAll(true),
                    x => new { x.HAM.Factory, x.HAM.Att_Month },
                    y => new { y.Factory, y.Att_Month },
                    (x, y) => new { x.HAM, x.HEP, HAMP = y })
                .SelectMany(x => x.HAMP.DefaultIfEmpty(),
                    (x, y) => new { x.HAM, x.HEP, HAMP = y })
                .Select(x => new MaintenanceActiveEmployeesDetail
                {
                    USER_GUID = x.HAM.USER_GUID,
                    Factory = x.HAM.Factory,
                    Division = x.HAM.Division,
                    Att_Month = x.HAM.Att_Month.ToString("yyyy/MM/dd"),
                    Deadline_Start = x.HAMP == null ? "" : x.HAMP.Deadline_Start.ToString("yyyy/MM/dd"),
                    Deadline_End = x.HAMP == null ? "" : x.HAMP.Deadline_End.ToString("yyyy/MM/dd"),
                    Pass = x.HAM.Pass == true ? "Y" : "N",
                    Employee_ID = x.HAM.Employee_ID,
                    Local_Full_Name = x.HEP.Local_Full_Name,
                    Salary_Days = x.HAM.Salary_Days,
                    Actual_Days = x.HAM.Actual_Days,
                    Permission_Group = x.HAM.Permission_Group,
                    Salary_Type = x.HAM.Salary_Type,
                    Resign_Status = x.HAM.Resign_Status,
                    Delay_Early = x.HAM.Delay_Early,
                    No_Swip_Card = x.HAM.No_Swip_Card,
                    Food_Expenses = x.HAM.Food_Expenses,

                    DayShift_Food = x.HAM.DayShift_Food,
                    NightShift_Food = x.HAM.NightShift_Food,

                    Night_Eat_Times = x.HAM.Night_Eat_Times,
                    Probation = x.HAM.Probation,
                    isProbation = x.HAM.isProbation
                })
                .FirstOrDefaultAsync();

            if (result is null)
                return null;
            (result.Leaves, result.Allowances) = await GetLeaveAllowance(param);
            return result;
        }
        #endregion

        #region GetLeaveAllowance
        public async Task<(List<LeaveAllowance>, List<LeaveAllowance>)> GetLeaveAllowance(MaintenanceActiveEmployeesDetailParam param)
        {
            var leaveCodes = await QueryCodeDetail(param, "1", BasicCodeTypeConstant.Leave);
            var data = !param.isProbation
                ? _repositoryAccessor.HRMS_Att_Monthly_Detail.FindAll(x =>
                                x.Factory == param.Factory &&
                                x.Employee_ID == param.Employee_ID &&
                                x.Att_Month == Convert.ToDateTime(param.Att_Month).Date, true)
                                .Project().To<MaintenanceActiveEmployeesDto_Detail>(cfg => cfg.Map(false).To(dto => dto.isProbation))
                : _repositoryAccessor.HRMS_Att_Probation_Monthly_Detail.FindAll(x =>
                                x.Factory == param.Factory &&
                                x.Employee_ID == param.Employee_ID &&
                                x.Att_Month == Convert.ToDateTime(param.Att_Month).Date, true)
                                .Project().To<MaintenanceActiveEmployeesDto_Detail>(cfg => cfg.Map(true).To(dto => dto.isProbation));
            var predicateYearly = PredicateBuilder
                .New<HRMS_Att_Yearly>(
                    x => x.Factory == param.Factory &&
                    x.Employee_ID == param.Employee_ID &&
                    x.Att_Year == new DateTime(Convert.ToDateTime(param.Att_Month).Year, 1, 1));
            List<LeaveAllowance> leaves = new();
            if (leaveCodes.Any())
            {
                var predicateYTemp = PredicateBuilder.New<HRMS_Att_Yearly>(predicateYearly);
                predicateYTemp.And(x => x.Leave_Type == "1");
                leaves = leaveCodes
                    .GroupJoin(data.Where(x => x.Leave_Type == "1"),
                        x => x.Key,
                        y => y.Leave_Code,
                        (x, y) => new { code = x, leave = y })
                    .SelectMany(x => x.leave.DefaultIfEmpty(),
                        (x, y) => new { x.code, leave = y })
                    .GroupJoin(_repositoryAccessor.HRMS_Att_Yearly.FindAll(predicateYTemp, true),
                        x => x.code.Key,
                        y => y.Leave_Code,
                        (x, y) => new { x.code, x.leave, year = y })
                    .SelectMany(x => x.year.DefaultIfEmpty(),
                        (x, y) => new { x.code, x.leave, year = y })
                    .Select(x => new LeaveAllowance()
                    {
                        Code = x.code.Key,
                        CodeName = x.code.Value,
                        Days = x.leave is null ? "0" : x.leave.Days.ToString(),
                        Total = x.year is null ? "0" : x.year.Days.ToString()
                    }).ToList();
            }
            var allowanceCodes = await QueryCodeDetail(param, "2", BasicCodeTypeConstant.Allowance);
            List<LeaveAllowance> allowances = new();
            if (allowanceCodes.Any())
            {
                var predicateYTemp = PredicateBuilder.New<HRMS_Att_Yearly>(predicateYearly);
                predicateYTemp.And(x => x.Leave_Type == "2");
                allowances = allowanceCodes
                    .GroupJoin(data.Where(x => x.Leave_Type == "2"),
                        x => x.Key,
                        y => y.Leave_Code,
                        (x, y) => new { code = x, leave = y })
                    .SelectMany(x => x.leave.DefaultIfEmpty(),
                        (x, y) => new { x.code, leave = y })
                    .GroupJoin(_repositoryAccessor.HRMS_Att_Yearly.FindAll(predicateYTemp, true),
                        x => x.code.Key,
                        y => y.Leave_Code,
                        (x, y) => new { x.code, x.leave, year = y })
                    .SelectMany(x => x.year.DefaultIfEmpty(),
                        (x, y) => new { x.code, x.leave, year = y })
                    .Select(x => new LeaveAllowance()
                    {
                        Code = x.code.Key,
                        CodeName = x.code.Value,
                        Days = x.leave is null ? "0" : x.leave.Days.ToString(),
                        Total = x.year is null ? "0" : x.year.Days.ToString()
                    }).ToList();
            }
            return (leaves, allowances);
        }

        private async Task<List<KeyValuePair<string, string>>> QueryCodeDetail(MaintenanceActiveEmployeesDetailParam param, string leave_Type, string type_Seq)
        {
            var monthlyLeaves = _repositoryAccessor.HRMS_Att_Use_Monthly_Leave
                .FindAll(x => x.Factory == param.Factory && x.Leave_Type == leave_Type && x.Effective_Month.Date <= Convert.ToDateTime(param.Att_Month).Date);

            if (!await monthlyLeaves.AnyAsync())
                return new List<KeyValuePair<string, string>>();

            var maxLeaveEffectiveMonth = await monthlyLeaves.MaxAsync(x => x.Effective_Month);

            var codes = await _repositoryAccessor.HRMS_Att_Use_Monthly_Leave
                .FindAll(x => x.Factory == param.Factory && x.Leave_Type == leave_Type && x.Effective_Month == maxLeaveEffectiveMonth)
                .OrderBy(x => x.Seq)
                .Select(x => x.Code).ToListAsync();

            var predicate = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == type_Seq && codes.Contains(x.Code));
            return await Query_HRMS_Basic_Code(predicate, param.Language);
        }
        #endregion 

        #region Download
        public async Task<OperationResult> Download(MaintenanceActiveEmployeesParam param, string userName)
        {
            MaintenanceActiveEmployeesDetailParam queryCodeParam = new()
            {
                Factory = param.Factory,
                Att_Month = param.Att_Month_End,
                Language = param.Language
            };
            var leaves = await QueryCodeDetail(queryCodeParam, "1", BasicCodeTypeConstant.Leave);
            var allowances = await QueryCodeDetail(queryCodeParam, "2", BasicCodeTypeConstant.Allowance);

            var data = await GetDataDownload(param, leaves, allowances);

            if (!data.Any())
                return new OperationResult(false, "No data for excel download");
            try
            {
                MemoryStream stream = new();
                var path = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Resources\\Template\\AttendanceMaintenance\\5_1_22_MonthlyAttendanceDataMaintenanceActiveEmployees\\Download.xlsx"
                );
                WorkbookDesigner designer = new() { Workbook = new Workbook(path) };


                Worksheet ws = designer.Workbook.Worksheets[0];
                ws.Cells["A1"].PutValue(param.Language == "en" ? "5.22 Monthly Attendance Data Maintenance-Active Employees" : "5.22 月份出勤資料維護-在職");
                ws.Cells["A2"].PutValue(param.Language == "en" ? "Factory" : "廠別");
                ws.Cells["C2"].PutValue(param.Language == "en" ? "Year-Month of Attendance" : "出勤年月");
                ws.Cells["F2"].PutValue(param.Language == "en" ? "Print By" : "列印人員");
                ws.Cells["H2"].PutValue(param.Language == "en" ? "Print Date" : "列印日期");
                ws.Cells["A4"].PutValue(param.Language == "en" ? "Department" : "部門");
                ws.Cells["B4"].PutValue(param.Language == "en" ? "Department Name" : "部門名稱");
                ws.Cells["C4"].PutValue(param.Language == "en" ? "Employee ID" : "工號");
                ws.Cells["D4"].PutValue(param.Language == "en" ? "Local Full Name" : "本地姓名");
                ws.Cells["E4"].PutValue(param.Language == "en" ? "New-Hired / Resigned" : "新進/離職");
                ws.Cells["F4"].PutValue(param.Language == "en" ? "Probation" : "試用期");
                ws.Cells["G4"].PutValue(param.Language == "en" ? "Permission Group" : "權限身分別");
                ws.Cells["H4"].PutValue(param.Language == "en" ? "Salary Type" : "薪資計別");
                ws.Cells["I4"].PutValue(param.Language == "en" ? "Paid Salary Days" : "計薪天數");
                ws.Cells["J4"].PutValue(param.Language == "en" ? "Actual Work Days" : "實際上班天數");
                ws.Cells["K4"].PutValue(param.Language == "en" ? "Delay/Early(times)" : "遲到/早退(次)");
                ws.Cells["L4"].PutValue(param.Language == "en" ? "No swip card(times)" : "未刷卡(次)");
                ws.Cells["M4"].PutValue(param.Language == "en" ? "Day Shift Meal Times" : "白班伙食次數");
                ws.Cells["N4"].PutValue(param.Language == "en" ? "Overtime Meal Times" : "加班伙食費");
                ws.Cells["O4"].PutValue(param.Language == "en" ? "Night Shift Allowance Times" : "夜點費次數");
                ws.Cells["P4"].PutValue(param.Language == "en" ? "Night Shift Meal Times" : "夜班伙食次數");
                ws.Cells["B2"].PutValue(param.Factory);
                ws.Cells["D2"].PutValue(param.Att_Month_Start);
                ws.Cells["G2"].PutValue(userName);
                ws.Cells["I2"].PutValue(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));

                designer.SetDataSource("result", data);
                designer.Process();

                int range = 0;
                Style style = ws.Cells["A4"].GetStyle();
                if (leaves.Any())
                {
                    Aspose.Cells.Range leaveRange = ws.Cells.CreateRange(3, 16, 1, leaves.Count);
                    style.ForegroundColor = Color.FromArgb(255, 242, 204);
                    leaveRange.SetStyle(style);
                    leaveRange.ColumnWidth = 20;

                    ArrayList leaveCodes = new();
                    leaves.ForEach(item => { leaveCodes.Add(item.Value); });
                    ws.Cells.ImportArrayList(leaveCodes, 3, 16, false);

                    range += leaves.Count;
                }

                if (allowances.Any())
                {
                    Aspose.Cells.Range allowanceRange = ws.Cells.CreateRange(3, 16 + leaves.Count, 1, allowances.Count);
                    style.ForegroundColor = Color.FromArgb(226, 239, 218);
                    allowanceRange.SetStyle(style);
                    allowanceRange.ColumnWidth = 20;

                    ArrayList allowanceCodes = new();
                    allowances.ForEach(item => { allowanceCodes.Add(item.Value); });
                    ws.Cells.ImportArrayList(allowanceCodes, 3, 16 + leaves.Count, false);

                    range += allowances.Count;
                }

                if (range > 0)
                {
                    Style styleRange = designer.Workbook.CreateStyle();
                    styleRange = AsposeUtility.SetAllBorders(styleRange);
                    styleRange.Custom = "0.00000";
                    Aspose.Cells.Range allowanceRange = ws.Cells.CreateRange(4, 16, data.Count, range);
                    allowanceRange.SetStyle(styleRange);
                }

                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].LeaveCodes.Any())
                    {
                        ArrayList leaveCodeDetail = new();
                        data[i].LeaveCodes.ForEach(item => { leaveCodeDetail.Add(item); });
                        ws.Cells.ImportArrayList(leaveCodeDetail, i + 4, 16, false);
                    }

                    if (data[i].AllowanceCodes.Any())
                    {
                        ArrayList allowanceDetail = new();
                        data[i].AllowanceCodes.ForEach(item => { allowanceDetail.Add(item); });
                        ws.Cells.ImportArrayList(allowanceDetail, i + 4, 16 + data[i].LeaveCodes.Count, false);
                    }
                }

                ws.AutoFitColumns();
                designer.Workbook.Save(stream, SaveFormat.Xlsx);
                return new OperationResult(true, stream.ToArray());
            }
            catch (Exception ex)
            {
                return new OperationResult(false, ex.InnerException.Message);
            }
        }

        private async Task<List<MaintenanceActiveEmployeesDetail>> GetDataDownload(MaintenanceActiveEmployeesParam param, List<KeyValuePair<string, string>> leaves, List<KeyValuePair<string, string>> allowances)
        {
            var (predMonthly, predProbation, predEmp) = SetPredicate(param);
            var HAM = _repositoryAccessor.HRMS_Att_Monthly
                         .FindAll(predMonthly, true)
                         .Project().To<MaintenanceActiveEmployeesDto>(cfg => cfg.Map(false).To(dto => dto.isProbation));
            var HAPM = _repositoryAccessor.HRMS_Att_Probation_Monthly
                        .FindAll(predProbation, true)
                        .Project().To<MaintenanceActiveEmployeesDto>(cfg => cfg.Map(true).To(dto => dto.isProbation));
            var HAPM_HAM = HAPM
                        .Join(HAM,
                        hapm => new { hapm.Factory, hapm.Att_Month, hapm.Employee_ID },
                        ham => new { ham.Factory, ham.Att_Month, ham.Employee_ID },
                        (hapm, ham) => hapm);
            var permissionGroup = await GetListPermissionGroup(param.Language);
            var salaryType = await GetListSalaryType(param.Language);
            var department = await GetDepartmentMain(param.Language);
            var personal = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predEmp, true);
            List<MaintenanceActiveEmployeesDetail> data = new();
            await HAM.Union(HAPM_HAM)
                .Join(personal,
                    x => new { x.USER_GUID },
                    y => new { y.USER_GUID },
                    (x, y) => new { HAM = x, HEP = y })
                .ForEachAsync(x =>
                {
                    var department_temp = string.IsNullOrWhiteSpace(x.HEP.Employment_Status)
                            ? department.FirstOrDefault(
                                d => d.Division == x.HEP.Division &&
                                d.Factory == x.HEP.Factory &&
                                d.Department_Code == x.HEP.Department)?.Department_Name
                            : x.HEP.Employment_Status == "A" || x.HEP.Employment_Status == "S"
                            ? department.FirstOrDefault(
                                d => d.Division == x.HEP.Assigned_Division &&
                                d.Factory == x.HEP.Assigned_Factory &&
                                d.Department_Code == x.HEP.Assigned_Department)?.Department_Name
                            : "";
                    var value = new MaintenanceActiveEmployeesDetail
                    {
                        Division = x.HAM.Division,
                        Factory = x.HAM.Factory,
                        Att_Month = x.HAM.Att_Month.ToString("yyyy/MM"),
                        Department = department_temp != null ? department_temp.Split(" - ")[0] : string.Empty,
                        Department_Name = department_temp != null ? department_temp.Split(" - ")[1] : string.Empty,
                        Employee_ID = x.HAM.Employee_ID,
                        Local_Full_Name = x.HEP.Local_Full_Name,
                        Pass = x.HAM.Pass == true ? "Y" : "N",
                        Resign_Status = x.HAM.Resign_Status,
                        Permission_Group = permissionGroup.FirstOrDefault(y => y.Key == x.HAM.Permission_Group).Value ?? x.HAM.Permission_Group,
                        Salary_Type = salaryType.FirstOrDefault(y => y.Key == x.HAM.Salary_Type).Value ?? x.HAM.Salary_Type,
                        Salary_Days = x.HAM.Salary_Days,
                        Actual_Days = x.HAM.Actual_Days,
                        Delay_Early = x.HAM.Delay_Early,
                        No_Swip_Card = x.HAM.No_Swip_Card,
                        Night_Eat_Times = x.HAM.Night_Eat_Times,
                        Food_Expenses = x.HAM.Food_Expenses,
                        DayShift_Food = x.HAM.DayShift_Food,
                        NightShift_Food = x.HAM.NightShift_Food,
                        Probation = x.HAM.Probation
                    };

                    var detail = !x.HAM.isProbation
                        ? _repositoryAccessor.HRMS_Att_Monthly_Detail.FindAll(y =>
                                        y.Factory == value.Factory &&
                                        y.Att_Month == x.HAM.Att_Month &&
                                        y.Employee_ID == value.Employee_ID, true)
                                        .Project().To<MaintenanceActiveEmployeesDto_Detail>(cfg => cfg.Map(false).To(dto => dto.isProbation))
                        : _repositoryAccessor.HRMS_Att_Probation_Monthly_Detail.FindAll(y =>
                                        y.Factory == value.Factory &&
                                        y.Att_Month == x.HAM.Att_Month &&
                                        y.Employee_ID == value.Employee_ID, true)
                                        .Project().To<MaintenanceActiveEmployeesDto_Detail>(cfg => cfg.Map(true).To(dto => dto.isProbation));
                    if (detail.Any())
                    {
                        var codesLeave = detail.Where(x => x.Leave_Type == "1").ToList();
                        var codesAllowance = detail.Where(x => x.Leave_Type == "2").ToList();
                        foreach (var item in leaves)
                        {
                            var code = codesLeave.FirstOrDefault(a => a.Leave_Code == item.Key);
                            if (code != null)
                                value.LeaveCodes.Add(code.Days);
                            else
                                value.LeaveCodes.Add(null);
                        }

                        foreach (var item in allowances)
                        {
                            var code = codesAllowance.FirstOrDefault(a => a.Leave_Code == item.Key);
                            if (code != null)
                                value.AllowanceCodes.Add(code.Days);
                            else
                                value.AllowanceCodes.Add(null);
                        }
                    }

                    data.Add(value);
                });

            return data;
        }
        #endregion

        private static (ExpressionStarter<HRMS_Att_Monthly> predMonthly, ExpressionStarter<HRMS_Att_Probation_Monthly> predProbation, ExpressionStarter<HRMS_Emp_Personal> predEmp) SetPredicate(MaintenanceActiveEmployeesParam param)
        {
            var predMonthly = PredicateBuilder.New<HRMS_Att_Monthly>(true);
            var predProbation = PredicateBuilder.New<HRMS_Att_Probation_Monthly>(true);
            var predEmp = PredicateBuilder.New<HRMS_Emp_Personal>(true);

            if (!string.IsNullOrWhiteSpace(param.Factory))
            {
                predMonthly.And(x => x.Factory == param.Factory);
                predProbation.And(x => x.Factory == param.Factory);
                predEmp.And(x => x.Factory == param.Factory || x.Assigned_Factory == param.Factory);
            }

            if (!string.IsNullOrWhiteSpace(param.Att_Month_Start) && !string.IsNullOrWhiteSpace(param.Att_Month_End))
            {
                predMonthly.And(x => x.Att_Month >= DateTime.Parse(param.Att_Month_Start) && x.Att_Month <= DateTime.Parse(param.Att_Month_End));
                predProbation.And(x => x.Att_Month >= DateTime.Parse(param.Att_Month_Start) && x.Att_Month <= DateTime.Parse(param.Att_Month_End));
            }

            if (!string.IsNullOrWhiteSpace(param.Department))
                predEmp.And(x => x.Department == param.Department || x.Assigned_Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
            {
                predMonthly.And(x => x.Employee_ID.ToLower().Contains(param.Employee_ID.Trim().ToLower()));
                predProbation.And(x => x.Employee_ID.ToLower().Contains(param.Employee_ID.Trim().ToLower()));
                predEmp.And(x => x.Employee_ID.ToLower().Contains(param.Employee_ID.Trim().ToLower())
                              || x.Assigned_Employee_ID.ToLower().Contains(param.Employee_ID.Trim().ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(param.Salary_Days))
            {
                predMonthly.And(x => x.Salary_Days == Convert.ToDecimal(param.Salary_Days));
                predProbation.And(x => x.Salary_Days == Convert.ToDecimal(param.Salary_Days));
            }

            return (predMonthly, predProbation, predEmp);
        }

        #region Setup for BaseService
        private async Task<List<HRMS_Att_Yearly>> Upd_HRMS_Att_Yearly(Upd_HRMS_Att_Yearly_Param param)
        {
            var codes = param.Details.Select(x => x.Code).ToList();
            var data = await _repositoryAccessor.HRMS_Att_Yearly
                .FindAll(x => x.Factory == param.Factory
                       && x.Att_Year == param.Att_Year
                       && x.Employee_ID == param.Employee_ID
                       && x.USER_GUID == param.USER_GUID
                       && x.Leave_Type == param.Leave_Type
                       && codes.Contains(x.Leave_Code))
                .ToListAsync();

            if (!data.Any())
                return new List<HRMS_Att_Yearly>();

            DateTime current = DateTime.Now;
            data.ForEach(x =>
            {
                var detail = param.Details.FirstOrDefault(d => d.Code == x.Leave_Code);
                if (detail is not null)
                {
                    x.Days += decimal.Parse(detail.Days);
                    x.Update_By = param.Account;
                    x.Update_Time = current;
                }
            });

            return data;
        }

        public async Task<List<KeyValuePair<string, string>>> Queryt_Factory_AddList(string userName, string language)
        {
            var factories = await _repositoryAccessor.HRMS_Basic_Role.FindAll(true)
                .Join(_repositoryAccessor.HRMS_Basic_Account_Role.FindAll(x => x.Account == userName, true),
                    HBR => HBR.Role,
                    HBAR => HBAR.Role,
                    (x, y) => new { HBR = x, HBAR = y })
                .Select(x => x.HBR.Factory)
                .Distinct()
                .ToListAsync();

            if (!factories.Any())
                return new();

            var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.Factory && factories.Contains(x.Code));
            var data = await Query_HRMS_Basic_Code(pred, language);

            return data;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string Language)
        {
            var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.Factory);
            var data = await Query_HRMS_Basic_Code(pred, Language);
            return data;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string Language)
        {
            var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup);
            var data = await Query_HRMS_Basic_Code(pred, Language);
            return data;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListSalaryType(string Language)
        {
            var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.SalaryType);
            var data = await Query_HRMS_Basic_Code(pred, Language);
            return data;
        }

        private async Task<List<DepartmentMain>> GetDepartmentMain(string language)
        {
            ExpressionStarter<HRMS_Org_Department> predDept = PredicateBuilder.New<HRMS_Org_Department>(true);
            ExpressionStarter<HRMS_Basic_Factory_Comparison> predCom = PredicateBuilder.New<HRMS_Basic_Factory_Comparison>(x => x.Kind == "1");
            var data = await QueryDepartment(predDept, predCom, language)
                .Select(
                    x => new DepartmentMain
                    {
                        Division = x.Department.Division,
                        Factory = x.Department.Factory,
                        Department_Code = x.Department.Department_Code,
                        Department_Name = $"{x.Department.Department_Code} - {(x.Language != null ? x.Language.Name : x.Department.Department_Name)}"
                    }
                ).ToListAsync();

            return data;
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

        private IOrderedQueryable<DepartmentJoinResult> QueryDepartment(ExpressionStarter<HRMS_Org_Department> predDept, ExpressionStarter<HRMS_Basic_Factory_Comparison> predCom, string language)
        {
            var data = _repositoryAccessor.HRMS_Org_Department.FindAll(predDept, true)
                .Join(_repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(predCom, true),
                    department => department.Division,
                    factoryComparison => factoryComparison.Division,
                    (department, factoryComparison) => department)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    department => new { department.Factory, department.Department_Code },
                    language => new { language.Factory, language.Department_Code },
                    (department, language) => new { Department = department, Language = language })
                .SelectMany(
                    x => x.Language.DefaultIfEmpty(),
                    (x, language) => new DepartmentJoinResult { Department = x.Department, Language = language })
                .OrderBy(x => x.Department.Department_Code);

            return data;
        }

        private async Task<List<KeyValuePair<string, string>>> Query_HRMS_Basic_Code(ExpressionStarter<HRMS_Basic_Code> predicate, string Language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(predicate, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
        }

        public async Task<List<KeyValuePair<string, string>>> GetEmployeeIDByFactorys(string factory)
        {
            var pred_Personal = PredicateBuilder.New<HRMS_Emp_Personal>(true);
            if (!string.IsNullOrWhiteSpace(factory))
                pred_Personal.And(x => x.Factory == factory);
            return await _repositoryAccessor.HRMS_Emp_Personal.FindAll(pred_Personal, true)
                        .Select(x => new KeyValuePair<string, string>(x.Employee_ID.Trim(), x.Employee_ID.Trim())).Distinct().ToListAsync();
        }
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
        #endregion
    }
}