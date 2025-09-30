using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API._Services.Interfaces.SalaryMaintenance;
using API.DTOs.SalaryMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryMaintenance
{
    public class S_7_1_26_FinSalaryCloseMaintenance : BaseServices, I_7_1_26_FinSalaryCloseMaintenance
    {
        public S_7_1_26_FinSalaryCloseMaintenance(DBContext dbContext) : base(dbContext)
        {
        }

        public async Task<OperationResult> DownLoadExcel(FinSalaryCloseMaintenance_Param param, string userName)
        {
            var result = await GetData(param);
            List<FinSalaryCloseMaintenance_MainData> data = result.Data as List<FinSalaryCloseMaintenance_MainData>;

            if (!data.Any())
                return new OperationResult(true, "NoData");
            List<Table> tables = new()
            {
                new Table("result", data)
            };
            List<SDCores.Cell> cells = new()
            {
                new SDCores.Cell("B2", userName),
                new SDCores.Cell("D2", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")),
            };
            ConfigDownload config = new() { IsAutoFitColumn = false };
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                tables,
                cells,
                "Resources\\Template\\SalaryMaintenance\\7_1_26_FinSalaryCloseMaintenance\\Download.xlsx", config
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }

        public async Task<OperationResult> GetDataPagination(PaginationParam pagination, FinSalaryCloseMaintenance_Param param)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;
            return new OperationResult(true, PaginationUtility<FinSalaryCloseMaintenance_MainData>.Create(result.Data as List<FinSalaryCloseMaintenance_MainData>, pagination.PageNumber, pagination.PageSize));
        }

        private async Task<OperationResult> GetData(FinSalaryCloseMaintenance_Param param)
        {
            DateTime yearMonth = Convert.ToDateTime(param.Year_Month);
            var predHSC = PredicateBuilder.New<HRMS_Sal_Close>(x => x.Factory == param.Factory &&
                    x.Sal_Month == yearMonth &&
                    param.Permission_Group.Contains(x.Permission_Group));
            List<FinSalaryCloseMaintenance_Temp> temp = new();
            switch (param.Kind)
            {
                case "O":
                    predHSC.And(x => x.Close_Status == "N");
                    temp = await _repositoryAccessor.HRMS_Sal_Close.FindAll(predHSC, true)
                        .Join(_repositoryAccessor.HRMS_Sal_Monthly.FindAll(x => x.Factory == param.Factory, true),
                            a => new { a.Factory, a.Sal_Month, a.Employee_ID },
                            b => new { b.Factory, b.Sal_Month, b.Employee_ID },
                            (hsc, hsm) => new { hsc, hsm })
                        .Select(x => new FinSalaryCloseMaintenance_Temp()
                        {
                            USER_GUID = x.hsm.USER_GUID,
                            Factory = x.hsc.Factory,
                            Sal_Month = x.hsc.Sal_Month,
                            Department = x.hsm.Department,
                            Employee_ID = x.hsc.Employee_ID,
                            Permission_Group = x.hsc.Permission_Group,
                            Close_End = x.hsc.Close_End,
                            Close_Status = x.hsc.Close_Status,
                            Update_By = x.hsc.Update_By,
                            Update_Time = x.hsc.Update_Time
                        }).ToListAsync();

                    break;
                case "R":
                    predHSC.And(x => x.Close_Status == "N");
                    temp = await _repositoryAccessor.HRMS_Sal_Close.FindAll(predHSC, true)
                        .Join(_repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(x => x.Factory == param.Factory, true),
                        a => new { a.Factory, a.Sal_Month, a.Employee_ID },
                        b => new { b.Factory, b.Sal_Month, b.Employee_ID },
                        (hsc, hsrm) => new { hsc, hsrm })
                        .Select(x => new FinSalaryCloseMaintenance_Temp()
                        {
                            USER_GUID = x.hsrm.USER_GUID,
                            Factory = x.hsc.Factory,
                            Sal_Month = x.hsc.Sal_Month,
                            Department = x.hsrm.Department,
                            Employee_ID = x.hsc.Employee_ID,
                            Permission_Group = x.hsc.Permission_Group,
                            Close_End = x.hsc.Close_End,
                            Close_Status = x.hsc.Close_Status,
                            Update_By = x.hsc.Update_By,
                            Update_Time = x.hsc.Update_Time
                        }).ToListAsync();
                    break;
                case "C":
                    predHSC.And(x => x.Close_Status == "Y");

                    var HSC_HSM = _repositoryAccessor.HRMS_Sal_Close.FindAll(predHSC, true)
                        .Join(_repositoryAccessor.HRMS_Sal_Monthly.FindAll(x => x.Factory == param.Factory, true),
                        a => new { a.Factory, a.Sal_Month, a.Employee_ID },
                        b => new { b.Factory, b.Sal_Month, b.Employee_ID },
                        (hsc, hsm) => new { hsc, hsm })
                       .Select(x => new FinSalaryCloseMaintenance_Temp()
                       {
                           USER_GUID = x.hsm.USER_GUID,
                           Factory = x.hsc.Factory,
                           Sal_Month = x.hsc.Sal_Month,
                           Department = x.hsm.Department,
                           Employee_ID = x.hsc.Employee_ID,
                           Permission_Group = x.hsc.Permission_Group,
                           Close_End = x.hsc.Close_End,
                           Close_Status = x.hsc.Close_Status,
                           Update_By = x.hsc.Update_By,
                           Update_Time = x.hsc.Update_Time
                       });

                    var HSC_HSRM = _repositoryAccessor.HRMS_Sal_Close.FindAll(predHSC, true)
                        .Join(_repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(x => x.Factory == param.Factory, true),
                        a => new { a.Factory, a.Sal_Month, a.Employee_ID },
                        b => new { b.Factory, b.Sal_Month, b.Employee_ID },
                        (hsc, hsrm) => new { hsc, hsrm })
                        .Select(x => new FinSalaryCloseMaintenance_Temp()
                        {
                            USER_GUID = x.hsrm.USER_GUID,
                            Factory = x.hsc.Factory,
                            Sal_Month = x.hsc.Sal_Month,
                            Department = x.hsrm.Department,
                            Employee_ID = x.hsc.Employee_ID,
                            Permission_Group = x.hsc.Permission_Group,
                            Close_End = x.hsc.Close_End,
                            Close_Status = x.hsc.Close_Status,
                            Update_By = x.hsc.Update_By,
                            Update_Time = x.hsc.Update_Time
                        });
                    temp = await HSC_HSM.Union(HSC_HSRM).ToListAsync();
                    break;
            }
            if (!temp.Any())
                return new OperationResult(true, new List<FinSalaryCloseMaintenance_MainData>());

            var HEP = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == param.Factory, true).ToListAsync();
            var department = await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == param.Factory, true)
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
                                   }).Distinct().ToListAsync();
            var permission = await GetBasicCode(BasicCodeTypeConstant.PermissionGroup, param.Language);
            var result = temp
                .Join(HEP,
                    a => a.USER_GUID,
                    b => b.USER_GUID,
                    (temp, hep) => new { temp, hep })
                .Select(x => new FinSalaryCloseMaintenance_MainData()
                {
                    Factory = x.temp.Factory,
                    Year_Month_Str = x.temp.Sal_Month.ToString("yyyy/MM"),
                    Year_Month = x.temp.Sal_Month,
                    Department = x.temp.Department,
                    Department_Name = department.FirstOrDefault(y => y.Code == x.temp.Department)?.Name,
                    Employee_ID = x.temp.Employee_ID,
                    Local_Full_Name = x.hep.Local_Full_Name,
                    Permission_Group = permission.FirstOrDefault(y => y.Key == x.temp.Permission_Group).Value,
                    Close_Status = x.temp.Close_Status,
                    Close_End_Str = x.temp.Close_End.HasValue ? x.temp.Close_End.Value.ToString("yyyy/MM/dd") : string.Empty,
                    Close_End = x.temp.Close_End,
                    Onboard_Date_Str = x.hep.Onboard_Date.ToString("yyyy/MM/dd"),
                    Onboard_Date = x.hep.Onboard_Date,
                    Resign_Date_Str = x.hep.Resign_Date.HasValue ? x.hep.Resign_Date.Value.ToString("yyyy/MM/dd") : string.Empty,
                    Resign_Date = x.hep.Resign_Date,
                    Update_By = x.temp.Update_By,
                    Update_Time = x.temp.Update_Time.ToString("yyyy/MM/dd HH:mm:ss")
                });
            if (!string.IsNullOrWhiteSpace(param.Department))
            {
                result = result.Where(x => x.Department == param.Department);
            }
            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
            {
                result = result.Where(x => x.Employee_ID == param.Employee_ID);
            }
            return new OperationResult(true, result.OrderBy(x => x.Department).ThenBy(x => x.Employee_ID).ToList());
        }

        private async Task<List<KeyValuePair<string, string>>> GetBasicCode(string typeSeq, string language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == typeSeq, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    a => new { a.Type_Seq, a.Code },
                    b => new { b.Type_Seq, b.Code },
                    (code, codeLang) => new { code, codeLang })
                .SelectMany(x => x.codeLang.DefaultIfEmpty(), (x, codeLang) => new { x.code, codeLang })
                .Select(x => new KeyValuePair<string, string>(
                    x.code.Code,
                    $"{x.code.Code} - {(x.codeLang != null ? x.codeLang.Code_Name : x.code.Code_Name)}"
                )).ToListAsync();
        }
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

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string userName, string language)
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

        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language)
        {
            var permissionGroups = await Query_Permission_List(factory);
            List<string> permissions = permissionGroups.Select(x => x.Permission_Group).ToList();
            var dataPermissionGroups = await GetDataBasicCode(BasicCodeTypeConstant.PermissionGroup, language);
            var results = dataPermissionGroups.Where(x => permissions.Contains(x.Key)).ToList();
            return results;
        }

        public async Task<List<string>> GetListTypeHeadEmployeeID(string factory)
        => await _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == factory, true).Select(x => x.Employee_ID).Distinct().ToListAsync();


        public async Task<OperationResult> Update(FinSalaryCloseMaintenance_MainData data)
        {
            var checkExited = await _repositoryAccessor.HRMS_Sal_Close.FirstOrDefaultAsync(x => x.Factory == data.Factory &&
                                x.Sal_Month == DateTime.Parse(data.Year_Month_Str) &&
                                x.Employee_ID == data.Employee_ID);
            if (checkExited is null)
                return new OperationResult(false, "Not found data in HRMS_Sal_Close.");
            checkExited.Close_Status = data.Close_Status;
            checkExited.Update_By = data.Update_By;
            checkExited.Update_Time = DateTime.Now;
            _repositoryAccessor.HRMS_Sal_Close.Update(checkExited);
            try
            {
                await _repositoryAccessor.Save();
                return new OperationResult(true, "Update successfully!");
            }
            catch (System.Exception)
            {

                return new OperationResult(false, "Update failed!");
            }
        }


        public async Task<OperationResult> BatchUpdateData(BatchUpdateData_Param param, string userName)
        {
            DateTime yearMonth = Convert.ToDateTime(param.Year_Month);
            var predHSC = PredicateBuilder.New<HRMS_Sal_Close>(x => x.Factory == param.Factory &&
                            x.Sal_Month == yearMonth &&
                            param.Permission_Group.Contains(x.Permission_Group));

            var HSC = _repositoryAccessor.HRMS_Sal_Close.FindAll(predHSC, true);

            if (!HSC.Any())
                return new OperationResult(false, "Not found data");
            List<BatchUpdateData> Sal_Close = new();
            if (param.Kind == "O")
            {
                var HSM = _repositoryAccessor.HRMS_Sal_Monthly.FindAll(x => x.Factory == param.Factory);
                Sal_Close = await HSC
                    .Join(HSM,
                        a => new { a.Factory, a.Sal_Month, a.Employee_ID },
                        b => new { b.Factory, b.Sal_Month, b.Employee_ID },
                        (hsc, hsm) => new { hsc, hsm })
                    .Select(x => new BatchUpdateData()
                    {
                        Department = x.hsm.Department,
                        Employee_ID = x.hsm.Employee_ID
                    })
                    .OrderBy(x => x.Department)
                    .ThenBy(x => x.Employee_ID)
                    .ToListAsync();
            }
            else
            {
                var HSRM = _repositoryAccessor.HRMS_Sal_Resign_Monthly.FindAll(x => x.Factory == param.Factory);
                Sal_Close = await HSC
                    .Join(HSRM,
                        a => new { a.Factory, a.Sal_Month, a.Employee_ID },
                        b => new { b.Factory, b.Sal_Month, b.Employee_ID },
                        (hsc, hsm) => new { hsc, hsm })
                    .Select(x => new BatchUpdateData()
                    {
                        Department = x.hsm.Department,
                        Employee_ID = x.hsm.Employee_ID
                    })
                    .OrderBy(x => x.Department)
                    .ThenBy(x => x.Employee_ID)
                    .ToListAsync();
            }

            List<HRMS_Sal_Close> updateList = new();
            DateTime today = DateTime.Now;
            foreach (var item in Sal_Close)
            {
                var findExited = HSC.FirstOrDefault(x => x.Employee_ID == item.Employee_ID);
                findExited.Close_Status = param.Close_Status;
                findExited.Close_End = today.Date;
                findExited.Update_By = userName;
                findExited.Update_Time = today;
                updateList.Add(findExited);
            }
            if (updateList.Any())
                _repositoryAccessor.HRMS_Sal_Close.UpdateMultiple(updateList);
            try
            {
                await _repositoryAccessor.Save();
                return new OperationResult(true, "Excute successfully!");
            }
            catch (System.Exception)
            {
                return new OperationResult(false, "Excute failed!");
            }

        }
    }
}