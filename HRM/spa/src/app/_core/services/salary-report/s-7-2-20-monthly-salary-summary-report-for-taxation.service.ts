import {HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { environment } from '@env/environment';
import { MonthlySalarySummaryReportForTaxation_Data, MonthlySalarySummaryReportForTaxation_Param, MonthlySalarySummaryReportForTaxation_Source } from '@models/salary-report/7_2_20_monthly-salary-summary-report-for-taxation';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';

@Injectable({
  providedIn: 'root'
})
export class S_7_2_20_MonthlySalarySummaryReportForTaxation {

  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  apiUrl: string = environment.apiUrl + 'C_7_2_20_MonthlySalarySummaryReportForTaxation/';
  initData: MonthlySalarySummaryReportForTaxation_Source = <MonthlySalarySummaryReportForTaxation_Source>{
    param: <MonthlySalarySummaryReportForTaxation_Param>{
      permission_Group: [],
      kind: 'All',
    },
    year_Month_Start: null,
    year_Month_End: null,
    totalRows: 0,
  }
  programSource = signal<MonthlySalarySummaryReportForTaxation_Source>(structuredClone(this.initData));
  programSource$ = toObservable(this.programSource);
  setSource = (source: MonthlySalarySummaryReportForTaxation_Source) => this.programSource.set(source);
  clearParams = () => {
    this.programSource.set(structuredClone(this.initData))
  }
  constructor(private _http: HttpClient) { }

  getListFactory() {
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}GetListFactory`, { params: { language: this.language } });
  }

  getListPermissionGroup(factory: string) {
    let params = new HttpParams().appendAll({ factory, language: this.language })
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}GetListPermissionGroup`, { params });
  }

  getListDepartment(factory: string) {
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}GetListDepartment`, { params: { factory, language: this.language } });
  }

  search(param: MonthlySalarySummaryReportForTaxation_Param) {
    const params = new HttpParams().appendAll({ ...param });
    return this._http.get<number>(`${this.apiUrl}GetTotalRows`, { params });
  }

  downloadExcel(param: MonthlySalarySummaryReportForTaxation_Param) {
      param.language = this.language;
      const params = new HttpParams().appendAll({ ...param });
      return this._http.get<OperationResult>(`${this.apiUrl}DownloadFileExcel`, { params });
    }
}
