export interface D_7_2_17_MonthlyPersonalIncomeTaxAmountReportParam {
    factory: string;
    year_Month: string;
    permission_Group: string[];
    department: string;
    employee_ID: string;
    userName: string;
    language: string;
}

export interface MonthlyPersonalIncomeTaxAmountReportSource {
    param : D_7_2_17_MonthlyPersonalIncomeTaxAmountReportParam;
    totalRows: number;
}