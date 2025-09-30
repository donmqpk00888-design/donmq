using AgileObjects.AgileMapper;
using API.Data;
using API._Services.Interfaces.BasicMaintenance;
using API.DTOs.BasicMaintenance;
using API.Helper.Enums;
using API.Helper.SignalR;
using API.Models;
using LinqKit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.BasicMaintenance
{
    public class S_2_1_2_AccountAuthorizationSetting : BaseServices, I_2_1_2_AccountAuthorizationSetting
    {
        private readonly IHubContext<SignalRHub> _hubContext;

        public S_2_1_2_AccountAuthorizationSetting(DBContext dbContext,IHubContext<SignalRHub> hubContext) : base(dbContext)
        {
            _hubContext = hubContext;
        }

        public async Task<PaginationUtility<AccountAuthorizationSetting_Data>> GetDataPagination(PaginationParam pagination, AccountAuthorizationSetting_Param param)
        {
            var data = await GetData(param);
            return PaginationUtility<AccountAuthorizationSetting_Data>.Create(data, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<List<AccountAuthorizationSetting_Data>> GetData(AccountAuthorizationSetting_Param param)
        {
            var predicate = PredicateBuilder.New<HRMS_Basic_Account>(true);

            if (!string.IsNullOrWhiteSpace(param.Account))
                predicate.And(x => x.Account.ToLower().Contains(param.Account.ToLower()));
            if (!string.IsNullOrWhiteSpace(param.Name))
                predicate.And(x => x.Name.ToLower().Contains(param.Name.ToLower()));
            if (!string.IsNullOrWhiteSpace(param.Division))
                predicate.And(x => x.Division == param.Division);
            if (!string.IsNullOrWhiteSpace(param.Factory))
                predicate.And(x => x.Factory == param.Factory);
            if (!string.IsNullOrWhiteSpace(param.Department_ID))
                predicate.And(x => x.Department_ID == param.Department_ID);
            if (param.IsActive == 1 || param.IsActive == 0)
                predicate.And(x => x.IsActive == (param.IsActive == 1));

            var account = await _repositoryAccessor.HRMS_Basic_Account.FindAll(predicate, true).Project().To<AccountAuthorizationSetting_Data>().ToListAsync();
            var accountRoles = await _repositoryAccessor.HRMS_Basic_Account_Role.FindAll(x => account.Select(a => a.Account).Contains(x.Account), true).ToListAsync();

            var result = account.Select(item =>
            {
                item.ListRole = accountRoles.Where(r => r.Account == item.Account).Select(r => r.Role).Distinct().ToList();
                item.ListRole_Str = string.Join("/ ", item.ListRole);
                item.Update_Time_str = item.Update_Time.ToString("yyyy/MM/dd HH:mm:ss");
                item.IsActive_str = param.Lang == "tw" ? (item.IsActive ? "Y.啟用" : "N.停用") : (item.IsActive ? "Y.Enabled" : "N.Disabled");
                return item;
            }).ToList();

            if (!string.IsNullOrWhiteSpace(param.ListRole_Str) && param.ListRole.Count > 0)
                result = result.Where(item => item.ListRole.Any(role => param.ListRole.Contains(role))).ToList();
            return result;
        }

        public async Task<OperationResult> Create(AccountAuthorizationSetting_Data data)
        {
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                if (await _repositoryAccessor.HRMS_Basic_Account.AnyAsync(x => x.Account == data.Account))
                    return new OperationResult(false, "System.Message.DataExisted");
                var dataAccount = new HRMS_Basic_Account
                {
                    Account = data.Account,
                    Name = data.Name,
                    Password = "000000",
                    Division = data.Division,
                    Password_Reset = true,
                    Factory = data.Factory,
                    Department_ID = data.Department_ID,
                    Update_By = data.Update_By,
                    Update_Time = data.Update_Time,
                    IsActive = data.IsActive
                };
                var existingRoles = await _repositoryAccessor.HRMS_Basic_Account_Role.FindAll(x => x.Account == data.Account).ToListAsync();
                if (existingRoles.Any())
                {
                    _repositoryAccessor.HRMS_Basic_Account_Role.RemoveMultiple(existingRoles);
                    await _repositoryAccessor.Save();
                }
                var dataRoles = data.ListRole.Select(l => new HRMS_Basic_Account_Role
                {
                    Account = data.Account,
                    Role = l,
                    Update_By = data.Update_By,
                    Update_Time = data.Update_Time
                }).ToList();
                _repositoryAccessor.HRMS_Basic_Account.Add(dataAccount);
                _repositoryAccessor.HRMS_Basic_Account_Role.AddMultiple(dataRoles);
                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true, "System.Message.CreateOKMsg");
            }
            catch (Exception)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, "System.Message.CreateErrorMsg");
            }
        }
        public async Task<OperationResult> DownloadFileExcel(AccountAuthorizationSetting_Param param)
        {
            var data = await GetData(param);

            if (!data.Any())
                return new OperationResult(false, "System.Message.NoData");

            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                data,
                "Resources\\Template\\BasicMaintenance\\2_1_2_Account_Authorization_Settings\\Download.xlsx");
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string division, string factory, string language)
        {
            var pred = PredicateBuilder.New<HRMS_Org_Department>(true);
            if (!string.IsNullOrWhiteSpace(division))
                pred = pred.And(x => x.Division.ToLower().Contains(division.ToLower()));
            if (!string.IsNullOrWhiteSpace(factory))
                pred = pred.And(x => x.Factory.ToLower().Contains(factory.ToLower()));

            return await _repositoryAccessor.HRMS_Org_Department.FindAll(pred)
                    .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Division == division && x.Factory == factory && x.Language_Code.ToLower() == language.ToLower()),
                                    x => x.Department_Code,
                                    y => y.Department_Code,
                                    (x, y) => new { x, y })
                                    .SelectMany(x => x.y.DefaultIfEmpty(),
                                    (x, y) => new { OrgDepartment = x.x, OrgDepartmentLanguage = y })
                .Select(x => new KeyValuePair<string, string>(x.OrgDepartment.Department_Code, $"{x.OrgDepartment.Department_Code} - {(x.OrgDepartmentLanguage != null ? x.OrgDepartmentLanguage.Name : x.OrgDepartment.Department_Name)}")).ToListAsync();
        }

        public async Task<List<KeyValuePair<string, string>>> GetListDivision(string language)
        {
            return await GetBasicCode("1", language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string language)
        {
            return await GetBasicCode("2", language);
        }

        private async Task<List<KeyValuePair<string, string>>> GetBasicCode(string typeSeq, string language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == typeSeq)
                   .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower()),
                                   x => new { x.Type_Seq, x.Code },
                                   y => new { y.Type_Seq, y.Code },
                                   (x, y) => new { x, y })
                                   .SelectMany(x => x.y.DefaultIfEmpty(),
                                   (x, y) => new { BasicCode = x.x, BasicCodeLanguage = y })
               .Select(x => new KeyValuePair<string, string>(x.BasicCode.Code, $"{x.BasicCode.Code} - {(x.BasicCodeLanguage != null ? x.BasicCodeLanguage.Code_Name : x.BasicCode.Code_Name)}")).ToListAsync();
        }

        public async Task<List<KeyValuePair<string, string>>> GetListRole()
        {
            return await _repositoryAccessor.HRMS_Basic_Role.FindAll()
                      .Select(x => new KeyValuePair<string, string>(x.Role, x.Role)).Distinct().ToListAsync();
        }

        public async Task<OperationResult> Update(AccountAuthorizationSetting_Data data)
        {
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                var item = await _repositoryAccessor.HRMS_Basic_Account.FirstOrDefaultAsync(x => x.Account == data.Account);
                if (item == null) return new OperationResult(false, "System.Message.NoData");
                item.Name = data.Name;
                item.Division = data.Division;
                item.Factory = data.Factory;
                item.Department_ID = data.Department_ID;
                item.Update_By = data.Update_By;
                item.Update_Time = data.Update_Time;
                item.IsActive = data.IsActive;
                var existingRoles = await _repositoryAccessor.HRMS_Basic_Account_Role.FindAll(x => x.Account == data.Account).ToListAsync();
                if (existingRoles.Any())
                {
                    _repositoryAccessor.HRMS_Basic_Account_Role.RemoveMultiple(existingRoles);
                    await _repositoryAccessor.Save();
                }
                var dataRoles = data.ListRole.Select(x => new HRMS_Basic_Account_Role
                {
                    Account = data.Account,
                    Role = x,
                    Update_By = data.Update_By,
                    Update_Time = data.Update_Time
                }).ToList();
                _repositoryAccessor.HRMS_Basic_Account_Role.AddMultiple(dataRoles);
                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                if (data.Account != data.Update_By)
                    await _hubContext.Clients.All.SendAsync(SignalRConstants.ACCOUNT_CHANGED, new List<string> { data.Account });
                return new OperationResult(true, "System.Message.UpdateOKMsg");
            }
            catch (Exception)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, "System.Message.UpdateErrorMsg");
            }
        }

        public async Task<OperationResult> ResetPassword(AccountAuthorizationSetting_Data data)
        {
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                var HBA = await _repositoryAccessor.HRMS_Basic_Account.FirstOrDefaultAsync(x => x.Account == data.Account);
                if (HBA == null) return new OperationResult(false, "System.Message.NoData");
                HBA.Password_Reset = true;
                HBA.Password = "000000";
                HBA.Update_By = data.Update_By;
                HBA.Update_Time = data.Update_Time;
                _repositoryAccessor.HRMS_Basic_Account.Update(HBA);
                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                if (data.Account != data.Update_By)
                    await _hubContext.Clients.All.SendAsync(SignalRConstants.ACCOUNT_CHANGED, new List<string> { data.Account });
                return new OperationResult(true, "System.Message.UpdateOKMsg");
            }
            catch (Exception)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, "System.Message.UpdateErrorMsg");
            }
        }
    }
}