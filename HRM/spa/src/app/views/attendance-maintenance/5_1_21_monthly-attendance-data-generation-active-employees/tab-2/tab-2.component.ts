import { Component, effect, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  SearchAlreadyDeadlineDataMain,
  SearchAlreadyDeadlineDataParam,
  SearchAlreadyDeadlineDataSource,
} from '@models/attendance-maintenance/5_1_21_monthly-attendance-data-generation-active-employees';
import { S_5_1_21_MonthlyAttendanceDataGenerationActiveEmployeesService } from '@services/attendance-maintenance/s_5_1_21_monthly-attendance-data-generation-active-employees.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'already-deadline-data-active',
  templateUrl: './tab-2.component.html',
  styleUrl: './tab-2.component.scss',
})
export class Tab2Component extends InjectBase implements OnInit {
  @ViewChild('searchAlreadyDeadlineDataForm')
  public searchAlreadyDeadlineDataForm: NgForm;
  iconButton = IconButton;
  classButton = ClassButton;
  param: SearchAlreadyDeadlineDataParam = <SearchAlreadyDeadlineDataParam>{};

  att_Month_Start: Date;
  att_Month_End: Date;

  data: SearchAlreadyDeadlineDataMain[] = [];
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
  };

  bsConfigMonthly: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };

  listFactory: KeyValuePair[] = [];

  constructor(
    private _service: S_5_1_21_MonthlyAttendanceDataGenerationActiveEmployeesService
  ) {
    super();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((res) => {
        this.getListFactoryAdd();
        if (!this.searchAlreadyDeadlineDataForm.invalid && this.functionUtility.checkFunction('Search'))
          this.query(false);
      });

    effect(() => {
      this.param = this._service.programSource2().param;
      this.pagination = this._service.programSource2().pagination;
      this.data = this._service.programSource2().data;
      if(this.param.att_Month_Start)
        this.att_Month_Start = new Date(this.param.att_Month_Start)

      if(this.param.att_Month_End)
        this.att_Month_End = new Date(this.param.att_Month_End)

      if (this.data.length > 0) {
        if (this.param.factory != null
          && this.att_Month_Start != null
          && this.att_Month_End != null
          && this.functionUtility.checkFunction('Search'))
          this.query(false)
        else
          this.clear()
      }
    });
  }

  ngOnInit(): void {
    this.getListFactoryAdd();
  }

  ngOnDestroy(){
    this._service.setSource2(<SearchAlreadyDeadlineDataSource>{
      param: this.param,
      data: this.data,
      pagination: this.pagination
    })
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
    this.spinnerService.show();
    this._service
      .searchAlreadyDeadlineData(this.pagination, this.param)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.data = res.result;
          this.pagination = res.pagination;
          if (isSearch)
            this.snotifyService.success(
              this.translateService.instant('System.Message.SearchOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
        },
      });
  }

  clear() {
    this.att_Month_Start = null;
    this.att_Month_End = null;
    this.data = [];
    this.param = <SearchAlreadyDeadlineDataParam>{};
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
      totalPage: 0,
    };
  }

  onChangeDate(name: string){
    this.param[name] = this[name] != null ? this[name].toStringDate('yyyy-MM-dd') : '';
  }

  //#region Get List
  getListFactoryAdd() {
    this._service.getListFactoryAdd().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }
  //#endregion
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
