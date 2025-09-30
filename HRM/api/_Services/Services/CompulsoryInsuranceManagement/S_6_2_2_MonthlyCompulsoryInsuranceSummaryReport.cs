using System.Drawing;
using API.Data;
using API._Services.Interfaces.CompulsoryInsuranceManagement;
using API.DTOs.CompulsoryInsuranceManagement;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.CompulsoryInsuranceManagement
{
    public class S_6_2_2_MonthlyCompulsoryInsuranceSummaryReport : BaseServices, I_6_2_2_MonthlyCompulsoryInsuranceSummaryReport
    {
        public S_6_2_2_MonthlyCompulsoryInsuranceSummaryReport(DBContext dbContext) : base(dbContext)
        {
        }
        public async Task<OperationResult> DownLoadExcel(MonthlyCompulsoryInsuranceSummaryReport_Param param, string userName)
        {
            List<MonthlyCompulsoryInsuranceSummaryReportExcel> data = await GetData(param);
            if (!data.Any())
                return new OperationResult(false, "System.Message.NoData");

            Total totalDta = new();
            int count = data.Count;
            foreach (var item in data)
            {
                totalDta.Total_Number_Employees += item.Number_Of_Employees;
                totalDta.Total_Insured_Salary += item.Insured_Salary;
                totalDta.ToTal_Employer_Contribution += item.Employer_Contribution;
                totalDta.ToTal_Employee_Contribution += item.Employee_Contribution;
                totalDta.Total_Amount += item.Total_Amount;
            }
            List<Cell> dataCells = new()
            {
                new Cell("B" + 2, param.Factory),
                new Cell("B" + 3, param.Insurance_Type_Full),
                new Cell("D" + 2, param.Year_Month),
                new Cell("D" + 3, param.Kind == "On Job" ? "在職On Job" : "離職 Resigned"),
                new Cell("F" + 2, param.Department),
                new Cell("F" + 3, userName),
                new Cell("H" + 2, string.Join(", ", param.Permission_Group_Name)),
                new Cell("H" + 3, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
            };

            int index = 7;
            for (int i = 0; i < count; i++)
            {
                dataCells.Add(new Cell("A" + index, data[i].Department));
                dataCells.Add(new Cell("B" + index, data[i].Department_Name));
                dataCells.Add(new Cell("C" + index, data[i].Number_Of_Employees));
                dataCells.Add(new Cell("D" + index, data[i].Insured_Salary));
                dataCells.Add(new Cell("E" + index, data[i].Employer_Contribution));
                dataCells.Add(new Cell("F" + index, data[i].Employee_Contribution));
                dataCells.Add(new Cell("G" + index, data[i].Total_Amount));
                index += 1;
            }
            // Apply color to the last row
            Aspose.Cells.Style style = new Aspose.Cells.CellsFactory().CreateStyle();
            style.Pattern = Aspose.Cells.BackgroundType.Solid;
            style.Font.Size = 12;
            style.Font.Name = "Calibri";
            style.ForegroundColor = Color.FromArgb(221, 235, 247);
            style.IsTextWrapped = true;
            // Add the total values to the last row
            dataCells.Add(new Cell("A" + index, "總計:\nTotal:", style));
            dataCells.Add(new Cell("B" + index, string.Empty, style));
            dataCells.Add(new Cell("C" + index, totalDta.Total_Number_Employees, style));
            dataCells.Add(new Cell("D" + index, totalDta.Total_Insured_Salary, style));
            dataCells.Add(new Cell("E" + index, totalDta.ToTal_Employer_Contribution, style));
            dataCells.Add(new Cell("F" + index, totalDta.ToTal_Employee_Contribution, style));
            dataCells.Add(new Cell("G" + index, totalDta.Total_Amount, style));
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataCells,
                "Resources\\Template\\CompulsoryInsuranceManagement\\6_2_2_MonthlyCompulsoryInsuranceSummaryReport\\Download.xlsx"
            );
            var dataResult = new
            {
                excelResult.Result,
                count
            };
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, dataResult);
        }

        private async Task<List<MonthlyCompulsoryInsuranceSummaryReportExcel>> GetData(MonthlyCompulsoryInsuranceSummaryReport_Param param)
        {
            DateTime yearMonth = DateTime.Parse(param.Year_Month);
            var predHSM = PredicateBuilder.New<HRMS_Sal_Monthly>(x =>
                x.Factory == param.Factory &&
                x.Sal_Month == yearMonth &&
                param.Permission_Group.Contains(x.Permission_Group)
            );
            var preHSMD = PredicateBuilder.New<HRMS_Sal_Monthly_Detail>(
                x => x.Type_Seq == "57"
                && x.Amount > 0
            );
            var preHSRM = PredicateBuilder.New<HRMS_Sal_Resign_Monthly>(x =>
                x.Factory == param.Factory &&
                x.Sal_Month == yearMonth &&
                param.Permission_Group.Contains(x.Permission_Group));
            var preHSRMD = PredicateBuilder.New<HRMS_Sal_Resign_Monthly_Detail>(
                x => x.Type_Seq == "57"
                && x.Amount > 0
            );

            if (param.Insurance_Type is "V01" or "V02" or "V03")
            {
                preHSMD.And(x => x.Item == param.Insurance_Type);
                preHSRMD.And(x => x.Item == param.Insurance_Type);
            }

            if (!string.IsNullOrWhiteSpace(param.Department))
            {
                predHSM.And(x => x.Department == param.Department);
                preHSRM.And(x => x.Department == param.Department);
            }

            List<MonthlyCompulsoryInsuranceSummaryReportDto> data = new();
            List<MonthlyCompulsoryInsuranceSummaryReportExcel> result = new();

            var department = await _repositoryAccessor.HRMS_Org_Department.FindAll(true)
                                   .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language
                                   .FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true),
                                       x => new { x.Division, x.Factory, x.Department_Code },
                                       y => new { y.Division, y.Factory, y.Department_Code },
                                       (x, y) => new { HOD = x, HODL = y })
                                   .SelectMany(x => x.HODL.DefaultIfEmpty(),
                                       (x, y) => new { x.HOD, HODL = y })
                                   .Select(x => new
                                   {
                                       Code = x.HOD.Department_Code,
                                       Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name,
                                       x.HOD.Division,
                                       x.HOD.Factory,
                                   }).Distinct().ToListAsync();

            if (param.Kind == "On Job")
                data = await _repositoryAccessor.HRMS_Sal_Monthly.FindAll(predHSM, true)
                        .Join(_repositoryAccessor.HRMS_Sal_Monthly_Detail.FindAll(preHSMD, true),
                            a => new { a.Factory, a.Sal_Month, a.Employee_ID },
                            b => new { b.Factory, b.Sal_Month, b.Employee_ID },
                            (a, b) => new { HSM = a, b.Amount })
                        .Select(x => new MonthlyCompulsoryInsuranceSummaryReportDto
                        {
                            Factory = x.HSM.Factory,
                            Employee_ID = x.HSM.Employee_ID,
                            Department = x.HSM.Department,
                            PermissionGroup = x.HSM.Permission_Group,
                            Salary_Type = x.HSM.Salary_Type,
                            Employee_Amt = param.Insurance_Type == "V04" ? 0 : x.Amount
                        }).Distinct().ToListAsync();
            else
                data = await _repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(preHSRM, true)
                        .Join(_repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail.FindAll(preHSRMD, true),
                            a => new { a.Factory, a.Sal_Month, a.Employee_ID },
                            b => new { b.Factory, b.Sal_Month, b.Employee_ID },
                            (a, b) => new { HSRM = a, b.Amount })
                        .Select(x => new MonthlyCompulsoryInsuranceSummaryReportDto
                        {
                            Factory = x.HSRM.Factory,
                            Employee_ID = x.HSRM.Employee_ID,
                            Department = x.HSRM.Department,
                            PermissionGroup = x.HSRM.Permission_Group,
                            Salary_Type = x.HSRM.Salary_Type,
                            Employee_Amt = param.Insurance_Type == "V04" ? 0 : x.Amount
                        }).Distinct().ToListAsync();

            foreach (var item in data)
            {

                Dictionary<string, VariableCombine> Insurance_57 = await Query_Ins_Rate_Variable_Combine("57", "Insurance", "EmployerRate", "EmployeeRate", "Amt", param.Factory, yearMonth, item.PermissionGroup);
                decimal V01_EmployerRate_57 = Insurance_57.GetValueOrDefault("V01_EmployerRate_57")?.Value as decimal? ?? 0m;
                decimal V02_EmployerRate_57 = Insurance_57.GetValueOrDefault("V02_EmployerRate_57")?.Value as decimal? ?? 0m;
                decimal V03_EmployerRate_57 = Insurance_57.GetValueOrDefault("V03_EmployerRate_57")?.Value as decimal? ?? 0m;
                decimal V04_EmployerRate_57 = Insurance_57.GetValueOrDefault("V04_EmployerRate_57")?.Value as decimal? ?? 0m;


                decimal basic_Amt = await Query_WageStandard_Sum("B", param.Factory, yearMonth, item.Employee_ID, item.PermissionGroup, item.Salary_Type);

                // 保險類別 - Insurance Type
                switch (param.Insurance_Type)
                {
                    case "V01":
                        item.Employer_Amt = Math.Round(basic_Amt * V01_EmployerRate_57, 0);
                        break;
                    case "V02":
                        item.Employer_Amt = Math.Round(basic_Amt * V02_EmployerRate_57, 0);
                        break;
                    case "V03":
                        item.Employer_Amt = Math.Round(basic_Amt * V03_EmployerRate_57, 0);
                        break;
                    case "V04":
                        item.Employer_Amt = Math.Round(basic_Amt * V04_EmployerRate_57, 0);
                        break;
                    default:
                        break;
                }
                item.Basic_Amt = basic_Amt;
                item.Department_Name = department.FirstOrDefault(dep => dep.Factory == item.Factory && dep.Code == item.Department)?.Name;
            }

            result = data
            .GroupBy(x => x.Department)
            .Select(g => new MonthlyCompulsoryInsuranceSummaryReportExcel
            {
                Department = g.Key,
                Department_Name = g.FirstOrDefault()?.Department_Name,
                Number_Of_Employees = g.Select(s => s.Employee_ID).Distinct().Count(),
                Insured_Salary = (int)g.Sum(s => s.Basic_Amt),
                Employer_Contribution = (int)g.Sum(s => s.Employer_Amt),
                Employee_Contribution = (int)g.Sum(s => s.Employee_Amt),
                Total_Amount = (int)g.Sum(s => s.Employee_Amt + s.Employer_Amt),
            }).ToList();

            return result;
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

        public async Task<List<KeyValuePair<string, string>>> GetListInsuranceType(string language)
        {
            var data = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.InsuranceType, true)
                        .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
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

        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language)
        {
            var permissionGroups = await Query_Permission_List(factory);
            List<string> permissions = permissionGroups.Select(x => x.Permission_Group).ToList();
            var dataPermissionGroups = await GetDataBasicCode(BasicCodeTypeConstant.PermissionGroup, language);
            var results = dataPermissionGroups.Where(x => permissions.Contains(x.Key)).ToList();
            return results;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
        {
            var data = await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == factory, true)
                        .Join(_repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(x => x.Kind == "1", true),
                            x => new { x.Division, x.Factory },
                            y => new { y.Division, y.Factory },
                            (x, y) => new { HOD = x, HBFC = y }
                        ).GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Factory == factory && x.Language_Code.ToLower() == language.ToLower(), true),
                            x => new { x.HOD.Division, x.HOD.Factory, x.HOD.Department_Code },
                            y => new { y.Division, y.Factory, y.Department_Code },
                            (x, y) => new { x.HOD, x.HBFC, HODL = y }
                        ).SelectMany(x => x.HODL.DefaultIfEmpty(),
                            (x, y) => new { x.HOD, x.HBFC, HODL = y }
                        ).Select(x => new KeyValuePair<string, string>(
                            x.HOD.Department_Code.Trim(),
                            $"{x.HOD.Department_Code.Trim()}-{(x.HODL != null ? x.HODL.Name.Trim() : x.HOD.Department_Name.Trim())}"
                        )).Distinct().ToListAsync();
            return data;
        }

        public async Task<int> GetCountRecords(MonthlyCompulsoryInsuranceSummaryReport_Param param)
        {
            var result = await GetData(param);
            return result.Count;
        }

    }
}