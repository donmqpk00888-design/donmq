using System.Drawing;
using System.Globalization;
using AgileObjects.AgileMapper;
using API.Data;
using API._Services.Interfaces.SalaryMaintenance;
using API.DTOs.SalaryMaintenance;
using API.Helper.Constant;
using API.Helper.Params.SalaryMaintenance;
using API.Models;
using Aspose.Cells;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryMaintenance
{
    public class S_7_1_17_MonthlySalaryMasterFileBackupQuery : BaseServices, I_7_1_17_MonthlySalaryMasterFileBackupQuery
    {
        private static readonly SemaphoreSlim semaphore = new(1, 1);
        public S_7_1_17_MonthlySalaryMasterFileBackupQuery(DBContext dbContext) : base(dbContext) { }

        #region GetData
        public async Task<OperationResult> GetDataPagination(PaginationParam paginationParams, MonthlySalaryMasterFileBackupQueryParam param)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;
            return new OperationResult(true, PaginationUtility<D_7_17_MonthlySalaryMasterFileBackupQueryDto>.Create(result.Data as List<D_7_17_MonthlySalaryMasterFileBackupQueryDto>, paginationParams.PageNumber, paginationParams.PageSize));
        }
        public async Task<OperationResult> GetData(MonthlySalaryMasterFileBackupQueryParam param)
        {
            if (!DateTime.TryParseExact(param.Year_Month_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime yearMonthDate))
                return new OperationResult(false, "InvalidInput");
            var predMaster = PredicateBuilder.New<HRMS_Sal_MasterBackup>(x =>
                x.Sal_Month.Year == yearMonthDate.Year &&
                x.Sal_Month.Month == yearMonthDate.Month &&
                x.Factory == param.Factory &&
                param.Permission_Group.Contains(x.Permission_Group)
            );

            var predProbation = PredicateBuilder.New<HRMS_Sal_Probation_MasterBackup>(x =>
                x.Sal_Month.Year == yearMonthDate.Year &&
                x.Sal_Month.Month == yearMonthDate.Month &&
                x.Factory == param.Factory &&
                param.Permission_Group.Contains(x.Permission_Group)
            );

            if (!string.IsNullOrWhiteSpace(param.Department))
            {
                predMaster.And(x => x.Department == param.Department);
                predProbation.And(x => x.Department == param.Department);
            }

            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
            {
                predMaster.And(x => x.Employee_ID.Contains(param.Employee_ID.Trim()));
                predProbation.And(x => x.Employee_ID.Contains(param.Employee_ID.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(param.Position_Title))
            {
                predMaster.And(x => x.Position_Title == param.Position_Title);
                predProbation.And(x => x.Position_Title == param.Position_Title);
            }

            if (!string.IsNullOrWhiteSpace(param.Salary_Type))
            {
                predMaster.And(x => x.Salary_Type == param.Salary_Type);
                predProbation.And(x => x.Salary_Type == param.Salary_Type);
            }

            if (!string.IsNullOrWhiteSpace(param.Salary_Grade) && decimal.TryParse(param.Salary_Grade, out var salaryGrade))
            {
                predMaster.And(x => x.Salary_Grade == salaryGrade);
                predProbation.And(x => x.Salary_Grade == salaryGrade);
            }

            if (!string.IsNullOrWhiteSpace(param.Salary_Level) && decimal.TryParse(param.Salary_Level, out var salaryLevel))
            {
                predMaster.And(x => x.Salary_Level == salaryLevel);
                predProbation.And(x => x.Salary_Level == salaryLevel);
            }

            var HEP_info = _repositoryAccessor.HRMS_Emp_Personal.FindAll()
                .GroupJoin(_repositoryAccessor.HRMS_Emp_Unpaid_Leave.FindAll(x => x.Effective_Status),
                    x => new { x.Division, x.Factory, x.Employee_ID },
                    y => new { y.Division, y.Factory, y.Employee_ID },
                    (x, y) => new { HEP = x, HEUL = y })
                .SelectMany(x => x.HEUL.DefaultIfEmpty(),
                    (x, y) => new { x.HEP, HEUL = y })
                .Select(x => new
                {
                    x.HEP.USER_GUID,
                    x.HEP.Local_Full_Name,
                    x.HEP.Onboard_Date,
                    Employment_Status = x.HEUL != null ? "U" : x.HEP.Deletion_Code
                });

            var HOD_Lang = _repositoryAccessor.HRMS_Org_Department.FindAll()
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower()),
                    x => new { x.Department_Code, x.Factory },
                    y => new { y.Department_Code, y.Factory },
                    (x, y) => new { HOD = x, HODL = y })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, HODL = y })
                .Select(x => new
                {
                    x.HOD.Factory,
                    Department = x.HOD.Department_Code,
                    Department_Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name
                });

            // Thay thế phần code từ dòng 75-95
            var HSPM = _repositoryAccessor.HRMS_Sal_Probation_MasterBackup.FindAll(predProbation)
                .Select(x => new
                {
                    Seq = 1,
                    Probation = "Y",
                    x.USER_GUID,
                    x.Division,
                    x.Factory,
                    x.Sal_Month,
                    x.Employee_ID,
                    x.Department,
                    x.Position_Grade,
                    x.Position_Title,
                    x.Permission_Group,
                    x.ActingPosition_Start,
                    x.ActingPosition_End,
                    x.Technical_Type,
                    x.Expertise_Category,
                    x.Salary_Type,
                    x.Salary_Grade,
                    x.Salary_Level,
                    x.Currency,
                    x.Update_By,
                    x.Update_Time
                });

            var HSM = _repositoryAccessor.HRMS_Sal_MasterBackup.FindAll(predMaster)
                .Select(x => new
                {
                    Seq = 2,
                    Probation = "N",
                    x.USER_GUID,
                    x.Division,
                    x.Factory,
                    x.Sal_Month,
                    x.Employee_ID,
                    x.Department,
                    x.Position_Grade,
                    x.Position_Title,
                    x.Permission_Group,
                    x.ActingPosition_Start,
                    x.ActingPosition_End,
                    x.Technical_Type,
                    x.Expertise_Category,
                    x.Salary_Type,
                    x.Salary_Grade,
                    x.Salary_Level,
                    x.Currency,
                    x.Update_By,
                    x.Update_Time
                });

            var HBC_Lang = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.IsActive)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower()),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Type_Seq,
                    x.HBC.Code,
                    Code_Name = $"{x.HBC.Code}-{(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                });

            var result = await HSPM.Union(HSM)
                .GroupJoin(HEP_info,
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { Backup = x, HEP_info = y })
                .SelectMany(x => x.HEP_info.DefaultIfEmpty(),
                    (x, y) => new { x.Backup, HEP_info = y })
                .GroupJoin(HOD_Lang,
                    x => new { x.Backup.Department, x.Backup.Factory },
                    y => new { y.Department, y.Factory },
                    (x, y) => new { x.Backup, x.HEP_info, HOD_Lang = y })
                .SelectMany(x => x.HOD_Lang.DefaultIfEmpty(),
                    (x, y) => new { x.Backup, x.HEP_info, HOD_Lang = y })
                .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.JobTitle),
                    x => x.Backup.Position_Title,
                    y => y.Code,
                    (x, y) => new { x.Backup, x.HEP_info, x.HOD_Lang, HBC_PositionTitle = y })
                .SelectMany(x => x.HBC_PositionTitle.DefaultIfEmpty(),
                    (x, y) => new { x.Backup, x.HEP_info, x.HOD_Lang, HBC_PositionTitle = y })
                .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup),
                    x => x.Backup.Permission_Group,
                    y => y.Code,
                    (x, y) => new { x.Backup, x.HEP_info, x.HOD_Lang, x.HBC_PositionTitle, HBC_PermissionGroup = y })
                .SelectMany(x => x.HBC_PermissionGroup.DefaultIfEmpty(),
                    (x, y) => new { x.Backup, x.HEP_info, x.HOD_Lang, x.HBC_PositionTitle, HBC_PermissionGroup = y })
                .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.SalaryType),
                    x => x.Backup.Salary_Type,
                    y => y.Code,
                    (x, y) => new { x.Backup, x.HEP_info, x.HOD_Lang, x.HBC_PositionTitle, x.HBC_PermissionGroup, HBC_SalaryType = y })
                .SelectMany(x => x.HBC_SalaryType.DefaultIfEmpty(),
                    (x, y) => new { x.Backup, x.HEP_info, x.HOD_Lang, x.HBC_PositionTitle, x.HBC_PermissionGroup, HBC_SalaryType = y })
                .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.Technical_Type),
                    x => x.Backup.Technical_Type,
                    y => y.Code,
                    (x, y) => new { x.Backup, x.HEP_info, x.HOD_Lang, x.HBC_PositionTitle, x.HBC_PermissionGroup, x.HBC_SalaryType, HBC_TechnicalType = y })
                .SelectMany(x => x.HBC_TechnicalType.DefaultIfEmpty(),
                    (x, y) => new { x.Backup, x.HEP_info, x.HOD_Lang, x.HBC_PositionTitle, x.HBC_PermissionGroup, x.HBC_SalaryType, HBC_TechnicalType = y })
                .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.Expertise_Category),
                    x => x.Backup.Expertise_Category,
                    y => y.Code,
                    (x, y) => new { x.Backup, x.HEP_info, x.HOD_Lang, x.HBC_PositionTitle, x.HBC_PermissionGroup, x.HBC_SalaryType, x.HBC_TechnicalType, HBC_ExpertiseCategory = y })
                .SelectMany(x => x.HBC_ExpertiseCategory.DefaultIfEmpty(),
                    (x, y) => new { x.Backup, x.HEP_info, x.HOD_Lang, x.HBC_PositionTitle, x.HBC_PermissionGroup, x.HBC_SalaryType, x.HBC_TechnicalType, HBC_ExpertiseCategory = y })
                .Select(x => new D_7_17_MonthlySalaryMasterFileBackupQueryDto
                {
                    Seq = x.Backup.Seq,
                    USER_GUID = x.Backup.USER_GUID,
                    Probation = x.Backup.Probation,
                    YearMonth = x.Backup.Sal_Month.ToString("yyyy/MM"),
                    Factory = x.Backup.Factory,
                    Department = x.HOD_Lang != null && !string.IsNullOrWhiteSpace(x.HOD_Lang.Department_Name)
                    ? x.HOD_Lang.Department + "-" + x.HOD_Lang.Department_Name : x.Backup.Department,
                    Employee_ID = x.Backup.Employee_ID,
                    Local_Full_Name = x.HEP_info.Local_Full_Name,
                    Employment_Status = x.HEP_info.Employment_Status,
                    Position_Grade = x.Backup.Position_Grade.ToString(),
                    Position_Title = x.HBC_PositionTitle != null ? x.HBC_PositionTitle.Code_Name : x.Backup.Position_Title,
                    Technical_Type = x.HBC_TechnicalType != null ? x.HBC_TechnicalType.Code_Name : x.Backup.Technical_Type,
                    Expertise_Category = x.HBC_ExpertiseCategory != null ? x.HBC_ExpertiseCategory.Code_Name : x.Backup.Expertise_Category,
                    Permission_Group = x.HBC_PermissionGroup != null ? x.HBC_PermissionGroup.Code_Name : x.Backup.Permission_Group,
                    Salary_Type = x.HBC_SalaryType != null ? x.HBC_SalaryType.Code_Name : x.Backup.Salary_Type,
                    ActingPosition_Start = x.Backup.ActingPosition_Start.Value.ToString("yyyy/MM/dd"),
                    ActingPosition_End = x.Backup.ActingPosition_End.Value.ToString("yyyy/MM/dd"),
                    Onboard_Date = x.HEP_info.Onboard_Date.ToString("yyyy/MM/dd"),
                    Salary_Grade = x.Backup.Salary_Grade.ToString(),
                    Salary_Level = x.Backup.Salary_Level.ToString(),
                    Currency = x.Backup.Currency,
                    Update_By = x.Backup.Update_By,
                    Update_Time = x.Backup.Update_Time.ToString("yyyy/MM/dd HH:mm:ss")
                })
                .OrderBy(x => x.Employee_ID).ThenBy(x => x.Seq)
                .ToListAsync();
            if (!string.IsNullOrWhiteSpace(param.Employment_Status))
                result = result.FindAll(x => x.Employment_Status == param.Employment_Status);
            return new OperationResult(true, result);
        }

        private static string GetCodeName(string code, string typeSeq, List<CodeNameDto> codeLang)
        {
            var item = codeLang.FirstOrDefault(y => y.Code == code && y.Type_Seq == typeSeq);
            if (item is null)
                return code ?? string.Empty;

            return $"{code} - {item.Code_Name}";
        }
        #endregion

        #region GetSalaryDetails
        public async Task<PaginationUtility<SalaryDetailDto>> GetSalaryDetails(PaginationParam pagination, string probation, string factory, string employeeID, string language, string yearMonth)
        {
            var basicCode = _repositoryAccessor.HRMS_Basic_Code.FindAll();
            var basicLanguage = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower());
            var firstDay = DateTime.Parse(yearMonth);
            var codeLang = await basicCode.GroupJoin(basicLanguage,
                x => new { x.Type_Seq, x.Code },
                y => new { y.Type_Seq, y.Code },
                (x, y) => new { Code = x, Language = y })
                .SelectMany(x => x.Language.DefaultIfEmpty(),
                    (x, y) => new { x.Code, Language = y })
                .Select(x => new
                {
                    x.Code.Code,
                    x.Code.Type_Seq,
                    Code_Name = x.Language != null ? x.Language.Code_Name : x.Code.Code_Name
                }).Distinct().ToListAsync();

            var result = new List<SalaryDetailDto>();
            if (probation == "N")
            {
                var salaryDetail = await _repositoryAccessor.HRMS_Sal_MasterBackup_Detail
                    .FindAll(x => x.Factory == factory && x.Employee_ID == employeeID && x.Sal_Month == firstDay, true)
                    .Distinct().ToListAsync();

                result = salaryDetail.Select(x => new SalaryDetailDto
                {
                    Salary_Item = $"{x.Salary_Item} - {codeLang.FirstOrDefault(y => y.Code == x.Salary_Item && y.Type_Seq == "45")?.Code_Name ?? "N/A"}",
                    Amount = x.Amount
                }).ToList();
            }
            else
            {
                var probationDetail = await _repositoryAccessor.HRMS_Sal_Probation_MasterBackup_Detail
                    .FindAll(x => x.Factory == factory && x.Employee_ID == employeeID && x.Sal_Month == firstDay, true)
                    .Distinct().ToListAsync();

                result = probationDetail.Select(x => new SalaryDetailDto
                {
                    Salary_Item = $"{x.Salary_Item} - {codeLang.FirstOrDefault(y => y.Code == x.Salary_Item && y.Type_Seq == "45")?.Code_Name ?? "N/A"}",
                    Amount = x.Amount
                }).ToList();
            }

            return PaginationUtility<SalaryDetailDto>.Create(result, pagination.PageNumber, pagination.PageSize);
        }
        #endregion

        #region GetList
        // List Factory
        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string language, string userName)
        {
            var factorys = await Queryt_Factory_AddList(userName);
            var factories = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory && factorys.Contains(x.Code), true)
                        .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                                    x => new { x.Type_Seq, x.Code },
                                    y => new { y.Type_Seq, y.Code },
                                    (x, y) => new { x, y })
                                    .SelectMany(x => x.y.DefaultIfEmpty(),
                                    (x, y) => new { x.x, y })
                        .Select(x => new KeyValuePair<string, string>(x.x.Code, $"{x.x.Code} - {(x.y != null ? x.y.Code_Name : x.x.Code_Name)}")).ToListAsync();
            return factories;
        }

        // List Department
        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
        {
            var data = await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == factory, true)
                .Join(_repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(b => b.Kind == "1" && b.Factory == factory, true),
                    x => x.Division,
                    y => y.Division,
                    (x, y) => x)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    x => new { x.Factory, x.Department_Code },
                    y => new { y.Factory, y.Department_Code },
                    (x, y) => new { Department = x, Language = y })
                .SelectMany(
                    x => x.Language.DefaultIfEmpty(),
                    (x, y) => new { x.Department, Language = y })
                .OrderBy(x => x.Department.Department_Code)
                .Select(
                    x => new KeyValuePair<string, string>(
                        x.Department.Department_Code,
                        $"{x.Department.Department_Code} - {(x.Language != null ? x.Language.Name : x.Department.Department_Name)}"
                    )
                ).Distinct().ToListAsync();
            return data;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListPositionTitle(string language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.JobTitle, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.PermissionGroup, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListSalaryType(string language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.SalaryType, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListTechnicalType(string language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.Technical_Type, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListExpertiseCategory(string language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.Expertise_Category, language);
        }

        private async Task<List<KeyValuePair<string, string>>> GetHRMS_Basic_Code(string Type_Seq, string language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == Type_Seq, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
        }

        public async Task<OperationResult> Execute(MonthlySalaryMasterFileBackupQueryParam param, string userName)
        {
            await semaphore.WaitAsync();
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {

                if (!DateTime.TryParseExact(param.Year_Month_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime yearMonth))
                {
                    await _repositoryAccessor.RollbackAsync();
                    return new OperationResult(false, "InvalidInput");
                }
                // 1. Check if there is data in the salary closing file. 
                // If it is locked, it cannot be generated again.
                var CNT = await _repositoryAccessor.HRMS_Sal_Close.FindAll(x => x.Factory == param.Factory 
                                && x.Sal_Month.Date == yearMonth.Date, true)
                        .Join(_repositoryAccessor.HRMS_Sal_Monthly.FindAll(true),
                                x => new { x.Factory, x.Sal_Month, x.Employee_ID },
                                y => new { y.Factory, y.Sal_Month, y.Employee_ID },
                                (x, y) => new { HSC = x, HSM = y })
                        .CountAsync();

                if (CNT > 0)
                {
                    await _repositoryAccessor.RollbackAsync();
                    return new OperationResult(false, "AlreadyLocked");
                }
                DateTime now = DateTime.Now;
                // 2. If the verification is passed, 
                // after deleting the data of factory type + salary month, 
                // add the employees in service and those who left in the same month again [Salary Master File]
                var delHSMBs = await _repositoryAccessor.HRMS_Sal_MasterBackup.FindAll(x =>
                    x.Factory == param.Factory &&
                    x.Sal_Month.Date == yearMonth.Date
                ).ToListAsync();

                if (delHSMBs.Any())
                {
                    var delHSMBResult = await CRUD_Data(new MonthlySalaryMasterFileBackupQuery_CRUD("Del_Multi_HRMS_Sal_MasterBackup", new MonthlySalaryMasterFileBackupQuery_General(delHSMBs)));
                    if (!delHSMBResult.IsSuccess)
                    {
                        await _repositoryAccessor.RollbackAsync();
                        return new OperationResult(false, delHSMBResult.Error);
                    }
                }

                var delHSMBDs = await _repositoryAccessor.HRMS_Sal_MasterBackup_Detail.FindAll(x =>
                    x.Factory == param.Factory &&
                    x.Sal_Month.Date == yearMonth.Date
                ).ToListAsync();
                if (delHSMBDs.Any())
                {
                    var delHSMBDResult = await CRUD_Data(new MonthlySalaryMasterFileBackupQuery_CRUD("Del_Multi_HRMS_Sal_MasterBackup_Detail", new MonthlySalaryMasterFileBackupQuery_General(delHSMBDs)));
                    if (!delHSMBDResult.IsSuccess)
                    {
                        await _repositoryAccessor.RollbackAsync();
                        return new OperationResult(false, delHSMBDResult.Error);
                    }
                }
                var Start_Date = yearMonth;
                var End_Date = yearMonth.AddMonths(1).AddDays(-1);

                var HEPs = _repositoryAccessor.HRMS_Emp_Personal.FindAll(x =>
                    x.Factory == param.Factory &&
                    x.Onboard_Date.Date <= End_Date.Date &&
                    (!x.Resign_Date.HasValue || (x.Resign_Date.HasValue && x.Resign_Date.Value.Date > End_Date.Date)) ||
                    (x.Resign_Date.HasValue && x.Resign_Date.Value.Date >= Start_Date.Date && x.Resign_Date.Value.Date <= End_Date.Date)
                ).Select(x => x.Employee_ID);

                var listMaster_Backup = await _repositoryAccessor.HRMS_Sal_Master
                   .FindAll(x => x.Factory == param.Factory && HEPs.Contains(x.Employee_ID))
                   .Project().To<HRMS_Sal_MasterBackup>().ToListAsync();
                if (listMaster_Backup.Any())
                {
                    listMaster_Backup.ForEach(x =>
                    {
                        x.Sal_Month = yearMonth;
                        x.Update_Time = now;
                        x.Update_By = userName;
                    });
                    var insHSMBResult = await CRUD_Data(new MonthlySalaryMasterFileBackupQuery_CRUD("Ins_Multi_HRMS_Sal_MasterBackup", new MonthlySalaryMasterFileBackupQuery_General(listMaster_Backup)));
                    if (!insHSMBResult.IsSuccess)
                    {
                        await _repositoryAccessor.RollbackAsync();
                        return new OperationResult(false, insHSMBResult.Error);
                    }
                }

                var listMasterBackup_Detail = await _repositoryAccessor.HRMS_Sal_Master_Detail
                    .FindAll(x => x.Factory == param.Factory && HEPs.Contains(x.Employee_ID))
                    .Project().To<HRMS_Sal_MasterBackup_Detail>().ToListAsync();
                if (listMasterBackup_Detail.Any())
                {
                    listMasterBackup_Detail.ForEach(x =>
                    {
                        x.Sal_Month = yearMonth;
                        x.Update_Time = now;
                        x.Update_By = userName;
                    });
                    var insHSMBDResult = await CRUD_Data(new MonthlySalaryMasterFileBackupQuery_CRUD("Ins_Multi_HRMS_Sal_MasterBackup_Detail", new MonthlySalaryMasterFileBackupQuery_General(listMasterBackup_Detail)));
                    if (!insHSMBDResult.IsSuccess)
                    {
                        await _repositoryAccessor.RollbackAsync();
                        return new OperationResult(false, insHSMBDResult.Error);
                    }
                }

                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true);
            }
            catch //(Exception e)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false);
            }
            finally
            {
                semaphore.Release();
            }
        }
        private async Task<MonthlySalaryMasterFileBackupQuery_CRUD> CRUD_Data(MonthlySalaryMasterFileBackupQuery_CRUD initial)
        {
            try
            {
                switch (initial.Function)
                {
                    case "Ins_Multi_HRMS_Sal_MasterBackup":
                        _repositoryAccessor.HRMS_Sal_MasterBackup.AddMultiple(initial.Data.HRMS_Sal_MasterBackup_List);
                        initial.IsSuccess = await _repositoryAccessor.Save();
                        break;
                    case "Ins_Multi_HRMS_Sal_MasterBackup_Detail":
                        _repositoryAccessor.HRMS_Sal_MasterBackup_Detail.AddMultiple(initial.Data.HRMS_Sal_MasterBackup_Detail_List);
                        initial.IsSuccess = await _repositoryAccessor.Save();
                        break;
                    case "Del_Multi_HRMS_Sal_MasterBackup":
                        _repositoryAccessor.HRMS_Sal_MasterBackup.RemoveMultiple(initial.Data.HRMS_Sal_MasterBackup_List);
                        initial.IsSuccess = await _repositoryAccessor.Save();
                        break;
                    case "Del_Multi_HRMS_Sal_MasterBackup_Detail":
                        _repositoryAccessor.HRMS_Sal_MasterBackup_Detail.RemoveMultiple(initial.Data.HRMS_Sal_MasterBackup_Detail_List);
                        initial.IsSuccess = await _repositoryAccessor.Save();
                        break;
                    default:
                        initial.IsSuccess = false;
                        break;
                }
                return initial;
            }
            catch //(Exception e)
            {
                initial.IsSuccess = false;
                return initial;
            }
            finally
            {
                if (!initial.IsSuccess)
                    initial.Error = initial.Function switch
                    {
                        "Ins_Multi_HRMS_Sal_MasterBackup" => "InsMultiFailHSM",
                        "Ins_Multi_HRMS_Sal_MasterBackup_Detail" => "InsMultiFailHSMD",
                        "Del_Multi_HRMS_Sal_MasterBackup" => "DelMultiFailHSM",
                        "Del_Multi_HRMS_Sal_MasterBackup_Detail" => "DelMultiFailHSMD",
                        _ => "ExecuteFail"
                    };
            }
        }

        public async Task<OperationResult> DownloadFileExcel(MonthlySalaryMasterFileBackupQueryParam param, string userName)
        {
            var data = await GetData(param);
            if (!(data.Data as List<D_7_17_MonthlySalaryMasterFileBackupQueryDto>).Any())
                return new OperationResult(false, "NoData");

            DateTime now = DateTime.Now;
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.SalaryItem, true).ToList();
            var excelData = new List<dynamic>();

            var salaryItems = await _repositoryAccessor.HRMS_Sal_MasterBackup_Detail.FindAll(x => x.Factory == param.Factory).Select(x => x.Salary_Item)
                .Union(_repositoryAccessor.HRMS_Sal_MasterBackup_Detail.FindAll(x => x.Factory == param.Factory).Select(x => x.Salary_Item))
                .Distinct().OrderBy(x => x).ToListAsync();
            if (!salaryItems.Any())
                return new OperationResult(false, "NoData");

            var data_Detail = await _repositoryAccessor.HRMS_Sal_MasterBackup_Detail.FindAll(x => x.Factory == param.Factory && salaryItems.Contains(x.Salary_Item))
                .Select(x => new MonthlySalaryMasterFileBackupQuery_SalaryItem
                {
                    Probation = "N",
                    Employee_ID = x.Employee_ID,
                    Sal_Month = x.Sal_Month,
                    Amount = x.Amount,
                    Salary_Item = x.Salary_Item,
                }).Union(_repositoryAccessor.HRMS_Sal_Probation_MasterBackup_Detail.FindAll(x => x.Factory == param.Factory && salaryItems.Contains(x.Salary_Item))
                .Select(x => new MonthlySalaryMasterFileBackupQuery_SalaryItem
                {
                    Probation = "Y",
                    Employee_ID = x.Employee_ID,
                    Sal_Month = x.Sal_Month,
                    Amount = x.Amount,
                    Salary_Item = x.Salary_Item,
                })).ToListAsync();
            foreach (var record in data.Data as List<D_7_17_MonthlySalaryMasterFileBackupQueryDto>)
            {
                var row = new Dictionary<string, object>
                {
                    { "Probation", record.Probation },
                    { "Factory", record.Factory },
                    { "Department", record.Department},
                    { "Employee_ID", record.Employee_ID },
                    { "Local_Full_Name", record.Local_Full_Name },
                    { "Employment_Status", record.Employment_Status },
                    { "Position_Grade", record.Position_Grade },
                    { "Position_Title", record.Position_Title},
                    { "ActingPosition_Start", string.IsNullOrWhiteSpace(record.ActingPosition_Start) ? null : ParseDateOrNull(record.ActingPosition_Start)},
                    { "ActingPosition_End", string.IsNullOrWhiteSpace(record.ActingPosition_End) ? null : ParseDateOrNull(record.ActingPosition_Start)},
                    { "Technical_Type", record.Technical_Type},
                    { "Expertise_Category", record.Expertise_Category},
                    { "Onboard_Date", string.IsNullOrWhiteSpace(record.Onboard_Date) ? null : ParseDateOrNull(record.Onboard_Date)},
                    { "Permission_Group", record.Permission_Group},
                    { "Salary_Type", record.Salary_Type},
                    { "Salary_Grade", record.Salary_Grade },
                    { "Salary_Level", record.Salary_Level },
                    { "Currency", record.Currency },
                    { "Update_By", record.Update_By },
                    { "Update_Time", record.Update_Time }
                };

                var salaryItemList = data_Detail
                    .FindAll(x => x.Employee_ID == record.Employee_ID && x.Sal_Month == DateTime.Parse(record.YearMonth) && x.Probation == record.Probation)
                    .Select(item => new SalaryItem
                    {
                        Salary_Item = item.Salary_Item,
                        Amount = item.Amount
                    }).ToList();

                foreach (var salaryItem in salaryItems)
                {
                    var amount = salaryItemList.FirstOrDefault(s => s.Salary_Item == salaryItem)?.Amount ?? 0;

                    row[$"{salaryItem}"] = amount;
                }
                excelData.Add(row);
            }

            MemoryStream memoryStream = new();
            string file = Path.Combine(
                rootPath,
                "Resources\\Template\\SalaryMaintenance\\7_1_17_MonthlySalaryMasterFileBackupQuery\\Download.xlsx"
            );
            WorkbookDesigner obj = new()
            {
                Workbook = new Workbook(file)
            };
            Worksheet worksheet = obj.Workbook.Worksheets[0];

            Style titleStyle = obj.Workbook.CreateStyle();
            titleStyle.Font.IsBold = true;
            titleStyle.ForegroundColor = Color.FromArgb(221, 235, 247);
            titleStyle.Pattern = BackgroundType.Solid;
            titleStyle = AsposeUtility.SetAllBorders(titleStyle);

            var salaryItemTitle = salaryItems.Select(x =>
            {
                var enLang = HBCL.FirstOrDefault(y => y.Language_Code.ToLower() == "en" && y.Code == x);
                var twLang = HBCL.FirstOrDefault(y => y.Language_Code.ToLower() == "tw" && y.Code == x);
                return new MonthlySalaryMasterFileBackupQuery_SalaryItem
                {
                    Salary_Item = x,
                    Salary_Item_Name = enLang != null ? x + " - " + enLang.Code_Name : "",
                    Salary_Item_NameTW = twLang != null ? x + " - " + twLang.Code_Name : "",
                };
            }).ToList();


            for (int i = 0; i < salaryItemTitle.Count; i++)
            {
                worksheet.Cells[4, i + 20].PutValue(salaryItemTitle[i].Salary_Item_NameTW);
                worksheet.Cells[5, i + 20].PutValue(salaryItemTitle[i].Salary_Item_Name);

                // Áp dụng style cho các ô vừa ghi
                worksheet.Cells[4, i + 20].SetStyle(titleStyle);
                worksheet.Cells[5, i + 20].SetStyle(titleStyle);

            }

            DateTime yearMonth = DateTime.Parse(param.Year_Month_Str);
            worksheet.Cells["B2"].PutValue(userName);
            worksheet.Cells["D2"].PutValue(now.ToString("yyyy/MM/dd HH:mm:ss"));
            worksheet.Cells["F2"].PutValue(yearMonth.ToString("yyyy/MM"));

            Style dataStyle = obj.Workbook.CreateStyle();
            dataStyle = AsposeUtility.SetAllBorders(dataStyle);

            // Tạo style cho định dạng ngày tháng
            Style dateStyle = obj.Workbook.CreateStyle();
            dateStyle.Custom = "YYYY/MM/DD";
            dateStyle = AsposeUtility.SetAllBorders(dateStyle);

            //tạo style định dạng amount
            Style dataStyleSalaryItem = obj.Workbook.CreateStyle();
            dataStyleSalaryItem.Custom = "#,##0";
            dataStyleSalaryItem = AsposeUtility.SetAllBorders(dataStyleSalaryItem);
            // Ghi dữ liệu
            for (int i = 0; i < excelData.Count; i++)
            {
                var row = excelData[i];
                int columnIndex = 0;

                foreach (var key in row.Keys)
                {
                    worksheet.Cells[i + 6, columnIndex].PutValue(row[key]);
                    worksheet.Cells[i + 6, columnIndex].SetStyle(dataStyle);
                    worksheet.Cells[i + 6, 8].SetStyle(dateStyle);
                    worksheet.Cells[i + 6, 9].SetStyle(dateStyle);
                    worksheet.Cells[i + 6, 12].SetStyle(dateStyle);
                    columnIndex++;
                }
            }
            worksheet.Cells.CreateRange(6, 20, excelData.Count, salaryItemTitle.Count).SetStyle(dataStyleSalaryItem);
            worksheet.AutoFitColumns();
            obj.Workbook.Save(memoryStream, SaveFormat.Xlsx);
            var excelResult = new ExcelResult(isSuccess: true, memoryStream.ToArray());
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }
        private static readonly string rootPath = Directory.GetCurrentDirectory();

        private static DateTime? ParseDateOrNull(string dateString)
        {
            return DateTime.TryParse(dateString, out DateTime result) && result != DateTime.MinValue
                ? result
                : null;
        }
        #endregion
    }
}