using System.Globalization;
using API.Data;
using API._Services.Interfaces.SalaryReport;
using API.DTOs.SalaryReport;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using API.Helper.Utilities;
using System.Drawing;
using Aspose.Cells;

namespace API._Services.Services.SalaryReport
{
    public class S_7_2_9_SalarySummaryReportExitedEmployee : BaseServices, I_7_2_9_SalarySummaryReportExitedEmployee
    {
        public S_7_2_9_SalarySummaryReportExitedEmployee(DBContext dbContext) : base(dbContext)
        {
        }

        private async Task<OperationResult> GetData(SalarySummaryReportExitedEmployeeParam param)
        {
            if (string.IsNullOrWhiteSpace(param.Factory)
                || !param.Permission_Group.Any()
                || string.IsNullOrWhiteSpace(param.Resignation_Start)
                || !DateTime.TryParseExact(param.Resignation_Start, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime ResignationStart)
                || string.IsNullOrWhiteSpace(param.Resignation_End)
                || !DateTime.TryParseExact(param.Resignation_End, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime ResignationEnd))
                return new OperationResult(false, "SalaryReport.SalarySummaryReportExitedEmployee.InvalidInput");

            var SalMonth = new DateTime(ResignationStart.Year, ResignationStart.Month, 1);
            var SalYear = new DateTime(ResignationStart.Year, 1, 1);

            var HSC = await _repositoryAccessor.HRMS_Sal_Close
                .FindAll(x => x.Factory == param.Factory
                      && x.Sal_Month == SalMonth
                      && x.Close_Status == "Y"
                      && param.Permission_Group.Contains(x.Permission_Group))
                .Select(x => x.Employee_ID)
                .Distinct()
                .ToListAsync();

            var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory
                                                                    && x.Resign_Date >= ResignationStart
                                                                    && x.Resign_Date <= ResignationEnd);

            var predHSRM = PredicateBuilder.New<HRMS_Sal_Resign_Monthly>(x => x.Factory == param.Factory
                                                                           && x.Sal_Month == SalMonth
                                                                           && !HSC.Contains(x.Employee_ID));

            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                predHEP.And(x => x.Employee_ID.Contains(param.Employee_ID));

            if (!string.IsNullOrWhiteSpace(param.Department))
                predHSRM.And(x => x.Department == param.Department);

            if (param.Transfer != "All")
                predHSRM.And(x => x.BankTransfer == param.Transfer);

            var wk_sql = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true)
                .Join(_repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(predHSRM, true),
                    x => x.Employee_ID,
                    y => y.Employee_ID,
                    (x, y) => new { HEP = x, HSRM = y })
                .OrderBy(x => x.HSRM.Department)
                .ToListAsync();

            var HSSCCM = _repositoryAccessor.HRMS_Sal_Dept_SAPCostCenter_Mapping
                .FindAll(x => x.Factory == param.Factory
                           && x.Cost_Year == SalYear, true)
                .ToList();

            var HSM = _repositoryAccessor.HRMS_Sal_Monthly
                .FindAll(x => x.Factory == param.Factory
                           && x.Sal_Month == SalMonth.AddMonths(-1), true)
                .ToList();

            var HSRM = _repositoryAccessor.HRMS_Sal_Resign_Monthly
                .FindAll(x => x.Factory == param.Factory
                           && x.Sal_Month == SalMonth, true)
                .ToList();

            var HSADIS = _repositoryAccessor.HRMS_Sal_AddDedItem_Settings
                .FindAll(x => x.Factory == param.Factory
                           && x.Effective_Month <= SalMonth, true)
                .ToList();

            var HSADIM = _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly
                .FindAll(x => x.Factory == param.Factory
                           && x.Sal_Month == SalMonth, true)
                .ToList();

            var HSMD = _repositoryAccessor.HRMS_Sal_Monthly_Detail
                .FindAll(x => x.Factory == param.Factory
                           && x.Sal_Month == SalMonth.AddMonths(-1), true)
                .ToList();

            var HSRMD = _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
                .FindAll(x => x.Factory == param.Factory
                           && x.Sal_Month == SalMonth, true)
                .ToList();

            var result = new List<SalarySummaryReportExitedEmployeeData>();

            Query_Sal_Monthly_Detail_Sum_Data query = new()
            {
                HRMS_Sal_Monthly_Detail = HSMD,
                HRMS_Sal_Resign_Monthly_Detail = HSRMD,
            };

            foreach (var cr_pt1 in wk_sql)
            {
                var Emp_Personal = cr_pt1.HEP;
                var Sal_Resign_Monthly = cr_pt1.HSRM;

                // 2
                var CCenter = HSSCCM.FirstOrDefault(x => x.Department == Sal_Resign_Monthly.Department)?.Cost_Code;

                // 3
                var LAdd_Sum = Query_Sal_Monthly_Detail_Add_Sum(query, "Y", Sal_Resign_Monthly.Factory, Sal_Resign_Monthly.Sal_Month.AddMonths(-1), Sal_Resign_Monthly.Employee_ID);
                var LDed_Sum = Query_Sal_Monthly_Detail_Ded_Sum(query, "Y", Sal_Resign_Monthly.Factory, Sal_Resign_Monthly.Sal_Month.AddMonths(-1), Sal_Resign_Monthly.Employee_ID);
                var LTax = HSM.FirstOrDefault(x => x.Employee_ID == Sal_Resign_Monthly.Employee_ID)?.Tax ?? 0;

                var LSalary = LAdd_Sum - LDed_Sum - LTax;

                // 4
                var TAdd_Sum = Query_Sal_Monthly_Detail_Add_Sum(query, "N", Sal_Resign_Monthly.Factory, Sal_Resign_Monthly.Sal_Month, Sal_Resign_Monthly.Employee_ID);
                var TDed_Sum = Query_Sal_Monthly_Detail_Ded_Sum(query, "N", Sal_Resign_Monthly.Factory, Sal_Resign_Monthly.Sal_Month, Sal_Resign_Monthly.Employee_ID);
                var TTax = HSRM.FirstOrDefault(x => x.Employee_ID == Sal_Resign_Monthly.Employee_ID)?.Tax ?? 0;

                var TSalary = TAdd_Sum - TDed_Sum - TTax;

                // 5
                var Max_Effective_Month = HSADIS
                    .Where(x => x.Permission_Group == Sal_Resign_Monthly.Permission_Group
                             && x.Salary_Type == Sal_Resign_Monthly.Salary_Type)
                    .Select(x => x.Effective_Month)
                    .OrderByDescending(x => x)
                    .FirstOrDefault();

                // 6
                var addlist = HSADIS
                    .Where(x => x.Permission_Group == Sal_Resign_Monthly.Permission_Group
                                 && x.Salary_Type == Sal_Resign_Monthly.Salary_Type
                                 && x.Effective_Month == Max_Effective_Month
                                 && (x.AddDed_Item.StartsWith("A") || x.AddDed_Item.StartsWith("B"))
                                 && x.Resigned_Print == "Y")
                    .Select(x => x.AddDed_Item)
                    .OrderBy(x => x)
                    .ToList();

                var addlistAmt = HSADIM
                    .Where(x => x.Employee_ID == Sal_Resign_Monthly.Employee_ID
                        && addlist.Contains(x.AddDed_Item))
                    .Select(x => new KeyValuePair<string, int>(x.AddDed_Item, x.Amount))
                    .ToList();

                // 7
                var delist = HSADIS
                    .Where(x => x.Permission_Group == Sal_Resign_Monthly.Permission_Group
                                 && x.Salary_Type == Sal_Resign_Monthly.Salary_Type
                                 && x.Effective_Month == Max_Effective_Month
                                 && (x.AddDed_Item.StartsWith("C") || x.AddDed_Item.StartsWith("D"))
                                 && x.Resigned_Print == "Y")
                    .Select(x => x.AddDed_Item)
                    .OrderBy(x => x)
                    .ToList();

                var delistAmt = HSADIM
                    .Where(x => x.Employee_ID == Sal_Resign_Monthly.Employee_ID
                        && delist.Contains(x.AddDed_Item))
                    .Select(x => new KeyValuePair<string, int>(x.AddDed_Item, x.Amount))
                    .ToList();

                var act_get = LSalary + TSalary + addlistAmt.Sum(x => x.Value) - delistAmt.Sum(x => x.Value);
                var act_sub = act_get < 0 ? (LSalary + TSalary + addlistAmt.Sum(x => x.Value)) : delistAmt.Sum(x => x.Value);

                var changetotal = Math.Max(0, act_get);

                var data = new SalarySummaryReportExitedEmployeeData
                {
                    Department = Sal_Resign_Monthly.Department,
                    CCenter = CCenter,
                    Employee_ID = Sal_Resign_Monthly.Employee_ID,
                    Local_Full_Name = Emp_Personal.Local_Full_Name,
                    Onboard_Date = Emp_Personal.Onboard_Date,
                    Resign_Date = Emp_Personal.Resign_Date,
                    LSalary = LSalary,
                    TSalary = TSalary,
                    addlist = addlistAmt,
                    addlistAmt = addlistAmt.Sum(x => x.Value),
                    delist = delistAmt,
                    delistAmt = delistAmt.Sum(x => x.Value),
                    act_sub = act_sub,
                    act_get = act_get,
                    changetotal = changetotal
                };

                result.Add(data);
            }

            return new OperationResult(true, result);
        }

        #region GetTotalRows
        public async Task<OperationResult> GetTotalRows(SalarySummaryReportExitedEmployeeParam param)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;
            var data = (List<SalarySummaryReportExitedEmployeeData>)result.Data;

            return new OperationResult(true, data.Count);
        }
        #endregion

        #region Download
        public async Task<OperationResult> Download(SalarySummaryReportExitedEmployeeParam param)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;

            var data = (List<SalarySummaryReportExitedEmployeeData>)result.Data;

            if (data.Count == 0)
                return new OperationResult(false, "System.Message.NoData");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources\\Template\\SalaryReport\\7_2_9_SalarySummaryReportExitedEmployee\\Download.xlsx");

            if (!File.Exists(filePath))
                return new OperationResult(false, "File not found");

            var ResignationStart = DateTime.Parse(param.Resignation_Start);
            var SalMonth = new DateTime(ResignationStart.Year, ResignationStart.Month, 1);
            var type_Seq = new List<string>()
            {
                BasicCodeTypeConstant.Factory,
                BasicCodeTypeConstant.PermissionGroup,
                BasicCodeTypeConstant.AdditionsAndDeductionsItem
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
                    Code_Name = $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                })
                .ToListAsync();

            var DepartmentLanguage = await _repositoryAccessor.HRMS_Org_Department
                    .FindAll(x => x.Factory == param.Factory, true)
                    .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true),
                        x => new { x.Division, x.Factory, x.Department_Code },
                        y => new { y.Division, y.Factory, y.Department_Code },
                        (HOD, HODL) => new { HOD, HODL })
                    .SelectMany(x => x.HODL.DefaultIfEmpty(),
                        (x, y) => new { x.HOD, HODL = y })
                    .Select(x => new
                    {
                        x.HOD.Department_Code,
                        Department_Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name,
                        Department_Title = $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}"
                    })
                    .ToListAsync();

            var department = DepartmentLanguage
                .FirstOrDefault(x => x.Department_Code == param.Department)?.Department_Title;

            var factory = BasicCodeLanguage
                .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.Factory
                    && x.Code == param.Factory).Code_Name;

            var permissionGroup = BasicCodeLanguage
                .Where(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup
                    && param.Permission_Group.Contains(x.Code))
                .Select(x => x.Code_Name);

            data.ForEach(item =>
            {
                item.Department_Name = DepartmentLanguage.FirstOrDefault(x => x.Department_Code == item.Department)?.Department_Name;
            });

            var addlist = data.SelectMany(x => x.addlist.Select(x => x.Key)).Distinct().OrderBy(x => x).ToList();
            var delist = data.SelectMany(x => x.delist.Select(x => x.Key)).Distinct().OrderBy(x => x).ToList();

            WorkbookDesigner designer = new()
            {
                Workbook = new Workbook(filePath)
            };
            Worksheet worksheet = designer.Workbook.Worksheets[0];
            designer.SetDataSource("result", data);
            designer.Process();

            Style borderStyle = new CellsFactory().CreateStyle();
            borderStyle = AsposeUtility.SetAllBorders(borderStyle);

            Style intStyle = new CellsFactory().CreateStyle();
            intStyle.Number = 3;

            Style sumStyle = new CellsFactory().CreateStyle();
            sumStyle.Number = 3;
            sumStyle.Pattern = BackgroundType.Solid;
            sumStyle.ForegroundColor = Color.FromArgb(255, 242, 204);

            Style groupStyle = new CellsFactory().CreateStyle();
            groupStyle.Pattern = BackgroundType.Solid;
            groupStyle.ForegroundColor = Color.FromArgb(252, 228, 214);

            Style labelAddStyle = new CellsFactory().CreateStyle();
            labelAddStyle = AsposeUtility.SetAllBorders(labelAddStyle);
            labelAddStyle.Pattern = BackgroundType.Solid;
            labelAddStyle.ForegroundColor = Color.FromArgb(217, 225, 242);

            Style labelDedStyle = new CellsFactory().CreateStyle();
            labelDedStyle = AsposeUtility.SetAllBorders(labelDedStyle);
            labelDedStyle.Pattern = BackgroundType.Solid;
            labelDedStyle.ForegroundColor = Color.FromArgb(226, 239, 218);

            worksheet.Cells["B2"].Value = factory;
            worksheet.Cells["D2"].Value = param.Resignation_Start + "~" + param.Resignation_End;
            worksheet.Cells["F2"].Value = string.Join(", ", permissionGroup);
            worksheet.Cells["H2"].Value = param.Transfer == "Y" ? "轉帳 Transfer" : param.Transfer == "N" ? "轉帳 No Transfer" : "全部 All";
            worksheet.Cells["J2"].Value = !string.IsNullOrEmpty(department) ? department : param.Department;
            worksheet.Cells["L2"].Value = param.Employee_ID;
            worksheet.Cells["B4"].Value = param.UserName;
            worksheet.Cells["D4"].Value = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            worksheet.Cells["H6"].Value = SalMonth.AddMonths(-1).ToString("yyyy/MM") + "薪資";
            worksheet.Cells["H7"].Value = SalMonth.AddMonths(-1).ToString("yyyy/MM") + " Salary";
            worksheet.Cells["I6"].Value = SalMonth.ToString("yyyy/MM") + "薪資";
            worksheet.Cells["I7"].Value = SalMonth.ToString("yyyy/MM") + " Salary";

            var rowIndex = 7;
            var colHeader = 7;

            worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.LSalary);
            worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
            colHeader++;
            worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.TSalary);
            worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
            colHeader++;
            // worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);

            // Header Add
            worksheet.Cells.Merge(5, colHeader, 1, addlist.Count + 1);
            worksheet.Cells[5, colHeader].Value = "加項";
            if (addlist.Count > 0)
                worksheet.Cells[5, colHeader].GetMergedRange().SetStyle(labelAddStyle);
            else
                worksheet.Cells[5, colHeader].SetStyle(labelAddStyle);

            foreach (var addCode in addlist)
            {
                worksheet.Cells[6, colHeader].Value = BasicCodeLanguage
                    .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsItem
                                      && x.Code == addCode)?.Code_Name;
                worksheet.Cells[6, colHeader].SetStyle(labelAddStyle);
                worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.addlist.FirstOrDefault(x => x.Key == addCode).Value);
                worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
                colHeader++;
            }

            worksheet.Cells[6, colHeader].Value = "總合計AddTotal";
            worksheet.Cells[6, colHeader].SetStyle(labelAddStyle);
            worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.addlistAmt);
            worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
            colHeader++;

            // Header Ded
            worksheet.Cells.Merge(5, colHeader, 1, delist.Count + 1);
            worksheet.Cells[5, colHeader].Value = "扣項";
            if (delist.Count > 0)
                worksheet.Cells[5, colHeader].GetMergedRange().SetStyle(labelDedStyle);
            else
                worksheet.Cells[5, colHeader].SetStyle(labelDedStyle);

            foreach (var deCode in delist)
            {
                worksheet.Cells[6, colHeader].Value = BasicCodeLanguage
                    .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsItem
                                      && x.Code == deCode)?.Code_Name;
                worksheet.Cells[6, colHeader].SetStyle(labelDedStyle);
                worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.delist.FirstOrDefault(x => x.Key == deCode).Value);
                worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
                colHeader++;
            }

            worksheet.Cells[6, colHeader].Value = "總扣額DedTotal";
            worksheet.Cells[6, colHeader].SetStyle(labelDedStyle);
            worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.delistAmt);
            worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
            colHeader++;

            worksheet.Cells[5, colHeader].Value = "實扣";
            worksheet.Cells[5, colHeader].SetStyle(borderStyle);
            worksheet.Cells[6, colHeader].Value = "Actual deduction";
            worksheet.Cells[6, colHeader].SetStyle(borderStyle);
            worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.act_sub);
            worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
            colHeader++;

            worksheet.Cells[5, colHeader].Value = "實領";
            worksheet.Cells[5, colHeader].SetStyle(borderStyle);
            worksheet.Cells[6, colHeader].Value = "Actual collection";
            worksheet.Cells[6, colHeader].SetStyle(borderStyle);
            worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.changetotal);
            worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
            colHeader++;

            worksheet.Cells[5, colHeader].Value = "簽名";
            worksheet.Cells[5, colHeader].SetStyle(borderStyle);
            worksheet.Cells[6, colHeader].Value = "Sign";
            worksheet.Cells[6, colHeader].SetStyle(borderStyle);
            worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
            colHeader++;

            for (int i = 0; i < data.Count; i++)
            {
                var columnIndex = 9;
                foreach (var addCode in addlist)
                {
                    worksheet.Cells[rowIndex + i, columnIndex++].Value = data[i].addlist.FirstOrDefault(x => x.Key == addCode).Value;
                }
                worksheet.Cells[rowIndex + i, columnIndex++].Value = data[i].addlistAmt;

                foreach (var deCode in delist)
                {
                    worksheet.Cells[rowIndex + i, columnIndex++].Value = data[i].delist.FirstOrDefault(x => x.Key == deCode).Value;
                }
                worksheet.Cells[rowIndex + i, columnIndex++].Value = data[i].delistAmt;
                worksheet.Cells[rowIndex + i, columnIndex++].Value = data[i].act_sub;
                worksheet.Cells[rowIndex + i, columnIndex++].Value = data[i].act_get;
            }
            Aspose.Cells.Range intRange = worksheet.Cells.CreateRange(7, 9, data.Count, 4 + addlist.Count + delist.Count);
            intRange.SetStyle(intStyle);

            // Group CCenter
            rowIndex = 11 + data.Count;
            var dataGroup = data.GroupBy(x => x.CCenter).OrderBy(x => x.Key).ToList();
            Aspose.Cells.Range groupRange = worksheet.Cells.CreateRange(11 + data.Count, 0, dataGroup.Count, 14 + addlist.Count + delist.Count);
            groupRange.SetStyle(groupStyle);
            groupStyle.Number = 3;
            foreach (var item in dataGroup)
            {
                var columnIndex = 7;

                worksheet.Cells[rowIndex, 0].Value = "成本中心";
                worksheet.Cells[rowIndex, 1].Value = "小計";
                worksheet.Cells[rowIndex, 2].Value = item.Key;

                worksheet.Cells[rowIndex, columnIndex].Value = item.Sum(x => x.LSalary);
                worksheet.Cells[rowIndex, columnIndex].SetStyle(groupStyle);
                columnIndex++;

                worksheet.Cells[rowIndex, columnIndex].Value = item.Sum(x => x.TSalary);
                worksheet.Cells[rowIndex, columnIndex].SetStyle(groupStyle);
                columnIndex++;

                foreach (var addCode in addlist)
                {
                    worksheet.Cells[rowIndex, columnIndex].Value = item.SelectMany(x => x.addlist).Where(x => x.Key == addCode).Sum(x => x.Value);
                    worksheet.Cells[rowIndex, columnIndex].SetStyle(groupStyle);
                    columnIndex++;
                }
                worksheet.Cells[rowIndex, columnIndex].Value = item.Sum(x => x.addlistAmt);
                worksheet.Cells[rowIndex, columnIndex].SetStyle(groupStyle);
                columnIndex++;

                foreach (var deCode in delist)
                {
                    worksheet.Cells[rowIndex, columnIndex].Value = item.SelectMany(x => x.delist).Where(x => x.Key == deCode).Sum(x => x.Value);
                    worksheet.Cells[rowIndex, columnIndex].SetStyle(groupStyle);
                    columnIndex++;
                }
                worksheet.Cells[rowIndex, columnIndex].Value = item.Sum(x => x.delistAmt);
                worksheet.Cells[rowIndex, columnIndex].SetStyle(groupStyle);
                columnIndex++;

                worksheet.Cells[rowIndex, columnIndex].Value = item.Sum(x => x.act_sub);
                worksheet.Cells[rowIndex, columnIndex].SetStyle(groupStyle);
                columnIndex++;

                worksheet.Cells[rowIndex, columnIndex].Value = item.Sum(x => x.act_get);
                worksheet.Cells[rowIndex, columnIndex].SetStyle(groupStyle);
                columnIndex++;
                rowIndex++;
            }

            MemoryStream memoryStream = new();
            designer.Workbook.Save(memoryStream, SaveFormat.Xlsx);
            return new OperationResult(true, new { TotalRows = data.Count, Excel = memoryStream.ToArray() });
        }
        #endregion

        private int Query_Sal_Monthly_Detail_Add_Sum(Query_Sal_Monthly_Detail_Sum_Data Data, string Kind, string Factory, DateTime Year_Month, string Employee_ID)
        {
            int total1 = Query_Sal_Monthly_Detail_Sum(Data, Kind, Factory, Year_Month, Employee_ID, "45", "A");
            int total2 = Query_Sal_Monthly_Detail_Sum(Data, Kind, Factory, Year_Month, Employee_ID, "42", "A");
            int total3 = Query_Sal_Monthly_Detail_Sum(Data, Kind, Factory, Year_Month, Employee_ID, "49", "A");
            int total4 = Query_Sal_Monthly_Detail_Sum(Data, Kind, Factory, Year_Month, Employee_ID, "49", "B");

            int addTotal = total1 + total2 + total3 + total4;
            return addTotal;
        }

        private int Query_Sal_Monthly_Detail_Ded_Sum(Query_Sal_Monthly_Detail_Sum_Data Data, string Kind, string Factory, DateTime Year_Month, string Employee_ID)
        {
            int total1 = Query_Sal_Monthly_Detail_Sum(Data, Kind, Factory, Year_Month, Employee_ID, "57", "D");
            int total2 = Query_Sal_Monthly_Detail_Sum(Data, Kind, Factory, Year_Month, Employee_ID, "49", "C");
            int total3 = Query_Sal_Monthly_Detail_Sum(Data, Kind, Factory, Year_Month, Employee_ID, "49", "D");

            int addTotal = total1 + total2 + total3;
            return addTotal;
        }

        private static int Query_Sal_Monthly_Detail_Sum(Query_Sal_Monthly_Detail_Sum_Data data, string kind, string factory, DateTime yearMonth, string employeeId, string typeSeq, string addedType)
        {
            if (kind == "Y")
            {
                return data.HRMS_Sal_Monthly_Detail
                    .Where(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 x.Employee_ID == employeeId &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType)
                    .Sum(x => (int?)x.Amount ?? 0);
            }
            else if (kind == "N")
            {
                return data.HRMS_Sal_Resign_Monthly_Detail
                    .Where(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 x.Employee_ID == employeeId &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType)
                    .Sum(x => (int?)x.Amount ?? 0);
            }
            return 0;
            // else if (kind == "PY")
            // {
            //     return _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail
            //         .FindAll(x => x.Factory == factory &&
            //                      x.Sal_Month == yearMonth &&
            //                      x.Employee_ID == employeeId &&
            //                      x.Probation == "Y" &&
            //                      x.Type_Seq == typeSeq &&
            //                      x.AddDed_Type == addedType, true)
            //         .Sum(x => (int?)x.Amount ?? 0);
            // }
            // else // kind == "PN"
            // {
            //     return _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail
            //         .FindAll(x => x.Factory == factory &&
            //                      x.Sal_Month == yearMonth &&
            //                      x.Employee_ID == employeeId &&
            //                      x.Probation == "N" &&
            //                      x.Type_Seq == typeSeq &&
            //                      x.AddDed_Type == addedType, true)
            //         .Sum(x => (int?)x.Amount ?? 0);
            // }
        }

        #region Get List
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
        #endregion

        private class Query_Sal_Monthly_Detail_Sum_Data
        {
            public List<HRMS_Sal_Monthly_Detail> HRMS_Sal_Monthly_Detail { get; set; }
            public List<HRMS_Sal_Resign_Monthly_Detail> HRMS_Sal_Resign_Monthly_Detail { get; set; }
        }
    }
}