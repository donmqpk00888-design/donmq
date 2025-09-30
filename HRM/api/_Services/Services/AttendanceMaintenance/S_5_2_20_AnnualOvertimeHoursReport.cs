using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Models;
using Aspose.Cells;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_2_20_AnnualOvertimeHoursReport : BaseServices, I_5_2_20_AnnualOvertimeHoursReport
    {
        private static readonly string rootPath = Directory.GetCurrentDirectory();

        public S_5_2_20_AnnualOvertimeHoursReport(DBContext dbContext) : base(dbContext)
        {
        }
        #region Download
        public async Task<OperationResult> Download(AnnualOvertimeHoursReportParam param)
        {
            var data = await GetData(param);

            if (!data.Any())
                return new OperationResult(false, "No data!");

            var result = ExportExcel(param, data);
            return result;
        }

        private static OperationResult ExportExcel(AnnualOvertimeHoursReportParam param, List<AnnualOvertimeHoursReportDto> data)
        {
            var monthTitle = new List<string>()
            {
                "January", "February", "March", "April", "May", "June",
                "July", "August", "September", "October", "November", "December"
            };
            var month = DateTime.Parse(param.Year_Month);

            string path = Path.Combine(
                rootPath, 
                "Resources\\Template\\AttendanceMaintenance\\5_2_20_AnnualOvertimeHoursReport\\Download.xlsx"
            );
            WorkbookDesigner designer = new() { Workbook = new Workbook(path) };
            Worksheet ws = designer.Workbook.Worksheets[0];

            designer.SetDataSource("result", data);
            designer.Process();

            ws.Cells["B2"].PutValue(param.Factory);
            ws.Cells["B4"].PutValue(param.UserName);
            ws.Cells["E2"].PutValue(param.Department);
            ws.Cells["E4"].PutValue(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
            ws.Cells["J2"].PutValue(param.Employee_ID);
            ws.Cells["L2"].PutValue(param.Year_Month);

            Cells cells = ws.Workbook.Worksheets[0].Cells;
            Aspose.Cells.Range rangeTitle = cells.CreateRange(7, 5, 2, month.Month + 1);
            Aspose.Cells.Range rangeData = cells.CreateRange(9, 5, data.Count, month.Month + 1);
            Style style = ws.Cells[7, 0].GetStyle();
            rangeTitle.ColumnWidth = 10;
            rangeTitle.SetStyle(style);

            for (int i = 0; i < month.Month; i++)
            {
                rangeTitle[0, i].PutValue($"{i + 1}月份");
                rangeTitle[1, i].PutValue(monthTitle[i]);
            }

            rangeTitle[0, month.Month].PutValue("年累計");
            rangeTitle[1, month.Month].PutValue("Total");

            // Tính tổng cho từng tháng và tổng năm
            decimal[] monthlyTotals = new decimal[month.Month];
            decimal? yearlyTotal = 0;

            for (int i = 0; i < data.Count; i++)
            {
                yearlyTotal += data[i].Total;
                for (int j = 0; j < month.Month; j++)
                {
                    monthlyTotals[j] += data[i].Monthly[j];
                }
            }

            //Thay đổi kích thước range để bao gồm dòng tổng
            rangeData = cells.CreateRange(9, 5, data.Count + 1, month.Month + 1);

            // Thêm total vào Excel
            int totalRowIndex = data.Count; 
            
            for (int i = 0; i < data.Count; i++)
            {
                for (int j = 0; j < month.Month; j++){
                    rangeData[i, j].PutValue(data[i].Monthly[j]);
                    if(i == 0)
                        rangeData[totalRowIndex, j].PutValue(monthlyTotals[j]);
                }

                rangeData[i, month.Month].PutValue(data[i].Total);
            }
            rangeData[totalRowIndex, month.Month].PutValue(yearlyTotal);
      

            //Thêm "Total" vào cột Employee ID
            ws.Cells[9 + totalRowIndex, 2].PutValue("Total");

            MemoryStream stream = new();
            designer.Workbook.Save(stream, SaveFormat.Xlsx);
            return new OperationResult(true, new { TotalRows = data.Count, Excel = stream.ToArray() });
        }
        #endregion

        #region Get Total Rows
        public async Task<int> GetTotalRows(AnnualOvertimeHoursReportParam param)
        {
            var data = await GetData(param);
            return data.Count;
        }
        #endregion

        #region Get Data
        private async Task<List<AnnualOvertimeHoursReportDto>> GetData(AnnualOvertimeHoursReportParam param)
        {
            var Current_Year_Month = DateTime.Parse(param.Year_Month);
            var First_Year_Month = ToFirstDateOfYear(Current_Year_Month);
            var Leave_Code = new List<string>() { "A01", "A04", "B01", "B02", "C01" };
            var predPersonal = PredicateBuilder.New<HRMS_Emp_Personal>(x => (x.Resign_Date.HasValue == false
                || (x.Resign_Date.HasValue && x.Resign_Date.Value.Date >= Current_Year_Month.Date))
                && (x.Factory == param.Factory || x.Assigned_Factory == param.Factory));

            var predResignMonthlyDetail = PredicateBuilder.New<HRMS_Att_Resign_Monthly_Detail>(x => x.Factory == param.Factory
                && Leave_Code.Contains(x.Leave_Code)
                && x.Leave_Type == "2"
                && x.Att_Month.Date >= First_Year_Month.Date
                && x.Att_Month.Date <= Current_Year_Month.Date);

            var predResignMonthly = PredicateBuilder.New<HRMS_Att_Monthly_Detail>(x => x.Factory == param.Factory
                && Leave_Code.Contains(x.Leave_Code)
                && x.Leave_Type == "2"
                && x.Att_Month.Date >= First_Year_Month.Date
                && x.Att_Month.Date <= Current_Year_Month.Date);

            if (!string.IsNullOrWhiteSpace(param.Department))
                predPersonal.And(x => x.Department == param.Department || x.Assigned_Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                predPersonal.And(x => x.Employee_ID == param.Employee_ID.Trim() || x.Assigned_Employee_ID == param.Employee_ID.Trim());

            var HEP = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(predPersonal, true).ToListAsync();
            var HARMD = _repositoryAccessor.HRMS_Att_Resign_Monthly_Detail.FindAll(predResignMonthlyDetail, true).ToList();
            var HAMD = _repositoryAccessor.HRMS_Att_Monthly_Detail.FindAll(predResignMonthly, true).ToList();

            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll(true).ToList();
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true).ToList();
            var Department = HOD
                .GroupJoin(HODL,
                    HOD => new { HOD.Division, HOD.Factory, HOD.Department_Code },
                    HODL => new { HODL.Division, HODL.Factory, HODL.Department_Code },
                    (HOD, HODL) => new { HOD, HODL })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, HODL) => new { x.HOD, HODL })
                .Select(x => new
                {
                    x.HOD.Factory,
                    x.HOD.Division,
                    x.HOD.Department_Code,
                    Department_Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name
                }).ToList();

            List<AnnualOvertimeHoursReportDto> result = new();

            HEP.ForEach(item =>
            {
                var data = new AnnualOvertimeHoursReportDto()
                {
                    Department_Code = item.Employment_Status is null ? Department.FirstOrDefault(y => y.Division == item.Division
                                            && y.Factory == item.Factory
                                            && y.Department_Code == item.Department)?.Department_Code : (item.Employment_Status == "A" || item.Employment_Status == "S") ? Department.FirstOrDefault(y => y.Division == item.Assigned_Division
                                            && y.Factory == item.Assigned_Factory
                                            && y.Department_Code == item.Assigned_Department)?.Department_Code : "",
                    Department_Name = item.Employment_Status is null ? Department.FirstOrDefault(y => y.Division == item.Division
                                            && y.Factory == item.Factory
                                            && y.Department_Code == item.Department)?.Department_Name : (item.Employment_Status == "A" || item.Employment_Status == "S") ?
                                            Department.FirstOrDefault(y => y.Division == item.Assigned_Division
                                            && y.Factory == item.Assigned_Factory
                                            && y.Department_Code == item.Assigned_Department)?.Department_Name : "",
                    Employee_ID = item.Employee_ID,
                    Local_Full_Name = item.Local_Full_Name,
                    Onboard_Date = item.Onboard_Date.ToString("yyyy/MM/dd"),
                };

                var employee_HARMD = HARMD.Where(x => x.USER_GUID == item.USER_GUID).ToList();
                var employee_HAMD = HAMD.Where(x => x.USER_GUID == item.USER_GUID).ToList();
                var isResigned = item.Resign_Date.HasValue && item.Resign_Date.Value.Month == Current_Year_Month.Month;

                for (int i = 1; i <= Current_Year_Month.Month; i++)
                    data.Monthly.Add(CalculationDay(isResigned, i, employee_HAMD, employee_HARMD));

                data.Total = data.Monthly.Sum();

                result.Add(data);
            });

            return result.OrderBy(x => x.Department_Code).ThenBy(x => x.Employee_ID).ThenBy(x => x.Onboard_Date).ToList();
        }
        #endregion

        private readonly Func<bool, int, IEnumerable<HRMS_Att_Monthly_Detail>, IEnumerable<HRMS_Att_Resign_Monthly_Detail>, decimal> CalculationDay = (isResigned, month, active, resign) =>
        {
            if (isResigned)
                return resign.Where(x => x?.Att_Month.Month == month).ToList().Sum(x => x?.Days ?? 0);
            else
                return active.Where(x => x?.Att_Month.Month == month).ToList().Sum(x => x?.Days ?? 0);
        };

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string language, List<string> roleList)
        {
            var factoriesByAccount = await Queryt_Factory_AddList(roleList);
            var factories = await GetDataBasicCode(BasicCodeTypeConstant.Factory, language);

            return factories.IntersectBy(factoriesByAccount, x => x.Key).ToList();
        }

        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
        {
            var HOD = await Query_Department_List(factory);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Factory == factory
                           && x.Language_Code.ToLower() == language.ToLower());

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

        private static DateTime ToFirstDateOfYear(DateTime dt) => new(dt.Year, 1, 1);
    }
}