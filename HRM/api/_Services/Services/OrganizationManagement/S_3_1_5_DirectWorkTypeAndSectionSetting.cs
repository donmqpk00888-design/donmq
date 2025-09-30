using AgileObjects.AgileMapper;
using API.Data;
using API._Services.Interfaces.OrganizationManagement;
using API.DTOs.OrganizationManagement;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.OrganizationManagement
{
    public class S_3_1_5_DirectWorkTypeAndSectionSetting : BaseServices, I_3_1_5_DirectWorkTypeAndSectionSetting
    {
        public S_3_1_5_DirectWorkTypeAndSectionSetting(DBContext dbContext) : base(dbContext)
        { }

        public async Task<PaginationUtility<HRMS_Org_Direct_SectionDto>> GetDataPagination(PaginationParam pagination, DirectWorkTypeAndSectionSettingParam param)
        {
            var data = await GetData(param);
            return PaginationUtility<HRMS_Org_Direct_SectionDto>.Create(data, pagination.PageNumber, pagination.PageSize);
        }

        private IQueryable<BasicCodeLanguageView> GetBasicSequence(string typeSeq, string languageCode, string char1)
        {
            var predicate = PredicateBuilder.New<HRMS_Basic_Code>(true);
            if (!string.IsNullOrWhiteSpace(typeSeq))
                predicate.And(x => x.Type_Seq == typeSeq);
            if (!string.IsNullOrWhiteSpace(char1))
                predicate.And(x => x.Char1 == char1);
            return _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == typeSeq && x.Char1 == char1, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language
                    .FindAll(x => x.Language_Code.ToLower() == languageCode.ToLower(), true),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new BasicCodeLanguageView
                {
                    Code = x.HBC.Code,
                    Name = x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name
                }).AsQueryable();
        }
        public async Task<List<HRMS_Org_Direct_SectionDto>> GetData(DirectWorkTypeAndSectionSettingParam param)
        {
            var predicate = PredicateBuilder.New<HRMS_Org_Direct_Section>(true);

            if (!string.IsNullOrWhiteSpace(param.Division))
                predicate.And(x => x.Division == param.Division);
            if (!string.IsNullOrWhiteSpace(param.Factory))
                predicate.And(x => x.Factory == param.Factory);
            if (!string.IsNullOrWhiteSpace(param.Effective_Date))
                predicate.And(x => x.Effective_Date.ToLower().Contains(param.Effective_Date.ToLower()));
            if (!string.IsNullOrWhiteSpace(param.Work_Type_Code))
                predicate.And(x => x.Work_Type_Code == param.Work_Type_Code);
            if (!string.IsNullOrWhiteSpace(param.Section_Code))
                predicate.And(x => x.Section_Code == param.Section_Code);

            var section = GetBasicSequence("6", param.Lang, "Y");
            var workType = GetBasicSequence("5", param.Lang, null);

            var result = await _repositoryAccessor.HRMS_Org_Direct_Section.FindAll(predicate)
            .GroupJoin(
                section,
                x => x.Section_Code,
                y => y.Code,
                (x, y) => new { OrgDirectSection = x, Section = y }
            )
            .SelectMany(
                x => x.Section.DefaultIfEmpty(),
                (x, y) => new { x.OrgDirectSection, Section = y }
            )
            .GroupJoin(
                workType,
                x => x.OrgDirectSection.Work_Type_Code,
                y => y.Code,
                (x, y) => new { x.OrgDirectSection, x.Section, WorkType = y }
            )
            .SelectMany(
                x => x.WorkType.DefaultIfEmpty(),
                (x, y) => new { x.OrgDirectSection, x.Section, WorkType = y }

            ).Select(
                x => new HRMS_Org_Direct_SectionDto
                {
                    Division = x.OrgDirectSection.Division,
                    Factory = x.OrgDirectSection.Factory,
                    Effective_Date = x.OrgDirectSection.Effective_Date,
                    Work_Type_Code = x.OrgDirectSection.Work_Type_Code,
                    Section_Code = x.OrgDirectSection.Section_Code,
                    Direct_Section = x.OrgDirectSection.Direct_Section,
                    Section_Code_Name = x.Section.Name != null ? x.OrgDirectSection.Section_Code + " - " + x.Section.Name : x.OrgDirectSection.Section_Code,
                    Work_Type_Code_Name = x.WorkType.Name ?? "",
                    Update_By = x.OrgDirectSection.Update_By,
                    Update_Time = x.OrgDirectSection.Update_Time
                }
            ).ToListAsync();

            return result;

        }

        public async Task<OperationResult> DownloadFileExcel(DirectWorkTypeAndSectionSettingParam param)
        {
            var data = await GetData(param);
            if (!data.Any())
                return new OperationResult(false, "System.Message.NoData");

            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                data, 
                "Resources\\Template\\OrganizationManagement\\3_1_5_DirectWorkTypeAndSectionSetting\\Download.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }


        public async Task<OperationResult> Create(HRMS_Org_Direct_SectionDto data)
        {
            if (await _repositoryAccessor.HRMS_Org_Direct_Section.AnyAsync(x =>
                    x.Division == data.Division &&
                    x.Factory == data.Factory &&
                    x.Effective_Date == data.Effective_Date &&
                    x.Work_Type_Code == data.Work_Type_Code &&
                    x.Section_Code == data.Section_Code))

                return new OperationResult(false, "System.Message.DataExisted");

            var dataNew = Mapper.Map(data).ToANew<HRMS_Org_Direct_Section>(x => x.MapEntityKeys());

            _repositoryAccessor.HRMS_Org_Direct_Section.Add(dataNew);

            try
            {
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.CreateOKMsg");
            }
            catch (Exception)
            {
                return new OperationResult(false, "System.Message.CreateErrorMsg");
            }

        }

        public async Task<OperationResult> Update(HRMS_Org_Direct_SectionDto data)
        {
            var item = await _repositoryAccessor.HRMS_Org_Direct_Section.FirstOrDefaultAsync(x => x.Division == data.Division &&
                    x.Factory == data.Factory &&
                    x.Effective_Date == data.Effective_Date &&
                    x.Work_Type_Code == data.Work_Type_Code &&
                    x.Section_Code == data.Section_Code);

            if (item == null)
                return new OperationResult(false, "System.Message.NoData");

            item = Mapper.Map(data).Over(item);

            _repositoryAccessor.HRMS_Org_Direct_Section.Update(item);
            await _repositoryAccessor.Save();

            try
            {
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.UpdateOKMsg");
            }
            catch (Exception)
            {
                return new OperationResult(false, "System.Message.UpdateErrorMsg");
            }
        }

        public async Task<List<KeyValuePair<string, string>>> GetListDivision(string language)
        {
            return await GetBasicCode("1", language, null);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string division, string language)
        {
            var pred = PredicateBuilder.New<HRMS_Basic_Factory_Comparison>(true);
            if (!string.IsNullOrWhiteSpace(division))
                pred = pred.And(x => x.Division.ToLower().Contains(division.ToLower()));
            var data = await _repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(pred)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower()),
                    x => x.Factory,
                    y => y.Code,
                    (x, y) => new { x.Factory, CodeNameLanguage = y.Select(z => z.Code_Name).FirstOrDefault() })
                .Join(_repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "2"),
                    x => x.Factory,
                    y => y.Code,
                    (x, y) => new { x.Factory, x.CodeNameLanguage, CodeName = y.Code_Name })
                .Select(x => new KeyValuePair<string, string>(x.Factory, x.CodeNameLanguage ?? x.CodeName))
                .Distinct().ToListAsync();

            if (!data.Any())
            {
                var allFactories = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "2")
                    .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower()),
                        x => x.Code,
                        y => y.Code,
                        (x, y) => new { x.Code, NameCode = x.Code_Name, NameLanguage = y.Select(z => z.Code_Name).FirstOrDefault() })
                    .Select(x => new KeyValuePair<string, string>(x.Code, x.NameLanguage ?? x.NameCode))
                    .ToListAsync();
                return allFactories;
            }

            return data;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListWorkType(string language)
        {
            return await GetBasicCode("5", language, null);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListSection(string language)
        {
            return await GetBasicCode("6", language, "Y");
        }

        private async Task<List<KeyValuePair<string, string>>> GetBasicCode(string typeSeq, string language, string chart1)
        {
            var predicate = PredicateBuilder.New<HRMS_Basic_Code>(true);
            if (!string.IsNullOrWhiteSpace(typeSeq))
                predicate.And(x => x.Type_Seq == typeSeq);
            if (!string.IsNullOrWhiteSpace(chart1))
                predicate.And(x => x.Char1 == chart1);
            return await _repositoryAccessor.HRMS_Basic_Code.FindAll(predicate)
                   .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower()),
                                   x => new { x.Type_Seq, x.Code },
                                   y => new { y.Type_Seq, y.Code },
                                   (x, y) => new { x, y })
                                   .SelectMany(x => x.y.DefaultIfEmpty(),
                                   (x, y) => new { BasicCode = x.x, BasicCodeLanguage = y })
               .Select(x => new KeyValuePair<string, string>(x.BasicCode.Code, $"{x.BasicCode.Code} - {(x.BasicCodeLanguage != null ? x.BasicCodeLanguage.Code_Name : x.BasicCode.Code_Name)}")).ToListAsync();
        }

        public async Task<OperationResult> CheckDuplicate(DirectWorkTypeAndSectionSettingParam param)
        {
            var predicate = PredicateBuilder.New<HRMS_Org_Direct_Section>(true);
            if (!string.IsNullOrWhiteSpace(param.Division))
                predicate.And(x => x.Division == param.Division);
            if (!string.IsNullOrWhiteSpace(param.Factory))
                predicate.And(x => x.Factory == param.Factory);
            if (!string.IsNullOrWhiteSpace(param.Effective_Date))
                predicate.And(x => x.Effective_Date.ToLower().Contains(param.Effective_Date.ToLower()));
            if (!string.IsNullOrWhiteSpace(param.Work_Type_Code))
                predicate.And(x => x.Work_Type_Code == param.Work_Type_Code);
            bool result = await _repositoryAccessor.HRMS_Org_Direct_Section.AnyAsync(predicate);
            return new OperationResult(!result);
        }
    }
}