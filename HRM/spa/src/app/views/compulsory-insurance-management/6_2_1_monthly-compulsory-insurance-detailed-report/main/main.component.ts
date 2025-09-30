import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { InjectBase } from '@utilities/inject-base-app';
import { LangChangeEvent } from '@ngx-translate/core';
import { KeyValuePair } from '@utilities/key-value-pair';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { MonthlyCompulsoryInsuranceDetailedReportParam, MonthlyCompulsoryInsuranceDetailedReportSource } from '@models/compulsory-insurance-management/6_1_5_monthly-compulsory-insurance-detailed-report';
import { S_6_2_1_MonthlyCompulsoryInsuranceDetailedReportService } from '@services/compulsory-insurance-management/s_6_2_1_monthly-compulsory-insurance-detailed-report.service';
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
  param: MonthlyCompulsoryInsuranceDetailedReportParam = <MonthlyCompulsoryInsuranceDetailedReportParam>{
    permission_Group: [],
    permission_Group_Name: [],
    kind: 'O'
  }
  totalPermissionGroup: number = 0;
  totalRows: number = 0;
  bsConfigMonthly: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  };
  yearMonth: Date = null;
  listFactory: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listInsuranceType: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listKind: KeyValuePair[] = [
    {
      key: 'O',
      value:
        'CompulsoryInsuranceManagement.MonthlyCompulsoryInsuranceDetailedReport.OnJob',
    },
    {
      key: "R",
      value: 'CompulsoryInsuranceManagement.MonthlyCompulsoryInsuranceDetailedReport.Resigned'
    },
  ];
  constructor(private service: S_6_2_1_MonthlyCompulsoryInsuranceDetailedReportService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropdownListSearch();
      if (!this.functionUtility.checkEmpty(this.param.factory)) this.getListPermissionGroup();
    });
    this.getDataFromSource();
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }
  ngOnDestroy(): void {
    if (!this.functionUtility.checkEmpty(this.yearMonth)) this.param.year_Month = this.yearMonth.toStringDate()
    this.service.setSource(<MonthlyCompulsoryInsuranceDetailedReportSource>{
      param: this.param,
      totalRows: this.totalRows
    })
  }

  getDataFromSource() {
    effect(() => {
      this.param = this.service.programSource().param;
      this.totalRows = this.service.programSource().totalRows;
      this.totalPermissionGroup = this.param.permission_Group.length
      this.loadDropdownListSearch();
      if (!this.functionUtility.checkEmpty(this.param.year_Month)) this.yearMonth = new Date(this.param.year_Month);
      if (!this.functionUtility.checkEmpty(this.param.factory)) this.getListPermissionGroup();
    })
  }

  loadDropdownListSearch(){
    this.getListFactory();
    this.getListInsuranceType();
    if (this.param.factory) {
      this.getDepartments();
    }
  }

  getTotalRows(isSearch?: boolean) {
    this.param.year_Month = this.yearMonth.toStringDate()
    if (this.param.year_Month == "NaN/NaN/NaN" || this.param.year_Month.split('/')?.[0] == "0")
      return this.snotifyService.warning(this.translateService.instant('CompulsoryInsuranceManagement.MonthlyCompulsoryInsuranceDetailedReport.InvalidDate'), this.translateService.instant('System.Caption.Error'));
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

  disableField() {
    return !this.param.factory || this.yearMonth == null || !this.param.kind || !this.param.insurance_Type || this.functionUtility.checkEmpty(this.param.permission_Group)
  }

  excel() {
    this.param.permission_Group.forEach((key, i) => {
      let matchingPermission = this.listPermissionGroup.find(x => x.key === key);
      if (matchingPermission) {
        this.param.permission_Group_Name[i] = matchingPermission.value;
      }
    });
    this.param.insurance_Type_Full = this.listInsuranceType.find(x => x.key === this.param.insurance_Type)?.value
    this.param.year_Month = this.yearMonth.toStringDate();
    if (this.param.year_Month == "NaN/NaN/NaN" || this.param.year_Month.split('/')?.[0] == "0")
      return this.snotifyService.warning(this.translateService.instant('CompulsoryInsuranceManagement.MonthlyCompulsoryInsuranceDetailedReport.InvalidDate'), this.translateService.instant('System.Caption.Error'));
    this.spinnerService.show();
    this.service.downloadExcel(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        if (result.isSuccess) {
          this.totalRows = result.data.count;
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data.result, fileName);
          this.param.permission_Group_Name = [];
        }
        else {
          this.totalRows = 0;
          this.snotifyService.warning(result.error, this.translateService.instant('System.Caption.Warning'));
        }
      }
    });
  }

  onSelectFactory() {
    this.param.permission_Group = [];
    this.getListPermissionGroup();
    this.deleteProperty("department");
    this.listDepartment = [];
    if (this.param.factory) {
      this.getDepartments();
    }
  }

  getListFactory() {
    this.service.getListFactory().subscribe({
      next: res => {
        this.listFactory = res
      }
    })
  }

  getListInsuranceType() {
    this.service.getListInsuranceType().subscribe({
      next: res => {
        this.listInsuranceType = res
      }
    })
  }

  getListPermissionGroup() {
    this.service.getListPermissionGroupByFactory(this.param.factory).subscribe({
      next: res => {
        this.listPermissionGroup = res
        this.selectAllForDropdownItems(this.listPermissionGroup)
      }
    })
  }

  getDepartments() {
    this.service.getDepartments(this.param.factory).subscribe({
      next: res => {
        this.listDepartment = res;
      }
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

  clear() {
    this.totalRows = 0
    this.yearMonth = null
    this.deleteProperty('factory')
    this.deleteProperty('year_Month')
    this.deleteProperty('insurance_Type')
    this.deleteProperty('department')
    this.param.permission_Group = []
    this.param.permission_Group_Name = []
    this.param.insurance_Type_Full = ''
    this.param.kind = 'O';
    this.totalPermissionGroup = 0
  }

  onPermissionChange() {
    this.totalPermissionGroup = this.param.permission_Group.length;
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }


}
