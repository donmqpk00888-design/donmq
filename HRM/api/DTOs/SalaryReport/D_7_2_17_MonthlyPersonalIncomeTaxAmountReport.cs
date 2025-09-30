using API.Models;

namespace API.DTOs.SalaryReport
{
    public class D_7_2_17_MonthlyPersonalIncomeTaxAmountReportParam
    {
        public string Factory { get; set; }
        public string Year_Month { get; set; }
        public List<string> Permission_Group { get; set; }
        public string Department { get; set; }
        public string Employee_ID { get; set; }

        public string UserName { get; set; }
        public string Language { get; set; }

    }
    public class D_7_2_17_MonthlyPersonalIncomeTaxAmountReportData
    {
        public int No { get; set; }
        public string Factory { get; set; }
        public string Department { get; set; }
        public string DepartmentName { get; set; }
        public string EmployeeID { get; set; }
        public string LocalFullName { get; set; }
        public string TaxNo { get; set; }
        public short NumberofDependents { get; set; }
        public int TotalAllowableDeductionAmountfromTaxableIncomeBasedonFamilyCircumstances { get; set; }
        public decimal DeductionAmountBasedonNumberofDependents { get; set; }
        public decimal TaxableAmountofPersonalIncome { get; set; }
        public int Tax { get; set; }
        public int TotalAdditionItem { get; set; }
    }
    public class DataSave
    {
        public HRMS_Emp_Personal HEP { get; set; }
        public HRMS_Sal_Tax HST { get; set; }
    }
}