import { Component, Input, OnInit } from '@angular/core';
import { EmployeeMode, IconButton } from '@constants/common.constants';
import { SessionStorageConstants } from '@constants/local-storage.constants';
import { EmployeeBasicInformationMaintenanceSource } from '@models/employee-maintenance/4_1_1_employee-basic-information-maintenance';
import { HRMSEmpExternalExperience, HRMSEmpExternalExperienceModel, HRMS_Emp_External_ExperienceParam } from '@models/employee-maintenance/4_1_5_external-experience';
import { S_4_1_5_ExternalExperienceService } from '@services/employee-maintenance/s_4_1_5_external-experience.service';

import { InjectBase } from '@utilities/inject-base-app';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { FunctionInfomation } from '@models/common';
import { ModalService } from '@services/modal.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main-4-1-5',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent415 extends InjectBase implements OnInit {
  get functions(): FunctionInfomation[] { return JSON.parse(sessionStorage.getItem(SessionStorageConstants.SELECTED_FUNCTIONS)) };
  employeeMode = EmployeeMode;
  @Input() tranfer: EmployeeBasicInformationMaintenanceSource = <EmployeeBasicInformationMaintenanceSource>{
    mode: this.employeeMode.query
  };
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
  };
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
  };
  iconButton = IconButton;
  data: HRMSEmpExternalExperience[] = [];
  filter: HRMS_Emp_External_ExperienceParam = <HRMS_Emp_External_ExperienceParam>{
    useR_GUID: this.tranfer.useR_GUID
  }
  constructor(
    private service: S_4_1_5_ExternalExperienceService,
    private modalService: ModalService
  ) {
    super()
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave)
        this.getData();
    })
  }

  ngOnInit(): void {
    this.filter.useR_GUID = this.tranfer.useR_GUID;
    this.getData();
  }

  getData() {
    this.spinnerService.show();
    this.service.getData(this.filter).subscribe({
      next: (res) => {
        this.data = res;
        this.data.forEach(x => {
          if (x.tenure_End && x.tenure_Start) {
            let endDate = new Date(x.tenure_End);
            let startDate = new Date(x.tenure_Start);
            let timeDiff = endDate.getTime() - startDate.getTime();
            let yearsDifference = timeDiff / (1000 * 60 * 60 * 24 * 365.25);
            x.tenure_Yealy = Number(yearsDifference.toFixed(1));
          } else {
            x.tenure_Yealy = null;
          }
        });
        this.spinnerService.hide();
      }
    })
  }

  onAdd() {
    let _param = <HRMSEmpExternalExperienceModel>{
      leadership_Role: false,
      useR_GUID: this.tranfer.useR_GUID,
      nationality: this.tranfer.nationality,
      identification_Number: this.tranfer.identificationNumber,
      local_Full_Name: this.tranfer.localFullName,
    }
    this.modalService.open({ data: _param, type: 'Add' }, "modal-4-1-5");
  }

  onEdit(item: HRMSEmpExternalExperienceModel) {
    this.modalService.open({ data: item, type: 'Edit' }, "modal-4-1-5");
  }

  deleteItem(item: HRMSEmpExternalExperienceModel) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      item.useR_GUID = this.tranfer.useR_GUID;
      this.service.delete(item).subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(res.isSuccess, `System.Message.${res.isSuccess ? 'DeleteOKMsg' : 'DeleteErrorMsg'}`)
          if (res.isSuccess) this.getData();
        }
      })
    });
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }
}
