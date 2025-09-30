import { Component, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { MonthlyAdditionsAndDeductionsSummaryReport_Param } from '@models/salary-report/7_2_22_monthly-additions-and-deductions-summary-report';
import { S_7_2_22_MonthlyAdditionsAndDeductionsSummaryReport } from '@services/salary-report/s_7_2_22_monthly-additions-and-deductions-summary-report.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit {
  i18n: string = 'SalaryReport.MonthlyAdditionsAndDeductionsSummaryReport.'
  title: string = '';
  programCode: string = '';
  iconButton = IconButton;
  classButton = ClassButton;
  param: MonthlyAdditionsAndDeductionsSummaryReport_Param = <MonthlyAdditionsAndDeductionsSummaryReport_Param>{}
  factoryList: KeyValuePair[] = [];
  departmentList: KeyValuePair[] = [];
  permissionGroupList: KeyValuePair[] = [];

  constructor(private service: S_7_2_22_MonthlyAdditionsAndDeductionsSummaryReport) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getFactoryList()
      this.getDropDownList()
    });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.param = this.service.paramSearch();
    this.getFactoryList()
    this.getDropDownList()
  }
  ngOnDestroy(): void {
    this.service.setParamSearch(this.param);
  }
  callFunction(func: string) {
    this.param.function_Type = func
    this[func]()
  }
  search() {
    this.spinnerService.show();
    this.service.process(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          this.functionUtility.snotifySuccessError(res.isSuccess, 'System.Message.SearchOKMsg');
          this.param.total_Rows = res.data
        } else {
          this.functionUtility.snotifySuccessError(res.isSuccess, `${this.i18n}${res.error}`);
        }
      }
    })
  }

  excel() {
    this.spinnerService.show();
    this.service
      .process(this.param)
      .subscribe({
        next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            if (res.data.count > 0) {
              const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
              this.functionUtility.exportExcel(res.data.result, fileName);
            }
            this.param.total_Rows = res.data.count
          } else {
            this.functionUtility.snotifySuccessError(res.isSuccess, `${this.i18n}${res.error}`);
          }
        }
      });
  }

  clear() {
    this.param = structuredClone(this.service.initData)
  }
  getFactoryList() {
    this.service.getFactoryList()
      .subscribe({
        next: (res) => {
          this.factoryList = res;
        }
      });
  }
  getDropDownList() {
    if (this.param.factory) {
      this.service
        .getDropDownList(this.param)
        .subscribe({
          next: (res) => {
            this.filterList(res)
          }
        });
    }
  }
  filterList(keys: KeyValuePair[]) {
    this.departmentList = structuredClone(keys.filter((x: { key: string; }) => x.key == "DE")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.permissionGroupList = structuredClone(keys.filter((x: { key: string; }) => x.key == "PE")).map(x => <KeyValuePair>{ key: x.key = x.value.substring(0, x.value.indexOf('-')), value: x.value })
    this.functionUtility.getNgSelectAllCheckbox(this.permissionGroupList)
  }

  onDateChange() {
    this.param.year_Month_Str = this.functionUtility.isValidDate(new Date(this.param.year_Month))
      ? this.functionUtility.getDateFormat(new Date(this.param.year_Month))
      : '';
  }

  onFactoryChange() {
    this.getDropDownList()
    this.deleteProperty('department')
    this.deleteProperty('permission_Group')
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }
}
