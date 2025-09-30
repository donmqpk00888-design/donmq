export interface SalarySummaryReportExitedEmployeeParam {
    factory: string;
    resignation_Start: string;
    resignation_End: string;
    transfer: string;
    permission_Group: string[];
    department: string;
    employee_ID: string;
    language: string;
}

export interface SalarySummaryReportExitedEmployeeSource {
    param : SalarySummaryReportExitedEmployeeParam;
    totalRows: number;
}