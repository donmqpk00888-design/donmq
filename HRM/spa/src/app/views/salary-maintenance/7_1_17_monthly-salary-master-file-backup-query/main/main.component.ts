import { S_7_1_17_MonthlySalaryMasterFileBackupQueryService } from '@services/salary-maintenance/s_7_1_17_monthly-salary-master-file-backup-query.service';
import { Component, OnDestroy, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { MonthlySalaryMasterFileBackupQueryDto, MonthlySalaryMasterFileBackupQueryParam, MonthlySalaryMasterFileBackupQuerySource } from '@models/salary-maintenance/7_1_17_monthly-salary-master-file-backup-query';
import { InjectBase } from '@utilities/inject-base-app';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig, BsDatepickerViewMode } from 'ngx-bootstrap/datepicker';
import { Observable } from 'rxjs';
import { TabComponentModel } from '@views/_shared/tab-component/tab.component';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  @ViewChild('salarySearchTab', { static: true }) salarySearchTab: TemplateRef<any>;
  @ViewChild('batchDataTab', { static: true }) batchDataTab: TemplateRef<any>;

  i18n: string = 'SalaryMaintenance.MonthlySalaryMasterFileBackupQuery.'

  tabs: TabComponentModel[] = [];
  selectedTab: string = ''

  title: string = '';
  programCode: string = '';
  pagination: Pagination = <Pagination>{}

  listFactory: KeyValuePair[] = [];
  listDepartment: KeyValuePair[] = [];
  listEmploymentStatus: KeyValuePair[] = [
    { key: 'Y', value: this.i18n + 'OnJob' },
    { key: 'N', value: this.i18n + 'Resigned' },
    { key: 'U', value: this.i18n + 'Unpaid' }
  ];
  listPositionTitle: KeyValuePair[] = [];
  listPermissionGroup: KeyValuePair[] = [];
  listSalaryType: KeyValuePair[] = [];

  salarySearch_Param: MonthlySalaryMasterFileBackupQueryParam = <MonthlySalaryMasterFileBackupQueryParam>{};
  batchData_Param: MonthlySalaryMasterFileBackupQueryParam = <MonthlySalaryMasterFileBackupQueryParam>{};
  minMode: BsDatepickerViewMode = 'month';
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM',
    minMode: this.minMode
  }
  yearMonth: Date;
  salarySearch_Data: MonthlySalaryMasterFileBackupQueryDto[] = [];
  totalPermissionGroup: number = 0;

  sourceItem: MonthlySalaryMasterFileBackupQueryDto
  iconButton = IconButton;
  classButton = ClassButton;
  placeholder = Placeholder;

  constructor(private service: S_7_1_17_MonthlySalaryMasterFileBackupQueryService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.initTab();
      this.loadDropdownList()
      this.processData()
    })
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }
  getSource() {
    const { salarySearch_Param, batchData_Param, salarySearch_Data, pagination, selected_Tab } = this.service.paramSearch();
    this.salarySearch_Param = salarySearch_Param;
    this.batchData_Param = batchData_Param;
    this.salarySearch_Data = salarySearch_Data;
    this.pagination = pagination;
    this.selectedTab = selected_Tab
    this.initTab();
    this.loadDropdownList()
    this.processData()
  }

  processData() {
    if (this.salarySearch_Data.length > 0) {
      if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams()) {
        this.getData()
      }
      else
        this.clear()
    }
  }
  loadDropdownList() {
    this.getListFactory();
    this.getListPositionTitle();
    this.getListPermissionGroup();
    this.getListSalaryType();
    if (!this.functionUtility.checkEmpty(this.salarySearch_Param.factory)) {
      this.getListDepartment();
    }
  }
  initTab() {
    this.tabs = [
      {
        id: 'salarySearch',
        title: this.translateService.instant(this.i18n + 'MonthlySalarySearch'),
        isEnable: true,
        content: this.salarySearchTab
      },
      {
        id: 'batchData',
        title: this.translateService.instant(this.i18n + 'BatchProductionData'),
        isEnable: true,
        content: this.batchDataTab
      },
    ]
  }

  ngOnDestroy(): void {
    this.service.setParamSearch(<MonthlySalaryMasterFileBackupQuerySource>{
      yearMonth: this.yearMonth,
      salarySearch_Param: this.salarySearch_Param,
      batchData_Param: this.batchData_Param,
      salarySearch_Data: this.salarySearch_Data,
      pagination: this.pagination,
      selectedData: this.sourceItem,
      selected_Tab: this.selectedTab
    })
  }

  checkRequiredParams(): boolean {
    var result =
      !this.functionUtility.checkEmpty(this.salarySearch_Param.year_Month_Str) &&
      !this.functionUtility.checkEmpty(this.salarySearch_Param.factory) &&
      !this.functionUtility.checkEmpty(this.salarySearch_Param.permission_Group);
    return result;
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
    this.deleteProperty('department');
    this.getListDepartment();
  }

  getListDepartment() {
    this.service.getListDepartment(this.salarySearch_Param.factory)
      .subscribe({
        next: (res) => {
          this.listDepartment = res;
        },
      });
  }

  getListPositionTitle() {
    this.getListData('listPositionTitle', this.service.getListPositionTitle.bind(this.service));
  }

  getListPermissionGroup() {
    this.service.getListPermissionGroup().subscribe({
      next: res => {
        this.listPermissionGroup = res;
        this.functionUtility.getNgSelectAllCheckbox(this.listPermissionGroup)
      }
    });
  }

  getListSalaryType() {
    this.getListData('listSalaryType', this.service.getListSalaryType.bind(this.service));
  }

  getListData(dataProperty: string, serviceMethod: () => Observable<any[]>): void {
    serviceMethod().subscribe({
      next: (res) => {
        this[dataProperty] = res;
      }
    });
  }
  //#endregion

  //#region getData
  getData(isSearch?: boolean) {
    this.spinnerService.show()
    this.service.getData(this.pagination, this.salarySearch_Param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          this.salarySearch_Data = res.data.result;
          this.pagination = res.data.pagination;
          if (isSearch)
            this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
        }
        else {
          this.functionUtility.snotifySuccessError(res.isSuccess, `${this.i18n}${res.error}`);
        }
      }
    })
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }
  //#endregion

  //#region query
  query(item: MonthlySalaryMasterFileBackupQueryDto) {
    this.sourceItem = { ...item }
    this.router.navigate([`${this.router.routerState.snapshot.url}/query`])
  }
  //#endregion

  //#region pageChanged
  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }
  //#endregion

  preventPaste(event: ClipboardEvent) {
    event.preventDefault();
    return false;
  }

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

  validateDecimal(event: KeyboardEvent, maxValue: number): boolean {
    const inputChar = event.key;

    const allowedKeys = ['Backspace', 'ArrowLeft', 'ArrowRight', 'Tab'];
    if (allowedKeys.includes(inputChar))
      return true;

    if (!/^\d$/.test(inputChar) && inputChar !== '.') {
      event.preventDefault();
      return false;
    }

    const input = event.target as HTMLInputElement;
    const currentValue = input.value;
    const newValue = currentValue.substring(0, input.selectionStart!) + inputChar + currentValue.substring(input.selectionEnd!);

    const parts = newValue.split('.');
    const decimalPartLength = parts.length > 1 ? parts[1].length : 0;

    if (decimalPartLength > 1) {
      event.preventDefault();
      return false;
    }

    const decimalRegex = /^[0-9]{0,3}(\.[0-9]{0,1})?$/;
    if (!decimalRegex.test(newValue)) {
      event.preventDefault();
      return false;
    }

    const newValueNum = parseFloat(newValue);
    if (newValueNum > maxValue) {
      event.preventDefault();
      return false;
    }

    if (newValueNum === maxValue && inputChar === '.') {
      event.preventDefault();
      return false;
    }

    return true;
  }
  //#endregion

  deleteProperty(name: string) {
    delete this[this.selectedTab + '_Param'][name]
  }
  changeTab() {
    this.getListFactory();
    this.getListDepartment();
    this.getListPositionTitle();
    this.getListPermissionGroup();
    this.getListSalaryType();
  }
  onDateChange() {
    this[this.selectedTab + '_Param'].year_Month_Str = this.functionUtility.isValidDate(new Date(this[this.selectedTab + '_Param'].year_Month))
      ? this.functionUtility.getDateFormat(new Date(this[this.selectedTab + '_Param'].year_Month))
      : '';
  }

  execute() {
    this.snotifyService.confirm(
      this.translateService.instant(this.i18n + 'ExecuteConfirm'),
      this.translateService.instant('System.Caption.Confirm'),
      () => {
        this.spinnerService.show();
        this.service.execute(this.batchData_Param).subscribe({
          next: (res) => {
            if (res.isSuccess) {
              this.functionUtility.snotifySuccessError(res.isSuccess, 'System.Message.CreateOKMsg');
              this.clear();
            } else {
              this.functionUtility.snotifySuccessError(res.isSuccess, `${this.i18n}${res.error}`);
            }
            this.spinnerService.hide();
          },
        });
      }
    );
  }
  clear() {
    const initParam = structuredClone(this.service.initData[this.selectedTab + '_Param'])
    this[this.selectedTab + '_Param'] = initParam
    if (this.selectedTab == 'salarySearch') {
      this.pagination.pageNumber = 1;
      this.pagination.totalCount = 0;
      this.salarySearch_Data = []
    }
  }
  download() {
    this.spinnerService.show();
    this.service.download(this.salarySearch_Param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Report')
        result.isSuccess
          ? this.functionUtility.exportExcel(result.data, fileName)
          : this.functionUtility.snotifySuccessError(result.isSuccess, `System.Message.${result.error}`)
      },
    });
  }
}

