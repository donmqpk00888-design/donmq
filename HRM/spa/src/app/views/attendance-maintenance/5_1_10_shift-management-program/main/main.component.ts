import { S_5_1_10_ShiftManagementProgram } from '@services/attendance-maintenance/s_5_1_10_shift-management-program.service';
import { InjectBase } from '@utilities/inject-base-app';
import { ClassButton, IconButton } from '@constants/common.constants';
import { Pagination } from '@utilities/pagination-utility';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild, effect } from '@angular/core';
import { NavigationExtras } from '@angular/router';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  ShiftManagementProgram_Main,
  ShiftManagementProgram_MainMemory,
  ShiftManagementProgram_Param
} from '@models/attendance-maintenance/5_1_10_shift-management-program';
import { CommonService } from '@services/common.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss'],
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  @ViewChild('inputRef') inputRef: ElementRef<HTMLInputElement>;
  title: string = '';
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{};

  acceptFormat: string = '.xls, .xlsx, .xlsm';

  iconButton = IconButton;
  classButton = ClassButton;

  pagination: Pagination = <Pagination>{};
  isCheckedAll: boolean = false;

  data: ShiftManagementProgram_Main[] = [];
  param: ShiftManagementProgram_Param = <ShiftManagementProgram_Param>{};
  selectedDatas: ShiftManagementProgram_Main[] = [];

  factoryList: KeyValuePair[] = [];
  divisionList: KeyValuePair[] = [];
  departmentList: KeyValuePair[] = [];
  workShiftTypeNewList: KeyValuePair[] = [];

  constructor(
    private service: S_5_1_10_ShiftManagementProgram,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.retryGetDropDownList()
      this.getListDepartment();
      if (this.data.length > 0)
        this.getData(false)
    });
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.pagination = this.service.paramSearch().pagination;
      this.data = this.service.paramSearch().data;
      if (!this.functionUtility.checkEmpty(this.param.division))
        this.retryGetDropDownList()
      this.data = this.service.paramSearch().data
      this.getListDepartment();
      if (this.data.length > 0) {
        if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams())
          this.getData(false)
        else
          this.clear()
      }
    });
  }
  ngOnInit(): void {
    this.bsConfig = Object.assign(
      {},
      {
        isAnimated: true,
        dateInputFormat: 'YYYY/MM/DD',
      }
    );
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.filterList(role.dataResolved)
      });
  }
  ngOnDestroy(): void {
    this.service.setParamSearch(<ShiftManagementProgram_MainMemory>{
      param: this.param,
      pagination: this.pagination,
      data: this.data
    });
  }

  retryGetDropDownList() {
    this.spinnerService.show()
    this.service.getDropDownList(this.param.division)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide()
          this.filterList(res)
        }
      });
  }
  filterList(keys: KeyValuePair[]) {
    this.factoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "FA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.divisionList = structuredClone(keys.filter((x: { key: string; }) => x.key == "DI")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.workShiftTypeNewList = structuredClone(keys.filter((x: { key: string; }) => x.key == "WO")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
  }
  getData = (isSearch: boolean) => {
    this.spinnerService.show();
    this.param.lang = localStorage.getItem(LocalStorageConstants.LANG)
    this.service
      .getSearchDetail(this.pagination, this.param)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.pagination = res.pagination;
          this.data = res.result;
          this.data.map((val: ShiftManagementProgram_Main) => {
            val.update_Time = new Date(val.update_Time)
            val.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date(val.update_Time))
            val.effective_Date_Str = val.effective_Date
          })
          if (isSearch)
            this.snotifyService.success(
              this.translateService.instant('System.Message.SearchOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
        }
      });
  };
  add() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }
  edit(byTab: string, selectedData: ShiftManagementProgram_Main) {
    this.spinnerService.show()
    this.service.isExistedData(selectedData)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            const value: NavigationExtras = { fragment: byTab };
            this.service.setParamForm(selectedData);
            this.router.navigate([`${this.router.routerState.snapshot.url}/edit`], { state: { value } });
          }
          else {
            this.getData(false)
            this.snotifyService.error(
              this.translateService.instant('AttendanceMaintenance.ShiftManagementProgram.NotExitedData'),
              this.translateService.instant('System.Caption.Error'));
          }
        }
      });
  }
  search = () => {
    this.pagination.pageNumber == 1
      ? (this.clearSelections(), this.getData(true))
      : (this.pagination.pageNumber = 1);
  };
  clear() {
    this.param = <ShiftManagementProgram_Param>{};
    this.data = []
    this.pagination.pageNumber = 1
    this.pagination.totalCount = 0
    this.clearSelections()
  }
  clearSelections() {
    this.selectedDatas = []
    this.isCheckedAll = false
  }
  checkAll() {
    this.isCheckedAll != this.isCheckedAll
    this.data.map(x => {
      if (!x.effective_State)
        x.isChecked = this.isCheckedAll
    })
    this.selectedDatas = this.data.filter(x => x.isChecked)
  }
  check() {
    this.selectedDatas = this.data.filter(x => x.isChecked)
    this.isCheckedAll = this.data.every(x => x.isChecked)
  }
  batchDelete() {
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Action.Delete'), () => {
      this.spinnerService.show();
      this.service.batchDelete(this.selectedDatas).subscribe({
        next: (res) => {
          if (res.isSuccess) {
            this.snotifyService.success(
              this.translateService.instant('System.Message.DeleteOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
            this.clearSelections()
            this.getData(false);
          }
          else {
            this.snotifyService.error(
              this.translateService.instant(`AttendanceMaintenance.ShiftManagementProgram.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
          this.spinnerService.hide();
        },
      });
    });
  }

  getListDepartment() {
    this.param.lang = localStorage.getItem(LocalStorageConstants.LANG)
    if (this.functionUtility.checkEmpty(this.param.factory)) {
      this.departmentList = []
      this.deleteProperty['department']
    }
    else
      this.commonService.getListDepartment(this.param.factory).subscribe({
        next: res => {
          this.departmentList = res;
        },
      });
  }
  checkRequiredParams(): boolean {
    let result = !this.functionUtility.checkEmpty(this.param.division) &&
      !this.functionUtility.checkEmpty(this.param.factory)
    return result
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
  onDivisionChange() {
    this.retryGetDropDownList()
    this.deleteProperty('factory')
    this.deleteProperty('work_Shift_Type_New')
    this.deleteProperty('effective_Date')
    this.getListDepartment()
  }
  changePage = (e: PageChangedEvent) => {
    this.pagination.pageNumber = e.page;
    this.clearSelections()
    this.getData(false);
  };
  onDateChange(name: string) {
    this.param[`${name}_Str`] = this.param[name] ? this.functionUtility.getDateFormat(new Date(this.param[name])) : '';
  }
}
