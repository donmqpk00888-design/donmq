import { Component, OnDestroy, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { MonthlySalaryTransferDetailsExitedEmployeeParam, MonthlySalaryTransferDetailsExitedEmployeeSource } from '@models/salary-report/7_2_13_monthly-salary-transfer-details-exited-employee';
import { S_7_2_13_MonthlySalaryTransferDetailsExitedEmployeeService } from '@services/salary-report/s-7-2-13-monthly-salary-transfer-details-exited-employee.service';
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

  param: MonthlySalaryTransferDetailsExitedEmployeeParam = <MonthlySalaryTransferDetailsExitedEmployeeParam>{}

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month'
  };

  bsConfigDate: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };

  title: string = '';
  year_Month: Date;
  start_Date: Date;
  end_Date: Date;
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  totalRows: number = 0;
  totalPermissionGroup: number = 0;
  programCode: string = '';
  constructor(private service: S_7_2_13_MonthlySalaryTransferDetailsExitedEmployeeService) {
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
    this.service.setSource(<MonthlySalaryTransferDetailsExitedEmployeeSource>{
      param: this.param,
      totalRows: this.totalRows,
      year_Month: this.year_Month,
      start_Date : this.start_Date,
      end_Date: this.end_Date
    });
  }

  getSource() {
    this.param = this.service.programSource().param;
    this.year_Month = this.service.programSource().year_Month;
    this.totalRows = this.service.programSource().totalRows;
    this.start_Date = this.service.programSource().start_Date;
    this.end_Date = this.service.programSource().end_Date;
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
    this.param.start_Date = this.formatDate(this.start_Date);
    this.param.end_Date = this.formatDate(this.end_Date);
    this.spinnerService.show();
    this.service.search(this.param).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.totalRows = res.data;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QueryOKMsg')
      }
    })
  }

  download() {
    this.param.year_Month = this.formatDate(this.year_Month);
    this.param.start_Date = this.formatDate(this.start_Date);
    this.param.end_Date = this.formatDate(this.end_Date);
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
    this.start_Date = null;
    this.end_Date = null;
    this.totalRows = 0;
    this.param = <MonthlySalaryTransferDetailsExitedEmployeeParam>{
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

  onChangeYearMonth(name: string) {
    this.param[name] = this.functionUtility.isValidDate(new Date(this.param[name])) ?
          this.functionUtility.getDateFormat(new Date(this.param[name])): '';
  }

  onPermissionChange() {
    this.totalPermissionGroup = this.param.permission_Group.length;
  }

}
