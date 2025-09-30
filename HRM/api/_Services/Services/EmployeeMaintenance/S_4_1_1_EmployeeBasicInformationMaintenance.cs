using System.Text.RegularExpressions;
using API.Data;
using API._Services.Interfaces.EmployeeMaintenance;
using API.DTOs.EmployeeMaintenance;
using API.Helper.Constant;
using API.Helper.Utilities;
using API.Models;
using Aspose.Cells;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.EmployeeMaintenance
{
    public class S_4_1_1_EmployeeBasicInformationMaintenance : BaseServices, I_4_1_1_EmployeeBasicInformationMaintenance
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        public S_4_1_1_EmployeeBasicInformationMaintenance(DBContext dbContext,IWebHostEnvironment webHostEnvironment) : base(dbContext)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<bool> CheckDuplicateCase1(string Nationality, string IdentificationNumber)
        {
            return await _repositoryAccessor.HRMS_Emp_Personal
                .AnyAsync(x => x.Nationality == Nationality
                            && x.Identification_Number == IdentificationNumber.Trim());
        }

        public async Task<bool> CheckDuplicateCase2(CheckDuplicateParam param)
        {
            return await _repositoryAccessor.HRMS_Emp_Personal
                .AnyAsync(x => x.Division == param.Division
                            && x.Factory == param.Factory
                            && x.Employee_ID == param.EmployeeID);
        }

        public async Task<bool> CheckDuplicateCase3(CheckDuplicateParam param)
        {
            return await _repositoryAccessor.HRMS_Emp_Personal
                .AnyAsync(x => x.Assigned_Division == param.Division
                            && x.Assigned_Factory == param.Factory
                            && x.Assigned_Employee_ID == param.EmployeeID);
        }

        public async Task<bool> CheckBlackList(CheckBlackList param)
        {
            var pred = PredicateBuilder.New<HRMS_Emp_Blacklist>(true);

            if (!string.IsNullOrWhiteSpace(param.USER_GUID))
                pred.And(x => x.USER_GUID == param.USER_GUID);
            else if (!string.IsNullOrWhiteSpace(param.Nationality) && !string.IsNullOrWhiteSpace(param.Identification_Number))
                pred.And(x => x.Nationality == param.Nationality
                           && x.Identification_Number == param.Identification_Number);

            return await _repositoryAccessor.HRMS_Emp_Blacklist.AnyAsync(pred);
        }

        public async Task<OperationResult> Add(EmployeeBasicInformationMaintenanceDto dto, string account)
        {
            DateTime current = DateTime.Now;

            // Create new guild
            var user_GUID = Guid.NewGuid().ToString();
            while (await _repositoryAccessor.HRMS_Emp_Personal.AnyAsync(x => x.USER_GUID == user_GUID))
            {
                user_GUID = Guid.NewGuid().ToString();
            }

            HRMS_Emp_Personal emp_Personal = new()
            {
                USER_GUID = user_GUID,
                Nationality = dto.Nationality,
                Identification_Number = dto.IdentificationNumber.Trim(),
                Issued_Date = Convert.ToDateTime(dto.IssuedDateStr),
                Company = dto.Company, // default W
                Deletion_Code = dto.EmploymentStatus,
                Division = dto.Division,
                Factory = dto.Factory,
                Employee_ID = dto.EmployeeID.Trim(),
                Department = dto.Department,
                Assigned_Division = dto.AssignedDivision,
                Assigned_Factory = dto.AssignedFactory,
                Assigned_Employee_ID = dto.AssignedEmployeeID?.Trim(),
                Assigned_Department = dto.AssignedDepartment,
                Permission_Group = dto.PermissionGroup,
                Employment_Status = dto.CrossFactoryStatus,
                Performance_Division = dto.PerformanceAssessmentResponsibilityDivision,
                Identity_Type = dto.IdentityType,
                Local_Full_Name = dto.LocalFullName.Trim(),
                Preferred_English_Full_Name = dto.PreferredEnglishFullName?.Trim(),
                Chinese_Name = dto.ChineseName?.Trim(),
                Gender = dto.Gender,
                Blood_Type = dto.BloodType,
                Marital_Status = dto.MaritalStatus,
                Birthday = Convert.ToDateTime(dto.DateOfBirthStr),
                Phone_Number = dto.PhoneNumber.Trim(),
                Mobile_Phone_Number = dto.MobilePhoneNumber?.Trim(),
                Education = dto.Education,
                Religion = dto.Religion,
                Transportation_Method = dto.TransportationMethod,
                Vehicle_Type = dto.VehicleType,
                License_Plate_Number = dto.LicensePlateNumber?.Trim(),
                Registered_Province_Directly = dto.RegisteredProvinceDirectly,
                Registered_City = dto.RegisteredCity,
                Registered_Address = dto.RegisteredAddress.Trim(),
                Mailing_Province_Directly = dto.MailingProvinceDirectly,
                Mailing_City = dto.MailingCity,
                Mailing_Address = dto.MailingAddress?.Trim(),
                Work_Shift_Type = dto.WorkShiftType, // default A001
                Swipe_Card_Option = dto.SwipeCardOption,
                Swipe_Card_Number = dto.SwipeCardNumber.Trim(),
                Position_Grade = dto.PositionGrade,
                Position_Title = dto.PositionTitle,
                Work_Type = dto.WorkType,
                Restaurant = dto.Restaurant,
                Work_Location = dto.WorkLocation,
                Union_Membership = dto.UnionMembership,
                Work8hours = dto.Work8hours,
                Onboard_Date = Convert.ToDateTime(dto.OnboardDateStr),
                Group_Date = Convert.ToDateTime(dto.DateOfGroupEmploymentStr),
                Seniority_Start_Date = Convert.ToDateTime(dto.SeniorityStartDateStr),
                Annual_Leave_Seniority_Start_Date = Convert.ToDateTime(dto.AnnualLeaveSeniorityStartDateStr),
                Resign_Date = !string.IsNullOrEmpty(dto.DateOfResignationStr) ? Convert.ToDateTime(dto.DateOfResignationStr) : null, // default null
                Resign_Reason = dto.ReasonResignation,// default null
                Blacklist = dto.Blacklist, // default null
                Update_By = account,
                Update_Time = current
            };
            HRMS_Emp_Identity_Card_History emp_Identity_Card_History = new();
            if (!await _repositoryAccessor.HRMS_Emp_Identity_Card_History
                .AnyAsync(x => x.Nationality_After == emp_Personal.Nationality
                    && x.Identification_Number_After == emp_Personal.Identification_Number
                    && x.Issued_Date_After == emp_Personal.Issued_Date))
            {

                // Create new guild
                var user_GUID_HEICH = Guid.NewGuid().ToString();
                while (await _repositoryAccessor.HRMS_Emp_Identity_Card_History.AnyAsync(x => x.USER_GUID == user_GUID_HEICH))
                {
                    user_GUID_HEICH = Guid.NewGuid().ToString();
                }

                emp_Identity_Card_History = new()
                {
                    History_GUID = user_GUID_HEICH,
                    USER_GUID = emp_Personal.USER_GUID,
                    Nationality_Before = emp_Personal.Nationality,
                    Identification_Number_Before = emp_Personal.Identification_Number,
                    Issued_Date_Before = emp_Personal.Issued_Date,
                    Nationality_After = emp_Personal.Nationality,
                    Identification_Number_After = emp_Personal.Identification_Number,
                    Issued_Date_After = emp_Personal.Issued_Date,
                    Update_By = account,
                    Update_Time = current,
                };
            }

            // Create new guild
            var user_GUID_HIEH = Guid.NewGuid().ToString();
            while (await _repositoryAccessor.HRMS_Emp_IDcard_EmpID_History.AnyAsync(x => x.USER_GUID == user_GUID_HIEH))
            {
                user_GUID_HIEH = Guid.NewGuid().ToString();
            }

            HRMS_Emp_IDcard_EmpID_History emp_IDcard_EmpID_History = new()
            {
                History_GUID = user_GUID_HIEH,
                USER_GUID = emp_Personal.USER_GUID,
                Nationality = emp_Personal.Nationality,
                Identification_Number = emp_Personal.Identification_Number,
                Division = emp_Personal.Division,
                Factory = emp_Personal.Factory,
                Employee_ID = emp_Personal.Employee_ID,
                Department = emp_Personal.Department,
                Assigned_Division = emp_Personal.Assigned_Division,
                Assigned_Factory = emp_Personal.Assigned_Factory,
                Assigned_Employee_ID = emp_Personal.Assigned_Employee_ID,
                Assigned_Department = emp_Personal.Assigned_Department,
                Onboard_Date = emp_Personal.Onboard_Date,
                Resign_Date = emp_Personal.Resign_Date,
                Update_By = account,
                Update_Time = current,
            };

            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                _repositoryAccessor.HRMS_Emp_Personal.Add(emp_Personal);
                if (emp_Identity_Card_History != null)
                    _repositoryAccessor.HRMS_Emp_Identity_Card_History.Add(emp_Identity_Card_History);
                _repositoryAccessor.HRMS_Emp_IDcard_EmpID_History.Add(emp_IDcard_EmpID_History);
                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true, data: emp_Personal.USER_GUID);
            }
            catch
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, "System.Message.CreateErrorMsg");
            }
        }

        public async Task<OperationResult> Update(EmployeeBasicInformationMaintenanceDto dto, string account)
        {
            var data = await _repositoryAccessor.HRMS_Emp_Personal
               .FirstOrDefaultAsync(x => x.USER_GUID == dto.USER_GUID);

            if (data == null)
                return new OperationResult(false, "System.Message.NoData");

            data.Permission_Group = dto.PermissionGroup;
            data.Employment_Status = dto.CrossFactoryStatus;
            data.Performance_Division = dto.PerformanceAssessmentResponsibilityDivision;
            data.Identity_Type = dto.IdentityType;
            data.Local_Full_Name = dto.LocalFullName;
            data.Preferred_English_Full_Name = dto.PreferredEnglishFullName;
            data.Chinese_Name = dto.ChineseName;
            data.Gender = dto.Gender;
            data.Blood_Type = dto.BloodType;
            data.Marital_Status = dto.MaritalStatus;
            data.Birthday = Convert.ToDateTime(dto.DateOfBirthStr);
            data.Phone_Number = dto.PhoneNumber;
            data.Mobile_Phone_Number = dto.MobilePhoneNumber;
            data.Education = dto.Education;
            data.Religion = dto.Religion;
            data.Transportation_Method = dto.TransportationMethod;
            data.Vehicle_Type = dto.VehicleType;
            data.License_Plate_Number = dto.LicensePlateNumber;
            data.Registered_Province_Directly = dto.RegisteredProvinceDirectly;
            data.Registered_City = dto.RegisteredCity;
            data.Registered_Address = dto.RegisteredAddress;
            data.Mailing_Province_Directly = dto.MailingProvinceDirectly;
            data.Mailing_City = dto.MailingCity;
            data.Mailing_Address = dto.MailingAddress;
            data.Work_Shift_Type = dto.WorkShiftType;
            data.Swipe_Card_Option = dto.SwipeCardOption;
            data.Swipe_Card_Number = dto.SwipeCardNumber;
            data.Position_Grade = dto.PositionGrade;
            data.Position_Title = dto.PositionTitle;
            data.Work_Type = dto.WorkType;
            data.Restaurant = dto.Restaurant;
            data.Work_Location = dto.WorkLocation;
            data.Union_Membership = dto.UnionMembership;
            data.Work8hours = dto.Work8hours;
            data.Onboard_Date = Convert.ToDateTime(dto.OnboardDateStr);
            data.Group_Date = Convert.ToDateTime(dto.DateOfGroupEmploymentStr);
            data.Seniority_Start_Date = Convert.ToDateTime(dto.SeniorityStartDateStr);
            data.Annual_Leave_Seniority_Start_Date = Convert.ToDateTime(dto.AnnualLeaveSeniorityStartDateStr);
            data.Update_By = account;
            data.Update_Time = DateTime.Now;

            try
            {
                _repositoryAccessor.HRMS_Emp_Personal.Update(data);
                await _repositoryAccessor.Save();
                return new OperationResult(true);
            }
            catch
            {
                return new OperationResult(false, "System.Message.UpdateErrorMsg");
            }
        }

        public async Task<OperationResult> Rehire(EmployeeBasicInformationMaintenanceDto dto, string account)
        {

            var data = await _repositoryAccessor.HRMS_Emp_Personal
               .FirstOrDefaultAsync(x => x.USER_GUID == dto.USER_GUID, true);

            if (data == null)
                return new OperationResult(false, "System.Message.NoData");

            DateTime current = DateTime.Now;

            // Tiến hành cập nhập dữ liệu
            HRMS_Emp_Personal emp_Personal = new()
            {
                USER_GUID = dto.USER_GUID,
                Nationality = dto.Nationality,
                Identification_Number = dto.IdentificationNumber.Trim(),
                Issued_Date = Convert.ToDateTime(dto.IssuedDateStr),
                Company = dto.Company,
                Deletion_Code = dto.EmploymentStatus,
                Division = dto.Division,
                Factory = dto.Factory,
                Employee_ID = dto.EmployeeID.Trim(),
                Department = dto.Department,
                Assigned_Division = dto.AssignedDivision,
                Assigned_Factory = dto.AssignedFactory,
                Assigned_Employee_ID = dto.AssignedEmployeeID?.Trim(),
                Assigned_Department = dto.AssignedDepartment,
                Permission_Group = dto.PermissionGroup,
                Employment_Status = dto.CrossFactoryStatus,
                Performance_Division = dto.PerformanceAssessmentResponsibilityDivision,
                Identity_Type = dto.IdentityType,
                Local_Full_Name = dto.LocalFullName.Trim(),
                Preferred_English_Full_Name = dto.PreferredEnglishFullName?.Trim(),
                Chinese_Name = dto.ChineseName?.Trim(),
                Gender = dto.Gender,
                Blood_Type = dto.BloodType,
                Marital_Status = dto.MaritalStatus,
                Birthday = Convert.ToDateTime(dto.DateOfBirthStr),
                Phone_Number = dto.PhoneNumber.Trim(),
                Mobile_Phone_Number = dto.MobilePhoneNumber?.Trim(),
                Education = dto.Education,
                Religion = dto.Religion,
                Transportation_Method = dto.TransportationMethod,
                Vehicle_Type = dto.VehicleType,
                License_Plate_Number = dto.LicensePlateNumber?.Trim(),
                Registered_Province_Directly = dto.RegisteredProvinceDirectly,
                Registered_City = dto.RegisteredCity,
                Registered_Address = dto.RegisteredAddress.Trim(),
                Mailing_Province_Directly = dto.MailingProvinceDirectly,
                Mailing_City = dto.MailingCity,
                Mailing_Address = dto.MailingAddress?.Trim(),
                Work_Shift_Type = dto.WorkShiftType, // default A001
                Swipe_Card_Option = dto.SwipeCardOption,
                Swipe_Card_Number = dto.SwipeCardNumber.Trim(),
                Position_Grade = dto.PositionGrade,
                Position_Title = dto.PositionTitle,
                Work_Type = dto.WorkType,
                Restaurant = dto.Restaurant,
                Work_Location = dto.WorkLocation,
                Union_Membership = dto.UnionMembership,
                Work8hours = dto.Work8hours,
                Onboard_Date = Convert.ToDateTime(dto.OnboardDateStr),
                Group_Date = Convert.ToDateTime(dto.DateOfGroupEmploymentStr),
                Seniority_Start_Date = Convert.ToDateTime(dto.SeniorityStartDateStr),
                Annual_Leave_Seniority_Start_Date = Convert.ToDateTime(dto.AnnualLeaveSeniorityStartDateStr),
                Resign_Date = !string.IsNullOrEmpty(dto.DateOfResignationStr) ? Convert.ToDateTime(dto.DateOfResignationStr) : null, // default null
                Resign_Reason = dto.ReasonResignation,// default null
                Blacklist = dto.Blacklist, // default null
                Update_By = account,
                Update_Time = current
            };

            HRMS_Emp_Exit_History emp_Exit_History = new()
            {
                USER_GUID = data.USER_GUID,
                Nationality = data.Nationality,
                Identification_Number = data.Identification_Number,
                Issued_Date = data.Issued_Date,
                Company = data.Company,
                Deletion_Code = data.Deletion_Code,
                Division = data.Division,
                Factory = data.Factory,
                Employee_ID = data.Employee_ID,
                Department = data.Department,
                Assigned_Division = data.Assigned_Division,
                Assigned_Factory = data.Assigned_Factory,
                Assigned_Employee_ID = data.Assigned_Employee_ID,
                Assigned_Department = data.Assigned_Department,
                Permission_Group = data.Permission_Group,
                Employment_Status = data.Employment_Status,
                Performance_Assessment_Responsibility_Division = data.Performance_Division,
                Identity_Type = data.Identity_Type,
                Local_Full_Name = data.Local_Full_Name,
                Preferred_English_Full_Name = data.Preferred_English_Full_Name,
                Chinese_Name = data.Chinese_Name,
                Gender = data.Gender,
                Blood_Type = data.Blood_Type,
                Marital_Status = data.Marital_Status,
                Date_of_Birth = data.Birthday,
                Phone_Number = data.Phone_Number,
                Mobile_Phone_Number = data.Mobile_Phone_Number,
                Education = data.Education,
                Religion = data.Religion,
                Transportation_Method = data.Transportation_Method,
                Vehicle_Type = data.Vehicle_Type,
                License_Plate_Number = data.License_Plate_Number,
                Registered_Province_Directly = data.Registered_Province_Directly,
                Registered_City = data.Registered_City,
                Registered_Address = data.Registered_Address,
                Mailing_Province_Directly = data.Mailing_Province_Directly,
                Mailing_City = data.Mailing_City,
                Mailing_Address = data.Mailing_Address,
                Work_Shift_Type = data.Work_Shift_Type,
                Swipe_Card_Option = data.Swipe_Card_Option,
                Swipe_Card_Number = data.Swipe_Card_Number,
                Position_Grade = data.Position_Grade,
                Position_Title = data.Position_Title,
                Work_Type = data.Work_Type,
                Restaurant = data.Restaurant,
                Work8hours = data.Work8hours,
                // Salary_Type 
                // Salary_Payment_Method 
                Work_Location = data.Work_Location,
                Union_Membership = data.Union_Membership,
                Onboard_Date = data.Onboard_Date,
                Group_Date = data.Group_Date,
                Seniority_Start_Date = data.Seniority_Start_Date,
                Annual_Leave_Seniority_Start_Date = data.Annual_Leave_Seniority_Start_Date,
                Resign_Date = data.Resign_Date.Value,
                Resign_Reason = data.Resign_Reason,
                Blacklist = data.Blacklist.Value,
                Update_By = account,
                Update_Time = current,
            };

            var user_GUID_HIEH = Guid.NewGuid().ToString();
            while (await _repositoryAccessor.HRMS_Emp_IDcard_EmpID_History.AnyAsync(x => x.USER_GUID == user_GUID_HIEH))
            {
                user_GUID_HIEH = Guid.NewGuid().ToString();
            }

            HRMS_Emp_IDcard_EmpID_History emp_IDcard_EmpID_History = new()
            {
                History_GUID = user_GUID_HIEH,
                USER_GUID = data.USER_GUID,
                Nationality = data.Nationality,
                Identification_Number = data.Identification_Number,
                Division = data.Division,
                Factory = data.Factory,
                Employee_ID = data.Employee_ID,
                Department = data.Department,
                Assigned_Division = data.Assigned_Division,
                Assigned_Factory = data.Assigned_Factory,
                Assigned_Employee_ID = data.Assigned_Employee_ID,
                Assigned_Department = data.Assigned_Department,
                Onboard_Date = data.Onboard_Date,
                Resign_Date = data.Resign_Date,
                Update_By = account,
                Update_Time = current,
            };

            var emp_Emergency_Contact = await _repositoryAccessor.HRMS_Emp_Emergency_Contact.FindAll(x => x.USER_GUID == dto.USER_GUID).ToListAsync();

            emp_Emergency_Contact.ForEach(x =>
            {
                x.Division = emp_Personal.Division;
                x.Factory = emp_Personal.Factory;
                x.Employee_ID = emp_Personal.Employee_ID;
            });

            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                _repositoryAccessor.HRMS_Emp_Personal.Update(emp_Personal);
                _repositoryAccessor.HRMS_Emp_IDcard_EmpID_History.Add(emp_IDcard_EmpID_History);
                _repositoryAccessor.HRMS_Emp_Exit_History.Add(emp_Exit_History);
                _repositoryAccessor.HRMS_Emp_Emergency_Contact.UpdateMultiple(emp_Emergency_Contact);
                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true);
            }
            catch
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false);
            }
        }

        public async Task<OperationResult> Delete(string USER_GUID)
        {
            var data = await _repositoryAccessor.HRMS_Emp_Personal
                .FirstOrDefaultAsync(x => x.USER_GUID == USER_GUID);

            if (data == null)
                return new OperationResult(false, "Data not exists");

            // Kiểm tra tồn tại trong Emergency Contact
            if (await _repositoryAccessor.HRMS_Emp_Emergency_Contact
                .AnyAsync(x => x.USER_GUID == USER_GUID))
                return new OperationResult(false, "You can not delete this employee");

            // Kiểm tra tồn tại trong Education
            if (await _repositoryAccessor.HRMS_Emp_Educational
                .AnyAsync(x => x.USER_GUID == USER_GUID))
                return new OperationResult(false, "You can not delete this employee");

            // Kiểm tra tồn tại trong Dependent Information
            if (await _repositoryAccessor.HRMS_Emp_Dependent
                .AnyAsync(x => x.USER_GUID == USER_GUID))
                return new OperationResult(false, "You can not delete this employee");

            // Kiểm tra tồn tại trong External Experience
            if (await _repositoryAccessor.HRMS_Emp_External_Experience
                .AnyAsync(x => x.USER_GUID == USER_GUID))
                return new OperationResult(false, "You can not delete this employee");

            var emp_Identity_Card_History = await _repositoryAccessor.HRMS_Emp_Identity_Card_History
                .FindAll(x => x.USER_GUID == data.USER_GUID)
                .ToListAsync();

            var emp_IDcard_EmpID_History = await _repositoryAccessor.HRMS_Emp_IDcard_EmpID_History
                .FindAll(x => x.USER_GUID == data.USER_GUID)
                .ToListAsync();

            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                _repositoryAccessor.HRMS_Emp_Personal.Remove(data);

                if (emp_Identity_Card_History.Any())
                    _repositoryAccessor.HRMS_Emp_Identity_Card_History.RemoveMultiple(emp_Identity_Card_History);

                if (emp_IDcard_EmpID_History.Any())
                    _repositoryAccessor.HRMS_Emp_IDcard_EmpID_History.RemoveMultiple(emp_IDcard_EmpID_History);

                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true);
            }
            catch
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, "System.Message.DeleteErrorMsg");
            }
        }

        public Task<OperationResult> DownloadExcelTemplate()
        {
            var path = Path.Combine(
                _webHostEnvironment.ContentRootPath, 
                "Resources\\Template\\EmployeeMaintenance\\4_1_1_EmployeeBasicInformationMaintenance\\Template.xlsx"
            );
            var workbook = new Workbook(path);
            var design = new WorkbookDesigner(workbook);
            MemoryStream stream = new();
            design.Workbook.Save(stream, SaveFormat.Xlsx);
            var result = stream.ToArray();
            return Task.FromResult(new OperationResult(true, null, result));
        }

        #region Upload
        public async Task<OperationResult> UploadData(IFormFile file, List<string> role_List, string userName)
        {
            ExcelResult resp = ExcelUtility.CheckExcel(
                file, 
                "Resources\\Template\\EmployeeMaintenance\\4_1_1_EmployeeBasicInformationMaintenance\\Template.xlsx"
            );
            if (!resp.IsSuccess)
                return new OperationResult(false, resp.Error);

            List<HRMS_Emp_Personal> empPersonalDatas = new();
            List<HRMS_Emp_Identity_Card_History> cardHistoryDatas = new();
            List<HRMS_Emp_IDcard_EmpID_History> empIDHistoryDatas = new();

            List<EmployeeBasicInformationMaintenanceReport> excelReportList = new();

            var roleFactories = await _repositoryAccessor.HRMS_Basic_Role
                .FindAll(x => role_List.Contains(x.Role))
                .Select(x => x.Factory).Distinct()
                .ToListAsync();
            if (!roleFactories.Any())
                return new OperationResult(false, "Recent account roles do not have any factory");

            List<string> basicCodeTypes = new()
            {
                BasicCodeTypeConstant.Nationality,
                BasicCodeTypeConstant.Division,
                BasicCodeTypeConstant.Factory,
                BasicCodeTypeConstant.PermissionGroup,
                BasicCodeTypeConstant.IdentityType,
                BasicCodeTypeConstant.Education,
                BasicCodeTypeConstant.Religion,
                BasicCodeTypeConstant.TransportationMethod,
                BasicCodeTypeConstant.VehicleType,
                BasicCodeTypeConstant.Province,
                BasicCodeTypeConstant.City,
                BasicCodeTypeConstant.WorkShiftType,
                BasicCodeTypeConstant.JobTitle,
                BasicCodeTypeConstant.WorkType,
                BasicCodeTypeConstant.Restaurant,
                BasicCodeTypeConstant.WorkLocation
            };

            var basicCodes = await GetAllBasicCodes(basicCodeTypes);

            var nationals = basicCodes[BasicCodeTypeConstant.Nationality];
            var divisions = basicCodes[BasicCodeTypeConstant.Division];
            var permissions = basicCodes[BasicCodeTypeConstant.PermissionGroup];
            var identities = basicCodes[BasicCodeTypeConstant.IdentityType];
            var educations = basicCodes[BasicCodeTypeConstant.Education];
            var religions = basicCodes[BasicCodeTypeConstant.Religion];
            var transportationMethods = basicCodes[BasicCodeTypeConstant.TransportationMethod];
            var vehicleTypes = basicCodes[BasicCodeTypeConstant.VehicleType];
            var province = basicCodes[BasicCodeTypeConstant.Province];
            var city = basicCodes[BasicCodeTypeConstant.City];
            var workShiftTypes = basicCodes[BasicCodeTypeConstant.WorkShiftType];
            var positionTitles = basicCodes[BasicCodeTypeConstant.JobTitle];
            var workTypes = basicCodes[BasicCodeTypeConstant.WorkType];
            var restaurants = basicCodes[BasicCodeTypeConstant.Restaurant];
            var workLocate = basicCodes[BasicCodeTypeConstant.WorkLocation];

            string dateFormat = @"^\d{4}/\d{2}/\d{2}$";
            Regex regex = new(dateFormat);

            HashSet<string> processedNationalityIDNumbers = new();
            HashSet<string> processedDivisionFactoryEmployeeIDs = new();
            HashSet<string> processedAssignedDivisionFactoryEmployeeIDs = new();

            List<string> allowGenders = new() { "F", "M" };
            List<string> allowBloods = new() { "A", "B", "C", "O", "AB" };
            List<string> allowMaritalStatus = new() { "M", "U", "O" };
            List<string> allowEmployeeStatus = new() { "A", "S" };
            List<string> allowYesNo = new() { "Y", "N" };

            List<HRMS_Basic_Level> allowLevels = _repositoryAccessor.HRMS_Basic_Level.FindAll().Distinct().ToList();

            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                for (int i = resp.WsTemp.Cells.Rows.Count; i < resp.Ws.Cells.Rows.Count; i++)
                {
                    //Set null to nullable columns
                    List<string> errorMessage = new();
                    var national = resp.Ws.Cells[i, 0].StringValue?.Trim();
                    var identification_Number = resp.Ws.Cells[i, 1].StringValue?.Trim();
                    var issued_Date = resp.Ws.Cells[i, 2].StringValue;
                    var division = resp.Ws.Cells[i, 3].StringValue?.Trim();
                    var factory = resp.Ws.Cells[i, 4].StringValue?.Trim();
                    var employeeID = resp.Ws.Cells[i, 5].StringValue?.Trim();
                    var department = resp.Ws.Cells[i, 6].StringValue?.Trim();
                    var assignedDivision = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 7].StringValue) ? null : resp.Ws.Cells[i, 7].StringValue?.Trim();
                    var assignedFactory = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 8].StringValue) ? null : resp.Ws.Cells[i, 8].StringValue?.Trim();
                    var assignedEmployeeID = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 9].StringValue) ? null : resp.Ws.Cells[i, 9].StringValue?.Trim();
                    var assignedDepartment = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 10].StringValue) ? null : resp.Ws.Cells[i, 10].StringValue?.Trim();
                    var permissionGroup = resp.Ws.Cells[i, 11].StringValue?.Trim();
                    var crossFactoryStatus = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 12].StringValue) ? null : resp.Ws.Cells[i, 12].StringValue?.Trim();
                    var performanceARDivision = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 13].StringValue) ? null : resp.Ws.Cells[i, 13].StringValue?.Trim();
                    var identityType = resp.Ws.Cells[i, 14].StringValue?.Trim();
                    var localFullName = resp.Ws.Cells[i, 15].StringValue?.Trim();
                    var preferredEnglishFullName = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 16].StringValue) ? null : resp.Ws.Cells[i, 16].StringValue?.Trim();
                    var chineseName = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 17].StringValue) ? null : resp.Ws.Cells[i, 17].StringValue?.Trim();
                    var gender = resp.Ws.Cells[i, 18].StringValue?.Trim();
                    var bloodType = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 19].StringValue) ? null : resp.Ws.Cells[i, 19].StringValue?.Trim();
                    var maritalStatus = resp.Ws.Cells[i, 20].StringValue?.Trim();
                    var birthDay = resp.Ws.Cells[i, 21].StringValue?.Trim();
                    var phoneNumber = resp.Ws.Cells[i, 22].StringValue?.Trim();
                    var mobile = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 23].StringValue) ? null : resp.Ws.Cells[i, 23].StringValue?.Trim();
                    var education = resp.Ws.Cells[i, 24].StringValue?.Trim();
                    var religion = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 25].StringValue) ? null : resp.Ws.Cells[i, 25].StringValue?.Trim();
                    var transportationMethod = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 26].StringValue) ? null : resp.Ws.Cells[i, 26].StringValue?.Trim();
                    var vehicleType = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 27].StringValue) ? null : resp.Ws.Cells[i, 27].StringValue?.Trim();
                    var licensePlateNumber = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 28].StringValue) ? null : resp.Ws.Cells[i, 28].StringValue?.Trim();
                    var registeredProvinceDirectly = resp.Ws.Cells[i, 29].StringValue?.Trim();
                    var registeredCity = resp.Ws.Cells[i, 30].StringValue?.Trim();
                    var registeredAddress = resp.Ws.Cells[i, 31].StringValue?.Trim();
                    var mailingProvinceDirectly = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 32].StringValue) ? null : resp.Ws.Cells[i, 32].StringValue?.Trim();
                    var mailingCity = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 33].StringValue) ? null : resp.Ws.Cells[i, 33].StringValue?.Trim();
                    var mailingAddress = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 34].StringValue) ? null : resp.Ws.Cells[i, 34].StringValue?.Trim();
                    var workShiftType = resp.Ws.Cells[i, 35].StringValue?.Trim();
                    var swipeCardOption = resp.Ws.Cells[i, 36].StringValue?.Trim();
                    var swipeCardNumber = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 37].StringValue) ? null : resp.Ws.Cells[i, 37].StringValue?.Trim();
                    var positionGrade = resp.Ws.Cells[i, 38].StringValue?.Trim();
                    var positionTitle = resp.Ws.Cells[i, 39].StringValue?.Trim();
                    var workType = resp.Ws.Cells[i, 40].StringValue?.Trim();
                    var restaurant = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 41].StringValue) ? null : resp.Ws.Cells[i, 41].StringValue?.Trim();
                    var workLocation = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 42].StringValue) ? null : resp.Ws.Cells[i, 42].StringValue?.Trim();
                    var unionMembership = string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 43].StringValue) ? null : resp.Ws.Cells[i, 43].StringValue?.Trim();
                    var onboardDate = resp.Ws.Cells[i, 44].StringValue?.Trim();
                    var groupEmploymentDate = resp.Ws.Cells[i, 45].StringValue?.Trim();
                    var seniorityStartDate = resp.Ws.Cells[i, 46].StringValue?.Trim();
                    var annualLeaveSeniorityStartDate = resp.Ws.Cells[i, 47].StringValue?.Trim();
                    string nationalityIDNumberKey = $"{national}_{identification_Number}";
                    string divisionFactoryEmployeeIDKey = $"{division}_{factory}_{employeeID}";
                    string assignedDivisionFactoryEmployeeIDKey = $"{assignedDivision}_{assignedFactory}_{assignedEmployeeID}";

                    // 0. Nationality
                    if (string.IsNullOrWhiteSpace(national) || !nationals.ContainsKey(national))
                        errorMessage.Add("Nationality is invalid");

                    // 1. Identification Number
                    if (string.IsNullOrWhiteSpace(identification_Number) || identification_Number.Length > 50)
                        errorMessage.Add("Identification Number is invalid");

                    if (!string.IsNullOrWhiteSpace(national) && !string.IsNullOrWhiteSpace(identification_Number))
                    {
                        var onCase1 = await CheckDuplicateCase1(national, identification_Number);
                        CheckBlackList param = new()
                        {
                            Nationality = national,
                            Identification_Number = identification_Number
                        };
                        var onBlackList = await CheckBlackList(param);
                        // Kiểm tra trùng lặp cho Nationality + Identification Number & Blacklist
                        if (processedNationalityIDNumbers.Contains(nationalityIDNumberKey) || onCase1)
                            errorMessage.Add("Nationality and ID number cannot be repeated");
                        else if (onBlackList)
                            errorMessage.Add("On blacklisted");
                        else
                            processedNationalityIDNumbers.Add(nationalityIDNumberKey);
                    }

                    // 2. Issued Date
                    if (string.IsNullOrWhiteSpace(issued_Date) || !regex.IsMatch(issued_Date))
                        errorMessage.Add("Issued Date is invalid. Expected format: YYYY/MM/DD");

                    // 3. Division
                    if (string.IsNullOrWhiteSpace(division) || !divisions.ContainsKey(division))
                        errorMessage.Add("Division is invalid");

                    // 4. Factory
                    if (string.IsNullOrWhiteSpace(factory))
                        errorMessage.Add("Factory is invalid");
                    if (!string.IsNullOrWhiteSpace(division) && !string.IsNullOrWhiteSpace(factory))
                    {
                        var factories = await Query_Factory_List(division);
                        if (!factories.Contains(factory))
                            errorMessage.Add("Factory is not existed");
                        if (!roleFactories.Contains(factory))
                            errorMessage.Add("uploaded [Factory] data does not match the role group");
                    }

                    // 5. Employee ID
                    if (string.IsNullOrWhiteSpace(employeeID) || employeeID.Length > 16)
                        errorMessage.Add("Employee ID is invalid");
                    else
                    {
                        if (!employeeID.Contains('-'))
                            employeeID = $"{factory}-{employeeID}";
                        else
                        {
                            var parts = employeeID.Split('-');
                            if (parts[0] != factory)
                                errorMessage.Add("Cannot find the corresponding factory with upload Employee ID");
                        }
                        // Kiểm tra trùng lặp cho Division + Factory + Employee_ID
                        if (!string.IsNullOrWhiteSpace(division) && !string.IsNullOrWhiteSpace(factory))
                        {
                            CheckDuplicateParam param = new()
                            {
                                Division = division,
                                Factory = factory,
                                EmployeeID = employeeID,
                            };
                            var onCase2 = await CheckDuplicateCase2(param);
                            if (processedDivisionFactoryEmployeeIDs.Contains(divisionFactoryEmployeeIDKey) || onCase2)
                                errorMessage.Add("Division, Factory, and Employee ID cannot be repeated");
                            else
                                processedDivisionFactoryEmployeeIDs.Add(divisionFactoryEmployeeIDKey);
                        }
                    }

                    // 6. Department
                    if (string.IsNullOrWhiteSpace(department))
                        errorMessage.Add("Department is invalid");
                    else
                    {
                        if (!_repositoryAccessor.HRMS_Org_Department.Any(x => x.Division == division && x.Factory == factory && x.Department_Code == department))
                            errorMessage.Add("Department is not existed");
                    }
                    // 7. Assigned/Supported Division
                    if (!string.IsNullOrWhiteSpace(assignedDivision) && !divisions.ContainsKey(assignedDivision))
                        errorMessage.Add("Assigned/Supported Division is invalid");

                    // 8.Assigned/Supported Factory

                    if (!string.IsNullOrWhiteSpace(assignedDivision) && !string.IsNullOrWhiteSpace(assignedFactory))
                    {
                        var factories = await Query_Factory_List(assignedDivision);
                        if (!factories.Contains(assignedFactory))
                            errorMessage.Add("Assigned/Supported Factory is not existed");
                        if (!roleFactories.Contains(assignedFactory))
                            errorMessage.Add("uploaded [Assigned/Supported Factory] data does not match the role group");
                    }

                    // 9. Assigned/Supported Employee ID
                    if (!string.IsNullOrWhiteSpace(assignedEmployeeID))
                    {
                        if (assignedEmployeeID.Length > 16)
                            errorMessage.Add("Assigned/Supported Employee ID is invalid");

                        if (!assignedEmployeeID.Contains('-'))
                            assignedEmployeeID = $"{assignedFactory}-{assignedEmployeeID}";
                        else
                        {
                            var parts = assignedEmployeeID.Split('-');
                            if (parts[0] != assignedFactory)
                                errorMessage.Add("Cannot find the corresponding factory with upload Assigned/Supported Employee ID");
                        }
                        // Kiểm tra trùng lặp cho Assigned/Supported Division + Assigned/Supported Factory + Assigned/Supported Employee ID
                        if (!string.IsNullOrWhiteSpace(assignedDivision) && !string.IsNullOrWhiteSpace(assignedFactory))
                        {
                            CheckDuplicateParam param = new()
                            {
                                Division = assignedDivision,
                                Factory = assignedFactory,
                                EmployeeID = assignedEmployeeID,
                            };
                            var onCase3 = await CheckDuplicateCase3(param);
                            if (processedAssignedDivisionFactoryEmployeeIDs.Contains(assignedDivisionFactoryEmployeeIDKey) || onCase3)
                                errorMessage.Add("Assigned/Supported Division, Assigned/Supported Factory, and Assigned/Supported Employee ID cannot be repeated");
                            else
                                processedAssignedDivisionFactoryEmployeeIDs.Add(assignedDivisionFactoryEmployeeIDKey);
                        }
                    }

                    // 10. Assigned/Supported Department
                    if (!string.IsNullOrWhiteSpace(assignedDepartment) && !string.IsNullOrWhiteSpace(assignedDivision) && !string.IsNullOrWhiteSpace(assignedFactory))
                    {
                        if (!_repositoryAccessor.HRMS_Org_Department.Any(x => x.Division == assignedDivision && x.Factory == assignedFactory && x.Department_Code == assignedDepartment))
                            errorMessage.Add("Assigned/Supported Department is invalid");
                    }

                    // 11. Permission Group
                    if (string.IsNullOrWhiteSpace(permissionGroup) || !permissions.ContainsKey(permissionGroup))
                        errorMessage.Add("Permission Group is invalid");

                    // 12. Cross Factory Status
                    if (!string.IsNullOrWhiteSpace(crossFactoryStatus) && !allowEmployeeStatus.Contains(crossFactoryStatus))
                        errorMessage.Add("Cross Factory Status is invalid");

                    // 13. Performance Assessment Responsibility Division
                    if (!string.IsNullOrWhiteSpace(performanceARDivision) && !divisions.ContainsKey(performanceARDivision))
                        errorMessage.Add("Performance Assessment Responsibility Division is invalid");

                    // 14. Identity Type
                    if (string.IsNullOrWhiteSpace(identityType) || !identities.ContainsKey(identityType))
                        errorMessage.Add("Identity Type is invalid");

                    // 15. Local Full Name
                    if (string.IsNullOrWhiteSpace(localFullName) || localFullName.Length > 100)
                        errorMessage.Add("Local Full Name is invalid");

                    // 16. Preferred English Full Name
                    if (!string.IsNullOrWhiteSpace(preferredEnglishFullName) && preferredEnglishFullName.Length > 100)
                        errorMessage.Add("Preferred English Full Name is invalid");

                    // 17. Chinese Name
                    if (!string.IsNullOrWhiteSpace(chineseName) && chineseName.Length > 50)
                        errorMessage.Add("Chinese Name is invalid");

                    // 18. Gender
                    if (string.IsNullOrWhiteSpace(gender))
                        errorMessage.Add("Gender is invalid");

                    if (!allowGenders.Contains(gender))
                        errorMessage.Add("Gender must be 'F' or 'M'");

                    // 19. Blood Type
                    if (!string.IsNullOrWhiteSpace(bloodType) && !allowBloods.Contains(bloodType))
                        errorMessage.Add("Blood Type is invalid");

                    if (bloodType == "AB")
                        bloodType = "C";

                    // 20. Marital Status
                    if (!string.IsNullOrWhiteSpace(maritalStatus) && !allowMaritalStatus.Contains(maritalStatus))
                        errorMessage.Add("Marital Status must be 'M'/'U'/'O'");

                    // 21. Date of Birth
                    if (string.IsNullOrWhiteSpace(birthDay) || !regex.IsMatch(birthDay))
                        errorMessage.Add("Date of Birth is invalid. Expected format: YYYY/MM/DD");

                    // 22. Phone Number
                    if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length > 30)
                        errorMessage.Add("Phone Number is invalid");

                    // 23. Mobile Phone Number
                    if (!string.IsNullOrWhiteSpace(mobile) && mobile.Length > 30)
                        errorMessage.Add("Mobile Phone Number is invalid");

                    // 24. Education
                    if (string.IsNullOrWhiteSpace(education) || !educations.ContainsKey(education))
                        errorMessage.Add("Education is invalid");

                    // 25. Religion
                    if (!string.IsNullOrWhiteSpace(religion) && !religions.ContainsKey(religion))
                        errorMessage.Add("Religion is invalid");

                    // 26. Transportation Method
                    if (!string.IsNullOrWhiteSpace(transportationMethod) && !transportationMethods.ContainsKey(transportationMethod))
                        errorMessage.Add("Transportation Method is invalid");

                    // 27. Vehicle Type
                    if (!string.IsNullOrWhiteSpace(vehicleType) && !vehicleTypes.ContainsKey(vehicleType))
                        errorMessage.Add("Vehicle Type is invalid");

                    // 28. License Plate Number
                    if (!string.IsNullOrWhiteSpace(licensePlateNumber) && licensePlateNumber.Length > 20)
                        errorMessage.Add("License Plate Number is invalid");

                    // 29. Registered: Province/Directly
                    var checkProvince = _repositoryAccessor.HRMS_Basic_Code.Any(x => province.ContainsKey(registeredProvinceDirectly) && x.Char1 == national && x.Code == registeredProvinceDirectly);
                    if (string.IsNullOrWhiteSpace(registeredProvinceDirectly) || checkProvince == false)
                        errorMessage.Add("Registered: Province/Directly is invalid");

                    // 30. Registered：City/District/County
                    var checkCity = _repositoryAccessor.HRMS_Basic_Code.Any(x => city.ContainsKey(registeredCity) && x.Char1 == registeredProvinceDirectly && x.Code == registeredCity);
                    if (string.IsNullOrWhiteSpace(registeredCity) || checkCity == false)
                        errorMessage.Add("Registered: City/District/County is invalid");

                    // 31. Registered Address
                    if (string.IsNullOrWhiteSpace(registeredAddress) || registeredAddress.Length > 255)
                        errorMessage.Add("Registered Address is invalid");

                    // 32. Mailing: Province/Directly
                    if (!string.IsNullOrWhiteSpace(mailingProvinceDirectly))
                    {
                        if (_repositoryAccessor.HRMS_Basic_Code.Any(x => province.ContainsKey(mailingProvinceDirectly) && x.Char1 == national && x.Code == mailingProvinceDirectly))
                            errorMessage.Add("Mailing: Province/Directly is invalid");
                    }

                    // 33. Mailing：City/District/County
                    if (!string.IsNullOrWhiteSpace(mailingCity))
                    {
                        if (_repositoryAccessor.HRMS_Basic_Code.Any(x => city.ContainsKey(mailingCity) && x.Char1 == mailingProvinceDirectly && x.Code == mailingCity))
                            errorMessage.Add("Mailing: City/District/County is invalid");
                    }

                    // 34. Mailing Address
                    if (!string.IsNullOrWhiteSpace(mailingAddress) && mailingAddress.Length > 255)
                        errorMessage.Add("Mailing Address is invalid");

                    // 35. Work Shift Type
                    if (string.IsNullOrWhiteSpace(workShiftType) || !workShiftTypes.ContainsKey(workShiftType))
                        errorMessage.Add("Work Shift Type is invalid");

                    // 36. Swipe Card Option
                    if (string.IsNullOrWhiteSpace(swipeCardOption))
                        errorMessage.Add("Swipe Card Option is invalid");
                    else
                    {
                        if (!allowYesNo.Contains(swipeCardOption))
                            errorMessage.Add("Swipe Card Option must be 'Y' or 'N'");
                    }
                    // 37. Swipe Card Number
                    if (string.IsNullOrWhiteSpace(swipeCardNumber) || swipeCardNumber.Length > 20)
                        errorMessage.Add("Swipe Card Number is invalid");

                    // 38. Position Grade && 39. Position Title
                    if (string.IsNullOrWhiteSpace(positionGrade) || !positionGrade.CheckDecimalValue(4, 1))
                        errorMessage.Add("Position Grade is invalid");
                    else
                    {
                        var LevelData = allowLevels.FindAll(x => x.Level == Convert.ToDecimal(positionGrade));
                        if (!LevelData.Any())
                            errorMessage.Add("Position Grade is not existed");
                        if (!string.IsNullOrWhiteSpace(positionTitle) && !LevelData.Any(x => x.Level_Code == positionTitle))
                            errorMessage.Add("Position Title is not existed");
                    }
                    if (string.IsNullOrWhiteSpace(positionTitle))
                        errorMessage.Add("Position Title is invalid");

                    // 40. Work Type/Job
                    if (string.IsNullOrWhiteSpace(workType) || !workTypes.ContainsKey(workType))
                        errorMessage.Add("Work Type/Job is invalid");

                    // 41. Restaurant
                    if (!string.IsNullOrWhiteSpace(restaurant) && !restaurants.ContainsKey(restaurant))
                        errorMessage.Add("Restaurant is invalid");

                    // 42. Work Location
                    if (!string.IsNullOrWhiteSpace(workLocation) && !workLocate.ContainsKey(workLocation))
                        errorMessage.Add("Work Location is invalid");

                    // 43. Union Membership
                    if (!string.IsNullOrWhiteSpace(unionMembership) && !allowYesNo.Contains(unionMembership))
                        errorMessage.Add("Union Membership must be 'Y' or 'N'");

                    // 44. Onboard Date
                    if (string.IsNullOrWhiteSpace(onboardDate) || !regex.IsMatch(onboardDate))
                        errorMessage.Add("Onboard Date is invalid. Expected format: YYYY/MM/DD");

                    // 45. Date of Group Employment
                    if (string.IsNullOrWhiteSpace(groupEmploymentDate) || !regex.IsMatch(groupEmploymentDate))
                        errorMessage.Add("Date of Group Employment is invalid. Expected format: YYYY/MM/DD");

                    // 46. Seniority Start Date
                    if (string.IsNullOrWhiteSpace(seniorityStartDate) || !regex.IsMatch(seniorityStartDate))
                        errorMessage.Add("Seniority Start Date is invalid. Expected format: YYYY/MM/DD");

                    // 47. Annual Leave Seniority Start Date
                    if (string.IsNullOrWhiteSpace(annualLeaveSeniorityStartDate) || !regex.IsMatch(annualLeaveSeniorityStartDate))
                        errorMessage.Add("Annual Leave Seniority Start Date is invalid. Expected format: YYYY/MM/DD");


                    if (!errorMessage.Any())
                    {
                        var user_GUID = Guid.NewGuid().ToString();
                        while (await _repositoryAccessor.HRMS_Emp_Personal.AnyAsync(x => x.USER_GUID == user_GUID))
                        {
                            user_GUID = Guid.NewGuid().ToString();
                        }
                        var empPersonalData = new HRMS_Emp_Personal
                        {
                            Nationality = national,
                            Identification_Number = identification_Number,
                            Issued_Date = Convert.ToDateTime(issued_Date),
                            Division = division,
                            Factory = factory,
                            Employee_ID = employeeID,
                            Department = department,
                            Assigned_Division = assignedDivision,
                            Assigned_Factory = assignedFactory,
                            Assigned_Employee_ID = assignedEmployeeID,
                            Assigned_Department = assignedDepartment,
                            Permission_Group = permissionGroup,
                            Employment_Status = crossFactoryStatus,
                            Performance_Division = performanceARDivision,
                            Identity_Type = identityType,
                            Local_Full_Name = localFullName,
                            Preferred_English_Full_Name = preferredEnglishFullName,
                            Chinese_Name = chineseName,
                            Gender = gender,
                            Blood_Type = bloodType,
                            Marital_Status = maritalStatus,
                            Birthday = Convert.ToDateTime(birthDay),
                            Phone_Number = phoneNumber,
                            Mobile_Phone_Number = mobile,
                            Education = education,
                            Religion = religion,
                            Transportation_Method = transportationMethod,
                            Vehicle_Type = vehicleType,
                            License_Plate_Number = licensePlateNumber,
                            Registered_Province_Directly = registeredProvinceDirectly,
                            Registered_City = registeredCity,
                            Registered_Address = registeredAddress,
                            Mailing_Province_Directly = mailingProvinceDirectly,
                            Mailing_City = mailingCity,
                            Mailing_Address = mailingAddress,
                            Work_Shift_Type = workShiftType,
                            Swipe_Card_Option = swipeCardOption == "Y",
                            Swipe_Card_Number = swipeCardOption == "Y" ? swipeCardNumber : "NA",
                            Position_Grade = Convert.ToDecimal(positionGrade),
                            Position_Title = positionTitle,
                            Work_Type = workType,
                            Restaurant = restaurant,
                            Work_Location = workLocation,
                            Union_Membership = unionMembership != null ? unionMembership == "Y" : null,
                            Onboard_Date = Convert.ToDateTime(onboardDate),
                            Group_Date = Convert.ToDateTime(groupEmploymentDate),
                            Seniority_Start_Date = Convert.ToDateTime(seniorityStartDate),
                            Annual_Leave_Seniority_Start_Date = Convert.ToDateTime(annualLeaveSeniorityStartDate),
                            USER_GUID = user_GUID,
                            Company = "W",
                            Deletion_Code = "Y",
                            Work8hours = false,
                            Resign_Date = null,
                            Resign_Reason = null,
                            Blacklist = null,
                            Update_By = userName,
                            Update_Time = DateTime.Now
                        };
                        empPersonalDatas.Add(empPersonalData);

                        if (!await _repositoryAccessor.HRMS_Emp_Identity_Card_History.AnyAsync(x =>
                            x.Nationality_After == empPersonalData.Nationality &&
                            x.Identification_Number_After == empPersonalData.Identification_Number &&
                            x.Issued_Date_After == empPersonalData.Issued_Date))
                        {
                            var cardHistory_GUID = Guid.NewGuid().ToString();
                            while (await _repositoryAccessor.HRMS_Emp_Identity_Card_History.AnyAsync(x => x.History_GUID == cardHistory_GUID || x.USER_GUID == cardHistory_GUID))
                            {
                                cardHistory_GUID = Guid.NewGuid().ToString();
                            }
                            HRMS_Emp_Identity_Card_History cardHistoryData = new()
                            {
                                History_GUID = cardHistory_GUID,
                                USER_GUID = empPersonalData.USER_GUID,
                                Nationality_Before = empPersonalData.Nationality,
                                Identification_Number_Before = empPersonalData.Identification_Number,
                                Issued_Date_Before = empPersonalData.Issued_Date,
                                Nationality_After = empPersonalData.Nationality,
                                Identification_Number_After = empPersonalData.Identification_Number,
                                Issued_Date_After = empPersonalData.Issued_Date,
                                Update_By = userName,
                                Update_Time = DateTime.Now,
                            };
                            cardHistoryDatas.Add(cardHistoryData);
                        }
                        var empIDHistory_GUID = Guid.NewGuid().ToString();
                        while (await _repositoryAccessor.HRMS_Emp_IDcard_EmpID_History.AnyAsync(x => x.History_GUID == empIDHistory_GUID || x.USER_GUID == empIDHistory_GUID))
                        {
                            empIDHistory_GUID = Guid.NewGuid().ToString();
                        }
                        HRMS_Emp_IDcard_EmpID_History empIDHistoryData = new()
                        {
                            History_GUID = empIDHistory_GUID,
                            USER_GUID = empPersonalData.USER_GUID,
                            Nationality = empPersonalData.Nationality,
                            Identification_Number = empPersonalData.Identification_Number,
                            Division = empPersonalData.Division,
                            Factory = empPersonalData.Factory,
                            Employee_ID = empPersonalData.Employee_ID,
                            Department = empPersonalData.Department,
                            Assigned_Division = empPersonalData.Assigned_Division,
                            Assigned_Factory = empPersonalData.Assigned_Factory,
                            Assigned_Employee_ID = empPersonalData.Assigned_Employee_ID,
                            Assigned_Department = empPersonalData.Assigned_Department,
                            Onboard_Date = empPersonalData.Onboard_Date,
                            Resign_Date = empPersonalData.Resign_Date,
                            Update_By = userName,
                            Update_Time = DateTime.Now,
                        };
                        empIDHistoryDatas.Add(empIDHistoryData);
                    }
                    else
                    {
                        EmployeeBasicInformationMaintenanceReport report = new()
                        {
                            Nationality = national,
                            IdentificationNumber = identification_Number,
                            IssuedDate = issued_Date,
                            Division = division,
                            Factory = factory,
                            EmployeeID = employeeID,
                            Department = department,
                            AssignedDivision = assignedDivision,
                            AssignedFactory = assignedFactory,
                            AssignedEmployeeID = assignedEmployeeID,
                            AssignedDepartment = assignedDepartment,
                            PermissionGroup = permissionGroup,
                            CrossFactoryStatus = crossFactoryStatus,
                            PerformanceAssessmentResponsibilityDivision = performanceARDivision,
                            IdentityType = identityType,
                            LocalFullName = localFullName,
                            PreferredEnglishFullName = preferredEnglishFullName,
                            ChineseName = chineseName,
                            Gender = gender,
                            BloodType = bloodType,
                            MaritalStatus = maritalStatus,
                            DateOfBirth = birthDay,
                            PhoneNumber = phoneNumber,
                            MobilePhoneNumber = mobile,
                            Education = education,
                            Religion = religion,
                            TransportationMethod = transportationMethod,
                            VehicleType = vehicleType,
                            LicensePlateNumber = licensePlateNumber,
                            RegisteredProvinceDirectly = registeredProvinceDirectly,
                            RegisteredCity = registeredCity,
                            RegisteredAddress = registeredAddress,
                            MailingProvinceDirectly = mailingProvinceDirectly,
                            MailingCity = mailingCity,
                            MailingAddress = mailingAddress,
                            WorkShiftType = workShiftType,
                            SwipeCardOption = swipeCardOption,
                            SwipeCardNumber = swipeCardNumber,
                            PositionGrade = positionGrade,
                            PositionTitle = positionTitle,
                            WorkType = workType,
                            Restaurant = restaurant,
                            WorkLocation = workLocation,
                            UnionMembership = unionMembership,
                            OnboardDate = onboardDate,
                            DateOfGroupEmployment = groupEmploymentDate,
                            SeniorityStartDate = seniorityStartDate,
                            AnnualLeaveSeniorityStartDate = annualLeaveSeniorityStartDate,
                            Error_Message = string.Join("\r\n", errorMessage)
                        };
                        excelReportList.Add(report);
                    }
                }

                EmployeeBasicInformationMaintenance_UploadResult resultDto = new()
                {
                    Total = resp.Ws.Cells.Rows.Count - resp.WsTemp.Cells.Rows.Count,
                    Success = empPersonalDatas.Count,
                    Error = excelReportList.Count
                };

                if (empPersonalDatas.Any())
                {
                    if (cardHistoryDatas.Any())
                        _repositoryAccessor.HRMS_Emp_Identity_Card_History.AddMultiple(cardHistoryDatas);
                    if (empIDHistoryDatas.Any())
                        _repositoryAccessor.HRMS_Emp_IDcard_EmpID_History.AddMultiple(empIDHistoryDatas);
                    _repositoryAccessor.HRMS_Emp_Personal.AddMultiple(empPersonalDatas);
                    await _repositoryAccessor.Save();
                    string folder = "uploaded\\excels\\EmployeeMaintenance\\4_1_1_EmployeeBasicInformationMaintenance\\Creates";
                    await FilesUtility.SaveFile(file, folder, $"EmployeeBasicInformationMaintenance_{DateTime.Now:yyyyMMddHHmmss}");
                }

                if (excelReportList.Any())
                {
                    string fileLocation = Path.Combine(
                        Directory.GetCurrentDirectory(), 
                        "Resources\\Template\\EmployeeMaintenance\\4_1_1_EmployeeBasicInformationMaintenance\\Report.xlsx"
                    );
                    var excel = ExcelUtility.DownloadExcel(excelReportList, fileLocation, new ConfigDownload(true, SaveFormat.Xlsx));
                    resultDto.ErrorReport = excel.Result;
                    await _repositoryAccessor.CommitAsync();
                    return new OperationResult { IsSuccess = false, Data = resultDto, Error = "Please check Error Report" };
                }
                await _repositoryAccessor.CommitAsync();
                return new OperationResult { IsSuccess = true, Data = resultDto };
            }
            catch (Exception ex)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, ex.Message);
            }
        }

        private async Task<Dictionary<string, Dictionary<string, HRMS_Basic_Code>>> GetAllBasicCodes(List<string> typeSeqs)
        {
            var codes = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => typeSeqs.Contains(x.Type_Seq))
                .ToListAsync();

            return codes.GroupBy(x => x.Type_Seq)
                .ToDictionary(x => x.Key, x => x.ToDictionary(c => c.Code));
        }
        #endregion

        public async Task<PaginationUtility<EmployeeBasicInformationMaintenanceView>> GetPagination(PaginationParam pagination, EmployeeBasicInformationMaintenanceParam param, List<string> roleList)
        {
            #region PredicateBuilder
            var pred = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Nationality == param.Nationality);
            var predEmp_Unpaid_Leave = PredicateBuilder.New<HRMS_Emp_Unpaid_Leave>(x => x.Effective_Status);

            if (!string.IsNullOrWhiteSpace(param.IdentificationNumber))
                pred.And(x => x.Identification_Number.Contains(param.IdentificationNumber));

            if (!string.IsNullOrWhiteSpace(param.Division))
            {
                pred.And(x => x.Division == param.Division);
                predEmp_Unpaid_Leave.And(x => x.Division == param.Division);
            }

            if (!string.IsNullOrWhiteSpace(param.Factory))
            {
                pred.And(x => x.Factory == param.Factory);
                predEmp_Unpaid_Leave.And(x => x.Factory == param.Factory);
            }

            if (!string.IsNullOrWhiteSpace(param.EmployeeID))
            {
                pred.And(x => x.Employee_ID.Contains(param.EmployeeID));
                predEmp_Unpaid_Leave.And(x => x.Employee_ID.Contains(param.EmployeeID));
            }

            if (!string.IsNullOrWhiteSpace(param.Department))
                pred.And(x => x.Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.AssignedDivision))
                pred.And(x => x.Assigned_Division == param.AssignedDivision);

            if (!string.IsNullOrWhiteSpace(param.AssignedFactory))
                pred.And(x => x.Assigned_Factory == param.AssignedFactory);

            if (!string.IsNullOrWhiteSpace(param.AssignedEmployeeID))
                pred.And(x => x.Assigned_Employee_ID.Contains(param.AssignedEmployeeID));

            if (!string.IsNullOrWhiteSpace(param.AssignedDepartment))
                pred.And(x => x.Assigned_Department == param.AssignedDepartment);

            if (!string.IsNullOrWhiteSpace(param.CrossFactoryStatus))
                pred.And(x => x.Employment_Status == param.CrossFactoryStatus);

            if (!string.IsNullOrWhiteSpace(param.PerformanceDivision))
                pred.And(x => x.Performance_Division == param.PerformanceDivision);

            if (!string.IsNullOrWhiteSpace(param.LocalFullName))
                pred.And(x => x.Local_Full_Name.Contains(param.LocalFullName));

            if (!string.IsNullOrWhiteSpace(param.OnboardDateStr))
                pred.And(x => x.Onboard_Date == Convert.ToDateTime(param.OnboardDateStr));

            if (!string.IsNullOrWhiteSpace(param.DateOfGroupEmploymentStr))
                pred.And(x => x.Group_Date == Convert.ToDateTime(param.DateOfGroupEmploymentStr));

            if (!string.IsNullOrWhiteSpace(param.SeniorityStartDateStr))
                pred.And(x => x.Seniority_Start_Date == Convert.ToDateTime(param.SeniorityStartDateStr));

            if (!string.IsNullOrWhiteSpace(param.AnnualLeaveSeniorityStartDateStr))
                pred.And(x => x.Annual_Leave_Seniority_Start_Date == Convert.ToDateTime(param.AnnualLeaveSeniorityStartDateStr));

            if (!string.IsNullOrWhiteSpace(param.DateOfResignationStr))
                pred.And(x => x.Resign_Date == Convert.ToDateTime(param.DateOfResignationStr));
            if (!string.IsNullOrWhiteSpace(param.WorkShiftType))
                pred.And(x => x.Work_Shift_Type == param.WorkShiftType);
            #endregion
            // get Role current
            var Eul = _repositoryAccessor.HRMS_Emp_Unpaid_Leave.FindAll(predEmp_Unpaid_Leave, true);

            var HEP = await Query_Permission_Data_Filter(roleList, pred);
            if (!string.IsNullOrWhiteSpace(param.EmploymentStatus))
            {
                var leaveUnpairs = await _repositoryAccessor.HRMS_Emp_Unpaid_Leave.FindAll(predEmp_Unpaid_Leave).Select(x => x.Employee_ID).ToListAsync();
                HEP = HEP.Where(x => x.Deletion_Code == "N"
                    ? x.Deletion_Code == param.EmploymentStatus
                    : leaveUnpairs.Contains(x.Employee_ID)
                        ? param.EmploymentStatus == "U"
                        : param.EmploymentStatus == "Y").ToList();
            }
            var org_Department = _repositoryAccessor.HRMS_Org_Department.FindAll(true).ToList();
            var org_Department_Language = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true).ToList();
            var emp_Exit_History = _repositoryAccessor.HRMS_Emp_Exit_History.FindAll(true).ToList();

            var result = HEP.Distinct()
                    .Join(org_Department,
                        HEP => new { HEP.Division, HEP.Factory, HEP.Department },
                        HOD => new { HOD.Division, HOD.Factory, Department = HOD.Department_Code },
                        (HEP, HOD) => new { HEP, HOD })
                    .GroupJoin(org_Department_Language,
                          last => new { last.HOD.Division, last.HOD.Factory, last.HOD.Department_Code },
                          HODL => new { HODL.Division, HODL.Factory, HODL.Department_Code },
                        (x, HODL) => new { x.HEP, x.HOD, HODL })
                    .SelectMany(x => x.HODL.DefaultIfEmpty(),
                        (x, HODL) => new { x.HEP, x.HOD, HODL })
                    .Select(x => new EmployeeBasicInformationMaintenanceView
                    {
                        USER_GUID = x.HEP.USER_GUID,
                        EmploymentStatus = param.EmploymentStatus == "U"
                                        ? x.HEP.Deletion_Code == "N"
                                            ? "N"
                                            : (x.HEP.Deletion_Code != "N" && Eul.Any(y => y.Division == x.HEP.Division && y.Factory == x.HEP.Factory && y.Employee_ID == x.HEP.Employee_ID))
                                                ? "U"
                                                : (x.HEP.Deletion_Code != "N" && !Eul.Any(y => y.Division == x.HEP.Division && y.Factory == x.HEP.Factory && y.Employee_ID == x.HEP.Employee_ID))
                                                    ? "Y" : ""

                                        : x.HEP.Deletion_Code,
                        Nationality = x.HEP.Nationality,
                        IdentificationNumber = x.HEP.Identification_Number,
                        LocalFullName = x.HEP.Local_Full_Name,
                        Division = x.HEP.Division,
                        Factory = x.HEP.Factory,
                        EmployeeID = x.HEP.Employee_ID,
                        Department = $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}",
                        CrossFactoryStatus = x.HEP.Employment_Status,
                        OnboardDate = x.HEP.Onboard_Date.ToString("yyyy/MM/dd"),
                        DateOfResignation = x.HEP.Resign_Date.HasValue ? x.HEP.Resign_Date.Value.ToString("yyyy/MM/dd") : "",
                        EnableRehire = x.HEP.Resign_Date.HasValue && !emp_Exit_History.Any(y => y.USER_GUID == x.HEP.USER_GUID
                                                                                             && y.Resign_Date == x.HEP.Resign_Date.Value)
                    }).Distinct().ToList();
            if (param.EmploymentStatus == "U")
                result = result.Where(x => x.EmploymentStatus == "U").ToList();
            return PaginationUtility<EmployeeBasicInformationMaintenanceView>.Create(result, pagination.PageNumber, pagination.PageSize);
        }


        public async Task<List<DepartmentSupervisorList>> GetDepartmentSupervisor(string USER_GUID, string Language)
        {
            var data = await _repositoryAccessor.HRMS_Emp_Personal
                .FirstOrDefaultAsync(x => x.USER_GUID == USER_GUID, true);

            if (data == null)
                return new();

            List<SupervisorType> supervisor = new()
            {
                new SupervisorType("A", "Formal", "EN"),
                new SupervisorType("B", "Deputy", "EN"),
                new SupervisorType("C", "Adjunction", "EN"),
                new SupervisorType("D", "Informal", "EN"),
                new SupervisorType("A", "正式", "TW"),
                new SupervisorType("B", "代理", "TW"),
                new SupervisorType("C", "兼任", "TW"),
                new SupervisorType("D", "非正式", "TW"),
            };

            var department = _repositoryAccessor.HRMS_Org_Department
                .FindAll(x => (x.Division == data.Division
                    && x.Factory == data.Factory
                    && x.Supervisor_Employee_ID == data.Employee_ID)
                    || (x.Division == data.Assigned_Division
                    && x.Factory == data.Assigned_Factory
                    && x.Supervisor_Employee_ID == data.Assigned_Employee_ID), true).ToList();

            var department_Language = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower(), true).ToList();

            var departmentSupervisor = department
                .GroupJoin(supervisor.Where(x => x.Lang == Language.ToUpper()),
                    HOD => new { Code = HOD.Supervisor_Type },
                    SUP => new { SUP.Code },
                    (x, y) => new { HOD = x, SUP = y })
                .SelectMany(x => x.SUP.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, SUP = y })
                .GroupJoin(department_Language,
                    x => new { x.HOD.Division, x.HOD.Factory, x.HOD.Department_Code },
                    HODL => new { HODL.Division, HODL.Factory, HODL.Department_Code },
                    (x, y) => new { x.HOD, x.SUP, HODL = y })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, x.SUP, HODL = y })
                .GroupBy(x => new { x.HOD.Supervisor_Type, Code_Name = x.SUP != null ? x.SUP.Code_Name : "" })
                .Select(x => new DepartmentSupervisorList
                {
                    SupervisorType = $"{x.Key.Supervisor_Type}.{x.Key.Code_Name}",
                    DepartmentSupervisor = string.Join(", ", x.Select(y => $"{y.HOD.Factory}-{y.HOD.Department_Code}-{(y.HODL != null ? y.HODL.Name : y.HOD.Department_Name)}").ToList())
                })
                .ToList();

            return departmentSupervisor;
        }

        public async Task<EmployeeBasicInformationMaintenanceDto> GetDetail(string USER_GUID)
        {
            var data = await _repositoryAccessor.HRMS_Emp_Personal
                .FirstOrDefaultAsync(x => x.USER_GUID == USER_GUID, true);

            if (data == null)
                return new();

            var passportFullName = await _repositoryAccessor.HRMS_Emp_Document
                .FindAll(x => x.Division == data.Division
                    && x.Factory == data.Factory
                    && x.Employee_ID == data.Employee_ID)
                .Select(x => new
                {
                    x.Validity_Start,
                    x.Passport_Name
                })
                .OrderByDescending(x => x.Validity_Start)
                .FirstOrDefaultAsync();

            var numberOfDependent = await _repositoryAccessor.HRMS_Emp_Dependent.CountAsync(x => x.USER_GUID == USER_GUID && x.Dependents == true);

            var account = await _repositoryAccessor.HRMS_Basic_Account
                .FirstOrDefaultAsync(x => x.Account == data.Update_By);

            EmployeeBasicInformationMaintenanceDto result = new()
            {
                USER_GUID = data.USER_GUID,
                Nationality = data.Nationality,
                IdentificationNumber = data.Identification_Number,
                IssuedDate = data.Issued_Date,
                Company = data.Company,
                EmploymentStatus = data.Deletion_Code,
                Division = data.Division,
                Factory = data.Factory,
                EmployeeID = data.Employee_ID,
                Department = data.Department,
                AssignedDivision = data.Assigned_Division,
                AssignedFactory = data.Assigned_Factory,
                AssignedEmployeeID = data.Assigned_Employee_ID,
                AssignedDepartment = data.Assigned_Department,
                PermissionGroup = data.Permission_Group,
                CrossFactoryStatus = data.Employment_Status,
                PerformanceAssessmentResponsibilityDivision = data.Performance_Division,
                IdentityType = data.Identity_Type,
                LocalFullName = data.Local_Full_Name,
                PreferredEnglishFullName = data.Preferred_English_Full_Name,
                ChineseName = data.Chinese_Name,
                PassportFullName = passportFullName?.Passport_Name ?? "",
                Gender = data.Gender,
                BloodType = data.Blood_Type,
                MaritalStatus = data.Marital_Status,
                DateOfBirth = data.Birthday,
                PhoneNumber = data.Phone_Number,
                MobilePhoneNumber = data.Mobile_Phone_Number,
                Education = data.Education,
                Religion = data.Religion,
                TransportationMethod = data.Transportation_Method,
                VehicleType = data.Vehicle_Type,
                LicensePlateNumber = data.License_Plate_Number,
                NumberOfDependents = numberOfDependent,
                RegisteredProvinceDirectly = data.Registered_Province_Directly,
                RegisteredCity = data.Registered_City,
                RegisteredAddress = data.Registered_Address,
                MailingProvinceDirectly = data.Mailing_Province_Directly,
                MailingCity = data.Mailing_City,
                MailingAddress = data.Mailing_Address,
                WorkShiftType = data.Work_Shift_Type,
                SwipeCardOption = data.Swipe_Card_Option,
                SwipeCardNumber = data.Swipe_Card_Number,
                PositionGrade = data.Position_Grade,
                PositionTitle = data.Position_Title,
                WorkType = data.Work_Type,
                Restaurant = data.Restaurant,
                // SalaryType
                // SalaryPaymentMethod
                WorkLocation = data.Work_Location,
                Work8hours = data.Work8hours,
                UnionMembership = data.Union_Membership,
                OnboardDate = data.Onboard_Date,
                DateOfGroupEmployment = data.Group_Date,
                AnnualLeaveSeniorityStartDate = data.Annual_Leave_Seniority_Start_Date,
                SeniorityStartDate = data.Seniority_Start_Date,
                DateOfResignation = data.Resign_Date,
                ReasonResignation = data.Resign_Reason,
                Blacklist = data.Blacklist,
                UpdateTime = data.Update_Time,
                UpdateBy = account?.Name,
            };
            return result;
        }

        #region Get List
        public async Task<List<KeyValuePair<string, string>>> GetListDivision(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.Division, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string Division, string Language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory, true)
                .Join(_repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(x => x.Kind == "1" && x.Division == Division, true),
                    x => new { Factory = x.Code },
                    y => new { y.Factory },
                    (x, y) => new { HBC = x, HBFC = y })
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower(), true),
                    x => new { x.HBC.Type_Seq, x.HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (x, y) => new { x.HBC, x.HBFC, HBCL = y })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, x.HBFC, HBCL = y })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
        }
        public async Task<List<KeyValuePair<string, string>>> GetListNationality(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.Nationality, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListPermission(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.PermissionGroup, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListIdentityType(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.IdentityType, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListEducation(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.Education, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListWorkType(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.WorkType, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListRestaurant(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.Restaurant, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListReligion(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.Religion, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListTransportationMethod(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.TransportationMethod, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListVehicleType(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.VehicleType, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListWorkLocation(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.WorkLocation, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListReasonResignation(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.ReasonResignation, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListWorkTypeShift(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.WorkShiftType, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string Division, string Factory, string Language)
        {
            return await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Division == Division && x.Factory == Factory, true)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower(), true),
                      HOD => new { HOD.Division, HOD.Factory, HOD.Department_Code },
                      HODL => new { HODL.Division, HODL.Factory, HODL.Department_Code },
                    (HOD, HODL) => new { HOD, HODL })
                    .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (prev, HODL) => new { prev.HOD, HODL })
                .Select(x => new KeyValuePair<string, string>(x.HOD.Department_Code, $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}"))
                .ToListAsync();
        }
        public async Task<List<KeyValuePair<string, string>>> GetListProvinceDirectly(string char1, string Language)
        {
            return await GetHRMS_Basic_Code_Char1(BasicCodeTypeConstant.Province, char1, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListCity(string char1, string Language)
        {
            return await GetHRMS_Basic_Code_Char1(BasicCodeTypeConstant.City, char1, Language);
        }
        public async Task<List<KeyValuePair<decimal, string>>> GetPositionGrade()
        {
            return await _repositoryAccessor.HRMS_Basic_Level.FindAll(true)
                .Select(x => new KeyValuePair<decimal, string>(x.Level, $"{x.Level}"))
                .Distinct()
                .ToListAsync();
        }
        public async Task<List<KeyValuePair<string, string>>> GetPositionTitle(decimal level, string Language)
        {
            return await _repositoryAccessor.HRMS_Basic_Level.FindAll(x => x.Level == level, true)
                .Join(_repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.JobTitle, true),
                    HBL => HBL.Level_Code,
                    HBC => HBC.Code,
                    (HBL, HBC) => new { HBL, HBC })
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower(), true),
                    prev => new { prev.HBC.Type_Seq, prev.HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (prev, HBCL) => new { prev.HBL, prev.HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBL, prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBL.Level_Code, $"{x.HBL.Level_Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .Distinct()
                .ToListAsync();
        }
        private async Task<List<KeyValuePair<string, string>>> GetHRMS_Basic_Code(string Type_Seq, string Language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == Type_Seq, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
        }
        private async Task<List<KeyValuePair<string, string>>> GetHRMS_Basic_Code_Char1(string Type_Seq, string char1, string Language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == Type_Seq && x.Char1.ToLower() == char1.ToLower(), true)
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
    }
}