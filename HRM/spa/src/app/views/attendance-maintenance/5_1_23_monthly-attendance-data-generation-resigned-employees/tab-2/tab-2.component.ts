import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { ResignedMonthlyDataCloseParam } from '@models/attendance-maintenance/5_1_23_monthly-attendance-data-generation-resigned-employees';
import { S_5_1_23_MonthlyAttendanceDataGenerationResignedEmployeesService } from '@services/attendance-maintenance/s_5_1_23_monthly-attendance-data-generation-resigned-employees.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'monthly-data-close-resigned',
  templateUrl: './tab-2.component.html',
  styleUrl: './tab-2.component.scss',
})
export class Tab2Component extends InjectBase implements OnInit {
  iconButton = IconButton;
  classButton = ClassButton;
  param: ResignedMonthlyDataCloseParam = <ResignedMonthlyDataCloseParam>{};
  bsConfigMonthly: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };
  att_Month: Date;
  closeStatusCodes: KeyValuePair[] = [
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' },
  ];
  listFactory: KeyValuePair[] = [];
  constructor(
    private _service: S_5_1_23_MonthlyAttendanceDataGenerationResignedEmployeesService
  ) {
    super();

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((res) => {
        this.getListFactoryAdd();
      });

    effect(() => {
      this.param = this._service.programSource2();
      if(this.param.att_Month)
        this.att_Month = new Date(this.param.att_Month)
    });
  }

  ngOnInit(): void {
    this.getListFactoryAdd();
  }

  ngOnDestroy(){
    this._service.setSource2(this.param)
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
          this.clear()
        } else {
          this.snotifyService.error(
            res.error ?? 'Close failed',
            this.translateService.instant('System.Caption.Error')
          );
        }
      },
    });
  }

  clear() {
    this.param = <ResignedMonthlyDataCloseParam>{
      factory: '',
      pass: null,
    };
    this.att_Month = null;
  }

  onChangeAttMonth(){
    this.param.att_Month = this.att_Month != null ? this.att_Month.toStringYearMonth() : '';
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
