import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { ActiveMonthlyDataCloseParam } from '@models/attendance-maintenance/5_1_21_monthly-attendance-data-generation-active-employees';
import { S_5_1_21_MonthlyAttendanceDataGenerationActiveEmployeesService } from '@services/attendance-maintenance/s_5_1_21_monthly-attendance-data-generation-active-employees.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'monthly-data-close-active',
  templateUrl: './tab-3.component.html',
  styleUrl: './tab-3.component.scss',
})
export class Tab3Component extends InjectBase implements OnInit {
  iconButton = IconButton;
  classButton = ClassButton;
  param: ActiveMonthlyDataCloseParam = <ActiveMonthlyDataCloseParam>{};
  att_Month: Date;
  listFactory: KeyValuePair[] = [];
  closeStatusCodes: KeyValuePair[] = [
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' },
  ];
  bsConfigMonthly: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };

  constructor(
    private _service: S_5_1_21_MonthlyAttendanceDataGenerationActiveEmployeesService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((res) => {
        this.getListFactoryAdd();
      });

    effect(() => {
      this.param = this._service.programSource3();
      if(this.param.att_Month)
        this.att_Month = new Date(this.param.att_Month)
    });
  }

  ngOnInit(): void {
    this.getListFactoryAdd();
  }

  ngOnDestroy(){
    this._service.setSource3(this.param);
  }

  monthlyDataClose() {
    this.spinnerService.show();
    this._service.monthlyDataClose(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          this.snotifyService.success(
            'Close successfully',
            this.translateService.instant('System.Caption.Success')
          );
          this.clear();
        } else {
          this.snotifyService.error(
            res.error ?? 'Close failed',
            this.translateService.instant('System.Caption.Error')
          );
        }
      },
    });
  }

  onChangeAttMonth(){
    this.param.att_Month = this.att_Month != null ? this.att_Month.toStringYearMonth() : '';
  }

  clear() {
    this.param = <ActiveMonthlyDataCloseParam>{ pass: null };
    this.att_Month = null;
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
