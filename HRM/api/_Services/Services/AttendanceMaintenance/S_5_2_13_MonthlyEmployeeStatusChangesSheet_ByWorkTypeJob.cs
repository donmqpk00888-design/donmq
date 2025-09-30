using System.Drawing;
using API.Data;
using API._Services.Interfaces.AttendanceMaintenance;
using API.DTOs.AttendanceMaintenance;
using API.Helper.Constant;
using API.Models;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace API._Services.Services.AttendanceMaintenance
{
    public class S_5_2_13_MonthlyEmployeeStatusChangesSheet_ByWorkTypeJob : BaseServices, I_5_2_13_MonthlyEmployeeStatusChangesSheet_ByWorkTypeJob
    {
        public S_5_2_13_MonthlyEmployeeStatusChangesSheet_ByWorkTypeJob(DBContext dbContext) : base(dbContext)
        {
        }

        #region Get Dropdown List
        public async Task<List<KeyValuePair<string, string>>> GetFactories(List<string> roleList, string language) 
        => await Query_Factory_AddList(roleList, language);
        public async Task<List<KeyValuePair<string, string>>> GetPermistionGroups(string factory, string language) 
        => await Query_BasicCode_PermissionGroup(factory, language);
        public async Task<List<KeyValuePair<string, string>>> GetLevels(string language) => await GetDataBasicCode(BasicCodeTypeConstant.Level, language);
        public async Task<List<KeyValuePair<string, string>>> GetWorkTypeJobs(string language) => await GetDataBasicCode(BasicCodeTypeConstant.WorkType, language);

        #endregion

        public async Task<OperationResult> GetTotalRecords(Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Param param)
        {
            var result = await GetData(param);
            return new OperationResult(true) { Data = result.Data.Count };
        }

        public async Task<OperationResult> ExportExcel(Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Param param)
        {
            // 1. Lấy dữ liệu chính
            var result = await GetData(param);

            if (!result.Data.Any()) return new OperationResult(false, "System.Message.NoData");

            var header = new Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Excel_Header
            {
                Factory = param.Factory,
                PrintBy = param.PrintBy,
                YearMonth = param.YearMonth,
                PrintDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                Work_Types_EN = await GetListCodeName(param.Work_Type, "EN", BasicCodeTypeConstant.WorkType),
                Work_Types_TW = await GetListCodeName(param.Work_Type, "TW", BasicCodeTypeConstant.WorkType),
                PermisionGroups = await GetListCodeName(param.PermisionGroup, param.Language, BasicCodeTypeConstant.PermissionGroup),
                Level = await GetListCodeName(param.Level, param.Language, BasicCodeTypeConstant.Level)
            };

            var exports = new List<Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Excel>();

            // Danh sách phòng ban 
            var org_Departments = await _repositoryAccessor.HRMS_Org_Department.FindAll(x => x.Factory == param.Factory, true).ToListAsync();
            var org_Departments_lang = await _repositoryAccessor.HRMS_Org_Department_Language.FindAll(x => x.Factory == param.Factory, true).ToListAsync();

            // 2 Lấy dữ liệu export 
            foreach (var item in result.Data)
            {
                // var parentDepartment = await Query_Department_Report(param.Factory, item.Parent_Department, param.Language);
                var exportData = await QueryDataReport(
                    new Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Excel(
                        item.Parent_Department,
                        item.Parent_Department_Name,
                        item.Parent_Department_Level
                    )
                    {
                        Department = item.Department,
                        Department_Name = item.Department_Name,
                    }
                    , item, param, org_Departments, org_Departments_lang, result.Local_Permission_list);
                exports.Add(exportData);
            }

            exports = exports.OrderBy(x => x.Department).ToList();

            // 3. Chuyển đổi dữ liệu thành dữ liệu In (CELL)
            var excelTable = ConvertToExcelTable(header, exports);
            // 4. in Dữ liệu
            ConfigDownload config = new() { IsAutoFitColumn = true };
            ExcelResult excelResult = ExcelUtility.DownloadExcel(
                excelTable, 
                "Resources\\Template\\AttendanceMaintenance\\5_2_13_MonthlyEmployeeStatusChangesSheetByWorkTypeJob\\Download.xlsx", 
                config
            );
            // 5. Trả data in
            var dataResult = new
            {
                excelResult.Result,
                result.Data.Count
            };

            return new OperationResult(excelResult.IsSuccess, excelResult.Error, dataResult);
        }

        #region Methods

        /// <summary>
        /// Lấy ngày bắt đầu hoặc ngày kết thúc của tháng trong năm
        /// </summary>
        /// <param name="yearMonth">Thời gian năm và tháng</param>
        /// <param name="isEndDateOfMonth"> Mặc định lấy ngày đầu tháng, True: Ngày cuối tháng </param>
        /// <returns>DateTime ngày đầu tháng hoặc cuối tháng </returns>
        private static DateTime? GetDateTimeOfMonth(string yearMonth, bool isEndDateOfMonth = false)
        {
            if(yearMonth == "NAN/NAN") return null;
            var time = yearMonth.Split("/");
            int year = int.Parse(time[0]);
            int month = int.Parse(time[1]);
            return new DateTime(year, month, !isEndDateOfMonth ? 1 : DateTime.DaysInMonth(year, month));
        }
        private static int ToInt(string value) => Convert.ToInt16(value);

        /// <summary>
        /// Lấy danh sách thống kê số lượng nhân viên theo phòng ban
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private async Task<Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Data_Result> GetData(Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Param param)
        {
            var predicate = PredicateBuilder.New<HRMS_Emp_Personal>(true);
            var predicateDepartment = PredicateBuilder.New<HRMS_Org_Department>(x => x.IsActive);
            var predicatePermission = PredicateBuilder.New<HRMS_Emp_Permission_Group>(x => x.Foreign_Flag == "N" && param.PermisionGroup.Any(z => z == x.Permission_Group));

            if (!string.IsNullOrWhiteSpace(param.Factory))
            {
                predicate.And(x => x.Factory == param.Factory);
                predicateDepartment.And(x => x.Factory == param.Factory);
                predicatePermission.And(x => x.Factory == param.Factory);
            }

            var local_Permission_list = await _repositoryAccessor.HRMS_Emp_Permission_Group.FindAll(predicatePermission, true).Select(x => x.Permission_Group).ToListAsync();
            predicate.And(x => local_Permission_list.Any(z => z == x.Permission_Group));
            var departmentQueryable = _repositoryAccessor.HRMS_Org_Department.FindAll(predicateDepartment, true);

            // Danh sách Deparments
            var allDepartment = await departmentQueryable.ToListAsync();

            var employee_Depts = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(predicate, true)
                        .Join(departmentQueryable,
                            x => new { x.Division, x.Factory, Department_Code = x.Department },
                            y => new { y.Division, y.Factory, y.Department_Code },
                            (personal, department) => new Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Data(
                                personal.Department, department.Org_Level
                            )
                            {
                                Department = personal.Department,
                                Department_Name = department.Department_Name,
                                Org_Level = department.Org_Level,
                            })
                        .Distinct()
                        .ToListAsync();

            // Loại bỏ danh sách có cấp bậc cao hơn [Cấp bậc cao nhất được chọn]
            employee_Depts = employee_Depts.Where(x => ToInt(x.Org_Level) >= ToInt(param.Level)).ToList();

            var new_employee_Depts = new List<Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Data>();
            // Xử lý theo [cấp bậc cao nhất được chọn]
            foreach (var employee_Dept in employee_Depts)
            {
                // Cùng cấp bậc
                if (!string.IsNullOrWhiteSpace(employee_Dept.Org_Level) && ToInt(employee_Dept.Org_Level) == ToInt(param.Level))
                    new_employee_Depts.Add(employee_Dept);

                // Kiểm tra Level hiện tại của item với [Level cao nhất được chọn]
                // Nếu cấp bậc hiện tại > cấp bậc được chọn (Cấp bậc hiện tại thấp hơn cấp đang được chọn)
                if (!string.IsNullOrWhiteSpace(employee_Dept.Org_Level) && ToInt(employee_Dept.Org_Level) > ToInt(param.Level))
                    GetMaxLevelOfEmployee_Dept(employee_Dept, null, ToInt(param.Level), allDepartment, new_employee_Depts);
            }


            return new Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Data_Result()
            {
                Data = new_employee_Depts,
                Local_Permission_list = local_Permission_list
            };
        }


        /// <summary>
        /// Lấy cấp bậc cao hơn nhưng không lớn hơn [Cấp bậc được chọn] từ Param
        /// </summary>
        /// <param name="model"> Model Cấp Bậc hiện tại </param>
        /// <param name="upper_Department"> Mã Phòng Ban Cấp Cao hơn</param>
        /// <param name="paramMaxLevel"> [Cấp bậc] được chọn </param>
        /// <param name="allDepartments"> Danh sách phòng ban theo [Factory] </param>
        /// <param name="result"> Trả về nhóm danh sách cấp bậc gần nhất với [Param.Level] được chọn </param>
        private void GetMaxLevelOfEmployee_Dept(Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Data model,
                                                string upper_Department,
                                                int paramMaxLevel,
                                                List<HRMS_Org_Department> allDepartments,
                                                List<Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Data> result)
        {
            if (ToInt(model.Parent_Department_Level) == paramMaxLevel)
                result.Add(model);

            // Nếu Level hiện tại <= Max level được Chọn
            if (ToInt(model.Parent_Department_Level) > paramMaxLevel)
            {
                // Lấy thông tin phòng ban - cấp bậc hiện tại 
                var currentDept = allDepartments.FirstOrDefault(x => x.Department_Code == (!string.IsNullOrWhiteSpace(upper_Department) ? upper_Department : model.Parent_Department));

                if (currentDept != null)
                {
                    var currentUpperDepartment = currentDept.Attribute == "Non-Directly" ? currentDept.Virtual_Department : currentDept.Upper_Department;
                    // 1. Nếu không có thuộc phòng ban nào và level hiện tại >= max level
                    if (string.IsNullOrWhiteSpace(currentDept.Upper_Department))
                    {
                        if (ToInt(currentDept.Org_Level) >= paramMaxLevel)
                        {
                            result.Add(new(currentDept.Department_Code, currentDept.Department_Name, currentDept.Org_Level)
                            {
                                Department = model.Department,
                                Department_Name = model.Department_Name,
                                Org_Level = model.Org_Level
                            });
                        }
                        return;
                    }
                    else
                    {
                        // 1. Nếu UpperDepartment == DepartmentCode
                        if (currentUpperDepartment == currentDept.Department_Code)
                            return;
                        // 2. Nếu lv hiện tại > Max level
                        else
                        {
                            if (ToInt(currentDept.Org_Level) >= paramMaxLevel)
                            {
                                model.Parent_Department = currentDept.Department_Code;
                                model.Parent_Department_Name = currentDept.Department_Name;
                                model.Parent_Department_Level = currentDept.Org_Level;

                                GetMaxLevelOfEmployee_Dept(
                                    model,
                                    currentUpperDepartment,
                                    paramMaxLevel,
                                    allDepartments,
                                    result
                                );
                            }
                            else result.Add(model);
                        }
                    }
                }
                else result.Add(model);
            }
        }



        /// <summary>
        /// Lấy dữ liệu Export : Department Code - Deparmtment Name
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="department"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        private static ParentDepartmentModel Query_Department_Report(List<HRMS_Org_Department> org_Departments, List<HRMS_Org_Department_Language> org_Departments_lang, string parent_Department, string language)
        {
            org_Departments = org_Departments.Where(x => x.Department_Code == parent_Department).ToList();
            org_Departments_lang = org_Departments_lang.Where(x => x.Department_Code == parent_Department && x.Language_Code.ToLower() == language.ToLower()).ToList();

            return org_Departments.GroupJoin(org_Departments_lang,
                        x => new { x.Division, x.Factory, x.Department_Code },
                        y => new { y.Division, y.Factory, y.Department_Code },
                    (x, y) => new { x, y })
                    .SelectMany(x => x.y.DefaultIfEmpty(),
                    (x, y) => new ParentDepartmentModel(
                        x.x.Department_Code,
                        y != null && !string.IsNullOrWhiteSpace(y.Name) ? y.Name : x.x.Department_Name,
                        x.x.Org_Level
                    )).FirstOrDefault();
        }

        /// <summary>
        /// Đếm số lượng nhân viên hoạt động trực tiếp
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="employee_Department"></param>
        /// <param name="yearMonth"></param>
        /// <returns></returns>
        private async Task<Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Excel> QueryDataReport(
                            Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Excel model,
                            Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Data item,
                            Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Param param,
                            List<HRMS_Org_Department> org_Departments,
                            List<HRMS_Org_Department_Language> org_Departments_lang,
                            List<string> localPermissionGroup)
        {
            var endDateOfMonth = GetDateTimeOfMonth(param.YearMonth, true);

            // Danh sách phòng ban có cấp bậc ngang hàng || thấp hơn
            var departmentHierarchies = GetDepartmentHierarchy(org_Departments, item.Department);

            // Tất cả nhân viên
            var allEmployee = await _repositoryAccessor.HRMS_Emp_Personal.FindAll(e => e.Factory == param.Factory).ToListAsync();
            // Số lượng nhân viên theo phòng ban cũ
            var employees = allEmployee.Where(e => e.Department == item.Department && localPermissionGroup.Any(permission => permission == e.Permission_Group)).ToList();        
        
            // Tìm vị trí của WorkType  trong danh sách [Departments - phòng ban]

            var foreign_Permission_list = await Query_Permission_Group_List(param.Factory, "N");
            
            var employeesOfDepartments = allEmployee.Where(e => departmentHierarchies.Any(d => d.Department_Code == e.Department)
                                                        && e.Onboard_Date.Date <= endDateOfMonth.Value.Date
                                                        && (e.Resign_Date.HasValue && e.Resign_Date.Value.Date > endDateOfMonth.Value.Date || e.Resign_Date == null)
                                                        && localPermissionGroup.Any(permission => permission == e.Permission_Group)
                                                        && foreign_Permission_list.Any(p => p == e.Permission_Group))
                                                    .ToList();



            // Lấy tên phòng ban
            var parentDepartment = Query_Department_Report(org_Departments, org_Departments_lang, model.Department, param.Language);
            if (parentDepartment != null)
            {
                model.Department = $"{model.Parent_Department_Level} {parentDepartment.Parent_Department}";
                model.Department_Name = parentDepartment.Parent_Department_Name;
            }
            else
            {
                model.Department = $"{model.Parent_Department_Level} {model.Parent_Department}";
                model.Department_Name = model.Parent_Department_Name;
            }

            model.Direct_Employees = GetDirectEmployees(employeesOfDepartments, param.Factory, param.YearMonth);
            model.Indirect_Employees = GetInDirectEmployees(employeesOfDepartments, param.Factory, param.YearMonth);

            var employeesOfDepartmentsGroup = employeesOfDepartments.GroupBy(g => g.Work_Type)
                                                        .Select(x => new CodeNameWorkTypePosition(x.Key, x.Count())).ToList();

            model.CodeNameList = GetCodeNames(employeesOfDepartmentsGroup, param.Work_Type);
            return model;
        }

        // lấy thông tin nhân viên chuyển trực tiếp
        private int GetDirectEmployees(List<HRMS_Emp_Personal> employeesOnboardDates, string factory, string yearMonth)
        {
            employeesOnboardDates = employeesOnboardDates.Where(a => GetLineCodes(a.Division, a.Factory).Any(z => z == a.Department)).ToList();
            var divions = employeesOnboardDates.Select(x => x.Division).Distinct().ToList();
            var workTypes = GetWorkTypeCodes(divions, factory, yearMonth);
            var result = employeesOnboardDates.Join(workTypes, x => x.Work_Type, b => b.Work_Type_Code, (a, b) => new { a, b }).ToList();
            return result.Count;
        }

        private int GetInDirectEmployees(List<HRMS_Emp_Personal> employeesOnboardDates, string factory, string yearMonth)
        {
            var employeesInDepartment = employeesOnboardDates.Where(a => GetLineCodes(a.Division, a.Factory).Any(z => z == a.Department)).ToList();

            var divions = employeesInDepartment.Select(x => x.Division).Distinct().ToList();
            var workTypes = GetWorkTypeCodes(divions, factory, yearMonth, false);

            var userIds = employeesInDepartment.Join(workTypes, x => x.Work_Type, b => b.Work_Type_Code, (a, b) => new { a, b }).Select(x => x.a.USER_GUID).Distinct().ToList();
            return employeesOnboardDates.Where(x => !userIds.Any(id => id == x.USER_GUID)).Count();
        }


        /// <summary>
        /// Lấy dữ liệu theo WorkType được chọn theo giao diện
        /// </summary>
        /// <param name="employeesOnboardDates"></param>
        /// <param name="workTypes">Danh sách Work Type được chọn </param>
        /// <returns></returns>
        private static List<int> GetCodeNames(List<CodeNameWorkTypePosition> employeesOfDepartments, List<string> workTypes)
        {

            var result = new List<int>();
            if (!workTypes.Any()) return result;

            // Lấy Count item Theo Work Type
            foreach (var workType in workTypes)
            {
                var workTypeCount = employeesOfDepartments.FirstOrDefault(a => a.Work_Type_Code == workType);
                result.Add(workTypeCount?.GroupCount ?? 0);
            }
            return result;
        }

        private List<string> GetLineCodes(string division, string factory)
        {
            return _repositoryAccessor.HRMS_Org_Direct_Department.FindAll(x => x.Division == division && x.Factory == factory).Select(x => x.Line_Code).ToList();
        }
        private List<MaxEffective_Date> GetWorkTypeCodes(List<string> division, string factory, string yearMonth, bool isIndirect = true)
        {
            var yearMonthDate = GetDateTimeOfMonth(yearMonth);
            var division_List = _repositoryAccessor.HRMS_Basic_Factory_Comparison
                                                        .FindAll(x =>  x.Factory == factory 
                                                                    && x.Kind == "1")
                                                        .Select(x => x.Division)
                                                        .Distinct()
                                                        .ToList();

            var direct_Sections = _repositoryAccessor.HRMS_Org_Direct_Section.FindAll(x =>
                                                            x.Factory == factory
                                                            && x.Direct_Section == "Y"
                                                            && division_List.Contains(x.Division)
                                                            , true)
                                                        .ToList();
            // Lấy theo yearMonthDate
            direct_Sections = direct_Sections.Where(ef => GetDateTimeOfMonth(ef.Effective_Date) <= yearMonthDate).ToList();
            
            var effective_DateMax = GetMaxEffective_Date(direct_Sections);
            return effective_DateMax;
        }

        private async Task<List<string>> GetListCodeName(List<string> listCode, string language, string basicCodeType)
        {
            if (!listCode.Any()) return listCode;

            var result = new List<string>();
            var data = await GetDataBasicCode(basicCodeType, language);
            foreach (var code in listCode)
            {
                var item = data.FirstOrDefault(x => x.Key == code);
                result.Add(!string.IsNullOrWhiteSpace(item.Key) ? item.Value : code);
            }
            return result;
        }

        private async Task<string> GetListCodeName(string code, string language, string basicCodeType)
        {
            if (string.IsNullOrWhiteSpace(code)) return code;
            var data = await GetDataBasicCode(basicCodeType, language);
            var item = data.FirstOrDefault(x => x.Key == code);
            return !string.IsNullOrWhiteSpace(item.Key) ? item.Value : code;
        }


        /// <summary>
        /// Lấy Max Effective_Date
        /// </summary>
        /// <param name="direct_Sections"></param>
        /// <param name="yearMonth"></param>
        /// <returns></returns>
        private static List<MaxEffective_Date> GetMaxEffective_Date(List<HRMS_Org_Direct_Section> direct_Sections)
        {
            var list = direct_Sections
                        .GroupBy(x => x.Work_Type_Code)
                        .Select(g => new MaxEffective_Date(g.Key, GetDateTimeOfMonth(g.Max(x => x.Effective_Date)).Value))
                        .ToList();
            return list;
        }


        #region DepartmentHierarchy
        private static List<DepartmentHierarchy> GetDepartmentHierarchy(List<HRMS_Org_Department> org_Departments, string departmentCode)
        {
            var departmentHierarchy = new List<DepartmentHierarchy>();

            var initialDepartment = org_Departments
                .FindAll(d => d.IsActive && d.Department_Code == departmentCode)
                .Select(d => new DepartmentHierarchy
                {
                    Department_Code = d.Department_Code,
                    Department_Name = d.Department_Name,
                    Upper_Department = d.Upper_Department
                })
                .FirstOrDefault();

            if (initialDepartment != null)
            {
                departmentHierarchy.Add(initialDepartment);
                AddSubDepartments(initialDepartment, org_Departments, departmentHierarchy);
            }

            return departmentHierarchy;
        }

        private static void AddSubDepartments(DepartmentHierarchy department, List<HRMS_Org_Department> HOD, List<DepartmentHierarchy> departmentHierarchy)
        {
            var subDepartments = HOD
                .Where(x => x.IsActive && x.Upper_Department == department.Department_Code)
                .Select(d => new DepartmentHierarchy
                {
                    Department_Code = d.Department_Code,
                    Department_Name = d.Department_Name,
                    Upper_Department = d.Upper_Department
                })
                .ToList();

            foreach (var subDept in subDepartments)
            {
                if (subDept.Department_Code != subDept.Upper_Department)
                {
                    departmentHierarchy.Add(subDept);
                    AddSubDepartments(subDept, HOD, departmentHierarchy);
                }
            }
        }

        #endregion

        /// <summary>
        /// Chuyển đổi dữ liệu thành dữ liệu In
        /// </summary>
        /// <param name="data">Dữ liệu cần In</param>
        /// <param name="workTypes"> Danh sách Loại công việc được chọn </param>
        /// <returns></returns>
        private static List<Cell> ConvertToExcelTable(Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Excel_Header header, List<Monthly_Employee_Status_Changes_Sheet_By_WorkType_Job_Excel> body)
        {
            Aspose.Cells.Style h2Style = new Aspose.Cells.CellsFactory().CreateStyle();
            h2Style.IsTextWrapped = true;
            h2Style.VerticalAlignment = Aspose.Cells.TextAlignmentType.Center;
            h2Style.HorizontalAlignment = Aspose.Cells.TextAlignmentType.Left;

            var result = new List<Cell>()
            {
                new("B2", header.Factory),
                new("B3", header.PrintBy),
                new("D2", header.YearMonth),
                new("D3", header.PrintDate),
                new("F2", header.Level),
                new("H2", string.Join(" / ",header.PermisionGroups), h2Style),
            };


            int headerTwRow = 4; // Dòng dữ liệu mặc định theo TW
            int headerVNRow = 5; // Dòng dữ liệu mặc định theo EN
            int bodyRow = 6; // Số dòng dữ liệu bắt đầu

            int startWorkTypeColumn = 4;
            // Gen Data WorkTypes
            // Cột bắt đầu từ cột thứ 5 -> (5 + số lượng workTypes)

            // Số dòng dữ liệu theo ngôn ngữ
            Aspose.Cells.Style commonStyle = new Aspose.Cells.CellsFactory().CreateStyle();
            commonStyle.Pattern = Aspose.Cells.BackgroundType.Solid;
            commonStyle.ForegroundColor = Color.FromArgb(255, 242, 204);
            commonStyle.VerticalAlignment = Aspose.Cells.TextAlignmentType.Center;
            commonStyle.HorizontalAlignment = Aspose.Cells.TextAlignmentType.Center;
            AsposeUtility.SetAllBorders(commonStyle);

            for (int column = startWorkTypeColumn; column < (startWorkTypeColumn + header.Work_Types_EN.Count); column++)
            {
                // Vị trí Header bắt đầu
                int headerStartIndex = Math.Abs(startWorkTypeColumn + header.Work_Types_EN.Count - column - header.Work_Types_EN.Count);
                for (int row = headerTwRow; row <= headerVNRow; row++)
                {
                    var cell = new Cell(row, column, row == headerTwRow ? header.Work_Types_TW[headerStartIndex] : header.Work_Types_EN[headerStartIndex], commonStyle);
                    result.Add(cell);
                }
            }

            int rowDataIndex = 0; // Số dòng dữ liệu 
            int rowMaxDefault = 3; // Số dòng dữ liệu mặc định
            for (int row = bodyRow; row < bodyRow + body.Count; row++)
            {
                // Dữ liệu Mặc định
                result.Add(new Cell(row, rowMaxDefault - 3, body[rowDataIndex].Department));
                result.Add(new Cell(row, rowMaxDefault - 2, body[rowDataIndex].Department_Name));
                result.Add(new Cell(row, rowMaxDefault - 1, body[rowDataIndex].Direct_Employees));
                result.Add(new Cell(row, rowMaxDefault, body[rowDataIndex].Indirect_Employees));

                // Số dòng dữ liệu theo WorkTypes
                int rowValue = 0;

                for (int column = 1; column <= body[rowDataIndex].CodeNameList.Count; column++)
                {
                    var cell = new Cell(row, rowMaxDefault + column, body[rowDataIndex].CodeNameList[rowValue]);
                    result.Add(cell);
                    rowValue++;
                }

                // Kết thúc 1 dòng dữ liệu
                rowDataIndex++;
            }
            return result;
        }
        #endregion
    }
}