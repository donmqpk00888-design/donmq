import {HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { environment } from '@env/environment';
import { MonthlySalarySummaryReportForFinance_Param, MonthlySalarySummaryReportForFinance_Source } from '@models/salary-report/7_2_19_monthly-salary-summary-report-for-finance';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';


@Injectable({
  providedIn: 'root'
})
export class S_7_2_19_MonthlySalarySummaryReportForFinance {

  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  apiUrl: string = environment.apiUrl + 'C_7_2_19_MonthlySalarySummaryReportForFinance/';
  initData: MonthlySalarySummaryReportForFinance_Source = <MonthlySalarySummaryReportForFinance_Source>{
    param: <MonthlySalarySummaryReportForFinance_Param>{
      kind: 'All',
    },
    year_Month_Start: null,
    year_Month_End: null,
    totalRows: 0,
  }
  programSource = signal<MonthlySalarySummaryReportForFinance_Source>(structuredClone(this.initData));
  programSource$ = toObservable(this.programSource);
  setSource = (source: MonthlySalarySummaryReportForFinance_Source) => this.programSource.set(source);
  clearParams = () => {
    this.programSource.set(structuredClone(this.initData))
  }
  constructor(private _http: HttpClient) { }

  getListFactory() {
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}GetListFactory`, { params: { language: this.language } });
  }

  getListDepartment(factory: string) {
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}GetListDepartment`, { params: { factory, language: this.language } });
  }

  search(param: MonthlySalarySummaryReportForFinance_Param) {
    const params = new HttpParams().appendAll({ ...param });
    return this._http.get<number>(`${this.apiUrl}GetTotalRows`, { params });
  }

  downloadExcel(param: MonthlySalarySummaryReportForFinance_Param) {
      param.language = this.language;
      const params = new HttpParams().appendAll({ ...param });
      return this._http.get<OperationResult>(`${this.apiUrl}DownloadFileExcel`, { params });
    }
}
