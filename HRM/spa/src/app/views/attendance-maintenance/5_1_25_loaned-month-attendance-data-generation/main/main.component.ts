import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { ClassButton, IconButton } from '@constants/common.constants';
import { KeyValuePair } from '@utilities/key-value-pair';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { LoanedDataGeneration_Param, LoanedDataGeneration_Base, LoanedDataGeneration_Memory } from '@models/attendance-maintenance/5_1_25_loaned-month-attendance-data-generation';
import { S_5_1_25_LoanedMonthAttendanceDataGenerationService } from '@services/attendance-maintenance/s_5_1_25_loaned-month-attendance-data-generation.service';
import { TabComponentModel } from '@views/_shared/tab-component/tab.component';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit {
  @ViewChild('dataGenerationTab', { static: true }) dataGenerationTab: TemplateRef<any>;
  @ViewChild('dataCloseTab', { static: true }) dataCloseTab: TemplateRef<any>;
  tabs: TabComponentModel[] = [];
  title: string = '';
  data: LoanedDataGeneration_Base = <LoanedDataGeneration_Base>{ param: <LoanedDataGeneration_Param>{} };

  factories: KeyValuePair[] = [];

  bsConfigMonthly: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };

  iconButton = IconButton;
  classButton = ClassButton;

  minDateStart: Date = null;
  maxDateStart: Date = null;
  minDateEnd: Date = null;
  maxDateEnd: Date = null;

  closeStatus_list: KeyValuePair[] = [
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' }
  ];
  constructor(private _service: S_5_1_25_LoanedMonthAttendanceDataGenerationService) {
    super();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.initTab();
      this.getListFactory();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.data = this._service.paramSearch().data;
    this.initTab();
    this.getListFactory();
    this.changeAttMonth()
  }
  ngOnDestroy(): void {
    this._service.setParamSearch(<LoanedDataGeneration_Memory>{ data: this.data });
  }

  initTab() {
    this.tabs = [
      {
        id: 'dataGeneration',
        title: this.translateService.instant('AttendanceMaintenance.LoanedMonthAttendanceDataGeneration.DataGeneration'),
        isEnable: true,
        content: this.dataGenerationTab
      },
      {
        id: 'dataClose',
        title: this.translateService.instant('AttendanceMaintenance.LoanedMonthAttendanceDataGeneration.DataClose'),
        isEnable: true,
        content: this.dataCloseTab
      },
    ]
  }

  getListFactory() {
    this._service.getListFactory()
      .subscribe({
        next: (res) => {
          this.factories = res;
        }
      });
  }

  changeAttMonth(onChange: boolean = false) {
    if (onChange) {
      this.deleteProperty('loaned_Date_Start')
      this.deleteProperty('loaned_Date_End')
    }
    this.onDateChange('loaned_Year_Month')
    this.changeDate();
  }
  onDateChange(name: string) {
    this.data.param[`${name}_Str`] = this.functionUtility.isValidDate(new Date(this.data[name])) ? this.functionUtility.getDateFormat(new Date(this.data[name])) : '';
  }
  changeDate() {
    let firstDate = new Date(this.data.loaned_Year_Month).toFirstDateOfMonth();
    let lastDate = new Date(this.data.loaned_Year_Month).toLastDateOfMonth();
    if (!this.data.loaned_Date_Start) {
      this.minDateStart = firstDate;
      this.minDateEnd = firstDate;
    }
    else if (this.data.loaned_Date_Start >= this.data.loaned_Date_End) {
      this.data.loaned_Date_End = this.data.loaned_Date_Start;
    }
    if (!this.data.loaned_Date_End) {
      this.maxDateStart = lastDate;
      this.maxDateEnd = lastDate;
    }
    else if (this.data.loaned_Date_End <= lastDate) {
      this.maxDateEnd = lastDate;
      this.maxDateStart = this.data.loaned_Date_End;
    }
    this.onDateChange('loaned_Date_Start')
    this.onDateChange('loaned_Date_End')
  }
  deleteProperty(name: string) {
    delete this.data[name]
  }
  execute() {
    this.spinnerService.show();
    this.data.param.language = localStorage.getItem(LocalStorageConstants.LANG)
    this._service.execute(this.data.param)
      .subscribe({
        next: (res) => {
          if (res.isSuccess) {
            this.snotifyService.success(
              this.translateService.instant('System.Message.ExecuteOKMsg'),
              this.translateService.instant('System.Caption.Success')
            );
          } else {
            this.snotifyService.error(
              res.error ??
              this.translateService.instant('System.Message.ExecuteErrorMsg'),
              this.translateService.instant('System.Caption.Error')
            );
          }
          this.spinnerService.hide();
        }
      });
  }
  changeTab() {
    this.getListFactory();
  }
}
