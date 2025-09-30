import { Component, OnDestroy, OnInit } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { NightShiftExtraAndOvertimePayParam, NightShiftExtraAndOvertimePaySource } from '@models/salary-report/7_2_14_tax-paying-employee-monthly-night-shift-extra-and-overtime-pay';
import { S_7_2_14_taxPayingEmployeeMonthlyNightShiftExtraAndOvertimePayService } from '@services/salary-report/s_7_2_14_tax-paying-employee-monthly-night-shift-extra-and-overtime-pay.service';
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
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;

  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month'
  };

  param: NightShiftExtraAndOvertimePayParam = <NightShiftExtraAndOvertimePayParam>{}
  title: string = ''
  year_Month: Date
  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  totalRows: number = 0;
  totalPermissionGroup: number = 0;
  programCode: string = '';

  constructor(private service: S_7_2_14_taxPayingEmployeeMonthlyNightShiftExtraAndOvertimePayService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropDownList()
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource();
  }

  ngOnDestroy(): void {
    this.service.setSource(<NightShiftExtraAndOvertimePaySource>{
      param: this.param,
      totalRows: this.totalRows
    })
  }

  getSource() {
    this.param = this.service.programSource().param;
    this.totalRows = this.service.programSource().totalRows;
    this.loadDropDownList();
    if (this.param.year_Month)
      this.year_Month = new Date(this.param.year_Month)
  }

  private loadDropDownList() {
    this.getListFactory();
    this.getListDepartment();
    this.getListPermissionGroup();
  }

  getTotalRows(isSearch?: boolean) {
    this.spinnerService.show()
    this.service.getTotalRows(this.param).subscribe({
      next: res => {
        this.spinnerService.hide()
        if (res.isSuccess) {
          this.totalRows = res.data
          if (isSearch)
            this.snotifyService.success(this.translateService.instant('System.Message.QueryOKMsg'),
              this.translateService.instant('System.Caption.Success'));
        } else {
          this.snotifyService.error(this.translateService.instant(res.error ?? 'System.Message.SystemError'),
            this.translateService.instant('System.Caption.Error'));
        }
      }
    })
  }

  clear() {
    this.year_Month = null;
    this.totalRows = 0;
    this.param = <NightShiftExtraAndOvertimePayParam>{
      permission_Group: []
    }
  }

  download() {
    this.spinnerService.show()
    this.service.download(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          this.totalRows = res.data.totalRows
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data.excel, fileName);
        } else {
          this.totalRows = 0
          this.snotifyService.error(this.translateService.instant(res.error), this.translateService.instant('System.Caption.Error'));
        }
      }
    })
  }

  onSelectFactory() {
    this.deleteProperty('department');
    this.deleteProperty('permission_Group');
    this.getListDepartment();
    this.getListPermissionGroup();
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res: KeyValuePair[]) => this.listFactory = res
    });
  }

  getListDepartment() {
    if (this.param.factory)
      this.service.getListDepartment(this.param.factory).subscribe({
        next: (res: KeyValuePair[]) => this.listDepartment = res
      });
  }

  getListPermissionGroup() {
    if (this.param.factory)
      this.service.getListPermissionGroup(this.param.factory).subscribe({
        next: res => {
          this.listPermissionGroup = res
          this.functionUtility.getNgSelectAllCheckbox(this.listPermissionGroup)
        }
      })
  }

  onYearMonthChange() {
    this.param.year_Month = this.functionUtility.isValidDate(this.year_Month) ? this.year_Month.toStringYearMonth() : ''
  }

  onPermissionChange() {
    this.totalPermissionGroup = this.param.permission_Group.length;
  }

  deleteProperty = (name: string) => delete this.param[name]
}
