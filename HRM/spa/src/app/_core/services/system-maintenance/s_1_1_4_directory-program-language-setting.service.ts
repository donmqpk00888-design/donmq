import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { environment } from '@env/environment';
import {
  DirectoryProgramLanguageSetting_Param,
  DirectoryProgramLanguageSetting_Data,
  DirectoryProgramLanguageSetting_Memory
} from '@models/system-maintenance/1_1_4_directory-program-language';
import { IClearCache } from '@services/cache.service';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { Pagination, PaginationParam, PaginationResult } from '@utilities/pagination-utility';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class S_1_1_4_DirectoryProgramLanguageSettingService implements IClearCache {
  baseUrl: string = environment.apiUrl + 'C_1_1_4_DirectoryProgramLanguageSetting/';
  initData: DirectoryProgramLanguageSetting_Memory = <DirectoryProgramLanguageSetting_Memory>{
    param: <DirectoryProgramLanguageSetting_Param>{},
    pagination: <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0
    },
    selectedData: <DirectoryProgramLanguageSetting_Data>{},
    data: []
  }
  programSource = signal<DirectoryProgramLanguageSetting_Memory>(structuredClone(this.initData))
  source = toObservable(this.programSource);
  SetSource = (source: DirectoryProgramLanguageSetting_Memory) => this.programSource.set(source);
  clearParams = () => {
    this.programSource.set(structuredClone(this.initData))
  }
  constructor(private http: HttpClient) { }

  getData(pagination: PaginationParam, param: DirectoryProgramLanguageSetting_Param) {
    let params = new HttpParams().appendAll({ ...pagination, ...param });
    return this.http.get<PaginationResult<DirectoryProgramLanguageSetting_Data>>(this.baseUrl + 'GetData', { params });
  }
  delete(kind: string, code: string): Observable<OperationResult> {
    let params = new HttpParams().appendAll({ kind, code })
    return this.http.delete<OperationResult>(this.baseUrl + 'Delete', { params });
  }
  addNew(param: DirectoryProgramLanguageSetting_Data) {
    return this.http.post<OperationResult>(this.baseUrl + "Add", param);
  }
  edit(param: DirectoryProgramLanguageSetting_Data) {
    return this.http.put<OperationResult>(this.baseUrl + "Update", param);
  }

  //Add
  getLanguage() {
    return this.http.get<KeyValuePair[]>(this.baseUrl + 'GetLanguage');
  }
  getProgram() {
    return this.http.get<KeyValuePair[]>(this.baseUrl + 'GetProgram');
  }
  getDirectory() {
    return this.http.get<KeyValuePair[]>(this.baseUrl + 'GetDirectory');
  }
  //Edit
  getDetail(kind: string, code: string,) {
    let params = new HttpParams().appendAll({ kind, code })
    return this.http.get<DirectoryProgramLanguageSetting_Data>(this.baseUrl + 'GetDetail', { params });
  }

}
