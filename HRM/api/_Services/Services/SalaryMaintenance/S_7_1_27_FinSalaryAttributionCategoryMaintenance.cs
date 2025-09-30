using System.Globalization;
using API.Data;
using API._Services.Interfaces.SalaryMaintenance;
using API.DTOs.SalaryMaintenance;
using API.Helper.Constant;
using API.Models;
using Aspose.Cells;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryMaintenance
{
    public class S_7_1_27_FinSalaryAttributionCategoryMaintenance : BaseServices, I_7_1_27_FinSalaryAttributionCategoryMaintenance
    {
        public S_7_1_27_FinSalaryAttributionCategoryMaintenance(DBContext dbContext) : base(dbContext)
        {
        }
        #region Dropdown List
        public async Task<List<KeyValuePair<string, string>>> GetDropDownList(FinSalaryAttributionCategoryMaintenance_Param param, List<string> roleList)
        {
            var HBC = await _repositoryAccessor.HRMS_Basic_Code.FindAll().ToListAsync();
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower()).ToList();
            var result = new List<KeyValuePair<string, string>>();
            var data = HBC.GroupJoin(HBCL,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { hbc = x, hbcl = y })
                    .SelectMany(x => x.hbcl.DefaultIfEmpty(),
                    (x, y) => new { x.hbc, hbcl = y });
            var authFactories = await Queryt_Factory_AddList(roleList);
            result.AddRange(data.Where(x => x.hbc.Type_Seq == BasicCodeTypeConstant.Factory && authFactories.Contains(x.hbc.Code)).Select(x => new KeyValuePair<string, string>("FA", $"{x.hbc.Code}-{(x.hbcl != null ? x.hbcl.Code_Name : x.hbc.Code_Name)}")).Distinct().ToList());
            result.AddRange(data.Where(x => x.hbc.Type_Seq == BasicCodeTypeConstant.Method).Select(x => new KeyValuePair<string, string>("ME", $"{x.hbc.Code}-{(x.hbcl != null ? x.hbcl.Code_Name : x.hbc.Code_Name)}")).Distinct().ToList());
            result.AddRange(data.Where(x => x.hbc.Type_Seq == BasicCodeTypeConstant.SalaryCategory).Select(x => new KeyValuePair<string, string>("SA", $"{x.hbc.Code}-{(x.hbcl != null ? x.hbcl.Code_Name : x.hbc.Code_Name)}")).Distinct().ToList());
            return result;
        }
        public async Task<List<KeyValuePair<string, string>>> GetDepartmentList(FinSalaryAttributionCategoryMaintenance_Param param)
        {
            var HOD = await Query_Department_List(param.Factory);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Factory == param.Factory && x.Language_Code.ToLower() == param.Lang.ToLower())
                .ToList();
            var dataDept = HOD
                .GroupJoin(HODL,
                    x => new {x.Division, x.Department_Code},
                    y => new {y.Division, y.Department_Code},
                    (x, y) => new { hod = x, hodl = y })
                .SelectMany(x => x.hodl.DefaultIfEmpty(),
                    (x, y) => new { x.hod, hodl = y });
            return dataDept.Select(x => new KeyValuePair<string, string>(x.hod.Department_Code, $"{x.hod.Department_Code}-{(x.hodl != null ? x.hodl.Name : x.hod.Department_Name)}")).Distinct().ToList();
        }
        public async Task<List<KeyValuePair<string, string>>> GetKindCodeList(FinSalaryAttributionCategoryMaintenance_Param param)
        {
            var HBC = await _repositoryAccessor.HRMS_Basic_Code.FindAll().ToListAsync();
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower()).ToList();
            var result = new List<KeyValuePair<string, string>>();
            var data = HBC.GroupJoin(HBCL,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { hbc = x, hbcl = y })
                    .SelectMany(x => x.hbcl.DefaultIfEmpty(),
                    (x, y) => new { x.hbc, hbcl = y });
            return data
                .Where(x => param.Kind == "1" ? x.hbc.Type_Seq == BasicCodeTypeConstant.JobTitle : x.hbc.Type_Seq == BasicCodeTypeConstant.PermissionGroup)
                .Select(x => new KeyValuePair<string, string>(x.hbc.Code, $"{x.hbc.Code}-{(x.hbcl != null ? x.hbcl.Code_Name : x.hbc.Code_Name)}")).Distinct().ToList();
        }
        public async Task<OperationResult> GetSearch(PaginationParam paginationParams, FinSalaryAttributionCategoryMaintenance_Param searchParam)
        {
            var result = await GetData(searchParam);
            if (!result.IsSuccess)
                return result;
            return new OperationResult(true, PaginationUtility<FinSalaryAttributionCategoryMaintenance_Data>.Create(result.Data as List<FinSalaryAttributionCategoryMaintenance_Data>, paginationParams.PageNumber, paginationParams.PageSize));
        }
        public async Task<OperationResult> GetData(FinSalaryAttributionCategoryMaintenance_Param param)
        {
            if (string.IsNullOrWhiteSpace(param.Factory) || string.IsNullOrWhiteSpace(param.Kind))
                return new OperationResult(false, "InvalidInput");
            var predicate = PredicateBuilder.New<HRMS_Sal_FinCategory>(x => x.Factory == param.Factory && x.Kind == param.Kind);
            if (!string.IsNullOrWhiteSpace(param.Department))
                predicate.And(x => x.Department == param.Department);
            if (!string.IsNullOrWhiteSpace(param.Salary_Category))
                predicate.And(x => x.Sortcod == param.Salary_Category);
            if (param.Kind_Code_List != null && param.Kind_Code_List.Count > 0)
                predicate.And(x => param.Kind_Code_List.Contains(x.Code));
            var HBC_Lang = IQuery_Code_Lang(param.Lang);
            var HOD_Lang = IQuery_Department_Lang(param.Factory, param.Lang);
            var data = await _repositoryAccessor.HRMS_Sal_FinCategory
                .FindAll(predicate)
                .GroupJoin(HOD_Lang,
                    x => x.Department,
                    y => y.Department,
                    (x, y) => new { HSF = x, HOD_Lang = y })
                .SelectMany(x => x.HOD_Lang.DefaultIfEmpty(),
                    (x, y) => new { x.HSF, HOD_Lang = y })
                .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.SalaryCategory),
                    x => x.HSF.Sortcod,
                    y => y.Code,
                    (x, y) => new { x.HSF, x.HOD_Lang, HBC_SalaryCategory = y })
                .SelectMany(x => x.HBC_SalaryCategory.DefaultIfEmpty(),
                    (x, y) => new { x.HSF, x.HOD_Lang, HBC_SalaryCategory = y })
                .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.Method),
                    x => x.HSF.Kind,
                    y => y.Code,
                    (x, y) => new { x.HSF, x.HOD_Lang, x.HBC_SalaryCategory, HBC_Kind = y })
                .SelectMany(x => x.HBC_Kind.DefaultIfEmpty(),
                    (x, y) => new { x.HSF, x.HOD_Lang, x.HBC_SalaryCategory, HBC_Kind = y })
                .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.JobTitle || x.Type_Seq == BasicCodeTypeConstant.PermissionGroup),
                    x => x.HSF.Code,
                    y => y.Code,
                    (x, y) => new { x.HSF, x.HOD_Lang, x.HBC_SalaryCategory, x.HBC_Kind, HBC_KindCode = y })
                .SelectMany(x => x.HBC_KindCode.DefaultIfEmpty(),
                    (x, y) => new { x.HSF, x.HOD_Lang, x.HBC_SalaryCategory, x.HBC_Kind, HBC_KindCode = y })
                .GroupBy(x => x.HSF)
                .ToListAsync();
            var result = data
                .Select(x =>
                {
                    var HOD_Lang = x.FirstOrDefault(y => y.HOD_Lang != null)?.HOD_Lang;
                    var HBC_SalaryCategory = x.FirstOrDefault(y => y.HBC_SalaryCategory != null)?.HBC_SalaryCategory;
                    var HBC_Kind = x.FirstOrDefault(y => y.HBC_Kind != null)?.HBC_Kind;
                    var HBC_KindCode = x.Key.Kind == "1"
                        ? x.FirstOrDefault(y => y.HBC_KindCode != null && y.HBC_KindCode.Type_Seq == BasicCodeTypeConstant.JobTitle)?.HBC_KindCode
                        : x.FirstOrDefault(y => y.HBC_KindCode != null && y.HBC_KindCode.Type_Seq == BasicCodeTypeConstant.PermissionGroup)?.HBC_KindCode;
                    return new FinSalaryAttributionCategoryMaintenance_Data
                    {
                        Factory = x.Key.Factory,
                        Department = x.Key.Department,
                        Department_Name = HOD_Lang?.Department_Name,
                        Department_Code_Name = HOD_Lang?.Department_Code_Name ?? x.Key.Department,
                        Salary_Category = x.Key.Sortcod,
                        Salary_Category_Name = HBC_SalaryCategory?.Code_Name_Str ?? x.Key.Sortcod,
                        Kind = x.Key.Kind,
                        Kind_Name = HBC_Kind?.Code_Name_Str ?? x.Key.Kind,
                        Kind_Code = x.Key.Code,
                        Kind_Code_Name = HBC_KindCode?.Code_Name_Str ?? x.Key.Code,
                        Update_By = x.Key.Update_By,
                        Update_Time = x.Key.Update_Time.ToString("yyyy/MM/dd HH:mm:ss")
                    };
                }).OrderBy(x => x.Department).ThenBy(x => x.Kind_Code).ToList();
            return new OperationResult(true, result);
        }
        #endregion
        public async Task<bool> IsExistedData(FinSalaryAttributionCategoryMaintenance_Param param)
        {
            return await _repositoryAccessor.HRMS_Sal_FinCategory.AnyAsync(x =>
                x.Factory == param.Factory &&
                x.Kind == param.Kind &&
                x.Department == param.Department &&
                x.Code == param.Kind_Code
            );
        }
        public async Task<OperationResult> PostData(FinSalaryAttributionCategoryMaintenance_Update input)
        {
            if (string.IsNullOrWhiteSpace(input.Param.Factory) || string.IsNullOrWhiteSpace(input.Param.Kind))
                return new OperationResult(false, "InvalidInput");
            try
            {
                var predicate = PredicateBuilder.New<HRMS_Sal_FinCategory>(x =>
                    x.Factory == input.Param.Factory &&
                    x.Kind == input.Param.Kind);
                var HSF = await _repositoryAccessor.HRMS_Sal_FinCategory.FindAll(predicate).ToListAsync();
                List<HRMS_Sal_FinCategory> addList = new();
                foreach (var item in input.Data)
                {
                    if (string.IsNullOrWhiteSpace(item.Department)
                     || string.IsNullOrWhiteSpace(item.Kind_Code)
                     || string.IsNullOrWhiteSpace(item.Update_By)
                     || string.IsNullOrWhiteSpace(item.Update_Time)
                     || !DateTime.TryParseExact(item.Update_Time, "yyyy/MM/dd HH:mm:ss", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime updateTimeValue)
                    )
                        return new OperationResult(false, "InvalidInput");
                    if (HSF.Any(x => x.Department == item.Department && x.Code == item.Kind_Code))
                        return new OperationResult(false, "AlreadyExitedData");
                    HRMS_Sal_FinCategory addData = new()
                    {
                        Factory = input.Param.Factory,
                        Kind = input.Param.Kind,
                        Department = item.Department,
                        Code = item.Kind_Code,
                        Sortcod = item.Salary_Category,
                        Update_By = item.Update_By,
                        Update_Time = updateTimeValue,
                    };
                    addList.Add(addData);
                }
                _repositoryAccessor.HRMS_Sal_FinCategory.AddMultiple(addList);
                await _repositoryAccessor.Save();
                return new OperationResult(true);
            }
            catch (Exception)
            {
                return new OperationResult(false, "ErrorException");
            }
        }

        public async Task<OperationResult> PutData(FinSalaryAttributionCategoryMaintenance_Data input)
        {
            if (string.IsNullOrWhiteSpace(input.Factory)
             || string.IsNullOrWhiteSpace(input.Kind)
             || string.IsNullOrWhiteSpace(input.Department)
             || string.IsNullOrWhiteSpace(input.Kind_Code)
             || string.IsNullOrWhiteSpace(input.Salary_Category)
             || string.IsNullOrWhiteSpace(input.Update_By)
             || string.IsNullOrWhiteSpace(input.Update_Time)
             || !DateTime.TryParseExact(input.Update_Time, "yyyy/MM/dd HH:mm:ss", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime updateTimeValue)
            )
                return new OperationResult(false, "InvalidInput");
            try
            {
                var HSF = await _repositoryAccessor.HRMS_Sal_FinCategory.FirstOrDefaultAsync(x =>
                    x.Factory == input.Factory &&
                    x.Department == input.Department &&
                    x.Kind == input.Kind &&
                    x.Code == input.Kind_Code
                );
                if (HSF == null)
                    return new OperationResult(false, "NotExitedData");
                HSF.Sortcod = input.Salary_Category;
                HSF.Update_By = input.Update_By;
                HSF.Update_Time = updateTimeValue;
                _repositoryAccessor.HRMS_Sal_FinCategory.Update(HSF);
                await _repositoryAccessor.Save();
                return new OperationResult(true);
            }
            catch (Exception)
            {
                return new OperationResult(false, "ErrorException");
            }
        }

        public async Task<OperationResult> DeleteData(FinSalaryAttributionCategoryMaintenance_Data data)
        {
            if (string.IsNullOrWhiteSpace(data.Factory)
             || string.IsNullOrWhiteSpace(data.Kind)
             || string.IsNullOrWhiteSpace(data.Department)
             || string.IsNullOrWhiteSpace(data.Kind_Code)
            )
                return new OperationResult(false, "InvalidInput");
            var removeData = await _repositoryAccessor.HRMS_Sal_FinCategory.FirstOrDefaultAsync(x =>
                    x.Factory == data.Factory &&
                    x.Department == data.Department &&
                    x.Kind == data.Kind &&
                    x.Code == data.Kind_Code
                );
            if (removeData == null)
                return new OperationResult(false, "NotExitedData");
            try
            {
                _repositoryAccessor.HRMS_Sal_FinCategory.Remove(removeData);
                return new OperationResult(await _repositoryAccessor.Save());
            }
            catch (Exception)
            {
                return new OperationResult(false, "ErrorException");
            }
        }
        public async Task<OperationResult> DownloadExcel(FinSalaryAttributionCategoryMaintenance_Param param, string userName)
        {
            var result = await GetData(param);
            if (!result.IsSuccess)
                return result;
            var data = result.Data as List<FinSalaryAttributionCategoryMaintenance_Data>;
            if (data.Count == 0)
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
                "Resources\\Template\\SalaryMaintenance\\7_1_27_FinSalaryAttributionCategoryMaintenance\\Download.xlsx", config
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }
        public async Task<OperationResult> DownloadExcelTemplate()
        {
            string path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Resources\\Template\\SalaryMaintenance\\7_1_27_FinSalaryAttributionCategoryMaintenance\\Template.xlsx"
            );
            if (!File.Exists(path))
                return await Task.FromResult(new OperationResult(false, "NotExitedFile"));
            byte[] bytes = File.ReadAllBytes(path);
            return await Task.FromResult(new OperationResult { IsSuccess = true, Data = $"data:xlsx;base64,{Convert.ToBase64String(bytes)}" });
        }

        public async Task<OperationResult> UploadExcel(IFormFile file, List<string> role_List, string username)
        {
            ExcelResult resp = ExcelUtility.CheckExcel(
                file,
                "Resources\\Template\\SalaryMaintenance\\7_1_27_FinSalaryAttributionCategoryMaintenance\\Template.xlsx"
            );
            if (!resp.IsSuccess)
                return new OperationResult(false, resp.Error);
            List<HRMS_Sal_FinCategory> excelDataList = new();
            List<FinSalaryAttributionCategoryMaintenance_Data> excelReportList = new();
            var authorized_factorys = _repositoryAccessor.HRMS_Basic_Role.FindAll(x => role_List.Contains(x.Role)).Select(x => x.Factory).ToList();
            var authorized_departments = await Query_Department_List(authorized_factorys);
            var allowed_kinds = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Method).ToList();
            var allowed_kindCodes = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.JobTitle || x.Type_Seq == BasicCodeTypeConstant.PermissionGroup).ToList();
            var allowed_salaryCategorys = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.SalaryCategory).ToList();
            bool isPassed = true;
            var now = DateTime.Now;
            for (int i = resp.WsTemp.Cells.Rows.Count; i < resp.Ws.Cells.Rows.Count; i++)
            {
                bool isKeyPassed = true;
                string errorMessage = "";

                string _factory = resp.Ws.Cells[i, 0].StringValue.Trim();
                string _kind = resp.Ws.Cells[i, 1].StringValue.Trim();
                string _department = resp.Ws.Cells[i, 2].StringValue.Trim();
                string _kindCode = resp.Ws.Cells[i, 3].StringValue.Trim();
                string _salaryCategory = resp.Ws.Cells[i, 4].StringValue.Trim();
                //Factory
                if (string.IsNullOrWhiteSpace(_factory))
                {
                    errorMessage += $"column Factory cannot be blank.\n";
                    isKeyPassed = false;
                }
                else
                {
                    if (_factory.Length > 10)
                    {
                        errorMessage += $"column Factory's length higher than required ";
                        isKeyPassed = false;
                    }
                    if (!authorized_factorys.Contains(_factory))
                    {
                        errorMessage += $"uploaded [Factory] data does not match the role group.\n";
                        isKeyPassed = false;
                    }
                }
                //Department
                if (string.IsNullOrWhiteSpace(_department))
                {
                    errorMessage += $"column Department cannot be blank.\n";
                    isKeyPassed = false;
                }
                else
                {
                    if (_department.Length > 10)
                    {
                        errorMessage += $"column Department's length higher than required ";
                        isKeyPassed = false;
                    }
                    if (isKeyPassed && !authorized_departments.Any(x => x.Factory == _factory && x.Department == _department))
                    {
                        errorMessage += $"uploaded [Department] data does not match the role group.\n";
                        isKeyPassed = false;
                    }
                }
                //Kind
                bool allowCheckKind = false;
                if (string.IsNullOrWhiteSpace(_kind))
                {
                    errorMessage += $"column Kind cannot be blank.\n";
                    isKeyPassed = false;
                }
                else
                {
                    if (_kind.Length > 10)
                    {
                        errorMessage += $"column Kind's length higher than required.\n";
                        isKeyPassed = false;
                    }
                    if (!allowed_kinds.Any(x => x.Code == _kind))
                    {
                        errorMessage += $"uploaded [Kind] data does not exist.\n";
                        isKeyPassed = false;
                    }
                    else allowCheckKind = true;
                }
                //Kind Code 
                if (string.IsNullOrWhiteSpace(_kindCode))
                {
                    errorMessage += $"column Kind Code cannot be blank.\n";
                    isKeyPassed = false;
                }
                else
                {
                    if (_kindCode.Length > 10)
                    {
                        errorMessage += $"column Kind Code's length higher than required.\n";
                        isKeyPassed = false;
                    }
                    if (!allowed_kindCodes.Any(x => x.Code == _kindCode))
                    {
                        errorMessage += $"uploaded [Kind Code] data does not exist.\n";
                        isKeyPassed = false;
                    }
                    else
                    {
                        if (allowCheckKind)
                        {
                            switch (_kind)
                            {
                                case "1":
                                    if (!allowed_kindCodes.Any(x => x.Type_Seq == BasicCodeTypeConstant.JobTitle && x.Code == _kindCode))
                                    {
                                        errorMessage += $"uploaded [Kind Code] data must be followed uploaded [Kind].\n";
                                        isKeyPassed = false;
                                    }
                                    break;
                                case "2":
                                    if (!allowed_kindCodes.Any(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup && x.Code == _kindCode))
                                    {
                                        errorMessage += $"uploaded [Kind Code] data must be followed uploaded [Kind].\n";
                                        isKeyPassed = false;
                                    }
                                    break;
                                default:
                                    errorMessage += $"uploaded [Kind Code] data must be followed uploaded [Kind].\n";
                                    isKeyPassed = false;
                                    break;
                            }
                        }
                    }
                }
                // Salary Category
                if (string.IsNullOrWhiteSpace(_salaryCategory))
                    errorMessage += $"column Salary Category cannot be blank.\n";
                else
                {
                    if (_salaryCategory.Length > 10)
                        errorMessage += $"column Salary Category's length higher than required.\n";
                    if (!allowed_salaryCategorys.Any(x => x.Code == _salaryCategory))
                    {
                        errorMessage += $"uploaded [Salary Category] data does not exist.\n";
                        isKeyPassed = false;
                    }
                }

                if (isKeyPassed)
                {
                    if (_repositoryAccessor.HRMS_Sal_FinCategory.Any(x => x.Factory == _factory && x.Department == _department && x.Kind == _kind && x.Code == _kindCode))
                        errorMessage += $"Data is already existed.\n";
                    if (excelReportList.Any(x => x.Factory == _factory && x.Department == _department && x.Kind == _kind && x.Kind_Code == _kindCode))
                        errorMessage += $"Identity Conflict Data.\n";
                }

                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    HRMS_Sal_FinCategory excelData = new()
                    {
                        Factory = _factory,
                        Department = _department,
                        Kind = _kind,
                        Code = _kindCode,
                        Sortcod = _salaryCategory,
                        Update_By = username,
                        Update_Time = now
                    };
                    excelDataList.Add(excelData);
                }
                else
                {
                    isPassed = false;
                    errorMessage = errorMessage[..^1];
                }
                FinSalaryAttributionCategoryMaintenance_Data report = new()
                {
                    Factory = _factory,
                    Department = _department,
                    Kind = _kind,
                    Kind_Code = _kindCode,
                    Salary_Category = _salaryCategory,
                    Error_Message = errorMessage
                };
                excelReportList.Add(report);
            }
            if (!isPassed)
            {
                MemoryStream memoryStream = new();
                string fileLocation = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Resources\\Template\\SalaryMaintenance\\7_1_27_FinSalaryAttributionCategoryMaintenance\\Report.xlsx"
                );
                WorkbookDesigner workbookDesigner = new() { Workbook = new Workbook(fileLocation) };
                Worksheet worksheet = workbookDesigner.Workbook.Worksheets[0];
                workbookDesigner.SetDataSource("result", excelReportList);
                workbookDesigner.Process();
                worksheet.AutoFitColumns(worksheet.Cells.MinDataColumn, worksheet.Cells.MaxColumn);
                worksheet.AutoFitRows(worksheet.Cells.MinDataRow + 1, worksheet.Cells.MaxRow);
                workbookDesigner.Workbook.Save(memoryStream, SaveFormat.Xlsx);
                return new OperationResult { IsSuccess = false, Data = memoryStream.ToArray(), Error = "Please check downloaded Error Report" };
            }
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                _repositoryAccessor.HRMS_Sal_FinCategory.AddMultiple(excelDataList);
                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult { IsSuccess = true };
            }
            catch (Exception)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult { IsSuccess = false };
            }
        }
    }
}
