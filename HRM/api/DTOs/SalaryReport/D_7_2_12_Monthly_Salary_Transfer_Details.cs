using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs.SalaryReport
{
    public class D_7_2_12_Monthly_Salary_Transfer_Details
    {

    }

    public class MonthlySalaryTransferDetailsParam
    {
        public string Factory { get; set; }
        public string Year_Month { get; set; }
        public List<string> Permission_Group { get; set; }
        public string Department { get; set; }
        public string UserName { get; set; }
        public string Language { get; set; }
    }

    public class MonthlySalaryTransferDetails
    {
        public string USER_GUID { get; set; }
        public int Seq { get; set; }
        public string Factory { get; set; }
        public string Permission_Group { get; set; }
        public string Department { get; set; }
        public string DepartmentName { get; set; }
        public string EmployeeID { get; set; }
        public string LocalFullName { get; set; }
        public string BankAccount { get; set; }
        public decimal Amount { get; set; }
        public string IdentificationNumber { get; set; }
        public string BankName { get; set; }
        public string Branch { get; set; }
        public int Tax { get; set; }
        public DateTime Sal_Month { get; set; }
    }
}