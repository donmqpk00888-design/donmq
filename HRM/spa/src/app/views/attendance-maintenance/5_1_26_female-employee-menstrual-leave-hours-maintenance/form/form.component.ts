import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { FemaleEmpMenstrualMain } from '@models/attendance-maintenance/5_1_26_female-employee-menstrual-leave-hours-maintenance';
import { EmployeeCommonInfo } from '@models/common';
import { S_5_1_26_FemaleEmployeeMenstrualLeaveHoursMaintenanceService } from '@services/attendance-maintenance/s_5_1_26_female-employee-menstrual-leave-hours-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';

  iconButton = IconButton;
  classButton = ClassButton;
  employeeList: EmployeeCommonInfo[] = [];
  factories: KeyValuePair[] = [];

  model: FemaleEmpMenstrualMain = <FemaleEmpMenstrualMain>{}

  minBreakDate: Date = null;
  maxBreakDate: Date = null;

  constructor(private _service: S_5_1_26_FemaleEmployeeMenstrualLeaveHoursMaintenanceService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getListEmployee();
    });
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.getListFactory();
  }

  //#region Methods
  back = () => this.router.navigate([this.url]);

  onChangeFactory() {
    this.getListEmployee()
    this.deleteProperty('employee_ID')
    this.clearEmpInfo()
  }

  onTypehead(isKeyPress: boolean = false) {
    if (isKeyPress)
      return this.clearEmpInfo()
    if (this.model.employee_ID.length > 9) {
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
  onDateChange(name: string) {
    this.model[`${name}_Str`] = this.functionUtility.isValidDate(new Date(this.model[name]))
      ? this.functionUtility.getDateFormat(new Date(this.model[name]))
      : '';
    if (name == 'att_Month') {
      this.deleteProperty('breaks_Date')
      this.deleteProperty('breaks_Date_Str')
      this.minBreakDate = this.model.att_Month.toDate().toFirstDateOfMonth();
      this.maxBreakDate = this.model.att_Month.toDate().toLastDateOfMonth();
    }
  }
  getListEmployee() {
    if (this.model.factory) {
      this.commonService.getListEmployeeAdd(this.model.factory).subscribe({
        next: res => {
          this.employeeList = res
          this.setEmployeeInfo();
        }
      })
    }
  }
  getListFactory() {
    this._service.getListFactoryAdd().subscribe({
      next: (res) => {
        this.factories = res;
      }
    });
  }
  private setEmployeeInfo() {
    if (!this.model.factory || !this.model.employee_ID)
      return this.clearEmpInfo()
    const emp = this.employeeList.find(x => x.factory == this.model.factory && x.employee_ID == this.model.employee_ID)
    if (emp) {
      this.model.useR_GUID = emp.useR_GUID;
      this.model.local_Full_Name = emp.local_Full_Name;
      this.model.department_Code = emp.actual_Department_Code;
      this.model.department_Name = emp.actual_Department_Name;
      this.model.department_Code_Name = emp.actual_Department_Code_Name;
      this.model.onboard_Date_Str = emp.onboard_Date_Str;
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
    this.deleteProperty('onboard_Date_Str')
  }
  //#endregion

  //#region SAVECHANGE
  save(isContinues: boolean = false) {
    this.spinnerService.show();
    this._service.add(this.model).subscribe({
      next: (res: OperationResult) => {
        this.spinnerService.hide();
        this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.CreateOKMsg' : res.error ?? 'System.Message.CreateErrorMsg');
        if (res.isSuccess) {
          if (isContinues)
            this.clearModel()
          else this.back();
        }
      }
    })
  };
  clearModel() {
    this.model = <FemaleEmpMenstrualMain>{}
    this.minBreakDate = null;
    this.maxBreakDate = null;
    this.employeeList = []
  }
  //#endregion
  deleteProperty = (name: string) => delete this.model[name]
}
