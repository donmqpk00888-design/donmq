using API._Repositories;
using API._Services.Interfaces.Common;
using API._Services.Interfaces.Leave;
using API.Dtos.Leave.LeaveApprove;
using API.Helpers.Enums;
using API.Helpers.Utilities;
using API.Models;
using AutoMapper;
using LinqKit;
using Microsoft.EntityFrameworkCore;
namespace API._Services.Services.Leave
{
    public class LeaveApproveService : ILeaveApproveService
    {
        private readonly IRepositoryAccessor _accessorRepo;
        private readonly IMapper _mapper;
        private readonly IMailUtility _mailUtility;
        private readonly ILeaveCommonService _leaveCommonService;
        private readonly ICommonService _commonService;
        private readonly IFunctionUtility _functionUtility;

        public LeaveApproveService(
            IRepositoryAccessor accessorRepo,
            IMapper mapper,
            IMailUtility mailUtility,
            ILeaveCommonService leaveCommonService,
            ICommonService commonService,
            IFunctionUtility functionUtility)
        {
            _accessorRepo = accessorRepo;
            _mapper = mapper;
            _mailUtility = mailUtility;
            _leaveCommonService = leaveCommonService;
            _commonService = commonService;
            _functionUtility = functionUtility;
        }

        public async Task<OperationResult> ExportExcel(PaginationParam pagination, SearchLeaveApproveDto dto)
        {
            var data = await GetLeaveData(dto, pagination, false);
            var result = data.Result.Where(x => x.ApprovedBy == null)
            .GroupBy(p => new { p.EmpID, p.EmpNumber, p.EmpName })
            .Select(x => new LeaveDataApproveDto()
            {
                EmpNumber = x.Key.EmpNumber,
                EmpName = x.Key.EmpName
            }).OrderBy(x => x.EmpNumber).ToList();

            List<Table> dataTable = new()
            {
                new("result", result)
            };

            List<Cell> dataTitle = new()
            {
                new("A1", dto.Label_Employee),
                new("B1", dto.Label_Fullname),
            };

            ExcelResult excelResult = ExcelUtility.DownloadExcel(dataTable, dataTitle, "Resources\\Template\\Leave\\LeaveApprove.xlsx");
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }

        public async Task<List<KeyValuePair<int, string>>> GetCategory(string lang)
        {
            if (lang == "zh")
                lang = LangConstants.ZH_TW;

            List<KeyValuePair<int, string>> data = await _accessorRepo.Category.FindAll(x => x.Visible == true)
            .Include(x => x.CatLangs)
            .Select(x => new KeyValuePair<int, string>(
                x.CateID, $"{x.CateSym} - {x.CatLangs.FirstOrDefault(x => x.CateID == x.CateID && x.LanguageID == lang).CateName}"
            )).ToListAsync();

            return data;
        }

        public async Task<PaginationUtility<LeaveDataApproveDto>> GetLeaveData(SearchLeaveApproveDto paramsSearch, PaginationParam pagination, bool isPaging = true)
        {
            var predCheckLeaveData = PredicateBuilder.New<LeaveData>(x => x.Status_Line == true);
            if (!paramsSearch.OnView)
            {
                predCheckLeaveData.And(x => x.Approved == 1);
                if (paramsSearch.IsSearch)
                {
                    DateTime startDateTime = Convert.ToDateTime(paramsSearch.StartTime + " 00:00:00");
                    DateTime endDateTime = Convert.ToDateTime(paramsSearch.EndTime + " 23:59:59");

                    predCheckLeaveData
                        .And(x => (x.Time_Start <= startDateTime && x.Time_End > startDateTime) ||
                            (x.Time_Start < endDateTime && x.Time_End >= endDateTime) ||
                            (x.Time_Start >= startDateTime && x.Time_End <= endDateTime) ||
                            (x.Time_Start <= startDateTime && x.Time_End >= endDateTime));
                }
            }
            else
            {
                LeaveData leave = await _accessorRepo.LeaveData.FirstOrDefaultAsync(x => x.LeaveID == paramsSearch.LeaveID);

                DateTime startDateTime = Convert.ToDateTime(leave.Time_Start);
                DateTime endDateTime = Convert.ToDateTime(leave.Time_End);

                predCheckLeaveData.And(x => x.LeaveID != paramsSearch.LeaveID);
                predCheckLeaveData
                    .And(x => (x.Time_Start <= startDateTime && x.Time_End > startDateTime) ||
                        (x.Time_Start < endDateTime && x.Time_End >= endDateTime) ||
                        (x.Time_Start >= startDateTime && x.Time_End <= endDateTime) ||
                        (x.Time_Start <= startDateTime && x.Time_End >= endDateTime));
            }

            if (paramsSearch.CategoryId != 0)
                predCheckLeaveData.And(x => x.CateID == paramsSearch.CategoryId);
            if (!string.IsNullOrEmpty(paramsSearch.EmpNumber))
                predCheckLeaveData.And(x => x.Emp.EmpNumber == paramsSearch.EmpNumber);

            List<LeaveData> queries = await _accessorRepo.LeaveData
                .FindAll(predCheckLeaveData, true)
                .Include(x => x.Emp)
                    .ThenInclude(x => x.Part)
                    .ThenInclude(x => x.Dept)
                .Include(x => x.Emp.Part.Dept.Area)
                .Include(x => x.Emp.Part.Dept.Building)
                .ToListAsync();

            List<LeaveData> leaves = new();
            List<string> allows = await _functionUtility.CheckAllowData(paramsSearch.UserCurrent);
            foreach (var item in allows)
            {
                string key = item.Split("-")[0];
                string sym = item.Split("-")[1];
                if (key == "A")
                {
                    List<LeaveData> plus = queries.Where(q => q.Emp?.Part?.Dept?.Area?.AreaSym == sym).ToList();
                    leaves.AddRange(plus);
                }
                else if (key == "B")
                {
                    List<LeaveData> plus = queries.Where(q => q.Emp?.Part?.Dept?.Building?.BuildingSym == sym).ToList();
                    leaves.AddRange(plus);
                }
                else if (key == "D")
                {
                    List<LeaveData> plus = queries.Where(q => q.Emp?.Part?.Dept?.DeptSym == sym).ToList();
                    leaves.AddRange(plus);
                }
                else
                {
                    List<LeaveData> plus = queries.Where(q => q.Emp?.Part?.PartSym == sym).ToList();
                    leaves.AddRange(plus);
                }
            }

            List<int> groups = await _functionUtility.CheckGroupBase(paramsSearch.UserCurrent);
            if (groups.Any())
            {
                leaves = leaves.Where(q => q.Emp.GBID.HasValue && groups.Contains(q.Emp.GBID.Value)).ToList();
            }
            else
            {
                leaves = leaves.Where(q => q.Emp.GBID == 0).ToList();
            }

            // Get LeaveData
            List<int> leaveIds = leaves.Select(x => x.LeaveID).ToList();
            var result = await _accessorRepo.LeaveData
                .FindAll(x => leaveIds.Contains(x.LeaveID), true)
                .Include(x => x.Emp)
                    .ThenInclude(x => x.Part)
                    .ThenInclude(x => x.Dept)
                .Include(x => x.Emp.Part.Dept.Area)
                .Include(x => x.Emp.Part.Dept.Building)
                .Include(x => x.Cate)
                    .ThenInclude(x => x.CatLangs)
                .Include(x => x.Emp.Part.PartLangs)
                .Include(x => x.Emp.HistoryEmps.Where(x => x.YearIn == DateTime.Now.Year).OrderByDescending(x => x.Updated))
                .Select(x => new LeaveDataApproveDto
                {
                    EmpID = x.EmpID,
                    EmpNumber = x.Emp.EmpNumber,
                    GBID = x.Emp.GBID,
                    EmpName = x.Emp.EmpName,
                    AreaSym = x.Emp.Part.Dept.Area.AreaSym,
                    DeptSym = x.Emp.Part.Dept.DeptSym,
                    PartSym = x.Emp.Part.PartSym,
                    BuildingSym = x.Emp.Part.Dept.Building.BuildingSym,
                    PartID = x.Emp.Part.PartID,
                    CateSym = x.Cate.CateSym,
                    DeptCode = x.Emp.Part.Dept.DeptCode,
                    PartNameLang = x.Emp.Part.PartCode + " - " + x.Emp.Part.PartName,
                    Category = x.Cate.CateSym + " - " + x.Cate.CateName,
                    DateStartOrder = x.Created.Value,
                    TimeStart = x.Time_Start.Value.ToString("HH:mm"),
                    TimeEnd = x.Time_End.Value.ToString("HH:mm"),
                    DateStart = x.Time_Start.Value.ToString("dd/MM/yyyy"),
                    DateEnd = x.Time_End.Value.ToString("dd/MM/yyyy"),
                    Status = x.Approved == 1 ? "Chờ duyệt" : x.Approved == 2 ? "Đã duyệt" : x.Approved == 3 ? "Từ chối" : "Hoàn thành",
                    Status_Lock = x.Status_Lock.Value,
                    Lock_Leave = x.Comment.Contains("ycsuachua"),
                    Approved = x.Approved,
                    LeaveArchive = x.LeaveArchive,
                    Time_Start = x.Time_Start.Value,
                    Time_End = x.Time_End.Value,
                    PartCode = x.Emp.Part.PartCode,
                    DeptIDName = x.Emp.Part.Dept.DeptName,
                    CreatedString = x.Created.Value.ToString("HH:mm dd/MM/yyyy"),
                    LeaveDayString = Math.Round((double)x.LeaveDay, 5, MidpointRounding.AwayFromZero).ToString().Replace(",", "."),
                    LeaveHourString = Math.Round((double)x.LeaveDay * 8, 5, MidpointRounding.AwayFromZero).ToString().Replace(",", "."),
                    SearchDate = paramsSearch.StartTime + " - " + paramsSearch.EndTime,
                    Updated = x.Updated,
                    UpdatedString = x.Updated.Value.ToString("HH:mm dd/MM/yyyy"),
                    CommentArchive = x.CommentArchive,
                    PNC = !paramsSearch.OnView && x.Emp.HistoryEmps != null
                    ? Math.Round((double)(x.Emp.HistoryEmps.FirstOrDefault(a => a.EmpID == x.Emp.EmpID && a.YearIn == DateTime.Now.Year).CountRestAgent ?? 0), 5, MidpointRounding.AwayFromZero).ToString().Replace(",", ".")
                    : null,
                    CreatedOrderBy = (x.Created == x.Updated) ? x.Created : x.Updated,
                    Comment = x.Comment,
                    LeaveID = x.LeaveID,
                    CateID = x.CateID.Value,
                    DateLeave = x.DateLeave.Value,
                    LeaveDay = x.LeaveDay.Value,
                    Time_Applied = x.Time_Applied.Value,
                    TimeLine = x.TimeLine,
                    LeavePlus = x.LeavePlus.Value,
                    LeaveArrange = x.LeaveArrange ?? null,
                    UserID = x.UserID.Value,
                    ApprovedBy = x.ApprovedBy,
                    EditRequest = x.EditRequest ?? null,
                    Status_Line = x.Status_Line.Value,
                    Created = x.Created.Value,
                    MailContent_Lock = x.MailContent_Lock,
                    DeptNameLangEN = x.Emp.Part.Dept.DeptCode + " - " + x.Emp.Part.PartLangs.FirstOrDefault(y => y.PartID == y.Part.PartID && y.LanguageID == LangConstants.EN).PartName,
                    DeptNameLangZH = x.Emp.Part.Dept.DeptCode + " - " + x.Emp.Part.PartLangs.FirstOrDefault(y => y.PartID == y.Part.PartID && y.LanguageID == LangConstants.ZH_TW).PartName,
                    DeptNameLangVN = x.Emp.Part.Dept.DeptCode + " - " + x.Emp.Part.PartLangs.FirstOrDefault(y => y.PartID == y.Part.PartID && y.LanguageID == LangConstants.VN).PartName,
                    CategoryLangEN = x.Cate.CateSym + " - " + x.Cate.CatLangs.FirstOrDefault(y => y.CateID == y.Cate.CateID && y.LanguageID == LangConstants.EN).CateName,
                    CategoryLangZH = x.Cate.CateSym + " - " + x.Cate.CatLangs.FirstOrDefault(y => y.CateID == y.Cate.CateID && y.LanguageID == LangConstants.ZH_TW).CateName,
                    CategoryLangVN = x.Cate.CateSym + " - " + x.Cate.CatLangs.FirstOrDefault(y => y.CateID == y.Cate.CateID && y.LanguageID == LangConstants.VN).CateName,
                }).ToListAsync();

            var resultApprove = PaginationUtility<LeaveDataApproveDto>.Create(result.OrderByDescending(x => x.Updated).ToList(), pagination.PageNumber, pagination.PageSize, isPaging);
            return resultApprove;
        }

        public async Task<OperationResult> UpdateLeaveData(List<LeaveDataApproveDto> models, bool check)
        {
            var dataLeave = _accessorRepo.LeaveData.FindAll(x => models.Select(y => y.LeaveID).Contains(x.LeaveID), true);
            models.ForEach(x =>
            {
                x.Status_Line = dataLeave.FirstOrDefault(y => y.LeaveID == x.LeaveID).Status_Line;
                x.Updated = _commonService.GetServerTime();
                x.Time_Applied = _commonService.GetServerTime();

                // 2: đồng ý 3: từ chối
                // Approve bị từ chối thì bỏ qua 
                x.Approved = dataLeave.FirstOrDefault(y => y.LeaveID == x.LeaveID).Approved == 3 ? 3 : check ? 2 : 3;
            });

            // Chỉ xét duyệt các item có Status_Line = true
            models = models.Where(x => x.Status_Line == true).ToList();

            List<LeaveData> listLeave = _mapper.Map<List<LeaveDataApproveDto>, List<LeaveData>>(models);

            foreach (var leave in listLeave)
            {
                if (leave.Approved == 2)
                {
                    DateTime from = Convert.ToDateTime(leave.Time_Start);
                    DateTime to = Convert.ToDateTime(leave.Time_End);

                    Employee emp = await _accessorRepo.Employee
                        .FindAll(x => x.EmpID == leave.EmpID)
                        .Include(x => x.Part.Dept.Building.Area)
                        .Include(x => x.Part.Dept.Area)
                        .FirstOrDefaultAsync();
                    // Lấy ra tất cả các ngày giữa 2 from và to
                    List<DateTime> allday = _leaveCommonService.EachDays(from, to);
                    // Tạo dữ liệu cho report
                    await _leaveCommonService.RecordForReport(1, allday, emp, leave.LeaveID);
                    await SendNotitoUser(leave.EmpID, leave.LeaveID, "được cấp trên phê duyệt.<br/>");
                }
                else
                {
                    // Nếu dữ liệu xin nghỉ phép chưa bị từ chối trước đó
                    if (dataLeave.FirstOrDefault(y => y.LeaveID == leave.LeaveID).Approved != 3)
                    {
                        var cateLeave = _accessorRepo.Category.FirstOrDefault(x => x.CateID == leave.CateID);
                        if (cateLeave.CateSym.Equals("U") || cateLeave.CateSym.Equals("J"))
                        {

                            HistoryEmp hisemp = _accessorRepo.HistoryEmp.FirstOrDefault(q => q.YearIn == leave.Time_Start.Value.Year && q.EmpID == leave.EmpID);
                            if (hisemp.CountLeave > 0)
                                hisemp.CountLeave -= leave.LeaveDay;

                            if (hisemp.CountTotal > 0)
                                hisemp.CountTotal -= leave.LeaveDay;

                            if (cateLeave.CateSym == "J" && hisemp.CountAgent > 0)
                            {
                                hisemp.CountAgent -= leave.LeaveDay;
                                hisemp.CountRestAgent += leave.LeaveDay;
                            }
                            else if (cateLeave.CateSym == "U" && hisemp.CountArran > 0)
                            {
                                hisemp.CountArran -= leave.LeaveDay;
                                hisemp.CountRestArran += leave.LeaveDay;
                            }

                            hisemp.Updated = _commonService.GetServerTime();
                            _accessorRepo.HistoryEmp.Update(hisemp);
                        }
                        var model = models.FirstOrDefault(x => x.EmpID == leave.EmpID);
                        await SendNotitoUser(leave.EmpID, leave.LeaveID, $"bị từ chối.<br/> Lý do: {model.CommentLeave}.<br/>");
                    }
                }
            }

            if (listLeave.Any())
                _accessorRepo.LeaveData.UpdateMultiple(listLeave);

            try
            {
                await _accessorRepo.SaveChangesAsync();
                return new OperationResult(true, MessageConstants.UPDATE_SUCCESS, MessageConstants.SUCCESS);
            }
            catch
            {
                return new OperationResult(false, MessageConstants.UPDATE_ERROR, MessageConstants.ERROR);
            }
        }

        public async Task<string> SendNotitoUser(int? empid, int leaveID, string status)
        {
            string titlesmtp = EmailContentContants.title;
            string displaynamesmtp = EmailContentContants.displayname;
            string contentsmtp = EmailContentContants.ApprovedBySuperiorContent;

            var mailto = await _accessorRepo.Users.FindAll(q => q.EmpID == empid && q.Visible == true).FirstOrDefaultAsync();

            // Truyền mã đơn xin phép động vào trong Email Content
            var body = contentsmtp.Replace("xxxxxxxx", leaveID.ToString());
            body = body.Replace("được cấp trên phê duyệt.<br/>", status);
            if (!string.IsNullOrWhiteSpace(mailto.EmailAddress))
            {
                try
                {
                    await _mailUtility.SendMailAsync(mailto.EmailAddress.ToString(), titlesmtp, body, "");
                    return "smtp ok";
                }
                catch (Exception ex)
                {
                    return "smtp false: " + ex;
                }
            }
            return "smtp false";
        }
    }
}