import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { environment } from '@env/environment';
import { DownloadPersonnelDataToExcel_Source, DownloadPersonnelDataToExcel_Param } from '@models/salary-report/7_2_16_download-personnel-data-to-excel';
import { IClearCache } from '@services/cache.service';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';

@Injectable({
  providedIn: 'root'
})
export class S_7_2_16_DownloadPersonnelDataToExcelService implements IClearCache {
  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  apiUrl = `${environment.apiUrl}C_7_2_16_DownloadPersonnelDatatoEXCEL/`
  initData: DownloadPersonnelDataToExcel_Source = <DownloadPersonnelDataToExcel_Source>{
    param: <DownloadPersonnelDataToExcel_Param>{
      employeeKind: "Onjob",
      reportKind: "EmployeeMasterFile",
      permissionGroup: []
    },
    totalRows: 0
  }

  paramSearch = signal<DownloadPersonnelDataToExcel_Source>(structuredClone(this.initData))
  paramSearch$ = toObservable(this.paramSearch)
  setParamSearch = (data: DownloadPersonnelDataToExcel_Source) => this.paramSearch.set(data);

  clearParams = () => {
    this.paramSearch.set(structuredClone(this.initData))
  };
  getTotalRows(param: DownloadPersonnelDataToExcel_Param) {
    param.language = this.language
    let params = new HttpParams().appendAll({ ...param });
    return this.http.get<OperationResult>(this.apiUrl + 'GetTotalRows', { params })
  }

  download(param: DownloadPersonnelDataToExcel_Param) {
    param.language = this.language
    let params = new HttpParams().appendAll({ ...param });
    return this.http.get<OperationResult>(this.apiUrl + 'Download', { params });
  }
  constructor(private http: HttpClient) { }
  getListFactory() {
    return this.http.get<KeyValuePair[]>(`${this.apiUrl}GetListFactory`, { params: { language: this.language } })
  }
  getDepartmentList(factory: string) {
    return this.http.get<KeyValuePair[]>(this.apiUrl + 'GetListDepartment', { params: { factory, language: this.language } });
  }

  getPermissionGroup(factory: string) {
    return this.http.get<KeyValuePair[]>(this.apiUrl + 'GetListPermissionGroup', { params: { factory, language: this.language } });
  }
}
