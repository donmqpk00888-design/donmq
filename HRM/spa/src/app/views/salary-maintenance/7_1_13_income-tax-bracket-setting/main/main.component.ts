import { Component, OnInit } from '@angular/core';
import { InjectBase } from '@utilities/inject-base-app';
import { S_7_1_13_IncomeTaxBracketSettingService } from "@services/salary-maintenance/s_7_1_13_income-tax-bracket-setting.service";
import { Pagination } from '@utilities/pagination-utility';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { IncomeTaxBracketSettingDto, IncomeTaxBracketSettingMain, IncomeTaxBracketSettingMemory, IncomeTaxBracketSettingParam } from '@models/salary-maintenance/7_1_13_income-tax-bracket-setting';
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
  param: IncomeTaxBracketSettingParam = <IncomeTaxBracketSettingParam>{};
  dataMain: IncomeTaxBracketSettingMain[] = [];
  selectedData: IncomeTaxBracketSettingDto;

  pagination: Pagination = <Pagination>{};

  bsConfig: Partial<BsDatepickerConfig> = {
    dateInputFormat: "YYYY/MM",
    minMode: "month"
  };

  start_Effective_Month: Date;
  end_Effective_Month: Date;
  listNationality: KeyValuePair[] = [];
  listTaxCode: KeyValuePair[] = [];

  iconButton = IconButton;
  placeholder = Placeholder;

  constructor(private service: S_7_1_13_IncomeTaxBracketSettingService) {
    super();
    this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
        this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
        this.loadDropdownList();
        this.processData()
      });
  }

  ngOnInit(): void {
    this.title = this.functionUtility.getTitle(this.route.snapshot.data['program'])
    this.getSource()
  }

  ngOnDestroy(): void {
    // case add or out page
    const source = <IncomeTaxBracketSettingMemory>{
      pagination: this.pagination,
      param: this.param,
      data: this.dataMain,
      selectedData: this.selectedData
    };
    this.service.setParamSearch(source);
  }
  getSource() {
    this.param = this.service.paramSearch().param;
    this.pagination = this.service.paramSearch().pagination;
    this.dataMain = this.service.paramSearch().data;
    if (this.functionUtility.isValidDate(new Date(this.param.start_Effective_Month)))
      this.start_Effective_Month = new Date(this.param.start_Effective_Month);
    if (this.functionUtility.isValidDate(new Date(this.param.end_Effective_Month)))
      this.end_Effective_Month = new Date(this.param.end_Effective_Month);
    this.loadDropdownList();
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
    return !this.functionUtility.checkEmpty(this.param.nationality);
  }

  deleteProperty(name: string) {
    delete this.param[name]
  }

  onChangeEffectiveMonth(name: string) {
    this.param[name] = this.functionUtility.isValidDate(this[name] as Date) ? this[name].toStringYearMonth() : ''
  }

  //#region  Search
  search = (isSearch: boolean) => {
    this.pagination.pageNumber == 1 ? this.getData(isSearch, false) : this.pagination.pageNumber = 1;
  };

  pageChanged(e: PageChangedEvent) {
    this.pagination.pageNumber = e.page;
    this.getData();
  }

  getData = (isSearch?: boolean, isDelete?: boolean) => {
    this.spinnerService.show();
    this.service.getDataPagination(this.pagination, this.param).subscribe({
      next: (res) => {
        this.dataMain = res.result;
        this.pagination = res.pagination;
        this.dataMain.forEach(x =>
          x.effective_Month_Str = this.functionUtility.isValidDate(new Date(x.effective_Month))
            ? new Date(x.effective_Month).toStringYearMonth()
            : ''
        )
        if (isSearch)
          this.functionUtility.snotifySuccessError(true, 'System.Message.QuerySuccess')
        if (isDelete)
          this.functionUtility.snotifySuccessError(true, 'System.Message.DeleteOKMsg')
        this.spinnerService.hide()
      }
    })
  };


  clear() {
    this.start_Effective_Month = null;
    this.end_Effective_Month = null;
    this.dataMain = [];
    this.param = <IncomeTaxBracketSettingParam>{};
    this.pagination = <Pagination>{
      pageNumber: 1,
      pageSize: 10,
      totalPage: 0,
      totalCount: 0
    };
  }
  //#endregion

  //#region Add, Edit, Delete
  onAdd() {
    this.router.navigate([`${this.router.routerState.snapshot.url}/add`]);
  }

  async onEdit(item: IncomeTaxBracketSettingMain) {
    this.selectedData = await this.getDetail({ ...item } as IncomeTaxBracketSettingDto)
    this.router.navigate([`${this.router.routerState.snapshot.url}/edit`]);
  }
  getDetail(item: IncomeTaxBracketSettingDto): Promise<IncomeTaxBracketSettingDto> {
    return new Promise((resolve) => {
      this.spinnerService.show();
      this.service.getDetail(item).subscribe({
        next: (res) => {
          this.spinnerService.hide();
          resolve(res)
        }
      })
    })
  }
  onDelete(item: IncomeTaxBracketSettingMain) {
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

  //#region Load & get list
  loadDropdownList() {
    this.getListNationality();
    this.getListTaxCode();
  }

  getListNationality() {
    this.service.getListNationality().subscribe({
      next: res => {
        this.listNationality = res;
      }
    });
  }

  getListTaxCode() {
    this.service.getListTaxCode().subscribe({
      next: res => {
        this.listTaxCode = res;
      }
    });
  }
  //#endregion
}
