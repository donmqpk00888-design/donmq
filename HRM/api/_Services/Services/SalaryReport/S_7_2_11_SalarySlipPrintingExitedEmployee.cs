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
    public class S_7_2_11_SalarySlipPrintingExitedEmployee : BaseServices, I_7_2_11_SalarySlipPrintingExitedEmployee
    {
        public S_7_2_11_SalarySlipPrintingExitedEmployee(DBContext dbContext) : base(dbContext) { }
        public async Task<OperationResult> GetTotalRows(SalarySlipPrintingExitedEmployeeParam param)
        {
            var result = await GetData(param, true);
            if (!result.IsSuccess)
                return result;
            var data = (List<HRMS_Emp_Personal>)result.Data;

            return new OperationResult(true, data.Count);
        }
        #region ExportPDF
        public async Task<OperationResult> ExportPDF(SalarySlipPrintingExitedEmployeeParam param)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;
            var type_Seq = new List<string>()
            {
                BasicCodeTypeConstant.SalaryItem,
                BasicCodeTypeConstant.JobTitle,
                BasicCodeTypeConstant.ReasonResignation,
                BasicCodeTypeConstant.AdditionsAndDeductionsItem,
                BasicCodeTypeConstant.Allowance,
                BasicCodeTypeConstant.InsuranceType,
                BasicCodeTypeConstant.Leave,
            };
            var HBC = _repositoryAccessor.HRMS_Basic_Code
                            .FindAll(x => type_Seq.Contains(x.Type_Seq));

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
                    Code_Name = $"{x.HBC.Code}-{(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                })
                .ToListAsync();

            #region Resignation system payment slip PDF column
            if (param.Kind == "Resignation")
            {
                try
                {
                    var getData = (SalarySlipPrintingExitedEmployeeDTO)result.Data;
                    var data = getData.ResignationSystemPaymentSlip;
                    if (!data.Any())
                        return new OperationResult(false, "No data for PDF export");

                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources\\Template\\SalaryReport\\7_2_11_SalarySlipPrintingExitedEmployee\\DownloadResignationSystemPaymentSlip.xlsx");
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

                        Style styleAmount = outputWorkbook.CreateStyle();
                        styleAmount.Custom = "#,##0";
                        styleAmount.Font.Size = 12;
                        styleAmount.Font.Name = "新細明體 (Body Asian)";
                        ws.Cells["A1"].PutValue(item.FactoryHeader);
                        ws.Cells["A2"].PutValue(GetLabel("Resignation", param.Language));
                        ws.Cells["A3"].PutValue(GetLabel("Department", param.Language));
                        ws.Cells["A4"].PutValue(GetLabel("Employee_ID", param.Language));
                        ws.Cells["A5"].PutValue(GetLabel("Local_Full_Name", param.Language));
                        ws.Cells["A6"].PutValue(GetLabel("Resign_Date", param.Language));
                        ws.Cells["D5"].PutValue(GetLabel("Position_Title", param.Language));
                        ws.Cells["D6"].PutValue(GetLabel("Resign_Reason", param.Language));
                        ws.Cells["F3"].PutValue(GetLabel("Year_Month", param.Language));
                        ws.Cells["F4"].PutValue(GetLabel("Print_Date", param.Language));
                        ws.Cells["F5"].PutValue(GetLabel("Onboard_Date", param.Language));

                        ws.Cells["B3"].PutValue(item.Department);
                        ws.Cells["B4"].PutValue(item.EmployeeID);
                        ws.Cells["B5"].PutValue(item.LocalFullName);
                        ws.Cells["B6"].PutValue(item.ResignDate);
                        ws.Cells["E5"].PutValue(BasicCodeLanguage.FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.JobTitle && x.Code == item.PositionTitle)?.Code_Name);
                        ws.Cells["E6"].PutValue(BasicCodeLanguage.FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.ReasonResignation && x.Code == item.ResignReason)?.Code_Name);
                        ws.Cells["G3"].PutValue(item.YearMonth);
                        ws.Cells["G4"].PutValue(item.PrintDate);
                        ws.Cells["G5"].PutValue(item.OnboardDate);

                        Style styleTitle = outputWorkbook.CreateStyle();
                        styleTitle.Font.IsBold = true;
                        styleTitle.Font.Size = 12;
                        ws.Cells["A8"].PutValue(GetLabel("Benefits", param.Language));
                        ws.Cells["A8"].SetStyle(styleTitle);
                        ws.Cells["A9"].PutValue(GetLabel("LSalary", param.Language));
                        ws.Cells["A10"].PutValue(GetLabel("TSalary", param.Language));

                        ws.Cells["C9"].PutValue(item.LastMonthSalary);
                        ws.Cells["C9"].SetStyle(styleAmount);
                        ws.Cells["C10"].PutValue(item.ThisMonthSalary);
                        ws.Cells["C10"].SetStyle(styleAmount);

                        int currentRow = 11;

                        foreach (var itemAddList in item.Addlist)
                        {
                            if (itemAddList == null) continue;
                            ws.Cells[$"A{currentRow}"].PutValue(BasicCodeLanguage.FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsItem && x.Code == itemAddList.Item)?.Code_Name);
                            ws.Cells[$"C{currentRow}"].PutValue(itemAddList?.Amount);
                            ws.Cells[$"C{currentRow}"].SetStyle(styleAmount);
                            currentRow++;
                        }

                        ws.Cells[$"A{currentRow}"].PutValue(GetLabel("Benefits_Total", param.Language));
                        ws.Cells[$"C{currentRow}"].PutValue(item.BenefitsTotal);
                        ws.Cells[$"C{currentRow}"].SetStyle(styleAmount);
                        currentRow++;


                        ws.Cells[$"A{currentRow}"].PutValue(GetLabel("Obligations", param.Language));
                        ws.Cells[$"A{currentRow}"].SetStyle(styleTitle);
                        currentRow++;

                        foreach (var itemDedList in item.Dedlist)
                        {
                            if (itemDedList == null) continue;
                            ws.Cells[$"A{currentRow}"].PutValue(BasicCodeLanguage.FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsItem && x.Code == itemDedList?.Item)?.Code_Name);
                            ws.Cells[$"C{currentRow}"].PutValue(itemDedList?.Amount);
                            ws.Cells[$"C{currentRow}"].SetStyle(styleAmount);
                            currentRow++;
                        }

                        ws.Cells[$"A{currentRow}"].PutValue(GetLabel("Obligations_Total", param.Language));
                        ws.Cells[$"C{currentRow}"].PutValue(item.ObligationsTotal);
                        ws.Cells[$"C{currentRow}"].SetStyle(styleAmount);
                        currentRow++;
                        ws.Cells[$"A{currentRow}"].PutValue(GetLabel("Net_Amount_Received", param.Language));
                        ws.Cells[$"C{currentRow}"].PutValue(item.NETAmountReceived);
                        ws.Cells[$"C{currentRow}"].SetStyle(styleAmount);
                        currentRow++;

                        Style signStyle = outputWorkbook.CreateStyle();
                        signStyle.Font.IsBold = true;
                        signStyle.Font.Size = 12;
                        signStyle.Font.Name = "新細明體 (Body Asian)";

                        ws.Cells[$"A{currentRow + 1}"].PutValue(GetLabel("Resigner", param.Language));
                        ws.Cells[$"A{currentRow + 1}"].SetStyle(signStyle);

                        ws.Cells[$"D{currentRow + 1}"].PutValue(GetLabel("HR_Dept", param.Language));
                        ws.Cells[$"D{currentRow + 1}"].SetStyle(signStyle);

                        ws.Cells[$"F{currentRow + 1}"].PutValue(GetLabel("FIN_Accounting", param.Language));
                        ws.Cells[$"F{currentRow + 1}"].SetStyle(signStyle);

                    }
                    foreach (Worksheet sheet in outputWorkbook.Worksheets)
                    {
                        sheet.PageSetup.PaperSize = PaperSizeType.PaperA5;
                        sheet.PageSetup.Orientation = PageOrientationType.Landscape;
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
            #region Salary slip PDF column
            else if (param.Kind == "Salary")
            {
                try
                {
                    var getData = (SalarySlipPrintingExitedEmployeeDTO)result.Data;
                    var data = getData.SalarySlip;
                    if (!data.Any())
                        return new OperationResult(false, "No data for PDF export");

                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources\\Template\\SalaryReport\\7_2_11_SalarySlipPrintingExitedEmployee\\DownloadSalarySlip.xlsx");
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

                        Style styleFont = outputWorkbook.CreateStyle();
                        styleFont.Font.Name = "新細明體 (Body Asian)";
                        ws.Cells["A1"].PutValue(item.FactoryHeader);
                        ws.Cells["A2"].PutValue(GetLabel("Department", param.Language));
                        ws.Cells["B2"].PutValue(item.Department);
                        ws.Cells["A3"].PutValue(GetLabel("Employee_ID", param.Language));
                        ws.Cells["B3"].PutValue(item.EmployeeID);
                        ws.Cells["A4"].PutValue(GetLabel("Local_Full_Name", param.Language));
                        ws.Cells["B4"].PutValue(item.LocalFullName);
                        ws.Cells["F2"].PutValue(GetLabel("Year_Month", param.Language));
                        ws.Cells["I2"].PutValue(item.YearMonth);
                        ws.Cells["F3"].PutValue(GetLabel("Position_Title", param.Language));
                        ws.Cells["I3"].PutValue(BasicCodeLanguage.FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.JobTitle && x.Code == item?.PositionTitle)?.Code_Name);

                        ws.Cells["F4"].PutValue(GetLabel("Onboard_Date", param.Language));
                        ws.Cells["I4"].PutValue(item.OnboardDate);

                        Style styleTitle = outputWorkbook.CreateStyle();
                        styleTitle.Font.IsBold = true;
                        styleTitle.Font.Size = 9;
                        styleTitle.Font.Name = "新細明體 (Body Asian)";
                        // Monthly_Positive_Items
                        ws.Cells["A6"].PutValue(GetLabel("Monthly_Positive_Items", param.Language));
                        ws.Cells["A6"].SetStyle(styleTitle);
                        int currentColum = 0;
                        int currentRow = 6;

                        Style styleAmount = outputWorkbook.CreateStyle();
                        styleAmount.Custom = "#,##0";
                        styleAmount.Font.Name = "新細明體 (Body Asian)";

                        foreach (var itemSI in item.SalaryItem)
                        {
                            if (itemSI == null) continue;
                            ws.Cells[currentRow, currentColum].PutValue(BasicCodeLanguage.FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.SalaryItem && x.Code == itemSI?.Item)?.Code_Name);
                            ws.Cells[currentRow, (currentColum + 2)].PutValue(itemSI?.Amount);
                            ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                            currentRow++;
                            CheckAndResetRow(ref currentRow, ref currentColum);
                        }
                        foreach (var itemOI in item.OverTimeItem)
                        {
                            if (itemOI == null) continue;
                            ws.Cells[currentRow, currentColum].PutValue(BasicCodeLanguage.FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.Allowance && x.Code == itemOI?.Item)?.Code_Name);
                            ws.Cells[currentRow, (currentColum + 2)].PutValue(itemOI?.Amount);
                            ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                            currentRow++;
                            CheckAndResetRow(ref currentRow, ref currentColum);
                        }
                        foreach (var itemAI in item.AddItemList)
                        {
                            if (itemAI == null) continue;
                            ws.Cells[currentRow, currentColum].PutValue(BasicCodeLanguage.FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsItem && x.Code == itemAI?.Item)?.Code_Name);
                            ws.Cells[currentRow, (currentColum + 2)].PutValue(itemAI?.Amount);
                            ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                            currentRow++;
                            CheckAndResetRow(ref currentRow, ref currentColum);
                        }
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Meal_Total", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.MealTotal);
                        ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Other_Additions", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.OtherAdditions);
                        ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Total_Addition_Item", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.TotalAdditionItem);
                        ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        // Monthly_Deductions
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Monthly_Deductions", param.Language));
                        ws.Cells[currentRow, currentColum].SetStyle(styleTitle);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Loaned_Amount", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.LoanedAmount);
                        ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        foreach (var itemID in item.InsuranceDeduction)
                        {
                            if (itemID == null) continue;
                            ws.Cells[currentRow, currentColum].PutValue(BasicCodeLanguage.FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.InsuranceType && x.Code == itemID?.Item)?.Code_Name);
                            ws.Cells[currentRow, (currentColum + 2)].PutValue(itemID?.Amount);
                            ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                            currentRow++;
                            CheckAndResetRow(ref currentRow, ref currentColum);
                        }
                        foreach (var itemDI in item.DedItemList)
                        {
                            if (itemDI == null) continue;
                            ws.Cells[currentRow, currentColum].PutValue(BasicCodeLanguage.FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsItem && x.Code == itemDI?.Item)?.Code_Name);
                            ws.Cells[currentRow, (currentColum + 2)].PutValue(itemDI?.Amount);
                            ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                            currentRow++;
                            CheckAndResetRow(ref currentRow, ref currentColum);
                        }
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Tax", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.Tax);
                        ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Other_Deductions", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.OtherDeductions);
                        ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Total_Deduction_Item", param.Language));
                        ws.Cells[currentRow, currentColum].SetStyle(styleTitle);
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.TotalDeductionItem);
                        ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Net_Amount_Received", param.Language));
                        ws.Cells[currentRow, currentColum].SetStyle(styleTitle);
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.NETAmountReceived);
                        ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Cumulative_Overtime_Hours_YTD", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.CumulativeOvertimeHoursYTD);
                        ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Total_Overtime_Hours_Current_Month", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.TotalOvertimeHours_CurrentMonth);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        foreach (var itemOH in item.OvertimeHours)
                        {
                            if (itemOH == null) continue;
                            ws.Cells[currentRow, currentColum].PutValue(BasicCodeLanguage.FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.Allowance && x.Code == itemOH?.Item)?.Code_Name);
                            ws.Cells[currentRow, (currentColum + 2)].PutValue(itemOH?.Days);
                            currentRow++;
                            CheckAndResetRow(ref currentRow, ref currentColum);
                        }
                        // Attendance
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Attendance", param.Language));
                        ws.Cells[currentRow, currentColum].SetStyle(styleTitle);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Actual_Work_Days", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.ActualWorkDays);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Delay_Early_Times", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.DelayEarlyTimes);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        foreach (var itemLD in item.LeaveDays)
                        {
                            if (itemLD == null) continue;
                            ws.Cells[currentRow, currentColum].PutValue(BasicCodeLanguage.FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.Leave && x.Code == itemLD?.Item)?.Code_Name);
                            ws.Cells[currentRow, (currentColum + 2)].PutValue(itemLD?.Days);
                            currentRow++;
                            CheckAndResetRow(ref currentRow, ref currentColum);
                        }
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Standard_Annual_Leave_Days", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.StandardAnnualLeaveDays);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Annual_Leave_Entitlement_This_Year", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.AnnualLeaveEntitlement_ThisYear);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Cumulative_Annual_Leave", param.Language));
                        ws.Cells[currentRow, currentColum].SetStyle(styleTitle);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Cumulative_Annual_Leave_Taken", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.CumulativeAnnualLeaveTaken);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Cumulative_Annual_Leave_Remaining_Split", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.CumulativeAnnualLeaveRemaining);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Cumulative_Annual_Leave_Remaining_Total", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.CumulativeAnnualLeaveRemainingTotal);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Total_Paid_Days", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.TotalPaidDays);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Day_Shift_Meal_Times", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.DayShiftMealTimes);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Overtime_Meal_Times", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.OvertimeMealTimes);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Night_Shift_Allowance_Times", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.NightShiftAllowanceTimes);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Night_Shift_Meal_Times", param.Language));
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.NightShiftMealTimes);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Salary_And_Allowance", param.Language));
                        ws.Cells[currentRow, currentColum].SetStyle(styleTitle);
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.SalaryAllowance);
                        ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                        ws.Cells[currentRow, currentColum].PutValue(GetLabel("Hourly_Wage", param.Language));
                        ws.Cells[currentRow, currentColum].SetStyle(styleTitle);
                        ws.Cells[currentRow, (currentColum + 2)].PutValue(item.HourlyWage);
                        ws.Cells[currentRow, (currentColum + 2)].SetStyle(styleAmount);
                        currentRow++;
                        CheckAndResetRow(ref currentRow, ref currentColum);
                    }
                    foreach (Worksheet sheet in outputWorkbook.Worksheets)
                    {
                        var pageSetup = sheet.PageSetup;
                        pageSetup.Orientation = PageOrientationType.Landscape;

                        // Thiết lập khổ giấy tùy chỉnh
                        // Đơn vị trong Aspose.Cells là inch (1 inch = 2.54 cm)
                        double widthInInches = 20.5 / 2.54;
                        double heightInInches = 21.5 / 2.54;

                        pageSetup.CustomPaperSize(widthInInches, heightInInches);

                        // Tùy chỉnh co trang in
                        pageSetup.FitToPagesWide = 1;
                        pageSetup.FitToPagesTall = 10;
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
                #endregion
            }
            return new OperationResult(false);
        }
        #endregion
        #region GetData
        private async Task<OperationResult> GetData(SalarySlipPrintingExitedEmployeeParam param, bool countOnly = false)
        {
            if (string.IsNullOrWhiteSpace(param.Factory)
                || !param.Permission_Group.Any()
                || string.IsNullOrWhiteSpace(param.Language)
                || string.IsNullOrWhiteSpace(param.Year_Month)
                || !DateTime.TryParseExact(param.Year_Month, "yyyy/MM", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime yearMonth))
                return new OperationResult(false, "SalaryReport.TaxPayingEmployeeMonthlyNightShiftExtraPayAndOvertimePay.InvalidInput");

            var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory
                && param.Permission_Group.Contains(x.Permission_Group));

            if (!string.IsNullOrWhiteSpace(param.Department))
                predHEP.And(x => x.Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.EmployeeID))
                predHEP.And(x => x.Employee_ID.Contains(param.EmployeeID));

            if (!string.IsNullOrWhiteSpace(param.StartDate) && !string.IsNullOrWhiteSpace(param.EndDate))
            {
                var start_Date = DateTime.Parse(param.StartDate);
                var end_Date = DateTime.Parse(param.EndDate);
                predHEP.And(x => x.Resign_Date.Value.Date >= start_Date.Date && x.Resign_Date.Value.Date <= end_Date.Date);
            }

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true);
            var HSC = _repositoryAccessor.HRMS_Sal_Close.FindAll(x => x.Factory == param.Factory && x.Sal_Month == yearMonth && x.Close_Status == "Y", true);
            var HSRM = _repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(x => x.Factory == param.Factory && x.Sal_Month == yearMonth && !HSC.Select(x => x.Employee_ID).Contains(x.Employee_ID), true);

            var wk_sql = await HEP
                .Join(HSRM,
                    x => x.Employee_ID,
                    y => y.Employee_ID,
                    (x, y) => new { HEP = x, HSRM = y })
                .OrderBy(x => x.HSRM.Employee_ID)
                .ToListAsync();
            SalarySlipPrintingExitedEmployeeDTO data = new() { };
            List<ResignationSystemPaymentSlip> resignationSystemPaymentSlip = new();
            List<SalarySlip> salarySlip = new();

            if (countOnly == true)
                return new OperationResult(true, wk_sql.Select(item => item.HEP).ToList());
            if (!wk_sql.Any())
            {
                data.ResignationSystemPaymentSlip = resignationSystemPaymentSlip;
                data.SalarySlip = salarySlip;
                return new OperationResult(true, data);
            }

            var yearStart = new DateTime(yearMonth.Year, 1, 1);
            var yearEnd = new DateTime(yearMonth.Year, 12, 31);
            var factoryHeader = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.FactoryHeader, true).ToListAsync();
            var listDepartments = await GetDepartmentName(param.Factory, param.Language);

            var listEmployeeID = wk_sql.Select(x => x.HSRM.Employee_ID).ToList();
            var listSalaryType = wk_sql.Select(x => x.HSRM.Salary_Type).ToList();
            var listPermissionGroup = wk_sql.Select(x => x.HSRM.Permission_Group).ToList();

            var HSAS = _repositoryAccessor.HRMS_Sal_AddDedItem_Settings.FindAll(x => x.Factory == param.Factory, true).ToList();
            var HSMonthlyD = _repositoryAccessor.HRMS_Sal_Monthly_Detail.FindAll(x => x.Factory == param.Factory && listEmployeeID.Contains(x.Employee_ID), true).ToList();
            var HSRMD = _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail.FindAll(x => x.Factory == param.Factory && listEmployeeID.Contains(x.Employee_ID), true).ToList();
            var HSPMD = _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail.FindAll(x => x.Factory == param.Factory && listEmployeeID.Contains(x.Employee_ID), true).ToList();
            // 5. && 12. Effective_Month
            var effectiveMonth = HSAS.Where(x => x.Effective_Month <= yearMonth).Max(x => (DateTime?)x.Effective_Month);
            var listQueryAddSumN = Query_Sal_Monthly_Detail_Add_Sum("N", yearMonth, listEmployeeID, HSMonthlyD, HSRMD, HSPMD);
            var listQueryDedSumN = Query_Sal_Monthly_Detail_Ded_Sum("N", yearMonth, listEmployeeID, HSMonthlyD, HSRMD, HSPMD);
            #region Kind = Resignation
            if (param.Kind == "Resignation")
            {
                var printDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                var HSM = _repositoryAccessor.HRMS_Sal_Monthly.FindAll(x => x.Factory == param.Factory && listEmployeeID.Contains(x.Employee_ID), true);
                var HSAM = _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.FindAll(x => x.Factory == param.Factory && x.Sal_Month == yearMonth && listEmployeeID.Contains(x.Employee_ID), true).ToList();
                var listQueryAddSumY = Query_Sal_Monthly_Detail_Add_Sum("Y", yearMonth.AddMonths(-1), listEmployeeID, HSMonthlyD, HSRMD, HSPMD);
                var listQueryDedSumY = Query_Sal_Monthly_Detail_Ded_Sum("Y", yearMonth.AddMonths(-1), listEmployeeID, HSMonthlyD, HSRMD, HSPMD);

                foreach (var item in wk_sql)
                {
                    // 3. Last Month Salary
                    var wk_addL = listQueryAddSumY.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Total ?? 0;
                    var wk_subL = listQueryDedSumY.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Total ?? 0;
                    var itemHSM = HSM.FirstOrDefault(x => x.Sal_Month == item.HSRM.Sal_Month.AddMonths(-1)
                        && x.Employee_ID == item.HSRM.Employee_ID);
                    var taxL = itemHSM != null ? itemHSM.Tax : 0;
                    var LSalary = wk_addL - wk_subL - taxL;
                    // 4. This Month Salary
                    var wk_addT = listQueryAddSumN.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Total ?? 0;
                    var wk_subT = listQueryDedSumN.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Total ?? 0;
                    var itemHSRM = HSRM.FirstOrDefault(x => x.Sal_Month == item.HSRM.Sal_Month
                        && x.Employee_ID == item.HSRM.Employee_ID);
                    var taxT = itemHSRM != null ? itemHSRM.Tax : 0;
                    var TSalary = wk_addT - wk_subT - taxT;
                    // 6. Benefit addlist
                    var benefit = HSAS
                        .Where(x => x.Permission_Group == item.HSRM.Permission_Group
                            && x.Salary_Type == item.HSRM.Salary_Type
                            && x.Effective_Month == effectiveMonth
                            && (x.AddDed_Item.StartsWith("A") || x.AddDed_Item.StartsWith("B"))
                            && x.Resigned_Print == "Y")
                        .OrderBy(x => x.AddDed_Item)
                        .Select(x => x.AddDed_Item)
                        .ToList();
                    var addListAmt = HSAM
                        .Where(x => x.Employee_ID == item.HSRM.Employee_ID
                            && benefit.Contains(x.AddDed_Item))
                        .Select(x => new Sal_Monthly_Detail_Temp_7_2_11
                        {
                            Employee_ID = x.Employee_ID,
                            Item = x.AddDed_Item,
                            Amount = x.Amount
                        })
                        .ToList();

                    // 7. Obligations dedlist
                    var obligations = HSAS
                        .Where(x => x.Permission_Group == item.HSRM.Permission_Group
                            && x.Salary_Type == item.HSRM.Salary_Type
                            && x.Effective_Month == effectiveMonth
                            && (x.AddDed_Item.StartsWith("C") || x.AddDed_Item.StartsWith("D"))
                            && x.Resigned_Print == "Y")
                        .OrderBy(x => x.AddDed_Item)
                        .Select(x => x.AddDed_Item)
                        .ToList();
                    var dedListAmt = HSAM
                        .Where(x => x.Employee_ID == item.HSRM.Employee_ID
                            && obligations.Contains(x.AddDed_Item))
                        .Select(x => new Sal_Monthly_Detail_Temp_7_2_11
                        {
                            Employee_ID = x.Employee_ID,
                            Item = x.AddDed_Item,
                            Amount = x.Amount
                        })
                        .ToList();

                    resignationSystemPaymentSlip.Add(new ResignationSystemPaymentSlip
                    {
                        FactoryHeader = factoryHeader.FirstOrDefault(x => x.Char1.Contains(item.HEP.Permission_Group))?.Code_Name,
                        Department = item.HEP.Department + "-" + listDepartments.FirstOrDefault(x => x.Key == item.HEP.Department).Value,
                        YearMonth = item.HSRM.Sal_Month.ToString("yyyy/MM"),
                        EmployeeID = item.HEP.Employee_ID,
                        PrintDate = printDate,
                        LocalFullName = item.HEP.Local_Full_Name,
                        PositionTitle = item.HEP.Position_Title,
                        OnboardDate = item.HEP.Onboard_Date.ToString("yyyy/MM/dd"),
                        ResignDate = item.HEP.Resign_Date?.ToString("yyyy/MM/dd"),
                        ResignReason = item.HEP.Resign_Reason,
                        LastMonthSalary = LSalary,
                        ThisMonthSalary = TSalary,
                        Addlist = addListAmt,
                        BenefitsTotal = addListAmt.Sum(x => x?.Amount ?? 0),
                        Dedlist = dedListAmt,
                        ObligationsTotal = dedListAmt.Sum(x => x?.Amount ?? 0),
                        NETAmountReceived = LSalary + TSalary + addListAmt.Sum(x => x?.Amount ?? 0) + dedListAmt.Sum(x => x?.Amount ?? 0)
                    });
                }
            }
            #endregion
            #region Kind = Salary
            if (param.Kind == "Salary")
            {
                var HSMaster = _repositoryAccessor.HRMS_Sal_Master.FindAll(x => x.Factory == param.Factory && listEmployeeID.Contains(x.Employee_ID), true);
                var HSMD = _repositoryAccessor.HRMS_Sal_Master_Detail.FindAll(x => x.Factory == param.Factory && listEmployeeID.Contains(x.Employee_ID), true).ToList();
                var HARM = _repositoryAccessor.HRMS_Att_Resign_Monthly.FindAll(x => x.Factory == param.Factory && x.Att_Month == yearMonth, true).ToList();
                var HAAL = _repositoryAccessor.HRMS_Att_Annual_Leave.FindAll(x => x.Factory == param.Factory && x.Annual_Start >= yearStart && x.Annual_End <= yearEnd && listEmployeeID.Contains(x.Employee_ID), true).ToList();
                var HARMD = _repositoryAccessor.HRMS_Att_Resign_Monthly_Detail.FindAll(x => x.Factory == param.Factory && x.Att_Month == yearMonth && listEmployeeID.Contains(x.Employee_ID), true).ToList();
                var HSP = _repositoryAccessor.HRMS_Sal_Parameter.FindAll(x => x.Seq == "6", true).ToList();
                var HAY = _repositoryAccessor.HRMS_Att_Yearly.FindAll(x => x.Factory == param.Factory && x.Att_Year == yearStart && x.Leave_Type == "2", true);
                var HSMBD = _repositoryAccessor.HRMS_Sal_MasterBackup_Detail.FindAll(x => x.Factory == param.Factory && x.Sal_Month == yearMonth && listEmployeeID.Contains(x.Employee_ID), true).ToList();
                var HALM = _repositoryAccessor.HRMS_Att_Leave_Maintain.FindAll(x => x.Factory == param.Factory && listEmployeeID.Contains(x.Employee_ID), true).ToList();
                var HAC = _repositoryAccessor.HRMS_Att_Calendar.FindAll(x => x.Factory == param.Factory && x.Type_Code == "C05", true).ToList();
                var HACR = _repositoryAccessor.HRMS_Att_Change_Record.FindAll(x => x.Factory == param.Factory && listEmployeeID.Contains(x.Employee_ID), true).ToList();

                HSRMD = HSRMD.Where(x => x.Sal_Month == yearMonth && x.Type_Seq == "49" && listEmployeeID.Contains(x.Employee_ID)).ToList();
                var maxSalaryDay = await _repositoryAccessor.HRMS_Att_Monthly
                    .FindAll(a => a.Factory == param.Factory
                        && a.Att_Month == yearMonth
                        && param.Permission_Group.Contains(a.Permission_Group))
                    .MaxAsync(a => (int?)a.Salary_Days) ?? 0;

                var listInsuranceDeduction = await Query_Sal_Monthly_Detail("N", param.Factory, yearMonth, listEmployeeID, "57", "D", param.Permission_Group, listSalaryType, "0");
                List<string> itemQuery1 = new() { "A", "B", "C", "D" };
                var query49 = await _Query_Sal_Monthly_Detail_Sum("N", param.Factory, yearMonth, listEmployeeID, "49", itemQuery1);
                var query49A = query49.Where(x => x.AddedType == "A");
                var query49B = query49.Where(x => x.AddedType == "B");
                var query49C = query49.Where(x => x.AddedType == "C");
                var query49D = query49.Where(x => x.AddedType == "D");
                List<string> itemQuery2 = new() { "J4", "A0", "B0", "C0", "H0", "I0", "I1" };
                var queryHALMSumDays = HALM.Where(x => x.Leave_Date >= yearStart && x.Leave_Date <= yearEnd && itemQuery2.Contains(x.Leave_code)).ToList();
                var queryJ4 = Query_Leave_Maintain_Sum_Days(listEmployeeID, "J4", queryHALMSumDays);
                var queryA0 = Query_Leave_Maintain_Sum_Days(listEmployeeID, "A0", queryHALMSumDays);
                var queryB0 = Query_Leave_Maintain_Sum_Days(listEmployeeID, "B0", queryHALMSumDays);
                var queryC0 = Query_Leave_Maintain_Sum_Days(listEmployeeID, "C0", queryHALMSumDays);
                var queryH0 = Query_Leave_Maintain_Sum_Days(listEmployeeID, "H0", queryHALMSumDays);
                var queryI0 = Query_Leave_Maintain_Sum_Days(listEmployeeID, "I0", queryHALMSumDays);
                var queryI1 = Query_Leave_Maintain_Sum_Days(listEmployeeID, "I1", queryHALMSumDays);
                var queryReport2 = await Query_Att_Monthly_Detail_Report("N", param.Factory, yearMonth, listEmployeeID, "2");
                var queryReport1 = await Query_Att_Monthly_Detail_Report("N", param.Factory, yearMonth, listEmployeeID, "1");
                var queryReport45A = await Query_Sal_Monthly_Detail_Report("N", param.Factory, yearMonth, listEmployeeID, "45", "A", listPermissionGroup, listSalaryType, "0");
                var queryReport42A = await Query_Sal_Monthly_Detail_Report("N", param.Factory, yearMonth, listEmployeeID, "42", "A", listPermissionGroup, listSalaryType, "2");

                var salaryDay = HARM
                    .Where(x => param.Permission_Group.Contains(x.Permission_Group))
                    .Max(x => (int?)x.Salary_Days) ?? 0;

                // 13
                var listHSASTypeAB = HSAS
                        .Where(x => x.Effective_Month == effectiveMonth
                            && listPermissionGroup.Contains(x.Permission_Group)
                            && new[] { "A", "B" }.Contains(x.AddDed_Type)
                            && x.Onjob_Print == "Y")
                        .OrderBy(x => x.AddDed_Item)
                        .Select(x => new
                        {
                            x.Permission_Group,
                            x.AddDed_Type,
                            x.AddDed_Item
                        })
                        .ToList();
                var listQuerySingleSalTypeAB = await _Query_Single_Sal_Monthly_Detail("N", param.Factory, yearMonth, listEmployeeID, "49", listHSASTypeAB.Select(x => x.AddDed_Type).ToList(), listHSASTypeAB.Select(x => x.AddDed_Item).ToList());
                // 17
                var listHSASTypeCD = HSAS
                        .Where(x => x.Effective_Month == effectiveMonth
                            && param.Permission_Group.Contains(x.Permission_Group)
                            && new[] { "C", "D" }.Contains(x.AddDed_Type)
                            && x.Onjob_Print == "Y")
                        .OrderBy(x => x.AddDed_Item)
                        .Select(x => new
                        {
                            x.Permission_Group,
                            x.AddDed_Type,
                            x.AddDed_Item
                        })
                        .ToList();
                var listQuerySingleSalTypeCD = await _Query_Single_Sal_Monthly_Detail("N", param.Factory, yearMonth, listEmployeeID, "49", listHSASTypeCD.Select(x => x.AddDed_Type).ToList(), listHSASTypeCD.Select(x => x.AddDed_Item).ToList());

                foreach (var item in wk_sql)
                {
                    // 8. Sal Master
                    var salMaster = HSMaster.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID);
                    // 9. Salary Allowance
                    var salaryAllowance = HSMD
                        .Where(x => x.Employee_ID == item.HSRM.Employee_ID
                            && (
                                x.Salary_Item == "A01"
                                || x.Salary_Item == "A02"
                                || x.Salary_Item.StartsWith("B")
                            ))
                        .Sum(x => (decimal?)x.Amount) ?? 0;
                    // 10. Hour Salary
                    var hourSalary = salaryDay > 0 ? salaryAllowance / (salaryDay * 8) : 0;
                    // 11. Att_Resign_Monthly
                    var attResignMonthly = HARM.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID);

                    // 13. Add Item List
                    var addItemList = listHSASTypeAB
                        .Where(x => x.Permission_Group == item.HSRM.Permission_Group)
                        .Select(x => new ItemList
                        {
                            Type = x.AddDed_Type,
                            Item = x.AddDed_Item,
                            Amount = listQuerySingleSalTypeAB.FirstOrDefault(y => y.Employee_ID == item.HEP.Employee_ID && y.AddedType == x.AddDed_Type && y.Item == x.AddDed_Item)?.Amount ?? 0
                        })
                        .ToList();
                    // 14.餐費合計(Meal Total)
                    var mealTotal = HSRMD.Where(x => x.Employee_ID == item.HSRM.Employee_ID && new[] { "B54", "B55", "B56", "B57" }.Contains(x.Item))
                    .Sum(x => (decimal?)x.Amount) ?? 0;
                    // 15.其他加項(Other Add Item)
                    var addItemAmt = HSRMD.Where(x => x.Employee_ID == item.HSRM.Employee_ID && addItemList.Select(a => a.Item).Contains(x.Item))
                        .Sum(x => (decimal?)x.Amount) ?? 0;
                    var orther49A = query49A.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Amount ?? 0;
                    var orther49B = query49B.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Amount ?? 0;
                    var otherAdd = orther49A + orther49B - addItemAmt;
                    // 16. 正項合計 (Total Addition Item)
                    var addTotal = listQueryAddSumN.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Total ?? 0;
                    // 17.扣項固定列印清單_在職(Ded_Item_List)
                    var dedItemList = listHSASTypeCD
                        .Where(x => x.Permission_Group == item.HSRM.Permission_Group)
                        .Select(x => new ItemList
                        {
                            Type = x.AddDed_Type,
                            Item = x.AddDed_Item,
                            Amount = listQuerySingleSalTypeCD.FirstOrDefault(y => y.Employee_ID == item.HEP.Employee_ID && y.AddedType == x.AddDed_Type && y.Item == x.AddDed_Item)?.Amount ?? 0
                        })
                        .ToList();
                    // 18.其他扣項(Other Deduction) 
                    var dedItemAmt = HSRMD
                        .Where(x => x.Employee_ID == item.HSRM.Employee_ID
                                && dedItemList.Select(x => x.Item).Contains(x.Item))
                        .Sum(x => (decimal?)x.Amount) ?? 0;

                    var orther49C = query49C.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Amount ?? 0;
                    var orther49D = query49D.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Amount ?? 0;
                    var otherDeductions = orther49C + orther49D - dedItemAmt;
                    // 19. 標準年假天數 (Year_Quota_Annual)
                    var yearQuotaAnnual = HAAL
                        .Where(x => x.Employee_ID == item.HSRM.Employee_ID)
                        .Sum(x => (decimal?)x.Total_Days) ?? 0;

                    if (yearQuotaAnnual < 0) yearQuotaAnnual = 0;
                    // 20.收集【扣後可休天數】需要的資訊
                    // 21.留職天數(Salary_Suspend)
                    var salarySuspend = queryJ4.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Total ?? 0;
                    // 22.事假天數天數 (Personal Leave)
                    var personalLeave = queryA0.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Total ?? 0;
                    // 23.病假天數(Sick Leave)
                    var sickLeave = queryB0.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Total ?? 0;
                    // 24.曠職天數(Absent)
                    var absentLeave = queryC0.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Total ?? 0;
                    // 25. 公傷假天數 (Work Injury)
                    var workInjuryLeave = queryH0.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Total ?? 0;
                    // 26. 超請假月份 (Sub_Month)
                    var subPersonal = personalLeave - (26 * 1);
                    if (subPersonal <= 0) subPersonal = 0;

                    var subSick = sickLeave - (26 * 2);
                    if (subSick <= 0) subSick = 0;

                    var subWorkInjury = workInjuryLeave - (26 * 6);
                    if (subWorkInjury <= 0) subWorkInjury = 0;

                    var subMonth = (subPersonal + subSick + subWorkInjury + absentLeave + salarySuspend) / 26;
                    // 27. 扣年假計算計算 (Available Leave Days – 可休天數)
                    var (Annual_Number_Months, Available_Days) = Query_Available_Leave_Days(item.HEP, yearQuotaAnnual, subMonth, yearStart, yearEnd, HAC, HACR, HALM);
                    // 28.年假天數分配(Allocation_Annual_Company, Allocation_Annual_Employee)
                    var availableDaysInt = (int)Math.Floor(Available_Days);
                    var availableDaysDec = Available_Days - availableDaysInt;

                    int allocationCompany = 0;
                    decimal allocationEmployee = 0;

                    if (availableDaysInt >= 12)
                    {
                        allocationCompany = 6;
                        allocationEmployee = availableDaysInt - allocationCompany;
                    }
                    else
                    {
                        if (availableDaysInt % 2 != 0)
                        {
                            allocationEmployee = (decimal)((availableDaysInt / 2.0) + 0.5);
                        }
                        else
                        {
                            allocationEmployee = availableDaysInt / 2;
                        }
                        allocationCompany = availableDaysInt - (int)allocationEmployee;
                    }

                    allocationEmployee += availableDaysDec;
                    // 29.累計年假已休天數天數 (Used Annual Leave)
                    var usedAnnualEmployee = queryI0.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Total ?? 0;
                    var usedAnnualCompany = queryI1.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Total ?? 0;

                    var usedAnnual = usedAnnualEmployee + usedAnnualCompany;
                    // 30.累計年假未休天數(Unuse Annual Leave)
                    var unuseAnnualEmployee = allocationEmployee - usedAnnualEmployee;
                    var unuseAnnualCompany = allocationCompany - usedAnnualCompany;
                    var unuseAnnualTotal = unuseAnnualEmployee + unuseAnnualCompany;

                    var salaryItem = queryReport45A.Where(x => x.Employee_ID == item.HSRM.Employee_ID && x.Permission_Group == item.HSRM.Permission_Group && x.Salary_Type == item.HSRM.Salary_Type).ToList();
                    var overTimeItem = queryReport42A.Where(x => x.Employee_ID == item.HSRM.Employee_ID).ToList();
                    var insuranceDeduction = listInsuranceDeduction.Where(x => x.Employee_ID == item.HEP.Employee_ID).ToList();
                    var dedTotal = listQueryDedSumN.FirstOrDefault(x => x.Employee_ID == item.HSRM.Employee_ID)?.Total ?? 0;
                    var overtimeHours = queryReport2.Where(x => x.Employee_ID == item.HSRM.Employee_ID).ToList();
                    var leaveDays = queryReport1.Where(x => x.Employee_ID == item.HSRM.Employee_ID).ToList();

                    int sumDays = HARMD
                        .Where(x => x.Employee_ID == item.HEP.Employee_ID)
                        .Join(HSP,
                            a => new { a.Factory, Code = a.Leave_Code },
                            b => new { b.Factory, b.Code },
                            (a, b) => new { a, b })
                        .Sum(x => (int?)x.a.Days) ?? 0;

                    decimal paidDays = (attResignMonthly != null ? attResignMonthly.Actual_Days : 0) + sumDays;

                    decimal hourlyWage = maxSalaryDay > 0 ? (decimal)salaryAllowance / (maxSalaryDay * 8) : 0;

                    salarySlip.Add(new SalarySlip
                    {
                        FactoryHeader = factoryHeader.FirstOrDefault(x => x.Char1.Contains(item.HEP.Permission_Group))?.Code_Name,
                        Department = item.HEP.Department + "-" + listDepartments.FirstOrDefault(x => x.Key == item.HEP.Department).Value,
                        EmployeeID = item.HEP.Employee_ID,
                        LocalFullName = item.HEP.Local_Full_Name,
                        YearMonth = item.HSRM.Sal_Month.ToString("yyyy/MM"),
                        PositionTitle = salMaster.Position_Title,
                        OnboardDate = item.HEP.Onboard_Date.ToString("yyyy/MM/dd"),
                        // MonthlyPositiveItems 
                        SalaryItem = salaryItem,
                        OverTimeItem = overTimeItem,
                        AddItemList = addItemList,
                        MealTotal = mealTotal,
                        OtherAdditions = otherAdd,
                        TotalAdditionItem = addTotal,
                        // Monthly Deductions
                        LoanedAmount = 0,
                        InsuranceDeduction = insuranceDeduction,
                        DedItemList = dedItemList,
                        Tax = item.HSRM.Tax,
                        OtherDeductions = otherDeductions,
                        TotalDeductionItem = dedTotal + item.HSRM.Tax,
                        NETAmountReceived = addTotal - (dedTotal + item.HSRM.Tax),
                        CumulativeOvertimeHoursYTD = HAY.Where(x => x.Employee_ID == item.HEP.Employee_ID).Sum(x => x.Days),
                        TotalOvertimeHours_CurrentMonth = HARMD.Where(x => x.Employee_ID == item.HEP.Employee_ID && x.Leave_Type == "2").Sum(x => x.Days),
                        OvertimeHours = overtimeHours,
                        // Attendance
                        ActualWorkDays = attResignMonthly != null ? attResignMonthly.Actual_Days : 0,
                        DelayEarlyTimes = attResignMonthly != null ? attResignMonthly.Delay_Early : 0,
                        LeaveDays = leaveDays,
                        StandardAnnualLeaveDays = yearQuotaAnnual,
                        AnnualLeaveEntitlement_ThisYear = Available_Days,
                        CumulativeAnnualLeaveTaken = usedAnnualCompany + "/" + usedAnnualEmployee,
                        CumulativeAnnualLeaveRemaining = unuseAnnualCompany + "/" + unuseAnnualEmployee,
                        CumulativeAnnualLeaveRemainingTotal = unuseAnnualTotal,
                        TotalPaidDays = paidDays,
                        DayShiftMealTimes = attResignMonthly?.DayShift_Food ?? 0,
                        OvertimeMealTimes = attResignMonthly != null ? attResignMonthly.Food_Expenses : 0,
                        NightShiftAllowanceTimes = attResignMonthly != null ? attResignMonthly.Night_Eat_Times : 0,
                        NightShiftMealTimes = attResignMonthly?.NightShift_Food ?? 0,
                        SalaryAllowance = HSMBD.Where(x => x.Employee_ID == item.HEP.Employee_ID
                                && (x.Salary_Item == "A01"
                                || x.Salary_Item == "A02"
                                || x.Salary_Item.StartsWith("B")))
                            .Sum(d => (int?)d.Amount) ?? 0,
                        HourlyWage = hourlyWage
                    });
                }
            }
            #endregion
            data.ResignationSystemPaymentSlip = resignationSystemPaymentSlip;
            data.SalarySlip = salarySlip;
            return new OperationResult(true, data);
        }
        #endregion
        #region Query_Sal_Monthly_Detail_Sum
        public async Task<List<SalaryDetailResult_7_2_11>> _Query_Sal_Monthly_Detail_Sum(string kind, string factory, DateTime yearMonth, List<string> employeeIds, string typeSeq, List<string> addedType)
        {
            if (kind == "Y")
            {
                return await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                        x.Sal_Month == yearMonth &&
                        employeeIds.Contains(x.Employee_ID) &&
                        x.Type_Seq == typeSeq &&
                        addedType.Contains(x.AddDed_Type), true)
                    .GroupBy(x => new { x.Employee_ID, x.AddDed_Type })
                    .Select(x => new SalaryDetailResult_7_2_11
                    {
                        Employee_ID = x.Key.Employee_ID,
                        AddedType = x.Key.AddDed_Type,
                        Amount = x.Sum(x => (int?)x.Amount ?? 0)
                    })
                    .ToListAsync();
            }
            else if (kind == "N")
            {
                return await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                        x.Sal_Month == yearMonth &&
                        employeeIds.Contains(x.Employee_ID) &&
                        x.Type_Seq == typeSeq &&
                        addedType.Contains(x.AddDed_Type), true)
                    .GroupBy(x => new { x.Employee_ID, x.AddDed_Type })
                    .Select(x => new SalaryDetailResult_7_2_11
                    {
                        Employee_ID = x.Key.Employee_ID,
                        AddedType = x.Key.AddDed_Type,
                        Amount = x.Sum(x => (int?)x.Amount ?? 0)
                    })
                    .ToListAsync();
            }
            else if (kind == "PY")
            {
                return await _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                        x.Sal_Month == yearMonth &&
                        employeeIds.Contains(x.Employee_ID) &&
                        x.Probation == "Y" &&
                        x.Type_Seq == typeSeq &&
                        addedType.Contains(x.AddDed_Type), true)
                    .GroupBy(x => new { x.Employee_ID, x.AddDed_Type })
                    .Select(x => new SalaryDetailResult_7_2_11
                    {
                        Employee_ID = x.Key.Employee_ID,
                        AddedType = x.Key.AddDed_Type,
                        Amount = x.Sum(x => (int?)x.Amount ?? 0)
                    })
                    .ToListAsync();
            }
            else // kind == "PN"
            {
                return await _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                        x.Sal_Month == yearMonth &&
                        employeeIds.Contains(x.Employee_ID) &&
                        x.Probation == "N" &&
                        x.Type_Seq == typeSeq &&
                        addedType.Contains(x.AddDed_Type), true)
                    .GroupBy(x => new { x.Employee_ID, x.AddDed_Type })
                    .Select(x => new SalaryDetailResult_7_2_11
                    {
                        Employee_ID = x.Key.Employee_ID,
                        AddedType = x.Key.AddDed_Type,
                        Amount = x.Sum(x => (int?)x.Amount ?? 0)
                    })
                    .ToListAsync();
            }
        }
        #endregion
        #region _Query_Single_Sal_Monthly_Detail
        public async Task<List<SalaryDetailResult_7_2_11>> _Query_Single_Sal_Monthly_Detail(string kind, string factory, DateTime yearMonth, List<string> employeeId, string typeSeq, List<string> addedType, List<string> item)
        {
            if (kind == "Y")
            {
                return await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                        x.Sal_Month == yearMonth &&
                        employeeId.Contains(x.Employee_ID) &&
                        item.Contains(x.Item) &&
                        x.Type_Seq == typeSeq &&
                        addedType.Contains(x.AddDed_Type), true)
                    .GroupBy(x => new { x.Employee_ID, x.Item, x.Sal_Month, x.AddDed_Type })
                    .Select(x => new SalaryDetailResult_7_2_11
                    {
                        Employee_ID = x.Key.Employee_ID,
                        Item = x.Key.Item,
                        AddedType = x.Key.AddDed_Type,
                        Sal_Month = x.Key.Sal_Month,
                        Amount = x.Sum(x => (int?)x.Amount ?? 0)
                    })
                    .ToListAsync();
            }
            else /// --kind = N
            {
                return await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                        x.Sal_Month == yearMonth &&
                        employeeId.Contains(x.Employee_ID) &&
                        item.Contains(x.Item) &&
                        x.Type_Seq == typeSeq &&
                        addedType.Contains(x.AddDed_Type), true)
                    .GroupBy(x => new { x.Employee_ID, x.Item, x.Sal_Month, x.AddDed_Type })
                    .Select(x => new SalaryDetailResult_7_2_11
                    {
                        Employee_ID = x.Key.Employee_ID,
                        Item = x.Key.Item,
                        AddedType = x.Key.AddDed_Type,
                        Sal_Month = x.Key.Sal_Month,
                        Amount = x.Sum(x => (int?)x.Amount ?? 0)
                    })
                    .ToListAsync();
            }
        }
        #endregion
        #region Query_Available_Leave_Days 
        private static (int Annual_Number_Months, decimal Available_Days) Query_Available_Leave_Days(HRMS_Emp_Personal item, decimal Total_Annual, int Sub_Month, DateTime yearStart, DateTime yearEnd,
            List<HRMS_Att_Calendar> HAC, List<HRMS_Att_Change_Record> HACR, List<HRMS_Att_Leave_Maintain> HALM)
        {
            int VN_Annual_Days = 12;
            var Annual_Number_months = 0;
            var Available_Days_After_Deduction = 0m;
            int yy = yearStart.Year;
            bool isResigned = item.Resign_Date != null;
            if (!isResigned)
            {
                if (item.Onboard_Date.Year == yy)
                {
                    int onboardMonth = item.Onboard_Date.Month;

                    DateTime onboardMonthFirst = new(item.Onboard_Date.Year, onboardMonth, 1);
                    DateTime onboardMonthLast = onboardMonthFirst.AddMonths(1).AddDays(-1);
                    // 一般假日天數
                    int calendarCnt = HAC
                        .Where(x => x.Att_Date >= onboardMonthFirst
                            && x.Att_Date <= onboardMonthLast)
                        .Count();
                    // --計薪天數
                    int salaryDays = (onboardMonthLast - onboardMonthFirst).Days + 1 - calendarCnt;

                    // #---算月份有薪出勤天數--- Actual_Days
                    int attDateCnt = HACR
                        .Where(x => x.Employee_ID == item.Employee_ID
                            && x.Att_Date >= onboardMonthFirst
                            && x.Att_Date <= onboardMonthLast)
                        .Count();

                    int leaveCnt = HALM
                        .Where(x => x.Employee_ID == item.Employee_ID
                            && x.Leave_Date >= onboardMonthFirst
                            && x.Leave_Date <= onboardMonthLast
                            && new[] { "A0", "B0", "C0", "G0", "J4", "J3", "O0" }.Contains(x.Leave_code))
                        .Sum(x => (int?)x.Days) ?? 0;

                    int actualDays = attDateCnt - leaveCnt;
                    double percent = salaryDays > 0 ? (actualDays * 100.0 / salaryDays) : 0;

                    if (percent >= 50)
                        Annual_Number_months = VN_Annual_Days - onboardMonth + 1 - Sub_Month;
                    else
                        Annual_Number_months = VN_Annual_Days - onboardMonth - Sub_Month;
                }
                else
                    Annual_Number_months = VN_Annual_Days - Sub_Month;
            }
            else
            {
                if (item.Resign_Date.Value.Year == yy)
                {
                    int onboardMonth = item.Onboard_Date.Month;
                    DateTime onboardMonthFirst = new(item.Onboard_Date.Year, onboardMonth, 1);
                    DateTime onboardMonthLast = onboardMonthFirst.AddMonths(1).AddDays(-1);

                    // --- 計薪天數 ---
                    int calendarCnt = HAC
                        .Where(x => x.Att_Date >= onboardMonthFirst
                            && x.Att_Date <= onboardMonthLast)
                        .Count();

                    int salaryDays = (onboardMonthLast - onboardMonthFirst).Days + 1 - calendarCnt;

                    // --- 出勤天數 ---
                    int attDateCnt = HACR
                        .Where(x => x.Employee_ID == item.Employee_ID
                            && x.Att_Date >= onboardMonthFirst
                            && x.Att_Date <= onboardMonthLast)
                        .Count();

                    // --- 請假天數 ---
                    int leaveCnt = HALM
                        .Where(x => x.Employee_ID == item.Employee_ID
                            && x.Leave_Date >= onboardMonthFirst
                            && x.Leave_Date <= onboardMonthLast
                            && new[] { "A0", "B0", "C0", "G0", "J4", "J3", "O0" }.Contains(x.Leave_code))
                        .Sum(l => (int?)l.Days) ?? 0;

                    int actualDays = attDateCnt - leaveCnt;
                    double percent = salaryDays > 0 ? (actualDays * 100.0 / salaryDays) : 0;

                    int onboardMonthInt = (percent < 50) ? onboardMonth + 1 : onboardMonth;

                    // --- 判斷離職月 ---
                    int resignMonth = item.Resign_Date.Value.Month;
                    DateTime multiFirstDate = new(item.Onboard_Date.Year, resignMonth, 1);
                    DateTime multiLastDate = multiFirstDate.AddMonths(1).AddDays(-1);

                    int resignSalaryDays;
                    if (resignMonth == onboardMonth) // 到職月 = 離職月
                    {
                        int resignCalendarCnt = HAC
                        .Where(x => x.Att_Date >= onboardMonthFirst
                            && x.Att_Date <= onboardMonthLast)
                        .Count();
                        resignSalaryDays = (onboardMonthLast - onboardMonthFirst).Days + 1 - resignCalendarCnt;
                    }
                    else
                    {
                        multiFirstDate = new(item.Resign_Date.Value.Year, resignMonth, 1);
                        multiLastDate = multiFirstDate.AddMonths(1).AddDays(-1);
                        int resignCalendarCnt = HAC
                            .Where(x => x.Att_Date >= multiFirstDate
                                && x.Att_Date <= multiLastDate)
                            .Count();
                        resignSalaryDays = (multiLastDate - multiFirstDate).Days + 1 - resignCalendarCnt;
                    }

                    // --- 出勤天數 trong tháng nghỉ ---
                    int resignAttDateCnt = HACR
                        .Where(x => x.Employee_ID == item.Employee_ID
                            && x.Att_Date >= multiFirstDate
                            && x.Att_Date <= multiLastDate)
                        .Count();

                    // --- 請假天數 trong tháng nghỉ ---
                    int resignLeaveCnt = HALM
                        .Where(x => x.Employee_ID == item.Employee_ID
                            && x.Leave_Date >= multiFirstDate
                            && x.Leave_Date <= multiLastDate
                            && new[] { "A0", "B0", "C0", "G0", "J4", "J3", "O0" }.Contains(x.Leave_code))
                        .Sum(l => (int?)l.Days) ?? 0;

                    int resignActualDays = resignAttDateCnt - resignLeaveCnt;
                    double resignPercent = resignSalaryDays > 0 ? (resignActualDays * 100.0 / resignSalaryDays) : 0;

                    int resignMonthInt = resignMonth;
                    if (resignPercent < 50)
                        resignMonthInt = resignMonth - 1;


                    // --- 年假計算月數 ---
                    Annual_Number_months = resignMonthInt - onboardMonthInt + 1 - Sub_Month;
                }
                else
                {
                    // 舊員工 + 離職
                    int resignMonth = item.Resign_Date.Value.Month;
                    DateTime resignMonthFirst = new(item.Resign_Date.Value.Year, resignMonth, 1);
                    DateTime resignMonthLast = resignMonthFirst.AddMonths(1).AddDays(-1);

                    int calendarCnt = HAC
                        .Where(x => x.Att_Date >= resignMonthFirst
                            && x.Att_Date <= resignMonthLast)
                        .Count();

                    int salaryDays = (resignMonthLast - resignMonthFirst).Days + 1 - calendarCnt;

                    int attDateCnt = HACR
                        .Where(x => x.Employee_ID == item.Employee_ID
                            && x.Att_Date >= resignMonthFirst
                            && x.Att_Date <= resignMonthLast)
                        .Count();

                    int leaveCnt = HALM
                        .Where(x => x.Employee_ID == item.Employee_ID
                            && x.Leave_Date >= resignMonthFirst
                            && x.Leave_Date <= resignMonthLast
                            && new[] { "A0", "B0", "C0", "G0", "J4", "J3", "O0" }.Contains(x.Leave_code))
                        .Sum(l => (int?)l.Days) ?? 0;

                    int actualDays = attDateCnt - leaveCnt;
                    double percent = salaryDays > 0 ? (actualDays * 100.0 / salaryDays) : 0;

                    if (percent >= 50)
                        Annual_Number_months = resignMonth - Sub_Month;
                    else
                        Annual_Number_months = resignMonth - 1 - Sub_Month;
                }
                if (Annual_Number_months <= 0)
                {
                    Annual_Number_months = 0;
                    Available_Days_After_Deduction = 0;
                }
                else
                    Available_Days_After_Deduction = Total_Annual / 12 * Annual_Number_months;
            }

            return (Annual_Number_months, Available_Days_After_Deduction);
        }
        #endregion

        #region Query_Sal_Monthly_Detail_Report
        private async Task<List<Sal_Monthly_Detail_Values_7_2_11>> Query_Sal_Monthly_Detail_Report(string Kind, string Factory, DateTime Year_Month, List<string> listEmployee_ID,
            string Type_Seq, string AddDed_Type, List<string> listPermission_Group, List<string> listSalary_Type, string Leave_Type)
        {
            List<Sal_Monthly_Detail_Temp_7_2_11> Sal_Monthly_Detail_Temp = new();

            if (Kind == "Y")
            {
                Sal_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == Factory
                        && x.Sal_Month == Year_Month
                        && listEmployee_ID.Contains(x.Employee_ID)
                        && x.Type_Seq == Type_Seq
                        && x.AddDed_Type == AddDed_Type
                        && x.Amount > 0, true)
                    .Select(x => new Sal_Monthly_Detail_Temp_7_2_11
                    {
                        Employee_ID = x.Employee_ID,
                        Item = x.Item,
                        Amount = x.Amount
                    })
                    .ToListAsync();
            }
            else if (Kind == "N")
            {
                Sal_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
                    .FindAll(x => x.Factory == Factory
                        && x.Sal_Month == Year_Month
                        && listEmployee_ID.Contains(x.Employee_ID)
                        && x.Type_Seq == Type_Seq
                        && x.AddDed_Type == AddDed_Type
                        && x.Amount > 0, true)
                    .Select(x => new Sal_Monthly_Detail_Temp_7_2_11
                    {
                        Employee_ID = x.Employee_ID,
                        Item = x.Item,
                        Amount = x.Amount
                    })
                    .ToListAsync();
            }
            var salSettingsAll = await _repositoryAccessor.HRMS_Sal_Item_Settings
                .FindAll(x => x.Factory == Factory
                    && x.Effective_Month <= Year_Month
                    && listPermission_Group.Contains(x.Permission_Group)
                    && listSalary_Type.Contains(x.Salary_Type))
                .ToListAsync();

            // Group theo Permission_Group, Salary_Type, Salary_Item và chọn bản ghi có Effective_Month mới nhất cho mỗi group
            var salSettingTemp = salSettingsAll
                .GroupBy(x => new { x.Permission_Group, x.Salary_Type, x.Salary_Item })
                .Select(g => g.OrderByDescending(s => s.Effective_Month).First())
                .Select(x => new
                {
                    x.Seq,
                    x.Salary_Item,
                    x.Permission_Group,
                    x.Salary_Type
                })
                .ToList();

            // Max Effective_Month Att
            var effectiveMonthAtt = _repositoryAccessor.HRMS_Att_Use_Monthly_Leave
                            .FindAll(x => x.Factory == Factory
                                && x.Effective_Month <= Year_Month
                                && x.Leave_Type == Leave_Type)
                            .Max(x => (DateTime?)x.Effective_Month);
            // Lấy cài đặt phép nghỉ/ làm thêm  
            var attSettingTemp = await _repositoryAccessor.HRMS_Att_Use_Monthly_Leave
                .FindAll(x => x.Factory == Factory
                            && x.Leave_Type == Leave_Type
                            && x.Effective_Month == effectiveMonthAtt)
                .Select(x => new { x.Seq, x.Code })
                .ToListAsync();
            List<Sal_Monthly_Detail_Values_7_2_11> Sal_Monthly_Detail_Values = new();
            // Lấy kết quả theo Type_Seq  
            if (Type_Seq == "45")
            {
                Sal_Monthly_Detail_Values = Sal_Monthly_Detail_Temp
                    .Join(salSettingTemp,
                        x => x.Item,
                        y => y.Salary_Item,
                        (detail, setting) => new { detail, setting })
                    .OrderBy(x => x.setting.Seq)
                    .Select(x => new Sal_Monthly_Detail_Values_7_2_11
                    {
                        Seq = x.setting.Seq,
                        Permission_Group = x.setting.Permission_Group,
                        Salary_Type = x.setting.Salary_Type,
                        Employee_ID = x.detail.Employee_ID,
                        Item = x.detail.Item,
                        Amount = x.detail.Amount
                    })
                    .ToList();
            }
            else if (Type_Seq == "42")
            {
                Sal_Monthly_Detail_Values = Sal_Monthly_Detail_Temp
                    .Join(attSettingTemp,
                        x => x.Item,
                        y => y.Code,
                        (detail, setting) => new { detail, setting })
                    .OrderBy(x => x.setting.Seq)
                    .Select(x => new Sal_Monthly_Detail_Values_7_2_11
                    {
                        Seq = x.setting.Seq,
                        Employee_ID = x.detail.Employee_ID,
                        Item = x.detail.Item,
                        Amount = x.detail.Amount
                    })
                    .ToList();
            }
            else if (Type_Seq == "49" || Type_Seq == "57")
            {
                Sal_Monthly_Detail_Values = Sal_Monthly_Detail_Temp
                .Select(x => new Sal_Monthly_Detail_Values_7_2_11
                {
                    Employee_ID = x.Employee_ID,
                    Item = x.Item,
                    Amount = x.Amount
                })
                .OrderBy(x => x.Item)
                .ToList();
            }

            return Sal_Monthly_Detail_Values;
        }
        #endregion

        #region Query_Att_Monthly_Detail_Report
        private async Task<List<Att_Monthly_Detail_Values_7_2_11>> Query_Att_Monthly_Detail_Report(string Kind, string Factory, DateTime Year_Month, List<string> ListEmployeeID, string Leave_Type)
        {
            List<Att_Monthly_Detail_Temp_7_2_11> Att_Monthly_Detail_Temp = new();

            if (Kind == "Y")
            {
                Att_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Att_Monthly_Detail
                    .FindAll(x => x.Factory == Factory
                        && x.Att_Month == Year_Month
                        && ListEmployeeID.Contains(x.Employee_ID)
                        && x.Leave_Type == Leave_Type
                        && x.Days > 0, true)
                    .Select(x => new Att_Monthly_Detail_Temp_7_2_11
                    {
                        Employee_ID = x.Employee_ID,
                        Leave_Code = x.Leave_Code,
                        Days = x.Days
                    })
                    .ToListAsync();
            }
            else if (Kind == "N")
            {
                Att_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Att_Resign_Monthly_Detail
                    .FindAll(x => x.Factory == Factory
                        && x.Att_Month == Year_Month
                        && ListEmployeeID.Contains(x.Employee_ID)
                        && x.Leave_Type == Leave_Type
                        && x.Days > 0, true)
                    .Select(x => new Att_Monthly_Detail_Temp_7_2_11
                    {
                        Employee_ID = x.Employee_ID,
                        Leave_Code = x.Leave_Code,
                        Days = x.Days
                    })
                    .ToListAsync();
            }

            // Max Effective_Month
            var effectiveMonth = _repositoryAccessor.HRMS_Sal_AddDedItem_Settings
                .FindAll(x => x.Factory == Factory && x.Effective_Month <= Year_Month)
                .Max(x => (DateTime?)x.Effective_Month);

            var settingTemp = _repositoryAccessor.HRMS_Att_Use_Monthly_Leave
                .FindAll(x => x.Factory == Factory
                    && x.Leave_Type == Leave_Type
                    && x.Effective_Month == effectiveMonth, true)
                .ToList();

            List<Att_Monthly_Detail_Values_7_2_11> Att_Monthly_Detail_Values = new();
            Att_Monthly_Detail_Values = Att_Monthly_Detail_Temp
                    .Join(settingTemp,
                        x => x.Leave_Code,
                        y => y.Code,
                        (detail, setting) => new { detail, setting })
                    .OrderBy(x => x.setting.Seq)
                    .Select(x => new Att_Monthly_Detail_Values_7_2_11
                    {
                        Seq = x.setting.Seq,
                        Employee_ID = x.detail.Employee_ID,
                        Item = x.detail.Leave_Code,
                        Days = x.detail.Days
                    })
                    .ToList();
            return Att_Monthly_Detail_Values;
        }
        #endregion
        #region Query_Sal_Monthly_Detail_Add_Sum
        private static List<ListSumDays> Query_Sal_Monthly_Detail_Add_Sum(string Kind, DateTime Year_Month, List<string> listEmployeeID,
            List<HRMS_Sal_Monthly_Detail> HSMD, List<HRMS_Sal_Resign_Monthly_Detail> HSRMD, List<HRMS_Sal_Probation_Monthly_Detail> HSPMD)
        {
            List<ListSumDays> addSum = new();
            // Lấy dữ liệu một lần cho tất cả các loại  
            var total1Dict = Query_Sal_Monthly_Detail_Sum_Dict(Kind, Year_Month, listEmployeeID, "45", "A", HSMD, HSRMD, HSPMD);
            var total2Dict = Query_Sal_Monthly_Detail_Sum_Dict(Kind, Year_Month, listEmployeeID, "42", "A", HSMD, HSRMD, HSPMD);
            var total3Dict = Query_Sal_Monthly_Detail_Sum_Dict(Kind, Year_Month, listEmployeeID, "49", "A", HSMD, HSRMD, HSPMD);
            var total4Dict = Query_Sal_Monthly_Detail_Sum_Dict(Kind, Year_Month, listEmployeeID, "49", "B", HSMD, HSRMD, HSPMD);

            // Tính tổng cho từng nhân viên  
            addSum = listEmployeeID.Select(employeeID => new ListSumDays
            {
                Employee_ID = employeeID,
                Total = (total1Dict.TryGetValue(employeeID, out int value1) ? value1 : 0)
                    + (total2Dict.TryGetValue(employeeID, out int value2) ? value2 : 0)
                    + (total3Dict.TryGetValue(employeeID, out int value3) ? value3 : 0)
                    + (total4Dict.TryGetValue(employeeID, out int value4) ? value4 : 0)
            }).ToList();

            return addSum;
        }
        #endregion
        private static Dictionary<string, int> Query_Sal_Monthly_Detail_Sum_Dict(string kind, DateTime Year_Month, List<string> employeeIds,
            string typeSeq, string addedType, List<HRMS_Sal_Monthly_Detail> HSMD, List<HRMS_Sal_Resign_Monthly_Detail> HSRMD, List<HRMS_Sal_Probation_Monthly_Detail> HSPMD)
        {
            if (kind == "Y")
            {
                return HSMD
                    .Where(x => x.Sal_Month == Year_Month &&
                        employeeIds.Contains(x.Employee_ID) &&
                        x.Type_Seq == typeSeq &&
                        x.AddDed_Type == addedType)
                    .GroupBy(x => x.Employee_ID)
                    .ToDictionary(g => g.Key, g => g.Sum(x => (int?)x.Amount ?? 0));
            }
            else if (kind == "N")
            {
                return HSRMD
                    .Where(x => x.Sal_Month == Year_Month &&
                        employeeIds.Contains(x.Employee_ID) &&
                        x.Type_Seq == typeSeq &&
                        x.AddDed_Type == addedType)
                    .GroupBy(x => x.Employee_ID)
                    .ToDictionary(g => g.Key, g => g.Sum(x => (int?)x.Amount ?? 0));
            }
            else if (kind == "PY")
            {
                return HSPMD
                    .Where(x => x.Sal_Month == Year_Month &&
                        employeeIds.Contains(x.Employee_ID) &&
                        x.Probation == "Y" &&
                        x.Type_Seq == typeSeq &&
                        x.AddDed_Type == addedType)
                    .GroupBy(x => x.Employee_ID)
                    .ToDictionary(g => g.Key, g => g.Sum(x => (int?)x.Amount ?? 0));
            }
            else // kind == "PN"  
            {
                return HSPMD
                    .Where(x => x.Sal_Month == Year_Month &&
                        employeeIds.Contains(x.Employee_ID) &&
                        x.Probation == "N" &&
                        x.Type_Seq == typeSeq &&
                        x.AddDed_Type == addedType)
                    .GroupBy(x => x.Employee_ID)
                    .ToDictionary(g => g.Key, g => g.Sum(x => (int?)x.Amount ?? 0));
            }
        }

        #region Query_Sal_Monthly_Detail_Ded_Sum
        private static List<ListSumDays> Query_Sal_Monthly_Detail_Ded_Sum(string Kind, DateTime Year_Month, List<string> ListEmployeeID,
            List<HRMS_Sal_Monthly_Detail> HSMD, List<HRMS_Sal_Resign_Monthly_Detail> HSRMD, List<HRMS_Sal_Probation_Monthly_Detail> HSPMD)
        {
            List<ListSumDays> dedSum = new();

            // Lấy dữ liệu một lần cho tất cả các loại  
            var total1Dict = Query_Sal_Monthly_Detail_Sum_Dict(Kind, Year_Month, ListEmployeeID, "57", "D", HSMD, HSRMD, HSPMD);
            var total2Dict = Query_Sal_Monthly_Detail_Sum_Dict(Kind, Year_Month, ListEmployeeID, "49", "C", HSMD, HSRMD, HSPMD);
            var total3Dict = Query_Sal_Monthly_Detail_Sum_Dict(Kind, Year_Month, ListEmployeeID, "49", "D", HSMD, HSRMD, HSPMD);
            // Tính tổng cho từng nhân viên  
            dedSum = ListEmployeeID.Select(employeeID => new ListSumDays
            {
                Employee_ID = employeeID,
                Total = (total1Dict.TryGetValue(employeeID, out int value1) ? value1 : 0)
                    + (total2Dict.TryGetValue(employeeID, out int value2) ? value2 : 0)
                    + (total3Dict.TryGetValue(employeeID, out int value3) ? value3 : 0)
            }).ToList();

            return dedSum;
        }
        #endregion

        #region Query_Leave_Maintain_Sum_Days 
        private static List<ListSumDays> Query_Leave_Maintain_Sum_Days(List<string> ListEmployeeID, string Leave_Code, List<HRMS_Att_Leave_Maintain> HALM)
        {
            var dedSum = HALM
                .Where(x => ListEmployeeID.Contains(x.Employee_ID) && x.Leave_code == Leave_Code)
                .GroupBy(x => x.Employee_ID)
                .Select(g => new ListSumDays()
                {
                    Employee_ID = g.Key,
                    Total = g.Sum(x => (int?)x.Days) ?? 0
                })
                .ToList();

            return dedSum;
        }
        #endregion
        #region GetList
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

        public async Task<List<KeyValuePair<string, string>>> GetListLanguage(string language)
        {
            return await GetBasicCodeList(language, BasicCodeTypeConstant.Language);
        }
        private async Task<List<KeyValuePair<string, string>>> GetBasicCodeList(string language, string typeSeq)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == typeSeq, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
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
        private string GetLabel(string key, string lang)
        {
            if (_labels.TryGetValue(key, out var label))
            {
                return label.Get(lang);
            }
            return key;
        }
        public class MultiLanguageLabel
        {
            public string EN { get; set; }
            public string VN { get; set; }
            public string TW { get; set; }

            public MultiLanguageLabel(string en, string vn, string tw)
            {
                EN = en;
                VN = vn;
                TW = tw;
            }

            public string Get(string lang)
            {
                return lang switch
                {
                    "EN" => EN,
                    "VN" => VN,
                    "TW" => TW,
                    _ => EN
                };
            }
        }
        private static void CheckAndResetRow(ref int currentRow, ref int currentColum)
        {
            if (currentRow == 35)
            {
                currentRow = 5;
                currentColum += 4;
            }
        }
        private readonly Dictionary<string, MultiLanguageLabel> _labels = new()
        {
            { "Monthly_Positive_Items", new MultiLanguageLabel("Monthly Positive Items", "Các khoản thu nhập trong tháng", "月份各正項") },
            { "Meal_Total", new MultiLanguageLabel("Meal Total", "Hỗ trợ tiền cơm", "餐費合計") },
            { "Other_Additions", new MultiLanguageLabel("Other Additions", "Hạng mục cộng khác", "其他加項") },
            { "Total_Addition_Item", new MultiLanguageLabel("Total Addition Item", "Tổng thu nhập", "正項合計") },
            { "Monthly_Deductions", new MultiLanguageLabel("Monthly Deductions", "Các khoản trừ trong tháng", "月份各扣項") },
            { "Loaned_Amount", new MultiLanguageLabel("Loaned Amount", "Tạm ứng", "借支金額扣") },
            { "Tax", new MultiLanguageLabel("Tax", "Thuế thu nhập cá nhân", "所得稅扣") },
            { "Other_Deductions", new MultiLanguageLabel("Other Deductions", "Trừ khác", "其他扣項") },
            { "Total_Deduction_Item", new MultiLanguageLabel("Total Deduction Item", "Tổng các khoản trừ", "扣項合計") },
            { "Net_Amount_Received", new MultiLanguageLabel("Net Amount Received", "Lương thực lãnh", "實領金額") },
            { "Cumulative_Overtime_Hours_YTD", new MultiLanguageLabel("Cumulative Overtime Hours (YTD)", "Tổng số giờ làm thêm trong năm", "年度累計加班時數") },
            { "Total_Overtime_Hours_Current_Month", new MultiLanguageLabel("Total Overtime Hours (Current Month)", "Tổng số giờ làm thêm trong tháng", "當月總加班時數") },
            { "Attendance", new MultiLanguageLabel("Attendance", "Chuyên cần", "出勤") },
            { "Actual_Work_Days", new MultiLanguageLabel("Actual Work Days", "Số ngày làm việc thực tế", "實際上班天數") },
            { "Delay_Early_Times", new MultiLanguageLabel("Delay/Early Times", "Số lần đến trễ/ về sớm", "遲到早退數") },
            { "Standard_Annual_Leave_Days", new MultiLanguageLabel("Standard Annual Leave Days", "Số ngày PN tiêu chuẩn", "標準年假天數") },
            { "Annual_Leave_Entitlement_This_Year", new MultiLanguageLabel("Annual Leave Entitlement (This Year)", "Số ngày PN được hưởng trong năm", "當年可以年假天數") },
            { "Cumulative_Annual_Leave", new MultiLanguageLabel("Cumulative Annual Leave", "Số ngày PN", "累計年假") },
            { "Cumulative_Annual_Leave_Taken", new MultiLanguageLabel("Taken: Company / Employee", "đã nghỉ: NSDLĐ/ NLĐ", "已休天數：公司 / 員工") },
            { "Cumulative_Annual_Leave_Remaining_Split", new MultiLanguageLabel("Remaining: Company / Employee", "chưa nghỉ: NSDLĐ/ NLĐ", "未休天數：公司 / 員工") },
            { "Cumulative_Annual_Leave_Remaining_Total", new MultiLanguageLabel("Remaining: Company + Employee", "chưa nghỉ: NSDLĐ + NLĐ", "未休天數：公司+員工") },
            { "Total_Paid_Days", new MultiLanguageLabel("Total paid days", "Tổng số ngày được trả lương", "有薪天數") },
            { "Day_Shift_Meal_Times", new MultiLanguageLabel("Day Shift Meal Times", "Suất cơm trưa", "白班伙食次數") },
            { "Overtime_Meal_Times", new MultiLanguageLabel("Overtime Meal Times", "Suất cơm chiều", "加班伙食費") },
            { "Night_Shift_Allowance_Times", new MultiLanguageLabel("Night Shift Allowance Times", "Suất cơm hỗ trợ", "夜點費次數") },
            { "Night_Shift_Meal_Times", new MultiLanguageLabel("Night Shift Meal Times", "Suất cơm ca đêm", "夜班伙食次數") },
            { "Salary_And_Allowance", new MultiLanguageLabel("Salary + Allowance", "Mức lương+các loại Phụ cấp", "底薪+津貼") },
            { "Hourly_Wage", new MultiLanguageLabel("Hourly Wage", "Lương giờ", "工時時薪") },
            { "Department", new MultiLanguageLabel("Department:", "Phòng ban:", "部門:") },
            { "Employee_ID", new MultiLanguageLabel("Employee ID:", "Mã nhân viên:", "工號") },
            { "Local_Full_Name", new MultiLanguageLabel("Local Full Name:", "Họ và Tên:", "本地姓名:") },
            { "Print_Date", new MultiLanguageLabel("Print Date:", "Print Date", "列印日期:") },
            { "Year_Month", new MultiLanguageLabel("Year Month:", "Năm-tháng:", "薪資年月:") },
            { "Position_Title", new MultiLanguageLabel("Position Title:", "Chức danh:", "職稱:") },
            { "Onboard_Date", new MultiLanguageLabel("Onboard Date:", "Ngày vào làm:", "到職日期:") },
            { "Resignation", new MultiLanguageLabel("Resignation system payment slip", "Resignation system payment slip", "離職制度付款單") },
            { "Resign_Date", new MultiLanguageLabel("Resign Date:", "Resign Date:", "離職日期:") },
            { "Resign_Reason", new MultiLanguageLabel("Resign Reason:", "Resign Reason:", "離職原因:") },
            { "Benefits", new MultiLanguageLabel("Benefits", "Benefits", "權力") },
            { "LSalary", new MultiLanguageLabel("Last Month Salary:", "Last Month Salary:", "上月薪資:") },
            { "TSalary", new MultiLanguageLabel("This Month Salary:", "This Month Salary:", "當月薪資:") },
            { "Benefits_Total", new MultiLanguageLabel("Benefits Total:", "Benefits Total:", "權力加扣項合計:") },
            { "Obligations", new MultiLanguageLabel("Obligations", "Obligations", "義務") },
            { "Obligations_Total", new MultiLanguageLabel("Obligations Total:", "Obligations Total:", "義務加扣項合計:") },
            { "Resigner", new MultiLanguageLabel("Resigner:", "Resigner:", "離職者:") },
            { "HR_Dept", new MultiLanguageLabel("HR Dept:", "HR Dept:", "人力資源部:") },
            { "FIN_Accounting", new MultiLanguageLabel("FIN-Accounting:", "FIN-Accounting:", "財務-會計室:") },
        };
        #endregion
    }
}