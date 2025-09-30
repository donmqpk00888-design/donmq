import { Component, Input } from '@angular/core';
import { EmployeeMode, IconButton } from '@constants/common.constants';
import { EmployeeBasicInformationMaintenanceSource } from '@models/employee-maintenance/4_1_1_employee-basic-information-maintenance';
import { EducationUpload, HRMS_Emp_Educational, HRMS_Emp_EducationalParam } from '@models/employee-maintenance/4_1_3-education';
import { S_4_1_3_EducationService } from '@services/employee-maintenance/s_4_1_3_education.service';
import { InjectBase } from '@utilities/inject-base-app';
import { ModalService } from '@services/modal.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main-4-1-3',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent413 extends InjectBase {
  mode = EmployeeMode;
  @Input() tranfer: EmployeeBasicInformationMaintenanceSource = <EmployeeBasicInformationMaintenanceSource>{
    mode: this.mode.query
  };

  //#region Variables
  iconButton = IconButton;
  totalItems: number = 0;
  //#endregion

  //#region Arrays
  educations: HRMS_Emp_Educational[];
  //#endregion

  //#region Pagination
  filter: HRMS_Emp_EducationalParam = <HRMS_Emp_EducationalParam>{}
  //#endregion

  constructor(
    private educationService: S_4_1_3_EducationService,
    private modalService: ModalService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.getPaginationData();
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave)
        this.getPaginationData()
    })
  }

  ngOnInit() {
    this.filter.useR_GUID = this.tranfer.useR_GUID;
    this.getPaginationData();
  }

  getPaginationData() {
    if (
      !this.functionUtility.checkEmpty(this.tranfer.nationality) &&
      !this.functionUtility.checkEmpty(this.tranfer.identificationNumber)
    ) {
      this.spinnerService.show();
      this.educationService.getDataPagination(this.filter).subscribe({
        next: result => {
          this.spinnerService.hide();
          this.educations = result;
          this.totalItems = result.length;
        }
      })
    }
    else this.educations = [];
  }

  onAdd = () => {
    const data = <HRMS_Emp_Educational>{
      useR_GUID: this.tranfer.useR_GUID,
      nationality: this.tranfer.nationality,
      identification_Number: this.tranfer.identificationNumber,
      local_Full_Name: this.tranfer.localFullName,
      graduation: true
    }
    this.modalService.open({ data: data, type: 'Add' }, "modal-form-4-1-3");
  }

  onEdit(education: HRMS_Emp_Educational) {
    this.modalService.open({ data: education, type: 'Edit' }, "modal-form-4-1-3");
  }

  onUploadFiles() {
    const data = <EducationUpload>{
      useR_GUID: this.tranfer.useR_GUID,
      mode: this.tranfer.mode,
      files: [] // Danh sách files up lên
    }
    this.modalService.open({ data: data, type: 'Edit' }, 'modal-upload-4-1-3');
  }

  onDelete(item: HRMS_Emp_Educational) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.educationService.delete(item).subscribe({
        next: result => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? 'System.Message.DeleteOKMsg' : result.error, result.isSuccess)
          if (result.isSuccess) this.getPaginationData();
        }
      })
    });
  }
}
