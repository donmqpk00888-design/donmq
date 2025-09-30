import { Component, OnInit } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { S_7_2_9_SalarySummaryReportExitedEmployeeService } from "@services/salary-report/s_7_2_9_salary-summary-report-exited-employee.service";
import { ClassButton, IconButton } from '@constants/common.constants';
import { SalarySummaryReportExitedEmployeeParam, SalarySummaryReportExitedEmployeeSource } from '@models/salary-report/7_2_9_salary-summary-report-exited-employee';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { LangChangeEvent } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  classButton = ClassButton;
  totalRows: number = 0;
  param: SalarySummaryReportExitedEmployeeParam = <SalarySummaryReportExitedEmployeeParam>{
    permission_Group: [],
    transfer: "Y"
  }
  resignation_Start: Date;
  resignation_End: Date;
  max_Resignation_Start: Date;
  min_Resignation_Start: Date;
  max_Resignation_End: Date;
  min_Resignation_End: Date;

  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];

  transfers: KeyValuePair[] = [
    { key: 'Y', value: 'SalaryReport.SalarySummaryReportExitedEmployee.Transfer' },
    { key: 'N', value: 'SalaryReport.SalarySummaryReportExitedEmployee.NoTransfer' },
    { key: 'All', value: 'SalaryReport.SalarySummaryReportExitedEmployee.All' }
  ];

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };

  constructor(private service: S_7_2_9_SalarySummaryReportExitedEmployeeService) {
    super()
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource();
  }

  ngOnDestroy(): void {
    this.service.setSource(<SalarySummaryReportExitedEmployeeSource>{
      param: this.param,
      totalRows: this.totalRows
    })
  }

  getSource(){
    this.param = this.service.programSource().param;
    this.totalRows = this.service.programSource().totalRows;
    this.loadDropDownList();
    if(this.param.resignation_Start)
      this.resignation_Start = new Date(this.param.resignation_Start)
    if(this.param.resignation_End)
      this.resignation_End = new Date(this.param.resignation_End)

    this.setMinMaxResignationDate();
  }

  private loadDropDownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListPermissionGroup();
  }

  getTotalRows(isSearch?: boolean) {
    this.spinnerService.show()
    this.service.getTotalRows(this.param).subscribe({
      next: res => {
        this.spinnerService.hide()
        if(res.isSuccess)
        {
          this.totalRows = res.data
          if (isSearch)
            this.snotifyService.success(this.translateService.instant('System.Message.QueryOKMsg'),
              this.translateService.instant('System.Caption.Success'));
        } else {
          this.snotifyService.error(this.translateService.instant(res.error ?? 'System.Message.SystemError'),
            this.translateService.instant('System.Caption.Error'));
        }
      }
    })
  }

  clear(){
    this.resignation_Start = null;
    this.resignation_End = null;
    this.min_Resignation_Start = null;
    this.max_Resignation_Start = null;
    this.min_Resignation_End = null;
    this.max_Resignation_End = null;
    this.totalRows = 0;
    this.param = <SalarySummaryReportExitedEmployeeParam> {
      permission_Group: [],
      transfer: "Y"
    }
  }

  download(){
    this.spinnerService.show()
    this.service.download(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if(res.isSuccess){
          this.totalRows = res.data.totalRows
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data.excel, fileName);
        } else {
          this.totalRows = 0
          this.snotifyService.error(this.translateService.instant(res.error), this.translateService.instant('System.Caption.Error'));
        }
      }
    })
  }

  onSelectFactory(){
    this.deleteProperty('department')
    this.getListDepartment();
    this.param.permission_Group = [];
    this.getListPermissionGroup();
  }

  onChangeDate(value: Date,field: string) {
    if (this[field]?.toString() == 'Invalid Date')
    {
      this[field] = null;
      return;
    }

    if(field == 'resignation_Start'){
      if(this.min_Resignation_Start && value && value < this.min_Resignation_Start)
        this[field] = this.min_Resignation_Start;

      if(this.max_Resignation_Start && value && value > this.max_Resignation_Start )
        this[field] = this.max_Resignation_Start;
    } else {
      if(this.min_Resignation_End && value && value < this.min_Resignation_End)
        this[field] = this.min_Resignation_End;

      if(this.max_Resignation_End && value && value > this.max_Resignation_End)
        this[field] = this.max_Resignation_End;
    }

    this.setMinMaxResignationDate();

    this.param[field] = this[field] != null ? this[field].toStringDate() : null
  }

  setMinMaxResignationDate() {
    this.min_Resignation_Start = this.resignation_End?.toFirstDateOfMonth();
    this.max_Resignation_Start = this.resignation_End;
    this.min_Resignation_End = this.resignation_Start;
    this.max_Resignation_End = this.resignation_Start?.toLastDateOfMonth();
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }

  //#region Get List
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: res => {
        this.listFactory = res
      }
    })
  }

  getListDepartment() {
    this.service.getListDepartment(this.param.factory).subscribe({
      next: res => this.listDepartment = res
    })
  }

  getListPermissionGroup() {
    this.service.getListPermissionGroup(this.param.factory).subscribe({
      next: res => {
        this.listPermissionGroup = res
        this.functionUtility.getNgSelectAllCheckbox(this.listPermissionGroup)
      }
    })
  }
  //#endregion
}
