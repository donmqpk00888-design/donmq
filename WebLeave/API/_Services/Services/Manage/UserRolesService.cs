using API._Repositories;
using API._Services.Interfaces.Manage;
using API.Dtos.Manage.UserManage;
using API.Models;
using Microsoft.EntityFrameworkCore;
using API.Helpers.Enums;
using API.Dtos.Auth;
using System.Text.Json;
using API._Services.Interfaces.Common;
namespace API._Services.Services.Manage
{
    public class UserRolesService : IUserRolesService
    {
        private readonly IRepositoryAccessor _repoAccessor;
        private readonly ICommonService _commonService;
        public UserRolesService(IRepositoryAccessor repoAccessor, ICommonService commonService)
        {
            _repoAccessor = repoAccessor;
            _commonService = commonService;
        }

        public async Task<(List<TreeNode<RoleNode>> Roles, List<TreeNode<RoleNode>> AssignedRoles)> GetAllRoleUser(int userId, string langId)
        {
            //Get company
           var company = await _repoAccessor.Company.FirstOrDefaultAsync(x => x.Visible == true);
            
            //Get area
            var areas = await _repoAccessor.Area
                .FindAll(x => x.CompanyID == company.CompanyID && x.Visible == true)
                .Include(x => x.AreaLangs)
                .OrderByDescending(x => x.AreaID)
                .ToListAsync();

            var areaIds = areas.Select(x => x.AreaID).Distinct().ToList();

            //Get building
            var buildings = await _repoAccessor.Building
                .FindAll(x => x.AreaID.HasValue && areaIds.Contains(x.AreaID.Value) && !x.Area.AreaName.Contains("NONE") && x.Visible == true)
                .Include(x => x.BuildLangs)
                .ToListAsync();

            var buildingIds = buildings.Select(x => x.BuildingID).Distinct().ToList();

            //Get department
            var departments = await _repoAccessor.Department
                .FindAll(x => (x.AreaID.HasValue && areaIds.Contains(x.AreaID.Value) || x.BuildingID.HasValue && buildingIds.Contains(x.BuildingID.Value)) && x.Visible == true)
                .Include(x => x.DetpLangs)
                .Include(x => x.Building).ThenInclude(b => b.Area)
                .Include(x => x.Area)
                .ToListAsync();

            var deptIds = departments.Select(x => x.DeptID).Distinct().ToList();

            //Get part
            var parts = await _repoAccessor.Part
                .FindAll(x => x.DeptID.HasValue && deptIds.Contains(x.DeptID.Value) && x.Visible == true)
                .Include(x => x.PartLangs)
                .Include(x => x.Dept).ThenInclude(d => d.Building)
                .Include(x => x.Dept).ThenInclude(d => d.Area)
                .ToListAsync();

            var buildingSyms = buildings.Select(x => x.BuildingSym).Distinct().ToList();
            var deptSyms = departments.Select(x => x.DeptSym).Distinct().ToList();
            var partSyms = parts.Select(x => x.PartSym).Distinct().ToList();

            //Get role
            var roles = await _repoAccessor.Roles
                .FindAll(x => true)
                .Include(x => x.Roles_User).Distinct()
                .ToListAsync();

            List<TreeNode<RoleNode>> results = new();
            foreach (var area in areas)
            {
                var areaRole = roles.FirstOrDefault(x => x.RoleSym == area.AreaSym);
                if (areaRole is not null)
                {
                    string areaRoleName = area.AreaLangs.FirstOrDefault(x => x.AreaID == area.AreaID && x.LanguageID.ToLower() == langId)?.AreaName;
                    TreeNode<RoleNode> areaNode = new()
                    {
                        Key = $"A_{areaRole.RoleID}",
                        Label = areaRoleName,
                        Data = new()
                        {
                            AreaID = area.AreaID,
                            RoleID = areaRole.RoleID,
                            RoleName = areaRoleName,
                            RoleRanked = areaRole.Ranked,
                            RoleSym = areaRole.RoleSym,
                            RoleAssigned = areaRole.Roles_User.Any(x => x.RoleID == areaRole.RoleID && x.UserID == userId)
                        },
                        Expanded = true
                    };

                    var buildingByAreas = buildings.Where(x => x.AreaID == area.AreaID);
                    if (buildingByAreas.Any())
                    {
                        foreach (var building in buildingByAreas)
                        {
                            var buildingRole = roles.FirstOrDefault(x => x.RoleSym == building.BuildingSym);
                            if (buildingRole is not null)
                            {
                                string buildingRoleName = building.BuildLangs.FirstOrDefault(x => x.BuildingID == building.BuildingID && x.LanguageID.ToLower() == langId)?.BuildingName;
                                TreeNode<RoleNode> buildingNode = new()
                                {
                                    Key = $"B_{buildingRole.RoleID}",
                                    Label = buildingRoleName,
                                    Data = new()
                                    {
                                        AreaID = building.AreaID,
                                        BuildingID = building.BuildingID,
                                        RoleID = buildingRole.RoleID,
                                        ParentRoleID = areaRole.RoleID,
                                        RoleName = buildingRoleName,
                                        RoleRanked = buildingRole.Ranked,
                                        RoleSym = buildingRole.RoleSym,
                                        RoleAssigned = buildingRole.Roles_User.Any(x => x.RoleID == buildingRole.RoleID && x.UserID == userId)
                                    },
                                    Expanded = true
                                };

                                var deptByBuildings = departments.Where(x => x.BuildingID == building.BuildingID && x.AreaID == building.AreaID);
                                if (deptByBuildings.Any())
                                {
                                    foreach (var dept in deptByBuildings)
                                    {
                                        var deptRole = roles.FirstOrDefault(x => x.RoleSym == dept.DeptSym);
                                        if (deptRole is not null)
                                        {
                                            string deptRoleName = dept.DetpLangs.FirstOrDefault(x => x.DeptID == dept.DeptID && x.LanguageID.ToLower() == langId)?.DeptName;
                                            TreeNode<RoleNode> deptNode = new()
                                            {
                                                Key = $"D_{deptRole.RoleID}",
                                                Label = deptRoleName,
                                                Data = new()
                                                {
                                                    AreaID = dept.AreaID,
                                                    BuildingID = dept.BuildingID,
                                                    DepartmentID = dept.DeptID,
                                                    RoleID = deptRole.RoleID,
                                                    ParentRoleID = buildingRole.RoleID,
                                                    RoleName = deptRoleName,
                                                    RoleRanked = deptRole.Ranked,
                                                    RoleSym = deptRole.RoleSym,
                                                    RoleAssigned = deptRole.Roles_User.Any(x => x.RoleID == deptRole.RoleID && x.UserID == userId)
                                                },
                                                Expanded = true
                                            };

                                            var partByDepts = parts.Where(x => x.DeptID == dept.DeptID);
                                            if (partByDepts.Any())
                                            {
                                                foreach (var part in partByDepts)
                                                {
                                                    var partRole = roles.FirstOrDefault(x => x.RoleSym == part.PartSym);
                                                    if (partRole is not null)
                                                    {
                                                        string partRoleName = part.PartLangs.FirstOrDefault(x => x.PartID == part.PartID && x.LanguageID.ToLower() == langId)?.PartName;
                                                        TreeNode<RoleNode> partNode = new()
                                                        {
                                                            Key = $"P_{partRole.RoleID}",
                                                            Label = partRoleName,
                                                            Data = new()
                                                            {
                                                                AreaID = part.Dept.Building.AreaID,
                                                                BuildingID = part.Dept.BuildingID,
                                                                DepartmentID = part.DeptID,
                                                                PartID = part.PartID,
                                                                RoleID = partRole.RoleID,
                                                                ParentRoleID = deptRole.RoleID,
                                                                RoleName = partRoleName,
                                                                RoleRanked = partRole.Ranked,
                                                                RoleSym = partRole.RoleSym,
                                                                RoleAssigned = partRole.Roles_User.Any(x => x.RoleID == partRole.RoleID && x.UserID == userId)
                                                            },
                                                            Expanded = true
                                                        };

                                                        partNode.Checked = partNode.Data.RoleAssigned || deptNode.Data.RoleAssigned || buildingNode.Data.RoleAssigned || areaNode.Data.RoleAssigned;

                                                        partNode.Selectable = partNode.Data.RoleAssigned && !deptNode.Data.RoleAssigned && !buildingNode.Data.RoleAssigned && !areaNode.Data.RoleAssigned;

                                                        deptNode.Children.Add(partNode);
                                                    }
                                                }
                                            }

                                            deptNode.Data.RoleChildAssigned = deptNode.Children.Any(x => x.Data.RoleAssigned);

                                            deptNode.Checked = deptNode.Data.RoleAssigned || buildingNode.Data.RoleAssigned || areaNode.Data.RoleAssigned;

                                            deptNode.Selectable = deptNode.Data.RoleAssigned && !buildingNode.Data.RoleAssigned && !areaNode.Data.RoleAssigned;

                                            buildingNode.Children.Add(deptNode);
                                        }
                                    }
                                }

                                buildingNode.Data.RoleChildAssigned
                                    = buildingNode.Children.Any(x => x.Data.RoleAssigned)
                                    || buildingNode.Children.Any(x => x.Data.RoleChildAssigned)
                                    || !areaNode.Children.Any() && roles.Any(x => x.Roles_User.Any(r => deptSyms.Contains(x.RoleSym) && r.UserID == userId));

                                buildingNode.Checked = buildingNode.Data.RoleAssigned || areaNode.Data.RoleAssigned;

                                buildingNode.Selectable = buildingNode.Data.RoleAssigned && !areaNode.Data.RoleAssigned;

                                areaNode.Children.Add(buildingNode);
                            }
                        }
                    }
                    else
                    {
                        var deptByAreas = departments.Where(x => x.AreaID == area.AreaID);
                        if (deptByAreas.Any())
                        {
                            foreach (var dept in deptByAreas)
                            {
                                var deptRole = roles.FirstOrDefault(x => x.RoleSym == dept.DeptSym);
                                if (deptRole is not null)
                                {
                                    string deptRoleName = dept.DetpLangs.FirstOrDefault(x => x.DeptID == dept.DeptID && x.LanguageID.ToLower() == langId)?.DeptName;
                                    TreeNode<RoleNode> deptNode = new()
                                    {
                                        Key = $"D_{deptRole.RoleID}",
                                        Label = deptRoleName,
                                        Data = new()
                                        {
                                            AreaID = dept.AreaID,
                                            DepartmentID = dept.DeptID,
                                            RoleID = deptRole.RoleID,
                                            ParentRoleID = areaRole.RoleID,
                                            RoleName = deptRoleName,
                                            RoleRanked = deptRole.Ranked,
                                            RoleSym = deptRole.RoleSym,
                                            RoleAssigned = deptRole.Roles_User.Any(x => x.RoleID == deptRole.RoleID && x.UserID == userId)
                                        },
                                        Expanded = true
                                    };

                                    var partByDepts = parts.Where(x => x.DeptID == dept.DeptID);
                                    if (partByDepts.Any())
                                    {
                                        foreach (var part in partByDepts)
                                        {
                                            var partRole = roles.FirstOrDefault(x => x.RoleSym == part.PartSym);
                                            if (partRole is not null)
                                            {
                                                string partRoleName = part.PartLangs.FirstOrDefault(x => x.PartID == part.PartID && x.LanguageID.ToLower() == langId)?.PartName;
                                                TreeNode<RoleNode> partNode = new()
                                                {
                                                    Key = $"P_{partRole.RoleID}",
                                                    Label = partRoleName,
                                                    Data = new()
                                                    {
                                                        AreaID = part.Dept.AreaID,
                                                        DepartmentID = part.DeptID,
                                                        PartID = part.PartID,
                                                        RoleID = partRole.RoleID,
                                                        ParentRoleID = deptRole.RoleID,
                                                        RoleName = partRoleName,
                                                        RoleRanked = partRole.Ranked,
                                                        RoleSym = partRole.RoleSym,
                                                        RoleAssigned = partRole.Roles_User.Any(x => x.RoleID == partRole.RoleID && x.UserID == userId)
                                                    },
                                                    Expanded = true
                                                };

                                                partNode.Checked = partNode.Data.RoleAssigned || deptNode.Data.RoleAssigned || areaNode.Data.RoleAssigned;

                                                partNode.Selectable = partNode.Data.RoleAssigned && !deptNode.Data.RoleAssigned && !areaNode.Data.RoleAssigned;

                                                deptNode.Children.Add(partNode);
                                            }
                                        }
                                    }

                                    deptNode.Data.RoleChildAssigned = deptNode.Children.Any(x => x.Data.RoleAssigned);

                                    deptNode.Checked = deptNode.Data.RoleAssigned || areaNode.Data.RoleAssigned;

                                    deptNode.Selectable = deptNode.Data.RoleAssigned && !areaNode.Data.RoleAssigned;

                                    areaNode.Children.Add(deptNode);
                                }
                            }
                        }
                    }

                    areaNode.Data.RoleChildAssigned
                        = areaNode.Children.Any(x => x.Data.RoleAssigned)
                        || areaNode.Children.Any(x => x.Data.RoleChildAssigned)
                        || !areaNode.Children.Any() && roles.Any(x => x.Roles_User.Any(r => buildingSyms.Contains(x.RoleSym) && r.UserID == userId))
                        || !areaNode.Children.Any() && roles.Any(x => x.Roles_User.Any(r => deptSyms.Contains(x.RoleSym) && r.UserID == userId))
                        || !areaNode.Children.Any() && roles.Any(x => x.Roles_User.Any(r => partSyms.Contains(x.RoleSym) && r.UserID == userId));

                    areaNode.Checked = areaNode.Data.RoleAssigned;

                    areaNode.Selectable = areaNode.Data.RoleAssigned;

                    results.Add(areaNode);
                }
            }

            return ExecuteLoadRoles(results);
        }

        private static (List<TreeNode<RoleNode>> Roles, List<TreeNode<RoleNode>> AssignedRoles) ExecuteLoadRoles(List<TreeNode<RoleNode>> results)
        {
            // using Json because results was change after RecursiveRoles data => RecursiveAssignedRoles wrongs
            var jsonData = JsonSerializer.Serialize(results);

            var jsonRoles = JsonSerializer.Deserialize<List<TreeNode<RoleNode>>>(jsonData);
            List<TreeNode<RoleNode>> roles = jsonRoles.Where(x => x.Checked == false).ToList();
            List<TreeNode<RoleNode>> Roles = RecursiveRoles(roles, false);

            var jsonAssignedRoles = JsonSerializer.Deserialize<List<TreeNode<RoleNode>>>(jsonData);
            List<TreeNode<RoleNode>> assignedRoles = jsonAssignedRoles.Where(x => x.Checked == true || x.Data.RoleChildAssigned == true).ToList();
            List<TreeNode<RoleNode>> AssignedRoles = RecursiveAssignedRoles(assignedRoles, true);

            return (Roles, AssignedRoles);
        }

        private static List<TreeNode<RoleNode>> RecursiveRoles(List<TreeNode<RoleNode>> results, bool isChecked)
        {
            return results
                .Where(x => x.Data.RoleAssigned == isChecked)
                .Select(x =>
                {
                    x.Children = RecursiveRoles(x.Children, isChecked);
                    x.Selectable = true;
                    return x;
                }).ToList();
        }

        private static List<TreeNode<RoleNode>> RecursiveAssignedRoles(List<TreeNode<RoleNode>> results, bool isChecked)
        {
            return results
                .Where(x => x.Checked == isChecked || x.Data.RoleChildAssigned == isChecked || x.Data.RoleAssigned == isChecked)
                .Select(x =>
                {
                    x.Children = RecursiveAssignedRoles(x.Children, isChecked);
                    return x;
                }).ToList();
        }

        public async Task<List<ExportAssignRolesDto>> GetAssignRoles(int userId, string langId)
        {
            List<ExportAssignRolesDto> data = new();
            (List<TreeNode<RoleNode>> Roles, List<TreeNode<RoleNode>> AssignedRoles) = await GetAllRoleUser(userId, langId);
            Users user = await _repoAccessor.Users.FindById(userId);
            foreach (var itemArea in AssignedRoles)
            {
                foreach (var itemDept in itemArea.Children)
                {
                    foreach (var itemPart in itemDept.Children.Where(x => x.Data.RoleAssigned == true))
                    {
                        ExportAssignRolesDto value = new()
                        {
                            Number = user.UserName,
                            FullName = user.FullName.ToUpper(),
                            BuildingName = itemArea.Label,
                            DeptMainName = itemDept.Label,
                            DeptName = itemPart.Label,
                            Employees = string.Join(" || ", await _repoAccessor.Employee.FindAll(x => x.PartID == itemPart.Data.PartID).Select(x => x.EmpName).ToListAsync())
                        };
                        data.Add(value);
                    }
                }
            }
            return data;
        }

        /// <summary>
        /// Assign role of user
        /// </summary>
        public async Task<OperationResult> AssignRole(int userId, int roleId, int updateBy)
        {
            List<Roles_User> roles = await _repoAccessor.RolesUser.FindAll(x => x.UserID == userId && x.RoleID == roleId).ToListAsync();
            if (roles.Any())
                return new OperationResult(false, MessageConstants.EXISTS, MessageConstants.ADD_ERROR);

            string updateByResult = await UpdatedByJoin(roles.OrderByDescending(x => x.Updated).FirstOrDefault(), updateBy);
            Roles_User model = new()
            {
                RoleID = roleId,
                UserID = userId,
                Updated = _commonService.GetServerTime(),
                Updated_By = updateByResult
            };

            _repoAccessor.RolesUser.Add(model);
            await _repoAccessor.SaveChangesAsync();

            return new OperationResult(true, MessageConstants.ADD_SUCCESS, MessageConstants.SUCCESS);
        }

        /// <summary>
        /// UnAssign Role of user
        /// </summary>
        public async Task<OperationResult> UnAssignRole(int userId, int roleId)
        {
            List<Roles_User> roleUsers = await _repoAccessor.RolesUser.FindAll(x => x.RoleID == roleId && x.UserID == userId).ToListAsync();
            _repoAccessor.RolesUser.RemoveMultiple(roleUsers);
            await _repoAccessor.SaveChangesAsync();

            return new OperationResult(true, MessageConstants.REMOVE_SUCCESS, MessageConstants.SUCCESS);
        }

        public async Task<OperationResult> UpdateRoleRank(int userId, int roleRank, bool isInherit)
        {
            Users user = await _repoAccessor.Users.FindById(userId);
            user.UserRank = roleRank;

            if (isInherit == false)
            {
                var roles = await _repoAccessor.RolesUser.FindAll(q => q.UserID == userId && q.Role.GroupIN > 1).ToListAsync();
                if (roles.Any()) _repoAccessor.RolesUser.RemoveMultiple(roles);
            }
            await _repoAccessor.SaveChangesAsync();

            return new OperationResult(true, MessageConstants.UPDATE_SUCCESS, MessageConstants.SUCCESS);
        }

        public async Task<(List<TreeNode<GroupBaseNode>> Roles, List<TreeNode<GroupBaseNode>> AssignedRoles)> GetAssignGroupBase(int userId, string langId)
        {
            List<SetApproveGroupBase> listSetApprove = await _repoAccessor.SetApproveGroupBase.FindAll(x => x.UserID == userId).ToListAsync();
            List<GroupLang> listLang = await _repoAccessor.GroupLang.FindAll(x => x.LanguageID.ToLower() == langId).ToListAsync();
            IQueryable<GroupBase> listGb = _repoAccessor.GroupBase.FindAll();
            List<TreeNode<GroupBaseNode>> gBaseList = new();
            foreach (var itemGb in listGb)
            {
                GroupLang groupLang = listLang.FirstOrDefault(x => x.GBID == itemGb.GBID);
                TreeNode<GroupBaseNode> gBase = new()
                {
                    Key = itemGb.GBID.ToString(),
                    Label = (groupLang != null) ? groupLang.BaseName : itemGb.BaseName,
                    Data = new()
                    {
                        GBID = itemGb.GBID,
                        BaseName = itemGb.BaseName,
                        BaseSym = itemGb.BaseSym
                    },
                    Checked = listSetApprove.Any(x => x.GBID == itemGb.GBID),
                    Icon = "fa fa-circle-arrow-right"
                };
                gBaseList.Add(gBase);
            }

            List<TreeNode<GroupBaseNode>> Roles = gBaseList.Where(x => !x.Checked).ToList();
            List<TreeNode<GroupBaseNode>> AssignedRoles = gBaseList.Where(x => x.Checked).ToList();

            return (Roles, AssignedRoles);
        }

        public async Task<OperationResult> AssignGroupBase(int gbId, int userId)
        {
            SetApproveGroupBase groupBase = new()
            {
                GBID = gbId,
                UserID = userId
            };
            _repoAccessor.SetApproveGroupBase.Add(groupBase);
            await _repoAccessor.SaveChangesAsync();

            return new OperationResult(true, MessageConstants.ADD_SUCCESS, MessageConstants.SUCCESS);
        }

        public async Task<OperationResult> UnAssignGroupBase(int gbId, int userId)
        {
            SetApproveGroupBase groupBase = await _repoAccessor.SetApproveGroupBase.FirstOrDefaultAsync(x => x.GBID == gbId && x.UserID == userId);
            if (groupBase != null)
                _repoAccessor.SetApproveGroupBase.Remove(groupBase);
            await _repoAccessor.SaveChangesAsync();

            return new OperationResult(true, MessageConstants.REMOVE_SUCCESS, MessageConstants.SUCCESS);
        }

        public async Task<OperationResult> SetPermit(int userId, string key, int updateBy)
        {
            if (key == "mod")
            {
                Roles role = await _repoAccessor.Roles.FirstOrDefaultAsync(q => q.RoleSym.ToLower().Equals("moderator") && q.GroupIN == 0);
                Roles_User roles_User = await _repoAccessor.RolesUser.FindAll(x => x.UserID == userId).OrderByDescending(x => x.Updated).FirstOrDefaultAsync();
                Roles_User roleUser = new()
                {
                    RoleID = role.RoleID,
                    UserID = userId,
                    Updated = _commonService.GetServerTime(),
                    Updated_By = await UpdatedByJoin(roles_User, updateBy)
                };
                _repoAccessor.RolesUser.Add(roleUser);
            }

            Users user = await _repoAccessor.Users.FindById(userId);
            user.ISPermitted = true;

            await _repoAccessor.SaveChangesAsync();
            return new OperationResult(true, MessageConstants.UPDATE_SUCCESS, MessageConstants.SUCCESS);
        }

        public async Task<OperationResult> RemovePermit(int userId, string key)
        {
            Roles_User roleUser = await _repoAccessor.RolesUser.FirstOrDefaultAsync(q => q.UserID == userId && q.Role.GroupIN == 0);

            if (roleUser != null)
                _repoAccessor.RolesUser.Remove(roleUser);

            if (key == "all")
            {
                Users user = await _repoAccessor.Users.FindById(userId);
                user.ISPermitted = false;
            }

            await _repoAccessor.SaveChangesAsync();
            return new OperationResult(true, MessageConstants.REMOVE_SUCCESS, MessageConstants.SUCCESS);
        }

        public async Task<OperationResult> SetReport(int userId, string key, int updateBy)
        {
            List<Roles_User> listRoleUser = new();
            Roles_User roles_User = await _repoAccessor.RolesUser.FindAll(x => x.UserID == userId).OrderByDescending(x => x.Updated).FirstOrDefaultAsync();
            string updateByResult = await UpdatedByJoin(roles_User, updateBy);

            if (await _repoAccessor.RolesUser.AnyAsync(q => q.Role.RoleSym.ToLower() == "viewonly" && q.UserID == userId && q.Role.GroupIN == 1))
            {
                Roles roleView = await _repoAccessor.Roles.FirstOrDefaultAsync(q => q.RoleSym.ToLower() == "viewonly" && q.GroupIN == 1);
                Roles_User user = new()
                {
                    RoleID = roleView.RoleID,
                    UserID = userId,
                    Updated = _commonService.GetServerTime(),
                    Updated_By = updateByResult
                };
                listRoleUser.Add(user);
            }

            if (key == "mod")
            {
                var roleModerator = await _repoAccessor.Roles.FirstOrDefaultAsync(q => q.RoleSym.ToLower() == "moderator" && q.GroupIN == 1);

                Roles_User r_User = new()
                {
                    RoleID = roleModerator.RoleID,
                    UserID = userId,
                    Updated = _commonService.GetServerTime(),
                    Updated_By = updateByResult
                };
                listRoleUser.Add(r_User);
            }
            _repoAccessor.RolesUser.AddMultiple(listRoleUser);

            await _repoAccessor.SaveChangesAsync();
            return new OperationResult(true, MessageConstants.UPDATE_SUCCESS, MessageConstants.SUCCESS);
        }

        public async Task<OperationResult> RemoveReport(int userId, string key)
        {
            Roles_User roleUser = await _repoAccessor.RolesUser.
                                FirstOrDefaultAsync(q => q.UserID == userId && q.Role.GroupIN == 1 &&
                                q.Role.RoleSym == (key == "mod" ? "moderator" : "viewonly"));

            if (roleUser != null) _repoAccessor.RolesUser.Remove(roleUser);

            await _repoAccessor.SaveChangesAsync();
            return new OperationResult(true, MessageConstants.REMOVE_SUCCESS, MessageConstants.SUCCESS);
        }

        public async Task<OperationResult> DownloadExcel(int userId, string langId)
        {
            var data = await GetAssignRoles(userId, langId);

            ExcelResult excelResult = ExcelUtility.DownloadExcel(data, "Resources\\Template\\Manage\\AssignRoles.xlsx");
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }

        public async Task<List<string>> ListUsers(int roleID)
        {
            var result = await _repoAccessor.Roles.FindAll(x => x.RoleID == roleID)
                        .Join(_repoAccessor.Part.FindAll(),
                            x => x.RoleSym,
                            y => y.PartSym,
                            (x, y) => new { Role = x, Part = y })
                        .Join(_repoAccessor.Employee.FindAll(x => x.Visible == true),
                            x => x.Part.PartID,
                            y => y.PartID,
                            (x, y) => new { x.Role, x.Part, Employee = y })
                        .Select(x => x.Employee.EmpName).ToListAsync();
            return result;
        }

        private async Task<string> UpdatedByJoin(Roles_User roles_User, int updateBy)
        {
            Users user = await _repoAccessor.Users.FirstOrDefaultAsync(x => x.UserID == updateBy);

            string result = $"{user.UserName} ({user.FullName})";

            if (roles_User is null)
                return result;

            List<string> updated = roles_User.Updated_By?.Split(";").ToList();
            if (updated is null)
                return result;

            if (updated.Count > 2)
                updated.RemoveAt(0);

            updated.Add(result);
            for (int i = 0; i < updated.Count - 1; i++)
            {
                if (updated[i] == updated[i + 1])
                {
                    updated.RemoveAt(i);
                    i--;
                }
            }
            return string.Join(";", updated);
        }
    }
}

