export interface SystemInfo {
  directories: DirectoryInfomation[];
  programs: ProgramInfomation[];
  functions: FunctionInfomation[];
  code_Information: CodeInformation[];
}
export interface DirectoryInfomation {
  seq: string;
  directory_Name: string;
  directory_Code: string;
}
export interface ProgramInfomation {
  program_Name: string;
  program_Code: string;
  parent_Directory_Code: string;
}
export interface FunctionInfomation {
  program_Code: string;
  function_Code: string;
}
export interface CodeInformation {
  code: string;
  name: string;
  kind: string;
  translations: CodeLang[];
}
export interface CodeLang {
  lang: string;
  name: string;
}
export interface HRMS_Emp_Personal {
  useR_GUID: string;
  nationality: string;
  identification_Number: string;
  issued_Date: Date;
  company: string;
  deletion_Code: string;
  division: string;
  factory: string;
  employee_ID: string;
  department: string;
  assigned_Division: string;
  assigned_Factory: string;
  assigned_Employee_ID: string;
  assigned_Department: string;
  permission_Group: string;
  employment_Status: string;
  performance_Division: string;
  identity_Type: string;
  local_Full_Name: string;
  preferred_English_Full_Name: string;
  chinese_Name: string;
  gender: string;
  blood_Type: string;
  marital_Status: string;
  birthday: Date;
  phone_Number: string;
  mobile_Phone_Number: string;
  education: string;
  religion: string;
  transportation_Method: string;
  vehicle_Type: string;
  license_Plate_Number: string;
  registered_Province_Directly: string;
  registered_City: string;
  registered_Address: string;
  mailing_Province_Directly: string;
  mailing_City: string;
  mailing_Address: string;
  work_Shift_Type: string;
  swipe_Card_Option: boolean;
  swipe_Card_Number: string;
  position_Grade: number;
  position_Title: string;
  work_Type: string;
  restaurant: string;
  work_Location: string;
  union_Membership: boolean | null;
  work8hours: boolean | null;
  onboard_Date: Date;
  group_Date: Date;
  seniority_Start_Date: Date;
  annual_Leave_Seniority_Start_Date: Date;
  resign_Date: Date | null;
  resign_Reason: string;
  blacklist: boolean | null;
  update_By: string;
  update_Time: Date;
}
export interface DepartmentInfo {
  division: string;
  factory: string;
  department_Code: string;
  department_Name: string;
  department_Code_Name: string;
}
export interface EmployeeCommonInfo extends HRMS_Emp_Personal {
  onboard_Date_Str: string;
  work8hours_Str: string;
  actual_Factory: string;
  work_Type_Name: string;
  actual_Division: string;
  actual_Employee_ID: string;
  actual_Department_Code: string;
  actual_Department_Name: string;
  actual_Department_Code_Name: string;
}
