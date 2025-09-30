using System.Globalization;
using API.Data;
using API._Services.Interfaces.SalaryReport;
using API.Helper.Constant;
using API.DTOs.SalaryReport;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace API._Services.Services.SalaryReport
{
  public class S_7_2_4_MonthlySalarySummaryReport : BaseServices, I_7_2_4_MonthlySalarySummaryReport
  {
    public S_7_2_4_MonthlySalarySummaryReport(DBContext dbContext) : base(dbContext) { }

    #region GetList
    // List Factory
    public async Task<List<KeyValuePair<string, string>>> GetListFactory(string language, string userName)
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

    //List Permistion_Group
    public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language)
    {
      return await Query_BasicCode_PermissionGroup(factory, language);
    }

    //List Level
    public async Task<List<KeyValuePair<string, string>>> GetListLevel(string language)
    {
      return await GetHRMS_Basic_Code(BasicCodeTypeConstant.Level, language);
    }

    // List Department
    public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
    {
      var data = await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == factory, true)
          .Join(_repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(b => b.Kind == "1" && b.Factory == factory, true),
              x => x.Division,
              y => y.Division,
              (x, y) => x)
          .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
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

    private async Task<List<KeyValuePair<string, string>>> GetHRMS_Basic_Code(string Type_Seq, string language)
    {
      return await _repositoryAccessor.HRMS_Basic_Code
          .FindAll(x => x.Type_Seq == Type_Seq, true)
          .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
              HBC => new { HBC.Type_Seq, HBC.Code },
              HBCL => new { HBCL.Type_Seq, HBCL.Code },
              (HBC, HBCL) => new { HBC, HBCL })
              .SelectMany(x => x.HBCL.DefaultIfEmpty(),
              (prev, HBCL) => new { prev.HBC, HBCL })
          .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
          .ToListAsync();
    }

    private async Task<List<KeyValuePair<string, string>>> GetDepartmentName(string factory, string language)
    {
      var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == factory, true);
      var HODL = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Factory == factory && x.Language_Code.ToLower() == language.ToLower(), true);

      return await HOD
          .GroupJoin(HODL,
              department => new { department.Factory, department.Department_Code },
              lang => new { lang.Factory, lang.Department_Code },
              (department, lang) => new { department, lang })
              .SelectMany(x => x.lang.DefaultIfEmpty(),
              (department, lang) => new { department.department, lang })
          .Select(x => new KeyValuePair<string, string>(x.department.Department_Code, $"{(x.lang != null ? x.lang.Name : x.department.Department_Name)}"))
          .ToListAsync();
    }
    #endregion

    #region GetData
    private async Task<OperationResult> GetData(MonthlySalarySummaryReportParam param, bool countOnly = false)
    {
      if (!DateTime.TryParseExact(param.Year_Month_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime yearMonth))
        return new OperationResult(false, "Invalid Year-Month");

      var startDate = new DateTime(yearMonth.Year, yearMonth.Month, 1);
      var endDate = startDate.AddMonths(1).AddDays(-1);

      var pred = PredicateBuilder.New<HRMS_Emp_Personal>(x =>
          x.Factory == param.Factory &&
          param.Permission_Group.Contains(x.Permission_Group));

      if (!string.IsNullOrWhiteSpace(param.Department))
        pred.And(x => x.Department == param.Department);

      var Emp_Personal = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(pred, true).ToListAsync();

      var isAll = param.Transfer == "All";
      var HSM = _repositoryAccessor.HRMS_Sal_Monthly
                        .FindAll(x => x.Factory == param.Factory &&
                                    x.Sal_Month == yearMonth &&
                                    (isAll || x.BankTransfer == param.Transfer), true)
                        .ToList();

      var HSRM = _repositoryAccessor.HRMS_Sal_Resign_Monthly
                  .FindAll(x => x.Factory == param.Factory &&
                              x.Sal_Month == yearMonth &&
                              (isAll || x.BankTransfer == param.Transfer), true)
                  .ToList();

      var Sal_Monthly = new List<Sal_Monthly_7_2_4>();

      if (param.Kind == "Y")
      // On job
      {
        Sal_Monthly = Emp_Personal.Where(x => x.Resign_Date > endDate || x.Resign_Date == null)
                           .Join(HSM,
                               personal => personal.Employee_ID,
                               salary => salary.Employee_ID,
                               (personal, salary) => new { Personal = personal, Salary = salary })
                           .Select(x => new Sal_Monthly_7_2_4
                           {
                             Employee_ID = x.Personal.Employee_ID,
                             Department = x.Salary.Department,
                             Permission_Group = x.Salary.Permission_Group,
                             Salary_Type = x.Salary.Salary_Type,
                             Tax = x.Salary.Tax,
                           })
                           .ToList();
      }
      else
      // Resigned
      {
        Sal_Monthly = Emp_Personal.Where(x => x.Resign_Date >= startDate && x.Resign_Date <= endDate && x.Resign_Date != null)
                           .Join(HSRM,
                               personal => personal.Employee_ID,
                               salary => salary.Employee_ID,
                               (personal, salary) => new { Personal = personal, Salary = salary })
                           .Select(x => new Sal_Monthly_7_2_4
                           {
                             Employee_ID = x.Personal.Employee_ID,
                             Department = x.Salary.Department,
                             Permission_Group = x.Salary.Permission_Group,
                             Salary_Type = x.Salary.Salary_Type,
                             Tax = x.Salary.Tax,
                           })
                           .ToList();
      }

      if (!Sal_Monthly.Any())
        return new OperationResult(false, "No Salary Data");

      List<KeyValuePair<string, string>> departmentMapping = new();
      List<string> targetDepartments;

      if (param.Report_Kind == "D")
      {
        targetDepartments = Sal_Monthly.Select(x => x.Department).Distinct().ToList();
        if (!countOnly)
        {
          foreach (var emp in Sal_Monthly)
            departmentMapping.Add(new KeyValuePair<string, string>(emp.Employee_ID, emp.Department));
        }
      }
      else
      {
        var departmentList = await _repositoryAccessor.HRMS_Org_Department
                          .FindAll(x => x.Factory == param.Factory && x.IsActive == true, true)
                          .ToListAsync();

        var groupByLevel = GetGroupByLevel(departmentList, param.Level);

        targetDepartments = Sal_Monthly
            .Select(x => GetDepartmentByLevel(x.Department, departmentList, groupByLevel))
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .ToList();

        if (!countOnly)
        {
          foreach (var emp in Sal_Monthly)
          {
            var mappedDept = GetDepartmentByLevel(emp.Department, departmentList, groupByLevel);
            if (!string.IsNullOrEmpty(mappedDept))
              departmentMapping.Add(new KeyValuePair<string, string>(emp.Employee_ID, mappedDept));
          }
        }
      }

      if (countOnly)
        return new OperationResult(true, targetDepartments);

      // Employees sau khi lọc Department
      var mappedEmployeeIds = departmentMapping.Select(x => x.Key).ToList();
      var sal_Monthly = Sal_Monthly.Where(x => mappedEmployeeIds.Contains(x.Employee_ID)).ToList();

      var departmentNames = await GetDepartmentName(param.Factory, param.Lang);
      var employeeIds = sal_Monthly.Select(x => x.Employee_ID).ToList();
      var permissionGroups = sal_Monthly.Select(x => x.Permission_Group).ToList();
      var salaryTypes = sal_Monthly.Select(x => x.Salary_Type).ToList();

      var salaryItemDetail = await Query_Sal_Monthly_Detail(param.Kind, param.Factory, yearMonth, employeeIds, "45", "A", permissionGroups, salaryTypes, "0");
      var overtimeItems = new[] { "A01", "B01", "C01" };
      var overtimeDetails = await Sal_Detail(param.Kind, param.Factory, yearMonth, employeeIds, "42", "A");
      var nightShiftItems = new[] { "A02", "B03", "B03" };
      var nightShiftDetails = await Sal_Detail(param.Kind, param.Factory, yearMonth, employeeIds, "42", "A");
      var total45A = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "45", "A");
      var total42A = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "42", "A");
      var total49A = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "49", "A");
      var total49B = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "49", "B");
      var insuranceDeductionDetail = await Query_Sal_Monthly_Detail(param.Kind, param.Factory, yearMonth, employeeIds, "57", "D", permissionGroups, salaryTypes, "0");
      var salDetailSumC = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "49", "C");
      var salDetailSumD = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "49", "D");
      var total57D = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "57", "D");
      var total49C = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "49", "C");
      var total49D = await Query_Sal_Monthly_Detail_Sum(param.Kind, param.Factory, yearMonth, employeeIds, "49", "D");

      var data = new List<EmployeeSalaryInfo>();

      foreach (var emp in sal_Monthly)
      {
        // Department
        var empDepartmentMapping = departmentMapping.FirstOrDefault(x => x.Key == emp.Employee_ID);
        string department = !string.IsNullOrEmpty(empDepartmentMapping.Value) ? empDepartmentMapping.Value : emp.Department;

        var mappedDeptName = departmentNames.FirstOrDefault(x => x.Key == department);
        string departmentName = mappedDeptName.Value ?? department;

        // Salary Item
        var salary_Item = salaryItemDetail.Where(x => x.Employee_ID == emp.Employee_ID &&
                                                                x.Permission_Group == emp.Permission_Group &&
                                                                x.Salary_Type == emp.Salary_Type);
        // Overtime Allowance
        var overtimeAllowance = overtimeDetails
          .Where(x => x.Employee_ID == emp.Employee_ID && overtimeItems.Contains(x.Item))?
          .Sum(x => x.Amount) ?? 0m;
        // Night Shift Allowance
        var nightShiftAllowance = nightShiftDetails
          .Where(x => x.Employee_ID == emp.Employee_ID && nightShiftItems.Contains(x.Item))?
          .Sum(x => x.Amount) ?? 0m;
        // Other Additions
        decimal totalTypeA = total49A.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0m;
        decimal totalTypeB = total49B.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0m;
        var totalOtherAdditions = totalTypeA + totalTypeB;
        // Total Addition Item 
        var totalAdditionItem = (total45A.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0m) +
                                (total42A.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0m) +
                                (total49A.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0m) +
                                (total49B.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0m);
        // Insurance Deduction
        var insurance_Deduction = insuranceDeductionDetail.Where(x => x.Employee_ID == emp.Employee_ID);
        // Other Deduction
        decimal totalTypeC = salDetailSumC.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0m;
        decimal totalTypeD = salDetailSumD.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0m;
        var totalOtherDeduction = totalTypeC + totalTypeD;
        // Total Deduction Item 
        var totalDeductionItem = (total57D.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0m) +
                                 (total49C.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0m) +
                                 (total49D.FirstOrDefault(x => x.Employee_ID == emp.Employee_ID)?.Amount ?? 0m) +
                                 emp.Tax;
        // Net Amount Received
        var netAmountReceived = totalAdditionItem - totalDeductionItem;

        data.Add(new EmployeeSalaryInfo
        {
          Employee_ID = emp.Employee_ID,
          Department = department,
          Department_Name = departmentName,
          SalaryDetails = salary_Item.Select(x => new KeyValuePair<string, decimal>(x.Item, x.Amount)).ToList(),
          OvertimeAllowance = overtimeAllowance,
          NightShiftAllowance = nightShiftAllowance,
          OtherAdditions = totalOtherAdditions,
          TotalAddtionsItem = totalAdditionItem,
          InsuranceDeductionDetails = insurance_Deduction.Select(x => new KeyValuePair<string, decimal>(x.Item, x.Amount)).ToList(),
          Tax = emp.Tax,
          OtherDeductions = totalOtherDeduction,
          TotalDeductionItem = totalDeductionItem,
          NetAmountReceived = netAmountReceived
        });
      }

      List<MonthlySalarySummaryReportDto> result = new();
      foreach (var group in data.GroupBy(x => x.Department))
      {
        var reportDto = await CreateReportDto(group.ToList(), param.Factory, yearMonth, param.Kind, param.Report_Kind);
        result.Add(reportDto);
      }
      result = result.OrderBy(x => x.Department).ToList();

      return new OperationResult(true, result);
    }
    #endregion

    #region CreateReportDto
    private async Task<MonthlySalarySummaryReportDto> CreateReportDto(List<EmployeeSalaryInfo> employees, string factory, DateTime yearMonth, string kind, string reportKind)
    {
      var allSalaryItems = employees.SelectMany(emp => emp.SalaryDetails).ToList();
      var salary_Item = allSalaryItems
          .GroupBy(x => x.Key)
          .Select(x => new KeyValuePair<string, decimal>(x.Key, x.Sum(x => x.Value)))
          .ToList();

      var allInsuranceDeductions = employees.SelectMany(emp => emp.InsuranceDeductionDetails).ToList();
      var insurance_Deductions = allInsuranceDeductions
          .GroupBy(item => item.Key)
          .Select(x => new KeyValuePair<string, decimal>(x.Key, x.Sum(x => x.Value)))
          .ToList();

      var department = employees.FirstOrDefault().Department;
      int departmentHeadcount = 0;

      if (reportKind == "D")
      {
        if (kind == "Y")
        {
          departmentHeadcount = await _repositoryAccessor.HRMS_Sal_Monthly
              .FindAll(x => x.Factory == factory &&
                           x.Sal_Month == yearMonth &&
                           x.Department == department, true)
              .CountAsync();
        }
        else
        {
          departmentHeadcount = await _repositoryAccessor.HRMS_Sal_Resign_Monthly
              .FindAll(x => x.Factory == factory &&
                           x.Sal_Month == yearMonth &&
                           x.Department == department, true)
              .CountAsync();
        }
      }
      else departmentHeadcount = employees.Count;

      return new MonthlySalarySummaryReportDto
      {
        Department = employees.FirstOrDefault().Department,
        Department_Name = employees.FirstOrDefault().Department_Name,
        Department_Headcount = departmentHeadcount,
        Salary_Item = salary_Item,
        Overtime_Allowance = employees.Sum(x => x.OvertimeAllowance),
        Night_Shift_Allowance = employees.Sum(x => x.NightShiftAllowance),
        Other_Additions = employees.Sum(x => x.OtherAdditions),
        Total_Addition_Item = employees.Sum(x => x.TotalAddtionsItem),
        Insurance_Deduction = insurance_Deductions,
        Tax = employees.Sum(x => x.Tax),
        Other_Deductions = employees.Sum(x => x.OtherDeductions),
        Total_Deduction_Item = employees.Sum(x => x.TotalDeductionItem),
        Net_Amount_Received = employees.Sum(x => x.NetAmountReceived)
      };
    }
    #endregion

    #region Sal_Detail
    private async Task<List<SalaryDetailResult>> Sal_Detail(string kind, string factory, DateTime yearMonth, List<string> employeeIds, string typeSeq, string addedType)
    {
      if (kind == "Y")
      {
        return await _repositoryAccessor.HRMS_Sal_Monthly_Detail
            .FindAll(x => x.Factory == factory &&
                         x.Sal_Month == yearMonth &&
                         employeeIds.Contains(x.Employee_ID) &&
                         x.Type_Seq == typeSeq &&
                         x.AddDed_Type == addedType, true)
            .Select(x => new SalaryDetailResult
            {
              Employee_ID = x.Employee_ID,
              Item = x.Item,
              Amount = x.Amount
            })
            .ToListAsync();
      }
      else
      {
        return await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
            .FindAll(x => x.Factory == factory &&
                         x.Sal_Month == yearMonth &&
                         employeeIds.Contains(x.Employee_ID) &&
                         x.Type_Seq == typeSeq &&
                         x.AddDed_Type == addedType, true)
            .Select(x => new SalaryDetailResult
            {
              Employee_ID = x.Employee_ID,
              Item = x.Item,
              Amount = x.Amount
            })
            .ToListAsync();
      }
    }
    #endregion

    #region GetDepartmentByLevel
    private static List<HRMS_Org_Department> GetGroupByLevel(List<HRMS_Org_Department> departmentList, string level)
    {
      int targetLevel = int.Parse(level);

      var lowerLevelDepartments = departmentList
          .Where(x => x.IsActive && int.Parse(x.Org_Level) < targetLevel)
          .Select(x => x.Department_Code)
          .ToList();

      return departmentList
          .Where(x => x.IsActive && x.Factory == x.Factory &&
              (int.Parse(x.Org_Level) == targetLevel ||
               (int.Parse(x.Org_Level) >= targetLevel && string.IsNullOrEmpty(x.Upper_Department)) ||
               (int.Parse(x.Org_Level) > targetLevel && !string.IsNullOrEmpty(x.Upper_Department) && lowerLevelDepartments.Contains(x.Upper_Department))))
          .ToList();
    }

    private static string GetDepartmentByLevel(string empDepartment, List<HRMS_Org_Department> departmentList, List<HRMS_Org_Department> groupByLevel)
    {
      var empDept = departmentList.FirstOrDefault(d => d.Department_Code == empDepartment);
      if (empDept == null) return null;

      var orgDept = groupByLevel.FirstOrDefault(od =>
          od.Department_Code == empDepartment);
      if (orgDept != null)
        return orgDept.Department_Code;

      var currentDept = empDept;
      while (!string.IsNullOrEmpty(currentDept.Upper_Department))
      {
        var parentDept = groupByLevel.FirstOrDefault(od => od.Department_Code == currentDept.Upper_Department);
        if (parentDept != null)
        {
          return parentDept.Department_Code;
        }
        currentDept = departmentList.FirstOrDefault(d => d.Department_Code == currentDept.Upper_Department);
        if (currentDept == null) break;
      }

      return null;
    }
    #endregion

    #region Get Total
    public async Task<int> GetTotal(MonthlySalarySummaryReportParam param)
    {
      var data = await GetData(param, true);

      if (data.Data == null)
        return 0;

      var departmentList = (IEnumerable<dynamic>)data.Data;
      return departmentList.Count();
    }
    #endregion

    #region Download Excel
    public async Task<OperationResult> DownloadExcel(MonthlySalarySummaryReportParam param, string userName)
    {
      var result = await GetData(param, false);
      if (result.Data == null)
        return new OperationResult(isSuccess: false, "No data for excel download");
      var data = (List<MonthlySalarySummaryReportDto>)result.Data;

      var listFactory = await GetHRMS_Basic_Code(BasicCodeTypeConstant.Factory, param.Lang);
      var listPermissionGroup = await GetListPermissionGroup(param.Factory, param.Lang);
      var listLevel = await GetHRMS_Basic_Code(BasicCodeTypeConstant.Level, param.Lang);
      var listDepartment = await GetListDepartment(param.Factory, param.Lang);
      var listSalary = await GetHRMS_Basic_Code(BasicCodeTypeConstant.SalaryItem, param.Lang);
      var listInsurance = await GetHRMS_Basic_Code(BasicCodeTypeConstant.InsuranceType, param.Lang);

      var factory = listFactory.Where(x => x.Key == param.Factory).Select(x => x.Value).FirstOrDefault();

      var updatedPermissionGroup = new List<string>();
      foreach (var item in param.Permission_Group)
      {
        var updatedItem = listPermissionGroup.FirstOrDefault(x => x.Key == item).Value;
        updatedPermissionGroup.Add(updatedItem);
      }

      var level = listLevel.Where(x => x.Key == param.Level).Select(x => x.Value).FirstOrDefault();
      var department = listDepartment.Where(x => x.Key == param.Department).Select(x => x.Value).FirstOrDefault();

      var salaryItem = data.SelectMany(x => x.Salary_Item.Select(y => y.Key)).Distinct().ToList();
      var salary_Name = listSalary.Where(x => salaryItem.Contains(x.Key)).ToList();

      var insuranceDeductionItem = data.SelectMany(x => x.Insurance_Deduction.Select(y => y.Key)).Distinct().ToList();
      var insuranceDeduction_Name = listInsurance.Where(x => insuranceDeductionItem.Contains(x.Key)).ToList();

      List<Cell> cells = new()
      {
        new Cell("A1", param.Lang == "en" ? "7.2.4 Monthly Salary Summary Report" : "7.2.4 月份薪資彙總表"),
        new Cell("A2",param.Lang == "en" ? "Factory" : "廠別"),
        new Cell("C2",param.Lang == "en" ? "Year-Month" : "薪資年月"),
        new Cell("E2",param.Lang == "en" ? "Kind" : "類別"),
        new Cell("E4",param.Lang == "en" ? "Transfer" : "轉帳"),
        new Cell("G4",param.Lang == "en" ? "Permission Group" : "權限身分別"),
        new Cell("G2",param.Lang == "en" ? "Report Kind" : "報表類別"),
        new Cell("I2",param.Lang == "en" ? "Level" : "組織層級"),
        new Cell("K2",param.Lang == "en" ? "Department" : "部門"),
        new Cell("A4",param.Lang == "en" ? "Print By" : "列印人員"),
        new Cell("C4",param.Lang == "en" ? "Print Date" : "列印日期"),
        new Cell("A6",param.Lang == "en" ? "Department" : "部門"),
        new Cell("B6",param.Lang == "en" ? "Department Name" : "部門名稱"),
        new Cell("C6",param.Lang == "en" ? "Department Headcount" : "部門人數"),
        new Cell("B2",factory),
        new Cell("D2", Convert.ToDateTime(param.Year_Month_Str).ToString("yyyy/MM")),
        new Cell("F2",param.Kind = param.Kind == "Y"
                                              ? param.Lang == "en" ? "On job" : "在職"
                                              : param.Lang == "en" ? "Resigned" : "離職"),
        new Cell("F4",param.Transfer == "Y" ? "Yes"
                                            : param.Transfer == "N" ? "No" : "All"),
        new Cell("H4",string.Join(",\n", updatedPermissionGroup)),
        new Cell("H2",param.Report_Kind = param.Report_Kind == "G"
                                              ? param.Lang == "en" ? "Group by Level" : "層級群組"
                                              : param.Lang == "en" ? "Department Detail" : "部門明細"),
        new Cell("J2",level),
        new Cell("L2",department),
        new Cell("B4",userName),
        new Cell("D4",DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))
      };

      Aspose.Cells.Style borderStyle = new Aspose.Cells.CellsFactory().CreateStyle();
      borderStyle = AsposeUtility.SetAllBorders(borderStyle);

      Aspose.Cells.Style amountStyle = new Aspose.Cells.CellsFactory().CreateStyle();
      amountStyle.Number = 3;

      Aspose.Cells.Style labelStyle = new Aspose.Cells.CellsFactory().CreateStyle();
      labelStyle.Font.Color = Color.FromArgb(0, 0, 255);
      labelStyle.Font.IsBold = true;

      int currentColumn = 3;

      //Salary Item label
      if (salary_Name.Any())
      {
        cells.Add(new Cell(4, currentColumn, "Salary Item", labelStyle));
        foreach (var salary in salary_Name)
        {
          cells.Add(new Cell(5, currentColumn, salary.Value, borderStyle));
          currentColumn++;
        }
      }

      //Overtime Allowance label
      cells.Add(new Cell(5, currentColumn++, param.Lang == "en" ? "Overtime Allowance" : "加班費", borderStyle));

      //Night Shift Allowance label
      cells.Add(new Cell(5, currentColumn++, param.Lang == "en" ? "Night Shift Allowance" : "夜班津貼", borderStyle));

      //Other Additions label
      cells.Add(new Cell(5, currentColumn++, param.Lang == "en" ? "Addition Item" : "其他加項", borderStyle));

      //Total Additions Item label
      cells.Add(new Cell(5, currentColumn++, param.Lang == "en" ? "Total Addition Item " : "正項合計", borderStyle));

      //Insurance Deduction label
      if (insuranceDeduction_Name.Any())
      {
        cells.Add(new Cell(4, currentColumn, "Insurance Deduction", labelStyle));
        foreach (var insuranceDeduction in insuranceDeduction_Name)
        {
          cells.Add(new Cell(5, currentColumn, insuranceDeduction.Value, borderStyle));
          currentColumn++;
        }
      }

      //Tax label
      cells.Add(new Cell(5, currentColumn++, param.Lang == "en" ? "Tax" : "所得稅", borderStyle));

      //Other Deduction label
      cells.Add(new Cell(5, currentColumn++, param.Lang == "en" ? "Other Deduction" : "其他扣項", borderStyle));

      //Total Deduction Item label
      cells.Add(new Cell(5, currentColumn++, param.Lang == "en" ? "Total Deduction Item " : "負項合計", borderStyle));

      //Net Amount Received label
      cells.Add(new Cell(5, currentColumn++, param.Lang == "en" ? "Net Amount Received" : "實領金額", borderStyle));

      for (int i = 0; i < data.Count; i++)
      {
        int colIndex = 3;
        var rowData = data[i];
        foreach (var salary in salary_Name)
        {
          var salaryItems = rowData.Salary_Item.FirstOrDefault(x => x.Key == salary.Key);
          SetCell(i + 6, colIndex++, cells, salaryItems.Value, amountStyle);
        }
        SetCell(i + 6, colIndex++, cells, rowData.Overtime_Allowance, amountStyle);
        SetCell(i + 6, colIndex++, cells, rowData.Night_Shift_Allowance, amountStyle);
        SetCell(i + 6, colIndex++, cells, rowData.Other_Additions, amountStyle);
        SetCell(i + 6, colIndex++, cells, rowData.Total_Addition_Item, amountStyle);
        foreach (var insuranceDeduction in insuranceDeduction_Name)
        {
          var insuranceDeductionItems = rowData.Insurance_Deduction.FirstOrDefault(x => x.Key == insuranceDeduction.Key);
          SetCell(i + 6, colIndex++, cells, insuranceDeductionItems.Value, amountStyle);
        }
        SetCell(i + 6, colIndex++, cells, rowData.Tax, amountStyle);
        SetCell(i + 6, colIndex++, cells, rowData.Other_Deductions, amountStyle);
        SetCell(i + 6, colIndex++, cells, rowData.Total_Deduction_Item, amountStyle);
        SetCell(i + 6, colIndex++, cells, rowData.Net_Amount_Received, amountStyle);
      }
      List<Table> tables = new() { new("result", data) };
      ConfigDownload configDownload = new(true);
      ExcelResult excelResult = ExcelUtility.DownloadExcel(
          tables,
          cells,
          "Resources\\Template\\SalaryReport\\7_2_4_MonthlySalarySummaryReport\\Download.xlsx",
          configDownload
      );

      if (excelResult.IsSuccess)
      {
        var downloadResult = new
        {
          fileData = excelResult.Result,
          totalCount = data.Count
        };
        return new OperationResult(true, downloadResult);
      }
      else
        return new OperationResult(false, excelResult.Error);
    }
    private static void SetCell(int rowIndex, int colIndex, List<Cell> cells, decimal value, Aspose.Cells.Style style)
    {
      decimal totalValue = value;
      var tempCell = new Cell(rowIndex, colIndex, value, style);
      var recentCell = cells.FirstOrDefault(x => x.Location == tempCell.Location);
      if (recentCell == null)
        cells.Add(tempCell);
      else
      {
        totalValue += (decimal)recentCell.Value;
        recentCell.Value = tempCell.Value;
      }
      cells.Add(new Cell(rowIndex + 1, colIndex, totalValue, style));
    }
    #endregion
  }
}