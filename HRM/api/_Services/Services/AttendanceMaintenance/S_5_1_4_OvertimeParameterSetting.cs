
using AgileObjects.AgileMapper;
using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_1_4_OvertimeParameterSetting : BaseServices, I_5_1_4_OvertimeParameterSetting
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        public S_5_1_4_OvertimeParameterSetting(DBContext dbContext,IWebHostEnvironment webHostEnvironment) : base(dbContext)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<OperationResult> Create(HRMS_Att_Overtime_ParameterDTO data)
        {
            if (await _repositoryAccessor.HRMS_Att_Overtime_Parameter.AnyAsync(x =>
                    x.Division == data.Division &&
                    x.Factory == data.Factory &&
                    x.Effective_Month == Convert.ToDateTime(data.Effective_Month) &&
                    x.Work_Shift_Type == data.Work_Shift_Type &&
                    x.Overtime_Start == data.Overtime_Start))

                return new OperationResult(false, "System.Message.DataExisted");

            var dataNew = new HRMS_Att_Overtime_Parameter
            {
                Division = data.Division,
                Factory = data.Factory,
                Work_Shift_Type = data.Work_Shift_Type,
                Effective_Month = Convert.ToDateTime(data.Effective_Month),
                Overtime_Start = data.Overtime_Start,
                Overtime_End = data.Overtime_End,
                Overtime_Hours = decimal.Parse(data.Overtime_Hours),
                Night_Hours = decimal.Parse(data.Night_Hours),
                Update_By = data.Update_By,
                Update_Time = Convert.ToDateTime(data.Update_Time),
            };

            _repositoryAccessor.HRMS_Att_Overtime_Parameter.Add(dataNew);

            try
            {
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.CreateOKMsg");
            }
            catch (Exception)
            {
                return new OperationResult(false, "System.Message.CreateErrorMsg");
            }
        }

        public async Task<OperationResult> DownloadFileExcel(HRMS_Att_Overtime_ParameterParam param, string userName)
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
                dataCells.Add(new Cell("A" + index, data[i].Division));
                dataCells.Add(new Cell("B" + index, data[i].Factory));
                dataCells.Add(new Cell("C" + index, data[i].Work_Shift_Type));
                dataCells.Add(new Cell("D" + index, data[i].Overtime_Start));
                dataCells.Add(new Cell("E" + index, data[i].Overtime_End));
                dataCells.Add(new Cell("F" + index, data[i].Overtime_Hours));
                dataCells.Add(new Cell("G" + index, data[i].Night_Hours));
                dataCells.Add(new Cell("H" + index, data[i].Effective_Month));
                index += 1;
            }

            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataCells, 
                "Resources\\Template\\AttendanceMaintenance\\5_1_4_OvertimeParameterSetting\\Download.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }

        public async Task<PaginationUtility<HRMS_Att_Overtime_ParameterDTO>> GetDataPagination(PaginationParam pagination, HRMS_Att_Overtime_ParameterParam param)
        {
            var data = await GetData(param);
            return PaginationUtility<HRMS_Att_Overtime_ParameterDTO>.Create(data, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<List<HRMS_Att_Overtime_ParameterDTO>> GetData(HRMS_Att_Overtime_ParameterParam param)
        {

            var predicate = PredicateBuilder.New<HRMS_Att_Overtime_Parameter>(true);
            if (!string.IsNullOrWhiteSpace(param.Division))
                predicate.And(x => x.Division == param.Division);
            if (!string.IsNullOrWhiteSpace(param.Factory))
                predicate.And(x => x.Factory == param.Factory);
            if (!string.IsNullOrWhiteSpace(param.Work_Shift_Type))
                predicate.And(x => x.Work_Shift_Type == param.Work_Shift_Type);
            if (!string.IsNullOrWhiteSpace(param.Effective_Month))
                predicate.And(x => x.Effective_Month == Convert.ToDateTime(param.Effective_Month));

            var dataWorkShiftType = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.WorkShiftType, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true),
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { HBC = x, HBCL = y })
                .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (x, y) => new { x.HBC, HBCL = y })
                .Select(x => new
                {
                    x.HBC.Code,
                    Name = $"{(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"
                }).Distinct().ToListAsync();

            var data = await _repositoryAccessor.HRMS_Att_Overtime_Parameter.FindAll(predicate).ToListAsync();
            var result = data.Select(x => new HRMS_Att_Overtime_ParameterDTO
            {
                Division = x.Division,
                Factory = x.Factory,
                Work_Shift_Type = x.Work_Shift_Type,
                Work_Shift_Type_Name = dataWorkShiftType.FirstOrDefault(y => y.Code == x.Work_Shift_Type)?.Name,
                Effective_Month = x.Effective_Month.ToString("yyyy/MM"),
                Overtime_Start = x.Overtime_Start,
                Overtime_End = x.Overtime_End,
                Overtime_Hours = x.Overtime_Hours.ToString(),
                Night_Hours = x.Night_Hours.ToString(),
                Update_By = x.Update_By,
                Update_Time = x.Update_Time.ToString("yyyy/MM/dd HH:mm:ss"),
            }).ToList();

            return result;
        }

        public async Task<OperationResult> Update(HRMS_Att_Overtime_ParameterDTO data)
        {
            var item = await _repositoryAccessor.HRMS_Att_Overtime_Parameter.FirstOrDefaultAsync(x =>
                    x.Division == data.Division &&
                    x.Factory == data.Factory &&
                    x.Effective_Month == Convert.ToDateTime(data.Effective_Month) &&
                    x.Work_Shift_Type == data.Work_Shift_Type &&
                    x.Overtime_Start == data.Overtime_Start_Old);
            if (item == null)
                return new OperationResult(false, "System.Message.NoData");
            HRMS_Att_Overtime_Parameter dataNew = Mapper.Map(data).ToANew<HRMS_Att_Overtime_Parameter>(x => x.MapEntityKeys());
            try
            {
                _repositoryAccessor.HRMS_Att_Overtime_Parameter.Remove(item);
                _repositoryAccessor.HRMS_Att_Overtime_Parameter.Add(dataNew);
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.UpdateOKMsg");
            }
            catch (Exception)
            {
                return new OperationResult(false, "System.Message.UpdateErrorMsg");
            }
        }

        public async Task<OperationResult> UploadFileExcel(HRMS_Att_Overtime_ParameterUploadParam param, string userName)
        {
            ExcelResult resp = ExcelUtility.CheckExcel(
                param.File, 
                "Resources\\Template\\AttendanceMaintenance\\5_1_4_OvertimeParameterSetting\\Template.xlsx"
            );
            if (!resp.IsSuccess)
                return new OperationResult(false, resp.Error);
            List<HRMS_Att_Overtime_Parameter> HAOTP_Update = new();
            List<HRMS_Att_Overtime_Parameter> HAOTP_Add = new();

            for (int i = resp.WsTemp.Cells.Rows.Count; i < resp.Ws.Cells.Rows.Count; i++)
            {
                var divisionData = await _repositoryAccessor.HRMS_Basic_Code
                                    .FirstOrDefaultAsync(x => x.Code == resp.Ws.Cells[i, 0].StringValue
                                                           && x.Type_Seq == BasicCodeTypeConstant.Division);

                if (divisionData == null || resp.Ws.Cells[i, 0].StringValue.Length > 10)
                    return new OperationResult { IsSuccess = false, Error = $"Division in row {i + 1} invalid" };

                var factoryData = await _repositoryAccessor.HRMS_Basic_Code
                               .FirstOrDefaultAsync(x => x.Code == resp.Ws.Cells[i, 1].StringValue
                                                      && x.Type_Seq == BasicCodeTypeConstant.Factory);

                if (factoryData == null || resp.Ws.Cells[i, 1].StringValue.Length > 10)
                    return new OperationResult { IsSuccess = false, Error = $"Factory in row {i + 1} invalid" };

                var workShiftData = await _repositoryAccessor.HRMS_Basic_Code
                                .FirstOrDefaultAsync(x => x.Code == resp.Ws.Cells[i, 2].StringValue
                                                       && x.Type_Seq == BasicCodeTypeConstant.WorkShiftType);
                if (workShiftData == null || resp.Ws.Cells[i, 2].StringValue.Length > 10)
                    return new OperationResult { IsSuccess = false, Error = $"Work Shift Type in row {i + 1} invalid" };

                if (resp.Ws.Cells[i, 3].Value == null || resp.Ws.Cells[i, 3].StringValue.Length > 4)
                    return new OperationResult { IsSuccess = false, Error = $"Overtime Start in row {i + 1} invalid" };

                if (resp.Ws.Cells[i, 4].Value == null || resp.Ws.Cells[i, 4].StringValue.Length > 4)
                    return new OperationResult { IsSuccess = false, Error = $"Overtime End in row {i + 1} invalid" };

                if (resp.Ws.Cells[i, 5].Value == null || resp.Ws.Cells[i, 5].StringValue.Length > 16)
                    return new OperationResult { IsSuccess = false, Error = $"Overtime Hours in row {i + 1} invalid" };

                if (resp.Ws.Cells[i, 6].Value == null || resp.Ws.Cells[i, 6].StringValue.Length > 16)
                    return new OperationResult { IsSuccess = false, Error = $"Night Hours in row {i + 1} invalid" };

                if (resp.Ws.Cells[i, 7].Value == null)
                    return new OperationResult { IsSuccess = false, Error = $"Effective Month in row {i + 1} invalid" };

                string division = resp.Ws.Cells[i, 0].StringValue.Trim();
                string factory = resp.Ws.Cells[i, 1].StringValue.Trim();
                string workShiftType = resp.Ws.Cells[i, 2].StringValue.Trim();
                string overtimeStart = resp.Ws.Cells[i, 3].StringValue.Trim();
                DateTime effectiveMonth = Convert.ToDateTime(resp.Ws.Cells[i, 7].StringValue.Trim());

                var item = await _repositoryAccessor.HRMS_Att_Overtime_Parameter.FirstOrDefaultAsync(x =>
                    x.Division == division &&
                    x.Factory == factory &&
                    x.Effective_Month == effectiveMonth &&
                    x.Work_Shift_Type == workShiftType &&
                    x.Overtime_Start == overtimeStart, true);

                if (item == null)
                {

                    HRMS_Att_Overtime_Parameter dataUpdate = new()
                    {
                        Division = resp.Ws.Cells[i, 0].StringValue?.Trim(),
                        Factory = resp.Ws.Cells[i, 1].StringValue?.Trim(),
                        Work_Shift_Type = resp.Ws.Cells[i, 2].StringValue?.Trim(),
                        Overtime_Start = resp.Ws.Cells[i, 3].StringValue?.Trim(),
                        Overtime_End = resp.Ws.Cells[i, 4].StringValue?.Trim(),
                        Overtime_Hours = (decimal)resp.Ws.Cells[i, 5].FloatValue,
                        Night_Hours = (decimal)resp.Ws.Cells[i, 6].FloatValue,
                        Effective_Month = Convert.ToDateTime(resp.Ws.Cells[i, 7].StringValue.Trim()),
                        Update_By = userName,
                        Update_Time = DateTime.Now
                    };

                    HAOTP_Add.Add(dataUpdate);
                }
                else
                {
                    item.Overtime_Start = resp.Ws.Cells[i, 3].StringValue?.Trim();
                    item.Overtime_End = resp.Ws.Cells[i, 4].StringValue?.Trim();
                    item.Overtime_Hours = (decimal)resp.Ws.Cells[i, 5].FloatValue;
                    item.Night_Hours = (decimal)resp.Ws.Cells[i, 6].FloatValue;
                    item.Update_By = userName;
                    item.Update_Time = DateTime.Now;

                    HAOTP_Update.Add(item);
                }
            }

            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                _repositoryAccessor.HRMS_Att_Overtime_Parameter.AddMultiple(HAOTP_Add);
                _repositoryAccessor.HRMS_Att_Overtime_Parameter.UpdateMultiple(HAOTP_Update);

                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();

                string path = @"uploaded/AttendanceMaintenance/5_4_Overtime_Parameter_Setting/Creates";
                await FilesUtility.SaveFile(param.File, path, $"Overtime_Parameter_Setting_{DateTime.Now:yyyyMMddHHmmss}");

                return new OperationResult { IsSuccess = true };
            }
            catch (Exception e)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult { IsSuccess = false, Error = e.ToString() };
            }
        }

        public async Task<List<KeyValuePair<string, string>>> GetListDivision(string language)
        {
            var data = await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Division, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
            return data;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListFactory(string division, string language, string userName)
        {
            var factories = await Query_Factory_List(division);
            var roleFactories = await Queryt_Factory_AddList(userName);
            var filterFatories = factories.Intersect(roleFactories).ToList();
            var HBC = _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Factory).ToList();
            var HBCL = _repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower()).ToList();
            var code_Lang = HBC.GroupJoin(HBCL,
                    x => new { x.Type_Seq, x.Code },
                    y => new { y.Type_Seq, y.Code },
                    (x, y) => new { hbc = x, hbcl = y })
                    .SelectMany(x => x.hbcl.DefaultIfEmpty(),
                    (x, y) => new { x.hbc, hbcl = y });
            var result = filterFatories
                .Join(code_Lang,
                    x => x,
                    y => y.hbc.Code,
                    (x, y) => new { x, y.hbc, y.hbcl })
                 .Select(x => new KeyValuePair<string, string>(x.hbc.Code, $"{x.hbc.Code}-{(x.hbcl != null ? x.hbcl.Code_Name : x.hbc.Code_Name)}")).Distinct().ToList();
            return result;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListWorkShiftType(string language)
        {
            return await _repositoryAccessor.HRMS_Basic_Code
                .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.WorkShiftType, true)
                .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                    HBC => new { HBC.Type_Seq, HBC.Code },
                    HBCL => new { HBCL.Type_Seq, HBCL.Code },
                    (HBC, HBCL) => new { HBC, HBCL })
                    .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                    (prev, HBCL) => new { prev.HBC, HBCL })
                .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}"))
                .ToListAsync();
        }

        public Task<OperationResult> DownloadFileTemplate()
        {
            var path = Path.Combine(
                _webHostEnvironment.ContentRootPath, 
                "Resources\\Template\\AttendanceMaintenance\\5_1_4_OvertimeParameterSetting\\Template.xlsx"
            );
            var workbook = new Aspose.Cells.Workbook(path);
            var design = new Aspose.Cells.WorkbookDesigner(workbook);
            MemoryStream stream = new();
            design.Workbook.Save(stream, Aspose.Cells.SaveFormat.Xlsx);
            var result = stream.ToArray();
            return Task.FromResult(new OperationResult(true, null, result));
        }

    }
}