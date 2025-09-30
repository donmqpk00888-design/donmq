import { AfterViewInit, Component, input, OnDestroy, ViewChild } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { HRMS_Emp_Unpaid_LeaveDto, HRMS_Emp_Unpaid_LeaveModel } from '@models/employee-maintenance/4_1_11_unpaid-leave';
import { S_4_1_11_UnpaidLeaveService } from '@services/employee-maintenance/s_4_1_11_unpaid-leave.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { BsModalRef, ModalDirective } from 'ngx-bootstrap/modal';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { Observable } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss']
})
export class ModalComponent extends InjectBase implements AfterViewInit, OnDestroy {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  isSave: boolean = false;

  title: string = '';
  iconButton = IconButton;
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listLeaveReason: KeyValuePair[] = [];
  employee_ID: string[] = []
  data: HRMS_Emp_Unpaid_LeaveDto = <HRMS_Emp_Unpaid_LeaveDto>{};
  model: HRMS_Emp_Unpaid_LeaveModel= <HRMS_Emp_Unpaid_LeaveModel>{}
  isEdit: boolean = false;
  warningDisplayed: boolean = false;
  minMode: BsDatepickerViewMode = 'day';
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM/DD',
    minMode: this.minMode,
  };
  leaveStartDate: Date;
  leaveEndDate: Date;

  continuation_of_Insurance = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' }
  ];
  seniority_Retention = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' }
  ];
  annual_Leave_Seniority_Retention = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' }
  ];
  effective_Status = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' }
  ];

  constructor(
    private service: S_4_1_11_UnpaidLeaveService,
    public modal: BsModalRef,
    private modalService: ModalService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.getListDivision();
      this.getListFactory();
      this.getListLeaveReason();
      this.getDataEmployee();
    });
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave, isEdit: this.isEdit})

  open(data: any): void {
    const source = structuredClone(data);
    this.model = source.model as HRMS_Emp_Unpaid_LeaveModel
    this.isEdit = source.isEdit
    if (this.model) {
      this.data = { ...this.model }
      this.leaveStartDate = this.model.leaveStartDate ? new Date(this.model.leaveStartDate) : null
      this.leaveEndDate = this.model.leaveEndDate ? new Date(this.model.leaveEndDate) : null
    }
    this.getListDivision();
    if (this.data.division)
      this.getListFactory();
    this.getListLeaveReason();
    this.getEmployeeID();
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.isSave = false
    this.directive.show()
  }
  save() {
    this.spinnerService.show();
    const leaveStart = this.leaveStartDate ? this.leaveStartDate.toStringDateTime() : null;
    const leaveEnd = this.leaveEndDate ? this.leaveEndDate.toStringDateTime() : null;
    this.data.leave_Start = leaveStart;
    this.data.leave_End = leaveEnd;
    this.isSave = true
    const observable = this.isEdit ? this.service.edit(this.data) : this.service.addNew(this.data);
    observable.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.spinnerService.hide();
        const message = this.isEdit ? 'System.Message.UpdateOKMsg' : 'System.Message.CreateOKMsg';
        this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? message : result.error)
        if (result.isSuccess) this.directive.hide();
      }
    });
  }
  close() {
    this.isSave = false
    this.directive.hide()
  }

  getListDivision() {
    this.getListData('listDivision', this.service.getListDivision.bind(this.service));
  }

  getListFactory() {
    this.service.getListFactory(this.data.division).subscribe({
      next: (res) => {
        this.listFactory = res;
      }
    });
  }

  getListLeaveReason() {
    this.getListData('listLeaveReason', this.service.getListLeaveReason.bind(this.service));
  }

  getOrdinalNumber() {
    this.service.getSeq(this.data).subscribe({
      next: (res) => {
        this.data.ordinal_Number = res.isSuccess ? res.data : null;
      }
    });
  }

  getListData(dataProperty: string, serviceMethod: () => Observable<any[]>): void {
    this.spinnerService.show();
    serviceMethod().subscribe({
      next: (res) => {
        this[dataProperty] = res;
        this.spinnerService.hide();
      }
    });
  }

  onTypehead(e: TypeaheadMatch) {
    if (e.value.length > 9)
      return this.functionUtility.snotifySuccessError(false, `System.Message.InvalidEmployeeIDLength`)
  }

  getEmployeeID() {
    this.service.getEmployeeID().subscribe({
      next: (res) => {
        this.employee_ID = res
      }
    })
  }

  getDataEmployee(): void {
    if (this.data.factory && this.data.employee_ID) {
      this.service.getEmployeeData(this.data.factory, this.data.employee_ID).subscribe({
        next: (res) => {
          if (res && res.length > 0) {
            this.data.department = res[0].department;
            this.data.local_Full_Name = res[0].local_Full_Name;
            this.data.onboard_Date = res[0].onboard_Date;
          }
        }
      });
    }
  }

  onDivisionChange() {
    this.deleteProperty('factory')
    this.listFactory = [];
    this.resetParam();
    if (!this.functionUtility.checkEmpty(this.data.division))
      this.getListFactory();
    else
      this.listFactory = [];
  }

  onChange() {
    this.resetParam();
    if (this.data.factory && this.data.employee_ID && this.data.employee_ID.length <= 9) {
      this.getDataEmployee();
      this.getOrdinalNumber();
    }
  }

  resetParam() {
    this.data.local_Full_Name = this.data.department = this.data.onboard_Date = '';
    this.data.ordinal_Number = null;
  }

  checkEmpty() {
    return [
      this.data.division, this.data.factory, this.data.employee_ID, this.data.local_Full_Name,
      this.data.department, this.data.onboard_Date, this.data.ordinal_Number, this.data.leave_Reason,
      this.leaveStartDate, this.leaveEndDate
    ].some(val => this.functionUtility.checkEmpty(val));
  }

  onDateInputKeyDown(event: KeyboardEvent, fieldName: string) {
    if (event.key === 'Backspace' || event.key === 'Delete') {
      this[fieldName] = null;
      event.preventDefault();
    }
  }

  checkSelected(): boolean {
    return [
      this.data.continuation_of_Insurance, this.data.seniority_Retention,
      this.data.annual_Leave_Seniority_Retention, this.data.effective_Status
    ].every(val => val != null);
  }

  deleteProperty(name: string) {
    delete this.data[name]
  }
}
