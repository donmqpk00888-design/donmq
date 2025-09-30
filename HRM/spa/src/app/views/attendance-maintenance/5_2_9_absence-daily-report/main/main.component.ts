import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { AbsenceDailyReportCount, AbsenceDailyReportParam, AbsenceDailyReportSource } from '@models/attendance-maintenance/5_2_9_absence-daily-report';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_5_2_9_AbsenceDailyReportService } from '@services/attendance-maintenance/s_5_2_9_absence-daily-report.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  date: string = '';
  iconButton = IconButton;
  classButton = ClassButton;
  resultCount: AbsenceDailyReportCount = <AbsenceDailyReportCount>{
    queryResult: 0,
    recruits: 0,
    resigning: 0
  };
  param: AbsenceDailyReportParam = <AbsenceDailyReportParam>{
  }
  listFactory: KeyValuePair[] = [];
  // #region constructor
  constructor(private service: S_5_2_9_AbsenceDailyReportService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList()
    });
    this.getDataFromSource()
  }

  // #region ngOnInit
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }
  // #region ngOnDestroy
  ngOnDestroy(): void {
    this.param.date = this.date;
    this.service.setSource(<AbsenceDailyReportSource>{
      param: this.param,
      resultCount: this.resultCount
    })
  }

  // #region get data from source
  getDataFromSource() {
    effect(() => {
      this.param = this.service.programSource().param;
      this.date = this.param.date;
      this.resultCount = this.service.programSource().resultCount;
      this.loadDropDownList()
    })
  }

  // #region loadDropDownList
  private loadDropDownList() {
    this.getListFactory();
  }

  // #region getTotalRows
  getTotalRows(isSearch?: boolean) {
    this.param.date = this.functionUtility.getDateFormat(new Date(this.date));
    if (this.checkRequiredParam()) return
    this.spinnerService.show()
    this.service.getTotalRows(this.param).subscribe({
      next: res => {
        this.spinnerService.hide()
        this.resultCount = res
        if (isSearch)
          this.snotifyService.success(this.translateService.instant('System.Message.QueryOKMsg'), this.translateService.instant('System.Caption.Success'));
      }
    })
  }

  // #region excel
  excel() {
    this.param.date = this.functionUtility.getDateFormat(new Date(this.date));
    if (this.checkRequiredParam()) return
    this.param.date = this.functionUtility.getDateFormat(new Date(this.date));
    this.spinnerService.show();
    this.service.downloadExcel(this.param).subscribe({
      next: (result) => {
        if (result.isSuccess) {
          this.getTotalRows()
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);
        }
        else {
          this.spinnerService.hide()
          this.resultCount = <AbsenceDailyReportCount>{ queryResult: 0, recruits: 0, resigning: 0 }
          this.snotifyService.warning(result.error, this.translateService.instant('System.Caption.Warning'));
        }
      }
    });
  }

  // #region getListFactory
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: res => {
        this.listFactory = res;
      }
    });
  }

  // #region checkRequiredParam
  private checkRequiredParam(): boolean {
    let isInvalid = false;
    let errorMessages = [];
    if (!this.param.factory) {
      errorMessages.push('Factory' + this.translateService.instant('System.Message.IsRequired'));
      isInvalid = true
    }

    if (!this.date) {
      errorMessages.push('Date' + this.translateService.instant('System.Message.IsRequired'));
      isInvalid = true
    }

    if (isInvalid) {
      this.functionUtility.snotifySuccessError(false, errorMessages.join('\n'));
    }

    return isInvalid;
  }

  // #region clear
  clear() {
    this.resultCount = <AbsenceDailyReportCount>{ queryResult: 0, recruits: 0, resigning: 0 }
    this.date = '';
    this.deleteProperty('factory');
  }

  // #region deleteProperty
  deleteProperty(name: string) {
    delete this.param[name]
  }

  // #region dateChange
  dateChange(isStart: boolean = true) {
    if (isStart) {
      if (this.date && this.date.toString() == 'Invalid Date')
        this.date = '';
    }
  }

}
