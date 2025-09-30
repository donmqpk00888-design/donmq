import { AfterViewInit, Component, input, ViewChild } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { HRMSEmpExternalExperienceModel } from '@models/employee-maintenance/4_1_5_external-experience';
import { S_4_1_5_ExternalExperienceService } from '@services/employee-maintenance/s_4_1_5_external-experience.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { ModalDirective } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-modal-4-1-5',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss']
})
export class ModalComponent415 extends InjectBase implements AfterViewInit {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  type: string
  isSave: boolean = false;

  iconButton = IconButton;
  minMode: BsDatepickerViewMode = 'month';
  title: string = '';
  model: HRMSEmpExternalExperienceModel = <HRMSEmpExternalExperienceModel>{};

  tenureResult: string;

  time_end: Date = null;
  time_start: Date = null;

  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM',
    minMode: this.minMode
  };
  key: KeyValuePair[] = [
    { key: 'Y', value: true },
    { key: 'N', value: false }
  ];

  constructor(
    private service: S_4_1_5_ExternalExperienceService,
    private modalService: ModalService
  ) { super() }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave })

  open(source: any): void {
    const _source = structuredClone(source);
    this.model = _source.data as HRMSEmpExternalExperienceModel;
    this.type = _source.type
    this.isSave = false
    this.title = this.functionUtility.getTitle(this.service.functions[0]?.program_Code)
    this.getDataFromSource()
    this.directive.show()
  }
  save() {
    this.spinnerService.show()
    this.model.tenure_Start = !this.time_start?.isValidDate() ? null : this.time_start.toBeginDate().toStringDateTime().toUTCDate().toJSON();
    this.model.tenure_End = !this.time_end?.isValidDate() ? null : this.time_end.toBeginDate().toStringDateTime().toUTCDate().toJSON();
    this.isSave = true
    this.service[this.type == 'Add' ? 'add' : 'edit'](this.model).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess,
          result.isSuccess ? (this.type == 'Add' ? 'System.Message.CreateOKMsg' : 'System.Message.UpdateOKMsg') : result.error)
        if (result.isSuccess)
          this.directive.hide()
      },

    })
  }

  close() {
    this.isSave = false
    this.directive.hide()
  }

  getDataFromSource() {
    if (this.type == 'Add') {
      this.service.getSeq(this.model).subscribe({
        next: res => this.model.seq = res
      })
    } else {
      this.time_start = this.functionUtility.checkEmpty(this.model.tenure_Start) ? null : new Date(this.model.tenure_Start);
      this.time_end = this.functionUtility.checkEmpty(this.model.tenure_End) ? null : new Date(this.model.tenure_End);
      this.calculateTenure();
    }
  }

  calculateTenure() {
    if (this.time_start && this.time_end) {
      // Tính toán số mili giây giữa hai thời điểm
      let timeDifference = this.time_end.getTime() - this.time_start.getTime();

      // Chuyển đổi số mili giây thành số năm và làm tròn kết quả đến 1 chữ số thập phân
      let yearsDifference = timeDifference / (1000 * 60 * 60 * 24 * 365.25);
      let roundedYears = Number(yearsDifference.toFixed(1));

      this.tenureResult = roundedYears.toString();
    } else
      this.tenureResult = ''
  }

  validateEmpty() {
    return this.functionUtility.checkEmpty(this.model.seq)
      || this.functionUtility.checkEmpty(this.model.company_Name)
      || this.functionUtility.checkEmpty(this.time_end)
      || this.functionUtility.checkEmpty(this.time_start)
  }
}
