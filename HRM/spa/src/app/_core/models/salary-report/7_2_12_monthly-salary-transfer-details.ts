export interface MonthlySalaryTransferDetailsParam {
    factory: string;
    year_Month: string;
    permission_Group: string[];
    department: string;
    userName: string;
    language: string;
}

export interface MonthlySalaryTransferDetailsSource {
  param: MonthlySalaryTransferDetailsParam;
  totalRows: number;
  year_Month: Date;
}
