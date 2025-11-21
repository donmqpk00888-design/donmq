using API._Repositories;
using API._Services.Interfaces.Common;
using API._Services.Interfaces.Leave;
using API.Dtos.Leave;
using API.Helpers.Enums;
using API.Models;
using Microsoft.EntityFrameworkCore;
namespace API._Services.Services.Leave
{
    public class EditLeaveDataService : IEditLeaveDataService
    {
        private readonly IRepositoryAccessor _repositoryAccessor;
        private readonly ICommonService _commonService;
        public EditLeaveDataService(IRepositoryAccessor repositoryAccessor, ICommonService commonService)
        {
            _repositoryAccessor = repositoryAccessor;
            _commonService = commonService;
        }
        public async Task<PaginationUtility<LeaveDataDTO>> GetAllEditLeave(PaginationParam param, int userID)
        {
            List<LeaveData> data = await _repositoryAccessor.LeaveData
                .FindAll(x => x.Status_Line == true && x.EditRequest == 1 && x.Approved == 2)
                .Include(x => x.Cate.CatLangs)
                .Include(x => x.Emp.Part.Dept.Building)
                .Include(x => x.Emp.Part.Dept.Area)
                .ToListAsync();

            var checkAllowData = await CheckAllowData(userID);
            List<LeaveData> dataSelects = new();
            foreach (var item in checkAllowData)
            {
                if (item.Key == "A")
                {
                    dataSelects.AddRange(data.Where(q => q.Emp.Part.Dept.Area.AreaSym == item.Value));
                }
                else if (item.Key == "B")
                {
                    dataSelects.AddRange(data.Where(q => q.Emp.Part.Dept.Building.BuildingSym == item.Value));
                }
                else if (item.Key == "D")
                {
                    dataSelects.AddRange(data.Where(q => q.Emp.Part.Dept.DeptSym == item.Value));
                }
                else
                {
                    dataSelects.AddRange(data.Where(q => q.Emp.Part.PartSym == item.Value));
                }
            }

            List<LeaveDataDTO> results = dataSelects.Distinct()
               .Select(x => new LeaveDataDTO
               {
                   LeaveID = x.LeaveID,
                   EmpID = x.EmpID,
                   EmpNumber = x.Emp.EmpNumber,
                   EmpName = x.Emp.EmpName,
                   DeptCode = x.Emp.Part.Dept.DeptCode,
                   CateID = x.CateID,
                   CateSym = x.Cate.CateSym,
                   DateLeave = x.DateLeave,
                   LeaveDay = ConvertLeaveDay(x.LeaveDay),
                   TimeLine = x.TimeLine,
                   Created = x.Created,
                   Updated = x.Updated,
                   exhibit = x.Cate.exhibit,
                   Comment = x.Comment,
                   ReasonAdjust  = x.ReasonAdjust,
                   CateNameVN = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.VN).CateName,
                   CateNameEN = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.EN).CateName,
                   CateNameTW = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.ZH_TW).CateName,
               }).OrderByDescending(x => x.Updated).ToList();

            return PaginationUtility<LeaveDataDTO>.Create(results, param.PageNumber, param.PageSize);
        }

        public async Task<LeaveDataDTO> GetLeaveByID(string leaveID)
        {
            LeaveDataDTO data = await _repositoryAccessor.LeaveData.FindAll(x => x.LeaveID == Convert.ToInt32(leaveID))
            .Include(x => x.Cate)
              .ThenInclude(x => x.CatLangs)
            .Include(x => x.Emp)
                .ThenInclude(x => x.Part)
                .ThenInclude(x => x.Dept)
            .Select(x => new LeaveDataDTO
            {
                LeaveID = x.LeaveID,
                EmpID = x.EmpID,
                EmpNumber = x.Emp.EmpNumber,
                EmpName = x.Emp.EmpName,
                DeptCode = x.Emp.Part.Dept.DeptCode,
                CateID = x.CateID,
                CateSym = x.Cate.CateSym,
                DateLeave = x.DateLeave,
                LeaveDay = ConvertLeaveDay(x.LeaveDay),
                TimeLine = x.TimeLine,
                Created = x.Created,
                Updated = x.Updated,
                exhibit = x.Cate.exhibit,
                Comment = x.Comment,
                PartName = x.Emp.Part.PartName,
                DateIn = x.Emp.DateIn.Value.ToString("dd-MM-yyyy"),
                Approved = x.Approved.ToString() == "1" ? "Common.Status1" : x.Approved.ToString() == "2" ? "Common.Status2" : x.Approved.ToString() == "3" ? "Common.Status3" : "Common.Status4",
                Status_Line = x.Status_Line,
                LeaveArrange = x.LeaveArrange,
                LeaveArchive = x.LeaveArchive,
                Time_Start = x.Time_Start,
                Time_End = x.Time_End,
                Time_Applied = x.Time_Applied,
                ApprovedBy = x.User.FullName,
                CateNameVN = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.VN).CateName,
                CateNameEN = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.EN).CateName,
                CateNameTW = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.ZH_TW).CateName,
            }).FirstOrDefaultAsync();
            data.listComment = data.Comment.Split('-');

            return data;
        }

        public async Task<DetailDTO> GetDetailEmployee(string leaveID)
        {
            if (string.IsNullOrEmpty(leaveID))
            {
                return null;
            }
            var leaveId = Convert.ToInt32(leaveID);
            DetailDTO model = new();
            model.ListLeave = await _repositoryAccessor.LeaveData.FindAll(x => x.LeaveID == leaveId && x.EditRequest == 1 && x.Approved == 2)
            .Include(x => x.Cate)
                .ThenInclude(x => x.CatLangs)
            .Include(x => x.Emp)
                .ThenInclude(x => x.Part)
                .ThenInclude(x => x.Dept)
            .Select(x => new LeaveDataDTO
            {
                LeaveID = x.LeaveID,
                EmpID = x.EmpID,
                EmpNumber = x.Emp.EmpNumber,
                EmpName = x.Emp.EmpName,
                DeptCode = x.Emp.Part.Dept.DeptCode,
                CateID = x.CateID,
                CateSym = x.Cate.CateSym,
                DateLeave = x.DateLeave,
                LeaveDay = ConvertLeaveDay(x.LeaveDay),
                TimeLine = x.TimeLine,
                Created = x.Created,
                Updated = x.Updated,
                exhibit = x.Cate.exhibit,
                Comment = x.Comment,
                PartName = x.Emp.Part.PartName,
                DateIn = x.Emp.DateIn.Value.ToString("dd-MM-yyyy"),
                Approved = x.Approved.ToString() == "1" ? "Common.Status1" : x.Approved.ToString() == "2" ? "Common.Status2" : x.Approved.ToString() == "3" ? "Common.Status3" : "Common.Status4",
                Status_Line = x.Status_Line,
                LeaveArrange = x.LeaveArrange,
                LeaveArchive = x.LeaveArchive,
                Time_Start = x.Time_Start,
                Time_End = x.Time_End,
                Time_Applied = x.Time_Applied,
                ApprovedBy = x.User.FullName,
                CateNameVN = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.VN).CateName,
                CateNameEN = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.EN).CateName,
                CateNameTW = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.ZH_TW).CateName,
            }).FirstOrDefaultAsync();

            model.ListLeave.listComment = model.ListLeave.Comment.Split('-');

            return model;
        }

        public static string ConvertLeaveDay(double? day)
        {
            return Math.Round(Convert.ToDecimal(day), 5) + "d - " + Math.Round(Convert.ToDecimal(day * 8), 5) + "h";
        }

        public async Task<OperationResult> EditLeaveData(int LeaveID, string UserName)
        {
            LeaveData leave = await _repositoryAccessor.LeaveData.FindById(LeaveID);
            leave.Comment += $"-[{_commonService.GetServerTime():dd/MM/yyyy HH:mm:ss}] đã chỉnh sửa {UserName}";
            leave.LeaveArrange = false;
            /**
            * * Trả EditRequest về 0
            * EditRequest = 0 => chưa từng gởi yêu cầu sửa phép
            * EditRequest = 1 => Đã gởi yêu cầu sửa phép tới chủ quản
            * EditRequest = 2 => Đã gởi yêu cầu sửa phép tới nhân sự
            */
            leave.EditRequest = 0;
            /**
            * * Trả Approved về 1
            * Approved = 1 => Đang chờ duyệt
            * Approved = 2 => Chủ quản đã duyệt
            * Approved = 3 => Đã từ chối
            * Approved = 4 => Nhân sự đã xác nhận
            */
            leave.Approved = 1;
            _repositoryAccessor.LeaveData.Update(leave);
            var result = await _repositoryAccessor.SaveChangesAsync();
            if (result)
            {
                return new OperationResult(true, "Edit Leave Successfully");
            }
            return new OperationResult(false, "Edit Leave Failed");
        }

        private async Task<List<KeyValuePair<string, string>>> CheckAllowData(int? userID)
        {
            List<KeyValuePair<string, string>> result = new();
            var roles = await _repositoryAccessor.RolesUser.FindAll(x => x.UserID == userID)
            .Include(x => x.Role).ToListAsync();

            if (roles.Count > 0)
            {
                foreach (var item in roles)
                {
                    switch (item.Role.Ranked.Value)
                    {
                        case 1:
                            result.Add(new KeyValuePair<string, string>("A", item.Role.RoleSym));
                            break;
                        case 2:
                            result.Add(new KeyValuePair<string, string>("B", item.Role.RoleSym));
                            break;
                        case 3:
                            result.Add(new KeyValuePair<string, string>("D", item.Role.RoleSym));
                            break;
                        default:
                            result.Add(new KeyValuePair<string, string>("P", item.Role.RoleSym));
                            break;
                    }
                }
            }
            return result;
        }
    }
}