import { HttpClient, HttpParams } from '@angular/common/http';
import { EventEmitter, Injectable } from '@angular/core';
import { environment } from '@env/environment';
import { Part, PartParam } from '@models/manage/team-management/part';
import { TeamManagementData } from '@models/manage/team-management/teamManagementData';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { Pagination, PaginationResult } from '@utilities/pagination-utility';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TeamManagementService {
  apiUrl: string = environment.apiUrl;
  partEmitter: EventEmitter<boolean> = new EventEmitter<boolean>();
  partSource = new BehaviorSubject<Part>(null);
  currentPart = this.partSource.asObservable();
  dataSource = new BehaviorSubject<PartParam>(null)
  currentDataSource = this.dataSource.asObservable();
  constructor(
    private http: HttpClient
  ) { }

  getData(pagination: Pagination, param: PartParam) {
    let params = new HttpParams().appendAll({...pagination, ...param });
    return this.http.get<PaginationResult<TeamManagementData>>(`${this.apiUrl}TeamManagement/GetDataPaginations`, { params });
  }

  getAllDepartment() {
    return this.http.get<KeyValuePair[]>(`${this.apiUrl}TeamManagement/GetAllDepartment`);
  }

  exportExcel(pagination: Pagination, param: PartParam) {
    let params = new HttpParams().appendAll({...pagination, ...param});
    return this.http.get<OperationResult>(`${this.apiUrl}TeamManagement/ExportExcel`, { params});
  }

  create(part: Part) {
    return this.http.post<OperationResult>(`${this.apiUrl}TeamManagement/Create`, part);
  }

  update(part: Part) {
    return this.http.post<OperationResult>(`${this.apiUrl}TeamManagement/Update`, part);
  }

  getDataDetail(partID: number) {
    let params = new HttpParams().set('partID', partID)
    return this.http.get<Part>(`${this.apiUrl}TeamManagement/Detail`, { params });
  }

  emitDataChange(check: boolean) {
    this.partEmitter.emit(check);
  }

  changeData(item: Part) {
    this.partSource.next(item);
  }
}
