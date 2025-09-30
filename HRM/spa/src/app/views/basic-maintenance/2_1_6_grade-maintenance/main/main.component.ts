import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { GradeMaintenanceParam, HRMS_Basic_Level, ParamInMain } from '@models/basic-maintenance/2_1_6_grade-maintenance';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_2_1_6_GradeMaintenanceService } from '@services/basic-maintenance/s_2_1_6_grade-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  ListLevelCode: KeyValuePair[] = [];
  types: KeyValuePair[] = [];

  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  }
  param: GradeMaintenanceParam = <GradeMaintenanceParam>{}
  data: HRMS_Basic_Level[] = [];

  constructor(
    private _service: S_2_1_6_GradeMaintenanceService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListLevelCode();
      this.getTypes();
      if (this.data.length > 0) {
        if (this.functionUtility.checkFunction('Search'))
          this.getData();
      }
    });
    effect(() => {
      this.param = this._service.getParamInMain()?.param;
      this.pagination = this._service.getParamInMain()?.pagination;
      this.data = this._service.getParamInMain()?.data;
      if (this.data.length > 0) {
        if (this.functionUtility.checkFunction('Search'))
          this.getData();
        else
          this.clear();
      }
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListLevelCode();
    this.getTypes();
  }

  ngOnDestroy(): void {
    this._service.getParamInMain.set(<ParamInMain>{ data: this.data, param: this.param, pagination: this.pagination });
  }

  getTypes() {
    this._service.getTypes().subscribe({
      next: res => {
        this.types = res;
      }
    })
  }

  getData(isSearch?: boolean) {
    this.param.level = this.functionUtility.checkEmpty(this.param.level) ? "" : this.param.level;
    this.spinnerService.show();
    this._service.getData(this.pagination, this.param).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch) this.functionUtility.snotifySuccessError(true, 'BasicMaintenance.2_6_GradeMaintenance.QueryOKMsg')

      }
    })
  }
  getListLevelCode() {
    this._service.getListLevelCode('main').subscribe({
      next: res => {
        this.ListLevelCode = res
      }
    })
  }
  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData();
  }

  search = (isSearch: boolean) => this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;

  clear() {
    this.param = <GradeMaintenanceParam>{};
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.data = [];
  }

  onForm(item: HRMS_Basic_Level = null) {
    let _param = item != null ? { ...item } : <HRMS_Basic_Level>{
      level: null,
      level_Code: "",
      type_Code: ""
    };
    this._service.data.set(_param);
    let _paramMain = <ParamInMain>{
      data: this.data,
      param: this.param,
      pagination: this.pagination
    }
    this._service.getParamInMain.set(_paramMain);
    this.router.navigate([`${this.router.routerState.snapshot.url}/${item != null ? 'edit' : 'add'}`]);
  }

  delete(item: HRMS_Basic_Level) {
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Caption.Confirm'), async () => {
      this.spinnerService.show();
      this._service.delete(item).subscribe({
        next: res => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(res.isSuccess, `System.Message.${res.isSuccess ? 'DeleteOKMsg' : 'DeleteErrorMsg'}`)
          if (res.isSuccess) this.getData();
        }
      });
    });
  }

  excel() {
    this.spinnerService.show();
    this._service.exportExcel(this.param).subscribe({
      next: res => {
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        this.functionUtility.exportExcel(res.data, fileName);
        this.spinnerService.hide();
      }
    });
  }

  changeState(item: HRMS_Basic_Level) {
    item.isActive = !item.isActive;
    this.spinnerService.show();
    this._service.edit(item).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(res.isSuccess, `System.Message.${res.isSuccess ? 'UpdateOKMsg' : 'UpdateErrorMsg'}`)
        if (res.isSuccess) this.getData();
      }
    })
  }

  deleteProperty = (name: string) => delete this.param[name]
}
