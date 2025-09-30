
using System.Globalization;
using API._Services.Interfaces.SalaryReport;
using API.Data;
using API.DTOs.SalaryReport;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryReport
{
    public class S_7_2_21_MonthlyAdditionsAndDeductionsSummaryReportForFinance : BaseServices, I_7_2_21_MonthlyAdditionsAndDeductionsSummaryReportForFinance
    {
        public S_7_2_21_MonthlyAdditionsAndDeductionsSummaryReportForFinance(DBContext dbContext) : base(dbContext)
        {
        }

        public async Task<OperationResult> Download(MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param param)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;

            var data = (List<MonthlyAdditionsAndDeductionsSummaryReportForFinance_Result>)result.Data;

            if (data.Count == 0)
                return new OperationResult(false, "System.Message.NoData");

            var type_Seq = new List<string>()
            {
                BasicCodeTypeConstant.Factory,
                BasicCodeTypeConstant.PermissionGroup,
                BasicCodeTypeConstant.AdditionsAndDeductionsItem,
                BasicCodeTypeConstant.AdditionsAndDeductionsType
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


            var factory = BasicCodeLanguage
                .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.Factory
                                  && x.Code == param.Factory).Code_Name;


            data.ForEach(item =>
            {
                item.AdditionsAndDeductionsType = BasicCodeLanguage
                    .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsType
                                      && x.Code == item.AdditionsAndDeductionsType)?.Code_Name;

                item.AdditionsAndDductionsItem = BasicCodeLanguage
                    .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsItem
                                      && x.Code == item.AdditionsAndDductionsItem)?.Code_Name;

                item.PermissionGroup = BasicCodeLanguage
                    .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup
                                      && x.Code == item.PermissionGroup)?.Code_Name;
            });

            List<Cell> dataCells = new()
            {
                new Cell("B2", factory),
                new Cell("D2", param.YearMonth),
                new Cell("B3", param.UserName) ,
                new Cell("D3", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
            };

            var index = 7;
            string currentOnJob = null;
            decimal subTotal = 0;
            Aspose.Cells.Style decimalStyle = new Aspose.Cells.CellsFactory().CreateStyle();
            decimalStyle.Custom = "#,##0";

            for (int i = 0; i < data.Count; i++)
            {
                dataCells.Add(new Cell("A" + index, data[i].OnJob == "O" ? "Y" : "N"));
                dataCells.Add(new Cell("B" + index, data[i].PermissionGroup));
                dataCells.Add(new Cell("C" + index, data[i].AdditionsAndDeductionsType));
                dataCells.Add(new Cell("D" + index, data[i].AdditionsAndDductionsItem));
                dataCells.Add(new Cell("E" + index, data[i].Amount, decimalStyle));

                currentOnJob ??= data[i].OnJob;
                subTotal += data[i].Amount;

                bool isLastGroup = (i == data.Count - 1)
                                   || data[i].OnJob != data[i + 1].OnJob
                                   || data[i].AdditionsAndDeductionsType != data[i + 1].AdditionsAndDeductionsType;

                if (isLastGroup)
                {
                    index++;
                    dataCells.Add(new Cell("A" + index, ""));
                    dataCells.Add(new Cell("B" + index, ""));
                    dataCells.Add(new Cell("C" + index, ""));
                    dataCells.Add(new Cell("D" + index, "小計 Sub Total"));
                    dataCells.Add(new Cell("E" + index, subTotal, decimalStyle));

                    subTotal = 0;
                    currentOnJob = (i == data.Count - 1) ? null : data[i + 1].OnJob;
                }

                index++;
            }

            dataCells.Add(new Cell("A" + (index + 2), "核決:"));
            dataCells.Add(new Cell("A" + (index + 3), "Approved by:"));
            dataCells.Add(new Cell("C" + (index + 2), "審核:"));
            dataCells.Add(new Cell("C" + (index + 3), "Checked by:"));
            dataCells.Add(new Cell("E" + (index + 2), "製表:"));
            dataCells.Add(new Cell("E" + (index + 3), "Applicant:"));

            ConfigDownload config = new(false);
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataCells,
               "Resources\\Template\\SalaryReport\\7_2_21_MonthlyAdditionsAndDeductionsSummaryReportForFinance\\Download.xlsx",
                config
            );
            if (excelResult.IsSuccess)
                return new OperationResult(excelResult.IsSuccess, new { TotalRows = data.Count, Excel = excelResult.Result });
            else
                return new OperationResult(excelResult.IsSuccess, excelResult.Error);
        }

        public async Task<OperationResult> GetTotalRows(MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param param)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;
            var data = (List<MonthlyAdditionsAndDeductionsSummaryReportForFinance_Result>)result.Data;
            return new OperationResult(true, data.Count);
        }

        private async Task<OperationResult> GetData(MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param param)
        {
            if (string.IsNullOrWhiteSpace(param.Factory)
             || string.IsNullOrWhiteSpace(param.Kind)
             || string.IsNullOrWhiteSpace(param.YearMonth)
             || !DateTime.TryParseExact(param.YearMonth, "yyyy/MM", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime yearMonth))
                return new OperationResult(false, "SalaryReport.MonthlyAdditionsAndDeductionsSummaryReportForFinance.InvalidInput");

            var result = new List<MonthlyAdditionsAndDeductionsSummaryReportForFinance_Result>();
            var predHSM = PredicateBuilder.New<HRMS_Sal_Monthly>(x => x.Factory == param.Factory);
            var predHSMR = PredicateBuilder.New<HRMS_Sal_Resign_Monthly>(x => x.Factory == param.Factory);
            if (!string.IsNullOrWhiteSpace(param.Department))
            {
                predHSM.And(x => x.Department == param.Department);
                predHSMR.And(x => x.Department == param.Department);
            }
            if (!string.IsNullOrWhiteSpace(param.EmployeeID))
            {
                predHSM.And(x => x.Employee_ID == param.EmployeeID);
                predHSMR.And(x => x.Employee_ID == param.EmployeeID);
            }
            var HSM = await _repositoryAccessor.HRMS_Sal_Monthly.FindAll(predHSM)
                            .Select(x => new TableData
                            {
                                PermissionGroup = x.Permission_Group,
                                EmployeeID = x.Employee_ID,
                                SalMonth = x.Sal_Month,
                                Factory = x.Factory
                            }).ToListAsync();
            var HSMR = await _repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(predHSMR)
                            .Select(x => new TableData
                            {
                                PermissionGroup = x.Permission_Group,
                                EmployeeID = x.Employee_ID,
                                SalMonth = x.Sal_Month,
                                Factory = x.Factory
                            }).ToListAsync();
            var HSAM = await _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.FindAll(
                        x => x.Sal_Month == yearMonth && x.Factory == param.Factory).ToListAsync();

            switch (param.Kind)
            {
                case "OnJob":
                    result = GetDataKind(HSM, HSAM, "O");
                    break;
                case "Resigned":
                    result = GetDataKind(HSMR, HSAM, "R");
                    break;
                case "All":
                    var onJobResult = GetDataKind(HSM, HSAM, "O");
                    var resignedResult = GetDataKind(HSMR, HSAM, "R");
                    result = onJobResult.Concat(resignedResult).ToList();
                    break;
            }
            return new OperationResult(true, result);
        }

        private static List<MonthlyAdditionsAndDeductionsSummaryReportForFinance_Result> GetDataKind(
                List<TableData> baseData,
                List<HRMS_Sal_AddDedItem_Monthly> HSAM,
                string onJob)
        {
            var result = baseData.GroupJoin(HSAM,
                    x => new { Sal_Month = x.SalMonth, Employee_ID = x.EmployeeID, x.Factory },
                    y => new { y.Sal_Month, y.Employee_ID, y.Factory },
                    (x, y) => new { Base = x, HSAM = y })
                .SelectMany(
                    x => x.HSAM.DefaultIfEmpty(),
                     (x, y) => new { x.Base, HSAM = y }
                )
                .GroupBy(x => new { x.Base.PermissionGroup, x.HSAM?.AddDed_Type, x.HSAM?.AddDed_Item })
                .Where(g => g.Key?.AddDed_Type != null && g.Key?.AddDed_Item != null)
                .Select(g => new MonthlyAdditionsAndDeductionsSummaryReportForFinance_Result
                {
                    OnJob = onJob,
                    PermissionGroup = g.Key?.PermissionGroup,
                    AdditionsAndDeductionsType = g.Key?.AddDed_Type,
                    AdditionsAndDductionsItem = g.Key?.AddDed_Item,
                    Amount = g.Sum(x => x.HSAM?.Amount ?? 0),
                })
                .OrderBy(x => x.OnJob)
                .ThenBy(x => x.PermissionGroup)
                .ThenBy(x => x.AdditionsAndDeductionsType)
                .ThenBy(x => x.AdditionsAndDductionsItem)
                .ToList();

            return result;
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
    }
}