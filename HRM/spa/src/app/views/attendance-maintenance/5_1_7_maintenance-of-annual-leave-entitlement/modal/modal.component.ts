import { AfterViewInit, Component, EventEmitter, input, OnDestroy, ViewChild } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { CaptionConstants } from '@constants/message.enum';
import { MaintenanceOfAnnualLeaveEntitlement } from '@models/attendance-maintenance/5_1_7_maintenance_of_annual_leave_entitlement';
import { EmployeeCommonInfo } from '@models/common';
import { S_5_1_7_MaintenanceOfAnnualLeaveEntitlementService } from '@services/attendance-maintenance/s_5_1_7_maintenance-of-annual-leave-entitlement.service';
import { ModalService } from '@services/modal.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss'],
})
export class ModalComponent extends InjectBase implements AfterViewInit, OnDestroy {
  @ViewChild('modal', { static: false }) directive: ModalDirective;
  id = input<string>(this.modalService.defaultModal)
  isSave: boolean = false;

  title: string = '';
  minMode: BsDatepickerViewMode = 'day';
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM/DD',
    minMode: this.minMode
  };

  model: MaintenanceOfAnnualLeaveEntitlement
  emitter: EventEmitter<boolean> = new EventEmitter();
  isEdit: boolean = false;

  dataMain: MaintenanceOfAnnualLeaveEntitlement = <MaintenanceOfAnnualLeaveEntitlement>{}
  employee_ID: string[] = []

  iconButton = IconButton;
  classButton = ClassButton;

  factoryList: KeyValuePair[] = [];
  departmentList: KeyValuePair[] = [];
  leaveCodeList: KeyValuePair[] = [];
  employeeList: EmployeeCommonInfo[] = [];

  constructor(
    private _service: S_5_1_7_MaintenanceOfAnnualLeaveEntitlementService,
    private modalService: ModalService
  ) {
    super();
  }
  ngAfterViewInit(): void { this.modalService.add(this); }
  ngOnDestroy(): void { this.modalService.remove(this.id()); }

  onHide = () => this.modalService.onHide.emit({ isSave: this.isSave })

  open(data: any): void {
    const source = structuredClone(data);
    this.model = source.model as MaintenanceOfAnnualLeaveEntitlement
    this.isEdit = source.isEdit
    if (this.model)
      this.dataMain = { ...this.model }
    this.getListFactory();
    this.getListLeaveCode();
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.isSave = false
    this.directive.show()
  }

  save(isContinue: boolean = false) {
    this.spinnerService.show();
    this.isSave = true
    const observable = this.isEdit ? this._service.edit(this.dataMain) : this._service.add(this.dataMain);
    observable.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.spinnerService.hide();
        const message = this.isEdit ? 'System.Message.UpdateOKMsg' : 'System.Message.CreateOKMsg';
        this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? message : result.error)
        if (result.isSuccess) {
          isContinue ? this.clear() : this.directive.hide();
        }
      }
    });
  }
  clear() {
    this.modalService.onHide.emit({ isSave: this.isSave })
    this.dataMain = <MaintenanceOfAnnualLeaveEntitlement>{};
  }
  close() {
    this.isSave = false
    this.directive.hide()
  }

  //#region getInfo
  getListFactory() {
    this._service.getListFactory()
      .subscribe({
        next: (res) => {
          this.factoryList = res
        }
      });
  }

  getListLeaveCode() {
    this._service.getListLeaveCode()
      .subscribe({
        next: (res) => {
          this.leaveCodeList = res;
        }
      });
  }

  getListEmployee() {
    if (this.dataMain.factory) {
      this.commonService.getListEmployeeAdd(this.dataMain.factory).subscribe({
        next: res => {
          this.employeeList = res
          this.setEmployeeInfo();
        }
      })
    }
  }
  private setEmployeeInfo() {
    if (!this.dataMain.factory || !this.dataMain.employee_ID)
      return this.clearEmpInfo()
    const emp = this.employeeList.find(x => x.factory == this.dataMain.factory && x.employee_ID == this.dataMain.employee_ID)
    if (emp) {
      if (['A', 'S'].indexOf(emp.employment_Status) != -1)
        this.snotifyService.warning('Assigned/Supported Employee', CaptionConstants.WARNING);
      this.dataMain.useR_GUID = emp.useR_GUID;
      this.dataMain.local_Full_Name = emp.local_Full_Name;
      this.dataMain.department_Code = emp.actual_Department_Code;
      this.dataMain.department_Name = emp.actual_Department_Name;
      this.dataMain.department_Code_Name = emp.actual_Department_Code_Name;
    }
    else {
      this.clearEmpInfo()
      this.functionUtility.snotifySuccessError(false, "Employee ID not exists")
    }
  }
  clearEmpInfo() {
    this.deleteProperty('useR_GUID')
    this.deleteProperty('department_Code')
    this.deleteProperty('department_Name')
    this.deleteProperty('department_Code_Name')
    this.deleteProperty('local_Full_Name')
  }
  //#endregion

  //#region onChange
  onFactoryChange() {
    this.getListEmployee();
    this.deleteProperty('employee_ID')
    this.clearEmpInfo();
  }

  onTypehead(isKeyPress: boolean = false) {
    if (isKeyPress)
      return this.clearEmpInfo()
    if (this.dataMain.employee_ID.length > 9) {
      this.clearEmpInfo()
      this.snotifyService.error(
        this.translateService.instant(`System.Message.InvalidEmployeeIDLength`),
        this.translateService.instant('System.Caption.Error')
      );
    }
    else {
      this.setEmployeeInfo()
    }
  }
  onDateChange() {
    this.dataMain.annual_Start = this.dataMain.annual_Start_Date ? this.functionUtility.getDateFormat(new Date(this.dataMain.annual_Start_Date)) : '';
    this.dataMain.annual_End = this.dataMain.annual_End_Date ? this.functionUtility.getDateFormat(new Date(this.dataMain.annual_End_Date)) : '';
    this.checkAndValidateData();
  }

  onLeaveCodeChange() {
    this.checkAndValidateData();
  }

  changeDayTime() {
    this.dataMain.total_Hours = +this.dataMain.previous_Hours + +this.dataMain.year_Hours;
    this.dataMain.total_Days = this.dataMain.total_Hours / 8;
  }

  checkAndValidateData() {
    if (this.dataMain.employee_ID && this.dataMain.leave_Code && this.dataMain.annual_Start) {
      this._service.checkExistedData(this.dataMain).subscribe(result => {
        if (result.isSuccess) {
          this.functionUtility.snotifySuccessError(false, 'System.Message.DataExisted')
        }
      });
    }
  }
  //#endregion

  deleteProperty(name: string) {
    delete this.dataMain[name]
  }
}
