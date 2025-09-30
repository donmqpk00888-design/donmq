import { AfterViewInit, Component, input, OnDestroy, ViewChild } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { HRMS_Att_Overtime_TempDto } from '@models/attendance-maintenance/5_1_16_overtime_temporary_record_maintenance';
import { S_5_1_16_OvertimeTemporaryRecordMaintenanceService } from '@services/attendance-maintenance/s_5_1_16_overtime_temporary_record_maintenance.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import {
  BsDatepickerConfig,
} from 'ngx-bootstrap/datepicker';
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

  title: string = '';
  data: HRMS_Att_Overtime_TempDto = <HRMS_Att_Overtime_TempDto>{};
  itemBackup: HRMS_Att_Overtime_TempDto = <HRMS_Att_Overtime_TempDto>{};

  iconButton = IconButton;
  classButton = ClassButton;
  bsConfig: Partial<BsDatepickerConfig> = { dateInputFormat: 'YYYY/MM/DD' };
  listFactory: KeyValuePair[] = []
  workShiftType: KeyValuePair[] = []
  listHoliday: KeyValuePair[] = []
  inputError: boolean = false;

  constructor(
    private service: S_5_1_16_OvertimeTemporaryRecordMaintenanceService,
    private modalService: ModalService
  ) { super(); }

  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave, data: this.data })

  open(data: HRMS_Att_Overtime_TempDto): void {
    this.data = structuredClone(data);
    this.itemBackup = structuredClone(data);
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.getListFactory();
    this.getListWorkShiftType();
    this.getListHoliday();
    this.isSave = false
    this.directive.show()
  }

  close() {
    this.isSave = false
    this.directive.hide()
  }

  save() {
    this.isSave = true
    this.checkEmptyDecimal()
    this.directive.hide();
  }

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: res => this.listFactory = res,

    })
  }
  getListWorkShiftType() {
    this.service.getListWorkShiftType().subscribe({
      next: res => this.workShiftType = res,

    })
  }
  getListHoliday() {
    this.service.getListHoliday().subscribe({
      next: res => this.listHoliday = res,

    })
  }
  getShiftTimeByWorkShift() {
    if (!this.data.work_Shift_Type)
      this.deleteProperty('shift_Time')
    else
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
  checkEmptyDecimal() {
    this.functionUtility.checkEmpty(this.data.overtime_Hours) ? this.data.overtime_Hours = 0 : this.data.overtime_Hours
    this.functionUtility.checkEmpty(this.data.night_Hours) ? this.data.night_Hours = 0 : this.data.night_Hours
    this.functionUtility.checkEmpty(this.data.night_Overtime_Hours) ? this.data.night_Overtime_Hours = 0 : this.data.night_Overtime_Hours
    this.functionUtility.checkEmpty(this.data.training_Hours) ? this.data.training_Hours = 0 : this.data.training_Hours
    this.functionUtility.checkEmpty(this.data.night_Eat) ? this.data.night_Eat = 0 : this.data.night_Eat
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
      this.data[field] = this.itemBackup[field];
    }
  }
  deleteProperty = (name: string) => delete this.data[name]
}
