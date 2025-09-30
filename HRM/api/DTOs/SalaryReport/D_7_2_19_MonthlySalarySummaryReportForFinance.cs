namespace API.DTOs.SalaryReport
{
    public class MonthlySalarySummaryReportForFinance_Param
    {
        public string Factory { get; set; }
        public DateTime Year_Month_Start { get; set; }
        public DateTime Year_Month_End { get; set; }
        public string Kind { get; set; }
        public List<string> Permission_Group { get; set; }
        public string Department { get; set; }
        public string Employee_ID { get; set; }
        public string Language { get; set; }
    }
    public class MonthlySalarySummaryReportForFinance_Dto
    {
        public DateTime? Resign_Date { get; set; }
        public string Department { get; set; }
        public string Position_Title { get; set; }
        public string Permission_Group { get; set; }
        public string Factory { get; set; }
        public DateTime Sal_Month { get; set; }
        public string Employee_ID { get; set; }
        public string BankTransfer { get; set; }
        public int Tax { get; set; }
        public string Out_day { get; set; }
    }
    public class MonthlySalarySummaryReportForFinance_Data
    {
        public string Out_day { get; set; }
        public string Permission_Group { get; set; }
        public string TypeCode { get; set; }
        public int Additional_Deduction { get; set; }
        public int Deduction_Items { get; set; }
        public int Salary { get; set; }
        public int OVTPay { get; set; }
        public int Foodpay { get; set; }
        public int Other_Del { get; set; }
        public int Other_Add { get; set; }
        public int Add_Total { get; set; }
        public int Loan { get; set; }
        public int Scfee { get; set; }
        public int Mdfee { get; set; }
        public int Seat { get; set; }
        public int Tax { get; set; }
        public int Wkmy { get; set; }
        public int Redtotal { get; set; }
        public int Total { get; set; }
        public int Actotal { get; set; }
        public string Act_flag { get; set; }
        public int Atm { get; set; }
        public int Natm { get; set; }
        public int Wk_natm { get; set; }
        public string BankNo { get; set; }
        public int DelAmt { get; set; }
        public int AddAmt { get; set; }
        public int Reserved { get; set; }
        public string BankTransfer { get; set; }
        public int Headcount { get; set; }

    }
    public class MonthlySalarySummaryReportForFinance_Total
    {
        public int TT_Salary { get; set; }
        public int TT_OVTPay { get; set; }
        public int TT_Head_count { get; set; }
        public int TT_Foodpay { get; set; }
        public int TT_Other_Del { get; set; }
        public int TT_Other_Add { get; set; }
        public int TT_Scfee { get; set; }
        public int TT_Add_Total { get; set; }
        public int TT_Mdfee { get; set; }
        public int TT_Seat { get; set; }
        public int TT_Tax { get; set; }
        public int TT_Wkmy { get; set; }
        public int TT_Redtotal { get; set; }
        public int TT_Actotal { get; set; }
        public int TT_Atm { get; set; }
        public int TT_Natm { get; set; }
        public int TT_Reserved { get; set; }
        public int TT_Total { get; set; }
        public int TT_Loan { get; set; }
    }
    public class SalaryDetailBatchResult_ForFinance
    {
        public string Employee_ID { get; set; }
        public string Item { get; set; }
        public int Amount { get; set; }
        public string TypeSeq { get; set; }
        public DateTime Sal_Month { get; set; }
        public string AddedType { get; set; }
    }
    
}
