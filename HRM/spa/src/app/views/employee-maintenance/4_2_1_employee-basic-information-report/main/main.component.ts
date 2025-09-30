import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import {
  ClassButton,
  IconButton
} from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { ValidateResult } from '@models/base-source';

import { EmployeeBasicInformationReportParam } from '@models/employee-maintenance/4_2_1_employee-basic-information-report';
import { S_4_2_1_EmployeeBasicInformationReportService } from '@services/employee-maintenance/s_4_2_1_employee-basic-information-report.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  //#region Commons
  title: string = '';
  programCode: string = '';
  //#endregion

  //#region Variables
  totalRows: number = 0;

  onBoardStart_Date: Date = null;
  onBoardEnd_Date: Date = null;

  dateOfGroupStart_Date: Date = null;
  dateOfGroupEnd_Date: Date = null;

  dateOfResignationStart_Date: Date = null;
  dateOfResignationEnd_Date: Date = null;

  //#endregion

  //#region Objects
  iconButton = IconButton;
  classButton = ClassButton;

  param: EmployeeBasicInformationReportParam = <EmployeeBasicInformationReportParam>{
    employmentStatus: 'all'
  };
  //#endregion

  //#region Arrays
  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listAssignedFactory: KeyValuePair[] = [];
  listNationality: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listAssignedDepartment: KeyValuePair[] = [];
  listPositionGrade: KeyValuePair[] = [];
  listPositionGrade_Start: KeyValuePair[] = [];
  listPositionGrade_End: KeyValuePair[] = [];
  employmentStatus: KeyValuePair[] = [
    { key: 'all', value: 'EmployeeInformationModule.EmployeeBasicInformationReport.Deletion_Code_All' },
    { key: 'Y', value: 'EmployeeInformationModule.EmployeeBasicInformationReport.Deletion_Code_Y' },
    { key: 'N', value: 'EmployeeInformationModule.EmployeeBasicInformationReport.Deletion_Code_N' },
    { key: 'U', value: 'EmployeeInformationModule.EmployeeBasicInformationReport.Deletion_Code_U' },
  ];

  listPermission: KeyValuePair[] = [];
  //#endregion

  //#region Pagination
  //#endregion

  constructor(private _reportService: S_4_2_1_EmployeeBasicInformationReportService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    effect(() => {
      this.param = this._reportService.baseInitSource();

      // Nếu có param load lại dropdown
      if (!this.functionUtility.checkEmpty(this.param.division)) {
        this.getListFactory();
        if (!this.functionUtility.checkEmpty(this.param.factory))
          this.getListDepartment();
      }

      if (!this.functionUtility.checkEmpty(this.param.assignedDivision)) {
        this.getListAssignedFactory();
        if (!this.functionUtility.checkEmpty(this.param.assignedFactory))
          this.getListAssignedDepartment();
      }

      // Load lại dữ liệu ngày
      if (!this.functionUtility.checkEmpty(this.param.onboardDateStartStr)) this.onBoardStart_Date = new Date(this.param.onboardDateStartStr);
      if (!this.functionUtility.checkEmpty(this.param.onboardDateEndStr)) this.onBoardEnd_Date = new Date(this.param.onboardDateEndStr);
      if (!this.functionUtility.checkEmpty(this.param.dateOfGroupEmploymentStart)) this.dateOfGroupStart_Date = new Date(this.param.dateOfGroupEmploymentStart);
      if (!this.functionUtility.checkEmpty(this.param.dateOfGroupEmploymentEnd)) this.dateOfGroupEnd_Date = new Date(this.param.dateOfGroupEmploymentEnd);
      if (!this.functionUtility.checkEmpty(this.param.dateOfResignationStart)) this.dateOfResignationStart_Date = new Date(this.param.dateOfResignationStart);
      if (!this.functionUtility.checkEmpty(this.param.dateOfResignationEnd)) this.dateOfResignationEnd_Date = new Date(this.param.dateOfResignationEnd);

      if (this.functionUtility.checkFunction('Search')) {
        if (this.checkRequiredParams())
          this.getPagination(false);
      }
      else this.clear()
    });

    // Load lại dữ liệu khi thay đổi ngôn ngữ
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList();

      if (!this.functionUtility.checkEmpty(this.param.division)) {
        this.getListFactory();
        if (!this.functionUtility.checkEmpty(this.param.factory))
          this.getListDepartment();
      }

      if (!this.functionUtility.checkEmpty(this.param.assignedDivision)) {
        this.getListAssignedFactory();
        if (!this.functionUtility.checkEmpty(this.param.assignedFactory))
          this.getListAssignedDepartment();
      }
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.loadDropDownList();
  }

  ngOnDestroy(): void {
    this._reportService.setSource(this.param);
  }

  //#region Methods
  disableSearch() {
    return (this.functionUtility.checkEmpty(this.param.division) ||
      this.functionUtility.checkEmpty(this.param.factory))
  }

  loadDropDownList() {
    this.getListNationality();
    this.getListDivision();
    this.getListPermission();
    this.getListPositionGrade();
  }

  validate(): ValidateResult {

    if (this.onBoardStart_Date != null && (this.onBoardStart_Date == undefined || this.onBoardStart_Date.toString() == "Invalid Date") ||
      this.onBoardEnd_Date != null && (this.onBoardEnd_Date == undefined || this.onBoardEnd_Date.toString() == "Invalid Date"))
      return new ValidateResult(`Onboard Date invalid`);

    if (this.dateOfGroupStart_Date != null && (this.dateOfGroupStart_Date == undefined || this.dateOfGroupStart_Date.toString() == "Invalid Date") ||
      this.dateOfGroupEnd_Date != null && (this.dateOfGroupEnd_Date == undefined || this.dateOfGroupEnd_Date.toString() == "Invalid Date"))
      return new ValidateResult(`Date of Group Employment invalid`);

    if (this.dateOfResignationStart_Date != null && (this.dateOfResignationStart_Date == undefined || this.dateOfResignationStart_Date.toString() == "Invalid Date") ||
      this.dateOfResignationEnd_Date != null && (this.dateOfResignationEnd_Date == undefined || this.dateOfResignationEnd_Date.toString() == "Invalid Date"))
      return new ValidateResult(`Date of Resignation invalid`);

    return { isSuccess: true };
  }
  getPagination(isQuery: boolean = true) {
    let checkValidate = this.validate();
    if (checkValidate.isSuccess) {
      if (this.disableSearch()) return;

      // Convert Data
      this.param.onboardDateStartStr = !this.functionUtility.checkEmpty(this.onBoardStart_Date) ? this.functionUtility.getDateFormat(new Date(this.onBoardStart_Date)) : '';
      this.param.onboardDateEndStr = !this.functionUtility.checkEmpty(this.onBoardEnd_Date) ? this.functionUtility.getDateFormat(new Date(this.onBoardEnd_Date)) : '';

      this.param.dateOfGroupEmploymentStart = !this.functionUtility.checkEmpty(this.dateOfGroupStart_Date) ? this.functionUtility.getDateFormat(new Date(this.dateOfGroupStart_Date)) : '';
      this.param.dateOfGroupEmploymentEnd = !this.functionUtility.checkEmpty(this.dateOfGroupEnd_Date) ? this.functionUtility.getDateFormat(new Date(this.dateOfGroupEnd_Date)) : '';


      this.param.dateOfResignationStart = !this.functionUtility.checkEmpty(this.dateOfResignationStart_Date) ? this.functionUtility.getDateFormat(new Date(this.dateOfResignationStart_Date)) : '';
      this.param.dateOfResignationEnd = !this.functionUtility.checkEmpty(this.dateOfResignationEnd_Date) ? this.functionUtility.getDateFormat(new Date(this.dateOfResignationEnd_Date)) : '';

      this.spinnerService.show();
      this._reportService.getPagination(this.param).subscribe({
        next: (result) => {
          this.spinnerService.hide();
          this.totalRows = result.data;
          if (isQuery)
            this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
        }
      });
    }
    else this.snotifyService.warning(checkValidate.message, this.translateService.instant('System.Caption.Warning'));
  }

  getListNationality() {
    this._reportService.getListNationality().subscribe({
      next: (res) => this.listNationality = res
    });
  }

  getListDivision() {
    this._reportService.getListDivision().subscribe({
      next: (res) => this.listDivision = res
    });
  }

  getListPositionGrade() {
    this._reportService.getListPositonGrade().subscribe({
      next: (res) => {
        this.listPositionGrade = res;
        this.filterPositionGrades();
      }
    });
  }

  filterPositionGrades() {
    this.listPositionGrade_Start = this.listPositionGrade;
    this.listPositionGrade_End = this.listPositionGrade;

    if (this.param.positionGrade_Start)
      this.listPositionGrade_End = this.listPositionGrade.filter(item => item.key > this.param.positionGrade_Start);

    if (this.param.positionGrade_End)
      this.listPositionGrade_Start = this.listPositionGrade.filter(item => item.key < this.param.positionGrade_End);
  }

  getListFactory() {
    this._reportService.getListFactory(this.param.division).subscribe({
      next: (res) => this.listFactory = res
    });
  }

  getListAssignedFactory() {
    this._reportService.getListFactory(this.param.assignedDivision).subscribe({
      next: (res) => this.listAssignedFactory = res
    });
  }

  getListDepartment() {
    this._reportService.getListDepartment(this.param.division, this.param.factory).subscribe({
      next: (res) => this.listDepartment = res
    })
  }

  getListAssignedDepartment() {
    this._reportService.getListDepartment(this.param.assignedDivision, this.param.assignedFactory).subscribe({
      next: (res) => this.listAssignedDepartment = res
    });
  }

  getListPermission() {
    this._reportService.getListPermission().subscribe({
      next: (res) => this.listPermission = res
    });
  }

  checkRequiredParams(): boolean {
    var result = (!this.functionUtility.checkEmpty(this.param.nationality) &&
      !this.functionUtility.checkEmpty(this.param.division) &&
      !this.functionUtility.checkEmpty(this.param.factory))
    return result;
  }

  clear() {
    this.param = <EmployeeBasicInformationReportParam>{
      employmentStatus: 'all',
    };

    this.onBoardStart_Date = null;
    this.onBoardEnd_Date = null;
    this.dateOfGroupStart_Date = null;
    this.dateOfGroupEnd_Date = null;
    this.dateOfResignationStart_Date = null;
    this.dateOfResignationEnd_Date = null;

    this.listFactory = [];
    this.listAssignedFactory = [];
    this.listDepartment = []
    this.listAssignedDepartment = []
    this.totalRows = 0;
  }

  deleteProperty = (name: string) => delete this.param[name]

  //#endregion

  //#region Events
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

  onboardDateChange(isStart: boolean = true) {
    if (isStart) {
      if (this.onBoardStart_Date && this.onBoardStart_Date.toString() == 'Invalid Date')
        this.param.onboardDateStartStr = '';
    }
    else {
      if (this.onBoardEnd_Date && this.onBoardEnd_Date.toString() == 'Invalid Date')
        this.param.onboardDateEndStr = '';
    }
  }

  onDateOfGroupEmploymentChange(isStart: boolean = true) {
    if (isStart) {
      if (this.dateOfGroupStart_Date && this.dateOfGroupStart_Date.toString() == 'Invalid Date')
        this.param.dateOfGroupEmploymentStart = '';
    }
    else {
      if (this.dateOfGroupEnd_Date && this.dateOfGroupEnd_Date.toString() == 'Invalid Date')
        this.param.dateOfGroupEmploymentEnd = '';
    }

  }


  onDateOfResignationChange(isStart: boolean = true) {
    if (isStart) {
      if (this.dateOfResignationStart_Date && this.dateOfResignationStart_Date.toString() == 'Invalid Date')
        this.param.dateOfResignationStart = '';
    }
    else {
      if (this.dateOfResignationEnd_Date && this.dateOfResignationEnd_Date.toString() == 'Invalid Date')
        this.param.dateOfResignationEnd = '';
    }
  }

  onExport() {
    this.spinnerService.show();
    this._reportService.export(this.param).subscribe({
      next: result => {
        this.spinnerService.hide();
        if (result.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);
        }
        else this.functionUtility.snotifySuccessError(result.isSuccess, result.error, false)
      }
    })
  }
  //#endregion
}
