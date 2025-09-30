import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { EmployeeCommonInfo } from '@models/common';
import { CompulsoryInsuranceDataMaintenanceDto } from '@models/compulsory-insurance-management/6_1_1_compulsory_insurance_data_maintenance';
import { S_6_1_1_Compulsory_Insurance_Data_MaintenanceService } from '@services/compulsory-insurance-management/s_6_1_1_compulsory_insurance_data_maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.css']
})
export class FormComponent extends InjectBase implements OnInit {
  listFactory: KeyValuePair[] = [];
  listInsuranceType: KeyValuePair[] = [];
  title: string = '';
  url: string = '';
  action: string = '';
  classButton = ClassButton;
  data: CompulsoryInsuranceDataMaintenanceDto = <CompulsoryInsuranceDataMaintenanceDto>{};
  isEdit: boolean = false;
  updateBy: string = JSON.parse(localStorage.getItem(LocalStorageConstants.USER)).id;
  iconButton = IconButton;
  insurance_Start_Date: Date;
  insurance_End_Date: Date;
  employeeList: EmployeeCommonInfo[] = [];
  constructor(
    private service: S_6_1_1_Compulsory_Insurance_Data_MaintenanceService,
  ) {
    super();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getListInsuranceType();
    });

    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.isEdit = res.title !== 'Add';
      this.action = res.title

      if (this.isEdit) {
        this.getDataFromSource();
      } else {
        this.data.update_By = this.updateBy;
        this.data.update_Time = new Date().toStringDateTime();
      }
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.getListFactory();
    this.getListInsuranceType();
  }

  getDataFromSource() {
    effect(() => {
      let source = this.service.paramSource();
      if (source.data != null) {
        this.data = source.data
        this.data.update_By = this.updateBy;
        if (!this.functionUtility.checkEmpty(this.data.insurance_Start))
          this.insurance_Start_Date = new Date(this.data.insurance_Start);
        if (!this.functionUtility.checkEmpty(this.data.insurance_End))
          this.insurance_End_Date = new Date(this.data.insurance_End);
      }
      else this.back();
    })
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

  getListInsuranceType() {
    this.service.getListInsuranceType().subscribe({
      next: (res) => {
        this.listInsuranceType = res
      },
    });
  }

  save(isNext: boolean) {
    this.data.insurance_Start = this.functionUtility.isValidDate(this.insurance_Start_Date)
      ? this.functionUtility.getDateFormat(this.insurance_Start_Date) : null;
    this.data.insurance_End = this.functionUtility.isValidDate(this.insurance_End_Date)
      ? this.functionUtility.getDateFormat(this.insurance_End_Date) : null;

    const observable = this.isEdit ? this.service.update(this.data) : this.service.create(this.data);
    this.spinnerService.show();
    observable.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.spinnerService.hide();
        if (result.isSuccess) {
          this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
          isNext ? this.setData() : this.back();
        } else {
          this.functionUtility.snotifySuccessError(result.isSuccess, result.error)
        }
      },

    });
  }

  setData() {
    this.data = <CompulsoryInsuranceDataMaintenanceDto>{
      factory: this.data.factory,
      update_By: this.updateBy,
      update_Time: new Date().toStringDateTime(),
    };
    this.insurance_Start_Date = null;
    this.insurance_End_Date = null;
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
    }
  }

  private setEmployeeInfo() {
    if (!this.data.factory || !this.data.employee_ID)
      return this.clearEmpInfo()
    const emp = this.employeeList.find(x => x.factory == this.data.factory && x.employee_ID == this.data.employee_ID)
    if (emp) {
      this.data.useR_GUID = emp.useR_GUID;
      this.data.local_Full_Name = emp.local_Full_Name;
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

  onChangeFactory() {
    this.getListEmployee()
    this.deleteProperty('employee_ID')
    this.clearEmpInfo()
  }
  onChangeDate() {
    this.data.insurance_Start = this.insurance_Start_Date == null ? null : this.functionUtility.getDateTimeFormat(this.insurance_Start_Date);
    this.data.insurance_End = this.insurance_End_Date == null ? null : this.functionUtility.getDateTimeFormat(this.insurance_End_Date);
    this.onValueChange()
  }
  onValueChange() {
    this.data.update_Time = this.functionUtility.getDateTimeFormat(new Date())
  }
}
