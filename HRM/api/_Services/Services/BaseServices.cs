using System.Linq.Expressions;
using API._Repositories;
using API.Data;
using API.DTOs;
using API.Helper.Constant;
using API.Helper.Utilities;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

#nullable enable
namespace API._Services.Services
{
    public class BaseServices
    {
        protected readonly IRepositoryAccessor _repositoryAccessor;
        public record VariableCombine(object Value);
        public enum TableSourceType
        {
            HRMS_Att_Monthly_Detail,
            HRMS_Att_Resign_Monthly_Detail,
            HRMS_Att_Probation_Monthly_Detail
        }
        public BaseServices(DBContext dbContext)
        {
            _repositoryAccessor = new RepositoryAccessor<DBContext>(dbContext);
        }

        #region Query_Permission_Data_Filter
        /// <summary>
        /// Get HRMS_Emp_Personal data by roles
        /// </summary>
        /// <param name="accountRoles">list of roles</param>
        /// <param name="predicate">HRMS_Emp_Personal predicate</param>
        /// 
        /// <returns></returns>
        public async Task<List<HRMS_Emp_Personal>> Query_Permission_Data_Filter(List<string> accountRoles, ExpressionStarter<HRMS_Emp_Personal>? predicate = null)
        {
            List<HRMS_Emp_Personal> result = new();
            var HODD = _repositoryAccessor.HRMS_Org_Direct_Department.FindAll().ToList();
            var HODS = _repositoryAccessor.HRMS_Org_Direct_Section.FindAll(x => x.Direct_Section == "Y").ToList();
            foreach (var accountRole in accountRoles)
            {
                var predicatePersonal = PredicateBuilder.New<HRMS_Emp_Personal>(true);
                if (predicate != null && predicate.IsStarted)
                    predicatePersonal.And(predicate);
                var role = await _repositoryAccessor.HRMS_Basic_Role.FirstOrDefaultAsync(x => x.Role == accountRole);
                if (role == null) continue;
                predicatePersonal.And(x =>
                    (x.Factory == role.Factory || x.Assigned_Factory == role.Factory) &&
                    x.Permission_Group == role.Permission_Group &&
                    role.Level_Start <= x.Position_Grade &&
                    x.Position_Grade <= role.Level_End
                );
                var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predicatePersonal).ToList();
                if (role.Direct == "3")
                    result.AddRange(HEP);
                else
                {
                    predicatePersonal.And(x => HODD.Any(y => y.Factory == x.Factory && y.Division == x.Division && y.Department_Code == x.Department)
                                            || HODD.Any(y => y.Factory == x.Assigned_Factory && y.Division == x.Assigned_Division && y.Department_Code == x.Assigned_Department));
                    predicatePersonal.And(x => HODS.Any(y => y.Factory == x.Factory && y.Division == x.Division && y.Work_Type_Code == x.Work_Type));
                    var filterList = HEP.Where(predicatePersonal);
                    if (role.Direct == "1")
                        result.AddRange(filterList);
                    else
                        result.AddRange(HEP.Except(filterList));
                }
            }
            return result;
        }
        /// <summary>
        /// Get HRMS_Emp_Personal data by roles
        /// </summary>
        /// <param name="accountRoles">list of roles</param>
        /// <param name="predicate">HRMS_Emp_Personal predicate</param>
        /// 
        /// <returns></returns>
        public async Task<List<HRMS_Emp_Personal>> Query_Permission_Data_Filter(string account)
        {
            List<HRMS_Emp_Personal> result = new();
            var HODD = _repositoryAccessor.HRMS_Org_Direct_Department.FindAll().ToList();
            var HODS = _repositoryAccessor.HRMS_Org_Direct_Section.FindAll(x => x.Direct_Section == "Y").ToList();
            var HBR = _repositoryAccessor.HRMS_Basic_Role.FindAll();
            var HBAR = _repositoryAccessor.HRMS_Basic_Account_Role.FindAll(x => x.Account == account);
            var roleList = await HBR
                .Join(HBAR,
                    x => x.Role,
                    y => y.Role,
                    (x, y) => new { HBR = x, HBAR = y })
                .Select(x => x.HBR)
                .ToListAsync();
            foreach (var role in roleList)
            {
                var predicatePersonal = PredicateBuilder.New<HRMS_Emp_Personal>(true);
                predicatePersonal.And(x =>
                    (x.Factory == role.Factory || x.Assigned_Factory == role.Factory) &&
                    x.Permission_Group == role.Permission_Group &&
                    role.Level_Start <= x.Position_Grade &&
                    x.Position_Grade <= role.Level_End
                );
                var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predicatePersonal).ToList();
                if (role.Direct == "3")
                    result.AddRange(HEP);
                else
                {
                    predicatePersonal.And(x => HODD.Any(y => y.Factory == x.Factory && y.Division == x.Division && y.Department_Code == x.Department)
                                            || HODD.Any(y => y.Factory == x.Assigned_Factory && y.Division == x.Assigned_Division && y.Department_Code == x.Assigned_Department));
                    predicatePersonal.And(x => HODS.Any(y => y.Factory == x.Factory && y.Division == x.Division && y.Work_Type_Code == x.Work_Type));
                    var filterList = HEP.Where(predicatePersonal);
                    if (role.Direct == "1")
                        result.AddRange(filterList);
                    else
                        result.AddRange(HEP.Except(filterList));
                }
            }
            return result;
        }
        #endregion
        #region Query_Permission_Group_Filter
        /// <summary>
        /// Get HRMS_Emp_Personal data by roles filter (role.Factory, role.Permission_Group)
        /// </summary>
        /// <param name="accountRoles">list of roles</param>
        /// 
        /// <returns></returns>
        public async Task<List<HRMS_Emp_Personal>> Query_Permission_Group_Filter(List<string> accountRoles)
        {
            List<HRMS_Emp_Personal> result = new();
            foreach (var accountRole in accountRoles)
            {
                var predicatePersonal = PredicateBuilder.New<HRMS_Emp_Personal>(true);
                var role = await _repositoryAccessor.HRMS_Basic_Role.FirstOrDefaultAsync(x => x.Role == accountRole);
                if (role == null) continue;
                predicatePersonal.And(x =>
                    (x.Factory == role.Factory || x.Assigned_Factory == role.Factory) &&
                    x.Permission_Group == role.Permission_Group
                );
                var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predicatePersonal).ToList();
                result.AddRange(HEP);
            }
            return result;
        }
        /// <summary>
        /// Get HRMS_Emp_Personal data by roles filter (role.Factory, role.Permission_Group)
        /// </summary>
        /// <param name="account">Account ID</param>
        /// 
        /// <returns></returns>
        public async Task<List<HRMS_Emp_Personal>> Query_Permission_Group_Filter(string account)
        {
            List<HRMS_Emp_Personal> result = new();
            var HBR = _repositoryAccessor.HRMS_Basic_Role.FindAll();
            var HBAR = _repositoryAccessor.HRMS_Basic_Account_Role.FindAll(x => x.Account == account);
            var roleList = await HBR
                .Join(HBAR,
                    x => x.Role,
                    y => y.Role,
                    (x, y) => new { HBR = x, HBAR = y })
                .Select(x => x.HBR)
                .ToListAsync();
            foreach (var role in roleList)
            {
                var predicatePersonal = PredicateBuilder.New<HRMS_Emp_Personal>(true);
                predicatePersonal.And(x =>
                    (x.Factory == role.Factory || x.Assigned_Factory == role.Factory) &&
                    x.Permission_Group == role.Permission_Group
                );
                var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predicatePersonal).ToList();
                result.AddRange(HEP);
            }
            return result;
        }
        #endregion
        #region GetDataBasicCode
        /// <summary>
        /// Lấy danh sách Divisions theo TypeSeq và Language
        /// </summary>
        /// <param name="typeSeq"> TypeSeq code </param>
        /// <param name="language"> En or Tw </param>
        /// <returns></returns>
        public async Task<List<KeyValuePair<string, string>>> GetDataBasicCode(string typeSeq, string language, bool isSingleValue = false)
        {
            var divisions = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == typeSeq && x.IsActive, true)
                    .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                                    x => new { x.Type_Seq, x.Code },
                                    y => new { y.Type_Seq, y.Code },
                                    (x, y) => new { basicCode = x, basicCode_lang = y })
                                    .SelectMany(x => x.basicCode_lang.DefaultIfEmpty(),
                                    (x, y) => new { x = x.basicCode, basicCode_lang = y })
                .Select(x => new KeyValuePair<string, string>(
                    x.x.Code,
                    !isSingleValue ? $"{x.x.Code} - {(x.basicCode_lang != null ? x.basicCode_lang.Code_Name : x.x.Code_Name)}" : (x.basicCode_lang != null ? x.basicCode_lang.Code_Name : x.x.Code_Name))
                ).ToListAsync();
            return divisions;
        }
        #endregion

        #region IQuery_Code_Lang
        /// <summary>
        /// Get basic code  languages list
        /// </summary>
        /// <param name="Lang"> Lang code </param>
        /// <returns>IQueryable<BasicCodeInfo></returns>
        public IQueryable<BasicCodeInfo> IQuery_Code_Lang(string Lang)
        {
            return _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.IsActive)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == Lang.ToLower()),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new BasicCodeInfo
                {
                    Type_Seq = x.HBC.Type_Seq,
                    Code = x.HBC.Code,
                    Code_Name = x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name,
                    Code_Name_Str = $"{x.HBC.Code}-{(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                });
        }
        #endregion
        #region IQuery_Department_Lang
        /// <summary>
        /// Get department languages list
        /// </summary>
        /// <param name="Factory"> Factory code </param>
        /// <param name="Lang"> Lang code </param>
        /// <returns>IQueryable<DepartmentInfo></returns>
        public IQueryable<DepartmentInfo> IQuery_Department_Lang(string Factory, string Lang)
        {
            return _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == Factory)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Factory == Factory && x.Language_Code.ToLower() == Lang.ToLower()),
                    x => new { x.Division, x.Factory, x.Department_Code },
                    y => new { y.Division, y.Factory, y.Department_Code },
                    (x, y) => new { HOD = x, HODL = y })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, HODL = y })
                .Select(x => new DepartmentInfo
                {
                    Division = x.HOD.Division,
                    Factory = x.HOD.Factory,
                    Department = x.HOD.Department_Code,
                    Department_Code = x.HOD.Department_Code,
                    Department_Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name,
                    Department_Code_Name = $"{x.HOD.Department_Code}-{(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}"
                });
        }
        #endregion
        #region Query_Factory_List
        /// <summary>
        /// Get factory list by division
        /// </summary>
        /// <param name="division">Division code</param>
        /// 
        /// <returns></returns>
        public async Task<List<string>> Query_Factory_List(string division)
        {
            var HBFC = _repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(x => x.Division == division && x.Kind == "1");
            var result = await HBFC.Select(x => x.Factory).ToListAsync();
            return result;
        }
        #endregion

        #region Queryt_Factory_AddList
        /// <summary>
        /// Get factory list by account roles
        /// </summary>
        /// <param name="accountRoles">list of roles</param>
        /// 
        /// <returns></returns>
        public async Task<List<string>> Queryt_Factory_AddList(List<string> accountRoles)
        {
            List<string> result = new();
            var HBR = await _repositoryAccessor.HRMS_Basic_Role.FindAll().ToListAsync();
            foreach (var accountRole in accountRoles)
            {
                var roleInfo = HBR.FirstOrDefault(x => x.Role == accountRole);
                if (roleInfo != null)
                    result.Add(roleInfo.Factory);
            }
            return result;
        }
        /// <summary>
        /// Get factory list by account
        /// </summary>
        /// <param name="account">account(UserName)</param>
        /// 
        /// <returns></returns>
        public async Task<List<string>> Queryt_Factory_AddList(string account)
        {
            var HBR = _repositoryAccessor.HRMS_Basic_Role.FindAll();
            var HBAR = _repositoryAccessor.HRMS_Basic_Account_Role.FindAll(x => x.Account == account);
            var data = HBR.Join(HBAR,
                    x => x.Role,
                    y => y.Role,
                    (x, y) => new { HBR = x, HBAR = y });
            var result = await data.Select(x => x.HBR.Factory).ToListAsync();
            return result;
        }
        #endregion

        public async Task<List<KeyValuePair<string, string>>> Query_Factory_AddList(List<string> roleList, string language)
        {
            var factories = await GetDataBasicCode(BasicCodeTypeConstant.Factory, language);
            var factoriesOfAccount = await _repositoryAccessor.HRMS_Basic_Role.FindAll(x => roleList.Any(r => r == x.Role), true).Select(x => x.Factory).ToListAsync();
            return factories.Join(factoriesOfAccount,
                            x => new { factory = x.Key },
                            factory => new { factory },
                            (x, y) => new KeyValuePair<string, string>(x.Key, x.Value)).Distinct().ToList();
        }

        #region Query_HRMS_Emp_Personal
        /// <summary>
        /// Get employee info by User_Guid
        /// </summary>
        /// <param name="USER_GUID">USER_GUID</param>
        /// 
        /// <returns></returns>
        public async Task<HRMS_Emp_Personal> Query_HRMS_Emp_Personal(string USER_GUID)
        {
            var HEP = await _repositoryAccessor.HRMS_Emp_Personal.FirstOrDefaultAsync(x => x.USER_GUID == USER_GUID);
            return HEP;
        }
        #endregion

        #region Query_EmpPersonal_Add
        /// <summary>
        /// Get add employee info list by factory and employee ID
        /// </summary>
        /// <param name="factory">Factory code</param>
        /// <param name="employee_ID">Employee ID</param>
        /// 
        /// <returns></returns>
        public async Task<List<EmployeeCommonInfo>> Query_EmpPersonal_Add(string factory, string language, string? employee_ID = null)
        {
            var predicateHEP = PredicateBuilder.New<HRMS_Emp_Personal>(true);
            if (!string.IsNullOrWhiteSpace(factory))
                predicateHEP.And(x => x.Factory == factory || x.Assigned_Factory == factory);
            if (!string.IsNullOrWhiteSpace(employee_ID))
                predicateHEP.And(x => x.Employee_ID == employee_ID || x.Assigned_Employee_ID == employee_ID);
            var HEP_info = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predicateHEP).Select(x => new
            {
                HEP = x,
                Actual_Employee_ID = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Employee_ID : x.Employee_ID,
                Actual_Division = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Division : x.Division,
                Actual_Factory = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Factory : x.Factory,
                Actual_Department = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Department : x.Department,
            });
            var HBFC = _repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(x => x.Kind == "1");
            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll();
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower());
            var data = await HEP_info
                .Join(HBFC,
                    last => new { last.HEP.Division, last.HEP.Factory },
                    HBFC => new { HBFC.Division, HBFC.Factory },
                    (x, y) => new { HEP_info = x })
                .GroupJoin(HOD,
                    last => new { Division = last.HEP_info.Actual_Division, Factory = last.HEP_info.Actual_Factory, Department_Code = last.HEP_info.Actual_Department },
                    HOD => new { HOD.Division, HOD.Factory, HOD.Department_Code },
                    (x, y) => new { x.HEP_info, HOD = y })
                .SelectMany(
                    x => x.HOD.DefaultIfEmpty(),
                    (x, y) => new { x.HEP_info, HOD = y })
                .GroupJoin(HODL,
                    last => new { Division = last.HEP_info.Actual_Division, Factory = last.HEP_info.Actual_Factory, Department_Code = last.HEP_info.Actual_Department },
                    HODL => new { HODL.Division, HODL.Factory, HODL.Department_Code },
                    (x, y) => new { x.HEP_info, x.HOD, HODL = y })
                .SelectMany(
                    x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HEP_info, x.HOD, HODL = y })
                .Distinct()
                .ToListAsync();
            if (!data.Any())
                return new List<EmployeeCommonInfo>();
            var result = data.Select(x =>
            {
                string department_Name = x.HODL?.Name ?? x.HOD?.Department_Name ?? "";
                return new EmployeeCommonInfo()
                {
                    USER_GUID = x.HEP_info.HEP.USER_GUID,
                    Employee_ID = x.HEP_info.HEP.Employee_ID,
                    Factory = x.HEP_info.HEP.Factory,
                    Division = x.HEP_info.HEP.Division,
                    Department = x.HEP_info.HEP.Department,
                    Assigned_Employee_ID = x.HEP_info.HEP.Assigned_Employee_ID,
                    Assigned_Factory = x.HEP_info.HEP.Assigned_Factory,
                    Assigned_Division = x.HEP_info.HEP.Assigned_Division,
                    Assigned_Department = x.HEP_info.HEP.Assigned_Department,
                    Local_Full_Name = x.HEP_info.HEP.Local_Full_Name,
                    Work_Type = x.HEP_info.HEP.Work_Type,
                    Permission_Group = x.HEP_info.HEP.Permission_Group,
                    Onboard_Date = x.HEP_info.HEP.Onboard_Date,
                    Onboard_Date_Str = x.HEP_info.HEP.Onboard_Date.ToString("yyyy/MM/dd"),
                    Work_Shift_Type = x.HEP_info.HEP.Work_Shift_Type,
                    Work8hours = x.HEP_info.HEP.Work8hours,
                    Work8hours_Str = x.HEP_info.HEP.Work8hours == true ? "Y" : "N",
                    Employment_Status = x.HEP_info.HEP.Employment_Status,

                    Actual_Employee_ID = x.HEP_info.Actual_Employee_ID,
                    Actual_Factory = x.HEP_info.Actual_Factory,
                    Actual_Division = x.HEP_info.Actual_Division,
                    Actual_Department_Code = x.HEP_info.Actual_Department,
                    Actual_Department_Name = department_Name,
                    Actual_Department_Code_Name = !string.IsNullOrWhiteSpace(department_Name)
                        ? $"{x.HEP_info.Actual_Department}-{department_Name}"
                        : x.HEP_info.Actual_Department
                };
            }).OrderBy(x => x.Employee_ID).ToList();
            return result;
        }
        #endregion

        #region Query_HRMS_Org_Department
        /// <summary>
        /// Get HRMS_Org_Department list by division, factory and department
        /// </summary>
        /// <param name="division">Factory code</param>
        /// <param name="factory">Factory code</param>
        /// <param name="department">Factory code</param>
        /// 
        /// 
        /// <returns></returns>
        public async Task<HRMS_Org_Department?> Query_HRMS_Org_Department(string division, string factory, string department)
        {
            var HOD = await _repositoryAccessor.HRMS_Org_Department.FirstOrDefaultAsync(x => x.Division == division && x.Factory == factory && x.Department_Code == department);
            return HOD;
        }
        public async Task<List<DepartmentInfo>> Query_Department_List(string factory, string language)
        {
            var result = await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == factory, true)
                 .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Factory == factory
                            && x.Language_Code.ToLower() == language.ToLower()),
                         x => new { x.Division, x.Factory, x.Department_Code },
                         y => new { y.Division, y.Factory, y.Department_Code },
                         (x, y) => new { dept = x, hodl = y })
                         .SelectMany(x => x.hodl.DefaultIfEmpty(),
                         (x, y) => new { x.dept, hodl = y })
                          .Select(x => new DepartmentInfo
                          {
                              Department_Code = x.dept.Department_Code,
                              Department_Name = x.hodl != null ? x.hodl.Name : x.dept.Department_Name
                          }).Distinct().ToListAsync();
            return result;
        }
        #endregion

        #region Query_Department_Lang_List
        /// <summary>
        /// Get deparment language list by language code
        /// </summary>
        /// <param name="language">Language code</param>
        /// 
        /// 
        /// <returns></returns>
        public async Task<List<DepartmentInfo>> Query_Department_Lang_List(string language)
        {
            var result = await _repositoryAccessor.HRMS_Org_Department.FindAll(true)
                 .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower()),
                         x => new { x.Division, x.Factory, x.Department_Code },
                         y => new { y.Division, y.Factory, y.Department_Code },
                         (x, y) => new { dept = x, hodl = y })
                         .SelectMany(x => x.hodl.DefaultIfEmpty(),
                         (x, y) => new { x.dept, hodl = y })
                          .Select(x => new DepartmentInfo
                          {
                              Division = x.dept.Division,
                              Factory = x.dept.Factory,
                              Department_Code = x.dept.Department_Code,
                              Department_Name = x.hodl != null ? x.hodl.Name : x.dept.Department_Name
                          }).Distinct().ToListAsync();
            return result;
        }
        #endregion

        #region Query_Department_List
        /// <summary>
        /// Get deparment list by factory
        /// </summary>
        /// <param name="factory">Factory code</param>
        /// 
        /// 
        /// <returns>List<DepartmentInfo></returns>
        public async Task<List<DepartmentInfo>> Query_Department_List(string factory)
        {
            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == factory);
            var HPFC = _repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(x => x.Kind == "1" && x.Factory == factory);
            var result = await HOD.Join(HPFC,
                x => x.Division,
                y => y.Division,
                (x, y) => new DepartmentInfo
                {
                    Division = x.Division,
                    Factory = x.Factory,
                    Department = x.Department_Code,
                    Department_Code = x.Department_Code,
                    Department_Name = x.Department_Name
                }).ToListAsync();
            return result;
        }
        #endregion
        #region Query_Department_List
        /// <summary>
        /// Get deparment list by factory list
        /// </summary>
        /// <param name="factory">Factory code list</param>
        /// 
        /// 
        /// <returns>List<DepartmentInfo></returns>
        public async Task<List<DepartmentInfo>> Query_Department_List(List<string> factory_List)
        {
            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll(x => factory_List.Contains(x.Factory));
            var HBFC = _repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(x => x.Kind == "1" && factory_List.Contains(x.Factory));
            var result = await HOD.Join(HBFC,
                x => new { x.Division, x.Factory },
                y => new { y.Division, y.Factory },
                (x, y) => new DepartmentInfo
                {
                    Division = x.Division,
                    Factory = x.Factory,
                    Department = x.Department_Code,
                    Department_Code = x.Department_Code,
                    Department_Name = x.Department_Name
                }).ToListAsync();
            return result;
        }
        #endregion

        #region Query_AttDepartment
        /// <summary>
        /// Get deparment of employee
        /// </summary>
        /// <param name="personal">HRMS_Emp_Personal</param>
        /// 
        /// 
        /// <returns></returns>
        public async Task<string> Query_AttDepartment(HRMS_Emp_Personal personal)
        {
            List<string> filter = new() { "A", "S" };
            if (personal == null)
                return string.Empty;
            HRMS_Org_Department? data = null;
            if (string.IsNullOrWhiteSpace(personal.Employment_Status))
                data = await Query_HRMS_Org_Department(personal.Division, personal.Factory, personal.Department);
            else
            {
                if (filter.Contains(personal.Employment_Status.Trim()))
                    data = await Query_HRMS_Org_Department(personal.Assigned_Division, personal.Assigned_Factory, personal.Assigned_Department);
            }
            return data != null ? data.Department_Code : personal.Department;
        }
        #endregion

        #region Query_AttDepartment
        /// <summary>
        /// Get deparment of employee
        /// </summary>
        /// <param name="personal">HRMS_Emp_Personal</param>
        /// 
        /// 
        /// <returns></returns>
        public async Task<string> Query_AttDepartment(string USER_GUID)
        {
            List<string> filter = new() { "A", "S" };
            var personal = _repositoryAccessor.HRMS_Emp_Personal.FirstOrDefault(x => x.USER_GUID == USER_GUID);
            if (personal == null)
                return string.Empty;
            HRMS_Org_Department? data = null;
            if (string.IsNullOrWhiteSpace(personal.Employment_Status))
                data = await Query_HRMS_Org_Department(personal.Division, personal.Factory, personal.Department);
            else
            {
                if (filter.Contains(personal.Employment_Status.Trim()))
                    data = await Query_HRMS_Org_Department(personal.Assigned_Division, personal.Assigned_Factory, personal.Assigned_Department);
            }
            return data != null ? data.Department_Code : personal.Department;
        }
        #endregion

        #region Query_AttDepartmentName
        /// <summary>
        /// Get deparment infomations of employee
        /// </summary>
        /// <param name="userGUID">string</param>
        /// <param name="language">string</param>
        /// 
        /// 
        /// <returns></returns>
        public async Task<DepartmentInfo> Query_AttDepartmentName(string userGUID, string language)
        {
            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.USER_GUID == userGUID).Select(x => new
            {
                x.USER_GUID,
                Division = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Division : x.Division,
                Factory = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Factory : x.Factory,
                Department_Code = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Department : x.Department,
            });
            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll().OrderBy(x => x.Department_Code);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower());
            var data = await HEP
                .GroupJoin(HOD,
                    HEP => new { HEP.Division, HEP.Factory, HEP.Department_Code },
                    HOD => new { HOD.Division, HOD.Factory, HOD.Department_Code },
                    (x, y) => new { HEP = x, HOD = y })
                .SelectMany(
                    x => x.HOD.DefaultIfEmpty(),
                    (x, y) => new { x.HEP, HOD = y })
                .GroupJoin(HODL,
                    last => new { last.HEP.Division, last.HEP.Factory, last.HEP.Department_Code },
                    HODL => new { HODL.Division, HODL.Factory, HODL.Department_Code },
                    (x, y) => new { x.HEP, x.HOD, HODL = y })
                .SelectMany(
                    x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HEP, x.HOD, HODL = y })
                .FirstOrDefaultAsync();
            if (data == null)
                return new DepartmentInfo();
            string department_Name = data.HODL?.Name ?? data.HOD?.Department_Name ?? "";
            DepartmentInfo result = new()
            {
                Department_Code = data.HEP.Department_Code,
                Department_Name = department_Name,
                Department_Code_Name = !string.IsNullOrWhiteSpace(department_Name)
                    ? $"{data.HEP.Department_Code} - {department_Name}"
                    : data.HEP.Department_Code
            };
            return result;
        }
        #endregion

        #region Query_Permission_List
        /// <summary>
        /// Get permission group
        /// </summary>
        /// <param name="Factory">string</param>
        /// 
        /// 
        /// <returns></returns>
        public async Task<List<HRMS_Emp_Permission_Group>> Query_Permission_List(string factory, string? foreign_Flag = null)
        {
            var predicateHEPG = PredicateBuilder.New<HRMS_Emp_Permission_Group>(x => x.Factory == factory);
            if (!string.IsNullOrWhiteSpace(foreign_Flag))
                predicateHEPG.And(x => x.Foreign_Flag == foreign_Flag);
            return await _repositoryAccessor.HRMS_Emp_Permission_Group.FindAll(predicateHEPG).ToListAsync();
        }

        public async Task<List<string>> Query_Permission_Group_List(string factory, string? foreign_Flag = null)
        {
            var data = await Query_Permission_List(factory, foreign_Flag);
            return data.Select(x => x.Permission_Group).ToList();
        }

        /// <summary>
        /// Query GetPermistion Group By BasicCode
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public async Task<List<KeyValuePair<string, string>>> Query_BasicCode_PermissionGroup(string factory, string language)
        {
            var permissionGroups = await Query_Permission_Group_List(factory);
            var permisstionGroupsInBasicCode = await GetDataBasicCode(BasicCodeTypeConstant.PermissionGroup, language);
            return permisstionGroupsInBasicCode.Join(permissionGroups,
                            x => new { permissionGroup = x.Key },
                            permissionGroup => new { permissionGroup },
                            (x, y) => new KeyValuePair<string, string>(x.Key, x.Value)).ToList();
        }
        #endregion
        /// <summary>
        /// Kiểm tra 2 đầu dữ liệu có cấu -
        /// </summary>
        /// <param name="firstStr"></param>
        /// <param name="secondStr"></param>
        /// <returns></returns>
        public string CheckValue(string firstStr, string secondStr)
        {
            if (string.IsNullOrWhiteSpace(firstStr) && string.IsNullOrWhiteSpace(secondStr))
                return string.Empty;
            else if (!string.IsNullOrWhiteSpace(firstStr) && string.IsNullOrWhiteSpace(secondStr))
                return firstStr;
            else if (string.IsNullOrWhiteSpace(firstStr) && !string.IsNullOrWhiteSpace(secondStr))
                return secondStr;
            else return $"{firstStr} - {secondStr}";
        }

        public static TimeData ConvertToMinutes(string timeString)
        {
            if (string.IsNullOrEmpty(timeString) || timeString.Length < 4)
                return new TimeData { };

            if (float.TryParse(timeString[..2], out float hours) && float.TryParse(timeString.AsSpan(2, 2), out float minutes))
            {
                return new TimeData { Hours = hours, Minutes = minutes };
            }
            return new TimeData { };
        }

        #region 5.42 - 5.43 : Tính tổng thời gian làm việc của nhân viên theo phòng ban
        /// <summary>
        /// <para> Tính số giờ làm việc của nhân viên không cần bấm thẻ </para>
        /// <para> Tổng số giờ làm việc = Số ngày làm việc * số giờ làm việc thực tế </para>
        /// </summary>
        /// <returns></returns>
        public async Task<decimal> TotalHoursNotSwipeCard(string factory, string employeeId, DateTime firstDateOfMonth, string work_Shift_Type)
        {
            // Số ngày làm việc thực tế 
            var actual_Days = (await _repositoryAccessor.HRMS_Att_Monthly.FirstOrDefaultAsync(x =>
                                                                        x.Factory == factory &&
                                                                        x.Employee_ID == employeeId &&
                                                                        x.Att_Month.Date == firstDateOfMonth.Date))?.Actual_Days ?? 0;

            // Số giờ làm việc
            var work_Hours = (await _repositoryAccessor.HRMS_Att_Work_Shift.FirstOrDefaultAsync(x =>
                                                                        x.Factory == factory &&
                                                                        x.Work_Shift_Type == work_Shift_Type &&
                                                                        x.Week == "1"))?.Work_Hours ?? 0;


            return actual_Days * work_Hours;
        }

        /// <summary>
        /// Tính tổng số giờ làm việc của nhân viên có bấm thẻ
        /// <list type="bullet|number|table">
        ///    <listheader>
        ///        <term><see langword='factory'/></term>
        ///        <description> Mã nhà máy</description>
        ///    </listheader>
        ///    <item>
        ///        <term><see langword='employeeId'/></term>
        ///        <description> Mã nhân viên cần được tính số giờ làm </description>
        ///    </item>
        ///    <item>
        ///        <term><see langword='firstDateOfMonth'/></term>
        ///        <description> Thời gian bắt đầu trong tháng </description>
        ///    </item>
        ///    <item>
        ///        <term><see langword='lastDateOfMonth'/></term>
        ///        <description> Thời gian kết thúc trong tháng </description>
        ///    </item>
        /// </list>
        /// </summary>
        /// <returns>Tổng thời gian</returns>
        public async Task<decimal?> TotalHoursHasSwipeCard(string factory, string employeeId, DateTime firstDateOfMonth, DateTime lastDateOfMonth)
        {
            var results = await _repositoryAccessor.HRMS_Att_Change_Record.FindAll(x => x.Factory == factory
                                && x.Employee_ID == employeeId
                                && x.Att_Date >= firstDateOfMonth && x.Att_Date <= lastDateOfMonth)
                        .GroupJoin(_repositoryAccessor.HRMS_Att_Work_Shift.FindAll(y => y.Effective_State, true),
                            x => new { x.Work_Shift_Type, x.Factory },
                            y => new { y.Work_Shift_Type, y.Factory },
                            (x, y) => new { x, y })
                        .SelectMany(x => x.y.DefaultIfEmpty(), (x, y) => new { x.x, y })

                        .GroupJoin(_repositoryAccessor.HRMS_Att_Leave_Maintain.FindAll(x => x.Leave_code != "D0", true),
                            x => new { x.x.Factory, x.x.Employee_ID, x.x.Att_Date },
                            z => new { z.Factory, z.Employee_ID, Att_Date = z.Leave_Date },
                            (x, z) => new { x.x, x.y, z })
                        .SelectMany(x => x.z.DefaultIfEmpty(), (x, z) => new { x.x, x.y, z })
                        .Select(x => new
                        {
                            Week = x.y != null ? x.y.Week : null,
                            Att_Date = ((int)x.x.Att_Date.DayOfWeek).ToString(),
                            AllHours = x.z == null ? (x.y != null ? x.y.Work_Hours : 0)
                                    : (x.y != null ? x.y.Work_Hours - (x.z.Days * x.y.Work_Hours) : 0)
                        })
                        .ToListAsync();

            results = results.Where(d => d.Week == d.Att_Date).ToList();
            if (results.Count == 0) return null;
            return results.Sum(x => x.AllHours);
        }
        #endregion

        #region Query_DayShift_Meal_Sum
        public async Task<int> Query_DayShift_Meal_Sum(string factory, DateTime startDate, DateTime endDate, string employeeID)
        {
            var work_Shift_Type = new List<string>() { "A0", "B0", "C0" };
            var leave_Codes = new List<string>() { "00", "01", "M0", "N0" };

            var eatTime = await _repositoryAccessor.HRMS_Att_Change_Record.CountAsync(x => x.Factory == factory
                                                                                && x.Employee_ID == employeeID
                                                                                && x.Att_Date >= startDate
                                                                                && x.Att_Date <= endDate
                                                                                && !work_Shift_Type.Contains(x.Work_Shift_Type)
                                                                                && leave_Codes.Contains(x.Leave_Code)
                                                                                && x.Clock_In.CompareTo("1130") <= 0);
            return eatTime;
        }
        #endregion

        #region Query_Ins_Rate_Variable_Combine
        /// <summary>
        /// Get insurance code and ratio based on parameters
        /// </summary>
        /// <param name="Type_Seq"></param>
        /// <param name="Combine_name"></param>
        /// <param name="Employer_name"></param>
        /// <param name="Employee_name"></param>
        /// <param name="Result_name"></param>
        /// <param name="Factory"></param>
        /// <param name="Year_Month"></param>
        /// <param name="Permission_Group"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, VariableCombine>> Query_Ins_Rate_Variable_Combine(
            string Type_Seq, string Combine_name, string Employer_name, string Employee_name,
            string Result_name, string Factory, DateTime Year_Month, string Permission_Group)
        {
            Dictionary<string, VariableCombine> result = new();
            var HIRS = _repositoryAccessor.HRMS_Ins_Rate_Setting.FindAll(x =>
                x.Factory == Factory &&
                x.Permission_Group == Permission_Group
            );
            var maxEffectiveMonth = HIRS
                .Where(y => y.Effective_Month.Date <= Year_Month.Date).DefaultIfEmpty()
                .Max(y => y != null ? y.Effective_Month.Date : DateTime.MinValue);
            await HIRS
                .Where(x => x.Effective_Month.Date == maxEffectiveMonth)
                .ForEachAsync(x =>
                {
                    string keyPrefix = $"{x.Insurance_Type}_{{0}}_{Type_Seq}";
                    result.Add(keyPrefix.GetVariableName(Combine_name), new VariableCombine(x.Insurance_Type));            // combine_char result "V01_Insurance_57"
                    result.Add(keyPrefix.GetVariableName(Employer_name), new VariableCombine(x.Employer_Rate / 100));      // combine_char result "V01_EmployerRate_57"
                    result.Add(keyPrefix.GetVariableName(Employee_name), new VariableCombine(x.Employee_Rate / 100));      // combine_char result "V01_EmployeeRate_57"
                    result.Add(keyPrefix.GetVariableName(Result_name), new VariableCombine(0m));                           // combine_char result "V01_Amt_57"
                });
            return result;
        }
        #endregion

        #region Query_Sal_Detail_Variable_Combine
        /// <summary>
        /// Get salary master/backup file details based on parameters
        /// </summary>
        /// <param name="Kind"></param>
        /// <param name="Type_Seq"></param>
        /// <param name="Combine_Code"></param>
        /// <param name="Combine_name"></param>
        /// <param name="Result_name"></param>
        /// <param name="Factory"></param>
        /// <param name="Year_Month"></param>
        /// <param name="Employee_ID"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, VariableCombine>> Query_Sal_Detail_Variable_Combine(
            string Kind, string Type_Seq, string Combine_Code, string Combine_name,
            string Result_name, string Factory, DateTime Year_Month, string Employee_ID)
        {
            Dictionary<string, VariableCombine> result = new();
            if (Kind == "B")
                await _repositoryAccessor.HRMS_Sal_MasterBackup_Detail
                    .FindAll(x =>
                        x.Factory == Factory &&
                        x.Sal_Month.Date == Year_Month.Date &&
                        x.Employee_ID.ToLower() == Employee_ID.ToLower())
                    .ForEachAsync(x =>
                    {
                        string keyPrefix = $"{x.Salary_Item}_{{0}}_{Type_Seq}";
                        result.Add(keyPrefix.GetVariableName(Combine_Code), new VariableCombine(x.Salary_Item));      // combine_char result "A01_Bkvalue_45"
                        result.Add(keyPrefix.GetVariableName(Combine_name), new VariableCombine((decimal)x.Amount));   // combine_char result "A01_Bkamt_45"
                        result.Add(keyPrefix.GetVariableName(Result_name), new VariableCombine(0m));                   // combine_char result "A01_Bamt_45"
                    });
            else if (Kind == "M")
            {
                await _repositoryAccessor.HRMS_Sal_Master_Detail
                    .FindAll(x =>
                        x.Factory == Factory &&
                        x.Employee_ID.ToLower() == Employee_ID.ToLower())
                    .ForEachAsync(x =>
                    {
                        string keyPrefix = $"{x.Salary_Item}_{{0}}_{Type_Seq}";
                        result.Add(keyPrefix.GetVariableName(Combine_Code), new VariableCombine(x.Salary_Item));       // combine_char result "A01_Mvalue_45"
                        result.Add(keyPrefix.GetVariableName(Combine_name), new VariableCombine((decimal)x.Amount));   // combine_char result "A01_Mainamt_45"
                        result.Add(keyPrefix.GetVariableName(Result_name), new VariableCombine(0m));                   // combine_char result "A01_Mamt_45"
                    });
            }
            else if (Kind == "P")
            {
                await _repositoryAccessor.HRMS_Sal_Probation_MasterBackup_Detail
                    .FindAll(x =>
                        x.Factory == Factory &&
                        x.Sal_Month.Date == Year_Month.Date &&
                        x.Employee_ID.ToLower() == Employee_ID.ToLower())
                    .ForEachAsync(x =>
                    {
                        string keyPrefix = $"{x.Salary_Item}_{{0}}_{Type_Seq}";
                        result.Add(keyPrefix.GetVariableName(Combine_Code), new VariableCombine(x.Salary_Item));      // combine_char result "A01_PBkvalue_45"
                        result.Add(keyPrefix.GetVariableName(Combine_name), new VariableCombine((decimal)x.Amount));   // combine_char result "A01_PBkamt_45"
                        result.Add(keyPrefix.GetVariableName(Result_name), new VariableCombine(0m));                   // combine_char result "A01_Pamt_45"
                    });
            }

            return result;
        }
        #endregion

        #region Query_Att_Monthly_Detail_Variable_Combine
        /// <summary>
        /// Provide salary generation program calculation based on parameter month attendance code variable and string combination
        /// </summary>
        /// <param name="TableSource"></param>
        /// <param name="Type_Seq"></param>
        /// <param name="Combine_Code"></param>
        /// <param name="Combine_name"></param>
        /// <param name="Result_name"></param>
        /// <param name="Factory"></param>
        /// <param name="Year_Month"></param>
        /// <param name="Employee_ID"></param>
        /// <param name="Leave_Type"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, VariableCombine>> Query_Att_Monthly_Detail_Variable_Combine(
            TableSourceType TableSource, string Type_Seq, string Combine_Code, string Combine_name,
            string Result_name, string Factory, DateTime Year_Month, string Employee_ID, string Leave_Type)
        {
            Dictionary<string, VariableCombine> result = TableSource switch
            {
                TableSourceType.HRMS_Att_Monthly_Detail => await Handle_Query_HAMD(Type_Seq, Combine_Code, Combine_name, Result_name, Factory, Year_Month, Employee_ID, Leave_Type),
                TableSourceType.HRMS_Att_Resign_Monthly_Detail => await Handle_Query_HARMD(Type_Seq, Combine_Code, Combine_name, Result_name, Factory, Year_Month, Employee_ID, Leave_Type),
                TableSourceType.HRMS_Att_Probation_Monthly_Detail => await Handle_Query_HAPMD(Type_Seq, Combine_Code, Combine_name, Result_name, Factory, Year_Month, Employee_ID, Leave_Type),
                _ => throw new ArgumentException($"Invalid TableSource: {TableSource}", nameof(TableSource))
            };
            return result;
        }
        private async Task<Dictionary<string, VariableCombine>> Handle_Query_HAMD(string Type_Seq, string Combine_Code, string Combine_name, string Result_name, string Factory, DateTime Year_Month, string Employee_ID, string Leave_Type)
        {
            Dictionary<string, VariableCombine> data = new();
            await _repositoryAccessor.HRMS_Att_Monthly_Detail
                .FindAll(x =>
                    x.Factory == Factory &&
                    x.Att_Month.Date == Year_Month.Date &&
                    x.Employee_ID.ToLower() == Employee_ID.ToLower() &&
                    x.Leave_Type == Leave_Type)
                .ForEachAsync(x =>
                {
                    string keyPrefix = $"{x.Leave_Code}_{{0}}_{Type_Seq}";
                    data.Add(keyPrefix.GetVariableName(Combine_Code), new VariableCombine(x.Leave_Code)); // combine_char result "A01_Leave_40"
                    data.Add(keyPrefix.GetVariableName(Combine_name), new VariableCombine(x.Days));       // combine_char result "A01_LeaveDays_40"
                    data.Add(keyPrefix.GetVariableName(Result_name), new VariableCombine(0m));            // combine_char result "A01_Amt_40"
                });
            return data;
        }
        private async Task<Dictionary<string, VariableCombine>> Handle_Query_HARMD(string Type_Seq, string Combine_Code, string Combine_name, string Result_name, string Factory, DateTime Year_Month, string Employee_ID, string Leave_Type)
        {
            Dictionary<string, VariableCombine> data = new();
            await _repositoryAccessor.HRMS_Att_Resign_Monthly_Detail
                .FindAll(x =>
                    x.Factory == Factory &&
                    x.Att_Month.Date == Year_Month.Date &&
                    x.Employee_ID.ToLower() == Employee_ID.ToLower() &&
                    x.Leave_Type == Leave_Type)
                .ForEachAsync(x =>
                {
                    string keyPrefix = $"{x.Leave_Code}_{{0}}_{Type_Seq}";
                    data.Add(keyPrefix.GetVariableName(Combine_Code), new VariableCombine(x.Leave_Code)); // combine_char result "A01_Leave_40"
                    data.Add(keyPrefix.GetVariableName(Combine_name), new VariableCombine(x.Days));       // combine_char result "A01_LeaveDays_40" 
                    data.Add(keyPrefix.GetVariableName(Result_name), new VariableCombine(0m));            // combine_char result "A01_Amt_40"
                });
            return data;
        }
        private async Task<Dictionary<string, VariableCombine>> Handle_Query_HAPMD(string Type_Seq, string Combine_Code, string Combine_name, string Result_name, string Factory, DateTime Year_Month, string Employee_ID, string Leave_Type)
        {
            Dictionary<string, VariableCombine> data = new();
            await _repositoryAccessor.HRMS_Att_Probation_Monthly_Detail
                .FindAll(x =>
                    x.Factory == Factory &&
                    x.Att_Month.Date == Year_Month.Date &&
                    x.Employee_ID.ToLower() == Employee_ID.ToLower() &&
                    x.Leave_Type == Leave_Type)
                .ForEachAsync(x =>
                {
                    string keyPrefix = $"{x.Leave_Code}_{{0}}_{Type_Seq}";
                    data.Add(keyPrefix.GetVariableName(Combine_Code), new VariableCombine(x.Leave_Code)); // combine_char result "A01_Leave_40"
                    data.Add(keyPrefix.GetVariableName(Combine_name), new VariableCombine(x.Days));       // combine_char result "A01_LeaveDays_40"
                    data.Add(keyPrefix.GetVariableName(Result_name), new VariableCombine(0m));            // combine_char result "A01_Amt_40"
                });
            return data;
        }
        #endregion

        #region Query_WageStandard_Sum
        /// <summary>
        /// Employee overtime pay and insurance calculation standards
        /// </summary>
        /// <param name="Kind"></param>
        /// <param name="Factory"></param>
        /// <param name="Year_Month"></param>
        /// <param name="Employee_ID"></param>
        /// <param name="Permission_Group"></param>
        /// <param name="Salary_Type"></param>
        /// <returns></returns>
        public async Task<decimal> Query_WageStandard_Sum(
            string Kind, string Factory, DateTime Year_Month,
            string Employee_ID, string Permission_Group, string Salary_Type)
        {
            var HSIS = _repositoryAccessor.HRMS_Sal_Item_Settings.FindAll(x =>
                x.Factory == Factory &&
                x.Permission_Group == Permission_Group &&
                x.Salary_Type == Salary_Type
            );
            var maxEffectiveMonth = HSIS
                .Where(y => y.Effective_Month.Date <= Year_Month.Date).DefaultIfEmpty()
                .Max(y => y != null ? y.Effective_Month.Date : DateTime.MinValue);
            var Salary_Code_List = HSIS.Where(x =>
                x.Insurance == "Y" &&
                x.Effective_Month.Date == maxEffectiveMonth
            ).Select(x => x.Salary_Item);

            Task<int> Amount_Values;

            if (Kind == "B")
            {
                Amount_Values = _repositoryAccessor.HRMS_Sal_MasterBackup_Detail
                    .FindAll(x =>
                        x.Factory == Factory &&
                        x.Sal_Month.Date == Year_Month.Date &&
                        x.Employee_ID == Employee_ID &&
                        Salary_Code_List.Contains(x.Salary_Item)
                    ).SumAsync(x => x.Amount);
            }
            else if (Kind == "M")
            {
                Amount_Values = _repositoryAccessor.HRMS_Sal_Master_Detail
                    .FindAll(x =>
                        x.Factory == Factory &&
                        x.Employee_ID == Employee_ID &&
                        Salary_Code_List.Contains(x.Salary_Item)
                    ).SumAsync(x => x.Amount);
            }
            else if (Kind == "P")
            {
                Amount_Values = _repositoryAccessor.HRMS_Sal_Probation_MasterBackup_Detail
                    .FindAll(x =>
                        x.Factory == Factory &&
                        x.Sal_Month.Date == Year_Month.Date &&
                        x.Employee_ID == Employee_ID &&
                        Salary_Code_List.Contains(x.Salary_Item)
                    ).SumAsync(x => x.Amount);
            }
            else
                return 0m;

            return await Amount_Values;
        }
        #endregion
        #region Query_HRMS_Sal_AddDedItem_Values
        /// <summary>
        /// Get the amount of the deduction item setting file based on the parameters
        /// </summary>
        /// <param name="Factory"></param>
        /// <param name="Year_Month"></param>
        /// <param name="Permission_Group"></param>
        /// <param name="Salary_Type"></param>
        /// <param name="AddDed_Type"></param>
        /// <param name="AddDed_Item"></param>
        /// <returns></returns>
        public async Task<decimal> Query_HRMS_Sal_AddDedItem_Values(
            string Factory, DateTime Year_Month, string Permission_Group,
            string Salary_Type, string AddDed_Type, string AddDed_Item)
        {
            var Amount_Values = await _repositoryAccessor.HRMS_Sal_AddDedItem_Settings
                .FindAll(x =>
                    x.Factory == Factory &&
                    x.Permission_Group == Permission_Group &&
                    x.Salary_Type == Salary_Type &&
                    x.AddDed_Type == AddDed_Type &&
                    x.AddDed_Item == AddDed_Item &&
                    x.Effective_Month.Date <= Year_Month.Date)
                .OrderByDescending(y => y.Effective_Month)
                .FirstOrDefaultAsync();
            return Amount_Values?.Amount ?? 0m;
        }
        #endregion
        #region Query_Sal_Monthly_Detail
        // Query_Sal_Monthly_Detail For Single Item
        public async Task<List<Sal_Monthly_Detail_Values>> Query_Sal_Monthly_Detail(string kind, string factory, DateTime yearMonth, string employeeId, string typeSeq, string addedType, string permissionGroup, string salaryType, string leaveType)
        {
            // Sal_Monthly_Detail_Temp
            List<Sal_Monthly_Detail_Temp> Sal_Monthly_Detail_Temp;

            if (kind == "Y")
            {
                Sal_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 x.Employee_ID == employeeId &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .Select(x => new Sal_Monthly_Detail_Temp
                    {
                        Employee_ID = x.Employee_ID,
                        Item = x.Item,
                        Amount = x.Amount
                    })
                    .ToListAsync();
            }
            else if (kind == "N")
            {
                Sal_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 x.Employee_ID == employeeId &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .Select(x => new Sal_Monthly_Detail_Temp
                    {
                        Employee_ID = x.Employee_ID,
                        Item = x.Item,
                        Amount = x.Amount
                    })
                    .ToListAsync();
            }
            else if (kind == "PY")
            {
                Sal_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 x.Employee_ID == employeeId &&
                                 x.Probation == "Y" &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .Select(x => new Sal_Monthly_Detail_Temp
                    {
                        Employee_ID = x.Employee_ID,
                        Item = x.Item,
                        Amount = x.Amount
                    })
                    .ToListAsync();
            }
            else // kind == "PN"
            {
                Sal_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 x.Employee_ID == employeeId &&
                                 x.Probation == "N" &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .Select(x => new Sal_Monthly_Detail_Temp
                    {
                        Employee_ID = x.Employee_ID,
                        Item = x.Item,
                        Amount = x.Amount
                    })
                    .ToListAsync();
            }

            var result = new List<Sal_Monthly_Detail_Values>();
            // Sal_Monthly_Detail_Values
            if (typeSeq == "45")
            {
                // Sal_Setting_Temp
                var Sal_Setting_Temp = await _repositoryAccessor.HRMS_Sal_Item_Settings
                    .FindAll(x => x.Factory == factory &&
                                x.Permission_Group == permissionGroup &&
                                x.Salary_Type == salaryType &&
                                x.Effective_Month <= yearMonth, true)
                    .ToListAsync();

                if (!Sal_Setting_Temp.Any())
                    return new List<Sal_Monthly_Detail_Values>();

                var maxEffectiveMonth = Sal_Setting_Temp.Max(x => x.Effective_Month);

                result = Sal_Monthly_Detail_Temp
                .GroupJoin(Sal_Setting_Temp.Where(x => x.Effective_Month == maxEffectiveMonth),
                    detail => detail.Item,
                    setting => setting.Salary_Item,
                    (detail, settings) => new { detail, settings })
                .SelectMany(x => x.settings.DefaultIfEmpty(),
                    (x, setting) => new Sal_Monthly_Detail_Values
                    {
                        Seq = setting?.Seq ?? 0,
                        Employee_ID = x.detail.Employee_ID,
                        Permission_Group = setting?.Permission_Group,
                        Salary_Type = setting?.Salary_Type,
                        Item = x.detail.Item,
                        Amount = x.detail.Amount,
                    })
                .OrderBy(x => x.Seq)
                .ToList();
            }
            else if (typeSeq == "42")
            {
                // Att_Setting_Temp
                var Att_Setting_Temp = await _repositoryAccessor.HRMS_Att_Use_Monthly_Leave
                .FindAll(x => x.Factory == factory &&
                            x.Leave_Type == leaveType &&
                            x.Effective_Month <= yearMonth, true)
                .ToListAsync();

                if (!Att_Setting_Temp.Any())
                    return new List<Sal_Monthly_Detail_Values>();

                var maxEffectiveMonth = Att_Setting_Temp.Max(x => x.Effective_Month);

                result = Sal_Monthly_Detail_Temp
                .GroupJoin(Att_Setting_Temp.Where(x => x.Effective_Month == maxEffectiveMonth),
                    detail => detail.Item,
                    setting => setting.Code,
                    (detail, settings) => new { detail, settings })
                .SelectMany(x => x.settings.DefaultIfEmpty(),
                    (x, setting) => new Sal_Monthly_Detail_Values
                    {
                        Seq = setting?.Seq ?? 0,
                        Employee_ID = x.detail.Employee_ID,
                        Item = x.detail.Item,
                        Amount = x.detail.Amount,
                    })
                .OrderBy(x => x.Seq)
                .ToList();
            }
            else
                result = Sal_Monthly_Detail_Temp
                .Select(x => new Sal_Monthly_Detail_Values
                {
                    Employee_ID = x.Employee_ID,
                    Item = x.Item,
                    Amount = x.Amount,
                })
                .OrderBy(x => x.Item)
                .ToList();

            return result;
        }

        // Query_Sal_Monthly_Detail For List Item
        public async Task<List<Sal_Monthly_Detail_Values>> Query_Sal_Monthly_Detail(string kind, string factory, DateTime yearMonth, List<string> employeeIds, string typeSeq, string addedType, List<string> permissionGroups, List<string> salaryTypes, string leaveType)
        {
            // Sal_Monthly_Detail_Temp
            List<Sal_Monthly_Detail_Temp> Sal_Monthly_Detail_Temp;

            if (kind == "Y")
            {
                Sal_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID) &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .Select(x => new Sal_Monthly_Detail_Temp
                    {
                        Employee_ID = x.Employee_ID,
                        Item = x.Item,
                        Amount = x.Amount
                    })
                    .ToListAsync();
            }
            else if (kind == "N")
            {
                Sal_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID) &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .Select(x => new Sal_Monthly_Detail_Temp
                    {
                        Employee_ID = x.Employee_ID,
                        Item = x.Item,
                        Amount = x.Amount
                    })
                    .ToListAsync();
            }
            else if (kind == "PY")
            {
                Sal_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID) &&
                                 x.Probation == "Y" &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .Select(x => new Sal_Monthly_Detail_Temp
                    {
                        Employee_ID = x.Employee_ID,
                        Item = x.Item,
                        Amount = x.Amount
                    })
                    .ToListAsync();
            }
            else // kind == "PN"
            {
                Sal_Monthly_Detail_Temp = await _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID) &&
                                 x.Probation == "N" &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .Select(x => new Sal_Monthly_Detail_Temp
                    {
                        Employee_ID = x.Employee_ID,
                        Item = x.Item,
                        Amount = x.Amount
                    })
                    .ToListAsync();
            }

            var result = new List<Sal_Monthly_Detail_Values>();
            // Sal_Monthly_Detail_Values
            if (typeSeq == "45")
            {
                // Sal_Setting_Temp
                var Sal_Setting_Temp = await _repositoryAccessor.HRMS_Sal_Item_Settings
                    .FindAll(x => x.Factory == factory &&
                                permissionGroups.Contains(x.Permission_Group) &&
                                salaryTypes.Contains(x.Salary_Type) &&
                                x.Effective_Month <= yearMonth, true)
                    .ToListAsync();

                if (!Sal_Setting_Temp.Any())
                    return new List<Sal_Monthly_Detail_Values>();

                var maxEffectiveMonth = Sal_Setting_Temp.Max(x => x.Effective_Month);

                result = Sal_Monthly_Detail_Temp
                .GroupJoin(Sal_Setting_Temp.Where(x => x.Effective_Month == maxEffectiveMonth),
                    detail => detail.Item,
                    setting => setting.Salary_Item,
                    (detail, settings) => new { detail, settings })
                .SelectMany(x => x.settings.DefaultIfEmpty(),
                    (x, setting) => new Sal_Monthly_Detail_Values
                    {
                        Seq = setting?.Seq ?? 0,
                        Employee_ID = x.detail.Employee_ID,
                        Permission_Group = setting?.Permission_Group,
                        Salary_Type = setting?.Salary_Type,
                        Item = x.detail.Item,
                        Amount = x.detail.Amount,
                    })
                .OrderBy(x => x.Seq)
                .ToList();
            }
            else if (typeSeq == "42")
            {
                // Att_Setting_Temp
                var Att_Setting_Temp = await _repositoryAccessor.HRMS_Att_Use_Monthly_Leave
                .FindAll(x => x.Factory == factory &&
                            x.Leave_Type == leaveType &&
                            x.Effective_Month <= yearMonth, true)
                .ToListAsync();

                if (!Att_Setting_Temp.Any())
                    return new List<Sal_Monthly_Detail_Values>();

                var maxEffectiveMonth = Att_Setting_Temp.Max(x => x.Effective_Month);

                result = Sal_Monthly_Detail_Temp
                .GroupJoin(Att_Setting_Temp.Where(x => x.Effective_Month == maxEffectiveMonth),
                    detail => detail.Item,
                    setting => setting.Code,
                    (detail, settings) => new { detail, settings })
                .SelectMany(x => x.settings.DefaultIfEmpty(),
                    (x, setting) => new Sal_Monthly_Detail_Values
                    {
                        Seq = setting?.Seq ?? 0,
                        Employee_ID = x.detail.Employee_ID,
                        Item = x.detail.Item,
                        Amount = x.detail.Amount,
                    })
                .OrderBy(x => x.Seq)
                .ToList();
            }
            else
                result = Sal_Monthly_Detail_Temp
                .Select(x => new Sal_Monthly_Detail_Values
                {
                    Employee_ID = x.Employee_ID,
                    Item = x.Item,
                    Amount = x.Amount,
                })
                .OrderBy(x => x.Item)
                .ToList();

            return result;
        }
        #endregion
        #region Query_Sal_Monthly_Detail_Sum
        public async Task<List<SalaryDetailResult>> Query_Sal_Monthly_Detail_Sum(string kind, string factory, DateTime yearMonth, List<string> employeeIds, string typeSeq, string addedType)
        {
            if (kind == "Y")
            {
                return await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID) &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .GroupBy(x => x.Employee_ID)
                    .Select(x => new SalaryDetailResult
                    {
                        Employee_ID = x.Key,
                        Amount = x.Sum(x => (int?)x.Amount ?? 0)
                    })
                    .ToListAsync();
            }
            else if (kind == "N")
            {
                return await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID) &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .GroupBy(x => x.Employee_ID)
                    .Select(x => new SalaryDetailResult
                    {
                        Employee_ID = x.Key,
                        Amount = x.Sum(x => (int?)x.Amount ?? 0)
                    })
                    .ToListAsync();
            }
            else if (kind == "PY")
            {
                return await _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID) &&
                                 x.Probation == "Y" &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .GroupBy(x => x.Employee_ID)
                    .Select(x => new SalaryDetailResult
                    {
                        Employee_ID = x.Key,
                        Amount = x.Sum(x => (int?)x.Amount ?? 0)
                    })
                    .ToListAsync();
            }
            else // kind == "PN"
            {
                return await _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeIds.Contains(x.Employee_ID) &&
                                 x.Probation == "N" &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .GroupBy(x => x.Employee_ID)
                    .Select(x => new SalaryDetailResult
                    {
                        Employee_ID = x.Key,
                        Amount = x.Sum(x => (int?)x.Amount ?? 0)
                    })
                    .ToListAsync();
            }
        }
        #endregion


        public async Task<int> Query_Sal_Monthly_Detail_Sum(string kind, string factory, DateTime yearMonth, string employeeId, string typeSeq, string addedType)
        {
            if (kind == "Y")
            {
                return await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 x.Employee_ID == employeeId &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .SumAsync(x => (int?)x.Amount ?? 0);
            }
            else if (kind == "N")
            {
                return await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 x.Employee_ID == employeeId &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .SumAsync(x => (int?)x.Amount ?? 0);
            }
            else if (kind == "PY")
            {
                return await _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 x.Employee_ID == employeeId &&
                                 x.Probation == "Y" &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .SumAsync(x => (int?)x.Amount ?? 0);
            }
            else // kind == "PN"
            {
                return await _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 x.Employee_ID == employeeId &&
                                 x.Probation == "N" &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .SumAsync(x => (int?)x.Amount ?? 0);
            }
        }
        #region 3.29.Query_NewEmployee_Resign_Actual_Days 
        /// <summary>
        /// Số ngày làm việc thực tế = số ngày làm việc của nhân viên mới/từ chức – số ngày nghỉ phép
        /// </summary>
        /// <returns></returns>
        public async Task<decimal> Query_NewEmployee_Resign_Actual_Days(string Factory, DateTime Start_date, DateTime End_date, string Employee_ID, string HolidayCode)
        {
            int Count_Values = await _repositoryAccessor.HRMS_Att_Change_Record
                                    .CountAsync(x => x.Factory == Factory &&
                                            x.Att_Date.Date >= Start_date.Date &&
                                            x.Att_Date.Date <= End_date.Date &&
                                            x.Employee_ID == Employee_ID &&
                                            x.Leave_Code != "K0" &&
                                            x.Holiday == HolidayCode);

            var sum = await _repositoryAccessor.HRMS_Att_Leave_Maintain
                        .FindAll(x => x.Factory == Factory &&
                            x.Leave_Date.Date >= Start_date.Date &&
                            x.Leave_Date.Date <= End_date.Date &&
                            x.Employee_ID == Employee_ID &&
                            x.Leave_code != "K0")
                        .SumAsync(x => x.Days);
            var result = (decimal)Count_Values - sum;
            return result;
        }
        #endregion
        #region 3.52.Query_Single_Sal_Monthly_Detail  
        /// <summary>
        /// Nhận các khoản khấu trừ lương hàng tháng dựa trên các thông số
        /// </summary>
        /// <param>Kind、Factory、Year Month、Employee ID、Type Seq、AddDed Type、Item</param>
        /// <returns>Sum(Amount)</returns>
        public async Task<int> Query_Single_Sal_Monthly_Detail(string kind, string factory, DateTime yearMonth, string employeeId, string typeSeq, string addedType, string item)
        {
            if (kind == "Y")
            {
                return await _repositoryAccessor.HRMS_Sal_Monthly_Detail.FindAll(x => x.Factory == factory &&
                                                                        x.Sal_Month == yearMonth &&
                                                                        x.Employee_ID == employeeId &&
                                                                        x.Type_Seq == typeSeq &&
                                                                        x.AddDed_Type == addedType &&
                                                                        x.Item == item)
                                                                        .SumAsync(x => (int?)x.Amount ?? 0);
            }
            else /// --kind = N
            {
                return await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail.FindAll(x => x.Factory == factory &&
                                                                        x.Sal_Month == yearMonth &&
                                                                        x.Employee_ID == employeeId &&
                                                                        x.Type_Seq == typeSeq &&
                                                                        x.AddDed_Type == addedType &&
                                                                        x.Item == item)
                                                                        .SumAsync(x => (int?)x.Amount ?? 0);
            }
        }
        public async Task<List<SalaryDetailResult>> Query_Single_Sal_Monthly_Detail(string kind, string factory, DateTime yearMonth, List<string> employeeId, string typeSeq, string addedType, List<string> item)
        {
            if (kind == "Y")
            {
                return await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeId.Contains(x.Employee_ID) &&
                                 item.Contains(x.Item) &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .GroupBy(x => new { x.Employee_ID, x.Item, x.Sal_Month })
                    .Select(x => new SalaryDetailResult
                    {
                        Employee_ID = x.Key.Employee_ID,
                        Item = x.Key.Item,
                        Sal_Month = x.Key.Sal_Month,
                        Amount = x.Sum(x => (int?)x.Amount ?? 0)
                    })
                    .ToListAsync();
            }
            else /// --kind = N
            {
                return await _repositoryAccessor.HRMS_Sal_Resign_Monthly_Detail
                    .FindAll(x => x.Factory == factory &&
                                 x.Sal_Month == yearMonth &&
                                 employeeId.Contains(x.Employee_ID) &&
                                 item.Contains(x.Item) &&
                                 x.Type_Seq == typeSeq &&
                                 x.AddDed_Type == addedType, true)
                    .GroupBy(x => new { x.Employee_ID, x.Item, x.Sal_Month  })
                    .Select(x => new SalaryDetailResult
                    {
                        Employee_ID = x.Key.Employee_ID,
                        Item = x.Key.Item,
                        Sal_Month = x.Key.Sal_Month,
                        Amount = x.Sum(x => (int?)x.Amount ?? 0)
                    })
                    .ToListAsync();
            }
        }
        #endregion
    }
}