using API._Repositories;
using API._Services.Interfaces.Common;
using API._Services.Interfaces.Manage;
using API.Dtos.Common;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.Manage
{
    public class LunchBreakService : ILunchBreakService
    {
        private readonly IRepositoryAccessor _repositoryAccessor;
        private readonly ICommonService _commonService;

        public LunchBreakService(IRepositoryAccessor repositoryAccessor, ICommonService commonService)
        {
            _repositoryAccessor = repositoryAccessor;
            _commonService = commonService;
        }

        public async Task<OperationResult> Create(LunchBreakDto dto)
        {
            if (await _repositoryAccessor.LunchBreak.AnyAsync(x => x.Key.Trim() == dto.Key))
                return new OperationResult { IsSuccess = false, Error = "System.Message.DuplicateMsg" };

            LunchBreak data = new()
            {
                Id = dto.Id,
                Key = dto.Key,
                WorkTimeStart = TimeSpan.Parse(dto.WorkTimeStart as string),
                WorkTimeEnd = TimeSpan.Parse(dto.WorkTimeEnd as string),
                LunchTimeStart = TimeSpan.Parse(dto.LunchTimeStart as string),
                LunchTimeEnd = TimeSpan.Parse(dto.LunchTimeEnd as string),
                Value_en = dto.Value_en,
                Value_vi = dto.Value_vi,
                Value_zh = dto.Value_zh,
                Seq = dto.Seq,
                Visible = dto.Visible,
                CreatedBy = dto.CreatedBy,
                CreatedTime = _commonService.GetServerTime()
            };

            try
            {
                _repositoryAccessor.LunchBreak.Add(data);
                await _repositoryAccessor.SaveChangesAsync();

                return new OperationResult { IsSuccess = true };
            }
            catch
            {
                return new OperationResult { IsSuccess = false, Error = "System.Message.CreateErrorMsg" };
            }
        }

        public async Task<OperationResult> Delete(int Id)
        {
            var data = await _repositoryAccessor.LunchBreak.FirstOrDefaultAsync(x => x.Id == Id);
            if (data is null)
                return new OperationResult { IsSuccess = false, Error = "Data not existed !" };

            _repositoryAccessor.LunchBreak.Remove(data);
            return new OperationResult { IsSuccess = await _repositoryAccessor.SaveChangesAsync() };
        }

        public async Task<PaginationUtility<LunchBreakDto>> GetDataPagination(PaginationParam pagination, bool isPaging)
        {
            var data = _repositoryAccessor.LunchBreak.FindAll(true)
                .Select(x => new LunchBreakDto
                {
                    Id = x.Id,
                    Key = x.Key,
                    WorkTimeStart = x.WorkTimeStart.ToString(@"hh\:mm"),
                    WorkTimeEnd = x.WorkTimeEnd.ToString(@"hh\:mm"),
                    LunchTimeStart = x.LunchTimeStart.ToString(@"hh\:mm"),
                    LunchTimeEnd = x.LunchTimeEnd.ToString(@"hh\:mm"),
                    Value_en = x.Value_en,
                    Value_vi = x.Value_vi,
                    Value_zh = x.Value_zh,
                    Seq = x.Seq,
                    Visible = x.Visible,
                    CreatedTime = x.CreatedTime,
                    UpdatedTime = x.UpdatedTime
                }).OrderBy(x => x.Seq);

            return await PaginationUtility<LunchBreakDto>.CreateAsync(data, pagination.PageNumber, pagination.PageSize, isPaging);
        }

        public async Task<LunchBreakDto> GetDetail(int Id)
        {
            var data = await _repositoryAccessor.LunchBreak
                .FindAll(x => x.Id == Id)
                .Select(x => new LunchBreakDto
                {
                    Id = x.Id,
                    Key = x.Key,
                    WorkTimeStart = new DateTime(x.WorkTimeStart.Ticks),
                    WorkTimeEnd = new DateTime(x.WorkTimeEnd.Ticks),
                    LunchTimeStart = new DateTime(x.LunchTimeStart.Ticks),
                    LunchTimeEnd = new DateTime(x.LunchTimeEnd.Ticks),
                    Value_en = x.Value_en,
                    Value_vi = x.Value_vi,
                    Value_zh = x.Value_zh,
                    Seq = x.Seq,
                    Visible = x.Visible,
                    CreatedTime = x.CreatedTime,
                    UpdatedTime = x.UpdatedTime
                }).FirstOrDefaultAsync();

            return data;
        }

        public async Task<List<LunchBreakDto>> GetListLunchBreak()
        {
            var data = await _repositoryAccessor.LunchBreak
                .FindAll(x => x.Visible == true, true)
                .Select(x => new LunchBreakDto
                {
                    Id = x.Id,
                    Key = x.Key,
                    WorkTimeStart = x.WorkTimeStart.ToString(@"hh\:mm"),
                    WorkTimeEnd = x.WorkTimeEnd.ToString(@"hh\:mm"),
                    LunchTimeStart = x.LunchTimeStart.ToString(@"hh\:mm"),
                    LunchTimeEnd = x.LunchTimeEnd.ToString(@"hh\:mm"),
                    Value_en = x.Value_en,
                    Value_vi = x.Value_vi,
                    Value_zh = x.Value_zh,
                    Seq = x.Seq,
                    Visible = x.Visible,
                    CreatedTime = x.CreatedTime,
                    UpdatedTime = x.UpdatedTime
                })
                .OrderBy(x => x.Seq).ToListAsync();

            return data;
        }

        public async Task<OperationResult> Update(LunchBreakDto dto)
        {
            var data = await _repositoryAccessor.LunchBreak.FindAll().ToListAsync();
            if (data.FirstOrDefault(x => x.Key == dto.Key) is not null)
                return new OperationResult { IsSuccess = false, Error = "System.Message.DuplicateMsg" };
            var item = data.FirstOrDefault(x => x.Id == dto.Id);
            if (item is null)
                return new OperationResult { IsSuccess = false, Error = "'System.Message.UpdateErrorMsg'" };

            item.Key = dto.Key;
            item.WorkTimeStart = TimeSpan.Parse(dto.WorkTimeStart as string);
            item.WorkTimeEnd = TimeSpan.Parse(dto.WorkTimeEnd as string);
            item.LunchTimeStart = TimeSpan.Parse(dto.LunchTimeStart as string);
            item.LunchTimeEnd = TimeSpan.Parse(dto.LunchTimeEnd as string);
            item.Value_en = dto.Value_en;
            item.Value_vi = dto.Value_vi;
            item.Value_zh = dto.Value_zh;
            item.Seq = dto.Seq;
            item.Visible = dto.Visible;
            item.UpdatedBy = dto.UpdatedBy;
            item.UpdatedTime = _commonService.GetServerTime();

            try
            {
                _repositoryAccessor.LunchBreak.Update(item);
                await _repositoryAccessor.SaveChangesAsync();

                return new OperationResult { IsSuccess = true };
            }
            catch
            {
                return new OperationResult { IsSuccess = false, Error = "System.Message.UpdateErrorMsg" };
            }
        }
    }
}