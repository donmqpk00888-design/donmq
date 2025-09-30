import {
  AfterViewInit,
  Component,
  EventEmitter,
  input,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { HRMS_Att_Change_RecordDto } from '@models/attendance-maintenance/5_1_18_attendance-change-record-maintenance';
import { UserForLogged } from '@models/auth/auth';
import { S_5_1_18_AttendanceChangeRecordMaintenanceService } from '@services/attendance-maintenance/s_5_1_18_attendance-change-record-maintenance.service';
import { CommonService } from '@services/common.service';
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
  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));
  title: string = '';
  data: HRMS_Att_Change_RecordDto = <HRMS_Att_Change_RecordDto>{};

  iconButton = IconButton;
  classButton = ClassButton;
  itemBackup: HRMS_Att_Change_RecordDto;

  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listWorkShiftType: KeyValuePair[] = [];
  listAttendance: KeyValuePair[] = [];
  listReasonCode: KeyValuePair[] = [];
  listHoliday: KeyValuePair[] = [];

  constructor(
    private _service: S_5_1_18_AttendanceChangeRecordMaintenanceService,
    private modalService: ModalService
  ) {
    super();
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave, data: this.data })

  open(data: HRMS_Att_Change_RecordDto): void {
    this.data = structuredClone(data);
    this.itemBackup = structuredClone(data);
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.loadDropdownList();
    this.isSave = false
    this.directive.show()
  }
  save() {
    this.spinnerService.show();
    this.isSave = true
    if (this.functionUtility.checkEmpty(this.data.days))
      this.data.days = 0;
    this.data.before_Days = this.itemBackup.days
    this.data.after_Days = this.data.days
    this.data.before_Leave_Code = this.itemBackup.leave_Code
    this.data.after_Leave_Code = this.data.leave_Code
    this.data.clock_In = this.itemBackup.modified_Clock_In;
    this.data.clock_Out = this.itemBackup.modified_Clock_Out;
    this.data.overtime_ClockIn = this.itemBackup.modified_Overtime_ClockIn;
    this.data.overtime_ClockOut = this.itemBackup.modified_Overtime_ClockOut;
    this._service.edit(this.data).subscribe({
      next: async res => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          this.directive.hide();
          this.snotifyService.success(res.error, this.translateService.instant('System.Caption.Success'));
        } else this.snotifyService.error(res.error, this.translateService.instant('System.Caption.Error'));
      },
    });
  }
  close() {
    this.isSave = false
    this.directive.hide()
  }

  loadDropdownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListWorkShiftType();
    this.getListAttendance();
    this.getListReasonCode();
    this.getListHoliday();
  }
  getListFactory() {
    this.commonService.getFactoryMain().subscribe({
      next: res => {
        this.listFactory = res;
      }
    });
  }

  getListDepartment() {
    this.commonService.getListDepartment(this.data.factory).subscribe({
      next: res => {
        this.listDepartment = res;
      }
    });
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
    this._service.getListHoliday('39', 1, 'Attendance').subscribe({
      next: res => {
        this.listHoliday = res;
      }
    });
  }
  updateTime(displayValue: string, field: string): void {
    const timePattern = /^([01]\d|2[0-3]):[0-5]\d$/;
    if (timePattern.test(displayValue)) {
      this.data[field] = displayValue.replace(':', '');
    } else {
      this.handleError('Invalid Input');
      this.data[field] = this.itemBackup[field];
    }
    this.onDataChange()
  }
  onDataChange() {
    this.data.update_By = this.user.id
    this.data.update_Time = new Date
    this.data.update_Time_Str = this.functionUtility.getDateTimeFormat(this.data.update_Time)
  }
  handleError(message: string) {
    this.spinnerService.hide()
    this.snotifyService.error(
      this.translateService.instant(message),
      this.translateService.instant('System.Caption.Error')
    )
  }

  deleteProperty(name: string) {
    delete this.data[name]
  }
}
