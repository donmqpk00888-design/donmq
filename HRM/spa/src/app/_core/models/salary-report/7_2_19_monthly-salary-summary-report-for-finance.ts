export interface MonthlySalarySummaryReportForFinance_Param {
  factory: string;
  year_Month_Start: string;
  year_Month_End: string;
  kind: string;
  department: string;
  employee_ID: string;
  language: string
}

export interface MonthlySalarySummaryReportForFinance_Dto {
  permission: string;
  resign_Date?: Date | null;
  department: string;
  position_Title: string;
  factory: string;
  sal_Month: Date;
  employee_ID: string;
  bankTransfer: string;
  tax: number;
  out_day: string;
}

export interface MonthlySalarySummaryReportForFinance_Data {
  permission: string;
  out_day: string;
  typeCode: string;
  additional_Deduction: number;
  deduction_Items: number;
  salary: number;
  ovtPay: number;
  foodpay: number;
  other_Del: number;
  other_Add: number;
  add_Total: number;
  loan: number;
  scfee: number;
  mdfee: number;
  seat: number;
  tax: number;
  wkmy: number;
  redtotal: number;
  total: number;
  actotal: number;
  act_flag: string;
  atm: number;
  natm: number;
  wk_natm: number;
  bankNo: string;
  delAmt: number;
  addAmt: number;
  reserved: number;
  bankTransfer: string;
  year_End_Bonus: number;
}

export interface MonthlySalarySummaryReportForFinance_Total {
  tT_Salary: number;
  tT_Foodpay: number;
  tT_Other_Del: number;
  tT_Other_Add: number;
  tT_Scfee: number;
  tT_Add_Total: number;
  tT_Mdfee: number;
  tT_Seat: number;
  tT_Tax: number;
  tT_Wkmy: number;
  tT_Redtotal: number;
  tT_Actotal: number;
  tT_Atm: number;
  tT_Natm: number;
  tT_Reserved: number;
}
export interface MonthlySalarySummaryReportForFinance_Source {
  param: MonthlySalarySummaryReportForFinance_Param;
  totalRows: number;
  year_Month_Start: Date;
  year_Month_End: Date;
}
