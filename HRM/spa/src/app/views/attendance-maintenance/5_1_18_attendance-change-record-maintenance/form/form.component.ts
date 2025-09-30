import { Component, OnInit } from '@angular/core';
import { IconButton, ClassButton } from '@constants/common.constants';
import { HRMS_Att_Change_RecordDto } from '@models/attendance-maintenance/5_1_18_attendance-change-record-maintenance';
import { EmployeeCommonInfo } from '@models/common';
import { S_5_1_18_AttendanceChangeRecordMaintenanceService } from '@services/attendance-maintenance/s_5_1_18_attendance-change-record-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  minDate: Date;
  maxDate: Date;
  listFactory: KeyValuePair[] = [];
  listWorkShiftType: KeyValuePair[] = [];
  listAttendance: KeyValuePair[] = [];
  listReasonCode: KeyValuePair[] = [];
  listHoliday: KeyValuePair[] = [];
  employee: EmployeeCommonInfo = <EmployeeCommonInfo>{};
  employeeList: EmployeeCommonInfo[] = [];
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{ dateInputFormat: 'YYYY/MM/DD' };
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
  };
  iconButton = IconButton;
  classButton = ClassButton;
  dataMain: HRMS_Att_Change_RecordDto = <HRMS_Att_Change_RecordDto>{};
  item: HRMS_Att_Change_RecordDto;
  att_Date: Date

  /**
   *
   */
  constructor(private service: S_5_1_18_AttendanceChangeRecordMaintenanceService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.loadDropdownList();
      });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.loadDropdownList();
    this.handleAttDate();
    this.setDefaultParam();
  }

  /**
   * Check Range：today-40 <= input date <= today+40
   */
  handleAttDate() {
    let today = new Date();
    this.minDate = new Date();
    this.minDate.setDate(today.getDate() - 40);
    this.maxDate = new Date();
    this.maxDate.setDate(today.getDate() + 40);
  }


  back = () => this.router.navigate([this.url]);

  setDefaultParam() {
    this.dataMain = <HRMS_Att_Change_RecordDto>{
      reason_Code: 'ZZ',
      clock_In: '0000',
      modified_Clock_In: '0000',
      clock_Out: '0000',
      modified_Clock_Out: '0000',
      overtime_ClockIn: '0000',
      modified_Overtime_ClockIn: '0000',
      overtime_ClockOut: '0000',
      modified_Overtime_ClockOut: '0000',
      days: 0,
      holiday: 'XXX'
    };
  }

  loadDropdownList() {
    this.getListFactory();
    this.getListEmployee();
    this.getListWorkShiftType();
    this.getListAttendance();
    this.getListReasonCode();
    this.getListHoliday();
  }

  getListFactory() {
    this.commonService.getListAccountAdd().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }
  getListEmployee() {
    if (this.dataMain.factory) {
      this.commonService.getListEmployeeAdd(this.dataMain.factory).subscribe({
        next: res => {
          this.employeeList = res
          this.setEmployeeInfo();
        }
      })
    }
  }
  getListWorkShiftType() {
    this.commonService.getListWorkShiftType().subscribe({
      next: res => {
        this.listWorkShiftType = res;
      }
    });
  }

  getListAttendance() {
    this.commonService.getListAttendanceOrLeave().subscribe({
      next: res => {
        this.listAttendance = res;
      }
    });
  }

  getListReasonCode() {
    this.commonService.getListReasonCode().subscribe({
      next: res => {
        this.listReasonCode = res;
      }
    });
  }

  getListHoliday() {
    this.service.getListHoliday('39', 1, 'Attendance').subscribe({
      next: res => {
        this.listHoliday = res;
      }
    });
  }

  private setEmployeeInfo() {
    if (!this.dataMain.factory || !this.dataMain.employee_ID)
      return this.clearEmpInfo()
    const emp = this.employeeList.find(x => x.factory == this.dataMain.factory && x.employee_ID == this.dataMain.employee_ID)
    if (emp) {
      this.dataMain.useR_GUID = emp.useR_GUID;
      this.dataMain.local_Full_Name = emp.local_Full_Name;
      this.dataMain.department_Code = emp.actual_Department_Code;
      this.dataMain.department_Name = emp.actual_Department_Name;
      this.dataMain.department_Code_Name = emp.actual_Department_Code_Name;
    }
    else {
      this.clearEmpInfo()
      this.functionUtility.snotifySuccessError(false, "Employee ID not exists")
    }
  }

  updateTime(displayValue: string, field: string): void {
    const timePattern = /^([01]\d|2[0-3]):[0-5]\d$/;
    if (timePattern.test(displayValue)) {
      this.dataMain[field] = displayValue.replace(':', '');
    } else {
      this.functionUtility.snotifySuccessError(false, 'InputTimeInValid');
      // Reset to default value
      this.dataMain[field] = '0000';
    }
  }

  validateTime(time: string): void {
    const timePattern = /^([01]\d|2[0-3])[0-5]\d$/;
    if (timePattern.test(time)) {
      this.dataMain.modified_Clock_In = time;
    } else {
      this.functionUtility.snotifySuccessError(false, 'AttendanceMaintenance.AttendanceChangeRecordMaintenance.InputDateInValid')
      this.dataMain.modified_Clock_In = '0000';
    }
  }

  onFactoryChange() {
    this.getListEmployee();
    this.deleteProperty('employee_ID')
    this.clearEmpInfo()
  }

  onTypehead(isKeyPress: boolean = false) {
    if (isKeyPress)
      return this.clearEmpInfo()
    if (this.dataMain.employee_ID.length > 9) {
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
  onDateChange() {
    this.dataMain.att_Date_Str = this.functionUtility.isValidDate(new Date(this.dataMain.att_Date))
      ? this.functionUtility.getDateFormat(new Date(this.dataMain.att_Date)) : '';
  }
  save(isNext: boolean) {
    this.spinnerService.show();
    if (this.functionUtility.checkEmpty(this.dataMain.days))
      this.dataMain.days = 0;
    this.service
      .addNew(this.dataMain)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            isNext ? this.setDefaultParam() : this.back();
            this.snotifyService.success(res.error,
              this.translateService.instant('System.Caption.Success')
            );
          } else {
            this.snotifyService.error(res.error,
              this.translateService.instant('System.Caption.Error'));
          }
        }
      })
  }
  clearEmpInfo() {
    this.deleteProperty('useR_GUID')
    this.deleteProperty('department_Code')
    this.deleteProperty('department_Name')
    this.deleteProperty('department_Code_Name')
    this.deleteProperty('local_Full_Name')
  }
  deleteProperty = (name: string) => delete this.dataMain[name]
}
