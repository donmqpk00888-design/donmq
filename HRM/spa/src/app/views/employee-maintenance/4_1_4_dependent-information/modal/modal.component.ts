import { AfterViewInit, Component, input, ViewChild } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { HRMS_Emp_Dependent } from '@models/employee-maintenance/4_1_4_dependent-information';
import { S_4_1_4_DependentInformationService } from '@services/employee-maintenance/s_4_1_4_dependent-information.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { ModalDirective } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-modal-4-1-4',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss']
})
export class ModalComponent414 extends InjectBase implements AfterViewInit {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  type: string

  isSave: boolean = false;
  title: string = '';
  iconButton = IconButton;

  param: HRMS_Emp_Dependent = <HRMS_Emp_Dependent>{}
  listRelationship: KeyValuePair[] = [];

  constructor(
    private service: S_4_1_4_DependentInformationService,
    private modalService: ModalService
  ) {
    super();
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave })

  open(source: any): void {
    const _source = structuredClone(source);
    this.param = _source.data as HRMS_Emp_Dependent;
    this.type = _source.type
    this.isSave = false
    this.title = this.functionUtility.getTitle(this.service.functions[0]?.program_Code)
    this.getDataFromSource();
    this.getListRelationship();
    this.directive.show()
  }

  save() {
    this.spinnerService.show();
    this.isSave = true
    this.service[this.type == 'Add' ? 'create' : 'edit'](this.param).subscribe({
      next: result => {
        this.spinnerService.hide()
        this.functionUtility.snotifySuccessError(result.isSuccess,
          result.isSuccess
            ? (this.type == 'Add' ? 'System.Message.UpdateOKMsg' : 'System.Message.UpdateOKMsg')
            : result.error,
          result.isSuccess)
        if (result.isSuccess)
          this.directive.hide();
      }
    })
  }

  close() {
    this.isSave = false
    this.directive.hide()
  }

  checkEmpty() {
    if (
      this.functionUtility.checkEmpty(this.param.nationality) ||
      this.functionUtility.checkEmpty(this.param.identification_Number) ||
      this.functionUtility.checkEmpty(this.param.seq) ||
      this.functionUtility.checkEmpty(this.param.name) ||
      this.functionUtility.checkEmpty(this.param.relationship)
    )
      return true
    return false
  }

  getDataFromSource() {
    if (this.type == 'Add')
      this.getSeqMax();
  }
  getSeqMax() {
    this.service.getSeq(this.param).subscribe({
      next: res => {
        this.param.seq = res
      }
    })
  }

  async getListRelationship() {
    this.service.GetListRelationship().subscribe({
      next: res => {
        this.listRelationship = res;
      }
    })
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }
}
