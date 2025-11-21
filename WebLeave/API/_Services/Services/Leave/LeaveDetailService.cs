using API._Repositories;
using API._Services.Interfaces.Common;
using API._Services.Interfaces.Leave;
using API.Dtos.Common;
using API.Helpers.Enums;
using API.Helpers.Utilities;
using API.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
namespace API._Services.Services.Leave
{
    public class LeaveDetailService : ILeaveDetailService
    {
        private readonly IRepositoryAccessor _repositoryAccessor;
        private readonly IFunctionUtility _functionUtility;
        private readonly ICommonService _commonService;
        private readonly INotification _notification;

        public LeaveDetailService(
            IRepositoryAccessor repositoryAccessor,
            IFunctionUtility functionUtility,
            ICommonService commonService,
            INotification notification)
        {
            _repositoryAccessor = repositoryAccessor;
            _functionUtility = functionUtility;
            _commonService = commonService;
            _notification = notification;
        }

        public async Task<OperationResult> EditApproval(string userName, int userID, int empId, int? slEditApproval, int leaveID, string notiText)
        {
            if (userName != null)
            {
                LeaveData leaveData = await _repositoryAccessor.LeaveData
                    .FindAll(x => x.LeaveID == leaveID && x.EmpID == empId).AsNoTracking()
                    .Include(x => x.Cate)
                    .FirstOrDefaultAsync();

                HistoryEmp historyEmp = _repositoryAccessor.HistoryEmp.FirstOrDefault(x => x.YearIn == leaveData.Time_Start.Value.Year && x.EmpID == empId);
                if (slEditApproval == 0)
                {
                    leaveData.Approved = 1;
                    leaveData.Status_Lock = true;
                    leaveData.Comment += "-[" + _commonService.GetServerTime().ToString("dd/MM/yyyy HH:mm:ss") + "] suadoichoduyet " + userName;
                }
                else if (slEditApproval == 1)
                {
                    leaveData.Approved = 3;
                    leaveData.Comment += "-[" + _commonService.GetServerTime().ToString("dd/MM/yyyy HH:mm:ss") + "] suadoituchoi " + userName;
                    if (historyEmp.CountLeave > 0)
                        historyEmp.CountLeave -= leaveData.LeaveDay;

                    if (historyEmp.CountTotal > 0)
                        historyEmp.CountTotal -= leaveData.LeaveDay;

                    if (leaveData.Cate.CateSym == "J" && historyEmp.CountAgent > 0)
                    {
                        historyEmp.CountAgent -= leaveData.LeaveDay;
                        historyEmp.CountRestAgent += leaveData.LeaveDay;
                    }
                    else if (leaveData.Cate.CateSym == "U" && historyEmp.CountArran > 0)
                    {
                        historyEmp.CountArran -= leaveData.LeaveDay;
                        historyEmp.CountRestArran += leaveData.LeaveDay;
                    }

                    historyEmp.Updated = _commonService.GetServerTime();
                    _repositoryAccessor.HistoryEmp.Update(historyEmp);
                }
                else
                {
                    leaveData.Approved = 2;
                    leaveData.Comment += "-[" + _commonService.GetServerTime().ToString("dd/MM/yyyy HH:mm:ss") + "] suadoichapnhan " + userName;
                    historyEmp.CountLeave += leaveData.LeaveDay;

                    if (leaveData.Cate.CateSym == "J")
                    {
                        historyEmp.CountAgent += leaveData.LeaveDay;
                        historyEmp.CountTotal += leaveData.LeaveDay;
                    }
                    else if (leaveData.Cate.CateSym == "U")
                    {
                        historyEmp.CountArran += leaveData.LeaveDay;
                        historyEmp.CountTotal += leaveData.LeaveDay;
                    }
                }
                leaveData.ApprovedBy = userID;
                leaveData.Updated = _commonService.GetServerTime();
                _repositoryAccessor.LeaveData.Update(leaveData);
                await _repositoryAccessor.SaveChangesAsync();

                if (notiText.Trim() != string.Empty)
                {
                    await SendNotitoUser(empId, userName, notiText, leaveID);
                }

                return new OperationResult(true, "Edit Approval Successfully");
            }
            return new OperationResult(false, "Edit Approval Failed");

        }

        public async Task<OperationResult> SendNotitoUser(int? empid, string userName, string notitext, int? leaveid)
        {
            await _notification.SendNotitoUser(notitext, empid);

            LeaveData leaveData = await _repositoryAccessor.LeaveData.FirstOrDefaultAsync(x => x.LeaveID == leaveid);
            leaveData.Comment += "-[" + _commonService.GetServerTime().ToString("dd/MM/yyyy HH:mm:ss") + "] Gửi mail: `" + notitext + "` (" + userName + ")";
            leaveData.Updated = _commonService.GetServerTime();
            if (notitext.Trim() != string.Empty)
            {
                leaveData.MailContent_Lock = notitext;

            }
            await _repositoryAccessor.SaveChangesAsync();
            return new OperationResult(true, "Send Noti Successfully");
        }

        public async Task<OperationResult> EditCommentArchive(int userID, int leaveID, int commentArchiveID)
        {
            LeaveData itemLeaveData = await _repositoryAccessor.LeaveData.FirstOrDefaultAsync(x => x.LeaveID == leaveID);
            if (itemLeaveData != null)
            {
                CommentArchive commentArchive = await _repositoryAccessor.CommentArchive.FirstOrDefaultAsync(x => x.Value == commentArchiveID);
                itemLeaveData.Comment += "-[" + _commonService.GetServerTime() + "] '" + commentArchive.Comment + "' chinhsuacomment " + userID.ToString();
                itemLeaveData.CommentArchive = commentArchive.Comment;
            }
            if (await _repositoryAccessor.SaveChangesAsync())
            {
                return new OperationResult(true, "Edit Comment Archive Successfully");
            }
            return new OperationResult(true, "Edit Comment Archive Failed");
        }

        public async Task<LeaveDetailDto> GetDetail(int leaveID, int userID)
        {
            var leaveData = await _repositoryAccessor.LeaveData.FindAll(x => x.LeaveID == leaveID, true)
            .Include(x => x.Cate)
               .ThenInclude(x => x.CatLangs)
            .Include(x => x.Emp)
               .ThenInclude(x => x.Part)
                   .ThenInclude(x => x.Dept)
                       .ThenInclude(x => x.DetpLangs)
            .Select(x => new LeaveDataDto
            {
                Approved = x.Approved,
                ApprovedBy = x.ApprovedBy,
                CateID = x.CateID,
                Comment = x.Comment,
                CommentArchive = x.CommentArchive,
                Created = x.Created,
                DateLeave = x.DateLeave,
                EditRequest = x.EditRequest,
                EmpID = x.Emp.EmpID,
                LeaveArchive = x.LeaveArchive,
                LeaveDay = x.LeaveDay,
                LeaveDayByString = _functionUtility.ConvertLeaveDay(x.LeaveDay.ToString()),
                LeaveID = x.LeaveID,
                LeaveArrange = x.LeaveArrange,
                LeavePlus = x.LeavePlus,
                MailContent_Lock = x.MailContent_Lock,
                Status_Line = x.Status_Line,
                Status_Lock = x.Status_Lock,
                Time_End = x.Time_End,
                Time_Start = x.Time_Start,
                Updated = x.Updated,
                Category = x.Cate.CateName,
                Time_Applied = x.Time_Applied,
                TimeLine = x.TimeLine,
                UserID = x.UserID,
                EmpName = x.Emp.EmpName,
                EmpNumber = x.Emp.EmpNumber,
                PartCodeTruncate = x.Emp.Part.PartCode.Length > 7 ? x.Emp.Part.PartCode.Substring(0, 6) + "..." : x.Emp.Part.PartCode,
                PartCode = x.Emp.Part.PartCode,
                CateSym = x.Cate.CateSym,
                exhibit = x.Cate.exhibit,
                PartID = x.Emp.Part.PartID,
                DateIn = x.Emp.DateIn,
                DeptID = x.Emp.Part.Dept.DeptID,
                DeptCode = x.Emp.Part.Dept.DeptCode,
                CateNameVN = x.Cate.CatLangs.FirstOrDefault(y => y.CateID.Value == x.Cate.CateID && y.LanguageID == LangConstants.VN).CateName,
                CateNameEN = x.Cate.CatLangs.FirstOrDefault(y => y.CateID.Value == x.Cate.CateID && y.LanguageID == LangConstants.EN).CateName,
                CateNameZH = x.Cate.CatLangs.FirstOrDefault(y => y.CateID.Value == x.Cate.CateID && y.LanguageID == LangConstants.ZH_TW).CateName,
                DeptNameVN = x.Emp.Part.Dept.DetpLangs.FirstOrDefault(y => y.DeptID.Value == x.Emp.Part.Dept.DeptID && y.LanguageID == LangConstants.VN).DeptName,
                DeptNameEN = x.Emp.Part.Dept.DetpLangs.FirstOrDefault(y => y.DeptID.Value == x.Emp.Part.Dept.DeptID && y.LanguageID == LangConstants.EN).DeptName,
                DeptNameZH = x.Emp.Part.Dept.DetpLangs.FirstOrDefault(y => y.DeptID.Value == x.Emp.Part.Dept.DeptID && y.LanguageID == LangConstants.ZH_TW).DeptName
            }).FirstOrDefaultAsync();

            if (leaveData == null)
                return new LeaveDetailDto();

            var timeLunch = await _repositoryAccessor.LunchBreak.FindAll(x => x.Visible == true).ToListAsync();

            leaveData.LunchBreakVN = timeLunch.FirstOrDefault(y => y.Key == leaveData.LeaveArchive.Split("-").Last())?.Value_vi ?? "";
            leaveData.LunchBreakEN = timeLunch.FirstOrDefault(y => y.Key == leaveData.LeaveArchive.Split("-").Last())?.Value_en ?? "";
            leaveData.LunchBreakZH = timeLunch.FirstOrDefault(y => y.Key == leaveData.LeaveArchive.Split("-").Last())?.Value_zh ?? "";

            leaveData.SendBy = await _functionUtility.GetUsername(leaveData.UserID);
            leaveData.Sender = await _functionUtility.GetUsername(leaveData.ApprovedBy);

            Users user = await _repositoryAccessor.Users.FirstOrDefaultAsync(x => x.UserID == userID);

            string fullName = user.FullName.ToUpper().Replace(".", " ");
            int userRank = user.UserRank.Value;

            Employee employee = await _repositoryAccessor.Employee.FirstOrDefaultAsync(x => x.EmpID == leaveData.EmpID);
            List<string> approvalUsers = await _functionUtility.GetPersonApproval(employee);

            DateTime firstDateOfCurrentMonth = new(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime timeStart = Convert.ToDateTime($"{DateTime.Now.Year}/01/01 00:00:00");
            DateTime timeEnd = Convert.ToDateTime($"{DateTime.Now.Year}/12/31 23:59:59");

            bool enableMonthPrevious = await _repositoryAccessor.DatePickerManager
                .FindAll(x => x.Type == 2)
                .Select(x => x.EnableMonthPrevious.Value)
                .FirstOrDefaultAsync();

            bool checkApprovalCond1 = (
                userRank == 3 &&
                leaveData.Approved > 1 &&
                leaveData.Approved != 4 &&
                leaveData.Status_Line.Value &&
                leaveData.EmpID != user.EmpID &&
                approvalUsers.Contains(fullName));

            bool checkApprovalCond2 = (
                userRank >= 4 &&
                leaveData.Approved > 1 &&
                leaveData.Status_Line.Value &&
                leaveData.EmpID != user.EmpID);

            List<LeaveDeleteDetail> deletedLeaves = await _repositoryAccessor.LeaveData
                .FindAll(x => x.EmpID == leaveData.EmpID && !x.Status_Line.Value && x.Updated >= timeStart && x.Updated <= timeEnd)
                .Include(x => x.Cate.CatLangs)
                .OrderByDescending(x => x.Created)
                .Take(5)
                .Select(x => new LeaveDeleteDetail()
                {
                    Comment = x.Comment,
                    TimeLine = x.TimeLine,
                    CateNameEN = x.Cate.CatLangs.FirstOrDefault(c => c.LanguageID == LangConstants.EN && c.CateID == x.CateID).CateName ?? "",
                    CateNameVN = x.Cate.CatLangs.FirstOrDefault(c => c.LanguageID == LangConstants.VN && c.CateID == x.CateID).CateName ?? "",
                    CateNameZH = x.Cate.CatLangs.FirstOrDefault(c => c.LanguageID == LangConstants.ZH_TW && c.CateID == x.CateID).CateName ?? ""
                }).ToListAsync();

            LeaveDetailDto result = new()
            {
                RoleApproved = checkApprovalCond1 || checkApprovalCond2,
                NotiUser = userRank == 4 || userRank == 5,
                EditCommentArchive = userRank >= 4 && leaveData.Approved == 4 && leaveData.Status_Line.Value && leaveData.EmpID != user.EmpID,
                LeaveData = leaveData,
                ApprovalPersons = approvalUsers,
                EnablePreviousMonthEditRequest = !(leaveData.Time_End.Value < firstDateOfCurrentMonth && !enableMonthPrevious),
                DeletedLeaves = deletedLeaves
            };

            return result;
        }

        public async Task<OperationResult> RequestEditLeave(int? leaveID, string ReasonAdjust)
        {
            if (leaveID == null || leaveID <= 0)
                return new OperationResult(false);

            LeaveData leaveData = await _repositoryAccessor.LeaveData.FirstOrDefaultAsync(x => x.LeaveID == leaveID);
            if (leaveData == null)
                return new OperationResult(false);

            if (leaveData.Approved.Value == 2)
                leaveData.EditRequest = 1;

            if (leaveData.Approved.Value == 4)
                leaveData.EditRequest = 2;

            leaveData.ReasonAdjust = ReasonAdjust;
            leaveData.Comment += "-[" + _commonService.GetServerTime().ToString("dd/MM/yyyy HH:mm:ss") + "] ycsuachua" + $", lydo: {ReasonAdjust}";
            leaveData.Updated = _commonService.GetServerTime();

            _repositoryAccessor.LeaveData.Update(leaveData);
            await _repositoryAccessor.SaveChangesAsync();
            return new OperationResult(true);
        }
    }
}