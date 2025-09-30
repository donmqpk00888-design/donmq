import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { EmployeeRewardAndPenaltyReportParam, EmployeeRewardAndPenaltyReportSource } from '@models/reward-and-penalty-report/8_2_1_employee-reward-and-penalty-report';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_8_2_1_EmployeeRewardAndPenaltyReportService } from '@services/reward-and-penalty-report/s_8_2_1_employee-reward-and-penalty-report.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
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
  placeholder = Placeholder;
  totalRows: number = 0;
  param: EmployeeRewardAndPenaltyReportParam = <EmployeeRewardAndPenaltyReportParam>{
    permission_Group: [],
    counts: '1'
  }
  start_Date: Date;
  end_Date: Date;
  start_Year_Month: Date;
  end_Year_Month: Date;
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listRewardPenalty: KeyValuePair[] = [];

  bsConfigMonthly: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };

  constructor(private service: S_8_2_1_EmployeeRewardAndPenaltyReportService) {
    super()
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }

  ngOnDestroy(): void {
    this.service.setSource(<EmployeeRewardAndPenaltyReportSource>{
      param: this.param,
      totalRows: this.totalRows,
      start_Date: this.start_Date,
      end_Date: this.end_Date,
      start_Year_Month: this.start_Year_Month,
      end_Year_Month: this.end_Year_Month
    });
  }

  getSource() {
    this.param = this.service.programSource().param;
    this.start_Date = this.service.programSource().start_Date;
    this.end_Date = this.service.programSource().end_Date;
    this.start_Year_Month = this.service.programSource().start_Year_Month;
    this.end_Year_Month = this.service.programSource().end_Year_Month;
    this.totalRows = this.service.programSource().totalRows;
    this.loadDropDownList();
  }

  loadDropDownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListPermissionGroup();
    this.getListRewardPenalty();
  }

  getTotalRows(isSearch?: boolean) {
    this.param.start_Date = this.formatDate(this.start_Date);
    this.param.end_Date = this.formatDate(this.end_Date);
    this.param.start_Year_Month = this.formatDate(this.start_Year_Month);
    this.param.end_Year_Month = this.formatDate(this.end_Year_Month);
    this.spinnerService.show()
    this.service.getTotalRows(this.param).subscribe({
      next: res => {
        this.spinnerService.hide()
        if (res.isSuccess) {
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
    this.start_Date = null;
    this.end_Date = null;
    this.start_Year_Month = null;
    this.end_Year_Month = null;
    this.totalRows = 0;
    this.param = <EmployeeRewardAndPenaltyReportParam>{
      permission_Group: [],
      counts: '1'
    }
  }

  download() {
    this.param.start_Date = this.formatDate(this.start_Date);
    this.param.end_Date = this.formatDate(this.end_Date);
    this.param.start_Year_Month = this.formatDate(this.start_Year_Month);
    this.param.end_Year_Month = this.formatDate(this.end_Year_Month);
    this.spinnerService.show()
    this.service.download(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
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

  onSelectFactory() {
    this.deleteProperty('department')
    this.getListDepartment();
    this.param.permission_Group = [];
    this.getListPermissionGroup();
  }

  formatDate(date: Date): string {
    return date ? this.functionUtility.getDateFormat(date) : '';
  }

  validateNumber(event: KeyboardEvent) {
    const inputChar = event.key;
    const numberRegex = /^[1-9]$/;
    const inputValue = (event.target as HTMLInputElement).value + inputChar;

    if (!numberRegex.test(inputChar) ||
      (parseInt(inputValue) > 2147483647)) {
      event.preventDefault();
    }
  }

  onChangeYearMonth(name: string) {
    this.param[name] = this.functionUtility.isValidDate(new Date(this.param[name])) ?
          this.functionUtility.getDateFormat(new Date(this.param[name])): '';
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

  getListRewardPenalty() {
    this.service.getListRewardPenalty().subscribe({
      next: res => {
        this.listRewardPenalty = res
      }
    })
  }
  //#endregion

}
