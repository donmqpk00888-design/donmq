using API.Models;

namespace API.DTOs;

public class SystemInfo
{
    public IEnumerable<DirectoryInfomation> Directories { get; set; }
    public IEnumerable<ProgramInfomation> Programs { get; set; }
    public IEnumerable<FunctionInfomation> Functions { get; set; }
    public IEnumerable<CodeInformation> Code_Information { get; set; }
}
public class DirectoryInfomation
{
    public string Seq { get; set; }
    public string Directory_Name { get; set; }
    public string Directory_Code { get; set; }
}
public class ProgramInfomation
{
    public string Seq { get; set; }
    public string Program_Name { get; set; }
    public string Program_Code { get; set; }
    public string Parent_Directory_Code { get; set; }
}
public class FunctionInfomation
{
    public string Program_Code { get; set; }
    public string Function_Code { get; set; }
}
public class CodeInformation
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Kind { get; set; }
    public IEnumerable<CodeLang> Translations { get; set; }
}
public class CodeLang
{
    public string Lang { get; set; }
    public string Name { get; set; }
}

public class BasicCodeInfo : HRMS_Basic_Code
{
    public string Code_Name_Str { get; set; }           // "<Code>-<Name>"
}

public class DepartmentInfo
{
    public string Division { get; set; }
    public string Factory { get; set; }
    public string Department { get; set; }              // "<Code>"
    public string Department_Code { get; set; }         // "<Code>"
    public string Department_Name { get; set; }         // "<Name>"
    public string Department_Code_Name { get; set; }    // "<Code>-<Name>"
}

public class EmployeeCommonInfo : HRMS_Emp_Personal
{
    public string Onboard_Date_Str { get; set; }
    public string Work8hours_Str { get; set; }
    public string Work_Type_Name { get; set; }
    public string Actual_Factory { get; set; }
    public string Actual_Division { get; set; }
    public string Actual_Employee_ID { get; set; }
    public string Actual_Department_Code { get; set; }
    public string Actual_Department_Name { get; set; }
    public string Actual_Department_Code_Name { get; set; }
}

public class TimeData
{
    public float Hours { get; set; }
    public float Minutes { get; set; }
}
public class DataResult
{
    public byte[] Result { get; set; }
    public int Count { get; set; }
}

#region Query_Sal_Monthly_Detail
public class Sal_Monthly_Detail_Temp
{
    public string Employee_ID { get; set; }
    public string Item { get; set; }
    public int Amount { get; set; }
}

public class Sal_Setting_Temp
{
    public int Seq { get; set; }
    public string Salary_Item { get; set; }
    public string Permission_Group { get; set; }
    public string Salary_Type { get; set; }
}

public class Att_Setting_Temp
{
    public int Seq { get; set; }
    public string Code { get; set; }
}

public class Sal_Monthly_Detail_Values
{
    public int Seq { get; set; }
    public string Employee_ID { get; set; }
    public string Permission_Group { get; set; }
    public string Salary_Type { get; set; }
    public string Item { get; set; }
    public int Amount { get; set; }
    public string Code { get; set; }
}

public class SalaryDetailResult
{
    public string Employee_ID { get; set; }
    public string Item { get; set; }
    public decimal Amount { get; set; }
    public string TypeSeq { get; set; }
    public DateTime Sal_Month { get; set; }
    public string AddedType { get; set; }
}
#endregion