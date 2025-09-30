import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { EmployeeCommonInfo } from '@models/common';
import { BankAccountMaintenanceDto } from '@models/salary-maintenance/7_1_4_bank_account_maintenance';
import { S_7_1_4_Bank_Account_MaintenanceService } from '@services/salary-maintenance/s_7_1_4_bank_account_maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.css']
})
export class FormComponent extends InjectBase implements OnInit {
  listFactory: KeyValuePair[] = [];
  title: string = '';
  tempUrl: string = '';
  formType: string = '';
  classButton = ClassButton;
  placeholder = Placeholder
  data: BankAccountMaintenanceDto = <BankAccountMaintenanceDto>{ bank_Code: "0000000" };
  isEdit: boolean = false;
  updateBy: string = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).id;
  iconButton = IconButton;
  pagination: Pagination = <Pagination>{};
  create_Date_Value: Date;
  employeeList: EmployeeCommonInfo[] = [];
  constructor(
    private service: S_7_1_4_Bank_Account_MaintenanceService,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory()
      this.getListEmployee()
    });
  }

  ngOnInit() {
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
      let source = this.service.paramSource();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.data = structuredClone(source.selectedData);
        if (this.functionUtility.isValidDate(new Date(this.data.create_Date)))
          this.create_Date_Value = new Date(this.data.create_Date);
      }
      else this.back();
    }
    this.getListFactory();
  }
  back = () => this.router.navigate([this.tempUrl]);

  deleteProperty = (name: string) => delete this.data[name]

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res
      },
    });
  }

  save(isNext: boolean) {
    const observable = this.isEdit ? this.service.update(this.data) : this.service.create(this.data);
    this.spinnerService.show();
    observable.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.spinnerService.hide();
        if (result.isSuccess) {
          this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
          isNext ? this.clear() : this.back();
        } else {
          this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
        }
      },
    });
  }
  onDateChange() {
    this.data.create_Date = this.functionUtility.isValidDate(this.create_Date_Value) ? this.functionUtility.getDateFormat(this.create_Date_Value) : '';
    this.onValueChange()
  }
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

  setEmployeeInfo() {
    if (!this.data.factory || !this.data.employee_ID)
      return this.clearEmpInfo()
    const emp = this.employeeList.find(x => x.factory == this.data.factory && x.employee_ID == this.data.employee_ID)
    if (emp) {
      this.data.useR_GUID = emp.useR_GUID;
      this.data.local_Full_Name = emp.local_Full_Name;
      this.onValueChange()
    }
    else {
      this.clearEmpInfo()
      this.functionUtility.snotifySuccessError(false, "Employee ID not exists")
    }
  }

  clearEmpInfo() {
    this.deleteProperty('useR_GUID')
    this.deleteProperty('local_Full_Name')
  }

  clear() {
    this.data = <BankAccountMaintenanceDto>{ factory: this.data.factory, bank_Code: "0000000" };
    this.create_Date_Value = null;
  }
  onValueChange() {
    this.data.update_By = this.updateBy
    this.data.update_Time = new Date().toStringDateTime()
  }
}
