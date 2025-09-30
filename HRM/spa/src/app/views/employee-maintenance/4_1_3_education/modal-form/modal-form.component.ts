import { AfterViewInit, Component, input, ViewChild } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { HRMS_Emp_Educational } from '@models/employee-maintenance/4_1_3-education';
import { S_4_1_3_EducationService } from '@services/employee-maintenance/s_4_1_3_education.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { ModalDirective } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-modal-form-4-1-3',
  templateUrl: './modal-form.component.html',
  styleUrls: ['./modal-form.component.scss']
})
export class ModalFormComponent413 extends InjectBase implements AfterViewInit {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  type: string

  //#region Vaiables
  title: string = '';
  isSave: boolean = false;
  iconButton = IconButton;
  //#endregion

  degrees: KeyValuePair[] = [];
  academicSystems: KeyValuePair[] = [];
  majors: KeyValuePair[] = [];

  periodStartDate: Date = new Date();
  periodEndDate: Date = null;

  education: HRMS_Emp_Educational = <HRMS_Emp_Educational>{}

  constructor(
    private service: S_4_1_3_EducationService,
    private modalService: ModalService
  ) {
    super()
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave })

  open(source: any): void {
    const _source = structuredClone(source);
    this.education = _source.data as HRMS_Emp_Educational;
    this.type = _source.type
    this.title = this.functionUtility.getTitle(this.service.functions[0]?.program_Code)
    this.loadDropDownList()
    this.initForm();
    this.isSave = false
    this.directive.show()
  }

  save() {
    let checkValidate = this.validate();
    this.isSave = true
    if (checkValidate) {
      this.spinnerService.show();
      // Add
      this.service[this.type == 'Add' ? 'create' : 'update'](this.education).subscribe({
        next: result => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(result.isSuccess,
            result.isSuccess ? (this.type == 'Add' ? 'System.Message.CreateOKMsg' : 'System.Message.UpdateOKMsg') : result.error)
          if (result.isSuccess)
            this.directive.hide();
        }
      })
    }
  }

  close() {
    this.isSave = false
    this.directive.hide()
  }

  loadDropDownList() {
    this.getDegrees();
    this.getAcademicSystems();
    this.getMajors();
  }

  //#region Methods

  initForm() {
    if (this.type == 'Add') {
      this.periodEndDate = null;
      this.education.period_Start = this.periodStartDate.toDate().toStringYearMonth();
    }
    else {
      if (!this.functionUtility.checkEmpty(this.education.period_Start))
        this.periodStartDate = new Date(this.education.period_Start);

      if (!this.functionUtility.checkEmpty(this.education.period_End))
        this.periodEndDate = new Date(this.education.period_End);
      else this.periodEndDate = null;
    }

    this.validateSave();
  }

  getDegrees() {
    this.service.getDegrees().subscribe({
      next: result => {
        this.degrees = result;
      }
    })
  }
  getAcademicSystems() {
    this.service.getAcademicSystems().subscribe({
      next: result => {
        this.academicSystems = result;
      }
    })
  }
  getMajors() {
    this.service.getMajors().subscribe({
      next: result => {
        this.majors = result;
      }
    })
  }

  //#endregion

  //#endregion

  //#region SAVECHANGE
  validate(): boolean {
    if (this.functionUtility.checkEmpty(this.education.nationality)) {
      this.snotifyService.warning(
        `Please input ${this.translateService.instant('EmployeeInformationModule.Education.Nationality')}`,
        this.translateService.instant('System.Caption.Warning'));
      return false;
    }

    if (this.functionUtility.checkEmpty(this.education.identification_Number)) {
      this.snotifyService.warning(
        `Please input ${this.translateService.instant('EmployeeInformationModule.Education.IdentificationNumber')}`,
        this.translateService.instant('System.Caption.Warning'));
      return false;
    }

    if (this.functionUtility.checkEmpty(this.education.degree)) {
      this.snotifyService.warning(
        `Please input ${this.translateService.instant('EmployeeInformationModule.Education.Degree')}`,
        this.translateService.instant('System.Caption.Warning'));
      return false;
    }

    if (this.education.period_Start == null || this.periodStartDate.toString() == 'Invalid Date' || this.periodStartDate.toString() == 'NaN/NaN') {
      this.snotifyService.warning(
        `Please input ${this.translateService.instant('EmployeeInformationModule.Education.PeriodStart')}`,
        this.translateService.instant('System.Caption.Warning'));
      return false;
    }

    if (this.education.period_Start != null && (this.periodStartDate.toString() == 'Invalid Date' || this.periodStartDate.toString() == 'NaN/NaN')) {
      this.snotifyService.warning(
        `Input ${this.translateService.instant('EmployeeInformationModule.Education.PeriodStart')} incorrect format`,
        this.translateService.instant('System.Caption.Warning'));
      return false;
    }

    if (this.education.period_End != null && (this.periodEndDate.toString() == 'Invalid Date' || this.periodEndDate.toString() == 'NaN/NaN')) {
      this.snotifyService.warning(
        `Input ${this.translateService.instant('EmployeeInformationModule.Education.PeriodEnd')} incorrect format`,
        this.translateService.instant('System.Caption.Warning'));
      return false;
    }

    return true;
  }

  validateSave() {
    if (!this.functionUtility.checkEmpty(this.education.nationality) &&
      !this.functionUtility.checkEmpty(this.education.identification_Number) &&
      !this.functionUtility.checkEmpty(this.education.degree) &&
      !this.functionUtility.checkEmpty(this.education.local_Full_Name) &&
      // Value of dropdown
      !this.functionUtility.checkEmpty(this.education.degree) &&
      !this.functionUtility.checkEmpty(this.education.academic_System) &&
      !this.functionUtility.checkEmpty(this.education.major) &&
      !this.functionUtility.checkEmpty(this.education.school) &&
      !this.functionUtility.checkEmpty(this.education.department) &&

      // value Datetime Picker
      this.periodStartDate != null &&
      this.periodStartDate?.toString() != 'Invalid Date' &&
      this.periodStartDate?.toString() != 'NaN/NaN' &&

      this.periodEndDate != null &&
      this.periodEndDate?.toString() != 'Invalid Date' &&
      this.periodEndDate?.toString() != 'NaN/NaN'
    ) this.isSave = true;
    else
      this.isSave = false;
  }

  //#endregion

  //#region Events
  onPeriodStartDateChange() {
    if (this.periodStartDate != null &&
      this.periodStartDate.toString() != 'Invalid Date' &&
      this.periodStartDate.toString() != 'NaN/NaN') {

      if (this.periodEndDate != null &&
        this.periodEndDate.toString() != 'Invalid Date' &&
        this.periodEndDate.toString() != 'NaN/NaN' &&
        (this.periodStartDate.getFullYear() > this.periodEndDate.getFullYear() ||
          this.periodStartDate.getFullYear() == this.periodEndDate.getFullYear() &&
          (this.periodStartDate.getMonth() + 1) > (this.periodEndDate.getMonth() + 1))
      ) {
        this.periodEndDate = null;
        this.education.period_End = '';
        this.validateSave();
        return this.functionUtility.snotifySuccessError(false, "Period start cannot be greater than to the period end", false)
      }
      else
        this.education.period_Start = this.periodStartDate.toDate().toStringYearMonth();
    }
    // Ngày bắt đầu có dữ liệu nhưng không đúng Format
    else if (this.periodStartDate != null &&
      (this.periodStartDate.toString() == 'Invalid Date' ||
        this.periodStartDate.toString() == 'NaN/NaN')) {
      this.snotifyService.warning(
        `Input ${this.translateService.instant('EmployeeInformationModule.Education.PeriodStart')} incorrect format`,
        this.translateService.instant('System.Caption.Warning'));
      this.education.period_End = '';
    }
    else this.education.period_Start = '';

    this.validateSave();
  }

  onPeriodEndDateChange() {
    // Có dữ liệu , đúng format
    if (this.periodEndDate != null &&
      this.periodEndDate.toString() != 'Invalid Date' &&
      this.periodEndDate.toString() != 'NaN/NaN') {
      // Ngày bắt đầu có dữ liệu nhưng lớn hơn ngày kết thúc
      if (this.periodStartDate != null &&
        this.periodStartDate.toString() != 'Invalid Date' &&
        this.periodStartDate.toString() != 'NaN/NaN' &&
        (this.periodEndDate.getFullYear() < this.periodStartDate.getFullYear()) ||
        this.periodEndDate.getFullYear() == this.periodStartDate.getFullYear() &&
        (this.periodEndDate.getMonth() + 1) < (this.periodStartDate.getMonth() + 1)
      ) {
        this.periodStartDate = null;
        this.education.period_Start = '';
        this.validateSave();
        return this.functionUtility.snotifySuccessError(false, "Period end cannot be less than to the period start", false)
      }
      else this.education.period_End = this.periodEndDate.toDate().toStringYearMonth();
    }
    // Ngày kết thúc Có dữ liệu nhưng không đúng Format
    else if (this.periodEndDate != null &&
      (this.periodEndDate.toString() == 'Invalid Date' ||
        this.periodEndDate.toString() == 'NaN/NaN')) {
      this.snotifyService.warning(
        `Input ${this.translateService.instant('EmployeeInformationModule.Education.PeriodEnd')} incorrect format`,
        this.translateService.instant('System.Caption.Warning'));
      this.education.period_End = '';
    }
    else this.education.period_End = '';

    this.validateSave();
  }
  deleteProperty(name: string) {
    delete this.education[name]
  }
  //#endregion
}
