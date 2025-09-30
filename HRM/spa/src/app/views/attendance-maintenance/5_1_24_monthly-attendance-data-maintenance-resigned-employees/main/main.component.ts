import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import {
  S_5_1_24_MonthlyAttendanceDataMaintenanceResignedEmployeesService
} from '@services/attendance-maintenance/s_5_1_24_monthly-attendance-data-maintenance-resigned-employees.service';
import { ResignedEmployeeMain, ResignedEmployeeMemory, ResignedEmployeeParam } from '@models/attendance-maintenance/5_1_24_monthly-attendance-resigned-employees';
import { Pagination } from '@utilities/pagination-utility';
import { ClassButton, IconButton } from '@constants/common.constants';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { CaptionConstants } from '@constants/message.enum';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  params: ResignedEmployeeParam = <ResignedEmployeeParam>{};
  pagination: Pagination = <Pagination>{};
  datas: ResignedEmployeeMain[] = [];

  att_Month_Start: Date = null;
  att_Month_End: Date = null;

  iconButton = IconButton;
  classButton = ClassButton;

  factories: KeyValuePair[] = [];
  departments: KeyValuePair[] = [];

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{};

  constructor(private _service: S_5_1_24_MonthlyAttendanceDataMaintenanceResignedEmployeesService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    effect(() => {
      this.params = this._service.paramSearch().params;
      this.pagination = this._service.paramSearch().pagination;
      this.datas = this._service.paramSearch().datas;

      this.getListFactory();
      this.getListDepartment();
      if (this.params.factory && this.att_Month_Start && this.att_Month_End && this.functionUtility.checkFunction('Search'))
        this.getDataPagination(false);
    });

    this._service.paramSearch$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(memory => {
      if (memory) {
        this.params = memory.params;
        this.pagination = memory.pagination;
        this.datas = memory.datas;
        this.getListDepartment();
        this.setQueryDate();
      }
    })

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getListDepartment();
      if (this.params.factory && this.att_Month_Start && this.att_Month_End && this.functionUtility.checkFunction('Search'))
        this.getDataPagination(false);
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.bsConfig = <Partial<BsDatepickerConfig>>{
      isAnimated: true,
      dateInputFormat: 'YYYY/MM',
      adaptivePosition: true
    };
  }
  ngOnDestroy(): void {
    let data: ResignedEmployeeMemory = <ResignedEmployeeMemory>{
      params: this.params,
      pagination: this.pagination,
      datas: this.datas
    }
    this._service.setParamSearch(data);
  }
  getListFactory() {
    this._service.getListFactoryByUser()
      .subscribe({
        next: (res) => {
          this.factories = res;
        }
      });
  }

  changeFactory() {
    this.deleteProperty('department')
    this.departments = [];
    if (this.params.factory)
      this.getListDepartment();
  }

  getListDepartment() {
    this._service.getListDepartment(this.params.factory)
      .subscribe({
        next: (res) => {
          this.departments = res;
        }
      });
  }

  getDataPagination(isSearch: boolean) {
    if (this.att_Month_End && !this.att_Month_End || !this.att_Month_End && this.att_Month_End)
      return this.snotifyService.warning('Please enter Year-Month of attendance range', CaptionConstants.WARNING);
    this.params.att_Month_Start = this.att_Month_Start ? new Date(this.att_Month_Start).toFirstDateOfMonth().toStringDate() : '';
    this.params.att_Month_End = this.att_Month_End ? new Date(this.att_Month_End).toFirstDateOfMonth().toStringDate() : '';
    if (!this.params.salary_Days)
      delete (this.params.salary_Days);
    this.spinnerService.show();
    this._service.getDataPagination(this.pagination, this.params)
      .subscribe({
        next: (res) => {
          this.datas = res.result;
          this.pagination = res.pagination;
          if (isSearch)
            this.snotifyService.success(
              this.translateService.instant('System.Message.SearchOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );

          this.spinnerService.hide();
        }
      });
  }

  search = () => {
    this.pagination.pageNumber == 1 ? this.getDataPagination(true) : this.pagination.pageNumber = 1;
  };

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getDataPagination(false);
  }

  onOpenCalendar(container: any) {
    container.monthSelectHandler = (event: any): void => {
      container._store.dispatch(container._actions.select(event.date));
    };
    container.setViewMode('month');
  }

  add() {
    let param: ResignedEmployeeParam = <ResignedEmployeeParam>{
      factory: this.params.factory,
      employee_ID: this.params.employee_ID,
      department: this.params.department,
      att_Month_Start: this.params.att_Month_Start,
      action: 'add'
    }

    let state = {
      'param': param,
      'factories': this.factories,
      'bsConfig': this.bsConfig
    }
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`], { state: state });
  }

  edit(item: ResignedEmployeeMain) {
    let param: ResignedEmployeeParam = <ResignedEmployeeParam>{
      factory: item.factory,
      employee_ID: item.employee_ID,
      department: item.department,
      departmentName: item.department_Name,
      att_Month_Start: item.att_Month,
      isProbation: item.isProbation,
      action: 'edit'
    }
    let state = {
      'param': param,
      'factories': this.factories,
      'bsConfig': this.bsConfig
    }
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`], { state: state });
  }

  query(item: ResignedEmployeeMain) {
    let param: ResignedEmployeeParam = <ResignedEmployeeParam>{
      factory: item.factory,
      employee_ID: item.employee_ID,
      department: item.department,
      att_Month_Start: item.att_Month,
      isProbation: item.isProbation,
      action: 'query'
    }
    let state = {
      'param': param,
      'factories': this.factories,
      'bsConfig': this.bsConfig
    }
    this.router.navigate([`${this.router.routerState.snapshot.url}/query`], { state: state });
  }

  download() {
    if (this.att_Month_End && !this.att_Month_End || !this.att_Month_End && this.att_Month_End)
      return this.snotifyService.warning('Please enter Year-Month of attendance range', CaptionConstants.WARNING);

    this.params.att_Month_Start = this.att_Month_Start ? new Date(this.att_Month_Start).toFirstDateOfMonth().toStringDate() : '';
    this.params.att_Month_End = this.att_Month_End ? new Date(this.att_Month_End).toFirstDateOfMonth().toStringDate() : '';

    this.spinnerService.show();
    this._service.exportExcel(this.params).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        result.isSuccess
          ? this.functionUtility.exportExcel(result.data, fileName)
          : this.functionUtility.snotifySuccessError(result.isSuccess, result.error);
      },
    });
  }

  setQueryDate() {
    if (this.params.att_Month_Start)
      this.att_Month_Start = new Date(this.params.att_Month_Start);
    if (this.params.att_Month_End)
      this.att_Month_End = new Date(this.params.att_Month_End);
  }

  clear() {
    this.params = <ResignedEmployeeParam>{}
    this.att_Month_Start = null;
    this.att_Month_End = null;
    this.departments = [];
    this.datas = [];
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
      totalPage: 0
    };
  }
  deleteProperty(name: string) {
    delete this.params[name]
  }
}
