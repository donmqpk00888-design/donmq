using API.DTOs.SalaryReport;

namespace API._Services.Interfaces.SalaryReport
{
    [DependencyInjection(ServiceLifetime.Scoped)]
    public interface I_7_2_11_SalarySlipPrintingExitedEmployee
    {
        Task<OperationResult> GetTotalRows(SalarySlipPrintingExitedEmployeeParam param);
        Task<OperationResult> Download(SalarySlipPrintingExitedEmployeeParam param);
        Task<List<KeyValuePair<string, string>>> GetListFactory(string userName, string language);
        Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language);
        Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language);
        Task<List<KeyValuePair<string, string>>> GetListLanguage(string factory, string language);
    }
}