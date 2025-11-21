using API._Repositories;
using API._Services.Interfaces.Common;
using API._Services.Interfaces.SeaHr;
using API.Dtos.Common;
using API.Dtos.SeaHr.EditLeave;
using API.Helpers.Enums;
using API.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
namespace API._Services.Services.SeaHr
{
    public class EditLeaveService : IEditLeaveService
    {
        private readonly IRepositoryAccessor _repositoryAccessor;
        private readonly IMapper _mapper;
        private readonly ICommonService _serviceCommon;

        public EditLeaveService(IRepositoryAccessor repositoryAccessor, IMapper mapper, ICommonService serviceCommon)
        {
            _repositoryAccessor = repositoryAccessor;
            _mapper = mapper;
            _serviceCommon = serviceCommon;
        }

        public async Task<OperationResult> AcceptEditLeave(int LeaveID, string UserName)
        {
            LeaveData leave = await _repositoryAccessor.LeaveData.FindById(LeaveID);
            leave.Comment += $"-[{_serviceCommon.GetServerTime():dd/MM/yyyy HH:mm:ss}] dasuachua {UserName}";
            leave.LeaveArrange = false;
            /**
            * * Trả EditRequest về 0
            * EditRequest = 0 => chưa từng gởi yêu cầu sửa phép
            * EditRequest = 1 => Đã gởi yêu cầu sửa phép tới chủ quản
            * EditRequest = 2 => Đã gởi yêu cầu sửa phép tới nhân sự
            * EditRequest = 3 => Nhân sự đã từ chối
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
            if (await _repositoryAccessor.SaveChangesAsync())
            {
                return new OperationResult(true, "Edit Leave Successfully");
            }
            return new OperationResult(false, "Edit Leave Failed");
        }

        public async Task<PaginationUtility<LeaveDataDto>> GetAllEditLeave(PaginationParam param)
        {
            List<LeaveDataDto> data = await _repositoryAccessor.LeaveData.FindAll(x => x.Status_Line == true && x.EditRequest == 2)
                .Include(x => x.Cate)
                .Include(x => x.Emp)
                    .ThenInclude(x => x.Part)
                        .ThenInclude(x => x.Dept)
                .Select(x => new LeaveDataDto
                {
                    LeaveID = x.LeaveID,
                    EmpID = x.Emp.EmpID,
                    EmpNumber = x.Emp.EmpNumber,
                    EmpName = x.Emp.EmpName,
                    DeptCode = x.Emp.Part.Dept.DeptCode,
                    CateID = x.Cate.CateID,
                    CateSym = x.Cate.CateSym,
                    PartID = x.Emp.Part.PartID,
                    DateLeave = x.DateLeave,
                    LeaveDay = x.LeaveDay,
                    LeaveDayByString = ConvertLeaveDay(x.LeaveDay.ToString()),
                    TimeLine = x.TimeLine,
                    Comment = x.Comment,
                    LeaveArrange = x.LeaveArrange,
                    Status_Line = x.Status_Line,
                    DateIn = x.Emp.DateIn,
                    Created = x.Created,
                    Updated = x.Updated,
                    exhibit = x.Cate.exhibit,
                    ReasonAdjust = x.ReasonAdjust,
                    CateName_vi = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.VN).CateName,
                    CateName_en = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.EN).CateName,
                    CateName_zh = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.ZH_TW).CateName,
                    PartName_vi = x.Emp.Part.PartLangs.FirstOrDefault(x => x.LanguageID == LangConstants.VN).PartName,
                    PartName_zh = x.Emp.Part.PartLangs.FirstOrDefault(x => x.LanguageID == LangConstants.ZH_TW).PartName,
                    PartName_en = x.Emp.Part.PartLangs.FirstOrDefault(x => x.LanguageID == LangConstants.EN).PartName,
                }).OrderByDescending(x => x.Updated).ToListAsync();
            return PaginationUtility<LeaveDataDto>.Create(data, param.PageNumber, param.PageSize);
        }

        public async Task<DetailEmployeeDto> GetDetailEmployee(int EmployeeID)
        {
            HistoryEmpDto employee = _mapper.Map<HistoryEmpDto>(await _repositoryAccessor.HistoryEmp.FindAll(x => x.EmpID == EmployeeID && x.YearIn == DateTime.Now.Year).FirstOrDefaultAsync());

            // Lấy lên top 5 phép của nhân viên đã xin trong năm
            List<LeaveDataDto> listLeave = _mapper.Map<List<LeaveDataDto>>(await _repositoryAccessor.LeaveData.FindAll(x => x.EmpID == EmployeeID && x.Status_Line == true).OrderByDescending(x => x.Created).Take(5).ToListAsync());

            // Lấy lên top 5 phép đã xóa của người đó đã xin trong năm
            DateTime dateStart = Convert.ToDateTime(DateTime.Now.Year + "/01" + "/01 " + "00:00:00");
            DateTime dateEnd = Convert.ToDateTime(DateTime.Now.Year + "/12" + "/31 " + "23:59:59");
            List<LeaveDataDto> leaveDelete = _mapper.Map<List<LeaveDataDto>>(await _repositoryAccessor.LeaveData.FindAll(x => x.EmpID == EmployeeID && x.Status_Line == false && (x.Time_Start >= dateStart && x.Time_Start <= dateEnd)).OrderByDescending(x => x.Created).Take(5).ToListAsync());

            listLeave.AddRange(leaveDelete);
            foreach (var item in listLeave)
            {
                var CatLang = await _repositoryAccessor.CatLang.FindAll(x => x.CateID == item.CateID).ToListAsync();
                item.CateName_vi = CatLang.FirstOrDefault(x => x.LanguageID == LangConstants.VN).CateName;
                item.CateName_en = CatLang.FirstOrDefault(x => x.LanguageID == LangConstants.EN).CateName;
                item.CateName_zh = CatLang.FirstOrDefault(x => x.LanguageID == LangConstants.ZH_TW).CateName;
                item.LeaveDayByString = ConvertLeaveDay(item.LeaveDay.ToString());
                
                if (item.Approved == 1)
                {
                    item.ApprovedString = "Common.Status1";
                }
                else if (item.Approved == 2)
                {
                    item.ApprovedString = "Common.Status2";
                }
                else if (item.Approved == 3)
                {
                    item.ApprovedString = "Common.Status3";
                }
                else
                {
                    item.ApprovedString = "Common.Status4";
                }

            }
            return new DetailEmployeeDto
            {
                Employee = employee,
                ListLeave = listLeave
            };
        }

        private static string ConvertLeaveDay(string day)
        {
            try
            {
                return Math.Round(decimal.Parse(day), 5) + "d" + " - " + Math.Round(decimal.Parse((Convert.ToDouble(day) * 8).ToString()), 5) + "h";
            }
            catch
            {
                return "0";
            }
        }

        public async Task<OperationResult> RejectEditLeave(int LeaveID, string UserName)
        {
            LeaveData leave = await _repositoryAccessor.LeaveData.FindById(LeaveID);
            leave.Comment += $"-[{_serviceCommon.GetServerTime():dd/MM/yyyy HH:mm:ss}] datuchoi,vuilonglienhenhansu {UserName}";
            leave.LeaveArrange = false;
            /**
            * * Trả EditRequest về 3
            * EditRequest = 0 => chưa từng gởi yêu cầu sửa phép
            * EditRequest = 1 => Đã gởi yêu cầu sửa phép tới chủ quản
            * EditRequest = 2 => Đã gởi yêu cầu sửa phép tới nhân sự
            * EditRequest = 3 => Nhân sự đã từ chối
            */
            leave.EditRequest = 3;
            _repositoryAccessor.LeaveData.Update(leave);
            if (await _repositoryAccessor.SaveChangesAsync())
            {
                return new OperationResult(true, "Reject Edit Leave Successfully");
            }
            return new OperationResult(false, "Reject Edit Leave Failed");
        }
    }
}
