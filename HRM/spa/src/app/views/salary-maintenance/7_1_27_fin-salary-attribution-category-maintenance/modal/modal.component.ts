import { AfterViewInit, Component, input, OnDestroy, ViewChild } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { FinSalaryAttributionCategoryMaintenance_Data } from '@models/salary-maintenance/7_1_27_fin-salary-attribution-category-maintenance';
import { ModalService } from '@services/modal.service';
import { S_7_1_27_FinSalaryAttributionCategoryMaintenance } from '@services/salary-maintenance/s_7_1_27_fin-salary-attribution-category-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import type { OperationResult } from '@utilities/operation-result';
import { ModalDirective } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-modal-7-1-27',
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.scss'
})
export class ModalComponent extends InjectBase implements AfterViewInit, OnDestroy {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  isSave: boolean = false;
  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));

  title: string = '';
  iconButton = IconButton;
  classButton = ClassButton;

  model: FinSalaryAttributionCategoryMaintenance_Data = <FinSalaryAttributionCategoryMaintenance_Data>{}

  salaryCategoryList: KeyValuePair[] = [];

  constructor(
    private service: S_7_1_27_FinSalaryAttributionCategoryMaintenance,
    private modalService: ModalService
  ) {
    super()
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }
  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave })
  open(data: FinSalaryAttributionCategoryMaintenance_Data): void {
    this.model = structuredClone(data);
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.isSave = false
    this.getDropDownList()
  }
  close() {
    this.isSave = false
    this.directive.hide()
  }
  save() {
    this.spinnerService.show();
    this.isSave = true
    this.service.putData(this.model).subscribe({
      next: (res: OperationResult) => {
        this.spinnerService.hide();
         if (res.isSuccess) {
             this.directive.hide();
            this.snotifyService.success(
              this.translateService.instant('System.Message.UpdateOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
          } else {
            this.snotifyService.error(
              this.translateService.instant(`System.Message.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
      }
    })
  }
  onSalaryChange() {
    this.model.update_By = this.user.id;
    this.model.update_Time = this.functionUtility.getDateTimeFormat(new Date())
  }
  private getDropDownList() {
    this.service.getDropDownList()
      .subscribe({
        next: (res) => {
          this.salaryCategoryList = structuredClone(res.filter((x: { key: string; }) => x.key == "SA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
          this.directive.show()
        }
      });
  }
  deleteProperty(name: string) {
    delete this.model[name]
  }
}
