
using API.DTOs.SalaryReport;

namespace API._Services.Interfaces.SalaryReport
{
    [DependencyInjection(ServiceLifetime.Scoped)]
    public interface I_7_2_22_MonthlyAdditionsAndDeductionsSummaryReport
    {
        Task<List<KeyValuePair<string, string>>> GetFactoryList(string lang, List<string> roleList);
        Task<List<KeyValuePair<string, string>>> GetDropDownList(MonthlyAdditionsAndDeductionsSummaryReport_Param param, List<string> roleList);
        Task<OperationResult> Process(MonthlyAdditionsAndDeductionsSummaryReport_Param param, string userName);
    }
}