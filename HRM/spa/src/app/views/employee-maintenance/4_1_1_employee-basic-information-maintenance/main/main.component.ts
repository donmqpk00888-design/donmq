import { Component, effect, OnInit } from '@angular/core';
import {
  ClassButton,
  EmployeeMode,
  IconButton,
} from '@constants/common.constants';
import {
  EmployeeBasicInformationMaintenanceMainMemory,
  EmployeeBasicInformationMaintenanceParam,
  EmployeeBasicInformationMaintenanceSource,
  EmployeeBasicInformationMaintenanceView,
} from '@models/employee-maintenance/4_1_1_employee-basic-information-maintenance';

import { S_4_1_1_EmployeeBasicInformationMaintenanceService } from '@services/employee-maintenance/s_4_1_1_employee-basic-information-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main-4-1-1',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss'],
})
export class MainComponent411 extends InjectBase implements OnInit {
  data: EmployeeBasicInformationMaintenanceView[];
  pagination: Pagination = <Pagination>{};
  title: string;
  mode = EmployeeMode;
  iconButton = IconButton;
  classButton = ClassButton;
  param: EmployeeBasicInformationMaintenanceParam = <EmployeeBasicInformationMaintenanceParam>{  };
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listAssignedFactory: KeyValuePair[] = [];
  listNationality: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listAssignedDepartment: KeyValuePair[] = [];
  listWorkTypeShift: KeyValuePair[] = [];
  listEmploymentStatus: KeyValuePair[] = [
    { key: '', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.All' },
    { key: 'Y', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Onjob' },
    { key: 'N', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Resigned' },
    { key: 'U', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Unpaid' },
  ];
  listCrossFactoryStatus: KeyValuePair[] = [
    { key: '', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.All' },
    { key: 'A', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Assigned' },
    { key: 'S', value: 'EmployeeInformationModule.EmployeeBasicInformationMaintenance.Supported' },
  ];

  constructor(
    private _service: S_4_1_1_EmployeeBasicInformationMaintenanceService,
  ) {
    super();
    this.getDataFromSource();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.loadDropDownList();
        if (
          this.functionUtility.checkFunction('Search') &&
          this.data?.length > 0
        )
          this.getPagination(false);
      });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.loadDropDownList();
  }

  ngOnDestroy(): void {
    this._service.setParamSearch(<EmployeeBasicInformationMaintenanceMainMemory>{
      param: this.param,
      pagination: this.pagination,
      data: this.data,
    });
  }

  loadDropDownList() {
    this.getListDivision();
    this.getListFactory();
    this.getListDepartment();
    this.getListAssignedFactory();
    this.getListAssignedDepartment();
    this.getListNationality();
    this.getListWorkTypeShift();
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getPagination(false);
  }
  getDataFromSource() {
    effect(() => {
      this.param = this._service.paramSearch().param;
      this.pagination = this._service.paramSearch().pagination;
      this.data = this._service.paramSearch().data;
      this.loadDropDownList();
      if (this.data?.length > 0) {
        if (this.functionUtility.checkFunction('Search')) {
          if (this.checkRequiredParams())
            this.getPagination(false);
        }
        else
          this.clear()
      }
    });
  }

  checkRequiredParams(): boolean {
    var result = !this.functionUtility.checkEmpty(this.param.nationality)
    return result;
  }
  disableSearch() {
    return this.functionUtility.checkEmpty(this.param.nationality);
  }
  //#region OnChange
  onChangeDivision() {
    this.deleteProperty('factory')
    this.getListFactory();
    this.onChangeFactory();
  }

  onChangeFactory() {
    this.deleteProperty('department')
    this.getListDepartment();
  }

  onChangeAssignedDivision() {
    this.deleteProperty('assignedFactory')
    this.getListAssignedFactory();
    this.onChangeAssignedFactory();
  }

  onChangeAssignedFactory() {
    this.deleteProperty('assignedDepartment')
    this.getListAssignedDepartment();
  }

  onChangeOnboardDate() {
    if (this.param.onboardDate == 'Invalid Date')
      this.param.onboardDate = '';
  }

  onChangeDateOfGroupEmployment() {
    if (this.param.dateOfGroupEmployment == 'Invalid Date')
      this.param.dateOfGroupEmployment = '';
  }

  onChangeSeniorityStartDate() {
    if (this.param.seniorityStartDate == 'Invalid Date')
      this.param.seniorityStartDate = '';
  }

  onChangeAunualLeaveSeniorityStartDate() {
    if (this.param.annualLeaveSeniorityStartDate == 'Invalid Date')
      this.param.annualLeaveSeniorityStartDate = '';
  }

  onChangeDateOfResignation() {
    if (this.param.onboardDate == 'Invalid Date')
      this.param.onboardDate = '';
  }

  //#endregion
  search() {
    this.pagination.pageNumber === 1
      ? this.getPagination(true)
      : (this.pagination.pageNumber = 1);
  };

  getPagination(isQuery: boolean = true) {
    if (this.disableSearch()) return;
    this.param.onboardDateStr = !this.functionUtility.checkEmpty(this.param.onboardDate)
      ? this.functionUtility.getDateFormat(new Date(this.param.onboardDate))
      : '';
    this.param.dateOfGroupEmploymentStr = !this.functionUtility.checkEmpty(this.param.dateOfGroupEmployment)
      ? this.functionUtility.getDateFormat(new Date(this.param.dateOfGroupEmployment))
      : '';
    this.param.seniorityStartDateStr = !this.functionUtility.checkEmpty(this.param.seniorityStartDate)
      ? this.functionUtility.getDateFormat(new Date(this.param.seniorityStartDate))
      : '';
    this.param.annualLeaveSeniorityStartDateStr = !this.functionUtility.checkEmpty(this.param.annualLeaveSeniorityStartDate)
      ? this.functionUtility.getDateFormat(new Date(this.param.annualLeaveSeniorityStartDate))
      : '';
    this.param.dateOfResignationStr = !this.functionUtility.checkEmpty(this.param.dateOfResignation)
      ? this.functionUtility.getDateFormat(new Date(this.param.dateOfResignation))
      : '';
    this.spinnerService.show();
    this._service.getPagination(this.pagination, this.param).subscribe({
      next: (res) => {
        this.data = res.result;
        this.pagination = res.pagination;
        this.spinnerService.hide();
        if (isQuery)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
      },
    });
  }

  add() {
    let source = <EmployeeBasicInformationMaintenanceSource>{
      mode: this.mode.add,
    };
    this._service.setParamForm(source);
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  upload() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/upload`]);
  }

  edit(item: EmployeeBasicInformationMaintenanceView) {
    let source = <EmployeeBasicInformationMaintenanceSource>{
      mode: this.mode.edit,
      useR_GUID: item.useR_GUID,
      division: item.division,
      factory: item.factory,
      employee_ID: item.employeeID,
      nationality: item.nationality,
      identificationNumber: item.identificationNumber,
      localFullName: item.localFullName,
      employmentStatus: item.employmentStatus
    };
    this._service.setParamForm(source);
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  rehide(item: EmployeeBasicInformationMaintenanceView) {
    let source = <EmployeeBasicInformationMaintenanceSource>{
      mode: this.mode.rehire,
      useR_GUID: item.useR_GUID,
      nationality: item.nationality,
      identificationNumber: item.identificationNumber,
      localFullName: item.localFullName,
    };
    this._service.setParamForm(source);
    this.router.navigate([`${this.router.routerState.snapshot.url}/rehire`]);
  }

  delete(item: EmployeeBasicInformationMaintenanceView) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show();
      this._service.delete(item.useR_GUID).subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.DeleteOKMsg' : res.error)
          if (res.isSuccess) this.getPagination(false);
        }
      });
    }
    );
  }

  query(item: EmployeeBasicInformationMaintenanceView) {
    let source = <EmployeeBasicInformationMaintenanceSource>{
      mode: this.mode.query,
      useR_GUID: item.useR_GUID,
      nationality: item.nationality,
      identificationNumber: item.identificationNumber,
      localFullName: item.localFullName,
      employmentStatus: item.employmentStatus
    };
    this._service.setParamForm(source);
    this.router.navigate([`${this.router.routerState.snapshot.url}/query`]);
  }

  clear() {
    this.param = <EmployeeBasicInformationMaintenanceParam>{
      employmentStatus: '',
      crossFactoryStatus: ''
    };
    this.data = [];
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
    };
  }

  //#region Get List
  getListDivision() {
    this._service.getListDivision().subscribe({
      next: (res) => this.listDivision = res,
    });
  }
  getListFactory() {
    this._service.getListFactory(this.param.division).subscribe({
      next: (res) => this.listFactory = res,
    });
  }

  getListAssignedFactory() {
    this._service
      .getListFactory(this.param.assignedDivision)
      .subscribe({
        next: (res) => this.listAssignedFactory = res,
      });
  }
  getListNationality() {
    this._service.getListNationality().subscribe({
      next: (res) => this.listNationality = res,
    });
  }

  getListWorkTypeShift() {
    this._service
      .getListWorkTypeShift()
      .subscribe({
        next: (res) => this.listWorkTypeShift = res,
      });
  }

  getListDepartment() {
    this._service
      .getListDepartment(this.param.division, this.param.factory)
      .subscribe({
        next: (res) => this.listDepartment = res,
      });
  }

  getListAssignedDepartment() {
    this._service.getListDepartment(this.param.assignedDivision, this.param.assignedFactory).subscribe({
      next: (res) => this.listAssignedDepartment = res,
    });
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
  //#endregion
}
