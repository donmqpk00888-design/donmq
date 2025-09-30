import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { GenerationActiveParam } from '@models/attendance-maintenance/5_1_21_monthly-attendance-data-generation-active-employees';
import { S_5_1_21_MonthlyAttendanceDataGenerationActiveEmployeesService } from '@services/attendance-maintenance/s_5_1_21_monthly-attendance-data-generation-active-employees.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'data-generation-active',
  templateUrl: './tab-1.component.html',
  styleUrl: './tab-1.component.scss',
})
export class Tab1Component extends InjectBase implements OnInit {
  iconButton = IconButton;
  classButton = ClassButton;
  param: GenerationActiveParam = <GenerationActiveParam>{};

  att_Month: Date = null;
  deadline_Start: Date = null;
  deadline_End: Date = null;
  minDateStart: Date = null;
  minDateEnd: Date = null;
  maxDateStart: Date = null;
  maxDateEnd: Date = null;

  bsConfigMonthly: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };

  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];

  constructor(
    private _service: S_5_1_21_MonthlyAttendanceDataGenerationActiveEmployeesService
  ) {
    super();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((res) => {
        this.getListFactoryAdd();
        this.getListDepartment();
      });

    effect(() => {
      this.param = this._service.programSource1();
      if(this.param.att_Month)
        this.att_Month = new Date(this.param.att_Month)

      if(this.param.deadline_Start)
        this.deadline_Start = new Date(this.param.deadline_Start)

      if(this.param.deadline_End)
        this.deadline_End = new Date(this.param.deadline_End)
    });
  }

  ngOnInit(): void {
    this.getListFactoryAdd();
  }

  ngOnDestroy(){
    this._service.setSource1(this.param)
  }

  changeFactory() {
    this.getListDepartment();
  }

  monthlyAttendanceDataGenerationExecute() {
    this.spinnerService.show();
    this._service.checkParam(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          if (res.error == 'DeleteData') {
            this.snotifyService.confirm(
              this.translateService.instant(
                'This area already exists, do you want to continue?'
              ),
              this.translateService.instant('System.Caption.Confirm'),
              () => {
                this.param.is_Delete = true;
                this.monthlyAttendanceDataGeneration();
              }
            );
          } else {
            this.param.is_Delete = false;
            this.monthlyAttendanceDataGeneration();
          }
        } else {
          this.snotifyService.error(
            res.error,
            this.translateService.instant('System.Caption.Error')
          );
        }
      }
    });
  }

  monthlyAttendanceDataGeneration() {
    this.spinnerService.show();
    this._service.monthlyAttendanceDataGeneration(this.param).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          this.snotifyService.success(
            this.translateService.instant('System.Message.CreateOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
          this.clear();
        } else {
          this.snotifyService.error(
            res.error ??
            this.translateService.instant('System.Message.CreateErrorMsg'),
            this.translateService.instant('System.Caption.Error')
          );
        }
        this.spinnerService.hide();
      },
    });
  }

  onChangeWorkingDays() {
    if (this.param.working_Days < 0) this.param.working_Days = 0;
  }

  clear() {
    this.param = <GenerationActiveParam>{};
    this.att_Month = null;
    this.deadline_Start = null;
    this.deadline_End = null;
    this.listDepartment = [];
  }

  onChangeAttMonth() {
    this.param.att_Month = this.att_Month != null ? this.att_Month.toStringYearMonth() : '';
    this.deadline_Start = null;
    this.deadline_End = null;
    this.changeDate();
  }

  onChangeDeadline(name: string){
    this.param[name] = this[name] != null ? this[name].toStringDate('yyyy/MM/dd') : '';
    this.changeDate();
  }

  changeDate() {
    let firstDate = new Date(this.att_Month).toFirstDateOfMonth();
    let lastDate = new Date(this.att_Month).toLastDateOfMonth();

    if (!this.deadline_Start) {
      this.minDateStart = firstDate;
      this.minDateEnd = firstDate;
    } else if (this.deadline_Start >= firstDate) {
      this.minDateEnd = this.deadline_Start;
    }

    if (!this.deadline_End) {
      this.maxDateStart = lastDate;
      this.maxDateEnd = lastDate;
    } else if (this.deadline_End <= lastDate) {
      this.maxDateEnd = lastDate;
      this.maxDateStart = this.deadline_End;
    }
  }

  //#region Get List
  getListFactoryAdd() {
    this._service.getListFactoryAdd().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }

  getListDepartment() {
    this._service
      .getListDepartment(this.param.factory)
      .subscribe({
        next: (res) => {
          this.listDepartment = res;
        },
      });
  }
  //#endregion
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
