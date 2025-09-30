import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { AnnualOvertimeHoursReportParam, AnnualOvertimeHoursReportSource } from "@models/attendance-maintenance/5_2_20_annual-overtime-hours-report";
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { S_5_2_20_AnnualOvertimeHoursReportService } from "@services/attendance-maintenance/s_5_2_20_annual-overtime-hours-report.service";
import { KeyValuePair } from '@utilities/key-value-pair';
import { LangChangeEvent } from '@ngx-translate/core';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit  {
  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  classButton = ClassButton;
  totalRows: number = 0;
  param: AnnualOvertimeHoursReportParam = <AnnualOvertimeHoursReportParam>{}

  year_Month: Date;
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };

  constructor(private service: S_5_2_20_AnnualOvertimeHoursReportService){
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList();
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
    this.service.setSource(<AnnualOvertimeHoursReportSource>{
      param: this.param,
      totalRows: this.totalRows
    })
  }

  private loadDropDownList() {
    this.getListFactory();
    this.getListDepartment();
  }

  setQueryDate(){
    if(this.param.year_Month)
      this.year_Month = new Date(this.param.year_Month)
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
    this.year_Month = null;
    this.totalRows = 0;
    this.param = <AnnualOvertimeHoursReportParam> {}
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

  onSelectFactory(){
    this.deleteProperty('department')
    this.getListDepartment();
  }

  onChangeYearMonth(){
    this.param.year_Month = this.year_Month != null ? this.year_Month.toStringYearMonth() : null
  }

  invalidParam(){
    return this.param.factory == null || this.param.year_Month == null
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
