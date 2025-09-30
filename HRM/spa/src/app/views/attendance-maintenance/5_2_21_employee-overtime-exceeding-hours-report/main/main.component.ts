import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { EmployeeOvertimeExceedingHoursReportParam, EmployeeOvertimeExceedingHoursReportSource } from "@models/attendance-maintenance/5_2_21_employee-overtime-exceeding-hours-report";
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { S_5_2_21_EmployeeOvertimeExceedingHoursReportService } from "@services/attendance-maintenance/s_5_2_21_employee-overtime-exceeding-hours-report.service";
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
  param: EmployeeOvertimeExceedingHoursReportParam = <EmployeeOvertimeExceedingHoursReportParam>{}

  start_Date: Date;
  end_Date: Date;
  current_Date: Date = new Date();
  listFactory: KeyValuePair[] = [];

  statisticalMethod: KeyValuePair[] = [
    { key: 'Weekly', value: 'AttendanceMaintenance.EmployeeOvertimeExceedingHoursReport.Weekly' },
    { key: 'Monthly', value: 'AttendanceMaintenance.EmployeeOvertimeExceedingHoursReport.Monthly' },
    { key: 'Annual', value: 'AttendanceMaintenance.EmployeeOvertimeExceedingHoursReport.Annual' },
    { key: 'DateRange', value: 'AttendanceMaintenance.EmployeeOvertimeExceedingHoursReport.DateRange' },
    { key: 'Details', value: 'AttendanceMaintenance.EmployeeOvertimeExceedingHoursReport.Details' },
  ];

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };

  constructor(private service: S_5_2_21_EmployeeOvertimeExceedingHoursReportService) {
    super()
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList()
    });

    effect(() => {
      this.param = this.service.programSource().param;
      this.totalRows = this.service.programSource().totalRows;
      this.loadDropDownList();
      this.setQueryDate();
    })
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }

  ngOnDestroy(): void {
    this.service.setSource(<EmployeeOvertimeExceedingHoursReportSource>{
      param: this.param,
      totalRows: this.totalRows
    })
  }

  private loadDropDownList() {
    this.getListFactory();
  }

  setQueryDate(){
    if(this.param.start_Date)
      this.start_Date = new Date(this.param.start_Date);
    if(this.param.end_Date)
      this.end_Date = new Date(this.param.end_Date);
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

  clear(){
    this.start_Date = null;
    this.end_Date = null;
    this.totalRows = 0;
    this.param = <EmployeeOvertimeExceedingHoursReportParam> {}
  }

  download(){
    this.spinnerService.show()
    this.service.download(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if(res.isSuccess){
          this.totalRows = res.data.totalRows;
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data.excel, fileName);
        } else {
          this.totalRows = 0
          this.snotifyService.warning(res.error, this.translateService.instant('System.Caption.Warning'));
        }
      }
    })
  }

  onChangeDate(name: string){
    this.param[name] = this[name] != null ? this[name].toStringDate() : null
  }

  onChangeStatisticalMethod(){
    switch(this.param.statistical_Method){
      case 'Weekly':
        this.setDefaulValue(12, this.current_Date.toLastMonday(), this.current_Date.toLastSaturday())
        break;
      case 'Monthly':
        this.setDefaulValue(40, this.current_Date.toFirstDateOfMonth(), this.current_Date.toLastDateOfMonth())
        break;
      case 'Annual':
        this.setDefaulValue(300, this.current_Date.toFirstDateOfYear(), this.current_Date)
        break;
      case 'DateRange':
        this.setDefaulValue(189)
        break;
      default:
        this.setDefaulValue()
    }
  }

  setDefaulValue(abnormal_Overtime_Hours: number = null, start_Date: Date = null, end_Date: Date = null){
    this.param.abnormal_Overtime_Hours = abnormal_Overtime_Hours;
    this.start_Date = start_Date;
    this.end_Date = end_Date;
    this.onChangeDate('start_Date');
    this.onChangeDate('end_Date');
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
  //#endregion
}
