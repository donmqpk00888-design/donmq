import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { EmployeeCommonInfo } from '@models/common';
import { PersonalIncomeTaxNumberMaintenanceDto } from '@models/salary-maintenance/7_1_6_personal-income-tax-number-maintenance';
import { S_7_1_6_PersonalIncomeTaxNumberMaintenanceService } from '@services/salary-maintenance/s_7_1_6_personal-income-tax-number-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  formType: string = '';
  listFactory: KeyValuePair[] = [];
  data: PersonalIncomeTaxNumberMaintenanceDto = <PersonalIncomeTaxNumberMaintenanceDto>{};
  minMode: BsDatepickerViewMode = 'year';
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY',
    minMode: this.minMode
  };
  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder
  isEdit: boolean = false;
  isDuplicate: boolean = false;
  employeeList: EmployeeCommonInfo[] = [];
  status = [
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' }
  ];

  constructor(
    private service: S_7_1_6_PersonalIncomeTaxNumberMaintenanceService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getListEmployee()
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.formType = res.title
      this.getSource()
    });
  }

  getSource() {
    this.isEdit = this.formType == 'Edit'
    if (this.isEdit) {
      let source = this.service.paramSearch();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.data = structuredClone(source.selectedData);
        if (this.functionUtility.isValidDate(new Date(this.data.year)))
          this.data.year_Date = new Date(this.data.year)
      } else this.back()
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

  //#region onChange
  onFactoryChange() {
    this.getListEmployee()
    this.deleteProperty('employee_ID')
    this.clearEmpInfo()
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

  onDateChange() {
    this.data.year = this.functionUtility.isValidDate(this.data.year_Date) ? this.formatDate(this.data.year_Date) : '';
    this.onValueChange()
    this.checkDuplicate()
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

  setEmployeeInfo() {
    if (!this.data.factory || !this.data.employee_ID)
      return this.clearEmpInfo()
    const emp = this.employeeList.find(x => x.factory == this.data.factory && x.employee_ID == this.data.employee_ID)
    if (emp) {
      this.data.useR_GUID = emp.useR_GUID;
      this.data.local_Full_Name = emp.local_Full_Name;
      this.data.department_Code = emp.actual_Department_Code;
      this.data.department_Name = emp.actual_Department_Name;
      this.data.department_Code_Name = emp.actual_Department_Code_Name;
      this.checkDuplicate()
      this.onValueChange()
    }
    else {
      this.clearEmpInfo()
      this.functionUtility.snotifySuccessError(false, "Employee ID not exists")
    }
  }

  checkDuplicate(): boolean | void {
    if (!this.data.factory || !this.data.employee_ID || !this.data.year || this.isEdit)
      return this.isDuplicate = false
    this.service.checkDuplicate(this.data.factory, this.data.employee_ID, this.data.year).subscribe({
      next: (res) => {
        this.isDuplicate = res.isSuccess
        this.snotifyService.clear()
        if (this.isDuplicate)
          this.functionUtility.snotifySuccessError(false, "System.Message.DataExisted")
      },
      error: () => { this.isDuplicate = false }
    });
  }
  clearEmpInfo() {
    this.deleteProperty('useR_GUID')
    this.deleteProperty('department_Code')
    this.deleteProperty('department_Name')
    this.deleteProperty('department_Code_Name')
    this.deleteProperty('local_Full_Name')
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
          isContinue ? this.data = <PersonalIncomeTaxNumberMaintenanceDto>{ factory: this.data.factory } : this.back();
      }
    });
  }
  //#endregion

  back = () => this.router.navigate([this.url]);

  deleteProperty(name: string) {
    delete this.data[name]
  }

  formatDate(date: Date): string {
    return date ? date.getFullYear().toString() : '';
  }

  validateNumber(event: any): boolean {
    const numberRegex = /^[0-9]+$/;
    return numberRegex.test(event.key);
  }
  onValueChange() {
    this.data.update_By = this.user.id
    this.data.update_Time = new Date().toStringDateTime()
  }
}
