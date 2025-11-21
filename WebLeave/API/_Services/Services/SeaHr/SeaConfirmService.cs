using System.Data.SqlTypes;
using API._Repositories;
using API._Services.Interfaces.Common;
using API._Services.Interfaces.SeaHr;
using API.Dtos.Common;
using API.Dtos.SeaHr;
using API.Helpers.Enums;
using API.Helpers.Params;
using API.Helpers.Utilities;
using API.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SeaHr
{
    public class SeaConfirmService : ISeaConfirmService
    {
        private readonly IRepositoryAccessor _repositoryAccessor;
        private readonly MapperConfiguration _mapperConfiguration;
        private readonly ICommonService _serviceCommon;
        private readonly IFunctionUtility _functionUtility;

        public SeaConfirmService(
            IRepositoryAccessor repositoryAccessor,
            MapperConfiguration mapperConfiguration,
            ICommonService serviceCommon,
            IFunctionUtility functionUtility)
        {
            _mapperConfiguration = mapperConfiguration;
            _serviceCommon = serviceCommon;
            _functionUtility = functionUtility;
            _repositoryAccessor = repositoryAccessor;
        }

        public async Task<List<KeyValueUtility>> GetCategories()
        {
            List<Category> data = await _repositoryAccessor.Category.FindAll(x => x.Visible.Value).ToListAsync();
            List<CatLang> dataLangs = await _repositoryAccessor.CatLang.FindAll(x => data.Select(y => y.CateID).Contains(x.CateID.Value)).ToListAsync();
            List<KeyValueUtility> result = data.Select(x => new KeyValueUtility
            {
                Key = x.CateID,
                Value_en = x.CateSym + " - " + dataLangs.FirstOrDefault(y => y.LanguageID == LangConstants.EN && y.CateID == x.CateID)?.CateName.Trim(),
                Value_vi = x.CateSym + " - " + dataLangs.FirstOrDefault(y => y.LanguageID == LangConstants.VN && y.CateID == x.CateID)?.CateName.Trim(),
                Value_zh = x.CateSym + " - " + dataLangs.FirstOrDefault(y => y.LanguageID == LangConstants.ZH_TW && y.CateID == x.CateID)?.CateName.Trim(),
            }).ToList();

            return result;
        }

        public async Task<List<KeyValueUtility>> GetParts(int deptID)
        {
            List<Part> data = await _repositoryAccessor.Part.FindAll(x => x.Visible.Value && x.DeptID == deptID).ToListAsync();
            List<PartLang> dataLangs = await _repositoryAccessor.PartLang.FindAll(x => data.Select(y => y.PartID).Contains(x.PartID.Value)).ToListAsync();
            List<KeyValueUtility> result = data.Select(x => new KeyValueUtility
            {
                Key = x.PartID,
                Value_en = dataLangs.FirstOrDefault(y => y.LanguageID == LangConstants.EN && y.PartID == x.PartID)?.PartName.Trim(),
                Value_vi = dataLangs.FirstOrDefault(y => y.LanguageID == LangConstants.VN && y.PartID == x.PartID)?.PartName.Trim(),
                Value_zh = dataLangs.FirstOrDefault(y => y.LanguageID == LangConstants.ZH_TW && y.PartID == x.PartID)?.PartName.Trim(),
            }).ToList();

            return result;
        }

        public async Task<SeaConfirmSearchDto> Search(SeaConfirmParam param, PaginationParam pagination, bool isPaging = true)
        {
            SeaConfirmSearchDto result = new();
            var pred = PredicateBuilder.New<LeaveData>(x => x.Status_Line.Value && x.Approved == 2 && x.EditRequest == 0);

            if (param.PartID.HasValue && param.PartID.Value > 0)
                pred.And(x => x.Emp.PartID == param.PartID.Value);

            if (param.DeptID.HasValue && param.DeptID.Value > 0)
                pred.And(x => x.Emp.Part.DeptID == param.DeptID.Value);

            if (param.CateID.HasValue && param.CateID.Value > 0)
                pred.And(x => x.CateID == param.CateID.Value);

            if (!string.IsNullOrEmpty(param.EmpNumber?.Trim()))
                pred.And(x => x.Emp.EmpNumber.Trim() == param.EmpNumber.Trim());

            if (param.LeaveDay.HasValue && param.LeaveDay.Value > 0)
                pred.And(x => x.LeaveDay == param.LeaveDay.Value);

            DateTime formDate = param.FromDate != null ? Convert.ToDateTime(param.FromDate + " 00:00:00") : SqlDateTime.MinValue.Value;
            DateTime toDate = param.ToDate != null ? Convert.ToDateTime(param.ToDate + " 23:59:59") : SqlDateTime.MaxValue.Value;
            pred.And(x => x.Time_Start >= formDate && x.Time_End <= toDate);

            var source = await _repositoryAccessor.LeaveData.FindAll(pred)
                .Where(x => x.Emp != null && x.Emp.Part != null)
                .Include(x => x.Emp)
                    .ThenInclude(x => x.Part)
                .Include(x => x.Cate)
                .Select(x => new LeaveDataDto
                {
                    Approved = x.Approved,
                    ApprovedBy = x.ApprovedBy,
                    CateID = x.CateID,
                    Comment = x.Comment,
                    CommentArchive = x.CommentArchive,
                    CommentLeave = 0,
                    Created = x.Created,
                    DateLeave = x.DateLeave,
                    EditRequest = x.EditRequest,
                    EmpID = x.EmpID,
                    LeaveArchive = x.LeaveArchive,
                    LeaveDay = x.LeaveDay,
                    LeaveDayByString = ConvertLeaveDay(x.LeaveDay.ToString()),
                    LeaveID = x.LeaveID,
                    LeaveArrange = x.LeaveArrange,
                    LeavePlus = x.LeavePlus,
                    MailContent_Lock = x.MailContent_Lock,
                    Status_Line = x.Status_Line,
                    Status_Lock = x.Status_Lock,
                    Time_End = x.Time_End,
                    Time_Start = x.Time_Start,
                    Updated = x.Updated,
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
                    DateIn = x.Emp.DateIn.Value,
                    CateName_vi = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.VN).CateName,
                    CateName_en = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.EN).CateName,
                    CateName_zh = x.Cate.CatLangs.FirstOrDefault(x => x.LanguageID == LangConstants.ZH_TW).CateName,
                    PartName_vi = x.Emp.Part.PartLangs.FirstOrDefault(x => x.LanguageID == LangConstants.VN).PartName,
                    PartName_en = x.Emp.Part.PartLangs.FirstOrDefault(x => x.LanguageID == LangConstants.EN).PartName,
                    PartName_zh = x.Emp.Part.PartLangs.FirstOrDefault(x => x.LanguageID == LangConstants.ZH_TW).PartName
                }).OrderByDescending(x => x.Created).ToListAsync();

            PaginationUtility<LeaveDataDto> paginationResult = PaginationUtility<LeaveDataDto>.Create(source, pagination.PageNumber, pagination.PageSize, isPaging);
            result.LeaveData = paginationResult;

            var categories = await GetCategories();

            categories.ForEach(item => item.Optional = source.Where(x => x.CateID == item.Key).Count());

            result.CountEachCategory = categories;

            return result;
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

        public async Task<SeaConfirmEmpDetailDto> GetEmpDetail(int empID)
        {
            int currentYear = DateTime.Now.Year;
            HistoryEmpDto historyEmp = await _repositoryAccessor.HistoryEmp
                .FindAll(x => x.EmpID.Value == empID && x.YearIn == currentYear)
                .ProjectTo<HistoryEmpDto>(_mapperConfiguration)
                .FirstOrDefaultAsync();

            // Lấy lên top 5 phép của người đó đã xin trong năm
            List<LeaveDataDto> leaveData = await _repositoryAccessor.LeaveData
                .FindAll(x => x.EmpID.Value == empID && x.Status_Line.Value)
                .ProjectTo<LeaveDataDto>(_mapperConfiguration)
                .OrderByDescending(x => x.Created)
                .Take(5).ToListAsync();

            // Lấy lên top 5 phép đã xóa của người đó đã xin trong năm
            DateTime fromDate = Convert.ToDateTime($"{currentYear}/01/01 00:00:00");
            DateTime toDate = Convert.ToDateTime($"{currentYear}/12/31 23:59:59");
            List<LeaveDataDto> deletedLeave = await _repositoryAccessor.LeaveData
                .FindAll(x => x.EmpID.Value == empID && !x.Status_Line.Value && x.Updated >= fromDate && x.Updated <= toDate)
                .ProjectTo<LeaveDataDto>(_mapperConfiguration)
                .OrderByDescending(x => x.Created)
                .Take(5).ToListAsync();
            leaveData.AddRange(deletedLeave);

            foreach (var item in leaveData)
            {
                item.CateName_vi = await _functionUtility.GetCateName(LangConstants.VN, item.CateID.Value);
                item.CateName_en = await _functionUtility.GetCateName(LangConstants.EN, item.CateID.Value);
                item.CateName_zh = await _functionUtility.GetCateName(LangConstants.ZH_TW, item.CateID.Value);
                item.LeaveDayByString = ConvertLeaveDay(item.LeaveDay.ToString());
            }

            var result = new SeaConfirmEmpDetailDto
            {
                HistoryEmp = historyEmp,
                LeaveData = leaveData
            };

            return result;
        }

        public async Task<SeaConfirmEmpDetailDto> GetLeaveDeleteTopFive(int empID)
        {
            int currentYear = DateTime.Now.Year;
            HistoryEmpDto historyEmp = await _repositoryAccessor.HistoryEmp
                .FindAll(x => x.EmpID.Value == empID && x.YearIn == currentYear)
                .ProjectTo<HistoryEmpDto>(_mapperConfiguration)
                .FirstOrDefaultAsync();

            // Lấy lên top 5 phép đã xóa của người đó đã xin trong năm
            DateTime fromDate = Convert.ToDateTime($"{currentYear}/01/01 00:00:00");
            DateTime toDate = Convert.ToDateTime($"{currentYear}/12/31 23:59:59");
            List<LeaveDataDto> deletedLeave = await _repositoryAccessor.LeaveData
                .FindAll(x => x.EmpID.Value == empID && !x.Status_Line.Value && x.Updated >= fromDate && x.Updated <= toDate)
                .ProjectTo<LeaveDataDto>(_mapperConfiguration)
                .OrderByDescending(x => x.Created)
                .Take(5).ToListAsync();

            foreach (var item in deletedLeave)
            {
                item.CateName_vi = await _functionUtility.GetCateName(LangConstants.VN, item.CateID.Value);
                item.CateName_en = await _functionUtility.GetCateName(LangConstants.EN, item.CateID.Value);
                item.CateName_zh = await _functionUtility.GetCateName(LangConstants.ZH_TW, item.CateID.Value);
                item.LeaveDayByString = ConvertLeaveDay(item.LeaveDay.ToString());
            }

            var result = new SeaConfirmEmpDetailDto
            {
                HistoryEmp = historyEmp,
                LeaveData = deletedLeave
            };

            return result;
        }
        public async Task<OperationResult> Confirm(List<LeaveDataDto> data, string username)
        {
            List<LeaveData> leaveData = new();
            var _serverTime = _serviceCommon.GetServerTime();
            List<LeaveData> listLeaveData = await _repositoryAccessor.LeaveData.FindAll(x => data.Select(d => d.LeaveID).Contains(x.LeaveID), true).ToListAsync();
            List<CommentArchive> listComment = await _repositoryAccessor.CommentArchive.FindAll(true).ToListAsync();
            // Xét duyệt các item có EditRequest == 0
            data.ForEach(x => x.EditRequest = _repositoryAccessor.LeaveData.FirstOrDefault(y => y.LeaveID == x.LeaveID, true)?.EditRequest);
            data = data.Where(x => x.EditRequest == 0).ToList();

            // Lấy dữ liệu trong ListLeaveData
            foreach (var leaveItem in data)
            {
                LeaveData model = listLeaveData.FirstOrDefault(x => x.LeaveID == leaveItem.LeaveID);
                if (model != null && !model.Status_Lock.Value && model.Approved != 4)
                {
                    // Các điều kiện khi duyệt phép ở trang nhân sự nếu ko chọn thì không thêm comment, 
                    // còn nếu chọn thì vào bảng CommentArchive load comment theo id nhân sự chọn
                    //1   NLĐ có chứng từ ốm mà không xin phép
                    //2   Cán bộ quên ký phép
                    //3   Người đại diện xin phép trể
                    //4   Nộp chứng từ trễ
                    //5   Ký phép có chứng từ mà không có giấy tờ bù
                    //6   Ký nhầm việc riêng với việc riêng con ốm
                    //7   Phép ký sai giờ hoặc ngày
                    //8   Phép ký không đúng với thời gian thực nghỉ

                    if (leaveItem.CommentLeave != null && leaveItem.CommentLeave != 0)
                    {
                        var commentArchive = listComment.FirstOrDefault(x => x.Value == leaveItem.CommentLeave)?.Comment;

                        model.Comment += "-[" + _serverTime.ToString("dd/MM/yyyy HH:mm:ss") + "] '" + commentArchive + "' duocluutru " + username;
                        model.CommentArchive = commentArchive;
                    }
                    else
                        model.Comment += "-[" + _serverTime.ToString("dd/MM/yyyy HH:mm:ss") + "] duocluutru " + username;


                    model.Approved = 4;
                    // Trả EditRequest về 0
                    // EditRequest    = 0 => Chưa từng gởi yêu cầu sửa phép
                    // EditRequest    = 1 => Đã gởi yêu cầu sửa phép tới chủ quản
                    // EditRequest    = 2 => Đã gởi yêu cầu sửa phép tới nhân sự
                    model.EditRequest = 0;
                    model.Updated = _serverTime;
                    model.LeaveArrange = false;

                    leaveData.Add(model);
                }
            }

            _repositoryAccessor.LeaveData.UpdateMultiple(leaveData);
            await _repositoryAccessor.SaveChangesAsync();
            return new OperationResult(true);
        }

        public async Task<List<KeyValueUtility>> GetDepartments()
        {
            List<Department> data = await _repositoryAccessor.Department.FindAll(x => x.Visible.Value).ToListAsync();
            List<DetpLang> dataLangs = await _repositoryAccessor.DetpLang.FindAll(x => data.Select(y => y.DeptID).Contains(x.DeptID.Value)).ToListAsync();
            List<KeyValueUtility> result = data.Select(x => new KeyValueUtility
            {
                Key = x.DeptID,
                Value_en = x.DeptCode + " - " + dataLangs.FirstOrDefault(y => y.LanguageID == LangConstants.EN && y.DeptID == x.DeptID)?.DeptName.Trim(),
                Value_vi = x.DeptCode + " - " + dataLangs.FirstOrDefault(y => y.LanguageID == LangConstants.VN && y.DeptID == x.DeptID)?.DeptName.Trim(),
                Value_zh = x.DeptCode + " - " + dataLangs.FirstOrDefault(y => y.LanguageID == LangConstants.ZH_TW && y.DeptID == x.DeptID)?.DeptName.Trim(),
            }).ToList();

            return result;
        }

        public async Task<OperationResult> DownloadExcel(SeaConfirmParam param, PaginationParam pagination)
        {
            var data = await Search(param, pagination, false);
            // data.LeaveData.Result = data.LeaveData.Result.OrderBy(x => x.Time_Start).ToList();

            List<ExportExcelSeaHr> exportParams = new();

            foreach (var item in data.LeaveData.Result)
            {
                var export = new ExportExcelSeaHr
                {
                    PartCode = item.PartCode,
                    DeptName = $"{item.PartCode} - {item.PartName_vi} - {item.PartName_zh}",
                    Employee = item.EmpName,
                    NumberID = item.EmpNumber,
                    Category = $"{item.CateSym} - {item.CateName_vi} - {item.CateName_zh}",
                    TimeStart = item.Time_Start.Value.ToString("HH:mm"),
                    DateStart = item.Time_Start.Value.ToString("MM/dd/yyyy"),
                    TimeEnd = item.Time_End.Value.ToString("HH:mm"),
                    DateEnd = item.Time_End.Value.ToString("MM/dd/yyyy"),
                    LeaveDay = item.LeaveDay.ToString(),
                    Status = GetStatus(item.Approved.Value, param),
                    Update = item.Updated.Value.ToString("HH:mm MM/dd/yyyy"),
                };
                exportParams.Add(export);
            }

            List<Table> dataTable = new()
            {
                new Table("result", exportParams)
            };

            List<Cell> dataTitle = new()
            {
                new("A1", param.Label_PartCode),
                new("B1", param.Label_DeptName),
                new("C1", param.Label_Employee),
                new("D1", param.Label_NumberID),
                new("E1", param.Label_Category),
                new("F1", param.Label_TimeStart),
                new("G1", param.Label_DateStart),
                new("H1", param.Label_TimeEnd),
                new("I1", param.Label_DateEnd),
                new("J1", param.Label_LeaveDay),
                new("K1", param.Label_Status),
                new("L1", param.Label_UpdateTime),
            };

            ExcelResult excelResult = ExcelUtility.DownloadExcel(dataTable, dataTitle, "Resources\\Template\\SeaHr\\SeaConfirmTemplate.xlsx");
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }

        public async Task<OperationResult> DownloadExcel1(SeaConfirmParam param, PaginationParam pagination)
        {
            var data = await Search(param, pagination, false);
            data.LeaveData.Result = data.LeaveData.Result.OrderBy(x => x.Time_Start).ToList();

            List<ExportExcelSeaHr> exportParams = new();
            foreach (var item in data.LeaveData.Result)
            {
                var export = new ExportExcelSeaHr
                {
                    PartCode = item.PartCode,
                    DeptName = $"{item.PartCode} - {item.PartName_vi} - {item.PartName_zh}",
                    Employee = item.EmpName,
                    NumberID = item.EmpNumber,
                    Category = $"{item.CateSym} - {item.CateName_vi} - {item.CateName_zh}",
                    TimeStart = item.Time_Start.Value.ToString("HH:mm"),
                    DateStart = item.Time_Start.Value.ToString("MM/dd/yyyy"),
                    TimeEnd = item.Time_End.Value.ToString("HH:mm"),
                    DateEnd = item.Time_End.Value.ToString("MM/dd/yyyy"),
                    LeaveDay = item.LeaveDay.ToString(),
                    Status = GetStatus(item.Approved.Value, param),
                    Update = item.Updated.Value.ToString("HH:mm MM/dd/yyyy"),
                };
                exportParams.Add(export);
            }

            ExcelResult excelResult = ExcelUtility.DownloadExcel(exportParams, "Resources\\Template\\SeaHr\\SeaConfirmTemplate.xlsx");
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }
        private static string GetStatus(int status, SeaConfirmParam param)
        {
            return status switch
            {
                1 => param.Status1,
                2 => param.Status2,
                3 => param.Status3,
                _ => param.Status4,
            };
        }
    }
}