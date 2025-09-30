import { Component, OnDestroy, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants, SessionStorageConstants } from '@constants/local-storage.constants';
import { HRMS_Att_Work_Shift, HRMS_Att_Work_ShiftParam, HRMS_Att_Work_ShiftSource } from '@models/attendance-maintenance/5_1_2_shift-schedule-setting';
import { FunctionInfomation } from '@models/common';
import { S_5_1_2_ShiftScheduleSettingService } from '@services/attendance-maintenance/s_5_1_2_shift-schedule-setting.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  currentUser = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));

  //#region Data
  workShifts: HRMS_Att_Work_Shift[] = [];

  divisions: KeyValuePair[] = [];
  factories: KeyValuePair[] = [];
  workShiftTypes: KeyValuePair[] = [];

  //#endregion

  //#region Vaiables
  param: HRMS_Att_Work_ShiftParam = <HRMS_Att_Work_ShiftParam>{}

  selectedData: HRMS_Att_Work_Shift;

  title: string = '';
  colSpanOperation: number = 3;
  iconButton = IconButton;
  //#endregion

  //#region Pagination
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  }
  //#endregion

  constructor(
    private shiftScheduleSettingServices: S_5_1_2_ShiftScheduleSettingService,
  ) {
    super();

    // Load danh sách Data trước đó
    effect(() => {
      // 0. Gán params & pagination
      this.param = this.shiftScheduleSettingServices.workShiftSource().param;
      this.pagination = this.shiftScheduleSettingServices.workShiftSource().pagination;
      this.workShifts = this.shiftScheduleSettingServices.workShiftSource().data
      if (!this.functionUtility.checkEmpty(this.param.division))
        this.getFactoriesByDivision(this.param.division);
      if (this.workShifts.length > 0) {
        this.functionUtility.checkFunction('Search') && this.checkRequiredParams()
          ? this.getPaginationData()
          : this.clear()
      }
    });

    // Load lại dữ liệu khi thay đổi ngôn ngữ
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDivisions();
      this.getFactoriesByDivision(this.param.division);
      this.getWorkShiftTypes()
      if (this.workShifts.length > 0)
        this.getPaginationData();
    });
  }
  checkRequiredParams(): boolean {
    var result = !this.functionUtility.checkEmpty(this.param.division) && !this.functionUtility.checkEmpty(this.param.factory)
    return result;
  }
  ngOnInit(): void {
    // load dữ liệu [Divisions]
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (res) => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.divisions = res.resolverDivisions
        this.workShiftTypes = res.resolverWorkShiftTypes
      });

    if (!this.functionUtility.checkEmpty(this.param.division))
      this.getFactoriesByDivision(this.param.division);

    this.setColSpanOperation();
  }

  ngOnDestroy(): void {
    this.shiftScheduleSettingServices.setSource(<HRMS_Att_Work_ShiftSource>{
      pagination: this.pagination, param: this.param, data: this.workShifts, model: this.selectedData
    });
  }

  //#region Methods
  getDivisions() {
    this.shiftScheduleSettingServices.getDivisions().subscribe({
      next: result => {
        this.divisions = result
      }
    })
  }

  getWorkShiftTypes() {
    this.shiftScheduleSettingServices.getWorkShiftTypes().subscribe({
      next: result => this.workShiftTypes = result
    })
  }

  getFactoriesByDivision(division: string) {
    if (!this.functionUtility.checkEmpty(division)) {
      this.shiftScheduleSettingServices.getFactoriesByDivision(division).subscribe({
        next: result => {
          this.factories = result
        }
      })
    }
    else this.factories = []
  }

  getPaginationData(isSearch?: boolean) {
    if (!this.functionUtility.checkEmpty(this.param.division) && !this.functionUtility.checkEmpty(this.param.factory)) {
      this.spinnerService.show();
      this.shiftScheduleSettingServices.getDataPagination(this.pagination, this.param).subscribe({
        next: result => {
          this.spinnerService.hide();
          this.workShifts = result.result;
          this.workShifts.map(x => {
            x.update_Time = new Date(x.update_Time);
            x.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date(x.update_Time))
          })
          this.pagination = result.pagination;
          if (isSearch)
            this.snotifyService.success(this.translateService.instant('System.Message.QuerySuccess'), this.translateService.instant('System.Caption.Success'));
        }
      })
    }
    else this.workShifts = []
  }

  search = (isSearch: boolean) => this.pagination.pageNumber === 1 ? this.getPaginationData(isSearch) : this.pagination.pageNumber = 1;

  clear() {
    this.param = <HRMS_Att_Work_ShiftParam>{}
    this.pagination.pageNumber = 1;
    this.pagination.totalCount = 0;
    this.workShifts = [];
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getPaginationData();
  }

  setColSpanOperation() {
    let functionCodes = ['Edit'];
    const functions: FunctionInfomation[] = JSON.parse(sessionStorage.getItem(SessionStorageConstants.SELECTED_FUNCTIONS)!);
    this.colSpanOperation = functions?.filter(val => functionCodes.includes(val.function_Code)).length;
  }

  //#endregion

  //#region Events
  onAdd() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  onEdit(item: HRMS_Att_Work_Shift) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  onDivisionChange() {
    this.factories = [];
    this.deleteProperty('factory')
    if (!this.functionUtility.checkEmpty(this.param.division))
      this.getFactoriesByDivision(this.param.division);
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
  //#endregion

}
