import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { EmployeeCommonInfo } from '@models/common';
import { PayslipDeliveryByEmailMaintenanceDto } from '@models/salary-maintenance/7_1_5_payslip-delivery-by-email-maintenance';
import { S_7_1_5_PayslipDeliveryByEmailMaintenanceService } from '@services/salary-maintenance/s_7_1_5_payslip-delivery-by-email-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  tempUrl: string = '';
  formType: string = '';
  listFactory: KeyValuePair[] = [];
  data: PayslipDeliveryByEmailMaintenanceDto = <PayslipDeliveryByEmailMaintenanceDto>{ status: 'Y' };
  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder
  isEdit: boolean = false;
  isDuplicate: boolean = false;

  employee_ID: string[] = [];
  status = [
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' }
  ];
  employeeList: EmployeeCommonInfo[] = [];
  constructor(
    private service: S_7_1_5_PayslipDeliveryByEmailMaintenanceService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.tempUrl = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.formType = res.title
      this.getSource();
    });
  }

  getSource() {
    this.isEdit = this.formType == 'Edit';
    if (this.isEdit) {
      let source = this.service.paramSearch();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.data = structuredClone(source.selectedData);
      }
      else this.back();
    }
    this.getListFactory();
  }

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      }
    });
  }
  getListEmployee() {
    if (this.data.factory) {
      this.commonService.getListEmployeeAdd(this.data.factory).subscribe({
        next: res => {
          this.employeeList = res
          this.setEmployeeInfo();
        }
      })
    }
  }

  //#region onChange
  onFactoryChange() {
    this.getListEmployee()
    this.deleteProperty('employee_ID')
    this.clearEmpInfo()
  }

  onTypehead(isKeyPress: boolean = false) {
    if (isKeyPress)
      return this.clearEmpInfo()
    if (this.data.employee_ID.length > 9) {
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
  //#endregion

  //#region save
  save(isContinue = false) {
    const observable = this.isEdit ? this.service.edit(this.data) : this.service.add(this.data);
    this.spinnerService.show();
    observable.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.spinnerService.hide();
        const message = this.isEdit ? 'System.Message.UpdateOKMsg' : 'System.Message.CreateOKMsg';
        this.functionUtility.snotifySuccessError(result.isSuccess, result.isSuccess ? message : result.error)
        if (result.isSuccess)
          isContinue ? this.data = <PayslipDeliveryByEmailMaintenanceDto>{ factory: this.data.factory, status: 'Y' } : this.back();
      }
    });
  }
  //#endregion
  setEmployeeInfo() {
    if (!this.data.factory || !this.data.employee_ID)
      return this.clearEmpInfo()
    const emp = this.employeeList.find(x => x.factory == this.data.factory && x.employee_ID == this.data.employee_ID)
    if (emp) {
      this.data.useR_GUID = emp.useR_GUID;
      this.data.local_Full_Name = emp.local_Full_Name;
      this.checkDuplicate()
      this.onValueChange()
    }
    else {
      this.clearEmpInfo()
      this.functionUtility.snotifySuccessError(false, "Employee ID not exists")
    }
  }
  checkDuplicate(): boolean | void {
    if (!this.data.factory || !this.data.employee_ID || this.isEdit)
      return this.isDuplicate = false
    this.service.checkDuplicate(this.data.factory, this.data.employee_ID).subscribe({
      next: (res) => {
        this.isDuplicate = res.isSuccess
        this.snotifyService.clear()
        if (this.isDuplicate) {
          this.functionUtility.snotifySuccessError(false, "System.Message.DataExisted")
        }
      },
      error: () => { this.isDuplicate = false }
    });
  }
  clearEmpInfo() {
    this.deleteProperty('useR_GUID')
    this.deleteProperty('local_Full_Name')
  }
  back = () => this.router.navigate([this.tempUrl]);

  deleteProperty(name: string) {
    delete this.data[name]
  }

  onValueChange() {
    this.data.update_By = this.user.id
    this.data.update_Time = new Date().toStringDateTime()
  }
}
