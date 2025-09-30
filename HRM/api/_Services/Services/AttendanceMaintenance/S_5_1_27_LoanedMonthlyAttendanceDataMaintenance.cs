using System.Collections;
using System.Diagnostics.SymbolStore;
using System.Drawing;
using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Helper.Utilities;
using API.Models;
using Aspose.Cells;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance
{
  public class S_5_1_27_LoanedMonthlyAttendanceDataMaintenance : BaseServices, I_5_1_27_LoanedMonthlyAttendanceDataMaintenance
  {
    public S_5_1_27_LoanedMonthlyAttendanceDataMaintenance(DBContext dbContext) : base(dbContext) { }

    #region GetList
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
    #endregion

    #region GetEmployeeData
    public async Task<OperationResult> GetEmployeeData(string factory, string att_Month, string employeeID, string language)
    {
      var attMonthDate = Convert.ToDateTime(att_Month);
      if (!employeeID.StartsWith(factory + "-"))
        employeeID = $"{factory}-{employeeID}";
      if (string.IsNullOrWhiteSpace(employeeID))
        return new OperationResult(false, "System.Message.NoData");

      var permissionGroupPred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup);
      var permissionGroupCodes = await GetBasicCodes(language, permissionGroupPred);

      var salaryTypePred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.SalaryType);
      var salaryTypeCodes = await GetBasicCodes(language, salaryTypePred);

      var useR_GUID = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == factory && x.Employee_ID == employeeID)
                      .Select(x => x.USER_GUID).FirstOrDefaultAsync();
      var personal = await _repositoryAccessor.HRMS_Emp_Personal
          .FindAll(x => x.USER_GUID == useR_GUID, true)
          .Join(
              _repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(x => x.Kind == "1", true),
              x => new { x.Division, x.Factory },
              y => new { y.Division, y.Factory },
              (personal, basic) => new { Personal = personal, Basic = basic }
          ).FirstOrDefaultAsync();

      if (personal == null)
        return new OperationResult(false, $"No Data in HRMS_Emp_Personal");

      var param = new LoanedMonthlyAttendanceDataMaintenanceDto
      {
        Factory = factory,
        Employee_ID = employeeID,
        Att_Month = attMonthDate.ToString("yyyy/MM"),
        Lang = language
      };

      var dataDetail = await GetDataDetail(param);

      var department = await GetAttDepartment(personal.Personal.USER_GUID, language);

      var result = new EmployeeInfo
      {
        USER_GUID = useR_GUID,
        Division = personal.Personal.Division,
        Local_Full_Name = personal.Personal.Local_Full_Name,
        Permission_Group = permissionGroupCodes.FirstOrDefault(y => y.Key == personal.Personal.Permission_Group).Value ?? string.Empty,
        Salary_Type = salaryTypeCodes.FirstOrDefault(y => y.Key == "10").Value,
        Department = department.Value,
        Leaves = dataDetail.Leaves,
        Allowances = dataDetail.Allowances
      };

      return new OperationResult(true, result);
    }

    private async Task<KeyValuePair<string, string>> GetAttDepartment(string userGUID, string language)
    {
      var personal = await _repositoryAccessor.HRMS_Emp_Personal.FirstOrDefaultAsync(x => x.USER_GUID == userGUID, true);
      var departmentQuery = _repositoryAccessor.HRMS_Org_Department.FindAll(true);
      var departmentLanguageQuery = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true);

      if (personal.Employment_Status == "A" || personal.Employment_Status == "S")
        departmentQuery = _repositoryAccessor.HRMS_Org_Department
                    .FindAll(x => x.Division == personal.Assigned_Division
                                  && x.Factory == personal.Assigned_Factory
                                  && x.Department_Code == personal.Assigned_Department, true);
      else
        departmentQuery = _repositoryAccessor.HRMS_Org_Department
                    .FindAll(x => x.Division == personal.Division
                                  && x.Factory == personal.Factory
                                  && x.Department_Code == personal.Department, true);

      var result = await departmentQuery
          .GroupJoin(
              departmentLanguageQuery,
              department => new { department.Division, department.Factory, department.Department_Code },
              language => new { language.Division, language.Factory, language.Department_Code },
              (department, languages) => new { Department = department, Languages = languages }
          )
          .SelectMany(
              x => x.Languages.DefaultIfEmpty(),
              (x, language) => new { x.Department, Language = language }
          )
          .OrderBy(x => x.Department.Department_Code)
          .Select(
              x => new KeyValuePair<string, string>(
                  x.Department.Department_Code,
                  $"{x.Department.Department_Code} - {(x.Language != null ? x.Language.Name : x.Department.Department_Name)}"
              )
          )
          .FirstOrDefaultAsync();

      return result;
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

    public async Task<List<string>> GetEmployeeID(string factory)
    {
      return await _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == factory && x.Employee_ID.Length <= 9, true).Select(x => x.Employee_ID).ToListAsync();
    }
    #endregion

    #region GetDetail
    public async Task<Detail> GetDataDetail(LoanedMonthlyAttendanceDataMaintenanceDto param)
    {
      var leaveCodes = await QueryCodeDetail(param, "1", BasicCodeTypeConstant.Leave);

      var predicateMonthly = PredicateBuilder
          .New<HRMS_Att_Loaned_Monthly_Detail>(
              x => x.Factory == param.Factory &&
              x.Employee_ID == param.Employee_ID &&
              x.Att_Month == Convert.ToDateTime(param.Att_Month).Date);

      var predicateYearly = PredicateBuilder
          .New<HRMS_Att_Yearly>(
              x => x.Factory == param.Factory &&
              x.Employee_ID == param.Employee_ID &&
              x.Att_Year == new DateTime(Convert.ToDateTime(param.Att_Month).Year, 1, 1));

      List<DetailDisplay> leaves = new();
      if (leaveCodes.Any())
      {
        var predicateMTemp = PredicateBuilder.New<HRMS_Att_Loaned_Monthly_Detail>(predicateMonthly);
        predicateMTemp.And(x => x.Leave_Type == "1");

        var predicateYTemp = PredicateBuilder.New<HRMS_Att_Yearly>(predicateYearly);
        predicateYTemp.And(x => x.Leave_Type == "1");

        leaves = leaveCodes
            .GroupJoin(_repositoryAccessor.HRMS_Att_Loaned_Monthly_Detail.FindAll(predicateMTemp, true),
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
            .Select(x => new DetailDisplay()
            {
              Code = x.code.Key,
              CodeName = x.code.Value,
              Days = x.leave is null ? 0 : x.leave.Days,
              Total = x.year is null ? 0 : x.year.Days
            }).ToList();
      }

      var allowanceCodes = await QueryCodeDetail(param, "2", BasicCodeTypeConstant.Allowance);
      List<DetailDisplay> allowances = new();
      if (allowanceCodes.Any())
      {
        var predicateMTemp = PredicateBuilder.New<HRMS_Att_Loaned_Monthly_Detail>(predicateMonthly);
        predicateMTemp.And(x => x.Leave_Type == "2");

        var predicateYTemp = PredicateBuilder.New<HRMS_Att_Yearly>(predicateYearly);
        predicateYTemp.And(x => x.Leave_Type == "2");

        allowances = allowanceCodes
            .GroupJoin(_repositoryAccessor.HRMS_Att_Loaned_Monthly_Detail.FindAll(predicateMTemp, true),
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
            .Select(x => new DetailDisplay()
            {
              Code = x.code.Key,
              CodeName = x.code.Value,
              Days = x.leave is null ? 0 : x.leave.Days,
              Total = x.year is null ? 0 : x.year.Days
            }).ToList();
      }

      return new Detail
      {
        Leaves = leaves,
        Allowances = allowances
      };
    }

    private async Task<List<KeyValuePair<string, string>>> QueryCodeDetail(LoanedMonthlyAttendanceDataMaintenanceDto param, string leaveType, string typeSeq)
    {
      var monthlyLeaves = _repositoryAccessor.HRMS_Att_Use_Monthly_Leave
          .FindAll(x => x.Factory == param.Factory && x.Leave_Type == leaveType && x.Effective_Month.Date <= Convert.ToDateTime(param.Att_Month).Date);

      if (!await monthlyLeaves.AnyAsync())
        return new List<KeyValuePair<string, string>>();

      var maxLeaveEffectiveMonth = await monthlyLeaves.MaxAsync(x => x.Effective_Month);

      var codes = await _repositoryAccessor.HRMS_Att_Use_Monthly_Leave.FindAll(x => x.Factory == param.Factory
                                  && x.Leave_Type == leaveType
                                  && x.Effective_Month == maxLeaveEffectiveMonth, true)
                  .Join(_repositoryAccessor.HRMS_Att_Loaned_Monthly_Detail.FindAll(true),
                      x => new { x.Factory, x.Leave_Type, x.Code },
                      y => new { y.Factory, y.Leave_Type, Code = y.Leave_Code },
                      (x, y) => new { Use = x, Loaned = y }
                  )
                 .OrderBy(x => x.Use.Seq)
              .Select(x => x.Loaned.Leave_Code).Distinct().ToListAsync();

      var predicate = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == typeSeq && codes.Contains(x.Code));
      return await GetBasicCodes(param.Lang, predicate);
    }

    private async Task<List<KeyValuePair<string, string>>> GetBasicCodes(string language, ExpressionStarter<HRMS_Basic_Code> predicate)
    {
      var data = await _repositoryAccessor.HRMS_Basic_Code.FindAll(predicate, true)
          .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
              x => new { x.Type_Seq, x.Code },
              y => new { y.Type_Seq, y.Code },
              (x, y) => new { code = x, codeLang = y })
          .SelectMany(
              x => x.codeLang.DefaultIfEmpty(),
              (x, y) => new { x.code, codeLang = y })
          .Select(x => new KeyValuePair<string, string>(x.code.Code, $"{x.code.Code} - {x.codeLang.Code_Name ?? x.code.Code_Name}"))
          .Distinct().ToListAsync();

      return data;
    }
    #endregion

    #region GetData
    private async Task<List<LoanedMonthlyAttendanceDataMaintenanceDto>> GetData(LoanedMonthlyAttendanceDataMaintenanceParam param)
    {
      var (predMonthly, predEmp) = SetPredicate(param);

      var personalQuery = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predEmp);
      var departmentQuery = _repositoryAccessor.HRMS_Org_Department.FindAll(true);
      var depLanguageQuery = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower(), true);
      var permissionGroupPred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup);
      var permissionGroupCodes = await GetBasicCodes(param.Lang, permissionGroupPred);
      var salaryTypePred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.SalaryType);
      var salaryTypeCodes = await GetBasicCodes(param.Lang, salaryTypePred);

      var permissionGroup = await GetListPermissionGroup(param.Lang);
      var salaryType = await GetListSalaryType(param.Lang);

      var data = await _repositoryAccessor.HRMS_Att_Loaned_Monthly.FindAll(predMonthly).ToListAsync();
      List<string> filter = new() { "A", "S" };

      var result = data
          .Join(personalQuery,
              loaned => new { loaned.USER_GUID },
              personal => new { personal.USER_GUID },
              (loaned, personal) => new { Loaned = loaned, Personal = personal })
          .GroupJoin(departmentQuery,
              x => new
              {
                Department = filter.Contains(x.Personal?.Employment_Status) ? x.Personal?.Assigned_Department : x.Personal?.Department,
                Factory = filter.Contains(x.Personal?.Employment_Status) ? x.Personal?.Assigned_Factory : x.Personal?.Factory
              },
              department => new { Department = department.Department_Code, department.Factory },
              (x, department) => new { x.Loaned, x.Personal, Department = department })
          .SelectMany(
              x => x.Department.DefaultIfEmpty(),
              (x, department) => new { x.Loaned, x.Personal, Department = department })
          .GroupJoin(depLanguageQuery,
              x => new { x.Department?.Department_Code, x.Department?.Factory },
              lang => new { lang.Department_Code, lang.Factory },
              (x, lang) => new { x.Loaned, x.Personal, x.Department, LanguageDepartment = lang })
          .SelectMany(
              x => x.LanguageDepartment.DefaultIfEmpty(),
              (x, lang) => new { x.Loaned, x.Personal, x.Department, LanguageDepartment = lang })
          .Select(x => new LoanedMonthlyAttendanceDataMaintenanceDto
          {
            USER_GUID = x.Loaned.USER_GUID,
            Division = x.Personal.Division,
            Pass = x.Loaned.Pass,
            Pass_Str = x.Loaned.Pass == true ? "Y" : "N",
            Factory = x.Loaned.Factory,
            Att_Month = x.Loaned.Att_Month.ToString("yyyy/MM"),
            Department = CheckValue(x.Department?.Department_Code, (x.LanguageDepartment?.Name ?? x.Department?.Department_Name) ?? string.Empty),
            Department_Code = x.Department != null ? x.Department?.Department_Code : string.Empty,
            Department_Name = x.Department != null ? x.LanguageDepartment?.Name ?? x.Department?.Department_Name ?? string.Empty : string.Empty,
            Employee_ID = x.Loaned.Employee_ID,
            Local_Full_Name = x.Personal?.Local_Full_Name,
            Resign_Status = x.Loaned.Resign_Status,
            Delay_Early = x.Loaned.Delay_Early,
            No_Swip_Card = x.Loaned.No_Swip_Card,
            Food_Expenses = x.Loaned.Food_Expenses,
            Night_Eat_Times = x.Loaned.Night_Eat_Times,
            Permission_Group = permissionGroup.FirstOrDefault(y => y.Key == x.Loaned.Permission_Group).Value ?? x.Loaned.Permission_Group,
            Salary_Type = salaryType.FirstOrDefault(y => y.Key == x.Loaned.Salary_Type).Value ?? x.Loaned.Salary_Type,
            Salary_Days = x.Loaned.Salary_Days,
            Actual_Days = x.Loaned.Actual_Days,
            Update_By = x.Loaned.Update_By,
            Update_Time = x.Loaned.Update_Time.ToString("yyyy/MM/dd HH:mm:ss")
          })
          .OrderBy(x => x.Department)
          .ToList();

      return result;
    }

    public async Task<PaginationUtility<LoanedMonthlyAttendanceDataMaintenanceDto>> GetDataPagination(PaginationParam pagination, LoanedMonthlyAttendanceDataMaintenanceParam param)
    {
      var data = await GetData(param);
      return PaginationUtility<LoanedMonthlyAttendanceDataMaintenanceDto>.Create(data, pagination.PageNumber, pagination.PageSize);
    }
    #endregion

    #region DownloadExcel
    public async Task<OperationResult> DownloadExcel(LoanedMonthlyAttendanceDataMaintenanceParam param, string userName)
    {
      LoanedMonthlyAttendanceDataMaintenanceDto queryCodeParam = new()
      {
        Factory = param.Factory,
        Att_Month = param.Att_Month_To,
        Lang = param.Lang
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
          "Resources\\Template\\AttendanceMaintenance\\5_1_27_LoanedMonthlyAttendanceDataMaintenance\\Download.xlsx"
        );
        WorkbookDesigner designer = new() { Workbook = new Workbook(path) };

        Worksheet ws = designer.Workbook.Worksheets[0];
        ws.Cells["A1"].PutValue(param.Lang == "en" ? "Factory" : "廠別");
        ws.Cells["C1"].PutValue(param.Lang == "en" ? "Year-Month of Attendance" : "出勤年月");
        ws.Cells["F1"].PutValue(param.Lang == "en" ? "Print By" : "列印人員");
        ws.Cells["H1"].PutValue(param.Lang == "en" ? "Print Date" : "列印日期");
        ws.Cells["A3"].PutValue(param.Lang == "en" ? "Department" : "部門");
        ws.Cells["B3"].PutValue(param.Lang == "en" ? "Department Name" : "部門名稱");
        ws.Cells["C3"].PutValue(param.Lang == "en" ? "Employee ID" : "工號");
        ws.Cells["D3"].PutValue(param.Lang == "en" ? "Local Full Name" : "本地姓名");
        ws.Cells["E3"].PutValue(param.Lang == "en" ? "New-Hired / Resigned" : "新進/離職");
        ws.Cells["F3"].PutValue(param.Lang == "en" ? "Permission Group" : "權限身分別");
        ws.Cells["G3"].PutValue(param.Lang == "en" ? "Salary Type" : "薪資計別");
        ws.Cells["H3"].PutValue(param.Lang == "en" ? "Paid Salary Days" : "計薪天數");
        ws.Cells["I3"].PutValue(param.Lang == "en" ? "Actual Work Days" : "實際上班天數");
        ws.Cells["J3"].PutValue(param.Lang == "en" ? "Delay/Early(times)" : "遲到/早退(次)");
        ws.Cells["K3"].PutValue(param.Lang == "en" ? "No swip card(times)" : "未刷卡(次)");
        ws.Cells["L3"].PutValue(param.Lang == "en" ? "Night Shift Allowance Times" : "夜點費次數");
        ws.Cells["M3"].PutValue(param.Lang == "en" ? "Meal Fee Times" : "伙食費次數");
        ws.Cells["B1"].PutValue(param.Factory);
        ws.Cells["D1"].PutValue($"{param.Att_Month_From.Substring(0, 7)}~{param.Att_Month_To.Substring(0, 7)}");
        ws.Cells["G1"].PutValue(userName);
        ws.Cells["I1"].PutValue(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));

        designer.SetDataSource("result", data);
        designer.Process();

        int range = 0;
        Style style = ws.Cells["A3"].GetStyle();
        if (leaves.Any())
        {
          Aspose.Cells.Range leaveRange = ws.Cells.CreateRange(2, 13, 1, leaves.Count);
          style.ForegroundColor = Color.FromArgb(255, 242, 204);
          leaveRange.SetStyle(style);
          leaveRange.ColumnWidth = 20;

          ArrayList leaveCodes = new();
          leaves.ForEach(item => { leaveCodes.Add(item.Value); });
          ws.Cells.ImportArrayList(leaveCodes, 2, 13, false);

          range += leaves.Count;
        }

        if (allowances.Any())
        {
          Aspose.Cells.Range allowanceRange = ws.Cells.CreateRange(2, 13 + leaves.Count, 1, allowances.Count);
          style.ForegroundColor = Color.FromArgb(226, 239, 218);
          allowanceRange.SetStyle(style);
          allowanceRange.ColumnWidth = 20;

          ArrayList allowanceCodes = new();
          allowances.ForEach(item => { allowanceCodes.Add(item.Value); });
          ws.Cells.ImportArrayList(allowanceCodes, 2, 13 + leaves.Count, false);

          range += allowances.Count;
        }

        if (range > 0)
        {
          Style styleRange = designer.Workbook.CreateStyle();
          styleRange = AsposeUtility.SetAllBorders(styleRange);

          Aspose.Cells.Range allowanceRange = ws.Cells.CreateRange(3, 13, data.Count, range);
          allowanceRange.SetStyle(styleRange);
        }

        for (int i = 0; i < data.Count; i++)
        {
          if (data[i].Leave_Days.Any())
          {
            ArrayList leaveCodeDetail = new();
            data[i].Leave_Days.ForEach(item => { leaveCodeDetail.Add(item); });
            ws.Cells.ImportArrayList(leaveCodeDetail, i + 3, 13, false);
          }

          if (data[i].Allowance_Days.Any())
          {
            ArrayList allowanceDetail = new();
            data[i].Allowance_Days.ForEach(item => { allowanceDetail.Add(item); });
            ws.Cells.ImportArrayList(allowanceDetail, i + 3, 13 + data[i].Leave_Days.Count, false);
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

    private async Task<List<LoanedMonthlyAttendanceDataMaintenanceDto>> GetDataDownload(LoanedMonthlyAttendanceDataMaintenanceParam param, List<KeyValuePair<string, string>> leaves, List<KeyValuePair<string, string>> allowances)
    {
      var (predMonthly, predEmp) = SetPredicate(param);
      var permissionGroup = await GetListPermissionGroup(param.Lang);
      var salaryType = await GetListSalaryType(param.Lang);
      var department = await GetDepartmentMain(param.Lang);
      var HALM = _repositoryAccessor.HRMS_Att_Loaned_Monthly.FindAll(predMonthly, true).ToList();
      var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predEmp, true).ToList();
      var HALMD = _repositoryAccessor.HRMS_Att_Loaned_Monthly_Detail.FindAll().ToList();
      List<LoanedMonthlyAttendanceDataMaintenanceDto> data = new();
      HALM
      .Join(HEP,
        x => new { x.USER_GUID },
        y => new { y.USER_GUID },
        (x, y) => new { data = new { HALM = x, HEP = y } })
      .GroupJoin(HALMD,
        x => new { x.data.HALM.Factory, x.data.HALM.Att_Month.Date, x.data.HALM.Employee_ID },
        y => new { y.Factory, y.Att_Month.Date, y.Employee_ID },
        (x, y) => new { x.data, HALMD = y })
      .SelectMany(
        x => x.HALMD.DefaultIfEmpty(),
        (x, y) => new { x.data, HALMD = y })
      .GroupBy(x => x.data)
      .ForEach(x =>
        {
          var department_temp = string.IsNullOrWhiteSpace(x.Key.HEP.Employment_Status)
                        ? department.FirstOrDefault(
                            d => d.Division == x.Key.HEP.Division &&
                            d.Factory == x.Key.HEP.Factory &&
                            d.Department_Code == x.Key.HEP.Department)?.Department_Name
                        : x.Key.HEP.Employment_Status == "A" || x.Key.HEP.Employment_Status == "S"
                        ? department.FirstOrDefault(
                            d => d.Division == x.Key.HEP.Assigned_Division &&
                            d.Factory == x.Key.HEP.Assigned_Factory &&
                            d.Department_Code == x.Key.HEP.Assigned_Department)?.Department_Name
                        : "";
          var value = new LoanedMonthlyAttendanceDataMaintenanceDto
          {
            Division = x.Key.HALM.Division,
            Factory = x.Key.HALM.Factory,
            Att_Month = x.Key.HALM.Att_Month.ToString("yyyy/MM"),
            Department = department_temp != null ? department_temp.Split(" - ")[0] : string.Empty,
            Department_Name = department_temp != null ? department_temp.Split(" - ")[1] : string.Empty,
            Employee_ID = x.Key.HALM.Employee_ID,
            Local_Full_Name = x.Key.HEP.Local_Full_Name,
            Pass = x.Key.HALM.Pass,
            Resign_Status = x.Key.HALM.Resign_Status,
            Permission_Group = permissionGroup.FirstOrDefault(y => y.Key == x.Key.HALM.Permission_Group).Value ?? x.Key.HALM.Permission_Group,
            Salary_Type = salaryType.FirstOrDefault(y => y.Key == x.Key.HALM.Salary_Type).Value ?? x.Key.HALM.Salary_Type,
            Salary_Days = x.Key.HALM.Salary_Days,
            Actual_Days = x.Key.HALM.Actual_Days,
            Delay_Early = x.Key.HALM.Delay_Early,
            No_Swip_Card = x.Key.HALM.No_Swip_Card,
            Night_Eat_Times = x.Key.HALM.Night_Eat_Times,
            Food_Expenses = x.Key.HALM.Food_Expenses
          };
          foreach (var item in leaves)
          {
            var leave = x.FirstOrDefault(l => l.HALMD.Leave_Type == "1" && l.HALMD.Leave_Code.StartsWith(item.Key));
            value.Leave_Days.Add(leave?.HALMD.Days);
          }
          foreach (var item in allowances)
          {
            var allowance = x.FirstOrDefault(l => l.HALMD.Leave_Type == "2" && l.HALMD.Leave_Code.StartsWith(item.Key));
            value.Leave_Days.Add(allowance?.HALMD.Days);
          }
          data.Add(value);
        });
      return data.OrderBy(x => x.Department).ToList();
    }

    private static (ExpressionStarter<HRMS_Att_Loaned_Monthly> predMonthly, ExpressionStarter<HRMS_Emp_Personal> predEmp) SetPredicate(LoanedMonthlyAttendanceDataMaintenanceParam param)
    {
      var predMonthly = PredicateBuilder.New<HRMS_Att_Loaned_Monthly>(true);
      var predEmp = PredicateBuilder.New<HRMS_Emp_Personal>(true);

      if (!string.IsNullOrWhiteSpace(param.Factory))
      {
        predMonthly.And(x => x.Factory == param.Factory);
        predEmp.And(x => x.Factory == param.Factory);
      }

      if (!string.IsNullOrWhiteSpace(param.Att_Month_From) && !string.IsNullOrWhiteSpace(param.Att_Month_To))
        predMonthly.And(x => x.Att_Month >= DateTime.Parse(param.Att_Month_From)
                   && x.Att_Month <= DateTime.Parse(param.Att_Month_To));

      if (!string.IsNullOrWhiteSpace(param.Department))
        predEmp.And(x => x.Department == param.Department);

      if (!string.IsNullOrWhiteSpace(param.Employee_ID))
      {
        predMonthly.And(x => x.Employee_ID.ToLower().Contains(param.Employee_ID.Trim().ToLower()));
        predEmp.And(x => x.Employee_ID.ToLower().Contains(param.Employee_ID.Trim().ToLower()));
      }

      if (!string.IsNullOrWhiteSpace(param.Salary_Days))
        predMonthly.And(x => x.Salary_Days == Convert.ToDecimal(param.Salary_Days));

      return (predMonthly, predEmp);
    }

    public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string language)
    {
      var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup);
      var data = await GetBasicCodes(language, pred);
      return data;
    }

    public async Task<List<KeyValuePair<string, string>>> GetListSalaryType(string language)
    {
      var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.SalaryType);
      var data = await GetBasicCodes(language, pred);
      return data;
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
    #endregion

    #region Add
    public async Task<OperationResult> AddNew(LoanedMonthlyAttendanceDataMaintenanceDto data, string userName)
    {
      var passCount = await _repositoryAccessor.HRMS_Att_Loaned_Monthly
                      .CountAsync(x => x.Factory == data.Factory && x.Att_Month == data.Att_Month.ToDateTime().Date && x.Pass == true);

      if (passCount >= 1)
        return new OperationResult(false, "Can't save data");

      if (await _repositoryAccessor.HRMS_Att_Loaned_Monthly.AnyAsync(
              x => x.Att_Month.Date == data.Att_Month.ToDateTime().Date &&
              x.Factory == data.Factory &&
              x.Employee_ID == data.Employee_ID))
      {
        string message = $"Year-Month of Attendance: {data.Att_Month}, Employee ID: {data.Employee_ID} exsited!";
        return new OperationResult { IsSuccess = false, Error = message };
      }

      var att_Loaned_Monthly_Detail = await _repositoryAccessor.HRMS_Att_Loaned_Monthly_Detail.FindAll().ToListAsync();
      await _repositoryAccessor.BeginTransactionAsync();
      try
      {
        DateTime now = DateTime.Now;
        HRMS_Att_Loaned_Monthly dataNew = new()
        {
          USER_GUID = data.USER_GUID,
          Division = data.Division,
          Factory = data.Factory,
          Att_Month = data.Att_Month.ToDateTime().Date,
          Pass = data.Pass,
          Employee_ID = data.Employee_ID,
          Department = data.Department?.Split(" - ")[0],
          Resign_Status = data.Resign_Status,
          Salary_Days = data.Salary_Days,
          Actual_Days = data.Actual_Days,
          Permission_Group = data.Permission_Group?.Split(" - ")[0],
          Salary_Type = data.Salary_Type?.Split(" - ")[0],
          Delay_Early = data.Delay_Early,
          Food_Expenses = data.Food_Expenses,
          Night_Eat_Times = data.Night_Eat_Times,
          No_Swip_Card = data.No_Swip_Card,
          Update_By = userName,
          Update_Time = now
        };
        _repositoryAccessor.HRMS_Att_Loaned_Monthly.Add(dataNew);

        List<HRMS_Att_Loaned_Monthly_Detail> details = new();
        List<HRMS_Att_Yearly> totals = new();
        if (data.Leaves is not null && data.Leaves.Any())
        {
          data.Leaves.ForEach(item =>
          {
            details.Add(new()
            {
              Factory = dataNew.Factory,
              Employee_ID = dataNew.Employee_ID,
              USER_GUID = dataNew.USER_GUID,
              Division = dataNew.Division,
              Att_Month = dataNew.Att_Month,
              Leave_Type = "1",
              Leave_Code = item.Code,
              Days = item.Days,
              Update_By = userName,
              Update_Time = now
            });
          });

          totals.AddRange(
              await Upd_HRMS_Att_Yearly(
                  new YearlyUpdate()
                  {
                    Factory = dataNew.Factory,
                    Employee_ID = dataNew.Employee_ID,
                    USER_GUID = dataNew.USER_GUID,
                    Att_Year = new DateTime(dataNew.Att_Month.Year, 1, 1),
                    Leave_Type = "1",
                    Account = userName,
                    Details = data.Leaves
                  }, att_Loaned_Monthly_Detail
              ));
        }

        if (data.Allowances is not null && data.Allowances.Any())
        {
          data.Allowances.ForEach(item =>
          {
            details.Add(new()
            {
              Factory = dataNew.Factory,
              Employee_ID = dataNew.Employee_ID,
              USER_GUID = dataNew.USER_GUID,
              Division = dataNew.Division,
              Att_Month = dataNew.Att_Month,
              Leave_Type = "2",
              Leave_Code = item.Code,
              Days = item.Days,
              Update_By = userName,
              Update_Time = now
            });
          });

          totals.AddRange(
              await Upd_HRMS_Att_Yearly(
                  new YearlyUpdate()
                  {
                    Factory = dataNew.Factory,
                    Employee_ID = dataNew.Employee_ID,
                    USER_GUID = dataNew.USER_GUID,
                    Att_Year = new DateTime(dataNew.Att_Month.Year, 1, 1),
                    Leave_Type = "2",
                    Account = userName,
                    Details = data.Allowances
                  }, att_Loaned_Monthly_Detail
              ));
        }

        if (details.Any())
          _repositoryAccessor.HRMS_Att_Loaned_Monthly_Detail.AddMultiple(details);

        if (totals.Any())
          _repositoryAccessor.HRMS_Att_Yearly.UpdateMultiple(totals);

        await _repositoryAccessor.Save();
        await _repositoryAccessor.CommitAsync();

        return new OperationResult { IsSuccess = true };
      }
      catch (Exception ex)
      {
        await _repositoryAccessor.RollbackAsync();
        return new OperationResult { IsSuccess = false, Error = ex.InnerException.Message };
      }
    }
    #endregion

    #region Edit
    public async Task<OperationResult> Edit(LoanedMonthlyAttendanceDataMaintenanceDto data, string userName)
    {
      var dataUpdate = await _repositoryAccessor.HRMS_Att_Loaned_Monthly.FirstOrDefaultAsync(
              x => x.Att_Month.Date == data.Att_Month.ToDateTime().Date &&
              x.Factory == data.Factory &&
              x.Employee_ID == data.Employee_ID, true);

      var att_Loaned_Monthly_Detail = _repositoryAccessor.HRMS_Att_Loaned_Monthly_Detail.FindAll(true).ToList();
      if (dataUpdate is null)
        return new OperationResult { IsSuccess = false, Error = "Data is not existed!" };

      var detailExists = await _repositoryAccessor.HRMS_Att_Loaned_Monthly_Detail
                        .FindAll(x => x.Factory == data.Factory &&
                                      x.Employee_ID == data.Employee_ID &&
                                      x.Att_Month == data.Att_Month.ToDateTime().Date, true).ToListAsync();

      await _repositoryAccessor.BeginTransactionAsync();
      try
      {
        dataUpdate.Salary_Days = data.Salary_Days;
        dataUpdate.Actual_Days = data.Actual_Days;
        dataUpdate.Resign_Status = data.Resign_Status;
        dataUpdate.Delay_Early = data.Delay_Early;
        dataUpdate.No_Swip_Card = data.No_Swip_Card;
        dataUpdate.Food_Expenses = data.Food_Expenses;
        dataUpdate.Night_Eat_Times = data.Night_Eat_Times;
        dataUpdate.Update_By = userName;
        dataUpdate.Update_Time = DateTime.Now;

        _repositoryAccessor.HRMS_Att_Loaned_Monthly.Update(dataUpdate);

        List<HRMS_Att_Loaned_Monthly_Detail> details = new();
        List<HRMS_Att_Yearly> totals = new();
        if (data.Leaves is not null && data.Leaves.Any())
        {
          var unmatchedLeaveCodes = data.Leaves
                .FindAll(x => !detailExists.Any(d => d.Leave_Code == x.Code))
                .Select(x => x.Code)
                .ToList();

          if (unmatchedLeaveCodes.Any())
          {
            string unmatchedCodes = string.Join(", ", unmatchedLeaveCodes);
            return new OperationResult
            {
              IsSuccess = false,
              Error = $"The following Leave_Code(s) do not match in HRMS_Att_Loaned_Monthly: {unmatchedCodes}"
            };
          }

          data.Leaves.ForEach(item =>
          {
            details.Add(new()
            {
              Factory = dataUpdate.Factory,
              Employee_ID = dataUpdate.Employee_ID,
              USER_GUID = dataUpdate.USER_GUID,
              Division = dataUpdate.Division,
              Att_Month = dataUpdate.Att_Month,
              Leave_Type = "1",
              Leave_Code = item.Code,
              Days = item.Days,
              Update_By = userName,
              Update_Time = dataUpdate.Update_Time
            });
          });

          totals.AddRange(await Upd_HRMS_Att_Yearly(new YearlyUpdate()
          {
            Factory = dataUpdate.Factory,
            Employee_ID = dataUpdate.Employee_ID,
            USER_GUID = dataUpdate.USER_GUID,
            Att_Year = dataUpdate.Att_Month,
            Leave_Type = "1",
            Account = userName,
            Details = data.Leaves
          }, att_Loaned_Monthly_Detail));
        }

        if (data.Allowances is not null && data.Allowances.Any())
        {
          var unmatchedLeaveCodes = data.Allowances
                .FindAll(x => !detailExists.Any(d => d.Leave_Code == x.Code))
                .Select(x => x.Code)
                .ToList();

          if (unmatchedLeaveCodes.Any())
          {
            string unmatchedCodes = string.Join(", ", unmatchedLeaveCodes);
            return new OperationResult
            {
              IsSuccess = false,
              Error = $"The following Leave_Code(s) do not match in the database: {unmatchedCodes}"
            };
          }

          data.Allowances.ForEach(item =>
          {
            details.Add(new()
            {
              Factory = dataUpdate.Factory,
              Employee_ID = dataUpdate.Employee_ID,
              USER_GUID = dataUpdate.USER_GUID,
              Division = dataUpdate.Division,
              Att_Month = dataUpdate.Att_Month,
              Leave_Type = "2",
              Leave_Code = item.Code,
              Days = item.Days,
              Update_By = userName,
              Update_Time = dataUpdate.Update_Time
            });
          });

          totals.AddRange(await Upd_HRMS_Att_Yearly(new YearlyUpdate()
          {
            Factory = dataUpdate.Factory,
            Employee_ID = dataUpdate.Employee_ID,
            USER_GUID = dataUpdate.USER_GUID,
            Att_Year = dataUpdate.Att_Month,
            Leave_Type = "2",
            Account = userName,
            Details = data.Allowances
          }, att_Loaned_Monthly_Detail));
        }

        if (details.Any())
          _repositoryAccessor.HRMS_Att_Loaned_Monthly_Detail.UpdateMultiple(details);

        if (totals.Any())
          _repositoryAccessor.HRMS_Att_Yearly.UpdateMultiple(totals);

        await _repositoryAccessor.Save();
        await _repositoryAccessor.CommitAsync();

        return new OperationResult { IsSuccess = true };
      }
      catch (Exception ex)
      {
        await _repositoryAccessor.RollbackAsync();
        return new OperationResult { IsSuccess = false, Error = ex.InnerException.Message };
      }
    }
    #endregion

    private async Task<List<HRMS_Att_Yearly>> Upd_HRMS_Att_Yearly(YearlyUpdate update, List<HRMS_Att_Loaned_Monthly_Detail> Loaned_Monthly_Detail)
    {
      var codes = update.Details.Select(x => x.Code).ToList();
      var att_Loaned_Monthly_Detail = Loaned_Monthly_Detail
                      .FindAll(x => x.Factory == update.Factory
                              && x.Att_Month == update.Att_Year
                              && x.Employee_ID == update.Employee_ID
                          && x.USER_GUID == update.USER_GUID
                          && x.Leave_Type == update.Leave_Type
                          && codes.Contains(x.Leave_Code));
      var data = await _repositoryAccessor.HRMS_Att_Yearly
          .FindAll(
              x => x.Factory == update.Factory &&
              x.Att_Year == new DateTime(update.Att_Year.Year, 1, 1) &&
              x.Employee_ID == update.Employee_ID &&
              x.USER_GUID == update.USER_GUID &&
              x.Leave_Type == update.Leave_Type &&
              codes.Contains(x.Leave_Code))
          .ToListAsync();

      if (!data.Any())
        return new List<HRMS_Att_Yearly>();

      DateTime current = DateTime.Now;
      data.ForEach(x =>
      {
        var detail = update.Details.FirstOrDefault(d => d.Code == x.Leave_Code);
        var detailOld = att_Loaned_Monthly_Detail.FirstOrDefault(d => d.Leave_Code == x.Leave_Code);
        if (detail is not null)
        {
          x.Days += detail.Days - (detailOld == null ? 0 : detailOld.Days);
          x.Update_By = update.Account;
          x.Update_Time = current;
        }
      });

      return data;
    }
  }
}