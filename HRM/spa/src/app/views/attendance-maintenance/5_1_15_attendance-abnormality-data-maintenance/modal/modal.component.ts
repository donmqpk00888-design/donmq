import { AfterViewInit, Component, EventEmitter, input, OnDestroy, OnInit, ViewChild } from '@angular/core';
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
import { ModalDirective } from 'ngx-bootstrap/modal';
import { Observable } from 'rxjs';
import { KeyValuePair } from '@utilities/key-value-pair';
import { ModalService } from '@services/modal.service';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss'],
})
export class ModalComponent extends InjectBase implements AfterViewInit, OnDestroy {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  isSave: boolean = false;

  title: string = '';
  user: UserForLogged = JSON.parse(
    localStorage.getItem(LocalStorageConstants.USER)
  );
  data: HRMS_Att_Temp_RecordDto = <HRMS_Att_Temp_RecordDto>{};
  itemBackup: HRMS_Att_Temp_RecordDto = <HRMS_Att_Temp_RecordDto>{};

  iconButton = IconButton;
  classButton = ClassButton;

  listFactory: KeyValuePair[] = [];
  listWorkShiftType: KeyValuePair[] = [];
  listAttendance: KeyValuePair[] = [];
  listUpdateReason: KeyValuePair[] = [];
  listHoliday: KeyValuePair[] = [];

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
    isAnimated: true,
    dateInputFormat: 'YYYY/MM/DD',
    minMode: this.minMode,
    minDate: this.minDate,
    maxDate: this.maxDate,
  };
  constructor(
    private service: S_5_1_15_AttendanceAbnormalityDataMaintenanceService,
    private modalService: ModalService,
  ) {
    super();
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave, data: this.data })

  open(data: HRMS_Att_Temp_RecordDto): void {
    this.data = structuredClone(data);
    this.itemBackup = structuredClone(data);
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);

    this.data.modified_Clock_In_Old = this.data.modified_Clock_In
    this.data.modified_Clock_Out_Old = this.data.modified_Clock_Out
    this.data.modified_Overtime_ClockIn_Old = this.data.modified_Overtime_ClockIn
    this.data.modified_Overtime_ClockOut_Old = this.data.modified_Overtime_ClockOut

    this.data.clock_In = this.data.modified_Clock_In;
    this.data.clock_Out = this.data.modified_Clock_Out;
    this.data.overtime_ClockIn = this.data.modified_Overtime_ClockIn;
    this.data.overtime_ClockOut = this.data.modified_Overtime_ClockOut;

    this.getDropdownList()
    this.isSave = false
    this.directive.show()
  }

  close() {
    this.isSave = false
    this.directive.hide()
  }

  save() {
    this.isSave = true
    this.directive.hide();
  }

  getDropdownList() {
    this.getListFactory();
    this.getListWorkShiftType();
    this.getListAttendance();
    this.getListUpdateReason();
    this.getListHoliday();
  }
  //#region getList
  getListFactory() {
    this.getList(
      () =>
        this.service.getListFactoryByUser(),
      this.listFactory
    );
  }

  getListWorkShiftType() {
    this.getList(
      () =>
        this.service.getListWorkShiftType(),
      this.listWorkShiftType
    );
  }

  getListAttendance() {
    this.getList(
      () =>
        this.service.getListAttendance(),
      this.listAttendance
    );
  }

  getListUpdateReason() {
    this.getList(
      () =>
        this.service.getListUpdateReason(),
      this.listUpdateReason
    );
  }

  getListHoliday() {
    this.getList(
      () =>
        this.service.getListHoliday(),
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
  handleError(message: string) {
    this.spinnerService.hide();
    this.snotifyService.error(
      this.translateService.instant(message),
      this.translateService.instant('System.Caption.Error')
    );
  }
  validateDecimal(value: string): void {
    const decimalPattern = /^\d{1,5}(\.\d{1,5})?$/;
    if (decimalPattern.test(value)) this.data.days = value;
    else {
      this.handleError('Invalid Input');
      this.data.days = this.itemBackup.days;
    }
  }

  //#region validate
  updateTime(displayValue: string, field: string): void {
    const timePattern = /^([01]\d|2[0-3]):[0-5]\d$/;
    if (timePattern.test(displayValue)) {
      this.data[field] = displayValue.replace(':', '');
    } else {
      this.handleError('Invalid Input');
      this.data[field] = this.itemBackup[field];
    }
  }
}
