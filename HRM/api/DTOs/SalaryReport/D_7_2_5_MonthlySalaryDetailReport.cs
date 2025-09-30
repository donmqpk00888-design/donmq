using API.Models;

namespace API.DTOs.SalaryReport
{
    public class MonthlySalaryDetailReportParam
    {
        public string Factory { get; set; }
        public string Year_Month { get; set; }
        public string Year_Month_Str { get; set; }
        public string Kind { get; set; }
        public List<string> Permission_Group { get; set; } = new();
        public string Transfer { get; set; }
        public string Department { get; set; }
        public string Employee_ID { get; set; }
        public string Lang { get; set; }
    }

    public class MonthlySalaryDetailReportDto
    {
        public int Seq { get; set; }
        public string Department { get; set; }
        public string Department_Name { get; set; }
        public string Employee_ID { get; set; }
        public string Local_Full_Name { get; set; }
        public string Position_Title { get; set; }
        public string Work_Type { get; set; }
        public decimal Actual_Days { get; set; }
        public List<KeyValuePair<string, decimal>> Leave_Days { get; set; }
        public int Delay_Early { get; set; }
        public List<KeyValuePair<string, decimal>> Overtime_Hours { get; set; }
        public decimal Total_Paid_Days { get; set; }
        public decimal Salary_Allowance { get; set; }
        public decimal Hourly_Wage { get; set; }
        public List<KeyValuePair<string, decimal>> Salary_Item { get; set; }
        public int? DayShift_Food { get; set; }
        public int Food_Expenses { get; set; }
        public int Night_Eat_Times { get; set; }
        public int? NightShift_Food { get; set; }
        public List<KeyValuePair<string, decimal>> Overtime_Allowance { get; set; }
        public decimal B49_amt { get; set; }
        public decimal Other_Additions { get; set; }
        public decimal Total_Addition_Item { get; set; }
        public decimal Loaned_Amount { get; set; }
        public List<KeyValuePair<string, decimal>> Insurance_Deduction { get; set; }
        public decimal Union_Fee { get; set; }
        public decimal Tax { get; set; }
        public decimal Other_Deductions { get; set; }
        public decimal Total_Deduction_Item { get; set; }
        public decimal Net_Amount_Received { get; set; }
        public string Currency { get; set; }
        public string Transfer { get; set; }
        public string Salary_Type { get; set; }
        public decimal Meal_Total { get; set; }
    }

    public class Sal_Monthly
    {
        public string Employee_ID { get; set; }
        public string Local_Full_Name { get; set; }
        public string Department { get; set; }
        public string Permission_Group { get; set; }
        public string Work_Type { get; set; }
        public string Salary_Type { get; set; }
        public int Tax { get; set; }
        public string Currency { get; set; }
        public string Transfer { get; set; }
    }

    public class Sal_Backup
    {
        public string Employee_ID { get; set; }
        public string Position_Title { get; set; }
    }

    public class Att_Monthly
    {
        public string Employee_ID { get; set; }
        public decimal Actual_Days { get; set; }
        public int? Delay_Early { get; set; }
        public int? DayShift_Food { get; set; }
        public int? Food_Expenses { get; set; }
        public int? Night_Eat_Times { get; set; }
        public int? NightShift_Food { get; set; }
    }

    #region Query_Att_Monthly_Detail
    public class Att_Monthly_Detail_Temp
    {
        public string Employee_ID { get; set; }
        public string Leave_Code { get; set; }
        public decimal Days { get; set; }
    }

    public class Setting_Temp
    {
        public int Seq { get; set; }
        public string Code { get; set; }
    }

    public class Att_Monthly_Values
    {
        public string Employee_ID { get; set; }
        public string Leave_Code { get; set; }
        public int Seq { get; set; }
        public decimal Days { get; set; }
    }
    #endregion

    #region Query_Sal_Monthly_Detail
    public class Sal_Monthly_Detail_Temp_7_2_5
    {
        public string Employee_ID { get; set; }
        public string Item { get; set; }
        public int Amount { get; set; }
    }

    public class Sal_Setting_Temp_7_2_5
    {
        public int Seq { get; set; }
        public string Salary_Item { get; set; }
        public string Permission_Group { get; set; }
        public string Salary_Type { get; set; }
    }

    public class Att_Setting_Temp_7_2_5
    {
        public int Seq { get; set; }
        public string Code { get; set; }
    }

    public class Sal_Monthly_Detail_Values_7_2_5
    {
        public int Seq { get; set; }
        public string Employee_ID { get; set; }
        public string Permission_Group { get; set; }
        public string Salary_Type { get; set; }
        public string Item { get; set; }
        public int Amount { get; set; }
        public string Code { get; set; }
    }
    #endregion

    public class SalaryDetail
    {
        public string Employee_ID { get; set; }
        public string Item { get; set; }
        public decimal Amount { get; set; }
        public string TypeSeq { get; set; }
        public string AddedType { get; set; }
    }

    public class CalculationList
    {
        public string Employee_ID { get; set; }
        public int Amount { get; set; }
    }
}