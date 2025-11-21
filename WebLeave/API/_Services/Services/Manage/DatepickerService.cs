using API._Repositories;
using API._Services.Interfaces.Manage;
using API.Dtos.Manage.DatepickerManagement;
using API.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
namespace API._Services.Services.Manage
{
    public class DatepickerService : IDatepickerService
    {
        private readonly IRepositoryAccessor _repositoryAccessor;
        private readonly IMapper _mapper;
        private readonly ICommonService _commonService;

        public DatepickerService(IRepositoryAccessor repositoryAccessor, IMapper mapper, ICommonService commonService)
        {
            _repositoryAccessor = repositoryAccessor;
            _mapper = mapper;
            _commonService = commonService;
        }

        public async Task<List<DatepickerDto>> GetAll()
        {
            List<DatePickerManager> data =  await _repositoryAccessor.DatePickerManager.FindAll(true).ToListAsync();
            foreach (var item in data)
            {
                item.Description = item.Type == 1 ? "Manage.DatepickerManage.RequestLeaveMonthBefore" 
                                                  : item.Type == 2 ? "Manage.DatepickerManage.EditLeaveMonthBefore" 
                                                  : item.Type == 3 ? "Manage.DatepickerManage.RequestLeaveDayBefore"
                                                  : "Manage.DatepickerManage.RequestTakeAnnualLeave";
            }
            return _mapper.Map<List<DatepickerDto>>(data);
        }

        public async Task<OperationResult> UpdateDatepicker(DatepickerDto datepickerDto, int UserID)
        {
            DatePickerManager datepicker = await _repositoryAccessor.DatePickerManager.FirstOrDefaultAsync(x => x.Type == datepickerDto.Type);
            datepicker.EnableMonthPrevious = datepickerDto.EnableMonthPrevious;
            datepicker.UpdateTime = _commonService.GetServerTime();
            datepicker.UserID = UserID;
            _repositoryAccessor.DatePickerManager.Update(datepicker);
            if (await _repositoryAccessor.SaveChangesAsync())
            {
                 return new OperationResult(true,"Successfully update datepicker","Success");
            }
            else
            {
                 return new OperationResult(true,"Failed to update datepicker","Failed");
            }
        }
    }
}
