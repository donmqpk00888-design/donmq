using API.Data;
using API._Services.Interfaces.SalaryMaintenance;
using API.DTOs;
using API.DTOs.SalaryMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.SalaryMaintenance
{
    public class S_7_1_24_MonthlySalaryMaintenance : BaseServices, I_7_1_24_MonthlySalaryMaintenance
    {
        public S_7_1_24_MonthlySalaryMaintenance(DBContext dbContext) : base(dbContext)
        {
        }

        public async Task<PaginationUtility<MonthlySalaryMaintenanceDto>> GetDataPagination(PaginationParam pagination, MonthlySalaryMaintenanceParam param)
        {
            var predProbation = PredicateBuilder.New<HRMS_Sal_Probation_Monthly>(x =>
                x.Factory == param.Factory &&
                param.Permission_Group.Contains(x.Permission_Group) &&
                x.Sal_Month == Convert.ToDateTime(param.SalMonth)
            );

            var predSalary = PredicateBuilder.New<HRMS_Sal_Monthly>(x =>
                x.Factory == param.Factory &&
                param.Permission_Group.Contains(x.Permission_Group) &&
                x.Sal_Month == Convert.ToDateTime(param.SalMonth)
            );

            if (!string.IsNullOrWhiteSpace(param.Employee_ID))
            {
                predProbation.And(x => x.Employee_ID == param.Employee_ID);
                predSalary.And(x => x.Employee_ID == param.Employee_ID);
            }

            if (!string.IsNullOrWhiteSpace(param.Department))
            {
                predProbation.And(x => x.Department == param.Department);
                predSalary.And(x => x.Department == param.Department);
            }

            var department = await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == param.Factory, true)
                                   .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language
                                   .FindAll(x => x.Language_Code.ToLower() == param.Language.ToLower(), true),
                                       x => new { x.Division, x.Factory, x.Department_Code },
                                       y => new { y.Division, y.Factory, y.Department_Code },
                                       (x, y) => new { HOD = x, HODL = y })
                                   .SelectMany(x => x.HODL.DefaultIfEmpty(),
                                       (x, y) => new { x.HOD, HODL = y })
                                   .Select(x => new
                                   {
                                       Code = x.HOD.Department_Code,
                                       Name = x.HODL != null ? x.HODL.Name : x.HOD.Department_Name,
                                       x.HOD.Division,
                                       x.HOD.Factory,
                                   }).Distinct().ToListAsync();

            var HSC = await _repositoryAccessor.HRMS_Sal_Close.FindAll(true).ToListAsync();
            var HSPM = await _repositoryAccessor.HRMS_Sal_Probation_Monthly.FindAll(predProbation, true).ToListAsync();
            var HSM = await _repositoryAccessor.HRMS_Sal_Monthly.FindAll(predSalary, true).ToListAsync();
            var HEP = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(true).ToListAsync();

            // HRMS_Sal_Probation_Monthly Probation = 'Y' (Seq = 1)
            var probationYData = HSPM.Where(x => x.Probation == "Y")
                                .Join(HSM,
                                    prob => new { prob.Factory, prob.Sal_Month, prob.Employee_ID },
                                    monthly => new { monthly.Factory, monthly.Sal_Month, monthly.Employee_ID },
                                    (prob, monthly) => new { HSPM = prob, HSM = monthly })
                                .GroupJoin(HEP,
                                    x => x.HSM.USER_GUID,
                                    y => y.USER_GUID,
                                    (x, y) => new { x.HSPM, x.HSM, HEP = y })
                                .SelectMany(x => x.HEP.DefaultIfEmpty(),
                                    (x, y) => new { x.HSPM, x.HSM, HEP = y })
                                .ToList();

            // HRMS_Sal_Probation_Monthly Probation = 'N' (Seq = 2)
            var probationNData = HSPM.Where(x => x.Probation == "N")
                                .Join(HSM,
                                    prob => new { prob.Factory, prob.Sal_Month, prob.Employee_ID },
                                    monthly => new { monthly.Factory, monthly.Sal_Month, monthly.Employee_ID },
                                    (prob, monthly) => new { HSPM = prob, HSM = monthly })
                                .GroupJoin(HEP,
                                    x => x.HSM.USER_GUID,
                                    y => y.USER_GUID,
                                    (x, y) => new { x.HSPM, x.HSM, HEP = y })
                                .SelectMany(x => x.HEP.DefaultIfEmpty(),
                                    (x, y) => new { x.HSPM, x.HSM, HEP = y })
                                .ToList();

            // HRMS_Sal_Monthly Probation = 'ALL' (Seq = 3)
            var monthlyData = HSM.GroupJoin(HEP,
                                    x => x.USER_GUID,
                                    y => y.USER_GUID,
                                    (x, y) => new { HSM = x, HEP = y })
                                .SelectMany(x => x.HEP.DefaultIfEmpty(),
                                    (x, y) => new { x.HSM, HEP = y })
                                .ToList();

            // Probation = 'Y' (Seq = 1)
            var probationYResult = probationYData.Select(x => CreateMainDto(x.HSM, x.HEP, "1", x.HSPM.Probation, x.HSPM.Tax));
            // Probation = 'N' (Seq = 2)
            var probationNResult = probationNData.Select(x => CreateMainDto(x.HSM, x.HEP, "2", x.HSPM.Probation, x.HSPM.Tax));
            // Probation = 'ALL' (Seq = 3)
            var monthlyResult = monthlyData.Select(x => CreateMainDto(x.HSM, x.HEP, "3", "ALL", x.HSM.Tax));

            // Union
            var combinedData = probationYResult
                              .Union(probationNResult)
                              .Union(monthlyResult)
                              .OrderBy(x => x.Employee_ID)
                              .ThenBy(x => x.Seq)
                              .ToList();

            var result = combinedData.Select(item =>
            {
                item.DepartmentName = department.FirstOrDefault(y => y.Code == item.Department)?.Name ?? string.Empty;
                item.FIN_Pass_Status = HSC.FirstOrDefault(y => y.Factory == item.Factory && y.Sal_Month == item.Sal_Month && y.Employee_ID == item.Employee_ID)?.Close_Status ?? string.Empty;
                item.IsDelete = HSC.Any(y => y.Factory == item.Factory && y.Sal_Month == item.Sal_Month && y.Employee_ID == item.Employee_ID && y.Close_Status == "Y");
                return item;
            }).ToList();

            return PaginationUtility<MonthlySalaryMaintenanceDto>.Create(result, pagination.PageNumber, pagination.PageSize);
        }

        private static MonthlySalaryMaintenanceDto CreateMainDto(dynamic salaryData, HRMS_Emp_Personal personal, string seq, string probationFlag, int tax)
        {
            return new MonthlySalaryMaintenanceDto
            {
                Seq = seq,
                Probation = probationFlag,
                USER_GUID = salaryData.USER_GUID,
                Lock = salaryData.Lock,
                Sal_Month = salaryData.Sal_Month,
                Factory = salaryData.Factory,
                Department = salaryData.Department,
                Employee_ID = salaryData.Employee_ID,
                Local_Full_Name = personal.Local_Full_Name,
                Permission_Group = salaryData.Permission_Group,
                Salary_Type = salaryData.Salary_Type,
                BankTransfer = salaryData.BankTransfer,
                Currency = salaryData.Currency,
                Tax = tax,
                Update_By = salaryData.Update_By,
                Update_Time = salaryData.Update_Time.ToString("yyyy/MM/dd HH:mm:ss"),
            };
        }

        public async Task<MonthlySalaryMaintenanceDetail> GetDetail(MonthlySalaryMaintenanceDto item)
        {
            #region MonthlyAttendanceData
            MonthlySalaryMaintenanceDetail result = new();
            TableDataList tableData = new()
            {
                HAMD = _repositoryAccessor.HRMS_Att_Monthly_Detail.FindAll(x => x.Factory == item.Factory && x.Att_Month == item.Sal_Month && x.Employee_ID == item.Employee_ID, true).ToList(),
                HAUML = _repositoryAccessor.HRMS_Att_Use_Monthly_Leave.FindAll(x => x.Factory == item.Factory, true).ToList(),
                HSIS = _repositoryAccessor.HRMS_Sal_Item_Settings.FindAll(x => x.Factory == item.Factory && x.Permission_Group == item.Permission_Group && x.Salary_Type == item.Salary_Type, true).ToList(),
                HSMD = _repositoryAccessor.HRMS_Sal_Monthly_Detail.FindAll(x => x.Factory == item.Factory && x.Sal_Month == item.Sal_Month && x.Employee_ID == item.Employee_ID, true).ToList(),
            };
            var queryAttMonthly = Query_Att_Monthly(item.Factory, item.Sal_Month, item.Employee_ID).Result;
            result.PaidSalaryDays = queryAttMonthly.Salary_Days;
            result.ActualWorkDays = queryAttMonthly.Actual_Days;
            result.NewHiredResigned = queryAttMonthly.Resign_Status;

            result.DelayEarly = queryAttMonthly.Delay_Early;
            result.NoSwipCard = queryAttMonthly.No_Swip_Card;
            result.DayShiftMealTimes = queryAttMonthly.DayShift_Food;
            result.OvertimeMealTimes = queryAttMonthly.Food_Expenses;
            result.NightShiftAllowanceTimes = queryAttMonthly.Night_Eat_Times;
            result.NightShiftMealTimes = queryAttMonthly.NightShift_Food;
            var queryLeave = await Query_Att_Monthly_Detail(item.Sal_Month, "1", tableData);
            var queryAllowance = await Query_Att_Monthly_Detail(item.Sal_Month, "2", tableData);

            var leaveType = await GetLeaveTypes(item.Language);
            // Overtime&Night 
            var allowances = await GetAllowances(item.Language);
            //Salary Item 
            var salaryItem = await GetSalaryItem(item.Language);
            //Addition Item && Deductions
            var additionItem = await GetAdditionItem(item.Language);
            //Insurance deductions
            var insuranceType = await GetInsuranceType(item.Language);

            result.Leave = queryLeave.Select(l => new MonthlyAttendanceData_Leave
            {
                Leave = l.Leave_Code,
                Leave_Name = leaveType.FirstOrDefault(x => x.Key == l.Leave_Code).Value,
                MonthlyDays = l.Days
            }).ToList();

            result.Allowance = queryAllowance.Select(a => new MonthlyAttendanceData_Allowance
            {
                Allowance = a.Leave_Code,
                Allowance_Name = allowances.FirstOrDefault(x => x.Key == a.Leave_Code).Value,
                MonthlyDays = a.Days
            }).ToList();
            #endregion

            #region MonthlySallaryDetail
            var probationYN = new List<string> { "Y", "N" };
            List<Sal_Monthly_Detail_Values> query_45 = new();
            List<Sal_Monthly_Detail_Values> query_42 = new();
            List<Sal_Monthly_Detail_Values> query_49_A = new();
            List<Sal_Monthly_Detail_Values> query_49_B = new();
            List<Sal_Monthly_Detail_Values> query_49_C = new();
            List<Sal_Monthly_Detail_Values> query_49_D = new();
            List<Sal_Monthly_Detail_Values> query_57 = new();

            // -月份薪資明細_核薪項目- (Chi tiết lương tháng mục xác minh lương) / Salary Item
            if (item.Probation == "Y")
                query_45 = await Query_Sal_Monthly_Detail("PY", item.Factory, item.Sal_Month, item.Employee_ID,
                                                            "45", "A", item.Permission_Group, item.Salary_Type, "0");
            else if (item.Probation == "N")
                query_45 = await Query_Sal_Monthly_Detail("PN", item.Factory, item.Sal_Month, item.Employee_ID,
                                                            "45", "A", item.Permission_Group, item.Salary_Type, "0");
            else
                query_45 = await Query_Sal_Monthly_Detail("Y", item.Factory, item.Sal_Month, item.Employee_ID,
                                                            "45", "A", item.Permission_Group, item.Salary_Type, "0");
            // -月份薪資明細_加班及夜班津貼- (Chi tiết lương tháng phụ cấp làm thêm giờ và ca đêm) / Overtime&Night 
            if (item.Probation == "Y")
                query_42 = await Query_Sal_Monthly_Detail("PY", item.Factory, item.Sal_Month, item.Employee_ID,
                                                            "42", "A", item.Permission_Group, item.Salary_Type, "2");
            else if (item.Probation == "N")
                query_42 = await Query_Sal_Monthly_Detail("PN", item.Factory, item.Sal_Month, item.Employee_ID,
                                                            "42", "A", item.Permission_Group, item.Salary_Type, "2");
            else
                query_42 = await Query_Sal_Monthly_Detail("Y", item.Factory, item.Sal_Month, item.Employee_ID,
                                                            "42", "A", item.Permission_Group, item.Salary_Type, "2");
            // -月份薪資明細_加項- (Chi tiết lương hàng tháng các mục bổ sung) / Addition Item
            if (!probationYN.Contains(item.Probation))
            {
                query_49_A = await Query_Sal_Monthly_Detail("Y", item.Factory, item.Sal_Month, item.Employee_ID,
                                                            "49", "A", item.Permission_Group, item.Salary_Type, "0");
                query_49_B = await Query_Sal_Monthly_Detail("Y", item.Factory, item.Sal_Month, item.Employee_ID,
                                                               "49", "B", item.Permission_Group, item.Salary_Type, "0");
            }
            // -月份薪資明細_扣項- (Chi tiết lương tháng khấu trừ) / Deductions
            if (!probationYN.Contains(item.Probation))
            {
                query_49_C = await Query_Sal_Monthly_Detail("Y", item.Factory, item.Sal_Month, item.Employee_ID,
                                                            "49", "C", item.Permission_Group, item.Salary_Type, "0");
                query_49_D = await Query_Sal_Monthly_Detail("Y", item.Factory, item.Sal_Month, item.Employee_ID,
                                                            "49", "D", item.Permission_Group, item.Salary_Type, "0");
            }
            // -月份薪資明細_保險扣項- (Chi tiết lương hàng tháng khấu trừ bảo hiểm) / Insurance deductions 
            if (!probationYN.Contains(item.Probation))
                query_57 = await Query_Sal_Monthly_Detail("Y", item.Factory, item.Sal_Month, item.Employee_ID,
                                                               "57", "D", item.Permission_Group, item.Salary_Type, "0");

            var query_45_converted = query_45.Select(x => new Query_Sal_Monthly_Detail { Item = x.Item, Amount = x.Amount }).ToList();
            var query_42_converted = query_42.Select(x => new Query_Sal_Monthly_Detail { Item = x.Item, Amount = x.Amount }).ToList();
            var query_49_A_converted = query_49_A.Select(x => new Query_Sal_Monthly_Detail { Item = x.Item, Amount = x.Amount }).ToList();
            var query_49_B_converted = query_49_B.Select(x => new Query_Sal_Monthly_Detail { Item = x.Item, Amount = x.Amount }).ToList();
            var query_49_C_converted = query_49_C.Select(x => new Query_Sal_Monthly_Detail { Item = x.Item, Amount = x.Amount }).ToList();
            var query_49_D_converted = query_49_D.Select(x => new Query_Sal_Monthly_Detail { Item = x.Item, Amount = x.Amount }).ToList();
            var query_57_converted = query_57.Select(x => new Query_Sal_Monthly_Detail { Item = x.Item, Amount = x.Amount }).ToList();

            var dataTable_1 = await GetDataTable(query_45_converted, salaryItem);
            var dataTable_2 = await GetDataTable(query_42_converted, allowances);
            var dataTable_3 = await GetDataTable(query_49_A_converted.Concat(query_49_B_converted).OrderBy(x => x.Item).ToList(), additionItem);
            var dataTable_4 = await GetDataTable(query_49_C_converted.Concat(query_49_D_converted).OrderBy(x => x.Item).ToList(), additionItem);
            var dataTable_5 = await GetDataTable(query_57_converted, insuranceType);
            result.SalaryDetail.TotalAmountReceived = dataTable_1.SumAmount + dataTable_2.SumAmount
                                                        + dataTable_3.SumAmount - dataTable_4.SumAmount - dataTable_5.SumAmount - item.Tax;

            result.SalaryDetail.Table_1.Add(dataTable_1);
            result.SalaryDetail.Table_2.Add(dataTable_2);
            result.SalaryDetail.Table_3.Add(dataTable_3);
            result.SalaryDetail.Table_4.Add(dataTable_4);
            result.SalaryDetail.Table_5.Add(dataTable_5);

            #endregion
            return result;
        }

        private static Task<MonthlySallaryDetail_Table> GetDataTable(List<Query_Sal_Monthly_Detail> listData, List<KeyValuePair<string, string>> listName)
        {
            var table = new MonthlySallaryDetail_Table
            {
                ListItem = listData.Select(q => new MonthlySallaryDetail_Item
                {
                    Item = q.Item,
                    Item_Name = listName.FirstOrDefault(x => x.Key == q.Item).Value ?? q.Item,
                    Amount = q.Amount
                }).ToList()
            };

            table.SumAmount = table.ListItem.Sum(x => x.Amount);

            return Task.FromResult(table);

        }
        // Spec: if Kind = Y
        private async Task<HRMS_Att_Monthly> Query_Att_Monthly(string factory, DateTime attMonth, string employee_ID)
        {

            return await _repositoryAccessor.HRMS_Att_Monthly
                                .FirstOrDefaultAsync(x => x.Factory == factory && x.Att_Month == attMonth && x.Employee_ID == employee_ID, true);
        }

        private static Task<List<Query_Att_Monthly_Detail>> Query_Att_Monthly_Detail(DateTime attMonth, string leaveType, TableDataList tableData)
        {
            List<HRMS_Att_Monthly_Detail> attMonthlyDetailTemp = tableData.HAMD.FindAll(x => x.Leave_Type == leaveType).ToList();
            List<SettingTemp> settingTemp = new();
            var dataHAUML = tableData.HAUML.FindAll(x => x.Leave_Type == leaveType && x.Effective_Month <= attMonth).ToList();
            if (dataHAUML.Any())
            {
                var maxEffectiveMonth = dataHAUML.Max(x => x.Effective_Month);
                // Lấy các thiết lập tương ứng với tháng hiệu lực tối đa
                settingTemp = tableData.HAUML.FindAll(x => x.Leave_Type == leaveType && x.Effective_Month == maxEffectiveMonth)
                    .Select(x => new SettingTemp { Seq = x.Seq, Code = x.Code })
                    .ToList();
            }

            var result = attMonthlyDetailTemp.GroupJoin(settingTemp,
                x => x.Leave_Code,
                y => y.Code,
                (x, y) => new { temp = x, settingTemp = y })
                .SelectMany(x => x.settingTemp.DefaultIfEmpty(),
                    (x, y) => new { x.temp, settingTemp = y })
                .Select(x => new Query_Att_Monthly_Detail
                {
                    Leave_Code = x.temp.Leave_Code,
                    Days = x.temp.Days,
                    Seq = x.settingTemp.Seq
                })
                .OrderBy(x => x.Seq)
                .ToList();

            return Task.FromResult(result);
        }

        private async Task<List<KeyValuePair<string, string>>> GetAllowances(string language)
        {
            var data = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Allowance && x.IsActive == true, true)
                        .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
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

        private async Task<List<KeyValuePair<string, string>>> GetSalaryItem(string language)
        {
            var data = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.SalaryItem && x.IsActive == true, true)
                        .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
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

        private async Task<List<KeyValuePair<string, string>>> GetAdditionItem(string language)
        {
            var data = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.AdditionsAndDeductionsItem && x.IsActive == true, true)
                        .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
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

        private async Task<List<KeyValuePair<string, string>>> GetInsuranceType(string language)
        {
            var data = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.InsuranceType && x.IsActive == true, true)
                        .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
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

        private async Task<List<KeyValuePair<string, string>>> GetLeaveTypes(string language)
        {
            var data = await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == BasicCodeTypeConstant.Leave && x.IsActive == true && x.Char1 == "Leave", true)
                        .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
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
        public async Task<List<string>> GetListTypeHeadEmployeeID(string factory)
        => await _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == factory, true).Select(x => x.Employee_ID).Distinct().ToListAsync();
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
        public async Task<List<KeyValuePair<string, string>>> GetListDepartment(string language, string factory)
        {
            var result = await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == factory, true)
                .Join(_repositoryAccessor.HRMS_Basic_Factory_Comparison.FindAll(b => b.Kind == "1" && b.Factory == factory, true),
                    x => x.Division,
                    y => y.Division,
                    (x, y) => x)
                .GroupJoin(_repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
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
        public async Task<List<KeyValuePair<string, string>>> GetListPermissionGroup(string factory, string language)
        {
            var permissionGroups = await Query_Permission_List(factory);
            var permissionGroupsWithLanguage = await _repositoryAccessor.HRMS_Basic_Code
                            .FindAll(x => x.Type_Seq == BasicCodeTypeConstant.PermissionGroup && permissionGroups.Select(x => x.Permission_Group).Contains(x.Code), true)
                            .GroupJoin(_repositoryAccessor.HRMS_Basic_Code_Language.FindAll(x => x.Language_Code.ToLower() == language.ToLower(), true),
                                x => new { x.Type_Seq, x.Code },
                                y => new { y.Type_Seq, y.Code },
                                (HBC, HBCL) => new { HBC, HBCL })
                            .SelectMany(x => x.HBCL.DefaultIfEmpty(),
                                (x, y) => new { x.HBC, HBCL = y })
                            .Select(x => new KeyValuePair<string, string>(x.HBC.Code, $"{x.HBC.Code} - {(x.HBCL != null ? x.HBCL.Code_Name : x.HBC.Code_Name)}")).ToListAsync();
            return permissionGroupsWithLanguage;
        }

        public async Task<MonthlySalaryMaintenance_Personal> GetDetailPersonal(string factory, string employee_ID)
        {
            var data = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(x => x.Factory == factory && x.Employee_ID == employee_ID, true)
                        .Select(x => new MonthlySalaryMaintenance_Personal
                        {
                            Local_Full_Name = x.Local_Full_Name,
                            USER_GUID = x.USER_GUID,
                        }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListSalaryType(string language)
        {
            return await GetDataBasicCode(BasicCodeTypeConstant.SalaryType, language);
        }

        public async Task<OperationResult> Update(MonthlySalaryMaintenance_Update dto)
        {
            RemoveUnwantedItems(dto.SalaryDetail.Table_1);
            RemoveUnwantedItems(dto.SalaryDetail.Table_2);
            RemoveUnwantedItems(dto.SalaryDetail.Table_3);
            RemoveUnwantedItems(dto.SalaryDetail.Table_4);
            RemoveUnwantedItems(dto.SalaryDetail.Table_5);

            var updateTime = Convert.ToDateTime(dto.Update_Time);

            var item = await _repositoryAccessor.HRMS_Sal_Monthly.FirstOrDefaultAsync(x => x.Factory == dto.Factory && x.Sal_Month == Convert.ToDateTime(dto.Sal_Month)
                                                                                   && x.Employee_ID == dto.Employee_ID);
            if (item is not null)
            {
                item.Update_Time = updateTime;
                item.Update_By = dto.Update_By;
                item.Tax = dto.Tax;

                _repositoryAccessor.HRMS_Sal_Monthly.Update(item);
            }

            // HRMS_Sal_Monthly_Detail
            var itemDetail = await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                .FindAll(x => x.Factory == dto.Factory && x.Sal_Month == Convert.ToDateTime(dto.Sal_Month)
                               && x.Employee_ID == dto.Employee_ID)
                .ToListAsync();

            var dataUpdate = new List<HRMS_Sal_Monthly_Detail>();
            // Cập nhật cho từng bảng
            UpdateItemDetails(dto.SalaryDetail.Table_1, itemDetail, "45", "A", updateTime, dto.Update_By, dataUpdate);
            UpdateItemDetails(dto.SalaryDetail.Table_2, itemDetail, "42", "A", updateTime, dto.Update_By, dataUpdate);
            UpdateItemDetails(dto.SalaryDetail.Table_3, itemDetail, "49", "A", updateTime, dto.Update_By, dataUpdate);
            UpdateItemDetails(dto.SalaryDetail.Table_3, itemDetail, "49", "B", updateTime, dto.Update_By, dataUpdate);
            UpdateItemDetails(dto.SalaryDetail.Table_4, itemDetail, "49", "C", updateTime, dto.Update_By, dataUpdate);
            UpdateItemDetails(dto.SalaryDetail.Table_4, itemDetail, "49", "D", updateTime, dto.Update_By, dataUpdate);
            UpdateItemDetails(dto.SalaryDetail.Table_5, itemDetail, "57", "D", updateTime, dto.Update_By, dataUpdate);

            try
            {
                _repositoryAccessor.HRMS_Sal_Monthly_Detail.UpdateMultiple(dataUpdate);
                await _repositoryAccessor.Save();
                return new OperationResult(true, "System.Message.UpdateOKMsg");
            }
            catch (System.Exception)
            {
                return new OperationResult(false, "System.Message.UpdateErrorMsg");
            }
        }
        private static void RemoveUnwantedItems(List<MonthlySallaryDetail_Table> tables)
        {
            foreach (var table in tables)
            {
                table.ListItem = table.ListItem.Where(listItem => listItem.Item != "...").ToList();
            }
        }

        private static void UpdateItemDetails(List<MonthlySallaryDetail_Table> salaryDetailTable, List<HRMS_Sal_Monthly_Detail> itemDetail, string typeSeq, string addDedType, DateTime updateTime, string updateBy, List<HRMS_Sal_Monthly_Detail> dataUpdate)
        {
            var dataTemp = itemDetail.FindAll(x => x.Type_Seq == typeSeq && x.AddDed_Type == addDedType).ToList();

            foreach (var itemTemp in salaryDetailTable)
            {
                foreach (var itemTemp_Sub in itemTemp.ListItem)
                {
                    var itemDetailTemp = dataTemp.FirstOrDefault(x => x.Item == itemTemp_Sub.Item);
                    if (itemDetailTemp != null)
                    {
                        itemDetailTemp.Amount = itemTemp_Sub.Amount;
                        itemDetailTemp.Update_Time = updateTime;
                        itemDetailTemp.Update_By = updateBy;

                        dataUpdate.Add(itemDetailTemp);
                    }
                }
            }
        }
        public async Task<OperationResult> Delete(MonthlySalaryMaintenance_Delete dto)
        {
            await _repositoryAccessor.BeginTransactionAsync();
            try
            {
                var salMonth = Convert.ToDateTime(dto.Sal_Month);

                var probationDetailList = await _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail
                    .FindAll(x => x.Factory == dto.Factory && x.Employee_ID == dto.Employee_ID && x.Sal_Month == salMonth).ToListAsync();
                var probationList = await _repositoryAccessor.HRMS_Sal_Probation_Monthly
                    .FindAll(x => x.Factory == dto.Factory && x.Employee_ID == dto.Employee_ID && x.Sal_Month == salMonth).ToListAsync();
                var closeList = await _repositoryAccessor.HRMS_Sal_Close
                    .FindAll(x => x.Factory == dto.Factory && x.Employee_ID == dto.Employee_ID && x.Sal_Month == salMonth).ToListAsync();
                var taxList = await _repositoryAccessor.HRMS_Sal_Tax
                    .FindAll(x => x.Factory == dto.Factory && x.Employee_ID == dto.Employee_ID && x.Sal_Month == salMonth).ToListAsync();
                var monthlyDetailList = await _repositoryAccessor.HRMS_Sal_Monthly_Detail
                    .FindAll(x => x.Factory == dto.Factory && x.Employee_ID == dto.Employee_ID && x.Sal_Month == salMonth).ToListAsync();
                var monthlyList = await _repositoryAccessor.HRMS_Sal_Monthly
                    .FindAll(x => x.Factory == dto.Factory && x.Employee_ID == dto.Employee_ID && x.Sal_Month == salMonth).ToListAsync();

                if (probationDetailList.Any())
                    _repositoryAccessor.HRMS_Sal_Probation_Monthly_Detail.RemoveMultiple(probationDetailList);
                if (probationList.Any())
                    _repositoryAccessor.HRMS_Sal_Probation_Monthly.RemoveMultiple(probationList);
                if (closeList.Any())
                    _repositoryAccessor.HRMS_Sal_Close.RemoveMultiple(closeList);
                if (taxList.Any())
                    _repositoryAccessor.HRMS_Sal_Tax.RemoveMultiple(taxList);
                if (monthlyDetailList.Any())
                    _repositoryAccessor.HRMS_Sal_Monthly_Detail.RemoveMultiple(monthlyDetailList);
                if (monthlyList.Any())
                    _repositoryAccessor.HRMS_Sal_Monthly.RemoveMultiple(monthlyList);

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
    }
}