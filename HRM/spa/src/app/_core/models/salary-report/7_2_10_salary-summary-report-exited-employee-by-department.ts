export interface SalarySummaryReportExitedEmployeeByDepartmentParam {
    factory: string;
    resignation_Start: string;
    resignation_End: string;
    permission_Group: string[];
    kind: string;
    department: string;
    employee_ID: string;
    language: string;
}

export interface SalarySummaryReportExitedEmployeeByDepartmentSource {
    param : SalarySummaryReportExitedEmployeeByDepartmentParam;
    totalRows: number;
}