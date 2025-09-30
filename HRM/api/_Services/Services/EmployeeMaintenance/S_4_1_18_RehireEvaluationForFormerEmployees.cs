using API.Data;
using API._Services.Interfaces.EmployeeMaintenance;
using API.DTOs.EmployeeMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.EmployeeMaintenance
{
    public class S_4_1_18_RehireEvaluationForFormerEmployees : BaseServices, I_4_1_18_RehireEvaluationForFormerEmployees
    {
        public S_4_1_18_RehireEvaluationForFormerEmployees(DBContext dbContext) : base(dbContext)
        {
        }

        public async Task<OperationResult> Create(RehireEvaluationForFormerEmployeesEvaluation dto)
        {
            if (await _repositoryAccessor.HRMS_Emp_Rehire_Evaluation.AnyAsync(x =>
                    x.USER_GUID == dto.USER_GUID && x.Seq == dto.Seq))
                return new OperationResult(false, "System.Message.DataExisted");

            if (await _repositoryAccessor.HRMS_Emp_Personal.AnyAsync(x =>
                    x.Factory == dto.Factory && x.Division == dto.Division && x.Employee_ID == dto.EmployeeID))
                return new OperationResult(false, "System.Message.DataExisted");

            var dataCreate = new HRMS_Emp_Rehire_Evaluation
            {
                USER_GUID = dto.USER_GUID,
                Employee_ID = dto.EmployeeID,
                Nationality = dto.Nationality,
                Identification_Number = dto.Identification_Number,
                Results = dto.Results,
                Explanation = dto.Explanation,
                Division = dto.Division,
                Factory = dto.Factory,
                Department = dto.Department,
                Seq = dto.Seq,
                Update_By = dto.Update_By,
                Update_Time = dto.Update_Time
            };
            try
            {
                _repositoryAccessor.HRMS_Emp_Rehire_Evaluation.Add(dataCreate);
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.CreateOKMsg");
            }
            catch (Exception)
            {
                return new OperationResult(false, "System.Message.CreateErrorMsg");
            }
        }

        public async Task<PaginationUtility<RehireEvaluationForFormerEmployeesDto>> GetDataPagination(PaginationParam pagination, RehireEvaluationForFormerEmployeesParam param)
        {
            var pred = PredicateBuilder.New<HRMS_Emp_Rehire_Evaluation>(true);
            if (!string.IsNullOrWhiteSpace(param.Nationality))
                pred = pred.And(x => x.Nationality == param.Nationality);
            if (!string.IsNullOrWhiteSpace(param.Identification_Number))
                pred = pred.And(x => x.Identification_Number == param.Identification_Number);


            var data = await _repositoryAccessor.HRMS_Emp_Rehire_Evaluation.FindAll(pred, true)
            .GroupJoin(
                _repositoryAccessor.HRMS_Emp_Personal.FindAll(true),
                x => new { x.Nationality, x.Identification_Number },
                y => new { y.Nationality, y.Identification_Number },
                (x, y) => new { Evaluation = x, Personal = y }
            )
            .SelectMany(
                x => x.Personal.DefaultIfEmpty(),
                (x, y) => new { x.Evaluation, Personal = y }
            )
            .GroupJoin(
                _repositoryAccessor.HRMS_Emp_Resignation.FindAll(true),
                x => new { x.Personal.Nationality, x.Personal.Identification_Number },
                y => new { y.Nationality, y.Identification_Number },
                (x, y) => new { x.Evaluation, x.Personal, Resignation = y }
            )
            .SelectMany(
                x => x.Resignation.DefaultIfEmpty(),
                (x, y) => new { x.Evaluation, x.Personal, Resignation = y }
            )
            .Select(x => new RehireEvaluationForFormerEmployeesDto
            {
                Personal = new RehireEvaluationForFormerEmployeesPersonal()
                {
                    Division = x.Personal.Division,
                    Factory = x.Personal.Factory,
                    Department = x.Personal.Department,
                    EmployeeID = x.Personal.Employee_ID,
                    Local_Full_Name = x.Personal.Local_Full_Name,
                    Blacklist = x.Personal.Blacklist,
                    Onboard_Date = x.Personal.Onboard_Date,
                    Date_of_Resignation = x.Personal.Resign_Date,
                    Resign_Reason = x.Resignation.Resign_Reason,
                    Resign_Type = x.Resignation.Resignation_Type,
                    USER_GUID = x.Evaluation.USER_GUID
                },
                Evaluation = new RehireEvaluationForFormerEmployeesEvaluation()
                {
                    Nationality = x.Personal.Nationality,
                    Identification_Number = x.Personal.Identification_Number,
                    Seq = x.Evaluation.Seq,
                    Results = x.Evaluation.Results,
                    Factory = x.Evaluation.Factory,
                    Division = x.Evaluation.Division,
                    EmployeeID = x.Evaluation.Employee_ID,
                    Update_By = x.Evaluation.Update_By,
                    Update_Time = x.Evaluation.Update_Time,
                    Explanation = x.Evaluation.Explanation,
                    Department = x.Evaluation.Department,
                    USER_GUID = x.Evaluation.USER_GUID
                }

            }).ToListAsync();

            return PaginationUtility<RehireEvaluationForFormerEmployeesDto>.Create(data, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<OperationResult> Update(RehireEvaluationForFormerEmployeesEvaluation dto)
        {
            var item = await _repositoryAccessor.HRMS_Emp_Rehire_Evaluation.FirstOrDefaultAsync(x =>
                x.USER_GUID == dto.USER_GUID &&
                x.Seq == dto.Seq, true);
            if (item == null)
                return new OperationResult(false, "System.Message.NoData");
            if (await _repositoryAccessor.HRMS_Emp_Personal.AnyAsync(x =>
                x.Factory == dto.Factory &&
                x.Division == dto.Division &&
                x.Employee_ID == dto.EmployeeID))
                return new OperationResult(false, "System.Message.DataExisted");
            item.Employee_ID = dto.EmployeeID;
            item.Nationality = dto.Nationality;
            item.Identification_Number = dto.Identification_Number;
            item.Results = dto.Results;
            item.Explanation = dto.Explanation;
            item.Division = dto.Division;
            item.Factory = dto.Factory;
            item.Department = dto.Department;
            item.Seq = dto.Seq;
            item.Update_By = dto.Update_By;
            item.Update_Time = dto.Update_Time;
            try
            {
                _repositoryAccessor.HRMS_Emp_Rehire_Evaluation.Update(item);
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.UpdateOKMsg");
            }
            catch (Exception)
            {
                return new OperationResult(false, "System.Message.UpdateErrorMsg");
            }
        }

        public async Task<List<KeyValuePair<string, string>>> GetListResignationType(string language)
        => await GetBasicCode(BasicCodeTypeConstant.ResignationType, language);
        public async Task<List<KeyValuePair<string, string>>> GetListReasonforResignation(string language)
        => await GetBasicCode(BasicCodeTypeConstant.ReasonResignation, language);
        public async Task<List<KeyValuePair<string, string>>> GetListDivision(string language)
        => await GetBasicCode(BasicCodeTypeConstant.Division, language);

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string language, string division)
        {
            var pred = PredicateBuilder.New<HRMS_Basic_Factory_Comparison>(x => x.Kind == "1");
            if (!string.IsNullOrWhiteSpace(division))
                pred.And(x => x.Division.ToLower() == division.Trim().ToLower());

            var data = await _repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(pred, true)
                    .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                        x => x.Factory,
                        y => y.Code,
                        (x, y) => new { x.Factory, CodeNameLanguage = y.Select(z => z.Code_Name).FirstOrDefault() })
                    .Join(_repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "2", true),
                    x => x.Factory,
                    y => y.Code,
                    (x, y) => new { x.Factory, x.CodeNameLanguage, CodeName = y.Code_Name })
                    .Select(x => new KeyValuePair<string, string>(
                        x.Factory,
                        x.CodeNameLanguage ?? x.CodeName))
                    .Distinct()
                    .ToListAsync();

            return data;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string language, string factory, string division)
        {
            return await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Division == division && x.Factory == factory, true)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                      HOD => new { HOD.Division, HOD.Factory, HOD.Department_Code },
                      HODL => new { HODL.Division, HODL.Factory, HODL.Department_Code },
                    (HOD, HODL) => new { HOD, HODL })
                    .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (prev, HODL) => new { prev.HOD, HODL })
                .Select(x => new KeyValuePair<string, string>(x.HOD.Department_Code, $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}"))
                .ToListAsync();
        }

        public async Task<int> GetMaxSeq(string nationality, string identification_Number)
        {
            var dataExist = await _repositoryAccessor.HRMS_Emp_Rehire_Evaluation
                .FindAll(x => x.Nationality == nationality && x.Identification_Number == identification_Number)
                .ToListAsync();

            if (!dataExist.Any())
                return 1;

            var seqList = new List<int>(dataExist.Select(x => x.Seq));
            var max_seq = seqList.Max();

            var result = Enumerable.Range(1, max_seq + 1)
                .Except(seqList)
                .ToList();

            return result.FirstOrDefault();
        }

        private async Task<List<KeyValuePair<string, string>>> GetBasicCode(string type_Seq, string language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == type_Seq, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
            .ToListAsync();
        }

        public async Task<RehireEvaluationForFormerEmployeesPersonal> GetDetail(string nationality, string identification_Number)
        {
            var pred = PredicateBuilder.New<HRMS_Emp_Personal>(true);
            if (!string.IsNullOrWhiteSpace(nationality))
                pred = pred.And(x => x.Nationality == nationality);
            if (!string.IsNullOrWhiteSpace(identification_Number))
                pred = pred.And(x => x.Identification_Number == identification_Number);

            var checkData = await _repositoryAccessor.HRMS_Emp_Personal
                .FirstOrDefaultAsync(pred, true);

            if (checkData == null)
                return new();

            var seqValue = GetMaxSeq(nationality, identification_Number);

            var data = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(pred, true)
            .GroupJoin(_repositoryAccessor.HRMS_Emp_Resignation.FindAll(true),
                        x => new { x.Nationality, x.Identification_Number },
                        y => new { y.Nationality, y.Identification_Number },
                        (x, y) => new { Personal = x, Resignation = y })
            .SelectMany(x => x.Resignation.DefaultIfEmpty(), (x, y) => new { x.Personal, Resignation = y })
            .Select(x => new RehireEvaluationForFormerEmployeesPersonal
            {
                Division = x.Personal.Division,
                Factory = x.Personal.Factory,
                Department = x.Personal.Department,
                EmployeeID = x.Personal.Employee_ID,
                Local_Full_Name = x.Personal.Local_Full_Name,
                Date_of_Resignation = x.Personal.Resign_Date,
                Seq = seqValue.Result,
                Resign_Type = x.Resignation.Resignation_Type,
                Onboard_Date = x.Personal.Onboard_Date,
                Resign_Reason = x.Resignation.Resign_Reason,
                USER_GUID = x.Personal.USER_GUID,
                Blacklist = x.Personal.Blacklist,
            }).FirstOrDefaultAsync();

            return data;
        }
        public async Task<List<string>> GetListTypeHeadIdentificationNumber(string nationality)
        => await _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Nationality == nationality, true).Select(x => x.Identification_Number).Distinct().ToListAsync();
    }

}