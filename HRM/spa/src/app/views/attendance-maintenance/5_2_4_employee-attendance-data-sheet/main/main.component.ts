import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { S_5_2_4_EmployeeAttendanceDataSheetService } from '@services/attendance-maintenance/s_5_2_4_employee-attendance-data-sheet.service';
import {
  EmployeeAttendanceDataSheet_Basic,
  EmployeeAttendanceDataSheetDTO,
  EmployeeAttendanceDataSheetParam
} from '@models/attendance-maintenance/5_2_4_employee-attendance-data-sheet';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  param: EmployeeAttendanceDataSheetParam = <EmployeeAttendanceDataSheetParam>{};
  lastSearchParam: EmployeeAttendanceDataSheetParam;
  data: EmployeeAttendanceDataSheetDTO[] = [];
  countRecord: number = 0;
  att_Date_From: Date = null;
  att_Date_To: Date = null;

  iconButton = IconButton;
  classButton = ClassButton;
  listFactory: KeyValuePair[] = [];
  listWorkShiftType: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  source: EmployeeAttendanceDataSheet_Basic
  constructor(
    private service: S_5_2_4_EmployeeAttendanceDataSheetService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.getDataFromSource();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getListWorkShiftType();
      this.getListDepartment();
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListFactory();
    this.getListWorkShiftType();
    this.lastSearchParam = { ...this.param };
  }

  ngOnDestroy(): void {
    this.checkDate();
    if (!this.source)
      this.source = <EmployeeAttendanceDataSheet_Basic>{
        param: { ...this.param }
      };
    this.service.setSource(this.source);
  }

  checkDate() {
    if (this.att_Date_From != null)
      this.param.att_Date_From = this.functionUtility.getDateFormat(this.att_Date_From);
    else this.deleteProperty('att_Date_From');

    if (this.att_Date_To != null)
      this.param.att_Date_To = this.functionUtility.getDateFormat(this.att_Date_To);
    else this.deleteProperty('att_Date_To');
  }

  getDataFromSource() {
    effect(() => {
      this.param = this.service.paramSource().param;
      this.getListDepartment();

      if (this.param.att_Date_From != null)
        this.att_Date_From = this.param.att_Date_From.toDate();

      if (this.param.att_Date_To != null)
        this.att_Date_To = this.param.att_Date_To.toDate();

      if (this.checkRequiredParams() && this.functionUtility.checkFunction('Search'))
        this.getCountRecords(false);
    });
  }
  checkRequiredParams() {
    if (this.param.att_Date_From != null
      && this.param.att_Date_To != null
      && this.param.factory != null
    )
      return true
    else return false
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  getListWorkShiftType() {
    this.service.getListWorkShiftType().subscribe({
      next: (res) => {
        this.listWorkShiftType = res;
      },
    });
  }

  getListDepartment() {
    this.service.getListDepartment(this.param.factory).subscribe({
      next: (res) => {
        this.listDepartment = res;
      },
    });
  }

  getCountRecords(isSearch?: boolean) {
    this.spinnerService.show();
    this.paramCheck()
    this.service.getCountRecords(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.countRecord = res;
        if (isSearch) {
          this.lastSearchParam = { ...this.param };
          this.snotifyService.success(
            this.translateService.instant('System.Message.SearchOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
        }
      },
    });
  }
  paramChanged(): boolean {
    return JSON.stringify(this.param) !== JSON.stringify(this.lastSearchParam);
  }
  paramCheck() {
    this.param.att_Date_From = (!this.functionUtility.checkEmpty(this.att_Date_From)
      && (this.att_Date_From.toString() != 'Invalid Date' && this.att_Date_From.toString() != 'NaN/NaN'))
      ? this.functionUtility.getDateFormat(this.att_Date_From)
      : "";

    this.param.att_Date_To = (!this.functionUtility.checkEmpty(this.att_Date_To)
      && (this.att_Date_To.toString() != 'Invalid Date' && this.att_Date_To.toString() != 'NaN/NaN'))
      ? this.functionUtility.getDateFormat(this.att_Date_To)
      : "";

    this.param.language = localStorage.getItem(LocalStorageConstants.LANG);
  }

  validateSearch() {
    if (!this.functionUtility.checkEmpty(this.param.factory)
      && this.att_Date_From != null
      && this.att_Date_From != undefined
      && this.att_Date_From.toString() != 'Invalid Date'
      && this.att_Date_To != null
      && this.att_Date_To != undefined
      && this.att_Date_To.toString() != 'Invalid Date'
    ) return false;
    return true;
  }

  clear() {
    this.countRecord = 0
    this.att_Date_From = null
    this.att_Date_To = null
    this.deleteProperty('factory')
    this.deleteProperty('employee_ID')
    this.deleteProperty('department')
    this.deleteProperty('work_Shift_Type')
    this.deleteProperty('att_Date')
  }

  download() {
    this.spinnerService.show();
    this.paramCheck()
    this.service.download(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data.result, fileName);
          this.countRecord = res.data.count
        } else this.functionUtility.snotifySuccessError(res.isSuccess, res.error)
      },
    });
  }

  onDepartmentChange() {
    this.deleteProperty('department')
    this.listDepartment = [];
    this.getListDepartment()
  }

  onDateChange(name: string) {
    this.paramCheck();
  }

  deleteProperty = (name: string) => delete this.param[name]

}
