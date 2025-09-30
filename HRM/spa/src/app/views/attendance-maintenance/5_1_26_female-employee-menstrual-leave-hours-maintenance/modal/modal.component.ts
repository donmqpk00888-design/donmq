import { AfterViewInit, Component, EventEmitter, input, OnDestroy, ViewChild } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import type { FemaleEmpMenstrualMain } from '@models/attendance-maintenance/5_1_26_female-employee-menstrual-leave-hours-maintenance';
import { S_5_1_26_FemaleEmployeeMenstrualLeaveHoursMaintenanceService } from '@services/attendance-maintenance/s_5_1_26_female-employee-menstrual-leave-hours-maintenance.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import type { OperationResult } from '@utilities/operation-result';
import { ModalDirective } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.scss'
})
export class ModalComponent extends InjectBase implements AfterViewInit, OnDestroy {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  isSave: boolean = false;

  title: string = '';
  iconButton = IconButton;
  classButton = ClassButton;

  model: FemaleEmpMenstrualMain = <FemaleEmpMenstrualMain>{}

  constructor(
    private service: S_5_1_26_FemaleEmployeeMenstrualLeaveHoursMaintenanceService,
    private modalService: ModalService
  ) {
    super()
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave })

  open(data: FemaleEmpMenstrualMain): void {
    this.model = structuredClone(data);
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.isSave = false
    this.directive.show()
  }

  close() {
    this.isSave = false
    this.directive.hide()
  }

  save() {
    this.spinnerService.show();
    this.isSave = true
    this.service.edit(this.model).subscribe({
      next: (res: OperationResult) => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(
          res.isSuccess,
          res.isSuccess ? 'System.Message.UpdateOKMsg' : res.error ?? 'System.Message.UpdateErrorMsg');
        if (res.isSuccess) this.directive.hide();
      }
    })
  }
}
