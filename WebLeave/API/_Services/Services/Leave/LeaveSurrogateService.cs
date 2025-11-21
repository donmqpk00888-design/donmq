using API._Repositories;
using API._Services.Interfaces.Common;
using API._Services.Interfaces.Leave;
using API.Dtos.Leave;
using API.Helpers.Utilities;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.Leave
{
    public class LeaveSurrogateService : ILeaveSurrogateService
    {
        private readonly IRepositoryAccessor _repositoryAccessor;
        private readonly ICommonService _commonService;

        public LeaveSurrogateService(IRepositoryAccessor repositoryAccessor, ICommonService commonService)
        {
            _repositoryAccessor = repositoryAccessor;
            _commonService = commonService;
        }

        public async Task<SurrogateDto> GetDetail(int userId)
        {
            Users user = await _repositoryAccessor.Users
                .FindAll(x => x.UserID == userId, true)
                .Include(x => x.Emp)
                .Include(x => x.Roles_User)
                    .ThenInclude(x => x.Role)
                .Include(x => x.SetApproveGroupBase)
                .FirstOrDefaultAsync();

            List<int?> roleIds = user.Roles_User.Select(p => p.RoleID).ToList();
            List<int?> groups = user.SetApproveGroupBase.Select(x => x.GBID).ToList();

            ExpressionStarter<Employee> employeePred = PredicateBuilder.New<Employee>(
                x => x.Visible == true &&
                x.Users
                    .Any(
                        z => z.UserID != userId &&
                        z.Visible == true &&
                        z.UserRank >= 3 &&
                        z.Roles_User.Count(r => roleIds.Contains(r.RoleID)) == roleIds.Count &&
                        z.SetApproveGroupBase.Count(r => groups.Contains(r.GBID)) == groups.Count)
            );

            // Trường hợp User là người lao động
            if (user.Emp is not null)
            {
                employeePred.And(x => x.PartID == user.Emp.PartID);
            }
            // Trường hợp User là chuyên gia
            else
            {
                List<string> roleSyms = user.Roles_User.Select(x => x.Role.RoleSym).Distinct().ToList();

                List<int> partIds = await GetPartIds(roleSyms);

                employeePred.And(x => x.PartID.HasValue && partIds.Contains(x.PartID.Value));
            }

            Employee employee = await _repositoryAccessor.Employee
                .FindAll(employeePred, true)
                .Include(x => x.Users)
                    .ThenInclude(x => x.Roles_User)
                .FirstOrDefaultAsync();

            SurrogateDto result = new()
            {
                UserID = user.UserID,
                FullName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName.ToUpper() : user.Emp.EmpName.ToUpper(),
                EmpID = user?.Emp?.EmpID,
                EmpNumber = user?.Emp?.EmpNumber,
                SurrogateId = employee?.Users?.FirstOrDefault(z => z.UserID != userId && z.Visible == true && z.UserRank >= 3)?.UserID ?? 0
            };

            return result;
        }

        public async Task<List<KeyValueUtility>> GetSurrogates(int userId)
        {
            Users user = await _repositoryAccessor.Users
                .FindAll(x => x.UserID == userId, true)
                .Include(x => x.Emp)
                .Include(x => x.Roles_User)
                    .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync();

            ExpressionStarter<Employee> employeePred = PredicateBuilder.New<Employee>(x => x.Visible == true);

            // Trường hợp User là người lao động
            if (user.Emp is not null)
            {
                employeePred.And(x => x.PartID == user.Emp.PartID && x.EmpID != user.Emp.EmpID);
            }
            // Trường hợp User là chuyên gia
            else
            {
                List<string> roleSyms = user.Roles_User.Select(x => x.Role.RoleSym).Distinct().ToList();

                List<int> partIds = await GetPartIds(roleSyms);

                employeePred.And(
                    x => x.PartID.HasValue &&
                    partIds.Contains(x.PartID.Value) &&
                    x.Users.Any(z => z.UserID != userId));
            }

            List<KeyValueUtility> employees = await _repositoryAccessor.Employee
                    .FindAll(employeePred, true)
                    .Include(x => x.Users)
                    .Select(x => new KeyValueUtility(x.Users.FirstOrDefault(z => z.EmpID == x.EmpID && z.Visible == true).UserID, x.EmpName, x.EmpNumber))
                    .ToListAsync();

            return employees;
        }

        private async Task<List<int>> GetPartIds(List<string> roleSyms)
        {
            List<int> partIds = await _repositoryAccessor.Part
                .FindAll(
                    x => roleSyms.Contains(x.PartSym) ||
                    roleSyms.Contains(x.Dept.DeptSym) ||
                    roleSyms.Contains(x.Dept.Building.BuildingSym) ||
                    roleSyms.Contains(x.Dept.Area.AreaSym), true)
                .Include(x => x.Dept.Building.Area)
                .Include(x => x.Dept.Area)
                .Select(x => x.PartID)
                .Distinct().ToListAsync();

            return partIds;
        }

        public async Task<OperationResult> RemoveSurrogate(SurrogateRemoveDto dto)
        {
            var _transaction = await _repositoryAccessor.BeginTransactionAsync();

            try
            {
                // Tài khoản người cấp quyền cho người đại diện
                var user = await _repositoryAccessor.Users
                    .FindAll(x => x.UserID == dto.UserID)
                    .Include(x => x.Roles_User)
                    .Include(x => x.SetApproveGroupBase)
                    .FirstOrDefaultAsync();

                // Tài khoản người đại diện
                var userSurrogate = await _repositoryAccessor.Users
                    .FindAll(x => x.UserID == dto.SurrogateId)
                    .Include(x => x.Roles_User)
                    .Include(x => x.SetApproveGroupBase)
                    .FirstOrDefaultAsync();

                // Lấy ra RoleIds của user
                var roleIds = user.Roles_User.Select(x => x.RoleID).ToList();

                // Tìm những Role của người đại diện theo RoleIds của user
                List<Roles_User> roleDeletes = userSurrogate.Roles_User.Where(x => roleIds.Contains(x.RoleID)).ToList();

                // Xóa Roles của người đại diện
                if (roleDeletes.Any())
                {
                    _repositoryAccessor.RolesUser.RemoveMultiple(roleDeletes);
                    await _repositoryAccessor.SaveChangesAsync();
                }

                var groups = user.SetApproveGroupBase.Select(x => x.GBID).ToList();
                // Tìm những GBID của người đại diện theo GBID của user
                List<SetApproveGroupBase> groupDeletes = userSurrogate.SetApproveGroupBase.Where(x => groups.Contains(x.GBID)).ToList();

                // Xóa GBID của người đại diện
                if (groupDeletes.Any())
                {
                    _repositoryAccessor.SetApproveGroupBase.RemoveMultiple(groupDeletes);
                    await _repositoryAccessor.SaveChangesAsync();
                }

                // Cập nhật lại Rank cho người đại diện nếu không tồn tại bất kỳ Role nào
                if (!userSurrogate.Roles_User.Any())
                    userSurrogate.UserRank = 1;

                userSurrogate.Updated = _commonService.GetServerTime();
                _repositoryAccessor.Users.Update(userSurrogate);
                await _repositoryAccessor.SaveChangesAsync();

                await _transaction.CommitAsync();
                return new OperationResult { IsSuccess = true };
            }
            catch (Exception ex)
            {
                await _transaction.RollbackAsync();
                return new OperationResult { IsSuccess = false, Error = ex.ToString() };
            }
        }

        public async Task<OperationResult> SaveSurrogate(SurrogateDto dto)
        {
            var _transaction = await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                var now = _commonService.GetServerTime();
                // tài khoản người đại diện
                var user = await _repositoryAccessor.Users
                    .FindAll(x => x.UserID == dto.SurrogateId)
                    .Include(x => x.Roles_User)
                    .Include(x => x.SetApproveGroupBase)
                    .FirstOrDefaultAsync();

                // roleIds của người đại diện
                var userRoleIds = user.Roles_User.Select(x => x.RoleID).ToList();

                // Lấy ra RoleIds của user không tồn tại trong RoleIds của người đại diện
                var roleIds = await _repositoryAccessor.RolesUser
                    .FindAll(x => x.UserID == dto.UserID && !userRoleIds.Contains(x.RoleID), true)
                    .Select(x => x.RoleID).Distinct().ToListAsync();
                // Thêm mới Role cho người đại diện (nếu có)
                if (roleIds.Any())
                {
                    List<Roles_User> creates = new();
                    foreach (var roleId in roleIds)
                    {
                        creates.Add(new Roles_User
                        {
                            RoleID = roleId,
                            UserID = dto.SurrogateId,
                            Updated = now
                        });
                    }

                    _repositoryAccessor.RolesUser.AddMultiple(creates);
                    await _repositoryAccessor.SaveChangesAsync();
                }

                // GBID của người đại diện
                var userGroups = user.SetApproveGroupBase.Select(x => x.GBID).ToList();

                // Lấy ra GBID của user không tồn tại trong GBID của người đại diện
                var groups = await _repositoryAccessor.SetApproveGroupBase
                    .FindAll(x => x.UserID == dto.UserID && !userGroups.Contains(x.GBID), true)
                    .Select(x => x.GBID).Distinct().ToListAsync();
                // Thêm mới GroupBase cho người đại diện (nếu có)
                if (groups.Any())
                {
                    List<SetApproveGroupBase> creates = new();
                    foreach (var gbid in groups)
                    {
                        creates.Add(new SetApproveGroupBase
                        {
                            GBID = gbid,
                            UserID = dto.SurrogateId,
                            Created = now
                        });
                    }

                    _repositoryAccessor.SetApproveGroupBase.AddMultiple(creates);
                    await _repositoryAccessor.SaveChangesAsync();
                }

                // Cập nhật Rank cho người đại diện
                if (user.UserRank < 3)
                {
                    user.UserRank = 3;
                    _repositoryAccessor.Users.Update(user);
                    await _repositoryAccessor.SaveChangesAsync();
                }

                await _transaction.CommitAsync();
                return new OperationResult { IsSuccess = true };
            }
            catch (Exception ex)
            {
                await _transaction.RollbackAsync();
                return new OperationResult { IsSuccess = false, Error = ex.ToString() };
            }
        }
    }
}