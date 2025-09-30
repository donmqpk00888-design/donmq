import { Component, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { D_7_2_2_UtilityWorkersQualificationSeniorityPrinting_Dto, D_7_2_2_UtilityWorkersQualificationSeniorityPrinting_Param } from '@models/salary-report/7_2_2_utility-workers-qualification-seniority-printing';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_7_2_2_UtilityWorkersQualificationSeniorityPrinting } from '@services/salary-report/s_7_2_2_utility-workers-qualification-seniority-printing.service';
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
  programCode: string = '';
  title: string = '';
  totalRows: number = 0;
  iconButton = IconButton;
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month'
  };
  yearMonth: Date;
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  param: D_7_2_2_UtilityWorkersQualificationSeniorityPrinting_Param = <D_7_2_2_UtilityWorkersQualificationSeniorityPrinting_Param>{}
  constructor(private service: S_7_2_2_UtilityWorkersQualificationSeniorityPrinting) {
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
    if (this.param.yearMonth)
      this.yearMonth = new Date(this.param.yearMonth)
  }
  ngOnDestroy(): void {
    this.service.setSource(<D_7_2_2_UtilityWorkersQualificationSeniorityPrinting_Dto>{
      param: this.param,
      totalRows: this.totalRows
    })
  }
  getSource(){
    this.param = this.service.programSource().param;
    this.totalRows = this.service.programSource().totalRows;
    this.loadDropDownList();
    if(this.param.yearMonth)
      this.yearMonth = new Date(this.param.yearMonth)
  }
  onSelectFactory() {
    this.getListDepartment();
  }
  onDateChange() {
    this.param.yearMonth = this.yearMonth != null ? this.yearMonth.toStringYearMonth() : null
  }
  onNumberOfMonthChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.value.length > 4) {
      input.value = input.value.slice(0, 4);
      this.param.numberOfMonth = input.value, 10;
    }
  }
  private loadDropDownList() {
    this.getListFactory();
    this.getListDepartment();
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: res => {
        this.listFactory = res
      }
    })
  }
  getListDepartment() {
    this.service.getListDepartment(this.param.factory).subscribe({
      next: res => {
        this.listDepartment = res
      }
    })
  }
  getTotalRows(isSearch?: boolean) {
    this.spinnerService.show()
    this.service.search(this.param).subscribe({
      next: res => {
        this.spinnerService.hide()
        this.totalRows = res
        if (isSearch)
          this.snotifyService.success(this.translateService.instant('System.Message.QueryOKMsg'), this.translateService.instant('System.Caption.Success'));
      }
    })
  }
  download() {
    this.spinnerService.show()
    this.service.download(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        if (res.isSuccess) {
          this.functionUtility.exportExcel(res.data.excel, fileName);
          this.totalRows = res.data.totalRows
        } else {
          this.totalRows = 0
          this.snotifyService.warning(this.translateService.instant(res.error), this.translateService.instant('System.Caption.Warning'));
        }
      }
    })
  }
  clear() {
    this.yearMonth = null;
    this.totalRows = 0;
    this.param = <D_7_2_2_UtilityWorkersQualificationSeniorityPrinting_Param>{}
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
