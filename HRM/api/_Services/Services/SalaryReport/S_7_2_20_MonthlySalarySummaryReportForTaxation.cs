using System;
using System.Drawing;
using AgileObjects.AgileMapper.Extensions.Internal;
using API._Services.Interfaces.SalaryReport;
using API.Data;
using API.DTOs;
using API.DTOs.SalaryReport;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryReport
{
    public class S_7_2_20_MonthlySalarySummaryReportForTaxation : BaseServices, I_7_2_20_MonthlySalarySummaryReportForTaxation
    {
        public S_7_2_20_MonthlySalarySummaryReportForTaxation(DBContext dbContext) : base(dbContext)
        {
        }

        #region GetDataKindO
        private IQueryable<MonthlySalarySummaryReportForTaxation_Dto> GetDataKindO(MonthlySalarySummaryReportForTaxation_Param param)
        {
            var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x =>
                x.Factory == param.Factory &&
                (x.Resign_Date != null || (x.Resign_Date.HasValue && x.Resign_Date.Value.Date > param.LYMEnd.Date)));

            if (!string.IsNullOrWhiteSpace(param.Department))
                predHEP.And(x => x.Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                predHEP.And(x => x.Employee_ID.Contains(param.Employee_ID));

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true);
            var HSM = _repositoryAccessor.HRMS_Sal_Monthly.FindAll(x =>
                x.Factory == param.Factory &&
                param.Permission_Group.Contains(x.Permission_Group) &&
                x.Sal_Month.Date <= param.YMEnd.Date &&
                x.Sal_Month.Date >= param.YMStart.Date);

            var wk_sql = HEP
                .Join(HSM,
                    x => x.Employee_ID,
                    y => y.Employee_ID,
                    (HEP, HSM) => new { HEP, HSM })
                .Select(x => new MonthlySalarySummaryReportForTaxation_Dto
                {
                    Resign_Date = x.HEP.Resign_Date,
                    Department = x.HEP.Department,
                    Position_Title = x.HEP.Position_Title,
                    Factory = x.HSM.Factory,
                    Sal_Month = x.HSM.Sal_Month,
                    Employee_ID = x.HSM.Employee_ID,
                    Permission_Group = x.HSM.Permission_Group,
                    Tax = x.HSM.Tax,
                    BankTransfer = x.HSM.BankTransfer,
                    Out_day = !x.HEP.Resign_Date.HasValue || (x.HEP.Resign_Date.HasValue && x.HEP.Resign_Date.Value.Date > param.LYMEnd.Date)
                        ? "Y" : "N"
                });
            return wk_sql;
        }
        #endregion
        #region GetDataKindR
        private IQueryable<MonthlySalarySummaryReportForTaxation_Dto> GetDataKindR(MonthlySalarySummaryReportForTaxation_Param param)
        {
            var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory);

            if (!string.IsNullOrWhiteSpace(param.Department))
                predHEP.And(x => x.Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                predHEP.And(x => x.Employee_ID.Contains(param.Employee_ID));

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true);
            var HSRM = _repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(x =>
                x.Factory == param.Factory &&
                param.Permission_Group.Contains(x.Permission_Group) &&
                x.Sal_Month.Date <= param.YMEnd.Date &&
                x.Sal_Month.Date >= param.YMStart.Date);

            var wk_sql = HEP
                .Join(HSRM,
                    x => x.Employee_ID,
                    y => y.Employee_ID,
                    (HEP, HSRM) => new { HEP, HSRM })
                .Select(x => new MonthlySalarySummaryReportForTaxation_Dto
                {
                    Factory = x.HSRM.Factory,
                    Resign_Date = x.HEP.Resign_Date,
                    Department = x.HEP.Department,
                    Position_Title = x.HEP.Position_Title,
                    Employee_ID = x.HEP.Employee_ID,
                    Sal_Month = x.HSRM.Sal_Month,
                    Permission_Group = x.HSRM.Permission_Group,
                    Tax = x.HSRM.Tax,
                    BankTransfer = x.HSRM.BankTransfer,
                    Out_day = !x.HEP.Resign_Date.HasValue || (x.HEP.Resign_Date.HasValue && x.HEP.Resign_Date.Value.Date > param.LYMEnd.Date)
                        ? "Y" : "N"
                });
            return wk_sql;
        }
        #endregion
        #region GetDataKindAll
        private IQueryable<MonthlySalarySummaryReportForTaxation_Dto> GetDataKindAll(MonthlySalarySummaryReportForTaxation_Param param)
        {
            var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory);

            if (!string.IsNullOrWhiteSpace(param.Department))
                predHEP.And(x => x.Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                predHEP.And(x => x.Employee_ID.Contains(param.Employee_ID));

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP);
            var HSM = _repositoryAccessor.HRMS_Sal_Monthly.FindAll(x =>
                x.Factory == param.Factory &&
                param.Permission_Group.Contains(x.Permission_Group) &&
                x.Sal_Month.Date <= param.YMEnd.Date &&
                x.Sal_Month.Date >= param.YMStart.Date);
            var HSRM = _repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(x =>
                x.Factory == param.Factory &&
                param.Permission_Group.Contains(x.Permission_Group) &&
                x.Sal_Month.Date <= param.YMEnd.Date &&
                x.Sal_Month.Date >= param.YMStart.Date);

            var wk_sql_O = HEP
                .Where(x =>
                    !x.Resign_Date.HasValue ||
                    (x.Resign_Date.HasValue && x.Resign_Date.Value.Date > param.LYMEnd.Date))
                .Join(HSM, // kind = Onjob :  x.Resign_Date != null
                    x => x.Employee_ID,
                    y => y.Employee_ID,
                    (HEP, HSM) => new { HEP, HSM })
                .Select(x => new MonthlySalarySummaryReportForTaxation_Dto
                {
                    Resign_Date = x.HEP.Resign_Date,
                    Department = x.HEP.Department,
                    Position_Title = x.HEP.Position_Title,
                    Factory = x.HSM.Factory,
                    Sal_Month = x.HSM.Sal_Month,
                    Employee_ID = x.HSM.Employee_ID,
                    Permission_Group = x.HSM.Permission_Group,
                    Tax = x.HSM.Tax,
                    BankTransfer = x.HSM.BankTransfer,
                    Out_day = !x.HEP.Resign_Date.HasValue || (x.HEP.Resign_Date.HasValue && x.HEP.Resign_Date.Value.Date > param.LYMEnd.Date)
                        ? "Y" : "N"
                });
            var wk_sql_R = HEP
                .Where(x =>
                    x.Resign_Date.HasValue &&
                    x.Resign_Date.Value.Date >= param.FYMStart.Date && x.Resign_Date.Value.Date <= param.LYMEnd.Date)
                .Join(HSRM, // kind = Resigned : not where
                    x => x.Employee_ID,
                    y => y.Employee_ID,
                    (HEP, HSRM) => new { HEP, HSRM })
                .Select(x => new MonthlySalarySummaryReportForTaxation_Dto
                {
                    Resign_Date = x.HEP.Resign_Date,
                    Department = x.HEP.Department,
                    Position_Title = x.HEP.Position_Title,
                    Factory = x.HSRM.Factory,
                    Sal_Month = x.HSRM.Sal_Month,
                    Employee_ID = x.HSRM.Employee_ID,
                    Permission_Group = x.HSRM.Permission_Group,
                    Tax = x.HSRM.Tax,
                    BankTransfer = x.HSRM.BankTransfer,
                    Out_day = !x.HEP.Resign_Date.HasValue || (x.HEP.Resign_Date.HasValue && x.HEP.Resign_Date.Value.Date > param.LYMEnd.Date)
                        ? "Y" : "N"
                });
            var wk_sql = wk_sql_O.Union(wk_sql_R);
            return wk_sql;
        }
        #endregion
        #region result Kind
        private IQueryable<MonthlySalarySummaryReportForTaxation_Dto> resultKind(MonthlySalarySummaryReportForTaxation_Param param)
        {
            param.YMStart = DateTime.Parse(param.Year_Month_Start);
            param.FYMStart = new(param.YMStart.Year, param.YMStart.Month, 1);
            param.YMEnd = DateTime.Parse(param.Year_Month_End);
            param.LYMEnd = param.YMEnd.AddMonths(1).AddDays(-1);

            IQueryable<MonthlySalarySummaryReportForTaxation_Dto> wk_sql = param.Kind switch
            {
                // 1	類別=在職 Kind = On Job	
                "OnJob" => GetDataKindO(param),
                // 2	類別=離職 Kind = Resigned	
                "Resigned" => GetDataKindR(param),
                // 3    類別=ALL Kind = ALL
                _ => GetDataKindAll(param),
            };
            return wk_sql;
        }
        #endregion
        #region GetData
        public async Task<int> GetTotalRows(MonthlySalarySummaryReportForTaxation_Param param)
        {
            var wk_sql = resultKind(param);
            return wk_sql != null ? await wk_sql.CountAsync() : 0;
        }
        private async Task<List<MonthlySalarySummaryReportForTaxation_Data>> GetData(MonthlySalarySummaryReportForTaxation_Param param)
        {
            List<MonthlySalarySummaryReportForTaxation_Data> result_List = new();
            var wk_sql = resultKind(param);

            if (wk_sql == null || !wk_sql.Any()) return result_List;

            var employeeIds = wk_sql.Select(x => x.Employee_ID).ToList();
            var salMonths = wk_sql.Select(x => x.Sal_Month.Date).Distinct().ToList();

            var HSF = _repositoryAccessor.HRMS_Sal_FinCategory.FindAll(x =>
                x.Factory == param.Factory &&
                x.Kind == "1" &&
                wk_sql.Select(y => y.Department).Contains(x.Department) &&
                wk_sql.Select(y => y.Position_Title).Contains(x.Code)
            ).ToList();
            var HSAM = _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.FindAll(x =>
                x.Factory == param.Factory &&
                salMonths.Contains(x.Sal_Month.Date) &&
                employeeIds.Contains(x.Employee_ID)
            ).ToList();
            var HSC = _repositoryAccessor.HRMS_Sal_Close.FindAll(x =>
                x.Factory == param.Factory &&
                salMonths.Contains(x.Sal_Month.Date) &&
                employeeIds.Contains(x.Employee_ID)
            ).ToList();
            var HSBA = _repositoryAccessor.HRMS_Sal_Bank_Account.FindAll(x =>
                x.Factory == param.Factory &&
                employeeIds.Contains(x.Employee_ID)
            ).ToList();

            var allSalaryResults = new List<SalaryDetailBatchResult>();
            var allOVTPayResults = new List<SalaryDetailBatchResult>();
            var allFeeDetails = new List<SalaryDetailBatchResult>();
            var allFoodDetails = new List<SalaryDetailBatchResult>();

            var groupMonths = wk_sql.GroupBy(x => x.Sal_Month)
                .Select(x => new
                {
                    Sal_Month = x.Key,
                    Out_Day_Groups = x.GroupBy(y => y.Out_day)
                        .Select(y => new
                        {
                            Out_day = y.Key,
                            Employees = y.Where(z => z.Employee_ID != null).Select(z => z.Employee_ID).ToList()
                        }).ToList(),
                }).ToList();
            var foodpayItems = new List<string> { "B54", "B55", "B56", "B57" };
            var feeItems = new List<string> { "D12", "V01", "V02", "V03" };
            foreach (var group in groupMonths)
            {
                foreach (var data in group.Out_Day_Groups)
                {
                    var salary = await Query_Sal_Monthly_Detail_Sum(data.Out_day, param.Factory, group.Sal_Month, data.Employees, "45", "A");
                    var oVTPay = await Query_Sal_Monthly_Detail_Sum(data.Out_day, param.Factory, group.Sal_Month, data.Employees, "42", "A");
                    allSalaryResults.AddRange(salary.Select(x => new SalaryDetailBatchResult { Employee_ID = x.Employee_ID, Sal_Month = group.Sal_Month, Amount = (int)x.Amount }));
                    allOVTPayResults.AddRange(oVTPay.Select(x => new SalaryDetailBatchResult { Employee_ID = x.Employee_ID, Sal_Month = group.Sal_Month, Amount = (int)x.Amount }));
                }
                var feeDetails = await Query_Single_Sal_Monthly_Detail("Y", param.Factory, group.Sal_Month, group.Out_Day_Groups.SelectMany(x => x.Employees).ToList(), "57", "D", feeItems);
                allFeeDetails.AddRange(feeDetails
                .Select(x => new SalaryDetailBatchResult
                {
                    Employee_ID = x.Employee_ID,
                    Sal_Month = group.Sal_Month,
                    Amount = (int)x.Amount,
                    Item = x.Item,
                }));
                var foofDetails = await Query_Single_Sal_Monthly_Detail("Y", param.Factory, group.Sal_Month, group.Out_Day_Groups.SelectMany(x => x.Employees).ToList(), "49", "B", foodpayItems);
                allFoodDetails.AddRange(foofDetails
                .Select(x => new SalaryDetailBatchResult
                {
                    Employee_ID = x.Employee_ID,
                    Sal_Month = group.Sal_Month,
                    Amount = (int)x.Amount,
                    Item = x.Item,
                }));

            }
            // 4	開始迴圈	Start looping
            foreach (var ev_pt in wk_sql)
            {
                var B54 = allFoodDetails.FirstOrDefault(x => x.Employee_ID == ev_pt.Employee_ID && x.Sal_Month.Date == ev_pt.Sal_Month.Date && x.Item == "B54")?.Amount ?? 0;
                var B55 = allFoodDetails.FirstOrDefault(x => x.Employee_ID == ev_pt.Employee_ID && x.Sal_Month.Date == ev_pt.Sal_Month.Date && x.Item == "B55")?.Amount ?? 0;
                var B56 = allFoodDetails.FirstOrDefault(x => x.Employee_ID == ev_pt.Employee_ID && x.Sal_Month.Date == ev_pt.Sal_Month.Date && x.Item == "B56")?.Amount ?? 0;
                var B57 = allFoodDetails.FirstOrDefault(x => x.Employee_ID == ev_pt.Employee_ID && x.Sal_Month.Date == ev_pt.Sal_Month.Date && x.Item == "B57")?.Amount ?? 0;

                var addData = new MonthlySalarySummaryReportForTaxation_Data
                {
                    Out_day = ev_pt.Out_day,
                    Permission_Group = ev_pt.Permission_Group,
                    Act_flag = "N",
                    Reserved = 0,
                    Atm = 0,
                    Natm = 0,
                    // 5	區分Type	Distinguishing types
                    TypeCode = HSF
                        .Where(x => x.Department == ev_pt.Department && x.Code == ev_pt.Position_Title)
                        .Select(x => string.IsNullOrWhiteSpace(x.Sortcod) ? "1" : x.Sortcod)
                        .FirstOrDefault() ?? "1",
                    // 6	取加扣項 加項	Take additional deduction items
                    AddAmt = HSAM.Where(x =>
                        x.Sal_Month == ev_pt.Sal_Month &&
                        x.Employee_ID == ev_pt.Employee_ID &&
                        (x.AddDed_Type == "A" || x.AddDed_Type == "B") &&
                        x.AddDed_Item != "A42")
                    ?.Sum(x => x.Amount) ?? 0,
                    // 7	取加扣項 扣項	Deduction items
                    DelAmt = HSAM.Where(x =>
                        x.Sal_Month == ev_pt.Sal_Month &&
                        x.Employee_ID == ev_pt.Employee_ID &&
                        (x.AddDed_Type == "C" || x.AddDed_Type == "D"))
                    ?.Sum(x => x.Amount) ?? 0,
                    // 8	薪資	Salary
                    Salary = allSalaryResults.FirstOrDefault(x => x.Employee_ID == ev_pt.Employee_ID && x.Sal_Month.Date == ev_pt.Sal_Month.Date)?.Amount ?? 0,
                    // 9	加班費	Overtime pay
                    OVTPay = allOVTPayResults.FirstOrDefault(x => x.Employee_ID == ev_pt.Employee_ID && x.Sal_Month.Date == ev_pt.Sal_Month.Date)?.Amount ?? 0,
                    // 12	伙食費	Meal expenses
                    Foodpay = B54 + B55 + B56 + B57,
                    // 14	借支	Advance
                    Loan = 0,
                    // 15	社會保險	Social Insurance
                    Scfee = allFeeDetails.FirstOrDefault(x => x.Employee_ID == ev_pt.Employee_ID && x.Sal_Month.Date == ev_pt.Sal_Month.Date && x.Item == "V01")?.Amount ?? 0,
                    // 16	醫療保險	 Medical insurance
                    Mdfee = allFeeDetails.FirstOrDefault(x => x.Employee_ID == ev_pt.Employee_ID && x.Sal_Month.Date == ev_pt.Sal_Month.Date && x.Item == "V02")?.Amount ?? 0,
                    // 17	失業保險	 unemployment insurance
                    Seat = allFeeDetails.FirstOrDefault(x => x.Employee_ID == ev_pt.Employee_ID && x.Sal_Month.Date == ev_pt.Sal_Month.Date && x.Item == "V03")?.Amount ?? 0,
                    // 18	所得稅	income tax
                    Tax = ev_pt.Tax,
                    // 19	工會會費	 union dues
                    Wkmy = allFeeDetails.FirstOrDefault(x => x.Employee_ID == ev_pt.Employee_ID && x.Sal_Month.Date == ev_pt.Sal_Month.Date && x.Item == "D12")?.Amount ?? 0,
                    // 26	年終獎金 Year-end bonus
                    Year_End_Bonus = 0,
                };

                // 10	其他加項	Other additional items
                addData.Other_Add = addData.AddAmt - addData.Foodpay;
                // 11	其他扣項	Other deductions
                addData.Other_Del = addData.DelAmt - addData.Wkmy;
                // 13	正項合計	Sum of positive terms
                addData.Add_Total = addData.Salary + addData.OVTPay + addData.Foodpay + addData.Other_Add;

                // 20	負項合計	 Sum of negative terms 
                addData.Redtotal = addData.Loan + addData.Scfee + addData.Mdfee + addData.Seat + addData.Tax + addData.Wkmy + addData.Other_Del;
                // 21	應扣未扣金額	Amount that should be deducted but was not deducted
                addData.Total = addData.Redtotal > addData.Add_Total ? addData.Redtotal - addData.Add_Total : 0;
                // 22	實領金額	Actual amount received
                var getAc_total = addData.Add_Total - addData.Redtotal + addData.Total;
                addData.Actotal = getAc_total > 0 ? getAc_total : 0;

                // 23	轉帳金額 Transfer amount atm	
                // 24	非轉帳金額 Non-transfer amount natm	
                // 25	保留金額 Retention Amount reserved	
                var _HSC = HSC.FirstOrDefault(x =>
                    x.Factory == ev_pt.Factory &&
                    x.Sal_Month.Date == ev_pt.Sal_Month.Date &&
                    x.Employee_ID == ev_pt.Employee_ID);

                var _HSBA = HSBA.FirstOrDefault(x => x.Factory == ev_pt.Factory && x.Employee_ID == ev_pt.Employee_ID);
                if (ev_pt.Out_day == "Y")
                {
                    if (_HSC != null && _HSC.Close_Status != "Y")
                    {
                        addData.Reserved = addData.Actotal;
                        addData.Act_flag = "Y";
                        addData.Atm = 0;
                        addData.Natm = 0;
                    }
                    else addData.Reserved = 0;
                }
                else addData.Reserved = 0;

                if (addData.Act_flag == "N")
                {
                    addData.Atm = ev_pt.Out_day == "Y"
                        ? ev_pt.BankTransfer == "Y" ? addData.Actotal : 0
                        : _HSBA != null && !string.IsNullOrWhiteSpace(_HSBA.BankNo) ? addData.Actotal : 0;
                    addData.Natm = ev_pt.Out_day == "Y"
                        ? ev_pt.BankTransfer == "Y" ? 0 : addData.Actotal
                        : _HSBA != null && !string.IsNullOrWhiteSpace(_HSBA.BankNo) ? 0 : addData.Actotal;
                }
                // 25	保留金額 Retention Amount reserved(set bằng 1 vì lặp mỗi lần 1 Employee_ID)
                addData.Headcount = 1;

                result_List.Add(addData);
            }
            return result_List;
        }
        #endregion
        #region DownloadFileExcel
        public async Task<OperationResult> DownloadFileExcel(MonthlySalarySummaryReportForTaxation_Param param, string userName)
        {
            var data = await GetData(param);
            if (!data.Any())
                return new OperationResult(false, "System.Message.NoData");

            var groupedData = data
                .GroupBy(x => new { x.Out_day, x.Permission_Group, x.TypeCode })
                .ToList();

            var index = 6;
            var type_Seq = new List<string>()
            {
                BasicCodeTypeConstant.Factory,
                BasicCodeTypeConstant.PermissionGroup,
            };

            var department = string.Empty;
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

            var factory = BasicCodeLanguage
                .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.Factory
                                  && x.Code == param.Factory).Code_Name;

            var getpermission = await GetDataBasicCode(BasicCodeTypeConstant.PermissionGroup, param.Language);
            var permissionGroup = getpermission
                .Where(x => param.Permission_Group.Contains(x.Key))
                .Select(x => x.Value);

            if (!string.IsNullOrWhiteSpace(param.Department))
            {
                department = await _repositoryAccessor.HRMS_Org_Department
                    .FindAll(x => x.Department_Code == param.Department, true)
                    .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true),
                        x => new { x.Division, x.Factory, x.Department_Code },
                        y => new { y.Division, y.Factory, y.Department_Code },
                        (HOD, HODL) => new { HOD, HODL })
                    .SelectMany(x => x.HODL.DefaultIfEmpty(),
                        (x, y) => new { x.HOD, HODL = y })
                    .Select(x => $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}")
                    .FirstOrDefaultAsync();
            }

            MonthlySalarySummaryReportForTaxation_Total totalDta = new();
            List<Cell> dataCells = new()
            {
                new Cell("B" + 2, factory),
                new Cell("B" + 3, userName),
                new Cell("D" + 2, param.Year_Month_Start + "-" + param.Year_Month_End),
                new Cell("D" + 3, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
                new Cell("F" + 2, param.Kind switch
                        {
                            "OnJob" => param.Language == "en" ? "On Job" : "在職",
                            "Resigned" => param.Language == "en" ? "Resigned" : "離職",
                            _ => param.Language == "en" ? "All" : "全部"
                        }),
                new Cell("H" + 2, string.Join(" / ", permissionGroup)),
                new Cell("J" + 2, department),
                new Cell("L" + 2, param.Employee_ID)
            };

            string currentOutDay = null;
            MonthlySalarySummaryReportForTaxation_Total subTotal = null;
            MonthlySalarySummaryReportForTaxation_Total groupTotal = null;
            foreach (var group in groupedData)
            {
                // khi đổi ngày -> in subtotal ngày trước
                if (currentOutDay != null && currentOutDay != group.Key.Out_day)
                {
                    WriteTotalRow(dataCells, ref index, "小計 Sub Total:", subTotal, Color.FromArgb(226, 239, 218));
                    subTotal = new MonthlySalarySummaryReportForTaxation_Total();
                }

                if (subTotal == null)
                    subTotal = new MonthlySalarySummaryReportForTaxation_Total();

                groupTotal = new MonthlySalarySummaryReportForTaxation_Total();
                currentOutDay = group.Key.Out_day;

                foreach (var item in group)
                {
                    // cộng các record theo từng group
                    AddToTotal(groupTotal, item);
                    // cộng subtotal ngày
                    AddToTotal(subTotal, item);
                    // cộng total cuối
                    AddToTotal(totalDta, item);
                }
                // dữ liệu chi tiết
                dataCells.Add(new Cell("A" + index, group.Key.Out_day));
                dataCells.Add(new Cell("B" + index, getpermission.FirstOrDefault(y => y.Key == group.Key.Permission_Group).Value ?? string.Empty));
                dataCells.Add(new Cell("C" + index, group.Key.TypeCode == "1" ? "直接" : (group.Key.TypeCode == "2" ? "間接" : "銷管")));
                dataCells.Add(new Cell("D" + index, groupTotal.TT_Head_count));
                dataCells.Add(new Cell("E" + index, groupTotal.TT_Salary));
                dataCells.Add(new Cell("F" + index, groupTotal.TT_OVTPay));
                dataCells.Add(new Cell("G" + index, groupTotal.TT_Foodpay));
                dataCells.Add(new Cell("H" + index, groupTotal.TT_Other_Add));
                dataCells.Add(new Cell("I" + index, groupTotal.TT_Add_Total));
                dataCells.Add(new Cell("J" + index, groupTotal.TT_Loan));
                dataCells.Add(new Cell("K" + index, groupTotal.TT_Scfee));
                dataCells.Add(new Cell("L" + index, groupTotal.TT_Mdfee));
                dataCells.Add(new Cell("M" + index, groupTotal.TT_Seat));
                dataCells.Add(new Cell("N" + index, groupTotal.TT_Tax));
                dataCells.Add(new Cell("O" + index, groupTotal.TT_Wkmy));
                dataCells.Add(new Cell("P" + index, groupTotal.TT_Other_Del));
                dataCells.Add(new Cell("Q" + index, groupTotal.TT_Redtotal));
                dataCells.Add(new Cell("R" + index, groupTotal.TT_Total));
                dataCells.Add(new Cell("S" + index, groupTotal.TT_Actotal));
                dataCells.Add(new Cell("T" + index, groupTotal.TT_Atm));
                dataCells.Add(new Cell("U" + index, groupTotal.TT_Natm));
                dataCells.Add(new Cell("V" + index, groupTotal.TT_Reserved));
                dataCells.Add(new Cell("W" + index, groupTotal.TT_Year_End_Bonus));
                index++;
            }

            // xuất subtotal cho Out_day cuối
            if (subTotal != null)
            {
                WriteTotalRow(dataCells, ref index, "小計 Sub Total:", subTotal, Color.FromArgb(226, 239, 218));
            }
            // xuất total cuối
            WriteTotalRow(dataCells, ref index, "總計 Total:", totalDta, Color.FromArgb(255, 242, 204));

            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataCells,
                "Resources\\Template\\SalaryReport\\7_2_20_MonthlySalarySummaryReportForTaxation\\Download.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, new { totalRows = data.Count, Excel = excelResult.Result });
        }
        #endregion
        #region WriteTotalRow
        private void WriteTotalRow(List<Cell> dataCells, ref int index, string label, MonthlySalarySummaryReportForTaxation_Total subTotal, Color bgColor)
        {
            Aspose.Cells.Style styleSub = new Aspose.Cells.CellsFactory().CreateStyle();
            styleSub.Pattern = Aspose.Cells.BackgroundType.Solid;
            styleSub.Font.Size = 11;
            styleSub.Font.Name = "Calibri";
            styleSub.Custom = "#,##0";
            styleSub.ForegroundColor = bgColor;
            styleSub.IsTextWrapped = true;

            dataCells.Add(new Cell("B" + index, label, styleSub));
            dataCells.Add(new Cell("D" + index, subTotal.TT_Head_count, styleSub));
            dataCells.Add(new Cell("E" + index, subTotal.TT_Salary, styleSub));
            dataCells.Add(new Cell("F" + index, subTotal.TT_OVTPay, styleSub));
            dataCells.Add(new Cell("G" + index, subTotal.TT_Foodpay, styleSub));
            dataCells.Add(new Cell("H" + index, subTotal.TT_Other_Add, styleSub));
            dataCells.Add(new Cell("I" + index, subTotal.TT_Add_Total, styleSub));
            dataCells.Add(new Cell("J" + index, subTotal.TT_Loan, styleSub));
            dataCells.Add(new Cell("K" + index, subTotal.TT_Scfee, styleSub));
            dataCells.Add(new Cell("L" + index, subTotal.TT_Mdfee, styleSub));
            dataCells.Add(new Cell("M" + index, subTotal.TT_Seat, styleSub));
            dataCells.Add(new Cell("N" + index, subTotal.TT_Tax, styleSub));
            dataCells.Add(new Cell("O" + index, subTotal.TT_Wkmy, styleSub));
            dataCells.Add(new Cell("P" + index, subTotal.TT_Other_Del, styleSub));
            dataCells.Add(new Cell("Q" + index, subTotal.TT_Redtotal, styleSub));
            dataCells.Add(new Cell("R" + index, subTotal.TT_Total, styleSub));
            dataCells.Add(new Cell("S" + index, subTotal.TT_Actotal, styleSub));
            dataCells.Add(new Cell("T" + index, subTotal.TT_Atm, styleSub));
            dataCells.Add(new Cell("U" + index, subTotal.TT_Natm, styleSub));
            dataCells.Add(new Cell("V" + index, subTotal.TT_Reserved, styleSub));
            dataCells.Add(new Cell("W" + index, subTotal.TT_Year_End_Bonus, styleSub));

            index++;
        }
        #endregion
        #region AddToTotal
        private void AddToTotal(MonthlySalarySummaryReportForTaxation_Total target, MonthlySalarySummaryReportForTaxation_Data source)
        {
            target.TT_Salary += source.Salary;
            target.TT_Head_count += source.Headcount;
            target.TT_OVTPay += source.OVTPay;
            target.TT_Foodpay += source.Foodpay;
            target.TT_Other_Add += source.Other_Add;
            target.TT_Other_Del += source.Other_Del;
            target.TT_Scfee += source.Scfee;
            target.TT_Add_Total += source.Add_Total;
            target.TT_Loan = 0; // cố định
            target.TT_Mdfee += source.Mdfee;
            target.TT_Seat += source.Seat;
            target.TT_Tax += source.Tax;
            target.TT_Wkmy += source.Wkmy;
            target.TT_Redtotal += source.Redtotal;
            target.TT_Total += source.Total;
            target.TT_Actotal += source.Actotal;
            target.TT_Atm += source.Atm;
            target.TT_Natm += source.Natm;
            target.TT_Reserved += source.Reserved;
            target.TT_Year_End_Bonus += source.Year_End_Bonus;
        }
        #endregion
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
    }
}