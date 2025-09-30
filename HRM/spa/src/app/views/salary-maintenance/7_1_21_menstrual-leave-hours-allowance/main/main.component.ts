import { Component, effect, OnInit } from '@angular/core';
import { S_7_1_21_MenstrualLeaveHoursAllowanceService } from '@services/salary-maintenance/s_7_1_21_menstrual-leave-hours-allowance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { MenstrualLeaveHoursAllowanceParam, MenstrualLeaveHoursAllowanceSource } from '@models/salary-maintenance/7_1_21_menstrual-leave-hours-allowance';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { LangChangeEvent } from '@ngx-translate/core';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss',
})
export class MainComponent extends InjectBase implements OnInit {
  title: string;
  param: MenstrualLeaveHoursAllowanceParam = <MenstrualLeaveHoursAllowanceParam>{  }

  year_Month: Date
  totalPermissionGroup: number = 0;
  listFactory: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };

  totalRows: number = 0;

  classButton = ClassButton;
  iconButton = IconButton;
  placeholder = Placeholder

  constructor(private service: S_7_1_21_MenstrualLeaveHoursAllowanceService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList();
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }
  getSource() {
    this.param = this.service.programSource().param;
    this.totalRows = this.service.programSource().totalRows;
    if (this.functionUtility.isValidDate(new Date(this.param.year_Month)))
      this.year_Month = new Date(this.param.year_Month);
    this.loadDropDownList()
  }

  ngOnDestroy(): void {
    this.service.setSource(<MenstrualLeaveHoursAllowanceSource>{
      param: this.param,
      totalRows: this.totalRows
    })
  }

  execute() {
    this.snotifyService.confirm(
      this.translateService.instant(
        'SalaryMaintenance.MenstrualLeaveHoursAllowance.ExecuteConfirm'
      ),
      this.translateService.instant('System.Caption.Confirm'),
      () => {
        this.spinnerService.show();
        this.service.checkData(this.param).subscribe({
          next: (res) => {
            this.spinnerService.hide();
            if (res.isSuccess) {
              if (res.error == 'DeleteData') {
                this.snotifyService.confirm(
                  this.translateService.instant(
                    'SalaryMaintenance.MenstrualLeaveHoursAllowance.DeleteConfirm'
                  ),
                  this.translateService.instant('System.Caption.Confirm'),
                  () => {
                    this.param.is_Delete = true;
                    this.executeData();
                  }
                );
              } else {
                this.param.is_Delete = false;
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
    this.service.execute(this.param).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          this.snotifyService.success(
            this.translateService.instant('System.Message.CreateOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
          this.totalRows = res.data
          this.clear();
        } else {
          this.totalRows = 0;
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

  clear() {
    this.year_Month = null;
    this.param = <MenstrualLeaveHoursAllowanceParam>{
      permission_Group: []
    }
  }

  loadDropDownList() {
    this.getListFactory();
    this.getListPermissionGroup();
  }

  onFactoryChange() {
    this.param.permission_Group = [];
    this.getListPermissionGroup();
  }

  onYearMonthChange() {
    this.param.year_Month = this.functionUtility.isValidDate(this.year_Month) ? this.year_Month.toStringYearMonth() : ''
  }

  onPermissionChange() {
    this.totalPermissionGroup = this.param.permission_Group.length;
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
    if (this.param.factory)
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
  //#endregion
}
