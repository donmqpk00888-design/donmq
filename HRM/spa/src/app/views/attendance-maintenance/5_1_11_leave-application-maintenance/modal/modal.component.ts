import {
  AfterViewInit,
  Component,
  EventEmitter,
  input,
  OnDestroy,
  ViewChild,
} from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { CaptionConstants } from '@constants/message.enum';
import { LeaveApplicationMaintenance_Main } from '@models/attendance-maintenance/5_1_11_leave-application-maintenance';
import { EmployeeCommonInfo } from '@models/common';
import { S_5_1_11_Leave_Application_Maintenance } from '@services/attendance-maintenance/s_5_1_11_leave-application-maintenance.service';
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
  data: LeaveApplicationMaintenance_Main = <LeaveApplicationMaintenance_Main>{};
  modalChange = new EventEmitter<boolean>();

  iconButton = IconButton;
  classButton = ClassButton;

  factoryList: KeyValuePair[] = [];
  leaveList: KeyValuePair[] = [];
  employeeList: EmployeeCommonInfo[] = [];

  constructor(
    private service: S_5_1_11_Leave_Application_Maintenance,
    private modalService: ModalService
  ) {
    super();
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave })

  open(data: LeaveApplicationMaintenance_Main): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.data = structuredClone(data);
    if (this.id() == 'Edit') {
      this.data.leave_Start_Datetime = this.data.leave_Start_Date
      this.data.leave_Start_Time = this.data.leave_Start_Date
      this.data.leave_End_Datetime = this.data.leave_End_Date
      this.data.leave_End_Time = this.data.leave_End_Date
    }
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
            this.snotifyService.success(
              this.translateService.instant(this.id() == 'Add' ? 'System.Message.CreateOKMsg' : 'System.Message.UpdateOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
            !isBack ? this.clear() : this.directive.hide();
          } else {
            this.snotifyService.error(
              this.translateService.instant(`AttendanceMaintenance.LeaveApplicationMaintenance.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
        },
      })
  }
  clear() {
    this.modalService.onHide.emit({ isSave: this.isSave })
    this.data = <LeaveApplicationMaintenance_Main>{ days: '0' }
  }
  close() {
    this.isSave = false
    this.directive.hide()
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
  private setEmployeeInfo() {
    if (!this.data.factory || !this.data.employee_Id)
      return this.clearEmpInfo()
    const emp = this.employeeList.find(x => x.factory == this.data.factory && x.employee_ID == this.data.employee_Id)
    if (emp) {
      if (['A', 'S'].indexOf(emp.employment_Status) != -1)
        this.snotifyService.warning('Assigned/Supported Employee', CaptionConstants.WARNING);
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
    this.deleteProperty('department_Code')
    this.deleteProperty('department_Name')
    this.deleteProperty('department_Code_Name')
    this.deleteProperty('local_Full_Name')
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
    this.leaveList = structuredClone(keys.filter((x: { key: string; }) => x.key == "LE")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
  }
  // #endregion

  // #region On Change Functions
  onFactoryChange() {
    this.getListEmployee()
    this.deleteProperty('employee_Id')
    this.clearEmpInfo()
  }

  onDateStartChange = () => this.setDate('Start')
  onTimeStartChange = () => this.setTime('Start')
  onDateEndChange = () => this.setDate('End')
  onTimeEndChange = () => this.setTime('End')
  // #endregion

  private setDate(name: string) {
    this.data[`leave_${name}`] = this.data[`leave_${name}_Date`] ? this.functionUtility.getDateFormat(this.data[`leave_${name}_Date`]) : ''
    if (!this.functionUtility.checkEmpty(this.data[`leave_${name}`]) && !this.functionUtility.checkEmpty(this.data[`min_${name}`]))
      this.setDateTime(name)
    this.checkDateTimeRange()
  }
  private setTime(name: string) {
    this.data[`min_${name}`] = this.data[`leave_${name}_Time`] ? this.getTimeFormat(this.data[`leave_${name}_Time`]) : ''
    if (!this.functionUtility.checkEmpty(this.data[`leave_${name}`]) && !this.functionUtility.checkEmpty(this.data[`min_${name}`]))
      this.setDateTime(name)
    this.checkDateTimeRange()
  }
  private setDateTime(name: string) {
    const hour = this.functionUtility.checkEmpty(this.data[`min_${name}`]) ? '00' : this.data[`min_${name}`].substring(0, 2)
    const minute = this.functionUtility.checkEmpty(this.data[`min_${name}`]) ? '00' : this.data[`min_${name}`].substring(2, 4)
    this.data[`leave_${name}_Datetime`] = new Date(`${this.data[`leave_${name}`]} ${hour}:${minute}:00`)
  }
  // #endregion

  private getTimeFormat(date: Date) {
    return date.getHours().toStringLeadingZeros(2) + date.getMinutes().toStringLeadingZeros(2)
  }

  checkDateTimeRange() {
    this.data.isErrorTime = this.data.leave_Start_Datetime > this.data.leave_End_Datetime
    if (this.data.isErrorTime) {
      this.snotifyService.clear()
      this.snotifyService.error(
        this.translateService.instant('AttendanceMaintenance.LeaveApplicationMaintenance.LimitDateTimeError'),
        this.translateService.instant('System.Caption.Error')
      );
    }
  }
  deleteProperty(name: string) {
    delete this.data[name]
  }
}
