using API.DTOs.SalaryReport;

namespace API._Services.Interfaces.SalaryReport
{
    [DependencyInjection(ServiceLifetime.Scoped)]
    public interface I_7_2_17_MonthlyPersonalIncomeTaxAmountReport 
    {
        Task<OperationResult> GetTotalRows(D_7_2_17_MonthlyPersonalIncomeTaxAmountReportParam param);
        Task<OperationResult> Download(D_7_2_17_MonthlyPersonalIncomeTaxAmountReportParam param);

        Task<List<KeyValuePair<string, string>>> GetListFactory(string userName, string language);
        Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language);
        Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language);
    }
}