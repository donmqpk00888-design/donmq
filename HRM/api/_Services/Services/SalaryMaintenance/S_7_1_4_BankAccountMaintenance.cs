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
    public class S_7_1_4_BankAccountMaintenance : BaseServices, I_7_1_4_BankAccountMaintenance
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        public S_7_1_4_BankAccountMaintenance(DBContext dbContext,IWebHostEnvironment webHostEnvironment) : base(dbContext)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<OperationResult> Create(BankAccountMaintenanceDto dto)
        {

            if (await _repositoryAccessor.HRMS_Sal_Bank_Account.AnyAsync(x => x.Factory == dto.Factory
                                                                        && x.Employee_ID == dto.Employee_ID))
                return new OperationResult(false, "SalaryMaintenance.BankAccountMaintenance.Duplicates");

            var dataCreate = new HRMS_Sal_Bank_Account
            {
                USER_GUID = dto.USER_GUID,
                Factory = dto.Factory,
                Employee_ID = dto.Employee_ID,
                BankNo = dto.BankNo,
                Bank_Code = dto.Bank_Code,
                Create_Date = Convert.ToDateTime(dto.Create_Date),
                Update_By = dto.Update_By,
                Update_Time = DateTime.Now
            };

            try
            {
                _repositoryAccessor.HRMS_Sal_Bank_Account.Add(dataCreate);
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.CreateOKMsg");
            }
            catch (System.Exception)
            {
                return new OperationResult(false, "System.Message.CreateErrorMsg");
            }
        }

        public async Task<OperationResult> Delete(BankAccountMaintenanceDto dto)
        {
            var item = await _repositoryAccessor.HRMS_Sal_Bank_Account.FirstOrDefaultAsync(x => x.Factory == dto.Factory && x.Employee_ID == dto.Employee_ID);
            if (item is not null)
                _repositoryAccessor.HRMS_Sal_Bank_Account.Remove(item);

            try
            {
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.DeleteOKMsg");
            }
            catch (System.Exception)
            {
                return new OperationResult(false, "System.Message.DeleteErrorMsg");
            }
        }

        public async Task<PaginationUtility<BankAccountMaintenanceDto>> GetDataPagination(PaginationParam pagination, BankAccountMaintenanceParam param)
        {
            var data = await GetData(param);
            return PaginationUtility<BankAccountMaintenanceDto>.Create(data, pagination.PageNumber, pagination.PageSize);
        }

        private async Task<List<BankAccountMaintenanceDto>> GetData(BankAccountMaintenanceParam param)
        {
            var predicate = PredicateBuilder.New<HRMS_Sal_Bank_Account>(x => x.Factory == param.Factory);
            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                predicate.And(x => x.Employee_ID.Contains(param.Employee_ID.Trim()));
            var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll();
            var HSBA = _repositoryAccessor.HRMS_Sal_Bank_Account.FindAll(predicate, true).OrderBy(x => x.Employee_ID);
            var data = await HSBA
                .GroupJoin(HEP,
                    x => x.USER_GUID,
                    y => y.USER_GUID,
                    (x, y) => new { HSBA = x, HEP = y })
                .SelectMany(x => x.HEP.DefaultIfEmpty(),
                    (x, y) => new { x.HSBA, HEP = y })
                .Select(x => new BankAccountMaintenanceDto
                {
                    USER_GUID = x.HSBA.USER_GUID,
                    Factory = x.HSBA.Factory,
                    Employee_ID = x.HSBA.Employee_ID,
                    Local_Full_Name = x.HEP.Local_Full_Name,
                    BankNo = x.HSBA.BankNo,
                    Bank_Code = x.HSBA.Bank_Code,
                    Create_Date = x.HSBA.Create_Date.ToString("yyyy/MM/dd"),
                    Update_By = x.HSBA.Update_By,
                    Update_Time = x.HSBA.Update_Time.ToString("yyyy/MM/dd HH:mm:ss"),
                }).ToListAsync();
            return data;
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

        public async Task<OperationResult> Update(BankAccountMaintenanceDto dto)
        {
            var item = await _repositoryAccessor.HRMS_Sal_Bank_Account.FirstOrDefaultAsync(x => x.Factory == dto.Factory && x.Employee_ID == dto.Employee_ID);
            if (item is not null)
            {
                item = Mapper.Map(dto).Over(item);
                item.Update_Time = DateTime.Now;
                item.Update_By = dto.Update_By;
                item.BankNo = dto.BankNo;
                item.Create_Date = Convert.ToDateTime(dto.Create_Date);

                _repositoryAccessor.HRMS_Sal_Bank_Account.Update(item);
            }

            try
            {
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.UpdateOKMsg");
            }
            catch (System.Exception)
            {
                return new OperationResult(false, "System.Message.UpdateErrorMsg");
            }
        }

        public async Task<OperationResult> DownloadFileExcel(BankAccountMaintenanceParam param, string userName)
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
                dataCells.Add(new Cell("A" + index, data[i].Factory));
                dataCells.Add(new Cell("B" + index, data[i].Employee_ID));
                dataCells.Add(new Cell("C" + index, data[i].Local_Full_Name));
                dataCells.Add(new Cell("D" + index, data[i].Bank_Code));

                dataCells.Add(new Cell("E" + index, data[i].BankNo));
                dataCells.Add(new Cell("F" + index, data[i].Create_Date));
                dataCells.Add(new Cell("G" + index, data[i].Update_By));
                dataCells.Add(new Cell("H" + index, data[i].Update_Time));
                index += 1;
            }

            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataCells,
                "Resources\\Template\\SalaryMaintenance\\7_1_4_BankAccountMaintenance\\Download.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }

        public Task<OperationResult> DownloadFileTemplate()
        {
            var path = Path.Combine(
                _webHostEnvironment.ContentRootPath,
                "Resources\\Template\\SalaryMaintenance\\7_1_4_BankAccountMaintenance\\Template.xlsx"
            );
            var workbook = new Aspose.Cells.Workbook(path);
            var design = new Aspose.Cells.WorkbookDesigner(workbook);
            MemoryStream stream = new();
            design.Workbook.Save(stream, Aspose.Cells.SaveFormat.Xlsx);
            var result = stream.ToArray();
            return Task.FromResult(new OperationResult(true, null, result));
        }

        public async Task<OperationResult> UploadFileExcel(BankAccountMaintenanceUpload param, List<string> roleList, string userName)
        {
            ExcelResult resp = ExcelUtility.CheckExcel(
                param.File,
                "Resources\\Template\\SalaryMaintenance\\7_1_4_BankAccountMaintenance\\Template.xlsx"
            );
            if (!resp.IsSuccess)
                return new OperationResult(false, resp.Error);
            List<HRMS_Sal_Bank_Account> HSBA_Creates = new();
            List<BankAccountMaintenanceReport> excelReportList = new();
            string errorMessage = "";
            bool isCheck = true;
            var HEPs = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(true)
            .Select(
                x => new
                {
                    x.Factory,
                    x.Employee_ID,
                    x.USER_GUID,
                }
            ).ToListAsync();

            var allowFactories = await GetListFactory(param.Language, roleList);
            var factoryCodes = allowFactories.Select(x => x.Key).ToHashSet();

            for (int i = resp.WsTemp.Cells.Rows.Count; i < resp.Ws.Cells.Rows.Count; i++)
            {

                var factoryData = await _repositoryAccessor.HRMS_Basic_Code
                               .FirstOrDefaultAsync(x => x.Code == resp.Ws.Cells[i, 0].StringValue
                                                      && x.Type_Seq == BasicCodeTypeConstant.Factory);
                string factory = resp.Ws.Cells[i, 0].StringValue.Trim();
                string employeeID = resp.Ws.Cells[i, 1].StringValue.Trim();
                if (!factoryCodes.Contains(factory))
                    errorMessage += $"Factory '{factory}' is not valid for user {userName}.\n";

                if (factoryData == null || factory.Length > 10)
                    errorMessage += $"Factory in row {i + 1} invalid\n";

                if (employeeID == null || employeeID.Length > 16)
                    errorMessage += $"Employee ID in row {i + 1} invalid\n";
                if (resp.Ws.Cells[i, 2].Value == null || resp.Ws.Cells[i, 2].StringValue.Length > 10)
                    errorMessage += $"Bank Code in row {i + 1} invalid\n";


                if (resp.Ws.Cells[i, 3].Value == null || resp.Ws.Cells[i, 3].StringValue.Length > 20)
                    errorMessage += $"Bank No in row {i + 1} invalid\n";

                if (string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 4].StringValue) || (!string.IsNullOrWhiteSpace(resp.Ws.Cells[i, 4].StringValue) && resp.Ws.Cells[i, 4].Value is not DateTime))
                    errorMessage += $"Create Date in row {i + 1} invalid\n";

                var existingData = await _repositoryAccessor.HRMS_Sal_Bank_Account.AnyAsync(x => x.Factory == factory &&
                                                                                                x.Employee_ID == employeeID);
                if (existingData)
                    errorMessage += $"Data already exists.\n";

                var itemHEP = HEPs.FirstOrDefault(x => x.Factory == factory && x.Employee_ID == employeeID);
                if (itemHEP is null)
                    errorMessage += $"Factory: {factory} \nEmployee ID : {employeeID} does not exist\n";

                if (HSBA_Creates.Any(x => x.Factory == factory && x.Employee_ID == employeeID)
                    || excelReportList.Any(x => x.Factory == factory && x.Employee_ID == employeeID))
                    errorMessage += $"Data are duplicated.\n";

                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    HRMS_Sal_Bank_Account dataCreates = new()
                    {
                        USER_GUID = itemHEP.USER_GUID,
                        Factory = factory,
                        Employee_ID = employeeID,
                        Bank_Code = resp.Ws.Cells[i, 2].StringValue?.Trim(),
                        BankNo = resp.Ws.Cells[i, 3].StringValue?.Trim(),
                        Create_Date = Convert.ToDateTime(resp.Ws.Cells[i, 4].StringValue.Trim()),
                        Update_By = userName,
                        Update_Time = DateTime.Now
                    };

                    HSBA_Creates.Add(dataCreates);
                }
                else
                {
                    isCheck = false;
                    errorMessage = errorMessage.Trim();
                }

                BankAccountMaintenanceReport report = new()
                {
                    Factory = factory,
                    Employee_ID = employeeID,
                    Bank_Code = resp.Ws.Cells[i, 2].StringValue?.Trim(),
                    BankNo = resp.Ws.Cells[i, 3].StringValue?.Trim(),
                    Create_Date_Str = resp.Ws.Cells[i, 4].StringValue.Trim(),
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
                    "Resources\\Template\\SalaryMaintenance\\7_1_4_BankAccountMaintenance\\Report.xlsx"
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
                _repositoryAccessor.HRMS_Sal_Bank_Account.AddMultiple(HSBA_Creates);

                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();

                string path = "uploaded\\SalaryMaintenance\\7_1_4_BankAccountMaintenance\\Creates";
                await FilesUtility.SaveFile(param.File, path, $"Overtime_Parameter_Setting_{DateTime.Now:yyyyMMddHHmmss}");

                return new OperationResult { IsSuccess = true };
            }
            catch (Exception e)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult { IsSuccess = false, Error = e.ToString() };
            }
        }
    }
}