

namespace API.DTOs.SalaryReport
{
    public class MonthlyAdditionsAndDeductionsSummaryReport_Param
    {
        public string Factory { get; set; }
        public string Salary_Type { get; set; }
        public string Year_Month { get; set; }
        public DateTime Year_Month_Date { get; set; }
        public string Year_Month_Str { get; set; }
        public List<string> Permission_Group { get; set; }
        public string Department { get; set; }
        public string Employee_ID { get; set; }
        public string Lang { get; set; }
        public int Total_Rows { get; set; }
        public string Function_Type { get; set; }
    }
    public class MonthlyAdditionsAndDeductionsSummaryReport_ExcelData
    {
        public List<MonthlyAdditionsAndDeductionsSummaryReport_Detail> Detail { get; set; }
        public int Sub_Total { get; set; }
    }
    public class MonthlyAdditionsAndDeductionsSummaryReport_Detail
    {
        public string Permission_Group { get; set; }
        public string Permission_Group_Name { get; set; }
        public string AddDed_Type { get; set; }
        public string AddDed_Type_Name { get; set; }
        public string VSortcod { get; set; }
        public string VSortcod_Name { get; set; }
        public string AddDed_Item { get; set; }
        public string AddDed_Item_Name { get; set; }
        public int Amount { get; set; }
    }
}