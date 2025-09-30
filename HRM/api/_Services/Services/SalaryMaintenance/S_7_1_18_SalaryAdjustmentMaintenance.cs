using System.Drawing;
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
    public class S_7_1_18_SalaryAdjustmentMaintenance : BaseServices, I_7_1_18_SalaryAdjustmentMaintenance
    {
        private static readonly SemaphoreSlim semaphore = new(1, 1);
        private readonly IWebHostEnvironment _webHostEnvironment;

        public S_7_1_18_SalaryAdjustmentMaintenance(DBContext dbContext,IWebHostEnvironment webHostEnvironment) : base(dbContext)
        {
            _webHostEnvironment = webHostEnvironment;
        }
        #region Create
        public async Task<OperationResult> Create(SalaryAdjustmentMaintenanceMain data)
        {
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrWhiteSpace(data.Employee_ID)
                         || string.IsNullOrWhiteSpace(data.Reason_For_Change)
                         || !DateTime.TryParseExact(data.Effective_Date, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime effectiveDateValue)
                         || string.IsNullOrWhiteSpace(data.After.Department)
                         || string.IsNullOrWhiteSpace(data.After.Position_Title)
                         || string.IsNullOrWhiteSpace(data.After.Permission_Group)
                         || string.IsNullOrWhiteSpace(data.Currency)
                         || string.IsNullOrWhiteSpace(data.After.Salary_Type)
                         || string.IsNullOrWhiteSpace(data.After.Technical_Type)
                         || string.IsNullOrWhiteSpace(data.After.Expertise_Category))
                    return new OperationResult(false, "InvalidInput");

                DateTime now = DateTime.Now;
                var History_GUID = Guid.NewGuid().ToString();
                var checkDataHistory = await _repositoryAccessor.HRMS_Sal_History.FindAll(x => x.Employee_ID == data.Employee_ID
                    && x.Effective_Date == Convert.ToDateTime(data.Effective_Date) && x.Seq == data.Seq)
                    .ToListAsync();
                if (checkDataHistory.Any())
                    return new OperationResult(false, "AlreadyExitedData");

                var salMaster = await _repositoryAccessor.HRMS_Sal_Master.FirstOrDefaultAsync(x => x.Factory == data.Factory && x.Employee_ID == data.Employee_ID);
                var salMasterDetail = await _repositoryAccessor.HRMS_Sal_Master_Detail.FindAll(x => x.Factory == data.Factory && x.Employee_ID == data.Employee_ID).ToListAsync();

                // Probation Salary Month
                if (!string.IsNullOrWhiteSpace(data.Probation_Salary_Month))
                {
                    if (!DateTime.TryParseExact(data.Probation_Salary_Month, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime salMonth))
                        return new OperationResult(false, "Invalid Year-Month");

                    var probationOldData = await _repositoryAccessor.HRMS_Sal_Probation_MasterBackup
                                        .FirstOrDefaultAsync(x => x.Factory == data.Factory && x.Employee_ID == data.Employee_ID && x.Sal_Month == salMonth);

                    var probationDetailOldData = await _repositoryAccessor.HRMS_Sal_Probation_MasterBackup_Detail
                                        .FindAll(x => x.Factory == data.Factory && x.Employee_ID == data.Employee_ID && x.Sal_Month == salMonth)
                                        .ToListAsync();

                    if (probationOldData != null)
                    {
                        _repositoryAccessor.HRMS_Sal_Probation_MasterBackup_Detail.RemoveMultiple(probationDetailOldData);
                        _repositoryAccessor.HRMS_Sal_Probation_MasterBackup.Remove(probationOldData);
                    };

                    if (salMaster != null)
                    {
                        var probationData = new HRMS_Sal_Probation_MasterBackup
                        {
                            USER_GUID = salMaster.USER_GUID,
                            Division = salMaster.Division,
                            Factory = salMaster.Factory,
                            Sal_Month = salMonth,
                            Employee_ID = salMaster.Employee_ID,
                            Department = salMaster.Department,
                            Position_Grade = salMaster.Position_Grade,
                            Position_Title = salMaster.Position_Title,
                            Permission_Group = salMaster.Permission_Group,
                            ActingPosition_Start = salMaster.ActingPosition_Start,
                            ActingPosition_End = salMaster.ActingPosition_End,
                            Technical_Type = salMaster.Technical_Type,
                            Expertise_Category = salMaster.Expertise_Category,
                            Salary_Type = salMaster.Salary_Type,
                            Salary_Grade = salMaster.Salary_Grade,
                            Salary_Level = salMaster.Salary_Level,
                            Currency = salMaster.Currency,
                            Update_By = data.Update_By,
                            Update_Time = now
                        };
                        _repositoryAccessor.HRMS_Sal_Probation_MasterBackup.Add(probationData);
                    }

                    if (salMasterDetail.Any())
                    {
                        List<HRMS_Sal_Probation_MasterBackup_Detail> probationDetailDataList = new();

                        foreach (var masterDetail in salMasterDetail)
                        {
                            var probationDetailData = new HRMS_Sal_Probation_MasterBackup_Detail
                            {
                                USER_GUID = masterDetail.USER_GUID,
                                Division = masterDetail.Division,
                                Factory = masterDetail.Factory,
                                Sal_Month = salMonth,
                                Employee_ID = masterDetail.Employee_ID,
                                Salary_Item = masterDetail.Salary_Item,
                                Amount = masterDetail.Amount,
                                Update_By = data.Update_By,
                                Update_Time = now
                            };
                            probationDetailDataList.Add(probationDetailData);
                        }

                        _repositoryAccessor.HRMS_Sal_Probation_MasterBackup_Detail.AddMultiple(probationDetailDataList);
                    }
                }

                // Thêm mới HRMS_Sal_History
                var dataHistory = new HRMS_Sal_History
                {
                    History_GUID = History_GUID,
                    USER_GUID = data.USER_GUID,
                    Division = data.Division,
                    Factory = data.Factory,
                    Effective_Date = effectiveDateValue,
                    Seq = data.Seq,
                    Reason = data.Reason_For_Change,
                    Employee_ID = data.Employee_ID,
                    Department = data.After.Department,
                    Position_Grade = data.After.Position_Grade,
                    Position_Title = data.After.Position_Title,
                    Permission_Group = data.After.Permission_Group,
                    ActingPosition_Start = data.After.Acting_Position_Start != null ? Convert.ToDateTime(data.After.Acting_Position_Start) : null,
                    ActingPosition_End = data.After.Acting_Position_End != null ? Convert.ToDateTime(data.After.Acting_Position_End) : null,
                    Currency = data.Currency,
                    Salary_Type = data.After.Salary_Type,
                    Salary_Grade = data.After.Salary_Grade,
                    Salary_Level = data.After.Salary_Level,
                    Technical_Type = data.After.Technical_Type,
                    Expertise_Category = data.After.Expertise_Category,
                    Update_By = data.Update_By,
                    Update_Time = now
                };
                _repositoryAccessor.HRMS_Sal_History.Add(dataHistory);

                // Thêm mới HRMS_Sal_History_Detail
                List<HRMS_Sal_History_Detail> dataHistoryDetails = new();
                if (data.Salary_Item != null)
                {
                    foreach (var item in data.Salary_Item)
                    {
                        HRMS_Sal_History_Detail dataHistoryDetail = new()
                        {
                            History_GUID = dataHistory.History_GUID,
                            USER_GUID = data.USER_GUID,
                            Division = data.Division,
                            Factory = data.Factory,
                            Employee_ID = data.Employee_ID,
                            Salary_Item = item.Salary_Item,
                            Amount = item.Amount,
                            Update_By = data.Update_By,
                            Update_Time = now
                        };
                        dataHistoryDetails.Add(dataHistoryDetail);
                    }
                    _repositoryAccessor.HRMS_Sal_History_Detail.AddMultiple(dataHistoryDetails);
                }

                // Kiểm tra HRMS_Sal_Master
                if (salMaster == null)
                {
                    // Nếu chưa có HRMS_Sal_Master thì thêm mới 
                    var dataMaster = new HRMS_Sal_Master
                    {
                        USER_GUID = data.USER_GUID,
                        Division = data.Division,
                        Factory = data.Factory,
                        Employee_ID = data.Employee_ID,
                        Department = data.After.Department,
                        Technical_Type = data.After.Technical_Type,
                        Expertise_Category = data.After.Expertise_Category,
                        ActingPosition_Start = data.After.Acting_Position_Start != null ? Convert.ToDateTime(data.After.Acting_Position_Start) : null,
                        ActingPosition_End = data.After.Acting_Position_End != null ? Convert.ToDateTime(data.After.Acting_Position_End) : null,
                        Position_Grade = data.After.Position_Grade,
                        Position_Title = data.After.Position_Title,
                        Permission_Group = data.After.Permission_Group,
                        Currency = data.Currency,
                        Salary_Type = data.After.Salary_Type,
                        Salary_Grade = data.After.Salary_Grade,
                        Salary_Level = data.After.Salary_Level,
                        Update_By = data.Update_By,
                        Update_Time = now
                    };
                    _repositoryAccessor.HRMS_Sal_Master.Add(dataMaster);
                }
                else
                {
                    // Nếu đã có HRMS_Sal_Master thì cập nhật lại theo Key 
                    var dataMaster = await _repositoryAccessor.HRMS_Sal_Master.FirstOrDefaultAsync(x => x.Factory == data.Factory && x.Employee_ID == data.Employee_ID);

                    dataMaster.Division = data.Division;
                    dataMaster.Department = data.After.Department;
                    dataMaster.Technical_Type = data.After.Technical_Type;
                    dataMaster.Expertise_Category = data.After.Expertise_Category;
                    dataMaster.ActingPosition_Start = data.After.Acting_Position_Start != null ? Convert.ToDateTime(data.After.Acting_Position_Start) : null;
                    dataMaster.ActingPosition_End = data.After.Acting_Position_End != null ? Convert.ToDateTime(data.After.Acting_Position_End) : null;
                    dataMaster.Position_Grade = data.After.Position_Grade;
                    dataMaster.Position_Title = data.After.Position_Title;
                    dataMaster.Permission_Group = data.After.Permission_Group;
                    dataMaster.Currency = data.Currency;
                    dataMaster.Salary_Type = data.After.Salary_Type;
                    dataMaster.Salary_Grade = data.After.Salary_Grade;
                    dataMaster.Salary_Level = data.After.Salary_Level;
                    dataMaster.Update_By = data.Update_By;
                    dataMaster.Update_Time = now;
                    _repositoryAccessor.HRMS_Sal_Master.Update(dataMaster);
                }

                // Xóa dữ liệu trong HRMS_Sal_Master_Detail để thêm mới lại
                if (salMasterDetail.Any())
                    _repositoryAccessor.HRMS_Sal_Master_Detail.RemoveMultiple(salMasterDetail);

                List<HRMS_Sal_Master_Detail> dataMasterDetails = new();
                if (data.Salary_Item != null)
                {
                    foreach (var item in data.Salary_Item)
                    {
                        HRMS_Sal_Master_Detail dataMasterDetail = new()
                        {
                            USER_GUID = data.USER_GUID,
                            Division = data.Division,
                            Factory = data.Factory,
                            Employee_ID = data.Employee_ID,
                            Salary_Item = item.Salary_Item,
                            Amount = item.Amount,
                            Update_By = data.Update_By,
                            Update_Time = now
                        };
                        dataMasterDetails.Add(dataMasterDetail);
                    }
                    _repositoryAccessor.HRMS_Sal_Master_Detail.AddMultiple(dataMasterDetails);
                }
                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true, "Create Successfully");
            }
            catch (Exception ex)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, $"Inner exception: {ex.InnerException?.Message ?? "No inner exception message available"}");
            }
        }
        #endregion
        #region Update

        public async Task<OperationResult> Update(SalaryAdjustmentMaintenanceMain data)
        {
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                var dataHistory = await _repositoryAccessor.HRMS_Sal_History.FirstOrDefaultAsync(x => x.History_GUID == data.History_GUID);
                if (dataHistory == null)
                    return new OperationResult(false, "No data");

                DateTime now = DateTime.Now;
                var checkHistoryDetail = await _repositoryAccessor.HRMS_Sal_History_Detail.FindAll(x => x.History_GUID == data.History_GUID).ToListAsync();
                if (checkHistoryDetail.Any())
                    _repositoryAccessor.HRMS_Sal_History_Detail.RemoveMultiple(checkHistoryDetail);
                // Xóa dữ liệu trong HRMS_Sal_Master_Detail để thêm mới lại
                var checkMasterDetail = await _repositoryAccessor.HRMS_Sal_Master_Detail.FindAll(x => x.Factory == data.Factory && x.Employee_ID == data.Employee_ID).ToListAsync();
                if (checkMasterDetail.Any())
                    _repositoryAccessor.HRMS_Sal_Master_Detail.RemoveMultiple(checkMasterDetail);
                await _repositoryAccessor.Save();

                // Cập nhật lại HRMS_Sal_History
                dataHistory.Reason = data.Reason_For_Change;
                dataHistory.Salary_Type = data.Salary_Type;
                dataHistory.Salary_Level = data.Salary_Level;
                dataHistory.Salary_Grade = data.Salary_Grade;
                dataHistory.Update_By = data.Update_By;
                dataHistory.Update_Time = now;
                _repositoryAccessor.HRMS_Sal_History.Update(dataHistory);

                // Cập nhật lại HRMS_Sal_Master
                var dataMaster = await _repositoryAccessor.HRMS_Sal_Master.FirstOrDefaultAsync(x => x.Factory == data.Factory && x.Employee_ID == data.Employee_ID);
                if (dataMaster != null)
                {
                    dataMaster.Salary_Type = data.Salary_Type;
                    dataMaster.Salary_Level = data.Salary_Level;
                    dataMaster.Salary_Grade = data.Salary_Grade;
                    dataMaster.Update_By = data.Update_By;
                    dataMaster.Update_Time = now;
                    _repositoryAccessor.HRMS_Sal_Master.Update(dataMaster);
                }

                List<HRMS_Sal_History_Detail> dataHistoryDetails = new();
                List<HRMS_Sal_Master_Detail> dataMasterDetails = new();

                foreach (var item in data.Salary_Item)
                {
                    // Thêm lại HRMS_Sal_History_Detail
                    HRMS_Sal_History_Detail dataHistoryDetail = new()
                    {
                        History_GUID = data.History_GUID,
                        USER_GUID = data.USER_GUID,
                        Division = data.Division,
                        Factory = data.Factory,
                        Employee_ID = data.Employee_ID,
                        Salary_Item = item.Salary_Item,
                        Amount = item.Amount,
                        Update_By = data.Update_By,
                        Update_Time = now
                    };
                    dataHistoryDetails.Add(dataHistoryDetail);

                    // Thêm lại HRMS_Sal_Master_Detail
                    HRMS_Sal_Master_Detail dataMasterDetail = new()
                    {
                        USER_GUID = data.USER_GUID,
                        Division = data.Division,
                        Factory = data.Factory,
                        Employee_ID = data.Employee_ID,
                        Salary_Item = item.Salary_Item,
                        Amount = item.Amount,
                        Update_By = data.Update_By,
                        Update_Time = now
                    };
                    dataMasterDetails.Add(dataMasterDetail);
                }
                _repositoryAccessor.HRMS_Sal_Master_Detail.AddMultiple(dataMasterDetails);
                _repositoryAccessor.HRMS_Sal_History_Detail.AddMultiple(dataHistoryDetails);


                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true, "Update Successfully");
            }
            catch (Exception ex)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, $"Inner exception: {ex.InnerException?.Message ?? "No inner exception message available"}");
            }
        }
        #endregion
        #region GetData

        public async Task<List<SalaryAdjustmentMaintenanceMain>> GetData(SalaryAdjustmentMaintenanceParam param, bool getSalaryItem = true)
        {
            if (string.IsNullOrWhiteSpace(param.Factory))
                return new List<SalaryAdjustmentMaintenanceMain>();
            // Tạo pred cho HRMS_Sal_History và HRMS_Emp_Personal
            var pred = PredicateBuilder.New<HRMS_Sal_History>(x => x.Factory == param.Factory);
            var predPersonal = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory);

            if (!string.IsNullOrWhiteSpace(param.Department))
                pred = pred.And(x => x.Department == param.Department);

            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
                pred = pred.And(x => x.Employee_ID.Contains(param.Employee_ID.Trim()));

            if (!string.IsNullOrWhiteSpace(param.Reason_For_Change))
                pred = pred.And(x => x.Reason == param.Reason_For_Change);

            if (param.Effective_Date_Start != null)
                pred = pred.And(x => x.Effective_Date >= Convert.ToDateTime(param.Effective_Date_Start));

            if (param.Effective_Date_End != null)
                pred = pred.And(x => x.Effective_Date <= Convert.ToDateTime(param.Effective_Date_End));

            if (!string.IsNullOrWhiteSpace(param.Onboard_Date))
                predPersonal = predPersonal.And(x => x.Onboard_Date == Convert.ToDateTime(param.Onboard_Date));

            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll(true);
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower(), true);
            var codLang = HBC
                .GroupJoin(HBCL,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Code,
                    x.HBC.Type_Seq,
                    Code_Name = x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name
                });
            var dataHistory = _repositoryAccessor.HRMS_Sal_History.FindAll(pred, true);
            var dataHistoryDetail = _repositoryAccessor.HRMS_Sal_History_Detail.FindAll(true);
            var dataUnpaidLeave = _repositoryAccessor.HRMS_Emp_Unpaid_Leave.FindAll(x => x.Factory == param.Factory && x.Effective_Status, true);

            var data = await _repositoryAccessor.HRMS_Sal_History.FindAll(pred, true)
                .Join(_repositoryAccessor.HRMS_Emp_Personal.FindAll(predPersonal),
                    x => new { x.USER_GUID },
                    y => new { y.USER_GUID },
                    (x, y) => new { HSH = x, HEP = y })
                .Join(_repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == param.Factory, true),
                    x => new { x.HSH.Division, x.HSH.Factory, x.HSH.Department },
                    y => new { y.Division, y.Factory, Department = y.Department_Code },
                    (x, y) => new { x.HSH, x.HEP, HOD = y })
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower(), true),
                    x => new { x.HOD.Department_Code, x.HOD.Division, x.HOD.Factory },
                    y => new { y.Department_Code, y.Division, y.Factory },
                    (x, y) => new { x.HSH, x.HEP, x.HOD, HODL = y })
                .SelectMany(x => x.HODL.DefaultIfEmpty(), (x, y) => new { x.HSH, x.HEP, x.HOD, HODL = y })
                .Select(x => new SalaryAdjustmentMaintenanceMain
                {
                    History_GUID = x.HSH.History_GUID,
                    USER_GUID = x.HSH.USER_GUID,
                    Division = x.HSH.Division,
                    Factory = x.HSH.Factory,
                    Department = x.HSH.Department,
                    Department_Name = x.HSH.Department +
                        (x.HODL != null ? (" - " + x.HODL.Name) :
                        (x.HOD != null ? (" - " + x.HOD.Department_Name) : "")),
                    Employee_ID = x.HSH.Employee_ID,
                    Local_Full_Name = x.HEP != null ? x.HEP.Local_Full_Name : "",
                    Onboard_Date_Str = x.HEP != null ? x.HEP.Onboard_Date.ToString("yyyy/MM/dd") : "",
                    Reason_For_Change = x.HSH.Reason,
                    Reason_For_Change_Name = codLang.Any(y => y.Type_Seq == BasicCodeTypeConstant.ReasonForChange && y.Code == x.HSH.Reason)
                        ? x.HSH.Reason + " - " + codLang.FirstOrDefault(y => y.Type_Seq == BasicCodeTypeConstant.ReasonForChange && y.Code == x.HSH.Reason).Code_Name
                        : x.HSH.Reason,
                    Effective_Date_Str = x.HSH.Effective_Date.ToString("yyyy/MM/dd"),
                    Seq = x.HSH.Seq,
                    Employment_Status = dataUnpaidLeave.Any(y => y.Employee_ID == x.HEP.Employee_ID) ? "U" : x.HEP.Deletion_Code == "Y" ? "Y" : "N",
                    Technical_Type = x.HSH.Technical_Type,
                    Technical_Type_Name = codLang.Any(y => y.Type_Seq == BasicCodeTypeConstant.Technical_Type && y.Code == x.HSH.Technical_Type)
                        ? x.HSH.Technical_Type + " - " + codLang.FirstOrDefault(y => y.Type_Seq == BasicCodeTypeConstant.Technical_Type && y.Code == x.HSH.Technical_Type).Code_Name
                        : x.HSH.Technical_Type,
                    Expertise_Category = x.HSH.Expertise_Category,
                    Expertise_Category_Name = codLang.Any(y => y.Type_Seq == BasicCodeTypeConstant.Expertise_Category && y.Code == x.HSH.Expertise_Category)
                        ? x.HSH.Expertise_Category + " - " + codLang.FirstOrDefault(y => y.Type_Seq == BasicCodeTypeConstant.Expertise_Category && y.Code == x.HSH.Expertise_Category).Code_Name
                        : x.HSH.Expertise_Category,
                    Acting_Position_Start = x.HSH.ActingPosition_Start.HasValue ? x.HSH.ActingPosition_Start.Value.ToString("yyyy/MM/dd") : "",
                    Acting_Position_End = x.HSH.ActingPosition_End.HasValue ? x.HSH.ActingPosition_End.Value.ToString("yyyy/MM/dd") : "",
                    Position_Grade = x.HSH.Position_Grade,
                    Position_Title = x.HSH.Position_Title,
                    Position_Title_Name = codLang.Any(y => y.Type_Seq == BasicCodeTypeConstant.JobTitle && y.Code == x.HSH.Position_Title)
                        ? x.HSH.Position_Title + " - " + codLang.FirstOrDefault(y => y.Type_Seq == BasicCodeTypeConstant.JobTitle && y.Code == x.HSH.Position_Title).Code_Name
                        : x.HSH.Position_Title,
                    Permission_Group = x.HSH.Permission_Group,
                    Salary_Type = x.HSH.Salary_Type,
                    Salary_Type_Name = codLang.Any(y => y.Type_Seq == BasicCodeTypeConstant.SalaryType && y.Code == x.HSH.Salary_Type)
                        ? x.HSH.Salary_Type + " - " + codLang.FirstOrDefault(y => y.Type_Seq == BasicCodeTypeConstant.SalaryType && y.Code == x.HSH.Salary_Type).Code_Name
                        : x.HSH.Salary_Type,
                    Salary_Grade = x.HSH.Salary_Grade,
                    Salary_Level = x.HSH.Salary_Level,
                    Currency = x.HSH.Currency,
                    Currency_Name = codLang.Any(y => y.Type_Seq == BasicCodeTypeConstant.Currency && y.Code == x.HSH.Currency)
                        ? x.HSH.Currency + " - " + codLang.FirstOrDefault(y => y.Type_Seq == BasicCodeTypeConstant.Currency && y.Code == x.HSH.Currency).Code_Name
                        : x.HSH.Currency,
                    Update_By = x.HSH.Update_By,
                    Update_Time = x.HSH.Update_Time.ToString("yyyy/MM/dd HH:mm:ss"),
                    Salary_Item = getSalaryItem ? dataHistoryDetail.Where(y => y.History_GUID == x.HSH.History_GUID)
                        .Select(z => new SalaryAdjustmentMaintenance_SalaryItem
                        {
                            Salary_Item = z.Salary_Item,
                            Amount = z.Amount
                        }).ToList() : new List<SalaryAdjustmentMaintenance_SalaryItem>(),
                    IsEdit = dataHistory.Any(y => y.Employee_ID == x.HSH.Employee_ID && y.Effective_Date > x.HSH.Effective_Date) ||
                            (dataHistory.Where(y => y.Employee_ID == x.HSH.Employee_ID && y.Effective_Date == x.HSH.Effective_Date).Max(y => y.Seq) != x.HSH.Seq) || x.HEP.Resign_Date.HasValue
                }).ToListAsync();

            // Tạo kết quả sau khi xử lý dữ liệu
            var result = data.OrderBy(x => x.Factory)
                         .ThenBy(x => x.Employee_ID)
                         .ThenByDescending(x => x.Effective_Date)
                         .ThenByDescending(x => x.Seq)
                         .ToList();

            // Sắp xếp và trả về kết quả
            return result;
        }
        // Lấy thông tin mã cơ bản (Basic Code) và mã ngôn ngữ
        private async Task<IEnumerable<dynamic>> GetCodeLanguage(string lang)
        {
            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll();
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == lang.ToLower());

            return await HBC
                .GroupJoin(HBCL,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Code,
                    x.HBC.Type_Seq,
                    Code_Name = x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name
                })
                .ToListAsync();
        }

        public async Task<PaginationUtility<SalaryAdjustmentMaintenanceMain>> GetDataPagination(PaginationParam pagination, SalaryAdjustmentMaintenanceParam param)
        {
            var data = await GetData(param, false);
            return PaginationUtility<SalaryAdjustmentMaintenanceMain>.Create(data, pagination.PageNumber, pagination.PageSize);
        }

        #endregion
        #region Download
        public async Task<OperationResult> DownloadExcel(SalaryAdjustmentMaintenanceParam param, string userName)
        {
            var data = await GetData(param);
            if (!data.Any())
                return new OperationResult(false, "No Data");

            DateTime now = DateTime.Now;
            var pred = PredicateBuilder.New<HRMS_Sal_History>(x => x.Factory == param.Factory);
            if (!string.IsNullOrWhiteSpace(param.Effective_Date_Start))
                pred = pred.And(x => x.Effective_Date >= DateTime.Parse(param.Effective_Date_Start));
            if (!string.IsNullOrWhiteSpace(param.Effective_Date_End))
                pred = pred.And(x => x.Effective_Date <= DateTime.Parse(param.Effective_Date_End));
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.SalaryItem, true).ToList();

            var salaryItems = await _repositoryAccessor.HRMS_Sal_History.FindAll(pred, true)
                 .Join(_repositoryAccessor.HRMS_Sal_History_Detail.FindAll(true),
                    x => x.History_GUID,
                    y => y.History_GUID,
                    (hsh, hshd) => new { hsh, hshd })
                  .Select(x => x.hshd.Salary_Item)
                  .Distinct()
                  .OrderBy(x => x)
                  .ToListAsync();

            if (!salaryItems.Any())
                return new OperationResult(false, "No Data");
            var salaryItemResults = salaryItems
                .Select(x => new SalaryAdjustmentMaintenance_SalaryItem
                {
                    Salary_Item = x,
                    Salary_Item_Name = x + " - " + HBCL.FirstOrDefault(y => y.Language_Code == "EN" && y.Code == x)?.Code_Name,
                    Salary_Item_NameTW = x + " - " + HBCL.FirstOrDefault(y => y.Language_Code == "TW" && y.Code == x)?.Code_Name,
                })
                .ToList();

            // Tạo danh sách biến thể để chứa dữ liệu cho Excel
            var excelData = new List<dynamic>();

            // Chuyển đổi dữ liệu từ định dạng hiện tại thành cấu trúc cho Excel
            foreach (var record in data)
            {
                var row = new Dictionary<string, object>
                {
                    { "Factory", record.Factory },
                    { "Department", record.Department_Name },
                    { "Employee_ID", record.Employee_ID },
                    { "Local_Full_Name", record.Local_Full_Name },
                    { "Onboard_Date_Str", record.Onboard_Date_Str },
                    { "Reason_For_Change_Name", record.Reason_For_Change_Name },
                    { "Effective_Date_Str", record.Effective_Date_Str },
                    { "Seq", record.Seq },
                    { "Employment_Status", record.Employment_Status },
                    { "Technical_Type", record.Technical_Type_Name },
                    { "Expertise_Category", record.Expertise_Category_Name },
                    { "Acting_Position_Start", record.Acting_Position_Start },
                    { "Acting_Position_End", record.Acting_Position_End },
                    { "Position_Grade", record.Position_Grade },
                    { "Position_Title", record.Position_Title_Name },
                    { "Permission_Group", record.Permission_Group },
                    { "Salary_Type", record.Salary_Type_Name },
                    { "Salary_Grade", record.Salary_Grade },
                    { "Salary_Level", record.Salary_Level },
                    { "Currency", record.Currency_Name },
                    { "Update_By", record.Update_By },
                    { "Update_Time", record.Update_Time }
                };

                // Thêm các Amount cho từng Salary Item vào từ bản ghi
                foreach (var salaryItem in salaryItemResults)
                {
                    // Lấy Amount cho Salary_Item tương ứng
                    var amount = record.Salary_Item.FirstOrDefault(s => s.Salary_Item == salaryItem.Salary_Item)?.Amount ?? 0;

                    // Gán Amount cho cột tương ứng
                    row[$"{salaryItem.Salary_Item}"] = amount;
                }
                excelData.Add(row);
            }

            MemoryStream memoryStream = new();
            string file = Path.Combine(
                rootPath,
                "Resources\\Template\\SalaryMaintenance\\7_1_18_SalaryAdjustmentMaintenance\\Download.xlsx"
            );
            WorkbookDesigner obj = new()
            {
                Workbook = new Workbook(file)
            };
            Worksheet worksheet = obj.Workbook.Worksheets[0];

            Style titleStyle = obj.Workbook.CreateStyle();
            titleStyle.Font.IsBold = true;
            titleStyle.ForegroundColor = Color.FromArgb(221, 235, 247);
            titleStyle.Pattern = BackgroundType.Solid;
            titleStyle = AsposeUtility.SetAllBorders(titleStyle);

            Style dataStyleSalaryItem = obj.Workbook.CreateStyle();
            dataStyleSalaryItem.Custom = "#,##0";
            dataStyleSalaryItem = AsposeUtility.SetAllBorders(dataStyleSalaryItem);

            for (int i = 0; i < salaryItemResults.Count; i++)
            {
                worksheet.Cells[5, i + 22].PutValue(salaryItemResults[i].Salary_Item_NameTW);
                worksheet.Cells[6, i + 22].PutValue(salaryItemResults[i].Salary_Item_Name);

                // Áp dụng style cho các ô vừa ghi
                worksheet.Cells[5, i + 22].SetStyle(titleStyle);
                worksheet.Cells[6, i + 22].SetStyle(titleStyle);

            }
            worksheet.Cells["B2"].PutValue(userName);
            worksheet.Cells["D2"].PutValue(now);

            Style dataStyle = obj.Workbook.CreateStyle();
            dataStyle = AsposeUtility.SetAllBorders(dataStyle);


            // Ghi dữ liệu
            for (int i = 0; i < excelData.Count; i++)
            {
                var row = excelData[i];
                int columnIndex = 0;

                foreach (var key in row.Keys)
                {
                    worksheet.Cells[i + 7, columnIndex].PutValue(row[key]);
                    worksheet.Cells[i + 7, columnIndex].SetStyle(dataStyle);
                    columnIndex++;
                }
            }
            worksheet.Cells.CreateRange(7, 22, excelData.Count, salaryItemResults.Count).SetStyle(dataStyleSalaryItem);
            worksheet.AutoFitColumns();
            obj.Workbook.Save(memoryStream, SaveFormat.Xlsx);
            var excelResult = new ExcelResult(isSuccess: true, memoryStream.ToArray());
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }
        #endregion
        private async Task<List<SalaryAdjustmentMaintenance_SalaryItem>> GetTitleDownload(string factory)
        {
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.SalaryItem, true).ToList();

            var maxEffectiveMonth = await _repositoryAccessor.HRMS_Sal_Item_Settings
                .FindAll(x => x.Factory == factory)
                .MaxAsync(x => x.Effective_Month);
            var salaryItems = await _repositoryAccessor.HRMS_Sal_Item_Settings
                .FindAll(x => x.Factory == factory && x.Effective_Month == maxEffectiveMonth)
                .Select(x => x.Salary_Item)
                .Distinct()
                .ToListAsync();
            if (!salaryItems.Any())
                return null;

            var salaryItemResults = salaryItems
                .Select(x => new SalaryAdjustmentMaintenance_SalaryItem
                {
                    Salary_Item = x,
                    Salary_Item_Name = x + " - " + HBCL.FirstOrDefault(y => y.Language_Code == "EN" && y.Code == x)?.Code_Name,
                    Salary_Item_NameTW = x + " - " + HBCL.FirstOrDefault(y => y.Language_Code == "TW" && y.Code == x)?.Code_Name,
                })
                .Distinct()
                .ToList();
            return salaryItemResults;
        }
        #region CheckExcel
        public async Task<OperationResult> DownloadTemplate(string factory)
        {
            var salaryItemResults = await GetTitleDownload(factory);
            List<Table> tables = new()
            {
                new Table("result", salaryItemResults)
            };
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                tables,
                "Resources\\Template\\SalaryMaintenance\\7_1_18_SalaryAdjustmentMaintenance\\Template.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }
        private static readonly string rootPath = Directory.GetCurrentDirectory();
        public static ExcelResult CheckExcel(IFormFile file, string subPath)
        {
            if (file == null)
                return new ExcelResult(false, "File not found");
            using Stream stream = file.OpenReadStream();
            WorkbookDesigner designer = new()
            {
                Workbook = new Workbook(stream)
            };
            Worksheet ws = designer.Workbook.Worksheets[0];
            if (designer.Workbook.Worksheets.Count > 1)
                return new ExcelResult(false, "More than one sheet");
            string pathTemp = Path.Combine(rootPath, subPath);
            designer.Workbook = new Workbook(pathTemp);
            Worksheet wsTemp = designer.Workbook.Worksheets[0];
            ws.Cells.DeleteBlankColumns();
            ws.Cells.DeleteBlankRows();
            wsTemp.Cells.DeleteBlankColumns();
            wsTemp.Cells.DeleteBlankRows();
            if (ws.Cells.MaxDataColumn < wsTemp.Cells.MaxDataColumn)
                return new ExcelResult(false, "Not enough column for data import");
            if (ws.Cells.MaxDataRow <= wsTemp.Cells.MaxDataRow)
                return new ExcelResult(false, "No data in excel file");
            string firstCellTemp = wsTemp.Cells[0, 0].Name;
            string lastCellTemp = wsTemp.Cells[wsTemp.Cells.MaxDataRow, wsTemp.Cells.MaxDataColumn].Name;
            Aspose.Cells.Range rangeTemp = wsTemp.Cells.CreateRange(firstCellTemp, lastCellTemp);
            Aspose.Cells.Range range = ws.Cells.CreateRange(firstCellTemp, lastCellTemp);
            for (int r = 0; r < rangeTemp.RowCount; r++)
            {
                for (int c = 0; c < rangeTemp.ColumnCount; c++)
                {
                    string val = range[r, c].Value != null ? range[r, c].StringValue.Trim() : "";
                    string valTmp = rangeTemp[r, c].Value != null ? rangeTemp[r, c].StringValue.Trim() : "";
                    if (val != valTmp)
                        return new ExcelResult(false, $"Header in cell {CellsHelper.CellIndexToName(r, c)} : {val}\nMust be : {valTmp}");
                }
            }
            return new ExcelResult(true, ws, wsTemp);
        }
        #endregion
        #region Upload
        public async Task<OperationResult> UploadExcel(IFormFile file, List<string> role_List, string userName)
        {
            await semaphore.WaitAsync();
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                ExcelResult resp = CheckExcel(
                    file,
                    "Resources\\Template\\SalaryMaintenance\\7_1_18_SalaryAdjustmentMaintenance\\TemplateCheck.xlsx"
                );
                if (!resp.IsSuccess)
                    return new OperationResult(false, resp.Error);

                DateTime now = DateTime.Now;
                List<HRMS_Sal_History> dataHistorys = new();
                List<HRMS_Sal_History_Detail> dataHistoryDetails = new();
                List<HRMS_Sal_Master> dataMasters = new();
                List<HRMS_Sal_Master_Detail> dataMasterDetails = new();
                List<SalaryAdjustmentMaintenanceMain> dataReport = new();
                List<HRMS_Sal_Probation_MasterBackup> dataProbationMasters = new();
                List<HRMS_Sal_Probation_MasterBackup_Detail> dataProbationMasterDetails = new();

                List<string> roleFactories = await _repositoryAccessor.HRMS_Basic_Role
                    .FindAll(x => role_List.Contains(x.Role))
                    .Select(x => x.Factory).Distinct()
                    .ToListAsync();
                if (!roleFactories.Any())
                    return new OperationResult(false, "Recent account roles do not have any factory.");

                List<HRMS_Basic_Code> checkHBC = await _repositoryAccessor.HRMS_Basic_Code.FindAll(true).ToListAsync();
                var dataHEP = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => roleFactories.Contains(x.Factory), true).ToListAsync();
                Dictionary<string, int> entryCounts = new();

                for (int i = 0; i < resp.Ws.Cells.Rows.Count - 3; i++)
                {
                    var History_GUID = Guid.NewGuid().ToString();
                    List<string> errorMessage = new();

                    string division = resp.Ws.Cells[i + 3, 0].StringValue?.Trim();
                    string factory = resp.Ws.Cells[i + 3, 1].StringValue?.Trim();
                    string employeeID = resp.Ws.Cells[i + 3, 2].StringValue?.Trim();
                    string permissionGroup = resp.Ws.Cells[i + 3, 3].StringValue?.Trim();
                    string reasonForChange = resp.Ws.Cells[i + 3, 4].StringValue?.Trim();
                    string effectiveDate = resp.Ws.Cells[i + 3, 5].StringValue.Trim();
                    string salaryType = resp.Ws.Cells[i + 3, 6].StringValue?.Trim();
                    string salaryGrade = resp.Ws.Cells[i + 3, 7].StringValue?.Trim();
                    string salaryLevel = resp.Ws.Cells[i + 3, 8].StringValue?.Trim();
                    string currency = resp.Ws.Cells[i + 3, 9].StringValue?.Trim();
                    string probationSalaryMonth = resp.Ws.Cells[i + 3, 10].StringValue?.Trim();

                    #region  Validate data upload
                    // Division
                    if (string.IsNullOrWhiteSpace(division))
                        errorMessage.Add("Division cannot be empty.\n");
                    if (!string.IsNullOrWhiteSpace(division))
                        if (!checkHBC.Where(x => x.Type_Seq == BasicCodeTypeConstant.Division).Select(x => x.Code).Contains(division))
                            errorMessage.Add($"Uploaded Division: {division} is invalid.\n");

                    // Factory
                    if (string.IsNullOrWhiteSpace(factory))
                        errorMessage.Add("Factory cannot be empty.\n");
                    if (!string.IsNullOrWhiteSpace(factory))
                        if (!roleFactories.Contains(factory))
                            errorMessage.Add($"Uploaded Factory: {factory} data does not match the role group.\n");

                    if (string.IsNullOrWhiteSpace(employeeID) || employeeID.Length > 16)
                        errorMessage.Add("Employee ID is invalid.\n");

                    var empInfo = dataHEP.FirstOrDefault(x => x.Factory == factory && x.Employee_ID == employeeID);
                    if (empInfo == null && !string.IsNullOrWhiteSpace(factory) && !string.IsNullOrWhiteSpace(employeeID))
                        errorMessage.Add("Employee Information data is not existed.\n");

                    // Permission Group
                    if (string.IsNullOrWhiteSpace(permissionGroup))
                        errorMessage.Add("Permisssion Group cannot be empty.\n");
                    if (!string.IsNullOrWhiteSpace(permissionGroup))
                        if (!checkHBC.Where(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup).Select(x => x.Code).Contains(permissionGroup))
                            errorMessage.Add($"Uploaded Permisssion Group: {permissionGroup} is invalid.\n");

                    // Reason
                    if (string.IsNullOrWhiteSpace(reasonForChange))
                        errorMessage.Add("Reason for Change cannot be empty.\n");
                    if (!string.IsNullOrWhiteSpace(reasonForChange))
                        if (!checkHBC.Where(x => x.Type_Seq == BasicCodeTypeConstant.ReasonForChange).Select(x => x.Code).Contains(reasonForChange))
                            errorMessage.Add($"Uploaded Reason for Change: {reasonForChange} is invalid.\n");

                    // Effective Date
                    bool validEffectiveDate = false;
                    DateTime effectiveDateValue = default;
                    var check = new CheckEffectiveDateResult { };
                    if (string.IsNullOrWhiteSpace(effectiveDate))
                        errorMessage.Add("Effective Date cannot be empty.\n");
                    else
                    {
                        validEffectiveDate = DateTime.TryParseExact(effectiveDate, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out effectiveDateValue);
                        if (!validEffectiveDate)
                            errorMessage.Add("Effective Date is invalid date format (yyyy/MM/dd).\n");
                        check = await CheckEffectiveDate(factory, employeeID, effectiveDate);
                        if (!check.CheckEffectiveDate)
                            errorMessage.Add("The effective date cannot be earlier than already stored effective dates in the transaction table.\n");
                    }

                    // Salary Type
                    if (string.IsNullOrWhiteSpace(salaryType))
                        errorMessage.Add("Salary Type for Change cannot be empty.\n");
                    if (!string.IsNullOrWhiteSpace(salaryType))
                        if (!checkHBC.Where(x => x.Type_Seq == BasicCodeTypeConstant.SalaryType).Select(x => x.Code).Contains(salaryType))
                            errorMessage.Add($"Uploaded Salary Type: {salaryType} is invalid.\n");

                    // Salary Grade
                    if (string.IsNullOrWhiteSpace(salaryGrade) || !decimal.TryParse(salaryGrade, out decimal gradeValue) || gradeValue < 0 || gradeValue > 15 || HasMoreThanOneDecimalPlace(salaryGrade))
                        errorMessage.Add("Salary Grade must be between 0 and 15.\n");
                    // Salary Level
                    if (string.IsNullOrWhiteSpace(salaryLevel) || !decimal.TryParse(salaryLevel, out decimal levelValue) || levelValue < 0 || levelValue > 9 || HasMoreThanOneDecimalPlace(salaryLevel))
                        errorMessage.Add("Salary Level must be between 0 and 9.\n");

                    // Currency
                    if (string.IsNullOrWhiteSpace(currency))
                        errorMessage.Add("Currency cannot be empty.\n");
                    if (!string.IsNullOrWhiteSpace(currency))
                        if (!checkHBC.Where(x => x.Type_Seq == BasicCodeTypeConstant.Currency).Select(x => x.Code).Contains(currency))
                            errorMessage.Add($"Uploaded Currency: {currency} is invalid.\n");

                    // Probation Salary Month
                    var isProbationSalaryMonth = await CheckReasonForChange(reasonForChange);
                    DateTime salMonth = default;
                    bool isValidFormat = DateTime.TryParseExact(probationSalaryMonth, "yyyy/MM", new CultureInfo("en-US"), DateTimeStyles.None, out salMonth);
                    if (isProbationSalaryMonth)
                    {
                        if (string.IsNullOrWhiteSpace(probationSalaryMonth))
                            errorMessage.Add("Probation Salary Month cannot be empty.\n");
                        else if (!isValidFormat)
                            errorMessage.Add("Probation Salary Month is invalid date format (yyyy/MM).\n");
                    }

                    // Query Technical_Type & Expertise_Category
                    var queryEmpGroup = _repositoryAccessor.HRMS_Emp_Group.FirstOrDefault(x => x.Division == division && x.Factory == factory && x.Employee_ID == employeeID);
                    #endregion
                    // 4. Kiểm tra mã lương với dữ liệu mới nhất
                    var validSalaryItems = _repositoryAccessor.HRMS_Sal_Item_Settings
                        .FindAll(x => x.Factory == factory &&
                                    x.Permission_Group == permissionGroup &&
                                    x.Salary_Type == salaryType &&
                                    x.Effective_Month <= effectiveDateValue)
                        .Select(x => x.Salary_Item);
                    // .ToListAsync();

                    // Lấy các giá trị từ cột K trở đi
                    for (int j = 11; j < resp.Ws.Cells.Columns.Count; j++)
                    {
                        string itemName = resp.Ws.Cells[0, j].StringValue?.Trim(); // Lấy tiêu đề cột
                        string itemValue = resp.Ws.Cells[i + 3, j].StringValue?.Trim(); // Lấy giá trị tương ứng

                        // Bỏ qua nếu item.Value là 0 hoặc null
                        if (string.IsNullOrWhiteSpace(itemValue) || itemValue == "0")
                            continue;

                        // Kiểm tra nếu item.Key không nằm trong validSalaryItems
                        if (!validSalaryItems.Contains(itemName))
                            errorMessage.Add($"Salary Item '{itemName}' is not valid for Factory '{factory}', Permission Group '{permissionGroup}', and Salary Type '{salaryType}'.\n");

                        if (!int.TryParse(itemValue, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int parsedValue))
                            errorMessage.Add($"Salary Item '{itemName}' has an invalid value '{itemValue}'. Expected a numeric value.\n");

                        if (!errorMessage.Any())
                        {
                            // Nếu vượt qua kiểm tra, tạo đối tượng HRMS_Sal_History_Detail mới và thêm vào danh sách
                            var dataHistoryDetail = new HRMS_Sal_History_Detail
                            {
                                History_GUID = History_GUID,
                                USER_GUID = empInfo.USER_GUID,
                                Division = division,
                                Factory = factory,
                                Employee_ID = employeeID,
                                Salary_Item = itemName,
                                Amount = parsedValue,
                                Update_Time = now,
                                Update_By = userName,
                            };
                            dataHistoryDetails.Add(dataHistoryDetail);

                            HRMS_Sal_Master_Detail dataMasterDetail = new()
                            {
                                USER_GUID = empInfo.USER_GUID,
                                Division = division,
                                Factory = factory,
                                Employee_ID = employeeID,
                                Salary_Item = itemName,
                                Amount = parsedValue,
                                Update_Time = now,
                                Update_By = userName,
                            };
                            dataMasterDetails.Add(dataMasterDetail);
                        }
                    }

                    var salMasterDetail = await _repositoryAccessor.HRMS_Sal_Master_Detail.FindAll(x => x.Factory == factory && x.Employee_ID == employeeID).ToListAsync();
                    if (salMasterDetail.Any())
                    {
                        _repositoryAccessor.HRMS_Sal_Master_Detail.RemoveMultiple(salMasterDetail);
                        await _repositoryAccessor.Save();
                    }

                    #region Add Data Upload
                    if (!errorMessage.Any())
                    {
                        var salMaster = await _repositoryAccessor.HRMS_Sal_Master.FirstOrDefaultAsync(x => x.Factory == factory && x.Employee_ID == employeeID);

                        // Query Period of Acting Position
                        var maxEffectiveDate = await _repositoryAccessor.HRMS_Emp_Transfer_History
                            .FindAll(x => x.USER_GUID == empInfo.USER_GUID)
                            .MaxAsync(x => (DateTime?)x.Effective_Date);
                        var queryActingPosition = await _repositoryAccessor.HRMS_Emp_Transfer_History
                            .FindAll(x => x.USER_GUID == empInfo.USER_GUID && x.Effective_Date == (maxEffectiveDate == null ? DateTime.MaxValue : maxEffectiveDate))
                            .GroupBy(x => new { x.ActingPosition_Star_After, x.ActingPosition_End_After })
                            .Select(x => new TransferHistory
                            {
                                Seq = x.Max(x => x.Seq),
                                ActingPosition_Star_After = x.Key.ActingPosition_Star_After.Value.ToString("yyyy/MM/dd"),
                                ActingPosition_End_After = x.Key.ActingPosition_End_After.Value.ToString("yyyy/MM/dd")
                            })
                            .OrderByDescending(x => x.Seq)
                            .FirstOrDefaultAsync();

                        HRMS_Sal_History dataHistory = new()
                        {
                            History_GUID = History_GUID,
                            USER_GUID = empInfo.USER_GUID,
                            Division = division,
                            Factory = factory,
                            Effective_Date = effectiveDateValue,
                            Seq = check.MaxSeq,
                            Reason = reasonForChange,
                            Employee_ID = employeeID,
                            Department = empInfo.Department,
                            Position_Grade = empInfo.Position_Grade,
                            Position_Title = empInfo.Position_Title,
                            Permission_Group = permissionGroup,
                            ActingPosition_Start = queryActingPosition != null ? Convert.ToDateTime(queryActingPosition.ActingPosition_Star_After) : null,
                            ActingPosition_End = queryActingPosition != null ? Convert.ToDateTime(queryActingPosition.ActingPosition_End_After) : null,
                            Currency = currency,
                            Salary_Type = salaryType,
                            Salary_Grade = Convert.ToDecimal(salaryGrade),
                            Salary_Level = Convert.ToDecimal(salaryLevel),
                            Technical_Type = queryEmpGroup?.Technical_Type,
                            Expertise_Category = queryEmpGroup?.Expertise_Category,
                            Update_Time = now,
                            Update_By = userName,
                        };
                        dataHistorys.Add(dataHistory);

                        if (isProbationSalaryMonth && !string.IsNullOrWhiteSpace(probationSalaryMonth) && isValidFormat)
                        {
                            var probationOldData = await _repositoryAccessor.HRMS_Sal_Probation_MasterBackup
                            .FirstOrDefaultAsync(x => x.Factory == factory && x.Employee_ID == employeeID && x.Sal_Month == salMonth);

                            var probationDetailOldData = await _repositoryAccessor.HRMS_Sal_Probation_MasterBackup_Detail
                            .FindAll(x => x.Factory == factory && x.Employee_ID == employeeID && x.Sal_Month == salMonth)
                            .ToListAsync();

                            if (probationOldData != null)
                            {
                                _repositoryAccessor.HRMS_Sal_Probation_MasterBackup_Detail.RemoveMultiple(probationDetailOldData);
                                _repositoryAccessor.HRMS_Sal_Probation_MasterBackup.Remove(probationOldData);
                                await _repositoryAccessor.Save();
                            }

                            var probationData = new HRMS_Sal_Probation_MasterBackup
                            {
                                USER_GUID = empInfo.USER_GUID,
                                Division = division,
                                Factory = factory,
                                Sal_Month = salMonth,
                                Employee_ID = employeeID,
                                Department = empInfo.Department,
                                Position_Grade = empInfo.Position_Grade,
                                Position_Title = empInfo.Position_Title,
                                Permission_Group = permissionGroup,
                                ActingPosition_Start = queryActingPosition != null ? Convert.ToDateTime(queryActingPosition.ActingPosition_Star_After) : null,
                                ActingPosition_End = queryActingPosition != null ? Convert.ToDateTime(queryActingPosition.ActingPosition_End_After) : null,
                                Technical_Type = queryEmpGroup?.Technical_Type,
                                Expertise_Category = queryEmpGroup?.Expertise_Category,
                                Salary_Type = salaryType,
                                Salary_Grade = Convert.ToDecimal(salaryGrade),
                                Salary_Level = Convert.ToDecimal(salaryLevel),
                                Currency = currency,
                                Update_By = userName,
                                Update_Time = now
                            };
                            dataProbationMasters.Add(probationData);

                            if (salMasterDetail.Any())
                            {
                                foreach (var masterDetail in salMasterDetail)
                                {
                                    var probationDetailData = new HRMS_Sal_Probation_MasterBackup_Detail
                                    {
                                        USER_GUID = masterDetail.USER_GUID,
                                        Division = masterDetail.Division,
                                        Factory = masterDetail.Factory,
                                        Sal_Month = salMonth,
                                        Employee_ID = masterDetail.Employee_ID,
                                        Salary_Item = masterDetail.Salary_Item,
                                        Amount = masterDetail.Amount,
                                        Update_By = userName,
                                        Update_Time = now
                                    };
                                    dataProbationMasterDetails.Add(probationDetailData);
                                }
                            }
                        }

                        // Kiểm tra HRMS_Sal_Master
                        if (salMaster == null)
                        {
                            // Nếu chưa có HRMS_Sal_Master thì thêm mới 
                            var dataMaster = new HRMS_Sal_Master
                            {
                                USER_GUID = empInfo.USER_GUID,
                                Division = division,
                                Factory = factory,
                                Employee_ID = employeeID,
                                Department = empInfo.Department,
                                Technical_Type = queryEmpGroup?.Technical_Type,
                                Expertise_Category = queryEmpGroup?.Expertise_Category,
                                ActingPosition_Start = queryActingPosition != null ? Convert.ToDateTime(queryActingPosition.ActingPosition_Star_After) : null,
                                ActingPosition_End = queryActingPosition != null ? Convert.ToDateTime(queryActingPosition.ActingPosition_End_After) : null,
                                Position_Grade = empInfo.Position_Grade,
                                Position_Title = empInfo.Position_Title,
                                Permission_Group = permissionGroup,
                                Currency = currency,
                                Salary_Type = salaryType,
                                Salary_Grade = Convert.ToDecimal(salaryGrade),
                                Salary_Level = Convert.ToDecimal(salaryLevel),
                                Update_Time = now,
                                Update_By = userName,
                            };
                            _repositoryAccessor.HRMS_Sal_Master.Add(dataMaster);
                        }
                        else
                        {
                            // Nếu đã có HRMS_Sal_Master thì cập nhật lại theo Key 
                            salMaster.Salary_Type = salaryType;
                            salMaster.Salary_Grade = Convert.ToDecimal(salaryGrade);
                            salMaster.Salary_Level = Convert.ToDecimal(salaryLevel);
                            salMaster.Update_Time = now;
                            salMaster.Update_By = userName;
                            _repositoryAccessor.HRMS_Sal_Master.Update(salMaster);
                        }
                    }
                    #endregion

                    // Kiểm tra trùng lặp Division, Factory, EmployeeID
                    string key = $"{division}_{factory}_{employeeID}";
                    if (entryCounts.TryGetValue(key, out int value))
                    {
                        // thêm thông báo khi lần trùng lặp thứ 2 trở đi
                        entryCounts[key] = ++value;
                        errorMessage.Add($"Duplicate entry found: Division = {division}, Factory = {factory}, Employee ID = {employeeID}.\n");
                    }
                    else
                        entryCounts[key] = 1; // Đặt lần xuất hiện đầu tiên

                    // Tạo báo cáo lỗi
                    SalaryAdjustmentMaintenanceMain report = new()
                    {
                        Division = division,
                        Factory = factory,
                        Employee_ID = employeeID,
                        Permission_Group = permissionGroup,
                        Reason_For_Change = reasonForChange,
                        Effective_Date = effectiveDate,
                        Salary_Type = salaryType,
                        Salary_Grade_Str = salaryGrade,
                        Salary_Level_Str = salaryLevel,
                        Currency = currency,
                        Probation_Salary_Month = probationSalaryMonth,
                        Update_Time = now.ToString(),
                        Update_By = userName,
                        Error_Message = !errorMessage.Any() ? "Y" : string.Join("\r\n", errorMessage)
                    };
                    dataReport.Add(report);
                }
                #region Check Error
                if (dataReport.Any(r => r.Error_Message != "Y"))
                {
                    using MemoryStream memoryStream = new();
                    Workbook originalWorkbook = new(file.OpenReadStream()); // Mở template đã tải lên
                    Worksheet worksheet = originalWorkbook.Worksheets[0];
                    var maxRow = resp.Ws.Cells.Columns.Count;
                    worksheet.Cells[1, maxRow].PutValue("錯誤訊息");
                    worksheet.Cells[2, maxRow].PutValue("Error Message");

                    Style style = originalWorkbook.CreateStyle();
                    style.Font.IsBold = true;
                    style.ForegroundColor = Color.FromArgb(255, 255, 0);
                    style.Pattern = BackgroundType.Solid;
                    style = AsposeUtility.SetAllBorders(style);
                    worksheet.Cells[1, maxRow].SetStyle(style);
                    worksheet.Cells[2, maxRow].SetStyle(style);

                    // Tạo kiểu cho văn bản thông báo lỗi
                    Style errorStyle = originalWorkbook.CreateStyle();
                    errorStyle.Font.Color = System.Drawing.Color.Red; // Màu chữ đỏ cho thông báo lỗi
                    errorStyle.IsTextWrapped = true;

                    // Lấy rowCount từ dataReport
                    for (int k = 0; k < dataReport.Count; k++)
                    {
                        var currentReport = dataReport[k];
                        int rowIndex = k + 3;
                        worksheet.Cells[rowIndex, maxRow].PutValue(currentReport.Error_Message);
                        worksheet.Cells[rowIndex, maxRow].SetStyle(errorStyle);
                    }

                    worksheet.AutoFitColumn(maxRow);

                    // Lưu file với các thông báo lỗi
                    originalWorkbook.Save(memoryStream, SaveFormat.Xlsx);
                    return new OperationResult { IsSuccess = false, Data = memoryStream.ToArray(), Error = "Please check Error Report" };
                }
                #endregion
                if (dataReport.Any())
                {
                    using MemoryStream memoryStream = new();
                    string fileLocation = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Resources\\Template\\SalaryMaintenance\\7_1_18_SalaryAdjustmentMaintenance\\Report.xlsx"
                    );
                    WorkbookDesigner workbookDesigner = new() { Workbook = new Workbook(fileLocation) };
                    Worksheet worksheet = workbookDesigner.Workbook.Worksheets[0];

                    worksheet.Cells["B2"].PutValue(userName);
                    worksheet.Cells["D2"].PutValue(now);
                    workbookDesigner.SetDataSource("result", dataReport);
                    workbookDesigner.Process();
                    worksheet.AutoFitColumns(worksheet.Cells.MinDataColumn, worksheet.Cells.MaxColumn);
                    worksheet.AutoFitRows(worksheet.Cells.MinDataRow + 1, worksheet.Cells.MaxRow);
                    workbookDesigner.Workbook.Save(memoryStream, SaveFormat.Xlsx);

                    // Kiểm tra nếu có lỗi
                    if (dataReport.Exists(x => x.Error_Message != "Y"))
                        return new OperationResult { IsSuccess = false, Data = memoryStream.ToArray(), Error = "Please check Error Report" };
                }
                _repositoryAccessor.HRMS_Sal_History.AddMultiple(dataHistorys);
                _repositoryAccessor.HRMS_Sal_History_Detail.AddMultiple(dataHistoryDetails);
                _repositoryAccessor.HRMS_Sal_Master.AddMultiple(dataMasters);
                _repositoryAccessor.HRMS_Sal_Master_Detail.AddMultiple(dataMasterDetails);
                _repositoryAccessor.HRMS_Sal_Probation_MasterBackup.AddMultiple(dataProbationMasters);
                _repositoryAccessor.HRMS_Sal_Probation_MasterBackup_Detail.AddMultiple(dataProbationMasterDetails);
                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true, "System.Message.UploadOKMsg");
            }
            catch (Exception ex)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, ex.Message);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static bool HasMoreThanOneDecimalPlace(string value)
        {
            var parts = value.Split('.');
            return parts.Length > 1 && parts[1].Length > 1;
        }
        #endregion
        #region GetDetail
        public async Task<SalaryAdjustmentMaintenance_PersonalDetail> GetDetailPersonal(string factory, string employee_ID, string language)
        {
            var checkUnpaid = await _repositoryAccessor.HRMS_Emp_Unpaid_Leave.FindAll(x => x.Factory == factory && x.Employee_ID == employee_ID && x.Effective_Status == true).ToListAsync();
            var data = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == factory && x.Employee_ID == employee_ID, true)
                        .Select(x => new SalaryAdjustmentMaintenance_PersonalDetail
                        {
                            USER_GUID = x.USER_GUID,
                            Division = x.Division,
                            Local_Full_Name = x.Local_Full_Name,
                            Resign_Date = x.Resign_Date,
                            Onboard_Date = x.Onboard_Date.ToString("yyyy/MM/dd"),
                            Employment_Status = language == "en" ? (checkUnpaid.Any() ? "U.Unpaid" : x.Deletion_Code == "Y" ? "Y.On job" : "N.Resigned")
                                                                 : (checkUnpaid.Any() ? "U.留停" : x.Deletion_Code == "Y" ? "Y.在職" : "N.離職"),
                        }).FirstOrDefaultAsync();
            if (data == null)
                return null;

            var salMaster = await _repositoryAccessor.HRMS_Sal_Master.FirstOrDefaultAsync(x => x.USER_GUID == data.USER_GUID);
            // Kiểm tra dữ liệu Sal_Master nếu không có thì trả về rỗng
            if (salMaster != null)
            {
                data.Before = new HistoryDetail
                {
                    Department = salMaster.Department,
                    Position_Grade = salMaster.Position_Grade,
                    Position_Title = salMaster.Position_Title,
                    Salary_Type = salMaster.Salary_Type,
                    Permission_Group = salMaster.Permission_Group,
                    Technical_Type = salMaster.Technical_Type,
                    Expertise_Category = salMaster.Expertise_Category,
                    Acting_Position_Start = salMaster.ActingPosition_Start.HasValue ? salMaster.ActingPosition_Start.Value.ToString("yyyy/MM/dd") : null,
                    Acting_Position_End = salMaster.ActingPosition_End.HasValue ? salMaster.ActingPosition_End.Value.ToString("yyyy/MM/dd") : null,
                    Salary_Grade = salMaster.Salary_Grade,
                    Salary_Level = salMaster.Salary_Level,
                    Item = await GetSalaryItemsAsync(factory, salMaster.Permission_Group, salMaster.Salary_Type, "before", language, employee_ID)
                };
            }

            // TODO: After
            // Query Technical_Type & Expertise_Category
            var queryEmpGroup = _repositoryAccessor.HRMS_Emp_Group.FirstOrDefault(x => x.Factory == factory && x.Employee_ID == employee_ID);
            // Query Period of Acting Position
            var maxEffectiveDate = await _repositoryAccessor.HRMS_Emp_Transfer_History
                .FindAll(x => x.USER_GUID == data.USER_GUID)
                .MaxAsync(x => (DateTime?)x.Effective_Date);
            var queryActingPosition = await _repositoryAccessor.HRMS_Emp_Transfer_History
                .FindAll(x => x.USER_GUID == data.USER_GUID && x.Effective_Date == (maxEffectiveDate == null ? DateTime.MaxValue : maxEffectiveDate))
                .GroupBy(x => new { x.ActingPosition_Star_After, x.ActingPosition_End_After })
                .Select(x => new TransferHistory
                {
                    Seq = x.Max(x => x.Seq),
                    ActingPosition_Star_After = x.Key.ActingPosition_Star_After.Value.ToString("yyyy/MM/dd"),
                    ActingPosition_End_After = x.Key.ActingPosition_End_After.Value.ToString("yyyy/MM/dd"),
                })
                .OrderByDescending(x => x.Seq)
                .FirstOrDefaultAsync();

            var salHistory = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.USER_GUID == data.USER_GUID)
                .GroupJoin(_repositoryAccessor.HRMS_Sal_Master.FindAll(),
                x => x.USER_GUID,
                y => y.USER_GUID,
                (x, y) => new { HEP = x, HSM = y })
                .SelectMany(x => x.HSM.DefaultIfEmpty(), (x, y) => new { x.HEP, HSM = y })
                .Select(x => new HistoryDetail
                {
                    Department = x.HEP.Department,
                    Position_Grade = x.HEP.Position_Grade,
                    Position_Title = x.HEP.Position_Title,
                    Salary_Type = x.HSM != null ? x.HSM.Salary_Type : null,
                    Permission_Group = x.HEP.Permission_Group,
                    Technical_Type = queryEmpGroup != null ? queryEmpGroup.Technical_Type : "XXX",
                    Expertise_Category = queryEmpGroup != null ? queryEmpGroup.Expertise_Category : "XXX",
                    Acting_Position_Start = queryActingPosition != null ? queryActingPosition.ActingPosition_Star_After : null,
                    Acting_Position_End = queryActingPosition != null ? queryActingPosition.ActingPosition_End_After : null,
                    Salary_Grade = x.HSM != null ? x.HSM.Salary_Grade : 0,
                    Salary_Level = x.HSM != null ? x.HSM.Salary_Level : 0,
                }).FirstOrDefaultAsync();
            salHistory.Item = await GetSalaryItemsAsync(factory, salHistory.Permission_Group, salHistory.Salary_Type, "after", language, employee_ID);

            data.After = salHistory;
            return data;
        }

        public async Task<List<SalaryAdjustmentMaintenance_SalaryItem>> GetSalaryItemsAsync(string factory, string permissionGroup, string salaryType, string type, string language, string employeeID = "")
        {
            if (type == "before")
            {
                var dataBefore = _repositoryAccessor.HRMS_Sal_Item_Settings
                .FindAll(x => x.Factory == factory
                    && x.Permission_Group == permissionGroup
                    && x.Salary_Type == salaryType);
                if (!dataBefore.Any())
                    return null;
                var maxEffectiveMonth = dataBefore?.Max(x => x.Effective_Month);
                var salaryItemsQuery = dataBefore.Where(x => x.Effective_Month == maxEffectiveMonth).OrderBy(s => s.Seq);

                return await salaryItemsQuery
                    .GroupJoin(_repositoryAccessor.HRMS_Sal_Master_Detail.FindAll(x => x.Factory == factory && x.Employee_ID == employeeID),
                        x => x.Salary_Item,
                        y => y.Salary_Item,
                        (x, y) => new { HSIS = x, HSMD = y })
                    .SelectMany(x => x.HSMD.DefaultIfEmpty(), (x, y) => new { x.HSIS, HSMD = y })
                    .GroupJoin(_repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.SalaryItem, true),
                        x => x.HSIS.Salary_Item,
                        y => y.Code,
                        (x, y) => new { x.HSIS, x.HSMD, HBC = y })
                    .SelectMany(x => x.HBC.DefaultIfEmpty(), (x, y) => new { x.HSIS, x.HSMD, HBC = y })
                    .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.SalaryItem && x.Language_Code.ToLower() == language.ToLower(), true),
                        x => new { x.HBC.Type_Seq, x.HBC.Code },
                        y => new { y.Type_Seq, y.Code },
                        (x, y) => new { x.HSIS, x.HSMD, x.HBC, HBCL = y })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(), (x, y) => new { x.HSIS, x.HSMD, x.HBC, HBCL = y })

                    .Select(x => new SalaryAdjustmentMaintenance_SalaryItem
                    {
                        Salary_Item = x.HSIS.Salary_Item,
                        Salary_Item_Name = x.HBC.Code + (x.HBCL != null ? (" - " + x.HBCL.Code_Name) : (" - " + x.HBC.Code_Name)),
                        Amount = x.HSMD != null ? x.HSMD.Amount : 0
                    })
                    .ToListAsync();
            }
            else
            {
                var dataAfter = _repositoryAccessor.HRMS_Sal_Item_Settings.FindAll(x => x.Factory == factory && x.Salary_Type == salaryType && x.Permission_Group == permissionGroup)
                    .Join(_repositoryAccessor.HRMS_Emp_Personal.FindAll(true),
                        x => new { x.Factory, x.Permission_Group },
                        y => new { y.Factory, y.Permission_Group },
                        (x, y) => new { HSIS = x, HEP = y })
                    .Distinct();

                if (!dataAfter.Any())
                    return null;
                // Lấy tháng hiệu lực lớn nhất
                var maxEffective_Month_After = dataAfter.Max(x => x.HSIS.Effective_Month);

                // Lọc theo tháng hiệu lực lớn nhất
                var salaryItemsQuery = dataAfter.Where(x => x.HSIS.Effective_Month == maxEffective_Month_After)
                    .Select(x => x.HSIS.Salary_Item).Distinct();

                // Tiến hành các join với các bảng khác
                var result = await salaryItemsQuery
                    .GroupJoin(_repositoryAccessor.HRMS_Sal_Master_Detail.FindAll(x => x.Factory == factory && x.Employee_ID == employeeID),
                        x => x,
                        y => y.Salary_Item,
                        (x, y) => new { SalaryItem = x, HSMD = y })
                    .SelectMany(x => x.HSMD.DefaultIfEmpty(), (x, y) => new { x.SalaryItem, HSMD = y })
                    .GroupJoin(_repositoryAccessor.HRMS_Basic_Code.FindAll(b => b.Type_Seq == BasicCodeTypeConstant.SalaryItem, true),
                        x => x.SalaryItem,
                        y => y.Code,
                        (x, y) => new { x.SalaryItem, x.HSMD, HBC = y })
                    .SelectMany(x => x.HBC.DefaultIfEmpty(), (x, y) => new { x.SalaryItem, x.HSMD, HBC = y })
                    .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(c => c.Type_Seq == BasicCodeTypeConstant.SalaryItem && c.Language_Code.ToLower() == language.ToLower(), true),
                        x => new { x.HBC.Type_Seq, x.HBC.Code },
                        y => new { y.Type_Seq, y.Code },
                        (x, y) => new { x.SalaryItem, x.HSMD, x.HBC, HBCL = y })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(), (x, y) => new { x.SalaryItem, x.HSMD, x.HBC, HBCL = y })
                    .Select(x => new SalaryAdjustmentMaintenance_SalaryItem
                    {
                        Salary_Item = x.SalaryItem,
                        Salary_Item_Name = x.HBC != null ? (x.HBC.Code + (x.HBCL != null ? (" - " + x.HBCL.Code_Name) : (" - " + x.HBC.Code_Name))) : "",
                        Amount = x.HSMD != null ? x.HSMD.Amount : 0
                    })
                    .ToListAsync();

                return result;
            }
        }
        public async Task<CheckEffectiveDateResult> CheckEffectiveDate(string factory, string employee_ID, string inputEffectiveDate)
        {
            // Chuyển đổi chuỗi ngày nhập vào thành DateTime
            DateTime effectiveDate = Convert.ToDateTime(inputEffectiveDate);

            // Tìm Seq lớn nhất mà có ngày hiệu lực sau ngày nhập
            var listSeq = _repositoryAccessor.HRMS_Sal_History
                .FindAll(x => x.Factory == factory && x.Employee_ID == employee_ID && x.Effective_Date == effectiveDate)
                .Select(x => x.Seq);
            int maxSeq = listSeq.Any() ? listSeq.Max() : 0;
            // Kiểm tra số lượng bản ghi trong HRMS_Sal_History
            int afterCount = await _repositoryAccessor.HRMS_Sal_History
                .FindAll(x => x.Factory == factory && x.Employee_ID == employee_ID && x.Effective_Date > effectiveDate)
                .CountAsync();

            var result = new CheckEffectiveDateResult
            {
                MaxSeq = maxSeq + 1
            };
            // Nếu sau ngày hiệu lực đã nhập có bản ghi, thì trả về không hợp lệ và seq
            if (afterCount > 0)
            {
                result.CheckEffectiveDate = false;
                return result;
            }

            // Nếu tất cả đều đúng, trả về hợp lệ và seq
            result.CheckEffectiveDate = true;
            return result;
        }
        #endregion
        #region GetList
        public async Task<List<string>> GetListEmployeeID(string factory)
        {
            return await _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == factory && x.Employee_ID.Length <= 9, true).Select(x => x.Employee_ID).ToListAsync();
        }
        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string language, List<string> roleList)
        {
            var factoriesByAccount = await Queryt_Factory_AddList(roleList);
            var factories = await GetDataBasicCode(BasicCodeTypeConstant.Factory, language);

            return factories.IntersectBy(factoriesByAccount, x => x.Key).ToList();
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

        public async Task<List<KeyValuePair<string, string>>> GetListReason(string language)
        {
            return await GetDataBasicCode(BasicCodeTypeConstant.ReasonForChange, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListTechnicalType(string language)
        {
            return await GetDataBasicCode(BasicCodeTypeConstant.Technical_Type, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListExpertiseCategory(string language)
        {
            return await GetDataBasicCode(BasicCodeTypeConstant.Expertise_Category, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string language)
        {
            return await GetDataBasicCode(BasicCodeTypeConstant.PermissionGroup, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListSalaryType(string language)
        {
            return await GetDataBasicCode(BasicCodeTypeConstant.SalaryType, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListPositionTitle(string language)
        {
            return await GetDataBasicCode(BasicCodeTypeConstant.JobTitle, language);
        }

        public async Task<List<SalaryAdjustmentMaintenance_SalaryItem>> GetListSalaryItem(string history_GUID, string language)
        {
            var data = await _repositoryAccessor.HRMS_Sal_History_Detail.FindAll(x => x.History_GUID == history_GUID, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.SalaryItem && x.IsActive, true),
                    x => x.Salary_Item,
                    y => y.Code,
                    (x, y) => new { HSHD = x, HBC = y })
                .SelectMany(x => x.HBC.DefaultIfEmpty(), (x, y) => new { x.HSHD, HBC = y })
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    x => new { x.HBC.Type_Seq, x.HBC.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { x.HSHD, x.HBC, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(), (x, y) => new { x.HSHD, x.HBC, HBCL = y })

                .Select(x => new SalaryAdjustmentMaintenance_SalaryItem
                {
                    Salary_Item = x.HBC.Code,
                    Salary_Item_Name = x.HBC.Code + (x.HBCL != null ? (" - " + x.HBCL.Code_Name) : (" - " + x.HBC.Code_Name)),
                    Amount = x.HSHD != null ? x.HSHD.Amount : 0
                }
                ).ToListAsync();
            return data;
        }
        public async Task<List<KeyValuePair<string, string>>> GetListCurrency(string language)
        {
            return await GetDataBasicCode(BasicCodeTypeConstant.Currency, language);
        }

        public async Task<bool> CheckReasonForChange(string reasonForChange)
        {
            var data = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "60" && x.Code == reasonForChange, true).FirstOrDefaultAsync();
            return data != null;
        }
    }
    #endregion
}