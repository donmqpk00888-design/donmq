import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { MonthlySalarySummaryReportForTaxation_Param, MonthlySalarySummaryReportForTaxation_Source } from '@models/salary-report/7_2_20_monthly-salary-summary-report-for-taxation';
import { S_7_2_20_MonthlySalarySummaryReportForTaxation } from '@services/salary-report/s-7-2-20-monthly-salary-summary-report-for-taxation.service';
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
  param: MonthlySalarySummaryReportForTaxation_Param = <MonthlySalarySummaryReportForTaxation_Param>{
    kind: 'OnJob',
    permission_Group: [],
  }
  listDepartment: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  kinds: KeyValuePair[] = [
    { key: 'OnJob', value: 'SalaryReport.MonthlySalarySummaryReportForTaxation.OnJob' },
    { key: 'Resigned', value: 'SalaryReport.MonthlySalarySummaryReportForTaxation.Resigned' },
    { key: 'All', value: 'SalaryReport.MonthlySalarySummaryReportForTaxation.All' },
  ];
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month'
  };
  year_Month_Start: Date;
  year_Month_End: Date;
  programCode: string = '';
  totalPermissionGroup: number = 0;
  totalRows: number = 0;
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;

  constructor(private service: S_7_2_20_MonthlySalarySummaryReportForTaxation) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDropdownList()

    });
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }
  ngOnDestroy(): void {
    this.service.setSource(<MonthlySalarySummaryReportForTaxation_Source>{
      param: this.param,
      totalRows: this.totalRows,
      year_Month_Start: this.year_Month_Start,
      year_Month_End: this.year_Month_End,
    });
  }
  getSource() {
    this.param = this.service.programSource().param;
    this.totalRows = this.service.programSource().totalRows;
    this.year_Month_Start = this.service.programSource().year_Month_Start
      ? new Date(this.service.programSource().year_Month_Start)
      : null;
    this.year_Month_End = this.service.programSource().year_Month_End
      ? new Date(this.service.programSource().year_Month_End)
      : null;
    this.getDropDownList();
  }
  getDropDownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListPermissionGroup();
  }
  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.search(this.param).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.totalRows = res;
        if (isSearch)
          this.snotifyService.success(this.translateService.instant('System.Message.QueryOKMsg'),
            this.translateService.instant('System.Caption.Success'));
      }
    })
  }
  download() {
    this.spinnerService.show();
    this.service.downloadExcel(this.param).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data.excel, fileName);
          this.totalRows = res.data.totalRows
        }
        else {
          this.totalRows = 0;
          this.functionUtility.snotifySuccessError(false, res.error);
        }
        this.spinnerService.hide();
      }
    });
  }
  getDropdownList() {
    this.getListFactory();
    if (!this.functionUtility.checkEmpty(this.param.factory)) {
      this.getListDepartment();
      this.getListPermissionGroup();
    }
  }
  formatDate(date: Date): string {
    return date ? this.functionUtility.getDateFormat(date) : '';
  }
  onSelectFactory() {
    this.deleteProperty('department');
    this.deleteProperty('permission_Group');
    this.getListDepartment();
    this.getListPermissionGroup();
  }
  clear() {
    this.year_Month_Start = null,
      this.year_Month_End = null,
      this.totalRows = 0;
    this.param = <MonthlySalarySummaryReportForTaxation_Param>{
      permission_Group: [],
      kind: 'All',
    }
  }
  deleteProperty = (name: string) => delete this.param[name]
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res: KeyValuePair[]) => this.listFactory = res
    });
  }
  getListDepartment() {
    if (this.param.factory)
      this.service.getListDepartment(this.param.factory).subscribe({
        next: (res: KeyValuePair[]) => this.listDepartment = res
      });
  }
  getListPermissionGroup() {
    if (this.param.factory)
      this.service.getListPermissionGroup(this.param.factory).subscribe({
        next: res => {
          this.listPermissionGroup = res
          this.functionUtility.getNgSelectAllCheckbox(this.listPermissionGroup)
        }
      })
  }
  onPermissionChange() {
    this.totalPermissionGroup = this.param.permission_Group.length;
  }
  onDateChange() {
    this.param.year_Month_Start = this.year_Month_Start ? this.year_Month_Start.toStringYearMonth() : null;
    this.param.year_Month_End = this.year_Month_End ? this.year_Month_End.toStringYearMonth() : null;
  }
}
