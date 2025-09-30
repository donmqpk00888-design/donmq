import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '@env/environment';
import { Pagination, PaginationResult } from '@utilities/pagination-utility';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import {
  OvertimeModificationMaintenanceParam,
  OvertimeModificationMaintenanceDto,
  ParamMain5_20,
  EmpPersonalAdd, ClockInClockOut
} from '../../models/attendance-maintenance/5_1_20_overtime-modification-maintenance';
import { toObservable } from '@angular/core/rxjs-interop';
import { IClearCache } from '@services/cache.service';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { EmployeeCommonInfo } from '@models/common';

@Injectable({
  providedIn: 'root'
})
export class S_5_1_20_OvertimeModificationMaintenanceService implements IClearCache {
  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  private apiUrl: string = environment.apiUrl + "C_5_1_20_OvertimeModificationMaintenance/";
  initData: ParamMain5_20 = <ParamMain5_20>{
    data: [],
    pagination: <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0
    },
    paramSearch: <OvertimeModificationMaintenanceParam>{}
  }
  signalDataMain = signal<ParamMain5_20>(structuredClone(this.initData));
  signalDataMain$ = toObservable(this.signalDataMain);

  constructor(private http: HttpClient) { }

  clearParams = () => {
    this.signalDataMain.set(structuredClone(this.initData));
  }

  getData(pagination: Pagination, param: OvertimeModificationMaintenanceParam) {
    param.language = this.language
    let params = new HttpParams().appendAll({ 'pageNumber': pagination.pageNumber, 'pageSize': pagination.pageSize, ...param });
    return this.http.get<PaginationResult<OvertimeModificationMaintenanceDto>>(this.apiUrl + "GetData", { params });
  }

  getListFactory() {
    return this.http.get<KeyValuePair[]>(this.apiUrl + "GetListFactory", { params: { language: this.language } });
  }

  getWorkShiftType(param: OvertimeModificationMaintenanceParam) {
    param.language = this.language
    let params = new HttpParams().appendAll({ ...param });
    return this.http.get<OperationResult>(this.apiUrl + "GetWorkShiftType", { params });
  }

  getListHoliday() {
    return this.http.get<KeyValuePair[]>(this.apiUrl + "GetListHoliday", { params: { language: this.language } });
  }

  getListDepartment(factory: string) {
    return this.http.get<KeyValuePair[]>(this.apiUrl + "GetListDepartment", { params: { factory, language: this.language } });
  }

  getWorkShiftTypeTime(work_Shift_Type: string, date: string, factory: string) {
    return this.http.get<ClockInClockOut>(this.apiUrl + "GetWorkShiftTypeTime", { params: { work_Shift_Type, date, factory } });
  }

  getClockTime(employee_ID: string, date: string) {
    return this.http.get<ClockInClockOut>(this.apiUrl + "GetClockInTimeAndClockOutTimeByEmpIdAndDate", { params: { employee_ID, date } });
  }

  create(models: OvertimeModificationMaintenanceDto) {
    return this.http.post<OperationResult>(this.apiUrl + "Create", models);
  }

  edit(model: OvertimeModificationMaintenanceDto) {
    return this.http.put<OperationResult>(this.apiUrl + "Edit", model);
  }

  delete(model: OvertimeModificationMaintenanceDto) {
    return this.http.delete<OperationResult>(this.apiUrl + "Delete", { params: {}, body: model });
  }
}
