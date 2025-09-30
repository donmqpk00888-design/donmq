import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from "@env/environment";
import { KeyValuePair } from '@utilities/key-value-pair';
import { EmployeeCommonInfo, SystemInfo } from '@models/common';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { decode } from '@utilities/encryption-utility';
@Injectable({
  providedIn: 'root'
})
export class CommonService {
  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  get systemInfo(): SystemInfo { return decode(localStorage.getItem(LocalStorageConstants.SYSTEM_INFO)) as SystemInfo }
  apiUrl = environment.apiUrl + 'Common/';

  constructor(private http: HttpClient) { }

  getSystemInfo() {
    return this.http.get(this.apiUrl + 'GetSystemInfo', { responseType: 'text' })
  }
  getPasswordReset() {
    return this.http.get<Boolean>(this.apiUrl + 'GetPasswordReset')
  }
  getFactoryMain() {
    return this.http.get<KeyValuePair[]>(this.apiUrl + 'GetListFactoryMain', { params: { language: this.language } });
  }

  getListDepartment(factory: string) {
    return this.http.get<KeyValuePair[]>(this.apiUrl + 'GetListDepartment', { params: { language: this.language, factory } });
  }

  getListWorkShiftType() {
    return this.http.get<KeyValuePair[]>(this.apiUrl + 'GetListWorkShiftType', { params: { language: this.language } });
  }

  getListAttendanceOrLeave() {
    return this.http.get<KeyValuePair[]>(this.apiUrl + 'GetListAttendanceOrLeave', { params: { language: this.language } });
  }

  getListReasonCode() {
    return this.http.get<KeyValuePair[]>(this.apiUrl + 'GetListReasonCode', { params: { language: this.language } });
  }

  getListAccountAdd() {
    return this.http.get<KeyValuePair[]>(this.apiUrl + 'GetListAccountAdd', { params: { language: this.language } });
  }

  getListEmployeeAdd(factory: string) {
    return this.http.get<EmployeeCommonInfo[]>(`${this.apiUrl}GetListEmployeeAdd`, { params: { factory, language: this.language } });
  }

  getListSalaryItems() {
    return this.http.get<KeyValuePair[]>(this.apiUrl + 'GetListSalaryItems', { params: { language: this.language } });
  }
}
