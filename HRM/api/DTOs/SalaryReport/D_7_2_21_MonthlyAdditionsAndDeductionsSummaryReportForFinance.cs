namespace API.DTOs.SalaryReport
{
    public class MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param
    {
        public string Factory { get; set; }
        public string YearMonth { get; set; }
        public string Kind { get; set; }
        public string Department { get; set; }
        public string EmployeeID { get; set; }
        public string Language { get; set; }
        public string UserName { get; set; }
    }
    public class TableData
    {
        public string PermissionGroup { get; set; }
        public string Factory { get; set; }
        public string EmployeeID { get; set; }
        public DateTime SalMonth { get; set; }
    }
    public class MonthlyAdditionsAndDeductionsSummaryReportForFinance_Result
    {
        public string OnJob { get; set; }
        public string PermissionGroup { get; set; }
        public string AdditionsAndDeductionsType { get; set; }
        public string AdditionsAndDductionsItem { get; set; }
        public decimal Amount { get; set; }
        public string SubTotal { get; set; }
    }
}