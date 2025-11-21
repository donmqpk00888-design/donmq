using API._Repositories;
using API._Services.Interfaces.Common;
using API._Services.Interfaces.Leave;
using API.Dtos.Leave;
using API.Dtos.Leave.Personal;
using API.Helpers.Utilities;
using API.Models;
using Microsoft.EntityFrameworkCore;
namespace API._Services.Services.Leave
{
    public class LeavePersonalService : ILeavePersonalService
    {
        private readonly IRepositoryAccessor _repositoryAccessor;
        private readonly INotification _notification;
        private readonly ILeaveCommonService _leaveCommonService;
        private readonly ICommonService _commonService;

        public LeavePersonalService(
            IRepositoryAccessor repositoryAccessor,
            INotification notification,
            ILeaveCommonService leaveCommonService,
            ICommonService commonService)
        {
            _repositoryAccessor = repositoryAccessor;
            _notification = notification;
            _leaveCommonService = leaveCommonService;
            _commonService = commonService;
        }

        public async Task<OperationResult> AddLeaveDataPersonal(LeavePersonalDto leavePersonalDto, string userId)
        {
            DateTime from = leavePersonalDto.Time_Start.ConvertToDateTime();
            DateTime to = leavePersonalDto.Time_End.ConvertToDateTime();

            string comment = leavePersonalDto.Comment != "" ? "Ghi chú: " + leavePersonalDto.Comment + ".<br/>" : "";
            if (leavePersonalDto.Comment == "")
                leavePersonalDto.Comment = "themtaitrangcanhan";
            else
                leavePersonalDto.Comment = $"themtaitrangcanhan. Ghi chú: {leavePersonalDto.Comment}";

            Employee emp = await _repositoryAccessor.Employee
                .FindAll(x => x.EmpID == leavePersonalDto.EmpID)
                .Include(x => x.Part.Dept.Building.Area)
                .Include(x => x.Part.Dept.Area)
                .FirstOrDefaultAsync();
            // Lưu vào LeaveData
            leavePersonalDto.LeaveType = "Personal";
            int leaveID = await _leaveCommonService.AddLeave(leavePersonalDto, userId);

            // Gởi email thông báo đến sếp
            await _notification.StepbyStepApproval(emp, leaveID, comment);

            // Lấy ra tất cả các ngày giữa 2 from và to
            List<DateTime> allday = _leaveCommonService.EachDays(from, to);

            // Tạo dữ liệu cho report
            await _leaveCommonService.RecordForReport(0, allday, emp, leaveID);

            await _leaveCommonService.AddCount(from, to, emp);
            try
            {
                return new OperationResult(true);
            }
            catch (System.Exception ex)
            {
                return new OperationResult(false, ex.Message);
            }
        }

        public async Task<bool> DeleteLeaveDataPerson(int leaveId, int empId)
        {

            var leave = await _repositoryAccessor.LeaveData.FindAll(x => x.LeaveID == leaveId && x.EmpID == empId, true)
            .Include(x => x.Cate)
            .FirstOrDefaultAsync();

            leave.Status_Line = false;
            leave.Comment += $"-[{_commonService.GetServerTime():dd/MM/yyyy HH:mm:ss}] Đã xóa";
            leave.Updated = _commonService.GetServerTime();

            if (leave.Cate.CateSym.Equals("U") || leave.Cate.CateSym.Equals("J"))
            {
                HistoryEmp hisemp = await _repositoryAccessor.HistoryEmp.FirstOrDefaultAsync(q => q.YearIn == leave.Time_Start.Value.Year && q.EmpID == empId);
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

            //Xóa dữ liệu đã tạo ra trong ReportData khi gởi đơn xin nghỉ phép
            List<ReportData> reportData = await _repositoryAccessor.ReportData.FindAll(x => x.LeaveID == leaveId && x.EmpID == empId, true).ToListAsync();
            if (reportData.Any())
            {
                _repositoryAccessor.ReportData.RemoveMultiple(reportData);
            }

            return await _repositoryAccessor.SaveChangesAsync();
        }

        public async Task<PersonalDataViewModel> GetDataDetail(string empNumber)
        {
            var empID = await _repositoryAccessor.Users
                .FindAll(true)
                .Include(x => x.Emp)
                .Where(x => x.Emp.EmpNumber == empNumber)
                .FirstOrDefaultAsync();
            return empID != null ? await GetData(empID.UserID.ToString()) : null;
        }

        public async Task<PersonalDataViewModel> GetData(string userId)
        {
            // get user
            Users user = await _repositoryAccessor.Users.FindById(Convert.ToInt32(userId));

            // get employee and deptcode
            EmployeeDataDto emp = await _leaveCommonService.GetEmployeeData(user.EmpID.Value);

            PersonalDataViewModel data = new()
            {
                Employee = emp,
                LeaveDataViewModel = await GetLeaveData(emp.EmpID),
                History = await _leaveCommonService.GetHistoryEmployee(emp.EmpID, DateTime.Now.Year)
            };
            return data;
        }

        private async Task<List<LeaveDataViewModel>> GetLeaveData(int empId)
        {
            List<LeaveData> leaveDatas = await _repositoryAccessor.LeaveData
                .FindAll(x => x.EmpID == empId && x.Status_Line == true && x.Approved == 1, true)
                .OrderByDescending(o => o.Updated).ToListAsync();

            List<LeaveDataViewModel> leaveDataViewModels = new();
            foreach (var item in leaveDatas)
            {
                leaveDataViewModels.Add(await _leaveCommonService.GetLeaveDataWithCategory(item));
            }

            return leaveDataViewModels;
        }
    }
}