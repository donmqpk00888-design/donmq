import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param, MonthlyAdditionsAndDeductionsSummaryReportForFinance_Source } from '@models/salary-report/7_2_21_monthly-additions-and-deductions-summary-report-for-finance';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_7_2_21_MonthlyAdditionsAndDeductionsSummaryReportForFinanceService } from '@services/salary-report/s_7_2_21_monthly-additions-and-deductions-summary-report-for-finance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})
export class MainComponent  extends InjectBase implements OnInit {
  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  classButton = ClassButton;
  totalRows: number = 0;
  param: MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param = <MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param>{
    kind: 'All',
  }
  year_Month: Date;
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];

  kinds: KeyValuePair[] = [
    { key: 'OnJob', value: 'SalaryReport.MonthlyAdditionsAndDeductionsSummaryReportForFinance.OnJob' },
    { key: 'Resigned', value: 'SalaryReport.MonthlyAdditionsAndDeductionsSummaryReportForFinance.Resigned' },
    { key: 'All', value: 'SalaryReport.MonthlyAdditionsAndDeductionsSummaryReportForFinance.All' },
  ];

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };
  constructor(private service: S_7_2_21_MonthlyAdditionsAndDeductionsSummaryReportForFinanceService) {
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
    this.service.setSource(<MonthlyAdditionsAndDeductionsSummaryReportForFinance_Source>{
      param: this.param,
      totalRows: this.totalRows
    })
  }

  getSource(){
    this.param = this.service.programSource().param;
    this.totalRows = this.service.programSource().totalRows;
    this.loadDropDownList();
    if(this.param.yearMonth)
      this.year_Month = new Date(this.param.yearMonth)
  }

  private loadDropDownList() {
    this.getListFactory();
    this.getListDepartment();
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
    this.param = <MonthlyAdditionsAndDeductionsSummaryReportForFinance_Param> {
      kind: 'All'
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
  }

  onChangeYearMonth(){
    this.param.yearMonth = this.year_Month != null ? this.year_Month.toStringYearMonth() : null
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
  //#endregion

}
