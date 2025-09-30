import { MonthlyAttendanceSettingParam_Main } from '@models/attendance-maintenance/5_1_9_monthly-attendance-setting';
import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { Pagination } from '@utilities/pagination-utility';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { LangChangeEvent } from '@ngx-translate/core';
import { HRMS_Att_Use_Monthly_LeaveDto, MonthlyAttendanceSettingParam_Form, ParamForm, ParamMain, ParamSource } from '@models/attendance-maintenance/5_1_9_monthly-attendance-setting';
import { S_5_1_9_MonthlyAttendanceSettingService } from '@services/attendance-maintenance/s_5_1_9_monthly-attendance-setting.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  language: string = localStorage.getItem(LocalStorageConstants.LANG);
  title: string = '';
  iconButton = IconButton;
  factorys: KeyValuePair[] = [];
  param: MonthlyAttendanceSettingParam_Main = <MonthlyAttendanceSettingParam_Main>{};
  pagination: Pagination = <Pagination>{ pageNumber: 1, pageSize: 10 };
  data: HRMS_Att_Use_Monthly_LeaveDto[] = [];
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: "YYYY/MM",
    minMode: "month"
  };

  constructor(private _service: S_5_1_9_MonthlyAttendanceSettingService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getFactorys();
      if (this.data.length > 0) {
        if (this.functionUtility.checkFunction('Search'))
          this.getData();
      }
    });

    effect(() => {
      let mainSignalData = this._service.data();
      this.param = mainSignalData?.paramMain.paramSearch;
      this.pagination = mainSignalData?.paramMain.pagination;
      this.data = mainSignalData?.paramMain.data;
      if (this.data?.length > 0) {
        if (this.functionUtility.checkFunction('Search'))
          this.getData();
        else
          this.clear();
      }
    });
  }

  ngOnDestroy(): void {
    this._service.data.set(<ParamSource>{
      paramMain: <ParamMain>{
        data: this.data,
        pagination: this.pagination,
        paramSearch: this.param
      }
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.initialize();
  }

  initialize() {
    this.getFactorys();
  }

  getFactorys() {
    this._service.getFactorys().subscribe({
      next: res => {
        this.factorys = res
      }
    });
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this._service.getDataPagination(this.pagination, this.param).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.data = res.result;
        this.data.map(x => {
          x.effective_Month = new Date(x.effective_Month);
          x.effective_Month_Str = this.functionUtility.getDateFormat(new Date(x.effective_Month))
        })
        this.pagination = res.pagination;
        if (isSearch) {
          this.functionUtility.snotifySuccessError(isSearch, 'BasicMaintenance.2_6_GradeMaintenance.QueryOKMsg');
        }
      }
    });
  }

  clear() {
    this.param = <MonthlyAttendanceSettingParam_Main>{};
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.data = [];
  }

  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData();
  }

  onQuery(item: HRMS_Att_Use_Monthly_LeaveDto) {
    let paramForm = <ParamForm>{
      param: <MonthlyAttendanceSettingParam_Form>{
        effective_Month: item.effective_Month,
        effective_Month_Str: item.effective_Month_Str,
        factory: item.factory
      }
    };
    this._service.setParamForm(paramForm);
    this.router.navigate([`${this.router.routerState.snapshot.url}/query`]);
  }

  onEdit(item: HRMS_Att_Use_Monthly_LeaveDto) {
    let paramForm = <ParamForm>{
      param: <MonthlyAttendanceSettingParam_Form>{
        effective_Month: item.effective_Month,
        effective_Month_Str: item.effective_Month_Str,
        factory: item.factory
      }
    }
    this._service.setParamForm(paramForm);
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  onDelete(item: HRMS_Att_Use_Monthly_LeaveDto) {
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Caption.Confirm'), async () => {
      this.spinnerService.show();
      this._service.delete(item.factory, item.effective_Month_Str).subscribe({
        next: res => {
          if (res.isSuccess) {
            this.functionUtility.snotifySuccessError(res.isSuccess, 'System.Message.DeleteOKMsg');
            this.getData();
          } else {
            this.functionUtility.snotifySuccessError(res.isSuccess, 'System.Message.DeleteErrorMsg');
          }
          this.spinnerService.hide();
        }
      });
    });
  }
  onDateChange() {
    this.param.effective_Month_Str = this.param.effective_Month ? this.functionUtility.getDateFormat(new Date(this.param.effective_Month)) : '';
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
