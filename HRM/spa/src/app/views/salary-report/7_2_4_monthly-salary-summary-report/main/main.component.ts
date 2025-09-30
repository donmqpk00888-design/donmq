import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Component, OnInit, OnDestroy } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { MonthlySalarySummaryReportParam, MonthlySalarySummaryReportSource } from '@models/salary-report/7_2_4_monthly-salary-summary-report';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { S_7_2_4_MonthlySalarySummaryReportService } from '@services/salary-report/s_7_2_4_monthly-salary-summary-report.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';

  listFactory: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listLevel: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  kind: KeyValuePair[] = [
    { key: 'Y', value: 'SalaryReport.MonthlySalarySummaryReport.OnJob' },
    { key: 'N', value: 'SalaryReport.MonthlySalarySummaryReport.Resigned' },
  ];
  transfer: KeyValuePair[] = [
    { key: 'All', value: 'SalaryReport.MonthlySalarySummaryReport.All' },
    { key: 'Y', value: 'SalaryReport.MonthlySalarySummaryReport.Yes' },
    { key: 'N', value: 'SalaryReport.MonthlySalarySummaryReport.No' },
  ];
  report_Kind: KeyValuePair[] = [
    { key: 'G', value: 'SalaryReport.MonthlySalarySummaryReport.GroupByLevel' },
    { key: 'D', value: 'SalaryReport.MonthlySalarySummaryReport.DepartmentDetail' },
  ];

  param: MonthlySalarySummaryReportParam = <MonthlySalarySummaryReportParam>{
    kind: 'Y',
    transfer: 'All',
    permission_Group: [],
    report_Kind: 'G'
  };
  minMode: BsDatepickerViewMode = 'month';
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM',
    minMode: this.minMode
  }
  totalPermissionGroup: number = 0;
  totalRows: number = 0;

  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;

  constructor(private service: S_7_2_4_MonthlySalarySummaryReportService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program']);
      this.getListFactory();
      this.getListPermissionGroup();
      this.getListLevel();
      this.getListDepartment();
    })
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource();
  }

  ngOnDestroy(): void {
    this.service.setParamSearch(<MonthlySalarySummaryReportSource>{
      param: this.param,
      totalRow: this.totalRows
    })
  }

  getSource() {
    this.param = this.service.paramSearch().param;
    this.totalRows = this.service.paramSearch().totalRow;
    this.getListFactory();
    this.getListPermissionGroup();
    this.getListLevel();
    this.getListDepartment();
  }

  //#region getList
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: (res) => {
        this.listFactory = res;
      }
    })
  }

  onChangeFactory() {
    this.deleteProperty('permission_Group')
    this.deleteProperty('department');
    this.getListPermissionGroup();
    this.getListDepartment();
  }

  onChangeReportKind() {
    this.deleteProperty('level');
  }

  getListLevel() {
    this.service.getListLevel().subscribe({
      next: (res) => {
        this.listLevel = res;
      }
    })
  }

  getListDepartment() {
    this.service.getListDepartment(this.param.factory)
      .subscribe({
        next: (res) => {
          this.listDepartment = res;
        },
      });
  }

  getListPermissionGroup() {
    this.service.getListPermissionGroup(this.param.factory).subscribe({
      next: res => {
        this.listPermissionGroup = res;
        this.functionUtility.getNgSelectAllCheckbox(this.listPermissionGroup)
      }
    });
  }
  //#endregion

  //#region getTotal
  getTotal() {
    this.spinnerService.show();
    this.service.getTotal(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide()
        this.totalRows = res;
        this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess');
      }
    })
  }
  //#endregion

  //#region downloadExcel
  download() {
    this.spinnerService.show();
    this.service.downloadExcel(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
        if (res.isSuccess) {
          this.functionUtility.exportExcel(res.data.fileData, fileName)
          this.totalRows = res.data.totalCount;
        }
        else {
          this.functionUtility.snotifySuccessError(res.isSuccess, res.error);
          this.totalRows = 0;
        }
      },
    });
  }
  //#endregion

  //#region clear
  clear() {
    this.param = <MonthlySalarySummaryReportParam>{
      kind: 'Y',
      transfer: 'All',
      report_Kind: 'G'
    };
    this.listPermissionGroup = [];
    this.listDepartment = [];
    this.totalRows = 0;
  }
  //#endregion

  //#region validate
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

    if (input.length >= 7)
      event.preventDefault();

    if (key === '/')
      if (input.length !== 4 || input.includes('/'))
        event.preventDefault();

    if (input.includes('/') && input.length > 4 && input.split('/')[1].length >= 2)
      event.preventDefault();

    if (input === '000' && key === '0') {
      this.resetToCurrentDate(inputField);
      event.preventDefault();
    }

    if (input.includes('/') && input.length === 6) {
      const monthPart = input.split('/')[1] + key;
      const month = parseInt(monthPart, 10);

      if (month < 1 || month > 12 || monthPart === '00') {
        this.resetToCurrentDate(inputField);
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
  //#endregion

  //#region Common
  onDateChange() {
    this.param.year_Month_Str = this.functionUtility.isValidDate(new Date(this.param.year_Month))
      ? this.functionUtility.getDateFormat(new Date(this.param.year_Month))
      : '';
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }

  checkParam() {
    return (
      this.functionUtility.checkEmpty(this.param.factory) ||
      this.functionUtility.checkEmpty(this.param.year_Month_Str) ||
      this.functionUtility.checkEmpty(this.param.permission_Group) ||
      (this.param.report_Kind == 'G' ? this.functionUtility.checkEmpty(this.param.level) : false)
    );
  }

  onPermissionChange() {
    this.totalPermissionGroup = this.param.permission_Group.length;
  }
  //#endregion
}
