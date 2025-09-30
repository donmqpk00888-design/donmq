import { Component, OnDestroy, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { ValidateResult } from '@models/base-source';
import { NightShiftSubsidyMaintenance_Param, NightShiftSubsidyMaintenanceSource } from '@models/salary-maintenance/7_1_20-night-shift-subsidy-maintenance';
import { S_7_1_20_NightShiftSubsidyMaintenanceService } from '@services/salary-maintenance/s_7_1_20_night-shift-subsidy-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    isAnimated: true,
    dateInputFormat: 'YYYY/MM/DD',
    minMode: 'month'
  };
  title: string = '';
  year_Month: Date = null;
  processedRecords: Number = null;
  iconButton = IconButton
  classButton = ClassButton
  placeholder = Placeholder
  param: NightShiftSubsidyMaintenance_Param = <NightShiftSubsidyMaintenance_Param>{}
  factories: KeyValuePair[] = []
  permissions: KeyValuePair[] = []
  constructor(private _services: S_7_1_20_NightShiftSubsidyMaintenanceService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getDropdownList()
    });
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }
  getSource() {
    this.param = this._services.programSource().param;
    this.processedRecords = this._services.programSource().processRecords;
    if (this.functionUtility.isValidDate(new Date(this.param.year_Month)))
      this.year_Month = new Date(this.param.year_Month);
    this.getDropdownList()
  }
  getDropdownList() {
    this.getFactory()
    if (this.param.factory)
      this.getPermissionGroup()
  }

  getFactory() {
    this._services.getFactories().subscribe({
      next: res => {
        this.factories = res
      }
    })
  }

  getPermissionGroup() {
    this._services.getPermissionGroup(this.param.factory).subscribe({
      next: res => {
        this.permissions = res
        this.selectAllForDropdownItems(this.permissions);
      }
    })
  }
  onFactoryChange() {
    this.param.permission = []
    this.getPermissionGroup()
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }

  onYearMonthChange() {
    this.param.year_Month = this.year_Month != null ? this.year_Month.toStringYearMonth() : ''
  }
  private selectAllForDropdownItems(items: KeyValuePair[]) {
    let allSelect = (items: KeyValuePair[]) => {
      items.forEach(element => {
        element['allGroup'] = 'allGroup';
      });
    };
    allSelect(items);
  }


  validateParam(): ValidateResult {
    if (this.functionUtility.checkEmpty(this.param.factory))
      return new ValidateResult('Please choose Factory');
    if (this.functionUtility.checkEmpty(this.param.permission))
      return new ValidateResult('Please choose Permission');
    return { isSuccess: true };
  }
  validateExcute() {
    return (
      this.functionUtility.checkEmpty(this.param.factory) ||
      this.functionUtility.checkEmpty(this.param.permission) ||
      this.year_Month?.toString() == "Invalid Date"
    )
  }
  ngOnDestroy(): void {
    this._services.setSource(<NightShiftSubsidyMaintenanceSource>{
      param: this.param,
      processRecords: this.processedRecords
    })
  }


  excuteConfirm() {
    this.snotifyService.confirm(
      this.translateService.instant(
        'SalaryMaintenance.NightShiftSubsidyMaintenance.ExecuteConfirm'
      ),
      this.translateService.instant('System.Caption.Confirm'),
      () => {
        this.spinnerService.show()
        this._services.checkData(this.param).subscribe({
          next: res => {
            this.spinnerService.hide()
            if (res.isSuccess) {
              if (res.error == 'DeleteData') {
                this.snotifyService.confirm(
                  this.translateService.instant(
                    'SalaryMaintenance.NightShiftSubsidyMaintenance.DeleteConfirm'
                  ),
                  this.translateService.instant('System.Caption.Confirm'),
                  () => {
                    this.param.is_Delete = true;
                    this.excute()
                  }
                )
              } else {
                this.param.is_Delete = false;
                this.excute()
              }
            } else {
              this.snotifyService.error(
                res.error,
                this.translateService.instant('System.Caption.Error')
              )
            }
          }
        })
      }
    )
  }

  excute() {
    this.spinnerService.show()
    this._services.excute(this.param).subscribe({
      next: res => {
        if (res.isSuccess) {
          this.snotifyService.success(
            this.translateService.instant('System.Message.CreateOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
          this.processedRecords = res.data
        } else {
          this.processedRecords = 0
          this.snotifyService.error(
            res.error ??
            this.translateService.instant('System.Message.CreateFailMsg'),
            this.translateService.instant('System.Caption.Error')
          )
        }
        this.spinnerService.hide()
      }
    })
  }

  clear() {
    this.year_Month = null
    this.param = <NightShiftSubsidyMaintenance_Param>{
      permission: []
    }
  }
}
