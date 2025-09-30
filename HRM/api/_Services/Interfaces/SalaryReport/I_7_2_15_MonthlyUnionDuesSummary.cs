using API.DTOs.SalaryReport;

namespace API._Services.Interfaces.SalaryReport
{
    [DependencyInjection(ServiceLifetime.Scoped)]
    public interface I_7_2_15_MonthlyUnionDuesSummary
    {
        Task<OperationResult> GetTotalRows(MonthlyUnionDuesSummaryParam param);
        Task<OperationResult> Download(MonthlyUnionDuesSummaryParam param);
        Task<List<KeyValuePair<string, string>>> GetListFactory(string userName, string language);
        Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language);
    }
}