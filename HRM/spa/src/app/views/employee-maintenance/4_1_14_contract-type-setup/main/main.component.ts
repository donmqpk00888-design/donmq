import { Component, effect, OnInit } from '@angular/core';
import { ClassButton, IconButton } from '@constants/common.constants';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { ContractTypeSetup_MainMemory, ContractTypeSetupDto, ContractTypeSetupParam, ContractTypeSetupSource } from '@models/employee-maintenance/4_1_14_contract-type-setup';
import { LangChangeEvent } from '@ngx-translate/core';
import { S_4_1_14_ContractTypeSetupService } from '@services/employee-maintenance/s_4_1_14_contract-type-setup.service';
import { InjectBase } from '@utilities/inject-base-app';
import { KeyValuePair } from '@utilities/key-value-pair';
import { Pagination } from '@utilities/pagination-utility';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  programCode: string = '';
  pagination: Pagination = <Pagination>{
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0
  };

  listDivision: KeyValuePair[] = [];
  listFactory: KeyValuePair[] = [];
  listContractType: KeyValuePair[] = [];
  alert: KeyValuePair[] = [
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' },
    { key: '', value: 'EmployeeInformationModule.ContractTypeSetup.All', }
  ];

  proPeriod: KeyValuePair[] = [
    { key: 'Y', value: 'Y' },
    { key: 'N', value: 'N' },
    { key: '', value: 'EmployeeInformationModule.ContractTypeSetup.All' }
  ];
  iconButton = IconButton;
  classButton = ClassButton;
  data: ContractTypeSetupDto[] = [];
  param: ContractTypeSetupParam = <ContractTypeSetupParam>{}
  constructor(
    private service: S_4_1_14_ContractTypeSetupService
  ) {
    super();
    this.programCode = this.route.snapshot.data['program'];
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe((event: LangChangeEvent) => {
      this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
      this.getListDivision();
      this.getListFactory();
      if (this.data.length > 0)
        this.getData(false);
    });
    effect(() => {
      this.param = this.service.paramSearch().param;
      this.pagination = this.service.paramSearch().pagination;
      this.data = this.service.paramSearch().data;
      if (this.data.length > 0) {
        if (this.functionUtility.checkFunction('Search')) {
          if (this.checkRequiredParams())
            this.getData()
        }
        else
          this.clear()
      }
    })
  }
  checkRequiredParams(): boolean {
    var result = !this.functionUtility.checkEmpty(this.param.division) &&
      !this.functionUtility.checkEmpty(this.param.factory)
    return result;
  }
  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getDataFromSource();
    this.getLisContractType();
    this.getListDivision();
    this.getListFactory();
  }

  ngOnDestroy(): void {
    this.service.setParamSearch(<ContractTypeSetup_MainMemory>{ param: this.param, pagination: this.pagination, data: this.data });
  }


  getListDivision() {
    this.service.getListDivision().subscribe({
      next: (res) => this.listDivision = res
    });
  }

  getDataFromSource() {
    this.service.contractTypeSetupSource$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(source => {
      if (source && source != null) {
        this.pagination = source.pagination;
        this.param = source.paramQuery;
      }
    })
  }

  getLisContractType() {
    this.service.getListContractType(this.param.division, this.param.factory).subscribe({
      next: (res) =>  this.listContractType = res
    });
  }

  onDivisionChange() {
    this.deleteProperty('factory')
    this.deleteProperty('contract_Type')
    if (!this.functionUtility.checkEmpty(this.param.division)) {
      this.getListFactory();
      this.onFactoryChange();
    }
    else
      this.listFactory = [];
    this.listContractType = [];
  }

  onFactoryChange() {
    this.deleteProperty('contract_Type')
    this.getLisContractType();
  }

  getListFactory() {
    this.service.getListFactory(this.param.division).subscribe({
      next: (res) => {
        this.listFactory = res;
      }
    });
  }

  getData(isSearch?: boolean) {
    this.spinnerService.show();
    this.service.getData(this.pagination, this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        this.data = res.result;
        this.pagination = res.pagination;
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
      }
    });
  }

  search = (isSearch: boolean) =>
    this.pagination.pageNumber === 1 ? this.getData(isSearch) : this.pagination.pageNumber = 1;

  clear() {
    this.param = <ContractTypeSetupParam>{
      probationary_Period_Str: '',
      alert_Str: ''
    }
    this.data = []
    this.pagination.totalCount = 0;
    this.listFactory = this.listContractType = [];
  }

  redirectToForm = (isEdit: boolean = false) => this.router.navigate([`employee-maintenance/contract-type-setup/${isEdit ? 'edit' : 'add'}`]);

  add() {
    let source = <ContractTypeSetupSource>{
      pagination: this.pagination,
      paramQuery: this.param,
    }
    this.service.setSource(source);
    this.redirectToForm();
  }

  edit(item: ContractTypeSetupDto) {
    let source = <ContractTypeSetupSource>{
      source: { ...item },
      pagination: this.pagination,
      paramQuery: this.param,
    }
    this.service.setSource(source);
    this.redirectToForm(true);
  }

  download() {
    this.spinnerService.show();
    this.service.downloadExcel(this.param).subscribe({
      next: (res) => {
        this.spinnerService.hide();
        if (res.isSuccess) {
          const fileName = this.functionUtility.getFileNameExport(this.programCode, 'Download')
          this.functionUtility.exportExcel(res.data, fileName);
        }
        else this.functionUtility.snotifySuccessError(res.isSuccess, res.error, false);
      }
    });
  }

  deleteItem(item: ContractTypeSetupParam) {
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Action.Delete'), () => {
      this.spinnerService.show();
      this.service.delete(item).subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.functionUtility.snotifySuccessError(res.isSuccess,res.isSuccess ? 'System.Message.DeleteOKMsg' : 'System.Message.DeleteErrorMsg')
          if (res.isSuccess) {
            this.getData();
            this.getLisContractType();
          }
        }
      })
    });
  }

  pageChanged(event: any) {
    this.pagination.pageNumber = event.page;
    this.getData();
  }
  deleteProperty(name: string) {
    delete this.param[name]
  }
}
