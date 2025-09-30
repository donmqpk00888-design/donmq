import { Component, Input, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { EmployeeBasicInformationMaintenanceSource } from '@models/employee-maintenance/4_1_1_employee-basic-information-maintenance';
import { EmployeeEmergencyContactsDto, EmployeeEmergencyContactsParam } from '@models/employee-maintenance/4_1_2_employee-emergency-contacts';
import { S_4_1_2_EmployeeEmergencyContactsService } from '@services/employee-maintenance/s_4_1_2_employee-emergency-contacts.service';
import { InjectBase } from '@utilities/inject-base-app';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { ModalService } from '@services/modal.service';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main-4-1-2',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})
export class MainComponent412 extends InjectBase implements OnInit {
  @Input() tranfer: EmployeeBasicInformationMaintenanceSource;
  modalRef?: BsModalRef;
  data: EmployeeEmergencyContactsDto[] = [];
  totalCount: number = 0;
  param: EmployeeEmergencyContactsParam = <EmployeeEmergencyContactsParam>{}
  iconButton = IconButton;

  constructor(
    private service: S_4_1_2_EmployeeEmergencyContactsService,
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
    this.param = <EmployeeEmergencyContactsParam>{
      useR_GUID: this.tranfer.useR_GUID,
      division: this.tranfer.division,
      factory: this.tranfer.factory,
      employee_ID: this.tranfer.employee_ID,
    }
    this.getData();
  }

  getData() {
    this.spinnerService.show();
    this.service.getData(this.param).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.data = res.result;
        this.totalCount = res.totalCount;
      }
    })
  }

  delete(item: EmployeeEmergencyContactsDto) {
    this.functionUtility.snotifyConfirmDefault(async () => {
      this.spinnerService.show();
      this.service.delete(item).subscribe({
        next: res => {
          this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.DeleteOKMsg' : 'System.Message.DeleteErrorMsg')
          if (res.isSuccess)
            this.getData();
          this.spinnerService.hide();
        }
      });
    });
  }

  edit(item: EmployeeEmergencyContactsDto) {
    this.modalService.open({ data: item, type: 'Edit' }, "modal-4-1-2");
  }

  add() {
    let _param = <EmployeeEmergencyContactsDto>{
      useR_GUID: this.tranfer.useR_GUID,
      division: this.tranfer.division,
      factory: this.tranfer.factory,
      employee_ID: this.tranfer.employee_ID,
      nationality: this.tranfer.nationality,
      identification_Number: this.tranfer.identificationNumber,
      localFullName: this.tranfer.localFullName
    }
    this.modalService.open({ data: _param, type: 'Add' }, "modal-4-1-2");
  }
}
