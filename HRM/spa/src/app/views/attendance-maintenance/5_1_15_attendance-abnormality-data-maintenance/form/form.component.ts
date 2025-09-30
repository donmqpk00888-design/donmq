import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { HRMS_Att_Temp_RecordDto } from '@models/attendance-maintenance/5_1_15_attendance-abnormality-data-maintenance';
import { UserForLogged } from '@models/auth/auth';
import { S_5_1_15_AttendanceAbnormalityDataMaintenanceService } from '@services/attendance-maintenance/s_5_1_15_attendance-abnormality-data-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import {
  BsDatepickerConfig,
  BsDatepickerViewMode,
} from 'ngx-bootstrap/datepicker';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { Observable } from 'rxjs';
import { KeyValuePair } from '@utilities/key-value-pair';
import { EmployeeCommonInfo } from '@models/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss',
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  defaultData: HRMS_Att_Temp_RecordDto = <HRMS_Att_Temp_RecordDto>{
    reason_Code: "ZZ",
    clock_In: '0000',
    modified_Clock_In: '0000',
    clock_Out: '0000',
    modified_Clock_Out: '0000',
    overtime_ClockIn: '0000',
    modified_Overtime_ClockIn: '0000',
    overtime_ClockOut: '0000',
    modified_Overtime_ClockOut: '0000',
    days: '0',
    holiday: "XXX",
  };
  iconButton = IconButton;
  classButton = ClassButton;
  minMode: BsDatepickerViewMode = 'day';
  today: Date = new Date();
  minDate: Date = new Date(
    this.today.getFullYear(),
    this.today.getMonth(),
    this.today.getDate() - 30
  );
  maxDate: Date = new Date(
    this.today.getFullYear(),
    this.today.getMonth(),
    this.today.getDate() + 30
  );
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM/DD',
    minMode: this.minMode,
    minDate: this.minDate,
    maxDate: this.maxDate,
  };
  listDepartment: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listWorkShiftType: KeyValuePair[] = [];
  listAttendance: KeyValuePair[] = [];
  listUpdateReason: KeyValuePair[] = [];
  listHoliday: KeyValuePair[] = [];
  data: HRMS_Att_Temp_RecordDto = <HRMS_Att_Temp_RecordDto>{};
  employeeList: EmployeeCommonInfo[] = [];
  constructor(
    private service: S_5_1_15_AttendanceAbnormalityDataMaintenanceService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.getDropdownList()
      });
  }

  ngOnInit(): void {
    this.data = structuredClone(this.defaultData)
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.getDropdownList()
  }

  getDropdownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListWorkShiftType();
    this.getListAttendance();
    this.getListUpdateReason();
    this.getListHoliday();
  }
  //#region getList
  getListFactory() {
    this.getList(
      () => this.service.getListFactoryByUser(),
      this.listFactory
    );
  }

  getListWorkShiftType() {
    this.getList(
      () => this.service.getListWorkShiftType(),
      this.listWorkShiftType
    );
  }

  getListAttendance() {
    this.getList(
      () => this.service.getListAttendance(),
      this.listAttendance
    );
  }

  getListUpdateReason() {
    this.getList(
      () => this.service.getListUpdateReason(),
      this.listUpdateReason
    );
  }

  getListHoliday() {
    this.getList(
      () => this.service.getListHoliday(),
      this.listHoliday
    );
  }

  private getList(
    serviceMethod: () => Observable<KeyValuePair[]>,
    resultList: KeyValuePair[]
  ) {
    serviceMethod().subscribe({
      next: (res) => {
        resultList.length = 0;
        resultList.push(...res);
      }
    });
  }
  //#endregion

  //#region onChange
  onFactoryChange() {
    this.getListEmployee();
    this.deleteProperty('employee_ID')
    this.clearEmpInfo()
  }

  //#region typeaead
  getListEmployee() {
    if (this.data.factory) {
      this.commonService.getListEmployeeAdd(this.data.factory).subscribe({
        next: res => {
          this.employeeList = res
          this.setEmployeeInfo();
        }
      })
    }
  }
  private setEmployeeInfo() {
    if (!this.data.factory || !this.data.employee_ID)
      return this.clearEmpInfo()
    const emp = this.employeeList.find(x => x.factory == this.data.factory && x.employee_ID == this.data.employee_ID)
    if (emp) {
      this.data.useR_GUID = emp.useR_GUID;
      this.data.local_Full_Name = emp.local_Full_Name;
      this.data.department_Code = emp.actual_Department_Code;
      this.data.department_Code_Name = emp.actual_Department_Code_Name;
    }
    else {
      this.clearEmpInfo()
      this.functionUtility.snotifySuccessError(false, "Employee ID not exists")
    }
  }

  getListDepartment() {
    if (this.data.factory)
      this.getList(
        () => this.service.getListDepartment(this.data.factory),
        this.listDepartment
      );
  }
  clearEmpInfo() {
    this.deleteProperty('useR_GUID')
    this.deleteProperty('local_Full_Name')
    this.deleteProperty('department_Code')
    this.deleteProperty('department_Code_Name')
  }
  onTypehead(isKeyPress: boolean = false) {
    if (isKeyPress)
      return this.clearEmpInfo()
    if (this.data.employee_ID.length > 9) {
      this.clearEmpInfo()
      this.snotifyService.error(
        this.translateService.instant(`System.Message.InvalidEmployeeIDLength`),
        this.translateService.instant('System.Caption.Error')
      );
    }
    else {
      this.setEmployeeInfo()
    }
  }

  //#region saveChange
  save(isNext: boolean) {
    this.spinnerService.show();
    this.service.addNew(this.data).subscribe({
      next: (result) => {
        if (result.isSuccess) {
          const message = 'System.Message.CreateOKMsg';
          this.handleSuccess(message);
          isNext ? this.data = structuredClone(this.defaultData) : this.back();
        } else {
          this.handleError(result.error);
        }
      }
    });
  }
  //#endregion

  back = () => this.router.navigate([this.url]);
  //#endregion

  //#region validate
  updateTime(displayValue: string, field: string): void {
    const timePattern = /^([01]\d|2[0-3]):[0-5]\d$/;
    if (timePattern.test(displayValue)) {
      this.data[field] = displayValue.replace(':', '');
    } else {
      this.handleError('Invalid Input');
      this.data[field] = this.defaultData[field];
    }
  }

  validateDecimal(value: string): void {
    const decimalPattern = /^\d{1,5}(\.\d{1,5})?$/;
    if (decimalPattern.test(value)) this.data.days = value;
    else {
      this.handleError('Invalid Input');
      this.data.days = this.defaultData.days;
    }
  }
  //#endregion

  handleSuccess(message: string) {
    this.spinnerService.hide();
    this.snotifyService.success(
      this.translateService.instant(message),
      this.translateService.instant('System.Caption.Success')
    );
  }

  handleError(message: string) {
    this.spinnerService.hide();
    this.snotifyService.error(
      this.translateService.instant(message),
      this.translateService.instant('System.Caption.Error')
    );
  }
  deleteProperty(name: string) {
    delete this.data[name]
  }
}
