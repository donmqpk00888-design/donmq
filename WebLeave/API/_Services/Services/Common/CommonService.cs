using API._Repositories;
using API._Services.Interfaces.Common;
using API.Dtos.Common;
using API.Models;
using Microsoft.EntityFrameworkCore;
namespace API._Services.Services.Common
{
    public class CommonService : ICommonService
    {
        private readonly IRepositoryAccessor _repoAccessor;
        public CommonService(IRepositoryAccessor repoAccessor)
        {
            _repoAccessor = repoAccessor;
        }

        public async Task<List<Area>> GetAreas()
        {
            return await _repoAccessor.Area.FindAll().ToListAsync();
        }

        public async Task<List<Building>> GetBuildings()
        {
            return await _repoAccessor.Building.FindAll().ToListAsync();
        }

        public async Task<List<CommentArchive>> GetCommentArchives()
        {
            return await _repoAccessor.CommentArchive.FindAll().OrderBy(x => x.Value).ToListAsync();
        }

        public async Task<List<Company>> GetCompanys()
        {
            return await _repoAccessor.Company.FindAll().ToListAsync();
        }

        public async Task<List<Department>> GetDepartments()
        {
            return await _repoAccessor.Department.FindAll().ToListAsync();
        }

        public async Task<BrowserInfoDto> GetLoginDetectInfo(string username)
        {
            BrowserInfoDto result = new()
            {
                Factory = SettingsConfigUtility.GetCurrentSettings("AppSettings:Factory")
            };
            if (!string.IsNullOrEmpty(username?.Trim()))
                result.LoginDetect = await _repoAccessor.LoginDetect.FirstOrDefaultAsync(x => x.UserName == username.Trim());

            return result;
        }

        public DateTime GetServerTime()
        {
            // Lấy múi giờ của máy chủ  
            var serverTimeZone = TimeZoneInfo.Local;
            var utcNow = DateTime.UtcNow;
            // Chuyển đổi sang thời gian máy chủ 
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, serverTimeZone);
        }
    }
}