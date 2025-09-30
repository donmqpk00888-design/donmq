using API.Data;
using API._Services.Interfaces.SystemMaintenance;
using API.DTOs.SystemMaintenance;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SystemMaintenance
{
    public class S_1_1_4_DirectoryProgramLanguageSetting : BaseServices, I_1_1_4_DirectoryProgramLanguageSetting
    {
        public S_1_1_4_DirectoryProgramLanguageSetting(DBContext dbContext) : base(dbContext)
        {
        }

        #region Add
        public async Task<OperationResult> Add(DirectoryProgramLanguageSetting_Data model, string userName)
        {
            if (await _repositoryAccessor.HRMS_SYS_Program_Language.AnyAsync(x => x.Code.Trim() == model.Code.Trim() || model.Code.Length == 0))
                return new OperationResult { IsSuccess = false, Error = "Code is exists" };
            List<HRMS_SYS_Program_Language> program_Languages = new();
            foreach (var item in model.Langs)
            {
                if (!string.IsNullOrWhiteSpace(item.Lang_Name))
                {
                    HRMS_SYS_Program_Language data = new()
                    {
                        Kind = model.Kind,
                        Code = model.Code,
                        Language_Code = item.Lang_Code.ToUpper(),
                        Name = item.Lang_Name,
                        Update_By = userName,
                        Update_Time = DateTime.Now
                    };
                    program_Languages.Add(data);
                }
            }
            _repositoryAccessor.HRMS_SYS_Program_Language.AddMultiple(program_Languages);
            try
            {
                await _repositoryAccessor.Save();
                return new OperationResult { IsSuccess = true, Error = "IsSuccess" };
            }
            catch
            {
                return new OperationResult { IsSuccess = false, Error = "Error" };
            }

        }
        #endregion

        #region Delete
        public async Task<OperationResult> Delete(string kind, string code)
        {
            var pred = PredicateBuilder.New<HRMS_SYS_Program_Language>(true);
            if (string.IsNullOrEmpty(kind.Trim()))
                return new OperationResult { IsSuccess = false, Error = "Error Kind" };
            if (string.IsNullOrEmpty(code.Trim()))
                return new OperationResult { IsSuccess = false, Error = "Error Code" };

            var dataDelete = _repositoryAccessor.HRMS_SYS_Program_Language.FindAll(x => x.Kind == kind.Trim() && x.Code == code.Trim()).ToList();
            if (dataDelete != null)
            {
                _repositoryAccessor.HRMS_SYS_Program_Language.RemoveMultiple(dataDelete);
                await _repositoryAccessor.Save();
                return new OperationResult { IsSuccess = true };
            }
            return new OperationResult { IsSuccess = false };
        }
        #endregion

        public async Task<PaginationUtility<DirectoryProgramLanguageSetting_Data>> GetData(PaginationParam pagination, DirectoryProgramLanguageSetting_Param param)
        {
            var pred = PredicateBuilder.New<HRMS_SYS_Program_Language>(true);
            if (!string.IsNullOrEmpty(param.Kind))
                pred.And(x => x.Kind.Trim().ToLower().Contains(param.Kind.Trim().ToLower()));
            if (!string.IsNullOrEmpty(param.Code))
                pred.And(x => x.Code.Trim().ToLower().Contains(param.Code.Trim().ToLower()));
            if (!string.IsNullOrEmpty(param.Name))
                pred.And(x => x.Name.Trim().ToLower().Contains(param.Name.Trim().ToLower()));
            var HSL = _repositoryAccessor.HRMS_SYS_Language.FindAll(x => x.IsActive == true).Select(x => x.Language_Code);
            var HSD = _repositoryAccessor.HRMS_SYS_Directory.FindAll()
                .Select(x => new
                {
                    Code = Convert.ToString(x.Directory_Code),
                    Name = Convert.ToString(x.Directory_Name),
                    Kind = "D"
                });
            var HSP = _repositoryAccessor.HRMS_SYS_Program.FindAll()
                .Select(x => new
                {
                    Code = Convert.ToString(x.Program_Code),
                    Name = Convert.ToString(x.Program_Name),
                    Kind = "P"
                });
            var sys_Dir_Pro = HSD.Union(HSP);
            var data = _repositoryAccessor.HRMS_SYS_Program_Language.FindAll(pred)
                .GroupBy(x => new { x.Kind, x.Code })
                .Select(x => new DirectoryProgramLanguageSetting_Data
                {
                    Kind = x.Key.Kind,
                    Code = x.Key.Code,
                    Name = sys_Dir_Pro.FirstOrDefault(y => y.Code == x.Key.Code && y.Kind == x.Key.Kind).Name
                        ?? x.FirstOrDefault(y => !HSL.Any() || HSL.Contains(y.Language_Code)).Name
                });
            return await PaginationUtility<DirectoryProgramLanguageSetting_Data>.CreateAsync(data, pagination.PageNumber, pagination.PageSize);
        }


        public async Task<DirectoryProgramLanguageSetting_Data> GetDetail(string kind, string code)
        {
            var HSD = _repositoryAccessor.HRMS_SYS_Directory.FindAll()
                .Select(x => new
                {
                    Code = Convert.ToString(x.Directory_Code),
                    Name = Convert.ToString(x.Directory_Name),
                    Kind = "D"
                });
            var HSP = _repositoryAccessor.HRMS_SYS_Program.FindAll()
                .Select(x => new
                {
                    Code = Convert.ToString(x.Program_Code),
                    Name = Convert.ToString(x.Program_Name),
                    Kind = "P"
                });
            var HSL = _repositoryAccessor.HRMS_SYS_Language.FindAll(x => x.IsActive == true).Select(x => x.Language_Code);
            var HSPL = _repositoryAccessor.HRMS_SYS_Program_Language.FindAll(x => x.Kind == kind && x.Code == code);

            var langs = await HSL
                .GroupJoin(HSPL,
                    a => a,
                    b => b.Language_Code,
                    (a, b) => new { a, b })
                .SelectMany(x => x.b.DefaultIfEmpty(),
                    (x, y) => new { Language_Code = x.a, y.Name })
                .Select(x => new Language
                {
                    Lang_Code = x.Language_Code,
                    Lang_Name = x.Name
                })
                .ToListAsync();
            var name = HSD.Union(HSP).FirstOrDefault(y => y.Code == code && y.Kind == kind)?.Name
                ?? HSPL.FirstOrDefault(y => !HSL.Any() || HSL.Contains(y.Language_Code))?.Name;
            DirectoryProgramLanguageSetting_Data result = new()
            {
                Kind = kind,
                Code = code,
                Code_Name = !string.IsNullOrWhiteSpace(name) ? $"{code} - {name}" : code,
                Langs = langs
            };
            return result;
        }

        #region Update
        public async Task<OperationResult> Update(DirectoryProgramLanguageSetting_Data model, string userName)
        {
            var res = await _repositoryAccessor.HRMS_SYS_Program_Language.FindAll(x => x.Kind == model.Kind && x.Code == model.Code).ToListAsync();
            if (!res.Any())
                return new OperationResult { IsSuccess = false, Error = "Data not found" };
            _repositoryAccessor.HRMS_SYS_Program_Language.RemoveMultiple(res);
            List<HRMS_SYS_Program_Language> program_Languages = new();
            foreach (var item in model.Langs)
            {
                if (!string.IsNullOrWhiteSpace(item.Lang_Name))
                {
                    HRMS_SYS_Program_Language data = new()
                    {
                        Code = model.Code,
                        Kind = model.Kind,
                        Language_Code = item.Lang_Code.ToUpper(),
                        Name = item.Lang_Name,
                        Update_By = userName,
                        Update_Time = DateTime.Now,
                    };
                    program_Languages.Add(data);
                }
            }
            _repositoryAccessor.HRMS_SYS_Program_Language.AddMultiple(program_Languages);
            try
            {
                await _repositoryAccessor.Save();
                return new OperationResult { IsSuccess = true };
            }
            catch
            {
                return new OperationResult { IsSuccess = false };
            }
        }
        #endregion
        public async Task<List<KeyValuePair<string, string>>> GetLanguage()
        {
            return await _repositoryAccessor.HRMS_SYS_Language.FindAll(x => x.IsActive == true).Select(x => new KeyValuePair<string, string>(x.Language_Code, x.Language_Name)).Distinct().ToListAsync();
        }

        public async Task<List<KeyValuePair<string, string>>> GetCodeProgram()
        {
            return await _repositoryAccessor.HRMS_SYS_Program.FindAll().Select(x => new KeyValuePair<string, string>(x.Program_Code, x.Program_Name)).Distinct().ToListAsync();
        }

        public async Task<List<KeyValuePair<string, string>>> GetCodeDirectory()
        {
            return await _repositoryAccessor.HRMS_SYS_Directory.FindAll().OrderBy(x => x.Seq).Select(x => new KeyValuePair<string, string>(x.Directory_Code, x.Directory_Name)).ToListAsync();
        }
    }
}