import { S_5_2_2_WorkingHoursReportService } from '@services/attendance-maintenance/s_5_2_2_working-hours-report.service';
import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { WorkingHoursReportParam, WorkingHoursReportSource } from '@models/attendance-maintenance/5_2_2_working_hours_report';
import { InjectBase } from '@utilities/inject-base-app';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { Observable } from 'rxjs';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  param: WorkingHoursReportParam = <WorkingHoursReportParam>{};
  lastSearchParam: WorkingHoursReportParam;
  total: number = 0;
  minMode: BsDatepickerViewMode = 'day';
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM/DD',
    minMode: this.minMode
  };
  dateFrom: Date;
  dateTo: Date;

  constructor(private service: S_5_2_2_WorkingHoursReportService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getListDepartment();
    });
    effect(() => {
      const { param, dateFrom, dateTo, total } = this.service.paramSearch();
      this.param = param;
      this.dateFrom = dateFrom;
      this.dateTo = dateTo;
      this.total = total;
      if (!this.functionUtility.checkEmpty(this.param.factory)) {
        this.getListFactory();
        this.getListDepartment();
      }
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListFactory();
    this.lastSearchParam = { ...this.param };
  }

  ngOnDestroy(): void {
    this.service.setParamSearch(<WorkingHoursReportSource>{
      param: this.param,
      dateFrom: this.dateFrom,
      dateTo: this.dateTo,
      total: this.total
    });
  }

  checkRequiredParams(): boolean {
    var result = !this.functionUtility.checkEmpty(this.param.factory) &&
      !this.functionUtility.checkEmpty(this.dateFrom) &&
      !this.functionUtility.checkEmpty(this.dateTo) &&
      !this.functionUtility.checkEmpty(this.param.salary_WorkDays);
    return result;
  }

  //#region getList
  getListFactory() {
    this.getList(() => this.service.getListFactory(), this.listFactory);
  }

  getListDepartment() {
    this.getList(() => this.service.getListDepartment(this.param.factory), this.listDepartment);
  }

  onFactoryChange() {
    this.deleteProperty('department');
    this.getListDepartment();
  }

  getList(serviceMethod: () => Observable<KeyValuePair[]>, resultList: KeyValuePair[]) {
    serviceMethod().subscribe({
      next: (res) => {
        resultList.length = 0;
        resultList.push(...res);
      }
    });
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }
  //#endregion

  //#region search
  getTotal(isSearch?: boolean): Promise<void> {
    this.spinnerService.show();
    Object.assign(this.param, {
      date_From: this.formatDate(this.dateFrom),
      date_To: this.formatDate(this.dateTo),
    });

    return this.service.getTotal(this.param).toPromise().then(res => {
      this.total = res;
      if (isSearch) {
        this.lastSearchParam = { ...this.param };
        this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess');
        this.spinnerService.hide()
      }
    });
  }

  search(isSearch: boolean) {
    this.getTotal(isSearch);
  }

  paramChanged(): boolean {
    return JSON.stringify(this.param) !== JSON.stringify(this.lastSearchParam);
  }
  //#endregion

  //#region clear
  clear() {
    this.param = <WorkingHoursReportParam>{};
    this.listDepartment = [];
    this.dateFrom = null;
    this.dateTo = null;
    this.total = 0;
  }
  //#endregion

  //#region download
  download() {
    this.spinnerService.show();
    Object.assign(this.param, {
      date_From: this.formatDate(this.dateFrom),
      date_To: this.formatDate(this.dateTo),
    });

    this.service.downloadExcel(this.param).subscribe({
      next: async (res) => {
        if (res.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data, fileName);
          if (this.total === 0 || this.paramChanged()) {
            await this.getTotal()
            this.lastSearchParam = { ...this.param }
          }
        } else
          this.functionUtility.snotifySuccessError(false, 'System.Message.NoData');
        this.spinnerService.hide()
      }
    });
  }
  //#endregion

  formatDate(date: Date): string {
    return date ? this.functionUtility.getDateFormat(date) : '';
  }

  isNumberKey(event: KeyboardEvent): boolean {
    const charCode = (event.which) ? event.which : event.keyCode;
    if (charCode < 48 || charCode > 57) {
      event.preventDefault();
      return false;
    }
    return true;
  }
}
