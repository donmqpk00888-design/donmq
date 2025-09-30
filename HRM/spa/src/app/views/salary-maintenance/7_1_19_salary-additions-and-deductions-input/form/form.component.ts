import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { EmployeeCommonInfo } from '@models/common';
import { SalaryAdditionsAndDeductionsInputDto } from '@models/salary-maintenance/7_1_19_salary-additions-and-deductions-input';
import { S_7_1_19_SalaryAdditionsAndDeductionsInputService } from '@services/salary-maintenance/s_7_1_19_salary-additions-and-deductions-input.service';
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
  listAddDedType: KeyValuePair[] = [];
  listAddDedItem: KeyValuePair[] = [];
  listCurrency: KeyValuePair[] = [];
  title: string = '';
  url: string = '';
  action: string = '';
  classButton = ClassButton;
  placeholder = Placeholder
  data: SalaryAdditionsAndDeductionsInputDto = <SalaryAdditionsAndDeductionsInputDto>{};
  isEdit: boolean = false;
  updateBy: string = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).id;
  iconButton = IconButton;
  pagination: Pagination = <Pagination>{};
  employeeList: EmployeeCommonInfo[] = [];
  constructor(
    private service: S_7_1_19_SalaryAdditionsAndDeductionsInputService,
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropdownList();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.action = res.title;
      this.getSource()
    });
  }
  getSource() {
    this.isEdit = this.action == 'Edit'
    if (this.isEdit) {
      let source = this.service.paramSource();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.data = structuredClone(source.selectedData)
      } else this.back()
    }
    this.loadDropdownList()
  }
  loadDropdownList() {
    this.getListFactory()
    this.getListAddDedType();
    this.getListAddDedItem();
    this.getListCurrency();
    this.getListEmployee()
  }

  back = () => this.router.navigate([this.url]);

  deleteProperty = (name: string) => delete this.data[name]

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res
      },
    });
  }

  getListAddDedType() {
    this.service.getListAddDedType().subscribe({
      next: (res) => {
        this.listAddDedType = res
      },
    });
  }

  getListCurrency() {
    this.service.getListCurrency().subscribe({
      next: (res) => {
        this.listCurrency = res
      },
    });
  }

  getListAddDedItem() {
    this.service.getListAddDedItem().subscribe({
      next: (res) => {
        this.listAddDedItem = res
      },
    });
  }

  save(isNext: boolean) {
    if (this.data.amount > 2147483647)
      return this.functionUtility.snotifySuccessError(false, "Amount must be a valid integer!");
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

  clear() {
    this.data = <SalaryAdditionsAndDeductionsInputDto>{};
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
  onFactoryChange() {
    this.getListEmployee()
    this.deleteProperty('employee_ID')
    this.clearEmpInfo()
    this.onDataChange()
  }
  onChangeSalMonth() {
    this.data.sal_Month_Str = this.functionUtility.isValidDate(this.data.sal_Month)
      ? this.functionUtility.getDateFormat(this.data.sal_Month)
      : "";
    this.onDataChange()
  }
  onDataChange() {
    this.data.update_By = this.updateBy;
    this.data.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date());
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
      this.onDataChange()
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
}
