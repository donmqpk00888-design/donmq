import { Component, OnInit, OnDestroy, effect } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { ClassButton, IconButton } from '@constants/common.constants';
import { S_5_2_16_IndividualMonthlyWorkingHoursReportService } from '@services/attendance-maintenance/s_5_2_16_individual-monthly-working-hours-report.service';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { LangChangeEvent } from '@ngx-translate/core';
import { IndividualMonthlyWorkingHoursReportMemory, IndividualMonthlyWorkingHoursReportParam } from '@models/attendance-maintenance/5_2_16_individual-monthly-working-hours-report';import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss',
  host: { 'id': 'main-5-2-16' }
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy{
  title: string = '';
  programCode: string = '';
  totalRows: number = 0;
  param: IndividualMonthlyWorkingHoursReportParam = <IndividualMonthlyWorkingHoursReportParam>{};
  yearMonth: Date = null;
  factorys: KeyValuePair[] = [];
  permissionGroups: KeyValuePair[] = [];
  minMode: BsDatepickerViewMode = 'month';
  bsConfig: Partial<BsDatepickerConfig> = {
    isAnimated: true,
    dateInputFormat: 'YYYY/MM',
    minMode: this.minMode
  };
  iconButton = IconButton;
  classButton = ClassButton;

  constructor(private service: S_5_2_16_IndividualMonthlyWorkingHoursReportService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getFactorys();
      if(!this.functionUtility.checkEmpty(this.param.factory)) {
        this.getPermissionGroups();
      }
    });
    effect(() => {
      let mainSignalData = this.service.signalDataMain();
      this.param = mainSignalData?.param;
      this.totalRows = mainSignalData?.totalRows;
      this.yearMonth = !this.functionUtility.checkEmpty(this.param.yearMonth) ? new Date(this.param.yearMonth) : null;
      if(!this.functionUtility.checkEmpty(this.param.factory)) {
        this.getPermissionGroups();
      }
    });
  }

  ngOnDestroy(): void {
    this.service.signalDataMain.set(<IndividualMonthlyWorkingHoursReportMemory>{
      param: this.param,
      totalRows: this.totalRows
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getFactorys();
  }

  getPermissionGroups() {
    this.service.getPermissionGroups(this.param.factory).subscribe({
      next: result => {
        this.permissionGroups = result;
        this.selectAllForDropdownItems(this.permissionGroups)
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

  getFactorys() {
    this.spinnerService.show()
    this.service.getFactorys().subscribe({
      next: result => {
        this.spinnerService.hide()
        this.factorys = result;
      }
    })
  }

  onChangeFactory() {
    this.param.permission_Group = "";
    this.param.permissionGroupTemp = [];
    if(!this.functionUtility.checkEmpty(this.param.factory)) {
      this.getPermissionGroups();
    }
  }

  clear() {
    this.param = <IndividualMonthlyWorkingHoursReportParam>{};
    this.totalRows = 0;
    this.yearMonth = null;
  }

  search(isFlag?: boolean) {
    this.spinnerService.show();
    this.param.yearMonth = this.yearMonth ? new Date(this.yearMonth).toFirstDateOfMonth().toStringDate() : '';
    this.param.permission_Group = this.param.permissionGroupTemp.join(',');
    this.service.getData(this.param).subscribe({
      next: res => {
        this.spinnerService.hide();
        this.totalRows = res.data?.dataExcels?.length;
        if (isFlag) {
          this.functionUtility.snotifySuccessError(isFlag, 'System.Message.QuerySuccess');
        }
      }
    });
  }

  excel() {
    this.spinnerService.show();
    this.param.yearMonth = this.yearMonth ? new Date(this.yearMonth).toFirstDateOfMonth().toStringDate() : '';
    this.param.permission_Group = this.param.permissionGroupTemp.join(',');
    this.service.excel(this.param).subscribe({
      next: res => {
        this.spinnerService.hide();
        if(res.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data, fileName);
        } else {
          if(res.error !== null) {
            this.functionUtility.snotifySuccessError(false, res.error);
          }
        }
      }
    });
  }

}
