import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { environment } from '@env/environment';
import { MonthlySalaryTransferDetailsExitedEmployeeParam, MonthlySalaryTransferDetailsExitedEmployeeSource } from '@models/salary-report/7_2_13_monthly-salary-transfer-details-exited-employee';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';

@Injectable({
  providedIn: 'root'
})
export class S_7_2_13_MonthlySalaryTransferDetailsExitedEmployeeService {

  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
    apiUrl: string = environment.apiUrl + 'C_7_2_13_MonthlySalaryTransferDetailsExitedEmployee/';

    initData: MonthlySalaryTransferDetailsExitedEmployeeSource = <MonthlySalaryTransferDetailsExitedEmployeeSource>{
      param: <MonthlySalaryTransferDetailsExitedEmployeeParam>{
        permission_Group: []
      },
      totalRows: 0,
    }

    programSource = signal<MonthlySalaryTransferDetailsExitedEmployeeSource>(structuredClone(this.initData));
    programSource$ = toObservable(this.programSource);
    setSource = (source: MonthlySalaryTransferDetailsExitedEmployeeSource) => this.programSource.set(source);

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

    search(param: MonthlySalaryTransferDetailsExitedEmployeeParam) {
      const params = new HttpParams().appendAll({ ...param });
      return this._http.get<OperationResult>(`${this.apiUrl}GetTotalRows`, { params });
    }

    download(param: MonthlySalaryTransferDetailsExitedEmployeeParam) {
        param.language = this.language
        let params = new HttpParams().appendAll({ ...param });
        return this._http.get<OperationResult>(`${this.apiUrl}Download`, { params });
    }
}
