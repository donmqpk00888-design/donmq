import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { ResolveFn } from '@angular/router';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { environment } from '@env/environment';
import { EmployeeCommonInfo } from '@models/common';
import {
  D_8_1_2_EmployeeRewardPenaltyRecordsData,
  D_8_1_2_EmployeeRewardPenaltyRecordsParam,
  EmployeeRewardAndPenaltyRecords_Memory,
  D_8_1_2_EmployeeRewardPenaltyRecordsSubParam,
  EmployeeRewardPenaltyRecordsReportDownloadFileModel,
} from '@models/reward-and-penalty-maintenance/8_1_2_employee-reward-and-penalty-records';
import { IClearCache } from '@services/cache.service';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { Pagination, PaginationParam, PaginationResult } from '@utilities/pagination-utility';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class S_8_1_2_EmployeeRewardAndPenaltyRecordsService
  implements IClearCache {
  get language(): string {
    return localStorage.getItem(LocalStorageConstants.LANG);
  }
  paramForm = new BehaviorSubject<string | null>(null);
  paramForm$ = this.paramForm.asObservable();
  setParamForm = (item: string) => this.paramForm.next(item);

  baseUrl = `${environment.apiUrl}C_8_1_2_EmployeeRewardPenaltyRecords/`;
  initData: EmployeeRewardAndPenaltyRecords_Memory = <
    EmployeeRewardAndPenaltyRecords_Memory
    >{

      param: <D_8_1_2_EmployeeRewardPenaltyRecordsParam>{},
      pagination: <Pagination>{
        pageNumber: 1,
        pageSize: 10,
        totalCount: 0,
      },
      selectedData: <D_8_1_2_EmployeeRewardPenaltyRecordsSubParam>{},
      data: [],
    };
  paramSearch = signal<EmployeeRewardAndPenaltyRecords_Memory>(
    structuredClone(this.initData)
  );
  paramSearch$ = toObservable(this.paramSearch);
  setParamSearch = (data: EmployeeRewardAndPenaltyRecords_Memory) =>
    this.paramSearch.set(data);

  constructor(private http: HttpClient) { }

  clearParams() {
    this.paramSearch.set(structuredClone(this.initData));
    this.paramForm.next(null);
  }
  downloadFile(data: EmployeeRewardPenaltyRecordsReportDownloadFileModel): Observable<OperationResult> {
    return this.http.post<OperationResult>(`${this.baseUrl}DownloadFile`, data);
  }
  downloadTemplate() {
    return this.http.get<OperationResult>(this.baseUrl + 'DownloadTemplate')
  }
  uploadExcel(file: FormData) {
    return this.http.post<OperationResult>(this.baseUrl + 'UploadFileExcel', file);
  }
  getSearch(pagination: PaginationParam, param: D_8_1_2_EmployeeRewardPenaltyRecordsParam) {
    param.language = this.language
    let params = new HttpParams().appendAll({ ...pagination, ...param });
    return this.http.get<PaginationResult<D_8_1_2_EmployeeRewardPenaltyRecordsData>>(`${this.baseUrl}GetSearch`, { params })
  }
  getDetail(history_GUID: string, language: string) {
    language = this.language
    var params = new HttpParams().appendAll({
      history_GUID, language
    });
    return this.http.get<D_8_1_2_EmployeeRewardPenaltyRecordsSubParam>(
      `${this.baseUrl}Data_Detail`,
      {
        params,
      }
    );
  }
  getEmployeeList(factory: string, employee_ID: string) {
    //language = this.language
    let param: D_8_1_2_EmployeeRewardPenaltyRecordsParam = <D_8_1_2_EmployeeRewardPenaltyRecordsParam>{
      factory: factory,
      employee_ID: employee_ID,
      language: this.language
    }

    let params = new HttpParams().appendAll({ ...param });
    return this.http.get<EmployeeCommonInfo[]>(` ${this.baseUrl}GetEmployeeList`, { params });
  }
  GetListReasonCode(factory: string) {
    return this.http.get<KeyValuePair[]>(`${this.baseUrl}GetListReasonCode`, { params: { factory } });
  }
  GetListRewardType() {
    return this.http.get<KeyValuePair[]>(`${this.baseUrl}GetListRewardType`, { params: { language: this.language } });
  }
  GetListFactory() {
    return this.http.get<KeyValuePair[]>(`${this.baseUrl}GetListFactory`, { params: { language: this.language } });
  }
  GetListDepartment(factory: string) {
    return this.http.get<KeyValuePair[]>(`${this.baseUrl}GetListDepartment`, { params: { language: this.language, factory } });
  }
  putData(data: D_8_1_2_EmployeeRewardPenaltyRecordsSubParam) {
    return this.http.put<OperationResult>(`${this.baseUrl}PutData`, data);
  }
  deleteData(data: D_8_1_2_EmployeeRewardPenaltyRecordsData): Observable<OperationResult> {
    return this.http.delete<OperationResult>(`${this.baseUrl}DeleteData`, { params: {}, body: data });
  }
  postData(data: D_8_1_2_EmployeeRewardPenaltyRecordsSubParam) {
    return this.http.post<OperationResult>(`${this.baseUrl}Create`, data);
  }
}
export const employeeRewardAndPenaltyResolver: ResolveFn<KeyValuePair[]> = () => {
  return inject(S_8_1_2_EmployeeRewardAndPenaltyRecordsService).GetListFactory();
};
