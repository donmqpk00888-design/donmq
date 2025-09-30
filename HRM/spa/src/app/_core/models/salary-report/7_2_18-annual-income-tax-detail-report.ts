export interface AnnualIncomeTaxDetailReportParam {
  factory: string;
  year_Month_Start: string;
  year_Month_End: string;
  employee_ID: string;
  permission_Group: string[];
  department: string;
  userName: string;
  language: string;
}

export interface AnnualIncomeTaxDetailReportSource {
    param : AnnualIncomeTaxDetailReportParam;
    totalRows: number;
}
