using API.Data;
using API._Services.Interfaces.SalaryMaintenance;
using API.DTOs;
using API.DTOs.SalaryMaintenance;
using API.Helper.Constant;
using API.Helper.Utilities;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryMaintenance;
public class S_7_1_25_MonthlySalaryMaintenanceExitedEmployees : BaseServices, I_7_1_25_MonthlySalaryMaintenanceExitedEmployees
{
    public S_7_1_25_MonthlySalaryMaintenanceExitedEmployees(DBContext dbContext) : base(dbContext)
    {
    }

    #region Search
    public async Task<PaginationUtility<D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain>> GetDataPagination(PaginationParam pagination, D_7_25_MonthlySalaryMaintenanceExitedEmployeesSearchParam param)
    {
        if (string.IsNullOrWhiteSpace(param.Year_Month) || string.IsNullOrWhiteSpace(param.Factory) || string.IsNullOrWhiteSpace(param.Year_Month))
            return PaginationUtility<D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain>.Create(new(), pagination.PageNumber, pagination.PageSize);

        DateTime yearMonth = Convert.ToDateTime(param.Year_Month);

        var predProbation = PredicateBuilder.New<HRMS_Sal_Probation_Monthly>(x =>
                            x.Factory == param.Factory &&
                            param.Permission_Group.Contains(x.Permission_Group) &&
                            x.Sal_Month.Year == yearMonth.Year &&
                            x.Sal_Month.Month == yearMonth.Month);

        var predResign = PredicateBuilder.New<HRMS_Sal_Resign_Monthly>(x =>
                            x.Factory == param.Factory &&
                            param.Permission_Group.Contains(x.Permission_Group) &&
                            x.Sal_Month.Year == yearMonth.Year &&
                            x.Sal_Month.Month == yearMonth.Month);

        var predPersonal = PredicateBuilder.New<HRMS_Emp_Personal>(x =>
                            x.Factory == param.Factory &&
                            param.Permission_Group.Contains(x.Permission_Group));

        if (!string.IsNullOrWhiteSpace(param.Department))
        {
            predProbation = predProbation.And(x => x.Department == param.Department);
            predResign = predResign.And(x => x.Department == param.Department);
        }

        if (!string.IsNullOrWhiteSpace(param.Employee_ID))
        {
            predProbation = predProbation.And(x => x.Employee_ID.Contains(param.Employee_ID));
            predResign = predResign.And(x => x.Employee_ID.Contains(param.Employee_ID));
        }

        var HSC = await _repositoryAccessor.HRMS_Sal_Close.FindAll(x => x.Factory == param.Factory, true).ToListAsync();
        var HAM = await _repositoryAccessor.HRMS_Att_Monthly.FindAll(x => x.Factory == param.Factory, true).ToListAsync();
        var HARM = await _repositoryAccessor.HRMS_Att_Resign_Monthly.FindAll(x => x.Factory == param.Factory, true).ToListAsync();
        var HOP = await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == param.Factory, true).ToListAsync();
        var HOPL = await _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower(), true).ToListAsync();
        var HSPM = await _repositoryAccessor.HRMS_Sal_Probation_Monthly.FindAll(predProbation, true).ToListAsync();
        var HSRM = await _repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(predResign, true).ToListAsync();
        var HEP = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(predPersonal, true).ToListAsync();

        var query_Att_Monthly_Source = new D_7_25_Query_Att_Monthly
        {
            tblHRMS_Att_Monthly = HAM,
            tblHRMS_Att_Resign_Monthly = HARM
        };

        var department = HOP.GroupJoin(HOPL,
                            x => new { x.Division, x.Factory, x.Department_Code },
                            y => new { y.Division, y.Factory, y.Department_Code },
                            (x, y) => new { HOD = x, HODL = y })
                        .SelectMany(x => x.HODL.DefaultIfEmpty(),
                            (x, y) => new { x.HOD, HODL = y })
                        .Select(x => new
                        {
                            Code = x.HOD.Department_Code,
                            Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name,
                            x.HOD.Division,
                            x.HOD.Factory,
                        }).Distinct().ToList();

        // Probation = 'Y' (Seq = 1)
        var probationYData = HSPM.Where(x => x.Probation == "Y")
                        .Join(HSRM,
                            prob => new { prob.Factory, prob.Sal_Month, prob.Employee_ID },
                            resign => new { resign.Factory, resign.Sal_Month, resign.Employee_ID },
                            (prob, resign) => new { HSPM = prob, HSRM = resign })
                        .GroupJoin(HEP,
                            x => x.HSRM.USER_GUID,
                            y => y.USER_GUID,
                            (x, y) => new { x.HSPM, x.HSRM, HEP = y })
                        .SelectMany(x => x.HEP.DefaultIfEmpty(),
                            (x, y) => new { x.HSPM, x.HSRM, HEP = y })
                        .ToList();

        // Probation = 'N' (Seq = 2)
        var probationNData = HSPM.Where(x => x.Probation == "N")
                        .Join(HSRM,
                            prob => new { prob.Factory, prob.Sal_Month, prob.Employee_ID },
                            resign => new { resign.Factory, resign.Sal_Month, resign.Employee_ID },
                            (prob, resign) => new { HSPM = prob, HSRM = resign })
                        .GroupJoin(HEP,
                            x => x.HSRM.USER_GUID,
                            y => y.USER_GUID,
                            (x, y) => new { x.HSPM, x.HSRM, HEP = y })
                        .SelectMany(x => x.HEP.DefaultIfEmpty(),
                            (x, y) => new { x.HSPM, x.HSRM, HEP = y })
                        .ToList();

        // Resign Monthly (Seq = 3)
        var resignData = HSRM.GroupJoin(HEP,
                            x => x.USER_GUID,
                            y => y.USER_GUID,
                            (x, y) => new { HSRM = x, HEP = y })
                        .SelectMany(x => x.HEP.DefaultIfEmpty(),
                            (x, y) => new { x.HSRM, HEP = y })
                        .ToList();

        var probationYResult = probationYData.Select(x => CreateMainDto(x.HSRM, x.HEP, query_Att_Monthly_Source, "1", x.HSPM.Probation, x.HSPM.Tax));
        var probationNResult = probationNData.Select(x => CreateMainDto(x.HSRM, x.HEP, query_Att_Monthly_Source, "2", x.HSPM.Probation, x.HSPM.Tax));
        var resignResult = resignData.Select(x => CreateMainDto(x.HSRM, x.HEP, query_Att_Monthly_Source, "3", "ALL", x.HSRM.Tax));

        // Union
        var combinedData = probationYResult
                            .Union(probationNResult)
                            .Union(resignResult)
                            .OrderBy(x => x.Employee_ID)
                            .ThenBy(x => x.Seq)
                            .ToList();

        var result = combinedData.Select(item =>
        {
            item.Department_Name = department.FirstOrDefault(y => y.Code == item.Department)?.Name ?? string.Empty;
            item.FIN_Pass_Status = HSC.FirstOrDefault(y => y.Sal_Month == item.Year_Month && y.Employee_ID == item.Employee_ID)?.Close_Status ?? string.Empty;
            item.isDelete = HSC.Any(y => y.Sal_Month == item.Year_Month && y.Employee_ID == item.Employee_ID && y.Close_Status == "Y");
            return item;
        }).ToList();

        return PaginationUtility<D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain>.Create(result, pagination.PageNumber, pagination.PageSize);
    }

    private D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain CreateMainDto(dynamic salaryData, HRMS_Emp_Personal personal,
                              D_7_25_Query_Att_Monthly query_Att_Monthly_Source,
                              string seq, string probationFlag, int tax)
    {
        var query_Att_Monthly = Query_Att_Monthly(query_Att_Monthly_Source, "N", salaryData.Sal_Month, salaryData.Employee_ID);

        return new D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain
        {
            Seq = seq,
            Probation = probationFlag,
            Salary_Lock = salaryData.Lock,
            Year_Month = salaryData.Sal_Month,
            Factory = salaryData.Factory,
            Department = salaryData.Department,
            Employee_ID = salaryData.Employee_ID,
            Local_Full_Name = personal.Local_Full_Name,
            Permission_Group = salaryData.Permission_Group,
            Salary_Type = salaryData.Salary_Type,
            Transfer = salaryData.BankTransfer,
            Currency = salaryData.Currency,
            Tax = tax,
            Monthly_Attendance = query_Att_Monthly,
            Update_By = salaryData.Update_By,
            Update_Time = salaryData.Update_Time.ToString("yyyy/MM/dd HH:mm:ss")
        };
    }

    private static D_7_25_Query_Att_MonthlyResult Query_Att_Monthly(D_7_25_Query_Att_Monthly data, string kind, DateTime yearMonth, string employee_ID)
    {
        if (kind == "N")
        {
            var result = data.tblHRMS_Att_Resign_Monthly.FirstOrDefault(x => (x.Att_Month.Year == yearMonth.Year && x.Att_Month.Month == yearMonth.Month) && x.Employee_ID == employee_ID);
            var response = new D_7_25_Query_Att_MonthlyResult
            {
                Paid_Salary_Days = result?.Salary_Days ?? 0m,
                Actual_Work_Days = result?.Actual_Days ?? 0m,
                New_Hired_Resigned = result?.Resign_Status ?? string.Empty,
                Delay_Early = result?.Delay_Early ?? 0,
                No_Swip_Card = result?.No_Swip_Card ?? 0,
                Day_Shift_Meal_Times = result?.DayShift_Food ?? 0,
                Overtime_Meal_Times = result?.Food_Expenses ?? 0,
                Night_Shift_Allowance_Times = result?.Night_Eat_Times ?? 0,
                Night_Shift_Meal_Times = result?.NightShift_Food ?? 0
            };
            return response;
        }
        if (kind == "Y")
        {
            var result = data.tblHRMS_Att_Monthly.FirstOrDefault(x => (x.Att_Month.Year == yearMonth.Year && x.Att_Month.Month == yearMonth.Month) && x.Employee_ID == employee_ID);
            var response = new D_7_25_Query_Att_MonthlyResult
            {
                Paid_Salary_Days = result?.Salary_Days ?? 0m,
                Actual_Work_Days = result?.Actual_Days ?? 0m,
                New_Hired_Resigned = result?.Resign_Status ?? string.Empty,
                Delay_Early = result?.Delay_Early ?? 0,
                No_Swip_Card = result?.No_Swip_Card ?? 0,
                Day_Shift_Meal_Times = result?.DayShift_Food ?? 0,
                Overtime_Meal_Times = result?.Food_Expenses ?? 0,
                Night_Shift_Allowance_Times = result?.Night_Eat_Times ?? 0,
                Night_Shift_Meal_Times = result?.NightShift_Food ?? 0
            };
            return response;
        }
        return new();
    }
    #endregion

    #region Get List Search main
    public async Task<List<KeyValuePair<string, string>>> GetListSalaryType(string language)
    {
        return await GetDataBasicCode(BasicCodeTypeConstant.SalaryType, language);
    }

    public async Task<List<KeyValuePair<string, string>>> GetListFactory(string userName, string language)
    {
        List<string> factories = await Queryt_Factory_AddList(userName);
        var factoriesWithLanguage = await _repositoryAccessor.HRMS_Basic_Code
            .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory && factories.Contains(x.Code), true)
            .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                x => new { x.Type_Seq, x.Code },
                y => new { y.Type_Seq, y.Code },
                (HBC, HBCL) => new { HBC, HBCL })
            .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                (x, y) => new { x.HBC, HBCL = y })
            .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}")).ToListAsync();
        return factoriesWithLanguage;
    }

    public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language)
    {
        var permissionGroups = await Query_Permission_List(factory);
        var permissionGroupsWithLanguage = await _repositoryAccessor.HRMS_Basic_Code
                        .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup && permissionGroups.Select(x => x.Permission_Group).Contains(x.Code), true)
                        .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                            x => new { x.Type_Seq, x.Code },
                            y => new { y.Type_Seq, y.Code },
                            (HBC, HBCL) => new { HBC, HBCL })
                        .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                            (x, y) => new { x.HBC, HBCL = y })
                        .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}")).ToListAsync();
        return permissionGroupsWithLanguage;
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
                    .Select(x => new KeyValuePair<string, string>(x.dept.Department_Code, $"{x.dept.Department_Code}-{(x.hodl != null ? x.hodl.Name : x.dept.Department_Name)}"))
                    .ToList();
        return deparment;
    }
    #endregion

    #region Detail page
    public async Task<D_7_25_Query_Sal_Monthly_Detail_Result_Source> Get_MonthlyAttendanceData_MonthlySalaryDetail(D_7_25_GetMonthlyAttendanceDataDetailParam param)
    {
        var monthlyAttendanceData = await GetMonthlyAttendanceData(param);
        var monthlySalaryDetail = await GetMonthlySalaryDetail(param);

        return new D_7_25_Query_Sal_Monthly_Detail_Result_Source
        {
            Monthly_Attendance_Data = monthlyAttendanceData,
            Monthly_Salary_Detail = monthlySalaryDetail
        };
    }

    private async Task<D_7_25_Query_Att_Monthly_DetailResult> GetMonthlyAttendanceData(D_7_25_GetMonthlyAttendanceDataDetailParam param)
    {
        DateTime yearMonth = param.Year_Month.ToDateTime();

        var result1 = await Query_Att_Monthly_Detail("N", param.Factory, yearMonth, param.Employee_ID, "1", param.Language);
        var result2 = await Query_Att_Monthly_Detail("N", param.Factory, yearMonth, param.Employee_ID, "2", param.Language);

        return new D_7_25_Query_Att_Monthly_DetailResult
        {
            Table_Left_Leave = result1,
            Table_Right_Allowance = result2
        };
    }

    private async Task<D_7_25_Query_Sal_Monthly_Detail_Result> GetMonthlySalaryDetail(D_7_25_GetMonthlyAttendanceDataDetailParam param)
    {
        DateTime yearMonth = param.Year_Month.ToDateTime();

        List<Sal_Monthly_Detail_Values> result1 = new();
        List<Sal_Monthly_Detail_Values> result2 = new();
        List<Sal_Monthly_Detail_Values> result3 = new();
        List<Sal_Monthly_Detail_Values> result4 = new();
        List<Sal_Monthly_Detail_Values> result5 = new();
        List<Sal_Monthly_Detail_Values> result6 = new();
        List<Sal_Monthly_Detail_Values> result7 = new();

        // Table 1: Monthly Salary Detail
        if (param.Probation == "ALL")
            result1 = await Query_Sal_Monthly_Detail("N", param.Factory, yearMonth, param.Employee_ID, "45", "A", param.Permission_Group, param.Salary_Type, "0");
        else if (param.Probation == "Y")
            result1 = await Query_Sal_Monthly_Detail("PY", param.Factory, yearMonth, param.Employee_ID, "45", "A", param.Permission_Group, param.Salary_Type, "0");
        else // param.Probation == "N"
            result1 = await Query_Sal_Monthly_Detail("PN", param.Factory, yearMonth, param.Employee_ID, "45", "A", param.Permission_Group, param.Salary_Type, "0");

        // Table 2: Overtime and Night Shift Allowance
        if (param.Probation == "ALL")
            result2 = await Query_Sal_Monthly_Detail("N", param.Factory, yearMonth, param.Employee_ID, "42", "A", param.Permission_Group, param.Salary_Type, "2");
        else if (param.Probation == "Y")
            result2 = await Query_Sal_Monthly_Detail("PY", param.Factory, yearMonth, param.Employee_ID, "42", "A", param.Permission_Group, param.Salary_Type, "2");
        else // param.Probation == "N" 
            result2 = await Query_Sal_Monthly_Detail("PN", param.Factory, yearMonth, param.Employee_ID, "42", "A", param.Permission_Group, param.Salary_Type, "2");

        // Table 3: Addition Items (49, A and B)
        if (param.Probation == "ALL")
        {
            result3 = await Query_Sal_Monthly_Detail("N", param.Factory, yearMonth, param.Employee_ID, "49", "A", param.Permission_Group, param.Salary_Type, "0");
            result4 = await Query_Sal_Monthly_Detail("N", param.Factory, yearMonth, param.Employee_ID, "49", "B", param.Permission_Group, param.Salary_Type, "0");
        }

        // Table 4: Deduction Item (49, C and D)
        if (param.Probation == "ALL")
        {
            result5 = await Query_Sal_Monthly_Detail("N", param.Factory, yearMonth, param.Employee_ID, "49", "C", param.Permission_Group, param.Salary_Type, "0");
            result6 = await Query_Sal_Monthly_Detail("N", param.Factory, yearMonth, param.Employee_ID, "49", "D", param.Permission_Group, param.Salary_Type, "0");
        }

        // Table 5: Insurance Deduction
        if (param.Probation == "ALL")
            result7 = await Query_Sal_Monthly_Detail("N", param.Factory, yearMonth, param.Employee_ID, "57", "D", param.Permission_Group, param.Salary_Type, "0");

        // Lấy danh sách Item_Name từ các loại leave types
        var leaveTypes45 = await GetLeaveTypes(param.Language, "45");
        var leaveTypes42 = await GetLeaveTypes(param.Language, "42");
        var leaveTypes49 = await GetLeaveTypes(param.Language, "49");
        var leaveTypes57 = await GetLeaveTypes(param.Language, "57");

        // Chuyển đổi từ Sal_Monthly_Detail_Values sang D_7_25_Salary_Item_Sal_Monthly_Detail_Values
        var table1 = ConvertToSalaryItemValues(result1, "45", "A", leaveTypes45);
        var table2 = ConvertToSalaryItemValues(result2, "42", "A", leaveTypes42);
        var table3 = ConvertToSalaryItemValues(result3, "49", "A", leaveTypes49)
            .Concat(ConvertToSalaryItemValues(result4, "49", "B", leaveTypes49)).ToList();
        var table4 = ConvertToSalaryItemValues(result5, "49", "C", leaveTypes49)
            .Concat(ConvertToSalaryItemValues(result6, "49", "D", leaveTypes49)).ToList();
        var table5 = ConvertToSalaryItemValues(result7, "57", "D", leaveTypes57);

        return new D_7_25_Query_Sal_Monthly_Detail_Result
        {
            Salary_Item_Table1 = table1,
            Salary_Item_Table2 = table2,
            Salary_Item_Table3 = table3,
            Salary_Item_Table4 = table4,
            Salary_Item_Table5 = table5,
            Tax = param.Tax,
            Total_Item_Table1 = table1.Sum(x => x.Amount),
            Total_Item_Table2 = table2.Sum(x => x.Amount),
            Total_Item_Table3 = table3.Sum(x => x.Amount),
            Total_Item_Table4 = table4.Sum(x => x.Amount),
            Total_Item_Table5 = table5.Sum(x => x.Amount)
        };
    }

    private static List<D_7_25_Salary_Item_Sal_Monthly_Detail_Values> ConvertToSalaryItemValues(
        List<Sal_Monthly_Detail_Values> salaryDetails,
        string typeSeq,
        string addDedType,
        List<D_7_25_Leave_Code_Name> leaveTypes)
    {
        return salaryDetails.Select(x => new D_7_25_Salary_Item_Sal_Monthly_Detail_Values
        {
            Item = x.Item,
            Item_Name = leaveTypes.FirstOrDefault(l => l.Code == x.Item)?.Code_Name ?? x.Item,
            Amount = x.Amount,
            Type_Seq = typeSeq,
            AddDed_Type = addDedType
        }).ToList();
    }

    private async Task<List<D_7_25_AttMonthlyDetailValues>> Query_Att_Monthly_Detail(string kind, string factory, DateTime yearMonth, string employeeId, string leaveType, string language)
    {
        IQueryable<D_7_25_Att_Monthly_Detail_Temp> attMonthlyDetailTemp;
        if (kind == "Y")
        {
            attMonthlyDetailTemp = _repositoryAccessor.HRMS_Att_Monthly_Detail
                .FindAll(d => d.Factory == factory && (d.Att_Month.Year == yearMonth.Year && d.Att_Month.Month == yearMonth.Month) && d.Employee_ID == employeeId && d.Leave_Type == leaveType)
                .Select(d => new D_7_25_Att_Monthly_Detail_Temp
                {
                    Factory = d.Factory,
                    Att_Month = d.Att_Month,
                    Employee_ID = d.Employee_ID,
                    Leave_Type = d.Leave_Type,
                    Leave_Code = d.Leave_Code,
                    Days = d.Days
                });
        }
        else if (kind == "N")
        {
            attMonthlyDetailTemp = _repositoryAccessor.HRMS_Att_Resign_Monthly_Detail
                 .FindAll(d => d.Factory == factory && (d.Att_Month.Year == yearMonth.Year && d.Att_Month.Month == yearMonth.Month) && d.Employee_ID == employeeId && d.Leave_Type == leaveType)
                 .Select(d => new D_7_25_Att_Monthly_Detail_Temp
                 {
                     Factory = d.Factory,
                     Att_Month = d.Att_Month,
                     Employee_ID = d.Employee_ID,
                     Leave_Type = d.Leave_Type,
                     Leave_Code = d.Leave_Code,
                     Days = d.Days
                 });
        }
        else
        {
            throw new ArgumentException("Invalid kind value");
        }
        var tblHRMS_Att_Use_Monthly_Leave = _repositoryAccessor.HRMS_Att_Use_Monthly_Leave.FindAll(s => s.Factory == factory && s.Leave_Type == leaveType, true);
        DateTime max_Effective_Month = await tblHRMS_Att_Use_Monthly_Leave.Where(s => s.Effective_Month <= yearMonth).Select(s => s.Effective_Month).MaxAsync();

        var settingTemp = tblHRMS_Att_Use_Monthly_Leave
                .Where(s => s.Effective_Month == max_Effective_Month);
        var leaveTypes = await GetLeaveTypes(language, leaveType);
        var attMonthlyDetailValues = attMonthlyDetailTemp
        .Join(settingTemp, x => x.Leave_Code, y => y.Code, (x, y) => new { attMonthlyDetailTemp = x, settingTemp = y })
        .OrderBy(x => x.settingTemp.Seq)
        .AsEnumerable()
        .Select(x => new D_7_25_AttMonthlyDetailValues
        {
            Leave_Code = x.attMonthlyDetailTemp.Leave_Code,
            Leave_Code_Name = leaveTypes.FirstOrDefault(l => l.Code == x.attMonthlyDetailTemp.Leave_Code)?.Code_Name ?? x.attMonthlyDetailTemp.Leave_Code,
            Days = x.attMonthlyDetailTemp.Days,
        }).ToList();

        return attMonthlyDetailValues;
    }

    private async Task<List<D_7_25_Leave_Code_Name>> GetLeaveTypes(string language, string leaveType)
    {
        string _type = string.Empty;
        ExpressionStarter<HRMS_Basic_Code> predicate = PredicateBuilder.New<HRMS_Basic_Code>(true);

        if (leaveType == "1")
        {
            _type = BasicCodeTypeConstant.Leave;
            predicate = predicate.And(x => x.Char1 == "Leave");
        }
        else if (leaveType == "45")
        {
            _type = BasicCodeTypeConstant.SalaryItem;
        }
        else if (leaveType == "42" || leaveType == "2")
        {
            _type = BasicCodeTypeConstant.Allowance;
        }
        else if (leaveType == "49")
        {
            _type = BasicCodeTypeConstant.AdditionsAndDeductionsItem;
        }
        else if (leaveType == "57")
        {
            _type = BasicCodeTypeConstant.InsuranceType;
        }

        predicate = predicate.And(x => x.Type_Seq == _type && x.IsActive == true);

        var result = await _repositoryAccessor.HRMS_Basic_Code.FindAll(predicate, true)
                    .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                       x => new { x.Type_Seq, x.Code },
                       y => new { y.Type_Seq, y.Code },
                       (x, y) => new { HBC = x, HBCL = y }
                    ).SelectMany(x => x.HBCL.DefaultIfEmpty(),
                        (x, y) => new { x.HBC, HBCL = y }
                    ).Select(x => new D_7_25_Leave_Code_Name
                    {
                        Code = x.HBC.Code.Trim(),
                        Code_Name = x.HBC.Code.Trim() + "-" + (x.HBCL != null ? x.HBCL.Code_Name.Trim() : x.HBC.Code_Name.Trim())
                    }).Distinct().ToListAsync();

        return result;
    }
    #endregion

    #region Update
    public async Task<OperationResult> Update(D_7_25_MonthlySalaryMaintenance_Update data)
    {
        await _repositoryAccessor.BeginTransactionAsync();
        try
        {
            var dataExists = await _repositoryAccessor.HRMS_Sal_Resign_Monthly
                .FirstOrDefaultAsync(x => x.Factory == data.Param.Factory
                    && x.Sal_Month == data.Param.Sal_Month.ToDateTime()
                    && x.Employee_ID == data.Param.Employee_ID);

            if (dataExists is null)
                return new OperationResult(false, "No data");
            DateTime now = DateTime.Now;
            dataExists.Tax = data.Param.Tax;
            dataExists.Update_By = data.Param.Update_By;
            dataExists.Update_Time = now;

            List<HRMS_Sal_Resign_Monthly_Detail> dataSal_Resign_MonthlyUpdateDetails = new();
            // Combine all salary item tables into one list
            List<D_7_25_Salary_Item_Sal_Monthly_Detail_Values> allSalaryItems = new();
            allSalaryItems.AddRange(data.Table_Details.Salary_Item_Table1);
            allSalaryItems.AddRange(data.Table_Details.Salary_Item_Table2);
            allSalaryItems.AddRange(data.Table_Details.Salary_Item_Table3);
            allSalaryItems.AddRange(data.Table_Details.Salary_Item_Table4);
            allSalaryItems.AddRange(data.Table_Details.Salary_Item_Table5);

            // Iterate through all items in the combined list
            foreach (var item in allSalaryItems)
            {
                var existingDetail = await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
                    .FirstOrDefaultAsync(x => x.Factory == data.Param.Factory
                        && x.Employee_ID == data.Param.Employee_ID
                        && x.Sal_Month == data.Param.Sal_Month.ToDateTime()
                        && x.Type_Seq == item.Type_Seq
                        && x.AddDed_Type == item.AddDed_Type && x.Item == item.Item);
                if (existingDetail is null) continue;
                existingDetail.Amount = (int)item.Amount;
                existingDetail.Update_By = data.Param.Update_By;
                existingDetail.Update_Time = now;
                dataSal_Resign_MonthlyUpdateDetails.Add(existingDetail);
            }

            _repositoryAccessor.HRMS_Sal_Resign_Monthly.Update(dataExists);
            _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail.UpdateMultiple(dataSal_Resign_MonthlyUpdateDetails);

            await _repositoryAccessor.Save();
            await _repositoryAccessor.CommitAsync();

            return new OperationResult(true, "System.Message.UpdateOKMsg");
        }
        catch //(Exception ex)
        {
            await _repositoryAccessor.RollbackAsync();
            return new OperationResult(false, $"System.Message.UpdateErrorMsg");
        }
    }
    #endregion

    #region Delete
    public async Task<OperationResult> Delete(D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain data)
    {
        await _repositoryAccessor.BeginTransactionAsync();
        try
        {
            var factory = data.Factory;
            var salMonth = data.Year_Month;
            var employeeId = data.Employee_ID;

            var deleteMark = _repositoryAccessor.HRMS_Att_Probation_Monthly
                            .FindAll(x => x.Factory == factory &&
                                            x.Att_Month == salMonth &&
                                            x.Employee_ID == employeeId)
                            .Count();

            _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail.RemoveMultiple(
                await _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail.FindAll(x => x.Factory == factory && x.Employee_ID == employeeId && x.Sal_Month == salMonth).ToListAsync());

            _repositoryAccessor.HRMS_Sal_Probation_Monthly.RemoveMultiple(
                await _repositoryAccessor.HRMS_Sal_Probation_Monthly.FindAll(x => x.Factory == factory && x.Employee_ID == employeeId && x.Sal_Month == salMonth).ToListAsync());

            _repositoryAccessor.HRMS_Sal_Close.RemoveMultiple(
                await _repositoryAccessor.HRMS_Sal_Close.FindAll(x => x.Factory == factory && x.Sal_Month == salMonth && x.Employee_ID == employeeId).ToListAsync());

            _repositoryAccessor.HRMS_Sal_Tax.RemoveMultiple(
                await _repositoryAccessor.HRMS_Sal_Tax.FindAll(x => x.Factory == factory && x.Sal_Month == salMonth && x.Employee_ID == employeeId).ToListAsync());

            _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail.RemoveMultiple(
                await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail.FindAll(x => x.Factory == factory && x.Sal_Month == salMonth && x.Employee_ID == employeeId).ToListAsync());

            _repositoryAccessor.HRMS_Sal_Resign_Monthly.RemoveMultiple(
                await _repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(x => x.Factory == factory && x.Sal_Month == salMonth && x.Employee_ID == employeeId).ToListAsync());

            _repositoryAccessor.HRMS_Att_Resign_Monthly_Detail.RemoveMultiple(
                await _repositoryAccessor.HRMS_Att_Resign_Monthly_Detail.FindAll(x => x.Factory == factory && x.Att_Month == salMonth && x.Employee_ID == employeeId).ToListAsync());

            _repositoryAccessor.HRMS_Att_Resign_Monthly.RemoveMultiple(
                await _repositoryAccessor.HRMS_Att_Resign_Monthly.FindAll(x => x.Factory == factory && x.Att_Month == salMonth && x.Employee_ID == employeeId).ToListAsync());

            if (deleteMark > 0)
            {
                _repositoryAccessor.HRMS_Att_Probation_Monthly.RemoveMultiple(
                    await _repositoryAccessor.HRMS_Att_Probation_Monthly.FindAll(x => x.Factory == factory && x.Att_Month == salMonth && x.Employee_ID == employeeId).ToListAsync());

                _repositoryAccessor.HRMS_Att_Probation_Monthly_Detail.RemoveMultiple(
                    await _repositoryAccessor.HRMS_Att_Probation_Monthly_Detail.FindAll(x => x.Factory == factory && x.Att_Month == salMonth && x.Employee_ID == employeeId).ToListAsync());
            }

            await _repositoryAccessor.Save();
            await _repositoryAccessor.CommitAsync();

            return new OperationResult(true, "System.Message.DeleteOKMsg");
        }
        catch (Exception)
        {
            await _repositoryAccessor.RollbackAsync();
            return new OperationResult(false, "System.Message.DeleteErrorMsg");
        }
    }
    #endregion
}
