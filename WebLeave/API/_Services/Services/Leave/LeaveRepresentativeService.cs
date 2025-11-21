using API._Repositories;
using API._Services.Interfaces.Common;
using API._Services.Interfaces.Leave;
using API.Dtos.Leave;
using API.Dtos.Leave.Personal;
using API.Dtos.Leave.Representative;
using API.Helpers.Enums;
using API.Helpers.Utilities;
using API.Models;
using Microsoft.EntityFrameworkCore;
using LinqKit;
namespace API._Services.Services.Leave
{
    public class LeaveRepresentativeService : ILeaveRepresentativeService
    {
        private readonly IRepositoryAccessor _repositoryAccessor;
        private readonly IFunctionUtility _functionUtility;
        private readonly ILeaveCommonService _leaveCommonService;
        private readonly ICommonService _commonService;
        private readonly INotification _notification;

        public LeaveRepresentativeService(
            IRepositoryAccessor repositoryAccessor,
            IFunctionUtility functionUtility,
            ILeaveCommonService leaveCommonService,
            ICommonService commonService,
            INotification notification)
        {
            _repositoryAccessor = repositoryAccessor;
            _functionUtility = functionUtility;
            _leaveCommonService = leaveCommonService;
            _commonService = commonService;
            _notification = notification;
        }

        public async Task<bool> AddLeaveData(LeavePersonalDto leavePersonal, string userId)
        {
            string addfast = _functionUtility.HashPasswordUser("addfast");
            bool? check = _repositoryAccessor.DefaultSetting.FindAll(q => q.GroupSett == 2 && q.KeySett == addfast).FirstOrDefault()?.ValueSett.Value;

            DateTime from = leavePersonal.Time_Start.ConvertToDateTime();
            DateTime to = leavePersonal.Time_End.ConvertToDateTime();
            //userid người đại diện xin nghỉ
            Employee emp = await _repositoryAccessor.Employee
                .FindAll(x => x.EmpNumber == leavePersonal.EmpNumber)
                .Include(x => x.Part.Dept.Building.Area)
                .Include(x => x.Part.Dept.Area)
                .FirstOrDefaultAsync();

            int leaveID = 0;
            string comment = leavePersonal.Comment != "" && check == true ? "Ghi chú: " + leavePersonal.Comment + ".<br/>" : "";

            if (check == true)
            {
                if (leavePersonal.Comment == "")
                    leavePersonal.Comment = "themtutrangdaidien";
                else
                    leavePersonal.Comment = $"themtutrangdaidien. Ghi chú: {leavePersonal.Comment}";
                leavePersonal.LeaveType = "Representative";
                leaveID = await _leaveCommonService.AddLeave(leavePersonal, userId);
            }

            //Gửi email thông báo đến sếp
            await _notification.StepbyStepApproval(emp, leaveID, comment);

            //Lấy ra tất cả các ngày giữa 2 from và to
            List<DateTime> allday = _leaveCommonService.EachDays(from, to);

            //Tạo dữ liệu cho report
            await _leaveCommonService.RecordForReport(0, allday, emp, leaveID);

            await _leaveCommonService.AddCount(from, to, emp);
            try
            {
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteLeave(List<RepresentativeDataViewModel> leaveDatas)
        {
            foreach (var item in leaveDatas)
            {
                if (item.LeaveDataViewModel.LeaveID > 0)
                {
                    var leave = _repositoryAccessor.LeaveData.FindAll(x => x.LeaveID == item.LeaveDataViewModel.LeaveID && x.EmpID == item.LeaveDataViewModel.EmpID)
                      .Include(x => x.Cate)
                      .FirstOrDefault();

                    leave.Status_Line = false;
                    leave.Comment += $"-[{_commonService.GetServerTime():dd/MM/yyyy HH:mm:ss}] Đã xóa";
                    leave.Updated = _commonService.GetServerTime();

                    if (leave.Cate.CateSym.Equals("U") || leave.Cate.CateSym.Equals("J"))
                    {
                        HistoryEmp hisemp = await _repositoryAccessor.HistoryEmp
                            .FirstOrDefaultAsync(q => q.YearIn == leave.Time_Start.Value.Year && q.EmpID == item.LeaveDataViewModel.EmpID);
                        hisemp.CountLeave -= leave.LeaveDay;
                        hisemp.CountTotal -= leave.LeaveDay;
                        if (leave.Cate.CateSym == "J")
                        {
                            hisemp.CountAgent -= leave.LeaveDay;
                            hisemp.CountRestAgent += leave.LeaveDay;
                        }
                        else if (leave.Cate.CateSym == "U")
                        {
                            hisemp.CountArran -= leave.LeaveDay;
                            hisemp.CountRestArran += leave.LeaveDay;
                        }

                        hisemp.Updated = _commonService.GetServerTime();
                        _repositoryAccessor.HistoryEmp.Update(hisemp);
                    }

                    _repositoryAccessor.LeaveData.Update(leave);

                    //Xóa dữ liệu đã tạo ra trong ReportData khi gởi đơn xin nghỉ phép
                    List<ReportData> reportData = await _repositoryAccessor.ReportData
                        .FindAll(x => x.LeaveID == item.LeaveDataViewModel.LeaveID && x.EmpID == item.LeaveDataViewModel.EmpID, true)
                       .ToListAsync();

                    if (reportData.Any())
                    {
                        _repositoryAccessor.ReportData.RemoveMultiple(reportData);
                    }
                }
            }
            return await _repositoryAccessor.SaveChangesAsync();
        }

        public async Task<OperationResult> GetEmployeeInfo(string userId, string empNumber)
        {
            EmployeeInfo emp = new();
            List<string> allowData = await _functionUtility.CheckAllowData(Convert.ToInt32(userId));
            if (!allowData.Any())
                return new OperationResult(false, "No body");

            HistoryEmp history = await _repositoryAccessor.HistoryEmp
                .FindAll(x => x.Emp.EmpNumber == empNumber && x.Emp.Visible == true && x.YearIn == DateTime.Now.Year)
                .Include(x => x.Emp.Part.Dept.Area)
                .Include(x => x.Emp.Part.Dept.Building.Area)
                .FirstOrDefaultAsync();

            bool temp = false;

            if (history != null)
            {
                foreach (var item in allowData)
                {
                    string[] group = item.Split('-');
                    string tableSym = group[0]; // A: Area, B: Builing, D: Department, P: Part
                    string sym = group[1];

                    if (tableSym == "A" && history.Emp.Part.Dept.Area.AreaSym == sym)
                    {
                        temp = true;
                        break;
                    }
                    else if (tableSym == "B" && history.Emp.Part.Dept.Building.BuildingSym == sym)
                    {
                        temp = true;
                        break;
                    }
                    else if (tableSym == "D" && history.Emp.Part.Dept.DeptSym == sym)
                    {
                        temp = true;
                        break;
                    }
                    else if (history.Emp.Part.PartSym == sym)
                    {
                        temp = true;
                        break;
                    }
                }

                if (temp)
                {
                    emp.empname = history.Emp.EmpName;
                    emp.empid = history.EmpID.Value;
                    emp.isSun = history.Emp.IsSun;
                    //Tổng số phép năm được cấp
                    emp.totalleave = history.TotalDay.Value;
                    //Phép năm đã dùng: phép năm cá nhân đã nghỉ + phép năm công ty đã nghỉ
                    emp.counttotal = history.CountTotal.Value;
                    //phép năm cá nhân còn lại
                    emp.cagent = history.CountRestAgent ?? history.Agent.Value - history.CountAgent.Value;
                    //phép năm công ty còn lại
                    emp.carrange = history.CountRestArran ?? history.Arrange.Value - history.CountArran.Value;
                    //Số ngày phép năm bạn đã xin
                    emp.restedleave = history.CountLeave.Value;
                }
            }
            return new OperationResult(true, emp);
        }

        public async Task<List<RepresentativeDataViewModel>> GetDataLeave(string userId)
        {
            int userid = Convert.ToInt32(userId);

            List<string> allowData = await _functionUtility.CheckAllowData(Convert.ToInt32(userId));
            IQueryable<LeaveDataQuery> dataJoin = GetDataJoin();
            List<LeaveDataQuery> data = await _repositoryAccessor.LeaveData.FindAll(q => q.Status_Line == true && q.Approved == 1 && q.UserID == userid, true)
                .Join(dataJoin,
                    x => x.EmpID,
                    y => y.Employee.EmpID,
                    (x, y) => new LeaveDataQuery { Leave = x, Employee = y.Employee, Area = y.Area, Building = y.Building, Department = y.Department, Part = y.Part })
                .ToListAsync();

            List<LeaveData> leaveDatas = await GetLeaveData(data, allowData);

            List<RepresentativeDataViewModel> leaveDataViewModels = new();
            foreach (var item in leaveDatas)
            {
                RepresentativeDataViewModel leaveData = new();
                EmployeeDataDto emp = await _leaveCommonService.GetEmployeeData(item.EmpID.Value, userid);
                emp.CountRestAgent = CountRestAgent(item.EmpID.Value);
                leaveData.Employee = emp;

                leaveData.LeaveDataViewModel = await _leaveCommonService.GetLeaveDataWithCategory(item);
                leaveData.LeaveDataViewModel.Disable = !emp.CheckUser && leaveData.LeaveDataViewModel.Status_Lock.Value;
                leaveDataViewModels.Add(leaveData);
            }

            return leaveDataViewModels;
        }

        public async Task<List<LeaveDataViewModel>> GetListOnTime(string userId, int leaveId)
        {
            int userid = Convert.ToInt32(userId);
            List<string> allowData = await _functionUtility.CheckAllowData(userid);
            LeaveData leave = await _repositoryAccessor.LeaveData.FindById(leaveId);

            DateTime d1 = Convert.ToDateTime(leave.Time_Start);
            DateTime d2 = Convert.ToDateTime(leave.Time_End);

            List<LeaveDataQuery> dataQuery = await _repositoryAccessor.LeaveData.FindAll(q => q.Status_Line == true && q.LeaveID != leaveId &&
            ((q.Time_Start <= d1 && q.Time_End > d1) ||
            (q.Time_Start < d2 && q.Time_End >= d2) ||
            (q.Time_Start >= d1 && q.Time_End <= d2) ||
            (q.Time_Start <= d1 && q.Time_End >= d2)), true)
            .Join(GetDataJoin(),
                x => x.EmpID,
                y => y.Employee.EmpID,
                (x, y) => new LeaveDataQuery { Leave = x, Employee = y.Employee, Area = y.Area, Building = y.Building, Department = y.Department, Part = y.Part })
            .ToListAsync();

            List<LeaveData> leaveDatas = await GetLeaveData(dataQuery, allowData);

            IQueryable<SetApproveGroupBase> role_group = _repositoryAccessor.SetApproveGroupBase.FindAll(q => q.UserID == userid, true);
            List<int> listGBID = await role_group.Select(x => x.GBID.Value).ToListAsync();
            var leaveDataQuery = leaveDatas
            .Join(await _repositoryAccessor.Employee.FindAll(true).ToListAsync(),
                x => x.EmpID,
                y => y.EmpID,
                (x, y) => new { LeaveData = x, Employee = y }
            ).ToList();

            List<LeaveData> leaveDataResult = new();

            if (listGBID.Any())
            {
                leaveDataResult = leaveDataQuery.Where(x => x.Employee.GBID.HasValue && listGBID.Contains(x.Employee.GBID.Value)).Select(x => x.LeaveData).ToList();
            }
            else
            {
                leaveDataResult = leaveDataQuery.Where(q => q.Employee.GBID == 0).Select(x => x.LeaveData).ToList();
            }

            leaveDataResult = leaveDataResult.OrderByDescending(o => o.Updated).Take(20).ToList();
            List<LeaveDataViewModel> leaveDataViewModels = new();
            foreach (var item in leaveDataResult)
            {
                LeaveDataViewModel leaveView = await _leaveCommonService.GetLeaveDataWithCategory(item);
                leaveView.Status = $"Common.{GetStatus(leaveView.Approved.Value)}";
                leaveDataViewModels.Add(leaveView);
            }
            return leaveDataViewModels;
        }

        private double CountRestAgent(int empId)
        {
            HistoryEmp historyEmp = _repositoryAccessor.HistoryEmp
                .FindAll(x => x.EmpID == empId && x.YearIn == DateTime.Now.Year)
                .OrderByDescending(x => x.Updated).FirstOrDefault(); ;
            if (historyEmp != null)
            {
                return Math.Round(historyEmp.CountRestAgent.Value, 5);
            }
            return 0;
        }

        private static async Task<List<LeaveData>> GetLeaveData(List<LeaveDataQuery> dataQuery, List<string> allowData)
        {
            List<LeaveData> leaveDatas = new();

            foreach (var item in allowData)
            {
                string[] group = item.Split('-');
                string tableSym = group[0]; // A: Area, B: Builing, D: Department, P: Part
                string sym = group[1];
                if (tableSym == "A")
                {
                    List<LeaveData> details = dataQuery.Where(x => x.Area.AreaSym == sym).Select(x => x.Leave).ToList();
                    leaveDatas.AddRange(details);
                }
                else if (tableSym == "B")
                {
                    List<LeaveData> details = dataQuery.Where(x => x.Building.BuildingSym == sym).Select(x => x.Leave).ToList();
                    leaveDatas.AddRange(details);
                }
                else if (tableSym == "D")
                {
                    List<LeaveData> details = dataQuery.Where(x => x.Department.DeptSym == sym).Select(x => x.Leave).ToList();
                    leaveDatas.AddRange(details);
                }
                else
                {
                    List<LeaveData> details = dataQuery.Where(x => x.Part.PartSym == sym).Select(x => x.Leave).ToList();
                    leaveDatas.AddRange(details);
                }
            }

            leaveDatas = leaveDatas.Distinct().OrderByDescending(x => x.Updated).Take(300).ToList();
            return await Task.FromResult(leaveDatas);
        }

        private static string GetStatus(int approved)
        {
            string satatus = approved switch
            {
                1 => CommonConstants.WAIT,
                2 => CommonConstants.APPROVED,
                3 => CommonConstants.REJECTED,
                _ => CommonConstants.FINISH,
            };
            return satatus;
        }

        private IQueryable<LeaveDataQuery> GetDataJoin(int? empId = null)
        {
            var employeePred = PredicateBuilder.New<Employee>(x => x.Visible == true);
            if (empId != null)
                employeePred.And(x => x.EmpID == empId);

            IQueryable<LeaveDataQuery> data = _repositoryAccessor.Employee.FindAll(employeePred, true)
                .Where(x => x.Part != null && x.Part.Dept != null && x.Part.Dept.Area != null && x.Part.Dept.Building != null)
                .Include(x => x.Part)
                    .ThenInclude(x => x.Dept)
                .Include(x => x.Part.Dept.Area)
                .Include(x => x.Part.Dept.Building)
                .Select(x => new LeaveDataQuery
                {
                    Employee = x,
                    Area = x.Part.Dept.Area,
                    Building = x.Part.Dept.Building,
                    Department = x.Part.Dept,
                    Part = x.Part
                });
            return data;
        }
    }
}