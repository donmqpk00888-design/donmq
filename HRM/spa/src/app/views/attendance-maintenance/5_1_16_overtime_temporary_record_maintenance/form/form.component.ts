import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { HRMS_Att_Overtime_TempDto } from '@models/attendance-maintenance/5_1_16_overtime_temporary_record_maintenance';
import { EmployeeCommonInfo } from '@models/common';
import { S_5_1_16_OvertimeTemporaryRecordMaintenanceService } from '@services/attendance-maintenance/s_5_1_16_overtime_temporary_record_maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss'],
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  data: HRMS_Att_Overtime_TempDto;
  defaultData: HRMS_Att_Overtime_TempDto = <HRMS_Att_Overtime_TempDto>{
    overtime_Start: '0000',
    overtime_End: '0000',
    overtime_Hours: 0,
    night_Hours: 0,
    night_Overtime_Hours: 0,
    training_Hours: 0,
    night_Eat: 0,
    holiday: 'XXX',
  };
  iconButton = IconButton;
  classButton = ClassButton;
  validEmp: boolean = false;
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM/DD',
  };
  inputError: boolean = false;
  listFactory: KeyValuePair[] = [];
  employeeList: EmployeeCommonInfo[] = [];
  workShiftType: KeyValuePair[] = [];
  listHoliday: KeyValuePair[] = [];
  constructor(
    private service: S_5_1_16_OvertimeTemporaryRecordMaintenanceService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.getListFactory();
        this.getListEmployee()
        this.getListHoliday();
        this.getListWorkShiftType();
        this.setEmployeeInfo();
      });
  }

  ngOnInit() {
    this.data = structuredClone(this.defaultData)
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.getListFactory();
    this.getListEmployee();
    this.getListWorkShiftType();
    this.getListHoliday();
  }

  save(isNext: boolean) {
    this.checkEmptyDecimal();
    this.spinnerService.show();
    this.service.create(this.data).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        if (result.isSuccess) {
          this.snotifyService.success(
            this.translateService.instant('System.Message.CreateOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
          isNext ? this.data = structuredClone(this.defaultData) : this.back();
        } else {
          this.snotifyService.error(
            result.error,
            this.translateService.instant('System.Caption.Error')
          );
        }
      },
    });
  }

  onFactoryChange() {
    this.getListEmployee();
    this.getClockInOut();
    this.getShiftTimeByWorkShift();
    this.deleteProperty('employee_ID')
    this.clearEmpInfo()
  }
  onDateChange() {
    this.data.date_Str = this.functionUtility.isValidDate(new Date(this.data.date))
      ? this.functionUtility.getDateFormat(new Date(this.data.date)) : '';
    this.getClockInOut();
    this.getShiftTimeByWorkShift();
  }
  onWorkShiftTypeChange() {
    this.getShiftTimeByWorkShift()
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
      this.getClockInOut();
    }
  }
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
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => this.listFactory = res,
    });
  }
  getListWorkShiftType() {
    this.service.getListWorkShiftType().subscribe({
      next: (res) => this.workShiftType = res,
    });
  }
  getListHoliday() {
    this.service.getListHoliday().subscribe({
      next: (res) => this.listHoliday = res,
    });
  }

  setEmployeeInfo() {
    if (!this.data.factory || !this.data.employee_ID)
      return this.clearEmpInfo()
    const emp = this.employeeList.find(x => x.factory == this.data.factory && x.employee_ID == this.data.employee_ID)
    if (emp) {
      this.data.useR_GUID = emp.useR_GUID;
      this.data.local_Full_Name = emp.local_Full_Name;
      this.data.department_Code = emp.actual_Department_Code;
      this.data.department_Code_Name = emp.actual_Department_Code_Name;
      this.validEmp = true;
    }
    else {
      this.clearEmpInfo()
      this.functionUtility.snotifySuccessError(false, "Employee ID not exists")
    }
  }
  clearEmpInfo() {
    this.deleteProperty('useR_GUID')
    this.deleteProperty('local_Full_Name')
    this.deleteProperty('department_Code')
    this.deleteProperty('department_Code_Name')
    this.validEmp = true;
  }
  getClockInOut() {
    if (!this.data.factory || !this.data.employee_ID || !this.data.date_Str)
      return this.clearClock()
    this.service.getClockInOutByTempRecord(this.data).subscribe({
      next: (res) => {
        if (res != null) {
          this.data.clock_In_Time = res.clock_In_Time;
          this.data.clock_Out_Time = res.clock_Out_Time;
        } else this.clearClock()
      },
      error: () => { this.clearClock() },
    });

  }
  clearClock() {
    this.deleteProperty('clock_In_Time')
    this.deleteProperty('clock_Out_Time')
  }
  getShiftTimeByWorkShift(): boolean | void {
    if (!this.data.factory || !this.data.work_Shift_Type || !this.data.date_Str)
      return this.deleteProperty('shift_Time')
    this.service
      .getShiftTimeByWorkShift(this.data)
      .subscribe({
        next: (res: KeyValuePair) => {
          const time = res.value as string
          !time.isNullOrWhiteSpace() ? this.data.shift_Time = res.value : this.deleteProperty('shift_Time')
        },
        error: () => { this.deleteProperty('shift_Time') },
      });
  }

  back = () => this.router.navigate([this.url]);

  preventNegativeInput(event: KeyboardEvent): void {
    if (event.key === '-' || event.key === 'e' || event.key === 'E')
      event.preventDefault();
  }
  validateInput(event: Event, name: string) {
    const decimalPattern = /^\d{0,10}(\.\d{1,5})?$/;
    const inputElement = event.target as HTMLInputElement;
    if (!decimalPattern.test(inputElement.value)) {
      this.inputError = true;
      return this.snotifyService.error(
        name + " Invalid Input",
        this.translateService.instant('System.Caption.Error')
      );
    }
    return this.inputError = false;
  }
  updateTime(displayValue: string, field: string): void {
    const timePattern = /^([01]\d|2[0-3]):[0-5]\d$/;
    if (timePattern.test(displayValue)) {
      this.data[field] = displayValue.replace(':', '');
    } else {
      this.snotifyService.error(
        'Invalid Input',
        this.translateService.instant('System.Caption.Error')
      );
      this.data[field] = this.defaultData[field];
    }
  }
  checkEmptyDecimal() {
    this.functionUtility.checkEmpty(this.data.overtime_Hours) ? this.data.overtime_Hours = 0 : this.data.overtime_Hours
    this.functionUtility.checkEmpty(this.data.night_Hours) ? this.data.night_Hours = 0 : this.data.night_Hours
    this.functionUtility.checkEmpty(this.data.night_Overtime_Hours) ? this.data.night_Overtime_Hours = 0 : this.data.night_Overtime_Hours
    this.functionUtility.checkEmpty(this.data.training_Hours) ? this.data.training_Hours = 0 : this.data.training_Hours
    this.functionUtility.checkEmpty(this.data.night_Eat) ? this.data.night_Eat = 0 : this.data.night_Eat
  }

  deleteProperty = (name: string) => delete this.data[name]
}
