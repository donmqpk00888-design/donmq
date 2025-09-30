using API._Services.Interfaces.RewardAndPenaltyMaintenance;
using API.Data;
using API.DTOs;
using API.DTOs.RewardAndPenaltyReport;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.RewardAndPenaltyReport
{
    public class S_8_2_1_EmployeeRewardAndPenaltyReport : BaseServices, I_8_2_1_EmployeeRewardAndPenaltyReport
    {
        public S_8_2_1_EmployeeRewardAndPenaltyReport(DBContext dbContext) : base(dbContext)
        {
        }

        #region Excel
        public async Task<OperationResult> Download(EmployeeRewardAndPenaltyReportParam param)
        {
            var department = string.Empty;
            var type_Seq = new List<string>()
            {
                BasicCodeTypeConstant.Factory,
                BasicCodeTypeConstant.PermissionGroup,
                BasicCodeTypeConstant.RewardPenaltyType
            };

            var data = GetData(param);

            if (!data.Any())
                return new OperationResult(false, "No data found");

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == param.Factory, true).Select(x => new
            {
                x.USER_GUID,
                x.Local_Full_Name,
                Division = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Division : x.Division,
                Factory = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Factory : x.Factory,
                Department_Code = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Department : x.Department,
            });

            var HOD = _repositoryAccessor.HRMS_Org_Department
                .FindAll(x => x.Factory == param.Factory, true);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Factory == param.Factory && x.Language_Code.ToLower() == param.Language.ToLower(), true);

            var HOD_Lang = HOD
                .GroupJoin(HODL,
                x => new { x.Division, x.Factory, x.Department_Code },
                y => new { y.Division, y.Factory, y.Department_Code },
                (x, y) => new { HOD = x, HODL = y })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                (x, y) => new { x.HOD, HODL = y })
                .Select(x => new
                {
                    x.HOD.Division,
                    x.HOD.Factory,
                    x.HOD.Department_Code,
                    Department_Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name
                });

            var HBC = _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => type_Seq.Contains(x.Type_Seq));
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language
                .FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true);

            var BasicCodeLanguage = HBC
                .GroupJoin(HBCL,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Code,
                    x.HBC.Type_Seq,
                    Code_Name = $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                });

            var HRR = _repositoryAccessor.HRMS_Rew_ReasonCode.FindAll(x => x.Factory == param.Factory, true)
                .Select(x => new
                {
                    x.Code,
                    Code_Name = string.IsNullOrEmpty(x.Code) ? null : (x.Code + "-" + x.Code_Name)
                });

            var result = await data
                .Join(HEP,
                x => x.USER_GUID,
                y => y.USER_GUID,
                (x, y) => new { data = x, HEP = y }).
                GroupJoin(BasicCodeLanguage.Where(x => x.Type_Seq == BasicCodeTypeConstant.RewardPenaltyType),
                x => x.data.Reward_Type,
                y => y.Code,
                (x, y) => new { x.data, x.HEP, RPT = y })
                .SelectMany(x => x.RPT.DefaultIfEmpty(),
                (x, y) => new { x.data, x.HEP, RPT = y })
                .GroupJoin(HRR,
                x => x.data.Reason_Code,
                y => y.Code,
                (x, y) => new { x.data, x.HEP, x.RPT, HRR = y })
                .SelectMany(x => x.HRR.DefaultIfEmpty(),
                (x, y) => new { x.data, x.HEP, x.RPT, HRR = y })
                .GroupJoin(HOD_Lang,
                x => new { x.HEP.Factory, x.HEP.Division, x.HEP.Department_Code },
                y => new { y.Factory, y.Division, y.Department_Code },
                (x, y) => new { x.data, x.HEP, x.RPT, x.HRR, HOD_Lang = y })
                .SelectMany(x => x.HOD_Lang.DefaultIfEmpty(),
                (x, y) => new { x.data, x.HEP, x.RPT, x.HRR, HOD_Lang = y })
                .GroupBy(x => x.data)
                .Select(x => new EmployeeRewardAndPenaltyReportDto
                {
                    Factory = x.Key.Factory,
                    Department = x.FirstOrDefault().HEP.Department_Code,
                    Department_Name = x.FirstOrDefault(y => y.HOD_Lang.Department_Code != null).HOD_Lang.Department_Name,
                    Employee_ID = x.Key.Employee_ID,
                    LocalFullName = x.Key.LocalFullName,
                    Date = x.Key.Date,
                    Reward_Type = x.FirstOrDefault(y => y.RPT.Code != null).RPT.Code_Name ?? x.Key.Reward_Type,
                    Reason_Code = x.FirstOrDefault(y => y.HRR.Code != null).HRR.Code_Name ?? x.Key.Reason_Code,
                    Year_Month = x.Key.Year_Month,
                    CountsOf = x.Key.CountsOf,
                    Remark = x.Key.Remark
                })
                .OrderBy(x => x.Department)
                .ThenBy(x => x.Employee_ID)
                .ThenBy(x => x.Date)
                .ToListAsync();        

            if (!string.IsNullOrWhiteSpace(param.Department))
            {
                department = await HOD_Lang
                .Where(x => x.Department_Code == param.Department)
                .Select(x => $"{x.Department_Code} - {x.Department_Name}").FirstOrDefaultAsync();
            }

            var factory = BasicCodeLanguage
                .FirstOrDefault(x => x.Type_Seq == BasicCodeTypeConstant.Factory
                                  && x.Code == param.Factory).Code_Name;

            var permissionGroup = BasicCodeLanguage
                .Where(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup
                         && param.Permission_Group.Contains(x.Code))
                .Select(x => x.Code_Name);

            var rewardPenalty = param.RewardPenaltyType != null ? BasicCodeLanguage.FirstOrDefault(x =>
               x.Type_Seq == BasicCodeTypeConstant.RewardPenaltyType &&
               x.Code == param.RewardPenaltyType).Code_Name : "";

            List<Cell> cells = new()
            {
                new Cell("B2", factory),
                new Cell("D2", string.Join(", ", permissionGroup)),
                new Cell("F2", rewardPenalty) ,
                new Cell("H2", param.Start_Date),
                new Cell("J2", param.End_Date),
                new Cell("L2", param.Start_Year_Month),
                new Cell("N2", param.End_Year_Month),
                new Cell("B3", department),
                new Cell("D3", param.Employee_ID),
                new Cell("F3", param.Counts),
                new Cell("J3", param.UserName),
                new Cell("L3", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
            };

            List<Table> tables = new()
            {
                new Table("result", result)
            };

            ConfigDownload configDownload = new(true);
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                tables,
                cells,
                "Resources\\Template\\RewardAndPenaltyReport\\8_2_1_EmployeeRewardAndPenaltyReport\\Download.xlsx",
                configDownload
            );
            if (excelResult.IsSuccess)
                return new OperationResult(excelResult.IsSuccess, new { TotalRows = result.Count, Excel = excelResult.Result });
            else
                return new OperationResult(excelResult.IsSuccess, excelResult.Error);
        }
        #endregion

        #region Search
        public async Task<OperationResult> GetTotalRows(EmployeeRewardAndPenaltyReportParam param)
        {
            var data = await GetData(param).CountAsync();
            return new OperationResult(true, data);
        }
        #endregion

        private IQueryable<EmployeeRewardAndPenaltyReportDto> GetData(EmployeeRewardAndPenaltyReportParam param)
        {
            var start_Date = DateTime.Parse(param.Start_Date);
            var end_Date = DateTime.Parse(param.End_Date);

            var minCounts = int.TryParse(param.Counts, out var parsedCounts) && parsedCounts > 1 ? parsedCounts : 1;

            var predHER_Base = PredicateBuilder.New<HRMS_Rew_EmpRecords>(x =>
                x.Factory == param.Factory &&
                x.Reward_Date >= start_Date && x.Reward_Date <= end_Date);

            var predHER = PredicateBuilder.New<HRMS_Rew_EmpRecords>(x =>
                x.Factory == param.Factory &&
                x.Reward_Date >= start_Date && x.Reward_Date <= end_Date);
            if (!string.IsNullOrWhiteSpace(param.Start_Year_Month) && !string.IsNullOrWhiteSpace(param.End_Year_Month))
                predHER = predHER.And(x =>
                    x.Sal_Month.HasValue &&
                    x.Sal_Month.Value >= Convert.ToDateTime(param.Start_Year_Month).Date &&
                    x.Sal_Month.Value <= Convert.ToDateTime(param.End_Year_Month).Date);
            if (!string.IsNullOrWhiteSpace(param.RewardPenaltyType))
                predHER = predHER.And(x => x.Reward_Type == param.RewardPenaltyType);

            var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x => param.Permission_Group.Contains(x.Permission_Group));
            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                predHEP = predHEP.And(x => x.Employee_ID.Contains(param.Employee_ID.Trim()));

            if (!string.IsNullOrWhiteSpace(param.Department))
                predHEP = predHEP.And(x => x.Department == param.Department);

            var HRER = _repositoryAccessor.HRMS_Rew_EmpRecords.FindAll(predHER, true);
            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP, true);

            var result = HRER
                .Join(HEP,
                    x => new { x.Factory, x.Employee_ID },
                    y => new { y.Factory, y.Employee_ID },
                    (x, y) => new { HRER = x, HEP = y })
                .Select(x => new
                {
                    x.HRER.Employee_ID,
                    x.HEP.Department,
                    x.HEP.Local_Full_Name
                }).Distinct()
                .Join(
                    HRER
                    .GroupBy(x => new { x.Employee_ID, x.Reward_Type })
                    .Select(x => new { x.Key.Employee_ID, x.Key.Reward_Type, Reward_Type_CNT = x.Sum(x => (int?)x.Reward_Times) ?? 0 })
                    .Where(x => x.Reward_Type_CNT >= minCounts),
                    x => x.Employee_ID,
                    y => y.Employee_ID,
                    (x, y) => new { x.Employee_ID, x.Local_Full_Name, y.Reward_Type, y.Reward_Type_CNT })
                .GroupJoin(_repositoryAccessor.HRMS_Rew_EmpRecords.FindAll(predHER_Base, true),
                    x => new { x.Employee_ID, x.Reward_Type },
                    y => new { y.Employee_ID, y.Reward_Type },
                    (x, y) => new { BASES = x, DATAS = y })
                .SelectMany(x => x.DATAS.DefaultIfEmpty(),
                    (x, y) => new EmployeeRewardAndPenaltyReportDto
                    {
                        History_GUID = y.History_GUID,
                        USER_GUID = y.USER_GUID,
                        Factory = y.Factory,
                        Employee_ID = y.Employee_ID,
                        LocalFullName = x.BASES.Local_Full_Name,
                        Date = y.Reward_Date,
                        Reward_Type = y.Reward_Type,
                        Reason_Code = y.Reason_Code,
                        Year_Month = y.Sal_Month,
                        CountsOf = y.Reward_Times,
                        Remark = y.Remark
                    }
                );

            return result.AsNoTracking();
        }

        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
        {
            var departments = await Query_Department_List(factory);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Factory == factory && x.Language_Code.ToLower() == language.ToLower())
                .ToList();
            var departmentsWithLanguage = departments
                .GroupJoin(HODL,
                    x => new { x.Division, x.Department_Code },
                    y => new { y.Division, y.Department_Code },
                    (HOD, HODL) => new { HOD, HODL })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, HODL = y })
                .Select(x => new KeyValuePair<string, string>(x.HOD.Department_Code, $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}"))
                .Distinct()
                .ToList();
            return departmentsWithLanguage;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string userName, string language)
        {
            List<string> factories = await Queryt_Factory_AddList(userName);
            var factoriesWithLanguage = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory
                           && factories.Contains(x.Code), true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
            return factoriesWithLanguage;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language)
        {
            var permissionGroups = await Query_Permission_List(factory);

            var permissionGroupsWithLanguage = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup
                           && permissionGroups.Select(y => y.Permission_Group).Contains(x.Code), true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
            return permissionGroupsWithLanguage;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListRewardPenaltyType(string language)
        {
            var rewardPenaltyWithLanguage = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.RewardPenaltyType)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
            return rewardPenaltyWithLanguage;
        }
    }
}