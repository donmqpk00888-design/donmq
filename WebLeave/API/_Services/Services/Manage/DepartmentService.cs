using API._Repositories;
using API._Services.Interfaces.Manage;
using API.Dtos;
using API.Helpers.Enums;
using API.Helpers.Params;
using API.Models;
using AutoMapper;
using LinqKit;
using Microsoft.EntityFrameworkCore;
namespace API._Services.Services.Manage
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IRepositoryAccessor _repo;
        private readonly IMapper _mapper;
        public DepartmentService(IRepositoryAccessor repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }
        public async Task<OperationResult> Add(DepartmentDto departmentDto)
        {
            departmentDto.DeptName = departmentDto.deptnamevn + " - " + departmentDto.deptnametw;
            Department dept = _mapper.Map<Department>(departmentDto);
            var check = await _repo.Department.AnyAsync(x => x.DeptCode == departmentDto.DeptCode && x.AreaID == departmentDto.AreaID && x.BuildingID == departmentDto.BuildingID);
            if (check)
            {
                return new OperationResult(false, "Department.DuplicateDeptCode");
            }
            using var _transaction = await _repo.BeginTransactionAsync();
            try
            {
                _repo.Department.Add(dept);
                await _repo.SaveChangesAsync();

                DetpLang vn = new()
                {
                    DeptName = departmentDto.deptnamevn,
                    DeptID = dept.DeptID,
                    LanguageID = LangConstants.VN
                };
                _repo.DetpLang.Add(vn);

                DetpLang en = new()
                {
                    DeptName = departmentDto.deptnameen,
                    DeptID = dept.DeptID,
                    LanguageID = LangConstants.EN
                };
                _repo.DetpLang.Add(en);

                DetpLang tw = new()
                {
                    DeptName = departmentDto.deptnametw,
                    DeptID = dept.DeptID,
                    LanguageID = LangConstants.ZH_TW
                };
                _repo.DetpLang.Add(tw);

                Part p = new()
                {
                    PartName = departmentDto.DeptName,
                    PartCode = departmentDto.DeptCode,
                    DeptID = dept.DeptID,
                    Visible = true,
                    Number = 0
                };
                _repo.Part.Add(p);
                await _repo.SaveChangesAsync();

                PartLang pvn = new()
                {
                    PartName = departmentDto.deptnamevn,
                    PartID = p.PartID,
                    LanguageID = LangConstants.VN
                };
                _repo.PartLang.Add(pvn);

                PartLang pen = new()
                {
                    PartName = departmentDto.deptnameen,
                    PartID = p.PartID,
                    LanguageID = LangConstants.EN
                };
                _repo.PartLang.Add(pen);

                PartLang ptw = new()
                {
                    PartName = departmentDto.deptnametw,
                    PartID = p.PartID,
                    LanguageID = LangConstants.ZH_TW
                };
                _repo.PartLang.Add(ptw);

                Roles r = new()
                {
                    Ranked = 3,
                    RoleName = departmentDto.DeptName,
                    RoleSym = "D" + dept.DeptID,
                    GroupIN = 4
                };
                _repo.Roles.Add(r);

                dept.DeptSym = "D" + dept.DeptID;

                await _repo.SaveChangesAsync();

                Roles r2 = new()
                {
                    Ranked = 4,
                    GroupIN = 5,
                    RoleName = p.PartName,
                    RoleSym = "P" + p.PartID
                };
                _repo.Roles.Add(r2);

                p.PartSym = "P" + p.PartID;

                await _repo.SaveChangesAsync();

                await _transaction.CommitAsync();
                return new OperationResult(true, "Add Successfully");
            }
            catch (Exception ex)
            {
                await _transaction.RollbackAsync();
                return new OperationResult(false, ex.Message);
            }
        }

        public async Task<List<KeyValuePair<int, string>>> GetAllAreas()
        {
            List<KeyValuePair<int, string>> data = await _repo.Area.FindAll(true)
                    .Select(x => new KeyValuePair<int, string>(
                        x.AreaID,
                        x.AreaName
                    )).Distinct().ToListAsync();
            return data;
        }

        public async Task<List<KeyValuePair<int, string>>> GetAllBuildings()
        {
            List<KeyValuePair<int, string>> data = await _repo.Building.FindAll(true)
                    .Select(x => new KeyValuePair<int, string>(
                        x.BuildingID,
                        x.BuildingName
                    )).Distinct().ToListAsync();
            return data;
        }

        public async Task<PaginationUtility<DepartmentDto>> GetAllDepartment(PaginationParam pagination, DepartmentParams search)
        {
            var layoutPred = PredicateBuilder.New<Department>(true);

            if (!string.IsNullOrEmpty(search.AreaID))
            {
                layoutPred.And(x => x.AreaID == Convert.ToInt32(search.AreaID));
            }

            if (!string.IsNullOrEmpty(search.deptCode))
            {
                layoutPred.And(x => x.DeptCode.Contains(search.deptCode.Trim()));
            }

            IQueryable<DepartmentDto> data = _repo.Department.FindAll(layoutPred, true)
            .Include(x => x.DetpLangs)
            .Include(x => x.Area)
            .Include(x => x.Building)
            .OrderBy(x => x.AreaID)
            .Select(x => new DepartmentDto
            {
                AreaID = x.AreaID,
                BuildingID = x.BuildingID,
                DeptCode = x.DeptCode,
                DeptID = x.DeptID,
                DeptName = x.DeptName,
                DeptSym = x.DeptSym,
                Number = x.Number,
                Shift_Time = x.Shift_Time,
                Visible = x.Visible,
                AreaName = x.Area.AreaName,
                BuildingName = x.Building.BuildingName,
                deptnamevn = x.DetpLangs.FirstOrDefault(y => y.DeptID == x.DeptID && y.LanguageID == LangConstants.VN).DeptName,
                deptnameen = x.DetpLangs.FirstOrDefault(y => y.DeptID == x.DeptID && y.LanguageID == LangConstants.EN).DeptName,
                deptnametw = x.DetpLangs.FirstOrDefault(y => y.DeptID == x.DeptID && y.LanguageID == LangConstants.ZH_TW).DeptName
            });

            return await PaginationUtility<DepartmentDto>.CreateAsync(data, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<OperationResult> Update(DepartmentDto departmentDto)
        {
            var checkDuplicate = await _repo.Department.FirstOrDefaultAsync(x => x.DeptCode == departmentDto.DeptCode && x.AreaID == departmentDto.AreaID && x.BuildingID == departmentDto.BuildingID);
            if (checkDuplicate is not null)
                return new OperationResult { IsSuccess = false, Error = "Department.DuplicateDeptCode" };
            try
            {
                DetpLang dvn = await _repo.DetpLang.FirstOrDefaultAsync(q => q.DeptID == departmentDto.DeptID && q.LanguageID == LangConstants.VN);
                dvn.DeptName = departmentDto.deptnamevn;
            }
            catch
            {
                DetpLang vn = new()
                {
                    DeptID = departmentDto.DeptID,
                    LanguageID = LangConstants.VN,
                    DeptName = departmentDto.deptnamevn
                };
                _repo.DetpLang.Add(vn);
            }

            try
            {
                DetpLang den = await _repo.DetpLang.FirstOrDefaultAsync(q => q.DeptID == departmentDto.DeptID && q.LanguageID == LangConstants.EN);
                den.DeptName = departmentDto.deptnameen;
            }
            catch
            {
                DetpLang en = new()
                {
                    DeptID = departmentDto.DeptID,
                    LanguageID = LangConstants.EN,
                    DeptName = departmentDto.deptnameen
                };
                _repo.DetpLang.Add(en);
            }

            try
            {
                DetpLang dtw = await _repo.DetpLang.FirstOrDefaultAsync(q => q.DeptID == departmentDto.DeptID && q.LanguageID == LangConstants.ZH_TW);
                dtw.DeptName = departmentDto.deptnametw;
            }
            catch
            {
                DetpLang tw = new()
                {
                    DeptID = departmentDto.DeptID,
                    LanguageID = LangConstants.ZH_TW,
                    DeptName = departmentDto.deptnametw
                };
                _repo.DetpLang.Add(tw);
            }

            await _repo.SaveChangesAsync();
            var dept = _mapper.Map<Department>(departmentDto);
            _repo.Department.Update(dept);
            dept.DeptName = departmentDto.deptnamevn + " - " + departmentDto.deptnametw;
            await _repo.SaveChangesAsync();
            return new OperationResult(true, "Update Successfully");
        }
    }
}