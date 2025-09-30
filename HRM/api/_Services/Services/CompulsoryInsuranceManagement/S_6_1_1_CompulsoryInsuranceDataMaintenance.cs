using System.Globalization;
using API.Data;
using API._Services.Interfaces.CompulsoryInsuranceManagement;
using API.DTOs.CompulsoryInsuranceManagement;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.CompulsoryInsuranceManagement
{
    public class S_6_1_1_CompulsoryInsuranceDataMaintenance : BaseServices, I_6_1_1_CompulsoryInsuranceDataMaintenance
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        public S_6_1_1_CompulsoryInsuranceDataMaintenance(DBContext dbContext,IWebHostEnvironment webHostEnvironment) : base(dbContext)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<OperationResult> Create(CompulsoryInsuranceDataMaintenanceDto dto)
        {
            if (await _repositoryAccessor.HRMS_Ins_Emp_Maintain.AnyAsync(x => x.Factory == dto.Factory && x.Employee_ID == dto.Employee_ID
                       && x.Insurance_Type == dto.Insurance_Type && x.Insurance_Start == Convert.ToDateTime(dto.Insurance_Start)))
                return new OperationResult(false, "SalaryMaintenance.BankAccountMaintenance.Duplicates");

            var dataCreate = new HRMS_Ins_Emp_Maintain
            {
                USER_GUID = dto.USER_GUID,
                Factory = dto.Factory,
                Employee_ID = dto.Employee_ID,
                Insurance_Type = dto.Insurance_Type,
                Insurance_Start = Convert.ToDateTime(dto.Insurance_Start),
                Insurance_End = dto.Insurance_End == null ? null : Convert.ToDateTime(dto.Insurance_End),
                Insurance_Num = dto.Insurance_Num,
                Update_Time = Convert.ToDateTime(dto.Update_Time),
                Update_By = dto.Update_By
            };

            try
            {
                _repositoryAccessor.HRMS_Ins_Emp_Maintain.Add(dataCreate);
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.CreateOKMsg");
            }
            catch (System.Exception)
            {
                return new OperationResult(false, "System.Message.CreateErrorMsg");
            }
        }

        public async Task<OperationResult> Delete(CompulsoryInsuranceDataMaintenanceDto dto)
        {
            var item = await _repositoryAccessor.HRMS_Ins_Emp_Maintain.FirstOrDefaultAsync(x => x.Factory == dto.Factory && x.Employee_ID == dto.Employee_ID
                             && x.Insurance_Type == dto.Insurance_Type && x.Insurance_Start == Convert.ToDateTime(dto.Insurance_Start));
            if (item is not null)
                _repositoryAccessor.HRMS_Ins_Emp_Maintain.Remove(item);

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

        public async Task<OperationResult> DownloadFileExcel(CompulsoryInsuranceDataMaintenanceParam param, string userName)
        {
            var data = await GetData(param);
            if (!data.Any())
                return new OperationResult(false, "System.Message.NoData");

            List<Cell> dataCells = new()
            {
                new Cell("B2", userName),
                new Cell("D2", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))
            };

            var index = 6;
            for (int i = 0; i < data.Count; i++)
            {
                dataCells.Add(new Cell("A" + index, data[i].Factory));
                dataCells.Add(new Cell("B" + index, data[i].Employee_ID));
                dataCells.Add(new Cell("C" + index, data[i].Local_Full_Name));
                dataCells.Add(new Cell("D" + index, data[i].Insurance_Type_Name));
                dataCells.Add(new Cell("E" + index, data[i].Insurance_Start));
                dataCells.Add(new Cell("F" + index, data[i].Insurance_End));
                dataCells.Add(new Cell("G" + index, data[i].Insurance_Num));
                dataCells.Add(new Cell("H" + index, data[i].Update_By));
                dataCells.Add(new Cell("I" + index, data[i].Update_Time));
                index += 1;
            }

            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataCells,
                "Resources\\Template\\CompulsoryInsuranceManagement\\6_1_1_CompulsoryInsuranceDataMaintenance\\Download.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);

        }

        public Task<OperationResult> DownloadFileTemplate()
        {
            var path = Path.Combine(
                _webHostEnvironment.ContentRootPath,
                "Resources\\Template\\CompulsoryInsuranceManagement\\6_1_1_CompulsoryInsuranceDataMaintenance\\Upload.xlsx"
            );
            var workbook = new Aspose.Cells.Workbook(path);
            var design = new Aspose.Cells.WorkbookDesigner(workbook);
            MemoryStream stream = new();
            design.Workbook.Save(stream, Aspose.Cells.SaveFormat.Xlsx);
            var result = stream.ToArray();
            return Task.FromResult(new OperationResult(true, null, result));
        }

        public async Task<PaginationUtility<CompulsoryInsuranceDataMaintenanceDto>> GetDataPagination(PaginationParam pagination, CompulsoryInsuranceDataMaintenanceParam param)
        {
            var data = await GetData(param);
            return PaginationUtility<CompulsoryInsuranceDataMaintenanceDto>.Create(data, pagination.PageNumber, pagination.PageSize);
        }

        private async Task<List<CompulsoryInsuranceDataMaintenanceDto>> GetData(CompulsoryInsuranceDataMaintenanceParam param)
        {
            var predicate = PredicateBuilder.New<HRMS_Ins_Emp_Maintain>(true);
            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                predicate.And(x => x.Employee_ID.Contains(param.Employee_ID.Trim()));
            if (!string.IsNullOrWhiteSpace(param.Factory))
                predicate.And(x => x.Factory == param.Factory);
            if (!string.IsNullOrWhiteSpace(param.Insurance_Type))
                predicate.And(x => x.Insurance_Type == param.Insurance_Type);
            if (!string.IsNullOrWhiteSpace(param.Insurance_Start)
                && DateTime.TryParseExact(param.Insurance_Start, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime insuranceStart)
                && !string.IsNullOrWhiteSpace(param.Insurance_End)
                && DateTime.TryParseExact(param.Insurance_End, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime insuranceEnd))
            {
                predicate.And(x => x.Insurance_Start.Date >= insuranceStart.Date
                                && x.Insurance_Start.Date <= insuranceEnd.Date);
            }

            var listInsuranceType = await GetListInsuranceType(param.Language);

            var data = await _repositoryAccessor.HRMS_Ins_Emp_Maintain.FindAll(predicate, true)
                        .GroupJoin(_repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == param.Factory, true),
                            x => x.USER_GUID,
                            y => y.USER_GUID,
                            (x, y) => new { HIEM = x, HEP = y })
                        .SelectMany(x => x.HEP.DefaultIfEmpty(),
                            (x, y) => new { x.HIEM, HEP = y })
                        .Select(x => new CompulsoryInsuranceDataMaintenanceDto
                        {
                            USER_GUID = x.HIEM.USER_GUID,
                            Factory = x.HIEM.Factory,
                            Employee_ID = x.HIEM.Employee_ID,
                            Local_Full_Name = x.HEP != null ? x.HEP.Local_Full_Name : string.Empty,
                            Insurance_Type = x.HIEM.Insurance_Type,
                            Insurance_Start = x.HIEM.Insurance_Start.ToString("yyyy/MM/dd"),
                            Insurance_End = x.HIEM.Insurance_End.HasValue
                                            ? x.HIEM.Insurance_End.Value.ToString("yyyy/MM/dd")
                                            : string.Empty,
                            Insurance_Num = x.HIEM.Insurance_Num,
                            Update_By = x.HIEM.Update_By,
                            Update_Time = x.HIEM.Update_Time.ToString("yyyy/MM/dd HH:mm:ss"),
                        })
                        .ToListAsync();

            var result = data.Select(item =>
           {
               item.Insurance_Type_Name = listInsuranceType.FirstOrDefault(y => y.Key == item.Insurance_Type).Value;
               return item;
           }).ToList();

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

        public async Task<OperationResult> Update(CompulsoryInsuranceDataMaintenanceDto dto)
        {
            var item = await _repositoryAccessor.HRMS_Ins_Emp_Maintain.FirstOrDefaultAsync(x => x.Factory == dto.Factory && x.Employee_ID == dto.Employee_ID
                            && x.Insurance_Type == dto.Insurance_Type && x.Insurance_Start == Convert.ToDateTime(dto.Insurance_Start));
            if (item is not null)
            {

                item.Factory = dto.Factory;
                item.Employee_ID = dto.Employee_ID;
                item.Insurance_Type = dto.Insurance_Type;
                item.Insurance_Start = Convert.ToDateTime(dto.Insurance_Start);
                item.Insurance_End = dto.Insurance_End == null ? null : Convert.ToDateTime(dto.Insurance_End);
                item.Insurance_Num = dto.Insurance_Num;
                item.Update_Time = Convert.ToDateTime(dto.Update_Time);
                item.Update_By = dto.Update_By;

                _repositoryAccessor.HRMS_Ins_Emp_Maintain.Update(item);
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

        public async Task<OperationResult> UploadFileExcel(CompulsoryInsuranceDataMaintenance_Upload param, List<string> roleList, string userName)
        {
            ExcelResult resp = ExcelUtility.CheckExcel(
                param.File,
                "Resources\\Template\\CompulsoryInsuranceManagement\\6_1_1_CompulsoryInsuranceDataMaintenance\\Upload.xlsx"
            );
            if (!resp.IsSuccess)
                return new OperationResult(false, resp.Error);
            List<HRMS_Ins_Emp_Maintain> HIEM_Add = new();
            var allowFactories = await GetListFactory(param.Language, roleList);
            var factoryCodes = allowFactories.Select(x => x.Key).ToHashSet();
            var HEPs = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => factoryCodes.Contains(x.Factory), true)
            .Select(
                x => new
                {
                    x.Factory,
                    x.Employee_ID,
                    x.USER_GUID,
                }
            ).ToListAsync();
            bool isCheck = true;
            List<CompulsoryInsuranceDataMaintenance_Report> excelReportList = new();
            var listInsuranceType = await GetListInsuranceType(param.Language);

            for (int i = resp.WsTemp.Cells.Rows.Count; i < resp.Ws.Cells.Rows.Count; i++)
            {
                string errorMessage = "";
                bool isKeyPassed = true;
                string factory = resp.Ws.Cells[i, 0].StringValue.Trim().ToUpper();
                string employeeID = resp.Ws.Cells[i, 1].StringValue.Trim();
                string insuranceType = resp.Ws.Cells[i, 2].StringValue.Trim();
                string insuranceNum = resp.Ws.Cells[i, 5].StringValue.Trim();

                var factoryData = await _repositoryAccessor.HRMS_Basic_Code
                               .FirstOrDefaultAsync(x => x.Code == factory
                                                      && x.Type_Seq == BasicCodeTypeConstant.Factory);

                if (factoryData == null || factory.Length > 10)
                {
                    errorMessage += $"Factory is invalid \n";
                    isKeyPassed = false;
                }

                if (employeeID == null || factory.Length > 9)
                {
                    errorMessage += $"Employee ID is invalid \n";
                    isKeyPassed = false;
                }

                var itemHEP = HEPs.FirstOrDefault(x => x.Factory == factory && x.Employee_ID == employeeID);
                if (itemHEP is null)
                {
                    errorMessage += $"Factory: {factory} \nEmployee ID : {employeeID} does not exist \n";
                    isKeyPassed = false;
                }

                if (insuranceType == null)
                {
                    errorMessage += $"Insurance Type is invalid \n";
                    isKeyPassed = false;
                }

                var insuranceTypeData = listInsuranceType.FirstOrDefault(x => x.Key == insuranceType);
                if (insuranceTypeData.Equals(default(KeyValuePair<string, string>)))
                {
                    errorMessage += $"Insurance Type does not exist \n";
                    isKeyPassed = false;
                }


                if (!DateTime.TryParse(resp.Ws.Cells[i, 3].StringValue.Trim(), out DateTime insuranceStart))
                {
                    errorMessage += $"Insurance Start date is invalid \n";
                    isKeyPassed = false;
                }

                DateTime? insuranceEnd = null;
                if (!string.IsNullOrEmpty(resp.Ws.Cells[i, 4].StringValue.Trim()))
                {
                    if (DateTime.TryParse(resp.Ws.Cells[i, 4].StringValue.Trim(), out DateTime parsedDate))
                        insuranceEnd = parsedDate;
                    else
                    {
                        errorMessage += $"Insurance End date is invalid \n";
                        isKeyPassed = false;
                    }

                }

                if (insuranceEnd < insuranceStart)
                {
                    errorMessage += $"Insurance End must be greater than Insurance Start.";
                    isKeyPassed = false;
                }

                if (insuranceNum.Length > 20)
                {
                    errorMessage += $"Insurance Num is invalid \n";
                    isKeyPassed = false;
                }

                if (isKeyPassed)
                {
                    if (_repositoryAccessor.HRMS_Ins_Emp_Maintain
                        .Any(x => x.Factory == factory && x.Employee_ID == employeeID
                            && x.Insurance_Type == insuranceType && x.Insurance_Start == insuranceStart))
                        errorMessage += $"Data already exists. \n";
                    if (excelReportList
                        .Any(x => x.Factory == factory && x.Employee_ID == employeeID
                            && x.Insurance_Type == insuranceType && x.Insurance_Start == insuranceStart))
                        errorMessage += $"Data are duplicated. \n";
                }

                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    HRMS_Ins_Emp_Maintain dataAdd = new()
                    {
                        USER_GUID = itemHEP.USER_GUID,
                        Factory = factory,
                        Employee_ID = employeeID,
                        Insurance_Type = insuranceType,
                        Insurance_Start = insuranceStart,
                        Insurance_End = insuranceEnd,
                        Insurance_Num = insuranceNum,
                        Update_By = userName,
                        Update_Time = DateTime.Now
                    };

                    HIEM_Add.Add(dataAdd);
                }
                else
                {
                    isCheck = false;
                    errorMessage = errorMessage.Trim();
                }

                CompulsoryInsuranceDataMaintenance_Report report = new()
                {
                    Factory = factory,
                    Employee_ID = employeeID,
                    Insurance_Type = insuranceType,
                    Insurance_Start = insuranceStart,
                    Insurance_End = insuranceEnd,
                    Insurance_Num = insuranceNum,
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
                    "Resources\\Template\\CompulsoryInsuranceManagement\\6_1_1_CompulsoryInsuranceDataMaintenance\\Report.xlsx"
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
                _repositoryAccessor.HRMS_Ins_Emp_Maintain.AddMultiple(HIEM_Add);

                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();

                string path = "uploaded\\CompulsoryInsuranceManagement\\6_1_1_CompulsoryInsuranceDataMaintenance\\Creates";
                await FilesUtility.SaveFile(param.File, path, $"Compulsory_Insurance_Data_Maintenance_{DateTime.Now:yyyyMMddHHmmss}");

                return new OperationResult { IsSuccess = true };
            }
            catch (Exception e)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult { IsSuccess = false, Error = e.ToString() };
            }
        }

        public async Task<List<KeyValuePair<string, string>>> GetListInsuranceType(string language)
        {
            var data = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.InsuranceType, true)
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
    }
}