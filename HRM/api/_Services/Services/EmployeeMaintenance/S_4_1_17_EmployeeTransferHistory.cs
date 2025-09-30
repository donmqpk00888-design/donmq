using API.Data;
using API._Services.Interfaces.EmployeeMaintenance;
using API.DTOs.EmployeeMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.EmployeeMaintenance
{
    public class S_4_1_17_EmployeeTransferHistory : BaseServices, I_4_1_17_EmployeeTransferHistory
    {
        public S_4_1_17_EmployeeTransferHistory(DBContext dbContext) : base(dbContext) { }

        public async Task<OperationResult> Create(EmployeeTransferHistoryDTO dto)
        {
            if (await _repositoryAccessor.HRMS_Emp_Transfer_History.AnyAsync(x => x.USER_GUID == dto.USER_GUID
            && x.Effective_Date == dto.Effective_Date && x.Seq == dto.Seq))
                return new OperationResult(false, "System.Message.DataExisted");

            var history_GUID = Guid.NewGuid().ToString();
            while (await _repositoryAccessor.HRMS_Emp_Transfer_History.AnyAsync(x => x.History_GUID == dto.History_GUID))
            {
                history_GUID = Guid.NewGuid().ToString();
            }

            // Tạo lịch sử di chuyển nhân viên sang nơi làm việc khác  [EffectStatus default = N]
            var dataNew = new HRMS_Emp_Transfer_History
            {
                History_GUID = history_GUID,
                USER_GUID = dto.USER_GUID,
                Reason_for_Change = dto.Reason_for_Change,
                Effective_Date = Convert.ToDateTime(dto.Effective_Date_Str),
                Effective_Status = dto.Effective_Status,
                Nationality_Before = dto.Nationality_Before,
                Identification_Number_Before = dto.Identification_Number_Before,
                Division_Before = dto.Division_Before,
                Factory_Before = dto.Factory_Before,
                Employee_ID_Before = dto.Employee_ID_Before,
                Department_Before = dto.Department_Before,
                Assigned_Division_Before = dto.Assigned_Division_Before,
                Assigned_Factory_Before = dto.Assigned_Factory_Before,
                Assigned_Employee_ID_Before = dto.Assigned_Employee_ID_Before,
                Assigned_Department_Before = dto.Assigned_Department_Before,
                Position_Grade_Before = dto.Position_Grade_Before,
                Position_Title_Before = dto.Position_Title_Before,
                Work_Type_Before = dto.Work_Type_Before,
                Nationality_After = dto.Nationality_After,
                Identification_Number_After = dto.Identification_Number_After,
                Division_After = dto.Division_After,
                Factory_After = dto.Factory_After,
                Employee_ID_After = dto.Employee_ID_After,
                Department_After = dto.Department_After,
                Assigned_Division_After = dto.Assigned_Division_After,
                Assigned_Factory_After = dto.Assigned_Factory_After,
                Assigned_Employee_ID_After = dto.Assigned_Employee_ID_After,
                Assigned_Department_After = dto.Assigned_Department_After,
                Position_Grade_After = dto.Position_Grade_After,
                Position_Title_After = dto.Position_Title_After,
                Work_Type_After = dto.Work_Type_After,
                Update_By = dto.Update_By,
                Update_Time = dto.Update_Time,
                Seq = dto.Seq,
                Data_Source = dto.Data_Source,

                ActingPosition_Star_Before = string.IsNullOrWhiteSpace(dto.ActingPosition_Start_Before_Str)
                    ? null : Convert.ToDateTime(dto.ActingPosition_Start_Before_Str),
                ActingPosition_End_Before = string.IsNullOrWhiteSpace(dto.ActingPosition_End_Before_Str)
                    ? null : Convert.ToDateTime(dto.ActingPosition_End_Before_Str),

                ActingPosition_Star_After = string.IsNullOrWhiteSpace(dto.ActingPosition_Start_After_Str)
                    ? null : Convert.ToDateTime(dto.ActingPosition_Start_After_Str),
                ActingPosition_End_After = string.IsNullOrWhiteSpace(dto.ActingPosition_End_After_Str)
                    ? null : Convert.ToDateTime(dto.ActingPosition_End_After_Str)
            };
            try
            {
                _repositoryAccessor.HRMS_Emp_Transfer_History.Add(dataNew);
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.CreateOKMsg");
            }
            catch (Exception)
            {
                return new OperationResult(false, "System.Message.CreateErrorMsg");
            }
        }

        public async Task<OperationResult> Update(EmployeeTransferHistoryDTO dto)
        {
            var item = await _repositoryAccessor.HRMS_Emp_Transfer_History.FirstOrDefaultAsync(x => x.History_GUID == dto.History_GUID);
            if (item == null) return new OperationResult(false, "System.Message.NoData");

            item.Department_After = dto.Department_After;
            item.Position_Grade_After = dto.Position_Grade_After;
            item.Position_Title_After = dto.Position_Title_After;
            item.Work_Type_After = dto.Work_Type_After;
            item.Reason_for_Change = dto.Reason_for_Change;
            item.Effective_Date = Convert.ToDateTime(dto.Effective_Date_Str);
            item.Seq = dto.Seq;
            item.Update_By = dto.Update_By;
            item.Update_Time = dto.Update_Time;
            item.ActingPosition_Star_After = string.IsNullOrWhiteSpace(dto.ActingPosition_Start_After_Str)
                ? null : Convert.ToDateTime(dto.ActingPosition_Start_After_Str);
            item.ActingPosition_End_After = string.IsNullOrWhiteSpace(dto.ActingPosition_End_After_Str)
                ? null : Convert.ToDateTime(dto.ActingPosition_End_After_Str);

            try
            {
                _repositoryAccessor.HRMS_Emp_Transfer_History.Update(item);
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.UpdateOKMsg");
            }
            catch (Exception)
            {
                return new OperationResult(false, "System.Message.UpdateErrorMsg");
            }
        }

        public async Task<OperationResult> DownloadFileExcel(EmployeeTransferHistoryParam param, List<string> roleList)
        {
            var data = await GetData(param, roleList);
            if (!data.Any()) return new OperationResult(false, "System.Message.NoData");

            List<Cell> dataCells = new();
            var index = 2;
            for (int i = 0; i < data.Count; i++)
            {

                index += 1;

                string nameDepartmentBefore = data[i].Department_Before;

                if (data[i].Department_Before != null)
                {
                    string[] parts = data[i].Department_Before.Split("-");

                    if (parts.Length >= 2)
                    {
                        nameDepartmentBefore = parts[1].Trim();

                        if (parts.Length == 3)
                            nameDepartmentBefore += " - " + parts[2].Trim();
                    }
                }
                dataCells.Add(new Cell("A" + index, ""));
                dataCells.Add(new Cell("B" + index, "Before Change"));
                dataCells.Add(new Cell("C" + index, data[i].Nationality_Before));
                dataCells.Add(new Cell("D" + index, data[i].Identification_Number_Before));
                dataCells.Add(new Cell("E" + index, data[i].Local_Full_Name_Before));
                dataCells.Add(new Cell("F" + index, data[i].Division_Before));
                dataCells.Add(new Cell("G" + index, data[i].Factory_Before));
                dataCells.Add(new Cell("H" + index, data[i].Employee_ID_Before));
                dataCells.Add(new Cell("I" + index, (data[i].Department_Before != null && data[i].Department_Before.Split("-").Length > 1) ? data[i].Department_Before.Split("-")[0].Trim() : data[i].Department_Before));
                dataCells.Add(new Cell("J" + index, nameDepartmentBefore));
                dataCells.Add(new Cell("K" + index, data[i].Assigned_Division_Before));
                dataCells.Add(new Cell("L" + index, data[i].Assigned_Factory_Before));
                dataCells.Add(new Cell("M" + index, data[i].Assigned_Employee_ID_Before));
                dataCells.Add(new Cell("N" + index, (data[i].Assigned_Department_Before != null && data[i].Assigned_Department_Before.Split("-").Length > 1) ? data[i].Assigned_Department_Before.Split("-")[0].Trim() : data[i].Assigned_Department_Before));
                dataCells.Add(new Cell("O" + index, (data[i].Assigned_Department_Before != null && data[i].Assigned_Department_Before.Split("-").Length > 1) ? data[i].Assigned_Department_Before.Split("-")[1].Trim() : data[i].Assigned_Department_Before));
                dataCells.Add(new Cell("P" + index, data[i].Position_Grade_Before.ToString()));
                dataCells.Add(new Cell("Q" + index, data[i].Position_Title_Before));
                dataCells.Add(new Cell("R" + index, data[i].Work_Type_Before));
                dataCells.Add(new Cell("S" + index, data[i].ActingPosition_Start_Before.HasValue ? data[i].ActingPosition_Start_Before.Value.ToString("yyyy/MM/dd") : ""));
                dataCells.Add(new Cell("T" + index, data[i].ActingPosition_End_Before.HasValue ? data[i].ActingPosition_End_Before.Value.ToString("yyyy/MM/dd") : ""));
                index += 1;
                string nameDepartmentAfter = data[i].Department_After;

                if (data[i].Department_After != null)
                {
                    string[] parts = data[i].Department_After.Split("-");

                    if (parts.Length >= 2)
                    {
                        nameDepartmentAfter = parts[1].Trim();

                        if (parts.Length == 3)
                            nameDepartmentAfter += " - " + parts[2].Trim();
                    }
                }
                dataCells.Add(new Cell("A" + index, data[i].Data_Source_Name));
                dataCells.Add(new Cell("B" + index, "After Change"));
                dataCells.Add(new Cell("C" + index, data[i].Nationality_After));
                dataCells.Add(new Cell("D" + index, data[i].Identification_Number_After));
                dataCells.Add(new Cell("E" + index, data[i].Local_Full_Name_After));
                dataCells.Add(new Cell("F" + index, data[i].Division_After));
                dataCells.Add(new Cell("G" + index, data[i].Factory_After));
                dataCells.Add(new Cell("H" + index, data[i].Employee_ID_After));
                dataCells.Add(new Cell("I" + index, (data[i].Department_After != null && data[i].Department_After.Split("-").Length > 1) ? data[i].Department_After.Split("-")[0].Trim() : data[i].Department_After));
                dataCells.Add(new Cell("J" + index, nameDepartmentAfter));
                dataCells.Add(new Cell("K" + index, data[i].Assigned_Division_After));
                dataCells.Add(new Cell("L" + index, data[i].Assigned_Factory_After));
                dataCells.Add(new Cell("M" + index, data[i].Assigned_Employee_ID_After));
                dataCells.Add(new Cell("N" + index, (data[i].Assigned_Department_After != null && data[i].Assigned_Department_Before.Split("-").Length > 1) ? data[i].Assigned_Department_After.Split("-")[0].Trim() : data[i].Assigned_Department_After));
                dataCells.Add(new Cell("O" + index, (data[i].Assigned_Department_After != null && data[i].Assigned_Department_Before.Split("-").Length > 1) ? data[i].Assigned_Department_After.Split("-")[1].Trim() : data[i].Assigned_Department_After));
                dataCells.Add(new Cell("P" + index, data[i].Position_Grade_After.ToString()));
                dataCells.Add(new Cell("Q" + index, data[i].Position_Title_After));
                dataCells.Add(new Cell("R" + index, data[i].Work_Type_After));
                dataCells.Add(new Cell("S" + index, data[i].ActingPosition_Start_After.HasValue ? data[i].ActingPosition_Start_After.Value.ToString("yyyy/MM/dd") : ""));
                dataCells.Add(new Cell("T" + index, data[i].ActingPosition_End_After.HasValue ? data[i].ActingPosition_End_After.Value.ToString("yyyy/MM/dd") : ""));
                dataCells.Add(new Cell("U" + index, (data[i].Reason_for_Change != null && data[i].Reason_for_Change.Split("-").Length > 1) ? data[i].Reason_for_Change.Split("-")[0].Trim() : data[i].Reason_for_Change));
                dataCells.Add(new Cell("V" + index, (data[i].Reason_for_Change != null && data[i].Reason_for_Change.Split("-").Length > 1) ? data[i].Reason_for_Change.Split("-")[1].Trim() : data[i].Reason_for_Change));
                dataCells.Add(new Cell("W" + index, data[i].Seq));
                dataCells.Add(new Cell("X" + index, data[i].Effective_Date_Str = data[i].Effective_Date == DateTime.MinValue ? "" : data[i].Effective_Date.ToString("yyyy/MM/dd")));
                dataCells.Add(new Cell("Y" + index, data[i].Effective_Status_Str = data[i].Effective_Status == true ? "Y" : "N"));
                dataCells.Add(new Cell("Z" + index, data[i].Update_By));
                dataCells.Add(new Cell("AA" + index, data[i].Update_Time.ToString("yyyy/MM/dd HH:mm:ss")));
            }
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataCells, 
                "Resources\\Template\\EmployeeMaintenance\\4_1_17_EmployeeTransferHistory\\Download.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }

        public async Task<List<EmployeeTransferHistoryDTO>> GetData(EmployeeTransferHistoryParam param, List<string> roleList)
        {
            var predEmpTransferHistory = PredicateBuilder.New<HRMS_Emp_Transfer_History>(true);
            var predEmpPersonal = PredicateBuilder.New<HRMS_Emp_Personal>(true);

            if (!string.IsNullOrWhiteSpace(param.Division_After))
            {
                predEmpTransferHistory = predEmpTransferHistory.And(x => x.Division_After == param.Division_After);
            }
            if (!string.IsNullOrWhiteSpace(param.Factory_After))
            {
                predEmpTransferHistory = predEmpTransferHistory.And(x => x.Factory_After == param.Factory_After);
            }
            if (!string.IsNullOrWhiteSpace(param.Employee_ID_After))
            {
                predEmpTransferHistory = predEmpTransferHistory.And(x => x.Employee_ID_After.ToLower().Contains(param.Employee_ID_After.ToLower()));
            }
            if (!string.IsNullOrWhiteSpace(param.Department_After))
                predEmpTransferHistory = predEmpTransferHistory.And(x => x.Department_After == param.Department_After);
            if (!string.IsNullOrWhiteSpace(param.USER_GUID))
                predEmpTransferHistory = predEmpTransferHistory.And(x => x.USER_GUID == param.USER_GUID);
            if (param.Effective_Date_Start != null)
                predEmpTransferHistory = predEmpTransferHistory.And(x => x.Effective_Date >= param.Effective_Date_Start);
            if (param.Effective_Date_End != null)
                predEmpTransferHistory = predEmpTransferHistory.And(x => x.Effective_Date <= param.Effective_Date_End);
            if (!string.IsNullOrWhiteSpace(param.Assigned_Division_After))
                predEmpTransferHistory = predEmpTransferHistory.And(x => x.Assigned_Division_After == param.Assigned_Division_After);
            if (!string.IsNullOrWhiteSpace(param.Assigned_Factory_After))
                predEmpTransferHistory = predEmpTransferHistory.And(x => x.Assigned_Factory_After == param.Assigned_Factory_After);
            if (!string.IsNullOrWhiteSpace(param.Assigned_Department_After))
                predEmpTransferHistory = predEmpTransferHistory.And(x => x.Assigned_Department_After == param.Assigned_Department_After);
            if (!string.IsNullOrWhiteSpace(param.Assigned_Employee_ID_After))
                predEmpTransferHistory = predEmpTransferHistory.And(x => x.Assigned_Employee_ID_After.ToLower().Contains(param.Assigned_Employee_ID_After.ToLower()));
            if (param.Effective_Status == 1 || param.Effective_Status == 2)
                predEmpTransferHistory = predEmpTransferHistory.And(x => x.Effective_Status == (param.Effective_Status == 1));
            if (!string.IsNullOrWhiteSpace(param.Local_Full_Name))
                predEmpPersonal = predEmpPersonal.And(x => x.Local_Full_Name.Contains(param.Local_Full_Name));

            var basicSeq1 = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == "1", true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language
                    .FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower(), true),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Code,
                    Name = $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                }).Distinct().ToListAsync();


            var basicSeq2 = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == "2", true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language
                    .FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower(), true),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Code,
                    Name = $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                }).Distinct().ToListAsync();

            var workType = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == "5", true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new
                {
                    x.HBC.Code,
                    Name = $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                }).Distinct().ToListAsync();

            var reasonforChange = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.ReasonChange && (x.Char1 != "OUT" || x.Char1 != "IN"), true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.Trim().ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new
                {
                    x.HBC.Code,
                    Name = $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                }).Distinct().ToListAsync();

            var department = await _repositoryAccessor.HRMS_Org_Department.FindAll(true)
               .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language
               .FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower(), true),
                   x => new { x.Division, x.Factory, x.Department_Code },
                   y => new { y.Division, y.Factory, y.Department_Code },
                   (x, y) => new { HOD = x, HODL = y })
               .SelectMany(x => x.HODL.DefaultIfEmpty(),
                   (x, y) => new { x.HOD, HODL = y })
               .Select(x => new
               {
                   Code = x.HOD.Department_Code,
                   Name = $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}",
                   x.HOD.Division,
                   x.HOD.Factory,
               }).Distinct().ToListAsync();

            var positionTitle = await _repositoryAccessor.HRMS_Basic_Level.FindAll(true)
                .Join(_repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "3", true),
                    HBL => HBL.Level_Code,
                    HBC => HBC.Code,
                    (HBL, HBC) => new { HBL, HBC })
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Lang.ToLower(), true),
                    prev => new { prev.HBC.Type_Seq, prev.HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (prev, HBCL) => new { prev.HBL, prev.HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBL, prev.HBC, HBCL })
            .Select(x => new
            {
                Code = x.HBL.Level_Code,
                Name = $"{x.HBL.Level_Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}",
                x.HBL.Level
            }).Distinct().ToListAsync();

            var iDataSources = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "44", true)
                        .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Type_Seq == "44" && x.Language_Code.ToLower() == param.Lang.ToLower(), true),
                           x => new { x.Type_Seq, x.Code },
                           y => new { y.Type_Seq, y.Code },
                           (x, y) => new { HBC = x, HBCL = y }
                        ).SelectMany(x => x.HBCL.DefaultIfEmpty(),
                            (x, y) => new { x.HBC, HBCL = y }
                        ).Select(x => new
                        {
                            Code = x.HBC.Code.Trim(),
                            Name = x.HBC.Code.Trim() + "-" + (x.HBCL != null ? x.HBCL.Code_Name.Trim() : x.HBC.Code_Name.Trim())
                        }).Distinct().ToListAsync();

            var dataEmpPersonal = await Query_Permission_Data_Filter(roleList);
            var dataEmpTransferHistory = await _repositoryAccessor.HRMS_Emp_Transfer_History.FindAll(predEmpTransferHistory).Distinct().ToListAsync();

            var data = dataEmpTransferHistory.Join(dataEmpPersonal.Where(predEmpPersonal).Distinct(),
                x => x.USER_GUID,
                y => y.USER_GUID,
                (x, y) => new { EmpTransferHistory = x, Personal = y });

            var result = data.Select(x => new EmployeeTransferHistoryDTO
            {
                History_GUID = x.EmpTransferHistory.History_GUID,
                USER_GUID = x.EmpTransferHistory.USER_GUID,
                Local_Full_Name_Before = x.Personal.Local_Full_Name,
                Local_Full_Name_After = x.Personal.Local_Full_Name,
                Reason_for_Change = reasonforChange.FirstOrDefault(y => y.Code == x.EmpTransferHistory.Reason_for_Change)?.Name ?? x.EmpTransferHistory.Reason_for_Change,
                Effective_Date = x.EmpTransferHistory.Effective_Date,
                Effective_Status = x.EmpTransferHistory.Effective_Status,
                Nationality_Before = x.EmpTransferHistory.Nationality_Before,
                Identification_Number_Before = x.EmpTransferHistory.Identification_Number_Before,
                Division_Before = basicSeq1.FirstOrDefault(y => y.Code == x.EmpTransferHistory.Division_Before)?.Name ?? x.EmpTransferHistory.Division_Before,
                Factory_Before = basicSeq2.FirstOrDefault(y => y.Code == x.EmpTransferHistory.Factory_Before)?.Name ?? x.EmpTransferHistory.Factory_Before,
                Employee_ID_Before = x.EmpTransferHistory.Employee_ID_Before,
                Assigned_Division_Before = basicSeq1.FirstOrDefault(y => y.Code == x.EmpTransferHistory.Assigned_Division_Before)?.Name,
                Assigned_Factory_Before = basicSeq2.FirstOrDefault(y => y.Code == x.EmpTransferHistory.Assigned_Factory_Before)?.Name,
                Assigned_Employee_ID_Before = x.EmpTransferHistory.Assigned_Employee_ID_Before,
                Position_Grade_Before = x.EmpTransferHistory.Position_Grade_Before,
                Work_Type_Before = workType.FirstOrDefault(y => y.Code == x.EmpTransferHistory.Work_Type_Before)?.Name ?? x.EmpTransferHistory.Work_Type_Before,
                ActingPosition_Start_Before = x.EmpTransferHistory.ActingPosition_Star_Before,
                ActingPosition_End_Before = x.EmpTransferHistory.ActingPosition_End_Before,
                Data_Source = x.EmpTransferHistory.Data_Source,
                Data_Source_Name = iDataSources.FirstOrDefault(s => s.Code == x.EmpTransferHistory.Data_Source)?.Name ?? x.EmpTransferHistory.Data_Source,
                Nationality_After = x.EmpTransferHistory.Nationality_After,
                Identification_Number_After = x.EmpTransferHistory.Identification_Number_After,
                Division_After = basicSeq1.FirstOrDefault(y => y.Code == x.EmpTransferHistory.Division_After)?.Name ?? x.EmpTransferHistory.Division_After,
                Factory_After = basicSeq2.FirstOrDefault(y => y.Code == x.EmpTransferHistory.Factory_After)?.Name ?? x.EmpTransferHistory.Factory_After,
                Employee_ID_After = x.EmpTransferHistory.Employee_ID_After,
                Assigned_Division_After = basicSeq1.FirstOrDefault(y => y.Code == x.EmpTransferHistory.Assigned_Division_After)?.Name,
                Assigned_Factory_After = basicSeq2.FirstOrDefault(y => y.Code == x.EmpTransferHistory.Assigned_Factory_After)?.Name,
                Assigned_Employee_ID_After = x.EmpTransferHistory.Assigned_Employee_ID_After,
                Position_Grade_After = x.EmpTransferHistory.Position_Grade_After,
                Work_Type_After = workType.FirstOrDefault(y => y.Code == x.EmpTransferHistory.Work_Type_After)?.Name ?? x.EmpTransferHistory.Work_Type_After,
                ActingPosition_Start_After = x.EmpTransferHistory.ActingPosition_Star_After,
                ActingPosition_End_After = x.EmpTransferHistory.ActingPosition_End_After,
                Seq = x.EmpTransferHistory.Seq ?? 0,
                Update_By = x.EmpTransferHistory.Update_By,
                Update_Time = x.EmpTransferHistory.Update_Time,
                Department_After = department.FirstOrDefault(y => y.Factory == x.EmpTransferHistory.Factory_After && y.Code == x.EmpTransferHistory.Department_After
                                                          && y.Division == x.EmpTransferHistory.Division_After)?.Name,
                Department_Before = department.FirstOrDefault(y => y.Factory == x.EmpTransferHistory.Factory_Before && y.Code == x.EmpTransferHistory.Department_Before
                                                          && y.Division == x.EmpTransferHistory.Division_Before)?.Name,
                Assigned_Department_After = department.FirstOrDefault(y => y.Factory == x.EmpTransferHistory.Assigned_Factory_After && y.Code == x.EmpTransferHistory.Assigned_Department_After
                                                          && y.Division == x.EmpTransferHistory.Assigned_Division_After)?.Name,
                Assigned_Department_Before = department.FirstOrDefault(y => y.Factory == x.EmpTransferHistory.Assigned_Factory_Before && y.Code == x.EmpTransferHistory.Assigned_Department_Before
                                                          && y.Division == x.EmpTransferHistory.Assigned_Division_Before)?.Name,
                Position_Title_After = positionTitle.FirstOrDefault(y => y.Code == x.EmpTransferHistory.Position_Title_After && y.Level == x.EmpTransferHistory.Position_Grade_After)?
                                                      .Name,
                Position_Title_Before = positionTitle.FirstOrDefault(y => y.Code == x.EmpTransferHistory.Position_Title_Before && y.Level == x.EmpTransferHistory.Position_Grade_Before)?
                                                      .Name,

            })
            .OrderBy(x => x.Division_After)
            .ThenBy(x => x.Factory_After)
            .ThenByDescending(x => x.Effective_Date)
            .ThenBy(x => x.Department_After)
            .ThenBy(x => x.Employee_ID_After)
            .ThenBy(x => x.Seq)
            .ToList();

            return result;
        }

        public async Task<PaginationUtility<EmployeeTransferHistoryDTO>> GetDataPagination(PaginationParam pagination, EmployeeTransferHistoryParam param, List<string> roleList)
        => PaginationUtility<EmployeeTransferHistoryDTO>.Create(await GetData(param, roleList), pagination.PageNumber, pagination.PageSize);

        public async Task<List<KeyValuePair<string, string>>> GetListAssignedDivisionAfter(string language)
        => await GetDivison(BasicCodeTypeConstant.Division, language);

        public async Task<List<KeyValuePair<string, string>>> GetListDivision(string language)
        => await GetDivison(BasicCodeTypeConstant.Division, language);
        public async Task<List<KeyValuePair<string, string>>> GetListAssignedFactoryAfter(string language, string assignedDivisionAfter)
        => await GetFactory(assignedDivisionAfter, language);


        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string language, string division)
        => await GetFactory(division, language);


        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string language, string factory, string division)
        => await GetDepartment(division, factory, language);

        public async Task<List<KeyValuePair<string, string>>> GetListDepartmentAfter(string language, string assignedDivisionAfter, string assignedFactoryAfter)
        => await GetDepartment(assignedDivisionAfter, assignedFactoryAfter, language);


        public async Task<List<KeyValuePair<string, string>>> GetListPositionTitle(string language, decimal? positionGrade)
        {
            var pred = PredicateBuilder.New<HRMS_Basic_Level>(true);

            if (positionGrade != null)
                pred = pred.And(x => x.Level == positionGrade);

            return await _repositoryAccessor.HRMS_Basic_Level.FindAll(pred, true)
                    .Join(_repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "3", true),
                        HBL => HBL.Level_Code,
                        HBC => HBC.Code,
                        (HBL, HBC) => new { HBL, HBC })
                    .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                        prev => new { prev.HBC.Type_Seq, prev.HBC.Code },
                        HBCL => new { HBCL.Type_Seq, HBCL.Code },
                        (prev, HBCL) => new { prev.HBL, prev.HBC, HBCL })
                        .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                        (prev, HBCL) => new { prev.HBL, prev.HBC, HBCL })
                    .Select(x => new KeyValuePair<string, string>(x.HBL.Level_Code,
                    $"{x.HBL.Level_Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                    .Distinct()
                    .ToListAsync();
        }

        public async Task<List<KeyValuePair<string, string>>> GetListDataSource(string language)
        {
            var data = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "44", true)
                        .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Type_Seq == "44" && x.Language_Code.ToLower() == language.ToLower(), true),
                           x => new { x.Type_Seq, x.Code },
                           y => new { y.Type_Seq, y.Code },
                           (x, y) => new { HBC = x, HBCL = y }
                        ).SelectMany(x => x.HBCL.DefaultIfEmpty(),
                            (x, y) => new { x.HBC, HBCL = y }
                        ).Select(x => new KeyValuePair<string, string>(
                            x.HBC.Code.Trim(),
                            x.HBC.Code.Trim() + "-" + (x.HBCL != null ? x.HBCL.Code_Name.Trim() : x.HBC.Code_Name.Trim())
                        )).Distinct().ToListAsync();
            return data;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListWorkType(string language)
        => await GetDivison(BasicCodeTypeConstant.WorkType, language);
        public async Task<List<string>> GetListTypeHeadEmployeeID(string factory, string division)
        => await _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == factory && x.Division == division && x.Employee_ID.Length <= 9, true).Select(x => x.Employee_ID).Distinct().ToListAsync();

        public async Task<EmployeeTransferHistoryDTO> GetDataDetail(string division, string employee_ID, string factory)
        {
            var pred = PredicateBuilder.New<HRMS_Emp_Personal>(true);
            if (!string.IsNullOrWhiteSpace(division))
                pred = pred.And(x => x.Division == division);
            if (!string.IsNullOrWhiteSpace(employee_ID))
                pred = pred.And(x => x.Employee_ID == employee_ID);
            else return new();
            if (!string.IsNullOrWhiteSpace(factory))
                pred = pred.And(x => x.Factory == factory);

            var checkData = await _repositoryAccessor.HRMS_Emp_Personal
                .FirstOrDefaultAsync(pred, true);

            var seqValue = await GetMaxSeq(factory, division, employee_ID);

            if (checkData == null) return new();

            // Read the latest data with the largest effective date and seq
            var HETHs = _repositoryAccessor.HRMS_Emp_Transfer_History.FindAll(x =>
               x.Factory_After == factory && x.Employee_ID_After == employee_ID
           );
            var HETH = await HETHs
                .Where(x => x.Effective_Date.Date == HETHs.Max(y => y.Effective_Date).Date)
                .GroupBy(x => new
                {
                    x.ActingPosition_Star_After,
                    x.ActingPosition_End_After
                })
                .Select(x => new
                {
                    x.Key.ActingPosition_Star_After,
                    x.Key.ActingPosition_End_After,
                    MaxSeq = x.Max(y => y.Seq)
                })
                .OrderByDescending(x => x.MaxSeq)
                .FirstOrDefaultAsync();

            var data = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(pred, true)
            .Select(x => new EmployeeTransferHistoryDTO
            {
                Nationality_Before = x.Nationality,
                Nationality_After = x.Nationality,
                Identification_Number_Before = x.Identification_Number,
                Identification_Number_After = x.Identification_Number,
                Local_Full_Name_Before = x.Local_Full_Name,
                Local_Full_Name_After = x.Local_Full_Name,
                Division_Before = x.Division,
                Factory_Before = x.Factory,
                Employee_ID_Before = x.Employee_ID,
                Department_Before = x.Department,
                Assigned_Division_Before = x.Assigned_Division,
                Assigned_Division_After = x.Assigned_Division,
                Assigned_Factory_Before = x.Assigned_Factory,
                Assigned_Factory_After = x.Assigned_Factory,
                Assigned_Employee_ID_Before = x.Assigned_Employee_ID,
                Assigned_Employee_ID_After = x.Assigned_Employee_ID,
                Assigned_Department_Before = x.Assigned_Department,
                Assigned_Department_After = x.Assigned_Department,
                Position_Grade_Before = x.Position_Grade,
                Position_Title_Before = x.Position_Title,
                Work_Type_Before = x.Work_Type,
                Seq = seqValue,
                USER_GUID = x.USER_GUID,
                ActingPosition_Start_Before = HETH != null ? HETH.ActingPosition_Star_After : null,
                ActingPosition_End_Before = HETH != null ? HETH.ActingPosition_End_After : null,
                ActingPosition_Start_After = HETH != null ? HETH.ActingPosition_Star_After : null,
                ActingPosition_End_After = HETH != null ? HETH.ActingPosition_End_After : null,
            }).FirstOrDefaultAsync();

            return data;
        }
        public async Task<int> GetMaxSeq(string factory, string division, string employee_ID)
        {
            var dataExist = await _repositoryAccessor.HRMS_Emp_Transfer_History
                .FindAll(x => x.Factory_After == factory && x.Division_After == division && x.Employee_ID_After == employee_ID, true)
                .ToListAsync();

            if (!dataExist.Any())
                return 1;

            var seqList = new List<int>(dataExist.Select(x => x.Seq.GetValueOrDefault()));
            var max_seq = seqList.Max();

            var result = Enumerable.Range(1, max_seq + 1)
                .Except(seqList)
                .ToList();

            return result.FirstOrDefault();
        }
        public async Task<List<KeyValuePair<decimal, decimal>>> GetListPositionGrade(string language)
        {
            return await _repositoryAccessor.HRMS_Basic_Level.FindAll(true)
            .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                                x => x.Level_Code,
                                y => y.Code,
                                (x, y) => new { x, y })
                                .SelectMany(x => x.y.DefaultIfEmpty(),
                                (x, y) => new { BasicLevel = x.x, BasicCodeLanguage = y })
            .Select(x => new KeyValuePair<decimal, decimal>(x.BasicLevel.Level, x.BasicLevel.Level)).Distinct().ToListAsync();
        }
        public async Task<List<KeyValuePair<string, string>>> GetListReasonforChange(string language)
        {
            var data = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.ReasonChange && (x.Char1 != "OUT" || x.Char1 != "IN"), true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code == language.ToLower(), true),
                                x => new { x.Type_Seq, x.Code },
                                y => new { y.Type_Seq, y.Code },
                                (x, y) => new { x, y })
                                .SelectMany(x => x.y.DefaultIfEmpty(),
                                (x, y) => new { HBC = x.x, HBCL = y })
            .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}")).ToListAsync();
            return data;
        }
        private async Task<List<KeyValuePair<string, string>>> GetDivison(string type_Seq, string language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == type_Seq, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
        }

        private async Task<List<KeyValuePair<string, string>>> GetDepartment(string division, string factory, string language)
        {
            return await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Division == division && x.Factory == factory, true)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                      HOD => new { HOD.Division, HOD.Factory, HOD.Department_Code },
                      HODL => new { HODL.Division, HODL.Factory, HODL.Department_Code },
                    (HOD, HODL) => new { HOD, HODL })
                    .SelectMany(x => x.HODL.DefaultIfEmpty(),
                    (prev, HODL) => new { prev.HOD, HODL })
                .Select(x => new KeyValuePair<string, string>(x.HOD.Department_Code, $"{x.HOD.Department_Code} - {(x.HODL != null ? x.HODL.Name : x.HOD.Department_Name)}"))
                .ToListAsync();
        }
        private async Task<List<KeyValuePair<string, string>>> GetFactory(string division, string language)
        {
            var predicate = PredicateBuilder.New<HRMS_Basic_Factory_Comparison>(x => x.Kind == "1");

            if (!string.IsNullOrWhiteSpace(division))
                predicate = predicate.And(x => x.Division.ToLower().Contains(division.ToLower()));

            List<HRMS_Basic_Factory_Comparison> HBFC = await _repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(predicate, true).ToListAsync();
            List<KeyValuePair<string, string>> HBC_Langs = await GetDataBasicCode(BasicCodeTypeConstant.Factory, language);
            List<KeyValuePair<string, string>> result = new();
            if (HBFC.Any())
            {
                result = HBFC.Select(item =>
                {
                    return new KeyValuePair<string, string>(HBC_Langs.FirstOrDefault(x => x.Key == item.Factory).Key, HBC_Langs.FirstOrDefault(x => x.Key == item.Factory).Value);
                }).ToList();

                return result;
            }
            return HBC_Langs;
        }

        /// <summary>
        /// Cập nhật Job chạy vào mỗi tối
        /// </summary>
        /// <returns></returns>
        public async Task CheckEffectiveDate()
        {
            var data = await _repositoryAccessor.HRMS_Emp_Transfer_History.FindAll(x => x.Effective_Date == DateTime.Today
            && x.Effective_Status == false, true).ToListAsync();

            foreach (var item in data)
            {
                item.Effective_Status = true;
                _repositoryAccessor.HRMS_Emp_Transfer_History.Update(item);


                // DataSource = "01" [Được phép cập nhật]
                if (item.Data_Source == "01")
                {
                    var dataPersonal = await _repositoryAccessor.HRMS_Emp_Personal.FirstOrDefaultAsync(x => x.Division == item.Division_Before
                    && x.Factory == item.Factory_Before && x.Employee_ID == item.Employee_ID_Before, true);

                    if (dataPersonal != null)
                    {
                        dataPersonal.Department = item.Department_After;
                        dataPersonal.Position_Grade = item.Position_Grade_After;
                        dataPersonal.Position_Title = item.Position_Title_After;
                        dataPersonal.Work_Type = item.Work_Type_After;
                        dataPersonal.Update_By = item.Update_By;
                        dataPersonal.Update_Time = DateTime.Now;

                        _repositoryAccessor.HRMS_Emp_Personal.Update(dataPersonal);
                    }
                }

                await _repositoryAccessor.Save();
            }
        }
        public async Task<OperationResult> Delete(EmployeeTransferHistoryDetele dto)
        {
            if (dto.Effective_Status) return new OperationResult(false, "System.Message.UpdateErrorStatus");

            var item = await _repositoryAccessor.HRMS_Emp_Transfer_History.FirstOrDefaultAsync(x => x.History_GUID == dto.History_GUID
                                                                            && x.Effective_Status == dto.Effective_Status);

            if (item == null) return new OperationResult(false, "System.Message.NoData");

            _repositoryAccessor.HRMS_Emp_Transfer_History.Remove(item);
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
        public async Task<OperationResult> BatchDelete(List<EmployeeTransferHistoryDetele> dto)
        {
            List<HRMS_Emp_Transfer_History> listDelete = new();
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                foreach (var item in dto)
                {
                    if (item.Effective_Status) return new OperationResult(false, "System.Message.UpdateErrorStatus");
                    var itemDelete = await _repositoryAccessor.HRMS_Emp_Transfer_History.FirstOrDefaultAsync(x => x.History_GUID == item.History_GUID
                                                                                && x.Effective_Status == item.Effective_Status, true);
                    if (itemDelete != null)
                        listDelete.Add(itemDelete);
                }

                _repositoryAccessor.HRMS_Emp_Transfer_History.RemoveMultiple(listDelete);

                await _repositoryAccessor.Save();

                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true, "System.Message.DeleteOKMsg");
            }
            catch (Exception)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, "System.Message.DeleteErrorMsg");
            }

        }

        /// <summary>
        /// Xác nhận Confirm Trạng thái cập nhật Dữ liệu nhân sự chính
        /// </summary>
        /// <param name="dto"> Danh sách nhân viên cần được cập nhật </param>
        /// <returns></returns>
        public async Task<OperationResult> EffectiveConfirm(List<EmployeeTransferHistoryEffectiveConfirm> dto, string currentUser)
        {
            var currrentTime = DateTime.Now;
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                // Danh sách lịch sử cập nhật
                foreach (var item in dto)
                {
                    // Lịch sử chuyển nhân viên
                    var checkResult = await CheckEffectiveConfirm(item);
                    if (!checkResult.IsSuccess)
                    {
                        await _repositoryAccessor.RollbackAsync();
                        return checkResult;
                    }
                    var transfer_employee_history = checkResult.Data as HRMS_Emp_Transfer_History;
                    transfer_employee_history.Effective_Status = true;
                    _repositoryAccessor.HRMS_Emp_Transfer_History.Update(transfer_employee_history);
                    // DataSource = "01" [Được phép cập nhật]
                    if (transfer_employee_history.Data_Source == "01")
                    {
                        // Cập nhật dữ liệu nhân viên chính [khi trạng thái Effective_Status = true hiệu lực cho phép ] 
                        var employee = await _repositoryAccessor.HRMS_Emp_Personal.FirstOrDefaultAsync(x => x.USER_GUID == item.USER_GUID);
                        if (employee != null)
                        {
                            employee.Department = transfer_employee_history.Department_After;
                            employee.Position_Grade = transfer_employee_history.Position_Grade_After;
                            employee.Position_Title = transfer_employee_history.Position_Title_After;
                            employee.Work_Type = transfer_employee_history.Work_Type_After;
                            employee.Update_By = currentUser;
                            employee.Update_Time = currrentTime;
                            _repositoryAccessor.HRMS_Emp_Personal.Update(employee);
                        }
                    }
                    if (!await _repositoryAccessor.Save())
                    {
                        await _repositoryAccessor.RollbackAsync();
                        return new OperationResult(false, "System.Message.UpdateErrorMsg");
                    }
                }
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true, "System.Message.UpdateOKMsg");
            }
            catch (Exception)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false, "System.Message.UpdateErrorMsg");
            }
        }
        public async Task<OperationResult> CheckEffectiveConfirm(EmployeeTransferHistoryEffectiveConfirm data)
        {
            var HETH = await _repositoryAccessor.HRMS_Emp_Transfer_History.FirstOrDefaultAsync(x =>
                x.History_GUID == data.History_GUID &&
                x.USER_GUID == data.USER_GUID &&
                x.Effective_Date.Date == Convert.ToDateTime(data.Effective_Date).Date &&
                x.Effective_Status == false
            );
            if (HETH == null ||
                HETH.Effective_Date.Date > DateTime.Now.Date ||
                HETH.Data_Source == "02" ||
                await _repositoryAccessor.HRMS_Emp_Transfer_History.AnyAsync(x =>
                    x.USER_GUID == data.USER_GUID &&
                    x.Effective_Date.Date < HETH.Effective_Date.Date &&
                    x.Effective_Status == false))
                return new OperationResult(false, "System.Message.ErrorEffectiveConfirm");
            return new OperationResult(true, HETH);
        }
    }
}