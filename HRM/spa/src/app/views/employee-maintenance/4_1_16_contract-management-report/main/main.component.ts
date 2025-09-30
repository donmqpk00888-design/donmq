import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { Contract_Management_ReportParamSource, ContractManagementReportDto, ContractManagementReportParam } from '@models/employee-maintenance/4_1_16_contract-management-report';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_4_1_16_ContractManagementReportService } from '@services/employee-maintenance/s_4_1_16_contract-management-report.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  programCode: string = '';
  param: ContractManagementReportParam = <ContractManagementReportParam>{};
  data: ContractManagementReportDto[] = [];
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10
  };
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM/DD'
  };
  iconButton = IconButton;
  division: KeyValuePair[] = [];
  factory: KeyValuePair[] = [];
  department: KeyValuePair[] = [];
  contractType: KeyValuePair[] = [];
  documentType: KeyValuePair[] = [
    { key: '1', value: 'EmployeeInformationModule.ContractManagementReport.New' },
    { key: '2', value: 'EmployeeInformationModule.ContractManagementReport.Renewal' }
  ];
  constructor(
    private service: S_4_1_16_ContractManagementReportService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadData()
    });
    this.getDataFromSource()
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }
  ngOnDestroy(): void {
    this.service.setSource(<Contract_Management_ReportParamSource>{
      param: this.param,
      currentPage: this.pagination,
      dataMain: this.data
    });
  }
  getDataFromSource() {
    effect(() => {
      this.pagination = this.service.programSource().currentPage;
      this.param = this.service.programSource().param;
      this.data = this.service.programSource().dataMain;
      this.loadData()
    })
  }
  private loadData() {
    this.getListDivision();
    this.getListFactory();
    this.getListContractType();
    this.getListDepartment()
    if (this.data.length > 0) {
      if (this.functionUtility.checkFunction('Search')) {
        if (this.checkRequiredParams())
          this.getData(false)
      }
      else
        this.clear()
    }
  }
  checkRequiredParams(): boolean {
    var result = !this.functionUtility.checkEmpty(this.param.division) &&
      !this.functionUtility.checkEmpty(this.param.factory)
    return result;
  }
  getData(isSearch?: boolean) {
    this.conditionSearch()
    this.spinnerService.show()
    this.service.getData(this.pagination, this.param).subscribe({
      next: res => {
        this.spinnerService.hide()
        this.data = res.result
        this.pagination = res.pagination
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QueryOKMsg')
      }
    })
  }

  excel() {
    this.conditionSearch()
    this.spinnerService.show();
    this.service.downloadExcel(this.param).subscribe({
      next: (result) => {
        this.spinnerService.hide();
        if (result.isSuccess){
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(result.data, fileName);
        }
        else
          this.snotifyService.warning(result.error, this.translateService.instant('System.Caption.Warning'));
      }
    });
  }

  clear() {
    this.data = []
    this.pagination.totalCount = 0
    this.param = <ContractManagementReportParam>{ document_Type: '1' };
  }

  conditionSearch() {
    this.param.onboard_Date_From = !this.functionUtility.checkEmpty(this.param.onboard_Date_From_Date) ? this.functionUtility.getDateFormat(new Date(this.param.onboard_Date_From_Date)) : ''
    this.param.onboard_Date_To = !this.functionUtility.checkEmpty(this.param.onboard_Date_To_Date) ? this.functionUtility.getDateFormat(new Date(this.param.onboard_Date_To_Date)) : ''
    this.param.contract_End_From = !this.functionUtility.checkEmpty(this.param.contract_End_From_Date) ? this.functionUtility.getDateFormat(new Date(this.param.contract_End_From_Date)) : ''
    this.param.contract_End_To = !this.functionUtility.checkEmpty(this.param.contract_End_To_Date) ? this.functionUtility.getDateFormat(new Date(this.param.contract_End_To_Date)) : ''
    this.param.document_Type_Name = this.translateService.instant(this.documentType.find(item => item.key === this.param.document_Type).value)
    const startIndex = this.department.findIndex(item => item.key === this.param.department_From);
    const endIndex = this.department.findIndex(item => item.key === this.param.department_To);
    this.param.department = this.department.slice(startIndex, endIndex + 1).map(item => item.key);
  }
  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }
  onSelectDivision() {
    this.deleteProperty('factory')
    this.getListFactory()
    this.onSelectFactory()
  }
  onSelectFactory() {
    this.deleteProperty('contract_Type')
    this.getListContractType();
    this.deleteProperty('department_From')
    this.deleteProperty('department_To')
    this.getListDepartment()
  }

  getListDivision() {
    this.service.getListDivision().subscribe({
      next: res => {
        this.division = res
      }
    })
  }
  getListFactory() {
    this.service.getListFactory(this.param.division).subscribe({
      next: res => {
        this.factory = res
      }
    })
  }

  getListContractType() {
    this.service.getListContractType(this.param.division, this.param.factory).subscribe({
      next: res => {
        this.contractType = res
      }
    })
  }
  getListDepartment() {
    this.service.getListDepartment(this.param.division, this.param.factory).subscribe({
      next: res => this.department = res
    })
  }
  clearForm() {
    this.deleteProperty('onboard_Date_From_Date')
    this.deleteProperty('onboard_Date_To_Date')
    this.deleteProperty('contract_End_From_Date')
    this.deleteProperty('contract_End_To_Date')
  }
  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  deleteProperty(name: string) {
    delete this.param[name];
  }

  onChangeOnboardFrom() {
    if (this.param.onboard_Date_From_Date == 'Invalid Date')
      this.deleteProperty('onboard_Date_From_Date')
    if (this.param.onboard_Date_To_Date != '' && this.param.onboard_Date_From_Date > this.param.onboard_Date_To_Date)
      this.param.onboard_Date_From_Date = this.param.onboard_Date_To_Date
  }
  onChangeOnboardTo() {
    if (this.param.onboard_Date_To_Date == 'Invalid Date')
      this.deleteProperty('onboard_Date_To_Date')
    if (this.param.onboard_Date_From_Date != '' && this.param.onboard_Date_To_Date < this.param.onboard_Date_From_Date)
      this.param.onboard_Date_To_Date = this.param.onboard_Date_From_Date
  }
  onChangeContractFrom() {
    if (this.param.contract_End_From_Date == 'Invalid Date')
      this.deleteProperty('contract_End_From_Date')
    if (this.param.contract_End_To_Date != '' && this.param.contract_End_From_Date > this.param.contract_End_To_Date)
      this.param.contract_End_From_Date = this.param.contract_End_To_Date
  }
  onChangeContractTo() {
    if (this.param.contract_End_To_Date == 'Invalid Date')
      this.deleteProperty('contract_End_To_Date')
    if (this.param.contract_End_From_Date != '' && this.param.contract_End_To_Date < this.param.contract_End_From_Date)
      this.param.contract_End_To_Date = this.param.contract_End_From_Date
  }
}
