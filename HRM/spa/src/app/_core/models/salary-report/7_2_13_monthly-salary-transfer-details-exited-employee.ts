export interface MonthlySalaryTransferDetailsExitedEmployeeParam {
  factory: string;
  year_Month: string;
  permission_Group: string[];
  department: string;
  start_Date: string;
  end_Date: string;
  userName: string;
  language: string;
}

export interface MonthlySalaryTransferDetailsExitedEmployeeSource {
  param: MonthlySalaryTransferDetailsExitedEmployeeParam;
  totalRows: number;
  year_Month: Date;
  start_Date: Date;
  end_Date: Date;
}
