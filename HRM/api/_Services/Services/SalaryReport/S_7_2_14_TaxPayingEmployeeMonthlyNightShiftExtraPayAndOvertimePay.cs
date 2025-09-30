using System.Drawing;
using System.Globalization;
using API._Services.Interfaces.SalaryReport;
using API.Data;
using API.DTOs.SalaryReport;
using API.Helper.Constant;
using API.Models;
using Aspose.Cells;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryReport
{
    public class S_7_2_14_TaxPayingEmployeeMonthlyNightShiftExtraPayAndOvertimePay : BaseServices, I_7_2_14_TaxPayingEmployeeMonthlyNightShiftExtraPayAndOvertimePay
    {
        public S_7_2_14_TaxPayingEmployeeMonthlyNightShiftExtraPayAndOvertimePay(DBContext dbContext) : base(dbContext) { }
        private static readonly string rootPath = Directory.GetCurrentDirectory();
        #region GetData
        private async Task<OperationResult> GetData(NightShiftExtraAndOvertimePayParam param, bool countOnly = false)
        {
            if (string.IsNullOrWhiteSpace(param.Factory)
                || !param.Permission_Group.Any()
                || string.IsNullOrWhiteSpace(param.Year_Month)
                || !DateTime.TryParseExact(param.Year_Month, "yyyy/MM", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime yearMonth))
                return new OperationResult(false, "SalaryReport.TaxPayingEmployeeMonthlyNightShiftExtraPayAndOvertimePay.InvalidInput");

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == param.Factory &&
                param.Permission_Group.Contains(x.Permission_Group), true);

            var pred = PredicateBuilder.New<HRMS_Sal_Monthly>(x => x.Factory == param.Factory
                && x.Sal_Month.Date == yearMonth.Date
                && x.Tax > 0
                && param.Permission_Group.Contains(x.Permission_Group)
                && HEP.Select(x => x.Employee_ID).Contains(x.Employee_ID));

            if (!string.IsNullOrWhiteSpace(param.Department))
                pred.And(x => x.Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.EmployeeID))
                pred.And(x => x.Employee_ID.Contains(param.EmployeeID));

            var wk_sql = await _repositoryAccessor.HRMS_Sal_Monthly.FindAll(pred).ToListAsync();

            if (countOnly == true)
                return new OperationResult(true, wk_sql);

            var result = new List<NightShiftExtraAndOvertimePayReport>();

            var HSTN = _repositoryAccessor.HRMS_Sal_Tax_Number.FindAll(x => x.Factory == param.Factory && x.Year <= yearMonth, true).ToList();
            var HSAM = _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.FindAll(x => x.Factory == param.Factory && x.Sal_Month <= yearMonth, true).ToList();
            var employeeIds = wk_sql.Select(x => x.Employee_ID).ToList();
            var permissionGroups = wk_sql.Select(x => x.Permission_Group).ToList();
            var salaryTypes = wk_sql.Select(x => x.Salary_Type).ToList();

            List<string> itemQuery1 = new() { "A01", "C01", "A03" };
            List<string> itemQuery2 = new() { "V01", "V02", "V03" };

            var query42A = await Query_Single_Sal_Monthly_Detail("Y", param.Factory, yearMonth, employeeIds, "42", "A", itemQuery1);
            var query57D = await Query_Single_Sal_Monthly_Detail("Y", param.Factory, yearMonth, employeeIds, "57", "D", itemQuery2);

            var overtimeHours = await Query_Att_Monthly_Detail("Y", param.Factory, yearMonth, employeeIds, "2");
            var querySalMonthlyDetail = await Query_Sal_Monthly_Detail("Y", param.Factory, yearMonth, employeeIds,
                    "42", "A", permissionGroups, salaryTypes, "2");

            foreach (var item in wk_sql)
            {
                var wageStandard = await Query_WageStandard_Sum("B", item.Factory, item.Sal_Month, item.Employee_ID, item.Permission_Group, item.Salary_Type);

                var A06_AMT = HSAM.Where(x => x.Employee_ID == item.Employee_ID
                    && x.AddDed_Type == "A"
                    && x.AddDed_Item == "A06").Select(x => x.Amount).FirstOrDefault();

                var overtime50_AMT = query42A.FirstOrDefault(x => x.Employee_ID == item.Employee_ID && x.Item == "A01")?.Amount ?? 0;
                var ho_AMT = query42A.FirstOrDefault(x => x.Employee_ID == item.Employee_ID && x.Item == "C01")?.Amount ?? 0;

                var total1 = query42A.FirstOrDefault(x => x.Employee_ID == item.Employee_ID && x.Item == "A03")?.Amount ?? 0;
                var total2 = query57D.FirstOrDefault(x => x.Employee_ID == item.Employee_ID && x.Item == "V01")?.Amount ?? 0;
                var total3 = query57D.FirstOrDefault(x => x.Employee_ID == item.Employee_ID && x.Item == "V02")?.Amount ?? 0;
                var total4 = query57D.FirstOrDefault(x => x.Employee_ID == item.Employee_ID && x.Item == "V03")?.Amount ?? 0;

                decimal nhno_AMT = (await Query_Att_Monthly_Detail_Item(item.Factory, item.Sal_Month, item.Employee_ID, "2", "A01")) <= 0
                    ? total1 * 100 / 210
                    : total1 * 110 / 210;

                decimal ins_AMT = total2 + total3 + total4;

                result.Add(new NightShiftExtraAndOvertimePayReport
                {
                    Factory = item.Factory,
                    Department = item.Department,
                    EmployeeID = item.Employee_ID,
                    LocalFullName = HEP.FirstOrDefault(x => x.Employee_ID == item.Employee_ID)?.Local_Full_Name,
                    TaxNo = HSTN.Where(x => x.Employee_ID == item.Employee_ID)
                        .OrderByDescending(x => x.Year)
                        .FirstOrDefault()?.TaxNo,
                    Standard = wageStandard,
                    OvertimeHours = overtimeHours.Where(x => x.Employee_ID == item.Employee_ID).ToList(),
                    OvertimeAndNightShiftAllowance = querySalMonthlyDetail.Where(x => x.Employee_ID == item.Employee_ID)
                        .Select(x => new OvertimeAndNightShiftAllowance
                        {
                            Employee_ID = x.Employee_ID,
                            Item = x.Item,
                            Amount = x.Amount
                        }).ToList(),
                    A06_AMT = A06_AMT,
                    Overtime50_AMT = (overtime50_AMT + A06_AMT) * 1 / 3,
                    NHNO_AMT = nhno_AMT,
                    HO_AMT = ho_AMT,
                    INS_AMT = ins_AMT,
                    SUM_AMT = nhno_AMT + ho_AMT + ins_AMT
                });
            }

            return new OperationResult(true, result);
        }
        #endregion

        #region Download
        public async Task<OperationResult> Download(NightShiftExtraAndOvertimePayParam param)
        {
            var updatedPermissionGroup = new List<string>();
            var listPermissionGroup = await GetPermissionGroup(param.Factory, param.Language);
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;

            var data = (List<NightShiftExtraAndOvertimePayReport>)result.Data;

            if (data.Count == 0)
                return new OperationResult(false, "System.Message.NoData");

            var listDepartments = await GetDepartmentName(param.Factory, param.Language);
            var listAllowances = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Allowance, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .ToListAsync();

            foreach (var item in data)
            {
                item.DepartmentName = listDepartments.FirstOrDefault(x => x.Key == item.Department).Value;
                item.OvertimeHours.ForEach(x =>
                {
                    x.CodeName_TW = listAllowances.FirstOrDefault(y => y.HBC.Code == x.Leave_Code && y.HBCL.Language_Code == "TW")?.HBCL.Code_Name;
                    x.CodeName_EN = listAllowances.FirstOrDefault(y => y.HBC.Code == x.Leave_Code && y.HBCL.Language_Code == "EN")?.HBCL.Code_Name;
                });
                item.OvertimeAndNightShiftAllowance.ForEach(x =>
                {
                    x.AllowanceName_TW = listAllowances.FirstOrDefault(y => y.HBC.Code == x.Item && y.HBCL.Language_Code == "TW")?.HBCL.Code_Name;
                    x.AllowanceName_EN = listAllowances.FirstOrDefault(y => y.HBC.Code == x.Item && y.HBCL.Language_Code == "EN")?.HBCL.Code_Name;
                });
            };

            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "2");
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language
                .FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true);

            var BasicCodeLanguage = await HBC
                .GroupJoin(HBCL,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Code,
                    x.HBC.Type_Seq,
                    Code_Name = $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                })
                .ToListAsync();
            var factory = BasicCodeLanguage
                .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.Factory
                    && x.Code == param.Factory).Code_Name;

            var department = string.Empty;
            if (!string.IsNullOrWhiteSpace(param.Department))
            {
                department = await _repositoryAccessor.HRMS_Org_Department
                    .FindAll(x => x.Factory == param.Factory && x.Department_Code == param.Department, true)
                    .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true),
                        x => new { x.Division, x.Factory, x.Department_Code },
                        y => new { y.Division, y.Factory, y.Department_Code },
                        (HOD, HODL) => new { HOD, HODL })
                    .SelectMany(x => x.HODL.DefaultIfEmpty(),
                        (x, y) => new { x.HOD, HODL = y })
                    .Select(x => $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}")
                    .FirstOrDefaultAsync();
            }

            MemoryStream memoryStream = new();
            string file = Path.Combine(
                rootPath,
                "Resources\\Template\\SalaryReport\\7_2_14_TaxPayingEmployeeMonthlyNightShiftExtraPayAndOvertimePay\\Download.xlsx"
            );
            WorkbookDesigner obj = new()
            {
                Workbook = new Workbook(file)
            };
            foreach (var item in param.Permission_Group)
            {
                var updatedItem = listPermissionGroup.FirstOrDefault(x => x.Key == item).Value;
                updatedPermissionGroup.Add(updatedItem);
            }
            Worksheet worksheet = obj.Workbook.Worksheets[0];

            worksheet.Cells["B2"].PutValue(factory);
            worksheet.Cells["D2"].PutValue(param.Year_Month);
            worksheet.Cells["F2"].PutValue(string.Join(",", updatedPermissionGroup));
            worksheet.Cells["H2"].PutValue(department);
            worksheet.Cells["J2"].PutValue(param.EmployeeID);
            worksheet.Cells["B3"].PutValue(param.UserName);
            worksheet.Cells["D3"].PutValue(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));

            var styleTitle = obj.Workbook.CreateStyle();
            styleTitle.IsTextWrapped = true;
            worksheet.Cells["F2"].SetStyle(styleTitle);

            var dataOvertime = Math.Max(data[0].OvertimeHours.Count, data[0].OvertimeAndNightShiftAllowance.Count);
            for (int i = 0; i < dataOvertime; i++)
            {
                if (i < data[0].OvertimeHours.Count)
                {
                    worksheet.Cells[4, i + 7].PutValue(data[0].OvertimeHours[i].Leave_Code + " - " + data[0].OvertimeHours[i].CodeName_TW);
                    worksheet.Cells[4, i + 7].SetStyle(GetStyle(obj, 226, 239, 218));
                    worksheet.Cells[5, i + 7].PutValue(data[0].OvertimeHours[i].Leave_Code + " - " + data[0].OvertimeHours[i].CodeName_EN);
                    worksheet.Cells[5, i + 7].SetStyle(GetStyle(obj, 226, 239, 218));
                }

                if (i < data[0].OvertimeAndNightShiftAllowance.Count)
                {
                    worksheet.Cells[4, i + data[0].OvertimeHours.Count + 7].PutValue(data[0].OvertimeAndNightShiftAllowance[i].Item + " - " + data[0].OvertimeAndNightShiftAllowance[i].AllowanceName_TW);
                    worksheet.Cells[4, i + data[0].OvertimeHours.Count + 7].SetStyle(GetStyle(obj, 255, 242, 204));
                    worksheet.Cells[5, i + data[0].OvertimeHours.Count + 7].PutValue(data[0].OvertimeAndNightShiftAllowance[i].Item + " - " + data[0].OvertimeAndNightShiftAllowance[i].AllowanceName_EN);
                    worksheet.Cells[5, i + data[0].OvertimeHours.Count + 7].SetStyle(GetStyle(obj, 255, 242, 204));
                }
                var style = obj.Workbook.CreateStyle();
                style.IsTextWrapped = true;
                style.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thin;
                style.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Thin;
                style.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thin;
                style.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thin;
                var totalIndex = data[0].OvertimeHours.Count + data[0].OvertimeAndNightShiftAllowance.Count + 7;
                worksheet.Cells[4, totalIndex].PutValue("A06加班費(參加非生產活動)");
                worksheet.Cells[5, totalIndex].PutValue("A06-Overtime Pay (Non-Production Activities)");
                worksheet.Cells[4, totalIndex].SetStyle(style);
                worksheet.Cells[5, totalIndex].SetStyle(style);
                worksheet.Cells[4, totalIndex + 1].PutValue("非假日加班費差額50%");
                worksheet.Cells[5, totalIndex + 1].PutValue("Overtime Paid 50% Difference on Normal Working Day");
                worksheet.Cells[4, totalIndex + 1].SetStyle(style);
                worksheet.Cells[5, totalIndex + 1].SetStyle(style);
                worksheet.Cells[4, totalIndex + 2].PutValue("非假日夜班加班費差額100%& 110%");
                worksheet.Cells[5, totalIndex + 2].PutValue("Night Shift Overtime Paid 100% or 110% Difference on Normal Working Day");
                worksheet.Cells[4, totalIndex + 2].SetStyle(style);
                worksheet.Cells[5, totalIndex + 2].SetStyle(style);
                worksheet.Cells[4, totalIndex + 3].PutValue("假日加班費差額300%");
                worksheet.Cells[5, totalIndex + 3].PutValue("Overtime Paid 300% Difference on National Holiday");
                worksheet.Cells[4, totalIndex + 3].SetStyle(style);
                worksheet.Cells[5, totalIndex + 3].SetStyle(style);
                worksheet.Cells[4, totalIndex + 4].PutValue("保險金額");
                worksheet.Cells[5, totalIndex + 4].PutValue("Insurance Amount");
                worksheet.Cells[4, totalIndex + 4].SetStyle(style);
                worksheet.Cells[5, totalIndex + 4].SetStyle(style);
                worksheet.Cells[4, totalIndex + 5].PutValue("家境狀況獲准豁免收入稅前不負稅之金額");
                worksheet.Cells[5, totalIndex + 5].PutValue("Non-Taxable Amount before Deduction for Dependents");
                worksheet.Cells[4, totalIndex + 5].SetStyle(style);
                worksheet.Cells[5, totalIndex + 5].SetStyle(style);

            }
            Style styleAmount = obj.Workbook.CreateStyle();
            styleAmount.Custom = "#,##0";

            for (int i = 0; i < data.Count; i++)
            {
                worksheet.Cells["A" + (i + 7)].PutValue(data[i].Factory);
                worksheet.Cells["B" + (i + 7)].PutValue(data[i].Department);
                worksheet.Cells["C" + (i + 7)].PutValue(data[i].DepartmentName);
                worksheet.Cells["D" + (i + 7)].PutValue(data[i].EmployeeID);
                worksheet.Cells["E" + (i + 7)].PutValue(data[i].LocalFullName);
                worksheet.Cells["F" + (i + 7)].PutValue(data[i].TaxNo);
                worksheet.Cells["G" + (i + 7)].PutValue(data[i].Standard);
                worksheet.Cells["G" + (i + 7)].SetStyle(styleAmount);


                int columnIndex = 7;
                for (int j = 0; j < dataOvertime; j++)
                {
                    if (j < data[i].OvertimeHours.Count)
                        worksheet.Cells[i + 6, columnIndex].PutValue(data[i].OvertimeHours[j].Days);
                    if (j < data[i].OvertimeAndNightShiftAllowance.Count)
                    {
                        worksheet.Cells[i + 6, columnIndex + data[i].OvertimeHours.Count].PutValue(data[i].OvertimeAndNightShiftAllowance[j].Amount);
                        worksheet.Cells[i + 6, columnIndex + data[i].OvertimeHours.Count].SetStyle(styleAmount);
                    }
                    columnIndex++;
                }
                var totalIndex = data[i].OvertimeHours.Count + data[i].OvertimeAndNightShiftAllowance.Count + 7;
                worksheet.Cells[i + 6, totalIndex].PutValue(data[i].A06_AMT);
                worksheet.Cells[i + 6, totalIndex].SetStyle(styleAmount);

                worksheet.Cells[i + 6, totalIndex + 1].PutValue(data[i].Overtime50_AMT);
                worksheet.Cells[i + 6, totalIndex + 1].SetStyle(styleAmount);

                worksheet.Cells[i + 6, totalIndex + 2].PutValue(data[i].NHNO_AMT);
                worksheet.Cells[i + 6, totalIndex + 2].SetStyle(styleAmount);

                worksheet.Cells[i + 6, totalIndex + 3].PutValue(data[i].HO_AMT);
                worksheet.Cells[i + 6, totalIndex + 3].SetStyle(styleAmount);

                worksheet.Cells[i + 6, totalIndex + 4].PutValue(data[i].INS_AMT);
                worksheet.Cells[i + 6, totalIndex + 4].SetStyle(styleAmount);

                worksheet.Cells[i + 6, totalIndex + 5].PutValue(data[i].SUM_AMT);
                worksheet.Cells[i + 6, totalIndex + 5].SetStyle(styleAmount);
            }
            var totalRow = data.Count + 9;
            worksheet.Cells["A" + totalRow].PutValue("核決:");
            worksheet.Cells["A" + (totalRow + 1)].PutValue("Approved by:");
            worksheet.Cells["E" + totalRow].PutValue("審核:");
            worksheet.Cells["E" + (totalRow + 1)].PutValue("Checked by:");
            worksheet.Cells["H" + totalRow].PutValue("製表:");
            worksheet.Cells["H" + (totalRow + 1)].PutValue("Applicant:");
            worksheet.AutoFitColumns(6, data[0].OvertimeHours.Count + data[0].OvertimeAndNightShiftAllowance.Count + 13);
            worksheet.AutoFitRows(1, 6);

            obj.Workbook.Save(memoryStream, SaveFormat.Xlsx);
            return new OperationResult(true, new { TotalRows = data.Count, Excel = memoryStream.ToArray() });
        }
        #endregion

        public async Task<OperationResult> GetTotalRows(NightShiftExtraAndOvertimePayParam param)
        {
            var result = await GetData(param, true);
            if (!result.IsSuccess)
                return result;
            var data = (List<HRMS_Sal_Monthly>)result.Data;
            return new OperationResult(true, data.Count);
        }

        #region Query_Att_Monthly_Detail_Item
        private async Task<decimal> Query_Att_Monthly_Detail_Item(string Factory, DateTime Year_Month, string EmployeeID, string Leave_Type, string Leave_Code)
        {
            var days = await _repositoryAccessor.HRMS_Att_Monthly_Detail.FindAll(x => x.Factory == Factory
                    && x.Att_Month == Year_Month
                    && x.Employee_ID == EmployeeID
                    && x.Leave_Type == Leave_Type
                    && x.Leave_Code == Leave_Code)
                .Select(x => x.Days)
                .FirstOrDefaultAsync();
            return days;
        }
        #endregion

        #region Query_Att_Monthly_Detail
        private async Task<List<Att_Monthly_Detail_Values>> Query_Att_Monthly_Detail(string Kind, string Factory, DateTime YearMonth, List<string> EmployeeIDs, string LeaveType)
        {
            List<Att_Monthly_Detail_Temp_7_2_14> Att_Monthly_Detail_Temp;

            if (Kind == "Y")
            {
                Att_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Att_Monthly_Detail
                    .FindAll(x => x.Factory == Factory
                        && x.Att_Month == YearMonth
                        && EmployeeIDs.Contains(x.Employee_ID)
                        && x.Leave_Type == LeaveType, true)
                    .Select(x => new Att_Monthly_Detail_Temp_7_2_14
                    {
                        Employee_ID = x.Employee_ID,
                        Leave_Code = x.Leave_Code,
                        Days = x.Days
                    })
                    .ToListAsync();
            }
            else
            {
                Att_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Att_Resign_Monthly_Detail
                    .FindAll(x => x.Factory == Factory
                        && x.Att_Month == YearMonth
                        && EmployeeIDs.Contains(x.Employee_ID)
                        && x.Leave_Type == LeaveType, true)
                    .Select(x => new Att_Monthly_Detail_Temp_7_2_14
                    {
                        Employee_ID = x.Employee_ID,
                        Leave_Code = x.Leave_Code,
                        Days = x.Days
                    })
                    .ToListAsync();
            }

            var maxEffectiveMonth = await _repositoryAccessor.HRMS_Att_Use_Monthly_Leave
                .FindAll(x => x.Factory == Factory
                    && x.Leave_Type == LeaveType
                    && x.Effective_Month <= YearMonth, true)
                .MaxAsync(x => (DateTime?)x.Effective_Month);

            if (!maxEffectiveMonth.HasValue)
                return new List<Att_Monthly_Detail_Values>();

            var Setting_Temp = await _repositoryAccessor.HRMS_Att_Use_Monthly_Leave
                .FindAll(x => x.Factory == Factory
                    && x.Leave_Type == LeaveType
                    && x.Effective_Month == maxEffectiveMonth.Value, true)
                .Select(x => new Setting_Temp_7_2_14
                {
                    Seq = x.Seq,
                    Code = x.Code
                })
                .ToListAsync();

            var result = Att_Monthly_Detail_Temp
                .GroupJoin(Setting_Temp,
                    x => x.Leave_Code,
                    y => y.Code,
                    (x, y) => new { AMDT = x, Setting_Temp = y })
                .SelectMany(x => x.Setting_Temp.DefaultIfEmpty(),
                    (x, y) => new { x.AMDT, Setting_Temp = y })
                .Select(x =>
                    new Att_Monthly_Detail_Values
                    {
                        Employee_ID = x.AMDT.Employee_ID,
                        Leave_Code = x.AMDT.Leave_Code,
                        Seq = x.Setting_Temp?.Seq ?? 0,
                        Days = x.AMDT.Days
                    })
                .OrderBy(x => x.Seq)
                .ThenBy(x => x.Leave_Code)
                .ToList();

            return result;
        }
        #endregion

        #region GetList
        private async Task<List<KeyValuePair<string, string>>> GetPermissionGroup(string factory, string Language)
        {
            return await Query_BasicCode_PermissionGroup(factory, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
        {
            var departments = await Query_Department_List(factory);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Factory == factory && x.Language_Code.ToLower() == language.ToLower())
                .ToList();
            var departmentsWithLanguage = departments
                .GroupJoin(HODL,
                    x => new { x.Division, x.Department_Code },
                    y => new { y.Division, y.Department_Code },
                    (HOD, HODL) => new { HOD, HODL })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, HODL = y })
                .Select(x => new KeyValuePair<string, string>(x.HOD.Department_Code, $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}"))
                .Distinct()
                .ToList();
            return departmentsWithLanguage;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string userName, string language)
        {
            List<string> factories = await Queryt_Factory_AddList(userName);
            var factoriesWithLanguage = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory
                    && factories.Contains(x.Code), true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
            return factoriesWithLanguage;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language)
        {
            var permissionGroups = await Query_Permission_List(factory);

            var permissionGroupsWithLanguage = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup
                    && permissionGroups.Select(y => y.Permission_Group).Contains(x.Code), true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
            return permissionGroupsWithLanguage;
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

        private static Style GetStyle(WorkbookDesigner obj, int color1, int color2, int color3)
        {
            Style style = obj.Workbook.CreateStyle();
            style.ForegroundColor = Color.FromArgb(color1, color2, color3);
            style.Pattern = BackgroundType.Solid;
            style.IsTextWrapped = true;
            style.HorizontalAlignment = TextAlignmentType.Center;
            style.VerticalAlignment = TextAlignmentType.Center;
            style = AsposeUtility.SetAllBorders(style);
            return style;
        }
        #endregion
    }
}