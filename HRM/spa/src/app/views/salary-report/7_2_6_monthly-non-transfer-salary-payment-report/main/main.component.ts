import { Component, OnDestroy, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { D_7_2_6_MonthlyNonTransferSalaryPaymentReportParam, MonthlyNonTransferSalaryPaymentReportSource } from '@models/salary-report/7_2_6_monthly-non-transfer-salary-payment-report';
import { S_7_2_6_MonthlyNonTransferSalaryPaymentReportService } from '@services/salary-report/s_7_2_6_monthly-non-transfer-salary-payment-report.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;

  param: D_7_2_6_MonthlyNonTransferSalaryPaymentReportParam = <D_7_2_6_MonthlyNonTransferSalaryPaymentReportParam>{}

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month'
  };
  title: string = '';
  year_Month: Date
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  totalRows: number = 0;
  totalPermissionGroup: number = 0;
  programCode: string = '';
  constructor(private service: S_7_2_6_MonthlyNonTransferSalaryPaymentReportService) {
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
    this.service.setSource(<MonthlyNonTransferSalaryPaymentReportSource>{
      param: this.param,
      totalRows: this.totalRows,
      year_Month: this.year_Month
    });
  }

  getSource() {
    this.param = this.service.programSource().param;
    this.year_Month = this.service.programSource().year_Month;
    this.totalRows = this.service.programSource().totalRows;
    this.getDropDownList();
  }


  getDropDownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListPermissionGroup();
  }

  checkRequiredParams(): boolean {
    return !this.functionUtility.checkEmpty(this.param.factory) &&
      this.functionUtility.isValidDate(new Date(this.param.year_Month)) &&
      this.param.permission_Group.length > 0
  }

  getData(isSearch?: boolean) {
    this.param.year_Month = this.formatDate(this.year_Month);
    this.spinnerService.show();
    this.service.search(this.param).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.totalRows = res;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QueryOKMsg')
      }
    })
  }

  downloadPdf() {
    this.spinnerService.show();
    this.service.downloadPdf(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'PDF')
        if (result.isSuccess) {
          this.functionUtility.exportExcel(result.data.filePdf, fileName, 'pdf')
          this.totalRows = result.data.totalRow;
        } else {
          this.functionUtility.snotifySuccessError(result.isSuccess, result.error);
          this.totalRows = 0;
        }
      },
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
    this.year_Month = null;
    this.totalRows = 0;
    this.param = <D_7_2_6_MonthlyNonTransferSalaryPaymentReportParam>{
      permission_Group: [],
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

  onYearMonthChange() {
    this.param.year_Month = this.functionUtility.isValidDate(this.year_Month) ? this.year_Month.toStringYearMonth() : ''
  }

  onPermissionChange() {
    this.totalPermissionGroup = this.param.permission_Group.length;
  }
}
