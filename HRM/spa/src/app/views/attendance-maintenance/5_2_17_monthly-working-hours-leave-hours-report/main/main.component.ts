import { S_5_2_17_MonthlyWorkingHoursLeaveHoursReportService } from '@services/attendance-maintenance/s_5_2_17_monthly-working-hours-leave-hours-report.service';
import { MonthlyWorkingHoursLeaveHoursReportParam, MonthlyWorkingHoursLeaveHoursReportSource } from '@models/attendance-maintenance/5_2_17_monthlyworkinghours-leavehoursreport';
import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { ValidateResult } from '@models/base-source';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  programCode: string = '';
  totalRows: number = 0;
  param: MonthlyWorkingHoursLeaveHoursReportParam = <MonthlyWorkingHoursLeaveHoursReportParam>{
    permissionGroup: []
  };
  yearMonth_Value: Date = null;

  iconButton = IconButton;
  classButton = ClassButton;
  listFactory: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{};

  constructor(private service: S_5_2_17_MonthlyWorkingHoursLeaveHoursReportService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList();
    });
    this.getDataFromSource()
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.loadDropDownList();
    this.bsConfig = <Partial<BsDatepickerConfig>>{
      isAnimated: true,
      dateInputFormat: 'YYYY/MM',
      adaptivePosition: true
    };

  }

  ngOnDestroy(): void {
    this.service.setSource(<MonthlyWorkingHoursLeaveHoursReportSource>{
      param: this.param,
      totalRows: this.totalRows,
      yearMonth_Value: this.yearMonth_Value
    })
  }

  loadDropDownList() {
    this.getListFactory();
    this.getListPermissionGroup();
  }

  getDataFromSource() {
    effect(() => {
      this.param = this.service.programSource().param;
      this.yearMonth_Value = this.service.programSource().yearMonth_Value;
      this.totalRows = this.service.programSource().totalRows;
      this.loadDropDownList()
    })
  }

  //#region GetDataRows
  getTotalRows(isSearch: boolean = true) {
    let checkValidate = this.validate();
    if (checkValidate.isSuccess) {
      if (this.disableSearch()) return;
      this.param.yearMonth = this.yearMonth_Value.toStringYearMonth();
      this.spinnerService.show();
      this.service.getTotalRows(this.param).subscribe({
        next: res => {
          this.spinnerService.hide()
          this.totalRows = res
          if (isSearch)
            this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess');
        }
      });
    }
    else this.snotifyService.warning(checkValidate.message, this.translateService.instant('System.Caption.Warning'));
  }

  //#region Excel
  excel() {
    let checkValidate = this.validate();
    if (checkValidate.isSuccess) {
      if (this.disableSearch()) return;
      this.param.yearMonth = this.yearMonth_Value.toStringYearMonth();
      this.spinnerService.show();
      this.service.exportExcel(this.param).subscribe({
        next: (result) => {
          this.spinnerService.hide()
          if (result.isSuccess) {
            const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
            this.functionUtility.exportExcel(result.data, fileName);
          }
          else {
            this.totalRows = 0
            this.snotifyService.error(result.error, this.translateService.instant('System.Caption.Warning'));
          }
        }
      });
    }
    else this.snotifyService.warning(checkValidate.message, this.translateService.instant('System.Caption.Warning'));
  }

  onFactoryClear() {
    this.param.permissionGroup = [];
  }

  onFactoryChange() {
    this.onFactoryClear();
    this.listPermissionGroup = [];
    this.getListPermissionGroup();
  }

  //#region  GetListFactory
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: res => {
        this.listFactory = res
      }
    })
  }

  //#region  GetListPermission
  getListPermissionGroup() {
    this.service.getListPermissionGroup(this.param.factory).subscribe({
      next: res => {
        this.listPermissionGroup = res
        this.selectAllForDropdownItems(this.listPermissionGroup)
      }
    })
  }

  //#region Select All For Dropdown Items
  private selectAllForDropdownItems(items: KeyValuePair[]) {
    let allSelect = (items: KeyValuePair[]) => {
      items.forEach(element => {
        element['allGroup'] = 'allGroup';
      });
    };
    allSelect(items);
  }

  getDataRowChange() {
    if (this.param.yearMonth != null && this.param.factory != null && this.param.permissionGroup != null)
      this.getTotalRows(true);
  }

  disableSearch() {
    return (this.functionUtility.checkEmpty(this.param.factory) ||
      this.yearMonth_Value == null ||
      this.yearMonth_Value == undefined ||
      this.param.permissionGroup.length == 0
    )
  }

  validate(): ValidateResult {
    if (this.yearMonth_Value != null && (this.yearMonth_Value == undefined || this.yearMonth_Value.toString() == "Invalid Date"))
      return new ValidateResult(`Year Month invalid`);
    return { isSuccess: true };
  }

  onOpenCalendar(container) {
    container.monthSelectHandler = (event: any): void => {
      container._store.dispatch(container._actions.select(event.date));
    };
    container.setViewMode('month');
  }

  clear() {
    this.totalRows = 0
    this.deleteProperty('factory')
    this.param.permissionGroup = [];
    this.yearMonth_Value = null;
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }
}
