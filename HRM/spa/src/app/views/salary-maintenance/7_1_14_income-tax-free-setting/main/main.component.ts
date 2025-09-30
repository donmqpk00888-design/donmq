import { Component, OnInit } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { S_7_1_14_IncomeTaxFreeSettingService } from "@services/salary-maintenance/s_7_1_14_income-tax-free-setting.service";
import { IncomeTaxFreeSetting_MainData, IncomeTaxFreeSettingMemory, IncomeTaxFreeSetting_MainParam, IncomeTaxFreeSetting_SubParam } from '@models/salary-maintenance/7_1_14_income-tax-free-setting';
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { IconButton, Placeholder } from '@constants/common.constants';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import { KeyValuePair } from '@utilities/key-value-pair';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent extends InjectBase implements OnInit {
  title: string = '';
  param: IncomeTaxFreeSetting_MainParam = <IncomeTaxFreeSetting_MainParam>{};
  dataMain: IncomeTaxFreeSetting_MainData[] = [];
  selectedData: IncomeTaxFreeSetting_MainData
  pagination: Pagination = <Pagination>{}

  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: "YYYY/MM",
    minMode: "month"
  };

  start_Effective_Month: Date;
  end_Effective_Month: Date;
  listFactory: KeyValuePair[] = [];
  listType: KeyValuePair[] = [];
  listSalaryType: KeyValuePair[] = [];

  iconButton = IconButton;
  placeholder = Placeholder;

  constructor(private service: S_7_1_14_IncomeTaxFreeSettingService) {
    super()
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.loadDropdownList();
        this.processData()
      });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource();
  }

  ngOnDestroy(): void {
    // case add or out page
    this.service.setParamSearch(<IncomeTaxFreeSettingMemory>{
      pagination: this.pagination,
      param: this.param,
      data: this.dataMain,
      selectedData: this.selectedData
    });
  }
  getSource() {
    this.param = this.service.paramSearch().param;
    this.pagination = this.service.paramSearch().pagination;
    this.dataMain = this.service.paramSearch().data;
    if (this.functionUtility.isValidDate(new Date(this.param.start_Effective_Month)))
      this.start_Effective_Month = new Date(this.param.start_Effective_Month);
    if (this.functionUtility.isValidDate(new Date(this.param.end_Effective_Month)))
      this.end_Effective_Month = new Date(this.param.end_Effective_Month);
    this.loadDropdownList()
    this.processData()
  }
  processData() {
    if (this.dataMain.length > 0) {
      if (this.functionUtility.checkFunction('Search') && this.checkRequiredParams()) {
        this.getData()
      }
      else
        this.clear()
    }
  }
  checkRequiredParams(): boolean {
    return !this.functionUtility.checkEmpty(this.param.factory);
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }

  onChangeEffectiveMonth(name: string) {
    this.param[name] = this[name] != null ? this[name].toStringYearMonth() : ''
  }

  //#region Initialization
  loadDropdownList() {
    this.getListFactory();
    this.getListType();
    this.getListSalaryType();
  }
  //#endregion

  //#region  Search
  search = (isSearch: boolean) => {
    this.pagination.pageNumber == 1 ? this.getData(isSearch, false) : this.pagination.pageNumber = 1;
  };

  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData();
  }

  getData = (isSearch?: boolean, isDelete?: boolean) => {
    return new Promise<void>((resolve, reject) => {
      this.spinnerService.show();
      this.service.getDataPagination(this.pagination, this.param).subscribe({
        next: (res) => {
          this.spinnerService.hide();
          this.dataMain = res.result;
          this.pagination = res.pagination;
          if (isSearch)
            this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
          if (isDelete)
            this.functionUtility.snotifySuccessError(true, 'System.Message.DeleteOKMsg')
          resolve()
        },
        error: () => { reject() }
      })
    })
  };


  clear() {
    this.dataMain = [];
    this.param = this.service.initData.param
    this.pagination = this.service.initData.pagination
    this.start_Effective_Month = null;
    this.end_Effective_Month = null;
  }
  //#endregion

  //#region Add, Edit, Delete
  onAdd() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  onEdit(item: IncomeTaxFreeSetting_MainData) {
    this.selectedData = item
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }

  onDelete(item: IncomeTaxFreeSetting_MainData) {
    this.snotifyService.confirm(this.translateService.instant('System.Message.ConfirmDelete'), this.translateService.instant('System.Caption.Confirm'), async () => {
      this.spinnerService.show();
      this.service.delete(item).subscribe({
        next: res => {
          if (res.isSuccess) {
            this.getData(false, true);
          } else {
            this.functionUtility.snotifySuccessError(res.isSuccess, res.error);
          }
          this.spinnerService.hide();
        }
      });
    });
  }
  //#endregion

  //#region Get list
  getListFactory() {
    this.service.getListFactoryByUser().subscribe({
      next: res => {
        this.listFactory = res;
      }
    });
  }

  getListType() {
    this.service.getListType().subscribe({
      next: res => {
        this.listType = res;
      }
    });
  }

  getListSalaryType() {
    this.service.getListSalaryType().subscribe({
      next: res => {
        this.listSalaryType = res;
      }
    });
  }
  //#endregion

}
