import {
  HRMSEmpExternalExperience,
  HRMSEmpExternalExperienceModel,
  HRMS_Emp_External_ExperienceParam
} from '@models/employee-maintenance/4_1_5_external-experience';
import { environment } from '@env/environment';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { OperationResult } from '@utilities/operation-result';
import { IClearCache } from '@services/cache.service';
import { SessionStorageConstants } from '@constants/local-storage.constants';
import { FunctionInfomation } from '@models/common';

@Injectable({
  providedIn: 'root'
})
export class S_4_1_5_ExternalExperienceService implements IClearCache {
  get functions(): FunctionInfomation[] { return JSON.parse(sessionStorage.getItem(SessionStorageConstants.SELECTED_FUNCTIONS)) };
  baseUrl: string = environment.apiUrl + "C_4_1_5_ExternalExperience/";

  constructor(private http: HttpClient) { }
  clearParams: () => void;

  getData(param: HRMS_Emp_External_ExperienceParam) {
    let params = new HttpParams().appendAll({ ...param })
    return this.http.get<HRMSEmpExternalExperience[]>(this.baseUrl + "GetData", { params });
  }

  getSeq(param: HRMSEmpExternalExperienceModel) {
    let params = new HttpParams().appendAll({ ...param })
    return this.http.get<number>(this.baseUrl + "GetSeq", { params })
  }

  add(param: HRMSEmpExternalExperienceModel) {
    return this.http.post<OperationResult>(this.baseUrl + "Create", param);
  }

  edit(param: HRMSEmpExternalExperienceModel) {
    return this.http.put<OperationResult>(this.baseUrl + "Update", param)
  }

  delete(model: HRMSEmpExternalExperienceModel) {
    return this.http.delete<OperationResult>(this.baseUrl + "Delete", { params: {}, body: model });
  }

}
