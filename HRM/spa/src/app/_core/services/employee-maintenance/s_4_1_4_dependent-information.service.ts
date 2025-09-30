import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '@env/environment';
import { HRMS_Emp_Dependent, HRMS_Emp_DependentParam } from '@models/employee-maintenance/4_1_4_dependent-information';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { IClearCache } from '@services/cache.service';
import { LocalStorageConstants, SessionStorageConstants } from '@constants/local-storage.constants';
import { FunctionInfomation } from '@models/common';

@Injectable({
  providedIn: 'root'
})
export class S_4_1_4_DependentInformationService implements IClearCache {
  get functions(): FunctionInfomation[] { return JSON.parse(sessionStorage.getItem(SessionStorageConstants.SELECTED_FUNCTIONS)) };
  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  baseUrl = `${environment.apiUrl}C_4_1_4_DependentInformation`;

  constructor(private http: HttpClient) { }
  clearParams: () => void;

  getData(model: HRMS_Emp_DependentParam) {
    model.lang = this.language
    let params = new HttpParams().appendAll({ ...model });
    return this.http.get<HRMS_Emp_Dependent[]>(`${this.baseUrl}/GetData`, { params });
  }

  create(model: HRMS_Emp_Dependent) {
    return this.http.post<OperationResult>(`${this.baseUrl}/AddNew`, model);
  }
  edit(model: HRMS_Emp_Dependent) {
    return this.http.put<OperationResult>(`${this.baseUrl}/Edit`, model);
  }

  delete(model: HRMS_Emp_Dependent) {
    return this.http.delete<OperationResult>(
      `${this.baseUrl}/Delete`, { params: {}, body: model }
    );
  }
  GetListRelationship() {
    return this.http.get<KeyValuePair[]>(
      `${this.baseUrl}/GetListRelationship`, { params: { language: this.language } }
    );
  }

  getSeq(model: HRMS_Emp_Dependent) {
    let params = new HttpParams().appendAll({ ...model })
    return this.http.get<number>(`${this.baseUrl}/GetSeqMax`, { params })
  }

}
