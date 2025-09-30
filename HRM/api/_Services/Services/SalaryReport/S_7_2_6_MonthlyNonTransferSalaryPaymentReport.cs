using System.Drawing.Text;
using System.Xml;
using API.Data;
using API._Services.Interfaces.SalaryReport;
using API.DTOs.SalaryReport;
using API.Helper.Constant;
using API.Models;
using Aspose.Cells;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryReport
{
  public class S_7_2_6_MonthlyNonTransferSalaryPaymentReport : BaseServices, I_7_2_6_MonthlyNonTransferSalaryPaymentReport
  {
    public S_7_2_6_MonthlyNonTransferSalaryPaymentReport(DBContext dbContext) : base(dbContext) { }

    #region DownloadFilePdf
    public async Task<OperationResult> DownloadFilePdf(D_7_2_6_MonthlyNonTransferSalaryPaymentReportParam param, string userName)
    {
      try
      {
        var dataList = await GetData(param);
        var dataDepartment = dataList.GroupBy(x => x.Department).ToList();
        if (!dataList.Any())
          return new OperationResult(false, "No data found.");

        var templatePath = "Resources\\Template\\SalaryReport\\7_2_6_MonthlyNonTransferSalaryPaymentReport\\7.2.6_Template.xlsx";
        Workbook workbook = new(templatePath);
        Worksheet worksheet = workbook.Worksheets[0];
        worksheet.Name = "Template";
        var deparmentList = await GetListDepartment(param.Factory, param.Language);
        var now = DateTime.Now;
        foreach (var group in dataDepartment)
        {
          var deptCode = group.Key;
          var department = deparmentList.FirstOrDefault(x => x.Key == deptCode).Value ?? deptCode;
          var newSheet = workbook.Worksheets.Add(deptCode);
          newSheet.Copy(worksheet);
          newSheet.Name = deptCode;

          var ws = newSheet;
          ws.PageSetup.Orientation = PageOrientationType.Landscape;
          ws.PageSetup.PaperSize = PaperSizeType.PaperA4;

          var departmentData = group.ToList();
          var first = departmentData.First();

          ws.Cells["A3"].PutValue($"廠別 Factory：{first.Factory}");
          ws.Cells["A4"].PutValue($"列印人員 Print By：{userName}");
          ws.Cells["D3"].PutValue($"薪資年月 Year-Month：{Convert.ToDateTime(first.Year_Month):yyyy/MM}");
          ws.Cells["D4"].PutValue($"列印日期 Print Date：{now:yyyy/MM/dd HH:mm:ss}");

          ws.Cells["A6"].PutValue(department);
          var style = ws.Cells["A6"].GetStyle();
          style.IsTextWrapped = true;
          ws.Cells["A6"].SetStyle(style);

          ws.Cells["B6"].PutValue(string.Join(" ", departmentData.SelectMany(x => x.List_Employee_ID).Distinct()));

          ws.Cells["A8"].PutValue(departmentData.Count);
          ws.Cells["B8"].PutValue(departmentData.Sum(x => x.Actual_Amount).ToString("N0"));

          ws.Cells["B10"].PutValue(departmentData.Sum(x => x.tt_h50));
          ws.Cells["C10"].PutValue(departmentData.Sum(x => x.tt_200));
          ws.Cells["D10"].PutValue(departmentData.Sum(x => x.tt_100));
          ws.Cells["E10"].PutValue(departmentData.Sum(x => x.tt_50));
          ws.Cells["F10"].PutValue(departmentData.Sum(x => x.tt_20));
          ws.Cells["G10"].PutValue(departmentData.Sum(x => x.tt_10));
          ws.Cells["H10"].PutValue(departmentData.Sum(x => x.tt_5));
          ws.Cells["I10"].PutValue(departmentData.Sum(x => x.tt_2));
          ws.Cells["J10"].PutValue(departmentData.Sum(x => x.tt_1));
        }
        workbook.Worksheets.RemoveAt("Template");

        using var stream = new MemoryStream();
        workbook.Save(stream, SaveFormat.Pdf);
        var result = new
        {
          filePdf = stream.ToArray(),
          totalRow = dataDepartment.Count
        };
        return new OperationResult(true, result);
      }
      catch
      {
        return new OperationResult(false, "Download File Pdf failed");
      }

    }
    #endregion

    #region Search Data
    public async Task<int> SearchData(D_7_2_6_MonthlyNonTransferSalaryPaymentReportParam param)
    {
      var data = await GetData(param);
      var dataSearch = data.GroupBy(x => x.Department).ToList();
      return dataSearch.Count;
    }
    #endregion

    private async Task<List<D_7_2_6_MonthlyNonTransferSalaryPaymentReportDto>> GetData(D_7_2_6_MonthlyNonTransferSalaryPaymentReportParam param)
    {
      var yearMonth = DateTime.Parse(param.Year_Month);
      var start_Date = yearMonth;
      var end_Date = yearMonth.AddMonths(1).AddDays(-1); ;

      var predSalMonthly = PredicateBuilder.New<HRMS_Sal_Monthly>(x =>
          x.Factory == param.Factory &&
          x.Sal_Month == yearMonth &&
          x.BankTransfer == "N"
      );

      var predEmpPersonal = PredicateBuilder.New<HRMS_Emp_Personal>(x =>
         x.Onboard_Date <= end_Date &&
         param.Permission_Group.Contains(x.Permission_Group) &&
         (!x.Resign_Date.HasValue || (x.Resign_Date.HasValue && x.Resign_Date.Value > end_Date))
     );

      if (!string.IsNullOrWhiteSpace(param.Department))
      {
        predSalMonthly.And(x => x.Department == param.Department);
        predEmpPersonal.And(x => x.Department == param.Department);
      }
      if (!string.IsNullOrWhiteSpace(param.Employee_ID))
      {
        predSalMonthly.And(x => x.Employee_ID.Contains(param.Employee_ID.Trim()));
        predEmpPersonal.And(x => x.Employee_ID.Contains(param.Employee_ID.Trim()));
      }

      var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predEmpPersonal, true);
      var HSM = _repositoryAccessor.HRMS_Sal_Monthly.FindAll(predSalMonthly, true);

      var wk_sql = HEP.Join(HSM,
      x => new { x.Factory, x.Employee_ID },
      y => new { y.Factory, y.Employee_ID },
      (x, y) => new { HEP = x, HSM = y }).Distinct().ToList();

      var resultList = new List<D_7_2_6_MonthlyNonTransferSalaryPaymentReportDto>();

      foreach (var item in wk_sql)
      {
        var factory = item.HSM.Factory;
        var employeeID = item.HSM.Employee_ID;
        var yearMonthValue = item.HSM.Sal_Month;
        var tax = item.HSM.Tax;

        // --正項合計 (Add)
        decimal wk_add = await Query_Sal_Monthly_Detail_Add_Sum("Y", factory, yearMonthValue, employeeID);

        // --扣項合計 (Deduct)
        decimal wk_sub = (await Query_Sal_Monthly_Detail_Ded_Sum("Y", factory, yearMonthValue, employeeID)) + tax;

        // --實領 (Amount)
        decimal wk_sum = wk_add - wk_sub;

        List<string> employeeIDList = new List<string> { item.HSM.Employee_ID };

        // 分解面額
        var (tt_h50, tt_200, tt_100, tt_50, tt_20, tt_10, tt_5, tt_2, tt_1) = CalculateDenomination(wk_sum);

        resultList.Add(new D_7_2_6_MonthlyNonTransferSalaryPaymentReportDto
        {
          Factory = factory,
          Year_Month = param.Year_Month,
          Department = item.HSM.Department,
          Employee_ID = employeeID,
          List_Employee_ID = employeeIDList,
          Actual_Amount = wk_sum,
          tt_h50 = tt_h50,
          tt_200 = tt_200,
          tt_100 = tt_100,
          tt_50 = tt_50,
          tt_20 = tt_20,
          tt_10 = tt_10,
          tt_5 = tt_5,
          tt_2 = tt_2,
          tt_1 = tt_1
        });
      }
      return resultList;
    }

    private (decimal h50, decimal c200, decimal c100, decimal c50, decimal c20, decimal c10, decimal c5, decimal c2, decimal c1)
    CalculateDenomination(decimal amount)
    {
      var remain = amount;
      var h50 = Math.Floor(remain / 500000); remain %= 500000;
      var c200 = Math.Floor(remain / 200000); remain %= 200000;
      var c100 = Math.Floor(remain / 100000); remain %= 100000;
      var c50 = Math.Floor(remain / 50000); remain %= 50000;
      var c20 = Math.Floor(remain / 20000); remain %= 20000;
      var c10 = Math.Floor(remain / 10000); remain %= 10000;
      var c5 = Math.Floor(remain / 5000); remain %= 5000;
      var c2 = Math.Floor(remain / 2000); remain %= 2000;
      var c1 = Math.Floor(remain / 1000);
      return (h50, c200, c100, c50, c20, c10, c5, c2, c1);
    }

    public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
    {
      var HOD = await Query_Department_List(factory);
      var HODL = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Factory == factory&& x.Language_Code.ToLower() == language.ToLower()).ToList();

      var deparment = HOD.GroupJoin(HODL,
                  x => new { x.Division, x.Department_Code },
                  y => new { y.Division, y.Department_Code },
                  (x, y) => new { dept = x, hodl = y })
                  .SelectMany(x => x.hodl.DefaultIfEmpty(),
                  (x, y) => new { x.dept, hodl = y })
                  .Select(x => new KeyValuePair<string, string>(x.dept.Department_Code, $"{x.dept.Department_Code}-{(x.hodl != null ? x.hodl.Name : x.dept.Department_Name)}"))
                  .ToList();
      return deparment;
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
    
    #region Query_Sal_Monthly_Detail_Add_Sum
    private async Task<int> Query_Sal_Monthly_Detail_Add_Sum(string Kind, string Factory, DateTime Year_Month, string Employee_ID)
    {
      int total1 = await Query_Sal_Monthly_Detail_Sum(Kind, Factory, Year_Month, Employee_ID, "45", "A");
      int total2 = await Query_Sal_Monthly_Detail_Sum(Kind, Factory, Year_Month, Employee_ID, "42", "A");
      int total3 = await Query_Sal_Monthly_Detail_Sum(Kind, Factory, Year_Month, Employee_ID, "49", "A");
      int total4 = await Query_Sal_Monthly_Detail_Sum(Kind, Factory, Year_Month, Employee_ID, "49", "B");

      int addTotal = total1 + total2 + total3 + total4;
      return addTotal;
    }
    #endregion

    #region Query_Sal_Monthly_Detail_Ded_Sum
    private async Task<int> Query_Sal_Monthly_Detail_Ded_Sum(string Kind, string Factory, DateTime Year_Month, string Employee_ID)
    {
      int total1 = await Query_Sal_Monthly_Detail_Sum(Kind, Factory, Year_Month, Employee_ID, "57", "D");
      int total2 = await Query_Sal_Monthly_Detail_Sum(Kind, Factory, Year_Month, Employee_ID, "49", "C");
      int total3 = await Query_Sal_Monthly_Detail_Sum(Kind, Factory, Year_Month, Employee_ID, "49", "D");

      int addTotal = total1 + total2 + total3;
      return addTotal;
    }
    #endregion
  }
}