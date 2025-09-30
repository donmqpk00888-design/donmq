import { Component, OnDestroy, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { DownloadPersonnelDataToExcel_Param, DownloadPersonnelDataToExcel_Source } from '@models/salary-report/7_2_16_download-personnel-data-to-excel';
import { S_7_2_16_DownloadPersonnelDataToExcelService } from '@services/salary-report/s_7_2_16-download-personnel-data-to-excel.service';
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
  iconButton = IconButton
  classButton = ClassButton
  placeholder = Placeholder

  param: DownloadPersonnelDataToExcel_Param = <DownloadPersonnelDataToExcel_Param>{}
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: "YYYY/MM/DD"
  }
  bsConfigMonth: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };
  totalRows: number = 0;
  title: string = ''
  programCode: string = ''
  startDate: Date = null
  endDate: Date = null
  yearMonth: Date = null
  listFactory: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listEmployeeKind: KeyValuePair[] = [
    {
      key: "Onjob",
      value: "SalaryReport.DownloadPersonnelDataToExcel.OnJob"
    },
    {
      key: "Resigned",
      value: "SalaryReport.DownloadPersonnelDataToExcel.Resigned"
    },
    {
      key: "All",
      value: "SalaryReport.DownloadPersonnelDataToExcel.All"
    }
  ]

  listReportKind: KeyValuePair[] = [
    {
      key: "EmployeeMasterFile",
      value: "SalaryReport.DownloadPersonnelDataToExcel.EmployeeMasterFile"
    },
    {
      key: "SalaryMasterFile",
      value: "SalaryReport.DownloadPersonnelDataToExcel.SalaryMasterFile"
    },
    {
      key: "MonthlyAttendance",
      value: "SalaryReport.DownloadPersonnelDataToExcel.MonthlyAttendance"
    },
    {
      key: "MonthlySalary",
      value: "SalaryReport.DownloadPersonnelDataToExcel.MonthlySalary"
    },

  ]
  constructor(private service: S_7_2_16_DownloadPersonnelDataToExcelService) {
    super();
    this.programCode = this.route.snapshot.data['program']
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
    this.service.setParamSearch(<DownloadPersonnelDataToExcel_Source>{
      param: this.param,
      totalRows: this.totalRows
    })
  }
  getSource() {
    this.param = this.service.paramSearch().param
    this.totalRows = this.service.paramSearch().totalRows
    if (this.functionUtility.isValidDate(new Date(this.param.yearMonth)))
      this.yearMonth = new Date(this.param.yearMonth)
    if (this.functionUtility.isValidDate(new Date(this.param.startDate)))
      this.startDate = new Date(this.param.startDate);
    if (this.functionUtility.isValidDate(new Date(this.param.endDate)))
      this.endDate = new Date(this.param.endDate);
    this.getDropdownList()
  }

  checkRequiredParams(): boolean {
    return this.functionUtility.checkEmpty(this.param.factory) || this.functionUtility.checkEmpty(this.param.permissionGroup) || this.functionUtility.checkEmpty(this.param.employeeKind) || this.functionUtility.checkEmpty(this.param.reportKind);
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res: KeyValuePair[]) => this.listFactory = res
    });
  }
  getPermissionGroup() {
    this.service.getPermissionGroup(this.param.factory).subscribe({
      next: res => {
        this.listPermissionGroup = res
        this.selectAllForDropdownItems(this.listPermissionGroup);
      }
    })
  }
  getTotalRows(isSearch?: boolean) {
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
  download() {
    this.spinnerService.show()
    this.service.download(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          this.totalRows = res.data.totalRows
          const fileName = this.functionUtility.getFileNameExport(this.programCode, `${this.param.reportKind}_Download`)
          this.functionUtility.exportExcel(res.data.excel, fileName);
        } else {
          this.totalRows = 0
          this.snotifyService.error(this.translateService.instant(res.error), this.translateService.instant('System.Caption.Error'));
        }
      }
    })
  }
  getDropdownList() {
    this.getListFactory()
    this.getPermissionGroup()
  }
  private selectAllForDropdownItems(items: KeyValuePair[]) {
    let allSelect = (items: KeyValuePair[]) => {
      items.forEach(element => {
        element['allGroup'] = 'allGroup';
      });
    };
    allSelect(items);
  }

  onSelectFactory() {
    this.param.permissionGroup = [];
    this.getPermissionGroup()
  }

  deleteProperty = (name: string) => delete this.param[name]
  clear() {
    this.param = <DownloadPersonnelDataToExcel_Param>{
      employeeKind: "Onjob",
      reportKind: "EmployeeMasterFile",
      permissionGroup: []
    }
    this.totalRows = 0
    this.startDate = null
    this.endDate = null
    this.yearMonth = null
    this.getDropdownList()
  }

  onChangeDate(name: string) {
    this.param[name] = this[name] != null ? this[name].toStringDate() : ""
  }
  disabledYearMonth() {
    return this.param.reportKind == "MonthlyAttendance" || this.param.reportKind == "MonthlySalary"
  }
  onChangeReportKind() {
    if (!this.disabledYearMonth())
      this.yearMonth = null
    this.onChangeDate('yearMonth');
  }
}
