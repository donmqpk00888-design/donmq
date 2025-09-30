using System.Globalization;
using API.Data;
using API._Services.Interfaces.SalaryReport;
using API.DTOs.SalaryReport;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Aspose.Cells;
using System.Drawing;

namespace API._Services.Services.SalaryReport
{
    public class S_7_2_10_SalarySummaryReportExitedEmployeeByDepartment : BaseServices, I_7_2_10_SalarySummaryReportExitedEmployeeByDepartment
    {
        public S_7_2_10_SalarySummaryReportExitedEmployeeByDepartment(DBContext dbContext) : base(dbContext)
        {
        }

        private async Task<OperationResult> GetData(SalarySummaryReportExitedEmployeeByDepartmentParam param)
        {
            if (string.IsNullOrWhiteSpace(param.Factory)
                || !param.Permission_Group.Any()
                || string.IsNullOrWhiteSpace(param.Resignation_Start)
                || !DateTime.TryParseExact(param.Resignation_Start, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime ResignationStart)
                || string.IsNullOrWhiteSpace(param.Resignation_End)
                || !DateTime.TryParseExact(param.Resignation_End, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime ResignationEnd))
                return new OperationResult(false, "SalaryReport.SalarySummaryReportExitedEmployeeByDepartment.InvalidInput");

            var SalMonth = new DateTime(ResignationStart.Year, ResignationStart.Month, 1);

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


            var wk_sql = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP)
                .Join(_repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(predHSRM),
                    x => x.Employee_ID,
                    y => y.Employee_ID,
                    (x, y) => new { HEP = x, HSRM = y })
                .OrderBy(x => x.HSRM.Department)
                .ToListAsync();

            var HSM = _repositoryAccessor.HRMS_Sal_Monthly
                .FindAll(x => x.Factory == param.Factory
                           && x.Sal_Month == SalMonth.AddMonths(-1))
                .ToList();

            var HSRM = _repositoryAccessor.HRMS_Sal_Resign_Monthly
                .FindAll(x => x.Factory == param.Factory
                           && x.Sal_Month == SalMonth)
                .ToList();

            var HSADIS = _repositoryAccessor.HRMS_Sal_AddDedItem_Settings
                .FindAll(x => x.Factory == param.Factory
                           && x.Effective_Month <= SalMonth)
                .ToList();

            var HSADIM = _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly
                .FindAll(x => x.Factory == param.Factory
                           && x.Sal_Month == SalMonth)
                .ToList();

            var HSMD = _repositoryAccessor.HRMS_Sal_Monthly_Detail
                .FindAll(x => x.Factory == param.Factory
                           && x.Sal_Month == SalMonth.AddMonths(-1), true)
                .ToList();

            var HSRMD = _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
                .FindAll(x => x.Factory == param.Factory
                           && x.Sal_Month == SalMonth, true)
                .ToList();

            var result = new List<SalarySummaryReportExitedEmployeeByDepartmentData>();

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
                var LAdd_Sum = Query_Sal_Monthly_Detail_Add_Sum(query, "Y", Sal_Resign_Monthly.Factory, Sal_Resign_Monthly.Sal_Month.AddMonths(-1), Sal_Resign_Monthly.Employee_ID);
                var LDed_Sum = Query_Sal_Monthly_Detail_Ded_Sum(query, "Y", Sal_Resign_Monthly.Factory, Sal_Resign_Monthly.Sal_Month.AddMonths(-1), Sal_Resign_Monthly.Employee_ID);
                var LTax = HSM.FirstOrDefault(x => x.Employee_ID == Sal_Resign_Monthly.Employee_ID)?.Tax ?? 0;

                var LSalary = LAdd_Sum - LDed_Sum - LTax;

                // 3
                var TAdd_Sum = Query_Sal_Monthly_Detail_Add_Sum(query, "N", Sal_Resign_Monthly.Factory, Sal_Resign_Monthly.Sal_Month, Sal_Resign_Monthly.Employee_ID);
                var TDed_Sum = Query_Sal_Monthly_Detail_Ded_Sum(query, "N", Sal_Resign_Monthly.Factory, Sal_Resign_Monthly.Sal_Month, Sal_Resign_Monthly.Employee_ID);
                var TTax = HSRM.FirstOrDefault(x => x.Employee_ID == Sal_Resign_Monthly.Employee_ID)?.Tax ?? 0;

                var TSalary = TAdd_Sum - TDed_Sum - TTax;

                // 4
                var AddTotal = Query_Sal_Monthly_Detail_Add_Sum(query, "N", Sal_Resign_Monthly.Factory, Sal_Resign_Monthly.Sal_Month, Sal_Resign_Monthly.Employee_ID);

                // 5
                var SubTotal = Query_Sal_Monthly_Detail_Ded_Sum(query, "N", Sal_Resign_Monthly.Factory, Sal_Resign_Monthly.Sal_Month, Sal_Resign_Monthly.Employee_ID) + TTax;

                // 6
                var Max_Effective_Month = HSADIS
                    .Where(x => x.Permission_Group == Sal_Resign_Monthly.Permission_Group
                             && x.Salary_Type == Sal_Resign_Monthly.Salary_Type)
                    .Select(x => x.Effective_Month)
                    .OrderByDescending(x => x)
                    .FirstOrDefault();

                // 7
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

                // 8
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

                var data = new SalarySummaryReportExitedEmployeeByDepartmentData
                {
                    Department = Sal_Resign_Monthly.Department,
                    LSalary = LSalary,
                    TSalary = TSalary,
                    AddTotal = AddTotal,
                    SubTotal = SubTotal,
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

            result = result
                .GroupBy(x => x.Department)
                .Select(x => new SalarySummaryReportExitedEmployeeByDepartmentData
                {
                    Department = x.Key,
                    Count = x.Count(),
                    LSalary = x.Sum(y => y.LSalary),
                    TSalary = x.Sum(y => y.TSalary),
                    AddTotal = x.Sum(y => y.AddTotal),
                    SubTotal = x.Sum(y => y.SubTotal),
                    addlist = x.SelectMany(y => y.addlist).Distinct().ToList(),
                    addlistAmt = x.Sum(y => y.addlistAmt),
                    delist = x.SelectMany(y => y.delist).Distinct().ToList(),
                    act_get = x.Sum(y => y.act_get),
                    act_sub = x.Sum(y => y.act_sub),
                    changetotal = x.Sum(y => y.changetotal),
                })
                .ToList();

            return new OperationResult(true, result);
        }

        #region GetTotalRows
        public async Task<OperationResult> GetTotalRows(SalarySummaryReportExitedEmployeeByDepartmentParam param)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;
            var data = (List<SalarySummaryReportExitedEmployeeByDepartmentData>)result.Data;
            return new OperationResult(true, data.Count);
        }
        #endregion

        #region Download
        public async Task<OperationResult> Download(SalarySummaryReportExitedEmployeeByDepartmentParam param)
        {
            var result = await GetData(param);

            if (!result.IsSuccess)
                return result;

            var data = (List<SalarySummaryReportExitedEmployeeByDepartmentData>)result.Data;

            if (data.Count == 0)
                return new OperationResult(false, "System.Message.NoData");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources\\Template\\SalaryReport\\7_2_10_SalarySummaryReportExitedEmployeeByDepartment\\Download.xlsx");

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
            sumStyle.Copy(intStyle);
            sumStyle.Pattern = BackgroundType.Solid;
            sumStyle.ForegroundColor = Color.FromArgb(255, 242, 204);

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
            worksheet.Cells["H2"].Value = !string.IsNullOrEmpty(department) ? department : param.Department;
            worksheet.Cells["J2"].Value = param.Employee_ID;
            worksheet.Cells["B4"].Value = param.UserName;
            worksheet.Cells["D4"].Value = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            var rowIndex = 7;
            var colHeader = 2;

            worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.Count);
            worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
            colHeader++;
            worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.LSalary);
            worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
            colHeader++;
            worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.AddTotal);
            worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
            colHeader++;
            worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.SubTotal);
            worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
            colHeader++;
            worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.TSalary);
            worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
            colHeader++;

            foreach (var addCode in addlist)
            {
                worksheet.Cells.Merge(5, colHeader, 2, 1);
                worksheet.Cells[5, colHeader].Value = BasicCodeLanguage
                    .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsItem
                                      && x.Code == addCode)?.Code_Name;
                worksheet.Cells[5, colHeader].GetMergedRange().SetStyle(labelAddStyle);
                worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.addlist.FirstOrDefault(x => x.Key == addCode).Value);
                worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
                colHeader++;
            }

            worksheet.Cells.Merge(5, colHeader, 2, 1);
            worksheet.Cells[5, colHeader].Value = "總合計AddTotal";
            worksheet.Cells[5, colHeader].GetMergedRange().SetStyle(labelAddStyle);
            worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.addlistAmt);
            worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
            colHeader++;

            foreach (var deCode in delist)
            {
                worksheet.Cells.Merge(5, colHeader, 2, 1);
                worksheet.Cells[5, colHeader].Value = BasicCodeLanguage
                    .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsItem
                                      && x.Code == deCode)?.Code_Name;
                worksheet.Cells[5, colHeader].GetMergedRange().SetStyle(labelDedStyle);
                worksheet.Cells[7 + data.Count, colHeader].Value = data.Sum(x => x.delist.FirstOrDefault(x => x.Key == deCode).Value);
                worksheet.Cells[7 + data.Count, colHeader].SetStyle(sumStyle);
                colHeader++;
            }

            worksheet.Cells.Merge(5, colHeader, 2, 1);
            worksheet.Cells[5, colHeader].Value = "總扣額DedTotal";
            worksheet.Cells[5, colHeader].GetMergedRange().SetStyle(labelDedStyle);
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

            for (int i = 0; i < data.Count; i++)
            {
                var columnIndex = 7;
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
            Aspose.Cells.Range intRange = worksheet.Cells.CreateRange(7, 7, data.Count, 4 + addlist.Count + delist.Count);
            intRange.SetStyle(intStyle);

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