import { LocalStorageConstants } from '@constants/local-storage.constants';
import { KeyValuePair } from '@utilities/key-value-pair';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { toObservable } from '@angular/core/rxjs-interop';
import { Pagination } from '@utilities/pagination-utility';
import { environment } from '@env/environment';
import { Day, FactoryCalendar_MainData, FactoryCalendar_MainMemory, FactoryCalendar_MainParam, FactoryCalendar_Table, Week } from '@models/attendance-maintenance/5_1_1_factory-calendar';
import { BehaviorSubject, Observable } from 'rxjs';
import { FunctionUtility } from '@utilities/function-utility';
import { OperationResult } from '@utilities/operation-result';
import { IClearCache } from '@services/cache.service';

@Injectable({
  providedIn: 'root',
})

export class S_5_1_1_FactoryCalendar implements IClearCache {
  get language(): string { return localStorage.getItem(LocalStorageConstants.LANG) }
  baseUrl = `${environment.apiUrl}C_5_1_1_FactoryCalendar/`;
  now: Date = new Date()
  initData: FactoryCalendar_MainMemory = <FactoryCalendar_MainMemory>{
    param: <FactoryCalendar_MainParam>{},
    data: <FactoryCalendar_MainData>{
      table: {
        result: [],
        pagination: <Pagination>{
          pageNumber: 1,
          pageSize: 10,
          totalCount: 0
        }
      },
      calendar: {
        weeks: []
      }
    }
  }
  paramSearch = signal<FactoryCalendar_MainMemory>(structuredClone(this.initData));
  paramSearch$ = toObservable(this.paramSearch);
  setParamSearch = (data: FactoryCalendar_MainMemory) => this.paramSearch.set(data)

  paramForm = new BehaviorSubject<FactoryCalendar_Table>(null);
  paramForm$ = this.paramForm.asObservable();
  setParamForm = (item: FactoryCalendar_Table) => this.paramForm.next(item);

  constructor(
    private http: HttpClient,
    private functionUtility: FunctionUtility
  ) { }

  clearParams() {
    this.paramSearch.set(structuredClone(this.initData))
    this.paramForm.next(null)
  }
  checkExistedData(division: string, factory: string, att_Date_Str: string) {
    let params = new HttpParams()
      .set('Division', division)
      .set('Factory', factory)
      .set('Att_Date', att_Date_Str)
    return this.http.get<OperationResult>(` ${this.baseUrl}CheckExistedData`, { params });
  }
  getDropDownList(division?: string) {
    let param: FactoryCalendar_MainParam = <FactoryCalendar_MainParam>{
      lang: this.language
    }
    if (division)
      param.division = division
    let params = new HttpParams().appendAll({ ...param });
    return this.http.get<KeyValuePair[]>(`${this.baseUrl}GetDropDownList`, { params });
  }
  getSearchDetail(
    param: Pagination,
    filter: FactoryCalendar_MainParam
  ): Observable<OperationResult> {
    filter.lang = this.language
    let params = new HttpParams().appendAll({ ...param, ...filter });
    return this.http.get<OperationResult>(
      `${this.baseUrl}GetSearchDetail`, { params }
    );
  }
  putData(data: FactoryCalendar_Table): Observable<OperationResult> {
    return this.http.put<OperationResult>(`${this.baseUrl}PutData`, data);
  }
  deleteData(data: FactoryCalendar_Table): Observable<OperationResult> {
    return this.http.delete<OperationResult>(`${this.baseUrl}DeleteData`, { params: {}, body: data });
  }
  postData(data: FactoryCalendar_Table): Observable<OperationResult> {
    return this.http.post<OperationResult>(`${this.baseUrl}PostData`, data);
  }
  downloadExcelTemplate() {
    return this.http.get<OperationResult>(` ${this.baseUrl}DownloadExcelTemplate`);
  }
  downloadExcel(param: FactoryCalendar_MainParam) {
    param.lang = this.language
    let params = new HttpParams().appendAll({ ...param });
    return this.http.get<OperationResult>(` ${this.baseUrl}DownloadExcel`, { params });
  }
  uploadExcel(file: FormData) {
    return this.http.post<OperationResult>(`${this.baseUrl}UploadExcel`, file);
  }
  getCalendarTemplate(division: string, factory: string, month: number, year: number): Week[] {
    const firstDate = new Date(year, month, 1)
    const lastDate = new Date(year, month + 1, 0)
    const monthTotalDays = lastDate.getDate()
    const firstDayPosition = firstDate.getDay()
    const preMonthEndDay = new Date(year, month, 0)
    const preMonthDays = preMonthEndDay.getDate()
    let weeks = []
    let start: number = 0
    let end: number = 0
    if (firstDayPosition === 1) {
      start = 1
      end = 7
    }
    else if (firstDayPosition === 0) {
      start = preMonthDays - 6 + 1
      end = 1
    }
    else {
      start = preMonthDays + 1 - firstDayPosition + 1
      end = 7 - firstDayPosition + 1
      weeks.push({ start: start, end: end })
      start = end + 1
      end = end + 7
    }
    while (start <= monthTotalDays) {
      weeks.push({ start: start, end: end });
      start = end + 1;
      end = end + 7;
      end = start === 1 && end === 8 ? 1 : end;
      if (end > monthTotalDays && start <= monthTotalDays) {
        end = end - monthTotalDays
        weeks.push({ start: start, end: end })
        break
      }
    }
    return weeks.map(({ start, end }, index) => {
      const sub = +(start > end && index === 0);
      const result = Array.from({ length: 7 }, (_, index) => {
        const date = new Date(year, month - sub, start + index);
        let dateString = this.functionUtility.getDateFormat(date)
        let style = date.getMonth() == month ? 'normal-date' : 'disabled-date'
        return <Day>{
          date: date.getDate(),
          month: date.toLocaleString('en', { month: 'numeric' }),
          day: date.toLocaleString('en', { weekday: 'long' }),
          date_String: dateString,
          style: dateString == this.functionUtility.getDateFormat(this.now) ? style + ' today' : style,
          division: division,
          factory: factory
        };
      });
      return <Week>{ days: result }
    })
  }
}
export const factoryCalendarResolver: ResolveFn<KeyValuePair[]> = () => {
  return inject(S_5_1_1_FactoryCalendar).getDropDownList();
};
