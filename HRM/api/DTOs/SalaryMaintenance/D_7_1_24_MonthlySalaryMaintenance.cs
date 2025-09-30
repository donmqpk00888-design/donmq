using API.Models;

namespace API.DTOs.SalaryMaintenance
{
    public class MonthlySalaryMaintenanceDto
    {
        public string Seq { get; set; }
        public string Probation { get; set; }
        public string USER_GUID { get; set; }
        public string DepartmentName { get; set; }
        public string Factory { get; set; }
        public DateTime Sal_Month { get; set; }
        public string Local_Full_Name { get; set; }
        public string Employee_ID { get; set; }
        public string Department { get; set; }
        public string Permission_Group { get; set; }
        public string Salary_Type { get; set; }
        public string Lock { get; set; }
        public string FIN_Pass_Status { get; set; }
        public string Update_By { get; set; }
        public string Update_Time { get; set; }
        public string BankTransfer { get; set; }
        public string Language { get; set; }
        public int Tax { get; set; }
        public string Currency { get; set; }
        public bool IsDelete { get; set; }

    }
    public class MonthlySalaryMaintenance_Delete
    {
        public string Employee_ID { get; set; }
        public string Sal_Month { get; set; }
        public string Factory { get; set; }
    }
    public class MonthlySalaryMaintenance_Update
    {
        public string Factory { get; set; }
        public DateTime Sal_Month { get; set; }
        public string Employee_ID { get; set; }
        public string Update_By { get; set; }
        public string Update_Time { get; set; }
        public int Tax { get; set; }
        public MonthlySallaryDetail SalaryDetail { get; set; }
    }

    public class MonthlySalaryMaintenanceDetail : MonthlyAttendanceData
    {
        public decimal PaidSalaryDays { get; set; }
        public decimal ActualWorkDays { get; set; }
        public string NewHiredResigned { get; set; }
        public MonthlyAttendanceData AttendanceData { get; set; }
        public MonthlySallaryDetail SalaryDetail { get; set; }

        public MonthlySalaryMaintenanceDetail()
        {
            AttendanceData = new MonthlyAttendanceData();
            SalaryDetail = new MonthlySallaryDetail();
        }
    }

    public class MonthlyAttendanceData
    {
        public int DelayEarly { get; set; }
        public int NoSwipCard { get; set; }
        public int? DayShiftMealTimes { get; set; }
        public int OvertimeMealTimes { get; set; }
        public int NightShiftAllowanceTimes { get; set; }
        public int? NightShiftMealTimes { get; set; }
        public List<MonthlyAttendanceData_Leave> Leave { get; set; }
        public List<MonthlyAttendanceData_Allowance> Allowance { get; set; }

        public MonthlyAttendanceData()
        {
            Leave = new List<MonthlyAttendanceData_Leave>();
            Allowance = new List<MonthlyAttendanceData_Allowance>();
        }
    }
    public class MonthlyDetail_Temp
    {
        public List<HRMS_Att_Resign_Monthly_Detail> HARMD { get; set; }
        public List<HRMS_Att_Monthly_Detail> HAMD { get; set; }
    }
    public class MonthlyAttendanceData_Leave
    {
        public string Leave { get; set; }
        public string Leave_Name { get; set; }
        public decimal MonthlyDays { get; set; }
    }
    public class Query_Att_Monthly_Detail
    {
        public string Leave_Code { get; set; }
        public decimal Days { get; set; }
        public int Seq { get; set; }
    }
    public class Query_Sal_Monthly_Detail
    {
        public string Item { get; set; }
        public int Amount { get; set; }
        public int? Seq { get; set; }
        public int SumAmount { get; set; }
    }
    public class MonthlySallaryDetail
    {
        public List<MonthlySallaryDetail_Table> Table_1 { get; set; }
        public List<MonthlySallaryDetail_Table> Table_2 { get; set; }
        public List<MonthlySallaryDetail_Table> Table_3 { get; set; }
        public List<MonthlySallaryDetail_Table> Table_4 { get; set; }
        public List<MonthlySallaryDetail_Table> Table_5 { get; set; }
        public int TotalAmountReceived { get; set; }
        public int Tax { get; set; }
        public MonthlySallaryDetail()
        {
            Table_1 = new List<MonthlySallaryDetail_Table>();
            Table_2 = new List<MonthlySallaryDetail_Table>();
            Table_3 = new List<MonthlySallaryDetail_Table>();
            Table_4 = new List<MonthlySallaryDetail_Table>();
            Table_5 = new List<MonthlySallaryDetail_Table>();
        }
    }
    public class MonthlySallaryDetail_Table
    {
        public List<MonthlySallaryDetail_Item> ListItem { get; set; }
        public int SumAmount { get; set; }
    }

    public class MonthlySallaryDetail_Item
    {
        public string Item { get; set; }
        public string Item_Name { get; set; }
        public int Amount { get; set; }
    }



    public class MonthlyAttendanceData_Allowance
    {
        public string Allowance { get; set; }
        public string Allowance_Name { get; set; }
        public decimal MonthlyDays { get; set; }
    }

    public class MonthlySalaryMaintenanceParam
    {
        public string Factory { get; set; }
        public string Employee_ID { get; set; }
        public string SalMonth { get; set; }
        public string Department { get; set; }
        public string Language { get; set; }
        public List<string> Permission_Group { get; set; }
    }
    public class SettingTemp
    {
        public string Code { get; set; }
        public int Seq { get; set; }
    }
    public class SalSettingTemp
    {
        public string SalaryItem { get; set; }
        public int Seq { get; set; }
    }

    public class MonthlySalaryMaintenance_Personal
    {
        public string USER_GUID { get; set; }
        public string Local_Full_Name { get; set; }
    }

    public class TableDataList
    {
        public List<HRMS_Att_Monthly_Detail> HAMD { get; set; }
        public List<HRMS_Att_Use_Monthly_Leave> HAUML { get; set; }
        public List<HRMS_Sal_Item_Settings> HSIS { get; set; }
        public List<HRMS_Sal_Monthly_Detail> HSMD { get; set; }
    }
}