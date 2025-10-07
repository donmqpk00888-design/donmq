
using API.Models;

namespace API.DTOs.SalaryReport
{
    public class SalarySlipPrintingExitedEmployeeParam
    {
        public string Factory { get; set; }
        public string Year_Month { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public List<string> Permission_Group { get; set; }
        public string Language { get; set; }
        public string Kind { get; set; }
        public string Department { get; set; }
        public string EmployeeID { get; set; }
        public string UserName { get; set; }
        public string Lang { get; set; }
    }
    public class SalarySlipPrintingExitedEmployeeDTO
    {
        public List<ResignationSystemPaymentSlip> ResignationSystemPaymentSlip { get; set; }
        public List<SalarySlip> SalarySlip { get; set; }
    }
    public class ResignationSystemPaymentSlip
    {
        public string FactoryHeader { get; set; }
        public string Department { get; set; }
        public string YearMonth { get; set; }
        public string EmployeeID { get; set; }
        public string PrintDate { get; set; }
        public string LocalFullName { get; set; }
        public string PositionTitle { get; set; }
        public string OnboardDate { get; set; }
        public string ResignDate { get; set; }
        public string ResignReason { get; set; }
        public int LastMonthSalary { get; set; }
        public int ThisMonthSalary { get; set; }
        public List<Sal_Monthly_Detail_Temp_7_2_11> Addlist { get; set; }
        public int BenefitsTotal { get; set; }
        public List<Sal_Monthly_Detail_Temp_7_2_11> Dedlist { get; set; }
        public int ObligationsTotal { get; set; }
        public int NETAmountReceived { get; set; }
    }
    public class SalarySlip
    {
        public string FactoryHeader { get; set; }
        public string Department { get; set; }
        public string EmployeeID { get; set; }
        public string LocalFullName { get; set; }
        public string YearMonth { get; set; }
        public string PositionTitle { get; set; }
        public string OnboardDate { get; set; }
        public string MonthlyPositiveItems { get; set; }
        public List<Sal_Monthly_Detail_Values_7_2_11> SalaryItem { get; set; }
        public List<Sal_Monthly_Detail_Values_7_2_11> OverTimeItem { get; set; }
        public List<ItemList> AddItemList { get; set; }
        public decimal MealTotal { get; set; }
        public decimal OtherAdditions { get; set; }
        public int TotalAdditionItem { get; set; }
        public string MonthlyDeductions { get; set; }
        public int LoanedAmount { get; set; }
        public List<DTOs.Sal_Monthly_Detail_Values> InsuranceDeduction { get; set; }
        public List<ItemList> DedItemList { get; set; }
        public int Tax { get; set; }
        public decimal OtherDeductions { get; set; }
        public int TotalDeductionItem { get; set; }
        public int NETAmountReceived { get; set; }
        public decimal CumulativeOvertimeHoursYTD { get; set; }
        public decimal TotalOvertimeHours_CurrentMonth { get; set; }
        public List<Att_Monthly_Detail_Values_7_2_11> OvertimeHours { get; set; }
        public string Attendance { get; set; }
        public decimal ActualWorkDays { get; set; }
        public int DelayEarlyTimes { get; set; }
        public List<Att_Monthly_Detail_Values_7_2_11> LeaveDays { get; set; }
        public decimal StandardAnnualLeaveDays { get; set; }
        public decimal AnnualLeaveEntitlement_ThisYear { get; set; }
        public string CumulativeAnnualLeaveTaken { get; set; }
        public string CumulativeAnnualLeaveRemaining { get; set; }
        public decimal CumulativeAnnualLeaveRemainingTotal { get; set; }
        public decimal TotalPaidDays { get; set; }
        public int DayShiftMealTimes { get; set; }
        public int OvertimeMealTimes { get; set; }
        public int NightShiftAllowanceTimes { get; set; }
        public int NightShiftMealTimes { get; set; }
        public int SalaryAllowance { get; set; }
        public decimal HourlyWage { get; set; }
    }
    public class Sal_Monthly_Detail_Temp_7_2_11
    {
        public string Employee_ID { get; set; }
        public string Permission_Group { get; set; }
        public string Salaty_Type { get; set; }
        public string Item { get; set; }
        public int Amount { get; set; }
    }
    public class Att_Monthly_Detail_Temp_7_2_11
    {
        public string Employee_ID { get; set; }
        public string Leave_Code { get; set; }
        public int Seq { get; set; }
        public decimal Days { get; set; }
    }

    public class ItemList
    {
        public string Type { get; set; }
        public string Item { get; set; }
        public decimal Amount { get; set; }

    }
    public class ListSumDays
    {
        public string Employee_ID { get; set; }
        public int Total { get; set; }

    }
    public class Sal_Monthly_Detail_Values_7_2_11
    {
        public int Seq { get; set; }
        public string Employee_ID { get; set; }
        public string Permission_Group { get; set; }
        public string Salary_Type { get; set; }
        public string Item { get; set; }
        public int Amount { get; set; }
        public string Code { get; set; }
    }
    public class Att_Monthly_Detail_Values_7_2_11
    {
        public int Seq { get; set; }
        public string Employee_ID { get; set; }
        public string Item { get; set; }
        public decimal Days { get; set; }
        public string Code { get; set; }
    }
    public class SalaryDetailResult_7_2_11
    {
        public string Employee_ID { get; set; }
        public string Item { get; set; }
        public decimal Amount { get; set; }
        public string TypeSeq { get; set; }
        public DateTime Sal_Month { get; set; }
        public string AddedType { get; set; }
    }
}