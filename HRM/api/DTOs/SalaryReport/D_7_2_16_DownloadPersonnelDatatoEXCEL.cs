using API.Models;

namespace API.DTOs.SalaryReport
{
    public class D_7_2_16_DownloadPersonnelDatatoEXCELParam
    {
        public string Factory { get; set; }
        public string FactoryCodeName { get; set; }

        public string YearMonth { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string EmployeeKind { get; set; }
        public string ReportKind { get; set; }
        public List<string> PermissionGroup { get; set; }
        public IEnumerable<string> PermissionGroupCodeName { get; set; }
        public  List<BasicCode> additionsAndDeductionsItem { get; set; }

        public string UserName { get; set; }
        public string Language { get; set; }
    }
    public class EmployeeMasterFile
    {
        public DateTime IssuedDate { get; set; }
        public string IdentificationNumber { get; set; }
        public string EmploymentStatus { get; set; }
        public string Division { get; set; }
        public string Factory { get; set; }
        public string EmployeeID { get; set; }
        public string Department { get; set; }
        public string DepartmentName { get; set; }
        public string LocalFullName { get; set; }
        public string PreferredEnglishFullName { get; set; }
        public string ChineseName { get; set; }
        public string PassportFullName { get; set; }
        public string Assigned_SupportedDivision { get; set; }
        public string Assigned_SupportedFactory { get; set; }
        public string Assigned_SupportedEmployeeID { get; set; }
        public string Assigned_SupportedDepartment { get; set; }
        public string Assigned_SupportedDepartmentName { get; set; }
        public string CrossFactoryStatus { get; set; }
        public string PermissionGroup { get; set; }
        public string PerformanceAssessmentResponsibilityDivision { get; set; }
        public string IdentityType { get; set; }
        public string Gender { get; set; }
        public string MaritalStatus { get; set; }
        public DateTime DateofBirth { get; set; }
        public string WorkShiftType { get; set; }
        public string PregnancyWorkfor8hours { get; set; }
        public string SalaryType { get; set; }
        public string SwipeCardOption { get; set; }
        public string SwipeCardNumber { get; set; }
        public decimal PositionGrade { get; set; }
        public string PositionTitle { get; set; }
        public string WorkType_Job { get; set; }
        public DateTime OnboardDate { get; set; }
        public DateTime DateofGoupEmployment { get; set; }
        public DateTime SeniorityStartDate { get; set; }
        public DateTime AnnualLeaveSeniorityStartDate { get; set; }
        public DateTime? DateofResignation { get; set; }
        public string ReasonforResignation { get; set; }
        public string Blacklist { get; set; }
        public string Restaurant { get; set; }
        public string WorkLocation { get; set; }
        public bool? UnionMembership { get; set; }
        public string Education { get; set; }
        public string Religion { get; set; }
        public string TransportationMethod { get; set; }
        public string PhoneNumber { get; set; }
        public string MobilePhoneNumber { get; set; }
        public string Registered_ProvinceDirectlyGovernedCity { get; set; } // Registered：Province/Directly Governed City/Prefecture-level City/County
        public string Registere_CityDistrictCounty { get; set; }
        public string RegisteredAddress { get; set; }
        public string Mailin_ProvinceDirectlyGovernedCity { get; set; } // Mailing：Province/Directly Governed City/Prefecture-level City/County
        public string Mailin_CityDistrictCounty { get; set; } // Mailing：City/District/County
        public string MailingAddress { get; set; }
        public DateTime? ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }
        public DateTime? HealthInsuranceDateStart { get; set; }
        public DateTime? HealthInsuranceDateEnd { get; set; }
        public string SocialInsuranceNumber { get; set; }
        public string HealthInsuranceNumber { get; set; }
        public string EmergencyContact { get; set; }
        public string EmergencyTemporaryAddressPhone { get; set; }
        public string TemporaryAddress { get; set; }
        public string EmergencyContactAddress { get; set; }
        public string UpdateBy { get; set; }
        public DateTime ModificationDate { get; set; }
    }
    public class SalaryMasterFile
    {
        public string EmploymentStatus { get; set; }
        public string Factory { get; set; }
        public string EmployeeID { get; set; }
        public string LocalFullName { get; set; }
        public string Department { get; set; }
        public string DepartmentName { get; set; }
        public decimal? PositionGrade { get; set; }
        public string PositionTitle { get; set; }
        public DateTime? PeriodofActingPosition_Start { get; set; }
        public DateTime? PeriodofActingPosition_End { get; set; }
        public string TechnicalType { get; set; }
        public string ExpertiseCategory { get; set; }
        public DateTime? OnboardDate { get; set; }
        public DateTime? DateofResignation { get; set; }
        public string ReasonforResignation { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string PermissionGroup { get; set; }
        public string SalaryType { get; set; }
        public decimal? SalaryLevelGrade { get; set; }
        public decimal? SalaryLevelLevel { get; set; }
        public string Currency { get; set; }
        public string Gender { get; set; }
        public string IdentificationNumber { get; set; }
        public DateTime? DateofBirth { get; set; }
        public string PerformanceCategory { get; set; }
        public DateTime? ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }
        public DateTime? AnnualLeaveSeniorityStartDate { get; set; }
        public string CompulsoryInsuranceNumber { get; set; }
        public string HealthInsuranceNumber { get; set; }
        public string UpdateBy { get; set; }
        public DateTime? UpdateTime { get; set; }
        public List<SalaryItem> salaryItem { get; set; }

    }
    public class MonthlyAttendance
    {
        public DateTime? YearMonth { get; set; }
        public string Department { get; set; }
        public string DepartmentName { get; set; }
        public string EmployeeID { get; set; }
        public string LocalFullName { get; set; }
        public string NewHiredResigned { get; set; }
        public string PermissionGroup { get; set; }
        public string SalaryType { get; set; }
        public decimal? PaidSalaryDays { get; set; }
        public decimal? ActualWorkDays { get; set; }
        public int? DelayEarlyTimes { get; set; }
        public int? NoSwipCardTimes { get; set; }
        public int? DayShiftMealTimes { get; set; }
        public int? OvertimeMealTimes { get; set; }
        public int? NightShiftAllowanceTimes { get; set; }
        public int? NightShiftMealTimes { get; set; }
        public DateTime? OnboardDate { get; set; }
        public DateTime? AnnualLeaveSeniorityStartDate { get; set; }
        public DateTime? DateofResignation { get; set; }
        public string WorkTypeJob { get; set; }
        public bool? UnionMembership { get; set; }
        public DateTime? ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }
        public DateTime? HealthInsuranceDateStart { get; set; }
        public DateTime? HealthInsuranceDateEnd { get; set; }
        public string UpdateBy { get; set; }
        public DateTime? UpdateTime { get; set; }
        public List<SalaryItem> ItemsLeaveType1 { get; set; }
        public List<SalaryItem> ItemsLeaveType2 { get; set; }

    }
    public class MonthlySalary
    {
        public DateTime? YearMonth { get; set; }
        public string Department { get; set; }
        public string DepartmentName { get; set; }
        public string EmployeeID { get; set; }
        public string LocalFullName { get; set; }
        public string PositionTitle { get; set; }
        public string WorkTypeJob { get; set; }
        public DateTime? OnboardDate { get; set; }
        public DateTime? AnnualLeaveSeniorityStartDate { get; set; }
        public DateTime? DateofResignation { get; set; }
        public decimal? ActualWorkDays { get; set; }
        public string PermissionGroup { get; set; }
        public string Transfer { get; set; }
        public string Currency { get; set; }
        public List<SalaryItem> SalaryItem { get; set; }
        public List<SalaryItem> Allowance { get; set; }
        public int SocialHealthUnemploymentInsurance { get; set; }
        public int AdditionItem { get; set; }
        public decimal TotalAdditionItem { get; set; }
        public decimal LoanedAmount { get; set; }
        public List<SalaryItem> InsuranceDeduction { get; set; }
        public decimal UnionFee { get; set; }
        public int? Tax { get; set; }
        public int OtherDeduction { get; set; }
        public decimal TotalDeductionItem { get; set; }
        public decimal NetAmountReceived { get; set; }
    }

    public class SalaryItem
    {
        public string Code { get; set; }
        public string CodeName_EN { get; set; }

        public string CodeName_TW { get; set; }
        public int? Value { get; set; }
        public decimal? Days { get; set; }

        public int? Seq { get; set; }
    }
    public class BasicCode
    {
        public string Language_Code { get; set; }
        public string Type_Seq { get; set; }
        public string Code { get; set; }
        public string CodeName { get; set; }
    }
    public class TabelGetData
    {
        public HRMS_Emp_Personal HEP { get; set; }
        public HRMS_Att_Monthly HAM { get; set; }
        public HRMS_Sal_Monthly HSM { get; set; }

    }
    public class D_7_2_16_Att_Monthly_Detail_Temp
    {
        public string USER_GUID { get; set; }
        public string Division { get; set; }
        public string Factory { get; set; }
        public DateTime Att_Month { get; set; }
        public string Employee_ID { get; set; }
        public string Leave_Type { get; set; }
        public string Leave_Code { get; set; }
        public decimal Days { get; set; }
        public string Update_By { get; set; }
        public DateTime Update_Time { get; set; }
    }
    public class AttMonthlyDetailValues
    {
        public string Leave_Code { get; set; }
        public string Leave_Code_Name { get; set; }
        public decimal Days { get; set; }
    }
    public class Sal_Monthly_Detail_Values
    {
        public int Seq { get; set; }
        public string Employee_ID { get; set; }
        public string Permission_Group { get; set; }
        public string Salary_Type { get; set; }
        public string Item { get; set; }
        public int Amount { get; set; }
        public string Code { get; set; }
    }
}