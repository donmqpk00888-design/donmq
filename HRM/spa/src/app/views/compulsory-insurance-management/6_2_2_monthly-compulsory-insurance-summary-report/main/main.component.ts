import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { MonthlyCompulsoryInsuranceSummaryReport_Source, MonthlyCompulsoryInsuranceSummaryReport_Param } from '@models/compulsory-insurance-management/6_1_6_monthly-compulsory-insurance-summary-report';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_6_2_2_MonthlyCompulsoryInsuranceSummaryReportService } from '@services/compulsory-insurance-management/s_6_2_2_monthly-compulsory-insurance-summary-report.service';
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
  classButton = ClassButton;
  iconButton = IconButton;

  totalPermissionGroup: number = 0;

  title: string = ''
  programCode: string = '';

  year_Month: Date
  total: number = 0;
  source: MonthlyCompulsoryInsuranceSummaryReport_Source;

  param: MonthlyCompulsoryInsuranceSummaryReport_Param = <MonthlyCompulsoryInsuranceSummaryReport_Param>{
    kind: "On Job",
    permission_Group: [],
    permission_Group_Name: []
  }
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: 'YYYY/MM',
    minMode: 'month',
  }

  listFactory: KeyValuePair[] = [];
  listInsuranceType: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = []
  key: KeyValuePair[] = [
    {
      'key': "On Job",
      'value': 'CompulsoryInsuranceManagement.MonthlyCompulsoryInsuranceSummaryReport.OnJob'
    },
    {
      'key': "Resigned",
      'value': 'CompulsoryInsuranceManagement.MonthlyCompulsoryInsuranceSummaryReport.Resigned'
    }
  ]
  constructor(
    private service: S_6_2_2_MonthlyCompulsoryInsuranceSummaryReportService
  ) {
    super()
    this.programCode = this.route.snapshot.data['program'];
    this.getDataFromSource();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadDropdownListSearch();
      if (!this.functionUtility.checkEmpty(this.param.factory)) this.getListPermissionGroup();
    });


  }


  getDataFromSource() {
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.total = this.service.paramSearch().total;
      this.totalPermissionGroup = this.param.permission_Group?.length
      this.loadDropdownListSearch()
      if (!this.functionUtility.checkEmpty(this.param.year_Month)) this.year_Month = new Date(this.param.year_Month);
      if (!this.functionUtility.checkEmpty(this.param.factory)) {
        this.getListDepartment()
        this.getListPermissionGroup()
      }
    })
  }

  loadDropdownListSearch() {
    this.getListFactory();
    this.getListInsuranceType();
    if (this.param.factory) {
      this.getListDepartment();
    }
  }
  checkRequiredParams() {
    if (!this.param.factory || !this.param.insurance_Type || !this.param.permission_Group || !this.param.year_Month)
      return false
    return true
  }
  ngOnDestroy(): void {
    this.service.setParamSearch(<MonthlyCompulsoryInsuranceSummaryReport_Source>{
      param: this.param,
      total: this.total
    })
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }

  deleteProperty = (name: string) => delete this.param[name]
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res: KeyValuePair[]) => this.listFactory = res
    });
  }

  getListDepartment() {
    this.service.getDepartmentList(this.param.factory).subscribe({
      next: res => {
        this.listDepartment = res
      }
    })
  }

  getListPermissionGroup() {
    this.service.getPermissionGroup(this.param.factory).subscribe({
      next: res => {
        this.listPermissionGroup = res
        this.selectAllForDropdownItems(this.listPermissionGroup)
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



  download() {
    this.param.permission_Group.forEach((key, i) => {
      let matchingPermission = this.listPermissionGroup.find(x => x.key === key);
      if (matchingPermission) {
        this.param.permission_Group_Name[i] = matchingPermission.value;
      }
    });
    this.param.insurance_Type_Full = this.listInsuranceType.find(x => x.key === this.param.insurance_Type)?.value
    this.param.year_Month = this.year_Month.toStringDate();
    this.spinnerService.show();
    this.service.downloadExcel(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        if (result.isSuccess) {
          this.total = result.data.count;
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data.result, fileName);
          this.param.permission_Group_Name = [];
        }
        else {
          this.total = 0;
          this.snotifyService.warning(this.translateService.instant(result.error), this.translateService.instant('System.Caption.Warning'));
        }
      }
    });
  }

  getCountRecords(isSearch?: boolean) {
    this.spinnerService.show()
    this.service.getCountRecords(this.param).subscribe({
      next: res => {
        this.spinnerService.hide()
        this.total = res
        if (isSearch) {
          this.snotifyService.success(
            this.translateService.instant('System.Message.SearchOKMsg'),
            this.translateService.instant('System.Caption.Success')
          );
        }

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
  onChangeFactory() {
    this.param.permission_Group = [];
    this.getListPermissionGroup();
    this.deleteProperty("department");
    this.listDepartment = [];
    if (this.param.factory) {
      this.getListDepartment();
    }
  }

  onChangePermission() {
    this.totalPermissionGroup = this.param.permission_Group.length;
  }

  onChangeEffectiveMonth() {
    this.param.year_Month = (!this.functionUtility.checkEmpty(this.year_Month)
      && (this.year_Month.toString() != 'Invalid Date' && this.year_Month.toString() != 'NaN/NaN'))
      ? this.functionUtility.getDateFormat(this.year_Month)
      : "";
  }

  validateYearMonth(event: KeyboardEvent): void {
    const inputField = event.target as HTMLInputElement;
    let input = inputField.value;
    const key = event.key;

    const allowedKeys = ['Backspace', 'Tab', 'ArrowLeft', 'ArrowRight'];
    if (allowedKeys.includes(key))
      return;

    if (!/^\d$/.test(key) && key !== '/') {
      event.preventDefault();
      return;
    }

    if (!input.includes('/') && input.length === 4 && key !== '/') {
      inputField.value = input + '/';
      input = inputField.value;
    }

    if (key === '/')
      if (input.length !== 4 || input.includes('/'))
        event.preventDefault();

    if (input === '000' && key === '0') {
      this.resetToCurrentDate(inputField);
      event.preventDefault();
    }

    if (input.includes('/') && input.split('/')[1].length < 2) {
      const monthPart = input.split('/')[1] + key;
      const month = parseInt(monthPart, 10);

      if (month > 12 || monthPart === '00') {
        event.preventDefault();
      }
    }
  }

  resetToCurrentDate(inputField: HTMLInputElement): void {
    const currentDate = new Date();
    const year = currentDate.getFullYear();
    const month = String(currentDate.getMonth() + 1).padStart(2, '0');
    inputField.value = `${year}/${month}`;
  }

  clear() {
    this.spinnerService.show()
    this.param = <MonthlyCompulsoryInsuranceSummaryReport_Param>{
      kind: "On Job",
      permission_Group: [],
      permission_Group_Name: []
    }
    this.year_Month = null
    this.listDepartment = []
    this.total = 0
    this.spinnerService.hide()
  }
}
