import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { ClassButton, IconButton, Placeholder } from '@constants/common.constants';
import { BatchUpdateData_Param, FinSalaryCloseMaintenance_Memory, FinSalaryCloseMaintenance_Param, FinSalaryCloseMaintenance_MainData } from '@models/salary-maintenance/7_1_26_fin-salary-close-maintenance';
import { S_7_1_26_FinSalaryCloseMaintenanceService } from '@services/salary-maintenance/s-7-1-26-fin-salary-close-maintenance.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { TabComponentModel } from '@views/_shared/tab-component/tab.component';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit {
  @ViewChild('salaryCloseSearchTab', { static: true }) salaryCloseSearchTab: TemplateRef<any>;
  @ViewChild('batchUpdateDataTab', { static: true }) batchUpdateDataTab: TemplateRef<any>;
  tabs: TabComponentModel[] = []
  programCode: string = '';
  title: string = ''
  selectedTab: string = ''

  //define param for tabs
  salaryCloseSearch_Param: FinSalaryCloseMaintenance_Param = <FinSalaryCloseMaintenance_Param>{
    kind: "O"
  }
  batchUpdateData_Param: BatchUpdateData_Param = <BatchUpdateData_Param>{
    kind: "O",
    close_Status: "Y"
  }
  //for list dropdown params
  totalPermissionGroup: number = 0;
  listFactory: KeyValuePair[] = []
  listDepartment: KeyValuePair[] = []
  listPermissionGroup: KeyValuePair[] = []
  listEmployeeID: string[] = []
  list_kind: KeyValuePair[] = [
    {
      key: 'O',
      value:
        'SalaryMaintenance.FinSalaryCloseMaintenance.OnJob',
    },
    {
      key: "R",
      value: 'SalaryMaintenance.FinSalaryCloseMaintenance.Resigned'
    },
    {
      key: "C",
      value: 'SalaryMaintenance.FinSalaryCloseMaintenance.Close'
    }
  ];
  list_kind_BatchData: KeyValuePair[] = [
    {
      key: 'O',
      value:
        'SalaryMaintenance.FinSalaryCloseMaintenance.OnJob',
    },
    {
      key: "R",
      value: 'SalaryMaintenance.FinSalaryCloseMaintenance.Resigned'
    }
  ];
  list_Close_Status: KeyValuePair[] = [
    {
      key: 'Y',
      value: 'Y',
    },
    {
      key: 'N',
      value: 'N',
    },
  ];

  //customs year month
  salaryCloseSearch_yearMonth: Date
  batchUpdateData_yearMonth: Date
  bsConfig: Partial<BsDatepickerConfig> = <Partial<BsDatepickerConfig>>{
    dateInputFormat: "YYYY/MM",
    minMode: "month"
  };

  //params main data
  salaryCloseSearch_Data: FinSalaryCloseMaintenance_MainData[] = []
  sourceItem: FinSalaryCloseMaintenance_MainData
  pagination: Pagination = <Pagination>{
    pageNumber: 1
  };

  //params css
  classButton = ClassButton;
  iconButton = IconButton;
  placeholder = Placeholder;
  constructor(private service: S_7_1_26_FinSalaryCloseMaintenanceService) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.initTab()
        this.loadDropdownList()
        this.processData()
      })
  }

  //#region ngOnInit, NgOnDestroy
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }
  ngOnDestroy(): void {
    this.service.setSource(<FinSalaryCloseMaintenance_Memory>{
      salaryCloseSearch_Param: this.salaryCloseSearch_Param,
      batchUpdateData_Param: this.batchUpdateData_Param,
      salaryCloseSearch_Data: this.salaryCloseSearch_Data,
      pagination: this.pagination,
      selectedData: this.sourceItem,
      selectedTab: this.selectedTab
    })
  }
  //#endregion

  //#region Load list drop down, get source, process data
  loadDropdownList() {
    this.getListFactory();
    this.getListPermissionGroup();
    if (!this.functionUtility.checkEmpty(this.salaryCloseSearch_Param.factory)) {
      this.getListDepartment();
    }
  }
  processData() {
    if (this.salaryCloseSearch_Data.length > 0) {
      if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams()) {
        this.getData()
      }
      else
        this.clear()
    }
  }

  getSource() {
    const { salaryCloseSearch_Param, batchUpdateData_Param, salaryCloseSearch_Data, pagination, selectedTab } = this.service.programSource();
    this.salaryCloseSearch_Data = salaryCloseSearch_Data;
    this.salaryCloseSearch_Param = salaryCloseSearch_Param;
    this.batchUpdateData_Param = batchUpdateData_Param;
    this.pagination = pagination;
    this.selectedTab = selectedTab;
    this[this.selectedTab + '_yearMonth'] = this[this.selectedTab + "_Param"].year_Month != null ? new Date(this[this.selectedTab + "_Param"].year_Month) : null;
    this.initTab()
    this.loadDropdownList()
    this.processData()
  }

  //#endregion

  //#region change state component
  initTab() {
    this.tabs = [
      {
        id: "salaryCloseSearch",
        title: this.translateService.instant("SalaryMaintenance.FinSalaryCloseMaintenance.SalaryCloseSearch"),
        isEnable: true,
        content: this.salaryCloseSearchTab
      },
      {
        id: "batchUpdateData",
        title: this.translateService.instant("SalaryMaintenance.FinSalaryCloseMaintenance.BathcUpdateData"),
        isEnable: true,
        content: this.batchUpdateDataTab
      }
    ]
  }

  onFactoryChange() {
    this.deleteProperty('department')
    this.deleteProperty('permission_Group')
    this.getListDepartment()
    this.getListPermissionGroup()
  }
  onDateChange() {
    this[this.selectedTab + '_Param'].year_Month = this[this.selectedTab + '_yearMonth'] != null ? this[this.selectedTab + '_yearMonth'].toStringYearMonth() : ''
  }
  onPermissionChange() {
    this.totalPermissionGroup = this[this.selectedTab + '_Param'].permission_Group.length
  }
  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  changeTab() {
    this.getListFactory();
    this.getListDepartment();
    this.getListPermissionGroup();
  }
  //#endregion

  //#region Get list dropdown
  getListFactory() {
    this.service.getListFactory().subscribe({
      next: res => {
        this.listFactory = res
      }
    })
  }

  getListDepartment() {
    this.service.GetDepartment(this.salaryCloseSearch_Param.factory).subscribe({
      next: res => {
        this.listDepartment = res
      }
    })
  }

  getListPermissionGroup() {
    this.service.getListPermissionGroup(this[this.selectedTab + '_Param'].factory).subscribe({
      next: res => {
        this.listPermissionGroup = res
        this.functionUtility.getNgSelectAllCheckbox(this.listPermissionGroup)
      }
    })
  }

  getListEmployeeID() {
    this.service.getListTypeHeadEmployeeID(this[this.selectedTab + '_Param'].factory).subscribe({
      next: res => {
        this.listEmployeeID = res
      }
    })
  }
  //#endregion

  //#region Action function
  deleteProperty(name: string) {
    delete this[this.selectedTab + '_Param'][name];
  }

  clear() {
    const initParam = structuredClone(this.service.initData[this.selectedTab + '_Param'])
    this[this.selectedTab + '_Param'] = initParam
    this[this.selectedTab + '_yearMonth'] = null
    if (this.selectedTab == 'salaryCloseSearch') {
      this.pagination.pageNumber = 1;
      this.pagination.totalCount = 0;
      this.salaryCloseSearch_Data = []
    }
  }
  edit(item: FinSalaryCloseMaintenance_MainData) {
    this.sourceItem = { ...item }
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`])
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }
  //#endregion

  //#region Fetch API

  getData(isSearch?: boolean) {
    this.spinnerService.show()
    this.service.getData(this.pagination, this.salaryCloseSearch_Param).subscribe({
      next: res => {
        this.spinnerService.hide()
        if (res.isSuccess) {
          this.salaryCloseSearch_Data = res.data.result
          this.pagination = res.data.pagination
          if (isSearch)
            this.functionUtility.snotifySuccessError(true, "System.Message.QuerySuccess")
        } else {
          this.snotifyService.error(
            this.translateService.instant("System.Message.QueryErrorMsg"),
            this.translateService.instant("System.Caption.Error")
          )
        }
      }
    })
  }
  download() {
    this.spinnerService.show();
    this.service.download(this.salaryCloseSearch_Param).subscribe({
      next: (res) => {
          this.spinnerService.hide();
          if (res.isSuccess) {
            if (res.error) {
              this.snotifyService.warning(
                this.translateService.instant(`System.Message.${res.error}`),
                this.translateService.instant('System.Caption.Warning')
              );
            } else {
              const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
              this.functionUtility.exportExcel(res.data, fileName);
            }
          } else {
            this.snotifyService.error(
              this.translateService.instant(`System.Message.${res.error}`),
              this.translateService.instant('System.Caption.Error'));
          }
        }
    });
  }
  excute(){
    this.spinnerService.show();
    this.service.excute(this.batchUpdateData_Param).subscribe({
      next: res =>{
        this.spinnerService.hide()
        if(res.isSuccess){
          this.functionUtility.snotifySuccessError(res.isSuccess, res.error)
        }else {
          this.functionUtility.snotifySuccessError(res.isSuccess, res.error)
        }
      }
    })


  }
  //#endregion


  //#region Valid input, form
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

  checkRequiredParams(): boolean {
    var result =
      !this.functionUtility.checkEmpty(this.salaryCloseSearch_Param.year_Month) &&
      !this.functionUtility.checkEmpty(this.salaryCloseSearch_Param.factory) &&
      !this.functionUtility.checkEmpty(this.salaryCloseSearch_Param.permission_Group);
    return result;
  }
  //#endregion

}

