import { AfterViewInit, Component, EventEmitter, input, OnDestroy, ViewChild } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { ContractManagementDto, Personal, PersonalParam, ProbationParam } from '@models/employee-maintenance/4_1_15_contract-management';
import { S_4_1_15_ContractManagementService } from '@services/employee-maintenance/s_4_1_15_contract-management.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss']
})
export class ModalComponent extends InjectBase implements AfterViewInit, OnDestroy {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  isSave: boolean = false;

  backEmitter: EventEmitter<{ division: string, factory: string, isSuccess: boolean }> = new EventEmitter();
  iconButton = IconButton;
  isEdit: boolean = false;
  selectedKey: string = 'Y'
  personalParam: PersonalParam = <PersonalParam>{};
  data: ContractManagementDto = <ContractManagementDto>{}
  title: string = '';
  action: string = '';
  minDate: Date = new Date(2000, 0, 1);
  minDateContract: Date = null
  minDateProbation: Date = null
  person: Personal = <Personal>{}
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM/DD'
  };
  division: KeyValuePair[] = []
  factory: KeyValuePair[] = []
  contractType: KeyValuePair[] = []
  assessmentResult: KeyValuePair[] = []
  employeeID: KeyValuePair[] = []
  key: KeyValuePair[] = [
    { key: 'Y', value: 'Y', },
    { key: 'N', value: 'N' }
  ];
  probation: ProbationParam = <ProbationParam>{}
  constructor(
    private service: S_4_1_15_ContractManagementService,
    private modalService: ModalService
  ) {
    super();
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave, division: this.data.division, factory: this.data.factory })

  open(data: any): void {
    const source = structuredClone(data);
    this.data = source.data as ContractManagementDto
    this.isEdit = source.isEdit
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.getDataFromSource()
    this.getListDivision()
    this.getListFactory()
    this.getListContractType()
    this.getListAssessmentResult()
    this.isSave = false
    this.directive.show()
  }
  save() {
    this.checkDate()
    this.isSave = true
    if (!this.isEdit) {
      this.spinnerService.show();
      this.service.create(this.data).subscribe({
        next: result => {
          this.spinnerService.hide()
          this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? 'System.Message.CreateOKMsg' : result.error)
          if (result.isSuccess) this.directive.hide();
        }
      })
    }
    else {
      this.spinnerService.show();
      this.service.update(this.data).subscribe({
        next: result => {
          this.spinnerService.hide()
          this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? 'System.Message.UpdateOKMsg' : result.error)
          if (result.isSuccess) this.directive.hide();
        }
      })
    }
  }
  close() {
    this.isSave = false
    this.directive.hide()
  }
  getDataFromSource() {
    if (!this.isEdit) {
      this.action = 'System.Action.Add'
      this.getEmployeeID()
    } else {
      this.action = 'System.Action.Edit'
      this.selectedKey = this.data.effective_Status ? "Y" : "N"
      this.getProbationDate(false)
      this.getPerson()
    }
  }

  getContractEnd() {
    this.minDateContract = this.data.contract_Start != null ? this.data.contract_Start.toDate() : null;
    if (this.person.nationality == "TW")
      this.data.contract_End = new Date(9999, 11, 31).toStringDate()
    else if (!this.functionUtility.checkEmpty(this.data.contract_Start) && Object.keys(this.probation).length > 0)
      this.data.contract_End = new Date(this.data.contract_Start.toDate().getFullYear() + this.probation.probationary_Year, this.data.contract_Start.toDate().getMonth() + this.probation.probationary_Month, this.data.contract_Start.toDate().getDate() + this.probation.probationary_Day).toStringDate()
    else
      this.deleteProperty('contract_End')
  }
  getProbationEnd() {
    this.minDateProbation = this.data.probation_Start != null ? this.data.probation_Start.toDate() : null
    if (!this.functionUtility.checkEmpty(this.data.probation_Start) && Object.keys(this.probation).length > 0)
      this.data.probation_End = new Date(this.data.probation_Start.toDate().getFullYear() + this.probation.probationary_Year, this.data.probation_Start.toDate().getMonth() + this.probation.probationary_Month, this.data.probation_Start.toDate().getDate() + this.probation.probationary_Day).toStringDate()
    else
      this.deleteProperty('probation_End')
  }

  clear() {
    this.modalService.onHide.emit({ isSave: this.isSave, division: this.data.division, factory: this.data.factory })
    this.data = <ContractManagementDto>{
      seq: 0,
      effective_Status: true
    }
  }

  checkDate() {
    this.data.contract_Start = this.functionUtility.getDateFormat(new Date(this.data.contract_Start))
    this.data.contract_End = this.functionUtility.getDateFormat(new Date(this.data.contract_End))
    this.data.probation_Start = !this.functionUtility.checkEmpty(this.data.probation_Start) ? this.functionUtility.getDateFormat(new Date(this.data.probation_Start)) : ''
    this.data.probation_End = !this.functionUtility.checkEmpty(this.data.probation_End) ? this.functionUtility.getDateFormat(new Date(this.data.probation_End)) : ''
    this.data.extend_to = !this.functionUtility.checkEmpty(this.data.extend_to) ? this.functionUtility.getDateFormat(new Date(this.data.extend_to)) : ''
    this.data.effective_Status = this.selectedKey == "Y"
    this.data.update_Time = new Date();
  }

  saveAndContinue() {
    this.checkDate()
    this.spinnerService.show();
    this.service.create(this.data).subscribe({
      next: result => {
        this.spinnerService.hide()
        this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? 'System.Message.CreateOKMsg' : result.error)
        if (result.isSuccess) this.clear()
      }
    })
  }


  validateEmpty() {
    return this.functionUtility.checkEmpty(this.data.local_Full_Name)
      || this.functionUtility.checkEmpty(this.data.contract_Type)
      || this.functionUtility.checkEmpty(this.data.contract_Start)
      || this.functionUtility.checkEmpty(this.data.contract_End)
      || this.data.contract_End == 'Invalid Date'
      || this.data.contract_Start == 'Invalid Date'
      || this.data.probation_Start == 'Invalid Date'
      || this.data.probation_End == 'Invalid Date'
      || this.data.extend_to == 'Invalid Date'
  }

  getProbationDate(isCheck: boolean) {
    if (this.functionUtility.checkEmpty(this.data.contract_Type))
      this.probation = <ProbationParam>{}
    else {
      this.spinnerService.show()
      this.service.getProbationDate(this.data.division, this.data.factory, this.data.contract_Type).subscribe({
        next: res => {
          this.spinnerService.hide()
          this.probation = res
          if (this.probation.probationary_Period == false) {
            this.deleteProperty('probation_Start')
            this.deleteProperty('probation_End')
            this.deleteProperty('assessment_Result')
            this.deleteProperty('extend_to')
            this.deleteProperty('reason')
          }
          if (isCheck) {
            if (!this.functionUtility.checkEmpty(this.data.contract_Start))
              this.getContractEnd()
            if (!this.functionUtility.checkEmpty(this.data.probation_Start))
              this.getProbationEnd()
          }
        }
      })
    }
  }

  changeDivision() {
    this.deleteProperty('factory')
    this.getListFactory()
    this.changePerson()
  }
  changePerson() {
    this.deleteProperty('local_Full_Name')
    this.data.onboard_Date = null
    this.getPerson()
    this.getListContractType()
    this.deleteProperty('contract_Type')
  }

  onTypehead(e: TypeaheadMatch) {
    if (e.value.length > 9)
      return this.functionUtility.snotifySuccessError(false, 'System.Message.InvalidEmployeeIDLength')
  }

  getPerson() {
    if (this.data.employee_ID?.length <= 9) {
      this.personalParam.division = this.data.division
      this.personalParam.factory = this.data.factory
      this.personalParam.employeeID = this.data.employee_ID
      this.service.getPerson(this.personalParam).subscribe({
        next: res => {
          if (res != null) {
            this.person = res
            this.data.local_Full_Name = this.person.local_Full_Name
            this.data.onboard_Date = this.person.onboard_Date
            this.data.department = this.person.department
            if (!this.isEdit)
              this.person.nationality == "TW" ? this.data.contract_End = new Date(9999, 11, 31).toStringDate() : this.data.contract_Start != '' ? this.getContractEnd() : this.data.contract_End = ''
            if (this.functionUtility.checkEmpty(this.data.employee_ID))
              this.data.seq = 0
            if (this.person.seq != null && !this.isEdit) {
              let existingSeqs = this.person.seq.map(item => item).sort((a, b) => a - b);
              let newSeq: number = 1
              existingSeqs.forEach(x => x === newSeq ? newSeq++ : x);
              this.data.seq = newSeq
            }
          }
          else {
            this.deleteProperty('local_Full_Name')
            this.deleteProperty('onboard_Date')
            this.deleteProperty('department')
            this.data.seq = 0
          }
        }
      })
    }
  }

  getListDivision() {
    this.service.getListDivision().subscribe({
      next: res => {
        this.division = res
      }
    })
  }
  getListFactory() {
    this.service.getListFactory(this.data.division).subscribe({
      next: res => {
        this.factory = res
      }
    })
  }
  getListContractType() {
    this.service.getListContractType(this.data.division, this.data.factory).subscribe({
      next: res => {
        this.contractType = res
      }
    })
  }
  getListAssessmentResult() {
    this.service.getListAssessmentResult().subscribe({
      next: res => {
        this.assessmentResult = res
      }
    })
  }
  getEmployeeID() {
    this.service.getEmployeeID().subscribe({
      next: res => {
        this.employeeID = res
      }
    })
  }

  onContractStart() {
    const contractStartDate = new Date(this.data.contract_Start);
    if (contractStartDate < this.minDate) {
      this.data.contract_Start = this.functionUtility.getDateFormat(this.minDate);
    }
    this.getContractEnd()
  }
  onContractEnd() {
    const contractEndDate = new Date(this.data.contract_End);
    if (!this.functionUtility.checkEmpty(this.data.contract_Start) && contractEndDate < new Date(this.data.contract_Start))
      this.data.contract_End = this.functionUtility.getDateFormat(this.data.contract_Start.toDate());
  }
  onProbationStart() {
    const probationStartDate = new Date(this.data.probation_Start);
    if (probationStartDate < this.minDate) {
      this.data.probation_Start = this.functionUtility.getDateFormat(this.minDate);
    }
    this.getProbationEnd()
  }
  onProbationEnd() {
    const probationEndDate = new Date(this.data.probation_End);
    if (!this.functionUtility.checkEmpty(this.data.probation_Start) && probationEndDate < new Date(this.data.probation_Start))
      this.data.probation_End = this.functionUtility.getDateFormat(this.data.probation_Start.toDate());
  }
  onExtendTo() {
    const extendTo = new Date(this.data.extend_to);
    if (extendTo < this.minDate) {
      this.data.extend_to = this.functionUtility.getDateFormat(this.minDate);
    }
  }
  deleteProperty(name: string) {
    delete this.data[name]
  }
}
