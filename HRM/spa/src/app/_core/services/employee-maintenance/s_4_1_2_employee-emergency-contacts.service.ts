import { HttpClient, HttpParams } from '@angular/common/http';
import { EventEmitter, Injectable } from '@angular/core';
import { environment } from '@env/environment';
import { DataMain, EmployeeEmergencyContactsDto, EmployeeEmergencyContactsParam } from '@models/employee-maintenance/4_1_2_employee-emergency-contacts';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { IClearCache } from '@services/cache.service';
import { LocalStorageConstants, SessionStorageConstants } from '@constants/local-storage.constants';
import { FunctionInfomation } from '@models/common';

@Injectable({
  providedIn: 'root'
})
export class S_4_1_2_EmployeeEmergencyContactsService implements IClearCache {
  get functions(): FunctionInfomation[] { return JSON.parse(sessionStorage.getItem(SessionStorageConstants.SELECTED_FUNCTIONS)) };
  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  apiUrl: string = environment.apiUrl + 'C_4_1_2_EmployeeEmergencyContacts/';
  clearParams = () => { }

  constructor(private http: HttpClient) { }

  getData(param: EmployeeEmergencyContactsParam) {
    let params = new HttpParams().appendAll({ 'useR_GUID': param.useR_GUID, 'language': this.language });
    return this.http.get<DataMain>(this.apiUrl + "GetData", { params });
  }

  getRelationships() {
    return this.http.get<KeyValuePair[]>(this.apiUrl + "GetRelationships", { params: { language: this.language } });
  }

  create(model: EmployeeEmergencyContactsDto) {
    return this.http.post<OperationResult>(this.apiUrl + "Create", model);
  }

  update(model: EmployeeEmergencyContactsDto) {
    return this.http.put<OperationResult>(this.apiUrl + "Update", model);
  }

  delete(model: EmployeeEmergencyContactsDto) {
    let params = new HttpParams().appendAll({ 'useR_GUID': model.useR_GUID, 'seq': model.seq });
    return this.http.delete<OperationResult>(this.apiUrl + "Delete", { params });
  }

  downloadExcelTemplate() {
    return this.http.get<OperationResult>(this.apiUrl + "DownloadExcelTemplate");
  }

  uploadExcel(file: FormData) {
    return this.http.post<OperationResult>(this.apiUrl + "UploadExcel", file)
  }

  getMaxSeq(useR_GUID: string) {
    return this.http.get<number>(this.apiUrl + "GetSeqMax", { params: { useR_GUID } });
  }

}
