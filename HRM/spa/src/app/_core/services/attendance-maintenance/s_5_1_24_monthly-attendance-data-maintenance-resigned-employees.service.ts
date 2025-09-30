import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { toObservable } from '@angular/core/rxjs-interop';
import { environment } from '@env/environment';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { Pagination, PaginationResult } from '@utilities/pagination-utility';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  ResignedEmployeeMemory,
  ResignedEmployeeParam,
  EmpResignedInfo,
  ResignedEmployeeMain,
  ResignedEmployeeDetail,
  ResignedEmployeeDetailParam
} from '@models/attendance-maintenance/5_1_24_monthly-attendance-resigned-employees';
import { IClearCache } from '@services/cache.service';

@Injectable({
  providedIn: 'root'
})
export class S_5_1_24_MonthlyAttendanceDataMaintenanceResignedEmployeesService implements IClearCache {
  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  apiUrl = `${environment.apiUrl}C_5_1_24_MonthlyAttendanceDataMaintenanceResignedEmployees`;

  initData = <ResignedEmployeeMemory>{
    params: <ResignedEmployeeParam>{
      language: localStorage.getItem(LocalStorageConstants.LANG)
    },
    pagination: <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0
    },
    datas: []
  };

  paramSearch = signal<ResignedEmployeeMemory>(structuredClone(this.initData));
  paramSearch$ = toObservable(this.paramSearch);

  setParamSearch = (data: ResignedEmployeeMemory) => this.paramSearch.set(data);
  clearParams = () => this.paramSearch.set(structuredClone(this.initData));

  constructor(private _http: HttpClient) { }

  add(data: ResignedEmployeeDetail) {
    return this._http.post<OperationResult>(`${this.apiUrl}/Add`, data);
  }

  edit(data: ResignedEmployeeDetail) {
    return this._http.put<OperationResult>(`${this.apiUrl}/Edit`, data);
  }

  getListFactory() {
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}/GetListFactory`, { params: { language: this.language } });
  }

  getListFactoryAdd() {
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}/GetListFactoryAdd`, { params: { language: this.language } });
  }

  getListDepartment(factory: string) {
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}/GetListDepartment`, { params: { factory, language: this.language } });
  }

  getListPermissionGroup() {
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}/GetListPermissionGroup`, { params: { language: this.language } });
  }

  getListSalaryType() {
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}/GetListSalaryType`, { params: { language: this.language } });
  }

  getDataPagination(pagination: Pagination, params: ResignedEmployeeParam) {
    params.language = this.language
    return this._http.get<PaginationResult<ResignedEmployeeMain>>(`${this.apiUrl}/GetDataPagination`, { params: { ...pagination, ...params } });
  }

  query(params: ResignedEmployeeParam) {
    params.language = this.language
    return this._http.get<ResignedEmployeeDetail>(`${this.apiUrl}/Query`, { params: { ...params } });
  }

  getEmpInfo(params: ResignedEmployeeParam) {
    params.language = this.language
    return this._http.get<OperationResult>(`${this.apiUrl}/GetEmpInfo`, { params: { ...params } });
  }

  getResignedDetail(params: ResignedEmployeeDetailParam) {
    params.language = this.language
    return this._http.get(`${this.apiUrl}/GetResignedDetail`, { params: { ...params } });
  }

  exportExcel(params: ResignedEmployeeParam) {
    params.language = this.language
    return this._http.get<OperationResult>(`${this.apiUrl}/ExportExcel`, { params: { ...params } });
  }
  getEmployeeIDByFactorys(factory: string) {
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}/GetEmployeeIDByFactorys`, { params: { factory } });
  }
  getListFactoryByUser() {
    return this._http.get<KeyValuePair[]>(`${this.apiUrl}/getListFactoryByUser`, { params: { language: this.language } });
  }
}
