import { Pagination } from "@utilities/pagination-utility";

export interface MonthlySalaryMaintenanceDto {
  probation: string;
  useR_GUID: string;
  departmentName: string;
  factory: string;
  sal_Month: string;
  local_Full_Name: string;
  employee_ID: string;
  department: string;
  permission_Group: string;
  salary_Type: string;
  lock: string;
  fiN_Pass_Status: string;
  update_By: string;
  update_Time: string;
  bankTransfer: string;
  currency: string;
  language: string;
  tax: number;
  isDelete: boolean;
}

export interface MonthlySalaryMaintenance_Basic {
  param: MonthlySalaryMaintenanceParam
  pagination: Pagination
  data: MonthlySalaryMaintenanceDto[]
  selectedData: MonthlySalaryMaintenanceDto,
}

export interface MonthlySalaryMaintenanceDetail extends MonthlyAttendanceData {
  paidSalaryDays: number;
  actualWorkDays: number;
  newHiredResigned: string;
  attendanceData: MonthlyAttendanceData;
  salaryDetail: MonthlySallaryDetail;
}

export interface MonthlyAttendanceData {
  delayEarly: number;
  noSwipCard: number;
  dayShiftMealTimes: number;
  overtimeMealTimes: number;
  nightShiftAllowanceTimes: number;
  nightShiftMealTimes: number;
  leave: MonthlyAttendanceData_Leave[];
  allowance: MonthlyAttendanceData_Allowance[];
}

export interface MonthlyAttendanceData_Leave {
  leave: string;
  leave_Name: string;
  monthlyDays: number;
}

export interface Query_Att_Monthly_Detail {
  leave_Code: string;
  days: number;
  seq: number;
}

export interface Query_Sal_Monthly_Detail {
  item: string;
  amount: number;
  seq: number | null;
  sumAmount: number;
}

export interface MonthlySallaryDetail {
  table_1: MonthlySallaryDetail_Table[];
  table_2: MonthlySallaryDetail_Table[];
  table_3: MonthlySallaryDetail_Table[];
  table_4: MonthlySallaryDetail_Table[];
  table_5: MonthlySallaryDetail_Table[];
  totalAmountReceived: number;
  tax: number;
}

export interface MonthlySallaryDetail_Table {
  listItem: MonthlySallaryDetail_Item[];
  sumAmount: number;
}

export interface MonthlySallaryDetail_Item {
  item: string;
  item_Name: string;
  amount: number;
}

export interface MonthlyAttendanceData_Allowance {
  allowance: string;
  allowance_Name: string;
  monthlyDays: number;
}

export interface MonthlySalaryMaintenanceParam {
  factory: string;
  employee_ID: string;
  salMonth: string;
  department: string;
  language: string;
  permission_Group: string[];
}

export interface SettingTemp {
  code: string;
  seq: number;
}

export interface SalSettingTemp {
  salaryItem: string;
  seq: number;
}

export interface MonthlySalaryMaintenance_Personal {
  useR_GUID: string;
  local_Full_Name: string;
}
export interface MonthlySalaryMaintenance_Update {
  factory: string;
  sal_Month: string;
  employee_ID: string;
  update_By: string;
  update_Time: string;
  tax: number;
  salaryDetail: MonthlySallaryDetail;
}

export interface MonthlySalaryMaintenance_Delete {
  employee_ID: string;
  sal_Month: string;
  factory: string;
}
