using System.Globalization;
using API.Data;
using API._Services.Interfaces.SalaryReport;
using API.DTOs.SalaryMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Cell = SDCores.Cell;

namespace API._Services.Services.SalaryReport
{
    public class S_7_2_2_UtilityWorkersQualificationSeniorityPrinting : BaseServices, I_7_2_2_UtilityWorkersQualificationSeniorityPrinting
    {
        public S_7_2_2_UtilityWorkersQualificationSeniorityPrinting(DBContext dbContext) : base(dbContext)
        {
        }

        #region Get Data
        public async Task<int> Search(UtilityWorkersQualificationSeniorityPrintingParam param)
        {
            var result = await GetData(param);
            return result.Count;
        }
        private async Task<List<UtilityWorkersQualificationSeniorityPrintingDto>> GetData(UtilityWorkersQualificationSeniorityPrintingParam param)
        {
            var yearMonth = !DateTime.TryParseExact(param.YearMonth, "yyyy/MM", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime dateValue);
            var Start_date = dateValue.Date;

            // // Tính ngày cuối tháng: cộng 1 tháng, trừ đi 1 ngày
            var End_date = Start_date.AddMonths(1).AddDays(-1);
            var predEmpPersonal = PredicateBuilder.New<HRMS_Emp_Personal>(x =>
                                                        x.Factory == param.Factory &&
                                                        x.Resign_Date == null);

            if (!string.IsNullOrWhiteSpace(param.Department))
                predEmpPersonal.And(x => x.Department == param.Department);
            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                predEmpPersonal.And(x => x.Employee_ID.Contains(param.Employee_ID.Trim()));

            var HEP = await _repositoryAccessor.HRMS_Emp_Personal
                    .FindAll(predEmpPersonal, true)
                    .OrderBy(x => x.Department).ThenBy(x => x.Employee_ID)
                    .ToListAsync();

            var HES = await _repositoryAccessor.HRMS_Emp_Skill
                    .FindAll(x => x.Factory == param.Factory &&
                            x.Skill_Certification == "02")
                    .ToListAsync();

            var amountList = await _repositoryAccessor.HRMS_Sal_Master_Detail
                .FindAll(x => x.Salary_Item == "B04")
                              .ToListAsync();
            var resultList = new List<UtilityWorkersQualificationSeniorityPrintingDto>();
            foreach (var item in HEP)
            {
                //Tính tổng số ngày làm việc
                var handale_date = (End_date - item.Onboard_Date).TotalDays;
                // Tính số năm làm việc (tính đến End_date), làm tròn 1 chữ số thập phân
                var wk_nz = Math.Round((decimal)(handale_date / 365), 1);

                var wpassdate2 = HES.Where(x => x.Employee_ID == item.Employee_ID)
                                    .Min(x => (DateTime?)x.Passing_Date);

                if (string.IsNullOrWhiteSpace(wpassdate2.ToString())) continue;

                var yy = (int)Math.Floor(param.NumberOfMonth / 12.0);
                var mm = param.NumberOfMonth % 12;
                DateTime wpassdate = wpassdate2.Value.AddYears(yy).AddMonths(mm);

                int endDateYM = int.Parse(End_date.ToString("yyyyMM"));
                int wpassDateYM = int.Parse(wpassdate.ToString("yyyyMM"));

                if (endDateYM < wpassDateYM || endDateYM != wpassDateYM) continue;

                var Fwpassdate = wpassdate2.Value;

                var amount = amountList.Where(x => x.Factory == item.Factory &&
                              x.Employee_ID == item.Employee_ID).FirstOrDefault()?.Amount ?? 0;

                int days = (DateTime.Now.Date - Fwpassdate.Date).Days / 30;

                resultList.Add(new UtilityWorkersQualificationSeniorityPrintingDto
                {
                    Factory = param.Factory,
                    Department = item.Department,
                    Employee_ID = item.Employee_ID,
                    Local_Full_Name = item.Local_Full_Name,
                    Onboard_Date = item.Onboard_Date,
                    Year = wk_nz,
                    Work_Type = item.Work_Type,
                    Wpassdate = Fwpassdate,
                    Number_Of_Months_After_Utility_Qualification = days,
                    Technical_Allowance = amount
                });
            }
            return resultList;
        }
        #endregion
        #region Export Excel
        public async Task<OperationResult> DownloadFileExcel(UtilityWorkersQualificationSeniorityPrintingParam param, string userName)
        {
            var data = await GetData(param);
            if (!data.Any()) return new OperationResult(false, "System.Message.NoData");

            var HOD = await Query_Department_List(param.Factory);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Factory == param.Factory
                           && x.Language_Code == param.Language);
            var deparmentList = HOD.GroupJoin(HODL,
                        x => new {x.Division, x.Department_Code},
                        y => new {y.Division, y.Department_Code},
                        (x, y) => new { dept = x, hodl = y })
                        .SelectMany(x => x.hodl.DefaultIfEmpty(),
                        (x, y) => new { x.dept, hodl = y })
                        .Select(x => new KeyValuePair<string, string>(x.dept.Department_Code, $"{(x.hodl != null ? x.hodl.Name : x.dept.Department_Name)}"))
                        .ToList();

            var basicCode = _repositoryAccessor.HRMS_Basic_Code.FindAll(true).ToHashSet();
            var basicLanguage = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true).ToHashSet();
            var codeLang = basicCode
                .GroupJoin(basicLanguage,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { Code = x, Language = y })
                .SelectMany(x => x.Language.DefaultIfEmpty(),
                    (x, y) => new { x.Code, Language = y })
                .Select(x => new
                {
                    x.Code.Code,
                    x.Code.Type_Seq,
                    Code_Name = x.Language != null ? x.Language.Code_Name : x.Code.Code_Name
                })
                .Distinct().ToList();

            var factoryList = await GetListFactory(param.Language, userName);
            var facetory = factoryList.FirstOrDefault(x => x.Key == param.Factory).Value ?? param.Factory;
            data.ForEach(item =>
            {
                item.Work_Type_Title = $"{item.Work_Type} - {codeLang.FirstOrDefault(y => y.Code == item.Work_Type && y.Type_Seq == "5")?.Code_Name ?? string.Empty}";
                item.Department_Name = deparmentList.FirstOrDefault(x => x.Key == item.Department).Value ?? item.Department;
            });
            List<Table> tables = new()
            {
                new Table("result", data)
            };
            List<Cell> cells = new()
            {
                new Cell("B2", facetory),
                new Cell("D2", param.YearMonth),
                new Cell("F2", param.NumberOfMonth),
                new Cell("H2", param.Department),
                new Cell("J2", param.Employee_ID),
                new Cell("B4", userName),
                new Cell("D4", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
            };
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                tables,
                cells,
                "Resources\\Template\\SalaryReport\\7_2_2_UtilityWorkersQualificationSeniorityPrinting\\Download.xlsx"
            );

            return new OperationResult(excelResult.IsSuccess, new { totalRows = data.Count, Excel = excelResult.Result });
        }
        #endregion
        #region Get List
        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string language, string userName)
        {
            List<string> factories = await Queryt_Factory_AddList(userName);
            var factoriesWithLanguage = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory && factories.Contains(x.Code), true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code == language, true),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}")).ToListAsync();
            return factoriesWithLanguage;
        }
        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string language, string factory)
        {
            var HOD = await Query_Department_List(factory);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Factory == factory
                           && x.Language_Code == language);

            var deparment = HOD.GroupJoin(HODL,
                        x => new {x.Division, x.Department_Code},
                        y => new {y.Division, y.Department_Code},
                        (x, y) => new { dept = x, hodl = y })
                        .SelectMany(x => x.hodl.DefaultIfEmpty(),
                        (x, y) => new { x.dept, hodl = y })
                        .Select(x => new KeyValuePair<string, string>(x.dept.Department_Code, $"{(x.hodl != null ? x.hodl.Name : x.dept.Department_Name)}"))
                        .ToList();
            return deparment;
        }
        #endregion
    }
}