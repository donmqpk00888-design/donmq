import { InjectBase } from '@utilities/inject-base-app';
import { IconButton } from '@constants/common.constants';
import { BsDatepickerViewMode, BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { Pagination } from '@utilities/pagination-utility';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { LoanedMonthlyAttendanceDataMaintenanceDto, LoanedMonthlyAttendanceDataMaintenanceParam, LoanedMonthlyAttendanceDataMaintenanceSource } from '@models/attendance-maintenance/5_1_27_loaned-monthly-attendance-data-maintenance';
import { S_5_1_27_LoanedMonthlyAttendanceDataMaintenanceService } from '@services/attendance-maintenance/s_5_1_27_loaned-monthly-attendance-data-maintenance.service';
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
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  };
  minMode: BsDatepickerViewMode = 'month';
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM',
    minMode: this.minMode
  };
  month_From: Date;
  month_To: Date;
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  param: LoanedMonthlyAttendanceDataMaintenanceParam = <LoanedMonthlyAttendanceDataMaintenanceParam>{};
  data: LoanedMonthlyAttendanceDataMaintenanceDto[] = [];
  sourceItem: LoanedMonthlyAttendanceDataMaintenanceDto
  iconButton = IconButton;
  isEdit: boolean = false;
  isQuery: boolean = false;

  constructor(
    private service: S_5_1_27_LoanedMonthlyAttendanceDataMaintenanceService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getListDepartment();
      this.getData();
    });
    effect(() => {
      const { param, att_Month_From, att_Month_To, isEdit, isQuery, pagination, source, data } = this.service.paramSearch();
      this.param = param;
      this.month_From = att_Month_From;
      this.month_To = att_Month_To;
      this.isEdit = isEdit;
      this.isQuery = isQuery;
      this.pagination = pagination;
      this.sourceItem = source;
      this.data = data;
      if (!this.functionUtility.checkEmpty(this.param.factory && this.month_From && this.month_To)) {
        this.getListFactory();
      }
      if (!this.functionUtility.checkEmpty(this.param.factory))
        this.getListDepartment();
      if (this.data.length > 0) {
        if (this.functionUtility.checkFunction('Search')) {
          if (this.checkRequiredParams())
            this.getData()
        }
        else
          this.clear()
      }
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListFactory();
  }

  ngOnDestroy(): void {
    this.service.setParamSearch(<LoanedMonthlyAttendanceDataMaintenanceSource>{
      param: this.param,
      att_Month_From: this.month_From,
      att_Month_To: this.month_To,
      isEdit: this.isEdit,
      isQuery: this.isQuery,
      pagination: this.pagination,
      source: this.sourceItem,
      data: this.data
    });
  }

  checkRequiredParams(): boolean {
    var result = !this.functionUtility.checkEmpty(this.param.factory) &&
      !this.functionUtility.checkEmpty(this.month_From) &&
      !this.functionUtility.checkEmpty(this.month_To)
    return result;
  }

  //#region getList
  getListFactory() {
    this.getList(
      () => this.service.getListFactory(),
      this.listFactory
    );
  }

  getListDepartment() {
    this.getList(
      () => this.service.getListDepartment(this.param.factory),
      this.listDepartment
    );
  }

  getList(
    serviceMethod: () => Observable<KeyValuePair[]>,
    resultList: KeyValuePair[]
  ) {
    serviceMethod().subscribe({
      next: (res) => {
        resultList.length = 0;
        resultList.push(...res);
      },
    });
  }

  onFactoryChange() {
    this.deleteProperty('department');
    this.getListDepartment();
  }
  //#endregion

  //#region getData
  getData(isSearch?: boolean, isDelete?: boolean) {
    this.spinnerService.show();
    this.param.att_Month_From = this.month_From ? new Date(this.month_From).toFirstDateOfMonth().toStringDate() : '';
    this.param.att_Month_To = this.month_To ? new Date(this.month_To).toFirstDateOfMonth().toStringDate() : '';
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
        if (isDelete)
          this.functionUtility.snotifySuccessError(true, 'System.Message.DeleteOKMsg')
      }
    });
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }
  //#endregion

  //#region download
  download() {
    this.spinnerService.show();
    Object.assign(this.param, {
      att_Month_From: this.month_From ? new Date(this.month_From).toFirstDateOfMonth().toStringDate() : '',
      att_Month_To: this.month_To ? new Date(this.month_To).toFirstDateOfMonth().toStringDate() : '',
    });

    this.service.downloadExcel(this.param).subscribe({
      next: async (res) => {
        if (res.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data, fileName);
        } else
          this.functionUtility.snotifySuccessError(false, 'System.Message.NoData');
        this.spinnerService.hide()
      }
    });
  }
  //#endregion

  //#region clear
  clear() {
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.param = <LoanedMonthlyAttendanceDataMaintenanceParam>{};
    this.month_From = null;
    this.month_To = null;
    this.data = [];
  }
  //#endregion

  //#region add-edit
  add() {
    this.isEdit = false;
    this.isQuery = false;
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  query(item: LoanedMonthlyAttendanceDataMaintenanceDto) {
    this.sourceItem = { ...item }
    this.isQuery = true;
    this.isEdit = false;
    this.router.navigate([`${this.router.routerState.snapshot.url}/query`]);
  }

  edit(item: LoanedMonthlyAttendanceDataMaintenanceDto) {
    this.sourceItem = { ...item }
    this.isEdit = true;
    this.isQuery = false;
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }
  //#endregion

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  deleteProperty(name: string) {
    delete this.param[name];
  }

  validateNumber(event: any): boolean {
    const numberRegex = /^[0-9]+$/;
    return numberRegex.test(event.key);
  }

  validateDate(dateString: Date) {
    const date = new Date(dateString);
    if (isNaN(date.getTime())) {
      if (dateString === this.month_From) {
        this.month_From = null;
      } else if (dateString === this.month_To) {
        this.month_To = null;
      }
    }
  }

  validateDecimal(event: any): boolean {
    const inputChar = event.key;
    const allowedKeys = ['Backspace', 'ArrowLeft', 'ArrowRight', 'Tab'];
    if (allowedKeys.indexOf(inputChar) !== -1)
      return true;

    const currentValue = event.target.value;
    const newValue = currentValue.substring(0, event.target.selectionStart) + inputChar + currentValue.substring(event.target.selectionEnd);
    const parts = newValue.split('.');
    const integerPartLength = parts[0].length;
    const decimalPartLength = parts.length > 1 ? parts[1].length : 0;

    if (integerPartLength > 5 || decimalPartLength > 5) {
      event.preventDefault();
      return false;
    }

    const decimalRegex = /^[0-9]*(\.[0-9]{0,5})?$/;
    if (!decimalRegex.test(newValue)) {
      event.preventDefault();
      return false;
    }

    return true;
  }
}
