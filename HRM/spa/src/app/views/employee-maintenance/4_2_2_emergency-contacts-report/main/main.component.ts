import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { EmergencyContactsReportParam, EmergencyContactsReportSource } from '@models/employee-maintenance/4_2_2_emergency-contacts-report';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_4_2_2_EmergencyContactsReportService } from '@services/employee-maintenance/s_4_2_2_emergency-contacts-report.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  classButton = ClassButton;
  totalRows: number = 0;
  param: EmergencyContactsReportParam = <EmergencyContactsReportParam>{
    employmentStatus: '',
  }
  listDivision: KeyValuePair[] = []
  listFactory: KeyValuePair[] = []
  listDepartment: KeyValuePair[] = []
  listAssignedFactory: KeyValuePair[] = [];
  listAssignedDepartment: KeyValuePair[] = [];
  employmentStatus: KeyValuePair[] = [
    { key: '', value: 'EmployeeInformationModule.EmployeeEmergencyContactsReport.All' },
    { key: 'Y', value: 'EmployeeInformationModule.EmployeeEmergencyContactsReport.Y' },
    { key: 'N', value: 'EmployeeInformationModule.EmployeeEmergencyContactsReport.N' },
    { key: 'U', value: 'EmployeeInformationModule.EmployeeEmergencyContactsReport.U' },
  ];
  constructor(private service: S_4_2_2_EmergencyContactsReportService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList()
    });
    this.getDataFromSource()
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }
  ngOnDestroy(): void {
    this.service.setSource(<EmergencyContactsReportSource>{
      param: this.param,
      totalRows: this.totalRows
    })
  }

  getDataFromSource() {
    effect(() => {
      this.param = this.service.programSource().param;
      this.totalRows = this.service.programSource().totalRows;
      this.loadDropDownList()
    })
  }

  private loadDropDownList() {
    this.getListDivision();
    this.getListFactory();
    this.getListDepartment();
    this.getListAssignedFactory();
    this.getListAssignedDepartment();
  }

  getTotalRows(isSearch?: boolean) {
    this.spinnerService.show()
    this.service.getTotalRows(this.param).subscribe({
      next: res => {
        this.spinnerService.hide()
        this.totalRows = res
        if (isSearch)
          this.snotifyService.success(this.translateService.instant('System.Message.QueryOKMsg'), this.translateService.instant('System.Caption.Success'));
      }
    })
  }

  excel() {
    this.spinnerService.show();
    this.service.downloadExcel(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide()
        if (result.isSuccess) {
          this.getTotalRows()
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);
        }
        else {
          this.totalRows = 0
          this.snotifyService.warning(result.error, this.translateService.instant('System.Caption.Warning'));
        }
      }
    });
  }
  getListDivision() {
    this.service.getListDivision().subscribe({
      next: res => {
        this.listDivision = res
      }
    })
  }
  getListFactory() {
    this.service.getListFactory(this.param.division).subscribe({
      next: res => {
        this.listFactory = res
      }
    })
  }
  getListDepartment() {
    this.service.getListDepartment(this.param.division, this.param.factory).subscribe({
      next: res => this.listDepartment = res
    })
  }

  onSelectDivision() {
    this.deleteProperty('factory')
    this.getListFactory()
  }
  onSelectFactory() {
    this.deleteProperty('department')
    this.getListDepartment()
  }

  getListAssignedFactory() {
    this.service.getListFactory(this.param.assignedDivision).subscribe({
      next: res => {
        this.listAssignedFactory = res
      }
    })
  }
  getListAssignedDepartment() {
    this.service.getListDepartment(this.param.assignedDivision, this.param.assignedFactory).subscribe({
      next: res => this.listAssignedDepartment = res
    })
  }

  onSelectAssignedDivision() {
    this.deleteProperty('assignedFactory')
    this.getListAssignedFactory()
  }
  onSelectAssignedFactory() {
    this.deleteProperty('assignedDepartment')
    this.getListAssignedDepartment()
  }

  clear() {
    this.totalRows = 0
    this.param.employmentStatus = ''
    this.deleteProperty('division')
    this.deleteProperty('factory')
    this.deleteProperty('employeeID')
    this.deleteProperty('department')
    this.deleteProperty('assignedDivision')
    this.deleteProperty('assignedFactory')
    this.deleteProperty('assignedEmployeeID')
    this.deleteProperty('assignedDepartment')
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }

}
