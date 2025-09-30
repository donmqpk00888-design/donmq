using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs.SalaryReport;

namespace API._Services.Interfaces.SalaryReport
{
    [DependencyInjection(ServiceLifetime.Scoped)]
    public interface I_7_2_20_MonthlySalarySummaryReportForTaxation
    {
        Task<int> GetTotalRows(MonthlySalarySummaryReportForTaxation_Param param);
        Task<OperationResult> DownloadFileExcel(MonthlySalarySummaryReportForTaxation_Param param, string userName);
        Task<List<KeyValuePair<string, string>>> GetListFactory(string userName, string language);
        Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language);
        Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language);
    }
}