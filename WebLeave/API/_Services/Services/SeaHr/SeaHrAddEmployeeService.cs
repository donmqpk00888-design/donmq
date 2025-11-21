using System.Globalization;
using API._Repositories;
using API._Services.Interfaces.Common;
using API._Services.Interfaces.SeaHr;
using API.Dtos.SeaHr;
using API.Helpers.Utilities;
using API.Models;
using Microsoft.EntityFrameworkCore;
namespace API._Services.Services.SeaHr
{
    public class SeaHrAddEmployeeService : ISeaHrAddEmployeeService
    {
        private static readonly SemaphoreSlim semaphore = new(1, 1);
        private readonly IRepositoryAccessor _repositoryAccessor;
        private readonly IFunctionUtility _functionUtility;
        private readonly ICommonService _commonService;

        public SeaHrAddEmployeeService(
            IRepositoryAccessor repositoryAccessor,
            IFunctionUtility functionUtility,
            ICommonService commonService)
        {
            _repositoryAccessor = repositoryAccessor;
            _functionUtility = functionUtility;
            _commonService = commonService;
        }
        public async Task<bool> IsExists(EmployeeDTO employeeDTO)
        {
            return await _repositoryAccessor.Employee.AnyAsync(x => x.EmpNumber == employeeDTO.EmpNumber);
        }

        public async Task<OperationResult> AddNewEmployee(EmployeeDTO employeeDTO)
        {
            if (await IsExists(employeeDTO))
                return new OperationResult(false, "employee already exists.");

            DateTime.TryParseExact(employeeDTO.DateIn, "dd/MM/yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dateIn);
            Employee employee = new()
            {
                EmpName = employeeDTO.EmpName,
                EmpNumber = employeeDTO.EmpNumber,
                DateIn = dateIn,
                PartID = employeeDTO.PartID,
                PositionID = employeeDTO.PositionID,
                GBID = employeeDTO.GBID,
                Visible = true,
                IsSun = false,
            };
            _repositoryAccessor.Employee.Add(employee);
            await _repositoryAccessor.SaveChangesAsync();

            HistoryEmp history = new();
            double TotalDay = Convert.ToDouble(employeeDTO.TotalDay);
            var _serverTime = _commonService.GetServerTime();

            history.EmpID = employee.EmpID;
            history.TotalDay = TotalDay;
            history.Agent = TotalDay / 2;
            history.Arrange = TotalDay / 2;
            history.YearIn = _serverTime.Year;
            history.CountAgent = employeeDTO.CountAgent;
            history.CountArran = employeeDTO.CountArran;
            history.CountLeave = employeeDTO.CountLeave;
            history.CountRestAgent = employeeDTO.CountRestAgent;
            history.CountRestArran = employeeDTO.CountRestArran;
            history.CountLeave = employeeDTO.CountTotal;
            history.CountTotal = employeeDTO.CountAgent + employeeDTO.CountRestAgent;
            history.Updated = _serverTime;
            _repositoryAccessor.HistoryEmp.Add(history);

            Users user = await _repositoryAccessor.Users.FirstOrDefaultAsync(x => x.UserName == employeeDTO.EmpNumber);
            if (user == null && employeeDTO.IsCreateAccount == true)
            {
                Users userItem = new()
                {
                    UserName = employeeDTO.EmpNumber,
                    HashPass = _functionUtility.HashPasswordUser(SettingsConfigUtility.GetCurrentSettings("AppSettings:Factory").ToLower() + "@1234"),
                    UserRank = 1,
                    ISPermitted = true,
                    EmpID = employee.EmpID,
                    Updated = _serverTime,
                    Visible = true,
                    FullName = _functionUtility.RemoveUnicode(employeeDTO.EmpName)
                };
                _repositoryAccessor.Users.Add(userItem);
            }
            try
            {
                await _repositoryAccessor.SaveChangesAsync();
                return new OperationResult(true, "Add employee was Succeeded .");
            }
            catch (Exception ex)
            {
                return new OperationResult(false, "Employee Member failed on save.", ex.ToString());
            }
        }

        public async Task<List<KeyValuePair<int, string>>> GetListDepartment()
        {
            List<KeyValuePair<int, string>> data = await _repositoryAccessor.Department.FindAll(x => x.Visible == true)
                .Select(x => new KeyValuePair<int, string>(x.DeptID, x.DeptCode + "-" + x.DeptName)).ToListAsync();
            return data;
        }

        public async Task<List<ListGroupBaseDTO>> GetListGroupBase()
        {
            List<ListGroupBaseDTO> data = await _repositoryAccessor.GroupBase.FindAll()
                .Include(x => x.GroupLangs)
                .SelectMany(x => x.GroupLangs, (x, groupLang) => new ListGroupBaseDTO
                {
                    GBID = x.GBID,
                    BaseName = groupLang.BaseName,
                    LanguageID = groupLang.LanguageID
                }).ToListAsync();
            return data;
        }

        public async Task<List<KeyValuePair<int, string>>> GetListPart(int departmentID)
        {
            List<KeyValuePair<int, string>> data = await _repositoryAccessor.Part.FindAll(x => x.Visible == true && x.DeptID == departmentID)
                .Select(x => new KeyValuePair<int, string>(x.PartID, x.PartName)).ToListAsync();
            return data;
        }

        public async Task<List<ListPositionDTO>> GetListPosition()
        {
            List<ListPositionDTO> data = await _repositoryAccessor.Position.FindAll()
                .Include(x => x.PosLangs)
                .SelectMany(x => x.PosLangs, (x, posLang) => new ListPositionDTO
                {
                    PositionID = x.PositionID,
                    PositionName = posLang.PositionName,
                    LanguageID = posLang.LanguageID
                }).ToListAsync();
            return data;
        }

        public async Task<OperationResult> UploadExcel(IFormFile file)
        {
            await semaphore.WaitAsync();
            try
            {
                ExcelResult excelResult = ExcelUtility.CheckExcel(file, "Resources\\Template\\SeaHr\\AddListEmployee.xlsx");
                if (!excelResult.IsSuccess)
                    return new OperationResult(false, excelResult.Error);
                string factory = SettingsConfigUtility.GetCurrentSettings("AppSettings:Factory").ToLower();
                DateTime now = _commonService.GetServerTime();
                ResultDataUploadEmp dataResult = new()
                {
                    CountCreateEmp = 0,
                    CountUpdateEmp = 0,
                    TotalEmp = excelResult.ws.Cells.Rows.Count - excelResult.wsTemp.Cells.Rows.Count,
                    Ignore = ""
                };
                for (int i = excelResult.wsTemp.Cells.Rows.Count; i < excelResult.ws.Cells.Rows.Count; i++)
                {
                    //Check imput values
                    var EmpNumber = excelResult.ws.Cells[i, 1].StringValue.Trim();
                    if (string.IsNullOrWhiteSpace(EmpNumber))
                        continue;
                    var PartCode = excelResult.ws.Cells[i, 0].StringValue.Trim();
                    var PositionSym = excelResult.ws.Cells[i, 5].StringValue.Trim();
                    var BaseSym = excelResult.ws.Cells[i, 6].StringValue.Trim();
                    var Part = await _repositoryAccessor.Part.FirstOrDefaultAsync(x => x.PartCode == PartCode);
                    var Position = await _repositoryAccessor.Position.FirstOrDefaultAsync(x => x.PositionSym == PositionSym);
                    var GroupBase = await _repositoryAccessor.GroupBase.FirstOrDefaultAsync(x => x.BaseSym == BaseSym);
                    if (Part == null || Position == null || GroupBase == null)
                    {
                        dataResult.Ignore += $"_{EmpNumber}";
                        continue;
                    }
                    //Lấy giá trị của cell từ cột đầu tiên đến cột cuối cùng
                    AddHistoryEmp data = new()
                    {
                        Factory = factory,
                        EmpNumber = EmpNumber,
                        Leave = double.Parse(excelResult.ws.Cells[i, 7].Value.ToString().Replace(",", ".")),
                        CountArrange = double.Parse(excelResult.ws.Cells[i, 8].Value.ToString().Replace(",", ".")),
                        CountRestArrange = double.Parse(excelResult.ws.Cells[i, 9].Value.ToString().Replace(",", ".")),
                        CountAgent = double.Parse(excelResult.ws.Cells[i, 10].Value.ToString().Replace(",", ".")),
                        CountRestAgent = double.Parse(excelResult.ws.Cells[i, 11].Value.ToString().Replace(",", ".")),
                        CountLeave = double.Parse(excelResult.ws.Cells[i, 12].Value.ToString().Replace(",", ".")),
                        Year = int.Parse(excelResult.ws.Cells[i, 15].Value.ToString()),
                        EmpName = excelResult.ws.Cells[i, 2].StringValue.Trim(),
                        Email = excelResult.ws.Cells[i, 3].StringValue.Trim(),
                        DateIn = Convert.ToDateTime(excelResult.ws.Cells[i, 4].Value.ToString()),
                        Disable = int.Parse(excelResult.ws.Cells[i, 13].Value.ToString()),
                        CreateUser = int.Parse(excelResult.ws.Cells[i, 14].Value.ToString()),
                        PartID = Part.PartID,
                        PositionID = Position.PositionID,
                        GBID = GroupBase.GBID,
                        Updated = now
                    };
                    (data.Arrange, data.Agent) = ArrangeAgent(data.Leave);
                    var empResult = await _repositoryAccessor.Employee.AnyAsync(x => x.EmpNumber == data.EmpNumber)
                        ? await OldEmployeeAsync(data)
                        : await NewEmployeeAsync(data);
                    if (empResult.IsSuccess)
                        if (empResult.Error == "New") dataResult.CountCreateEmp++; else dataResult.CountUpdateEmp++;
                    else
                        dataResult.Ignore += $"_{data.EmpNumber}";
                }
                dataResult.Ignore = string.IsNullOrEmpty(dataResult.Ignore) ? "" : dataResult.Ignore.Remove(0, 1);
                await _functionUtility.SaveFile(file, "uploaded/excels", $"Upload_AddListEmployee_{now:yyyyMMddHHmmss}");
                return new OperationResult(true, "File was uploaded.", dataResult);
            }
            catch (Exception)
            {
                return new OperationResult(false, "Uploading file failed on save. Please check the excel data again", "Error!");
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<OperationResult> NewEmployeeAsync(AddHistoryEmp data)
        {
            using var _transaction = await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                //Thêm nhân viên
                Employee employeeItem = new()
                {
                    EmpNumber = data.EmpNumber,
                    EmpName = data.EmpName,
                    PartID = data.PartID,
                    PositionID = data.PositionID,
                    GBID = data.GBID,
                    DateIn = data.DateIn,
                    Visible = true,
                    IsSun = false
                };
                _repositoryAccessor.Employee.Add(employeeItem);
                await _repositoryAccessor.SaveChangesAsync();
                data.EmpID = employeeItem.EmpID;

                //Thêm phép của nhân viên mới vào bảng history
                var history = new HistoryEmp
                {
                    EmpID = data.EmpID,
                    TotalDay = data.Leave,
                    YearIn = data.Year,
                    Arrange = data.Arrange,
                    Agent = data.Agent,
                    CountArran = data.CountArrange,
                    CountRestArran = data.CountRestArrange,
                    CountAgent = data.CountAgent,
                    CountRestAgent = data.CountRestAgent,
                    CountLeave = data.CountLeave,
                    CountTotal = data.CountArrange + data.CountAgent,
                    Updated = data.Updated
                };
                _repositoryAccessor.HistoryEmp.Add(history);
                await _repositoryAccessor.SaveChangesAsync();
                if (data.CreateUser == 1)
                {
                    var userResult = await UserAsync(data);
                    if (!userResult.IsSuccess)
                    {
                        await _transaction.RollbackAsync();
                        return new OperationResult(false);
                    }
                }
                await _transaction.CommitAsync();
                return new OperationResult(true, "New");
            }
            catch (Exception)
            {
                await _transaction.RollbackAsync();
                return new OperationResult(false);
            }
        }
        private async Task<OperationResult> OldEmployeeAsync(AddHistoryEmp data)
        {
            using var _transaction = await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                Employee emp = await _repositoryAccessor.Employee.FirstOrDefaultAsync(x => x.EmpNumber == data.EmpNumber);
                emp.EmpName = data.EmpName;
                emp.PartID = data.PartID;
                emp.PositionID = data.PositionID;
                emp.GBID = data.GBID;
                emp.DateIn = Convert.ToDateTime(data.DateIn);
                emp.Visible = data.Disable == 1; //Người lao động còn làm hay đã nghỉ 

                _repositoryAccessor.Employee.Update(emp);
                await _repositoryAccessor.SaveChangesAsync();
                data.EmpID = emp.EmpID;

                //Lịch sử nhân viên
                HistoryEmp hisEmp = await _repositoryAccessor.HistoryEmp
                    .FirstOrDefaultAsync(x => x.EmpID == data.EmpID && x.YearIn == data.Year);
                //Nếu mà trong hisEmp mà có là update lại cũ, ngược lại là up dữ liệu phép năm của năm mới
                if (hisEmp != null)
                {
                    hisEmp.TotalDay = data.Leave;
                    hisEmp.YearIn = data.Year;
                    hisEmp.Arrange = data.Arrange;
                    hisEmp.Agent = data.Agent;
                    hisEmp.CountArran = data.CountArrange;
                    hisEmp.CountRestArran = data.CountRestArrange;
                    hisEmp.CountAgent = data.CountAgent;
                    hisEmp.CountRestAgent = data.CountRestAgent;
                    hisEmp.CountLeave = data.CountLeave;
                    hisEmp.CountTotal = data.CountArrange + data.CountAgent;
                    hisEmp.Updated = data.Updated;
                    _repositoryAccessor.HistoryEmp.Update(hisEmp);
                    await _repositoryAccessor.SaveChangesAsync();
                }
                else
                {
                    //Lịch sử của nhân viên cũ
                    var history = new HistoryEmp
                    {
                        EmpID = data.EmpID,
                        TotalDay = data.Leave,
                        YearIn = data.Year,
                        Arrange = data.Arrange,
                        Agent = data.Agent,
                        CountArran = data.CountArrange,
                        CountRestArran = data.CountRestArrange,
                        CountAgent = data.CountAgent,
                        CountRestAgent = data.CountRestAgent,
                        CountLeave = data.CountLeave,
                        CountTotal = data.CountArrange + data.CountAgent,
                        Updated = data.Updated
                    };
                    _repositoryAccessor.HistoryEmp.Add(history);
                    await _repositoryAccessor.SaveChangesAsync();
                }
                if (data.CreateUser == 1)
                {
                    var userResult = await UserAsync(data);
                    if (!userResult.IsSuccess)
                    {
                        await _transaction.RollbackAsync();
                        return new OperationResult(false);
                    }
                }

                await _transaction.CommitAsync();
                return new OperationResult(true, "Old");
            }
            catch (Exception)
            {
                await _transaction.RollbackAsync();
                return new OperationResult(false);
            }
        }
        private async Task<OperationResult> UserAsync(AddHistoryEmp data)
        {
            try
            {
                Users userCheck = await _repositoryAccessor.Users.FirstOrDefaultAsync(x => x.UserName == data.EmpNumber);
                if (userCheck != null)
                {
                    userCheck.EmpID = data.EmpID;
                    userCheck.UserName = data.EmpNumber;
                    userCheck.FullName = _functionUtility.RemoveUnicode(data.EmpName);
                    userCheck.EmailAddress = data.Email;
                    userCheck.Visible = data.Disable == 1;
                    userCheck.Updated = data.Updated;
                    _repositoryAccessor.Users.Update(userCheck);
                }
                else
                {
                    Users userItem = new()
                    {
                        EmpID = data.EmpID,
                        UserName = data.EmpNumber,
                        FullName = _functionUtility.RemoveUnicode(data.EmpName),
                        HashPass = _functionUtility.HashPasswordUser(data.Factory + "@1234"),
                        EmailAddress = data.Email,
                        ISPermitted = true,
                        UserRank = 1,
                        Visible = data.Disable == 1,
                        Updated = data.Updated
                    };
                    _repositoryAccessor.Users.Add(userItem);
                }
                return new OperationResult(await _repositoryAccessor.SaveChangesAsync());
            }
            catch
            {
                return new OperationResult(false);
            }
        }
        private static (double Arrange, double Agent) ArrangeAgent(double leave)
        {
            double arrange, agent;

            if (leave >= 12)
            {
                arrange = 6;
                agent = leave - 6;
            }
            else
            {
                if (leave % 2 != 0)
                {
                    arrange = leave / 2 - 0.5;
                    agent = leave / 2 + 0.5;
                }
                else
                {
                    arrange = leave / 2;
                    agent = leave / 2;
                }
            }

            return (arrange, agent);
        }
    }
}