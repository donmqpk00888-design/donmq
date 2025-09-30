import { OperationResult } from '../../utilities/operation-result';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { environment } from '@env/environment';
import { D_7_2_6_MonthlyNonTransferSalaryPaymentReportParam, MonthlyNonTransferSalaryPaymentReportSource } from '@models/salary-report/7_2_6_monthly-non-transfer-salary-payment-report';
import { KeyValuePair } from '@utilities/key-value-pair';

@Injectable({
  providedIn: 'root'
})
export class S_7_2_6_MonthlyNonTransferSalaryPaymentReportService {
  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  apiUrl: string = environment.apiUrl + 'C_7_2_6_MonthlyNonTransferSalaryPaymentReport/';

  initData: MonthlyNonTransferSalaryPaymentReportSource = <MonthlyNonTransferSalaryPaymentReportSource>{
    param: <D_7_2_6_MonthlyNonTransferSalaryPaymentReportParam>{
      permission_Group: []
    },
    totalRows: 0,
    year_Month: null
  }

  programSource = signal<MonthlyNonTransferSalaryPaymentReportSource>(structuredClone(this.initData));
  programSource$ = toObservable(this.programSource);
  setSource = (source: MonthlyNonTransferSalaryPaymentReportSource) => this.programSource.set(source);

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

  search(param: D_7_2_6_MonthlyNonTransferSalaryPaymentReportParam) {
    const params = new HttpParams().appendAll({ ...param });
    return this._http.get<number>(`${this.apiUrl}SearchData`, { params });
  }

  downloadPdf(param: D_7_2_6_MonthlyNonTransferSalaryPaymentReportParam) {
    param.language = this.language;
    const params = new HttpParams().appendAll({ ...param });
    return this._http.get<OperationResult>(`${this.apiUrl}DownloadPdf`, { params });
  }

}
