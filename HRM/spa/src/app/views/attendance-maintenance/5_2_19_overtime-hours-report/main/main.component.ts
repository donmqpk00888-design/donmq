import { Component, OnInit, effect } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import {
  OvertimeHoursReport,
  OvertimeHoursReportMemory,
  OvertimeHoursReportParam,
} from '@models/attendance-maintenance/5_2_19_overtime-hours-report';
import { S_5_2_19_OvertimeHoursReportService } from '@services/attendance-maintenance/s_5_2_19_overtime-hours-report.service';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss',
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  programCode: string = '';
  params: OvertimeHoursReportParam = <OvertimeHoursReportParam>{
    factory: '',
    department: '',
  };
  datas: OvertimeHoursReport[] = [];
  dateFrom: Date = null;
  dateTo: Date = null;
  totalRows: number = 0;
  iconButton = IconButton;

  factories: KeyValuePair[] = [];
  departments: KeyValuePair[] = [];
  workshifttypes: KeyValuePair[] = [];
  permissiongroup: KeyValuePair[] = [];
  list_kind: KeyValuePair[] = [
    {
      key: 'O',
      value:
        'AttendanceMaintenance.OvertimeHoursReport.OvertimeHoursStatistics',
    },
    {
      key: "D",
      value: 'AttendanceMaintenance.OvertimeHoursReport.DailyWorkingHoursStatistics'
    },
  ];

  bsConfig?: Partial<BsDatepickerConfig> = Object.assign(
    {},
    { isAnimated: true, dateInputFormat: 'YYYY/MM/DD' }
  );

  constructor(private _service: S_5_2_19_OvertimeHoursReportService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    effect(() => {
      this.params = this._service.paramSearch().params;
      this.datas = this._service.paramSearch().datas;
      this.totalRows = this.datas.length;
      if (!this.functionUtility.checkEmpty(this.params.date_From))
        this.dateFrom = new Date(this.params.date_From);
      if (!this.functionUtility.checkEmpty(this.params.date_To))
        this.dateTo = new Date(this.params.date_To);
      this.getListFactory();
      this.getListWorkShiftType();
      if (this.params.factory) {
        this.getListDepartment();
        this.GetListPermissionGroup();
        if (this.functionUtility.checkFunction('Search') && this.totalRows > 0) this.getData(false);
      }
    });

    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((res) => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.params.lang = res.lang;
        this.getListFactory();
        this.getListWorkShiftType();
        if (this.params.factory) {
          this.getListDepartment();
          this.GetListPermissionGroup();
          if (this.functionUtility.checkFunction('Search') && this.totalRows > 0)
            this.getData(false);
        }
      });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getListWorkShiftType();
  }
  getData(isSearch: boolean) {
    this.spinnerService.show();
    this.params.date_To =
      this.dateFrom == undefined
        ? ''
        : this.functionUtility.getDateFormat(this.dateTo);
    this.params.date_From =
      this.dateTo == undefined
        ? ''
        : this.functionUtility.getDateFormat(this.dateFrom);
    this._service.getData(this.params).subscribe({
      next: (res) => {
        this.datas = res;
        this.totalRows = res.length;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
        this.spinnerService.hide();
      },
    });
  }
  getListFactory() {
    this._service.getListFactoryAdd().subscribe({
      next: (res) => {
        this.factories = res;
      },
    });
  }
  getListDepartment() {
    this._service
      .getListDepartment(this.params.factory)
      .subscribe({
        next: (res) => {
          this.departments = res;
        },
      });
  }
  getListWorkShiftType() {
    this._service.getListWorkShiftType().subscribe({
      next: (res) => {
        this.workshifttypes = res;
      },
    });
  }
  GetListPermissionGroup() {
    this._service
      .GetListPermissionGroup(this.params.factory)
      .subscribe({
        next: (res) => {
          this.permissiongroup = res;
          this.selectAllForDropdownItems(this.permissiongroup)
        },
      });
  }
  private selectAllForDropdownItems(items: KeyValuePair[]) {
    let allSelect = (items: KeyValuePair[]) => {
      items.forEach(element => {
        element['allGroup'] = 'allGroup';
      });
    };
    allSelect(items);
  }
  changeFactory() {
    this.departments = [];
    this.permissiongroup = [];
    this.params.department = '';
    this.params.permission_Group = [];
    if (this.params.factory) {
      this.getListDepartment();
      this.GetListPermissionGroup();
    }
  }
  clear() {
    this.params = <OvertimeHoursReportParam>{
      factory: '',
      department: '',
      work_Shift_Type: '',
      kind: "O",
      lang: localStorage.getItem(LocalStorageConstants.LANG)
    };
    this.dateFrom = null;
    this.dateTo = null;
    this.totalRows = 0;
  }
  ngOnDestroy(): void {
    //Called once, before the instance is destroyed.
    //Add 'implements OnDestroy' to the class.
    this.params.date_To =
      this.dateFrom == undefined
        ? ''
        : this.functionUtility.getDateFormat(this.dateTo);
    this.params.date_From =
      this.dateTo == undefined
        ? ''
        : this.functionUtility.getDateFormat(this.dateFrom);
    this.setSource();
  }
  onExport() {
    this.spinnerService.show();
    this.params.date_To =
      this.dateFrom == undefined
        ? ''
        : this.functionUtility.getDateFormat(this.dateTo);
    this.params.date_From =
      this.dateTo == undefined
        ? ''
        : this.functionUtility.getDateFormat(this.dateFrom);
    this._service.exportExcel(this.params).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        if (result.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);
        } else {
          this.snotifyService.error(
            this.translateService.instant(result.error),
            this.translateService.instant('System.Caption.Error')
          );
        }
      },
    });
  }
  setSource() {
    let data: OvertimeHoursReportMemory = <OvertimeHoursReportMemory>{
      params: this.params,
      datas: this.datas
    }
    this._service.setParamSearch(data);
  }
}
