import { Component, effect, Input, OnInit } from '@angular/core';
import { IconButton, ClassButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { EmployeeCommonInfo } from '@models/common';
import { HRMS_Sal_Childcare_SubsidyDto, ListofChildcareSubsidyRecipientsMaintenanceParam } from '@models/salary-maintenance/7_1_7_list-of-child-care-subsidy-recipients-maintenance';
import { S_7_1_7_ListofChildcareSubsidyRecipientsMaintenanceService } from '@services/salary-maintenance/s_7_1_7_list-of-child-care-subsidy-recipients-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { Observable } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  @Input('title') formType?: string;
  isEdit: boolean = false;
  listFactory: KeyValuePair[] = [];
  YearMonth_Start: Date = null;
  YearMonth_End: Date = null;
  bsConfigBirthday: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };
  bsConfigYearMonth: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: "YYYY/MM",
    minMode: "month"
  };
  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
  };
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;

  data: HRMS_Sal_Childcare_SubsidyDto = <HRMS_Sal_Childcare_SubsidyDto>{ num_Children: 0 };
  item: HRMS_Sal_Childcare_SubsidyDto;
  title: string = '';
  tempUrl: string = ''
  employeeList: EmployeeCommonInfo[] = [];
  // #region constructor
  constructor(private service: S_7_1_7_ListofChildcareSubsidyRecipientsMaintenanceService) {
    super();
    this.getSource();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.getListFactory();
        this.getListEmployee();
      });
  }
  getSource() {
    this.isEdit = this.formType == 'Edit'
    if (this.isEdit) {
      let source = this.service.paramSearch();
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.data = structuredClone(source.selectedData);
        this.data.employee_ID_Old = this.data.employee_ID;
      } else this.back()
    }
    this.getListFactory();
  }

  // #region ngOnInit
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.tempUrl = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.getSource()
  }

  // #region getListFactory
  getListFactory() {
    this.commonService.getListAccountAdd().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  onFactoryChange() {
    this.getListEmployee();
    this.deleteProperty('employee_ID')
    this.clearEmpInfo()
  }

  // #region area typehead getEmployeeID
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
      this.onUpdateTimeChangeAlways()
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

  back = () => this.router.navigate([this.tempUrl]);

  // #region save
  save(isNext?: boolean) {
    let action: Observable<OperationResult>
    this.data.birthday_Child = this.data.birthday_Child.toUTCDate()
    this.data.month_Start = this.data.month_Start.toUTCDate()
    this.data.month_End = this.data.month_End.toUTCDate()
    if (this.isEdit) {
      action = this.service.edit(this.data)
    } else
      action = this.service.addNew(this.data)
    this.spinnerService.show();
    action.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.spinnerService.hide()
        if (result.isSuccess) {
          const message = this.isEdit ? 'System.Message.UpdateOKMsg' : 'System.Message.CreateOKMsg';
          this.functionUtility.snotifySuccessError(true, message)
          isNext ? this.data = <HRMS_Sal_Childcare_SubsidyDto>{ factory: this.data.factory, num_Children: 0 } : this.back();
        } else {
          this.functionUtility.snotifySuccessError(false, result.error)
        }
      }
    })
  }

  onUpdateTimeChangeAlways() {
    this.data.update_By = this.user.id
    this.data.update_Time = this.functionUtility.getDateTimeFormat(new Date())
  }
  deleteProperty(name: string) {
    delete this.data[name]
  }
}

