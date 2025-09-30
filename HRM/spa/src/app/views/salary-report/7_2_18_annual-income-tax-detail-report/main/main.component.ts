import { Component, effect, OnInit } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { ClassButton, IconButton } from '@constants/common.constants';
import { MonthlySalaryAdditionsDeductionsSummaryReportParam, MonthlySalaryAdditionsDeductionsSummaryReportSource } from '@models/salary-report/7_2_7_monthly-salary-additions-deductions-summary-report';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_7_2_18_AnnualIncomeTaxDetailReportService } from '@services/salary-report/s_7_2_18-annual-income-tax-detail-report.service';
import { AnnualIncomeTaxDetailReportParam, AnnualIncomeTaxDetailReportSource } from '@models/salary-report/7_2_18-annual-income-tax-detail-report';
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
  param: AnnualIncomeTaxDetailReportParam = <AnnualIncomeTaxDetailReportParam>{
    permission_Group: [],
  }
  year_Month_Start: Date;
  year_Month_End: Date;

  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };

  constructor(private service: S_7_2_18_AnnualIncomeTaxDetailReportService ) {
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
    if(!this.year_Month_Start || !this.year_Month_End) {
    this.setDefaultDateRange();
  }
  }

  ngOnDestroy(): void {
    this.service.setSource(<AnnualIncomeTaxDetailReportSource>{
      param: this.param,
      totalRows: this.totalRows
    })
  }

  getSource(){
    this.param = this.service.programSource().param;
    this.totalRows = this.service.programSource().totalRows;
    this.loadDropDownList();
    if(this.param.year_Month_Start)
      this.year_Month_Start = new Date(this.param.year_Month_Start)
    if(this.param.year_Month_End)
      this.year_Month_End = new Date(this.param.year_Month_End)
    if(!this.param.year_Month_Start || !this.param.year_Month_End) {
     this.setDefaultDateRange();
    }
  }

  private setDefaultDateRange(): void {
  const currentDate = new Date();
  const currentYear = currentDate.getFullYear();

  this.year_Month_Start = new Date(currentYear - 1, 11, 1);

  this.year_Month_End = new Date(currentYear, 10, 1);

  this.param.year_Month_Start = this.year_Month_Start.toStringYearMonth();
  this.param.year_Month_End = this.year_Month_End.toStringYearMonth();
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

  clear() {
    this.year_Month_Start = null;
    this.year_Month_End = null;
    this.totalRows = 0;
    this.param = <AnnualIncomeTaxDetailReportParam> {
      permission_Group: [],
    }
    this.setDefaultDateRange();
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

  onChangeYearMonth(){
    this.param.year_Month_Start = this.year_Month_Start != null ? this.year_Month_Start.toStringYearMonth() : null
    this.param.year_Month_End = this.year_Month_End != null ? this.year_Month_End.toStringYearMonth() : null
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
