using System;
using System.Globalization;
using API._Repositories;
using API._Services.Interfaces.Common;
using API.Dtos.Common;
using API.Dtos.Leave;
using API.Dtos.Leave.Personal;
using API.Helpers.Enums;
using API.Helpers.Utilities;
using API.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;


namespace API._Services.Services.Common
{
    public class LeaveCommonService : ILeaveCommonService
    {
        private readonly IRepositoryAccessor _repoAccessor;
        private readonly IMapper _mapper;
        private readonly ICommonService _serviceCommon;

        public LeaveCommonService(
            IRepositoryAccessor repoAccessor,
            ICommonService serviceCommon,
            IMapper mapper)
        {
            _repoAccessor = repoAccessor;
            _serviceCommon = serviceCommon;
            _mapper = mapper;
        }

        public async Task<int> AddLeave(LeavePersonalDto leavePersonalDto, string userId)
        {
            double dleave = Convert.ToDouble(leavePersonalDto.LeaveDay);

            Category cate = await _repoAccessor.Category.FirstOrDefaultAsync(x => x.CateID == leavePersonalDto.CateID);

            DateTime from = leavePersonalDto.Time_Start.ConvertToDateTime();
            DateTime to = leavePersonalDto.Time_End.ConvertToDateTime();

            Employee emp = await _repoAccessor.Employee.FirstOrDefaultAsync(x => x.EmpID == leavePersonalDto.EmpID);

            var _serverTime = _serviceCommon.GetServerTime(); 

            LeaveData leave = new()
            {
                EmpID = leavePersonalDto.EmpID,
                DateLeave = from,
                CateID = leavePersonalDto.CateID,
                Approved = 1,
                EditRequest = 0,
                LeaveDay = dleave,
                LeavePlus = false,
                Status_Line = true,
                Status_Lock = false,
                Time_Applied = _serverTime,
                Time_Start = from,
                Time_End = to,
                UserID = Convert.ToInt32(userId),
                Updated = _serverTime,
                Created = _serverTime,
                TimeLine = $"{from:HH:mm dd/MM/yyyy} - {to:HH:mm dd/MM/yyyy}",
                LeaveArchive = ReturnArchive(emp.EmpNumber, leavePersonalDto.Time_Lunch),
                Comment = $"[{_serverTime:dd/MM/yyyy HH:mm:ss}] {leavePersonalDto.Comment}"
            };

            _repoAccessor.LeaveData.Add(leave);

            string cateSym = cate?.CateSym;
            if (cateSym.Equals("U") || cateSym.Equals("J"))
            {
                HistoryEmp hisemp = await _repoAccessor.HistoryEmp.FirstOrDefaultAsync(q => q.EmpID == leavePersonalDto.EmpID && q.YearIn == from.Year);
                hisemp.CountTotal += dleave;
                hisemp.CountLeave += dleave;

                // J = Phép năm
                // U = Phép năm công ty
                if (cateSym == "J")
                {
                    hisemp.CountAgent += dleave;
                    hisemp.CountRestAgent -= dleave;
                }
                else
                {
                    hisemp.CountArran += dleave;
                    hisemp.CountRestArran -= dleave;
                }
                _repoAccessor.HistoryEmp.Update(hisemp);
            }

            await _repoAccessor.SaveChangesAsync();

            await AddLeaveLog(leave, leavePersonalDto, emp, cate?.CateName);
            return leave.LeaveID;
        }

        private async Task AddLeaveLog(LeaveData leave, LeavePersonalDto leavePersonalDto, Employee emp, string cateName)
        {
            Users user = await _repoAccessor.Users.FirstOrDefaultAsync(x => x.UserID == leave.UserID);
            LeaveLog leaveLog = new()
            {
                AddedByEmpName = user?.FullName?.Trim().ToUpper() ?? "",
                AddedByEmpNumber = user?.UserName?.Trim() ?? "",
                CateName = cateName?.Trim(),
                EmpName = emp.EmpName?.Trim(),
                EmpNumber = emp.EmpNumber?.Trim(),
                LeaveID = leave.LeaveID,
                LeaveType = leavePersonalDto.LeaveType?.Trim(),
                RequestDate = _serviceCommon.GetServerTime(),
                LoggedByIP = leavePersonalDto.IpLocal
            };
            _repoAccessor.LeaveLog.Add(leaveLog);
            await _repoAccessor.SaveChangesAsync();
        }

        public async Task AddCount(DateTime from, DateTime to, Employee emp)
        {
            List<DateTime> days = EachDays(from, to);

            foreach (DateTime dt in days)
            {
                DateTime day = dt.AddDays(1);

                CountPart part = await _repoAccessor.CountPart.FirstOrDefaultAsync(q => q.Count_Date >= dt && q.Count_Date < day && q.Count_PartID == emp.PartID);
                if (part != null)
                {
                    part.Count_Apply += 1;
                    _repoAccessor.CountPart.Update(part);
                }

                CountDepartment dept = await _repoAccessor.CountDepartment.FirstOrDefaultAsync(q => q.Count_Date >= dt && q.Count_Date < day && q.Count_DeptID == emp.Part.DeptID);
                if (dept != null)
                {
                    dept.Count_Apply += 1;
                    _repoAccessor.CountDepartment.Update(dept);
                }

                CountBuilding build = await _repoAccessor.CountBuilding.FirstOrDefaultAsync(q => q.Count_Date >= dt && q.Count_Date < day && q.Count_BuildID == emp.Part.Dept.BuildingID);
                if (build != null)
                {
                    build.Count_Apply += 1;
                    _repoAccessor.CountBuilding.Update(build);
                }

                CountArea area = await _repoAccessor.CountArea.FirstOrDefaultAsync(q => q.Count_Date >= dt && q.Count_Date < day && q.Count_AreaID == emp.Part.Dept.AreaID);
                if (area != null)
                {
                    area.Count_Apply += 1;
                    _repoAccessor.CountArea.Update(area);
                }

                CountAll all = await _repoAccessor.CountAll.FirstOrDefaultAsync(q => q.Count_Date >= dt && q.Count_Date < day && q.Count_ComID == emp.Part.Dept.Building.Area.CompanyID);
                if (all != null)
                {
                    all.Count_Apply += 1;
                    _repoAccessor.CountAll.Update(all);
                }
            }
            await _repoAccessor.SaveChangesAsync();
        }

        public List<DateTime> EachDays(DateTime from, DateTime to)
        {
            return Enumerable.Range(0, 1 + to.Subtract(from).Days).Select(i => from.AddDays(i)).ToList();
        }

        /// <summary>
        /// Ghi dữ liệu cho report
        /// Gởi đơn mới => index = 0;
        /// Chủ quản approve => index = 1 ;
        /// </summary>
        /// <param name="index"></param>
        /// <param name="allDay"></param>
        /// <param name="emp"></param>
        /// <param name="leaveID"></param>
        /// <returns></returns>
        public async Task<bool> RecordForReport(int index, List<DateTime> allDay, Employee emp, int leaveID)
        {
            /*
             index = 0 => Gởi đơn xin nghỉ phép
             index = 1 => Chủ quản duyệt
             */

            //Duyệt qua tất cả những ngày mà user xin nghỉ và insert vào table ReportData
            foreach (var day in allDay)
            {
                ReportData reportdata = new()
                {
                    LeaveID = leaveID,
                    LeaveDate = day,
                    DayOfWeek = (int)day.DayOfWeek,
                    StatusLine = index == 0 ? 1 : 2,
                    EmpID = emp.EmpID,

                    //Lấy tổng số nhân viên thuộc Part mà Employee đang xin nghỉ phép
                    PartTotalEmpByEmp = await _repoAccessor.Employee.CountAsync(x => x.Visible == true && x.PartID == emp.PartID),
                    //Lấy tổng số nhân viên thuộc Department của Employee đang xin nghỉ phép
                    DeptTotalEmpByEmp = await _repoAccessor.Employee.CountAsync(x => x.Visible == true && x.Part.Dept.DeptID == emp.Part.Dept.DeptID),
                    //Lấy tổng số nhân viên thuộc Building của Employee đang xin nghỉ phép
                    BuildingTotalEmpByEmp = await _repoAccessor.Employee.CountAsync(x => x.Visible == true && x.Part.Dept.Building.BuildingID == emp.Part.Dept.BuildingID),
                    //Lấy tổng số nhân viên thuộc Area của Employee đang xin nghỉ phép
                    AreaTotalEmpByEmp = await _repoAccessor.Employee.CountAsync(x => x.Visible == true && x.Part.Dept.AreaID == emp.Part.Dept.AreaID),
                    //Tổng số nhân viên cả Công ty tại thời điểm xin nghỉ phép
                    CompTotalEmp = await _repoAccessor.Employee.CountAsync(x => x.Visible == true),
                    MPPoolIn = 0,
                    MPPoolOut = 0
                };
                _repoAccessor.ReportData.Add(reportdata);
            }
            return await _repoAccessor.SaveChangesAsync();
        }

        public string ReturnArchive(string empnumber, string shift)
        {
            if (string.IsNullOrEmpty(shift))
                return $"E{empnumber}.{DateTime.Now.ToString("dd-MM-yy").Replace("-", "")}-001";

            return $"E{empnumber}.{DateTime.Now.ToString("dd-MM-yy").Replace("-", "")}-001-{shift}";
        }

        public async Task<HistoryEmployeeDto> GetHistoryEmployee(int empId, int year)
        {
            HistoryEmp emp = await _repoAccessor.HistoryEmp.FirstOrDefaultAsync(q => q.YearIn == year && q.EmpID == empId);
            HistoryEmployeeDto history = new();
            if (emp != null)
            {
                //Phép năm
                history.TotalDay = Math.Round((decimal)emp.TotalDay, 6).ToString();
                //Phép năm đã nghỉ
                history.CountTotal = emp.CountTotal.ToString();
                //Tổng phép đã nghỉ
                history.CountLeave = emp.CountLeave.ToString();
                //Phép năm cá nhân
                history.CountAgent = emp.CountAgent + "/" + Math.Round((decimal)emp.Agent, 6).ToString();
                //Phép năm công ty
                history.CountArran = emp.CountArran + "/" + Math.Round((decimal)emp.Arrange, 6).ToString();
                //Phép cá nhân chưa nghỉ
                history.CountRestAgent = !emp.CountRestAgent.HasValue
                                            ? Math.Round((decimal)(emp.Agent - emp.CountAgent), 6).ToString()
                                            : Math.Round((decimal)emp.CountRestAgent, 6).ToString();
                //Phép năm công ty chưa nghỉ
                history.CountRestArran = !emp.CountRestArran.HasValue
                                            ? Math.Round((decimal)(emp.Arrange - emp.CountArran), 6).ToString()
                                            : Math.Round((decimal)emp.CountRestArran, 6).ToString();
            }
            else
            {
                history.TotalDay = "0";
                history.CountTotal = "0";
                history.CountLeave = "0";
                history.CountAgent = "0/0";
                history.CountArran = "0/0";
                history.CountRestAgent = "0";
                history.CountRestArran = "0";
            }
            return history;
        }

        public async Task<List<KeyValuePair<int, string>>> GetAllCategory(string language)
        {
            language = (language == "zh") ? "zh-TW" : language;
            var data = await _repoAccessor.Category.FindAll(x => x.Visible == true)
            .Join(_repoAccessor.CatLang.FindAll(x => x.LanguageID == language),
                x => x.CateID,
                y => y.CateID,
                (x, y) => new { Category = x, CatLang = y }
            )
            .Select(x => new KeyValuePair<int, string>
            (
                x.Category.CateID,
                $"{x.Category.CateSym} - {x.CatLang.CateName}"
            )).ToListAsync();
            return data;
        }

        public async Task<double?> GetCountRestAgent(int empId, int year)
        {
            return await _repoAccessor.HistoryEmp.FindAll(x => x.YearIn == year && x.EmpID == empId).Select(x => x.CountRestAgent).FirstOrDefaultAsync();
        }

        public async Task<List<HolidayDto>> GetListHoliday()
        {
            List<HolidayDto> data = await _repoAccessor.Holiday.FindAll()
            .Select(
                x => new HolidayDto
                {
                    Day = x.Date.Value.Day,
                    Month = x.Date.Value.Month,
                    Year = x.Date.Value.Year
                }
            ).ToListAsync();
            return data;
        }

        public async Task<string> CheckDateLeave(string start, string end, int empid)
        {
            DateTime d1 = Convert.ToDateTime(start);
            DateTime d2 = Convert.ToDateTime(end);

            IQueryable<LeaveData> data = _repoAccessor.LeaveData.FindAll(
                x => x.Status_Line == true &&
                x.Approved != 3 &&
                x.EmpID == empid &&
                ((x.Time_Start <= d1 && x.Time_End > d1) ||
                (x.Time_Start < d2 && x.Time_End >= d2) ||
                (x.Time_Start >= d1 && x.Time_End <= d2) ||
                (x.Time_Start <= d1 && x.Time_End >= d2)), true);

            if (data.Any())
                return await Task.FromResult("ERROR");

            DateTime d3 = Convert.ToDateTime(DateTime.Now.Year + "/" + "1" + "/1 " + "00:00");
            DateTime d4 = Convert.ToDateTime(DateTime.Now.Year + 1 + "/" + "1" + "/1 " + "00:00");
            IQueryable<LeaveData> leaveWaitAndLock = _repoAccessor.LeaveData.FindAll(
                x => x.Status_Line == true &&
                x.Status_Lock == true &&
                x.Approved == 1 &&
                x.Time_Start >= d3 &&
                x.Time_End <= d4 &&
                x.EmpID == empid, true);

            if (leaveWaitAndLock.Any())
                return await Task.FromResult("EXISTLEAVEWAITLOCK");

            return await Task.FromResult("SUCCESS");
        }

        private static string ReturnDay(string day)
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

        public async Task<LeaveDataViewModel> GetLeaveDataWithCategory(LeaveData leaveData)
        {
            LeaveDataViewModel leaveView = _mapper.Map<LeaveDataViewModel>(leaveData);
            leaveView.EmpName = (await _repoAccessor.Employee.FindById(leaveData.EmpID))?.EmpName;
            leaveView.LeaveDayReturn = ReturnDay(leaveData.LeaveDay.ToString());
            Category cate = await _repoAccessor.Category.FirstOrDefaultAsync(x => x.CateID == leaveData.CateID);
            List<CatLang> cateLang = await _repoAccessor.CatLang.FindAll(x => x.CateID == leaveData.CateID, true).ToListAsync();
            if (cateLang.Any())
            {
                leaveView.CategoryNameVN = $"{cate?.CateSym} - {cateLang.FirstOrDefault(x => x.LanguageID == LangConstants.VN)?.CateName}";
                leaveView.CategoryNameEN = $"{cate?.CateSym} - {cateLang.FirstOrDefault(x => x.LanguageID == LangConstants.EN)?.CateName}";
                leaveView.CategoryNameTW = $"{cate?.CateSym} - {cateLang.FirstOrDefault(x => x.LanguageID == LangConstants.ZH_TW).CateName}";
            }
            if (cate != null)
                leaveView.Exhibit = cate.exhibit;
            return leaveView;
        }

        public async Task<EmployeeDataDto> GetEmployeeData(int empId, int? userId = null)
        {
            EmployeeDataDto emp = await _repoAccessor.Employee.FindAll(x => x.EmpID == empId, true)
            .Include(x => x.Part)
                .ThenInclude(x => x.Dept)
            .Select(x => new EmployeeDataDto
            {
                EmpID = x.EmpID,
                EmpName = x.EmpName,
                EmpNumber = x.EmpNumber,
                Descript = x.Descript,
                DateIn = x.DateIn,
                PartID = x.PartID,
                PositionID = x.PositionID,
                GBID = x.GBID,
                Visible = x.Visible,
                DeptCode = x.Part.Dept.DeptCode,
                IsSun = x.IsSun
            }).FirstOrDefaultAsync();

            // get partname
            var partLangs = await _repoAccessor.PartLang.FindAll(x => x.PartID == emp.PartID, true).ToListAsync();
            emp.PartNameVN = partLangs.FirstOrDefault(x => x.LanguageID == LangConstants.VN)?.PartName;
            emp.PartNameEN = partLangs.FirstOrDefault(x => x.LanguageID == LangConstants.EN)?.PartName;
            emp.PartNameTW = partLangs.FirstOrDefault(x => x.LanguageID == LangConstants.ZH_TW)?.PartName;
            emp.DateInVN = emp.DateIn.Value.ToString("dd - MMMM - yyyy", CultureInfo.GetCultureInfo("vi"));
            emp.DateInEN = emp.DateIn.Value.ToString("dd - MMMM - yyyy", CultureInfo.GetCultureInfo("en"));
            emp.DateInTW = emp.DateIn.Value.ToString("dd - MMMM - yyyy", CultureInfo.GetCultureInfo("zh-TW"));

            // get positionname
            var posLangs = await _repoAccessor.PosLang.FindAll(x => x.PositionID == emp.PositionID, true).ToListAsync();
            emp.PositionNameVN = posLangs.FirstOrDefault(x => x.LanguageID == LangConstants.VN)?.PositionName;
            emp.PositionNameEN = posLangs.FirstOrDefault(x => x.LanguageID == LangConstants.EN)?.PositionName;
            emp.PositionNameTW = posLangs.FirstOrDefault(x => x.LanguageID == LangConstants.ZH_TW)?.PositionName;
            if (userId != null)
            {
                var user = await _repoAccessor.Users.FirstOrDefaultAsync(x => x.EmpID == emp.EmpID);
                emp.CheckUser = user != null && user.UserID == userId;
            }

            return emp;
        }

        public async Task LeaveLogClear()
        {
            var now = DateTime.Now;
            DateTime dateValidate = new(now.Year, now.Month, 1, 0, 0, 0);

            List<LeaveLog> logs = await _repoAccessor.LeaveLog.FindAll(x => x.RequestDate < dateValidate).ToListAsync();
            if (logs.Any())
            {
                _repoAccessor.LeaveLog.RemoveMultiple(logs);
                await _repoAccessor.SaveChangesAsync();
            }
        }

        public async Task<bool> CheckDataDatePicker()
        {
            var dataCheck = await _repoAccessor.DatePickerManager.FirstOrDefaultAsync(x => x.Type == 4);
            return dataCheck?.EnableMonthPrevious ?? false;
        }

        public async Task<WorkShiftDto> GetWorkShift(string shift)
        {
            WorkShiftDto result = await _repoAccessor.LunchBreak
                .FindAll(x => x.Key.Trim() == shift.Trim() && x.Visible == true, true)
                .Select(x => new WorkShiftDto
                {
                    Key = x.Key,
                    WorkTimeStart = new TimeDto { Hours = x.WorkTimeStart.Hours, Minutes = x.WorkTimeStart.Minutes, Seconds = x.WorkTimeStart.Seconds },
                    WorkTimeEnd = new TimeDto { Hours = x.WorkTimeEnd.Hours, Minutes = x.WorkTimeEnd.Minutes, Seconds = x.WorkTimeEnd.Seconds },
                    LunchTimeStart = new TimeDto { Hours = x.LunchTimeStart.Hours, Minutes = x.LunchTimeStart.Minutes, Seconds = x.LunchTimeStart.Seconds },
                    LunchTimeEnd = new TimeDto { Hours = x.LunchTimeEnd.Hours, Minutes = x.LunchTimeEnd.Minutes, Seconds = x.LunchTimeEnd.Seconds }
                }).FirstOrDefaultAsync();

            return result;
        }

        public async Task<List<WorkShiftDto>> GetWorkShifts()
        {
            List<WorkShiftDto> result = await _repoAccessor.LunchBreak
                .FindAll(x => x.Visible == true, true)
                .Select(x => new WorkShiftDto
                {
                    Key = x.Key,
                    WorkTimeStart = new TimeDto { Hours = x.WorkTimeStart.Hours, Minutes = x.WorkTimeStart.Minutes, Seconds = x.WorkTimeStart.Seconds },
                    WorkTimeEnd = new TimeDto { Hours = x.WorkTimeEnd.Hours, Minutes = x.WorkTimeEnd.Minutes, Seconds = x.WorkTimeEnd.Seconds },
                    LunchTimeStart = new TimeDto { Hours = x.LunchTimeStart.Hours, Minutes = x.LunchTimeStart.Minutes, Seconds = x.LunchTimeStart.Seconds },
                    LunchTimeEnd = new TimeDto { Hours = x.LunchTimeEnd.Hours, Minutes = x.LunchTimeEnd.Minutes, Seconds = x.LunchTimeEnd.Seconds }
                }).ToListAsync();

            return result;
        }
    }
}