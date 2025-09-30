namespace API.DTOs.SalaryReport
{
    public class D_7_2_9_SalarySummaryReportExitedEmployee
    {

    }

    public class SalarySummaryReportExitedEmployeeData
    {
        public string Department { get; set; }
        public string Department_Name { get; set; }
        public string CCenter { get; set; }
        public string Employee_ID { get; set; }
        public string Local_Full_Name { get; set; }
        public DateTime Onboard_Date { get; set; }
        public DateTime? Resign_Date { get; set; }
        public decimal LSalary { get; set; }
        public decimal TSalary { get; set; }
        public List<KeyValuePair<string, int>> addlist { get; set; }
        public decimal addlistAmt { get; set; }
        public List<KeyValuePair<string, int>> delist { get; set; }
        public decimal delistAmt { get; set; }
        public decimal act_sub { get; set; }
        public decimal act_get { get; set; }
        public decimal changetotal { get; set; }
    }

    public class SalarySummaryReportExitedEmployeeParam
    {
        public string Factory { get; set; }
        public string Resignation_Start { get; set; }
        public string Resignation_End { get; set; }
        public string Transfer { get; set; }
        public List<string> Permission_Group { get; set; }
        public string Department { get; set; }
        public string Employee_ID { get; set; }
        public string UserName { get; set; }
        public string Language { get; set; }
    }
}