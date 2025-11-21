using API.Dtos.Manage.UserManage;
using API.Models;
namespace API._Services.Interfaces.Manage
{
    [DependencyInjection(ServiceLifetime.Scoped)]
    public interface IUserService
    {
        Task<UserForDetailDto> GetUser(int userId);
        Task<PaginationUtility<UserForDetailDto>> GetAll(PaginationParam pagination, string keyword);
        Task<OperationResult> Add(UserForDetailDto userDto);
        Task<OperationResult> Edit(UserForDetailDto userDto);
        Task<OperationResult> UploadExcel(IFormFile file);
        Task<OperationResult> DownloadExcel(UserManageTitleExcel title, string keyword, string lang);
        Task<bool> CheckLeavePermission(int? userId, int? group);
    }
}