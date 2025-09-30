import { Component, input, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { MonthlyDataLockParam } from '@models/salary-maintenance/7_1_22_monthly-salary-generation';
import { S_7_1_22_MonthlySalaryGenerationService } from '@services/salary-maintenance/s_7_1_22_monthly-salary-generation.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'monthly-data-lock-active',
  templateUrl: './tab-2.component.html',
  styleUrl: './tab-2.component.scss'
})
export class Tab2Component extends InjectBase implements OnInit {
  param = input.required<MonthlyDataLockParam>()
  year_Month: Date
  totalPermissionGroup: number = 0;
  listFactory: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listSalaryLock: KeyValuePair[] = [
    { key: 'Y', value: 'Y' },
    { key: "N", value: 'N' },
  ];
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };
  classButton = ClassButton;
  iconButton = IconButton;
  placeholder = Placeholder

  constructor(private service: S_7_1_22_MonthlySalaryGenerationService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.loadDropDownList();
    });
  }

  ngOnInit(): void {
    if (this.param().year_Month)
      this.year_Month = new Date(this.param().year_Month)
    this.loadDropDownList();
  }

  execute() {
    this.snotifyService.confirm(
      this.translateService.instant('SalaryMaintenance.MonthlySalaryGeneration.ExecuteConfirm'),
      this.translateService.instant('System.Caption.Confirm'),
      () => this.executeData()
    );
  }

  executeData() {
    this.spinnerService.show();
    this.service.monthlyDataLockExecute(this.param()).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          this.snotifyService.success(
            this.translateService.instant('System.Message.CreateOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
          this.param().totalRows = res.data
        } else {
          this.param().totalRows = 0;
          this.snotifyService.error(
            this.translateService.instant(res.error ? 'SalaryMaintenance.MonthlySalaryGeneration.' + res.error : 'System.Message.CreateErrorMsg'),
            this.translateService.instant('System.Caption.Error')
          );
        }
        this.spinnerService.hide();
      },
    });
  }

  loadDropDownList() {
    this.getListFactory();
    if (this.param().factory)
      this.getListPermissionGroup();
  }

  onYearMonthChange() {
    this.param().year_Month = this.year_Month != null ? this.year_Month.toStringYearMonth() : ''
  }

  onFactoryChange() {
    this.param().permission_Group = [];
    this.getListPermissionGroup();
  }

  onPermissionChange() {
    this.totalPermissionGroup = this.param().permission_Group.length;
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

  getListPermissionGroup() {
    this.service.getListPermissionGroup(this.param().factory).subscribe({
      next: res => {
        this.listPermissionGroup = res
        this.functionUtility.getNgSelectAllCheckbox(this.listPermissionGroup)
      }
    })
  }
  //#endregion
}
