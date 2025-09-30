import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { EmployeeCommonInfo } from '@models/common';
import { ApplySocialInsuranceBenefitsMaintenanceDto } from '@models/compulsory-insurance-management/6_1_3_apply_social_insurance_benefits_maintenance';
import { S_6_1_3_ApplySocialInsuranceBenefitsMaintenanceService } from '@services/compulsory-insurance-management/s_6_1_3_apply_social_insurance_benefits_maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.css'],
})
export class FormComponent extends InjectBase implements OnInit {
  listFactory: KeyValuePair[] = [];
  listBenefitsKind: KeyValuePair[] = [];
  title: string = '';
  url: string = '';
  action: string = '';
  classButton = ClassButton;
  data: ApplySocialInsuranceBenefitsMaintenanceDto = <ApplySocialInsuranceBenefitsMaintenanceDto>{};
  isEdit: boolean = false;
  updateBy: string = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).id;
  iconButton = IconButton;
  dataTypeaHead: string[];
  validDate: boolean = false;
  employeeList: EmployeeCommonInfo[] = [];
  constructor(
    private service: S_6_1_3_ApplySocialInsuranceBenefitsMaintenanceService
  ) {
    super();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
      this.getListFactory();
      this.getListBenefitsKind();
    });
  }
  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (res) => {
        this.isEdit = res.title == 'Edit';
        this.action = res.title;
        this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
        this.getListFactory();
        this.getListBenefitsKind();
        this.getSource()
      })
  }
  getSource() {
    if (this.isEdit) {
      let source = this.service.paramSource();
      this.validDate = true
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.data = structuredClone(source.selectedData);
        this.data.benefits_End = new Date(this.data.benefits_End_Str);
        this.data.benefits_Start = new Date(this.data.benefits_Start_Str);
        this.data.declaration_Month = new Date(this.data.declaration_Month_Str);
        this.data.update_Time = new Date(this.data.update_Time_Str);
        if (this.data.birthday_Child_Str)
          this.data.birthday_Child = new Date(this.data.birthday_Child_Str);
      }
      else
        this.back()
    }
    this.data.is_Edit = this.isEdit
  }

  back = () => this.router.navigate([this.url]);

  deleteProperty = (name: string) => delete this.data[name];

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  getListBenefitsKind() {
    this.service.getListBenefitsKind().subscribe({
      next: (res) => {
        this.listBenefitsKind = res;
      },
    });
  }
  save(isNext: boolean) {
    const observable = this.isEdit
      ? this.service.update(this.data)
      : this.service.create(this.data);
    this.spinnerService.show();
    observable.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.spinnerService.hide()
        if (result.isSuccess) {
          this.functionUtility.snotifySuccessError(
            result.isSuccess,
            result.error
          );
          isNext ? this.data = <ApplySocialInsuranceBenefitsMaintenanceDto>{} : this.back();
        } else {
          this.functionUtility.snotifySuccessError(
            result.isSuccess,
            result.error
          );
        }
      }
    });
  }

  getListEmployee() {
    if (this.data.factory) {
      this.commonService.getListEmployeeAdd(this.data.factory).subscribe({
        next: res => this.employeeList = res
      })
    }
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
      this.onValueChange();
    }
  }

  onChangeFactory() {
    this.getListEmployee()
    this.deleteProperty('employee_ID')
    this.clearAdditionData()
    this.clearEmpInfo()
  }

  onChangeKind() {
    this.checkDate()
    this.onValueChange();
  }

  onChangeSeq(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.value === '0') {
      input.value = '';
    }
    this.onValueChange();
  }

  onChangeDate(name: string) {
    this.data[`${name}_Str`] = this.functionUtility.isValidDate(this.data[name])
      ? this.functionUtility.getDateFormat(new Date(this.data[name])) : '';
    if (this.data[`${name}_Str`] == '') {
      switch (name) {
        case 'benefits_Start':
          this.clearCalculation()
          return this.functionUtility.snotifySuccessError(false, 'Date(Start) Invalid date', false);
        case 'benefits_End':
          this.clearCalculation()
          return this.functionUtility.snotifySuccessError(false, 'Date(End) Invalid date', false);
        default: break
      }
    }
    if (name != 'birthday_Child') this.checkDate()
    this.onValueChange();
  }

  onValueChange() {
    this.data.update_By = this.updateBy
    this.data.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date());
  }

  setEmployeeInfo() {
    if (!this.data.factory || !this.data.employee_ID)
      return this.clearEmpInfo()
    const emp = this.employeeList.find(x => x.factory == this.data.factory && x.employee_ID == this.data.employee_ID)
    if (emp) {
      this.data.useR_GUID = emp.useR_GUID;
      this.data.local_Full_Name = emp.local_Full_Name;
      this.data.work_Type = emp.work_Type;
      this.getAdditionData()
    }
    else {
      this.clearEmpInfo()
      this.functionUtility.snotifySuccessError(false, "Employee ID not exists")
    }
  }

  clearEmpInfo() {
    this.deleteProperty('local_Full_Name')
    this.deleteProperty('useR_GUID')
    this.deleteProperty('work_Type')
  }

  getAdditionData() {
    if (!this.data.factory || !this.data.employee_ID || !this.data.work_Type)
      this.clearAdditionData()
    else
      this.service.getAdditionData(this.data).subscribe({
        next: (res) => {
          if (res.isSuccess) {
            this.data.compulsory_Insurance_Number = res.data.compulsory_Insurance_Number;
            this.data.special_Work_Type = res.data.special_Work_Type;
            this.checkDate()
          } else {
            this.functionUtility.snotifySuccessError(res.isSuccess, res.error);
            this.clearAdditionData();
          }
        },
        error: () => { this.clearAdditionData(); },
      });
  }

  clearAdditionData() {
    this.deleteProperty('compulsory_Insurance_Number')
    this.deleteProperty('special_Work_Type')
  }

  checkDate() {
    if (
      !this.data.factory || !this.data.employee_ID || !this.data.declaration_Month_Str ||
      !this.data.benefits_Kind || !this.data.benefits_Start_Str || !this.data.benefits_End_Str
    )
      this.clearCalculation()
    else {
      this.spinnerService.show();
      this.service.checkDate(this.data).subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            this.validDate = true
            this.calculate();
          } else {
            this.functionUtility.snotifySuccessError(res.isSuccess, res.error, false)
            this.clearCalculation()
          }
        },
        error: () => { this.clearCalculation() }
      });
    }
  }

  calculate() {
    this.spinnerService.show();
    this.service.formula(this.data).subscribe({
      next: (res) => {
        this.spinnerService.hide()
        if (res.isSuccess) {
          this.data.annual_Accumulated_Days = res?.data.annual_Accumulated_Days;
          this.data.amt = res?.data.amt;
          this.data.total_Days = res?.data.total_Days;
        } else {
          this.clearCalculation()
          this.functionUtility.snotifySuccessError(res.isSuccess, res.error);
        }
      },
      error: () => { this.clearCalculation() }
    });
  }
  clearCalculation() {
    this.validDate = false
    this.deleteProperty('annual_Accumulated_Days');
    this.deleteProperty('amt');
    this.deleteProperty('total_Days');
  }

}
