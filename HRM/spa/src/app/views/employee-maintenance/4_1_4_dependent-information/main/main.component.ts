import { Component, Input, OnInit } from '@angular/core';
import { EmployeeMode, IconButton } from '@constants/common.constants';
import { SessionStorageConstants } from '@constants/local-storage.constants';
import { EmployeeBasicInformationMaintenanceSource } from '@models/employee-maintenance/4_1_1_employee-basic-information-maintenance';
import { HRMS_Emp_Dependent, HRMS_Emp_DependentParam } from '@models/employee-maintenance/4_1_4_dependent-information';
import { S_4_1_4_DependentInformationService } from '@services/employee-maintenance/s_4_1_4_dependent-information.service';
import { InjectBase } from '@utilities/inject-base-app';
import { ModalService } from '@services/modal.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main-4-1-4',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent414 extends InjectBase implements OnInit {
  employeeMode = EmployeeMode
  @Input() tranfer: EmployeeBasicInformationMaintenanceSource = <EmployeeBasicInformationMaintenanceSource>{
    mode: this.employeeMode.query
  };
  iconButton = IconButton;

  data: HRMS_Emp_Dependent[] = []
  param: HRMS_Emp_DependentParam = <HRMS_Emp_DependentParam>{}
  constructor(
    private service: S_4_1_4_DependentInformationService,
    private modalService: ModalService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.getData();
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave)
        this.getData();
    })
  }

  ngOnInit() {
    this.param.useR_GUID = this.tranfer.useR_GUID;
    this.getData();
  }

  getData() {
    this.spinnerService.show();
    this.service.getData(this.param).subscribe({
      next: res => {
        this.data = res;
        this.spinnerService.hide();
      }
    })
  }

  deleteItem(item: HRMS_Emp_Dependent) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this.service.delete(item).subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.DeleteOKMsg' : 'System.Message.DeleteErrorMsg')
          if (res.isSuccess) this.getData();
        }
      })
    });
  }
  onAdd() {
    let _param = <HRMS_Emp_Dependent>{
      seq: 1,
      nationality: this.tranfer.nationality,
      identification_Number: this.tranfer.identificationNumber,
      local_Full_Name: this.tranfer.localFullName,
      dependents: true,
      useR_GUID: this.tranfer.useR_GUID
    }
    this.modalService.open({ data: _param, type: 'Add' }, "modal-4-1-4");
  }

  onEdit(item: HRMS_Emp_Dependent) {
    this.modalService.open({ data: item, type: 'Edit' }, "modal-4-1-4");
  }
}
