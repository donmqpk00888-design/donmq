using System.Globalization;
using System.Linq;
using API.Data;
using API._Services.Interfaces.SalaryMaintenance;
using API.DTOs.SalaryMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryMaintenance
{
    public class S_7_1_9_DepartmentToSAPCostCenterMappingMaintenance : BaseServices, I_7_1_9_DepartmentToSAPCostCenterMappingMaintenance
    {
        public S_7_1_9_DepartmentToSAPCostCenterMappingMaintenance(DBContext dbContext) : base(dbContext)
        {
        }
        #region Create
        public async Task<OperationResult> Create(D_7_9_Sal_Dept_SAPCostCenter_MappingDTO data, string userName)
        {
            var item = new HRMS_Sal_Dept_SAPCostCenter_Mapping
            {
                Factory = data.Factory,
                Cost_Year = DateTime.ParseExact(data.Cost_Year_Str, "yyyy", CultureInfo.InvariantCulture),
                Cost_Code = data.Cost_Code,
                Department = data.Department,
                Update_By = userName,
                Update_Time = DateTime.Now
            };
            try
            {
                _repositoryAccessor.HRMS_Sal_Dept_SAPCostCenter_Mapping.Add(item);
                await _repositoryAccessor.Save();
                return new OperationResult(true, "Create Successfully");
            }
            catch (Exception ex)
            {
                return new OperationResult(false, $"Inner exception: {ex.InnerException?.Message ?? "No inner exception message available"}");
            }
        }
        #endregion
        #region Update
        public async Task<OperationResult> Update(D_7_9_Sal_Dept_SAPCostCenter_MappingDTO data, string userName)
        {
            var item = await _repositoryAccessor.HRMS_Sal_Dept_SAPCostCenter_Mapping.FirstOrDefaultAsync(x => x.Cost_Year.Year == int.Parse(data.Cost_Year_Str)
                && x.Factory == data.Factory
                && x.Department == data.Department);
            if (item == null)
                return new OperationResult(false, "No data");
            // item.Factory = data.Factory;
            // item.Cost_Year = DateTime.ParseExact(data.Cost_Year, "yyyy", CultureInfo.InvariantCulture);
            item.Cost_Code = data.Cost_Code;
            // item.Department = data.Department_New;
            item.Update_By = userName;
            item.Update_Time = DateTime.Now;

            _repositoryAccessor.HRMS_Sal_Dept_SAPCostCenter_Mapping.Update(item);
            try
            {
                await _repositoryAccessor.Save();
                return new OperationResult(true, "Update Successfully");
            }
            catch (Exception ex)
            {
                return new OperationResult(false, $"Inner exception: {ex.InnerException?.Message ?? "No inner exception message available"}");
            }
        }
        #endregion
        #region Delete
        public async Task<OperationResult> Delete(D_7_9_Sal_Dept_SAPCostCenter_MappingDTO data)
        {
            var item = await _repositoryAccessor.HRMS_Sal_Dept_SAPCostCenter_Mapping.FirstOrDefaultAsync(x => x.Cost_Year.Year == int.Parse(data.Cost_Year_Str)
                && x.Factory == data.Factory
                && x.Department == data.Department);
            if (item == null)
                return new OperationResult(false, "Data not exist");
            _repositoryAccessor.HRMS_Sal_Dept_SAPCostCenter_Mapping.Remove(item);
            if (await _repositoryAccessor.Save())
                return new OperationResult(true, "Delete Successfully");
            return new OperationResult(false, "Delete failed");
        }
        #endregion
        #region GetData
        public async Task<List<D_7_9_Sal_Dept_SAPCostCenter_MappingDTO>> GetData(D_7_9_Sal_Dept_SAPCostCenter_MappingParam param)
        {
            var pred = PredicateBuilder.New<HRMS_Sal_Dept_SAPCostCenter_Mapping>(true);
            var predHSS = PredicateBuilder.New<HRMS_Sal_SAPCostCenter>(true);

            if (!string.IsNullOrWhiteSpace(param.Factory))
            {
                pred = pred.And(x => x.Factory == param.Factory);
                predHSS = predHSS.And(x => x.Factory == param.Factory);
            }

            if (!string.IsNullOrWhiteSpace(param.Year_Str))
            {
                pred = pred.And(x => x.Cost_Year.Year == int.Parse(param.Year_Str));
                predHSS = predHSS.And(x => x.Cost_Year.Year == int.Parse(param.Year_Str));
            }
            if (!string.IsNullOrWhiteSpace(param.Department))
                pred = pred.And(x => x.Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.CostCenter))
                pred = pred.And(x => x.Cost_Code == param.CostCenter);

            var data = await _repositoryAccessor.HRMS_Sal_Dept_SAPCostCenter_Mapping.FindAll(pred, true)
            .GroupJoin(_repositoryAccessor.HRMS_Sal_SAPCostCenter.FindAll(predHSS),
                x => new { x.Cost_Code, x.Cost_Year.Year },
                y => new { y.Cost_Code, y.Cost_Year.Year },
                (x, y) => new { HADSM = x, HSS = y })
            .SelectMany(x => x.HSS.DefaultIfEmpty(),
                (x, y) => new { x.HADSM, HSS = y })
            .GroupJoin(_repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == param.Factory, true),
                x => x.HADSM.Department,
                y => y.Department_Code,
                (x, y) => new { x.HADSM, x.HSS, HOD = y })
            .SelectMany(x => x.HOD.DefaultIfEmpty(),
                (x, y) => new { x.HADSM, x.HSS, HOD = y })
            .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true),
                x => new { x.HOD.Department_Code, x.HOD.Division, x.HOD.Factory },
                y => new { y.Department_Code, y.Division, y.Factory },
                (x, y) => new { x.HADSM, x.HSS, x.HOD, HODL = y })
            .SelectMany(x => x.HODL.DefaultIfEmpty(),
                (x, y) => new { x.HADSM, x.HSS, x.HOD, HODL = y }).ToListAsync();
            var result = data.Select(x => new D_7_9_Sal_Dept_SAPCostCenter_MappingDTO
            {
                Factory = x.HADSM.Factory,
                Cost_Year = x.HADSM.Cost_Year.ToString("yyyy"),
                Cost_Year_Str = x.HADSM.Cost_Year.ToString("yyyy"),
                Cost_Code = x.HADSM.Cost_Code,
                Code_Name = x.HSS != null ? param.Language == "en" ? x.HSS.Code_Name_EN : x.HSS.Code_Name : "",
                Department = x.HADSM.Department,
                Department_Name = x.HODL != null ? x.HODL.Name : (x.HOD != null ? x.HOD.Department_Name : ""),
                Update_By = x.HADSM.Update_By,
                Update_Time = x.HADSM.Update_Time.ToString("yyyy/MM/dd HH:mm:ss")
            }).ToList();
            return result.GroupBy(x => new { x.Factory, x.Cost_Year, x.Department }).Select(x => x.First()).ToList();
        }

        public async Task<PaginationUtility<D_7_9_Sal_Dept_SAPCostCenter_MappingDTO>> GetDataPagination(PaginationParam pagination, D_7_9_Sal_Dept_SAPCostCenter_MappingParam param)
        {
            var data = await GetData(param);
            return PaginationUtility<D_7_9_Sal_Dept_SAPCostCenter_MappingDTO>.Create(data, pagination.PageNumber, pagination.PageSize);
        }
        #endregion
        #region Download
        public async Task<OperationResult> DownloadExcel(D_7_9_Sal_Dept_SAPCostCenter_MappingParam param, string userName)
        {
            var data = await GetData(param);
            if (!data.Any())
                return new OperationResult(false, "No Data");
            List<Table> tables = new()
            {
                new Table("result", data)
            };
            List<Cell> cells = new()
            {
                new Cell("B1", userName),
                new Cell("D1", DateTime.Now),
            };
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                tables, 
                cells, 
                "Resources\\Template\\SalaryMaintenance\\7_1_9_SalDeptSAPCostCenterMapping\\Download.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }
        #endregion
        #region UploadExcel

        public async Task<OperationResult> UploadExcel(IFormFile file, List<string> role_List, string userName)
        {
            ExcelResult resp = ExcelUtility.CheckExcel(
                file, 
                "Resources\\Template\\SalaryMaintenance\\7_1_9_SalDeptSAPCostCenterMapping\\Template.xlsx"
            );
            if (!resp.IsSuccess)
                return new OperationResult(false, resp.Error);

            List<HRMS_Sal_Dept_SAPCostCenter_Mapping> data = new();
            List<D_7_9_Sal_Dept_SAPCostCenter_MappingDTO> dataReport = new();

            List<string> roleFactories = await _repositoryAccessor.HRMS_Basic_Role
                .FindAll(x => role_List.Contains(x.Role))
                .Select(x => x.Factory).Distinct()
                .ToListAsync();

            if (!roleFactories.Any())
                return new OperationResult(false, "Recent account roles do not have any factory.");

            for (int i = resp.WsTemp.Cells.Rows.Count; i < resp.Ws.Cells.Rows.Count; i++)
            {
                List<string> errorMessage = new();
                string factory = resp.Ws.Cells[i, 0].StringValue?.Trim();
                string year = resp.Ws.Cells[i, 1].StringValue?.Trim();
                string cost_Center = resp.Ws.Cells[i, 2].StringValue?.Trim();
                string department = resp.Ws.Cells[i, 3].StringValue?.Trim();

                // area validate data
                // 1. Factory
                if (string.IsNullOrWhiteSpace(factory))
                    errorMessage.Add("Factory is invalid.\n");
                if (!string.IsNullOrWhiteSpace(factory))
                {
                    if (!roleFactories.Contains(factory))
                        errorMessage.Add($"Uploaded Factory: {factory} data does not match the role group.\n");
                }

                // 2. Year
                var validYear = DateTime.TryParseExact(year, "yyyy", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime yearValue);
                if (!validYear)
                    errorMessage.Add("Year is invalid date format (yyyy). ");

                // 3. Cost Center
                if (string.IsNullOrWhiteSpace(cost_Center))
                    errorMessage.Add("Cost Center is invalid.\n");

                var dept = await Query_Department_List(factory);
                List<string> checkDepartment = dept.Select(x => x.Department_Code).ToList();
                // 4. Department
                if (string.IsNullOrWhiteSpace(department))
                    errorMessage.Add("Department is invalid.\n");
                if (!checkDepartment.Contains(department) && !string.IsNullOrWhiteSpace(factory))
                    errorMessage.Add($"Department: {department} not under the corresponding Factory: {factory}.\n");

                // Kiểm tra trùng lặp cho Factory + Year + Department
                if (!string.IsNullOrWhiteSpace(factory) && validYear && !string.IsNullOrWhiteSpace(department))
                {
                    var checkDuplicate = await _repositoryAccessor.HRMS_Sal_Dept_SAPCostCenter_Mapping.AnyAsync(x => x.Factory == factory
                            && x.Cost_Year.Year == yearValue.Year
                            && x.Department == department);
                    if (checkDuplicate)
                        errorMessage.Add("In the same year, a factory can only perform maintenance for one department once.\n");
                }

                if (!errorMessage.Any())
                {
                    HRMS_Sal_Dept_SAPCostCenter_Mapping newData = new()
                    {
                        Factory = factory,
                        Cost_Year = yearValue,
                        Cost_Code = cost_Center,
                        Department = department,
                        Update_Time = DateTime.Now,
                        Update_By = userName,
                    };
                    data.Add(newData);
                }
                D_7_9_Sal_Dept_SAPCostCenter_MappingDTO report = new()
                {
                    Factory = factory,
                    Cost_Year = year,
                    Cost_Code = cost_Center,
                    Department = department,
                    Update_Time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    Update_By = userName,
                    Error_Message = !errorMessage.Any() ? "Y" : string.Join("\r\n", errorMessage)
                };
                dataReport.Add(report);
            }

            if (dataReport.Any())
            {
                MemoryStream memoryStream = new();
                string fileLocation = Path.Combine(
                    Directory.GetCurrentDirectory(), 
                    "Resources\\Template\\SalaryMaintenance\\7_1_9_SalDeptSAPCostCenterMapping\\Report.xlsx"
                );
                Aspose.Cells.WorkbookDesigner workbookDesigner = new() { Workbook = new Aspose.Cells.Workbook(fileLocation) };
                Aspose.Cells.Worksheet worksheet = workbookDesigner.Workbook.Worksheets[0];
                worksheet.Cells["B2"].PutValue(userName);
                worksheet.Cells["D2"].PutValue(DateTime.Now);
                workbookDesigner.SetDataSource("result", dataReport);
                workbookDesigner.Process();
                worksheet.AutoFitColumns(worksheet.Cells.MinDataColumn, worksheet.Cells.MaxColumn);
                worksheet.AutoFitRows(worksheet.Cells.MinDataRow + 1, worksheet.Cells.MaxRow);
                workbookDesigner.Workbook.Save(memoryStream, Aspose.Cells.SaveFormat.Xlsx);
                if (dataReport.Exists(x => x.Error_Message != "Y"))
                    return new OperationResult { IsSuccess = false, Data = memoryStream.ToArray(), Error = "Please check Error Report" };
            }

            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                _repositoryAccessor.HRMS_Sal_Dept_SAPCostCenter_Mapping.AddMultiple(data);
                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true, "System.Message.UploadOKMsg");
            }
            catch (Exception ex)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, ex.Message);
            }
        }
        #endregion
        #region Get List
        public async Task<List<KeyValuePair<string, string>>> GetListCostCenter(D_7_9_Sal_Dept_SAPCostCenter_MappingParam param)
        {
            if (string.IsNullOrWhiteSpace(param.Factory) || string.IsNullOrWhiteSpace(param.Year_Str) || !int.TryParse(param.Year_Str, out int year))
                return new List<KeyValuePair<string, string>>();
            string language = param.Language?.Trim().ToUpper(); 
            var query = _repositoryAccessor.HRMS_Sal_SAPCostCenter.FindAll(x => x.Factory == param.Factory && x.Cost_Year.Year == year);
            if (!await query.AnyAsync())
                return new List<KeyValuePair<string, string>>();
            List<KeyValuePair<string, string>> result;
            if (language == "EN")
                result = await query.Select(x => new KeyValuePair<string, string>(x.Cost_Code, $"{x.Cost_Code} - {x.Code_Name_EN}")).ToListAsync();
            else
                result = await query.Select(x => new KeyValuePair<string, string>(x.Cost_Code, $"{x.Cost_Code} - {x.Code_Name}")).ToListAsync();
            return result;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(List<string> roleList, string language)
        {
            var predHBC = PredicateBuilder.New<HRMS_Basic_Code>(x => x.Type_Seq == BasicCodeTypeConstant.Factory);

            var factorys = await Queryt_Factory_AddList(roleList);
            predHBC.And(x => factorys.Contains(x.Code));

            var data = await _repositoryAccessor.HRMS_Basic_Code.FindAll(predHBC, true)
                        .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                           x => new { x.Type_Seq, x.Code },
                           y => new { y.Type_Seq, y.Code },
                           (x, y) => new { HBC = x, HBCL = y }
                        ).SelectMany(x => x.HBCL.DefaultIfEmpty(),
                            (x, y) => new { x.HBC, HBCL = y }
                        ).Select(x => new KeyValuePair<string, string>(
                            x.HBC.Code.Trim(),
                            x.HBC.Code.Trim() + " - " + (x.HBCL != null ? x.HBCL.Code_Name.Trim() : x.HBC.Code_Name.Trim())
                        )).Distinct().ToListAsync();
            return data;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string factory, string language)
        {
            var HOD = await Query_Department_List(factory);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language
                .FindAll(x => x.Factory == factory
                           && x.Language_Code.ToLower() == language.ToLower());

            var deparment = HOD.GroupJoin(HODL,
                        x => new {x.Division, x.Department_Code},
                        y => new {y.Division, y.Department_Code},
                        (x, y) => new { dept = x, hodl = y })
                        .SelectMany(x => x.hodl.DefaultIfEmpty(),
                        (x, y) => new { x.dept, hodl = y })
                        .Select(x => new KeyValuePair<string, string>(x.dept.Department_Code, $"{x.dept.Department_Code}-{(x.hodl != null ? x.hodl.Name : x.dept.Department_Name)}"))
                        .ToList();
            return deparment;
        }
        #endregion
        public async Task<OperationResult> DownloadTemplate()
        {
            string path = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "Resources\\Template\\SalaryMaintenance\\7_1_9_SalDeptSAPCostCenterMapping\\Template.xlsx"
            );
            if (!File.Exists(path))
                return await Task.FromResult(new OperationResult(false, "NotExitedFile"));
            byte[] bytes = File.ReadAllBytes(path);
            return await Task.FromResult(new OperationResult { IsSuccess = true, Data = $"data:xlsx;base64,{Convert.ToBase64String(bytes)}" });
        }

        public async Task<bool> CheckDuplicate(string factory, string year, string department)
        {
            var checkDepartment = await _repositoryAccessor.HRMS_Sal_Dept_SAPCostCenter_Mapping.AnyAsync(x => x.Cost_Year.Year == int.Parse(year)
                && x.Factory == factory
                && x.Department == department);
            return checkDepartment;
        }
    }
}