import { Component, effect, OnInit } from '@angular/core';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { WeeklyWorkingHoursReport_Basic, WeeklyWorkingHoursReportDto, WeeklyWorkingHoursReportParam } from '@models/attendance-maintenance/5_2_3_weekly-working-hours-report';
import { ClassButton, IconButton } from '@constants/common.constants';
import { S_5_2_3_WeeklyWorkingHoursReportService } from '@services/attendance-maintenance/s_5_2_3_weekly-working-hours-report.service';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  programCode: string = '';
  param: WeeklyWorkingHoursReportParam = <WeeklyWorkingHoursReportParam>{
  };
  lastSearchParam: WeeklyWorkingHoursReportParam;
  data: WeeklyWorkingHoursReportDto[] = [];
  countRecord: number = 0;
  date_Start_Value: Date;
  date_End_Value: Date;
  iconButton = IconButton;
  classButton = ClassButton;
  listFactory: KeyValuePair[] = [];
  listLevel: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  source: WeeklyWorkingHoursReport_Basic;
  key: KeyValuePair[] = [
      {
        'key': 'Department',
        'value': 'AttendanceMaintenance.WeeklyWorkingHoursReport.Department'
      },
      {
        'key': 'Personal',
        'value': 'AttendanceMaintenance.WeeklyWorkingHoursReport.Personal'
      },
    ];
  constructor(
    private service: S_5_2_3_WeeklyWorkingHoursReportService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.getDataFromSource();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(()=> {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getListLevel();
      if(this.param.factory)
        this.getListDepartment();
    });
  }

  ngOnInit() {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListFactory();
    this.getListLevel();
    this.lastSearchParam = { ...this.param };
  }

  ngOnDestroy(): void {
    if (!this.source)
      this.source = <WeeklyWorkingHoursReport_Basic>{
        param: { ...this.param }
      };
    this.service.setSource(this.source);
  }


  getDataFromSource() {
    effect(() => {
      this.param = this.service.paramSource().param;

      if (this.param.date_Start != null)
        this.date_Start_Value = this.param.date_Start.toDate();
      if (this.param.date_End != null)
        this.date_End_Value = this.param.date_End.toDate();

      if (this.checkRequiredParams() && this.functionUtility.checkFunction('Search'))
        this.getCountRecords(false);
      if (this.param.factory)
        this.getListDepartment();
    });
  }
  checkRequiredParams() {
    if (this.param.level != null && this.param.factory != null && this.param.date_Start != null && this.param.date_End != null)
      return true
    else return false
  }

  checkDate() {
    if (this.date_Start_Value != null)
      this.param.date_Start = this.functionUtility.getDateFormat(this.date_Start_Value);
    else this.deleteProperty('date_Start');

    if (this.date_End_Value != null)
      this.param.date_End = this.functionUtility.getDateFormat(this.date_End_Value);
    else this.deleteProperty('date_End');
  }

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }
  getListDepartment() {
    this.service.getLisDepartment(this.param.factory).subscribe({
      next: (res) => {
        this.listDepartment = res;
      },
    });
  }

  getListLevel() {
    this.service.getListLevel().subscribe({
      next: (res) => {
        this.listLevel = res;
      },
    });
  }

  getCountRecords(isSearch?: boolean) {
    this.spinnerService.show();
    this.paramCheck()
    this.service.getCountRecords(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.countRecord = res;
        if (isSearch) {
          this.lastSearchParam = { ...this.param };
          this.snotifyService.success(
            this.translateService.instant('System.Message.SearchOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
        }
      },
    });
  }
  paramChanged(): boolean {
    return JSON.stringify(this.param) !== JSON.stringify(this.lastSearchParam);
  }
  paramCheck() {
    this.param.date_Start = (!this.functionUtility.checkEmpty(this.date_Start_Value)
      && (this.date_Start_Value.toString() != 'Invalid Date' && this.date_Start_Value.toString() != 'NaN/NaN'))
      ? this.functionUtility.getDateFormat(this.date_Start_Value)
      : "";

    this.param.date_End = (!this.functionUtility.checkEmpty(this.date_End_Value)
      && (this.date_End_Value.toString() != 'Invalid Date' && this.date_End_Value.toString() != 'NaN/NaN'))
      ? this.functionUtility.getDateFormat(this.date_End_Value)
      : "";

    this.param.language = localStorage.getItem(LocalStorageConstants.LANG);
  }

  clear() {
    this.countRecord = 0
    this.date_End_Value = null
    this.date_Start_Value = null
    this.deleteProperty('factory')
    this.deleteProperty('date_Start')
    this.deleteProperty('date_End')
    this.deleteProperty('level')
    this.deleteProperty("department");
  }

  download() {
    this.spinnerService.show();
    this.paramCheck()
    this.service.download(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data.result, fileName);
          this.countRecord = res.data.count
        } else this.functionUtility.snotifySuccessError(res.isSuccess, res.error)
      },
    });
  }
  deleteProperty = (name: string) => delete this.param[name]

  changeDateStartValue() {
    if (this.date_Start_Value != null && isNaN(this.date_Start_Value.getTime()))
      this.date_Start_Value = null;
  }
  changeDateEndValue() {
    if (this.date_End_Value != null && isNaN(this.date_End_Value.getTime()))
      this.date_End_Value = null;
  }
  onChangeFactory(){
    this.getListDepartment();
    this.deleteProperty("department");
  }

}

