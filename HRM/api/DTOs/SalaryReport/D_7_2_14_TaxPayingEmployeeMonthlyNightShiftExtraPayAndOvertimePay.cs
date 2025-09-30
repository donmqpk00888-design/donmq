
namespace API.DTOs.SalaryReport
{
    public class NightShiftExtraAndOvertimePayReport
    {
        public string Factory { get; set; }
        public string Department { get; set; }
        public string DepartmentName { get; set; }
        public string EmployeeID { get; set; }
        public string LocalFullName { get; set; }
        public string TaxNo { get; set; }
        public decimal Standard { get; set; }
        public List<Att_Monthly_Detail_Values> OvertimeHours { get; set; }
        public List<OvertimeAndNightShiftAllowance> OvertimeAndNightShiftAllowance { get; set; }
        public decimal A06_AMT { get; set; }
        public decimal Overtime50_AMT { get; set; }
        public decimal NHNO_AMT { get; set; }
        public decimal HO_AMT { get; set; }
        public decimal INS_AMT { get; set; }
        public decimal SUM_AMT { get; set; }

    }
    public class NightShiftExtraAndOvertimePayParam
    {
        public string Factory { get; set; }
        public string Year_Month { get; set; }
        public List<string> Permission_Group { get; set; }
        public string Department { get; set; }
        public string EmployeeID { get; set; }
        public string UserName { get; set; }
        public string Language { get; set; }

    }
    public class Att_Monthly_Detail_Values
    {
        public string Employee_ID { get; set; }
        public string Leave_Code { get; set; }
        public string CodeName_TW { get; set; }
        public string CodeName_EN { get; set; }
        public int Seq { get; set; }
        public decimal Days { get; set; }
    }
    public class Att_Monthly_Detail_Temp_7_2_14
    {
        public string Employee_ID { get; set; }
        public string Leave_Code { get; set; }
        public decimal Days { get; set; }
    }

    public class Setting_Temp_7_2_14
    {
        public int Seq { get; set; }
        public string Code { get; set; }
    }

    public class OvertimeAndNightShiftAllowance
    {
        public int Seq { get; set; }
        public string Employee_ID { get; set; }
        public string AllowanceName_EN { get; set; }
        public string AllowanceName_TW { get; set; }
        public string Salary_Type { get; set; }
        public string Item { get; set; }
        public int Amount { get; set; }
        public string Code { get; set; }
    }
}