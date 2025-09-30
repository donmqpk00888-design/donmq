using API.Data;
using API._Services.Interfaces.SalaryMaintenance;
using API.DTOs.SalaryMaintenance;
using API.Helper.Constant;
using API.Models;
using Aspose.Cells;
using LinqKit;
using Microsoft.EntityFrameworkCore;


namespace API._Services.Services.SalaryMaintenance
{
    public class S_7_2_1_SalaryApprovalForm : BaseServices, I_7_2_1_SalaryApprovalForm
    {
        public S_7_2_1_SalaryApprovalForm(DBContext dbContext) : base(dbContext)
        {
        }
        #region Get Data
        public async Task<int> Search(D_7_2_1_SalaryApprovalForm_Param param)
        {
            var result = await GetData(param);
            return result.Count;
        }
        public async Task<List<D_7_2_1_SalaryApprovalForm_Data>> GetData(D_7_2_1_SalaryApprovalForm_Param param)
        {
            var result = new List<D_7_2_1_SalaryApprovalForm_Data>();

            var predEmpPersonal = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory && param.Permission_Group.Contains(x.Permission_Group));
            predEmpPersonal = param.Kind switch
            {
                "O" => predEmpPersonal.And(x => x.Resign_Date == null && x.Deletion_Code == "Y"),
                "R" => predEmpPersonal.And(x => x.Resign_Date != null),
                _ => predEmpPersonal
            };
            if (!string.IsNullOrWhiteSpace(param.Department))
                predEmpPersonal = predEmpPersonal.And(x => x.Department == param.Department);
            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                predEmpPersonal = predEmpPersonal.And(x => x.Employee_ID.Contains(param.Employee_ID));
            if (!string.IsNullOrWhiteSpace(param.Position_Title))
                predEmpPersonal = predEmpPersonal.And(x => x.Position_Title == param.Position_Title);

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predEmpPersonal, true).ToList();
            var HES = _repositoryAccessor.HRMS_Emp_Skill.FindAll(x => x.Factory == param.Factory, true).ToList();

            List<HRMS_Emp_Personal> wk_sql = HEP;

            List<string> empList = wk_sql.Select(x => x.Employee_ID).ToList();
            var HSH = _repositoryAccessor.HRMS_Sal_History.FindAll(x => empList.Contains(x.Employee_ID)).ToList();
            var HSM = _repositoryAccessor.HRMS_Sal_Master.FindAll(x => x.Factory == param.Factory && empList.Contains(x.Employee_ID)).ToList();
            var HSMD = _repositoryAccessor.HRMS_Sal_Master_Detail.FindAll(x => x.Factory == param.Factory && empList.Contains(x.Employee_ID)).ToList();
            var HSIS = _repositoryAccessor.HRMS_Sal_Item_Settings.FindAll(x => x.Factory == param.Factory && x.Effective_Month <= DateTime.Now, true).ToList();

            var listBasicCode = await GetDataBasicCode(BasicCodeTypeConstant.SalaryItem, param.Language);
            var listDepartment = await GetListDepartment(param.Language, param.Factory);
            var listSalaryType = await GetDataBasicCode(BasicCodeTypeConstant.SalaryType, param.Language);
            var listTechnicalType = await GetDataBasicCode(BasicCodeTypeConstant.Technical_Type, param.Language);
            var listExpertiseCategory = await GetDataBasicCode(BasicCodeTypeConstant.Expertise_Category, param.Language);
            var listPositionTitle = await GetDataBasicCode(BasicCodeTypeConstant.JobTitle, param.Language);

            foreach (var pr_str in wk_sql)
            {
                var Sal_History = HSH.Where(x => x.Employee_ID == pr_str.Employee_ID);
                var maxEffectiveDate = Sal_History.Any() ? Sal_History.Max(x => (DateTime?)x.Effective_Date) : null;

                var wpassdate1 = HES.Where(x => x.Division == pr_str.Division &&
                                                            x.Factory == pr_str.Factory &&
                                                            x.Employee_ID == pr_str.Employee_ID &&
                                                            x.Skill_Certification == "01")
                                                    .Min(x => (DateTime?)x.Passing_Date);

                var wpassdate2 = HES.Where(x => x.Division == pr_str.Division &&
                                            x.Factory == pr_str.Factory &&
                                            x.Employee_ID == pr_str.Employee_ID &&
                                            x.Skill_Certification == "02")
                                    .Min(x => (DateTime?)x.Passing_Date);

                var Sal_Master = HSM.FirstOrDefault(x => x.Employee_ID == pr_str.Employee_ID);

                var Sal_Detail_Temp = HSMD.FindAll(x => x.Employee_ID == pr_str.Employee_ID);

                // Sal_Setting_Temp với null check
                DateTime? maxEffectiveMonth = null;
                var Sal_Setting_Temp = new List<HRMS_Sal_Item_Settings>();

                if (HSIS != null && Sal_Master != null && !string.IsNullOrEmpty(Sal_Master.Salary_Type))
                {
                    var filteredSettings = HSIS.FindAll(x =>
                        x.Permission_Group == pr_str.Permission_Group &&
                        x.Salary_Type == Sal_Master.Salary_Type
                    );
                    if (filteredSettings.Any())
                    {
                        maxEffectiveMonth = filteredSettings.Max(x => (DateTime?)x.Effective_Month);
                        Sal_Setting_Temp = filteredSettings.FindAll(x => x.Effective_Month == maxEffectiveMonth);
                    }
                }

                var Sal_Detail_Values = new List<SalaryDetailValueDto>();
                if (Sal_Setting_Temp.Any())
                {                
                    Sal_Detail_Values = Sal_Setting_Temp
                        .GroupJoin(Sal_Detail_Temp,
                            x => x.Salary_Item,
                            y => y.Salary_Item,
                            (x, SalaryDetail) => new { x, SalaryDetail })
                        .SelectMany(x => x.SalaryDetail.DefaultIfEmpty(),
                            (x, s) => new SalaryDetailValueDto
                            {
                                USER_GUID = s?.USER_GUID,
                                Division = s?.Division,
                                Factory = s?.Factory,
                                Employee_ID = s?.Employee_ID,
                                SalaryItem = x.x.Salary_Item,
                                SalaryItem_Name = listBasicCode.FirstOrDefault(a => a.Key == x.x.Salary_Item),
                                Amount = s != null ? s.Amount : 0,
                                Update_By = s?.Update_By,
                                Update_Time = s?.Update_Time,
                                Seq = x.x.Seq
                            })
                        .OrderBy(x => x.Seq).ToList();
                }

                // 計算工齡
                var data = new D_7_2_1_SalaryApprovalForm_Data
                {
                    Local_Full_Name = pr_str.Local_Full_Name,
                    Employee_ID = pr_str.Employee_ID,
                    Onboard_Date = pr_str.Onboard_Date,
                    Effective_Date = null,
                    WPassDate1 = wpassdate1,
                    WPassDate2 = wpassdate2,
                    Previous_Adjustment_Date = maxEffectiveDate,
                    Department_Name = listDepartment.FirstOrDefault(m => m.Key == pr_str.Department).Value ?? pr_str.Department ?? "",
                    SalaryGrade_SalaryLevel = Sal_Master != null ? $"{Sal_Master?.Salary_Grade}/{Sal_Master?.Salary_Level}" : "",
                    Salary_Type_Name = listSalaryType.FirstOrDefault(s => s.Key == Sal_Master?.Salary_Type).Value ?? Sal_Master?.Salary_Type ?? "",
                    Technical_Type_Name = listTechnicalType.FirstOrDefault(t => t.Key == Sal_Master?.Technical_Type).Value ?? Sal_Master?.Technical_Type ?? "",
                    Expertise_Category_Name = listExpertiseCategory.FirstOrDefault(e => e.Key == Sal_Master?.Expertise_Category).Value ?? Sal_Master?.Expertise_Category ?? "",
                    SalaryDetailValues = Sal_Detail_Values
                };

                result.Add(data);
            }
            return result.OrderBy(e => e.Department).ThenBy(e => e.Employee_ID).ToList();
        }
        #endregion
        #region PDF
        public async Task<OperationResult> ExportPDF(D_7_2_1_SalaryApprovalForm_Param param, string userName)
        {
            var data = await GetData(param);


            if (!data.Any())
                return new OperationResult(false, "No data for PDF export");

            try
            {
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources\\Template\\SalaryReport\\7_2_1_SalaryApprovalForm\\Download.xlsx");
                var templateWorkbook = new Workbook(templatePath);

                var outputWorkbook = new Workbook();

                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];

                    // Tạo worksheet mới cho mỗi người
                    Worksheet ws;
                    if (i == 0)
                    {
                        ws = outputWorkbook.Worksheets[0];
                        ws.Copy(templateWorkbook.Worksheets[0]);
                        ws.Name = $"Sheet{i + 1}";
                    }
                    else
                    {
                        int idx = outputWorkbook.Worksheets.Add();
                        ws = outputWorkbook.Worksheets[idx];
                        ws.Copy(templateWorkbook.Worksheets[0]);
                        ws.Name = $"Sheet{i + 1}";
                    }


                    // Ghi thông tin cơ bản
                    ws.Cells["B2"].PutValue(item.Employee_ID);
                    ws.Cells["F2"].PutValue(item.Local_Full_Name);
                    ws.Cells["B3"].PutValue(item.Onboard_Date.HasValue ? item.Onboard_Date.Value.ToString("yyyy/MM/dd") : string.Empty);
                    ws.Cells["F3"].PutValue(item.Effective_Date.HasValue ? item.Effective_Date.Value.ToString("yyyy/MM/dd") : string.Empty);
                    ws.Cells["B4"].PutValue(item.WPassDate1.HasValue ? item.WPassDate1.Value.ToString("yyyy/MM/dd") : string.Empty);
                    ws.Cells["F4"].PutValue(item.WPassDate2.HasValue ? item.WPassDate2.Value.ToString("yyyy/MM/dd") : string.Empty);
                    ws.Cells["B6"].PutValue(userName);
                    ws.Cells["F6"].PutValue(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));

                    ws.Cells["C9"].PutValue(item.Previous_Adjustment_Date);
                    ws.Cells["C10"].PutValue(item.Department_Name);
                    ws.Cells["C11"].PutValue(item.SalaryGrade_SalaryLevel);
                    ws.Cells["C12"].PutValue(item.Salary_Type_Name);
                    ws.Cells["C13"].PutValue(item.Technical_Type_Name);
                    ws.Cells["C14"].PutValue(item.Expertise_Category_Name);

                    int detailStartRow = 15;
                    int currentRow = detailStartRow;

                    foreach (var detail in item.SalaryDetailValues)
                    {
                        // Ghi dòng Amount
                        ws.Cells[$"A{currentRow}"].PutValue(detail.SalaryItem_Name.Value);
                        ws.Cells[$"C{currentRow}"].PutValue(detail.Amount);
                        currentRow++;
                    }

                    // Ghi dòng tổng
                    ws.Cells[$"A{currentRow}"].PutValue("合計 Total");
                    ws.Cells[$"C{currentRow}"].PutValue(item.SalaryDetailValues.Sum(x => x.Amount));
                    currentRow++;

                    ws.Cells.SetRowHeight(currentRow, 50);
                    ws.Cells.Merge(currentRow, 2, 1, 2);
                    Style signStyle = outputWorkbook.CreateStyle();
                    signStyle.IsTextWrapped = true;
                    signStyle.HorizontalAlignment = TextAlignmentType.Center;
                    signStyle.VerticalAlignment = TextAlignmentType.Center;
                    signStyle.Font.Size = 12;
                    signStyle.Font.Name = "新細明體 (Body Asian)";

                    ws.Cells[$"A{currentRow + 1}"].PutValue("核決:\nApproved by:");
                    ws.Cells[$"A{currentRow + 1}"].SetStyle(signStyle);

                    ws.Cells[$"C{currentRow + 1}"].PutValue("審核:\nChecked by:");
                    ws.Cells[$"C{currentRow + 1}"].SetStyle(signStyle);

                    ws.Cells[$"E{currentRow + 1}"].PutValue("製表:\nApplicant by:");
                    ws.Cells[$"E{currentRow + 1}"].SetStyle(signStyle);

                    // Định dạng số và căn trái
                    var wbTemp = new Workbook();
                    Style amountStyle = wbTemp.CreateStyle();
                    amountStyle.HorizontalAlignment = TextAlignmentType.Left;
                    amountStyle.VerticalAlignment = TextAlignmentType.Center;
                    amountStyle.Number = 3; // #,##0
                    StyleFlag flag = new() { NumberFormat = true, HorizontalAlignment = true, VerticalAlignment = true };

                    for (int row = detailStartRow; row < currentRow; row++)
                    {
                        ws.Cells[$"C{row}"].SetStyle(amountStyle, flag);
                        ws.Cells[$"A{row}"].SetStyle(amountStyle, flag);
                        ws.Cells.Merge(row - 1, 2, 1, 2);
                        ws.Cells.SetRowHeight(row - 1, 35);
                    }

                    // Tạo border cho toàn bộ vùng
                    string endColumn = "G";
                    var range = ws.Cells.CreateRange($"A{detailStartRow - 1}:{endColumn}{currentRow - 1}");
                    Style borderStyle = outputWorkbook.CreateStyle();
                    borderStyle.SetAllBorders();

                    StyleFlag borderFlag = new() { Borders = true };
                    range.ApplyStyle(borderStyle, borderFlag);

                }
                foreach (Worksheet sheet in outputWorkbook.Worksheets)
                {
                    sheet.PageSetup.PaperSize = PaperSizeType.PaperA4;
                    sheet.PageSetup.Orientation = PageOrientationType.Portrait;
                    sheet.PageSetup.FitToPagesWide = 1;
                    sheet.PageSetup.FitToPagesTall = 10;
                }

                using var stream = new MemoryStream();

                var pdfOptions = new PdfSaveOptions { };

                outputWorkbook.Save(stream, pdfOptions);

                var downloadResult = new
                {
                    fileData = stream.ToArray(),
                    totalRows = data.Count
                };
                return new OperationResult(true, downloadResult);
            }
            catch (Exception ex)
            {
                return new OperationResult(false, ex.InnerException?.Message ?? ex.Message);
            }
        }
        #endregion

        #region Get List
        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string language, string userName)
        {
            var predHBC = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.Factory);

            var factorys = await Queryt_Factory_AddList(userName);
            predHBC.And(x => factorys.Contains(x.Code));

            var data = await _repositoryAccessor.HRMS_Basic_Code.FindAll(predHBC, true)
                        .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language
                        .FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                           x => new { x.Type_Seq, x.Code },
                           y => new { y.Type_Seq, y.Code },
                           (x, y) => new { HBC = x, HBCL = y }
                        ).SelectMany(x => x.HBCL.DefaultIfEmpty(),
                            (x, y) => new { x.HBC, HBCL = y }
                        ).Select(x => new KeyValuePair<string, string>(
                            x.HBC.Code.Trim(),
                            x.HBC.Code.Trim() + " - " + (x.HBCL != null ? x.HBCL.Code_Name.Trim() : x.HBC.Code_Name.Trim())
                        )).Distinct().ToListAsync();
            return data;
        }
        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string language, string factory)
        {
            var result = await _repositoryAccessor.HRMS_Org_Department
                .FindAll(x => x.Factory == factory, true)
                .Join(_repositoryAccessor.HRMS_Basic_Factory_Comparison
                .FindAll(b => b.Kind == "1" && b.Factory == factory, true),
                    x => x.Division,
                    y => y.Division,
                    (x, y) => x)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    x => new { x.Factory, x.Department_Code },
                    y => new { y.Factory, y.Department_Code },
                    (x, y) => new { HOD = x, HODL = y })
                .SelectMany(
                    x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, HODL = y })
                .OrderBy(x => x.HOD.Department_Code)
                .Select(
                    x => new KeyValuePair<string, string>(
                        x.HOD.Department_Code,
                        x.HOD.Department_Code.Trim() + " - " + (x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)
                    )
                ).Distinct().ToListAsync();
            return result;
        }
        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language)
        {
            var permissionGroups = await Query_Permission_List(factory);
            var permissionGroupsWithLanguage = await _repositoryAccessor.HRMS_Basic_Code
                            .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup &&
                                        permissionGroups.Select(x => x.Permission_Group).Contains(x.Code), true)
                            .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language
                            .FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                                x => new { x.Type_Seq, x.Code },
                                y => new { y.Type_Seq, y.Code },
                                (HBC, HBCL) => new { HBC, HBCL })
                            .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                                (x, y) => new { x.HBC, HBCL = y })
                            .Select(x => new KeyValuePair<string, string>(x.HBC.Code,
                            $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}")).ToListAsync();
            return permissionGroupsWithLanguage;
        }
        public async Task<List<KeyValuePair<string, string>>> GetPositionTitles(string language)
        {
            return await GetDataBasicCode(BasicCodeTypeConstant.JobTitle, language);
        }
        #endregion
    }
}