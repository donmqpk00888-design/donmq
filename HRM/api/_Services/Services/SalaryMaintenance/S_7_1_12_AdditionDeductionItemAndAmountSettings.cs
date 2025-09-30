using System.Globalization;
using API.Data;
using API._Services.Interfaces.SalaryMaintenance;
using API.DTOs.SalaryMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryMaintenance
{
    public class S_7_1_12_AdditionDeductionItemAndAmountSettings : BaseServices, I_7_1_12_AdditionDeductionItemAndAmountSettings
    {
        public S_7_1_12_AdditionDeductionItemAndAmountSettings(DBContext dbContext) : base(dbContext)
        {
        }

        #region Get Data Pagination
        public async Task<PaginationUtility<AdditionDeductionItemAndAmountSettingsDto>> GetDataPagination(PaginationParam pagination, AdditionDeductionItemAndAmountSettingsParam param)
        {
            var result = await GetData(param);
            return PaginationUtility<AdditionDeductionItemAndAmountSettingsDto>.Create(result, pagination.PageNumber, pagination.PageSize);
        }
        private async Task<List<AdditionDeductionItemAndAmountSettingsDto>> GetData(AdditionDeductionItemAndAmountSettingsParam param)
        {
            var pred = PredicateBuilder.New<HRMS_Sal_AddDedItem_Settings>(
                x => x.Factory == param.Factory
                  && x.AddDed_Type == param.AddDed_Type
                  && param.Permission_Group.Contains(x.Permission_Group)
            );

            var pred_Sal_Monthly = PredicateBuilder.New<HRMS_Sal_Monthly>(
                x => x.Factory == param.Factory
                  && param.Permission_Group.Contains(x.Permission_Group)
                  && x.Lock == "Y"
            );

            if (!string.IsNullOrWhiteSpace(param.Salary_Type))
                pred.And(x => x.Salary_Type == param.Salary_Type);

            if (!string.IsNullOrWhiteSpace(param.Effective_Month))
                pred.And(x => x.Effective_Month == Convert.ToDateTime(param.Effective_Month));

            if (!string.IsNullOrWhiteSpace(param.AddDed_Item))
                pred.And(x => x.AddDed_Item == param.AddDed_Item);

            var permissionGroup = await GetDataBasicCode(BasicCodeTypeConstant.PermissionGroup, param.Language);
            var salaryType = await GetListSalaryType(param.Language);
            var addDedType = await GetListAdditionsAndDeductionsType(param.Language);
            var addDedItem = await GetListAdditionsAndDeductionsItem(param.Language);

            var sal_Monthly = await _repositoryAccessor.HRMS_Sal_Monthly.FindAll(pred_Sal_Monthly, true).ToListAsync();
            var data = await _repositoryAccessor.HRMS_Sal_AddDedItem_Settings.FindAll(pred, true).ToListAsync();

            var result = data
                .Select(x => new AdditionDeductionItemAndAmountSettingsDto
                {
                    Factory = x.Factory,
                    Permission_Group = x.Permission_Group,
                    Permission_Group_Title = permissionGroup.FirstOrDefault(y => y.Key == x.Permission_Group).Value ?? string.Empty,
                    Salary_Type = x.Salary_Type,
                    Salary_Type_Title = salaryType.FirstOrDefault(y => y.Key == x.Salary_Type).Value ?? string.Empty,
                    Effective_Month = x.Effective_Month,
                    Effective_Month_Str = x.Effective_Month.ToString("yyyy/MM"),
                    AddDed_Type = x.AddDed_Type,
                    AddDed_Type_Title = addDedType.FirstOrDefault(y => y.Key == x.AddDed_Type).Value ?? string.Empty,
                    AddDed_Item = x.AddDed_Item,
                    AddDed_Item_Title = addDedItem.FirstOrDefault(y => y.Key == x.AddDed_Item).Value ?? string.Empty,
                    Amount = x.Amount,
                    Onjob_Print = x.Onjob_Print,
                    Resigned_Print = x.Resigned_Print,
                    Update_By = x.Update_By,
                    Update_Time = x.Update_Time,
                    IsDisable = sal_Monthly.Any(t => t.Factory == x.Factory
                                                  && t.Permission_Group == x.Permission_Group
                                                  && t.Salary_Type == x.Salary_Type
                                                  && t.Sal_Month >= x.Effective_Month
                                                  && t.Lock == "Y")
                })
                .OrderByDescending(x => x.Effective_Month)
                .ThenBy(x => x.Permission_Group)
                .ThenBy(x => x.Salary_Type)
                .ThenBy(x => x.AddDed_Type)
                .ThenBy(x => x.AddDed_Item)
                .ToList();
            return result;
        }
        #endregion

        #region GetDetail
        public async Task<AdditionDeductionItemAndAmountSettings_Form> GetDetail(AdditionDeductionItemAndAmountSettings_SubParam dto)
        {

            var HSAS = await _repositoryAccessor.HRMS_Sal_AddDedItem_Settings.FindAll(true).ToListAsync();
            var data = HSAS
                .FindAll(x => x.Factory == dto.Factory
                            && x.Permission_Group == dto.Permission_Group
                            && x.Salary_Type == dto.Salary_Type
                            && x.Effective_Month.Date == Convert.ToDateTime(dto.Effective_Month_Str).Date)
                .ToList();

            if (data is null)
                return new();

            var result = new AdditionDeductionItemAndAmountSettings_Form
            {
                Param = new AdditionDeductionItemAndAmountSettings_SubParam()
                {
                    Factory = dto.Factory,
                    Permission_Group = dto.Permission_Group,
                    Salary_Type = dto.Salary_Type,
                    Effective_Month = dto.Effective_Month,
                },

                Data = data.Select(x => new AdditionDeductionItemAndAmountSettings_SubData()
                {
                    AddDed_Type = x.AddDed_Type,
                    AddDed_Item = x.AddDed_Item,
                    Amount = x.Amount,
                    Onjob_Print = x.Onjob_Print,
                    Resigned_Print = x.Resigned_Print,
                    Update_By = x.Update_By,
                    Update_Time = x.Update_Time,
                    Update_Time_Str = x.Update_Time.ToString("yyyy/MM/dd HH:mm:ss")
                }).ToList()
            };

            return result;
        }
        #endregion

        #region CheckData
        public async Task<OperationResult> CheckData(AdditionDeductionItemAndAmountSettings_SubParam param, string userName)
        {
            bool isDuplicate = await _repositoryAccessor.HRMS_Sal_AddDedItem_Settings
                .AnyAsync(x => x.Factory == param.Factory
                            && x.Permission_Group == param.Permission_Group
                            && x.Salary_Type == param.Salary_Type
                            && x.Effective_Month == Convert.ToDateTime(param.Effective_Month_Str));

            if (isDuplicate)
                return new OperationResult(false, "Data input duplicate");

            var maxEffectiveMonth = await _repositoryAccessor.HRMS_Sal_AddDedItem_Settings
                    .FindAll(x => x.Factory == param.Factory
                             && x.Permission_Group == param.Permission_Group
                             && x.Salary_Type == param.Salary_Type
                             && x.Effective_Month < Convert.ToDateTime(param.Effective_Month_Str))
                    .Select(x => (DateTime?)x.Effective_Month)
                    .MaxAsync();

            DateTime now = DateTime.Now;

            var data = await _repositoryAccessor.HRMS_Sal_AddDedItem_Settings
                .FindAll(x => x.Factory == param.Factory
                        && x.Permission_Group == param.Permission_Group
                        && x.Salary_Type == param.Salary_Type
                        && x.Effective_Month == maxEffectiveMonth)
                .Select(x => new AdditionDeductionItemAndAmountSettings_SubData
                {
                    AddDed_Type = x.AddDed_Type,
                    AddDed_Item = x.AddDed_Item,
                    Amount = x.Amount,
                    Onjob_Print = x.Onjob_Print,
                    Resigned_Print = x.Resigned_Print,
                    Update_By = userName,
                    Update_Time = now,
                    Update_Time_Str = now.ToString("yyyy/MM/dd HH:mm:ss"),
                })
                .ToListAsync();

            return new OperationResult(true, data);
        }
        #endregion

        #region Create
        public async Task<OperationResult> Create(AdditionDeductionItemAndAmountSettings_Form dto)
        {
            var pred = PredicateBuilder.New<HRMS_Sal_AddDedItem_Settings>(true);
            if (string.IsNullOrWhiteSpace(dto.Param.Factory)
             || string.IsNullOrWhiteSpace(dto.Param.Permission_Group)
             || string.IsNullOrWhiteSpace(dto.Param.Salary_Type)
             || string.IsNullOrWhiteSpace(dto.Param.Effective_Month_Str)
             || !DateTime.TryParseExact(dto.Param.Effective_Month_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime effectiveMonthValue)
            )
                return new OperationResult(false, "InvalidInput");

            try
            {
                pred.And(x => x.Factory == dto.Param.Factory
                           && x.Permission_Group == dto.Param.Permission_Group
                           && x.Salary_Type == dto.Param.Salary_Type
                           && x.Effective_Month.Date == effectiveMonthValue.Date);

                var HSAS = await _repositoryAccessor.HRMS_Sal_AddDedItem_Settings.FindAll(pred).ToListAsync();
                List<HRMS_Sal_AddDedItem_Settings> addList = new();

                foreach (var item in dto.Data)
                {
                    if (string.IsNullOrWhiteSpace(item.AddDed_Type)
                     || string.IsNullOrWhiteSpace(item.AddDed_Item)
                     || string.IsNullOrWhiteSpace(item.Amount.ToString())
                     || string.IsNullOrWhiteSpace(item.Update_By)
                     || string.IsNullOrWhiteSpace(item.Update_Time_Str)
                     || !DateTime.TryParseExact(item.Update_Time_Str, "yyyy/MM/dd HH:mm:ss", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime updateTimeValue)
                    )
                        return new OperationResult(false, "InvalidInput");

                    if (HSAS.Any(x => x.AddDed_Type == item.AddDed_Type && x.AddDed_Item == item.AddDed_Item))
                        return new OperationResult(false, "AlreadyExitedData");

                    HRMS_Sal_AddDedItem_Settings addData = new()
                    {
                        Factory = dto.Param.Factory,
                        Permission_Group = dto.Param.Permission_Group,
                        Salary_Type = dto.Param.Salary_Type,
                        Effective_Month = effectiveMonthValue,
                        AddDed_Type = item.AddDed_Type,
                        AddDed_Item = item.AddDed_Item,
                        Amount = item.Amount,
                        Onjob_Print = item.Onjob_Print,
                        Resigned_Print = item.Resigned_Print,
                        Update_By = item.Update_By,
                        Update_Time = updateTimeValue
                    };

                    addList.Add(addData);
                }

                _repositoryAccessor.HRMS_Sal_AddDedItem_Settings.AddMultiple(addList);
                await _repositoryAccessor.Save();
                return new OperationResult(true);
            }
            catch (Exception)
            {
                return new OperationResult(false);
            }
        }
        #endregion

        #region Update
        public async Task<OperationResult> Update(AdditionDeductionItemAndAmountSettings_Form dto)
        {
            var pred = PredicateBuilder.New<HRMS_Sal_AddDedItem_Settings>(true);
            if (string.IsNullOrWhiteSpace(dto.Param.Factory)
             || string.IsNullOrWhiteSpace(dto.Param.Permission_Group)
             || string.IsNullOrWhiteSpace(dto.Param.Salary_Type)
             || string.IsNullOrWhiteSpace(dto.Param.Effective_Month_Str)
             || !DateTime.TryParseExact(dto.Param.Effective_Month_Str, "yyyy/MM/dd", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime effectiveMonthValue)
            )
                return new OperationResult(false, "InvalidInput");

            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                pred.And(x => x.Factory == dto.Param.Factory
                           && x.Permission_Group == dto.Param.Permission_Group
                           && x.Salary_Type == dto.Param.Salary_Type
                           && x.Effective_Month.Date == effectiveMonthValue.Date);

                var removeList = await _repositoryAccessor.HRMS_Sal_AddDedItem_Settings.FindAll(pred).ToListAsync();
                if (!removeList.Any())
                    return new OperationResult(false, "Not Exited Data");

                List<HRMS_Sal_AddDedItem_Settings> addList = new();

                foreach (var item in dto.Data)
                {
                    if (string.IsNullOrWhiteSpace(item.AddDed_Type)
                     || string.IsNullOrWhiteSpace(item.AddDed_Item)
                     || string.IsNullOrWhiteSpace(item.Amount.ToString())
                     || string.IsNullOrWhiteSpace(item.Update_By)
                     || string.IsNullOrWhiteSpace(item.Update_Time_Str)
                     || !DateTime.TryParseExact(item.Update_Time_Str, "yyyy/MM/dd HH:mm:ss", new CultureInfo("en-US"), DateTimeStyles.None, out DateTime updateTimeValue)
                    )
                        return new OperationResult(false, "Invalid Input");

                    HRMS_Sal_AddDedItem_Settings addData = new()
                    {
                        Factory = dto.Param.Factory,
                        Permission_Group = dto.Param.Permission_Group,
                        Effective_Month = effectiveMonthValue,
                        Salary_Type = dto.Param.Salary_Type,
                        AddDed_Type = item.AddDed_Type,
                        AddDed_Item = item.AddDed_Item,
                        Amount = item.Amount,
                        Onjob_Print = item.Onjob_Print,
                        Resigned_Print = item.Resigned_Print,
                        Update_By = item.Update_By,
                        Update_Time = updateTimeValue
                    };

                    addList.Add(addData);
                }

                _repositoryAccessor.HRMS_Sal_AddDedItem_Settings.RemoveMultiple(removeList);
                await _repositoryAccessor.Save();

                _repositoryAccessor.HRMS_Sal_AddDedItem_Settings.AddMultiple(addList);
                await _repositoryAccessor.Save();
                await _repositoryAccessor.CommitAsync();
                return new OperationResult(true);
            }
            catch (Exception)
            {
                await _repositoryAccessor.RollbackAsync();
                return new OperationResult(false);
            }
        }
        #endregion

        #region Delete
        public async Task<OperationResult> Delete(AdditionDeductionItemAndAmountSettingsDto dto)
        {
            var data = await _repositoryAccessor.HRMS_Sal_AddDedItem_Settings.FirstOrDefaultAsync(
                x => x.Factory == dto.Factory
                    && x.Permission_Group == dto.Permission_Group
                    && x.Salary_Type == dto.Salary_Type
                    && x.Effective_Month == Convert.ToDateTime(dto.Effective_Month_Str)
                    && x.AddDed_Type == dto.AddDed_Type
                    && x.AddDed_Item == dto.AddDed_Item
            );
            if (data is null)
                return new OperationResult(false, "Data not existed");

            try
            {
                _repositoryAccessor.HRMS_Sal_AddDedItem_Settings.Remove(data);
                await _repositoryAccessor.Save();
                return new OperationResult(true);
            }
            catch (Exception)
            {
                return new OperationResult(false);
            }
        }
        #endregion

        #region Get List
        public async Task<List<KeyValuePair<string, string>>> GetListFactoryByUser(string userName, string language)
        {
            var factoriesByAccount = await Queryt_Factory_AddList(userName);
            var factories = await GetDataBasicCode(BasicCodeTypeConstant.Factory, language);

            return factories.IntersectBy(factoriesByAccount, x => x.Key).ToList();
        }

        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroupByFactory(string factory, string language)
        {
            var permissionGroupByFactory = await Query_Permission_Group_List(factory);
            var permissionGroup = await GetDataBasicCode(BasicCodeTypeConstant.PermissionGroup, language);
            return permissionGroup.IntersectBy(permissionGroupByFactory, x => x.Key).ToList();
        }

        public async Task<List<KeyValuePair<string, string>>> GetListSalaryType(string language)
        {
            return await GetDataBasicCode(BasicCodeTypeConstant.SalaryType, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListAdditionsAndDeductionsType(string language)
        {
            return await GetDataBasicCode(BasicCodeTypeConstant.AdditionsAndDeductionsType, language);
        }

        public async Task<List<KeyValuePair<string, string>>> GetListAdditionsAndDeductionsItem(string language)
        {
            return await GetDataBasicCode(BasicCodeTypeConstant.AdditionsAndDeductionsItem, language);
        }
        #region DowloadExcel
        public async Task<OperationResult> DownloadFileExcel(AdditionDeductionItemAndAmountSettingsParam param, string userName)
        {
            var data = await GetData(param);
            if (!data.Any())
                return new OperationResult(false, "System.Message.NoData");

            List<Cell> dataCells = new()
            {
                new Cell("B" + 2, userName),
                new Cell("D" + 2, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))
            };

            var index = 6;
            for (int i = 0; i < data.Count; i++)
            {
                dataCells.Add(new Cell("A" + index, data[i].Factory));
                dataCells.Add(new Cell("B" + index, data[i].Effective_Month));
                dataCells.Add(new Cell("C" + index, data[i].Permission_Group_Title));
                dataCells.Add(new Cell("D" + index, data[i].Salary_Type_Title));

                dataCells.Add(new Cell("E" + index, data[i].AddDed_Type_Title));
                dataCells.Add(new Cell("F" + index, data[i].AddDed_Item_Title));
                dataCells.Add(new Cell("G" + index, data[i].Amount));
                dataCells.Add(new Cell("H" + index, data[i].Onjob_Print));
                dataCells.Add(new Cell("I" + index, data[i].Resigned_Print));
                dataCells.Add(new Cell("J" + index, data[i].Update_By));
                dataCells.Add(new Cell("K" + index, data[i].Update_Time.ToString("yyyy/MM/dd HH:mm:ss")));
                index += 1;
            }

            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                dataCells,
                "Resources\\Template\\SalaryMaintenance\\7_1_12_AdditionDeductionItemAndAmountSettings\\Download.xlsx"
            );
            return new OperationResult(excelResult.IsSuccess, excelResult.Error, excelResult.Result);
        }
        #endregion
        #endregion
    }
}