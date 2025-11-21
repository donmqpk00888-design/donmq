using API._Repositories;
using API._Services.Interfaces;
using API._Services.Interfaces.Common;
using API.Dtos.Auth;
using API.Helpers.Enums;
using API.Helpers.Hubs;
using API.Helpers.Params;
using API.Helpers.Utilities;
using API.Models;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
namespace API._Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepositoryAccessor _repositoryAccessor;
        private readonly IMapper _mapper;
        private readonly IJwtUtility _jwtUtility;
        private readonly IFunctionUtility _functionUtility;
        private readonly ICommonService _commonService;
        private readonly IHubContext<LoginDetectHub> _loginDetectHub;

        public AuthService(
            IRepositoryAccessor repositoryAccessor,
            IMapper mapper,
            IJwtUtility jwtUtility,
            IFunctionUtility functionUtility,
            ICommonService commonService,
            IHubContext<LoginDetectHub> loginDetectHub)
        {
            _repositoryAccessor = repositoryAccessor;
            _mapper = mapper;
            _jwtUtility = jwtUtility;
            _functionUtility = functionUtility;
            _commonService = commonService;
            _loginDetectHub = loginDetectHub;
        }

        public async Task<UserForLoggedDto> Login(UserForLoginParam userForLogin, UsersDto userDto)
        {
            // Đăng nhập thành công, tiến hành lấy roles
            List<LoggedRolesDto> roles = await GetUserRoles(userDto.UserID);

            // Tạo Token
            string token = _jwtUtility.GenerateJwtToken(userDto);

            // Lưu trạng thái đăng nhập, trừ tài khoản [administrator]
            if (userForLogin.Username.ToLower() != CommonConstants.USER_ADMINISTRATOR)
            {
                LoginDetect detect = new()
                {
                    UserName = userForLogin.Username,
                    Expires = _commonService.GetServerTime().AddDays(30),
                    LoggedAt = _commonService.GetServerTime(),
                    LoggedByIP = userForLogin.IpLocal
                };
                _repositoryAccessor.LoginDetect.Add(detect);
                await _repositoryAccessor.SaveChangesAsync();
            }

            // Tạo đối tượng trả về
            UserForLoggedDto userToReturn = new()
            {
                Username = userDto.UserName,
                FullName = userDto.FullName,
                UserID = userDto.UserID,
                UserRank = userDto.UserRank,
                Roles = roles,
                Token = token
            };

            return userToReturn;
        }

        private async Task<List<LoggedRolesDto>> GetUserRoles(int userID)
        {
            var StatusGroup1 = await CheckGroup2(userID, 1);
            var StatusGroup2 = await CheckGroup2(userID, 2);
            var StatusGroup3 = await CheckGroup2(userID, 3);
            List<LoggedRolesDto> roles = new()
            {
                new LoggedRolesDto
                {
                    RoleSym = RoleConstants.DASHBOARD_LEAVE,
                    Status = await CheckGroup1(userID, 1),
                    Route = "/leave",
                    ImgSrc = "assets/images/leave.jpg",
                    SubRoles = new List<LoggedRolesDto>
                    {
                        new() {
                            RoleSym = RoleConstants.LEAVE_PERSONAL,
                            Status = ((await _repositoryAccessor.Users.FindById(userID))?.UserName?.ToLower()) != "administrator",
                            Route = "/leave/personal",
                            ImgSrc = "assets/images/confirmed.jpg"
                        },
                        new() {
                            RoleSym = RoleConstants.LEAVE_REPRESENTATIVE,
                            Status = StatusGroup1 > 0,
                            Route = "/leave/add",
                            ImgSrc = "assets/images/man-helping.jpg"
                        },
                        new() {
                            RoleSym = RoleConstants.LEAVE_SURROGATE,
                            Status = StatusGroup1 > 0 && ((await _repositoryAccessor.Users.FindById(userID))?.UserName?.ToLower()) != "administrator",
                            Route = "/leave/surrogate",
                            ImgSrc = "assets/images/undraw_Interview_re_e5jn.png"
                        },
                        new() {
                            RoleSym = RoleConstants.LEAVE_APPROVE,
                            Status = (StatusGroup2 > 2) && (StatusGroup3 != 4),
                            Route = "/leave/approval",
                            ImgSrc = "assets/images/management.jpg"
                        },
                        new() {
                            RoleSym = RoleConstants.LEAVE_HISTORY,
                            Status = true,
                            Route = "/leave/history",
                            ImgSrc = "assets/images/time.jpg"
                        },
                        new() {
                            RoleSym = RoleConstants.LEAVE_EDIT_LEAVE,
                            Status = StatusGroup3 >= 3 && StatusGroup3 != 6,
                            Route = "/leave/edit",
                            ImgSrc = "assets/images/hand-drawn.jpg"
                        }
                    }
                },
                new LoggedRolesDto
                {
                    RoleSym = RoleConstants.DASHBOARD_SEAHR,
                    Status = StatusGroup3 >= 4,
                    Route = "/seahr",
                    ImgSrc = "assets/images/seahr.jpg",
                    SubRoles = new List<LoggedRolesDto>
                    {
                        new() {
                            RoleSym = RoleConstants.SEAHR_NEW_EMPLOYEE,
                            Status = StatusGroup3 == 4 || StatusGroup3 == 5,
                            Route = "/seahr/new-employee",
                            ImgSrc = "assets/images/new-team.jpg"
                        },
                        new() {
                            RoleSym = RoleConstants.SEAHR_DELETE_EMPLOYEE,
                            Status = StatusGroup3 == 4 || StatusGroup3 == 5,
                            Route = "/seahr/delete-employee",
                            ImgSrc = "assets/images/waving.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.SEAHR_SEA_CONFIRM,
                            Status = StatusGroup3 >= 4,
                            Route = "/seahr/sea-confirm",
                            ImgSrc = "assets/images/confirmed.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.SEAHR_EDIT_LEAVE,
                            Status = StatusGroup3 == 4 || StatusGroup3 == 5,
                            Route = "/seahr/edit-leave",
                            ImgSrc = "assets/images/illustration.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.SEAHR_REPORT_DATA_DAILY,
                            Status = StatusGroup3 == 4 || StatusGroup3 == 5,
                            Route = "/seahr",
                            ImgSrc = "assets/images/time.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.SEAHR_EXPORT_HP,
                            Status = StatusGroup3 == 4 || StatusGroup3 == 5,
                            Route = "/seahr/export-hp",
                            ImgSrc = "assets/images/list.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.SEAHR_EMP_MANAGEMENT,
                            Status = StatusGroup3 >= 3 && StatusGroup3 != 6,
                            Route = "/seahr/emp-management",
                            ImgSrc = "assets/images/business.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.SEAHR_HISTORY,
                            Status = StatusGroup3 >= 4,
                            Route = "/seahr/history",
                            ImgSrc = "assets/images/isometric.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.SEAHR_VIEW_DATA,
                            Status = StatusGroup3 == 4 || StatusGroup3 == 5,
                            Route = "/seahr/view-data",
                            ImgSrc = "assets/images/stats.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.SEAHR_LEAVE_REPORT,
                            Status = StatusGroup3 == 4 || StatusGroup3 == 5,
                            Route = "/seahr",
                            ImgSrc = "assets/images/chart.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.SEAHR_ADD_MANUALLY,
                            Status = StatusGroup3 == 4 || StatusGroup3 == 5,
                            Route = "/seahr/add-manually",
                            ImgSrc = "assets/images/hand-drawn-2.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.SEAHR_MANAGE_COMMENT_ARCHIVE,
                            Status = StatusGroup3 == 4 || StatusGroup3 == 5,
                            Route = "/seahr/manage-comment-archive",
                            ImgSrc = "assets/images/freelancer.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.SEAHR_PERMISSION_RIGHTS,
                            Status = StatusGroup3 == 4 || StatusGroup3 == 5,
                            Route = "/seahr/permission-rights",
                            ImgSrc = "assets/images/permission.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.SEAHR_ALLOW_LEAVE_SUNDAY,
                            Status = StatusGroup3 == 4 || StatusGroup3 == 5,
                            Route = "/seahr/allow-leave-sunday",
                            ImgSrc = "assets/images/leave-sun.png"
                        },
                    }
                },
                new LoggedRolesDto
                {
                    RoleSym = RoleConstants.DASHBOARD_REPORT,
                    Status = StatusGroup3 >= 4 && StatusGroup3 != 6,
                    Route = "/report",
                    ImgSrc = "assets/images/report.jpg",
                },
                new LoggedRolesDto
                {
                    RoleSym = RoleConstants.DASHBOARD_MANAGE,
                    Status = StatusGroup3 >= 4 && StatusGroup3 != 6,
                    Route = "/manage",
                    ImgSrc = "assets/images/manage.jpg",
                    SubRoles = new List<LoggedRolesDto>
                    {
                        new()
                        {
                            RoleSym = RoleConstants.MANAGE_EMPLOYEE,
                            Status = StatusGroup3 >= 4,
                            Route = "/manage/employee",
                            ImgSrc = "assets/images/internship.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.MANAGE_CATEGORY,
                            Status = StatusGroup3 >= 4,
                            Route = "/manage/category",
                            ImgSrc = "assets/images/isometric.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.MANAGE_DATEPICKER,
                            Status = StatusGroup3 >= 4,
                            Route = "/manage/datepicker",
                            ImgSrc = "assets/images/illustrated.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.MANAGE_USER,
                            Status = StatusGroup3 >= 4,
                            Route = "/manage/user",
                            ImgSrc = "assets/images/work.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.MANAGE_POSITION,
                            Status = StatusGroup3 >= 4,
                            Route = "/manage/position",
                            ImgSrc = "assets/images/management.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.MANAGE_COMPANY,
                            Status = StatusGroup3 >= 4,
                            Route = "/manage/company",
                            ImgSrc = "assets/images/business-2.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.MANAGE_AREA,
                            Status = StatusGroup3 >= 4,
                            Route = "/manage/area",
                            ImgSrc = "assets/images/hand-drawn-2.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.MANAGE_BUILDING,
                            Status = StatusGroup3 >= 4,
                            Route = "/manage/building",
                            ImgSrc = "assets/images/hand-drawn-3.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.MANAGE_DEPARTMENT,
                            Status = StatusGroup3 >= 4,
                            Route = "/manage/department",
                            ImgSrc = "assets/images/hand-drawn.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.MANAGE_TEAM,
                            Status = StatusGroup3 >= 4,
                            Route = "/manage/part",
                            ImgSrc = "assets/images/organic.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.MANAGE_GROUP,
                            Status = StatusGroup3 >= 4,
                            Route = "/manage/group-base",
                            ImgSrc = "assets/images/internship-2.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.MANAGE_HOLIDAY,
                            Status = StatusGroup3 >= 4,
                            Route = "/manage/holiday",
                            ImgSrc = "assets/images/tourists.jpg"
                        },
                        new()
                        {
                            RoleSym = RoleConstants.MANAGE_LUNCHBREAK,
                            Status = StatusGroup3 >= 4,
                            Route = "/manage/lunch-break",
                            ImgSrc = "assets/images/lunch-break.jpg"
                        }
                    }
                },
                new LoggedRolesDto
                {
                    RoleSym = RoleConstants.DASHBOARD_ABOUT,
                    Status = true,
                    Route = "/about",
                    ImgSrc = "assets/images/about.jpg",
                }
            };

            roles = roles.Where(x => x.Status).ToList();
            foreach (var role in roles)
            {
                if (role.SubRoles != null)
                {
                    role.SubRoles = role.SubRoles.Where(x => x.Status).ToList();
                }
            }

            return roles;
        }

        private async Task<bool> CheckGroup1(int? userID, int? group)
        {
            // Kiểm tra quyền theo 3 khu vực: Manage (1) - Approve (2) - Report (3)
            Users user = await _repositoryAccessor.Users.FirstOrDefaultAsync(x => x.UserID == userID);

            return group switch
            {
                1 => user.ISPermitted.Value,
                2 => true,
                _ => user.ISPermitted.Value || await _repositoryAccessor.RolesUser.AnyAsync(q => q.Role.GroupIN == 1 && q.UserID == userID),
            };
        }

        private async Task<int> CheckGroup2(int? userID, int? group)
        {
            // Kiểm tra quyền theo 3 khu vực: Apply (1) - Approve (2)
            Users user = await _repositoryAccessor.Users.FirstOrDefaultAsync(x => x.UserID == userID);

            List<Roles_User> roles = await _repositoryAccessor.RolesUser.FindAll(q => q.UserID == userID).ToListAsync();

            return group switch
            {
                1 => user.UserRank <= 1 || roles.Count <= 0 ? 0 : user.UserRank.Value,
                2 => user.UserRank <= 2 || roles.Count <= 0 ? 0 : user.UserRank.Value,
                _ => user.UserRank < 3 ? 0 : user.UserRank.Value,
            };
        }

        public async Task<int> CountLeaveEdit(int userID)
        {
            List<LeaveData> result = new();
            List<string> allowData = await _functionUtility.CheckAllowData(userID);
            var leaveData = await _repositoryAccessor.LeaveData
                .FindAll(x => x.Status_Line.Value && x.EditRequest == 1 && x.Approved == 2)
                .Join(_repositoryAccessor.Employee.FindAll(),
                    x => x.EmpID,
                    y => y.EmpID,
                    (x, y) => new { LeaveData = x, Employee = y })
                .Join(_repositoryAccessor.Part.FindAll(),
                    x => x.Employee.PartID,
                    y => y.PartID,
                    (x, y) => new { x.LeaveData, x.Employee, Part = y })
                .Join(_repositoryAccessor.Department.FindAll(),
                    x => x.Part.DeptID,
                    y => y.DeptID,
                    (x, y) => new { x.LeaveData, x.Employee, x.Part, Department = y })
                .Join(_repositoryAccessor.Area.FindAll(),
                    x => x.Department.AreaID,
                    y => y.AreaID,
                    (x, y) => new { x.LeaveData, x.Employee, x.Part, x.Department, Area = y })
                .Join(_repositoryAccessor.Building.FindAll(),
                    x => x.Department.BuildingID,
                    y => y.BuildingID,
                    (x, y) => new { x.LeaveData, x.Employee, x.Part, x.Department, x.Area, Building = y })
                .ToListAsync();

            foreach (var item in allowData)
            {
                var group = item.Split('-');
                var tableSym = group[0]; // A: Area, B: Builing, D: Department, P: Part
                var sym = group[1];
                List<LeaveData> data = new();

                switch (group[0])
                {
                    case "A":
                        data = leaveData.Where(x => x.Area.AreaSym == sym).Select(x => x.LeaveData).ToList();
                        result.AddRange(data);
                        break;

                    case "B":
                        data = leaveData.Where(x => x.Building.BuildingSym == sym).Select(x => x.LeaveData).ToList();
                        result.AddRange(data);
                        break;

                    case "D":
                        data = leaveData.Where(x => x.Department.DeptSym == sym).Select(x => x.LeaveData).ToList();
                        result.AddRange(data);
                        break;

                    default: // P
                        data = leaveData.Where(x => x.Part.PartSym == sym).Select(x => x.LeaveData).ToList();
                        result.AddRange(data);
                        break;
                }
            }

            result = result.Distinct().ToList();
            return result.Count;
        }

        public async Task<int> CountSeaHrEdit()
        {
            int result = await _repositoryAccessor.LeaveData
                .FindAll(x => x.Status_Line.Value && x.EditRequest == 2)
                .CountAsync();
            return result;
        }

        public async Task<int> CountSeaHrConfirm()
        {
            int result = await _repositoryAccessor.LeaveData
                .FindAll(x => x.Status_Line.Value && x.EditRequest == 0 && x.Approved == 2)
                .CountAsync();
            return result;
        }

        public async Task<OperationResult> ChangePassword(UserForLoginParam userForLogin)
        {
            // Kiểm tra user có tồn tại không
            Users user = await _repositoryAccessor.Users.FirstOrDefaultAsync(x => x.UserName == userForLogin.Username);
            if (user == null)
                return new OperationResult(false, "System.Message.AccountNotFound");

            // Kiểm tra mật khẩu cũ có khớp không
            string hassPassword = _functionUtility.HashPasswordUser(userForLogin.Password);
            if (user.HashPass != hassPassword)
                return new OperationResult(false, "System.Message.IncorrectPassword");

            // Kiểm tra hoàn tất, tiến hành lưu mật khẩu mới
            user.HashPass = _functionUtility.HashPasswordUser(userForLogin.NewPassword);
            _repositoryAccessor.Users.Update(user);
            await _repositoryAccessor.SaveChangesAsync();
            return new OperationResult(true);
        }

        public async Task<bool> CheckLoggedIn(UserForLoginParam userForLogin)
        {
            LoginDetect detect = await _repositoryAccessor.LoginDetect.FirstOrDefaultAsync(x => x.UserName == userForLogin.Username && x.LoggedByIP != userForLogin.IpLocal);
            return detect != null && detect.Expires > _commonService.GetServerTime();
        }

        public async Task Logout(UserForLoginParam userForLogin)
        {
            LoginDetect detect = await _repositoryAccessor.LoginDetect.FirstOrDefaultAsync(x => x.UserName == userForLogin.Username);
            if (detect != null)
            {
                // Gửi SignalR qua SPA để đăng xuất
                if (userForLogin.IpLocal != detect.LoggedByIP)
                    await _loginDetectHub.Clients.All.SendAsync(CommonConstants.SR_LOGIN_DETECT, detect.LoggedByIP);

                // Xoá dòng hiện tại
                _repositoryAccessor.LoginDetect.Remove(detect);
                await _repositoryAccessor.SaveChangesAsync();
            }
        }

        public async Task<UsersDto> GetUser(UserForLoginParam userForLogin)
        {
            string hassPass = _functionUtility.HashPasswordUser(userForLogin.Password);
            UsersDto userDto = _mapper.Map<UsersDto>(await _repositoryAccessor.Users
                .FirstOrDefaultAsync(x => x.Visible.Value && x.UserName == userForLogin.Username && x.HashPass == hassPass));

            // Không tồn tại username hoặc mật khẩu
            if (userDto == null)
                return null;

            return userDto;
        }
    }
}