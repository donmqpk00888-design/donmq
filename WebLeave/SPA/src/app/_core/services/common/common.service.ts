import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '@env/environment';
import { Area } from '@models/common/area';
import { Building } from '@models/common/building';
import { CommentArchive } from '@models/common/comment-archive';
import { Company } from '@models/common/company';
import { Department } from '@models/common/department';
import { BrowserInfo } from '@models/common/browser-info';

@Injectable({
  providedIn: 'root'
})
export class CommonService {

  apiUrl = environment.apiUrl;
  constructor(private http: HttpClient) { }
  getCompany() {
    return this.http.get<Company[]>(`${this.apiUrl}common/GetCompanys`);
  }
  getAreas() {
    return this.http.get<Area[]>(`${this.apiUrl}common/GetAreas`);
  }
  getBuildings() {
    return this.http.get<Building[]>(`${this.apiUrl}common/GetBuildings`);
  }
  getDepartments() {
    return this.http.get<Department[]>(`${this.apiUrl}common/GetDepartments`);
  }
  getCommentArchives() {
    return this.http.get<CommentArchive[]>(`${this.apiUrl}common/GetCommentArchives`);
  }
  getBrowserInfo(ipLocal: string, username: string) {
    return this.http.get<BrowserInfo>(`${this.apiUrl}common/GetBrowserInfo`, { params: { ipLocal, username } });
  }

  getServerTime() {
    return this.http.get<Date>(`${this.apiUrl}common/GetServerTime`);
  }
}
