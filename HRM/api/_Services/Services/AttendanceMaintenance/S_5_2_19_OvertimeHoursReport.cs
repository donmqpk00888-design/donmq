using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Aspose.Cells;
using AgileObjects.AgileMapper.Extensions;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_2_19_OvertimeHoursReport : BaseServices, I_5_2_19_OvertimeHoursReport
    {
        private static readonly string rootPath = Directory.GetCurrentDirectory();
        public S_5_2_19_OvertimeHoursReport(DBContext dbContext) : base(dbContext)
        {
        }
        #region Getdata
        public async Task<List<OvertimeHoursReport>> GetData(OvertimeHoursReportParam param, List<string> roleList)
        {
            //Personal
            var predPersonal = PredicateBuilder.New<HRMS_Emp_Personal>(x => param.Permission_Group.Contains(x.Permission_Group) && x.Factory == param.Factory);
            if (!string.IsNullOrWhiteSpace(param.Department))
            {
                predPersonal.And(x => x.Department == param.Department);
            }
            if (!string.IsNullOrWhiteSpace(param.Employee_Id))
            {
                predPersonal.And(x => x.Employee_ID == param.Employee_Id);
            }
            // Overtime_Maintain
            var predOvertime_Maintain = PredicateBuilder.New<HRMS_Att_Overtime_Maintain>();
            // Factory
            predOvertime_Maintain.And(x => x.Factory == param.Factory);
            // Date
            predOvertime_Maintain.And(x => x.Overtime_Date >= Convert.ToDateTime(param.Date_From) && x.Overtime_Date <= Convert.ToDateTime(param.Date_To));
            // Work_Shift_Type
            if (!string.IsNullOrWhiteSpace(param.Work_Shift_Type))
            {
                predOvertime_Maintain.And(x => x.Work_Shift_Type == param.Work_Shift_Type);
            }
            var personalQuery = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(predPersonal, true).ToListAsync();
            var overtimeMaintainQuery = await _repositoryAccessor.HRMS_Att_Overtime_Maintain.FindAll(predOvertime_Maintain, true).ToListAsync();
            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll(true).ToList();
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower(), true).ToList();
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

            var positionTitleList = Query_HRMS_Basic_Code(PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.JobTitle), param.Lang).Result;
            var workShiftList = Query_HRMS_Basic_Code(PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.WorkShiftType), param.Lang).Result;
            var listChangeRecore = await _repositoryAccessor.HRMS_Att_Change_Record.FindAll(x => x.Factory == param.Factory
                            && x.Att_Date >= Convert.ToDateTime(param.Date_From)
                            && x.Att_Date <= Convert.ToDateTime(param.Date_To) && !string.IsNullOrWhiteSpace(x.Clock_In) && !string.IsNullOrWhiteSpace(x.Overtime_ClockOut) && !string.IsNullOrWhiteSpace(x.Overtime_ClockIn) && !string.IsNullOrWhiteSpace(x.Clock_Out), true).ToListAsync();
            List<OvertimeHoursReport> result = new();
            var dataJoin = personalQuery
                .Join(overtimeMaintainQuery,
                    x => new { x.USER_GUID },
                    y => new { y.USER_GUID },
                    (x, y) => new { personal = x, overtime = y });

            if (param.Kind == "O")
            {
                result = dataJoin.GroupBy(x => new { x.overtime.Department, x.overtime.Employee_ID, x.personal.Local_Full_Name, x.overtime.Work_Shift_Type, x.personal.Employment_Status, x.personal.Assigned_Department, x.personal.Factory, x.personal.Division, x.personal.Assigned_Factory, x.personal.Assigned_Division }).Select(x => new OvertimeHoursReport
                {
                    Department = x.Key.Employment_Status is null ? x.Key.Department : (x.Key.Employment_Status == "A" || x.Key.Employment_Status == "S") ? x.Key.Assigned_Department : "",
                    Department_Name = x.Key.Employment_Status is null ? Department.FirstOrDefault(y => y.Division == x.Key.Division
                                            && y.Factory == x.Key.Factory
                                            && y.Department_Code == x.Key.Department)?.Department_Name : (x.Key.Employment_Status == "A" || x.Key.Employment_Status == "S") ?
                                            Department.FirstOrDefault(y => y.Division == x.Key.Assigned_Division
                                            && y.Factory == x.Key.Assigned_Factory
                                            && y.Department_Code == x.Key.Assigned_Department)?.Department_Name : "",
                    Employee_Id = x.Key.Employee_ID,
                    Local_Full_Name = x.Key.Local_Full_Name,
                    Work_Shift_Type = workShiftList.FirstOrDefault(z => z.Key == x.Key.Work_Shift_Type).Value,
                    TrainingHours = x.Sum(x => x.overtime.Training_Hours),
                    RegularOvertime = x.Filter(x => x.overtime.Holiday == "XXX").Sum(x => x.overtime.Overtime_Hours),
                    HolidayOvertime = x.Filter(x => x.overtime.Holiday != "XXX").Sum(x => x.overtime.Overtime_Hours),
                    NightHours = x.Sum(x => x.overtime.Night_Hours),
                    NightOvertimeHour = x.Sum(x => x.overtime.Night_Overtime_Hours)
                }).ToList();
            }
            else
            {
                result = dataJoin.GroupBy(x => new { x.overtime.Department, x.overtime.Employee_ID, x.personal.Local_Full_Name, x.personal.Position_Title, x.personal.Position_Grade, x.overtime.USER_GUID, x.personal.Employment_Status, x.personal.Assigned_Department, x.personal.Factory, x.personal.Division, x.personal.Assigned_Factory, x.personal.Assigned_Division }).Select(x => new OvertimeHoursReport
                {
                    Department = x.Key.Employment_Status is null ? x.Key.Department : (x.Key.Employment_Status == "A" || x.Key.Employment_Status == "S") ? x.Key.Assigned_Department : "",
                    Department_Name = x.Key.Employment_Status is null ? Department.FirstOrDefault(y => y.Division == x.Key.Division
                                            && y.Factory == x.Key.Factory
                                            && y.Department_Code == x.Key.Department)?.Department_Name : (x.Key.Employment_Status == "A" || x.Key.Employment_Status == "S") ?
                                            Department.FirstOrDefault(y => y.Division == x.Key.Assigned_Division
                                            && y.Factory == x.Key.Assigned_Factory
                                            && y.Department_Code == x.Key.Assigned_Department)?.Department_Name : "",
                    Employee_Id = x.Key.Employee_ID,
                    Local_Full_Name = x.Key.Local_Full_Name,
                    PositionTitle = positionTitleList.FirstOrDefault(z => z.Key == x.Key.Position_Title).Value,
                    OvertimeHours = x.Sum(x => x.overtime.Overtime_Hours),
                    WorkingHours = getSumWorkingHours(listChangeRecore, x.Key.USER_GUID)
                }).ToList();
            }
            return result.OrderBy(x => x.Department).ThenBy(x => x.Employee_Id).ToList();
        }
        public decimal getSumWorkingHours(List<HRMS_Att_Change_Record> listChangeRecore, string USER_GUID)
        {
            // Truy xuất dữ liệu trước khi thực hiện tính toán
            var records = listChangeRecore.FindAll(x => x.USER_GUID == USER_GUID);

            // Thực hiện tính toán trên bộ nhớ
            var result = records
                .Select(x => new
                {
                    WorkingHours = (decimal)(ConvertToTimeSpan(getValueHM(x.Overtime_ClockOut, x.Overtime_ClockIn, x.Clock_Out))
                                - ConvertToTimeSpan(x.Clock_In)).TotalHours
                });

            return result.Sum(x => x.WorkingHours);
        }

        public TimeSpan ConvertToTimeSpan(string time)
        {
            if (time.Length != 4)
                throw new ArgumentException("Thời gian phải có định dạng 'HHmm'");

            // Tách giờ và phút
            int hours = int.Parse(time.Substring(0, 2));
            int minutes = int.Parse(time.Substring(2, 2));

            // Tạo đối tượng TimeSpan
            return new TimeSpan(hours, minutes, 0);
        }

        public string getValueHM(string Overtime_ClockOut, string Overtime_ClockIn, string Clock_Out)
        {
            TimeSpan time = ConvertToTimeSpan("0000");
            if (ConvertToTimeSpan(Overtime_ClockOut) > time)
            {
                return Overtime_ClockOut;
            }
            else if (ConvertToTimeSpan(Overtime_ClockIn) > time)
            {
                return Overtime_ClockIn;
            }
            else
            {
                return Clock_Out;
            }
        }
        #endregion
        #region Setup for BaseService
        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string Language, string factory)
        {
            List<HRMS_Emp_Permission_Group> permissionList = await Query_Permission_List(factory);
            var permission = permissionList.Select(x => x.Permission_Group);
            var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup && permission.Contains(x.Code));
            var data = await Query_HRMS_Basic_Code(pred, Language);
            return data;
        }
        public async Task<List<KeyValuePair<string, string>>> GetListWorkShiftType(string Language)
        {
            var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.WorkShiftType);
            var data = await Query_HRMS_Basic_Code(pred, Language);
            return data;
        }

        public async Task<List<KeyValuePair<string, string>>> Query_DropDown_List(string factory, string language)
        {
            var comparisonDepartment = await Query_Department_List(factory);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                    .FindAll(x =>
                        x.Factory == factory &&
                        x.Language_Code.ToLower() == language.ToLower())
                    .ToList();
            var dataDept = comparisonDepartment.GroupJoin(HODL,
                    x => new {x.Division, x.Department_Code},
                    y => new {y.Division, y.Department_Code},
                    (x, y) => new { dept = x, hodl = y })
                    .SelectMany(x => x.hodl.DefaultIfEmpty(),
                    (x, y) => new { x.dept, hodl = y });
            return dataDept.Select(x => new KeyValuePair<string, string>(x.dept.Department_Code, $"{(x.hodl != null ? x.hodl.Name : x.dept.Department_Name)}")).Distinct().ToList();
        }
        public async Task<List<KeyValuePair<string, string>>> Query_Factory_AddList(string userName, string language)
        {
            var factories = await _repositoryAccessor.HRMS_Basic_Role.FindAll(true)
                .Join(_repositoryAccessor.HRMS_Basic_Account_Role.FindAll(x => x.Account == userName, true),
                    HBR => HBR.Role,
                    HBAR => HBAR.Role,
                    (x, y) => new { HBR = x, HBAR = y })
                .Select(x => x.HBR.Factory)
                .Distinct()
                .ToListAsync();

            if (!factories.Any())
                return new();

            var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.Factory && factories.Contains(x.Code));
            var data = await Query_HRMS_Basic_Code(pred, language);

            return data;
        }

        private async Task<List<KeyValuePair<string, string>>> Query_HRMS_Basic_Code(ExpressionStarter<HRMS_Basic_Code> predicate, string Language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(predicate, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
        }
        #endregion
        #region Export
        public async Task<OperationResult> Export(OvertimeHoursReportParam param, List<string> roleList, string userName)
        {
            List<OvertimeHoursReport> data = await GetData(param, roleList);

            ConfigDownload configDownload = new ConfigDownload();
            if (!data.Any())
            {
                return new OperationResult(isSuccess: false, "No data for excel download");
            }

            try
            {
                // Handle param    
                var listDepartment = await Query_DropDown_List(param.Factory, param.Lang);
                var listPermissionGroup = await GetListPermissionGroup(param.Lang, param.Factory);
                var workShiftList = Query_HRMS_Basic_Code(PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.WorkShiftType), param.Lang).Result;
                if (!string.IsNullOrWhiteSpace(param.Work_Shift_Type))
                    param.Work_Shift_Type = workShiftList.FirstOrDefault(x => x.Key == param.Work_Shift_Type).Value;
                if (!string.IsNullOrWhiteSpace(param.Department))
                    param.Department = $"{param.Department} - {listDepartment.FirstOrDefault(x => x.Key == param.Department).Value}";
                var updatedPermissionGroup = new List<string>();
                foreach (var item in param.Permission_Group)
                {
                    var updatedItem = listPermissionGroup.FirstOrDefault(x => x.Key == item).Value;
                    updatedPermissionGroup.Add(updatedItem);
                }
                // Export
                MemoryStream memoryStream = new MemoryStream();
                string subpath = param.Kind == "O" ? "OvertimeHoursStatistics" : "DailyWorkingHoursStatistics";
                string file = Path.Combine(
                    rootPath, 
                    $"Resources\\Template\\AttendanceMaintenance\\5_2_19_OvertimeHoursReport\\{subpath}.xlsx"
                );
                WorkbookDesigner obj = new WorkbookDesigner
                {
                    Workbook = new Workbook(file)
                };
                Worksheet worksheet = obj.Workbook.Worksheets[0];
                // Put Value
                worksheet.Cells["B2"].PutValue(param.Factory);
                worksheet.Cells["E2"].PutValue(param.Department);
                worksheet.Cells["H2"].PutValue(param.Employee_Id);
                worksheet.Cells["K2"].PutValue(param.Date_From + " - " + param.Date_To);
                worksheet.Cells["B4"].PutValue(param.Work_Shift_Type);
                worksheet.Cells["H4"].PutValue(String.Join(",", updatedPermissionGroup));
                worksheet.Cells["B7"].PutValue(userName);
                worksheet.Cells["D7"].PutValue(DateTime.Now.ToString("yyyy/MM/dd  HH:mm:ss"));
                obj.SetDataSource("result", (object)data);
                obj.Process();
                if (configDownload.IsAutoFitColumn)
                {
                    worksheet.AutoFitColumns(worksheet.Cells.MinDataColumn, worksheet.Cells.MaxColumn);
                }

                obj.Workbook.Save(memoryStream, configDownload.SaveFormat);
                return new OperationResult(isSuccess: true, memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                return new OperationResult(isSuccess: false, ex.InnerException.Message);
            }
        }
    }
    #endregion
}