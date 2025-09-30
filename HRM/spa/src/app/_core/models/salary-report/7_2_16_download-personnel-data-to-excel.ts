export interface DownloadPersonnelDataToExcel_Param
{
    factory: string,
    startDate: string,
    endDate: string,
    permissionGroup: string[],
    employeeKind: string,
    reportKind: string,
    yearMonth: string,
    userName: string;
    language: string;
}
export interface DownloadPersonnelDataToExcel_Source 
{
    param: DownloadPersonnelDataToExcel_Param,
    totalRows: number
}