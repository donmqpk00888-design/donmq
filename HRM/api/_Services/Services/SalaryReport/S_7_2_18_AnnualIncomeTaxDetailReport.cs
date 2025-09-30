using System.Drawing;
using System.Threading.Tasks;
using API._Services.Interfaces.SalaryReport;
using API.Data;
using API.DTOs;
using API.DTOs.SalaryReport;
using API.Helper.Constant;
using API.Models;
using Aspose.Cells;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using SDCores;

namespace API._Services.Services.SalaryReport
{
    public class S_7_2_18_AnnualIncomeTaxDetailReport : BaseServices, I_7_2_18_AnnualIncomeTaxDetailReport
    {
        public S_7_2_18_AnnualIncomeTaxDetailReport(DBContext dbContext) : base(dbContext)
        {
        }
        #region DownLoad Data
        public async Task<OperationResult> Download(AnnualIncomeTaxDetailReportParam param)
        {
            var department = string.Empty;

            var type_Seq = new List<string>()
            {
                BasicCodeTypeConstant.Factory,
                BasicCodeTypeConstant.PermissionGroup,
            };

            var (result, months) = await GetAnualIncomeTaxDetailByEmployee(param);

            TotalResultAnnualIncomeTaxDetail totalResult = new()
            {
                Count = result.Count
            };
            if (result.Count == 0)
                return new OperationResult(false, "System.Message.NoData");
            var groupedData = result
                            .GroupBy(item => new { USER_GUID = item.USER_GUID }) // Group by USER_GUID
                            .SelectMany(group => group) // Flatten the groups back into individual items
                            .ToList();
            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => type_Seq.Contains(x.Type_Seq));
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true);

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

            if (!string.IsNullOrWhiteSpace(param.Department))
            {
                department = await _repositoryAccessor.HRMS_Org_Department
                    .FindAll(x => x.Department_Code == param.Department, true)
                    .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true),
                        x => new
                        {
                            x.Division,
                            x.Factory,
                            x.Department_Code
                        },
                        y => new
                        {
                            y.Division,
                            y.Factory,
                            y.Department_Code
                        },
                        (HOD, HODL) => new { HOD, HODL })
                    .SelectMany(x => x.HODL.DefaultIfEmpty(),
                        (x, y) => new { x.HOD, HODL = y })
                    .Select(x => $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}")
                    .FirstOrDefaultAsync();
            }

            var factory = BasicCodeLanguage
                .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.Factory
                                  && x.Code == param.Factory).Code_Name;

            var permissionGroup = BasicCodeLanguage
                .Where(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup
                         && param.Permission_Group.Contains(x.Code))
                .Select(x => x.Code_Name).ToList();
            foreach (var item in groupedData)
            {
                totalResult.Total_y_amt += item.y_amt;
                totalResult.Total_pertax += item.pertax;
                totalResult.Total_loan += item.loan;
            }
            var header = new AnnualIncomeTaxDetail_Hearder
            {
                Factory = factory,
                PrintBy = param.UserName,
                YearMonth = param.Year_Month_Start.ToString("yyyy/MM") + " - " + param.Year_Month_End.ToString("yyyy/MM"),
                PrintDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                Department = department,
                Employee_Id = param.Employee_ID,
                PermisionGroups = string.Join(", ", permissionGroup),
            };

            // Convert To Data Excel 
            List<SDCores.Cell> resultCells = ConvertToDataExcel(groupedData, months, header, totalResult);


            List<Table> tables = new()
            {
                new Table("result", groupedData)
            };

            ConfigDownload config = new() {IsAutoFitColumn = false };
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                tables,
                resultCells,
                "Resources\\Template\\SalaryReport\\7_2_18_AnnualIncomeTaxDetailReport\\Download.xlsx",
                config
            );
            if (excelResult.IsSuccess)
                return new OperationResult(excelResult.IsSuccess, new { TotalRows = result.Count, Excel = excelResult.Result });
            else
                return new OperationResult(excelResult.IsSuccess, excelResult.Error);
        }

        /// <param name="isTitle"> default Title else SUM</param>  
        /// <returns></returns>  
        private static Style GetTitleStyle(bool isTitle = true)  
        {
            Style titleStyle = new CellsFactory().CreateStyle(); 
            titleStyle.Pattern = BackgroundType.Solid; 
            titleStyle.Font.Size = 12; 
            titleStyle.Font.Name = "Calibri"; 
            titleStyle.ForegroundColor = isTitle ?  Color.FromArgb(255, 242, 204) : Color.FromArgb(226, 239, 218);
            titleStyle.IsTextWrapped = true;
            titleStyle.Custom =  isTitle ? null : "#,##0";
            titleStyle.VerticalAlignment = TextAlignmentType.Center;
            // Thêm border cho tất cả các cạnh
            titleStyle.Borders[BorderType.TopBorder].LineStyle = isTitle ? CellBorderType.Thin : CellBorderType.None;
            titleStyle.Borders[BorderType.BottomBorder].LineStyle = isTitle ? CellBorderType.Thin : CellBorderType.None;
            titleStyle.Borders[BorderType.LeftBorder].LineStyle = isTitle ? CellBorderType.Thin : CellBorderType.None;
            titleStyle.Borders[BorderType.RightBorder].LineStyle = isTitle ? CellBorderType.Thin : CellBorderType.None;

            return titleStyle; 
        }
        private static Style GetTitleStyleNumber()  
        {
            Style titleStyle = new CellsFactory().CreateStyle(); 
            titleStyle.Font.Size = 12; 
            titleStyle.Font.Name = "Calibri"; 
            titleStyle.Custom = "#,##0";
            titleStyle.IsTextWrapped = true;

            return titleStyle; 
        }
        private static Style GetTitleStyleWhite()
        {
            Style style = new CellsFactory().CreateStyle();
            style.Pattern = BackgroundType.Solid;
            style.Font.Size = 12;
            style.Font.Name = "Calibri";
            style.IsTextWrapped = true;
            style.VerticalAlignment = TextAlignmentType.Center;
            style.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thin;
            style.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Thin;
            style.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thin;
            style.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thin;
            return style;
        }
        #region xử lí xuất excel
        private static List<SDCores.Cell> ConvertToDataExcel(List<AnnualIncomeTaxDetailReport> data, List<AnnualIncomeTax_Column_Month> months,
                                                            AnnualIncomeTaxDetail_Hearder hearder, TotalResultAnnualIncomeTaxDetail total)
        {
            List<SDCores.Cell> result = new()
            {
                new ("B2", hearder.Factory),
                new ("D2", hearder.YearMonth),
                new ("F2", hearder.PermisionGroups),
                new ("H2", hearder.Department),
                new ("J2", hearder.Employee_Id),
                new ("B3", hearder.PrintBy),
                new ("D3", hearder.PrintDate),
            };

            // init style chung cho ô tiêu đề & ô Sum 
            var titleStle = GetTitleStyle();
            var sumStyle = GetTitleStyle(false);
            var whiteStyle = GetTitleStyleWhite();
            var NumberStyle = GetTitleStyleNumber();
            int startRow = 5; // Dòng dữ liệu bắt đầu generate 
            int startColumn = 0; // Cột dữ liệu bắt đầu generate (A) 

            // Tính tổng số cột tối đa cần thiết 
            int defaultColumn = 8; // Số cột cố định trong bảng 
            // Generate Tháng # 
            for (int i = 0; i < months.Count; i++)
            {
                int baseColumn = 13 + (i * defaultColumn);
                bool isTotalYear = months[i].Month == "年度合計Year Total";
                var monthStyle = isTotalYear ? whiteStyle : titleStle; // Cột bắt đầu cho tháng hiện tại 
                result.Add(new(startRow - 3, baseColumn, months[i].Month));

                // Generate Title
                int indexColumn = 0;
                for (int iTitle = 0; iTitle < months[i].EN_Title.Count; iTitle++)
                {
                    result.Add(new SDCores.Cell(startRow - 2, baseColumn + indexColumn, months[i].ZH_Title[iTitle], monthStyle));
                    result.Add(new SDCores.Cell(startRow - 1, baseColumn + indexColumn, months[i].EN_Title[iTitle], monthStyle));
                    indexColumn++;
                }
            }

            // Tạo dictionary để lưu tổng theo từng tháng
            Dictionary<int, (int amt, int tax, int ovtm, int wage_t, int wage_h, int ins_fee, int sum_amt)> monthlyTotals = new();

            // Khởi tạo tổng cho từng tháng
            for (int i = 0; i < months.Count; i++)
            {
                monthlyTotals[i] = (0, 0, 0, 0, 0, 0, 0);
            }

            int currentRow = startRow;
            int stt = 1;

            foreach (var data_by_employee in data)
            {
                // Các cột cố định
                result.Add(new(currentRow, startColumn + 0, data_by_employee.No + stt));
                result.Add(new(currentRow, startColumn + 1, data_by_employee.Factory));
                result.Add(new(currentRow, startColumn + 2, data_by_employee.Department));
                result.Add(new(currentRow, startColumn + 3, data_by_employee.Department_Name));
                result.Add(new(currentRow, startColumn + 4, data_by_employee.Employee_ID));
                result.Add(new(currentRow, startColumn + 5, data_by_employee.Local_Full_Name));
                result.Add(new(currentRow, startColumn + 6, data_by_employee.mon_cnt));
                result.Add(new(currentRow, startColumn + 7, data_by_employee.subqty));
                result.Add(new(currentRow, startColumn + 8, data_by_employee.TaxNo));
                result.Add(new(currentRow, startColumn + 9, data_by_employee.Identification_Number));
                result.Add(new(currentRow, startColumn + 10, data_by_employee.y_amt, NumberStyle));
                result.Add(new(currentRow, startColumn + 11, data_by_employee.pertax, NumberStyle));
                result.Add(new(currentRow, startColumn + 12, data_by_employee.loan, NumberStyle));

                // Các cột động theo tháng
                int baseColumn = 13;
                for (int monthIndex = 0; monthIndex < data_by_employee.Detail_By_Months.Count; monthIndex++)
                {
                    var monthDetail = data_by_employee.Detail_By_Months[monthIndex];

                    result.Add(new(currentRow, baseColumn + 0, monthDetail.Detail_Pattent.amt, NumberStyle));
                    result.Add(new(currentRow, baseColumn + 1, monthDetail.Detail_Pattent.tax, NumberStyle));
                    result.Add(new(currentRow, baseColumn + 2, monthDetail.Detail_Pattent.subqty, NumberStyle));
                    result.Add(new(currentRow, baseColumn + 3, monthDetail.Detail_Pattent.ovtm, NumberStyle));
                    result.Add(new(currentRow, baseColumn + 4, monthDetail.Detail_Pattent.wage_t, NumberStyle));
                    result.Add(new(currentRow, baseColumn + 5, monthDetail.Detail_Pattent.wage_h, NumberStyle));
                    result.Add(new(currentRow, baseColumn + 6, monthDetail.Detail_Pattent.ins_fee, NumberStyle));
                    result.Add(new(currentRow, baseColumn + 7, monthDetail.Detail_Pattent.sum_amt, NumberStyle));

                    // Cộng dồn vào tổng theo tháng
                    (int amt, int tax, int ovtm, int wage_t, int wage_h, int ins_fee, int sum_amt) = monthlyTotals[monthIndex];
                    monthlyTotals[monthIndex] = (
                        amt + monthDetail.Detail_Pattent.amt,
                        tax + monthDetail.Detail_Pattent.tax,
                        ovtm + monthDetail.Detail_Pattent.ovtm,
                        wage_t + monthDetail.Detail_Pattent.wage_t,
                        wage_h + monthDetail.Detail_Pattent.wage_h,
                        ins_fee + monthDetail.Detail_Pattent.ins_fee,
                        sum_amt + monthDetail.Detail_Pattent.sum_amt
                    );

                    baseColumn += defaultColumn;
                }

                // Thêm cột tổng cuối cùng "年度合計Year Total"
                result.Add(new(currentRow, baseColumn + 0, data_by_employee.Sum_amt, NumberStyle));
                result.Add(new(currentRow, baseColumn + 1, data_by_employee.Sum_tax, NumberStyle));
                result.Add(new(currentRow, baseColumn + 2, data_by_employee.Sum_subqty, NumberStyle));
                result.Add(new(currentRow, baseColumn + 3, data_by_employee.Sum_ovtm, NumberStyle));
                result.Add(new(currentRow, baseColumn + 4, data_by_employee.Sum_wage_t, NumberStyle));
                result.Add(new(currentRow, baseColumn + 5, data_by_employee.Sum_wage_h, NumberStyle));
                result.Add(new(currentRow, baseColumn + 6, data_by_employee.Sum_ins_fee, NumberStyle));
                result.Add(new(currentRow, baseColumn + 7, data_by_employee.Sum_sum_amt, NumberStyle));

                currentRow++;
                stt++;
            }

            // Tính dòng tổng = startRow + số lượng nhân viên
            int totalRowIndex = startRow + data.Count;

            // Thêm dòng tổng cố định
            result.Add(new("F" + (totalRowIndex + 1), "總計金額\n Total:", sumStyle));
            result.Add(new("K" + (totalRowIndex + 1), total.Total_y_amt, sumStyle));
            result.Add(new("L" + (totalRowIndex + 1), total.Total_pertax, sumStyle));
            result.Add(new("M" + (totalRowIndex + 1), total.Total_loan, sumStyle));

            // Thêm tổng theo từng tháng - IN TỔNG TẤT CẢ NHÂN VIÊN THEO TỪNG THÁNG
            int baseTotalColumn = 13;
            for (int i = 0; i < months.Count - 1; i++)
            {
                (int amt, int tax, int ovtm, int wage_t, int wage_h, int ins_fee, int sum_amt) = monthlyTotals[i];
                result.Add(new(totalRowIndex, baseTotalColumn + 0, amt, sumStyle));
                result.Add(new(totalRowIndex, baseTotalColumn + 1, tax, sumStyle));
                result.Add(new(totalRowIndex, baseTotalColumn + 2, string.Empty)); 
                result.Add(new(totalRowIndex, baseTotalColumn + 3, ovtm, sumStyle));
                result.Add(new(totalRowIndex, baseTotalColumn + 4, wage_t, sumStyle));
                result.Add(new(totalRowIndex, baseTotalColumn + 5, wage_h, sumStyle));
                result.Add(new(totalRowIndex, baseTotalColumn + 6, ins_fee, sumStyle));
                result.Add(new(totalRowIndex, baseTotalColumn + 7, sum_amt, sumStyle));

                baseTotalColumn += defaultColumn;
            }
            return result;
        }
        #endregion
        #endregion
        #region  Get List Data
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
        #endregion
        #region  GetTotalRows
        public async Task<OperationResult> GetTotalRows(AnnualIncomeTaxDetailReportParam param)
        {
            DateTime YMStart  = param.Year_Month_Start;
            DateTime YMEnd = param.Year_Month_End;
            DateTime FYMStart = ToFirstDateOfMonth(YMStart);
            DateTime LYMEnd = ToLastDateOfMonth(YMEnd);
            var wk_sql = await _repositoryAccessor.HRMS_Emp_Personal
                .FindAll(e => e.Factory == param.Factory
                            && param.Permission_Group.Contains(e.Permission_Group)
                            && (!e.Resign_Date.HasValue || e.Resign_Date.Value.Date > FYMStart.Date)
                            && (string.IsNullOrWhiteSpace(param.Department) || e.Department == param.Department)
                            && (string.IsNullOrWhiteSpace(param.Employee_ID) || e.Employee_ID.Contains(param.Employee_ID)), true)
                .Select(e => e.USER_GUID)
                .ToListAsync();
            return new OperationResult(true, wk_sql.Count);
        }
        #endregion
        #region GetData Download
        private async Task<(List<AnnualIncomeTaxDetailReport>, List<AnnualIncomeTax_Column_Month>)> GetAnualIncomeTaxDetailByEmployee(AnnualIncomeTaxDetailReportParam param)
        {
            DateTime YMStart  = param.Year_Month_Start;
            DateTime YMEnd = param.Year_Month_End;
            DateTime FYMStart = ToFirstDateOfMonth(YMStart);
            DateTime LYMEnd = ToLastDateOfMonth(YMEnd);

            var wk_sql = await _repositoryAccessor.HRMS_Emp_Personal
                .FindAll(e => e.Factory == param.Factory
                    && param.Permission_Group.Contains(e.Permission_Group)
                    && (!e.Resign_Date.HasValue || e.Resign_Date.Value.Date > FYMStart.Date)
                    && (string.IsNullOrWhiteSpace(param.Department) || e.Department == param.Department)
                    && (string.IsNullOrWhiteSpace(param.Employee_ID) || e.Employee_ID.Contains(param.Employee_ID)), true)
                .Select(e => e.USER_GUID)
                .ToListAsync();

            var result = new List<AnnualIncomeTaxDetailReport>();
            var months = new List<AnnualIncomeTax_Column_Month>();
            var departmentNames = await GetDepartmentName(param.Factory, param.Language);

            // Truy vấn dữ liệu theo batch
            var HEP = await _repositoryAccessor.HRMS_Emp_Personal
                .FindAll(x => x.Factory == param.Factory && wk_sql.Contains(x.USER_GUID), true).ToListAsync();
            var HSTN = await _repositoryAccessor.HRMS_Sal_Tax_Number
                .FindAll(x => x.Factory == param.Factory && wk_sql.Contains(x.USER_GUID), true).ToListAsync();
            var HST = await _repositoryAccessor.HRMS_Sal_Tax
                .FindAll(x => x.Factory == param.Factory && wk_sql.Contains(x.USER_GUID), true).ToListAsync();
            var HSMD = await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                .FindAll(x => x.Factory == param.Factory && wk_sql.Contains(x.USER_GUID), true).ToListAsync();
            var HSRMD = await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
                .FindAll(x => x.Factory == param.Factory && wk_sql.Contains(x.USER_GUID), true).ToListAsync();
            var HSAM = await _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly
                .FindAll(x => x.Factory == param.Factory && wk_sql.Contains(x.USER_GUID), true).ToListAsync();
            var HAMD = await _repositoryAccessor.HRMS_Att_Monthly_Detail
                .FindAll(x => x.Factory == param.Factory && wk_sql.Contains(x.USER_GUID), true).ToListAsync();
            var HARMD = await _repositoryAccessor.HRMS_Att_Resign_Monthly_Detail
                .FindAll(x => x.Factory == param.Factory && wk_sql.Contains(x.USER_GUID), true).ToListAsync();

            var items = new[] { "A01", "B02", "B04", "B05", "C02" };
            var itemsType = new[] { "45", "42" };
            var itemAddedType = new[] { "A", "B" };
            var itemDedType = new[] { "C", "D" };

            // Dictionary cho tổng nhiều tháng
            var HSMDByUser = HSMD.GroupBy(x => x.USER_GUID).ToDictionary(g => g.Key, g => g.ToList());
            var HSRMDByUser = HSRMD.GroupBy(x => x.USER_GUID).ToDictionary(g => g.Key, g => g.ToList());

            // Dictionary cho từng tháng
            var HSMDByMonth = HSMD.GroupBy(x => new { x.USER_GUID, x.Sal_Month }).ToDictionary(g => g.Key, g => g.ToList());
            var HSRMDByMonth = HSRMD.GroupBy(x => new { x.USER_GUID, x.Sal_Month }).ToDictionary(g => g.Key, g => g.ToList());
            var HSAMByMonth = HSAM.GroupBy(x => new { x.USER_GUID, x.Sal_Month }).ToDictionary(g => g.Key, g => g.ToList());
            var HAMDByMonth = HAMD.GroupBy(x => new { x.USER_GUID, x.Att_Month }).ToDictionary(g => g.Key, g => g.ToList());
            var HARMDByMonth = HARMD.GroupBy(x => new { x.USER_GUID, x.Att_Month }).ToDictionary(g => g.Key, g => g.ToList());

            int y_amt = 0, pretax = 0;
            foreach (var userGuid in wk_sql)
            {
                var emp = HEP.Where(e => e.USER_GUID == userGuid)
                    .Select(e => new
                    {
                        e.Employee_ID,
                        e.Department,
                        e.Local_Full_Name,
                        e.Identification_Number,
                        e.USER_GUID
                    }).FirstOrDefault();

                var TaxNo = HSTN.Where(t => t.USER_GUID == userGuid && t.Year.Year <= YMEnd.Year)
                    .OrderByDescending(t => t.Year.Year)
                    .FirstOrDefault();
                int loan = 0;

                var tempRecord = new AnnualIncomeTaxDetailReport
                {
                    Employee_ID = emp?.Employee_ID,
                    Local_Full_Name = emp?.Local_Full_Name,
                    USER_GUID = userGuid,
                    Identification_Number = emp?.Identification_Number,
                    Department = emp?.Department,
                    Department_Name = departmentNames.FirstOrDefault(d => d.Key == emp?.Department).Value ?? string.Empty,
                    TaxNo = TaxNo?.TaxNo ?? "0",
                    Factory = param.Factory
                };

                for (var month = YMStart; month <= YMEnd; month = month.AddMonths(1))
                {
                    if (!months.Any(x => x.Month == month.ToString("yyyy/MM")))
                        months.Add(new AnnualIncomeTax_Column_Month(month.ToString("yyyy/MM")));

                    var TYearMonth = month;
                    int amt = 0, tax = 0, subqty = 0, ins_fee = 0, ovtm = 0, wage_t = 0, wage_h = 0, add_total = 0, ded_total = 0, sum_amt = 0, mon_cnt = 0;

                    var salTax = HST.FirstOrDefault(x => x.USER_GUID == userGuid && x.Sal_Month == TYearMonth);
                    if (salTax != null)
                    {
                        amt = salTax.Salary_Amt;
                        tax = salTax.Tax;
                        subqty = salTax.Num_Dependents;
                    }
                    mon_cnt = (amt == 0) ? 0 : 1;

                    if (amt <= 0) amt = 0;
                    if (tax <= 0) tax = 0;
                    if (subqty <= 0) subqty = 0;
                    int wk_allamt = y_amt + pretax + loan + amt + tax;

                    // Tổng nhiều tháng
                    int wk_allamt1 = HSMDByUser.TryGetValue(userGuid, out var hsmdListamt1)
                        ? hsmdListamt1.Where(d => d.Sal_Month.Date >= YMStart.Date
                            && d.Sal_Month.Date <= YMEnd.Date
                            && d.Type_Seq == "45"
                            && d.AddDed_Type == "A"
                            && items.Contains(d.Item))
                        .Sum(d => d.Amount)
                        : 0;

                    int wk_allamt2 = HSRMDByUser.TryGetValue(userGuid, out var hsrmdListamt2)
                        ? hsrmdListamt2.Where(d => d.Sal_Month.Date >= YMStart.Date
                            && d.Sal_Month.Date <= YMEnd.Date
                            && d.Type_Seq == "45"
                            && d.AddDed_Type == "A"
                            && items.Contains(d.Item))
                        .Sum(d => d.Amount)
                        : 0;

                    if ((wk_allamt + wk_allamt1 + wk_allamt2) == 0)
                    {
                        var annualIncomeTaxDetailNone = new AnnualIncomeTaxDetail_By_Month(month.ToString("yyyy/MM"), new AnnualIncomeTaxDetail_Pattent()
                        {
                            amt = 0,
                            tax = 0,
                            subqty = 0,
                            ins_fee = 0,
                            ovtm = 0,
                            wage_t = 0,
                            wage_h = 0,
                            sum_amt = 0,
                        });
                        tempRecord.Detail_By_Months.Add(annualIncomeTaxDetailNone);
                        continue;
                    }

                    // Truy vấn từng tháng
                    var hsmdList = HSMDByMonth.TryGetValue(new { USER_GUID = userGuid, Sal_Month = TYearMonth }, out var hsmd) ? hsmd : null;
                    var hsrmdList = HSRMDByMonth.TryGetValue(new { USER_GUID = userGuid, Sal_Month = TYearMonth }, out var hsrmd) ? hsrmd : null;
                    var hsamList = HSAMByMonth.TryGetValue(new { USER_GUID = userGuid, Sal_Month = TYearMonth }, out var hsam) ? hsam : null;
                    var hamdList = HAMDByMonth.TryGetValue(new { USER_GUID = userGuid, Att_Month = TYearMonth }, out var hamd) ? hamd : null;
                    var harmdList = HARMDByMonth.TryGetValue(new { USER_GUID = userGuid, Att_Month = TYearMonth }, out var harmd) ? harmd : null;

                    // ins_fee
                    int monthly_ins_fee = hsmdList?.Where(d => d.Type_Seq == "57").Sum(d => d.Amount) ?? 0;
                    int resign_ins_fee = hsrmdList?.Where(d => d.Type_Seq == "57").Sum(d => d.Amount) ?? 0;
                    ins_fee = monthly_ins_fee + resign_ins_fee;
                    if (ins_fee == 0) ins_fee = 0;
                    // Overtime
                    int monthly_ovtm = hsmdList?.Where(d => d.Type_Seq == "42" && d.AddDed_Type == "A" && d.Item == "A01").Sum(d => d.Amount) ?? 0;
                    int resign_ovtm = hsrmdList?.Where(d => d.Type_Seq == "42" && d.AddDed_Type == "A" && d.Item == "A01").Sum(d => d.Amount) ?? 0;
                    int a06Amt = hsamList?.Where(d => d.AddDed_Type == "A" && d.AddDed_Item == "A06").Select(d => d.Amount).FirstOrDefault() ?? 0;
                    ovtm = (monthly_ovtm + resign_ovtm + a06Amt) / 3;

                    // wage_t
                    wage_t = 0;
                    decimal attDaysOnJob = hamdList?.Where(x => x.Employee_ID == emp.Employee_ID && x.Leave_Type == "2" && x.Leave_Code == "A01").FirstOrDefault()?.Days ?? 0;
                    int sum_wage_t_onjob = hsmdList?.Where(x => x.Type_Seq == "42" && x.AddDed_Type == "A" && x.Item == "A03").Sum(x => x.Amount) ?? 0;
                    wage_t += attDaysOnJob <= 0
                        ? sum_wage_t_onjob * 100 / 210
                        : sum_wage_t_onjob * 110 / 210;

                    decimal attDaysResign = harmdList?.Where(x => x.Employee_ID == emp.Employee_ID && x.Leave_Type == "2" && x.Leave_Code == "A01").FirstOrDefault()?.Days ?? 0;
                    int sum_wage_t_resign = hsrmdList?.Where(x => x.Type_Seq == "42" && x.AddDed_Type == "A" && x.Item == "A03").Sum(x => x.Amount) ?? 0;
                    wage_t += attDaysResign <= 0
                        ? sum_wage_t_resign * 100 / 210
                        : sum_wage_t_resign * 110 / 210;

                    // wage_h
                    int sum_wageHMonthly = hsmdList?.Where(d => d.Type_Seq == "42" && d.AddDed_Type == "A" && d.Item == "C01").Sum(x => x.Amount) ?? 0;
                    int sum_wageHResign = hsrmdList?.Where(d => d.Type_Seq == "42" && d.AddDed_Type == "A" && d.Item == "C01").Sum(x => x.Amount) ?? 0;
                    wage_h = sum_wageHMonthly + sum_wageHResign;

                    // add_total
                    add_total =
                        (hsmdList?.Where(d => itemsType.Contains(d.Type_Seq)).Sum(d => d.Amount) ?? 0)
                        + (hsmdList?.Where(d => d.Type_Seq == "49" && itemAddedType.Contains(d.AddDed_Type)).Sum(d => d.Amount) ?? 0)
                        + (hsrmdList?.Where(d => itemsType.Contains(d.Type_Seq)).Sum(d => d.Amount) ?? 0)
                        + (hsrmdList?.Where(d => d.Type_Seq == "49" && itemAddedType.Contains(d.AddDed_Type)).Sum(d => d.Amount) ?? 0);
                    if (add_total < 0) add_total = 0;

                    // ded_total
                    ded_total =
                        (hsmdList?.Where(d => d.Type_Seq == "57").Sum(d => d.Amount) ?? 0)
                        + (hsmdList?.Where(d => d.Type_Seq == "49" && itemDedType.Contains(d.AddDed_Type)).Sum(d => d.Amount) ?? 0)
                        + (hsrmdList?.Where(d => d.Type_Seq == "57").Sum(d => d.Amount) ?? 0)
                        + (hsrmdList?.Where(d => d.Type_Seq == "49" && itemDedType.Contains(d.AddDed_Type)).Sum(d => d.Amount) ?? 0);
                    if (ded_total < 0) ded_total = 0;

                    sum_amt = add_total - ded_total;
                    if (sum_amt <= 0) sum_amt = 0;

                    var annualIncomeTaxDetail_by_month = new AnnualIncomeTaxDetail_By_Month(month.ToString("yyyy/MM"), new AnnualIncomeTaxDetail_Pattent()
                    {
                        amt = amt,
                        tax = tax,
                        subqty = subqty,
                        ins_fee = ins_fee,
                        ovtm = ovtm,
                        wage_t = wage_t,
                        wage_h = wage_h,
                        sum_amt = sum_amt,
                    });
                    tempRecord.mon_cnt = mon_cnt;
                    tempRecord.Detail_By_Months.Add(annualIncomeTaxDetail_by_month);
                }

                // Tính tổng các cột
                tempRecord.Sum_amt = tempRecord.Detail_By_Months.Sum(d => d.Detail_Pattent.amt);
                tempRecord.Sum_tax = tempRecord.Detail_By_Months.Sum(d => d.Detail_Pattent.tax);
                tempRecord.Sum_subqty = tempRecord.Detail_By_Months.Sum(d => d.Detail_Pattent.subqty);
                tempRecord.Sum_ovtm = tempRecord.Detail_By_Months.Sum(d => d.Detail_Pattent.ovtm);
                tempRecord.Sum_wage_t = tempRecord.Detail_By_Months.Sum(d => d.Detail_Pattent.wage_t);
                tempRecord.Sum_wage_h = tempRecord.Detail_By_Months.Sum(d => d.Detail_Pattent.wage_h);
                tempRecord.Sum_ins_fee = tempRecord.Detail_By_Months.Sum(d => d.Detail_Pattent.ins_fee);
                tempRecord.Sum_sum_amt = tempRecord.Detail_By_Months.Sum(d => d.Detail_Pattent.sum_amt);

                result.Add(tempRecord);
            }
            months.Add(new AnnualIncomeTax_Column_Month("年度合計Year Total"));
            return (result, months);
        }
        #endregion
        private static DateTime ToFirstDateOfMonth(DateTime dt) => new(dt.Year, dt.Month, 1);
        private static DateTime ToLastDateOfMonth(DateTime dt) => ToFirstDateOfMonth(dt.AddMonths(1)).AddDays(-1);
    }
}
