using API._Repositories;
using API._Services.Interfaces.Common;
using API._Services.Interfaces.SeaHr;
using API.Data;
using API.Dtos.SeaHr;
using API.Helpers.Utilities;
using API.Models;
using Aspose.Cells;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SeaHr
{
    public class HrDeleteEmployeeService : IHrDeleteEmployeeService
    {
        private static readonly SemaphoreSlim semaphore = new(1, 1);
        private readonly IRepositoryAccessor _repositoryAccessor;
        private readonly IFunctionUtility _functionUtility;
        private readonly ICommonService _commonService;
        private const string SUCCESS = "Success";
        private const string FAILED = "Failed";
        private const string NOTEXIST = "Not Exist";

        public HrDeleteEmployeeService(
            IRepositoryAccessor repositoryAccessor,
            ICommonService commonService,
            IFunctionUtility functionUtility)
        {
            _repositoryAccessor = repositoryAccessor;
            _commonService = commonService;
            _functionUtility = functionUtility;
        }

        public async Task<OperationResult> UploadExcelDelete(IFormFile file)
        {
            await semaphore.WaitAsync();
            try
            {
                ExcelResult excelResult = ExcelUtility.CheckExcel(file, "Resources\\Template\\SeaHr\\ListDelete.xlsx");
                if (!excelResult.IsSuccess)
                    return new OperationResult(false, excelResult.Error);
                DateTime now = _commonService.GetServerTime();
                List<HrDeleteEmployeeExcel> dataList = new();
                for (int i = excelResult.wsTemp.Cells.Rows.Count; i < excelResult.ws.Cells.Rows.Count; i++)
                {
                    // Lấy dữ liệu excel theo từng cột
                    HrDeleteEmployeeExcel data = new()
                    {
                        EmpNumber = excelResult.ws.Cells[i, 0].StringValue,
                        FullName = excelResult.ws.Cells[i, 1].StringValue,
                        Message = NOTEXIST
                    };
                    try
                    {
                        if (string.IsNullOrWhiteSpace(data.EmpNumber)) continue;
                        Employee employee = _repositoryAccessor.Employee.FirstOrDefault(x => x.EmpNumber == data.EmpNumber);
                        if (employee == null) continue;
                        await _repositoryAccessor.LeaveData.FindAll(x => x.EmpID == employee.EmpID).ExecuteDeleteAsync();
                        await _repositoryAccessor.HistoryEmp.FindAll(x => x.EmpID == employee.EmpID).ExecuteDeleteAsync();
                        await _repositoryAccessor.ReportData.FindAll(x => x.EmpID == employee.EmpID).ExecuteDeleteAsync();
                        //User related data
                        Users user = _repositoryAccessor.Users.FirstOrDefault(i => i.EmpID == employee.EmpID);
                        if (user != null)
                        {
                            await _repositoryAccessor.RolesUser.FindAll(x => x.UserID == user.UserID).ExecuteDeleteAsync();
                            await _repositoryAccessor.SetApproveGroupBase.FindAll(x => x.UserID == user.UserID).ExecuteDeleteAsync();
                            await _repositoryAccessor.LeaveData.FindAll(x => x.UserID == user.UserID || x.ApprovedBy == user.UserID)
                                .ExecuteUpdateAsync(setters => setters
                                    .SetProperty(x => x.UserID, (int?)null)
                                    .SetProperty(x => x.ApprovedBy, (int?)null)
                                    .SetProperty(x => x.Updated, now)
                                );
                            await _repositoryAccessor.Users.FindAll(i => i.EmpID == employee.EmpID).ExecuteDeleteAsync();
                        }
                        await _repositoryAccessor.Employee.FindAll(x => x.EmpNumber == data.EmpNumber).ExecuteDeleteAsync();
                        data.Message = SUCCESS;
                    }
                    catch (Exception)
                    {
                        data.Message = FAILED;
                    }
                    finally
                    {
                        dataList.Add(data);
                    }
                }
                OperationResult result = new(false);
                if (dataList.Count == 0)
                {
                    result.Error = "EmptyList";
                    return result;
                }
                var failedList = dataList.FindAll(x => x.Message != SUCCESS);
                if (failedList.Count > 0)
                {
                    MemoryStream memoryStream = new();
                    string fileLocation = Path.Combine(Directory.GetCurrentDirectory(), "Resources\\Template\\SeaHr\\ListDelete_Report.xlsx");
                    WorkbookDesigner workbookDesigner = new() { Workbook = new Workbook(fileLocation) };
                    Worksheet worksheet = workbookDesigner.Workbook.Worksheets[0];
                    workbookDesigner.SetDataSource("result", failedList);
                    workbookDesigner.Process();
                    worksheet.AutoFitColumns(worksheet.Cells.MinDataColumn, worksheet.Cells.MaxColumn);
                    worksheet.AutoFitRows(worksheet.Cells.MinDataRow + 1, worksheet.Cells.MaxRow);
                    workbookDesigner.Workbook.Save(memoryStream, SaveFormat.Xlsx);
                    result.Error = "WithReport";
                    result.Data = new { file = memoryStream.ToArray(), name = $"SeaHRDelete_Report_{now:yyyyMMddHHmmss}" };
                }
                if (dataList.Count == failedList.Count)
                {
                    result.Error = $"DeletedFailed{result.Error}";
                    return result;
                }
                await _functionUtility.SaveFile(file, "uploaded/excels", $"ListDelete_{now:yyyyMMddHHmmss}");
                result.Error = $"DeletedSuccessfully{result.Error}";
                result.IsSuccess = true;
                return result;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}