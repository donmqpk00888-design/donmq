using API.DTOs.SalaryReport;

namespace API._Services.Interfaces.SalaryReport
{
    [DependencyInjection(ServiceLifetime.Scoped)]
    public interface I_7_2_21_MonthlyAdditionsAndDeductionsSummaryReportForFinance
    {
        Task<OperationResult> GetTotalRows(MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param param);
        Task<OperationResult> Download(MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param param);
        Task<List<KeyValuePair<string, string>>> GetListFactory(string userName, string language);
        Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language);
    }
}