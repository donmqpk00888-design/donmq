import { AfterViewInit, Component, input, OnDestroy, ViewChild } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { HRMS_Emp_Identity_Card_HistoryDto, HRMS_Emp_Identity_Card_HistoryParam } from '@models/employee-maintenance/4_1_6_identification-card-history';
import { S_4_1_6_IdentificationCardHistoryService } from '@services/employee-maintenance/s_4_1_6_identification-card-history.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsModalRef, ModalDirective } from 'ngx-bootstrap/modal';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.css'],
})
export class ModalComponent extends InjectBase implements AfterViewInit, OnDestroy {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  isSave: boolean = false;

  title: string = '';
  iconButton = IconButton;
  listNationality: KeyValuePair[] = [];
  issuedDateBefore_Value: Date;
  issuedDateAfter_Value: Date;
  updateTime_Value_String_After: string
  updateTime_Value_String_Before: string
  paramData: HRMS_Emp_Identity_Card_HistoryParam = <HRMS_Emp_Identity_Card_HistoryParam>{}
  param: HRMS_Emp_Identity_Card_HistoryDto = <HRMS_Emp_Identity_Card_HistoryDto>{};

  constructor(
    private service: S_4_1_6_IdentificationCardHistoryService,
    public modal: BsModalRef,
    private modalService: ModalService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.getListNationality();
    });
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave })

  open(data: HRMS_Emp_Identity_Card_HistoryDto): void {
    this.param = structuredClone(data);
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.getListNationality();
    this.getDataDetail();
    this.isSave = false
    this.directive.show()
  }
  save() {
    this.spinnerService.show();
    this.isSave = true
    this.param.issued_Date_After = this.convertDateFormat(this.functionUtility.getUTCDate(this.issuedDateAfter_Value));
    this.param.issued_Date_Before = this.convertDateFormat(this.functionUtility.getUTCDate(this.issuedDateBefore_Value));
    this.service.create(this.param).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
        if (result.isSuccess)
          this.directive.hide();
      },
    })
  }
  close() {
    this.isSave = false
    this.directive.hide()
  }

  getDataDetail() {
    this.param.nationality_Before = this.param.nationality_After

    this.param.identification_Number_Before = this.param.identification_Number_After
    this.issuedDateBefore_Value = new Date(this.param.issued_Date_After);
    this.issuedDateAfter_Value = new Date(this.param.issued_Date_After);

    this.updateTime_Value_String_After = this.functionUtility.getDateFormat(new Date());
    this.updateTime_Value_String_Before = this.functionUtility.getDateFormat(this.param.update_Time.toDate());

    this.param.update_By_Before = this.param.update_By
    this.param.update_By_After = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).name

    this.deleteProperty('identification_Number_After')
  }

  getListNationality() {
    this.service.getListNationality().subscribe({
      next: (res) => {
        this.listNationality = res;
      },
    });
  }
  private convertDateFormat(inputDate: Date): string {
    const year = inputDate.getUTCFullYear();
    const month = (inputDate.getUTCMonth() + 1).toString().padStart(2, '0');
    const day = inputDate.getUTCDate().toString().padStart(2, '0');

    return `${year}-${month}-${day}T00:00:00`;
  }

  deleteProperty = (name: string) => delete this.param[name];
  validateEmpty() {
    return this.functionUtility.checkEmpty(this.param.identification_Number_After)
      || this.functionUtility.checkEmpty(this.param.nationality_After)
      || this.functionUtility.checkEmpty(this.issuedDateAfter_Value)
  }

}
