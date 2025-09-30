using API.DTOs.SalaryReport;

namespace API._Services.Interfaces.SalaryReport
{
    [DependencyInjection(ServiceLifetime.Scoped)]
    public interface I_7_2_19_MonthlySalarySummaryReportForFinance 
    {
        Task<int> GetTotalRows(MonthlySalarySummaryReportForFinance_Param param);
         Task<OperationResult> DownloadFileExcel(MonthlySalarySummaryReportForFinance_Param param, string userName);
        Task<List<KeyValuePair<string, string>>> GetListFactory(string userName, string language);
        Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language);
    }
}