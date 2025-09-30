import { Component, OnInit, effect } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import {
  MaintenanceActiveEmployeesMain,
  MaintenanceActiveEmployeesMemory,
  MaintenanceActiveEmployeesDetailParam,
  MaintenanceActiveEmployeesParam,
} from '@models/attendance-maintenance/5_1_22_monthly-attendance-data-maintenance-active-employees';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { KeyValuePair } from '@utilities/key-value-pair';
import { S_5_1_22_MonthlyAttendanceDataMaintenanceActiveEmployeesService } from '@services/attendance-maintenance/s_5_1_22_monthly-attendance-data-maintenance-active-employees.service';
import { ClassButton, IconButton } from '@constants/common.constants';
import { CaptionConstants } from '@constants/message.enum';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss',
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  classButton = ClassButton;

  param: MaintenanceActiveEmployeesParam = <MaintenanceActiveEmployeesParam>{};
  att_Month_Start: Date = null;
  att_Month_End: Date = null;
  data: MaintenanceActiveEmployeesMain[] = [];
  pagination: Pagination = <Pagination>{};
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];

  constructor(
    private _service: S_5_1_22_MonthlyAttendanceDataMaintenanceActiveEmployeesService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    effect(() => {
      this.param = this._service.paramSearch().param;
      this.pagination = this._service.paramSearch().pagination;
      this.data = this._service.paramSearch().data;

      this.getListFactory();
      this.getListDepartment();
      this.setQueryDate();
      if (this.data.length > 0) {
        if (!this.checkParam() && this.functionUtility.checkFunction('Search'))
          this.query(false);
        else
          this.clear()
      }
    });

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((res) => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.param.language = res.lang;
        this.getListFactory();
        this.getListDepartment();
        if(this.data.length > 0){
          if(!this.checkParam() && this.functionUtility.checkFunction('Search'))
            this.query(false);
          else
            this.clear();
        }
      });
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.param.language = localStorage.getItem(LocalStorageConstants.LANG);
    this.getListFactory();
  }
  ngOnDestroy(): void {
    const data: MaintenanceActiveEmployeesMemory = <MaintenanceActiveEmployeesMemory>{
      param: this.param,
      pagination: this.pagination,
      data: this.data,
    };
    this._service.setParamSearch(data);
  }
  changeFactory() {
    this.getListDepartment();
  }

  checkParam() {
    return (
      !this.param.factory ||
      this.functionUtility.checkEmpty(this.param.att_Month_Start) ||
      this.functionUtility.checkEmpty(this.param.att_Month_End)
    );
  }

  setQueryDate() {
    if (this.param.att_Month_Start)
      this.att_Month_Start = new Date(this.param.att_Month_Start);
    if (this.param.att_Month_End)
      this.att_Month_End = new Date(this.param.att_Month_End);
  }

  clear() {
    this.param = <MaintenanceActiveEmployeesParam>{ language: localStorage.getItem(LocalStorageConstants.LANG) };
    this.att_Month_End = null;
    this.att_Month_Start = null;
    this.listDepartment = [];
    this.data = [];
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
      totalPage: 0,
    };
  }

  search() {
    this.pagination.pageNumber == 1
      ? this.query(true)
      : (this.pagination.pageNumber = 1);
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.query(false);
  }

  query(isSearch: boolean) {
    if (!this.att_Month_Start || !this.att_Month_End)
      return this.snotifyService.warning(
        'Please enter Year-Month of Attendance',
        CaptionConstants.WARNING
      );

    this.param.att_Month_Start = this.att_Month_Start.toStringYearMonth();
    this.param.att_Month_End = this.att_Month_End.toStringYearMonth();
    this.param.salary_Days =
      this.param.salary_Days != null ? `${this.param.salary_Days}` : '';
    this.spinnerService.show();
    this._service.query(this.pagination, this.param).subscribe({
      next: (res) => {
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.snotifyService.success(
            this.translateService.instant('System.Message.SearchOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );

        this.spinnerService.hide();
      },
    });
  }

  add() {
    let param: MaintenanceActiveEmployeesParam = <MaintenanceActiveEmployeesParam>{ action: 'add' };
    let state = { param: param };
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`], { state: state });
  }

  edit(item: MaintenanceActiveEmployeesMain) {
    let param: MaintenanceActiveEmployeesDetailParam = <MaintenanceActiveEmployeesDetailParam>{
      factory: item.factory,
      employee_ID: item.employee_ID,
      att_month: item.att_Month,
      isProbation: item.isProbation,
      action: 'edit',
      department: item.department
    };
    let state = { param: param };
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`], { state: state });
  }

  detail(item: MaintenanceActiveEmployeesMain) {
    let param: MaintenanceActiveEmployeesDetailParam = <MaintenanceActiveEmployeesDetailParam>{
      factory: item.factory,
      employee_ID: item.employee_ID,
      att_month: item.att_Month,
      isProbation: item.isProbation,
      action: 'query',
    };
    let state = { param: param };
    this.router.navigate([`${this.router.routerState.snapshot.url}/query`], { state: state });
  }

  download() {
    this.spinnerService.show();
    this._service.download(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        result.isSuccess
          ? this.functionUtility.exportExcel(result.data, fileName)
          : this.functionUtility.snotifySuccessError(result.isSuccess, result.error);
      },
    });
  }
  //#region Get List
  getListFactory() {
    this._service.getListFactoryByUser().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  getListDepartment() {
    this._service
      .getListDepartment(this.param.factory)
      .subscribe({
        next: (res) => {
          this.listDepartment = res;
        },
      });
  }
  onDateChange(name: string) {
    this.param[name] = this.functionUtility.isValidDate(new Date(this[name])) ? this.functionUtility.getDateFormat(new Date(this[name])) : '';
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
  //#endregion
}
