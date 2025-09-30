export interface MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param {
    factory: string;
    yearMonth: string;
    kind: string;
    department: string;
    employeeID: string;
    language: string;
    userName: string;
}
export interface MonthlyAdditionsAndDeductionsSummaryReportForFinance_Source {
    param : MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param;
    totalRows: number;
}