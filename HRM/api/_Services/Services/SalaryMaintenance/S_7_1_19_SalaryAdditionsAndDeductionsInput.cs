using AgileObjects.AgileMapper;
using API.Data;
using API._Services.Interfaces.SalaryMaintenance;
using API.DTOs.SalaryMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryMaintenance
{
    public class S_7_1_19_SalaryAdditionsAndDeductionsInput : BaseServices, I_7_1_19_SalaryAdditionsAndDeductionsInput
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        public S_7_1_19_SalaryAdditionsAndDeductionsInput(DBContext dbContext,IWebHostEnvironment webHostEnvironment) : base(dbContext)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<OperationResult> Create(SalaryAdditionsAndDeductionsInputDto dto)
        {
            if (await _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.AnyAsync(x =>
                x.Factory == dto.Factory &&
                x.AddDed_Item == dto.AddDed_Item &&
                x.AddDed_Type == dto.AddDed_Type &&
                x.Sal_Month == Convert.ToDateTime(dto.Sal_Month_Str) &&
                x.Employee_ID == dto.Employee_ID))
                return new OperationResult(false, "SalaryMaintenance.BankAccountMaintenance.Duplicates");

            var dataCreate = new HRMS_Sal_AddDedItem_Monthly
            {
                USER_GUID = dto.USER_GUID,
                Factory = dto.Factory,
                Employee_ID = dto.Employee_ID,
                Sal_Month = Convert.ToDateTime(dto.Sal_Month_Str),
                AddDed_Type = dto.AddDed_Type,
                AddDed_Item = dto.AddDed_Item,
                Currency = dto.Currency,
                Amount = dto.Amount,
                Update_By = dto.Update_By,
                Update_Time = Convert.ToDateTime(dto.Update_Time_Str)
            };

            try
            {
                _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.Add(dataCreate);
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.CreateOKMsg");
            }
            catch (Exception)
            {
                return new OperationResult(false, "System.Message.CreateErrorMsg");
            }
        }

        public async Task<OperationResult> Delete(SalaryAdditionsAndDeductionsInputDto dto)
        {
            var item = await _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.FirstOrDefaultAsync(x =>
                x.Factory == dto.Factory &&
                x.AddDed_Item == dto.AddDed_Item &&
                x.AddDed_Type == dto.AddDed_Type &&
                x.Sal_Month == Convert.ToDateTime(dto.Sal_Month_Str) &&
                x.Employee_ID == dto.Employee_ID);
            if (item is not null)
                _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.Remove(item);

            try
            {
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.DeleteOKMsg");
            }
            catch (Exception)
            {
                return new OperationResult(false, "System.Message.DeleteErrorMsg");
            }
        }

        public async Task<OperationResult> Update(SalaryAdditionsAndDeductionsInputDto dto)
        {
            var item = await _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.FirstOrDefaultAsync(x =>
                x.Factory == dto.Factory &&
                x.AddDed_Item == dto.AddDed_Item &&
                x.AddDed_Type == dto.AddDed_Type &&
                x.Sal_Month == Convert.ToDateTime(dto.Sal_Month_Str) &&
                x.Employee_ID == dto.Employee_ID);
            if (item is not null)
            {
                item = Mapper.Map(dto).Over(item);

                item.Update_Time = Convert.ToDateTime(dto.Update_Time_Str);
                item.Update_By = dto.Update_By;
                item.Currency = dto.Currency;
                item.Amount = dto.Amount;

                _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.Update(item);
            }

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

        public async Task<OperationResult> DownloadFileExcel(SalaryAdditionsAndDeductionsInput_Param param, string userName)
        {
            var data = await GetData(param);
            if (!data.Any())
                return new OperationResult(false, "System.Message.NoData");

            List<Cell> dataCells = new()
            {
                new Cell("B" + 2, userName),
                new Cell("D" + 2, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))
            };

            var index = 5;
            for (int i = 0; i < data.Count; i++)
            {
                dataCells.Add(new Cell("A" + index, data[i].Sal_Month));
                dataCells.Add(new Cell("B" + index, data[i].Department_Code));
                dataCells.Add(new Cell("C" + index, data[i].Department_Name));
                dataCells.Add(new Cell("D" + index, data[i].Employee_ID));
                dataCells.Add(new Cell("E" + index, data[i].Local_Full_Name));
                dataCells.Add(new Cell("F" + index, data[i].AddDed_Type_Str));
                dataCells.Add(new Cell("G" + index, data[i].AddDed_Item_Str));
                dataCells.Add(new Cell("H" + index, data[i].Amount));
                dataCells.Add(new Cell("I" + index, data[i].Resign_Date));
                index += 1;
            }

            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataCells,
                "Resources\\Template\\SalaryMaintenance\\7_1_19_SalaryAdditionsAndDeductionsInput\\Download.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }

        public Task<OperationResult> DownloadFileTemplate()
        {
            var path = Path.Combine(
                _webHostEnvironment.ContentRootPath,
                "Resources\\Template\\SalaryMaintenance\\7_1_19_SalaryAdditionsAndDeductionsInput\\Upload.xlsx"
            );
            var workbook = new Aspose.Cells.Workbook(path);
            var design = new Aspose.Cells.WorkbookDesigner(workbook);
            MemoryStream stream = new();
            design.Workbook.Save(stream, Aspose.Cells.SaveFormat.Xlsx);
            var result = stream.ToArray();
            return Task.FromResult(new OperationResult(true, null, result));
        }

        public async Task<PaginationUtility<SalaryAdditionsAndDeductionsInputDto>> GetDataPagination(PaginationParam pagination, SalaryAdditionsAndDeductionsInput_Param param)
        {
            var data = await GetData(param);
            return PaginationUtility<SalaryAdditionsAndDeductionsInputDto>.Create(data, pagination.PageNumber, pagination.PageSize);
        }

        private async Task<List<SalaryAdditionsAndDeductionsInputDto>> GetData(SalaryAdditionsAndDeductionsInput_Param param)
        {

            var predicate = PredicateBuilder.New<HRMS_Sal_AddDedItem_Monthly>(x => x.Factory == param.Factory);

            if (!string.IsNullOrWhiteSpace(param.AddDed_Type))
                predicate.And(x => x.AddDed_Type == param.AddDed_Type);
            if (!string.IsNullOrWhiteSpace(param.AddDed_Item))
                predicate.And(x => x.AddDed_Item == param.AddDed_Item);
            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                predicate = predicate.And(x => x.Employee_ID.Contains(param.Employee_ID.Trim()));
            if (!string.IsNullOrWhiteSpace(param.Sal_Month))
                predicate.And(x => x.Sal_Month == Convert.ToDateTime(param.Sal_Month));

            var HBC_Lang = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.IsActive)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower()),
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
            var HBC_AdditionsAndDeductionsItem = HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsItem);
            var HBC_AdditionsAndDeductionsType = HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsType);
            var HBC_Currency = HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.Currency);

            var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == param.Factory);
            var HODL = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower());
            var HOD_Lang = HOD
                .GroupJoin(HODL,
                    x => new { x.Department_Code, x.Factory },
                    y => new { y.Department_Code, y.Factory },
                    (x, y) => new { HOD = x, HODL = y })
                .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (x, y) => new { x.HOD, HODL = y })
                .Select(x => new
                {
                    x.HOD.Factory,
                    x.HOD.Division,
                    x.HOD.Department_Code,
                    Department_Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name
                });

            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll()
                .Select(x => new
                {
                    x.USER_GUID,
                    x.Local_Full_Name,
                    x.Resign_Date,
                    Division = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Division : x.Division,
                    Factory = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Factory : x.Factory,
                    Department = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Department : x.Department,
                });
            var HSAM = _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.FindAll(predicate, true).OrderBy(x => x.Employee_ID);

            var result = await HSAM
                .GroupJoin(HEP,
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { HSAM = x, HEP = y })
                .SelectMany(x => x.HEP.DefaultIfEmpty(),
                    (x, y) => new { x.HSAM, HEP = y })
                .GroupJoin(HOD_Lang,
                    x => new { x.HEP.Factory, x.HEP.Division, Department_Code = x.HEP.Department },
                    y => new { y.Factory, y.Division, y.Department_Code },
                    (x, y) => new { x.HSAM, x.HEP, HOD_Lang = y })
                .SelectMany(x => x.HOD_Lang.DefaultIfEmpty(),
                    (x, y) => new { x.HSAM, x.HEP, HOD_Lang = y })
                .GroupJoin(HBC_AdditionsAndDeductionsItem,
                    x => x.HSAM.AddDed_Item,
                    y => y.Code,
                    (x, y) => new { x.HSAM, x.HEP, x.HOD_Lang, HBC_AdditionsAndDeductionsItem = y })
                .SelectMany(x => x.HBC_AdditionsAndDeductionsItem.DefaultIfEmpty(),
                    (x, y) => new { x.HSAM, x.HEP, x.HOD_Lang, HBC_AdditionsAndDeductionsItem = y })
                .GroupJoin(HBC_AdditionsAndDeductionsType,
                    x => x.HSAM.AddDed_Type,
                    y => y.Code,
                    (x, y) => new { x.HSAM, x.HEP, x.HOD_Lang, x.HBC_AdditionsAndDeductionsItem, HBC_AdditionsAndDeductionsType = y })
                .SelectMany(x => x.HBC_AdditionsAndDeductionsType.DefaultIfEmpty(),
                    (x, y) => new { x.HSAM, x.HEP, x.HOD_Lang, x.HBC_AdditionsAndDeductionsItem, HBC_AdditionsAndDeductionsType = y })
                .GroupJoin(HBC_Currency,
                    x => x.HSAM.Currency,
                    y => y.Code,
                    (x, y) => new { x.HSAM, x.HEP, x.HOD_Lang, x.HBC_AdditionsAndDeductionsItem, x.HBC_AdditionsAndDeductionsType, HBC_Currency = y })
                .SelectMany(x => x.HBC_Currency.DefaultIfEmpty(),
                    (x, y) => new { x.HSAM, x.HEP, x.HOD_Lang, x.HBC_AdditionsAndDeductionsItem, x.HBC_AdditionsAndDeductionsType, HBC_Currency = y })
                .Select(x => new SalaryAdditionsAndDeductionsInputDto
                {
                    USER_GUID = x.HSAM.USER_GUID,
                    Factory = x.HSAM.Factory,
                    Employee_ID = x.HSAM.Employee_ID,
                    Department_Code = x.HEP.Department,
                    Department_Name = x.HOD_Lang.Department_Name,
                    Department_Code_Name = x.HOD_Lang != null && !string.IsNullOrWhiteSpace(x.HOD_Lang.Department_Name)
                        ? x.HEP.Department + "-" + x.HOD_Lang.Department_Name : x.HEP.Department,
                    AddDed_Type = x.HSAM.AddDed_Type,
                    AddDed_Type_Str = x.HBC_AdditionsAndDeductionsType.Code_Name,
                    AddDed_Item = x.HSAM.AddDed_Item,
                    AddDed_Item_Str = x.HBC_AdditionsAndDeductionsItem.Code_Name,
                    Currency = x.HSAM.Currency,
                    Currency_Str = x.HBC_Currency.Code_Name,
                    Sal_Month = x.HSAM.Sal_Month,
                    Sal_Month_Str = x.HSAM.Sal_Month.ToString("yyyy/MM/dd"),
                    Amount = x.HSAM.Amount,
                    Local_Full_Name = x.HEP != null ? x.HEP.Local_Full_Name : string.Empty,
                    Update_By = x.HSAM.Update_By,
                    Update_Time = x.HSAM.Update_Time,
                    Update_Time_Str = x.HSAM.Update_Time.ToString("yyyy/MM/dd HH:mm:ss"),
                    Resign_Date = x.HEP.Resign_Date,
                    Resign_Date_Str = x.HEP.Resign_Date.HasValue ? x.HEP.Resign_Date.Value.ToString("yyyy/MM/dd HH:mm:ss") : null,
                }).ToListAsync();

            if (!string.IsNullOrWhiteSpace(param.Department))
                result = result.Where(x => x.Department_Code == param.Department).ToList();

            return result;
        }
        public async Task<List<KeyValuePair<string, string>>> GetListAddDedItem(string language)
        => await GetDataBasicCode(BasicCodeTypeConstant.AdditionsAndDeductionsItem, language);

        public async Task<List<KeyValuePair<string, string>>> GetListAddDedType(string language)
        => await GetDataBasicCode(BasicCodeTypeConstant.AdditionsAndDeductionsType, language);
        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string language, string factory)
        {
            var result = await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == factory, true)
                           .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Factory == factory && x.Language_Code.ToLower() == language.ToLower()),
                                       x => new { x.Division, x.Factory, x.Department_Code },
                                       y => new { y.Division, y.Factory, y.Department_Code },
                                       (x, y) => new { HOD = x, HODL = y }
                            ).SelectMany(x => x.HODL.DefaultIfEmpty(),
                                (x, y) => new { x.HOD, HODL = y }
                            ).Select(x => new KeyValuePair<string, string>
                            (
                                x.HOD.Department_Code,
                                x.HOD.Department_Code.Trim() + " - " + (x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)
                            )).Distinct().ToListAsync();
            return result;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string language, List<string> roleList)
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

        public async Task<OperationResult> UploadFileExcel(SalaryAdditionsAndDeductionsInput_Upload param, List<string> roleList, string userName)
        {
            ExcelResult resp = ExcelUtility.CheckExcel(
                param.File,
                "Resources\\Template\\SalaryMaintenance\\7_1_19_SalaryAdditionsAndDeductionsInput\\Upload.xlsx"
            );
            if (!resp.IsSuccess)
                return new OperationResult(false, resp.Error);
            List<HRMS_Sal_AddDedItem_Monthly> HSAM_Creates = new();
            List<SalaryAdditionsAndDeductionsInput_Report> excelReportList = new();

            bool isCheck = true;
            var currencys = await GetListCurrency(param.Language);
            var addDedItems = await GetListAddDedItem(param.Language);
            var addDedTypes = await GetListAddDedType(param.Language);
            var allowFactories = await GetListFactory(param.Language, roleList);
            var factoryCodes = allowFactories.Select(x => x.Key).ToHashSet();
            var HEPs = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => factoryCodes.Contains(x.Factory), true)
            .Select(
                x => new SalaryAdditionsAndDeductionsInput_Personal
                {
                    Factory = string.IsNullOrEmpty(x.Employment_Status) ? x.Factory : x.Assigned_Factory,
                    Division = string.IsNullOrEmpty(x.Employment_Status) ? x.Division : x.Assigned_Division,
                    Employee_ID = x.Employee_ID,
                    USER_GUID = x.USER_GUID,
                }
            ).ToListAsync();

            var listFactorys = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory).ToListAsync();
            var now = DateTime.Now;
            for (int i = resp.WsTemp.Cells.Rows.Count; i < resp.Ws.Cells.Rows.Count; i++)
            {
                bool isKeyPassed = true;
                string errorMessage = "";
                // Factory
                string factoryCode = resp.Ws.Cells[i, 0].StringValue.Trim();
                var factoryData = listFactorys.FirstOrDefault(x => x.Code == factoryCode);
                if (factoryData == null || factoryCode.Length > 10)
                {
                    errorMessage += $"Factory is invalid \n";
                    isKeyPassed = false;
                }

                if (!factoryCodes.Contains(factoryCode))
                {
                    errorMessage += $"Factory '{factoryCode}' is not valid for user {userName}. \n";
                    isKeyPassed = false;
                }

                // Sal_Month
                var salMonthStr = resp.Ws.Cells[i, 1].StringValue?.Trim();
                var salMonthDate = DateTime.TryParse(salMonthStr, out var _date) ? new DateTime(_date.Year, _date.Month, 1) : (DateTime?)null;
                if (!salMonthDate.HasValue)
                {
                    errorMessage += $"Sal Month is invalid\n";
                    isKeyPassed = false;
                }

                // Employee ID
                string employeeID = resp.Ws.Cells[i, 2].StringValue.Trim();
                if (employeeID == null || employeeID.Length > 16)
                {
                    errorMessage += $"Employee ID is invalid \n";
                    isKeyPassed = false;
                }

                var itemHEP = HEPs.FirstOrDefault(x => x.Factory == factoryCode && x.Employee_ID == employeeID);
                if (itemHEP is null)
                {
                    errorMessage += $"Factory: {factoryCode} \nEmployee ID : {employeeID} does not exist.  \n";
                    isKeyPassed = false;
                }
                // AddDedType
                string addDedTypeCode = resp.Ws.Cells[i, 3].StringValue.Trim();
                if (addDedTypeCode == null || addDedTypeCode.Length > 1)
                {
                    errorMessage += $"AddDedType is invalid \n";
                    isKeyPassed = false;
                }
                var addDedTypeData = addDedTypes.FirstOrDefault(x => x.Key == addDedTypeCode);
                if (EqualityComparer<KeyValuePair<string, string>>.Default.Equals(addDedTypeData, default))
                {
                    errorMessage += $"AddDedType : {addDedTypeCode} does not exist \n";
                    isKeyPassed = false;
                }
                // AddDedItem
                string addDedItemCode = resp.Ws.Cells[i, 4].StringValue.Trim();
                if (addDedItemCode == null || addDedItemCode.Length > 3)
                {
                    errorMessage += $"AddDedItem is invalid \n";
                    isKeyPassed = false;
                }
                var addDedItemData = addDedItems.FirstOrDefault(x => x.Key == addDedItemCode);
                if (EqualityComparer<KeyValuePair<string, string>>.Default.Equals(addDedItemData, default))
                {
                    errorMessage += $"AddDedItem : {addDedItemCode} does not exist. \n";
                    isKeyPassed = false;
                }
                // Currency
                string currencyCode = resp.Ws.Cells[i, 5].StringValue.Trim();
                if (currencyCode == null || currencyCode.Length > 3)
                    errorMessage += $"Currency is invalid \n";

                var currencyData = currencys.FirstOrDefault(x => x.Key == currencyCode);
                if (EqualityComparer<KeyValuePair<string, string>>.Default.Equals(currencyData, default))
                    errorMessage += $"Currency : {currencyCode} does not exist \n";

                // Amount
                string amountStr = resp.Ws.Cells[i, 6].StringValue.Trim();
                if (string.IsNullOrEmpty(amountStr) || !int.TryParse(amountStr, out int amount) || amount > int.MaxValue || amount < 0)
                    errorMessage += $"Amount is invalid \n";

                if (isKeyPassed)
                {
                    if (_repositoryAccessor.HRMS_Sal_AddDedItem_Monthly
                        .Any(x =>
                            x.Factory == factoryCode &&
                            x.Employee_ID == employeeID &&
                           x.Sal_Month == salMonthDate.Value &&
                            x.AddDed_Type == addDedTypeCode &&
                            x.AddDed_Item == addDedItemCode))
                        errorMessage += $"Data already exists. \n";
                    if (excelReportList
                        .Any(x =>
                            x.Factory == factoryCode &&
                            x.Employee_ID == employeeID &&
                            x.Sal_MonthStr == salMonthStr &&
                            x.AddDed_Type == addDedTypeCode &&
                            x.AddDed_Item == addDedItemCode))
                        errorMessage += $"Data are duplicated. \n";
                }

                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    HRMS_Sal_AddDedItem_Monthly dataCreates = new()
                    {
                        USER_GUID = itemHEP.USER_GUID,
                        Factory = factoryCode,
                        Employee_ID = employeeID,
                        Sal_Month = salMonthDate.Value,
                        AddDed_Type = addDedTypeCode,
                        AddDed_Item = addDedItemCode,
                        Currency = currencyCode,
                        Amount = int.Parse(amountStr),
                        Update_By = userName,
                        Update_Time = now
                    };
                    HSAM_Creates.Add(dataCreates);
                }
                else
                {
                    isCheck = false;
                    errorMessage = errorMessage.Trim();
                }
                SalaryAdditionsAndDeductionsInput_Report report = new()
                {
                    Factory = factoryCode,
                    Employee_ID = employeeID,
                    Sal_MonthStr = salMonthStr,
                    AddDed_Type = addDedTypeCode,
                    AddDed_Item = addDedItemCode,
                    Currency = currencyCode,
                    AmountStr = amountStr,
                    IsCorrect = string.IsNullOrEmpty(errorMessage) ? "Y" : "N",
                    Error_Message = errorMessage
                };
                excelReportList.Add(report);
            }

            if (!isCheck)
            {
                MemoryStream memoryStream = new();
                string fileLocation = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Resources\\Template\\SalaryMaintenance\\7_1_19_SalaryAdditionsAndDeductionsInput\\Report.xlsx"
                );
                Aspose.Cells.WorkbookDesigner workbookDesigner = new() { Workbook = new Aspose.Cells.Workbook(fileLocation) };
                Aspose.Cells.Worksheet worksheet = workbookDesigner.Workbook.Worksheets[0];
                workbookDesigner.SetDataSource("result", excelReportList);
                workbookDesigner.Process();
                worksheet.AutoFitColumns(worksheet.Cells.MinDataColumn, worksheet.Cells.MaxColumn);
                worksheet.AutoFitRows(worksheet.Cells.MinDataRow + 1, worksheet.Cells.MaxRow);
                workbookDesigner.Workbook.Save(memoryStream, Aspose.Cells.SaveFormat.Xlsx);
                return new OperationResult { IsSuccess = false, Data = memoryStream.ToArray(), Error = "Please check Error Report" };
            }

            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                _repositoryAccessor.HRMS_Sal_AddDedItem_Monthly.AddMultiple(HSAM_Creates);

                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();

                string path = "uploaded\\SalaryMaintenance\\7_1_19_SalaryAdditionsAndDeductionsInput\\Creates";
                await FilesUtility.SaveFile(param.File, path, $"Salary_Additions_And_Deductions_Input_{now:yyyyMMddHHmmss}");

                return new OperationResult { IsSuccess = true };
            }
            catch (Exception)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult { IsSuccess = false };
            }

        }

        public async Task<List<KeyValuePair<string, string>>> GetListCurrency(string language)
        => await GetDataBasicCode(BasicCodeTypeConstant.Currency, language);
    }
}