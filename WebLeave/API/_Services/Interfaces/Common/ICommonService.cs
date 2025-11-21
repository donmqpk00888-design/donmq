using API.Dtos.Common;
using API.Models;

namespace API._Services.Interfaces.Common
{
    [DependencyInjection(ServiceLifetime.Scoped)]
    public interface ICommonService
    {
        Task<List<Company>> GetCompanys();
        Task<List<Area>> GetAreas();
        Task<List<Building>> GetBuildings();
        Task<List<Department>> GetDepartments();
        Task<List<CommentArchive>> GetCommentArchives();
        Task<BrowserInfoDto> GetLoginDetectInfo(string username);
        DateTime GetServerTime();
    }
}