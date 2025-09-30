using System.Globalization;
using API.Data;
using API._Services.Interfaces.SalaryReport;
using API.DTOs.SalaryReport;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using AgileObjects.AgileMapper.Extensions;
using System.Drawing;
using AgileObjects.AgileMapper;

namespace API._Services.Services.SalaryReport
{
    public class S_7_2_16_DownloadPersonnelDatatoEXCEL : BaseServices, I_7_2_16_DownloadPersonnelDatatoEXCEL
    {
        private static readonly string rootPath = Directory.GetCurrentDirectory();
        public S_7_2_16_DownloadPersonnelDatatoEXCEL(DBContext dbContext) : base(dbContext)
        {
        }
        #region GetData
        private async Task<OperationResult> GetData(D_7_2_16_DownloadPersonnelDatatoEXCELParam param)
        {
            var Today = DateTime.Now;
            var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory);
            switch (param.ReportKind)
            {
                case "EmployeeMasterFile":
                    switch (param.EmployeeKind)
                    {
                        case "All":
                            if (!string.IsNullOrWhiteSpace(param.StartDate) && !string.IsNullOrWhiteSpace(param.EndDate))
                                predHEP.And(x => x.Onboard_Date.Date <= Convert.ToDateTime(param.EndDate).Date && ((x.Resign_Date.HasValue && x.Resign_Date.Value.Date >= Convert.ToDateTime(param.StartDate).Date) || !x.Resign_Date.HasValue));
                            break;
                        case "Onjob":
                            if (string.IsNullOrWhiteSpace(param.StartDate) && string.IsNullOrWhiteSpace(param.EndDate))
                                predHEP.And(x => (x.Resign_Date.HasValue && x.Resign_Date.Value.Date > Today.Date) || !x.Resign_Date.HasValue);
                            else
                                predHEP.And(x => (x.Resign_Date.HasValue && x.Resign_Date.Value.Date > Convert.ToDateTime(param.EndDate).Date) || !x.Resign_Date.HasValue);
                            break;
                        case "Resigned":
                            if (string.IsNullOrWhiteSpace(param.StartDate) && string.IsNullOrWhiteSpace(param.EndDate))
                                predHEP.And(x => !x.Resign_Date.HasValue);
                            else
                                predHEP.And(x => x.Resign_Date.HasValue && x.Resign_Date.Value.Date <= Convert.ToDateTime(param.EndDate).Date && x.Resign_Date.Value.Date >= Convert.ToDateTime(param.StartDate).Date);
                            break;
                        default:
                            break;
                    }
                    predHEP.And(x => param.PermissionGroup.Contains(x.Permission_Group));
                    var employeeMasterFile = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true).ToListAsync();

                    return new OperationResult(true, employeeMasterFile);

                case "SalaryMasterFile":
                    switch (param.EmployeeKind)
                    {
                        case "All":
                            break;
                        case "Onjob":
                            if (string.IsNullOrWhiteSpace(param.StartDate) && string.IsNullOrWhiteSpace(param.EndDate))
                                predHEP.And(x => (x.Resign_Date.HasValue && x.Resign_Date.Value.Date > Today.Date) || !x.Resign_Date.HasValue);
                            else
                                predHEP.And(x => (x.Resign_Date.HasValue && x.Resign_Date.Value.Date > Convert.ToDateTime(param.EndDate).Date) || !x.Resign_Date.HasValue);
                            break;
                        case "Resigned":
                            if (string.IsNullOrWhiteSpace(param.StartDate) && string.IsNullOrWhiteSpace(param.EndDate))
                                predHEP.And(x => !x.Resign_Date.HasValue);
                            else
                                predHEP.And(x => x.Resign_Date.HasValue && x.Resign_Date.Value.Date <= Convert.ToDateTime(param.EndDate).Date && x.Resign_Date.Value.Date >= Convert.ToDateTime(param.StartDate).Date);
                            break;
                        default:
                            break;
                    }
                    predHEP.And(x => param.PermissionGroup.Contains(x.Permission_Group));
                    var salaryMasterFile = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true).ToListAsync();
                    return new OperationResult(true, salaryMasterFile);
                case "MonthlyAttendance":
                    switch (param.EmployeeKind)
                    {
                        case "All":
                            break;
                        case "Onjob":
                            if (string.IsNullOrWhiteSpace(param.StartDate) && string.IsNullOrWhiteSpace(param.EndDate))
                                predHEP.And(x => (x.Resign_Date.HasValue && x.Resign_Date.Value.Date > Today.Date) || !x.Resign_Date.HasValue);
                            else
                                predHEP.And(x => (x.Resign_Date.HasValue && x.Resign_Date.Value.Date > Convert.ToDateTime(param.EndDate).Date) || !x.Resign_Date.HasValue);
                            break;
                        case "Resigned":
                            if (string.IsNullOrWhiteSpace(param.StartDate) && string.IsNullOrWhiteSpace(param.EndDate))
                                predHEP.And(x => !x.Resign_Date.HasValue);
                            else
                                predHEP.And(x => x.Resign_Date.HasValue && x.Resign_Date.Value.Date <= Convert.ToDateTime(param.EndDate).Date && x.Resign_Date.Value.Date >= Convert.ToDateTime(param.StartDate).Date);
                            break;
                        default:
                            break;
                    }
                    if (param.EmployeeKind == "Resigned")
                    {
                        var predHARM = PredicateBuilder.New<HRMS_Att_Resign_Monthly>(x => x.Factory == param.Factory && param.PermissionGroup.Contains(x.Permission_Group) && x.Att_Month.Date == Convert.ToDateTime(param.YearMonth).Date);
                        var monthlyAttendance = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true)
                                            .Join(_repositoryAccessor.HRMS_Att_Resign_Monthly.FindAll(predHARM, true).Project().To<HRMS_Att_Monthly>(),
                                            x => new { x.Factory, x.Employee_ID },
                                            y => new { y.Factory, y.Employee_ID },
                                            (x, y) => new TabelGetData { HEP = x, HAM = y }).ToListAsync();
                        return new OperationResult(true, monthlyAttendance);
                    }
                    else
                    {
                        var predHAM = PredicateBuilder.New<HRMS_Att_Monthly>(x => x.Factory == param.Factory && param.PermissionGroup.Contains(x.Permission_Group) && x.Att_Month.Date == Convert.ToDateTime(param.YearMonth).Date);
                        var monthlyAttendance = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true)
                                            .Join(_repositoryAccessor.HRMS_Att_Monthly.FindAll(predHAM, true),
                                            x => new { x.Factory, x.Employee_ID },
                                            y => new { y.Factory, y.Employee_ID },
                                            (x, y) => new TabelGetData { HEP = x, HAM = y }).ToListAsync();
                        return new OperationResult(true, monthlyAttendance);
                    }

                case "MonthlySalary":
                    switch (param.EmployeeKind)
                    {
                        case "All":
                            break;
                        case "Onjob":
                            if (string.IsNullOrWhiteSpace(param.StartDate) && string.IsNullOrWhiteSpace(param.EndDate))
                                predHEP.And(x => (x.Resign_Date.HasValue && x.Resign_Date.Value.Date > Today.Date) || !x.Resign_Date.HasValue);
                            else
                                predHEP.And(x => (x.Resign_Date.HasValue && x.Resign_Date.Value.Date > Convert.ToDateTime(param.EndDate).Date) || !x.Resign_Date.HasValue);
                            break;
                        case "Resigned":
                            if (string.IsNullOrWhiteSpace(param.StartDate) && string.IsNullOrWhiteSpace(param.EndDate))
                                predHEP.And(x => !x.Resign_Date.HasValue);
                            else
                                predHEP.And(x => x.Resign_Date.HasValue && x.Resign_Date.Value.Date <= Convert.ToDateTime(param.EndDate).Date && x.Resign_Date.Value.Date >= Convert.ToDateTime(param.StartDate).Date);
                            break;
                        default:
                            break;
                    }
                    if (param.EmployeeKind == "Resigned")
                    {
                        var predHSRM = PredicateBuilder.New<HRMS_Sal_Resign_Monthly>(x => x.Factory == param.Factory && param.PermissionGroup.Contains(x.Permission_Group) && x.Sal_Month.Date == Convert.ToDateTime(param.YearMonth).Date);
                        var monthlySalary = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true)
                                            .Join(_repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(predHSRM, true).Project().To<HRMS_Sal_Monthly>(),
                                            x => new { x.Factory, x.Employee_ID },
                                            y => new { y.Factory, y.Employee_ID },
                                            (x, y) => new TabelGetData { HEP = x, HSM = y }).ToListAsync();
                        return new OperationResult(true, monthlySalary);
                    }
                    else
                    {
                        var predHSM = PredicateBuilder.New<HRMS_Sal_Monthly>(x => x.Factory == param.Factory && param.PermissionGroup.Contains(x.Permission_Group) && x.Sal_Month.Date == Convert.ToDateTime(param.YearMonth).Date);
                        var monthlySalary = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true)
                                            .Join(_repositoryAccessor.HRMS_Sal_Monthly.FindAll(predHSM, true),
                                            x => new { x.Factory, x.Employee_ID },
                                            y => new { y.Factory, y.Employee_ID },
                                            (x, y) => new TabelGetData { HEP = x, HSM = y }).ToListAsync();
                        return new OperationResult(true, monthlySalary);
                    }

                default:
                    return new OperationResult(false);
            }
        }
        #endregion
        #region GetTotalRows
        public async Task<OperationResult> GetTotalRows(D_7_2_16_DownloadPersonnelDatatoEXCELParam param)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;
            switch (param.ReportKind)
            {
                case "EmployeeMasterFile":
                    var employeeMasterFile = (List<HRMS_Emp_Personal>)result.Data;
                    return new OperationResult(true, employeeMasterFile.Count());
                case "SalaryMasterFile":
                    var salaryMasterFile = (List<HRMS_Emp_Personal>)result.Data;
                    return new OperationResult(true, salaryMasterFile.Count());
                case "MonthlyAttendance":
                    var monthlyAttendance = (List<TabelGetData>)result.Data;
                    return new OperationResult(true, monthlyAttendance.Count());
                case "MonthlySalary":
                    var monthlySalary = (List<TabelGetData>)result.Data;
                    return new OperationResult(true, monthlySalary.Count());
                default:
                    return new OperationResult(false);
            }
        }
        #endregion
        #region Download
        public async Task<OperationResult> Download(D_7_2_16_DownloadPersonnelDatatoEXCELParam param)
        {

            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;

            switch (param.ReportKind)
            {
                case "EmployeeMasterFile":
                    //EmployeeMasterFile
                    var employeeMasterFile = (List<HRMS_Emp_Personal>)result.Data;
                    if (!employeeMasterFile.Any())
                        return new OperationResult(false, "System.Message.NoData");
                    var DataDownloadEmployeeMasterFile = await GetDataByEmployeeMasterFile(param, employeeMasterFile);
                    ExcelResult excelResult = DownloadEmployeeMasterFile(param, DataDownloadEmployeeMasterFile);
                    if (excelResult.IsSuccess)
                        return new OperationResult(excelResult.IsSuccess, new { TotalRows = DataDownloadEmployeeMasterFile.Count, Excel = excelResult.Result });
                    else
                        return new OperationResult(excelResult.IsSuccess, excelResult.Error);
                case "SalaryMasterFile":
                    //SalaryMasterFile
                    var salaryMasterFile = (List<HRMS_Emp_Personal>)result.Data;
                    if (!salaryMasterFile.Any())
                        return new OperationResult(false, "System.Message.NoData");
                    var DataDownloadSalaryMasterFile = await GetDataBySalaryMasterFile(param, salaryMasterFile);
                    ExcelResult excelResult2 = DownloadSalaryMasterFile(param, DataDownloadSalaryMasterFile);
                    if (excelResult2.IsSuccess)
                        return new OperationResult(excelResult2.IsSuccess, new { TotalRows = DataDownloadSalaryMasterFile.Count, Excel = excelResult2.Result });
                    else
                        return new OperationResult(excelResult2.IsSuccess, excelResult2.Error);
                case "MonthlyAttendance":
                    //MonthlyAttendance
                    var monthlyAttendance = (List<TabelGetData>)result.Data;
                    if (!monthlyAttendance.Any())
                        return new OperationResult(false, "System.Message.NoData");
                    var DataDownloadmonthlyAttendance = await GetDataByMonthlyAttendance(param, monthlyAttendance);
                    var excelResult3 = DownloadMonthlyAttendance(param, DataDownloadmonthlyAttendance);
                    if (excelResult3.IsSuccess)
                        return new OperationResult(excelResult3.IsSuccess, new { TotalRows = DataDownloadmonthlyAttendance.Count, Excel = excelResult3.Result });
                    else
                        return new OperationResult(excelResult3.IsSuccess, excelResult3.Error);
                case "MonthlySalary":
                    //MonthlySalary
                    var monthlySalary = (List<TabelGetData>)result.Data;
                    if (!monthlySalary.Any())
                        return new OperationResult(false, "System.Message.NoData");
                    var DataDownloadMonthlySalary = await GetDataByMonthlySalary(param, monthlySalary);
                    var excelResult4 = DownloadMonthlySalary(param, DataDownloadMonthlySalary);
                    if (excelResult4.IsSuccess)
                        return new OperationResult(excelResult4.IsSuccess, new { TotalRows = DataDownloadMonthlySalary.Count, Excel = excelResult4.Result });
                    else
                        return new OperationResult(excelResult4.IsSuccess, excelResult4.Error);
                default:
                    return new OperationResult(false);
            }

        }
        public static ExcelResult DownloadExcel(List<Table> dataTable, List<Cell> dataCell, string subPath, ConfigDownload configDownload = null)
        {
            if (configDownload == null)
            {
                configDownload = new ConfigDownload();
            }

            if (!dataTable.Any() && !dataCell.Any())
            {
                return new ExcelResult(isSuccess: false, "No data for excel download");
            }

            try
            {
                MemoryStream memoryStream = new MemoryStream();
                string file = Path.Combine(rootPath, subPath);
                Aspose.Cells.WorkbookDesigner designer = new Aspose.Cells.WorkbookDesigner
                {
                    Workbook = new Aspose.Cells.Workbook(file)
                };
                Aspose.Cells.Worksheet ws = designer.Workbook.Worksheets[0];
                if (dataTable.Any())
                {
                    dataTable.ForEach(delegate (Table item)
                    {
                        designer.SetDataSource(item.Root, item.Data);
                    });
                }

                designer.Process();
                if (dataCell.Any())
                {
                    dataCell.ForEach(delegate (Cell item)
                    {
                        ws.Cells[item.Location].PutValue(item.Value);
                        if (item.IsStyle)
                        {
                            ws.Cells[item.Location].SetStyle(item.Style);
                        }
                    });
                }

                ws.AutoFitColumns(9, ws.Cells.MaxColumn);
                ws.AutoFitRows(4, ws.Cells.MaxDataRow);
                designer.Workbook.Save(memoryStream, configDownload.SaveFormat);
                return new ExcelResult(isSuccess: true, memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                return new ExcelResult(isSuccess: false, ex.InnerException.Message);
            }
        }
        #endregion
        #region EmployeeMasterFile
        private async Task<List<EmployeeMasterFile>> GetDataByEmployeeMasterFile(D_7_2_16_DownloadPersonnelDatatoEXCELParam param, List<HRMS_Emp_Personal> HEP)
        {

            // Getdata
            var HECM = await _repositoryAccessor.HRMS_Emp_Contract_Management.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID), true).OrderByDescending(x => x.Seq).ToListAsync();
            var HIEM = await _repositoryAccessor.HRMS_Ins_Emp_Maintain.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID)
            && new List<string> { "V01", "V02" }.Contains(x.Insurance_Type), true).OrderByDescending(x => x.Insurance_Start).ToListAsync();
            var HEEC = await _repositoryAccessor.HRMS_Emp_Emergency_Contact.FindAll(x => HEP.Select(hep => hep.USER_GUID).Contains(x.USER_GUID), true).ToListAsync();
            var HSM = await _repositoryAccessor.HRMS_Sal_Master.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID), true).ToListAsync();
            var HEUL = await _repositoryAccessor.HRMS_Emp_Unpaid_Leave.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID) && x.Effective_Status, true).ToListAsync();
            var HED = await _repositoryAccessor.HRMS_Emp_Document.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID)
            && x.Document_Type == "01", true).ToListAsync();

            // GetList
            List<string> Type_Seq_Accept = new(){
                BasicCodeTypeConstant.Division,
                BasicCodeTypeConstant.IdentityType,
                BasicCodeTypeConstant.WorkShiftType,
                BasicCodeTypeConstant.WorkType,
                BasicCodeTypeConstant.ReasonResignation,
                BasicCodeTypeConstant.Restaurant,
                BasicCodeTypeConstant.WorkLocation,
                BasicCodeTypeConstant.Education,
                BasicCodeTypeConstant.Religion,
                BasicCodeTypeConstant.TransportationMethod,
                BasicCodeTypeConstant.Province,
                BasicCodeTypeConstant.City,
                BasicCodeTypeConstant.JobTitle,
                BasicCodeTypeConstant.SalaryType};

            var departmentList = await GetListDepartment(param.Factory, param.Language);
            var permissionGroupList = await GetListPermissionGroup(param.Factory, param.Language);
            var FactoryList = await GetListFactory(param.UserName, param.Language);
            var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => Type_Seq_Accept.Contains(x.Type_Seq));
            var listBasicCode = await Query_HRMS_Basic_Code_All(pred, param.Language);
            var divisionList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Division && x.Language_Code?.ToLower() == param.Language.ToLower());
            var IdentityTypeList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.IdentityType && x.Language_Code?.ToLower() == param.Language.ToLower());
            var workShiftTypeList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.WorkShiftType && x.Language_Code?.ToLower() == param.Language.ToLower());
            var workTypeList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.WorkType && x.Language_Code?.ToLower() == param.Language.ToLower());
            var reasonResignationList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.ReasonResignation && x.Language_Code?.ToLower() == param.Language.ToLower());
            var restaurantList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Restaurant && x.Language_Code?.ToLower() == param.Language.ToLower());
            var workLocationList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.WorkLocation && x.Language_Code?.ToLower() == param.Language.ToLower());
            var educationList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Education && x.Language_Code?.ToLower() == param.Language.ToLower());
            var religionList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Religion && x.Language_Code?.ToLower() == param.Language.ToLower());
            var transportationMethodList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.TransportationMethod && x.Language_Code?.ToLower() == param.Language.ToLower());
            var provinceList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Province && x.Language_Code?.ToLower() == param.Language.ToLower());
            var cityList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.City && x.Language_Code?.ToLower() == param.Language.ToLower());
            var jobTitleList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.JobTitle && x.Language_Code?.ToLower() == param.Language.ToLower());
            var SalaryTypeList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.SalaryType && x.Language_Code?.ToLower() == param.Language.ToLower());
            param.FactoryCodeName = FactoryList.FirstOrDefault(x => x.Key == param.Factory).Value;
            param.PermissionGroupCodeName = permissionGroupList.FindAll(x => param.PermissionGroup.Contains(x.Key)).Select(x => x.Value);
            List<EmployeeMasterFile> employeeMasterFiles = new();
            foreach (var emp_Personal in HEP)
            {
                //合同日期 Contract Date
                var Emp_Contract = HECM.FirstOrDefault(x => x.Employee_ID == emp_Personal.Employee_ID);

                // 醫療、社會保險
                // Social 社保
                var Ins_V01 = HIEM.FirstOrDefault(x => x.Employee_ID == emp_Personal.Employee_ID && x.Insurance_Type == "V01");

                //Health醫保
                var Ins_V02 = HIEM.FirstOrDefault(x => x.Employee_ID == emp_Personal.Employee_ID && x.Insurance_Type == "V02");

                //Emergency Contact
                var Emp_Emergency_Contact = HEEC.FirstOrDefault(x => x.USER_GUID == emp_Personal.USER_GUID);

                //Salary Master
                var Sal_Master = HSM.FirstOrDefault(x => x.Employee_ID == emp_Personal.Employee_ID);

                var department = departmentList.FirstOrDefault(x => x.Key == emp_Personal.Department);
                var supportedDepartment = departmentList.FirstOrDefault(x => x.Key == emp_Personal?.Assigned_Department);
                var performanceAssessmentResponsibilityDivision = divisionList.FirstOrDefault(x => x.Code == emp_Personal?.Performance_Division);

                employeeMasterFiles.Add(new EmployeeMasterFile
                {
                    IssuedDate = emp_Personal.Issued_Date,
                    IdentificationNumber = emp_Personal?.Identification_Number,
                    EmploymentStatus = HEUL.FirstOrDefault(x => x.Division == emp_Personal?.Division && x.Employee_ID == emp_Personal?.Employee_ID) is not null ? param.Language == "en" ? "U.Unpaid" : "U.全部" : GetTypeDeletion_Code(emp_Personal?.Deletion_Code, param.Language),
                    Division = emp_Personal?.Division,
                    Factory = emp_Personal?.Factory,
                    EmployeeID = emp_Personal?.Employee_ID,
                    Department = emp_Personal?.Department,
                    DepartmentName = department.Equals(default(KeyValuePair<string, string>)) ? "" : department.Value.Split(" - ")[1],
                    LocalFullName = emp_Personal?.Local_Full_Name,
                    PreferredEnglishFullName = emp_Personal?.Preferred_English_Full_Name,
                    ChineseName = emp_Personal?.Chinese_Name,
                    PassportFullName = HED.FindAll(x => x.Division == emp_Personal?.Division && x.Employee_ID == emp_Personal?.Employee_ID).OrderByDescending(x => x.Validity_Start).FirstOrDefault()?.Passport_Name,
                    Assigned_SupportedDivision = emp_Personal?.Assigned_Division,
                    Assigned_SupportedFactory = emp_Personal?.Assigned_Factory,
                    Assigned_SupportedEmployeeID = emp_Personal?.Assigned_Employee_ID,
                    Assigned_SupportedDepartment = emp_Personal?.Assigned_Department,
                    Assigned_SupportedDepartmentName = supportedDepartment.Equals(default(KeyValuePair<string, string>)) ? "" : supportedDepartment.Value.Length == 2 ? supportedDepartment.Value.Split(" - ")[1] : "",
                    PermissionGroup = permissionGroupList.FirstOrDefault(x => x.Key == emp_Personal?.Permission_Group).Value,
                    CrossFactoryStatus = GetCrossFactoryStatus(emp_Personal?.Employment_Status, param.Language),
                    PerformanceAssessmentResponsibilityDivision = performanceAssessmentResponsibilityDivision?.CodeName,
                    IdentityType = IdentityTypeList.FirstOrDefault(x => x.Code == emp_Personal?.Identity_Type)?.CodeName,
                    Gender = GetGender(emp_Personal?.Gender, param.Language),
                    MaritalStatus = GetMarital_Status(emp_Personal?.Marital_Status, param.Language),
                    DateofBirth = emp_Personal.Birthday,
                    WorkShiftType = workShiftTypeList.FirstOrDefault(x => x.Code == emp_Personal?.Work_Shift_Type)?.CodeName,
                    PregnancyWorkfor8hours = emp_Personal?.Work8hours == true ? "Y" : "N",
                    SalaryType = SalaryTypeList.FirstOrDefault(x => x.Code == Sal_Master?.Salary_Type)?.CodeName,
                    SwipeCardOption = emp_Personal.Swipe_Card_Option ? "Y" : "N",
                    SwipeCardNumber = emp_Personal?.Swipe_Card_Number,
                    PositionGrade = emp_Personal.Position_Grade,
                    PositionTitle = jobTitleList.FirstOrDefault(x => x.Code == emp_Personal?.Position_Title)?.CodeName,
                    WorkType_Job = workTypeList.FirstOrDefault(x => x.Code == emp_Personal?.Work_Type)?.CodeName,
                    OnboardDate = emp_Personal.Onboard_Date,
                    DateofGoupEmployment = emp_Personal.Group_Date,
                    SeniorityStartDate = emp_Personal.Seniority_Start_Date,
                    AnnualLeaveSeniorityStartDate = emp_Personal.Annual_Leave_Seniority_Start_Date,
                    DateofResignation = emp_Personal?.Resign_Date,
                    ReasonforResignation = reasonResignationList.FirstOrDefault(x => x.Code == emp_Personal?.Resign_Reason)?.CodeName, // Thiếu Reason_Resignation ở bảng Personal
                    Blacklist = emp_Personal?.Blacklist == null ? "" : emp_Personal?.Blacklist == true ? "Y" : "N",
                    Restaurant = restaurantList.FirstOrDefault(x => x.Code == emp_Personal?.Restaurant)?.CodeName,
                    WorkLocation = workLocationList.FirstOrDefault(x => x.Code == emp_Personal?.Work_Location)?.CodeName,
                    UnionMembership = emp_Personal?.Union_Membership,
                    Education = educationList.FirstOrDefault(x => x.Code == emp_Personal?.Education)?.CodeName,
                    Religion = religionList.FirstOrDefault(x => x.Code == emp_Personal?.Religion)?.CodeName,
                    TransportationMethod = transportationMethodList.FirstOrDefault(x => x.Code == emp_Personal?.Transportation_Method)?.CodeName,
                    PhoneNumber = emp_Personal?.Phone_Number,
                    MobilePhoneNumber = emp_Personal?.Mobile_Phone_Number,
                    Registered_ProvinceDirectlyGovernedCity = provinceList.FirstOrDefault(x => x.Code == emp_Personal?.Registered_Province_Directly)?.CodeName,
                    Registere_CityDistrictCounty = cityList.FirstOrDefault(x => x.Code == emp_Personal?.Registered_City)?.CodeName,
                    RegisteredAddress = emp_Personal?.Registered_Address,
                    Mailin_ProvinceDirectlyGovernedCity = provinceList.FirstOrDefault(x => x.Code == emp_Personal?.Mailing_Province_Directly)?.CodeName,
                    Mailin_CityDistrictCounty = cityList.FirstOrDefault(x => x.Code == emp_Personal?.Mailing_City)?.CodeName,
                    MailingAddress = emp_Personal?.Mailing_Address,
                    ContractStartDate = Emp_Contract?.Contract_Start,
                    ContractEndDate = Emp_Contract?.Contract_End,
                    HealthInsuranceDateStart = Ins_V01?.Insurance_Start,
                    HealthInsuranceDateEnd = Ins_V01?.Insurance_End,
                    SocialInsuranceNumber = Ins_V01?.Insurance_Num,
                    HealthInsuranceNumber = Ins_V02?.Insurance_Num,
                    EmergencyContact = Emp_Emergency_Contact?.Emergency_Contact,
                    EmergencyTemporaryAddressPhone = Emp_Emergency_Contact?.Emergency_Contact_Phone,
                    TemporaryAddress = Emp_Emergency_Contact?.Temporary_Address,
                    EmergencyContactAddress = Emp_Emergency_Contact?.Emergency_Contact_Address,
                    UpdateBy = emp_Personal?.Update_By,
                    ModificationDate = emp_Personal.Update_Time
                });
            }

            return employeeMasterFiles;
        }
        private ExcelResult DownloadEmployeeMasterFile(D_7_2_16_DownloadPersonnelDatatoEXCELParam param, List<EmployeeMasterFile> DataDownload)
        {
            List<Cell> cells = new()
            {
                new Cell("B2", param.FactoryCodeName),
                new Cell("D2", param.StartDate),
                new Cell("F2", param.EndDate),
                new Cell("H2", string.Join(", ", param.PermissionGroupCodeName)),
                new Cell("J2", param.YearMonth),
                new Cell("B3", param.UserName),
                new Cell("D3", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
            };

            List<Table> tables = new()
            {
                new Table("result", DataDownload)
            };

            ConfigDownload configDownload = new(true);
            ExcelResult excelResult = DownloadExcel(
                tables,
                cells,
                "Resources\\Template\\SalaryReport\\7_2_16_DownloadPersonnelDatatoEXCEL\\EmployeeMasterFile_Download.xlsx",
                configDownload
            );
            return excelResult;
        }
        #endregion
        #region SalaryMasterFile
        private async Task<List<SalaryMasterFile>> GetDataBySalaryMasterFile(D_7_2_16_DownloadPersonnelDatatoEXCELParam param, List<HRMS_Emp_Personal> HEP)
        {
            var today = DateTime.Now;
            // Getdata
            var HSMaster = await _repositoryAccessor.HRMS_Sal_Master.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID), true).ToListAsync();
            var HSMD = await _repositoryAccessor.HRMS_Sal_Master_Detail.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID), true).ToListAsync();
            var HSIS = await _repositoryAccessor.HRMS_Sal_Item_Settings.FindAll(x => x.Factory == param.Factory, true).ToListAsync();
            var HEG = await _repositoryAccessor.HRMS_Emp_Group.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID), true).ToListAsync();
            var HECM = await _repositoryAccessor.HRMS_Emp_Contract_Management.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID), true).OrderByDescending(x => x.Seq).ToListAsync();
            var HIEM = await _repositoryAccessor.HRMS_Ins_Emp_Maintain.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID)
            && new List<string> { "V01", "V02" }.Contains(x.Insurance_Type), true).OrderByDescending(x => x.Insurance_Start).ToListAsync();
            var HEUL = await _repositoryAccessor.HRMS_Emp_Unpaid_Leave.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID) && x.Effective_Status, true).ToListAsync();
            var HSH = await _repositoryAccessor.HRMS_Sal_History.FindAll(x => HEP.Select(hep => hep.USER_GUID).Distinct().Contains(x.USER_GUID), true).ToListAsync();
            // GetList
            var departmentList = await GetListDepartment(param.Factory, param.Language);
            var permissionGroupList = await GetListPermissionGroup(param.Factory, param.Language);
            List<string> Type_Seq_Accept = new(){
                BasicCodeTypeConstant.ReasonResignation,
                BasicCodeTypeConstant.Performance_Category,
                BasicCodeTypeConstant.SalaryType,
                BasicCodeTypeConstant.Expertise_Category,
                BasicCodeTypeConstant.Technical_Type,
                BasicCodeTypeConstant.JobTitle,
                BasicCodeTypeConstant.SalaryItem};

            var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => Type_Seq_Accept.Contains(x.Type_Seq));
            var listBasicCode = await Query_HRMS_Basic_Code_All(pred, param.Language);
            var FactoryList = await GetListFactory(param.UserName, param.Language);
            var reasonResignationList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.ReasonResignation && x.Language_Code?.ToLower() == param.Language.ToLower());
            var Performance_CategoryList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Performance_Category && x.Language_Code?.ToLower() == param.Language.ToLower());
            var SalaryTypeList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.SalaryType && x.Language_Code?.ToLower() == param.Language.ToLower());
            var expertise_CategoryList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Expertise_Category && x.Language_Code?.ToLower() == param.Language.ToLower());
            var technical_TypeList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Technical_Type && x.Language_Code?.ToLower() == param.Language.ToLower());
            var jobTitleList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.JobTitle && x.Language_Code?.ToLower() == param.Language.ToLower());
            var SalaryItemList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.SalaryItem);
            param.FactoryCodeName = FactoryList.FirstOrDefault(x => x.Key == param.Factory).Value;
            param.PermissionGroupCodeName = permissionGroupList.FindAll(x => param.PermissionGroup.Contains(x.Key)).Select(x => x.Value);
            //--Salary Item Sorting
            var Sal_Setting_Temp = HSMaster.Join(HSIS.FindAll(x => x.Effective_Month <= today),
            x => new { x.Permission_Group, x.Salary_Type },
            y => new { y.Permission_Group, y.Salary_Type },
            (x, y) => new { HSMaster = x, HSIS = y }).GroupBy(x => new { x.HSIS.Permission_Group, x.HSIS.Salary_Type, x.HSIS.Salary_Item, x.HSIS.Seq })
            .Select(x => new
            {
                x.Key.Salary_Item,
                MaxSeq = x.Max(x => x.HSIS.Seq)
            });
            var SalaryItemExcel = HSMD.Select(x => x.Salary_Item).Distinct().Select(x =>
            {
                var CodeName_EN = SalaryItemList.FirstOrDefault(si => si.Code == x && si?.Language_Code.ToLower() == "en")?.CodeName;
                var CodeName_TW = SalaryItemList.FirstOrDefault(si => si.Code == x && si?.Language_Code.ToLower() == "tw")?.CodeName;
                return new SalaryItem
                {
                    Code = x,
                    CodeName_EN = CodeName_EN ?? CodeName_TW,
                    CodeName_TW = CodeName_TW ?? CodeName_EN,
                    Value = 0,
                    Seq = Sal_Setting_Temp.FirstOrDefault(sst => sst.Salary_Item == x).MaxSeq
                };
            }).OrderBy(x => x.Seq).ToList();

            List<SalaryMasterFile> salaryMasterFile = new();
            foreach (var emp_Personal in HEP)
            {
                var Sal_Master = HSMaster.FirstOrDefault(x => x.Employee_ID == emp_Personal?.Employee_ID);

                var Sal_Master_Detail = HSMD.FindAll(x => x.Employee_ID == emp_Personal?.Employee_ID);
                //--Salary Item Sorting
                // var Sal_Setting_Temp = HSIS.FindAll(x => x.Permission_Group == Sal_Master?.Permission_Group && x.Salary_Type == Sal_Master?.Salary_Type && x.Effective_Month <= today).OrderByDescending(x => x.Effective_Month).FirstOrDefault();
                var Emp_Group = HEG.FirstOrDefault(x => x.Employee_ID == emp_Personal?.Employee_ID);
                var Emp_Contract = HECM.FirstOrDefault(x => x.Employee_ID == emp_Personal?.Employee_ID);

                // 醫療、社會保險
                // Social 社保
                var Ins_V01 = HIEM.FirstOrDefault(x => x.Employee_ID == emp_Personal?.Employee_ID && x.Insurance_Type == "V01");

                //Health醫保
                var Ins_V02 = HIEM.FirstOrDefault(x => x.Employee_ID == emp_Personal?.Employee_ID && x.Insurance_Type == "V02");

                var department = departmentList.FirstOrDefault(x => x.Key == emp_Personal?.Department);
                var technicalType = technical_TypeList.FirstOrDefault(x => x.Code == Sal_Master?.Technical_Type);
                var expertiseCategory = expertise_CategoryList.FirstOrDefault(x => x.Code == Sal_Master?.Expertise_Category);
                var reasonforResignation = reasonResignationList.FirstOrDefault(x => x.Code == emp_Personal?.Resign_Reason);
                var permissionGroup = permissionGroupList.FirstOrDefault(x => x.Key == Sal_Master?.Permission_Group);
                var salaryType = SalaryTypeList.FirstOrDefault(x => x.Code == Sal_Master?.Salary_Type);
                var performanceCategory = Performance_CategoryList.FirstOrDefault(x => x.Code == Emp_Group?.Performance_Category);

                var salaryItem = SalaryItemExcel.DeepClone();
                if (Sal_Master_Detail.Any())
                {
                    foreach (var itemdetail in Sal_Master_Detail)
                    {
                        var Amount = salaryItem.FirstOrDefault(x => x.Code == itemdetail?.Salary_Item);
                        if (Amount is not null)
                            Amount.Value = itemdetail?.Amount;
                    }
                }
                salaryMasterFile.Add(new SalaryMasterFile
                {
                    EmploymentStatus = HEUL.FirstOrDefault(x => x.Division == emp_Personal?.Division && x.Employee_ID == emp_Personal?.Employee_ID) is not null ? param.Language == "en" ? "U.Unpaid" : "U.全部" : GetTypeDeletion_Code(emp_Personal.Deletion_Code, param.Language),
                    Factory = Sal_Master?.Factory,
                    EmployeeID = emp_Personal?.Employee_ID,
                    LocalFullName = emp_Personal?.Local_Full_Name,
                    Department = emp_Personal?.Department,
                    DepartmentName = department.Equals(default(KeyValuePair<string, string>)) ? "" : department.Value.Split(" - ")[1],
                    PositionGrade = emp_Personal?.Position_Grade,
                    PositionTitle = jobTitleList.FirstOrDefault(x => x.Code == emp_Personal?.Position_Title)?.CodeName,
                    PeriodofActingPosition_Start = Sal_Master?.ActingPosition_Start,
                    PeriodofActingPosition_End = Sal_Master?.ActingPosition_End,
                    TechnicalType = technicalType?.CodeName,
                    ExpertiseCategory = expertiseCategory?.CodeName,
                    OnboardDate = emp_Personal?.Onboard_Date,
                    DateofResignation = emp_Personal?.Resign_Date,
                    ReasonforResignation = reasonforResignation?.CodeName,
                    EffectiveDate = HSH.FindAll(x => x.USER_GUID == Sal_Master?.USER_GUID).Max(x => x?.Effective_Date),
                    PermissionGroup = permissionGroup.Equals(default(KeyValuePair<string, string>)) ? Sal_Master?.Permission_Group : permissionGroup.Value,
                    SalaryType = salaryType?.CodeName,
                    SalaryLevelGrade = Sal_Master?.Salary_Grade,
                    SalaryLevelLevel = Sal_Master?.Salary_Level,
                    Currency = Sal_Master?.Currency,
                    Gender = GetGender(emp_Personal?.Gender, param.Language),
                    IdentificationNumber = emp_Personal?.Identification_Number,
                    DateofBirth = emp_Personal?.Birthday,
                    PerformanceCategory = performanceCategory?.CodeName,
                    ContractStartDate = Emp_Contract?.Contract_Start,
                    ContractEndDate = Emp_Contract?.Contract_End,
                    AnnualLeaveSeniorityStartDate = emp_Personal?.Annual_Leave_Seniority_Start_Date,
                    CompulsoryInsuranceNumber = Ins_V01?.Insurance_Num,
                    HealthInsuranceNumber = Ins_V02?.Insurance_Num,
                    UpdateBy = Sal_Master?.Update_By,
                    UpdateTime = Sal_Master?.Update_Time,
                    salaryItem = salaryItem
                });
            }

            return salaryMasterFile;
        }
        private ExcelResult DownloadSalaryMasterFile(D_7_2_16_DownloadPersonnelDatatoEXCELParam param, List<SalaryMasterFile> DataDownloadSalaryMasterFile)
        {
            List<Cell> cells = new()
            {
                new Cell("B2", param.FactoryCodeName),
                new Cell("D2", param.StartDate),
                new Cell("F2", param.EndDate),
                new Cell("H2", string.Join(", ", param.PermissionGroupCodeName)),
                new Cell("J2", param.YearMonth),
                new Cell("B3", param.UserName),
                new Cell("D3", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
            };
            Aspose.Cells.Style style = new Aspose.Cells.CellsFactory().CreateStyle();
            style.IsTextWrapped = true;
            style.HorizontalAlignment = Aspose.Cells.TextAlignmentType.Right;
            style.VerticalAlignment = Aspose.Cells.TextAlignmentType.Center;
            style.Custom = "#,##0";
            for (int i = 0; i < DataDownloadSalaryMasterFile.Count; i++)
            {
                for (int j = 0; j < DataDownloadSalaryMasterFile[i].salaryItem.Count; j++)
                {
                    if (i < 1)
                    {
                        cells.Add(new Cell(4, j + 31, DataDownloadSalaryMasterFile[0].salaryItem[j].CodeName_TW, GetStyle(221, 235, 247)));
                        cells.Add(new Cell(5, j + 31, DataDownloadSalaryMasterFile[0].salaryItem[j].CodeName_EN, GetStyle(221, 235, 247)));
                    }
                    cells.Add(new Cell(i + 6, j + 31, DataDownloadSalaryMasterFile[i].salaryItem[j].Value, style));
                }
            }

            List<Table> tables = new()
            {
                new Table("result", DataDownloadSalaryMasterFile)
            };

            ConfigDownload configDownload = new(true);
            ExcelResult excelResult = DownloadExcel(
                tables,
                cells,
                "Resources\\Template\\SalaryReport\\7_2_16_DownloadPersonnelDatatoEXCEL\\SalaryMasterFile_Download.xlsx",
                configDownload
            );
            return excelResult;
        }
        #endregion
        #region MonthlyAttendance
        private async Task<List<MonthlyAttendance>> GetDataByMonthlyAttendance(D_7_2_16_DownloadPersonnelDatatoEXCELParam param, List<TabelGetData> monthlyAttendance)
        {
            // Getdata
            var HEP = monthlyAttendance.Select(x => x.HEP);
            var HECM = await _repositoryAccessor.HRMS_Emp_Contract_Management.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID), true).OrderByDescending(x => x.Seq).ToListAsync();
            var HIEM = await _repositoryAccessor.HRMS_Ins_Emp_Maintain.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID)
            && new List<string> { "V01", "V02" }.Contains(x.Insurance_Type), true).OrderByDescending(x => x.Insurance_Start).ToListAsync();
            var HEUL = await _repositoryAccessor.HRMS_Emp_Unpaid_Leave.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID) && x.Effective_Status, true).ToListAsync();

            // GetList
            var departmentList = await GetListDepartment(param.Factory, param.Language);
            var permissionGroupList = await GetListPermissionGroup(param.Factory, param.Language);
            var FactoryList = await GetListFactory(param.UserName, param.Language);
            List<string> Type_Seq_Accept = new(){
                BasicCodeTypeConstant.WorkType,
                BasicCodeTypeConstant.JobTitle,
                BasicCodeTypeConstant.Leave,
                BasicCodeTypeConstant.Allowance,
                BasicCodeTypeConstant.SalaryType,
                BasicCodeTypeConstant.Factory};

            var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => Type_Seq_Accept.Contains(x.Type_Seq));
            var listBasicCode = await Query_HRMS_Basic_Code_All(pred, param.Language);

            var workTypeList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.WorkType && x.Language_Code?.ToLower() == param.Language.ToLower());
            var jobTitleList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.JobTitle && x.Language_Code?.ToLower() == param.Language.ToLower());
            var LeaveList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Leave);
            var AllowanceList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Allowance);
            var SalaryTypeList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.SalaryType && x.Language_Code?.ToLower() == param.Language.ToLower());
            param.FactoryCodeName = FactoryList.FirstOrDefault(x => x.Key == param.Factory).Value;
            param.PermissionGroupCodeName = permissionGroupList.FindAll(x => param.PermissionGroup.Contains(x.Key)).Select(x => x.Value);
            List<MonthlyAttendance> monthlyAttendances = new();
            // Mapper Clear Code
            List<HRMS_Att_Monthly_Detail> HAMD = new();
            if (param.EmployeeKind != "Resigned")
                HAMD = await _repositoryAccessor.HRMS_Att_Monthly_Detail.FindAll(x => x.Factory == param.Factory && x.Att_Month.Date == Convert.ToDateTime(param.YearMonth).Date
                && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID) && new List<string> { "1", "2" }.Contains(x.Leave_Type), true).ToListAsync();
            else
            {
                HAMD = await _repositoryAccessor.HRMS_Att_Resign_Monthly_Detail.FindAll(x => x.Factory == param.Factory && x.Att_Month.Date == Convert.ToDateTime(param.YearMonth).Date
                && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID) && new List<string> { "1", "2" }.Contains(x.Leave_Type), true).Project().To<HRMS_Att_Monthly_Detail>().ToListAsync();
            }
            var tblHRMS_Att_Use_Monthly_Leave = _repositoryAccessor.HRMS_Att_Use_Monthly_Leave.FindAll(s => s.Factory == param.Factory, true);
            DateTime max_Effective_Month = await tblHRMS_Att_Use_Monthly_Leave.Where(s => s.Effective_Month.Date <= Convert.ToDateTime(param.YearMonth).Date).Select(s => s.Effective_Month).MaxAsync();
            // get Leave
            var settingTempLeave1 = tblHRMS_Att_Use_Monthly_Leave.Filter(x => x.Leave_Type == "1").Where(s => s.Effective_Month == max_Effective_Month).ToList();
            var attMonthlybyLeave = HAMD.FindAll(x => x.Leave_Type == "1")
            .Join(settingTempLeave1, x => x.Leave_Code, y => y.Code, (x, y) => new { attMonthlyDetailTemp = x, settingTemp = y })
            .OrderBy(x => x.settingTemp.Seq).ToList();
            var Leave1Item = attMonthlybyLeave.Select(x => x.attMonthlyDetailTemp.Leave_Code).Distinct().Select(x =>
            {
                {
                    var CodeName_EN = LeaveList.FirstOrDefault(si => si.Code == x && si?.Language_Code.ToLower() == "en")?.CodeName;
                    var CodeName_TW = LeaveList.FirstOrDefault(si => si.Code == x && si?.Language_Code.ToLower() == "tw")?.CodeName;
                    return new SalaryItem
                    {
                        Code = x,
                        CodeName_EN = CodeName_EN ?? CodeName_TW,
                        CodeName_TW = CodeName_TW ?? CodeName_EN,
                        Days = 0
                    };
                }
            }).ToList();
            // get Allowance
            var settingTempLeave2 = tblHRMS_Att_Use_Monthly_Leave.Filter(x => x.Leave_Type == "2").Where(s => s.Effective_Month == max_Effective_Month).ToList();
            var attMonthlybyAllowance = HAMD.FindAll(x => x.Leave_Type == "2")
            .Join(settingTempLeave2, x => x.Leave_Code, y => y.Code, (x, y) => new { attMonthlyDetailTemp = x, settingTemp = y })
            .OrderBy(x => x.settingTemp.Seq).ToList();
            var Leave2Item = attMonthlybyAllowance.Select(x => x.attMonthlyDetailTemp.Leave_Code).Distinct().Select(x =>
            {
                var CodeName_EN = AllowanceList.FirstOrDefault(si => si.Code == x && si?.Language_Code.ToLower() == "en")?.CodeName;
                var CodeName_TW = AllowanceList.FirstOrDefault(si => si.Code == x && si?.Language_Code.ToLower() == "tw")?.CodeName;
                return new SalaryItem
                {
                    Code = x,
                    CodeName_EN = CodeName_EN ?? CodeName_TW,
                    CodeName_TW = CodeName_TW ?? CodeName_EN,
                    Days = 0
                };
            }).ToList();

            foreach (var itemExcel in monthlyAttendance)
            {
                HRMS_Emp_Personal emp_Personal = itemExcel.HEP;
                HRMS_Att_Monthly Att_Monthly = itemExcel.HAM;

                var Emp_Contract = HECM.FirstOrDefault(x => x.Employee_ID == emp_Personal?.Employee_ID);
                // 醫療、社會保險
                var Ins_V01 = HIEM.FirstOrDefault(x => x.Employee_ID == emp_Personal?.Employee_ID && x.Insurance_Type == "V01");
                var Ins_V02 = HIEM.FirstOrDefault(x => x.Employee_ID == emp_Personal?.Employee_ID && x.Insurance_Type == "V02");
                var department = departmentList.FirstOrDefault(x => x.Key == Att_Monthly?.Department);
                // get Leave
                var itemsLeaveType1 = Leave1Item.DeepClone();
                var query_Att_Monthly_DetailLeave1 = Query_Att_Monthly_Detail(Att_Monthly.Factory, Att_Monthly.Att_Month, Att_Monthly.Employee_ID, "1", HAMD);
                foreach (var item in query_Att_Monthly_DetailLeave1)
                {
                    var itemUpdate = itemsLeaveType1.FirstOrDefault(x => x.Code == item.Leave_Code);
                    if (itemUpdate is not null)
                        itemUpdate.Days = item.Days;
                }
                // get Allowance
                var itemsLeaveType2 = Leave2Item.DeepClone();
                var query_Att_Monthly_DetailLeave2 = Query_Att_Monthly_Detail(Att_Monthly.Factory, Att_Monthly.Att_Month, Att_Monthly.Employee_ID, "2", HAMD);
                foreach (var item in query_Att_Monthly_DetailLeave2)
                {
                    var itemUpdate = itemsLeaveType1.FirstOrDefault(x => x.Code == item.Leave_Code);
                    if (itemUpdate is not null)
                        itemUpdate.Days = item.Days;
                }
                monthlyAttendances.Add(new MonthlyAttendance
                {
                    YearMonth = Att_Monthly?.Att_Month,
                    Department = Att_Monthly?.Department,
                    DepartmentName = department.Equals(default(KeyValuePair<string, string>)) ? "" : department.Value.Split(" - ")[1],
                    EmployeeID = Att_Monthly?.Employee_ID,
                    LocalFullName = emp_Personal?.Local_Full_Name,
                    NewHiredResigned = Att_Monthly?.Resign_Status,
                    PermissionGroup = permissionGroupList.FirstOrDefault(x => x.Key == Att_Monthly?.Permission_Group).Value,
                    SalaryType = SalaryTypeList.FirstOrDefault(x => x.Code == Att_Monthly?.Salary_Type)?.CodeName,
                    PaidSalaryDays = Att_Monthly?.Salary_Days,
                    ActualWorkDays = Att_Monthly?.Actual_Days,
                    DelayEarlyTimes = Att_Monthly?.Delay_Early,
                    NoSwipCardTimes = Att_Monthly?.No_Swip_Card,
                    DayShiftMealTimes = Att_Monthly?.DayShift_Food,
                    OvertimeMealTimes = Att_Monthly?.Food_Expenses,
                    NightShiftAllowanceTimes = Att_Monthly?.Night_Eat_Times,
                    NightShiftMealTimes = Att_Monthly?.NightShift_Food,
                    OnboardDate = emp_Personal?.Onboard_Date,
                    AnnualLeaveSeniorityStartDate = emp_Personal?.Annual_Leave_Seniority_Start_Date,
                    DateofResignation = emp_Personal?.Resign_Date,
                    WorkTypeJob = workTypeList.FirstOrDefault(x => x.Code == emp_Personal?.Work_Type)?.CodeName,
                    UnionMembership = emp_Personal?.Union_Membership,
                    ContractStartDate = Emp_Contract?.Contract_Start,
                    ContractEndDate = Emp_Contract?.Contract_End,
                    HealthInsuranceDateStart = Ins_V02?.Insurance_Start,
                    HealthInsuranceDateEnd = Ins_V02?.Insurance_End,
                    UpdateBy = Att_Monthly?.Update_By,
                    UpdateTime = Att_Monthly?.Update_Time,
                    ItemsLeaveType1 = itemsLeaveType1,
                    ItemsLeaveType2 = itemsLeaveType2
                });
            }
            return monthlyAttendances;
        }
        private List<AttMonthlyDetailValues> Query_Att_Monthly_Detail(string factory, DateTime yearMonth, string employeeId, string leaveType, List<HRMS_Att_Monthly_Detail> att_Monthly_Details)
        {
            List<AttMonthlyDetailValues> attMonthlyDetailTemp = att_Monthly_Details
                    .FindAll(d => d.Factory == factory && (d.Att_Month.Year == yearMonth.Year && d.Att_Month.Month == yearMonth.Month) && d.Employee_ID == employeeId && d.Leave_Type == leaveType)
                    .Select(d => new D_7_2_16_Att_Monthly_Detail_Temp
                    {
                        Factory = d.Factory,
                        Att_Month = d.Att_Month,
                        Employee_ID = d.Employee_ID,
                        Leave_Type = d.Leave_Type,
                        Leave_Code = d.Leave_Code,
                        Days = d.Days
                    }).Select(x => new AttMonthlyDetailValues
                    {
                        Leave_Code = x.Leave_Code,
                        Days = x.Days,
                    }).ToList();
            return attMonthlyDetailTemp;
        }
        private ExcelResult DownloadMonthlyAttendance(D_7_2_16_DownloadPersonnelDatatoEXCELParam param, List<MonthlyAttendance> DataDownloadMonthlyAttendance)
        {

            List<Cell> cells = new()
            {
                new Cell("B2", param.FactoryCodeName),
                new Cell("D2", param.StartDate),
                new Cell("F2", param.EndDate),
                new Cell("H2", string.Join(", ", param.PermissionGroupCodeName)),
                new Cell("J2", Convert.ToDateTime(param.YearMonth).ToString("yyyy/MM")),
                new Cell("B3", param.UserName),
                new Cell("D3", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
            };
            var MaxList = Math.Max(DataDownloadMonthlyAttendance[0].ItemsLeaveType1.Count, DataDownloadMonthlyAttendance[0].ItemsLeaveType2.Count);
            var lengthLeaveType1 = DataDownloadMonthlyAttendance[0].ItemsLeaveType1.Count;
            for (int i = 0; i < DataDownloadMonthlyAttendance.Count; i++)
            {
                for (int j = 0; j < MaxList; j++)
                {
                    if (i < 1)
                    {
                        if (j < DataDownloadMonthlyAttendance[0].ItemsLeaveType1.Count)
                        {
                            cells.Add(new Cell(3, j + 27, DataDownloadMonthlyAttendance[0].ItemsLeaveType1[j].CodeName_TW, GetStyle(255, 242, 204)));
                            cells.Add(new Cell(4, j + 27, DataDownloadMonthlyAttendance[0].ItemsLeaveType1[j].CodeName_EN, GetStyle(255, 242, 204)));
                        }
                        if (j < DataDownloadMonthlyAttendance[0].ItemsLeaveType2.Count)
                        {
                            cells.Add(new Cell(3, j + lengthLeaveType1 + 27, DataDownloadMonthlyAttendance[0].ItemsLeaveType2[j].CodeName_TW, GetStyle(226, 239, 218)));
                            cells.Add(new Cell(4, j + lengthLeaveType1 + 27, DataDownloadMonthlyAttendance[0].ItemsLeaveType2[j].CodeName_EN, GetStyle(226, 239, 218)));
                        }
                    }
                    if (j < DataDownloadMonthlyAttendance[0].ItemsLeaveType1.Count)
                        cells.Add(new Cell(i + 5, j + 27, DataDownloadMonthlyAttendance[i].ItemsLeaveType1[j].Days));
                    if (j < DataDownloadMonthlyAttendance[0].ItemsLeaveType2.Count)
                        cells.Add(new Cell(i + 5, j + lengthLeaveType1 + 27, DataDownloadMonthlyAttendance[i].ItemsLeaveType2[j].Days));
                }
            }

            List<Table> tables = new()
            {
                new Table("result", DataDownloadMonthlyAttendance)
            };

            ConfigDownload configDownload = new(true);
            ExcelResult excelResult = DownloadExcel(
                tables,
                cells,
                "Resources\\Template\\SalaryReport\\7_2_16_DownloadPersonnelDatatoEXCEL\\MonthlyAttendance_Download.xlsx",
                configDownload
            );
            return excelResult;
        }

        #endregion
        #region MonthlySalary
        private async Task<List<MonthlySalary>> GetDataByMonthlySalary(D_7_2_16_DownloadPersonnelDatatoEXCELParam param, List<TabelGetData> monthlySalary)
        {
            // Getdata
            var HEP = monthlySalary.Select(x => x.HEP);
            var HSMbackup = await _repositoryAccessor.HRMS_Sal_MasterBackup.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID) && x.Sal_Month.Date == Convert.ToDateTime(param.YearMonth).Date, true).ToListAsync();
            var HSM = await _repositoryAccessor.HRMS_Sal_Master.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID), true).ToListAsync();

            // GetList
            var departmentList = await GetListDepartment(param.Factory, param.Language);
            var permissionGroupList = await GetListPermissionGroup(param.Factory, param.Language);
            var FactoryList = await GetListFactory(param.UserName, param.Language);
            List<string> Type_Seq_Accept = new(){
                BasicCodeTypeConstant.JobTitle,
                BasicCodeTypeConstant.WorkType,
                BasicCodeTypeConstant.SalaryItem,
                BasicCodeTypeConstant.Allowance,
                BasicCodeTypeConstant.AdditionsAndDeductionsItem,
                BasicCodeTypeConstant.InsuranceType,
                BasicCodeTypeConstant.Factory};

            var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => Type_Seq_Accept.Contains(x.Type_Seq));
            var listBasicCode = await Query_HRMS_Basic_Code_All(pred, param.Language);
            var jobTitleList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.JobTitle && x.Language_Code?.ToLower() == param.Language.ToLower());
            var workTypeList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.WorkType && x.Language_Code?.ToLower() == param.Language.ToLower());
            var SalaryItemList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.SalaryItem);
            var AllowanceList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Allowance);
            param.additionsAndDeductionsItem = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsItem);
            var InsuranceTypeItemList = listBasicCode.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.InsuranceType);
            param.FactoryCodeName = FactoryList.FirstOrDefault(x => x.Key == param.Factory).Value;
            param.PermissionGroupCodeName = permissionGroupList.FindAll(x => param.PermissionGroup.Contains(x.Key)).Select(x => x.Value);

            List<MonthlySalary> monthlySalaries = new();
            List<HRMS_Sal_Monthly_Detail> sal_Resign_Monthly_Details = new();
            List<HRMS_Att_Monthly> Query_Att_Monthly = new();
            if (param.EmployeeKind != "Resigned")
            {
                sal_Resign_Monthly_Details = await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                .FindAll(x => x.Factory == param.Factory &&
                             x.Sal_Month.Date == Convert.ToDateTime(param.YearMonth).Date &&
                             HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID) &&
                             new List<string> { BasicCodeTypeConstant.SalaryItem, BasicCodeTypeConstant.Allowance, BasicCodeTypeConstant.AdditionsAndDeductionsItem, BasicCodeTypeConstant.InsuranceType }.Contains(x.Type_Seq) &&
                             new List<string> { "A", "D" }.Contains(x.AddDed_Type), true)
                .ToListAsync();
                Query_Att_Monthly = await _repositoryAccessor.HRMS_Att_Monthly.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID)
                    && x.Att_Month.Date == Convert.ToDateTime(param.YearMonth).Date, true).ToListAsync();
            }

            else
            {
                sal_Resign_Monthly_Details = await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
                    .FindAll(x => x.Factory == param.Factory &&
                                 x.Sal_Month.Date == Convert.ToDateTime(param.YearMonth).Date &&
                                 HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID) &&
                                 new List<string> { BasicCodeTypeConstant.SalaryItem, BasicCodeTypeConstant.Allowance, BasicCodeTypeConstant.AdditionsAndDeductionsItem, BasicCodeTypeConstant.InsuranceType }.Contains(x.Type_Seq) &&
                                 new List<string> { "A", "D" }.Contains(x.AddDed_Type), true).Project().To<HRMS_Sal_Monthly_Detail>()
                    .ToListAsync();
                Query_Att_Monthly = await _repositoryAccessor.HRMS_Att_Resign_Monthly.FindAll(x => x.Factory == param.Factory && HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID)
                    && x.Att_Month.Date == Convert.ToDateTime(param.YearMonth).Date, true).Project().To<HRMS_Att_Monthly>().ToListAsync();

            }
            List<HRMS_Sal_Monthly_Detail> sal_Monthly_Detail = await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                .FindAll(x => x.Factory == param.Factory &&
                             x.Sal_Month.Date == Convert.ToDateTime(param.YearMonth).Date &&
                             HEP.Select(hep => hep.Employee_ID).Distinct().Contains(x.Employee_ID) &&
                             new List<string> { BasicCodeTypeConstant.SalaryItem, BasicCodeTypeConstant.Allowance, BasicCodeTypeConstant.AdditionsAndDeductionsItem, BasicCodeTypeConstant.InsuranceType }.Contains(x.Type_Seq) &&
                             new List<string> { "A", "D" }.Contains(x.AddDed_Type), true)
                .ToListAsync();
            // Type_Seq = 45--------------------------------------------------------------------
            var Sal_Setting_Temp = await _repositoryAccessor.HRMS_Sal_Item_Settings
                .FindAll(x => x.Factory == param.Factory &&
                            param.PermissionGroup.Contains(x.Permission_Group) &&
                            monthlySalary.Select(x => x.HSM).Select(x => x.Salary_Type).Distinct().Contains(x.Salary_Type) &&
                            x.Effective_Month.Date <= Convert.ToDateTime(param.YearMonth).Date, true)
                .ToListAsync();
            var maxEffectiveMonth45 = Sal_Setting_Temp.Max(x => x.Effective_Month);
            var sal_Monthly_DetailSortbySeq = sal_Monthly_Detail.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.SalaryItem && x.AddDed_Type == "A")
            .GroupJoin(Sal_Setting_Temp.Where(x => x.Effective_Month == maxEffectiveMonth45),
                detail => detail.Item,
                setting => setting.Salary_Item,
                (detail, settings) => new { detail, settings })
            .SelectMany(x => x.settings.DefaultIfEmpty(),
                (x, setting) => new Sal_Monthly_Detail_Values
                {
                    Seq = setting?.Seq ?? 0,
                    Item = x.detail.Item,
                })
            .OrderBy(x => x.Seq)
            .ToList();
            // SalaryItem as standard
            var SalaryItem = sal_Monthly_DetailSortbySeq.Select(x => x.Item).Distinct().Select(x =>
            {
                var CodeName_EN = SalaryItemList.FirstOrDefault(si => si.Code == x && si?.Language_Code.ToLower() == "en")?.CodeName;
                var CodeName_TW = SalaryItemList.FirstOrDefault(si => si.Code == x && si?.Language_Code.ToLower() == "tw")?.CodeName;

                return new SalaryItem
                {
                    Code = x,
                    CodeName_EN = CodeName_EN ?? CodeName_TW,
                    CodeName_TW = CodeName_TW ?? CodeName_EN,
                    Value = 0,
                };
            }).ToList();

            // Type_Seq = 42--------------------------------------------------------------------       
            var Att_Setting_Temp = await _repositoryAccessor.HRMS_Att_Use_Monthly_Leave
            .FindAll(x => x.Factory == param.Factory &&
                        x.Leave_Type == "2" &&
                        x.Effective_Month.Date <= Convert.ToDateTime(param.YearMonth).Date, true)
            .ToListAsync();

            var maxEffectiveMonth42 = Att_Setting_Temp.Max(x => x.Effective_Month);

            var sal_Monthly_DetailSortbySeq42 = sal_Monthly_Detail.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Allowance && x.AddDed_Type == "A")
            .GroupJoin(Att_Setting_Temp.Where(x => x.Effective_Month == maxEffectiveMonth42),
                detail => detail.Item,
                setting => setting.Code,
                (detail, settings) => new { detail, settings })
            .SelectMany(x => x.settings.DefaultIfEmpty(),
                (x, setting) => new Sal_Monthly_Detail_Values
                {
                    Seq = setting?.Seq ?? 0,
                    Item = x.detail.Item,
                })
            .OrderBy(x => x.Seq)
            .ToList();

            // AllowanceItem as standard
            var AllowanceItem = sal_Monthly_DetailSortbySeq42.Select(x => x.Item).Distinct().Select(x =>
            {
                var CodeName_EN = AllowanceList.FirstOrDefault(si => si.Code == x && si?.Language_Code.ToLower() == "en")?.CodeName;
                var CodeName_TW = AllowanceList.FirstOrDefault(si => si.Code == x && si?.Language_Code.ToLower() == "tw")?.CodeName;
                return new SalaryItem
                {
                    Code = x,
                    CodeName_EN = CodeName_EN ?? CodeName_TW,
                    CodeName_TW = CodeName_TW ?? CodeName_EN,
                    Value = 0,
                };
            }).ToList();

            // Type_Seq = 57--------------------------------------------------------------------       

            var InsuranceTypeItem = sal_Monthly_Detail.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.InsuranceType && x.AddDed_Type == "D").Select(x => x.Item).Distinct().Select(x =>
            {
                var CodeName_EN = InsuranceTypeItemList.FirstOrDefault(si => si.Code == x && si?.Language_Code.ToLower() == "en")?.CodeName;
                var CodeName_TW = InsuranceTypeItemList.FirstOrDefault(si => si.Code == x && si?.Language_Code.ToLower() == "tw")?.CodeName;
                return new SalaryItem
                {
                    Code = x,
                    CodeName_EN = CodeName_EN ?? CodeName_TW,
                    CodeName_TW = CodeName_TW ?? CodeName_EN,
                    Value = 0,
                };
            }).ToList();
            foreach (var items in monthlySalary)
            {
                var Emp_Personal = items.HEP;
                var Sal_Monthly = items.HSM;
                var Sal_Backup = HSMbackup.FirstOrDefault(x => x.Employee_ID == Sal_Monthly?.Employee_ID);
                var Sal_BackupIFNull = HSM.FirstOrDefault(x => x.Employee_ID == Sal_Monthly?.Employee_ID);
                var department = departmentList.FirstOrDefault(x => x.Key == Sal_Monthly?.Department);
                // Get SalaryItem
                var query_Sal_Monthly_Detail = Query_Sal_Monthly_Detail(Sal_Monthly.Factory, Sal_Monthly.Sal_Month, Sal_Monthly.Employee_ID, BasicCodeTypeConstant.SalaryItem, "A", sal_Monthly_Detail);
                // Att_Month
                var Att_Month = Query_Att_Monthly.FirstOrDefault(x => x.Employee_ID == Emp_Personal.Employee_ID);
                var salaryItemUpdate = SalaryItem.DeepClone();
                foreach (var query_Sal in query_Sal_Monthly_Detail)
                {
                    var itemUpdate = salaryItemUpdate.FirstOrDefault(x => x.Code == query_Sal.Code);
                    if (itemUpdate is not null)
                        itemUpdate.Value = query_Sal.Value;
                }
                // Get AllowanceItem
                var query_Sal_Monthly_Detailby42 = Query_Sal_Monthly_Detail(Sal_Monthly.Factory, Sal_Monthly.Sal_Month, Sal_Monthly.Employee_ID, BasicCodeTypeConstant.Allowance, "A", sal_Monthly_Detail);
                var allowanceItemUpdate = AllowanceItem.DeepClone();
                foreach (var query_Sal in query_Sal_Monthly_Detailby42)
                {
                    var itemUpdate = allowanceItemUpdate.FirstOrDefault(x => x.Code == query_Sal.Code);
                    if (itemUpdate is not null)
                        itemUpdate.Value = query_Sal.Value;
                }
                // B49_amt
                var B49_amt = Query_Single_Sal_Monthly_Detail(Sal_Monthly.Factory, Sal_Monthly.Sal_Month, Sal_Monthly.Employee_ID, BasicCodeTypeConstant.AdditionsAndDeductionsItem, "B", "B49", sal_Monthly_Detail);
                // Addition Item And Total Addition Item 
                int total1 = Query_Sal_Monthly_Detail_Sum(Sal_Monthly.Factory, Sal_Monthly.Sal_Month, Sal_Monthly.Employee_ID, BasicCodeTypeConstant.SalaryItem, "A", sal_Monthly_Detail);
                int total2 = Query_Sal_Monthly_Detail_Sum(Sal_Monthly.Factory, Sal_Monthly.Sal_Month, Sal_Monthly.Employee_ID, BasicCodeTypeConstant.Allowance, "A", sal_Monthly_Detail);
                int total3 = Query_Sal_Monthly_Detail_Sum(Sal_Monthly.Factory, Sal_Monthly.Sal_Month, Sal_Monthly.Employee_ID, BasicCodeTypeConstant.AdditionsAndDeductionsItem, "A", sal_Monthly_Detail);
                int total4 = Query_Sal_Monthly_Detail_Sum(Sal_Monthly.Factory, Sal_Monthly.Sal_Month, Sal_Monthly.Employee_ID, BasicCodeTypeConstant.AdditionsAndDeductionsItem, "B", sal_Monthly_Detail);
                var additionItem = total3 + total4 - B49_amt;
                var Add_Total = total1 + total2 + total3 + total4;
                // LoanedAmount
                var LoanedAmount = 0;
                // Get InsuranceTypeItem
                var query_Sal_Monthly_Detailby57 = Query_Sal_Monthly_Detail(Sal_Monthly.Factory, Sal_Monthly.Sal_Month, Sal_Monthly.Employee_ID, BasicCodeTypeConstant.InsuranceType, "D", sal_Monthly_Detail);
                var insuranceTypeItemUpdate = InsuranceTypeItem.DeepClone();
                foreach (var query_Sal in query_Sal_Monthly_Detailby57)
                {
                    var itemUpdate = insuranceTypeItemUpdate.FirstOrDefault(x => x.Code == query_Sal.Code);
                    if (itemUpdate is not null)
                        itemUpdate.Value = query_Sal.Value;
                }
                // Union fee
                var unionfee = Sal_Add_Ded(Sal_Monthly.Factory, Sal_Monthly.Sal_Month, Sal_Monthly.Employee_ID, BasicCodeTypeConstant.AdditionsAndDeductionsItem, "D", "D12", sal_Monthly_Detail);
                // Other Deductions 
                int total1Other = Query_Sal_Monthly_Detail_Sum(Sal_Monthly.Factory, Sal_Monthly.Sal_Month, Sal_Monthly.Employee_ID, BasicCodeTypeConstant.InsuranceType, "D", sal_Monthly_Detail);
                int total2Other = Query_Sal_Monthly_Detail_Sum(Sal_Monthly.Factory, Sal_Monthly.Sal_Month, Sal_Monthly.Employee_ID, BasicCodeTypeConstant.AdditionsAndDeductionsItem, "C", sal_Monthly_Detail);
                int total3Other = Query_Sal_Monthly_Detail_Sum(Sal_Monthly.Factory, Sal_Monthly.Sal_Month, Sal_Monthly.Employee_ID, BasicCodeTypeConstant.AdditionsAndDeductionsItem, "D", sal_Monthly_Detail);

                var otherDeductions = total2Other + total3Other;
                // Total Deduction Item
                var Ded_Total = total1Other + total2Other + total3Other + Sal_Monthly?.Tax ?? 0 + LoanedAmount;
                monthlySalaries.Add(new MonthlySalary
                {
                    YearMonth = Sal_Monthly?.Sal_Month,
                    Department = Sal_Monthly?.Department,
                    DepartmentName = department.Equals(default(KeyValuePair<string, string>)) ? "" : department.Value.Split(" - ")[1],
                    EmployeeID = Sal_Monthly?.Employee_ID,
                    LocalFullName = Emp_Personal?.Local_Full_Name,
                    PositionTitle = jobTitleList.FirstOrDefault(x => x.Code == Emp_Personal?.Position_Title)?.CodeName,
                    WorkTypeJob = workTypeList.FirstOrDefault(x => x.Code == Emp_Personal?.Work_Type)?.CodeName,
                    OnboardDate = Emp_Personal?.Onboard_Date,
                    AnnualLeaveSeniorityStartDate = Emp_Personal?.Annual_Leave_Seniority_Start_Date,
                    DateofResignation = Emp_Personal?.Resign_Date,
                    ActualWorkDays = Att_Month?.Actual_Days,
                    PermissionGroup = permissionGroupList.FirstOrDefault(x => x.Key == Sal_Monthly?.Permission_Group).Value,
                    Transfer = Sal_Monthly?.BankTransfer,
                    Currency = Sal_Monthly?.Currency,
                    SalaryItem = salaryItemUpdate,
                    Allowance = allowanceItemUpdate,
                    SocialHealthUnemploymentInsurance = B49_amt,
                    AdditionItem = additionItem,
                    TotalAdditionItem = Add_Total,
                    LoanedAmount = 0,
                    InsuranceDeduction = insuranceTypeItemUpdate,
                    UnionFee = unionfee,
                    Tax = Sal_Monthly?.Tax,
                    OtherDeduction = otherDeductions - unionfee,
                    TotalDeductionItem = Ded_Total,
                    NetAmountReceived = Add_Total - Ded_Total
                });
            }
            return monthlySalaries;
        }
        private ExcelResult DownloadMonthlySalary(D_7_2_16_DownloadPersonnelDatatoEXCELParam param, List<MonthlySalary> DataDownloadMonthlySalary)
        {
            List<Cell> cells = new()
            {
                new Cell("B2", param.FactoryCodeName),
                new Cell("D2", param.StartDate),
                new Cell("F2", param.EndDate),
                new Cell("H2", string.Join(", ", param.PermissionGroupCodeName)),
                new Cell("J2", Convert.ToDateTime(param.YearMonth).ToString("yyyy/MM")),
                new Cell("B3", param.UserName),
                new Cell("D3", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
            };
            List<int> Count3list = new() { DataDownloadMonthlySalary[0].SalaryItem.Count, DataDownloadMonthlySalary[0].Allowance.Count, DataDownloadMonthlySalary[0].InsuranceDeduction.Count };
            var MaxList = Count3list.Max();
            var SalaryItemCount = Count3list[0];
            var AllowanceCount = Count3list[1];
            var InsuranceDeductionCount = Count3list[2];

            Aspose.Cells.Style style = new Aspose.Cells.CellsFactory().CreateStyle();
            style.Pattern = Aspose.Cells.BackgroundType.Solid;
            style.IsTextWrapped = true;
            style.HorizontalAlignment = Aspose.Cells.TextAlignmentType.Center;
            style.VerticalAlignment = Aspose.Cells.TextAlignmentType.Center;
            style = AsposeUtility.SetAllBorders(style);

            Aspose.Cells.Style stylebody = new Aspose.Cells.CellsFactory().CreateStyle();
            stylebody.IsTextWrapped = true;
            stylebody.HorizontalAlignment = Aspose.Cells.TextAlignmentType.Right;
            stylebody.VerticalAlignment = Aspose.Cells.TextAlignmentType.Center;
            stylebody.Custom = "#,##0";
            for (int i = 0; i < DataDownloadMonthlySalary.Count; i++)
            {
                for (int j = 0; j < MaxList; j++)
                {
                    if (i < 1)
                    {
                        if (j < SalaryItemCount)
                        {
                            cells.Add(new Cell(3, j + 14, DataDownloadMonthlySalary[0].SalaryItem[j].CodeName_TW, GetStyle(255, 242, 204)));
                            cells.Add(new Cell(4, j + 14, DataDownloadMonthlySalary[0].SalaryItem[j].CodeName_EN, GetStyle(255, 242, 204)));
                        }
                        if (j < AllowanceCount)
                        {
                            cells.Add(new Cell(3, j + SalaryItemCount + 14, DataDownloadMonthlySalary[0].Allowance[j].CodeName_TW, GetStyle(255, 242, 204)));
                            cells.Add(new Cell(4, j + SalaryItemCount + 14, DataDownloadMonthlySalary[0].Allowance[j].CodeName_EN, GetStyle(255, 242, 204)));
                        }
                        if (j < InsuranceDeductionCount)
                        {
                            cells.Add(new Cell(3, j + SalaryItemCount + AllowanceCount + 18, DataDownloadMonthlySalary[0].InsuranceDeduction[j].CodeName_TW, style));
                            cells.Add(new Cell(4, j + SalaryItemCount + AllowanceCount + 18, DataDownloadMonthlySalary[0].InsuranceDeduction[j].CodeName_EN, style));
                        }
                    }
                    if (j < SalaryItemCount)
                    {
                        cells.Add(new Cell(i + 5, j + 14, DataDownloadMonthlySalary[i].SalaryItem[j].Value, stylebody));
                    }
                    if (j < AllowanceCount)
                    {
                        cells.Add(new Cell(i + 5, j + SalaryItemCount + 14, DataDownloadMonthlySalary[i].Allowance[j].Value, stylebody));
                    }

                    if (j < InsuranceDeductionCount)
                    {
                        cells.Add(new Cell(i + 5, j + SalaryItemCount + AllowanceCount + 18, DataDownloadMonthlySalary[i].InsuranceDeduction[j].Value, stylebody));
                    }
                }
                if (i < 1)
                {
                    cells.Add(new Cell(3, SalaryItemCount + AllowanceCount + 14, param.additionsAndDeductionsItem.FirstOrDefault(x => x.Code == "B49" && x.Language_Code == "TW").CodeName, style));
                    cells.Add(new Cell(4, SalaryItemCount + AllowanceCount + 14, param.additionsAndDeductionsItem.FirstOrDefault(x => x.Code == "B49" && x.Language_Code == "EN").CodeName, style));
                    cells.Add(new Cell(3, SalaryItemCount + AllowanceCount + 15, "其他加項", style));
                    cells.Add(new Cell(4, SalaryItemCount + AllowanceCount + 15, "Addition Item", style));
                    cells.Add(new Cell(3, SalaryItemCount + AllowanceCount + 16, "正項合計", style));
                    cells.Add(new Cell(4, SalaryItemCount + AllowanceCount + 16, "Total Addition Item", style));
                    cells.Add(new Cell(3, SalaryItemCount + AllowanceCount + 17, "借支金額扣", style));
                    cells.Add(new Cell(4, SalaryItemCount + AllowanceCount + 17, "Loaned Amount", style));
                    cells.Add(new Cell(3, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 18, "工會費", style));
                    cells.Add(new Cell(4, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 18, "Union fee", style));
                    cells.Add(new Cell(3, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 19, "所得稅", style));
                    cells.Add(new Cell(4, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 19, "Tax", style));
                    cells.Add(new Cell(3, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 20, "其他扣項", style));
                    cells.Add(new Cell(4, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 20, "Other Deduction", style));
                    cells.Add(new Cell(3, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 21, "負項合計", style));
                    cells.Add(new Cell(4, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 21, "Total Deduction Item", style));
                    cells.Add(new Cell(3, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 22, "實領金額", style));
                    cells.Add(new Cell(4, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 22, "Net Amount Received", style));
                }
                cells.Add(new Cell(i + 5, SalaryItemCount + AllowanceCount + 14, DataDownloadMonthlySalary[i].SocialHealthUnemploymentInsurance, stylebody));
                cells.Add(new Cell(i + 5, SalaryItemCount + AllowanceCount + 15, DataDownloadMonthlySalary[i].AdditionItem, stylebody));
                cells.Add(new Cell(i + 5, SalaryItemCount + AllowanceCount + 16, DataDownloadMonthlySalary[i].TotalAdditionItem, stylebody));
                cells.Add(new Cell(i + 5, SalaryItemCount + AllowanceCount + 17, DataDownloadMonthlySalary[i].LoanedAmount, stylebody));
                cells.Add(new Cell(i + 5, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 18, DataDownloadMonthlySalary[i].UnionFee, stylebody));
                cells.Add(new Cell(i + 5, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 19, DataDownloadMonthlySalary[i].Tax, stylebody));
                cells.Add(new Cell(i + 5, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 20, DataDownloadMonthlySalary[i].OtherDeduction, stylebody));
                cells.Add(new Cell(i + 5, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 21, DataDownloadMonthlySalary[i].TotalDeductionItem, stylebody));
                cells.Add(new Cell(i + 5, SalaryItemCount + AllowanceCount + InsuranceDeductionCount + 22, DataDownloadMonthlySalary[i].NetAmountReceived, stylebody));
            }

            List<Table> tables = new()
            {
                new Table("result", DataDownloadMonthlySalary)
            };

            ConfigDownload configDownload = new(true);
            ExcelResult excelResult = DownloadExcel(
                tables,
                cells,
                "Resources\\Template\\SalaryReport\\7_2_16_DownloadPersonnelDatatoEXCEL\\MonthlySalary_Download.xlsx",
                configDownload
            );
            return excelResult;
        }
        private List<SalaryItem> Query_Sal_Monthly_Detail(string factory, DateTime yearMonth, string employeeId, string typeSeq, string addedType,
        List<HRMS_Sal_Monthly_Detail> HRMS_Sal_Monthly_Detail)
        {
            return HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month.Date == yearMonth.Date &&
                                 x.Employee_ID == employeeId &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType)
                    .Select(x => new SalaryItem
                    {
                        Code = x.Item,
                        Value = x.Amount
                    }).ToList();
        }
        private int Query_Single_Sal_Monthly_Detail(string factory, DateTime yearMonth, string employeeId, string typeSeq, string addedType, string item,
        List<HRMS_Sal_Monthly_Detail> HRMS_Sal_Monthly_Detail)
        {
            return HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month.Date == yearMonth.Date &&
                                 x.Employee_ID == employeeId &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType &&
                                 x.Item == item)
                    .Sum(x => x.Amount);
        }

        private int Query_Sal_Monthly_Detail_Sum(string factory, DateTime yearMonth, string employeeId, string typeSeq, string addedType,
        List<HRMS_Sal_Monthly_Detail> HRMS_Sal_Monthly_Detail)
        {
            return HRMS_Sal_Monthly_Detail
                .FindAll(x => x.Factory == factory &&
                             x.Sal_Month.Date == yearMonth.Date &&
                             x.Employee_ID == employeeId &&
                             x.Type_Seq == typeSeq &&
                             x.AddDed_Type == addedType)
                .Sum(x => (int?)x.Amount ?? 0);

        }
        private int Sal_Add_Ded(string factory, DateTime yearMonth, string employeeId, string typeSeq, string addedType, string item,
        List<HRMS_Sal_Monthly_Detail> HRMS_Sal_Monthly_Detail)
        {
            return HRMS_Sal_Monthly_Detail
                .FindAll(x => x.Factory == factory &&
                             x.Sal_Month.Date == yearMonth.Date &&
                             x.Employee_ID == employeeId &&
                             x.Type_Seq == typeSeq &&
                             x.AddDed_Type == addedType &&
                             x.Item == item)
                .Sum(x => x.Amount);
        }
        #endregion
        #region Utility function
        public string GetTypeDeletion_Code(string value, string lang)
        {
            switch (value)
            {
                case "Y":
                    return lang == "en" ? "Y.On job" : "Y.在職";
                case "N":
                    return lang == "en" ? "N.Resigned" : "N.離職";
                case "U":
                    return lang == "en" ? "U.Unpaid" : "U.全部";
                default:
                    return "";
            }
        }
        public string GetCrossFactoryStatus(string value, string lang)
        {
            switch (value)
            {
                case "A":
                    return lang == "en" ? "A.Assigned" : "A.派駐";
                case "B":
                    return lang == "en" ? "S.Supported" : "B.支援";
                default:
                    return "";
            }
        }
        public string GetGender(string value, string lang)
        {
            switch (value)
            {
                case "F":
                    return lang == "en" ? "F.Female" : "F.女";
                case "M":
                    return lang == "en" ? "M.Male" : "M.男";
                default:
                    return "";
            }
        }
        public string GetMarital_Status(string value, string lang)
        {
            switch (value)
            {
                case "M":
                    return lang == "en" ? "M.Married " : "M.已婚";
                case "U":
                    return lang == "en" ? "U.Unmarried" : "U.未婚";
                case "O":
                    return lang == "en" ? "O.Other" : "O.其他";
                default:
                    return "";
            }
        }

        private Aspose.Cells.Style GetStyle(int color1, int color2, int color3)
        {
            Aspose.Cells.Style style = new Aspose.Cells.CellsFactory().CreateStyle();
            style.ForegroundColor = Color.FromArgb(color1, color2, color3);
            style.Pattern = Aspose.Cells.BackgroundType.Solid;
            style.IsTextWrapped = true;
            style.HorizontalAlignment = Aspose.Cells.TextAlignmentType.Center;
            style.VerticalAlignment = Aspose.Cells.TextAlignmentType.Center;
            style = AsposeUtility.SetAllBorders(style);
            return style;
        }
        #endregion
        #region GetList
        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
        {
            var departments = await Query_Department_List(factory);
            var departmentsWithLanguage = await _repositoryAccessor.HRMS_Org_Department
                .FindAll(x => x.Factory == factory
                           && departments.Select(y => y.Department_Code).Contains(x.Department_Code), true)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    x => new { x.Division, x.Factory, x.Department_Code },
                    y => new { y.Division, y.Factory, y.Department_Code },
                    (HOD, HODL) => new { HOD, HODL })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, HODL = y })
                .Select(x => new KeyValuePair<string, string>(x.HOD.Department_Code, $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}"))
                .Distinct()
                .ToListAsync();
            return departmentsWithLanguage;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string userName, string language)
        {
            List<string> factories = await Queryt_Factory_AddList(userName);
            var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.Factory && factories.Contains(x.Code));
            return await Query_HRMS_Basic_Code(pred, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language)
        {
            var permissionGroups = await Query_Permission_List(factory);
            var pred = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup && permissionGroups.Select(y => y.Permission_Group).Contains(x.Code));
            return await Query_HRMS_Basic_Code(pred, language);
        }

        private async Task<List<KeyValuePair<string, string>>> Query_HRMS_Basic_Code(ExpressionStarter<HRMS_Basic_Code> pred, string Language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(pred, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
        }
        private async Task<List<BasicCode>> Query_HRMS_Basic_Code_All(ExpressionStarter<HRMS_Basic_Code> pred, string Language)
        {
            var result = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(pred, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new BasicCode
                {
                    Language_Code = x.HBCL != null ? x.HBCL.Language_Code : Language,
                    Type_Seq = x.HBC.Type_Seq,
                    Code = x.HBC.Code,
                    CodeName = $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                })
                .ToListAsync();
            return result;
        }
        #endregion
    }
}