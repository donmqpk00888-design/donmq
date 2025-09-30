import { LocalStorageConstants } from '@constants/local-storage.constants';
import { KeyValuePair } from '@utilities/key-value-pair';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { environment } from '@env/environment';
import { OperationResult } from '@utilities/operation-result';
import { IClearCache } from '@services/cache.service';
import { MonthlyAdditionsAndDeductionsSummaryReport_Param } from '@models/salary-report/7_2_22_monthly-additions-and-deductions-summary-report';

@Injectable({
  providedIn: 'root',
})

export class S_7_2_22_MonthlyAdditionsAndDeductionsSummaryReport implements IClearCache {
  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  baseUrl = `${environment.apiUrl}C_7_2_22_MonthlyAdditionsAndDeductionsSummaryReport/`;
  initData: MonthlyAdditionsAndDeductionsSummaryReport_Param = <MonthlyAdditionsAndDeductionsSummaryReport_Param>{
    total_Rows: 0
  }
  paramSearch = signal<MonthlyAdditionsAndDeductionsSummaryReport_Param>(structuredClone(this.initData))
  paramSearch$ = toObservable(this.paramSearch);

  setParamSearch = (data: MonthlyAdditionsAndDeductionsSummaryReport_Param) => this.paramSearch.set(data)
  clearParams() {
    this.paramSearch.set(structuredClone(this.initData))
  }

  constructor(
    private http: HttpClient
  ) { }
  getFactoryList() {
    let params = new HttpParams().set('Lang', this.language)
    return this.http.get<KeyValuePair[]>(` ${this.baseUrl}GetFactoryList`, { params });
  }
  getDropDownList(param: MonthlyAdditionsAndDeductionsSummaryReport_Param) {
    param.lang = this.language
    let params = new HttpParams().appendAll({ ...param });
    return this.http.get<KeyValuePair[]>(`${this.baseUrl}GetDropDownList`, { params });
  }
  process(param: MonthlyAdditionsAndDeductionsSummaryReport_Param) {
    param.lang = this.language
    let params = new HttpParams().appendAll({ ...param });
    return this.http.get<OperationResult>(`${this.baseUrl}Process`, { params });
  }
}
