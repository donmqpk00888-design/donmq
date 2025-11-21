using API._Repositories;
using API._Services.Interfaces.Manage;
using API.Dtos.Manage.TeamManagement;
using API.Helpers.Enums;
using API.Models;
using AutoMapper;
using LinqKit;
using Microsoft.EntityFrameworkCore;
namespace API._Services.Services.Manage
{
    public class TeamManagementService : ITeamManagementService
    {
        private readonly IRepositoryAccessor _repositoryAccessor;
        private readonly IMapper _mapper;

        public TeamManagementService(
            IRepositoryAccessor repositoryAccessor,
            IMapper mapper)
        {
            _repositoryAccessor = repositoryAccessor;
            _mapper = mapper;
        }

        public async Task<OperationResult> Create(PartDto partDto)
        {
            using var _transaction = await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                int partId = 1;
                IQueryable<Part> checkExist = _repositoryAccessor.Part.FindAll(true);
                if(checkExist.FirstOrDefault(x => x.PartCode == partDto.PartCode) != null)
                    return new OperationResult { IsSuccess = false, Error = "Manage.TeamManager.DuplicatePartCode" };
                if (checkExist.Any())
                    partId = checkExist.Max(x => x.PartID) + 1;

                partDto.PartSym = $"P{partId}";
                partDto.PartName = $"{partDto.PartNameVN} - {partDto.PartNameTW}";
                Part part = _mapper.Map<Part>(partDto);
                _repositoryAccessor.Part.Add(part);
                await _repositoryAccessor.SaveChangesAsync();

                PartLang vi = new()
                {
                    PartID = part.PartID,
                    LanguageID = LangConstants.VN,
                    PartName = partDto.PartNameVN
                };
                PartLang en = new()
                {
                    PartID = part.PartID,
                    LanguageID = LangConstants.EN,
                    PartName = partDto.PartNameEN
                };
                PartLang tw = new()
                {
                    PartID = part.PartID,
                    LanguageID = LangConstants.ZH_TW,
                    PartName = partDto.PartNameTW
                };

                _repositoryAccessor.PartLang.AddMultiple(new List<PartLang>() { vi, en, tw });
                await _repositoryAccessor.SaveChangesAsync();

                Roles role = new()
                {
                    Ranked = 4,
                    RoleName = partDto.PartName,
                    RoleSym = partDto.PartSym,
                    GroupIN = 5
                };
                _repositoryAccessor.Roles.Add(role);

                await _repositoryAccessor.SaveChangesAsync();
                await _transaction.CommitAsync();

                return new OperationResult { IsSuccess = true };
            }
            catch
            {
                await _transaction.RollbackAsync();
                return new OperationResult { IsSuccess = false, Error = "System.Message.CreateErrorMsg" };
            }
        }

        public async Task<OperationResult> ExportExcel(PaginationParam pagination, PartParam param)
        {
            var result = await GetDataPaginations(pagination, param.DeptID, param.PartCode, false);

            List<Table> dataTable = new()
            {
                new Table("result", result.Result)
            };

            List<Cell> dataTitle = new()
            {
                new Cell("A1", param.Label_PartName),
                new Cell("B1", param.Label_PartCode),
                new Cell("C1", param.Label_Number),
            };

            ExcelResult excelResult = ExcelUtility.DownloadExcel(dataTable, dataTitle, "Resources\\Template\\Manage\\TeamManagementTemplate.xlsx");
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }

        public async Task<List<KeyValuePair<string, string>>> GetAllDepartment()
        {
            var data = await _repositoryAccessor.Department.FindAll()
            .Select(x => new KeyValuePair<string, string>(
                x.DeptID.ToString(),
                x.DeptName
            )).ToListAsync();
            return data;
        }

        public async Task<PartDto> GetDataDetail(int partID)
        {
            PartDto data = await _repositoryAccessor.Part.FindAll(x => x.PartID == partID, true)
            .Include(x => x.PartLangs)
            .Select(x => new PartDto
            {
                DeptID = x.DeptID,
                Number = x.Number,
                PartCode = x.PartCode,
                PartID = x.PartID,
                PartName = x.PartName,
                PartSym = x.PartSym,
                Visible = x.Visible,
                PartNameVN = x.PartLangs.FirstOrDefault(y => y.PartID == x.PartID && y.LanguageID == LangConstants.VN).PartName,
                PartNameEN = x.PartLangs.FirstOrDefault(y => y.PartID == x.PartID && y.LanguageID == LangConstants.EN).PartName,
                PartNameTW = x.PartLangs.FirstOrDefault(y => y.PartID == x.PartID && y.LanguageID == LangConstants.ZH_TW).PartName
            }).FirstOrDefaultAsync();

            return data;
        }

        public async Task<PaginationUtility<TeamManagementDataDto>> GetDataPaginations(PaginationParam pagination, string deptID, string partCode, bool isPaging = true)
        {
            var partPred = PredicateBuilder.New<Part>(true);
            if (!string.IsNullOrEmpty(deptID))
                partPred.And(x => x.DeptID.ToString() == deptID);
            if (!string.IsNullOrEmpty(partCode))
                partPred.And(x => x.PartCode.StartsWith(partCode.Trim()));
            IQueryable<TeamManagementDataDto> data = _repositoryAccessor.Part.FindAll(partPred)
            .Include(x => x.Dept)
            .Select(x => new TeamManagementDataDto
            {
                DeptID = x.DeptID,
                DeptName = x.Dept.DeptName,
                Number = x.Number,
                PartCode = x.PartCode,
                PartID = x.PartID,
                PartName = x.PartName,
                PartSym = x.PartSym,
                Visible = x.Visible
            });

            return await PaginationUtility<TeamManagementDataDto>.CreateAsync(data, pagination.PageNumber, pagination.PageSize, isPaging);
        }

        public async Task<OperationResult> Update(PartDto partDto)
        {
            using var _transaction = await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                PartLang partVN = await _repositoryAccessor.PartLang.FirstOrDefaultAsync(x => x.LanguageID == LangConstants.VN && x.PartID == partDto.PartID);
                PartLang partEN = await _repositoryAccessor.PartLang.FirstOrDefaultAsync(x => x.LanguageID == LangConstants.EN && x.PartID == partDto.PartID);
                PartLang partTW = await _repositoryAccessor.PartLang.FirstOrDefaultAsync(x => x.LanguageID == LangConstants.ZH_TW && x.PartID == partDto.PartID);

                partVN.PartName = partDto.PartNameVN;
                partEN.PartName = partDto.PartNameEN;
                partTW.PartName = partDto.PartNameTW;

                partDto.PartName = $"{partVN.PartName} - {partTW.PartName}";

                var data = await _repositoryAccessor.Part.FindAll().ToListAsync();
                if (data.FirstOrDefault(x => x.PartCode == partDto.PartCode) is not null)
                    return new OperationResult { IsSuccess = false, Error = "Manage.TeamManager.DuplicatePartCode" };
                var item = data.FirstOrDefault(x => x.PartID == partDto.PartID);
                if (item is null)
                    return new OperationResult { IsSuccess = false, Error = "System.Message.UpdateErrorMsg" };

                // item.part
                item.PartCode = partDto.PartCode;
                item.PartName = partDto.PartName;
                item.Number = partDto.Number;
                item.DeptID = partDto.DeptID;
                item.Visible = partDto.Visible;
                _repositoryAccessor.Part.Update(item);

                _repositoryAccessor.PartLang.UpdateMultiple(new List<PartLang> { partVN, partEN, partTW });

                await _repositoryAccessor.SaveChangesAsync();
                await _transaction.CommitAsync();

                return new OperationResult { IsSuccess = true };
            }
            catch
            {
                await _transaction.RollbackAsync();
                return new OperationResult { IsSuccess = false, Error = "System.Message.UpdateErrorMsg" };
            }
        }
    }
}