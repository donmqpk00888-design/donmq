import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import {
  HRDailyReportCount,
  HRDailyReportParam,
  HRDailyReportSource
} from '@models/attendance-maintenance/5_2_10_hr-daily-report';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_5_2_10_HRDailyReportService } from '@services/attendance-maintenance/s_5_2_10_hr-daily-report.service';
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
  totalPermissionGroup: number = 0;
  resultCount: HRDailyReportCount = <HRDailyReportCount>{
    queryResult: 0,
    headCount: 0,
    monthlyAbsenteeism: 0
  };
  param: HRDailyReportParam = <HRDailyReportParam>{
  }
  listFactory: KeyValuePair[] = [];
  listLevel: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  // #region constructor
  constructor(private service: S_5_2_10_HRDailyReportService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList()
    });
    this.getDataFromSource()
  }

  // #`region ngOnInit
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }

  // #region ngOnDestroy
  ngOnDestroy(): void {
    this.param.date = this.date;
    this.service.setSource(<HRDailyReportSource>{
      param: this.param,
      resultCount: this.resultCount
    })
  }



  // #region getDataFromSource
  getDataFromSource() {
    effect(() => {
      // Set total
      this.param = this.service.programSource().param;
      this.date = this.param.date;
      this.resultCount = this.service.programSource().resultCount;
      this.loadDropDownList()
    })
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
    if (!this.param.level) {
      errorMessages.push('Level' + this.translateService.instant('System.Message.IsRequired'));
      isInvalid = true
    }
    if (!this.param.permissionGroups?.length) {
      errorMessages.push('Permission Group' + this.translateService.instant('System.Message.IsRequired'));
      isInvalid = true
    }
    if (isInvalid) {
      this.functionUtility.snotifySuccessError(false, errorMessages.join('\n'));
    }

    return isInvalid;
  }

  // #region loadDropDownList
  private loadDropDownList() {
    this.getListFactory();
    this.getListLevel();
    this.getListPermissionGroup();
  }

  // #region getTotalRows
  getTotalRows(isSearch?: boolean) {
    this.spinnerService.show()
    Object.assign(this.param, {
      date: this.functionUtility.getDateFormat(new Date(this.date)),
    });
    return this.service.getTotalRows(this.param).toPromise().then(res => {
      if (res.data != null)
        this.resultCount = res.data;
      if (isSearch) {
        this.snotifyService.success(this.translateService.instant('System.Message.QueryOKMsg'), this.translateService.instant('System.Caption.Success'));
        this.spinnerService.hide()
      }
    })
  }

  // #region getListFactory
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: res => {
        this.listFactory = res;
      }
    });
  }

  // #region onSelectFactory
  onSelectFactory() {
    this.deleteProperty('permissionGroups');
    this.getListPermissionGroup();
  }

  // #region getListLevel
  getListLevel() {
    this.service.getListLevel().subscribe({
      next: res => {
        this.listLevel = res;
      }
    });
  }

  // #region getListPermissionGroup
  getListPermissionGroup() {
    this.service.getListPermissionGroup(this.param.factory).subscribe({
      next: res => {
        this.listPermissionGroup = res
        this.selectAllForDropdownItems(this.listPermissionGroup)
      }
    })
  }
  private selectAllForDropdownItems(items: KeyValuePair[]) {
    let allSelect = (items: KeyValuePair[]) => {
      items.forEach(element => {
        element['allGroup'] = 'allGroup';
      });
    };
    allSelect(items);
  }

  // #region excel
  excel() {
    this.spinnerService.show();
    this.param.level_Name = this.listLevel.find(x => x.key === this.param.level)?.value;
    this.param.date = this.functionUtility.getDateFormat(new Date(this.date));
    if (this.checkRequiredParam()) return;
    this.param.date = this.functionUtility.getDateFormat(new Date(this.date));
    this.service.downloadExcel(this.param).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data.result, fileName);
          this.resultCount.queryResult = res.data.queryResult;
          this.resultCount.headCount = res.data.headCount;
          this.resultCount.monthlyAbsenteeism = res.data.monthlyAbsenteeism;
        } else {
          this.functionUtility.snotifySuccessError(false, 'System.Message.NoData');
        }
        this.spinnerService.hide();
      }
    });
  }


  // #region clear
  clear() {
    this.resultCount = <HRDailyReportCount>{ queryResult: 0, headCount: 0, monthlyAbsenteeism: 0 }
    this.date = '';
    this.totalPermissionGroup = 0;
    this.param.permissionGroups = [];
    this.deleteProperty('factory');
    this.deleteProperty('permissionGroup');
    this.deleteProperty('level');
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
