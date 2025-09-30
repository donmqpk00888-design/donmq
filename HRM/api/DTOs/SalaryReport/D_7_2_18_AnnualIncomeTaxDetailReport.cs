using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs.SalaryReport
{

    public class AnnualIncomeTaxDetailReportParam
    {
        public string Factory { get; set; }
        public DateTime Year_Month_Start { get; set; }
        public DateTime Year_Month_End { get; set; }
        public string Employee_ID { get; set; }
        public List<string> Permission_Group { get; set; }
        public string Department { get; set; }
        public string UserName { get; set; }
        public string Language { get; set; }

    }




    /// <summary>
    /// Thông tin chi tiết báo cáo thuế thu nhập cá nhân
    /// </summary>
    public class AnnualIncomeTaxDetailReport
    {
        public int No { get; set; }
        public DateTime yymm { get; set; }
        public string Employee_ID { get; set; }
        public string Local_Full_Name { get; set; }
        public string USER_GUID { get; set; }
        public string Identification_Number { get; set; }
        public string Department { get; set; }
        public string Department_Name { get; set; }
        public string Factory { get; set; }

        public string TaxNo { get; set; }
        public int y_amt { get; set; }
        public int loan { get; set; }
        public int pertax { get; set; }
        public int mon_cnt { get; set; }
        public int subqty { get; set; }


        //=================== Tổng của tất cả tháng của nhân viên ===================
        public int Sum_amt { get; set; }
        public int Sum_tax { get; set; }
        public int Sum_subqty { get; set; }
        public int Sum_ovtm { get; set; }
        public int Sum_wage_t { get; set; }
        public int Sum_wage_h { get; set; }
        public int Sum_ins_fee { get; set; }
        public int Sum_sum_amt { get; set; }

        /// <summary>
        /// Danh sách theo từng tháng & tổng chi tiết theo từng tháng
        /// </summary>
        public List<AnnualIncomeTaxDetail_By_Month> Detail_By_Months { get; set; }


        public AnnualIncomeTaxDetailReport()
        {
            Detail_By_Months = new List<AnnualIncomeTaxDetail_By_Month>();
        }
    }



    /// <summary>
    ///  Mẫu chi tiết thuế thu nhập cá nhân theo tháng
    /// </summary>
    public class AnnualIncomeTaxDetail_By_Month
    {
        public string YearMonth { get; set; }
        public AnnualIncomeTaxDetail_Pattent Detail_Pattent { get; set; }

        public AnnualIncomeTaxDetail_By_Month(string yearMonth, AnnualIncomeTaxDetail_Pattent detail_Pattent)
        {
            YearMonth = yearMonth;
            Detail_Pattent = detail_Pattent;
        }
    }
    
    public class AnnualIncomeTax_Column_Month
    {
        public AnnualIncomeTax_Column_Month(string months)
        {
            Month = months;
        }

        public string Month { get; set; }
        public List<string> EN_Title
        {
            get
            {
                return new List<string>(){

                    "Total Taxable Income after Deduction for Dependents",
                    "Tax",
                    "Dependents",
                    "Overtime Paid 50% Difference on Normal Working Day",
                    "Night Shift Overtime Paid 100% or 110% Difference on Normal Working Day",
                    "Overtime Paid 300% Difference on National Holiday",
                    "Insurance Amount",
                    "Net Amount Received"
                };
            }
        }
        public List<string> ZH_Title
        {
            get
            {
                return new List<string>(){
                    "個人薪資計算應繳交所得稅",
                    "所得稅金額",
                    "扶養",
                    "非假日加班費差額50%",
                    "非假日夜班加班費差額100%& 110%",
                    "假日加班費差額300%",
                    "保險費",
                    "實領金額"
                };
            }
        }
       
    }

    public class AnnualIncomeTaxDetail_Pattent
    {
        public int amt { get; set; }
        public int tax { get; set; }
        public int subqty { get; set; }
        public int ovtm { get; set; }
        public int wage_t { get; set; }
        public int wage_h { get; set; }
        public int ins_fee { get; set; }
        public int sum_amt { get; set; }
        public int mon_cnt { get; set; }

    }
    
    public class TotalResultAnnualIncomeTaxDetail
    {
        public int Count { get; set; }
        public int Total_y_amt { get; set; }
        public int Total_pertax { get; set; }
        public int Total_loan { get; set; }

    }
    public class AnnualIncomeTaxDetail_Hearder
    {
        public string Factory { get; set; }
        public string YearMonth { get; set; }
        public string PermisionGroups { get; set; }
        public string Department { get; set; }
        public string Employee_Id { get; set; }
        public string PrintBy { get; set; }
        public string PrintDate { get; set; }
    }
}