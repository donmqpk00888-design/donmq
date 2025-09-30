import { Pagination } from "@utilities/pagination-utility";

export interface D_7_25_MonthlySalaryMaintenanceExitedEmployeesSearchParam {
  factory: string;
  department: string;
  permission_Group: string[];
  year_Month: string;
  employee_ID: string;
  lang: string;
  userName: string;
}
export interface D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain {
  probation: string;
  salary_Lock: string;
  year_Month: string;
  useR_GUID: string;
  division: string;
  factory: string;
  department: string;
  department_Name: string;
  employee_ID: string;
  local_Full_Name: string;
  permission_Group: string;
  salary_Type: string;
  fiN_Pass_Status: string;
  transfer: string;
  tax: number;
  currency: string;
  // Monthly attendance data can be expanded/collapsed
  monthly_Attendance: D_7_25_Query_Att_MonthlyResult;
  update_By: string;
  update_Time: string;
  isDelete: boolean;
}
export interface D_7_25_Query_Att_MonthlyResult {
  paid_Salary_Days: number;
  actual_Work_Days: number;
  new_Hired_Resigned: string;
  delay_Early: number | null;
  no_Swip_Card: number | null;
  day_Shift_Meal_Times: number | null;
  overtime_Meal_Times: number | null; // Food_Expenses
  night_Shift_Allowance_Times: number | null; // Night_Eat_Times
  night_Shift_Meal_Times: number | null;
}

export interface D_7_25_AttUseMonthlyLeave {
  factory: string;
  leaveType: string;
  effectiveMonth: string;
  seq: number;
  code: string;
  // Các thuộc tính khác
}
export interface D_7_25_AttMonthlyDetailValues {
  leave_Code: string;
  leave_Code_Name: string;
  days: number;
}
export interface D_7_25_Query_Att_Monthly_DetailResult {
  table_Left_Leave: D_7_25_AttMonthlyDetailValues[];
  table_Right_Allowance: D_7_25_AttMonthlyDetailValues[];
}
//#endregion
//#region 3.40.Query_Sal_Monthly_Detail
export interface D_7_25_Sal_Monthly_Detail_Temp {
  useR_GUID: string;
  division: string;
  factory: string;
  att_Month: Date | string;
  employee_ID: string;
  leave_Type: string;
  leave_Code: string;
  days: number;
  update_By: string;
  update_Time: Date | string;
}

export interface D_7_25_Salary_Item_Sal_Monthly_Detail_Values {
  item: string;
  item_Name: string;
  amount: number;
  type_Seq: string;
  addDed_Type: string;
}

export interface D_7_25_MonthlySalaryMaintenance_Update {
  param: D_7_25_GetMonthlyAttendanceDataDetailUpdateParam;
  table_details: D_7_25_Query_Sal_Monthly_Detail_Result;
}

export interface D_7_25_GetMonthlyAttendanceDataDetailUpdateParam {
  tax: number;
  useR_GUID: string;
  factory: string;
  division: string;
  sal_Month: string;
  employee_ID: string;
  type_Seq: string;
  addDed_Type: string;
  item: string;
  update_By: string;
}

export interface D_7_25_Query_Sal_Monthly_Detail_Result {
  salary_Item_Table1: D_7_25_Salary_Item_Sal_Monthly_Detail_Values[];
  salary_Item_Table2: D_7_25_Salary_Item_Sal_Monthly_Detail_Values[];
  salary_Item_Table3: D_7_25_Salary_Item_Sal_Monthly_Detail_Values[];
  salary_Item_Table4: D_7_25_Salary_Item_Sal_Monthly_Detail_Values[];
  salary_Item_Table5: D_7_25_Salary_Item_Sal_Monthly_Detail_Values[];
  total_Item_Table1: number;
  total_Item_Table2: number;
  total_Item_Table3: number;
  total_Item_Table4: number;
  total_Item_Table5: number;
  totalAmountReceived: number;
}
export interface D_7_25_Query_Sal_Monthly_Detail_Result_Source {
  monthly_Attendance_Data: D_7_25_Query_Att_Monthly_DetailResult;
  monthly_Salary_Detail: D_7_25_Query_Sal_Monthly_Detail_Result;
}

export interface D_7_25_GetMonthlyAttendanceDataDetailParam {
  probation: string;
  tax: number;
  language: string;
  factory: string;
  year_Month: string; // Sal_Month
  employee_ID: string;
  permission_Group: string;
  salary_Type: string;
}
//#endregion

export interface MonthlySalaryMaintenanceExitedEmployeesSource {
  paramSearch: D_7_25_MonthlySalaryMaintenanceExitedEmployeesSearchParam;
  pagination: Pagination;
  dataItem: D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain;
  dataMain: D_7_25_MonthlySalaryMaintenanceExitedEmployeesMain[];
}
