
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
        public string Lang{ get; set; }
    }
}