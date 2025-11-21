using API.Dtos.Manage.TeamManagement;
namespace API._Services.Interfaces.Manage
{
    [DependencyInjection(ServiceLifetime.Scoped)]
    public interface ITeamManagementService
    {
        Task<OperationResult> Create(PartDto partDto);
        Task<OperationResult> Update(PartDto partDto);
        Task<List<KeyValuePair<string, string>>> GetAllDepartment();
        Task<PaginationUtility<TeamManagementDataDto>> GetDataPaginations(PaginationParam pagination, string deptID, string partCode, bool isPaging = true);
        Task<PartDto> GetDataDetail(int partID);
        Task<OperationResult> ExportExcel(PaginationParam pagination, PartParam param);
    }
}