import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { GenerationResigned } from '@models/attendance-maintenance/5_1_23_monthly-attendance-data-generation-resigned-employees';
import { S_5_1_23_MonthlyAttendanceDataGenerationResignedEmployeesService } from '@services/attendance-maintenance/s_5_1_23_monthly-attendance-data-generation-resigned-employees.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'data-generation-resigned',
  templateUrl: './tab-1.component.html',
  styleUrl: './tab-1.component.scss',
})
export class Tab1Component extends InjectBase implements OnInit {
  iconButton = IconButton;
  classButton = ClassButton;
  param: GenerationResigned = <GenerationResigned>{};
  att_Month: Date;
  resign_Date: Date;

  bsConfigMonthly: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM/DD',
  };

  listFactory: KeyValuePair[] = [];

  constructor(
    private _service: S_5_1_23_MonthlyAttendanceDataGenerationResignedEmployeesService
  ) {
    super();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((res) => {
        this.getListFactoryAdd();
      });

    effect(() => {
      this.param = this._service.programSource1();
      if(this.param.att_Month)
        this.att_Month = new Date(this.param.att_Month)

      if(this.param.resign_Date)
        this.resign_Date = new Date(this.param.resign_Date)
    });
  }

  ngOnInit(): void {
    this.getListFactoryAdd();
  }

  ngOnDestroy(){
    this._service.setSource1(this.param)
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
                'AttendanceMaintenance.MonthlyAttendanceDataGenerationResignedEmployees.MessageConfirm'
              ),
              this.translateService.instant('System.Caption.Confirm'),
              () => {
                this.param.is_Delete = true;
                this.monthlyAttendanceDataGeneration()
              }
            );
          } else {
            this.param.is_Delete = false;
            this.monthlyAttendanceDataGeneration()
          }
        } else {
          this.snotifyService.error(
            this.translateService.instant(
              'AttendanceMaintenance.MonthlyAttendanceDataGenerationResignedEmployees.AccountClose'
            ),
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
          this.clear()
        } else {
          this.snotifyService.error(
            res.error ?? this.translateService.instant('System.Message.CreateErrorMsg'),
            this.translateService.instant('System.Caption.Error')
          );
        }
        this.spinnerService.hide();
      },
    });
  }

  onChangeAttMonth(){
    this.param.att_Month = this.att_Month != null ? this.att_Month.toStringYearMonth() : '';
  }

  onChangeResignDate(){
    this.param.resign_Date = this.resign_Date != null ? this.resign_Date.toStringDate() : '';
  }

  onChangeWorkingDays() {
    if (this.param.working_Days < 0)
      this.param.working_Days = 0;
  }

  clear() {
    this.param = <GenerationResigned>{}
    this.att_Month = null;
    this.resign_Date = null;
  }

  //#region Get List
  getListFactoryAdd() {
    this._service.getListFactoryAdd().subscribe({
      next: (res) => {
        this.listFactory = res;
      },
    });
  }
  //#endregion
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
