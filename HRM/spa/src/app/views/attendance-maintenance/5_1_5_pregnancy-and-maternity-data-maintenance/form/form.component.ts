import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { CaptionConstants } from '@constants/message.enum';
import { PregnancyMaternityDetail } from '@models/attendance-maintenance/5_1_5_pregnancy_and_maternity_data';
import { EmployeeCommonInfo } from '@models/common';
import { S_5_1_5_PregnancyAndMaternityDataMaintenanceService } from '@services/attendance-maintenance/s_5_1_5_pregnancy-and-maternity-data-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { OperationResult } from '@utilities/operation-result';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { forkJoin } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrl: './form.component.scss'
})
export class FormComponent extends InjectBase implements OnInit {
  title: string = '';
  url: string = '';
  action: string = '';
  data: PregnancyMaternityDetail = <PregnancyMaternityDetail>{
    pregnancy_Week: '0',
    close_Case: false
  };

  emp: EmployeeCommonInfo = <EmployeeCommonInfo>{};
  validEmp: boolean = false

  bsConfig: Partial<BsDatepickerConfig> = {
    isAnimated: true,
    dateInputFormat: 'YYYY/MM/DD',
    adaptivePosition: true
  };

  factories: KeyValuePair[] = [];
  departments: KeyValuePair[] = [];
  workShiftTypes: KeyValuePair[] = [];
  workTypes: KeyValuePair[] = [];

  iconButton = IconButton;
  classButton = ClassButton;

  workTypeCodes: KeyValuePair[] = [
    { key: 'Y', value: 'AttendanceMaintenance.PregnancyAndMaternityData.Special' },
    { key: 'N', value: 'AttendanceMaintenance.PregnancyAndMaternityData.Regular' }
  ]
  work8hours: KeyValuePair[] = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' }
  ]
  closeTypeCodes: KeyValuePair[] = [
    { key: true, value: 'Y' },
    { key: false, value: 'N' }
  ]

  employeeList: EmployeeCommonInfo[] = [];

  constructor(
    private _service: S_5_1_5_PregnancyAndMaternityDataMaintenanceService
  ) {
    super();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactoryAdd();
      this.getListDepartment();
      this.getListWorkType();
      if (this.action == 'Add')
        this.getListEmployee()
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.setDates();
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(res => {
      this.action = res.title;
      this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
      this.getSource()
    })
  }
  getSource() {
    if (this.action != 'Add') {
      let source = this._service.paramSearch();
      this.validEmp = true
      if (source.selectedData && Object.keys(source.selectedData).length > 0) {
        this.data = structuredClone(source.selectedData);
      }
      else
        this.back()
    } else this.getListEmployee()

    this.getListFactoryAdd();
    this.getListDepartment();
    this.getListWorkType();
  }

  onChangeFactory() {
    this.getListEmployee()
    this.getListDepartment();
    this.getSpecialRegularWorkType()
    this.deleteProperty('employee_ID')
    this.clearEmpInfo()
  }
  onWorkTypeBeforeChange() {
    this.getSpecialRegularWorkType()
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
  onDateChange(name: string) {
    this.data[`${name}_Str`] = this.functionUtility.isValidDate(new Date(this.data[name]))
      ? this.functionUtility.getDateFormat(new Date(this.data[name]))
      : '';
    if (name == 'ultrasound_Date')
      this.changeUltrasoundDate()
    if (name == 'baby_Start')
      this.changeBabyDateStart()
  }
  back = () => this.router.navigate([this.url]);

  setDates() {
    this.data.baby_Start = this.data.baby_Start ? new Date(this.data.baby_Start) : null;
    this.data.baby_End = this.data.baby_End ? new Date(this.data.baby_End) : null;
    this.data.maternity_Start = this.data.maternity_Start ? new Date(this.data.maternity_Start) : null;
    this.data.maternity_End = this.data.maternity_End ? new Date(this.data.maternity_End) : null;
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

  getListFactoryAdd() {
    this._service.getListFactory()
      .subscribe({
        next: (res) => {
          this.factories = res
        }
      });
  }

  getListWorkType() {
    forkJoin({
      workShiftTypes: this._service.getListWorkType(true),
      workTypes: this._service.getListWorkType(false)
    }).subscribe({
      next: (res) => {
        this.workShiftTypes = res.workShiftTypes;
        this.workTypes = res.workTypes;
      }
    });
  }
  getListDepartment() {
    if (this.data.factory) {
      this._service.getListDepartment(this.data.factory)
        .subscribe({
          next: (res) => {
            this.departments = res
          }
        });
    }
  }
  getSpecialRegularWorkType() {
    if (!this.data.factory || !this.data.work_Type_Before)
      return this.deleteProperty('special_Regular_Work_Type')
    this._service.getSpecialRegularWorkType(this.data.factory, this.data.work_Type_Before).subscribe({
      next: res => this.data.special_Regular_Work_Type = res.special_Regular_Work_Type
    })
  }

  changeUltrasoundDate() {
    if (this.data.ultrasound_Date_Str) {
      let work7hourDays = 175 - (+this.data.pregnancy_Week * 7);
      this.data.work7hours = new Date(this.data.ultrasound_Date).addDays(work7hourDays);
      this.data.work7hours_Str = this.functionUtility.getDateFormat(this.data.work7hours)

      let pregnancy36WeekDays = 252 - (+this.data.pregnancy_Week * 7);
      this.data.pregnancy36Weeks = new Date(this.data.ultrasound_Date).addDays(pregnancy36WeekDays);
      this.data.pregnancy36Weeks_Str = this.functionUtility.getDateFormat(this.data.pregnancy36Weeks)

      this.data.work8hours = true;
    }
    else {
      this.deleteProperty('work7hours')
      this.deleteProperty('work7hours_Str')
      this.deleteProperty('pregnancy36Weeks')
      this.deleteProperty('pregnancy36Weeks_Str')
      this.data.work8hours = this.emp.work8hours;
    }
  }

  changeBabyDateStart() {
    if (this.data.baby_Start_Str) {
      this.data.baby_End = new Date(this.data.baby_Start).addDays(-1).addYears(1);
      this.data.baby_End_Str = this.functionUtility.getDateFormat(this.data.baby_End)
    }
  }
  private setEmployeeInfo() {
    if (!this.data.factory || !this.data.employee_ID)
      return this.clearEmpInfo()
    const emp = this.employeeList.find(x => x.factory == this.data.factory && x.employee_ID == this.data.employee_ID)
    if (emp) {
      if (['A', 'S'].indexOf(emp.employment_Status) != -1)
        this.snotifyService.warning('Assigned/Supported Employee', CaptionConstants.WARNING);
      this.data.useR_GUID = emp.useR_GUID;
      this.data.local_Full_Name = emp.local_Full_Name;
      this.data.department_Code = emp.actual_Department_Code;
      this.data.work_Shift_Type = emp.work_Shift_Type;
      this.data.work8hours = emp.work8hours;
      this.data.work_Type_Before = emp.work_Type;
      this.getSpecialRegularWorkType()
      this.emp = structuredClone(emp)
      this.validEmp = true
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
    this.deleteProperty('work_Shift_Type')
    this.deleteProperty('work8hours')
    this.deleteProperty('work_Type_Before')
    this.deleteProperty('special_Regular_Work_Type')
    this.validEmp = false
  }
  disableDetail() {
    return this.action === 'Query';
  }

  disableEditOrDetail() {
    return this.action === 'Edit' || this.action === 'Query';
  }

  save() {
    let success: string = this.action === 'Add' ? 'System.Message.CreateOKMsg' : 'System.Message.UpdateOKMsg';
    let error: string = this.action === 'Add' ? 'System.Message.CreateErrorMsg' : 'System.Message.UpdateErrorMsg';

    this.execute(success, error);
  }

  execute(success: string, error: string) {
    this.spinnerService.show();
    this._service[this.action.toLowerCase()](this.data)
      .subscribe({
        next: (res: OperationResult) => {
          if (res.isSuccess) {
            this.snotifyService.success(
              this.translateService.instant(success),
              this.translateService.instant('System.Caption.Success')
            );
            this.back();
          } else {
            this.snotifyService.error(
              this.translateService.instant(res.error ?? error),
              this.translateService.instant('System.Caption.Error')
            );
          }
          this.spinnerService.hide();
        }
      })
  }
  deleteProperty(name: string) {
    delete this.data[name]
  }
}
