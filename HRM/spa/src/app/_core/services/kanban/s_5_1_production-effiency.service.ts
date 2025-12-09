import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { environment } from '@env/environment';
import { ProductionEfficiencyDTO, ProductionEfficiencyParam } from '@models/kanban/5_1_production-effiency';
import { PaginationResult } from '@utilities/pagination-utility';

@Injectable({
  providedIn: 'root'
})
export class S_5_1_productionEffiencyService {
  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  baseUrl: string = environment.apiUrl + "C_5_1_ProductionEfficiency/"
  constructor(private http: HttpClient) { }
  getData(param: ProductionEfficiencyParam) {
    param.lang = this.language
    let params = new HttpParams().appendAll({ ...param });
    return this.http.get<ProductionEfficiencyDTO[]>(this.baseUrl + 'GetData', { params });
  }
}
