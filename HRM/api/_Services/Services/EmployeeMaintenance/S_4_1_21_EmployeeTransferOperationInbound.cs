using API.Data;
using API._Services.Interfaces.EmployeeMaintenance;
using API.DTOs.EmployeeMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.EmployeeMaintenance
{
    public class S_4_1_21_EmployeeTransferOperationInbound : BaseServices, I_4_1_21_EmployeeTransferOperationInbound
    {
        public S_4_1_21_EmployeeTransferOperationInbound(DBContext dbContext) : base(dbContext)
        {
        }
        public async Task<OperationResult> Update(EmployeeTransferOperationInboundDto dto, string account, bool onConfirm = false)
        {
            HRMS_Emp_Transfer_Operation data = await _repositoryAccessor.HRMS_Emp_Transfer_Operation
               .FirstOrDefaultAsync(x => x.History_GUID == dto.History_GUID);

            if (data == null)
                return new OperationResult(false, "System.Message.NoData");

            if (await _repositoryAccessor.HRMS_Emp_Transfer_Operation
                .AnyAsync(x => x.History_GUID != dto.History_GUID
                            && x.USER_GUID == dto.USER_GUID
                            && x.Effective_Date_After == Convert.ToDateTime(dto.EffectiveDateAfterStr)))
                return new OperationResult(false, "Duplicate Data");
            data.Division_After = dto.DivisionAfter;
            data.Factory_After = dto.FactoryAfter;
            data.Employee_ID_After = dto.EmployeeIDAfter;
            data.Department_After = dto.DepartmentAfter;
            data.Assigned_Division_After = dto.AssignedDivisionAfter;
            data.Assigned_Factory_After = dto.AssignedFactoryAfter;
            data.Assigned_Employee_ID_After = dto.AssignedEmployeeIDAfter;
            data.Assigned_Department_After = dto.AssignedDepartmentAfter;
            data.Position_Grade_After = dto.PositionGradeAfter;
            data.Position_Title_After = dto.PositionTitleAfter;
            data.Work_Type_After = dto.WorkTypeAfter;
            data.Reason_for_Change_After = dto.ReasonForChangeAfter;
            data.Effective_Date_After = Convert.ToDateTime(dto.EffectiveDateAfterStr);
            data.Update_By_After = account;
            data.Update_Time_After = DateTime.Now;
            if (onConfirm)
            {
                data.Effective_Status_Before = true;
                data.Effective_Status_After = true;
                data.Update_By_Before = account;
                data.Update_Timee_Before = DateTime.Now;
            }
            try
            {
                _repositoryAccessor.HRMS_Emp_Transfer_Operation.Update(data);
                await _repositoryAccessor.Save();
                return new OperationResult(true);
            }
            catch
            {
                return new OperationResult(false, "System.Message.UpdateErrorMsg");
            }
        }

        public async Task<OperationResult> Confirm(EmployeeTransferOperationInboundDto dto, string account)
        {
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                var updateResult = await Update(dto, account, true);
                if (!updateResult.IsSuccess)
                {
                    await _repositoryAccessor.RollbackAsync();
                    return updateResult;
                }
                var transferResult = await TransferData(dto, account);
                if (!transferResult.IsSuccess)
                {
                    await _repositoryAccessor.RollbackAsync();
                    return transferResult;
                }
                var history_GUID = Guid.NewGuid().ToString();
                while (await _repositoryAccessor.HRMS_Emp_IDcard_EmpID_History.AnyAsync(x => x.History_GUID == history_GUID))
                {
                    history_GUID = Guid.NewGuid().ToString();
                }
                HRMS_Emp_IDcard_EmpID_History emp_IDcard_EmpID_History = new()
                {
                    History_GUID = history_GUID,
                    USER_GUID = dto.USER_GUID,
                    Nationality = dto.NationalityAfter,
                    Identification_Number = dto.IdentificationNumberAfter,
                    Division = dto.DivisionAfter,
                    Factory = dto.FactoryAfter,
                    Employee_ID = dto.EmployeeIDAfter,
                    Assigned_Division = dto.AssignedDivisionAfter,
                    Assigned_Factory = dto.AssignedFactoryAfter,
                    Assigned_Employee_ID = dto.AssignedEmployeeIDAfter,
                    Onboard_Date = Convert.ToDateTime(dto.EffectiveDateAfterStr),
                    Resign_Date = null,
                    Update_By = account,
                    Update_Time = DateTime.Now,
                };
                _repositoryAccessor.HRMS_Emp_IDcard_EmpID_History.Add(emp_IDcard_EmpID_History);
                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true);
            }
            catch
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, "System.Message.UpdateErrorMsg");
            }
        }
        private async Task<OperationResult> TransferData(EmployeeTransferOperationInboundDto dto, string account)
        {
            var emp_Personal = await _repositoryAccessor.HRMS_Emp_Personal.FirstOrDefaultAsync(x => x.USER_GUID == dto.USER_GUID);
            if (emp_Personal == null)
                return new OperationResult(false, "System.Message.NoData");
            emp_Personal.Division = dto.DivisionAfter;
            emp_Personal.Factory = dto.FactoryAfter;
            emp_Personal.Employee_ID = dto.EmployeeIDAfter;
            emp_Personal.Department = dto.DepartmentAfter;
            emp_Personal.Assigned_Division = dto.AssignedDivisionAfter;
            emp_Personal.Assigned_Factory = dto.AssignedFactoryAfter;
            emp_Personal.Assigned_Employee_ID = dto.AssignedEmployeeIDAfter;
            emp_Personal.Assigned_Department = dto.AssignedDepartmentAfter;
            emp_Personal.Position_Grade = dto.PositionGradeAfter.Value;
            emp_Personal.Position_Title = dto.PositionTitleAfter;
            emp_Personal.Work_Type = dto.WorkTypeAfter;
            emp_Personal.Onboard_Date = Convert.ToDateTime(dto.EffectiveDateAfterStr);
            emp_Personal.Update_By = account;
            emp_Personal.Update_Time = DateTime.Now;
            try
            {
                _repositoryAccessor.HRMS_Emp_Personal.Update(emp_Personal);
                await _repositoryAccessor.Save();
                return new OperationResult(true);
            }
            catch
            {
                return new OperationResult(false, "System.Message.UpdateErrorMsg");
            }
        }
        public async Task<PaginationUtility<EmployeeTransferOperationInboundDto>> GetPagination(PaginationParam pagination, EmployeeTransferOperationInboundParam param, List<string> roleList)
        {
            #region PredicateBuilder
            var pred = PredicateBuilder.New<HRMS_Emp_Transfer_Operation>(true);
            var predEmpPersonal = PredicateBuilder.New<HRMS_Emp_Personal>(true);

            if (!string.IsNullOrWhiteSpace(param.Nationality))
                pred.And(x => x.Nationality_After == param.Nationality);

            if (!string.IsNullOrWhiteSpace(param.IdentificationNumber))
                pred.And(x => x.Identification_Number_After.Contains(param.IdentificationNumber));

            if (!string.IsNullOrWhiteSpace(param.Division))
                pred.And(x => x.Division_After == param.Division);

            if (!string.IsNullOrWhiteSpace(param.Factory))
                pred.And(x => x.Factory_After == param.Factory);

            if (!string.IsNullOrWhiteSpace(param.EmployeeID))
                pred.And(x => x.Employee_ID_After.Contains(param.EmployeeID));

            if (!string.IsNullOrWhiteSpace(param.AssignedDivision))
                pred.And(x => x.Assigned_Division_After == param.AssignedDivision);

            if (!string.IsNullOrWhiteSpace(param.AssignedFactory))
                pred.And(x => x.Assigned_Factory_After == param.AssignedFactory);

            if (!string.IsNullOrWhiteSpace(param.AssignedEmployeeID))
                pred.And(x => x.Assigned_Employee_ID_After.Contains(param.AssignedEmployeeID));

            if (!string.IsNullOrWhiteSpace(param.ReasonForChange))
                pred.And(x => x.Reason_for_Change_After == param.ReasonForChange);

            if (!string.IsNullOrWhiteSpace(param.EffectiveDateStart_Str) && !string.IsNullOrWhiteSpace(param.EffectiveDateEnd_Str))
                pred.And(x => x.Effective_Date_After >= Convert.ToDateTime(param.EffectiveDateStart_Str)
                           && x.Effective_Date_After <= Convert.ToDateTime(param.EffectiveDateEnd_Str));

            if (!string.IsNullOrWhiteSpace(param.EffectiveStatus))
            {
                if (param.EffectiveStatus == "Y")
                    pred.And(x => x.Effective_Status_After == true || x.Effective_Status_Before == true);
                else if (param.EffectiveStatus == "N")
                    pred.And(x => x.Effective_Status_After == false || x.Effective_Status_Before == false);
            }

            if (!string.IsNullOrWhiteSpace(param.LocalFullName))
                predEmpPersonal.And(x => x.Local_Full_Name.ToLower().Contains(param.LocalFullName));

            #endregion
            var department = _repositoryAccessor.HRMS_Org_Department.FindAll(true)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true),
                    x => new { x.Division, x.Factory, x.Department_Code },
                    y => new { y.Division, y.Factory, y.Department_Code },
                    (x, y) => new { HOD = x, HODL = y })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, HODL = y })
                .Select(x => new
                {
                    Code = x.HOD.Department_Code,
                    Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name,
                    x.HOD.Division,
                    x.HOD.Factory,
                }).ToList();

            var basic = _repositoryAccessor.HRMS_Basic_Code.FindAll(true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language
                .FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Code,
                    Name = x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name
                }).ToList();

            var iWorkTypes = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.WorkType, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.WorkType && x.Language_Code.ToLower() == param.Language.ToLower(), true),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Code,
                    Name = x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name
                }).ToList();

            var accounts = _repositoryAccessor.HRMS_Basic_Account.FindAll(true)
                .Select(x => new { x.Account, x.Name })
                .ToList();

            var HEP = await Query_Permission_Data_Filter(roleList, predEmpPersonal);

            var HETO = _repositoryAccessor.HRMS_Emp_Transfer_Operation.FindAll(pred, true).ToList();

            var datas = HETO
                .Join(HEP.Distinct(),
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { HETO = x, HEP = y })
                .Select(x => new EmployeeTransferOperationInboundDto
                {
                    History_GUID = x.HETO.History_GUID,
                    USER_GUID = x.HETO.USER_GUID,
                    NationalityBefore = x.HETO.Nationality_Before,
                    IdentificationNumberBefore = x.HETO.Identification_Number_Before,
                    LocalFullNameBefore = x.HEP.Local_Full_Name,
                    DivisionBefore = x.HETO.Division_Before,
                    FactoryBefore = x.HETO.Factory_Before,
                    EmployeeIDBefore = x.HETO.Employee_ID_Before,
                    DepartmentBefore = department.Where(y => y.Division == x.HETO.Division_Before
                                                    && y.Factory == x.HETO.Factory_Before
                                                    && y.Code == x.HETO.Department_Before)
                                                .Select(y => $"{y.Code} - {y.Name}").FirstOrDefault(),
                    AssignedDivisionBefore = x.HETO.Assigned_Division_Before,
                    AssignedFactoryBefore = x.HETO.Assigned_Factory_Before,
                    AssignedEmployeeIDBefore = x.HETO.Assigned_Employee_ID_Before,
                    AssignedDepartmentBefore = department.Where(y => y.Division == x.HETO.Assigned_Division_Before
                                                    && y.Factory == x.HETO.Assigned_Factory_Before
                                                    && y.Code == x.HETO.Assigned_Department_Before)
                                                .Select(y => $"{y.Code} - {y.Name}").FirstOrDefault(),
                    PositionGradeBefore = x.HETO.Position_Grade_Before,
                    PositionTitleBefore = basic.Where(y => y.Code == x.HETO.Position_Title_Before)
                                               .Select(y => $"{y.Code} - {y.Name}").FirstOrDefault(),
                    WorkTypeBefore = iWorkTypes.Where(y => y.Code == x.HETO.Work_Type_Before)
                                               .Select(y => $"{y.Code} - {y.Name}").FirstOrDefault(),
                    ReasonForChangeBefore = basic.Where(y => y.Code == x.HETO.Reason_for_Change_Before)
                                               .Select(y => $"{y.Code} - {y.Name}").FirstOrDefault(),
                    EffectiveDateBefore = x.HETO.Effective_Date_Before,
                    EffectiveStatusBefore = x.HETO.Effective_Status_Before,
                    UpdateTimeBefore = x.HETO.Update_Timee_Before,
                    UpdateByBefore = accounts.Where(y => y.Account == x.HETO.Update_By_Before)
                                                .Select(x => x.Name).FirstOrDefault(),

                    NationalityAfter = x.HETO.Nationality_After,
                    IdentificationNumberAfter = x.HETO.Identification_Number_After,
                    LocalFullNameAfter = x.HEP.Local_Full_Name,
                    DivisionAfter = x.HETO.Division_After,
                    FactoryAfter = x.HETO.Factory_After,
                    EmployeeIDAfter = x.HETO.Employee_ID_After,
                    DepartmentAfter = department.Where(y => y.Division == x.HETO.Division_After
                                                          && y.Factory == x.HETO.Factory_After
                                                          && y.Code == x.HETO.Department_After)
                                                        .Select(y => $"{y.Code} - {y.Name}").FirstOrDefault(),

                    AssignedDivisionAfter = x.HETO.Assigned_Division_After,
                    AssignedFactoryAfter = x.HETO.Assigned_Factory_After,
                    AssignedEmployeeIDAfter = x.HETO.Assigned_Employee_ID_After,
                    AssignedDepartmentAfter = department.Where(y => y.Division == x.HETO.Assigned_Division_After
                                                          && y.Factory == x.HETO.Assigned_Factory_After
                                                          && y.Code == x.HETO.Assigned_Department_After)
                                                        .Select(y => $"{y.Code} - {y.Name}").FirstOrDefault(),

                    PositionGradeAfter = x.HETO.Position_Grade_After,
                    PositionTitleAfter = basic.Where(y => y.Code == x.HETO.Position_Title_After)
                                               .Select(y => $"{y.Code} - {y.Name}").FirstOrDefault(),
                    WorkTypeAfter = iWorkTypes.Where(y => y.Code == x.HETO.Work_Type_After)
                                               .Select(y => $"{y.Code} - {y.Name}").FirstOrDefault(),
                    ReasonForChangeAfter = basic.Where(y => y.Code == x.HETO.Reason_for_Change_After)
                                               .Select(y => $"{y.Code} - {y.Name}").FirstOrDefault(),
                    EffectiveDateAfter = x.HETO.Effective_Date_After,
                    EffectiveStatusAfter = x.HETO.Effective_Status_After,
                    UpdateTimeAfter = x.HETO.Update_Time_After,
                    UpdateByAfter = accounts.Where(y => y.Account == x.HETO.Update_By_After)
                                                .Select(x => x.Name).FirstOrDefault()
                }).ToList();

            return PaginationUtility<EmployeeTransferOperationInboundDto>.Create(datas, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<EmployeeTransferOperationInboundDto> GetDetail(string History_GUID)
        {
            var data = await _repositoryAccessor.HRMS_Emp_Transfer_Operation
                .FirstOrDefaultAsync(x => x.History_GUID == History_GUID, true);

            if (data == null)
                return new();

            var accountBefore = _repositoryAccessor.HRMS_Basic_Account
                .FirstOrDefault(x => x.Account == data.Update_By_Before, true);

            var accountAfter = _repositoryAccessor.HRMS_Basic_Account
                .FirstOrDefault(x => x.Account == data.Update_By_After, true);

            var localFullNameBefore = _repositoryAccessor.HRMS_Emp_Personal
                .FirstOrDefault(x => x.USER_GUID == data.USER_GUID, true);

            var localFullNameAfter = _repositoryAccessor.HRMS_Emp_Personal
                .FirstOrDefault(x => x.USER_GUID == data.USER_GUID, true);

            EmployeeTransferOperationInboundDto result = new()
            {
                History_GUID = data.History_GUID,
                USER_GUID = data.USER_GUID,
                NationalityBefore = data.Nationality_Before,
                IdentificationNumberBefore = data.Identification_Number_Before,
                LocalFullNameBefore = localFullNameBefore.Local_Full_Name,
                DivisionBefore = data.Division_Before,
                FactoryBefore = data.Factory_Before,
                EmployeeIDBefore = data.Employee_ID_Before,
                DepartmentBefore = data.Department_Before,
                AssignedDivisionBefore = data.Assigned_Division_Before,
                AssignedFactoryBefore = data.Assigned_Factory_Before,
                AssignedEmployeeIDBefore = data.Assigned_Employee_ID_Before,
                AssignedDepartmentBefore = data.Assigned_Department_Before,
                PositionGradeBefore = data.Position_Grade_Before,
                PositionTitleBefore = data.Position_Title_Before,
                WorkTypeBefore = data.Work_Type_Before,
                ReasonForChangeBefore = data.Reason_for_Change_Before,
                EffectiveDateBefore = data.Effective_Date_Before,
                EffectiveStatusBefore = data.Effective_Status_Before,
                UpdateTimeBefore = data.Update_Timee_Before,
                UpdateByBefore = accountBefore?.Name,
                NationalityAfter = data.Nationality_After,
                IdentificationNumberAfter = data.Identification_Number_After,
                LocalFullNameAfter = localFullNameAfter?.Local_Full_Name,
                DivisionAfter = data.Division_After,
                FactoryAfter = data.Factory_After,
                EmployeeIDAfter = data.Employee_ID_After,
                DepartmentAfter = data.Department_After,
                AssignedDivisionAfter = data.Assigned_Division_After,
                AssignedFactoryAfter = data.Assigned_Factory_After,
                AssignedEmployeeIDAfter = data.Assigned_Employee_ID_After,
                AssignedDepartmentAfter = data.Assigned_Department_After,
                PositionGradeAfter = data.Position_Grade_After,
                PositionTitleAfter = data.Position_Title_After,
                WorkTypeAfter = data.Work_Type_After,
                ReasonForChangeAfter = data.Reason_for_Change_After,
                EffectiveDateAfter = data.Effective_Date_After,
                EffectiveStatusAfter = data.Effective_Status_After,
                UpdateTimeAfter = data.Update_Time_After,
                UpdateByAfter = accountBefore?.Name,
            };
            return result;
        }

        #region Get List
        public async Task<List<KeyValuePair<string, string>>> GetListDivision(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.Division, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.Factory, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListFactoryByDivision(string Division, string Language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
               .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory, true)
               .Join(_repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(x => x.Kind == "1" && x.Division == Division, true),
                   x => new { Factory = x.Code },
                   y => new { y.Factory },
                   (x, y) => new { HBC = x, HBFC = y })
               .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower(), true),
                   x => new { x.HBC.Type_Seq, x.HBC.Code },
                   HBCL => new { HBCL.Type_Seq, HBCL.Code },
                   (x, y) => new { x.HBC, x.HBFC, HBCL = y })
                   .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                   (x, y) => new { x.HBC, x.HBFC, HBCL = y })
               .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
               .ToListAsync();
        }
        public async Task<List<KeyValuePair<string, string>>> GetListNationality(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.Nationality, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListWorkType(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.WorkType, Language);
        }
        public async Task<List<KeyValuePair<string, string>>> GetListReasonChange(string Language)
        {
            return await GetHRMS_Basic_Code(BasicCodeTypeConstant.ReasonChange, Language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListReasonChangeOut(string Language)
        {
            return await GetHRMS_Basic_Code_Char1(BasicCodeTypeConstant.ReasonChange, BasicCodeCharConstant.Out, Language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListReasonChangeIn(string Language)
        {
            return await GetHRMS_Basic_Code_Char1(BasicCodeTypeConstant.ReasonChange, BasicCodeCharConstant.In, Language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string Division, string Factory, string Language)
        {
            return await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Division == Division && x.Factory == Factory, true)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower(), true),
                      HOD => new { HOD.Division, HOD.Factory, HOD.Department_Code },
                      HODL => new { HODL.Division, HODL.Factory, HODL.Department_Code },
                    (HOD, HODL) => new { HOD, HODL })
                    .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (prev, HODL) => new { prev.HOD, HODL })
                .Select(x => new KeyValuePair<string, string>(x.HOD.Department_Code, $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}"))
                .ToListAsync();
        }
        public async Task<List<KeyValuePair<decimal, string>>> GetPositionGrade()
        {
            return await _repositoryAccessor.HRMS_Basic_Level.FindAll(true)
                .Select(x => new KeyValuePair<decimal, string>(x.Level, $"{x.Level}"))
                .Distinct()
                .ToListAsync();
        }
        public async Task<List<KeyValuePair<string, string>>> GetPositionTitle(decimal level, string Language)
        {
            return await _repositoryAccessor.HRMS_Basic_Level.FindAll(x => x.Level == level, true)
                .Join(_repositoryAccessor.HRMS_Basic_Code.FindAll(true),
                    HBL => HBL.Level_Code,
                    HBC => HBC.Code,
                    (HBL, HBC) => new { HBL, HBC })
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower(), true),
                    prev => new { prev.HBC.Type_Seq, prev.HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (prev, HBCL) => new { prev.HBL, prev.HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBL, prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBL.Level_Code, $"{x.HBL.Level_Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .Distinct()
                .ToListAsync();
        }
        private async Task<List<KeyValuePair<string, string>>> GetHRMS_Basic_Code(string Type_Seq, string Language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == Type_Seq, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
        }
        private async Task<List<KeyValuePair<string, string>>> GetHRMS_Basic_Code_Char1(string Type_Seq, string char1, string Language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == Type_Seq && x.Char1.ToLower() == char1.ToLower(), true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
        }
        #endregion
    }
}