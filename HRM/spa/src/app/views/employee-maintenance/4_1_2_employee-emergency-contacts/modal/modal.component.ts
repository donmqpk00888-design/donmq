import { AfterViewInit, Component, ViewChild, input } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { EmployeeEmergencyContactsDto } from '@models/employee-maintenance/4_1_2_employee-emergency-contacts';
import { S_4_1_2_EmployeeEmergencyContactsService } from '@services/employee-maintenance/s_4_1_2_employee-emergency-contacts.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { ModalDirective } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-modal-4-1-2',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.css']
})
export class ModalComponent412 extends InjectBase implements AfterViewInit {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  type: string
  title: string
  model: EmployeeEmergencyContactsDto = <EmployeeEmergencyContactsDto>{};
  relationships: KeyValuePair[] = [];
  iconButton = IconButton;
  isSave: boolean = false;

  constructor(
    private service: S_4_1_2_EmployeeEmergencyContactsService,
    private modalService: ModalService
  ) {
    super();
  }

  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }
  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave })

  open(source: any): void {
    const _source = structuredClone(source);
    this.model = _source.data as EmployeeEmergencyContactsDto;
    this.type = _source.type
    this.model.relationship = this.model.relationship?.split('-')[0].trim();
    this.title = this.functionUtility.getTitle(this.service.functions[0]?.program_Code)
    if (this.type === "Add")
      this.getMaxSeq();
    this.getRelationships();
    this.isSave = false
    this.directive.show()
  }
  save() {
    this.trimStringItem();
    this.isSave = true
    if (this.type === 'Add') {
      if (this.functionUtility.checkEmpty(this.model.useR_GUID))
        return this.snotifyService.error(
          "USER_GUID is NULL. Can’t save data",
          this.translateService.instant('System.Caption.Error')
        );
      if (this.model.seq == 0 || this.model.seq > 99)
        return this.snotifyService.error(
          "系統流水號1~99",
          this.translateService.instant('System.Caption.Error')
        );
    }
    this.spinnerService.show();
    this.service[this.type === 'Add' ? 'create' : 'update'](this.model).subscribe({
      next: result => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(result.isSuccess,
          result.isSuccess ? (this.type === 'Add' ? 'System.Message.CreateOKMsg' : 'System.Message.UpdateOKMsg') :
            result.error ?? (this.type === 'Add' ? "System.Message.CreateErrorMsg" : 'System.Message.UpdateErrorMsg'))
        if (result.isSuccess)
          this.directive.hide()
      }
    })
  }

  close() {
    this.isSave = false
    this.directive.hide()
  }

  getMaxSeq() {
    this.service.getMaxSeq(this.model.useR_GUID).subscribe({
      next: res => this.model.seq = res,
    })
  }

  getRelationships() {
    this.service.getRelationships().subscribe({
      next: res => {
        this.relationships = res;
      }
    })
  }

  back = () => this.router.navigate(['employee-information/employee-emergency-contacts']);

  isDisableSave() {
    return this.functionUtility.checkEmpty(this.model.seq) ||
      this.functionUtility.checkEmpty(this.model.emergency_Contact) ||
      this.functionUtility.checkEmpty(this.model.relationship) ||
      this.functionUtility.checkEmpty(this.model.emergency_Contact_Phone) ||
      this.functionUtility.checkEmpty(this.model.emergency_Contact_Address);
  }

  trimStringItem() {
    this.model.emergency_Contact = this.model.emergency_Contact?.trim();
    this.model.temporary_Address = this.model.temporary_Address?.trim();
    this.model.emergency_Contact_Address = this.model.emergency_Contact_Address?.trim();
  }
  deleteProperty(name: string) {
    delete this.model[name]
  }
}
