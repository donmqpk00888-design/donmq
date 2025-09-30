import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { NavigationExtras } from '@angular/router';
import { InjectBase } from '@utilities/inject-base-app';
import { ClassButton, IconButton } from '@constants/common.constants';
import { KeyValuePair } from '@utilities/key-value-pair';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { UserForLogged } from '@models/auth/auth';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { Observable, Observer, map, mergeMap, tap } from 'rxjs';
import { ShiftManagementProgram_Main, TypeheadKeyValue } from '@models/attendance-maintenance/5_1_10_shift-management-program';
import { S_5_1_10_ShiftManagementProgram } from '@services/attendance-maintenance/s_5_1_10_shift-management-program.service';
import { TypeaheadMatch } from 'ngx-bootstrap/typeahead';
import { OperationResult } from '@utilities/operation-result';
import { TabComponentModel } from '@views/_shared/tab-component/tab.component';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-form',
  templateUrl: './form.component.html',
  styleUrls: ['./form.component.scss']
})
export class FormComponent extends InjectBase implements OnInit {
  @ViewChild('employeeTab', { static: true }) employeeTab: TemplateRef<any>;
  @ViewChild('departmentTab', { static: true }) departmentTab: TemplateRef<any>;
  tabs: TabComponentModel[] = [];
  title: string = ''
  selectedTab: string = '';
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{};

  user: UserForLogged = JSON.parse((localStorage.getItem(LocalStorageConstants.USER)));

  iconButton = IconButton;
  classButton = ClassButton;

  dataEmployee: ShiftManagementProgram_Main = <ShiftManagementProgram_Main>{}
  dataDepartment: ShiftManagementProgram_Main = <ShiftManagementProgram_Main>{}
  subData: ShiftManagementProgram_Main[] = []
  tempData: ShiftManagementProgram_Main = <ShiftManagementProgram_Main>{}

  action: string = '';
  url: string = '';

  divisionList_Department: KeyValuePair[] = [];
  divisionList_Empployee: KeyValuePair[] = [];
  factoryList_Department: KeyValuePair[] = [];
  factoryList_Empployee: KeyValuePair[] = [];
  workShiftTypeNewList_Department: KeyValuePair[] = [];
  workShiftTypeNewList_Empployee: KeyValuePair[] = [];
  workShiftTypeOldList_Department: KeyValuePair[] = [];
  workShiftTypeOldList_Empployee: KeyValuePair[] = [];
  departmentList_Department: KeyValuePair[] = [];
  departmentList_Empployee: KeyValuePair[] = [];
  effectiveStateList: KeyValuePair[] = [
    { key: true, value: 'Yes' },
    { key: false, value: 'No' }
  ]
  employeeList$: Observable<TypeheadKeyValue[]>;

  constructor(
    private service: S_5_1_10_ShiftManagementProgram
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.initTab();
      this.retryGetDropDownList()
      this.getDepartment()
    });
    const navigation = this.router.getCurrentNavigation();
    const value = navigation?.extras.state?.value as NavigationExtras;
    this.selectedTab = 'employee';
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
    this.url = this.functionUtility.getRootUrl(this.router.routerState.snapshot.url);
    this.bsConfig = Object.assign(
      {},
      {
        isAnimated: true,
        dateInputFormat: 'YYYY/MM/DD',
      }
    );
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(
      (role) => {
        this.action = role.title;
        this.initTab();
        this.filterList(role.dataResolved)
      })

    this.service.paramForm.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((res) => {
      if (this.action == 'Edit') {
        if (res == null)
          this.back()
        else {
          if (this.selectedTab == 'employee') {
            this.dataEmployee = res
            this.dataEmployee.department = res.department
            this.tempData = Object.assign({}, this.dataEmployee);
          }
          else {
            this.dataDepartment = res
            this.tempData = Object.assign({}, this.dataDepartment)
          }
        }
      }
    })
    this.retryGetDropDownList()
    this.getDepartment()
    this.setTypehead()
  }

  initTab(){
    this.tabs = [
      {
        id: 'employee',
        title: this.translateService.instant('AttendanceMaintenance.ShiftManagementProgram.ByEmployee'),
        isEnable: true,
        content: this.employeeTab
      },
      {
        id: 'department',
        title: this.translateService.instant('AttendanceMaintenance.ShiftManagementProgram.ByDepartment'),
        isEnable: this.action != 'Edit',
        content: this.departmentTab
      },
    ]
  }

  checkEmpty() {
    if(this.functionUtility.checkEmpty(this.dataDepartment.division) ||
      this.functionUtility.checkEmpty(this.dataDepartment.factory) ||
      this.functionUtility.checkEmpty(this.dataDepartment.department))
      return true
    return false
  }
  getEmployeeDetail() {
    if (!this.checkEmpty()) {
      this.spinnerService.show();
      let currentDate = new Date();
      this.service.getEmployeeDetail(this.dataDepartment.division, this.dataDepartment.factory, this.dataDepartment.department)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            this.subData = res
            this.subData.map(x => {
              x.division = this.dataDepartment.division;
              x.factory = this.dataDepartment.factory;
              x.work_Shift_Type_New = this.dataDepartment.work_Shift_Type_New;
              x.effective_Date = this.dataDepartment.effective_Date;
              x.update_By = this.user.id;
              x.update_Time = currentDate;
              x.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date(x.update_Time))
            })
          }
        });
    } else
      this.subData = []
  }
  // #region Typehead
  setTypehead(index?: number) {
    this.employeeList$ = new Observable((observer: Observer<any>) => {
      observer.next({
        division: this.selectedTab == 'employee' ? this.dataEmployee.division : this.dataDepartment.division,
        factory: this.selectedTab == 'employee' ? this.dataEmployee.factory : this.dataDepartment.factory ,
        employee_Id: this.selectedTab == 'employee' ? this.dataEmployee.employee_Id : this.subData[index].employee_Id
      });
    }).pipe(mergeMap((_param: any) =>
      this.service.getEmployeeList(_param.division, _param.factory, _param.employee_Id)
        .pipe(
          map((data: TypeheadKeyValue[]) => data || []),
          tap(res => {
            if(this.selectedTab == 'employee'){
              this.dataEmployee.local_Full_Name = res.length == 1 && _param.employee_Id == res[0].key ? res[0].name : null
              this.dataEmployee.department = res.length == 1 && _param.employee_Id == res[0].key ? res[0].department : null
              this.dataEmployee.work_Shift_Type_Old = res.length == 1 && _param.employee_Id == res[0].key ? res[0].work_Shift_Type_Old : null
              this.dataEmployee.useR_GUID = res.length == 1 && _param.employee_Id == res[0].key ? res[0].useR_GUID : null
            } else {
              this.subData[index].local_Full_Name = res.length == 1 && _param.employee_Id == res[0].key ? res[0].name : null
              this.subData[index].department = res.length == 1 && _param.employee_Id == res[0].key ? res[0].department : null
              this.subData[index].work_Shift_Type_Old = res.length == 1 && _param.employee_Id == res[0].key ? res[0].work_Shift_Type_Old : null
              this.subData[index].useR_GUID = res.length == 1 && _param.employee_Id == res[0].key ? res[0].useR_GUID : null
            }

          })
        ))
    );
  }
  onTypehead(e: TypeaheadMatch, index?: number): void {
    if (e.value.length > 9)
      return this.snotifyService.error(
        this.translateService.instant(`System.Message.InvalidEmployeeIDLength`),
        this.translateService.instant('System.Caption.Error'));
    if(this.selectedTab == 'employee'){
      this.dataEmployee.local_Full_Name = e.item.name
      this.dataEmployee.department = e.item.department
      this.dataEmployee.work_Shift_Type_Old = e.item.work_Shift_Type_Old
      this.dataEmployee.useR_GUID = e.item.useR_GUID
    } else {
      this.subData[index].local_Full_Name = e.item.name
      this.subData[index].department = e.item.department
      this.subData[index].work_Shift_Type_Old = e.item.work_Shift_Type_Old
      this.subData[index].useR_GUID = e.item.useR_GUID
    }
  }
  // #endregion

  // #region Dropdown List
  retryGetDropDownList(tabID?: string) {
    const selectedTab = tabID ? tabID : this.selectedTab
    this.spinnerService.show()
    this.service.getDropDownList(selectedTab == 'employee' ? this.dataEmployee.division : this.dataDepartment.division)
      .subscribe({
        next: (res) => {
          this.filterList(res, selectedTab)
          this.spinnerService.hide()
        }
      });
  }
  filterList(keys: KeyValuePair[], tabID?: string) {
    const selectedTab = tabID ? tabID : this.selectedTab
    const divisionList = structuredClone(keys.filter((x: { key: string; }) => x.key == "DI")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    const factoryList = structuredClone(keys.filter((x: { key: string; }) => x.key == "FA")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    const workShiftTypeList = structuredClone(keys.filter((x: { key: string; }) => x.key == "WO")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    selectedTab == 'employee'
      ? (this.factoryList_Empployee = factoryList, this.divisionList_Empployee = divisionList)
      : (this.factoryList_Department = factoryList, this.divisionList_Department = divisionList)
    this.workShiftTypeNewList_Empployee = workShiftTypeList
    this.workShiftTypeOldList_Empployee = workShiftTypeList
  }
  getWorkShiftTypeDepartment(tabID?: string) {
    const selectedTab = tabID ? tabID : this.selectedTab
    if (selectedTab == 'department'
      && !this.functionUtility.checkEmpty(this.dataDepartment.division)
      && !this.functionUtility.checkEmpty(this.dataDepartment.factory)
      && !this.functionUtility.checkEmpty(this.dataDepartment.department)) {
      this.spinnerService.show();
      this.service.getWorkShiftTypeDepartmentList(this.dataDepartment.division, this.dataDepartment.factory, this.dataDepartment.department)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            this.workShiftTypeOldList_Department = res.filter((x: { key: string; }) => x.key == "O").map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
            this.workShiftTypeNewList_Department = res.filter((x: { key: string; }) => x.key == "N").map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
          }
        });
    }
  }
  getDepartment(onFactoryChange?: boolean, tabID?: string) {
    const selectedTab = tabID ? tabID : this.selectedTab
    if ((selectedTab == 'employee' && !this.functionUtility.checkEmpty(this.dataEmployee.division) && !this.functionUtility.checkEmpty(this.dataEmployee.factory))
      || (selectedTab == 'department' && !this.functionUtility.checkEmpty(this.dataDepartment.division) && !this.functionUtility.checkEmpty(this.dataDepartment.factory))) {
      this.spinnerService.show();
      this.service
        .getDepartmentList(
          selectedTab == 'employee' ? this.dataEmployee.division : this.dataDepartment.division,
          selectedTab == 'employee' ? this.dataEmployee.factory : this.dataDepartment.factory
        )
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            selectedTab == 'employee' ? this.departmentList_Empployee = res : this.departmentList_Department = res
            if (onFactoryChange)
              this.deleteProperty('department', selectedTab)
          }
        });
    }

  }
  // #endregion

  // #region On Change Functions
  onDivisionChange() {
    this.deleteProperty('factory')
    this.deleteProperty('employee_Id')
    this.deleteProperty('local_Full_Name')
    this.deleteProperty('emp_Department')
    this.deleteProperty('department')
    this.deleteProperty('useR_GUID')
    this.deleteProperty('work_Shift_Type_Old')
    this.deleteProperty('work_Shift_Type_New')
    this.retryGetDropDownList()
    this.getWorkShiftTypeDepartment()
    this.subData = []
  }
  onFactoryChange() {
    this.deleteProperty('employee_Id')
    this.deleteProperty('local_Full_Name')
    this.deleteProperty('emp_Department')
    this.deleteProperty('department')
    this.deleteProperty('useR_GUID')
    this.deleteProperty('work_Shift_Type_Old')
    this.deleteProperty('work_Shift_Type_New')
    this.getWorkShiftTypeDepartment()
    this.getDepartment(true)
    this.editData()
    this.subData = []
  }
  onDepartmentChange() {
    this.deleteProperty('work_Shift_Type_Old')
    this.deleteProperty('work_Shift_Type_New')
    this.getWorkShiftTypeDepartment()
    this.editData()
    this.getEmployeeDetail();
  }
  async onEmployeeChange() {
    if (this.functionUtility.checkEmpty(this.dataEmployee.employee_Id)) {
      this.deleteProperty('local_Full_Name')
      this.deleteProperty('emp_Department')
      this.deleteProperty('work_Shift_Type_Old')
      this.deleteProperty('useR_GUID')
    }
    if (!this.functionUtility.checkEmpty(this.dataEmployee.division)
      && !this.functionUtility.checkEmpty(this.dataEmployee.factory)
      && !this.functionUtility.checkEmpty(this.dataEmployee.employee_Id)
      && !this.functionUtility.checkEmpty(this.dataEmployee.effective_Date_Str)) {
      const isExisted = await this.checkDataExisted()
      if (isExisted) {
        this.deleteProperty('employee_Id')
        this.deleteProperty('local_Full_Name')
        this.deleteProperty('emp_Department')
        this.deleteProperty('work_Shift_Type_Old')
        this.deleteProperty('useR_GUID')
        this.snotifyService.clear()
        this.snotifyService.error(
          this.translateService.instant('AttendanceMaintenance.ShiftManagementProgram.AlreadyExitedData'),
          this.translateService.instant('System.Caption.Error')
        );
      }
    }
    this.editData()
    this.checkDuplicate()
  }
  isDuplicate: boolean

  checkDuplicate() {
    const employeeIDs: Set<string> = new Set();

    this.subData.map((item) => {
      if (employeeIDs.has(item.employee_Id)) {
        item.is_Duplicate = true;
      } else {
        employeeIDs.add(item.employee_Id);
        item.is_Duplicate = false;
      }
    });

    const duplicateEmps = new Set<string>();
    this.subData.map((item) => {
      if (item.is_Duplicate) {
        duplicateEmps.add(item.employee_Id);
      }
    });

    this.subData.map((item) => {
      if (duplicateEmps.has(item.employee_Id)) {
        item.is_Duplicate = true;
      }
    });

    this.isDuplicate = duplicateEmps.size > 0;
    if (this.isDuplicate == true) {
      this.snotifyService.clear();
      this.functionUtility.snotifySuccessError(false, 'SalaryMaintenance.IncomeTaxFreeSetting.DuplicateInput');
    }
  }
  async onDateChange(name: string) {
    this.selectedTab == 'employee'
      ? this.dataEmployee[`${name}_Str`] = this.dataEmployee[name] ? this.functionUtility.getDateFormat(new Date(this.dataEmployee[name])) : ''
      : this.dataDepartment[`${name}_Str`] = this.dataDepartment[name] ? this.functionUtility.getDateFormat(new Date(this.dataDepartment[name])) : '';
    const isExisted = await this.checkDataExisted()
    if (this.action == 'Add') {
      if (!isExisted) {
        this.selectedTab == 'employee' ?
          this.dataEmployee.effective_State = false :
          this.dataDepartment.effective_State = false
      }
      else {
        this.deleteProperty(name)
        this.deleteProperty(`${name}_Str`)
        this.snotifyService.error(
          this.translateService.instant('AttendanceMaintenance.ShiftManagementProgram.AlreadyExitedData'),
          this.translateService.instant('System.Caption.Error')
        );
      }
    }
    else {
      if (this.selectedTab == 'employee') {
        if (this.dataEmployee.effective_Date_Str != this.tempData.effective_Date_Str) {
          if (!isExisted)
            this.dataEmployee.effective_State = false
          else {
            this.deleteProperty(name)
            this.deleteProperty(`${name}_Str`)
            this.snotifyService.error(
              this.translateService.instant('AttendanceMaintenance.ShiftManagementProgram.AlreadyExitedData'),
              this.translateService.instant('System.Caption.Error')
            );
          }
        }
        else this.dataEmployee.effective_State = false
      }
      else this.dataDepartment.effective_State = false
    }
    this.editData()
    this.getEmployeeDetail();
  }
  checkDataExisted() {
    return new Promise((resolve) => {
      this.spinnerService.show()
      this.service.isExistedData(this.selectedTab == 'employee' ? this.dataEmployee : this.dataDepartment)
        .subscribe({
          next: (res) => {
            this.spinnerService.hide();
            resolve(res.isSuccess)
          }
        });
    })
  }
  editData() {
    if (this.selectedTab == 'employee') {
      this.dataEmployee.update_By = this.user.id
      this.dataEmployee.update_Time = new Date
      this.dataEmployee.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date(this.dataEmployee.update_Time))
    }
    else {
      this.dataDepartment.update_By = this.user.id
      this.dataDepartment.update_Time = new Date
      this.dataDepartment.update_Time_Str = this.functionUtility.getDateTimeFormat(new Date(this.dataDepartment.update_Time))
    }
  }
  deleteProperty(name: string, tabID?: string) {
    const selectedTab = tabID ? tabID : this.selectedTab
    selectedTab == 'employee'
      ? delete this.dataEmployee[name]
      : delete this.dataDepartment[name]
    this.editData()
  }
  // #endregion

  // #region Tabs Resolve

  changeTab(event: any) {
    this.retryGetDropDownList(event)
    this.getWorkShiftTypeDepartment(event)
    this.getDepartment(false, event)
  }
  // #endregion

  // #region Save & Back
  save(isBack: boolean) {
    this.spinnerService.show()
    const callback: Partial<Observer<OperationResult>> | ((value: OperationResult) => void) = {
      next: (res) => {
        this.spinnerService.hide()
        if (res.isSuccess) {
          isBack ? this.back()
            : (this.dataEmployee = <ShiftManagementProgram_Main>{
              division: this.dataEmployee.division,
              factory: this.dataEmployee.factory
            }, this.dataDepartment = <ShiftManagementProgram_Main>{})
          this.snotifyService.success(
            this.translateService.instant(this.action == 'Add' ? 'System.Message.CreateOKMsg' : 'System.Message.UpdateOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
        } else {
          this.snotifyService.error(
            this.translateService.instant(`AttendanceMaintenance.ShiftManagementProgram.${res.error}`),
            this.translateService.instant('System.Caption.Error'));
        }
      }
    }
    this.action == 'Add'
      ? this.selectedTab == 'employee'
        ? this.service.postDataEmployee(this.dataEmployee).subscribe(callback)
        : this.service.postDataDepartment(this.subData).subscribe(callback)
      : this.service.putDataEmployee(this.dataEmployee, this.tempData).subscribe(callback)
  }

  remove(index: number) {
    this.subData.splice(index, 1)
  }

  back = () => this.router.navigate([this.url]);
  // #endregion
}
