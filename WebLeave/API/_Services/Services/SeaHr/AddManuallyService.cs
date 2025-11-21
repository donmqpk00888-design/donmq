using API._Repositories;
using API._Services.Interfaces.Common;
using API.Dtos.Leave.Personal;
using API.Dtos.SeaHr.ViewModel;
using API.Helpers.Enums;
using API.Helpers.Params.Seahr;
using API.Helpers.Utilities;
using API.Models;
using Microsoft.EntityFrameworkCore;
namespace API._Services.Services.SeaHr
{
    public class AddManuallyService : IAddManuallyService
    {
        private readonly IRepositoryAccessor _repositoryAccessor;
        private readonly ILeaveCommonService _leaveCommonService;
        private readonly ICommonService _commonService;
        private readonly INotification _notification;

        public AddManuallyService(
            IRepositoryAccessor repositoryAccessor,
            ILeaveCommonService leaveCommonService,
            ICommonService commonService,
            INotification notification)
        {
            _notification = notification;
            _repositoryAccessor = repositoryAccessor;
            _leaveCommonService = leaveCommonService;
            _commonService = commonService;
        }
        public async Task<OperationResult> AddManually(AddManuallyParam param, string userId)
        {
            Employee emp = await _repositoryAccessor.Employee
                .FindAll(x => x.EmpNumber == param.empNumber)
                .Include(x => x.Part.Dept.Building.Area)
                .Include(x => x.Part.Dept.Area)
                .FirstOrDefaultAsync();

            if (emp == null)
                return new OperationResult(false, "Mã nhân viên không tồn tại");

            if (await _leaveCommonService.CheckDateLeave(param.txtPersonalBeign, param.txtPersonalEnd, emp.EmpID) == "ERROR")
                return new OperationResult(false, "Thời gian bạn xin phép trùng với thời gian người dùng đã xin!");

            LeavePersonalDto leavePersonalDto = new()
            {
                CateID = param.slCategoryPersonal,
                Comment = "themphepthucong",
                EmpID = emp.EmpID,
                EmpNumber = param.empNumber,
                LeaveDay = param.leaveday,
                Time_End = param.txtPersonalEnd,
                Time_Lunch = param.lunchTime,
                Time_Start = param.txtPersonalBeign,
                LeaveType = "AddManually",
                IpLocal = param.ipLocal
            };

            DateTime d1 = param.txtPersonalBeign.ConvertToDateTime();
            DateTime d2 = param.txtPersonalEnd.ConvertToDateTime();

            int leaveID = await _leaveCommonService.AddLeave(leavePersonalDto, userId);

            //Gởi email thông báo đến sếp
            string result = await _notification.StepbyStepApproval(emp, leaveID, "");

            //Lấy ra tất cả các ngày giữa 2 from và to
            List<DateTime> allday = _leaveCommonService.EachDays(d1, d2);

            // Tạo dữ liệu cho report
            await _leaveCommonService.RecordForReport(0, allday, emp, leaveID);

            await _leaveCommonService.AddCount(d1, d2, emp);
            try
            {
                return new OperationResult(true, "Thêm thành công", leaveID);
            }
            catch (System.Exception ex)
            {
                return new OperationResult(false, ex.Message);
            }
        }

        //Xóa Phép
        public async Task<OperationResult> DeleteManual(string leaveId)
        {
            var leave = await _repositoryAccessor.LeaveData.FindAll(x => x.LeaveID.ToString() == leaveId, true)
            .Include(x => x.Cate).FirstOrDefaultAsync();

            if (leave == null)
                return new OperationResult(true, "Không tìm thấy");

            leave.Status_Line = false;
            leave.Comment += $"-[{_commonService.GetServerTime():dd/MM/yyyy HH:mm:ss}] Đã xóa";
            leave.Updated = _commonService.GetServerTime();

            if (leave.Cate.CateSym.Equals("U") || leave.Cate.CateSym.Equals("J"))
            {
                HistoryEmp hisemp = await _repositoryAccessor.HistoryEmp.FirstOrDefaultAsync(q => q.YearIn == leave.Time_Start.Value.Year && q.EmpID == leave.EmpID);
                if (hisemp.CountLeave > 0)
                    hisemp.CountLeave -= leave.LeaveDay;

                if (hisemp.CountTotal > 0)
                    hisemp.CountTotal -= leave.LeaveDay;

                if (leave.Cate.CateSym == "J" && hisemp.CountAgent > 0)
                {
                    hisemp.CountAgent -= leave.LeaveDay;
                    hisemp.CountRestAgent += leave.LeaveDay;
                }
                else if (leave.Cate.CateSym == "U" && hisemp.CountArran > 0)
                {
                    hisemp.CountArran -= leave.LeaveDay;
                    hisemp.CountRestArran += leave.LeaveDay;
                }

                hisemp.Updated = _commonService.GetServerTime();
                _repositoryAccessor.HistoryEmp.Update(hisemp);
            }
            _repositoryAccessor.LeaveData.Update(leave);

            List<ReportData> reportDatas = await _repositoryAccessor.ReportData
                .FindAll(x => x.LeaveID.ToString() == leaveId && x.EmpID == leave.EmpID, true)
                .ToListAsync();
            if (reportDatas.Any())
            {
                _repositoryAccessor.ReportData.RemoveMultiple(reportDatas);
            }
            var result = await _repositoryAccessor.SaveChangesAsync();
            return new OperationResult(true, result ? "Xóa thành công" : "Xóa thất bại");
        }

        public async Task<List<KeyValuePair<int, string>>> GetAllCategory(string lang)
        {
            if (lang == "zh")
            {
                lang = LangConstants.ZH_TW;
            }

            List<KeyValuePair<int, string>> data = await _repositoryAccessor.Category.FindAll(x => x.Visible == true)
            .Include(x => x.CatLangs)
            .Select(x => new KeyValuePair<int, string>(
                x.CateID, $"{x.CateSym} - {x.CatLangs.FirstOrDefault(z => z.CateID == x.CateID && z.LanguageID == lang).CateName}"
            )).ToListAsync();

            return data;
        }

        public async Task<AddManuallyViewModel> GetDetail(int leaveId)
        {
            AddManuallyViewModel data = await _repositoryAccessor.LeaveData.FindAll(x => x.LeaveID == leaveId, true)
            .Include(x => x.Cate)
            .Include(x => x.Emp)
                .ThenInclude(x => x.Part)
            .Select(x => new AddManuallyViewModel
            {
                LeaveId = x.LeaveID,
                CateID = x.CateID,
                EmpName = x.Emp.EmpName,
                EmpNumber = x.Emp.EmpNumber,
                CateName = x.Cate.CateSym + " - " + x.Cate.CateName,
                DepName = x.Emp.Part.PartCode,
                LeaveDay = x.LeaveDay,
                Time_Start = x.Time_Start,
                Time_End = x.Time_End
            }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<string> CheckDateLeave(string start, string end, string empNumber)
        {
            Employee emp = await _repositoryAccessor.Employee.FirstOrDefaultAsync(x => x.EmpNumber == empNumber);
            return await _leaveCommonService.CheckDateLeave(start, end, emp.EmpID);
        }

        public async Task<double?> GetCountRestAgent(int year, string empNumber)
        {
            Employee emp = await _repositoryAccessor.Employee.FirstOrDefaultAsync(x => x.EmpNumber == empNumber);
            return await _leaveCommonService.GetCountRestAgent(emp.EmpID, year);
        }

        public async Task<bool> CheckIsSun(string empNumber)
        {
            var check = await _repositoryAccessor.Employee.FirstOrDefaultAsync(x => x.EmpNumber == empNumber);
            return check?.IsSun == true;
        }
    }
}