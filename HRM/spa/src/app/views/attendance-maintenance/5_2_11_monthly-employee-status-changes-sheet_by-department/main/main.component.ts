import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { S_5_2_11_monthlyEmployeeStatusChangesSheet_byDepartmentService } from '@services/attendance-maintenance/s_5_2_11_monthly-employee-status-changes-sheet_by-department.service';
import { LangChangeEvent } from '@ngx-translate/core';
import {
  MonthlyEmployeeStatus_ByDepartmentParam,
  MonthlyEmployeeStatus_ByDepartmentSource
} from '@models/attendance-maintenance/5_2_11_monthly-employee-status-changes-sheet_by-department';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  classButton = ClassButton;
  param: MonthlyEmployeeStatus_ByDepartmentParam = <MonthlyEmployeeStatus_ByDepartmentParam>{
    permissionGroup: [],
    permissionName: []
  }
  totalPermissionGroup: number = 0;
  totalRows: number = 0;
  bsConfigMonthly: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };
  yearMonth: Date = null;
  listFactory: KeyValuePair[] = []
  listLevel: KeyValuePair[] = []
  listPermissionGroup: KeyValuePair[] = []

  constructor(private service: S_5_2_11_monthlyEmployeeStatusChangesSheet_byDepartmentService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListFactory();
      this.getListLevel();
      if (!this.functionUtility.checkEmpty(this.param.factory)) this.getListPermissionGroup();
    });
    this.getDataFromSource();
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }
  ngOnDestroy(): void {
    if (!this.functionUtility.checkEmpty(this.yearMonth)) this.param.yearMonth = this.yearMonth.toStringDate()
    this.service.setSource(<MonthlyEmployeeStatus_ByDepartmentSource>{
      param: this.param,
      totalRows: this.totalRows
    })
  }



  getDataFromSource() {
    effect(() => {
      this.param = this.service.programSource().param;
      this.totalRows = this.service.programSource().totalRows;
      this.totalPermissionGroup = this.param.permissionGroup.length
      this.getListFactory();
      this.getListLevel();
      if (!this.functionUtility.checkEmpty(this.param.yearMonth)) this.yearMonth = new Date(this.param.yearMonth);
      if (!this.functionUtility.checkEmpty(this.param.factory)) this.getListPermissionGroup();
    })
  }

  getTotalRows(isSearch?: boolean) {
    this.param.yearMonth = this.yearMonth.toStringDate()
    this.param.firstDate = this.yearMonth.toFirstDateOfMonth().toStringDate()
    this.param.lastDate = this.yearMonth.toLastDateOfMonth().toStringDate()
    this.spinnerService.show()
    this.service.getTotalRows(this.param).subscribe({
      next: res => {
        this.spinnerService.hide()
        this.totalRows = res
        if (isSearch)
          this.snotifyService.success(this.translateService.instant('System.Message.QueryOKMsg'), this.translateService.instant('System.Caption.Success'));
      }
    })
  }

  excel() {
    this.param.levelName = this.listLevel.find(x => x.key === this.param.level).value;
    this.param.permissionGroup.forEach((key, i) => {
      let matchingPermission = this.listPermissionGroup.find(x => x.key === key);
      if (matchingPermission) {
        this.param.permissionName[i] = matchingPermission.value;
      }
    });

    this.param.yearMonth = this.yearMonth.toStringDate()
    this.param.firstDate = this.yearMonth.toFirstDateOfMonth().toStringDate()
    this.param.lastDate = this.yearMonth.toLastDateOfMonth().toStringDate()
    this.spinnerService.show();
    this.service.downloadExcel(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide()
        if (result.isSuccess) {
          this.totalRows = result.data.count
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data.result, fileName);
          this.param.permissionName = []
        }
        else {
          this.totalRows = 0
          this.snotifyService.warning(result.error, this.translateService.instant('System.Caption.Warning'));
        }
      }
    });
  }
  onSelectFactory() {
    this.param.permissionGroup = []
    this.getListPermissionGroup()
  }
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: res => {
        this.listFactory = res
      }
    })
  }
  getListLevel() {
    this.service.getListLevel().subscribe({
      next: res => this.listLevel = res
    })
  }
  getListPermissionGroup() {
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

  clear() {
    this.totalRows = 0
    this.yearMonth = null
    this.deleteProperty('factory')
    this.deleteProperty('yearMonth')
    this.deleteProperty('level')
    this.param.permissionGroup = []
    this.param.permissionName = []
    this.totalPermissionGroup = 0
  }

  onPermissionChange() {
    this.totalPermissionGroup = this.param.permissionGroup.length;
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }


}
