import {
  AfterViewInit,
  Component,
  EventEmitter,
  input,
  OnDestroy,
  ViewChild,
} from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { OvertimeApplicationMaintenance_Main } from '@models/attendance-maintenance/5_1_12_overtime-application-maintenance';
import { EmployeeCommonInfo } from '@models/common';
import { S_5_1_12_Overtime_Application_Maintenance } from '@services/attendance-maintenance/s_5_1_12_overtime-application-maintenance.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { ModalDirective } from 'ngx-bootstrap/modal';
@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss'],
})
export class ModalComponent extends InjectBase implements AfterViewInit, OnDestroy {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  isSave: boolean = false;

  bsConfig: Partial<BsDatepickerConfig> = {
    isAnimated: true,
    dateInputFormat: 'YYYY/MM/DD',
  };
  title: string = '';
  data: OvertimeApplicationMaintenance_Main = <OvertimeApplicationMaintenance_Main>{};
  modalChange = new EventEmitter<boolean>();

  iconButton = IconButton;
  classButton = ClassButton;

  factoryList: KeyValuePair[] = [];
  workShiftList: KeyValuePair[] = [];
  callQuery: boolean = false
  employeeList: EmployeeCommonInfo[] = [];

  constructor(
    private service: S_5_1_12_Overtime_Application_Maintenance,
    private modalService: ModalService
  ) {
    super();
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave })

  open(data: OvertimeApplicationMaintenance_Main): void {
    this.data = structuredClone(data);
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.retryGetDropDownList()
    this.isSave = false
    this.directive.show()
  }
  save(isBack: boolean): void {
    this.spinnerService.show();
    this.isSave = true
    this.service[this.id() == 'Add' ? 'postData' : 'putData'](this.data)
      .subscribe({
        next: async (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.callQuery = true
            this.snotifyService.success(
              this.translateService.instant(this.id() == 'Add' ? 'System.Message.CreateOKMsg' : 'System.Message.UpdateOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
            !isBack ? this.clear() : this.directive.hide();
          } else {
            this.snotifyService.error(
              this.translateService.instant(`AttendanceMaintenance.OvertimeApplicationMaintenance.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
        },
      })
  }
  clear() {
    this.modalService.onHide.emit({ isSave: this.isSave })
    this.data = <OvertimeApplicationMaintenance_Main>{
      overtime_Hours: '0',
      night_Hours: '0',
      training_Hours: '0',
      night_Eat_Times: 0
    }
  }
  close() {
    this.isSave = false
    this.directive.hide()
  }

  // #region Dropdown List
  retryGetDropDownList() {
    this.spinnerService.show()
    this.service.getDropDownList(this.data.factory)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide()
          this.filterList(res)
        }
      });
  }
  filterList(keys: KeyValuePair[]) {
    this.factoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "FA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.workShiftList = structuredClone(keys.filter((x: { key: string; }) => x.key == "WO")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
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
  // #endregion

  // #region On Change Functions
  onFactoryChange() {
    this.getListEmployee()
    this.getShiftTime()
    this.getOvertimeParam()
    this.deleteProperty('employee_Id')
    this.clearEmpInfo()
  }
  onTimeChange(name: string) {
    const clone = structuredClone(this.data[`${name}_Str`])
    this.data[`${name}_Str`] = this.data[name] ? this.getTimeFormat(this.data[name]) : '';
    if (clone != this.data[`${name}_Str`])
      this.getOvertimeParam()
  }
  onWorkShiftTypeChange() {
    this.getShiftTime()
    this.getOvertimeParam()
  }
  onOvertimeDateChange() {
    if (this.data.overtime_Date instanceof Date && !isNaN(this.data.overtime_Date.getTime())) {
      this.data.overtime_Date_Str = this.data.overtime_Date ? this.functionUtility.getDateFormat(new Date(this.data.overtime_Date)) : '';
      this.getShiftTime()
      this.getOvertimeParam()
    }
  }
  onTypehead(isKeyPress: boolean = false) {
    if (isKeyPress)
      return this.clearEmpInfo()
    if (this.data.employee_Id.length > 9) {
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
  // #endregion
  getShiftTime() {
    if (!this.data.factory || !this.data.work_Shift_Type || !this.data.overtime_Date_Str) {
      this.deleteProperty('clock_In')
      this.deleteProperty('clock_Out')
      return
    }
    this.service.getShiftTime(this.data)
      .subscribe({
        next: (res) => {
          if (!this.functionUtility.checkEmpty(res.clock_In_Str)) {
            const clockInHour = +res.clock_In_Str.substring(0, 2)
            const clockInMinute = +res.clock_In_Str.substring(2, 4)
            this.data.clock_In = new Date()
            this.data.clock_In.setHours(clockInHour)
            this.data.clock_In.setMinutes(clockInMinute)
          } else
            this.data.clock_In = null
          if (!this.functionUtility.checkEmpty(res.clock_Out_Str)) {
            const clockOutHour = +res.clock_Out_Str.substring(0, 2)
            const clockOutMinute = +res.clock_Out_Str.substring(2, 4)
            this.data.clock_Out = new Date()
            this.data.clock_Out.setHours(clockOutHour)
            this.data.clock_Out.setMinutes(clockOutMinute)
          } else
            this.data.clock_Out = null
          this.getOvertimeParam()
        }
      });
  }

  getOvertimeParam() {
    if (!this.data.factory || !this.data.work_Shift_Type || !this.data.overtime_Date_Str ||
      !this.data.overtime_Start_Str || !this.data.overtime_End_Str) {
      this.deleteProperty('overtime_Hours')
      this.deleteProperty('night_Hours')
      return
    }
    this.service.getOvertimeParam(this.data)
      .subscribe({
        next: (res) => {
          if (res.isSuccess) {
            this.data.overtime_Hours = res.data.overtime_Hours.toString();
            this.data.night_Hours = res.data.night_Hours.toString();
          }
        }
      });

  }
  setEmployeeInfo() {
    if (!this.data.factory || !this.data.employee_Id)
      return this.clearEmpInfo()
    const emp = this.employeeList.find(x => x.factory == this.data.factory && x.employee_ID == this.data.employee_Id)
    if (emp) {
      this.data.useR_GUID = emp.useR_GUID;
      this.data.local_Full_Name = emp.local_Full_Name;
      this.data.department_Code = emp.actual_Department_Code;
      this.data.department_Name = emp.actual_Department_Name;
      this.data.department_Code_Name = emp.actual_Department_Code_Name;
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
    this.deleteProperty('department_Name')
    this.deleteProperty('department_Code_Name')

  }
  getTimeFormat(date: Date) {
    return date.getHours().toStringLeadingZeros(2) + date.getMinutes().toStringLeadingZeros(2)
  }
  deleteProperty(name: string) {
    delete this.data[name]
  }

}
