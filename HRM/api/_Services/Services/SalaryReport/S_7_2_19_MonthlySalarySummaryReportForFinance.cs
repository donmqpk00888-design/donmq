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
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryReport
{
    public class S_7_2_19_MonthlySalarySummaryReportForFinance : BaseServices, I_7_2_19_MonthlySalarySummaryReportForFinance
    {
        public S_7_2_19_MonthlySalarySummaryReportForFinance(DBContext dbContext) : base(dbContext)
        {
        }
        #region GetDataKindO - Fixed Logic
        private   IQueryable<MonthlySalarySummaryReportForFinance_Dto> GetDataKindO(MonthlySalarySummaryReportForFinance_Param param)
        {
            // Lấy thời gian bắt đầu
            DateTime YMStart  = param.Year_Month_Start;
            DateTime YMEnd = param.Year_Month_End;
            DateTime FYMStart = ToFirstDateOfMonth(YMStart);
            DateTime LYMEnd = ToLastDateOfMonth(YMEnd);
            var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x =>
                x.Factory == param.Factory &&
                (x.Resign_Date != null || (x.Resign_Date.HasValue && x.Resign_Date.Value.Date > LYMEnd)));

            if (!string.IsNullOrWhiteSpace(param.Department))
                predHEP.And(x => x.Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                predHEP.And(x => x.Employee_ID.Contains(param.Employee_ID));

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true);
            var HSM = _repositoryAccessor.HRMS_Sal_Monthly.FindAll(x =>
                                                        x.Factory == param.Factory &&
                                                        x.Sal_Month <= YMEnd &&
                                                        x.Sal_Month >= YMStart, true);

            // Fixed: Điều kiện logic cho On Job - (Resign_Date != null OR Resign_Date > LYMEnd)
            var wk_sql =  HEP
                           .Join(HSM,
                           x => new { x.Employee_ID },
                           y => new { y.Employee_ID },
                           (HEP, HSM) => new { HEP, HSM })
                           .Select(x => new MonthlySalarySummaryReportForFinance_Dto
                           {
                               Resign_Date = x.HEP.Resign_Date,
                               Permission_Group = x.HSM.Permission_Group,
                               Department = x.HEP.Department,
                               Position_Title = x.HEP.Position_Title,
                               Factory = x.HSM.Factory,
                               Sal_Month = x.HSM.Sal_Month,
                               Employee_ID = x.HSM.Employee_ID,
                               Tax = x.HSM.Tax, // Added missing field
                               BankTransfer = x.HSM.BankTransfer, // Added missing field
                               Out_day = !x.HEP.Resign_Date.HasValue || (x.HEP.Resign_Date.HasValue
                               && x.HEP.Resign_Date.Value.Date > LYMEnd.Date) ? "Y" : "N"
                           });
            return wk_sql;
        }
        #endregion

        #region GetDataKindR - Fixed Logic
        private  IQueryable<MonthlySalarySummaryReportForFinance_Dto> GetDataKindR(MonthlySalarySummaryReportForFinance_Param param)
        {
            DateTime YMStart  = param.Year_Month_Start;
            DateTime YMEnd = param.Year_Month_End;
            DateTime FYMStart = ToFirstDateOfMonth(YMStart);
            DateTime LYMEnd = ToLastDateOfMonth(YMEnd);

            var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory);

            if (!string.IsNullOrWhiteSpace(param.Department))
                predHEP.And(x => x.Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                predHEP.And(x => x.Employee_ID.Contains(param.Employee_ID));

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true);
            var HSRM = _repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(x => x.Factory == param.Factory &&
                                                        x.Sal_Month.Date <= YMEnd.Date &&
                                                        x.Sal_Month.Date >= YMStart.Date, true);

            var wk_sql =  HEP
                            .Join(HSRM,
                                x => x.Employee_ID,
                                y => y.Employee_ID,
                            (HEP, HSRM) => new { HEP, HSRM })
                            .Select(x => new MonthlySalarySummaryReportForFinance_Dto
                            {
                                Resign_Date = x.HEP.Resign_Date,
                                Permission_Group = x.HSRM.Permission_Group,
                                Department = x.HEP.Department,
                                Position_Title = x.HEP.Position_Title,
                                Factory = x.HSRM.Factory,
                                Sal_Month = x.HSRM.Sal_Month,
                                Employee_ID = x.HSRM.Employee_ID,
                                Tax = x.HSRM.Tax,
                                BankTransfer = x.HSRM.BankTransfer,
                                Out_day = !x.HEP.Resign_Date.HasValue || (x.HEP.Resign_Date.HasValue
                                && x.HEP.Resign_Date.Value.Date > LYMEnd.Date) ? "Y" : "N"
                            });
            return wk_sql;
        }
        #endregion

        #region GetDataKindAll - New Method for "All" case
        private IQueryable<MonthlySalarySummaryReportForFinance_Dto> GetDataKindAll(MonthlySalarySummaryReportForFinance_Param param)
        {
            DateTime YMStart  = param.Year_Month_Start;
            DateTime YMEnd = param.Year_Month_End;
            DateTime FYMStart = ToFirstDateOfMonth(YMStart);
            DateTime LYMEnd = ToLastDateOfMonth(YMEnd);

            var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x =>
                x.Factory == param.Factory);

            if (!string.IsNullOrWhiteSpace(param.Department))
                predHEP.And(x => x.Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                predHEP.And(x => x.Employee_ID.Contains(param.Employee_ID));

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true);
            var HSRM = _repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(x => x.Factory == param.Factory &&
                                                    x.Sal_Month.Date <= YMEnd.Date &&
                                                    x.Sal_Month.Date >= YMStart.Date, true);
            var HSM = _repositoryAccessor.HRMS_Sal_Monthly.FindAll(x =>
                                                    x.Factory == param.Factory &&
                                                    x.Sal_Month.Date <= YMEnd.Date &&
                                                    x.Sal_Month.Date >= YMStart.Date, true);

            var wk_sql_R =  HEP.Where(x => x.Resign_Date.HasValue &&
                    x.Resign_Date.Value.Date >= FYMStart.Date && x.Resign_Date.Value.Date <= LYMEnd.Date).Join(HSRM, // kind = Resigned : not where
                    x => new { x.Factory, x.Employee_ID },
                    y => new { y.Factory, y.Employee_ID },
                    (HEP, HSRM) => new { HEP, HSRM })
                    .Select(x => new MonthlySalarySummaryReportForFinance_Dto
                    {
                        Resign_Date = x.HEP.Resign_Date,
                        Department = x.HEP.Department,
                        Permission_Group = x.HSRM.Permission_Group,
                        Position_Title = x.HEP.Position_Title,
                        Factory = x.HSRM.Factory,
                        Sal_Month = x.HSRM.Sal_Month,
                        Employee_ID = x.HSRM.Employee_ID,
                        Tax = x.HSRM.Tax,
                        BankTransfer = x.HSRM.BankTransfer,
                        Out_day = !x.HEP.Resign_Date.HasValue || (x.HEP.Resign_Date.HasValue
                               && x.HEP.Resign_Date.Value.Date > LYMEnd.Date) ? "Y" : "N"
                    });
            var wk_sql_O =  HEP.Where(x =>
                                !x.Resign_Date.HasValue ||
                                (x.Resign_Date.HasValue && x.Resign_Date.Value.Date > LYMEnd.Date))
                           .Join(HSM,
                           x => new { x.Factory, x.Employee_ID },
                           y => new { y.Factory, y.Employee_ID },
                           (HEP, HSM) => new { HEP, HSM })
                           .Select(x => new MonthlySalarySummaryReportForFinance_Dto
                           {
                               Resign_Date = x.HEP.Resign_Date,
                               Permission_Group = x.HSM.Permission_Group,
                               Department = x.HEP.Department,
                               Position_Title = x.HEP.Position_Title,
                               Factory = x.HSM.Factory,
                               Sal_Month = x.HSM.Sal_Month,
                               Employee_ID = x.HSM.Employee_ID,
                               Tax = x.HSM.Tax,
                               BankTransfer = x.HSM.BankTransfer,
                               Out_day = !x.HEP.Resign_Date.HasValue || (x.HEP.Resign_Date.HasValue
                                && x.HEP.Resign_Date.Value.Date > LYMEnd.Date) ? "Y" : "N"
                           });
            var wk_sql = wk_sql_O.Union(wk_sql_R);
            return wk_sql;
        }
        #endregion

        #region GetData - Fixed Main Logic
        public async Task<int> GetTotalRows(MonthlySalarySummaryReportForFinance_Param param)
        {
            IQueryable<MonthlySalarySummaryReportForFinance_Dto> wk_sql = param.Kind switch
            {
                "OnJob" => GetDataKindO(param),
                "Resigned" => GetDataKindR(param),
                _ => GetDataKindAll(param),
            };
            return wk_sql != null ? await wk_sql.CountAsync() : 0;
        }

        #endregion
        
        private async Task<List<MonthlySalarySummaryReportForFinance_Data>> GetData(MonthlySalarySummaryReportForFinance_Param param)
        {
            DateTime YMStart  = param.Year_Month_Start;
            DateTime YMEnd = param.Year_Month_End;
            DateTime FYMStart = ToFirstDateOfMonth(YMStart);
            DateTime LYMEnd = ToLastDateOfMonth(YMEnd);

            List<MonthlySalarySummaryReportForFinance_Data> result_List = new();

            IQueryable<MonthlySalarySummaryReportForFinance_Dto> wk_sql = param.Kind switch
            {
                "OnJob" => GetDataKindO(param),
                "Resigned" => GetDataKindR(param),
                _ => GetDataKindAll(param),
            };

            var HSAM = await _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.FindAll(x => x.Factory == param.Factory )
                .ToListAsync();

            var HSC = await _repositoryAccessor.HRMS_Sal_Close.FindAll(x => x.Factory == param.Factory)
                .ToListAsync();

            var HSBA = await _repositoryAccessor.HRMS_Sal_Bank_Account
                .FindAll(x => x.Factory == param.Factory)
                .ToListAsync();
            var allSalaryResults = new List<SalaryDetailBatchResult_ForFinance>();
            var allOVTPayResults = new List<SalaryDetailBatchResult_ForFinance>();
            var allFeeDetails = new List<SalaryDetailBatchResult_ForFinance>();
            var allFoodDetails = new List<SalaryDetailBatchResult_ForFinance>();

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
                    allSalaryResults.AddRange(salary.Select(x => new SalaryDetailBatchResult_ForFinance { Employee_ID = x.Employee_ID, Sal_Month = group.Sal_Month, Amount = (int)x.Amount }));
                    allOVTPayResults.AddRange(oVTPay.Select(x => new SalaryDetailBatchResult_ForFinance { Employee_ID = x.Employee_ID, Sal_Month = group.Sal_Month, Amount = (int)x.Amount }));
                }
                var feeDetails = await _Query_Single_Sal_Monthly_Detail("Y", param.Factory, group.Sal_Month, group.Out_Day_Groups.SelectMany(x => x.Employees).ToList(), "57", "D", feeItems);
                allFeeDetails.AddRange(feeDetails
                .Select(x => new SalaryDetailBatchResult_ForFinance
                {
                    Employee_ID = x.Employee_ID,
                    Sal_Month = group.Sal_Month,
                    Amount = x.Amount,
                    Item = x.Item,
                }));
                var foofDetails = await _Query_Single_Sal_Monthly_Detail("Y", param.Factory, group.Sal_Month, group.Out_Day_Groups.SelectMany(x => x.Employees).ToList(), "49", "B", foodpayItems);
                allFoodDetails.AddRange(foofDetails
                .Select(x => new SalaryDetailBatchResult_ForFinance
                {
                    Employee_ID = x.Employee_ID,
                    Sal_Month = group.Sal_Month,
                    Amount = x.Amount,
                    Item = x.Item,
                }));

            }
            foreach (var item in wk_sql)
            {

                // Tính AddAmt, DelAmt
                var addAmt = HSAM.Where(x => x.Sal_Month == item.Sal_Month &&
                                            x.Employee_ID == item.Employee_ID &&
                                            (x.AddDed_Type == "A" || x.AddDed_Type == "B"))
                                .Sum(x => x.Amount);

                var delAmt = HSAM.Where(x => x.Sal_Month == item.Sal_Month &&
                                            x.Employee_ID == item.Employee_ID &&
                                            (x.AddDed_Type == "C" || x.AddDed_Type == "D"))
                                .Sum(x => x.Amount);

                // Foodpay
                var B54 = allFoodDetails.FirstOrDefault(x => x.Employee_ID == item.Employee_ID && x.Sal_Month.Date == item.Sal_Month.Date && x.Item == "B54")?.Amount ?? 0;
                var B55 = allFoodDetails.FirstOrDefault(x => x.Employee_ID == item.Employee_ID && x.Sal_Month.Date == item.Sal_Month.Date && x.Item == "B55")?.Amount ?? 0;
                var B56 = allFoodDetails.FirstOrDefault(x => x.Employee_ID == item.Employee_ID && x.Sal_Month.Date == item.Sal_Month.Date && x.Item == "B56")?.Amount ?? 0;
                var B57 = allFoodDetails.FirstOrDefault(x => x.Employee_ID == item.Employee_ID && x.Sal_Month.Date == item.Sal_Month.Date && x.Item == "B57")?.Amount ?? 0;
                int foodpay = B54 + B55 + B56 + B57;

                // wkmy
                int wkmy = allFeeDetails.FirstOrDefault(x => x.Employee_ID == item.Employee_ID && x.Sal_Month.Date == item.Sal_Month.Date && x.Item == "D12")?.Amount ?? 0;
                // OtherAdd, OtherDel
                int otherAdd = addAmt - foodpay;
                int otherDel = delAmt - wkmy;

                // Salary, OVTPay
                int salary = await Query_Sal_Monthly_Detail_Sum(item.Out_day, item.Factory, item.Sal_Month, item.Employee_ID, "45", "A");
                int oVTPay = await Query_Sal_Monthly_Detail_Sum(item.Out_day, item.Factory, item.Sal_Month, item.Employee_ID, "42", "A");

                // AddTotal
                int addTotal = salary + oVTPay + foodpay + otherAdd;

                // Loan, scfee, mdfee, seat, tax
                int loan = 0;

                int scfee = allFeeDetails.FirstOrDefault(x => x.Employee_ID == item.Employee_ID && x.Sal_Month.Date == item.Sal_Month.Date && x.Item == "V01")?.Amount ?? 0;
                // 16	醫療保險	 Medical insurance
                int mdfee = allFeeDetails.FirstOrDefault(x => x.Employee_ID == item.Employee_ID && x.Sal_Month.Date == item.Sal_Month.Date && x.Item == "V02")?.Amount ?? 0;
                // 17	失業保險	 unemployment insurance
                int seat = allFeeDetails.FirstOrDefault(x => x.Employee_ID == item.Employee_ID && x.Sal_Month.Date == item.Sal_Month.Date && x.Item == "V03")?.Amount ?? 0;
                // 18	所得稅	income tax
                // 19	工會會費	 union dues
                
                int tax = item.Tax;

                // Redtotal
                int redtotal = loan + scfee + mdfee + seat + tax + wkmy + otherDel;

                // Total
                int total = redtotal > addTotal ? redtotal - addTotal : 0;

                // Actotal
                int actotal = addTotal - redtotal + total;
                if (actotal < 0) actotal = 0;

                // CloseStatus
                var closeStatus = HSC.Where(x => x.Sal_Month == item.Sal_Month &&
                                                x.Employee_ID == item.Employee_ID)
                                    .Select(x => x.Close_Status)
                                    .FirstOrDefault();

                // BankNo
                var bankNo = HSBA.Where(x => x.Employee_ID == item.Employee_ID)
                                .Select(x => x.BankNo)
                                .FirstOrDefault();

                // Phân loại ATM/NATM/Reserved
                string act_flag = "N";
                int reserved = 0, natm = 0, atm = 0;

                if (item.Out_day == "Y")
                {
                    if (closeStatus != "Y")
                    {
                        reserved = actotal;
                        act_flag = "Y";
                        natm = 0;
                        atm = 0;
                    }
                    else
                    {
                        reserved = 0;
                    }
                }
                else
                {
                    reserved = 0;
                }

                if (act_flag == "N")
                {
                    if (item.Out_day == "Y")
                    {
                        if (item.BankTransfer == "Y")
                        {
                            atm = actotal;
                            natm = 0;
                        }
                        else
                        {
                            natm = actotal;
                            atm = 0;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(bankNo))
                        {
                            atm = actotal;
                            natm = 0;
                        }
                        else
                        {
                            natm = actotal;
                            atm = 0;
                        }
                    }
                }

                result_List.Add(new MonthlySalarySummaryReportForFinance_Data
                {
                    Out_day = item.Out_day,
                    Permission_Group = item.Permission_Group,
                    AddAmt = addAmt,
                    DelAmt = delAmt,
                    Salary = salary,
                    OVTPay = oVTPay,
                    Other_Add = otherAdd,
                    Other_Del = otherDel,
                    Foodpay = foodpay,
                    Add_Total = addTotal,
                    Loan = loan,
                    Scfee = scfee,
                    Mdfee = mdfee,
                    Seat = seat,
                    Tax = tax,
                    Wkmy = wkmy,
                    Redtotal = redtotal,
                    Total = total,
                    Actotal = actotal,
                    Act_flag = act_flag,
                    Reserved = reserved,
                    Natm = natm,
                    Atm = atm,
                    BankNo = bankNo,
                    Headcount = 1
                });
            }
            return result_List;
        }
     
        #region Download

        public async Task<OperationResult> DownloadFileExcel(MonthlySalarySummaryReportForFinance_Param param, string userName)
        {
            var data = await GetData(param);
            if (!data.Any())
                return new OperationResult(false, "System.Message.NoData");

            var groupedData = data
                .GroupBy(x => new { x.Out_day, x.Permission_Group })
                .ToList();

            var index = 6;
            var type_Seq = new List<string>()
            {
                BasicCodeTypeConstant.Factory,
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


            MonthlySalarySummaryReportForFinance_Total totalDta = new();
            List<Cell> dataCells = new()
            {
                new Cell("B" + 2, factory),
                new Cell("B" + 3, userName),
                new Cell("D" + 2, param.Year_Month_Start.ToString("yyyy/MM") + "-" + param.Year_Month_End.ToString("yyyy/MM")),
                new Cell("D" + 3, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
                new Cell("F" + 2, param.Kind switch
                        {
                            "OnJob" => param.Language == "en" ? "On Job" : "在職",
                            "Resigned" => param.Language == "en" ? "Resigned" : "離職",
                            _ => param.Language == "en" ? "All" : "全部"
                        }),
                new Cell("H" + 2, department),
                new Cell("J" + 2, param.Employee_ID),

            };

            string currentOutDay = null;
            MonthlySalarySummaryReportForFinance_Total subTotal = null;
            MonthlySalarySummaryReportForFinance_Total groupTotal = null;
            foreach (var group in groupedData)
            {

                // khi đổi ngày -> in subtotal ngày trước
                if (currentOutDay != null && currentOutDay != group.Key.Out_day)
                {
                    WriteTotalRow(dataCells, ref index, "小計 Sub Total:", subTotal, Color.FromArgb(226, 239, 218));
                    subTotal = new MonthlySalarySummaryReportForFinance_Total();
                }

                if (subTotal == null)
                    subTotal = new MonthlySalarySummaryReportForFinance_Total();

                groupTotal = new MonthlySalarySummaryReportForFinance_Total();
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
                dataCells.Add(new Cell("c" + index, groupTotal.TT_Head_count));
                dataCells.Add(new Cell("D" + index, groupTotal.TT_Salary));
                dataCells.Add(new Cell("E" + index, groupTotal.TT_OVTPay));
                dataCells.Add(new Cell("F" + index, groupTotal.TT_Foodpay));
                dataCells.Add(new Cell("G" + index, groupTotal.TT_Other_Add));
                dataCells.Add(new Cell("H" + index, groupTotal.TT_Add_Total));
                dataCells.Add(new Cell("I" + index, groupTotal.TT_Loan));
                dataCells.Add(new Cell("J" + index, groupTotal.TT_Scfee));
                dataCells.Add(new Cell("K" + index, groupTotal.TT_Mdfee));
                dataCells.Add(new Cell("L" + index, groupTotal.TT_Seat));
                dataCells.Add(new Cell("M" + index, groupTotal.TT_Tax));
                dataCells.Add(new Cell("N" + index, groupTotal.TT_Wkmy));
                dataCells.Add(new Cell("O" + index, groupTotal.TT_Other_Del));
                dataCells.Add(new Cell("P" + index, groupTotal.TT_Redtotal));
                dataCells.Add(new Cell("Q" + index, groupTotal.TT_Total));
                dataCells.Add(new Cell("R" + index, groupTotal.TT_Actotal));
                dataCells.Add(new Cell("S" + index, groupTotal.TT_Atm));
                dataCells.Add(new Cell("T" + index, groupTotal.TT_Natm));
                dataCells.Add(new Cell("U" + index, groupTotal.TT_Reserved));
                index++;
            }

            // xuất subtotal cho Out_day cuối
            if (subTotal != null)
            {
                WriteTotalRow(dataCells, ref index,"小計 Sub Total:", subTotal, Color.FromArgb(226, 239, 218));
            }
            // xuất total cuối
            WriteTotalRow(dataCells, ref index, "總計 Total:", totalDta, Color.FromArgb(255, 242, 204));

            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataCells,
                "Resources\\Template\\SalaryReport\\7_2_19_MonthlySalarySummaryReportForFinance\\Download.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, new { totalRows = data.Count, Excel = excelResult.Result });
        }
        private static void AddToTotal(MonthlySalarySummaryReportForFinance_Total target, MonthlySalarySummaryReportForFinance_Data source)
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
        }
        // helper để in subtotal
       private static void WriteTotalRow(List<Cell> dataCells, ref int index, string label, MonthlySalarySummaryReportForFinance_Total subTotal, Color bgColor)
        {
            Aspose.Cells.Style styleSub = new Aspose.Cells.CellsFactory().CreateStyle();
            styleSub.Pattern = Aspose.Cells.BackgroundType.Solid;
            styleSub.Font.Size = 11;
            styleSub.Font.Name = "Calibri";
            styleSub.Custom = "#,##0";
            styleSub.ForegroundColor = bgColor;
            styleSub.IsTextWrapped = true;

            dataCells.Add(new Cell("B" + index, label, styleSub));
            dataCells.Add(new Cell("C" + index, subTotal.TT_Head_count, styleSub));
            dataCells.Add(new Cell("D" + index, subTotal.TT_Salary, styleSub));
            dataCells.Add(new Cell("E" + index, subTotal.TT_OVTPay, styleSub));
            dataCells.Add(new Cell("F" + index, subTotal.TT_Foodpay, styleSub));
            dataCells.Add(new Cell("G" + index, subTotal.TT_Other_Add, styleSub));
            dataCells.Add(new Cell("H" + index, subTotal.TT_Add_Total, styleSub));
            dataCells.Add(new Cell("I" + index, subTotal.TT_Loan, styleSub));
            dataCells.Add(new Cell("J" + index, subTotal.TT_Scfee, styleSub));
            dataCells.Add(new Cell("K" + index, subTotal.TT_Mdfee, styleSub));
            dataCells.Add(new Cell("L" + index, subTotal.TT_Seat, styleSub));
            dataCells.Add(new Cell("M" + index, subTotal.TT_Tax, styleSub));
            dataCells.Add(new Cell("N" + index, subTotal.TT_Wkmy, styleSub));
            dataCells.Add(new Cell("O" + index, subTotal.TT_Other_Del, styleSub));
            dataCells.Add(new Cell("P" + index, subTotal.TT_Redtotal, styleSub));
            dataCells.Add(new Cell("Q" + index, subTotal.TT_Total, styleSub));
            dataCells.Add(new Cell("R" + index, subTotal.TT_Actotal, styleSub));
            dataCells.Add(new Cell("S" + index, subTotal.TT_Atm, styleSub));
            dataCells.Add(new Cell("T" + index, subTotal.TT_Natm, styleSub));
            dataCells.Add(new Cell("U" + index, subTotal.TT_Reserved, styleSub));

            index++;
        }
        #endregion

        #region GetList
        public async Task<List<SalaryDetailBatchResult_ForFinance>> _Query_Single_Sal_Monthly_Detail(string kind, string factory, DateTime yearMonth, List<string> employeeId, string typeSeq, string addedType, List<string> item)
        {
            if (kind == "Y")
            {
                return await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeId.Contains(x.Employee_ID) &&
                                 item.Contains(x.Item) &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .GroupBy(x => new { x.Employee_ID, x.Item, x.Sal_Month })
                    .Select(x => new SalaryDetailBatchResult_ForFinance
                    {
                        Employee_ID = x.Key.Employee_ID,
                        Item = x.Key.Item,
                        Sal_Month = x.Key.Sal_Month,
                        Amount = x.Sum(x => (int?)x.Amount ?? 0)
                    })
                    .ToListAsync();
            }
            else /// --kind = N
            {
                return await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeId.Contains(x.Employee_ID) &&
                                 item.Contains(x.Item) &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .GroupBy(x => new { x.Employee_ID, x.Item })
                    .Select(x => new SalaryDetailBatchResult_ForFinance
                    {
                        Employee_ID = x.Key.Employee_ID,
                        Item = x.Key.Item,
                        Amount = x.Sum(x => (int?)x.Amount ?? 0)
                    })
                    .ToListAsync();
            }
        }
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
        #endregion
        private static DateTime ToFirstDateOfMonth(DateTime dt) => new(dt.Year, dt.Month, 1);
        private static DateTime ToLastDateOfMonth(DateTime dt) => ToFirstDateOfMonth(dt.AddMonths(1)).AddDays(-1);
    }
}
