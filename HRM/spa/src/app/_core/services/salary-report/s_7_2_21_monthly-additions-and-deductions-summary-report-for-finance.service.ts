import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { environment } from '@env/environment';
import { MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param, MonthlyAdditionsAndDeductionsSummaryReportForFinance_Source } from '@models/salary-report/7_2_21_monthly-additions-and-deductions-summary-report-for-finance';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
@Injectable({
  providedIn: 'root'
})
export class S_7_2_21_MonthlyAdditionsAndDeductionsSummaryReportForFinanceService {
  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  apiUrl: string = environment.apiUrl + "C_7_2_21_MonthlyAdditionsAndDeductionsSummaryReportForFinance/"
  initData: MonthlyAdditionsAndDeductionsSummaryReportForFinance_Source = <MonthlyAdditionsAndDeductionsSummaryReportForFinance_Source>{
    param: <MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param>{
      kind: 'All',
    },
    totalRows: 0
  }

  programSource = signal<MonthlyAdditionsAndDeductionsSummaryReportForFinance_Source>(structuredClone(this.initData));
  programSource$ = toObservable(this.programSource);
  setSource = (source: MonthlyAdditionsAndDeductionsSummaryReportForFinance_Source) => this.programSource.set(source);
  clearParams = () => {
    this.programSource.set(structuredClone(this.initData))
  }
  constructor(private http: HttpClient) { }
getTotalRows(param: MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param) {
    param.language = this.language
    let params = new HttpParams().appendAll({ ...param });
    return this.http.get<OperationResult>(this.apiUrl + 'GetTotalRows', { params })
  }

  download(param: MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param) {
    param.language = this.language
    let params = new HttpParams().appendAll({ ...param });
    return this.http.get<OperationResult>(this.apiUrl + 'Download', { params });
  }

  getListFactory() {
    const language: string = localStorage.getItem(LocalStorageConstants.LANG)
    return this.http.get<KeyValuePair[]>(`${this.apiUrl}GetListFactory`, { params: { language } });
  }
  
  getListDepartment(factory: string) {
    return this.http.get<KeyValuePair[]>(this.apiUrl + "GetListDepartment", { params: { factory, language: this.language } });
  }
}
