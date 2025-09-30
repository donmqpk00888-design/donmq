import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { environment } from '@env/environment';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { Pagination, PaginationResult } from '@utilities/pagination-utility';
import {
  EmpLeaveInfo,
  MaintenanceOfAnnualLeaveEntitlement,
  MaintenanceOfAnnualLeaveEntitlementMemory,
  MaintenanceOfAnnualLeaveEntitlementParam
} from '@models/attendance-maintenance/5_1_7_maintenance_of_annual_leave_entitlement';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { IClearCache } from '@services/cache.service';

@Injectable({
  providedIn: 'root'
})
export class S_5_1_7_MaintenanceOfAnnualLeaveEntitlementService implements IClearCache {
  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  apiUrl = `${environment.apiUrl}C_5_1_7_MaintenanceOfAnnualLeaveEntitlement`;

  initData: MaintenanceOfAnnualLeaveEntitlementMemory = <MaintenanceOfAnnualLeaveEntitlementMemory>{
    params: <MaintenanceOfAnnualLeaveEntitlementParam>{
      language: localStorage.getItem(LocalStorageConstants.LANG)
    },
    pagination: <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0
    },
    datas: []
  }
  paramSearch = signal<MaintenanceOfAnnualLeaveEntitlementMemory>(structuredClone(this.initData));
  paramSearch$ = toObservable(this.paramSearch);
  setParamSearch = (data: MaintenanceOfAnnualLeaveEntitlementMemory) => this.paramSearch.set(data)
  clearParams = () => {
    this.paramSearch.set(structuredClone(this.initData))
  }

  constructor(private _http: HttpClient) { }

  add(data: MaintenanceOfAnnualLeaveEntitlement) {
    return this._http.post<OperationResult>(`${this.apiUrl}/Add`, data);
  }

  edit(data: MaintenanceOfAnnualLeaveEntitlement) {
    return this._http.put<OperationResult>(`${this.apiUrl}/Edit`, data);
  }

  delete(data: MaintenanceOfAnnualLeaveEntitlement) {
    return this._http.delete<OperationResult>(`${this.apiUrl}/Delete`, { body: data });
  }
  getListFactory() {
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}/GetListFactory`, { params: { language: this.language } });
  }

  getListDepartment(factory: string,) {
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}/GetListDepartment`, { params: { factory, language: this.language } });
  }

  getListLeaveCode() {
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}/GetListLeaveCode`, { params: { language: this.language } });
  }

  query(pagination: Pagination, params: MaintenanceOfAnnualLeaveEntitlementParam) {
    params.language = this.language
    return this._http.get<PaginationResult<MaintenanceOfAnnualLeaveEntitlement>>(`${this.apiUrl}/Query`, { params: { ...pagination, ...params } });
  }

  exportExcel() {
    return this._http.get(`${this.apiUrl}/ExportExcel`, { responseType: 'blob' });
  }
  downloadExcel(param: MaintenanceOfAnnualLeaveEntitlementParam) {
    param.language = this.language
    let params = new HttpParams().appendAll({ ...param });
    return this._http.get<OperationResult>(`${this.apiUrl}/DownloadExcel`, { params });
  }

  uploadExcel(formData: FormData) {
    formData.append('language', this.language);
    return this._http.post<OperationResult>(`${this.apiUrl}/UploadExcel`, formData);
  }
  checkExistedData(data: MaintenanceOfAnnualLeaveEntitlement) {
    let params = new HttpParams()
      .set('Annual_Start', data.annual_Start)
      .set('Factory', data.factory)
      .set('Employee_ID', data.employee_ID)
      .set('Leave_Code', data.leave_Code)
    return this._http.get<OperationResult>(`${this.apiUrl}/CheckExistedData`, { params });
  }
}
