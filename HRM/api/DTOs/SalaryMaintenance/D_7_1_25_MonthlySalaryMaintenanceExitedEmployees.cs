using API.Models;

namespace API.DTOs.SalaryMaintenance;
public class D_7_25_MonthlySalaryMaintenanceExitedEmployeesSearchParam
{
    public string Factory { get; set; }
    public string Department { get; set; }
    public List<string> Permission_Group { get; set; }
    public string Year_Month { get; set; }
    public string Employee_ID { get; set; }
    public string Lang { get; set; }
    public string UserName { get; set; }
}
public class D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain
{
    public string Seq { get; set; }
    public string Probation { get; set; }
    public string Salary_Lock { get; set; }
    public DateTime Year_Month { get; set; }
    public string Factory { get; set; }
    public string Department { get; set; }
    public string Department_Name { get; set; }
    public string Employee_ID { get; set; }
    public string Local_Full_Name { get; set; }
    public string Permission_Group { get; set; }
    public string Salary_Type { get; set; }
    public string FIN_Pass_Status { get; set; }
    public string Transfer { get; set; }
    public string Currency { get; set; }
    public int Tax { get; set; }
    // Monthly attendance data can be expanded/collapsed
    public D_7_25_Query_Att_MonthlyResult Monthly_Attendance { get; set; }
    public string Update_By { get; set; }
    public string Update_Time { get; set; }
    public bool isDelete { get; set; }
}

public class D_7_25_Query_Att_MonthlyResult
{
    public decimal Paid_Salary_Days { get; set; }
    public decimal Actual_Work_Days { get; set; }
    public string New_Hired_Resigned { get; set; }
    public int? Delay_Early { get; set; }
    public int? No_Swip_Card { get; set; }
    public int? Day_Shift_Meal_Times { get; set; }
    public int? Overtime_Meal_Times { get; set; } // Food_Expenses
    public int? Night_Shift_Allowance_Times { get; set; } // Night_Eat_Times
    public int? Night_Shift_Meal_Times { get; set; }

}

public class D_7_25_Query_Att_Monthly
{
    public List<HRMS_Att_Monthly> tblHRMS_Att_Monthly { get; set; }
    public List<HRMS_Att_Resign_Monthly> tblHRMS_Att_Resign_Monthly { get; set; }
}

#region Query_Att_Monthly_Detail
public class D_7_25_Att_Monthly_Detail_Temp
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

public class D_7_25_AttMonthlyDetailValues
{
    public string Leave_Code { get; set; }
    public string Leave_Code_Name { get; set; }
    public decimal Days { get; set; }
}

public class D_7_25_Query_Att_Monthly_DetailResult
{
    public List<D_7_25_AttMonthlyDetailValues> Table_Left_Leave { get; set; }
    public List<D_7_25_AttMonthlyDetailValues> Table_Right_Allowance { get; set; }
}

#endregion

#region 3.40.Query_Sal_Monthly_Detail 
public class D_7_25_Salary_Item_Sal_Monthly_Detail_Temp
{
    public string Factory { get; set; }
    public DateTime Sal_Month { get; set; }
    public string Employee_ID { get; set; }
    public int Type_Seq { get; set; }
    public string AddDed_Type { get; set; }
    public string Item { get; set; }
    public decimal Amount { get; set; }
}

public class D_7_25_Leave_Code_Name
{
    public string Code { get; set; }
    public string Code_Name { get; set; }
}

public class D_7_25_Salary_Item_Sal_Monthly_Detail_Values
{
    public string Item { get; set; }
    public string Item_Name { get; set; }
    public decimal Amount { get; set; }
    public string Type_Seq { get; set; }
    public string AddDed_Type { get; set; }
}

public class D_7_25_Query_Sal_Monthly_Detail_Result
{
    public List<D_7_25_Salary_Item_Sal_Monthly_Detail_Values> Salary_Item_Table1 { get; set; }
    public List<D_7_25_Salary_Item_Sal_Monthly_Detail_Values> Salary_Item_Table2 { get; set; }
    public List<D_7_25_Salary_Item_Sal_Monthly_Detail_Values> Salary_Item_Table3 { get; set; }
    public List<D_7_25_Salary_Item_Sal_Monthly_Detail_Values> Salary_Item_Table4 { get; set; }
    public List<D_7_25_Salary_Item_Sal_Monthly_Detail_Values> Salary_Item_Table5 { get; set; }
    public int Tax { get; set; }
    public decimal Total_Item_Table1 { get; set; }
    public decimal Total_Item_Table2 { get; set; }
    public decimal Total_Item_Table3 { get; set; }
    public decimal Total_Item_Table4 { get; set; }
    public decimal Total_Item_Table5 { get; set; }
    /*
    Total Item+ Total Allowance+ Total Addition Item- Total Deduction Item- Total Leave-Tax
    */
    public decimal TotalAmountReceived
    {
        get
        {
            return Total_Item_Table1 + Total_Item_Table2 + Total_Item_Table3 - Total_Item_Table4 - Total_Item_Table5 - Tax;
        }
    }
}

public class D_7_25_Query_Sal_Monthly_Detail_Result_Source
{
    public D_7_25_Query_Att_Monthly_DetailResult Monthly_Attendance_Data { get; set; }
    public D_7_25_Query_Sal_Monthly_Detail_Result Monthly_Salary_Detail { get; set; }
}

public class D_7_25_GetMonthlyAttendanceDataDetailParam
{
    public string Probation { get; set; }
    public int Tax { get; set; }
    public string Language { get; set; }
    public string Factory { get; set; }
    public string Year_Month { get; set; }
    public string Employee_ID { get; set; }
    public string Permission_Group { get; set; }
    public string Salary_Type { get; set; }
}
#endregion

public class D_7_25_MonthlySalaryMaintenance_Update
{
    public D_7_25_GetMonthlyAttendanceDataDetailUpdateParam Param { get; set; }
    public D_7_25_Query_Sal_Monthly_Detail_Result Table_Details { get; set; }
}

public class D_7_25_GetMonthlyAttendanceDataDetailUpdateParam
{
    public int Tax { get; set; }
    public string USER_GUID { get; set; }
    public string Division { get; set; }
    public string Factory { get; set; }
    public string Sal_Month { get; set; }
    public string Employee_ID { get; set; }
    public string Update_By { get; set; }
    public DateTime Update_Time { get; set; }
}