import { Component, input, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { MonthlySalaryGenerationParam } from '@models/salary-maintenance/7_1_22_monthly-salary-generation';
import { S_7_1_22_MonthlySalaryGenerationService } from '@services/salary-maintenance/s_7_1_22_monthly-salary-generation.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'monthly-salary-generation-active',
  templateUrl: './tab-1.component.html',
  styleUrl: './tab-1.component.scss'
})
export class Tab1Component extends InjectBase implements OnInit {
  param = input.required<MonthlySalaryGenerationParam>()
  year_Month: Date
  totalPermissionGroup: number = 0;
  listFactory: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
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
      () => {
        this.spinnerService.show();
        this.service.checkData(this.param()).subscribe({
          next: (res) => {
            this.spinnerService.hide();
            if (res.isSuccess) {
              if (res.error == 'DeleteData') {
                this.snotifyService.confirm(
                  this.translateService.instant('SalaryMaintenance.MonthlySalaryGeneration.DeleteConfirm'),
                  this.translateService.instant('System.Caption.Confirm'),
                  () => {
                    this.param().is_Delete = true;
                    this.executeData();
                  }
                );
              } else {
                this.param().is_Delete = false;
                this.executeData();
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
    );
  }

  executeData() {
    this.spinnerService.show();
    this.service.monthlySalaryGenerationExecute(this.param()).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          if (res.data.error)
            this.snotifyService.warning(res.data.error, this.translateService.instant('System.Caption.Warning'));
          this.snotifyService.success(
            this.translateService.instant('System.Message.CreateOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
          this.param().totalRows = res.data.count
        } else {
          this.param().totalRows = 0;
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
    delete this.param()[name]
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
