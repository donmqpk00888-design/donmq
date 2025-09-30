import { Component, effect, OnInit } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { S_7_2_7_MonthlySalaryAdditionsDeductionsSummaryReportService } from "@services/salary-report/s_7_2_7_monthly-salary-additions-deductions-summary-report.service";
import { ClassButton, IconButton } from '@constants/common.constants';
import { MonthlySalaryAdditionsDeductionsSummaryReportParam, MonthlySalaryAdditionsDeductionsSummaryReportSource } from '@models/salary-report/7_2_7_monthly-salary-additions-deductions-summary-report';
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
  param: MonthlySalaryAdditionsDeductionsSummaryReportParam = <MonthlySalaryAdditionsDeductionsSummaryReportParam>{
    kind: 'OnJob',
    permission_Group: [],
  }
  year_Month: Date;
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];

  kinds: KeyValuePair[] = [
    { key: 'OnJob', value: 'SalaryReport.MonthlySalaryAdditionsDeductionsSummaryReport.OnJob' },
    { key: 'Resigned', value: 'SalaryReport.MonthlySalaryAdditionsDeductionsSummaryReport.Resigned' },
  ];

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };

  constructor(private service: S_7_2_7_MonthlySalaryAdditionsDeductionsSummaryReportService) {
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
    this.service.setSource(<MonthlySalaryAdditionsDeductionsSummaryReportSource>{
      param: this.param,
      totalRows: this.totalRows
    })
  }

  getSource(){
    this.param = this.service.programSource().param;
    this.totalRows = this.service.programSource().totalRows;
    this.loadDropDownList();
    if(this.param.year_Month)
      this.year_Month = new Date(this.param.year_Month)
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
    this.year_Month = null;
    this.totalRows = 0;
    this.param = <MonthlySalaryAdditionsDeductionsSummaryReportParam> {
      permission_Group: [],
      kind: 'OnJob'
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

  onChangeYearMonth(){
    this.param.year_Month = this.year_Month != null ? this.year_Month.toStringYearMonth() : null
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
