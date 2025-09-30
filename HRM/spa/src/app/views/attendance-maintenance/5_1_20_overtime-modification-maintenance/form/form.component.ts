import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { OvertimeModificationMaintenanceDto, OvertimeModificationMaintenanceParam } from '@models/attendance-maintenance/5_1_20_overtime-modification-maintenance';
import { EmployeeCommonInfo } from '@models/common';
import { S_5_1_20_OvertimeModificationMaintenanceService } from '@services/attendance-maintenance/s_5_1_20_overtime-modification-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
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
  iconButton = IconButton;
  classButton = ClassButton;

  factories: KeyValuePair[] = [];
  workShiftTypes: KeyValuePair[] = [];
  holidays: KeyValuePair[] = [];
  employeeList: EmployeeCommonInfo[] = [];
  existEmloyeeID: boolean = false;
  minDate: Date;
  maxDate: Date;
  model: OvertimeModificationMaintenanceDto = <OvertimeModificationMaintenanceDto>{
    overtime_Hours: '0',
    night_Hours: '0',
    night_Overtime_Hours: '0',
    training_Hours: '0',
    night_Eat_Times: '0',
    holiday: "XXX"
  };

  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: "YYYY/MM/DD"
  };

  constructor(private service: S_5_1_20_OvertimeModificationMaintenanceService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getListEmployee()
      this.getListWorkShiftType();
      this.getListHoliday();
      // Load lại thông tin nhân viên
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    /* Check Range：today-90 <= input date <= today+90*/
    this.minDate = new Date().addDays(-90);
    this.maxDate = new Date().addDays(90);
    this.getListFactory();
    this.getListWorkShiftType();
    this.getListHoliday();
  }

  back = () => this.router.navigate([this.url]);

  save(isContinues: boolean = false) {
    this.spinnerService.show();
    this.service.create(this.model).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(
          res.isSuccess,
          res.isSuccess ? 'System.Message.CreateOKMsg' : res.error ?? 'System.Message.CreateErrorMsg')
        if (res.isSuccess)
          isContinues ? this.clearModel() : this.back()
      }
    });
  }

  clearModel() {
    this.model = <OvertimeModificationMaintenanceDto>{
      overtime_Hours: '0',
      night_Hours: '0',
      night_Overtime_Hours: '0',
      training_Hours: '0',
      night_Eat_Times: '0',
      holiday: "XXX"
    };
    this.employeeList = []
  }

  onFactoryChange() {
    this.getListEmployee()
    this.getWorkShiftType();
    this.getWorkShiftTypeTime()
    this.deleteProperty('employee_ID')
    this.clearEmpInfo()
  }
  onChangeDate() {
    this.model.overtime_Date_Str = this.functionUtility.isValidDate(new Date(this.model.overtime_Date))
      ? this.functionUtility.getDateFormat(new Date(this.model.overtime_Date)) : '';
    this.getWorkShiftTypeTime()
    this.getWorkShiftType();
    this.getClockTime()
  }
  onChangeWorkShiftType() {
    this.getWorkShiftTypeTime()
  }
  onTypehead(isKeyPress: boolean = false) {
    if (isKeyPress)
      return this.clearEmpInfo()
    if (this.model.employee_ID.length > 9) {
      this.clearEmpInfo()
      this.snotifyService.error(
        this.translateService.instant(`System.Message.InvalidEmployeeIDLength`),
        this.translateService.instant('System.Caption.Error')
      );
    }
    else {
      this.setEmployeeInfo()
      this.getWorkShiftType();
      this.getClockTime()
    }
  }
  private getListFactory() {
    this.service.getListFactory().subscribe({
      next: res => this.factories = res
    })
  }
  private getListWorkShiftType() {
    this.commonService.getListWorkShiftType().subscribe({
      next: res => this.workShiftTypes = res
    })
  }

  private getListHoliday() {
    this.service.getListHoliday().subscribe({
      next: res => this.holidays = res
    })
  }
  private getListEmployee() {
    if (this.model.factory) {
      this.commonService.getListEmployeeAdd(this.model.factory).subscribe({
        next: res => {
          this.employeeList = res
          this.setEmployeeInfo();
        }
      })
    }
  }
  private getWorkShiftType(): boolean | void {
    if (this.model.factory && this.model.employee_ID && this.model.overtime_Date_Str) {
      let param = <OvertimeModificationMaintenanceParam>{
        factory: this.model.factory,
        employee_ID: this.model.employee_ID,
        attDate: this.model.overtime_Date_Str,
      }
      this.service.getWorkShiftType(param).subscribe({
        next: res => {
          if (res.isSuccess) {
            this.model.work_Shift_Type = res.data.work_Shift_Type
            this.getWorkShiftTypeTime()
          }
        }
      })
    }
  }
  private setEmployeeInfo() {
    if (!this.model.factory || !this.model.employee_ID)
      return this.clearEmpInfo()
    const emp = this.employeeList.find(x => x.factory == this.model.factory && x.employee_ID == this.model.employee_ID)
    if (emp) {
      this.model.useR_GUID = emp.useR_GUID;
      this.model.local_Full_Name = emp.local_Full_Name;
      this.model.department_Code = emp.actual_Department_Code;
      this.model.department_Name = emp.actual_Department_Name;
      this.model.department_Code_Name = emp.actual_Department_Code_Name;
      this.existEmloyeeID = true;
    }
    else {
      this.clearEmpInfo()
      this.functionUtility.snotifySuccessError(false, "Employee ID not exists")
    }
  }
  private clearEmpInfo() {
    this.deleteProperty('department_Code')
    this.deleteProperty('department_Name')
    this.deleteProperty('department_Code_Name')
    this.deleteProperty('local_Full_Name')
    this.existEmloyeeID = false;
  }
  private getClockTime() {
    if (!this.model.employee_ID || !this.model.overtime_Date_Str)
      return this.clearClockTime()
    this.service.getClockTime(this.model.employee_ID, this.model.overtime_Date_Str).subscribe({
      next: res => {
        if (res) {
          this.model.clock_In_Time = res?.clock_In_Time;
          this.model.clock_Out_Time = res?.clock_Out_Time;
        } else {
          this.clearClockTime()
        }
      },
      error: () => { this.clearClockTime() }
    });
  }
  private clearClockTime() {
    this.deleteProperty('clock_In_Time')
    this.deleteProperty('clock_Out_Time')
  }
  private getWorkShiftTypeTime(): boolean | void {
    if (!this.model.factory || !this.model.work_Shift_Type || !this.model.overtime_Date_Str)
      return this.deleteProperty('work_Shift_Type_Time')
    this.service.getWorkShiftTypeTime(this.model.work_Shift_Type, this.model.overtime_Date_Str, this.model.factory).subscribe({
      next: res => {
        if (res)
          this.model.work_Shift_Type_Time = res?.work_Shift_Type_Time;
        else
          this.deleteProperty('work_Shift_Type_Time')
      },
      error: () => { this.deleteProperty('work_Shift_Type_Time') }
    })
  }

  changeValue = (property: string, max: number = 20) => this.model[property] = +this.model[property] > max ? Math.min(+this.model[property], max) + '' : this.model[property]
  deleteProperty = (name: string) => delete this.model[name]
}
