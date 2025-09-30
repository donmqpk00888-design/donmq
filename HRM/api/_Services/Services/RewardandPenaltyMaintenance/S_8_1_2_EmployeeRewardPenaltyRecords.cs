using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using API._Repositories;
using API._Services.Interfaces.RewardandPenaltyMaintenance;
using API.Data;
using API.DTOs;
using API.DTOs.RewardandPenaltyMaintenance;
using API.Helper.Constant;
using API.Helper.Utilities;
using API.Models;
using Aspose.Cells;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.RewardandPenaltyMaintenance
{
  public class S_8_1_2_EmployeeRewardPenaltyRecords : BaseServices, I_8_1_2_EmployeeRewardPenaltyRecords
  {
    private readonly string folder = "uploaded\\RewardandPenaltyMaintenance\\8_1_2_EmployeeRewardPenaltyRecords";
    private static readonly SemaphoreSlim semaphore = new(1, 1);
    public S_8_1_2_EmployeeRewardPenaltyRecords(DBContext dbContext) : base(dbContext)
    {
    }
    #region Get data
    public async Task<PaginationUtility<D_8_1_2_EmployeeRewardPenaltyRecordsData>> GetSearch(PaginationParam paginationParams, D_8_1_2_EmployeeRewardPenaltyRecordsParam searchParam)
    {
      var data = await GetData(searchParam);
      return PaginationUtility<D_8_1_2_EmployeeRewardPenaltyRecordsData>.Create(data, paginationParams.PageNumber, paginationParams.PageSize);
    }

    private async Task<List<D_8_1_2_EmployeeRewardPenaltyRecordsData>> GetData(D_8_1_2_EmployeeRewardPenaltyRecordsParam param)
    {
      var result = new List<D_8_1_2_EmployeeRewardPenaltyRecordsData>();
      var predRewEmpRecords = PredicateBuilder.New<HRMS_Rew_EmpRecords>(x => x.Factory == param.Factory);
      var predHEP = PredicateBuilder.New<HRMS_Emp_Personal>(x => x.Factory == param.Factory);

      if (!string.IsNullOrWhiteSpace(param.Employee_ID))
        predRewEmpRecords.And(x => x.Employee_ID.Contains(param.Employee_ID));

      if (!string.IsNullOrWhiteSpace(param.Department))
        predHEP = predHEP.And(x => x.Department.Contains(param.Department));

      if (!string.IsNullOrWhiteSpace(param.Date_Start_Str))
        predRewEmpRecords.And(x => x.Reward_Date >= Convert.ToDateTime(param.Date_Start_Str));

      if (!string.IsNullOrWhiteSpace(param.Date_End_Str))
        predRewEmpRecords.And(x => x.Reward_Date <= Convert.ToDateTime(param.Date_End_Str));

      if (!string.IsNullOrWhiteSpace(param.Yearly_Month_Start_Str))
        predRewEmpRecords.And(x => x.Sal_Month >= Convert.ToDateTime(param.Yearly_Month_Start_Str));

      if (!string.IsNullOrWhiteSpace(param.Yearly_Month_End_Str))
        predRewEmpRecords.And(x => x.Sal_Month <= Convert.ToDateTime(param.Yearly_Month_End_Str));

      var HBC_Lang = IQuery_Code_Lang(param.Language);
      var HOD_Lang = IQuery_Department_Lang(param.Factory, param.Language);
      var HRR_Reason = _repositoryAccessor.HRMS_Rew_ReasonCode.FindAll(x => x.Factory == param.Factory, true)
          .Select(x => new
          {
            x.Code,
            Code_Name = $"{x.Code}-{x.Code_Name}"
          });
      var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predHEP)
          .Select(x => new
          {
            x.USER_GUID,
            x.Local_Full_Name,
            x.Work_Type,
            Division = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Division : x.Division,
            Factory = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Factory : x.Factory,
            Department_Code = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Department : x.Department,
          });
      var all_Data = _repositoryAccessor.HRMS_Rew_EmpRecords.FindAll(predRewEmpRecords);
      var data = await all_Data
          .Join(HEP,
              x => new { x.Factory, x.USER_GUID },
              y => new { y.Factory, y.USER_GUID },
              (x, y) => new { HRE = x, HEP = y })
          .GroupJoin(HOD_Lang,
              x => new { x.HEP.Department_Code, x.HEP.Factory, x.HEP.Division },
              y => new { y.Department_Code, y.Factory, y.Division },
              (x, y) => new { x.HRE, x.HEP, HOD_Lang = y })
          .SelectMany(x => x.HOD_Lang.DefaultIfEmpty(),
              (x, y) => new { x.HRE, x.HEP, HOD_Lang = y })
          .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.WorkType),
              x => x.HEP.Work_Type,
              y => y.Code,
              (x, y) => new { x.HRE, x.HEP, x.HOD_Lang, HBC_WorkType = y })
          .SelectMany(x => x.HBC_WorkType.DefaultIfEmpty(),
              (x, y) => new { x.HRE, x.HEP, x.HOD_Lang, HBC_WorkType = y })
          .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.RewardPenaltyType),
              x => x.HRE.Reward_Type,
              y => y.Code,
              (x, y) => new { x.HRE, x.HEP, x.HOD_Lang, x.HBC_WorkType, HBC_RewardPenaltyType = y })
          .SelectMany(x => x.HBC_RewardPenaltyType.DefaultIfEmpty(),
              (x, y) => new { x.HRE, x.HEP, x.HOD_Lang, x.HBC_WorkType, HBC_RewardPenaltyType = y })
          .GroupJoin(HRR_Reason,
              x => x.HRE.Reason_Code,
              y => y.Code,
              (x, y) => new { x.HRE, x.HEP, x.HOD_Lang, x.HBC_WorkType, x.HBC_RewardPenaltyType, HRR_Reason = y })
          .SelectMany(x => x.HRR_Reason.DefaultIfEmpty(),
              (x, y) => new { x.HRE, x.HEP, x.HOD_Lang, x.HBC_WorkType, x.HBC_RewardPenaltyType, HRR_Reason = y })
          .GroupBy(x => x.HRE)
          .Select(x => new D_8_1_2_EmployeeRewardPenaltyRecordsData
          {
            History_GUID = x.Key.History_GUID,
            Factory = x.Key.Factory,
            Division = x.FirstOrDefault().HEP.Division,
            Employee_ID = x.Key.Employee_ID,
            Local_Full_Name = x.FirstOrDefault().HEP.Local_Full_Name,
            Department_Code = x.FirstOrDefault().HEP.Department_Code,
            Department_Code_Name = x.FirstOrDefault(y => y.HOD_Lang.Department_Code != null).HOD_Lang.Department_Code_Name ?? x.FirstOrDefault().HEP.Department_Code,
            Work_Type = x.FirstOrDefault().HEP.Work_Type,
            Work_Type_Name = x.FirstOrDefault(y => y.HBC_WorkType.Code != null).HBC_WorkType.Code_Name_Str ?? x.FirstOrDefault().HEP.Work_Type,
            Reward_Date = x.Key.Reward_Date,
            Reward_Date_Str = x.Key.Reward_Date.ToString("yyyy/MM/dd"),
            Reward_Penalty_Type = x.Key.Reward_Type,
            Reward_Penalty_Type_Name = x.FirstOrDefault(y => y.HBC_RewardPenaltyType.Code != null).HBC_RewardPenaltyType.Code_Name_Str ?? x.Key.Reward_Type,
            Reason_Code = x.Key.Reason_Code,
            Reason_Code_Name = x.FirstOrDefault(y => y.HRR_Reason.Code != null).HRR_Reason.Code_Name ?? x.Key.Reason_Code,
            Yearly_Month = x.Key.Sal_Month,
            Yearly_Month_Str = x.Key.Sal_Month.Value.ToString("yyyy/MM"),
            Counts_of = x.Key.Reward_Times,
            Update_By = x.Key.Update_By,
            Update_Time = x.Key.Update_Time.ToString("yyyy/MM/dd HH:mm:ss"),
          })
          .ToListAsync();
      return data;
    }
    #endregion
    #region Query data Detail
    public async Task<D_8_1_2_EmployeeRewardPenaltyRecordsSubParam> Data_Detail(string History_GUID, string Language)
    {
      var HRE = _repositoryAccessor.HRMS_Rew_EmpRecords.FindAll(x => x.History_GUID == History_GUID, true);
      var HBC_Lang = IQuery_Code_Lang(Language);
      var HOD_Lang = _repositoryAccessor.HRMS_Org_Department.FindAll()
          .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == Language.ToLower()),
            x => x.Department_Code,
            y => y.Department_Code,
            (x, y) => new { HOD = x, HODL = y })
          .SelectMany(x => x.HODL.DefaultIfEmpty(),
            (x, y) => new { x.HOD, HODL = y })
          .Select(x => new DepartmentInfo
          {
            Factory = x.HOD.Factory,
            Division = x.HOD.Division,
            Department_Code = x.HOD.Department_Code,
            Department_Name = x.HOD != null ? x.HODL != null ? x.HODL.Name : x.HOD.Department_Name : null,
            Department_Code_Name = x.HOD != null ? $"{x.HOD.Department_Code}-{(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}" : null
          });
      var HRR_Reason = _repositoryAccessor.HRMS_Rew_ReasonCode.FindAll()
          .Select(x => new
          {
            x.Factory,
            x.Code,
            Code_Name = $"{x.Code}-{x.Code_Name}"
          });
      var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll()
          .Select(x => new
          {
            x.USER_GUID,
            x.Local_Full_Name,
            x.Work_Type,
            Division = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Division : x.Division,
            Factory = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Factory : x.Factory,
            Department_Code = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Department : x.Department,
          });

      var HEF = _repositoryAccessor.HRMS_Emp_File.FindAll(x => x.Program_Code == "8.1.2");
      var data = await HRE
          .Join(HEP,
              x => new { x.Factory, x.USER_GUID },
              y => new { y.Factory, y.USER_GUID },
              (x, y) => new { HRE = x, HEP = y })
          .GroupJoin(HOD_Lang,
              x => new { x.HEP.Department_Code, x.HEP.Factory, x.HEP.Division },
              y => new { y.Department_Code, y.Factory, y.Division },
              (x, y) => new { x.HRE, x.HEP, HOD_Lang = y })
          .SelectMany(x => x.HOD_Lang.DefaultIfEmpty(),
              (x, y) => new { x.HRE, x.HEP, HOD_Lang = y })
          .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.WorkType),
              x => x.HEP.Work_Type,
              y => y.Code,
              (x, y) => new { x.HRE, x.HEP, x.HOD_Lang, HBC_WorkType = y })
          .SelectMany(x => x.HBC_WorkType.DefaultIfEmpty(),
              (x, y) => new { x.HRE, x.HEP, x.HOD_Lang, HBC_WorkType = y })
          .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.RewardPenaltyType),
              x => x.HRE.Reward_Type,
              y => y.Code,
              (x, y) => new { x.HRE, x.HEP, x.HOD_Lang, x.HBC_WorkType, HBC_RewardPenaltyType = y })
          .SelectMany(x => x.HBC_RewardPenaltyType.DefaultIfEmpty(),
              (x, y) => new { x.HRE, x.HEP, x.HOD_Lang, x.HBC_WorkType, HBC_RewardPenaltyType = y })
          .GroupJoin(HRR_Reason,
              x => x.HRE.Reason_Code,
              y => y.Code,
              (x, y) => new { x.HRE, x.HEP, x.HOD_Lang, x.HBC_WorkType, x.HBC_RewardPenaltyType, HRR_Reason = y })
          .SelectMany(x => x.HRR_Reason.DefaultIfEmpty(),
              (x, y) => new { x.HRE, x.HEP, x.HOD_Lang, x.HBC_WorkType, x.HBC_RewardPenaltyType, HRR_Reason = y })
          .GroupJoin(HEF,
              x => new { x.HEP.Division, x.HRE.Factory, x.HRE.SerNum },
              y => new { y.Division, y.Factory, y.SerNum },
              (x, y) => new { x.HRE, x.HEP, x.HOD_Lang, x.HBC_WorkType, x.HBC_RewardPenaltyType, x.HRR_Reason, HEF = y })
          .SelectMany(x => x.HEF.DefaultIfEmpty(),
              (x, y) => new { x.HRE, x.HEP, x.HOD_Lang, x.HBC_WorkType, x.HBC_RewardPenaltyType, x.HRR_Reason, HEF = y })
          .GroupBy(x => x.HRE)
          .Select(x => new D_8_1_2_EmployeeRewardPenaltyRecordsSubParam
          {
            History_GUID = x.Key.History_GUID,
            USER_GUID = x.Key.USER_GUID,
            Factory = x.Key.Factory,
            Employee_ID = x.Key.Employee_ID,
            Local_Full_Name = x.FirstOrDefault().HEP.Local_Full_Name,
            Division = x.FirstOrDefault().HEP.Division,
            Department_Code = x.FirstOrDefault().HEP.Department_Code,
            Department_Code_Name = x.FirstOrDefault(y => y.HOD_Lang.Department_Code != null).HOD_Lang.Department_Code_Name ?? x.FirstOrDefault().HEP.Department_Code,
            Work_Type = x.FirstOrDefault().HEP.Work_Type,
            Work_Type_Name = x.FirstOrDefault(y => y.HBC_WorkType.Code != null).HBC_WorkType.Code_Name_Str ?? x.FirstOrDefault().HEP.Work_Type,
            Reward_Date = x.Key.Reward_Date,
            Reward_Date_Str = x.Key.Reward_Date.ToString("yyyy/MM/dd"),
            Reward_Penalty_Type = x.Key.Reward_Type,
            Reward_Penalty_Type_Name = x.FirstOrDefault(y => y.HBC_RewardPenaltyType.Code != null).HBC_RewardPenaltyType.Code_Name_Str ?? x.Key.Reward_Type,
            Remark = x.Key.Remark,
            Reason_Code = x.Key.Reason_Code,
            Reason_Code_Name = x.FirstOrDefault(y => y.HRR_Reason.Code != null).HRR_Reason.Code_Name ?? x.Key.Reason_Code,
            Yearly_Month = x.Key.Sal_Month,
            Yearly_Month_Str = x.Key.Sal_Month.Value.ToString("yyyy/MM"),
            Counts_of = x.Key.Reward_Times,
            SerNum = x.Key.SerNum,
            Update_By = x.Key.Update_By,
            Update_Time = x.Key.Update_Time.ToString("yyyy/MM/dd HH:mm:ss"),
            File_List = x.Where(y => y.HEF != null).GroupBy(y => y.HEF)
                .Select(y => new EmployeeRewardPenaltyRecordsReportFileModel
                {
                  Id = y.Key.FileID,
                  Name = y.Key.FileName,
                  Size = Convert.ToInt32(y.Key.FileSize)
                }).ToList()
          }).FirstOrDefaultAsync();
      return data;
    }
    #endregion
    #region DownloadTemplate
    public async Task<OperationResult> DownloadTemplate()
    {
      string path = Path.Combine(
          Directory.GetCurrentDirectory(),
          "Resources\\Template\\RewardandPenaltyMaintenance\\8_1_2_EmployeeRewardPenaltyRecords\\Template.xlsx"
      );
      if (!File.Exists(path))
        return await Task.FromResult(new OperationResult(false, "NotExitedFile"));
      byte[] bytes = File.ReadAllBytes(path);
      return await Task.FromResult(new OperationResult { IsSuccess = true, Data = $"data:xlsx;base64,{Convert.ToBase64String(bytes)}" });
    }
    #endregion
    #region Download Attachment
    public async Task<OperationResult> DownloadFile(EmployeeRewardPenaltyRecordsReportDownloadFileModel param)
    {
      var getDivision = await _repositoryAccessor.HRMS_Emp_Personal.FirstOrDefaultAsync(x => x.Factory == param.Factory && x.Employee_ID == param.Employee_Id);
      var file = await _repositoryAccessor.HRMS_Emp_File.FirstOrDefaultAsync(x =>
          x.Division == getDivision.Division &&
         x.Factory == param.Factory &&
         x.SerNum == param.SerNum &&
         x.FileName == param.File_Name &&
         x.Program_Code == "8.1.2"
     );
      if (file == null)
        return new OperationResult(false, "NotExitedData");
      string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
      string path = $"{webRootPath}\\{folder}\\{param.Factory}\\{getDivision.Division}\\{param.Employee_Id}\\{param.SerNum}\\{file.FileName}";

      if (!File.Exists(path))
        return new OperationResult(false, "NotExitedFile");
      byte[] bytes = File.ReadAllBytes(path);
      var fileNameArray = file.FileName.Split(".");
      EmployeeRewardPenaltyRecordsReportFileModel fileData = new()
      {
        Name = file.FileName,
        Content = $"data:{fileNameArray[^1]};base64,{Convert.ToBase64String(bytes)}"
      };
      return new OperationResult(true, fileData);
    }
    #endregion
    #region UploadFileExcel
    public async Task<OperationResult> UploadFileExcel(IFormFile file, List<string> role_List, string userName)
    {
      await semaphore.WaitAsync();
      await _repositoryAccessor.BeginTransactionAsync();
      try
      {
        ExcelResult resp = ExcelUtility.CheckExcel(
            file,
            "Resources\\Template\\RewardandPenaltyMaintenance\\8_1_2_EmployeeRewardPenaltyRecords\\Template.xlsx"
        );
        if (!resp.IsSuccess)
          return new OperationResult(false, resp.Error);

        List<HRMS_Rew_EmpRecords> addData = new();
        List<D_8_1_2_EmployeeRewardPenaltyRecordsReport> excelReportList = new();
        List<string> roleFactories = await _repositoryAccessor.HRMS_Basic_Role
            .FindAll(x => role_List.Contains(x.Role))
            .Select(x => x.Factory).Distinct()
            .ToListAsync();

        if (!roleFactories.Any())
          return new OperationResult(false, "Recent account roles do not have any factory.");

        var HBC_Factory = (await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "2").ToListAsync()).ToDictionary(x => x.Code);
        var HBC_Reward_Type = (await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "66").ToListAsync()).ToDictionary(x => x.Code);
        var HEP = _repositoryAccessor.HRMS_Emp_Personal.FindAll(true).ToList();
        var HRR = _repositoryAccessor.HRMS_Rew_ReasonCode.FindAll().ToList();

        bool isPassed = true;

        string serNumStart = DateTime.Now.ToString("yyyyMMdd");
        string serNum = "";
        var recentFiles = _repositoryAccessor.HRMS_Rew_EmpRecords
          .FindAll(x => x.Program_Code == "8.1.2" && x.SerNum.StartsWith(serNumStart))
          .OrderByDescending(x => x.SerNum);
        if (recentFiles.Any())
          serNum = recentFiles.FirstOrDefault().SerNum;
        serNum = string.IsNullOrWhiteSpace(serNum) ? serNumStart + "0000001" : serNum[0..8] + (int.Parse(serNum[9..]) + 1).ToString("0000000");
        for (int i = resp.WsTemp.Cells.Rows.Count; i < resp.Ws.Cells.Rows.Count; i++)
        {
          var History_GUID = Guid.NewGuid().ToString();
          string errorMessage = "";

          string factory = resp.Ws.Cells[i, 0].StringValue?.Trim();
          string employeeID = resp.Ws.Cells[i, 1].StringValue?.Trim();
          string reward_Date = resp.Ws.Cells[i, 2].StringValue?.Trim();
          string reward_Penalty_Type = resp.Ws.Cells[i, 3].StringValue?.Trim();
          string reason_Code = resp.Ws.Cells[i, 4].StringValue?.Trim();
          string yearly_Month = resp.Ws.Cells[i, 5].StringValue?.Trim();
          string counts_of = resp.Ws.Cells[i, 6].StringValue?.Trim();
          string remark = resp.Ws.Cells[i, 7].StringValue?.Trim();

          if (string.IsNullOrWhiteSpace(factory))
            errorMessage += "Factory is not valid.\n";
          else
          {
            if (!HBC_Factory.ContainsKey(factory))
              errorMessage += "Factory Code does not exist.\n";
            if (!roleFactories.Contains(factory))
              errorMessage += "Uploaded [Factory] data does not match the role group.\n";
          }

          if (string.IsNullOrWhiteSpace(employeeID))
            errorMessage += $"Employee ID is not valid.\n";
          else
          {
            if (!string.IsNullOrWhiteSpace(factory) && !HEP.Any(x => x.Employee_ID == employeeID && x.Factory == factory))
              errorMessage += $"Employee ID does not exist.\n";
          }
          if (string.IsNullOrWhiteSpace(reward_Date))
            errorMessage += $"Reward Date is not valid.\n";
          if (!DateTime.TryParseExact(reward_Date, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime reward_Date_value))
            errorMessage += "Reward Date is invalid date format (yyyy/MM/dd).\n";

          if (string.IsNullOrWhiteSpace(reward_Penalty_Type))
            errorMessage += $"Reward/Penalty Type is not valid.\n";
          else
          {
            if (!HBC_Reward_Type.ContainsKey(reward_Penalty_Type))
              errorMessage += $"Reward/Penalty Type is not valid.\n";
          }

          if (string.IsNullOrWhiteSpace(reason_Code))
            errorMessage += $"Reason Code is not valid.\n";
          else
          {
            if (!string.IsNullOrWhiteSpace(factory) && !HRR.Any(x => x.Code == reason_Code && x.Factory == factory))
              errorMessage += $"Reason Code or Factory is not valid.\n";
          }
          var validYearMonth = DateTime.TryParseExact(yearly_Month, "yyyy/MM", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime yearly_Month_Value);
          if (!string.IsNullOrEmpty(yearly_Month))
          {
            if (!validYearMonth)
            {
              errorMessage += "Yearly Month is invalid date format (yyyy/MM).\n";
            }
          }

          var USER_GUID = HEP.FirstOrDefault(x => x.Employee_ID == employeeID && x.Factory == factory)?.USER_GUID;
          var counts_of_value = short.TryParse(counts_of, out var tmp) ? tmp : (short)1;

          if (string.IsNullOrWhiteSpace(errorMessage))
          {
            var newData = new HRMS_Rew_EmpRecords
            {
              History_GUID = History_GUID,
              USER_GUID = USER_GUID,
              Factory = factory,
              Employee_ID = employeeID,
              Reward_Date = reward_Date_value,
              Reward_Type = reward_Penalty_Type,
              Reason_Code = reason_Code,
              Sal_Month = !string.IsNullOrWhiteSpace(yearly_Month) ? yearly_Month_Value : null,
              Reward_Times = counts_of_value,
              SerNum = serNum,
              Remark = !string.IsNullOrWhiteSpace(remark) ? remark : null,
              Program_Code = "8.1.2",
              Update_Time = DateTime.Now,
              Update_By = userName,
            };
            serNum = serNum[0..8] + (int.Parse(serNum[9..]) + 1).ToString("0000000");
            addData.Add(newData);
          }
          else
          {
            isPassed = false;
            errorMessage = errorMessage.Remove(errorMessage.Length - 1);
          }
          D_8_1_2_EmployeeRewardPenaltyRecordsReport report = new()
          {
            Factory = factory,
            Employee_ID = employeeID,
            Date = reward_Date,
            Reward_Penalty_Type = reward_Penalty_Type,
            Reason_Code = reason_Code,
            Yearly_Month = yearly_Month,
            Counts_of = counts_of_value,
            Remark = remark,
            IsCorrect = string.IsNullOrEmpty(errorMessage) ? "Y" : "N",
            Error_Message = string.Join("\r\n", errorMessage)
          };
          excelReportList.Add(report);
        }
        if (!isPassed)
        {
          MemoryStream memoryStream = new();
          string fileLocation = Path.Combine(
              Directory.GetCurrentDirectory(),
              "Resources\\Template\\RewardandPenaltyMaintenance\\8_1_2_EmployeeRewardPenaltyRecords\\Report.xlsx"
          );
          WorkbookDesigner workbookDesigner = new() { Workbook = new Workbook(fileLocation) };
          Worksheet worksheet = workbookDesigner.Workbook.Worksheets[0];
          workbookDesigner.SetDataSource("result", excelReportList);
          workbookDesigner.Process();
          worksheet.AutoFitColumns(worksheet.Cells.MinDataColumn, worksheet.Cells.MaxColumn);
          worksheet.AutoFitRows(worksheet.Cells.MinDataRow + 1, worksheet.Cells.MaxRow);
          workbookDesigner.Workbook.Save(memoryStream, SaveFormat.Xlsx);
          return new OperationResult { IsSuccess = false, Data = memoryStream.ToArray(), Error = "Please check Error Report" };
        }
        _repositoryAccessor.HRMS_Rew_EmpRecords.AddMultiple(addData);
        await _repositoryAccessor.Save();
        await _repositoryAccessor.CommitAsync();
        string folder = "uploaded\\excels\\RewardandPenaltyMaintenance\\8_1_2_EmployeeRewardPenaltyRecords\\Creates";
        await FilesUtility.SaveFile(file, folder, $"EmployeeLunchBreakTimeSetting_{DateTime.Now:yyyyMMddHHmmss}");
        return new OperationResult(true, "Upload data successfully!");
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
    #endregion
    #region Create
    public async Task<OperationResult> Create(D_8_1_2_EmployeeRewardPenaltyRecordsSubParam data, string userName)
    {
      await _repositoryAccessor.BeginTransactionAsync();
      try
      {
        string serNumStart = DateTime.Now.ToString("yyyyMMdd");
        string serNum = "";
        var recentFiles = _repositoryAccessor.HRMS_Rew_EmpRecords
          .FindAll(x => x.Program_Code == "8.1.2" && x.SerNum.StartsWith(serNumStart))
          .OrderByDescending(x => x.SerNum);
        if (recentFiles.Any())
          serNum = recentFiles.FirstOrDefault().SerNum;
        serNum = string.IsNullOrWhiteSpace(serNum) ? serNumStart + "0000001" : serNum[0..8] + (int.Parse(serNum[9..]) + 1).ToString("0000000");
        var dataNew = new HRMS_Rew_EmpRecords
        {
          History_GUID = Guid.NewGuid().ToString(),
          USER_GUID = data.USER_GUID,
          Factory = data.Factory,
          Employee_ID = data.Employee_ID,
          Reward_Date = Convert.ToDateTime(data.Reward_Date_Str),
          Reward_Type = data.Reward_Penalty_Type,
          Reason_Code = data.Reason_Code,
          Sal_Month = data.Yearly_Month_Str != null ? Convert.ToDateTime(data.Yearly_Month_Str) : null,
          Reward_Times = data.Counts_of == 0 ? short.Parse("1") : data.Counts_of,
          Remark = data.Remark,
          Program_Code = "8.1.2",
          SerNum = serNum,
          Update_By = data.Update_By,
          Update_Time = Convert.ToDateTime(data.Update_Time),
        };
        List<HRMS_Emp_File> addFiles = new();
        string path = $"{folder}\\{data.Factory}\\{data.Division}\\{data.Employee_ID}\\{dataNew.SerNum}";
        foreach (EmployeeRewardPenaltyRecordsReportFileModel file in data.File_List)
        {
          var fileNameArray = file.Name.Split(".");
          string savedFileName = await FilesUtility.UploadAsync(file.Content, path, fileNameArray[0], fileNameArray[^1]);
          if (string.IsNullOrWhiteSpace(savedFileName))
            return new OperationResult(false, "SaveFileError");
          HRMS_Emp_File addFile = new()
          {
            Division = data.Division,
            Factory = dataNew.Factory,
            Program_Code = dataNew.Program_Code,
            SerNum = dataNew.SerNum,
            FileID = file.Id,
            FileName = savedFileName,
            FileSize = file.Size,
            Update_By = dataNew.Update_By,
            Update_Time = dataNew.Update_Time
          };
          addFiles.Add(addFile);
        }
        _repositoryAccessor.HRMS_Rew_EmpRecords.Add(dataNew);
        if (addFiles.Any())
          _repositoryAccessor.HRMS_Emp_File.AddMultiple(addFiles);

        await _repositoryAccessor.Save();
        await _repositoryAccessor.CommitAsync();
        return new OperationResult(true, "System.Message.CreateOKMsg");
      }
      catch (Exception)
      {
        await _repositoryAccessor.RollbackAsync();
        return new OperationResult(false, "System.Message.CreateErrorMsg");
      }
    }
    #endregion
    #region Delete
    public async Task<OperationResult> Delete(D_8_1_2_EmployeeRewardPenaltyRecordsData data)
    {
      await _repositoryAccessor.BeginTransactionAsync();
      try
      {
        var existingData = await _repositoryAccessor.HRMS_Rew_EmpRecords
            .FirstOrDefaultAsync(x => x.History_GUID == data.History_GUID);
        if (existingData == null)
          return new OperationResult(false, "Data is not exist");
        var attData = await _repositoryAccessor.HRMS_Emp_File
          .FindAll(x => x.Division == data.Division &&
            x.Factory == existingData.Factory &&
            x.Program_Code == existingData.Program_Code &&
            x.SerNum == existingData.SerNum)
          .ToListAsync();
        if (attData.Any())
        {
          _repositoryAccessor.HRMS_Emp_File.RemoveMultiple(attData);
          string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
          string path = $"{webRootPath}\\{folder}\\{existingData.Factory}\\{data.Division}\\{existingData.Employee_ID}\\{existingData.SerNum}";
          if (Directory.Exists(path))
            Directory.Delete(path, true);
        }
        _repositoryAccessor.HRMS_Rew_EmpRecords.Remove(existingData);
        await _repositoryAccessor.Save();
        await _repositoryAccessor.CommitAsync();
        return new OperationResult(true, "Delete Successfully");
      }
      catch
      {
        await _repositoryAccessor.RollbackAsync();
        return new OperationResult(false, "Delete failed");
      }
    }
    #endregion
    #region Update
    public async Task<OperationResult> Update(D_8_1_2_EmployeeRewardPenaltyRecordsSubParam data, string userName)
    {
      await _repositoryAccessor.BeginTransactionAsync();
      try
      {
        var HRE = await _repositoryAccessor.HRMS_Rew_EmpRecords
            .FirstOrDefaultAsync(x => x.History_GUID == data.History_GUID);
        if (HRE == null)
          return new OperationResult(false, "NotExitedData");
        HRE.Reason_Code = data.Reason_Code;
        HRE.Sal_Month = !string.IsNullOrWhiteSpace(data.Yearly_Month_Str) ? Convert.ToDateTime(data.Yearly_Month_Str) : null;
        HRE.Reward_Times = data.Counts_of <= 0 ? (short)1 : data.Counts_of;
        HRE.Remark = data.Remark;
        HRE.Update_By = userName;
        HRE.Update_Time = DateTime.Now;
        string location = $"{folder}\\{HRE.Factory}\\{data.Division}\\{HRE.Employee_ID}\\{HRE.SerNum}";
        var oldHEFs = await _repositoryAccessor.HRMS_Emp_File
            .FindAll(x =>
                x.Division == data.Division &&
                x.Factory == HRE.Factory &&
                x.SerNum == HRE.SerNum &&
                x.Program_Code == "8.1.2"
            ).ToListAsync();
        string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        if (!data.File_List.Any())
        {
          string path = $"{webRootPath}\\{location}";
          if (Directory.Exists(path))
            Directory.Delete(path, true);
        } else {
          
          var removeList = oldHEFs.Where(x => !data.File_List.Any(n => x.FileName == n.Name)).ToList();
          if (removeList.Any())
          {
            foreach (var item in removeList)
            {
              string path = $"{webRootPath}\\{location}\\{item.FileName}";
              if (File.Exists(path))
                File.Delete(path);
            }
          }
        }
        List<HRMS_Emp_File> newHREs = new();
        foreach (var file in data.File_List)
        {
          HRMS_Emp_File fileAdd = new()
          {
            Division = data.Division,
            Factory = HRE.Factory,
            Program_Code = "8.1.2",
            SerNum = HRE.SerNum,
            FileID = file.Id,
            FileName = file.Name,
            FileSize = file.Size,
            Update_By = HRE.Update_By,
            Update_Time = HRE.Update_Time
          };
          if (!string.IsNullOrWhiteSpace(file.Content))
          {
            var fileNameArray = file.Name.Split(".");
            string savedFileName = await FilesUtility.UploadAsync(file.Content, location, fileNameArray[0], fileNameArray[^1]);
            if (string.IsNullOrWhiteSpace(savedFileName))
              return new OperationResult(false, "SaveFileError");
            fileAdd.FileName = savedFileName;
          }
          newHREs.Add(fileAdd);
        }
        _repositoryAccessor.HRMS_Rew_EmpRecords.Update(HRE);
        if (oldHEFs.Any())
          _repositoryAccessor.HRMS_Emp_File.RemoveMultiple(oldHEFs);
        await _repositoryAccessor.Save();
        if (newHREs.Any())
          _repositoryAccessor.HRMS_Emp_File.AddMultiple(newHREs);
        await _repositoryAccessor.Save();
        await _repositoryAccessor.CommitAsync();
        return new OperationResult(true);
      }
      catch (Exception)
      {
        await _repositoryAccessor.RollbackAsync();
        return new OperationResult(false, "ErrorException");
      }
    }
    #endregion
    #region GetList
    public async Task<List<KeyValuePair<string, string>>> GetListFactory(string userName, string language)
    {
      var factories = await Queryt_Factory_AddList(userName);
      var factoriesWithLanguage = await _repositoryAccessor.HRMS_Basic_Code
          .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory && factories.Contains(x.Code), true)
          .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code == language, true),
              x => new { x.Type_Seq, x.Code },
              y => new { y.Type_Seq, y.Code },
              (HBC, HBCL) => new { HBC, HBCL })
          .SelectMany(x => x.HBCL.DefaultIfEmpty(),
              (x, y) => new { x.HBC, HBCL = y })
          .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}")).ToListAsync();
      return factoriesWithLanguage;
    }
    public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string language, string factory)
    {
      var result = await _repositoryAccessor.HRMS_Org_Department
          .FindAll(x => x.Factory == factory, true)
          .Join(_repositoryAccessor.HRMS_Basic_Factory_Comparison
          .FindAll(b => b.Kind == "1" && b.Factory == factory, true),
              x => x.Division,
              y => y.Division,
              (x, y) => x)
          .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language
          .FindAll(x => x.Language_Code == language, true),
              x => new { x.Factory, x.Department_Code },
              y => new { y.Factory, y.Department_Code },
              (x, y) => new { HOD = x, HODL = y })
          .SelectMany(
              x => x.HODL.DefaultIfEmpty(),
              (x, y) => new { x.HOD, HODL = y })
          .OrderBy(x => x.HOD.Department_Code)
          .Select(
              x => new KeyValuePair<string, string>(
                  x.HOD.Department_Code,
                  x.HOD.Department_Code.Trim() + " - " + (x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)
              )
          ).Distinct().ToListAsync();
      return result;
    }
    public async Task<List<KeyValuePair<string, string>>> GetListRewardType(string Language)
    {
      return await GetDataBasicCode(BasicCodeTypeConstant.RewardPenaltyType, Language);
    }
    public async Task<List<KeyValuePair<string, string>>> GetListReasonCode(string Factory)
    {
      var HRC = _repositoryAccessor.HRMS_Rew_ReasonCode.FindAll(x => x.Factory == Factory, true);
      var result = await HRC.Select(x => new KeyValuePair<string, string>(x.Code, $"{x.Code} - {(x.Code_Name)}")).ToListAsync();
      return result;
    }
    public async Task<List<EmployeeCommonInfo>> GetEmployeeList(D_8_1_2_EmployeeRewardPenaltyRecordsParam param)
    {
      var predicateHEP = PredicateBuilder.New<HRMS_Emp_Personal>(true);
      if (!string.IsNullOrWhiteSpace(param.Factory))
        predicateHEP.And(x => x.Factory == param.Factory || x.Assigned_Factory == param.Factory);
      if (!string.IsNullOrWhiteSpace(param.Employee_ID))
        predicateHEP.And(x => x.Employee_ID.Contains(param.Employee_ID) || x.Assigned_Employee_ID.Contains(param.Employee_ID));

      var HEP_info = _repositoryAccessor.HRMS_Emp_Personal.FindAll(predicateHEP).Select(x => new
      {
        HEP = x,
        Actual_Employee_ID = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Employee_ID : x.Employee_ID,
        Actual_Division = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Division : x.Division,
        Actual_Factory = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Factory : x.Factory,
        Actual_Department = x.Employment_Status == "A" || x.Employment_Status == "S" ? x.Assigned_Department : x.Department,
      });
      var HBFC = _repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(x => x.Kind == "1");
      var HOD = _repositoryAccessor.HRMS_Org_Department.FindAll();
      var HBC_Lang = IQuery_Code_Lang(param.Language);
      var HODL = _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower());
      var data = await HEP_info
          .Join(HBFC,
              last => new { last.HEP.Division, last.HEP.Factory },
              HBFC => new { HBFC.Division, HBFC.Factory },
              (x, y) => new { HEP_info = x })
          .GroupJoin(HOD,
              last => new { Division = last.HEP_info.Actual_Division, Factory = last.HEP_info.Actual_Factory, Department_Code = last.HEP_info.Actual_Department },
              HOD => new { HOD.Division, HOD.Factory, HOD.Department_Code },
              (x, y) => new { x.HEP_info, HOD = y })
          .SelectMany(
              x => x.HOD.DefaultIfEmpty(),
              (x, y) => new { x.HEP_info, HOD = y })
          .GroupJoin(HODL,
              last => new { Division = last.HEP_info.Actual_Division, Factory = last.HEP_info.Actual_Factory, Department_Code = last.HEP_info.Actual_Department },
              HODL => new { HODL.Division, HODL.Factory, HODL.Department_Code },
              (x, y) => new { x.HEP_info, x.HOD, HODL = y })
          .SelectMany(
              x => x.HODL.DefaultIfEmpty(),
              (x, y) => new { x.HEP_info, x.HOD, HODL = y })
          .GroupJoin(HBC_Lang.Where(x => x.Type_Seq == BasicCodeTypeConstant.WorkType),
              x => x.HEP_info.HEP.Work_Type,
              y => y.Code,
              (x, y) => new { x.HEP_info, x.HOD, x.HODL, HBC_WorkType = y })
          .SelectMany(x => x.HBC_WorkType.DefaultIfEmpty(),
              (x, y) => new { x.HEP_info, x.HOD, x.HODL, HBC_WorkType = y })
          .Distinct()
          .ToListAsync();
      if (!data.Any())
        return new List<EmployeeCommonInfo>();
      var result = data.Select(x =>
      {
        string department_Name = x.HODL?.Name ?? x.HOD?.Department_Name ?? "";
        return new EmployeeCommonInfo()
        {
          USER_GUID = x.HEP_info.HEP.USER_GUID,
          Employee_ID = x.HEP_info.HEP.Employee_ID,
          Factory = x.HEP_info.HEP.Factory,
          Division = x.HEP_info.HEP.Division,
          Local_Full_Name = x.HEP_info.HEP.Local_Full_Name,
          Work_Type_Name = x.HBC_WorkType.Code_Name_Str ?? x.HEP_info.HEP.Work_Type,
          Work_Shift_Type = x.HBC_WorkType.Code_Name_Str ?? x.HEP_info.HEP.Work_Type,
          Actual_Employee_ID = x.HEP_info.Actual_Employee_ID,
          Actual_Factory = x.HEP_info.Actual_Factory,
          Actual_Division = x.HEP_info.Actual_Division,
          Actual_Department_Code = x.HEP_info.Actual_Department,
          Actual_Department_Name = department_Name,
          Actual_Department_Code_Name = !string.IsNullOrWhiteSpace(department_Name)
                      ? $"{x.HEP_info.Actual_Department}-{department_Name}"
                      : x.HEP_info.Actual_Department
        };
      }).OrderBy(x => x.Employee_ID).ToList();
      return result;
    }
    #endregion
  }
}
