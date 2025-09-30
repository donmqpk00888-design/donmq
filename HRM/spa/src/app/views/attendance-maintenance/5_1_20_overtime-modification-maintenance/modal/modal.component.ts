import { AfterViewInit, Component, input, OnDestroy, ViewChild } from '@angular/core';
import { IconButton, ClassButton } from '@constants/common.constants';
import { OvertimeModificationMaintenanceDto } from '@models/attendance-maintenance/5_1_20_overtime-modification-maintenance';
import { S_5_1_20_OvertimeModificationMaintenanceService } from '@services/attendance-maintenance/s_5_1_20_overtime-modification-maintenance.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { ModalDirective } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.scss'
})
export class ModalComponent extends InjectBase implements AfterViewInit, OnDestroy {
  @ViewChild('modal', { static: false }) directive: ModalDirective;

  //#region Variables
  id = input<string>(this.modalService.defaultModal)
  isSave: boolean = false;

  title: string = '';
  iconButton = IconButton;
  classButton = ClassButton;

  bsConfig: Partial<BsDatepickerConfig> = { dateInputFormat: "YYYY/MM/DD" };

  model: OvertimeModificationMaintenanceDto = <OvertimeModificationMaintenanceDto>{}

  workShiftTypes: KeyValuePair[] = [];
  holidays: KeyValuePair[] = [];
  //#endregion

  constructor(
    private service: S_5_1_20_OvertimeModificationMaintenanceService,
    private modalService: ModalService
  ) {
    super()
  }

  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave })

  open(data: OvertimeModificationMaintenanceDto): void {
    this.model = structuredClone(data);
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.getHolidays();
    this.getWorkShiftTypes();
    this.isSave = false
    this.directive.show()
  }

  saveChange() {
    this.spinnerService.show();
    this.isSave = true
    this.service.edit(this.model).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(
          res.isSuccess,
          res.isSuccess ? 'System.Message.UpdateOKMsg' : res.error ?? 'System.Message.UpdateErrorMsg')
        if (res.isSuccess) this.directive.hide();
      }
    });
  }

  close() {
    this.isSave = false
    this.directive.hide()
  }

  //#region Methods

  /**
   * Danh sách ngày nghỉ
   */
  getHolidays() {
    this.service.getListHoliday().subscribe({
      next: res => {
        this.holidays = res;
      }
    })
  }

  /**
   * Danh sách lịch làm việc
   */
  getWorkShiftTypes() {
    this.commonService.getListWorkShiftType().subscribe({
      next: res => {
        this.workShiftTypes = res;
      }
    })
  }

  //#region Events

  onChangeWorkShiftType() {
    if (this.functionUtility.checkEmpty(this.model.overtime_Date))
      return this.functionUtility.snotifySuccessError(false, "Please check Date");
    this.service.getWorkShiftTypeTime(this.model.work_Shift_Type, this.model.overtime_Date.toDate().toStringDateTime(), this.model.factory).subscribe({
      next: res => {
        this.model.work_Shift_Type_Time = res?.work_Shift_Type_Time;
      }
    })
  }

  //#endregion
  changeValue = (property: string, max: number = 20) => this.model[property] = +this.model[property] > max ? Math.min(+this.model[property], max) + '' : this.model[property]
  deleteProperty = (name: string) => delete this.model[name]
}
