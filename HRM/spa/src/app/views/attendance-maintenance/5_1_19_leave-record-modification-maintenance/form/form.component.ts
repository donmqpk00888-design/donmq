import { Component, OnInit } from '@angular/core';
import { IconButton, ClassButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { Leave_Record_Modification_MaintenanceDto } from '@models/attendance-maintenance/5_1_19_leave-record-modification-maintenance';
import { UserForLogged } from '@models/auth/auth';
import { EmployeeCommonInfo } from '@models/common';
import { S_5_1_19_LeaveRecordModificationMaintenanceService } from '@services/attendance-maintenance/s_5_1_19_leave-record-modification-maintenance.service';
import { CommonService } from '@services/common.service';
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
  minDate: Date;
  maxDate: Date;
  listFactory: KeyValuePair[] = [];
  listWorkShiftType: KeyValuePair[] = [];
  listAttendance: KeyValuePair[] = [];
  listReasonCode: KeyValuePair[] = [];
  listHoliday: KeyValuePair[] = [];
  title: string = '';
  url: string = '';
  action: string = '';
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };
  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
  };
  iconButton = IconButton;
  classButton = ClassButton;
  isDuplicate: boolean = false;
  dataMain: Leave_Record_Modification_MaintenanceDto = <Leave_Record_Modification_MaintenanceDto>{};
  employeeList: EmployeeCommonInfo[] = [];

  constructor(private service: S_5_1_19_LeaveRecordModificationMaintenanceService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropdownList();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.dataMain = <Leave_Record_Modification_MaintenanceDto>{ days: 0 }
    /* Check Range：today-90 <= input date <= today+90*/
    this.minDate = new Date().addDays(-90);
    this.maxDate = new Date().addDays(90);
    this.loadDropdownList();
  }

  save(isNext: boolean) {
    this.spinnerService.show();
    this.onValueChange()
    this.service.addNew(this.dataMain).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          isNext ? this.dataMain = <Leave_Record_Modification_MaintenanceDto>{ days: 0 } : this.back()
          this.snotifyService.success(
            this.translateService.instant('System.Message.CreateOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
        } else {
          this.snotifyService.error(res.error,
            this.translateService.instant('System.Caption.Error'));
        }
      },
    })
  }

  back = () => this.router.navigate([this.url]);

  onValueChange() {
    this.dataMain.update_By = this.user.id
    this.dataMain.update_Time_Str = new Date().toStringDateTime()
  }
  loadDropdownList() {
    this.getListFactory();
    this.getListEmployee()
    this.getListWorkShiftType();
    this.getListAttendance();
    this.getListReasonCode();
    this.getListHoliday();
  }
  onFactoryChange() {
    this.getListEmployee();
    this.getWorkShiftType();
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
      this.getWorkShiftType();
    }
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

  onDateChange() {
    this.dataMain.leave_Date_Str = this.functionUtility.isValidDate(this.dataMain.leave_Date)
      ? this.functionUtility.getDateFormat(this.dataMain.leave_Date)
      : '';
    this.getWorkShiftType()
  }

  getWorkShiftType() {
    if (!this.dataMain.factory || !this.dataMain.employee_ID || !this.dataMain.leave_Date_Str)
      this.deleteProperty('work_Shift_Type')
    else
      this.service.getWorkShiftType(this.dataMain).subscribe({
        next: res => {
          res.isSuccess
            ? this.dataMain.work_Shift_Type = res.data.work_Shift_Type
            : this.deleteProperty('work_Shift_Type')
        },
        error: () => { this.deleteProperty('work_Shift_Type') }
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
  getListFactory() {
    this.commonService.getListAccountAdd().subscribe({
      next: (res) => this.listFactory = res,
    });
  }
  getListWorkShiftType() {
    this.commonService.getListWorkShiftType().subscribe({
      next: res => this.listWorkShiftType = res
    })
  }

  getListAttendance() {
    this.service.GetListLeave().subscribe({
      next: res => this.listAttendance = res
    });
  }

  getListReasonCode() {
    this.commonService.getListReasonCode().subscribe({
      next: res => this.listReasonCode = res
    });
  }

  getListHoliday() {
    this.service.getListHoliday('39', 1, 'Attendance').subscribe({
      next: res => this.listHoliday = res
    });
  }

  clearEmpInfo() {
    this.deleteProperty('useR_GUID')
    this.deleteProperty('local_Full_Name')
    this.deleteProperty('department_Code')
    this.deleteProperty('department_Code_Name')
    this.deleteProperty('department_Name')
  }

  deleteProperty = (name: string) => delete this.dataMain[name]
}
