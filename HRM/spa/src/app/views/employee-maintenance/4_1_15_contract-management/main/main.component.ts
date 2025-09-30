import { Component, effect, OnDestroy, OnInit } from '@angular/core';
import { IconButton } from '@constants/common.constants';
import { Contract_ManagementParamSource, ContractManagementDto, ContractManagementParam } from '@models/employee-maintenance/4_1_15_contract-management';
import { S_4_1_15_ContractManagementService } from '@services/employee-maintenance/s_4_1_15_contract-management.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { ModalService } from '@services/modal.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit, OnDestroy {
  title: string = '';
  iconButton = IconButton;
  param: ContractManagementParam = <ContractManagementParam>{};
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
  }
  key: KeyValuePair[] = [
    { key: 'All', value: 'All' },
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' }
  ];
  data: ContractManagementDto[] = [];
  division: KeyValuePair[] = [];
  factory: KeyValuePair[] = [];
  department: KeyValuePair[] = [];
  contractType: KeyValuePair[] = [];
  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: 'YYYY/MM/DD'
  };
  minDate: Date = new Date(2000, 0, 1);
  constructor(
    private service: S_4_1_15_ContractManagementService,
    private modalService: ModalService
  ) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.loadData()
    });
    this.modalService.onHide.pipe(takeUntilDestroyed()).subscribe((res: any) => {
      if (res.isSave) {
        this.param.division = res.division;
        this.param.factory = res.factory;
        this.getListFactory()
        if (this.functionUtility.checkFunction('Search'))
          this.getData();
      }
    })
    this.getDataFromSource()
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
  }
  ngOnDestroy(): void {
    this.service.setSource(<Contract_ManagementParamSource>{ param: this.param, currentPage: this.pagination, dataMain: this.data });
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
    this.loadDropdownList()
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
  loadDropdownList() {
    this.getListDivision();
    this.getListFactory();
    this.getListContractType();
    this.getListDepartment();
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

  onAdd() {
    const data = {
      data: {
        seq: 0,
        effective_Status: true
      },
      isEdit: false,
    }
    this.modalService.open(data);
  }

  onEdit(item: ContractManagementDto) {
    const data = {
      data: { ...item },
      isEdit: true,
    }
    this.modalService.open(data);
  }

  onDelete(item: ContractManagementDto) {
    this.functionUtility.snotifyConfirmDefault(() => {
      this.spinnerService.show()
      this.service.delete(item).subscribe({
        next: (res) => {
          this.spinnerService.hide()
          this.functionUtility.snotifySuccessError(res.isSuccess, res.isSuccess ? 'System.Message.DeleteOKMsg' : 'System.Message.DeleteErrorMsg')
          if (res.isSuccess) this.getData();
        }
      })
    });
  }

  getListDivision() {
    this.service.getListDivision().subscribe({
      next: res => this.division = res
    })
  }
  getListFactory() {
    this.service.getListFactory(this.param.division).subscribe({
      next: res => this.factory = res
    })
  }

  getListDepartment() {
    this.service.getListDepartment(this.param.division, this.param.factory).subscribe({
      next: res => this.department = res
    })
  }

  getListContractType() {
    this.service.getListContractType('', '').subscribe({
      next: res => {
        this.contractType = res
      }
    })
  }

  onSelectDivision() {
    this.deleteProperty('factory')
    this.getListFactory()
    this.onSelectFactory()
  }

  onSelectFactory() {
    this.deleteProperty('department')
    this.getListDepartment()
  }

  conditionSearch() {
    this.param.onboard_Date_From = !this.functionUtility.checkEmpty(this.param.onboard_Date_From_Date) ? this.functionUtility.getDateFormat(new Date(this.param.onboard_Date_From_Date)) : '';
    this.param.onboard_Date_To = !this.functionUtility.checkEmpty(this.param.onboard_Date_To_Date) ? this.functionUtility.getDateFormat(new Date(this.param.onboard_Date_To_Date)) : '';
    this.param.contract_End_From = !this.functionUtility.checkEmpty(this.param.contract_End_From_Date) ? this.functionUtility.getDateFormat(new Date(this.param.contract_End_From_Date)) : '';
    this.param.contract_End_To = !this.functionUtility.checkEmpty(this.param.contract_End_To_Date) ? this.functionUtility.getDateFormat(new Date(this.param.contract_End_To_Date)) : '';
    this.param.probation_End_From = !this.functionUtility.checkEmpty(this.param.probation_End_From_Date) ? this.functionUtility.getDateFormat(new Date(this.param.probation_End_From_Date)) : '';
    this.param.probation_End_To = !this.functionUtility.checkEmpty(this.param.probation_End_To_Date) ? this.functionUtility.getDateFormat(new Date(this.param.probation_End_To_Date)) : '';
  }

  search(isSearch: boolean) {
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;
  }

  clear() {
    this.data = []
    this.pagination.totalCount = 0;
    this.param = <ContractManagementParam>{
      effectiveStatus: 'Y'
    }
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }

  deleteProperty(name: string) {
    delete this.param[name]
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
  onChangeProbationFrom() {
    if (this.param.probation_End_From_Date == 'Invalid Date')
      this.deleteProperty('probation_End_From_Date')
    if (this.param.probation_End_To_Date != '' && this.param.probation_End_From_Date > this.param.probation_End_To_Date)
      this.param.probation_End_From_Date = this.param.probation_End_To_Date
  }
  onChangeProbationTo() {
    if (this.param.probation_End_To_Date == 'Invalid Date')
      this.deleteProperty('probation_End_To_Date')
    if (this.param.probation_End_From_Date != '' && this.param.probation_End_To_Date < this.param.probation_End_From_Date)
      this.param.probation_End_To_Date = this.param.probation_End_From_Date
  }
}
