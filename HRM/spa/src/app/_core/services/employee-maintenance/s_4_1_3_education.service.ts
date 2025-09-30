import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { LocalStorageConstants, SessionStorageConstants } from '@constants/local-storage.constants';
import { environment } from '@env/environment';
import { FunctionUtility } from '@utilities/function-utility';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import {
  EducationFile,
  EducationUpload,
  HRMS_Emp_Educational,
  HRMS_Emp_EducationalParam,
  HRMS_Emp_Educational_FileUpload
} from '@models/employee-maintenance/4_1_3-education';
import { IClearCache } from '@services/cache.service';
import { FunctionInfomation } from '@models/common';

@Injectable({
  providedIn: 'root'
})
export class S_4_1_3_EducationService implements IClearCache {
  get functions(): FunctionInfomation[] { return JSON.parse(sessionStorage.getItem(SessionStorageConstants.SELECTED_FUNCTIONS)) };
  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  baseUrl = `${environment.apiUrl}C_4_1_3_Education`;
  constructor(private _http: HttpClient, private _functionUtility: FunctionUtility) { }
  clearParams: () => void;

  getDegrees() {
    return this._http.get<KeyValuePair[]>(
      `${this.baseUrl}/GetDegrees`, { params: { language: this.language } }
    );
  }

  getAcademicSystems() {
    return this._http.get<KeyValuePair[]>(
      `${this.baseUrl}/GetAcademicSystems`, { params: { language: this.language } }
    );
  }

  getMajors() {
    return this._http.get<KeyValuePair[]>(
      `${this.baseUrl}/GetMajors`, { params: { language: this.language } }
    );
  }

  getDataPagination(filter: HRMS_Emp_EducationalParam) {
    filter.language = this.language
    let params = new HttpParams().appendAll({ ...filter });
    return this._http.get<HRMS_Emp_Educational[]>(
      `${this.baseUrl}/GetDataPagination`, { params }
    );
  }

  getEducationFiles(user_GUID: string) {
    return this._http.get<HRMS_Emp_Educational_FileUpload[]>(
      `${this.baseUrl}/GetEducationFiles`, { params: { user_GUID } }
    );
  }

  create(model: HRMS_Emp_Educational) {
    return this._http.post<OperationResult>(`${this.baseUrl}/Create`, model);
  }

  update(model: HRMS_Emp_Educational) {
    return this._http.put<OperationResult>(`${this.baseUrl}/Update`, model);
  }


  delete(model: HRMS_Emp_Educational) {
    return this._http.delete<OperationResult>(
      `${this.baseUrl}/Delete`, { params: {}, body: model }
    );
  }

  deleteEducationFile(model: EducationFile) {
    return this._http.delete<OperationResult>(
      `${this.baseUrl}/DeleteEducationFile`, { params: {}, body: model }
    );
  }

  uploadFiles(model: EducationUpload) {
    let formdata = this._functionUtility.toFormData(model);
    return this._http.post<OperationResult>(`${this.baseUrl}/UploadFiles`, formdata);
  }

  downloadFile(model: EducationFile) {
    return this._http.post<OperationResult>(`${this.baseUrl}/DownloadFile`, model);
  }
}

export const resolverDegrees: ResolveFn<KeyValuePair[]> = () => {
  return inject(S_4_1_3_EducationService).getDegrees();
};

export const resolverAcademicSystems: ResolveFn<KeyValuePair[]> = () => {
  return inject(S_4_1_3_EducationService).getAcademicSystems();
};

export const resolverMajors: ResolveFn<KeyValuePair[]> = () => {
  return inject(S_4_1_3_EducationService).getMajors();
};
